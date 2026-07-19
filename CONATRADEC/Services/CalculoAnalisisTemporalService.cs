using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ======= SERVICIO: CalculoAnalisisTemporalService ==========
    // ===========================================================
    // Este servicio mantiene en memoria y en archivo local temporal
    // TODO el flujo de cálculos del análisis actual.
    //
    // Reglas:
    // 1. Solo existe un cálculo temporal activo.
    // 2. Un nuevo análisis reemplaza al anterior.
    // 3. Cada cálculo guarda su request y su resultado.
    // 4. Si el usuario modifica datos después de calcular, el cálculo
    //    debe marcarse como pendiente de recalcular.
    // 5. La copia local se actualiza cada vez que cambia el estado.
    // ===========================================================

    class CalculoAnalisisTemporalService
    {
        // ===========================================================
        // ======================== SINGLETON ========================
        // ===========================================================

        private static readonly Lazy<CalculoAnalisisTemporalService> instancia =
            new Lazy<CalculoAnalisisTemporalService>(() => new CalculoAnalisisTemporalService());

        public static CalculoAnalisisTemporalService Instance => instancia.Value;

        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        private const string NombreArchivoTemporal = "calculo_analisis_temporal.json";

        private CalculoAnalisisTemporalState estadoActual = new CalculoAnalisisTemporalState();

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly SemaphoreSlim archivoLock = new(1, 1);

        private string RutaArchivoTemporal =>
            Path.Combine(FileSystem.AppDataDirectory, NombreArchivoTemporal);

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        private CalculoAnalisisTemporalService()
        {
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        public CalculoAnalisisTemporalState ObtenerEstadoActual()
        {
            return estadoActual;
        }

        public async Task IniciarNuevoCalculoAsync(
            AnalisisSueloCalculoDataResponse? resultadoAnalisis,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            string nuevaClave = ConstruirClaveCalculo(resultadoAnalisis, requestGuardar);

            await CargarDesdeArchivoAsync();

            bool esMismoCalculo =
                !string.IsNullOrWhiteSpace(estadoActual.CalculoKey) &&
                string.Equals(estadoActual.CalculoKey, nuevaClave, StringComparison.Ordinal);

            if (!esMismoCalculo)
            {
                estadoActual = new CalculoAnalisisTemporalState
                {
                    CalculoKey = nuevaClave,
                    FechaCreacion = DateTime.Now,
                    FechaUltimaModificacion = DateTime.Now,
                    ResultadoAnalisisSuelo = resultadoAnalisis,
                    RequestGuardarAnalisis = requestGuardar
                };
            }
            else
            {
                estadoActual.ResultadoAnalisisSuelo = resultadoAnalisis;
                estadoActual.RequestGuardarAnalisis = requestGuardar;
                estadoActual.FechaUltimaModificacion = DateTime.Now;
            }

            await GuardarCalculoAsync(
                TipoCalculoTemporal.RequerimientoAnual,
                requestGuardar,
                resultadoAnalisis,
                "Requerimiento anual cargado desde el resultado del análisis de suelo."
            );
        }

        public async Task GuardarCalculoAsync<TRequest, TResultado>(
            TipoCalculoTemporal tipoCalculo,
            TRequest? request,
            TResultado? resultado,
            string? mensajeEstado = null)
        {
            Task<string?> tareaRequest =
                SerializarAsync(request);

            Task<string?> tareaResultado =
                SerializarAsync(resultado);

            await Task.WhenAll(
                tareaRequest,
                tareaResultado);

            CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

            seccion.TipoCalculo = tipoCalculo;
            seccion.Estado = EstadoCalculoTemporal.Calculado;
            seccion.RequestJson = await tareaRequest;
            seccion.ResultadoJson = await tareaResultado;

            seccion.FechaCalculo = DateTime.Now;
            seccion.FechaUltimaModificacion = DateTime.Now;
            seccion.MensajeEstado = mensajeEstado ?? "Cálculo actualizado correctamente.";

            estadoActual.FechaUltimaModificacion = DateTime.Now;

            await GuardarEnArchivoAsync();
        }

        public async Task MarcarPendienteRecalculoAsync(
            TipoCalculoTemporal tipoCalculo,
            string? mensajeEstado = null,
            bool limpiarResultado = true)
        {
            CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

            seccion.Estado = EstadoCalculoTemporal.PendienteRecalculo;
            seccion.FechaUltimaModificacion = DateTime.Now;
            seccion.MensajeEstado = mensajeEstado ?? "Hay cambios pendientes. Debe recalcular para actualizar el resultado.";

            if (limpiarResultado)
                seccion.ResultadoJson = null;

            estadoActual.FechaUltimaModificacion = DateTime.Now;

            await GuardarEnArchivoAsync();
        }

        public async Task ReiniciarCalculoAsync(
            TipoCalculoTemporal tipoCalculo,
            string? mensajeEstado = null)
        {
            CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

            seccion.Estado = EstadoCalculoTemporal.Reiniciado;
            seccion.RequestJson = null;
            seccion.ResultadoJson = null;
            seccion.FechaCalculo = null;
            seccion.FechaUltimaModificacion = DateTime.Now;
            seccion.MensajeEstado = mensajeEstado ?? "Cálculo reiniciado.";

            estadoActual.FechaUltimaModificacion = DateTime.Now;

            await GuardarEnArchivoAsync();
        }

        public bool TieneResultadoValido(TipoCalculoTemporal tipoCalculo)
        {
            CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

            return seccion.TieneResultadoValido;
        }

        public TResultado? ObtenerResultado<TResultado>(TipoCalculoTemporal tipoCalculo)
        {
            try
            {
                CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

                if (string.IsNullOrWhiteSpace(seccion.ResultadoJson))
                    return default;

                return JsonSerializer.Deserialize<TResultado>(
                    seccion.ResultadoJson,
                    jsonOptions
                );
            }
            catch
            {
                return default;
            }
        }

        public TRequest? ObtenerRequest<TRequest>(TipoCalculoTemporal tipoCalculo)
        {
            try
            {
                CalculoSeccionTemporalState seccion = ObtenerSeccion(tipoCalculo);

                if (string.IsNullOrWhiteSpace(seccion.RequestJson))
                    return default;

                return JsonSerializer.Deserialize<TRequest>(
                    seccion.RequestJson,
                    jsonOptions
                );
            }
            catch
            {
                return default;
            }
        }

        public async Task GuardarEnArchivoAsync()
        {
            await archivoLock.WaitAsync();

            try
            {
                string json = await Task.Run(() =>
                    JsonSerializer.Serialize(
                        estadoActual,
                        jsonOptions));

                await File.WriteAllTextAsync(RutaArchivoTemporal, json);
            }
            catch
            {
                // Si falla el respaldo local, no rompemos la UI.
                // El estado en memoria sigue disponible durante la sesión.
            }
            finally
            {
                archivoLock.Release();
            }
        }

        public async Task<bool> CargarDesdeArchivoAsync()
        {
            await archivoLock.WaitAsync();

            try
            {
                if (!File.Exists(RutaArchivoTemporal))
                    return false;

                string json = await File.ReadAllTextAsync(RutaArchivoTemporal);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                CalculoAnalisisTemporalState? estado =
                    await Task.Run(() =>
                        JsonSerializer.Deserialize<
                            CalculoAnalisisTemporalState>(
                                json,
                                jsonOptions));

                if (estado == null)
                    return false;

                estadoActual = estado;

                AsegurarSecciones();

                return true;
            }
            catch
            {
                estadoActual = new CalculoAnalisisTemporalState();
                return false;
            }
            finally
            {
                archivoLock.Release();
            }
        }

        public async Task LimpiarTodoAsync()
        {
            estadoActual = new CalculoAnalisisTemporalState();

            try
            {
                if (File.Exists(RutaArchivoTemporal))
                    File.Delete(RutaArchivoTemporal);
            }
            catch
            {
                await Task.CompletedTask;
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================

        private CalculoSeccionTemporalState ObtenerSeccion(TipoCalculoTemporal tipoCalculo)
        {
            AsegurarSecciones();

            return tipoCalculo switch
            {
                TipoCalculoTemporal.RequerimientoAnual => estadoActual.RequerimientoAnual,
                TipoCalculoTemporal.BalanceFormula => estadoActual.BalanceFormula,
                TipoCalculoTemporal.FertilizacionMixta => estadoActual.FertilizacionMixta,
                TipoCalculoTemporal.EnmiendaCalcarea => estadoActual.EnmiendaCalcarea,
                _ => estadoActual.RequerimientoAnual
            };
        }

        private Task<string?> SerializarAsync<T>(T? valor)
        {
            if (valor is null)
                return Task.FromResult<string?>(null);

            return Task.Run<string?>(() =>
                JsonSerializer.Serialize(
                    valor,
                    jsonOptions));
        }

        private void AsegurarSecciones()
        {
            estadoActual.RequerimientoAnual ??= new CalculoSeccionTemporalState
            {
                TipoCalculo = TipoCalculoTemporal.RequerimientoAnual
            };

            estadoActual.BalanceFormula ??= new CalculoSeccionTemporalState
            {
                TipoCalculo = TipoCalculoTemporal.BalanceFormula
            };

            estadoActual.FertilizacionMixta ??= new CalculoSeccionTemporalState
            {
                TipoCalculo = TipoCalculoTemporal.FertilizacionMixta
            };

            estadoActual.EnmiendaCalcarea ??= new CalculoSeccionTemporalState
            {
                TipoCalculo = TipoCalculoTemporal.EnmiendaCalcarea
            };
        }

        private static string ConstruirClaveCalculo(
            AnalisisSueloCalculoDataResponse? resultadoAnalisis,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            if (resultadoAnalisis == null)
                return Guid.NewGuid().ToString("N");

            StringBuilder builder = new StringBuilder();

            builder.Append("TerrenoId:");
            builder.Append(resultadoAnalisis.TerrenoId);
            builder.Append("|");

            builder.Append("TipoCultivoId:");
            builder.Append(resultadoAnalisis.TipoCultivoId);
            builder.Append("|");

            builder.Append("TipoAnalisisSueloId:");
            builder.Append(resultadoAnalisis.TipoAnalisisSueloId);
            builder.Append("|");

            builder.Append("CantidadQuintalesOro:");
            builder.Append(FormatearDecimalClave(resultadoAnalisis.CantidadQuintalesOro));
            builder.Append("|");

            builder.Append("TamanoFinca:");
            builder.Append(FormatearDecimalClave(resultadoAnalisis.TamanoFinca));
            builder.Append("|");

            builder.Append("Ph:");
            builder.Append(FormatearDecimalClave(resultadoAnalisis.Ph));
            builder.Append("|");

            builder.Append("AcidezTotal:");
            builder.Append(FormatearDecimalClave(resultadoAnalisis.AcidezTotal));
            builder.Append("|");

            builder.Append("FechaAnalisis:");
            builder.Append(requestGuardar?.FechaAnalisisSuelo ?? string.Empty);
            builder.Append("|");

            builder.Append("Identificador:");
            builder.Append(requestGuardar?.IdentificadorAnalisisSuelo ?? string.Empty);
            builder.Append("|");

            if (resultadoAnalisis.Elementos != null)
            {
                foreach (var elemento in resultadoAnalisis.Elementos.OrderBy(x => x.ElementoQuimicosId))
                {
                    builder.Append("Elemento:");
                    builder.Append(elemento.ElementoQuimicosId);
                    builder.Append(":");
                    builder.Append(FormatearDecimalClave(elemento.RequerimientoCalculado));
                    builder.Append("|");
                }
            }

            string textoBase = builder.ToString();

            using SHA256 sha256 = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(textoBase);
            byte[] hash = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        private static string FormatearDecimalClave(decimal? valor)
        {
            return (valor ?? 0).ToString("0.########", CultureInfo.InvariantCulture);
        }
    }
}
