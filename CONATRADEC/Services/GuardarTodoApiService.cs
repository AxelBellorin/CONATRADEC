using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio central para guardar, editar, listar y eliminar análisis.
    ///
    /// Antes de enviar una solicitud:
    /// 1. Recupera el requerimiento anual completo cuando las pantallas
    ///    complementarias trabajaron con una lista filtrada.
    /// 2. Conserva exactamente los valores calculados por la API o por el
    ///    motor local; solamente normaliza la unidad y el orden de prioridad.
    /// 3. Corrige los totales del balance usando sus detalles si la cabecera
    ///    temporal llegó con mezclaTotalQq o totalLibras en cero.
    /// </summary>
    public sealed class GuardarTodoApiService
    {
        private const string EndpointGuardar =
            "api/guardar-todo";

        private const string EndpointListado =
            "api/guardar-todo";

        private const string CodigoConflictoEdicion =
            "ANALYSIS_EDIT_CONFLICT";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions
            jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition =
                    JsonIgnoreCondition.Never
            };

        public GuardarTodoApiService()
            : this(ApiClientService.Client)
        {
        }

        public GuardarTodoApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public Task<GuardarTodoResponse> GuardarAsync(
            GuardarTodoRequest request,
            CancellationToken cancellationToken =
                default)
        {
            return EnviarSolicitudAsync(
                HttpMethod.Post,
                EndpointGuardar,
                request,
                "guardar",
                cancellationToken);
        }

        public Task<GuardarTodoResponse> EditarAsync(
            int analisisSueloCalculoId,
            GuardarTodoRequest request,
            CancellationToken cancellationToken =
                default)
        {
            if (analisisSueloCalculoId <= 0)
            {
                return Task.FromResult(
                    new GuardarTodoResponse
                    {
                        Success = false,
                        Message =
                            "El identificador del cálculo que se debe editar no es válido."
                    });
            }

            return EnviarSolicitudAsync(
                HttpMethod.Put,
                $"{EndpointGuardar}/editar/" +
                $"{analisisSueloCalculoId}",
                request,
                "actualizar",
                cancellationToken);
        }

        public async Task<
            AnalisisGuardadoListaResponse>
            ListarAsync(
                CancellationToken cancellationToken =
                    default)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        EndpointListado,
                        cancellationToken);

                string jsonResponse =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                AnalisisGuardadoListaResponse?
                    resultado =
                        DeserializarSeguro<
                            AnalisisGuardadoListaResponse>(
                                jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    return new
                        AnalisisGuardadoListaResponse
                    {
                        Success = false,
                        Message =
                                ExtraerMensajeError(
                                    jsonResponse,
                                    "No fue posible cargar los análisis. " +
                                    $"Código HTTP {(int)response.StatusCode}.")
                    };
                }

                if (resultado == null)
                {
                    return new
                        AnalisisGuardadoListaResponse
                    {
                        Success = false,
                        Message =
                                "La API respondió, pero no se pudo interpretar la lista de análisis."
                    };
                }

                resultado.Data ??= new();
                return resultado;
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return new
                    AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message =
                            "La carga tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new
                    AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message =
                            "No fue posible conectarse con el servidor para cargar los análisis."
                };
            }
            catch (Exception ex)
            {
                return new
                    AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message =
                            "Ocurrió un error al cargar los análisis: " +
                            ex.Message
                };
            }
        }

        public async Task<
            AnalisisGuardadoDetalleResponse>
            ObtenerDetalleAsync(
                int analisisSueloCalculoId,
                CancellationToken cancellationToken =
                    default)
        {
            if (analisisSueloCalculoId <= 0)
            {
                return new
                    AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message =
                            "El identificador del cálculo no es válido."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"{EndpointGuardar}/listardetalle/" +
                        $"{analisisSueloCalculoId}",
                        cancellationToken);

                string jsonResponse =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                AnalisisGuardadoDetalleResponse?
                    resultado =
                        DeserializarSeguro<
                            AnalisisGuardadoDetalleResponse>(
                                jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    return new
                        AnalisisGuardadoDetalleResponse
                    {
                        Success = false,
                        Message =
                                ExtraerMensajeError(
                                    jsonResponse,
                                    "No fue posible cargar el detalle. " +
                                    $"Código HTTP {(int)response.StatusCode}.")
                    };
                }

                if (resultado?.Data == null)
                {
                    return new
                        AnalisisGuardadoDetalleResponse
                    {
                        Success = false,
                        Message =
                                "La API respondió, pero no devolvió el detalle del análisis."
                    };
                }

                return resultado;
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return new
                    AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message =
                            "La consulta tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new
                    AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message =
                            "No fue posible conectarse con el servidor para cargar el detalle."
                };
            }
            catch (Exception ex)
            {
                return new
                    AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message =
                            "Ocurrió un error al cargar el detalle: " +
                            ex.Message
                };
            }
        }

        public async Task<EliminarAnalisisResponse>
            EliminarAsync(
                int analisisSueloId,
                CancellationToken cancellationToken =
                    default)
        {
            if (analisisSueloId <= 0)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message =
                        "El identificador del análisis no es válido."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.DeleteAsync(
                        $"{EndpointGuardar}/" +
                        $"{analisisSueloId}",
                        cancellationToken);

                string jsonResponse =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                EliminarAnalisisResponse?
                    resultado =
                        DeserializarSeguro<
                            EliminarAnalisisResponse>(
                                jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    string mensajeError =
                        ExtraerMensajeError(
                            jsonResponse,
                            "No fue posible eliminar el análisis. " +
                            $"Código HTTP {(int)response.StatusCode}.");

                    if (resultado != null)
                    {
                        resultado.Success = false;

                        if (string.IsNullOrWhiteSpace(
                                resultado.Message))
                        {
                            resultado.Message =
                                mensajeError;
                        }

                        return resultado;
                    }

                    return new
                        EliminarAnalisisResponse
                    {
                        Success = false,
                        Message = mensajeError
                    };
                }

                return resultado ??
                    new EliminarAnalisisResponse
                    {
                        Success = false,
                        Message =
                            "La API procesó la eliminación, pero no se pudo interpretar su respuesta."
                    };
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message =
                        "La eliminación tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message =
                        "No fue posible conectarse con el servidor para eliminar el análisis."
                };
            }
            catch (Exception ex)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message =
                        "Ocurrió un error al eliminar el análisis: " +
                        ex.Message
                };
            }
        }

        private async Task<GuardarTodoResponse>
            EnviarSolicitudAsync(
                HttpMethod method,
                string endpoint,
                GuardarTodoRequest request,
                string accion,
                CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message =
                        "No se recibieron los datos que se deben procesar."
                };
            }

            try
            {
                NormalizarAntesDeEnviar(request);

                string jsonRequest =
                    JsonSerializer.Serialize(
                        request,
                        jsonOptions);

                using HttpRequestMessage mensaje =
                    new(method, endpoint)
                    {
                        Content = new StringContent(
                            jsonRequest,
                            Encoding.UTF8,
                            "application/json")
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        mensaje,
                        cancellationToken);

                string jsonResponse =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                GuardarTodoResponse? resultado =
                    DeserializarSeguro<
                        GuardarTodoResponse>(
                            jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    string mensajeError =
                        ExtraerMensajeError(
                            jsonResponse,
                            $"No fue posible {accion} el análisis. " +
                            $"Código HTTP {(int)response.StatusCode}.");

                    GuardarTodoResponse respuestaError =
                        resultado ??
                        new GuardarTodoResponse();

                    respuestaError.Success = false;
                    respuestaError.StatusCode =
                        (int)response.StatusCode;

                    if (string.IsNullOrWhiteSpace(
                            respuestaError.Message))
                    {
                        respuestaError.Message = mensajeError;
                    }

                    await IntentarResolverConflictoEdicionAsync(
                        method,
                        endpoint,
                        respuestaError);

                    return respuestaError;
                }

                GuardarTodoResponse respuestaFinal =
                    resultado ??
                    new GuardarTodoResponse
                    {
                        Success = false,
                        Message =
                            "La API procesó la solicitud, pero no se pudo interpretar la respuesta al " +
                            accion +
                            "."
                    };

                respuestaFinal.StatusCode =
                    (int)response.StatusCode;

                if (respuestaFinal.Success)
                {
                    SeleccionElementosComplementariosService
                        .Limpiar();
                }

                return respuestaFinal;
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message =
                        "La solicitud tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message =
                        "No fue posible conectarse con el servidor. Verifique su conexión."
                };
            }
            catch (Exception ex)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message =
                        $"Ocurrió un error al {accion} el análisis: " +
                        ex.Message
                };
            }
        }

        /// <summary>
        /// El backend rechaza una edición cuando la versión vista por el
        /// usuario ya no es la actual. En ese caso se vuelve a preparar el
        /// análisis desde la API y se abre el formulario con datos frescos.
        /// Nunca se transforma el conflicto en un guardado exitoso.
        /// </summary>
        private static async Task IntentarResolverConflictoEdicionAsync(
            HttpMethod method,
            string endpoint,
            GuardarTodoResponse respuesta)
        {
            if (method != HttpMethod.Put ||
                respuesta.StatusCode !=
                    (int)HttpStatusCode.Conflict ||
                !string.Equals(
                    respuesta.Code,
                    CodigoConflictoEdicion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int id = ObtenerUltimoEntero(endpoint);
            if (id <= 0)
                return;

            try
            {
                AnalisisGuardadoResumen? resumen =
                    AnalisisEdicionService.Instance
                        .ContextoActual?
                        .Resumen;

                (bool success, string message) =
                    await AnalisisEdicionService.Instance
                        .PrepararAsync(id, resumen);

                if (!success)
                {
                    respuesta.Message =
                        (respuesta.Message + " " +
                         "Además, no fue posible recargar los datos actuales: " +
                         message).Trim();
                    return;
                }

                await CalculoAnalisisTemporalService.Instance
                    .LimpiarTodoAsync();

                SeleccionElementosComplementariosService
                    .Limpiar();

                respuesta.Message =
                    "El análisis fue modificado por otra operación después de que usted lo abrió. " +
                    "La actualización fue rechazada para proteger la información y ya se cargaron los datos actuales del servidor.";

                var navegacion = new GlobalService();
                await navegacion.GoToAsyncParameters(
                    "//NuevoAnalisisFormPage");
            }
            catch (Exception ex)
            {
                respuesta.Message =
                    (respuesta.Message + " " +
                     "No fue posible abrir automáticamente la versión actual: " +
                     ex.Message).Trim();
            }
        }

        private static int ObtenerUltimoEntero(string valor)
        {
            string ultimo =
                (valor ?? string.Empty)
                    .Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault() ??
                string.Empty;

            return int.TryParse(
                    ultimo,
                    out int resultado)
                ? resultado
                : 0;
        }

        private static void NormalizarAntesDeEnviar(
            GuardarTodoRequest request)
        {
            RestaurarRequerimientoCompleto(request);
            NormalizarRequerimientoAnual(request);
            NormalizarBalance(request.BalanceNutricional);
        }

        private static void RestaurarRequerimientoCompleto(
            GuardarTodoRequest request)
        {
            AnalisisSueloCalculoDataResponse?
                completo =
                    SeleccionElementosComplementariosService
                        .ObtenerRequerimientoCompleto(
                            request
                                .DatosAnalisis
                                .IdentificadorAnalisisSuelo);

            if (completo?.Elementos == null ||
                completo.Elementos.Count == 0)
            {
                return;
            }

            request.RequerimientoAnual.Elementos =
                completo.Elementos
                    .Where(x =>
                        x.ElementoQuimicosId is > 0)
                    .Select(x =>
                        new
                            GuardarTodoRequerimientoElementoRequest
                        {
                            ElementoQuimicosId =
                                    x.ElementoQuimicosId!.Value,
                            SimboloElementoQuimico =
                                    x.SimboloElementoQuimico?
                                        .Trim() ??
                                    string.Empty,
                            NombreElementoQuimico =
                                    x.NombreElementoQuimico?
                                        .Trim() ??
                                    string.Empty,
                            CantidadIngresada =
                                    x.CantidadIngresada ?? 0,
                            CantidadConvertidaLbMz =
                                    x.CantidadConvertidaLbMz,
                            ExtraccionPorQQOro =
                                    x.ExtraccionPorQQOro,
                            ExtraccionPorProduccion =
                                    x.ExtraccionPorProduccion,
                            RangoMinimo =
                                    x.RangoMinimo,
                            RangoMaximo =
                                    x.RangoMaximo,
                            RangoMinimoLbMz =
                                    x.RangoMinimoLbMz,
                            RangoMaximoLbMz =
                                    x.RangoMaximoLbMz,
                            RequerimientoCalculado =
                                    x.RequerimientoCalculado,
                            UnidadBase =
                                    x.UnidadBase?.Trim() ??
                                    string.Empty,
                            UnidadMedidaResultadoId =
                                    x.UnidadMedidaResultadoId,
                            UnidadResultado =
                                    string.IsNullOrWhiteSpace(
                                        x.UnidadResultado)
                                        ? "lb/Mz"
                                        : x.UnidadResultado.Trim(),
                            Clasificacion =
                                    x.Clasificacion?.Trim() ??
                                    string.Empty,
                            Observacion =
                                    x.Observacion?.Trim() ??
                                    string.Empty,
                            IncluirCalculosComplementarios =
                                    x.IncluirEnCalculosComplementarios
                        })
                    .ToList();
        }

        /// <summary>
        /// Mantiene el mismo resultado que ya produjo la API o el motor local.
        /// No vuelve a calcular la extracción ni el requerimiento durante el
        /// guardado, porque podría mezclar la producción de otro estado del
        /// formulario con el resultado que el usuario ya revisó.
        ///
        /// Solo asegura la unidad lb/Mz y ordena de mayor a menor necesidad.
        /// </summary>
        private static void NormalizarRequerimientoAnual(
            GuardarTodoRequest request)
        {
            GuardarTodoRequerimientoAnualRequest requerimiento =
                request.RequerimientoAnual;

            requerimiento.Elementos ??=
                new List<GuardarTodoRequerimientoElementoRequest>();

            foreach (
                GuardarTodoRequerimientoElementoRequest elemento
                in requerimiento.Elementos)
            {
                if (string.IsNullOrWhiteSpace(
                        elemento.UnidadResultado))
                {
                    elemento.UnidadResultado = "lb/Mz";
                }
            }

            requerimiento.Elementos =
                requerimiento.Elementos
                    .OrderByDescending(x =>
                        x.RequerimientoCalculado ?? 0)
                    .ThenBy(
                        x => x.NombreElementoQuimico,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
        }

        private static void NormalizarBalance(
            GuardarTodoBalanceNutricionalRequest?
                balance)
        {
            if (balance?.Resultado == null)
                return;

            GuardarTodoBalanceResultadoRequest
                resultado = balance.Resultado;

            List<GuardarTodoBalanceDetalleRequest>
                detalles =
                    resultado.Detalle ??
                    new List<
                        GuardarTodoBalanceDetalleRequest>();

            if (resultado.TotalLibras <= 0)
            {
                resultado.TotalLibras =
                    detalles.Sum(x => x.Lb);
            }

            if (resultado.MezclaTotalQq <= 0)
            {
                resultado.MezclaTotalQq =
                    detalles.Sum(x => x.Qq);
            }

            if (resultado.MezclaTotalQq <= 0 &&
                resultado.TotalLibras > 0)
            {
                resultado.MezclaTotalQq =
                    resultado.TotalLibras / 100m;
            }

            if (resultado.MezclaTotalQq <= 0)
            {
                throw new InvalidOperationException(
                    "La mezcla total del balance es igual a cero. Recalcule el balance y verifique las fuentes seleccionadas antes de guardar.");
            }
        }

        private T? DeserializarSeguro<T>(
            string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(
                    json,
                    jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static string ExtraerMensajeError(
            string json,
            string mensajePredeterminado)
        {
            if (string.IsNullOrWhiteSpace(json))
                return mensajePredeterminado;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                if (TryGetPropertyIgnoreCase(
                        root,
                        "message",
                        out JsonElement message) &&
                    message.ValueKind ==
                        JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(
                        message.GetString()))
                {
                    return message.GetString()!;
                }

                if (TryGetPropertyIgnoreCase(
                        root,
                        "title",
                        out JsonElement title) &&
                    title.ValueKind ==
                        JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(
                        title.GetString()))
                {
                    return title.GetString()!;
                }

                if (TryGetPropertyIgnoreCase(
                        root,
                        "errors",
                        out JsonElement errors) &&
                    errors.ValueKind ==
                        JsonValueKind.Object)
                {
                    foreach (
                        JsonProperty property
                        in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind !=
                            JsonValueKind.Array)
                        {
                            continue;
                        }

                        string? firstError =
                            property.Value
                                .EnumerateArray()
                                .Where(x =>
                                    x.ValueKind ==
                                        JsonValueKind.String)
                                .Select(x =>
                                    x.GetString())
                                .FirstOrDefault(x =>
                                    !string.IsNullOrWhiteSpace(
                                        x));

                        if (!string.IsNullOrWhiteSpace(
                                firstError))
                        {
                            return firstError;
                        }
                    }
                }
            }
            catch
            {
            }

            return mensajePredeterminado;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            foreach (
                JsonProperty property
                in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
