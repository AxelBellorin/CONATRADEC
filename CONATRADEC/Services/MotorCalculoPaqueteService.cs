using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga y activa de forma atómica el paquete del motor de cálculo.
    ///
    /// El archivo anterior no se reemplaza hasta que la nueva descarga supera
    /// la validación de esquema, contenido y hash SHA-256.
    /// </summary>
    public sealed class MotorCalculoPaqueteService
    {
        private const int VersionEsquemaSoportada = 2;

        private static readonly Lazy<MotorCalculoPaqueteService> lazy =
            new(() => new MotorCalculoPaqueteService());

        private readonly SemaphoreSlim syncLock = new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

        private MotorCalculoPaquete? cache;
        private string cacheUsuarioId = string.Empty;

        public static MotorCalculoPaqueteService Instance =>
            lazy.Value;

        public event EventHandler? PaqueteCambiado;

        private MotorCalculoPaqueteService()
        {
        }

        public string ObtenerRutaPaqueteActual()
        {
            string usuarioId = ObtenerUsuarioId();
            string directorio = Path.Combine(
                FileSystem.AppDataDirectory,
                "motor-calculo",
                usuarioId);

            Directory.CreateDirectory(directorio);

            return Path.Combine(
                directorio,
                "motor-calculo-activo.json");
        }

        public long ObtenerTamanoPaqueteBytes()
        {
            try
            {
                string ruta = ObtenerRutaPaqueteActual();
                return File.Exists(ruta)
                    ? new FileInfo(ruta).Length
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<MotorCalculoPaquete?>
            ObtenerPaqueteActivoAsync(
                CancellationToken cancellationToken = default)
        {
            string usuarioId = ObtenerUsuarioId();

            if (cache != null &&
                string.Equals(
                    cacheUsuarioId,
                    usuarioId,
                    StringComparison.Ordinal))
            {
                return cache;
            }

            string ruta = ObtenerRutaPaqueteActual();

            if (!File.Exists(ruta))
                return null;

            try
            {
                string json =
                    await File.ReadAllTextAsync(
                        ruta,
                        cancellationToken);

                MotorCalculoPaquete? paquete =
                    JsonSerializer.Deserialize<
                        MotorCalculoPaquete>(
                        json,
                        jsonOptions);

                if (paquete == null ||
                    !ValidarPaquete(paquete, out _))
                {
                    return null;
                }

                cache = paquete;
                cacheUsuarioId = usuarioId;

                return paquete;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> TienePaqueteValidoAsync(
            CancellationToken cancellationToken = default) =>
            await ObtenerPaqueteActivoAsync(
                cancellationToken) != null;

        public async Task<ResultadoDescargaMotor>
            DescargarOActualizarAsync(
                bool forzar,
                CancellationToken cancellationToken = default)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return ResultadoDescargaMotor.Fail(
                    "Su usuario no tiene habilitado el trabajo sin conexión.");
            }

            if (!ModoSesionService.EsEnLinea)
            {
                return ResultadoDescargaMotor.Fail(
                    "El motor solamente puede descargarse durante una sesión en línea.");
            }

            await syncLock.WaitAsync(cancellationToken);

            try
            {
                MotorCalculoPaquete? actual =
                    await ObtenerPaqueteActivoAsync(
                        cancellationToken);

                if (!forzar && actual != null)
                {
                    MotorCalculoEstado? estado =
                        await ConsultarEstadoServidorAsync(
                            cancellationToken);

                    if (estado != null &&
                        string.Equals(
                            estado.HashSha256,
                            actual.HashSha256,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return ResultadoDescargaMotor.Ok(
                            "El motor de cálculo ya está actualizado.",
                            actual.VersionPaquete,
                            ContarRegistros(actual),
                            actualizado: false);
                    }
                }

                using HttpRequestMessage request =
                    new(
                        HttpMethod.Get,
                        "api/motor-calculo/paquete");

                using HttpResponseMessage response =
                    await ApiClientService.Client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                string jsonRespuesta =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ResultadoDescargaMotor.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            jsonRespuesta,
                            "No fue posible descargar el motor de cálculo."));
                }

                ApiEnvelopeMotor<MotorCalculoPaquete>? envelope =
                    JsonSerializer.Deserialize<
                        ApiEnvelopeMotor<MotorCalculoPaquete>>(
                        jsonRespuesta,
                        jsonOptions);

                MotorCalculoPaquete? nuevo =
                    envelope?.Data;

                if (envelope?.Success != true ||
                    nuevo == null)
                {
                    return ResultadoDescargaMotor.Fail(
                        envelope?.Message ??
                        "La API no devolvió un paquete válido.");
                }

                if (!ValidarPaquete(
                        nuevo,
                        out string errorValidacion))
                {
                    return ResultadoDescargaMotor.Fail(
                        errorValidacion);
                }

                string rutaActual =
                    ObtenerRutaPaqueteActual();

                string rutaTemporal =
                    rutaActual + ".tmp";

                string jsonPaquete =
                    JsonSerializer.Serialize(
                        nuevo,
                        jsonOptions);

                await File.WriteAllTextAsync(
                    rutaTemporal,
                    jsonPaquete,
                    Encoding.UTF8,
                    cancellationToken);

                /*
                 * Relectura del archivo temporal: evita activar un archivo
                 * incompleto si el almacenamiento falló.
                 */
                string jsonVerificacion =
                    await File.ReadAllTextAsync(
                        rutaTemporal,
                        cancellationToken);

                MotorCalculoPaquete? verificado =
                    JsonSerializer.Deserialize<
                        MotorCalculoPaquete>(
                        jsonVerificacion,
                        jsonOptions);

                if (verificado == null ||
                    !ValidarPaquete(
                        verificado,
                        out errorValidacion))
                {
                    File.Delete(rutaTemporal);

                    return ResultadoDescargaMotor.Fail(
                        errorValidacion);
                }

                File.Move(
                    rutaTemporal,
                    rutaActual,
                    overwrite: true);

                cache = verificado;
                cacheUsuarioId = ObtenerUsuarioId();

                PaqueteCambiado?.Invoke(
                    this,
                    EventArgs.Empty);

                return ResultadoDescargaMotor.Ok(
                    "Motor de cálculo descargado correctamente.",
                    verificado.VersionPaquete,
                    ContarRegistros(verificado),
                    actualizado: true);
            }
            catch (OperationCanceledException)
            {
                return ResultadoDescargaMotor.Fail(
                    "La descarga del motor fue cancelada.");
            }
            catch (Exception ex)
            {
                return ResultadoDescargaMotor.Fail(
                    $"No fue posible descargar el motor: {ex.Message}");
            }
            finally
            {
                syncLock.Release();
            }
        }

        private async Task<MotorCalculoEstado?>
            ConsultarEstadoServidorAsync(
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await ApiClientService.Client.GetAsync(
                        "api/motor-calculo/estado",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                ApiEnvelopeMotor<MotorCalculoEstado>? envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiEnvelopeMotor<MotorCalculoEstado>>(
                        jsonOptions,
                        cancellationToken);

                return envelope?.Success == true
                    ? envelope.Data
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private bool ValidarPaquete(
            MotorCalculoPaquete paquete,
            out string mensaje)
        {
            if (paquete.VersionEsquema !=
                VersionEsquemaSoportada)
            {
                mensaje =
                    "La versión del paquete no es compatible con esta aplicación.";
                return false;
            }

            if (paquete.Modulos?.RequerimientoAnual != true ||
                paquete.Modulos.EnmiendaCalcarea != true ||
                paquete.Modulos.BalanceFormula != true ||
                paquete.Modulos.FertilizacionMixta != true ||
                paquete.Modulos.GuardadoLocal != true ||
                paquete.Modulos.Sincronizacion != true ||
                paquete.Modulos.ReportePdfLocal != true)
            {
                mensaje =
                    "El paquete no contiene todos los módulos necesarios para trabajar sin conexión.";
                return false;
            }

            if (paquete.Contenido == null ||
                paquete.Contenido.UnidadResultadoId <= 0 ||
                paquete.Contenido.UnidadRangoKgHaId <= 0)
            {
                mensaje =
                    "El paquete no contiene las unidades internas requeridas.";
                return false;
            }

            if (paquete.Contenido.Elementos.Count == 0 ||
                paquete.Contenido.ConversionesElementos.Count == 0 ||
                paquete.Contenido.ConversionesMateriaOrganica.Count == 0)
            {
                mensaje =
                    "El paquete no contiene todas las reglas de conversión.";
                return false;
            }

            if (paquete.Contenido.FuentesNutrientes.Count == 0 ||
                paquete.Contenido.AportesFuentes.Count == 0)
            {
                mensaje =
                    "El paquete no contiene las fuentes, precios y composiciones requeridas.";
                return false;
            }

            if (paquete.Contenido.ParametrosEnmiendaCalcarea.Count == 0)
            {
                mensaje =
                    "El paquete no contiene parámetros de enmienda calcárea.";
                return false;
            }

            if (paquete.Contenido.FuentesFertilizacionMixtaIds.Count == 0)
            {
                mensaje =
                    "El paquete no contiene fuentes habilitadas para fertilización mixta.";
                return false;
            }

            string jsonContenido =
                JsonSerializer.Serialize(
                    paquete.Contenido,
                    jsonOptions);

            string hashCalculado =
                Convert.ToHexString(
                        SHA256.HashData(
                            Encoding.UTF8.GetBytes(
                                jsonContenido)))
                    .ToLowerInvariant();

            if (!string.Equals(
                    hashCalculado,
                    paquete.HashSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                mensaje =
                    "El paquete descargado no superó la validación de integridad.";
                return false;
            }

            mensaje = string.Empty;
            return true;
        }

        private static int ContarRegistros(
            MotorCalculoPaquete paquete)
        {
            MotorCalculoContenido contenido =
                paquete.Contenido;

            return
                contenido.TiposCultivo.Count +
                contenido.TiposAnalisis.Count +
                contenido.Elementos.Count +
                contenido.Unidades.Count +
                contenido.ConversionesElementos.Count +
                contenido.ConversionesMateriaOrganica.Count +
                contenido.ParametrosExtraccion.Count +
                contenido.RangosCultivo.Count +
                contenido.FuentesNutrientes.Count +
                contenido.AportesFuentes.Count +
                contenido.ParametrosEnmiendaCalcarea.Count +
                contenido.FuentesFertilizacionMixtaIds.Count;
        }

        private static string ObtenerUsuarioId()
        {
            string valor =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    "0");

            return string.IsNullOrWhiteSpace(valor)
                ? "0"
                : valor.Trim();
        }
    }
}
