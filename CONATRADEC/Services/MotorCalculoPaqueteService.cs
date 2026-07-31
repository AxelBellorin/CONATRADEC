using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        /*
         * La versión 3 exige expresamente los catálogos utilizados por el
         * formulario, las conversiones por elemento, materia orgánica y los
         * rangos nutricionales. Los paquetes anteriores deben descargarse otra
         * vez para evitar continuar con datos incompletos.
         */
        private const int VersionEsquemaSoportada = 3;

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
                    LimpiarCache();
                    return null;
                }

                cache = paquete;
                cacheUsuarioId = usuarioId;

                return paquete;
            }
            catch
            {
                LimpiarCache();
                return null;
            }
        }

        public async Task<bool> TienePaqueteValidoAsync(
            CancellationToken cancellationToken = default) =>
            await ObtenerPaqueteActivoAsync(
                cancellationToken) != null;

        public void InvalidarCache()
        {
            LimpiarCache();
        }

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
                        estado.VersionEsquema ==
                            VersionEsquemaSoportada &&
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

                AnalisisOfflineFormularioValidacionService.Instance
                    .Invalidar();

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
                    "La versión del paquete no es compatible con esta aplicación. Ejecute Descargar todo nuevamente.";
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

            MotorCalculoContenido? contenido = paquete.Contenido;

            if (contenido == null ||
                contenido.UnidadResultadoId <= 0 ||
                contenido.UnidadRangoKgHaId <= 0)
            {
                mensaje =
                    "El paquete no contiene las unidades internas requeridas.";
                return false;
            }

            List<MotorTipoCultivo> cultivos =
                contenido.TiposCultivo
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoCultivoId > 0 &&
                        !string.IsNullOrWhiteSpace(
                            x.NombreTipoCultivo))
                    .ToList();

            List<MotorTipoAnalisis> tiposAnalisis =
                contenido.TiposAnalisis
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoAnalisisSueloId > 0)
                    .ToList();

            List<MotorElemento> elementos =
                contenido.Elementos
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId > 0)
                    .ToList();

            List<MotorUnidad> unidades =
                contenido.Unidades
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.UnidadMedidaId > 0 &&
                        !string.IsNullOrWhiteSpace(
                            x.NombreUnidadMedida))
                    .ToList();

            if (cultivos.Count == 0 ||
                tiposAnalisis.Count == 0 ||
                elementos.Count == 0 ||
                unidades.Count == 0)
            {
                mensaje =
                    "El paquete no contiene todos los catálogos necesarios para crear un análisis.";
                return false;
            }

            if (contenido.ConversionesElementos.Count == 0 ||
                contenido.ConversionesMateriaOrganica.Count == 0)
            {
                mensaje =
                    "El paquete no contiene todas las reglas de conversión.";
                return false;
            }

            HashSet<int> unidadesIds =
                unidades.Select(x => x.UnidadMedidaId).ToHashSet();

            HashSet<int> elementosIds =
                elementos.Select(x => x.ElementoQuimicosId).ToHashSet();

            HashSet<int> cultivosIds =
                cultivos.Select(x => x.TipoCultivoId).ToHashSet();

            foreach (MotorElemento elemento in elementos)
            {
                bool tieneConversion =
                    contenido.ConversionesElementos.Any(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId ==
                            elemento.ElementoQuimicosId &&
                        unidadesIds.Contains(x.UnidadMedidaId));

                if (!tieneConversion)
                {
                    mensaje =
                        $"El paquete no contiene unidades válidas para {ObtenerNombreElemento(elemento)}.";
                    return false;
                }
            }

            if (!contenido.ConversionesMateriaOrganica.Any(x =>
                    x != null &&
                    x.Activo &&
                    unidadesIds.Contains(x.UnidadMedidaId)))
            {
                mensaje =
                    "El paquete no contiene una unidad válida para materia orgánica.";
                return false;
            }

            if (!contenido.RangosCultivo.Any(x =>
                    x != null &&
                    x.Activo &&
                    cultivosIds.Contains(x.TipoCultivoId) &&
                    elementosIds.Contains(x.ElementoQuimicosId)))
            {
                mensaje =
                    "El paquete no contiene rangos nutricionales válidos.";
                return false;
            }

            if (contenido.FuentesNutrientes.Count == 0 ||
                contenido.AportesFuentes.Count == 0)
            {
                mensaje =
                    "El paquete no contiene las fuentes, precios y composiciones requeridas.";
                return false;
            }

            if (contenido.ParametrosEnmiendaCalcarea.Count == 0)
            {
                mensaje =
                    "El paquete no contiene parámetros de enmienda calcárea.";
                return false;
            }

            if (contenido.FuentesFertilizacionMixtaIds.Count == 0)
            {
                mensaje =
                    "El paquete no contiene fuentes habilitadas para fertilización mixta.";
                return false;
            }

            string jsonContenido =
                JsonSerializer.Serialize(
                    contenido,
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

        private static string ObtenerNombreElemento(
            MotorElemento elemento)
        {
            string nombre =
                elemento.NombreElementoQuimico?.Trim() ??
                string.Empty;

            string simbolo =
                elemento.SimboloElementoQuimico?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = $"Elemento #{elemento.ElementoQuimicosId}";

            return string.IsNullOrWhiteSpace(simbolo)
                ? nombre
                : $"{nombre} ({simbolo})";
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

        private void LimpiarCache()
        {
            cache = null;
            cacheUsuarioId = string.Empty;
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
