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
    // Mantiene en memoria y en archivo local el flujo completo del
    // análisis actual.
    //
    // La sincronización se divide en dos niveles:
    // 1. operacionLock serializa las operaciones asincrónicas.
    // 2. estadoSync protege únicamente accesos breves en memoria.
    //
    // Nunca se mantiene un bloqueo de memoria mientras se espera una
    // operación asincrónica. Esto evita que la interfaz de MAUI quede
    // bloqueada al navegar desde Resultado hacia MultiCálculo.
    // ===========================================================

    class CalculoAnalisisTemporalService
    {
        private static readonly Lazy<CalculoAnalisisTemporalService> instancia =
            new(() => new CalculoAnalisisTemporalService());

        public static CalculoAnalisisTemporalService Instance =>
            instancia.Value;

        private const string NombreArchivoTemporal =
            "calculo_analisis_temporal.json";

        private readonly object estadoSync = new();

        private readonly SemaphoreSlim operacionLock =
            new(1, 1);

        private readonly SemaphoreSlim archivoLock =
            new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

        private CalculoAnalisisTemporalState estadoActual =
            new();

        private bool estadoInicializadoDesdeArchivo;

        private string RutaArchivoTemporal =>
            Path.Combine(
                FileSystem.AppDataDirectory,
                NombreArchivoTemporal);

        private CalculoAnalisisTemporalService()
        {
        }

        public CalculoAnalisisTemporalState ObtenerEstadoActual()
        {
            lock (estadoSync)
            {
                AsegurarSeccionesInterno();
                return estadoActual;
            }
        }

        public async Task IniciarNuevoCalculoAsync(
            AnalisisSueloCalculoDataResponse? resultadoAnalisis,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                await CargarDesdeArchivoSiEsNecesarioAsync()
                    .ConfigureAwait(false);

                string nuevaClave =
                    ConstruirClaveCalculo(
                        resultadoAnalisis,
                        requestGuardar);

                lock (estadoSync)
                {
                    bool esMismoCalculo =
                        !string.IsNullOrWhiteSpace(
                            estadoActual.CalculoKey) &&
                        string.Equals(
                            estadoActual.CalculoKey,
                            nuevaClave,
                            StringComparison.Ordinal);

                    if (!esMismoCalculo)
                    {
                        estadoActual =
                            new CalculoAnalisisTemporalState
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
                        /*
                         * Si es el mismo análisis, se actualizan únicamente
                         * los datos base y se conservan Balance, Enmienda y
                         * Mixta que ya hayan sido restaurados.
                         */
                        estadoActual.ResultadoAnalisisSuelo =
                            resultadoAnalisis;

                        estadoActual.RequestGuardarAnalisis =
                            requestGuardar;

                        estadoActual.FechaUltimaModificacion =
                            DateTime.Now;
                    }

                    AsegurarSeccionesInterno();
                }

                await GuardarCalculoInternoAsync(
                        TipoCalculoTemporal.RequerimientoAnual,
                        requestGuardar,
                        resultadoAnalisis,
                        "Requerimiento anual cargado desde el resultado del análisis de suelo.")
                    .ConfigureAwait(false);
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public async Task GuardarCalculoAsync<TRequest, TResultado>(
            TipoCalculoTemporal tipoCalculo,
            TRequest? request,
            TResultado? resultado,
            string? mensajeEstado = null)
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                await CargarDesdeArchivoSiEsNecesarioAsync()
                    .ConfigureAwait(false);

                await GuardarCalculoInternoAsync(
                        tipoCalculo,
                        request,
                        resultado,
                        mensajeEstado)
                    .ConfigureAwait(false);
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public async Task MarcarPendienteRecalculoAsync(
            TipoCalculoTemporal tipoCalculo,
            string? mensajeEstado = null,
            bool limpiarResultado = true)
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                await CargarDesdeArchivoSiEsNecesarioAsync()
                    .ConfigureAwait(false);

                string json;

                lock (estadoSync)
                {
                    CalculoSeccionTemporalState seccion =
                        ObtenerSeccionInterna(
                            tipoCalculo);

                    seccion.Estado =
                        EstadoCalculoTemporal.PendienteRecalculo;

                    seccion.FechaUltimaModificacion =
                        DateTime.Now;

                    seccion.MensajeEstado =
                        mensajeEstado ??
                        "Hay cambios pendientes. Debe recalcular para actualizar el resultado.";

                    if (limpiarResultado)
                        seccion.ResultadoJson = null;

                    estadoActual.FechaUltimaModificacion =
                        DateTime.Now;

                    json =
                        SerializarEstadoActualInterno();
                }

                await EscribirArchivoAsync(json)
                    .ConfigureAwait(false);
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public async Task ReiniciarCalculoAsync(
            TipoCalculoTemporal tipoCalculo,
            string? mensajeEstado = null)
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                await CargarDesdeArchivoSiEsNecesarioAsync()
                    .ConfigureAwait(false);

                string json;

                lock (estadoSync)
                {
                    CalculoSeccionTemporalState seccion =
                        ObtenerSeccionInterna(
                            tipoCalculo);

                    seccion.Estado =
                        EstadoCalculoTemporal.Reiniciado;

                    seccion.RequestJson = null;
                    seccion.ResultadoJson = null;
                    seccion.FechaCalculo = null;
                    seccion.FechaUltimaModificacion =
                        DateTime.Now;

                    seccion.MensajeEstado =
                        mensajeEstado ??
                        "Cálculo reiniciado.";

                    estadoActual.FechaUltimaModificacion =
                        DateTime.Now;

                    json =
                        SerializarEstadoActualInterno();
                }

                await EscribirArchivoAsync(json)
                    .ConfigureAwait(false);
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public bool TieneResultadoValido(
            TipoCalculoTemporal tipoCalculo)
        {
            lock (estadoSync)
            {
                return ObtenerSeccionInterna(
                    tipoCalculo)
                    .TieneResultadoValido;
            }
        }

        public TResultado? ObtenerResultado<TResultado>(
            TipoCalculoTemporal tipoCalculo)
        {
            string? resultadoJson;

            lock (estadoSync)
            {
                resultadoJson =
                    ObtenerSeccionInterna(
                        tipoCalculo)
                        .ResultadoJson;
            }

            if (string.IsNullOrWhiteSpace(
                    resultadoJson))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<TResultado>(
                    resultadoJson,
                    jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public TRequest? ObtenerRequest<TRequest>(
            TipoCalculoTemporal tipoCalculo)
        {
            string? requestJson;

            lock (estadoSync)
            {
                requestJson =
                    ObtenerSeccionInterna(
                        tipoCalculo)
                        .RequestJson;
            }

            if (string.IsNullOrWhiteSpace(
                    requestJson))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<TRequest>(
                    requestJson,
                    jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public async Task GuardarEnArchivoAsync()
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                string json;

                lock (estadoSync)
                {
                    json =
                        SerializarEstadoActualInterno();
                }

                await EscribirArchivoAsync(json)
                    .ConfigureAwait(false);
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public async Task<bool> CargarDesdeArchivoAsync()
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                bool cargado =
                    await CargarDesdeArchivoInternoAsync()
                        .ConfigureAwait(false);

                estadoInicializadoDesdeArchivo = true;

                return cargado;
            }
            finally
            {
                operacionLock.Release();
            }
        }

        public async Task LimpiarTodoAsync()
        {
            await operacionLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                lock (estadoSync)
                {
                    estadoActual =
                        new CalculoAnalisisTemporalState();

                    AsegurarSeccionesInterno();
                }

                estadoInicializadoDesdeArchivo = true;

                await archivoLock
                    .WaitAsync()
                    .ConfigureAwait(false);

                try
                {
                    if (File.Exists(
                            RutaArchivoTemporal))
                    {
                        File.Delete(
                            RutaArchivoTemporal);
                    }
                }
                catch
                {
                    // La limpieza en memoria ya fue realizada.
                }
                finally
                {
                    archivoLock.Release();
                }
            }
            finally
            {
                operacionLock.Release();
            }
        }

        private async Task GuardarCalculoInternoAsync<TRequest, TResultado>(
            TipoCalculoTemporal tipoCalculo,
            TRequest? request,
            TResultado? resultado,
            string? mensajeEstado)
        {
            Task<string?> tareaRequest =
                SerializarAsync(request);

            Task<string?> tareaResultado =
                SerializarAsync(resultado);

            await Task.WhenAll(
                    tareaRequest,
                    tareaResultado)
                .ConfigureAwait(false);

            string? requestJson =
                await tareaRequest
                    .ConfigureAwait(false);

            string? resultadoJson =
                await tareaResultado
                    .ConfigureAwait(false);

            string estadoJson;

            lock (estadoSync)
            {
                CalculoSeccionTemporalState seccion =
                    ObtenerSeccionInterna(
                        tipoCalculo);

                seccion.TipoCalculo =
                    tipoCalculo;

                seccion.Estado =
                    EstadoCalculoTemporal.Calculado;

                seccion.RequestJson =
                    requestJson;

                seccion.ResultadoJson =
                    resultadoJson;

                seccion.FechaCalculo =
                    DateTime.Now;

                seccion.FechaUltimaModificacion =
                    DateTime.Now;

                seccion.MensajeEstado =
                    mensajeEstado ??
                    "Cálculo actualizado correctamente.";

                estadoActual.FechaUltimaModificacion =
                    DateTime.Now;

                estadoJson =
                    SerializarEstadoActualInterno();
            }

            await EscribirArchivoAsync(
                    estadoJson)
                .ConfigureAwait(false);
        }

        private async Task CargarDesdeArchivoSiEsNecesarioAsync()
        {
            if (estadoInicializadoDesdeArchivo)
                return;

            await CargarDesdeArchivoInternoAsync()
                .ConfigureAwait(false);

            estadoInicializadoDesdeArchivo = true;
        }

        private async Task<bool> CargarDesdeArchivoInternoAsync()
        {
            await archivoLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!File.Exists(
                        RutaArchivoTemporal))
                {
                    return false;
                }

                string json =
                    await File.ReadAllTextAsync(
                            RutaArchivoTemporal)
                        .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(json))
                    return false;

                CalculoAnalisisTemporalState? estado =
                    await Task.Run(() =>
                        JsonSerializer.Deserialize<
                            CalculoAnalisisTemporalState>(
                                json,
                                jsonOptions))
                        .ConfigureAwait(false);

                if (estado == null)
                    return false;

                lock (estadoSync)
                {
                    estadoActual = estado;
                    AsegurarSeccionesInterno();
                }

                return true;
            }
            catch
            {
                lock (estadoSync)
                {
                    estadoActual =
                        new CalculoAnalisisTemporalState();

                    AsegurarSeccionesInterno();
                }

                return false;
            }
            finally
            {
                archivoLock.Release();
            }
        }

        private async Task EscribirArchivoAsync(
            string json)
        {
            await archivoLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                await File.WriteAllTextAsync(
                        RutaArchivoTemporal,
                        json)
                    .ConfigureAwait(false);
            }
            catch
            {
                /*
                 * Una falla del respaldo local no rompe la interfaz.
                 * El estado en memoria continúa disponible.
                 */
            }
            finally
            {
                archivoLock.Release();
            }
        }

        private CalculoSeccionTemporalState
            ObtenerSeccionInterna(
                TipoCalculoTemporal tipoCalculo)
        {
            AsegurarSeccionesInterno();

            return tipoCalculo switch
            {
                TipoCalculoTemporal.RequerimientoAnual =>
                    estadoActual.RequerimientoAnual,

                TipoCalculoTemporal.BalanceFormula =>
                    estadoActual.BalanceFormula,

                TipoCalculoTemporal.FertilizacionMixta =>
                    estadoActual.FertilizacionMixta,

                TipoCalculoTemporal.EnmiendaCalcarea =>
                    estadoActual.EnmiendaCalcarea,

                _ =>
                    estadoActual.RequerimientoAnual
            };
        }

        private Task<string?> SerializarAsync<T>(
            T? valor)
        {
            if (valor is null)
                return Task.FromResult<string?>(null);

            return Task.Run<string?>(() =>
                JsonSerializer.Serialize(
                    valor,
                    jsonOptions));
        }

        private string SerializarEstadoActualInterno()
        {
            AsegurarSeccionesInterno();

            return JsonSerializer.Serialize(
                estadoActual,
                jsonOptions);
        }

        private void AsegurarSeccionesInterno()
        {
            estadoActual.RequerimientoAnual ??=
                new CalculoSeccionTemporalState
                {
                    TipoCalculo =
                        TipoCalculoTemporal.RequerimientoAnual
                };

            estadoActual.BalanceFormula ??=
                new CalculoSeccionTemporalState
                {
                    TipoCalculo =
                        TipoCalculoTemporal.BalanceFormula
                };

            estadoActual.FertilizacionMixta ??=
                new CalculoSeccionTemporalState
                {
                    TipoCalculo =
                        TipoCalculoTemporal.FertilizacionMixta
                };

            estadoActual.EnmiendaCalcarea ??=
                new CalculoSeccionTemporalState
                {
                    TipoCalculo =
                        TipoCalculoTemporal.EnmiendaCalcarea
                };
        }

        private static string ConstruirClaveCalculo(
            AnalisisSueloCalculoDataResponse? resultadoAnalisis,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            if (resultadoAnalisis == null)
                return Guid.NewGuid().ToString("N");

            StringBuilder builder = new();

            builder.Append("TerrenoId:");
            builder.Append(resultadoAnalisis.TerrenoId);
            builder.Append('|');

            builder.Append("TipoCultivoId:");
            builder.Append(resultadoAnalisis.TipoCultivoId);
            builder.Append('|');

            builder.Append("TipoAnalisisSueloId:");
            builder.Append(resultadoAnalisis.TipoAnalisisSueloId);
            builder.Append('|');

            builder.Append("CantidadQuintalesOro:");
            builder.Append(
                FormatearDecimalClave(
                    resultadoAnalisis.CantidadQuintalesOro));
            builder.Append('|');

            builder.Append("TamanoFinca:");
            builder.Append(
                FormatearDecimalClave(
                    resultadoAnalisis.TamanoFinca));
            builder.Append('|');

            builder.Append("Ph:");
            builder.Append(
                FormatearDecimalClave(
                    resultadoAnalisis.Ph));
            builder.Append('|');

            builder.Append("AcidezTotal:");
            builder.Append(
                FormatearDecimalClave(
                    resultadoAnalisis.AcidezTotal));
            builder.Append('|');

            builder.Append("FechaAnalisis:");
            builder.Append(
                requestGuardar?.FechaAnalisisSuelo ??
                string.Empty);
            builder.Append('|');

            builder.Append("Identificador:");
            builder.Append(
                requestGuardar?
                    .IdentificadorAnalisisSuelo ??
                string.Empty);
            builder.Append('|');

            if (resultadoAnalisis.Elementos != null)
            {
                foreach (ElementoResultadoCalculoResponse elemento
                         in resultadoAnalisis.Elementos
                             .OrderBy(x =>
                                 x.ElementoQuimicosId))
                {
                    builder.Append("Elemento:");
                    builder.Append(
                        elemento.ElementoQuimicosId);
                    builder.Append(':');
                    builder.Append(
                        FormatearDecimalClave(
                            elemento.RequerimientoCalculado));
                    builder.Append('|');
                }
            }

            string textoBase = builder.ToString();

            using SHA256 sha256 =
                SHA256.Create();

            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    textoBase);

            byte[] hash =
                sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        private static string FormatearDecimalClave(
            decimal? valor)
        {
            return (valor ?? 0)
                .ToString(
                    "0.########",
                    CultureInfo.InvariantCulture);
        }
    }
}
