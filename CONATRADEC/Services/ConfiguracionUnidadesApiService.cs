using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consume los endpoints del sistema parametrizable de unidades.
    /// Incluye una caché breve para que el formulario de análisis no
    /// descargue la misma configuración cada vez que se abre.
    /// </summary>
    public sealed class ConfiguracionUnidadesApiService
    {
        private const string RutaBase =
            "api/configuracion-unidades";

        private static readonly SemaphoreSlim
            CacheLock = new(1, 1);

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        private static ConfiguracionFormularioAnalisisResponse?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static long cacheVersion;

        public static long CacheVersion =>
            Interlocked.Read(
                ref cacheVersion);

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public ConfiguracionUnidadesApiService()
            : this(ApiClientService.Client)
        {
        }

        public ConfiguracionUnidadesApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<
            ConfiguracionUnidadesApiResult<
                ConfiguracionFormularioAnalisisResponse>>
            ObtenerConfiguracionFormularioAsync(
                bool forzarRecarga = false,
                CancellationToken cancellationToken =
                    default)
        {
            if (!forzarRecarga &&
                CacheVigente())
            {
                return ConfiguracionUnidadesApiResult<
                    ConfiguracionFormularioAnalisisResponse>
                    .Ok(
                        ClonarConfiguracionFormulario(
                            cacheFormulario!));
            }

            await CacheLock.WaitAsync(
                cancellationToken);

            try
            {
                if (!forzarRecarga &&
                    CacheVigente())
                {
                    return ConfiguracionUnidadesApiResult<
                        ConfiguracionFormularioAnalisisResponse>
                        .Ok(
                            ClonarConfiguracionFormulario(
                                cacheFormulario!));
                }

                ConfiguracionUnidadesApiResult<
                    ConfiguracionFormularioAnalisisResponse>
                    resultado =
                        await GetEnvelopeAsync<
                            ConfiguracionFormularioAnalisisResponse>(
                                $"{RutaBase}/formulario-analisis",
                                cancellationToken);

                if (resultado.Success &&
                    resultado.Data != null)
                {
                    cacheFormulario =
                        ClonarConfiguracionFormulario(
                            resultado.Data);

                    cacheCreadoUtc =
                        DateTime.UtcNow;

                    return ConfiguracionUnidadesApiResult<
                        ConfiguracionFormularioAnalisisResponse>
                        .Ok(
                            ClonarConfiguracionFormulario(
                                cacheFormulario),
                            resultado.Message);
                }

                return resultado;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public Task<
            ConfiguracionUnidadesApiResult<
                List<ElementoConfiguracionUnidadesResponse>>>
            ListarElementosAsync(
                bool incluirInactivas = true,
                CancellationToken cancellationToken =
                    default) =>
                GetEnvelopeAsync<
                    List<ElementoConfiguracionUnidadesResponse>>(
                        $"{RutaBase}/elementos" +
                        $"?incluirInactivas=" +
                        $"{incluirInactivas.ToString().ToLowerInvariant()}",
                        cancellationToken);

        public Task<
            ConfiguracionUnidadesApiResult<
                ElementoConfiguracionUnidadesResponse>>
            ObtenerElementoAsync(
                int elementoQuimicosId,
                bool incluirInactivas = true,
                CancellationToken cancellationToken =
                    default) =>
                GetEnvelopeAsync<
                    ElementoConfiguracionUnidadesResponse>(
                        $"{RutaBase}/elemento/" +
                        $"{elementoQuimicosId}" +
                        $"?incluirInactivas=" +
                        $"{incluirInactivas.ToString().ToLowerInvariant()}",
                        cancellationToken);

        public async Task<
            ConfiguracionUnidadesApiResult<
                ElementoConfiguracionUnidadesResponse>>
            GuardarElementoAsync(
                int elementoQuimicosId,
                GuardarConfiguracionElementoUnidadesRequest
                    request,
                CancellationToken cancellationToken =
                    default)
        {
            ConfiguracionUnidadesApiResult<
                ElementoConfiguracionUnidadesResponse>
                resultado =
                    await SendEnvelopeAsync<
                        GuardarConfiguracionElementoUnidadesRequest,
                        ElementoConfiguracionUnidadesResponse>(
                            HttpMethod.Put,
                            $"{RutaBase}/elemento/" +
                            $"{elementoQuimicosId}",
                            request,
                            cancellationToken);

            if (resultado.Success)
                InvalidarCache();

            return resultado;
        }

        public Task<
            ConfiguracionUnidadesApiResult<
                List<UnidadConversionConfiguradaResponse>>>
            ObtenerMateriaOrganicaAsync(
                bool incluirInactivas = true,
                CancellationToken cancellationToken =
                    default) =>
                GetEnvelopeAsync<
                    List<UnidadConversionConfiguradaResponse>>(
                        $"{RutaBase}/materia-organica" +
                        $"?incluirInactivas=" +
                        $"{incluirInactivas.ToString().ToLowerInvariant()}",
                        cancellationToken);

        public async Task<
            ConfiguracionUnidadesApiResult<
                List<UnidadConversionConfiguradaResponse>>>
            GuardarMateriaOrganicaAsync(
                GuardarConfiguracionMateriaOrganicaRequest
                    request,
                CancellationToken cancellationToken =
                    default)
        {
            ConfiguracionUnidadesApiResult<
                List<UnidadConversionConfiguradaResponse>>
                resultado =
                    await SendEnvelopeAsync<
                        GuardarConfiguracionMateriaOrganicaRequest,
                        List<UnidadConversionConfiguradaResponse>>(
                            HttpMethod.Put,
                            $"{RutaBase}/materia-organica",
                            request,
                            cancellationToken);

            if (resultado.Success)
                InvalidarCache();

            return resultado;
        }

        public Task<
            ConfiguracionUnidadesApiResult<
                List<FormulaConversionDisponibleResponse>>>
            ListarFormulasAsync(
                CancellationToken cancellationToken =
                    default) =>
                GetEnvelopeAsync<
                    List<FormulaConversionDisponibleResponse>>(
                        $"{RutaBase}/formulas",
                        cancellationToken);

        public Task<
            ConfiguracionUnidadesApiResult<
                List<UnidadMedidaCatalogoConfiguracionResponse>>>
            ListarCatalogoUnidadesAsync(
                bool incluirInactivas = false,
                CancellationToken cancellationToken =
                    default) =>
                GetEnvelopeAsync<
                    List<UnidadMedidaCatalogoConfiguracionResponse>>(
                        $"{RutaBase}/catalogo-unidades" +
                        $"?incluirInactivas=" +
                        $"{incluirInactivas.ToString().ToLowerInvariant()}",
                        cancellationToken);

        public Task<
            ConfiguracionUnidadesApiResult<
                ResultadoPruebaConversionResponse>>
            ProbarConversionAsync(
                ProbarConversionUnidadRequest request,
                CancellationToken cancellationToken =
                    default) =>
                SendEnvelopeAsync<
                    ProbarConversionUnidadRequest,
                    ResultadoPruebaConversionResponse>(
                        HttpMethod.Post,
                        $"{RutaBase}/probar",
                        request,
                        cancellationToken);

        public static void InvalidarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;

            Interlocked.Increment(
                ref cacheVersion);
        }

        private bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow - cacheCreadoUtc <
                DuracionCache;

        private async Task<
            ConfiguracionUnidadesApiResult<T>>
            GetEnvelopeAsync<T>(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                return await LeerEnvelopeAsync<T>(
                    response,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return ConfiguracionUnidadesApiResult<T>
                    .Fail(
                        "No fue posible conectarse con la API de unidades y conversiones.");
            }
            catch (Exception ex)
            {
                return ConfiguracionUnidadesApiResult<T>
                    .Fail(
                        $"No fue posible consultar la configuración: {ex.Message}");
            }
        }

        private async Task<
            ConfiguracionUnidadesApiResult<TResponse>>
            SendEnvelopeAsync<TRequest, TResponse>(
                HttpMethod method,
                string ruta,
                TRequest request,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpRequestMessage message =
                    new(method, ruta)
                    {
                        Content =
                            JsonContent.Create(
                                request,
                                options:
                                    jsonOptions)
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
                        cancellationToken);

                return await LeerEnvelopeAsync<TResponse>(
                    response,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return ConfiguracionUnidadesApiResult<TResponse>
                    .Fail(
                        "No fue posible conectarse con la API de unidades y conversiones.");
            }
            catch (Exception ex)
            {
                return ConfiguracionUnidadesApiResult<TResponse>
                    .Fail(
                        $"No fue posible completar la operación: {ex.Message}");
            }
        }

        private async Task<
            ConfiguracionUnidadesApiResult<T>>
            LeerEnvelopeAsync<T>(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            string contenido =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            ConfiguracionUnidadesApiEnvelope<T>?
                envelope = null;

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope =
                        JsonSerializer.Deserialize<
                            ConfiguracionUnidadesApiEnvelope<T>>(
                                contenido,
                                jsonOptions);
                }
                catch (JsonException)
                {
                    // El parser estándar resolverá el mensaje HTTP.
                }
            }

            if (response.IsSuccessStatusCode)
            {
                if (envelope == null)
                {
                    return ConfiguracionUnidadesApiResult<T>
                        .Fail(
                            "La API devolvió una respuesta vacía o no válida.");
                }

                if (!envelope.Success)
                {
                    return ConfiguracionUnidadesApiResult<T>
                        .Fail(
                            string.IsNullOrWhiteSpace(
                                envelope.Message)
                                ? "La API no pudo completar la operación."
                                : envelope.Message);
                }

                return ConfiguracionUnidadesApiResult<T>
                    .Ok(
                        envelope.Data,
                        envelope.Message);
            }

            string? mensaje =
                envelope?.Message;

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                mensaje =
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        ApiErrorMessageParser.GetDefaultMessage(
                            response.StatusCode,
                            "No fue posible completar la operación."));
            }

            return ConfiguracionUnidadesApiResult<T>
                .Fail(
                    mensaje);
        }

        private static
            ConfiguracionFormularioAnalisisResponse
            ClonarConfiguracionFormulario(
                ConfiguracionFormularioAnalisisResponse
                    origen)
        {
            return new ConfiguracionFormularioAnalisisResponse
            {
                UnidadResultadoId =
                    origen.UnidadResultadoId,
                UnidadResultado =
                    origen.UnidadResultado,
                UnidadesMateriaOrganica =
                    origen.UnidadesMateriaOrganica
                        .Select(
                            ClonarUnidad)
                        .ToList(),
                Elementos =
                    origen.Elementos
                        .Select(elemento =>
                            new
                                ElementoConfiguracionUnidadesResponse
                                {
                                    ElementoQuimicosId =
                                        elemento
                                            .ElementoQuimicosId,
                                    SimboloElementoQuimico =
                                        elemento
                                            .SimboloElementoQuimico,
                                    NombreElementoQuimico =
                                        elemento
                                            .NombreElementoQuimico,
                                    PesoEquivalenteElementoQuimico =
                                        elemento
                                            .PesoEquivalenteElementoQuimico,
                                    UnidadPredeterminadaId =
                                        elemento
                                            .UnidadPredeterminadaId,
                                    Unidades =
                                        elemento.Unidades
                                            .Select(
                                                ClonarUnidad)
                                            .ToList()
                                })
                        .ToList()
            };
        }

        private static
            UnidadConversionConfiguradaResponse
            ClonarUnidad(
                UnidadConversionConfiguradaResponse
                    origen)
        {
            return new UnidadConversionConfiguradaResponse
            {
                ConfiguracionId =
                    origen.ConfiguracionId,
                UnidadMedidaId =
                    origen.UnidadMedidaId,
                NombreUnidadMedida =
                    origen.NombreUnidadMedida,
                CodigoFormulaConversion =
                    origen.CodigoFormulaConversion,
                FactorPrincipal =
                    origen.FactorPrincipal,
                FactorSecundario =
                    origen.FactorSecundario,
                FactorTerciario =
                    origen.FactorTerciario,
                Divisor =
                    origen.Divisor,
                Desplazamiento =
                    origen.Desplazamiento,
                UnidadPredeterminada =
                    origen.UnidadPredeterminada,
                VisibleEnFormulario =
                    origen.VisibleEnFormulario,
                Orden =
                    origen.Orden,
                Observacion =
                    origen.Observacion,
                Activo =
                    origen.Activo
            };
        }
    }
}
