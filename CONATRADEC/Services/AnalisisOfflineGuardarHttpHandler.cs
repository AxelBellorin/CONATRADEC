using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Guarda el análisis completo en SQLite cuando se trabaja localmente.
    ///
    /// También atiende el detalle, los datos del reporte y el PDF usando el
    /// identificador local positivo que recibe la interfaz actual.
    /// </summary>
    public sealed class AnalisisOfflineGuardarHttpHandler :
        DelegatingHandler
    {
        private const string RutaGuardar =
            "/api/guardar-todo";

        private const string RutaEditarPrefijo =
            "/api/guardar-todo/editar/";

        private const string RutaDetallePrefijo =
            "/api/guardar-todo/listardetalle/";

        private const string RutaReportePrefijo =
            "/api/reportes/analisis/";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = ObtenerPath(request);

            if (request.Method == HttpMethod.Delete &&
                path.StartsWith(
                    RutaGuardar + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                int idEliminar = ObtenerUltimoEntero(path);

                if (AnalisisOfflineDatabaseService
                    .EsIdLocal(idEliminar))
                {
                    bool eliminado =
                        await AnalisisOfflineDatabaseService.Instance
                            .EliminarLocalAsync(idEliminar);

                    return eliminado
                        ? CrearJson(
                            request,
                            new
                            {
                                success = true,
                                message =
                                    "El análisis pendiente fue eliminado de este dispositivo.",
                                analisisSueloId = idEliminar,
                                calculosDesactivados = 0
                            })
                        : CrearError(
                            request,
                            HttpStatusCode.NotFound,
                            "No se encontró el análisis local.");
                }
            }

            if (request.Method == HttpMethod.Get)
            {
                HttpResponseMessage? local =
                    await IntentarResponderLecturaLocalAsync(
                        request,
                        path,
                        cancellationToken);

                if (local != null)
                    return local;

                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            bool esGuardar =
                request.Method == HttpMethod.Post &&
                string.Equals(
                    path,
                    RutaGuardar,
                    StringComparison.OrdinalIgnoreCase);

            bool esEditar =
                request.Method == HttpMethod.Put &&
                path.StartsWith(
                    RutaEditarPrefijo,
                    StringComparison.OrdinalIgnoreCase);

            if (!esGuardar && !esEditar)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (!DatosSinConexionPermisos.TienePermiso &&
                ModoSesionService.EsOffline)
            {
                return CrearError(
                    request,
                    HttpStatusCode.Forbidden,
                    "Su usuario no tiene habilitado el guardado sin conexión.");
            }

            int idEdicion = esEditar
                ? ObtenerUltimoEntero(path)
                : 0;

            bool esEdicionLocal =
                AnalisisOfflineDatabaseService
                    .EsIdLocal(idEdicion);

            /*
             * Online guarda siempre en la API. La única excepción es editar
             * una operación que todavía no posee ID de servidor.
             */
            if (ModoSesionService.EsEnLinea &&
                !esEdicionLocal)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            byte[] contenido =
                request.Content == null
                    ? Array.Empty<byte>()
                    : await request.Content.ReadAsByteArrayAsync(
                        cancellationToken);

            RestaurarContenido(request, contenido);

            return await GuardarLocalAsync(
                request,
                path,
                contenido,
                cancellationToken);
        }

        private static async Task<HttpResponseMessage>
            GuardarLocalAsync(
                HttpRequestMessage request,
                string path,
                byte[] contenido,
                CancellationToken cancellationToken)
        {
            if (contenido.Length == 0)
            {
                return CrearError(
                    request,
                    HttpStatusCode.BadRequest,
                    "No se recibieron los datos del análisis.");
            }

            GuardarTodoRequest? solicitud;

            try
            {
                solicitud =
                    JsonSerializer.Deserialize<
                        GuardarTodoRequest>(
                        contenido,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                return CrearError(
                    request,
                    HttpStatusCode.BadRequest,
                    "No fue posible interpretar el análisis: " +
                    ex.Message);
            }

            if (solicitud == null)
            {
                return CrearError(
                    request,
                    HttpStatusCode.BadRequest,
                    "El contenido del análisis no es válido.");
            }

            MotorCalculoPaquete? paquete =
                await MotorCalculoPaqueteService.Instance
                    .ObtenerPaqueteActivoAsync(
                        cancellationToken);

            if (paquete == null ||
                !paquete.Modulos.GuardadoLocal)
            {
                return CrearError(
                    request,
                    HttpStatusCode.ServiceUnavailable,
                    "El dispositivo no tiene un paquete completo para guardar el análisis localmente.");
            }

            string tipoOperacion =
                request.Method == HttpMethod.Put
                    ? "EDITAR"
                    : "CREAR";

            int? idServidor = null;
            int? idLocalExistente = null;

            if (tipoOperacion == "EDITAR")
            {
                int id =
                    ObtenerUltimoEntero(path);

                if (AnalisisOfflineDatabaseService
                    .EsIdLocal(id))
                {
                    idLocalExistente = id;
                }
                else if (id > 0)
                {
                    idServidor = id;
                }
            }

            string json =
                Encoding.UTF8.GetString(contenido);

            AnalisisOfflineLocalEntity entity =
                await AnalisisOfflineDatabaseService
                    .Instance
                    .GuardarAsync(
                        json,
                        tipoOperacion,
                        idServidor,
                        paquete,
                        idLocalExistente,
                        cancellationToken);

            int idPublico =
                AnalisisOfflineDatabaseService
                    .CrearIdPublico(entity.Id);

            AnalisisListadoEstadoService
                .MarcarActualizacionPendiente();

            object respuesta =
                new
                {
                    success = true,
                    message =
                        "El análisis fue calculado y guardado en este dispositivo. Se enviará al iniciar una próxima sesión en línea.",
                    data = new
                    {
                        analisisSueloId =
                            idPublico,
                        analisisSueloCalculoId =
                            idPublico,
                        formulaNutricionalId =
                            solicitud.BalanceNutricional != null
                                ? idPublico
                                : (int?)null,
                        enmiendaCalcareaId =
                            solicitud.EnmiendaCalcarea != null
                                ? idPublico
                                : (int?)null,
                        fertilizacionMixtaId =
                            solicitud.FertilizacionMixta != null
                                ? idPublico
                                : (int?)null
                    }
                };

            HttpResponseMessage response =
                CrearJson(
                    request,
                    respuesta);

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Guardado-Origen",
                "LOCAL");

            return response;
        }

        private static async Task<HttpResponseMessage?>
            IntentarResponderLecturaLocalAsync(
                HttpRequestMessage request,
                string path,
                CancellationToken cancellationToken)
        {
            if (path.StartsWith(
                    RutaDetallePrefijo,
                    StringComparison.OrdinalIgnoreCase))
            {
                int idDetalleLocal =
                    ObtenerUltimoEntero(path);

                if (!AnalisisOfflineDatabaseService
                    .EsIdLocal(idDetalleLocal))
                {
                    return null;
                }

                AnalisisOfflineLocalEntity? entity =
                    await AnalisisOfflineDatabaseService
                        .Instance
                        .ObtenerPorIdPublicoAsync(
                            idDetalleLocal);

                if (entity == null)
                {
                    return CrearError(
                        request,
                        HttpStatusCode.NotFound,
                        "No se encontró el análisis local.");
                }

                string detalle =
                    CrearDetalleJson(
                        entity,
                        idDetalleLocal);

                return CrearJsonCrudo(
                    request,
                    detalle);
            }

            if (!path.StartsWith(
                    RutaReportePrefijo,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string[] partes =
                path.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length < 5 ||
                !int.TryParse(
                    partes[3],
                    out int idReporteLocal) ||
                !AnalisisOfflineDatabaseService
                    .EsIdLocal(idReporteLocal))
            {
                return null;
            }

            AnalisisOfflineLocalEntity? local =
                await AnalisisOfflineDatabaseService
                    .Instance
                    .ObtenerPorIdPublicoAsync(
                        idReporteLocal);

            if (local == null)
            {
                return CrearError(
                    request,
                    HttpStatusCode.NotFound,
                    "No se encontró el análisis local para generar el reporte.");
            }

            GuardarTodoRequest? solicitud =
                JsonSerializer.Deserialize<
                    GuardarTodoRequest>(
                    local.PayloadJson,
                    JsonOptions);

            if (solicitud == null)
            {
                return CrearError(
                    request,
                    HttpStatusCode.UnprocessableEntity,
                    "No fue posible reconstruir el reporte local.");
            }

            AnalisisGuardadoResumen resumenReporte =
                await AnalisisReporteLocalEnrichmentService
                    .CrearResumenAsync(
                        local.TerrenoId,
                        idReporteLocal,
                        idReporteLocal,
                        local.IdentificadorAnalisis);

            AnalisisReporte reporte =
                AnalisisReporteMapper
                    .DesdeSolicitudGuardada(
                        solicitud,
                        resumenReporte);

            reporte.AnalisisSueloCalculoId =
                idReporteLocal;

            reporte.Responsable =
                Microsoft.Maui.Storage.Preferences.Get(
                    SessionKeys.KeyNombreCompletoUsuario,
                    "Usuario local");

            if (string.Equals(
                    partes[4],
                    "datos",
                    StringComparison.OrdinalIgnoreCase))
            {
                return CrearJson(
                    request,
                    reporte);
            }

            if (!string.Equals(
                    partes[4],
                    "pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            byte[] pdf =
                AnalisisPdfLocalService
                    .Generar(reporte);

            var response =
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    RequestMessage =
                        request,
                    Content =
                        new ByteArrayContent(pdf)
                };

            response.Content.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/pdf");

            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue(
                    "attachment")
                {
                    FileNameStar =
                        reporte.NombreArchivoBase +
                        ".pdf"
                };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Reporte-Origen",
                "LOCAL");

            return response;
        }

        private static string CrearDetalleJson(
            AnalisisOfflineLocalEntity entity,
            int idPublico)
        {
            JsonObject root =
                JsonNode.Parse(
                    entity.PayloadJson)?
                    .AsObject()
                ?? throw new InvalidOperationException(
                    "El contenido local no es válido.");

            JsonObject datos =
                root["datosAnalisis"]?
                    .DeepClone()
                    .AsObject()
                ?? new JsonObject();

            datos["analisisSueloId"] =
                idPublico;

            datos["fechaCreacionAnalisisSuelo"] =
                entity.FechaCreacionUtc;

            JsonArray elementosOriginales =
                datos["elementosQuimicos"]
                    as JsonArray
                ?? new JsonArray();

            foreach (JsonNode? node
                     in elementosOriginales)
            {
                if (node is JsonObject item)
                {
                    item["analisisSueloElementoQuimicoId"] =
                        0;
                }
            }

            JsonObject requerimiento =
                root["requerimientoAnual"]?
                    .DeepClone()
                    .AsObject()
                ?? new JsonObject();

            requerimiento["analisisSueloCalculoId"] =
                idPublico;

            if (requerimiento["elementos"]
                is JsonArray elementosCalculados)
            {
                foreach (JsonNode? node
                         in elementosCalculados)
                {
                    if (node is not JsonObject item)
                        continue;

                    item[
                        "analisisSueloCalculoElementoQuimicoId"] =
                            0;

                    item["unidadMedidaId"] =
                        item["unidadMedidaResultadoId"]?
                            .DeepClone();
                }
            }

            JsonNode? balance =
                CrearBalanceDetalle(
                    root["balanceNutricional"],
                    root["requerimientoAnual"],
                    idPublico);

            JsonNode? enmienda =
                CrearEnmiendaDetalle(
                    root["enmiendaCalcarea"],
                    idPublico);

            JsonNode? mixta =
                CrearMixtaDetalle(
                    root["fertilizacionMixta"],
                    idPublico);

            var response =
                new JsonObject
                {
                    ["success"] = true,
                    ["message"] =
                        "Detalle local cargado correctamente.",
                    ["data"] =
                        new JsonObject
                        {
                            ["datosAnalisis"] =
                                datos,
                            ["requerimientoAnual"] =
                                requerimiento,
                            ["balanceNutricional"] =
                                balance,
                            ["enmiendaCalcarea"] =
                                enmienda,
                            ["fertilizacionMixta"] =
                                mixta
                        }
                };

            return response.ToJsonString(
                JsonOptions);
        }

        private static JsonNode? CrearBalanceDetalle(
            JsonNode? node,
            JsonNode? requerimientoNode,
            int idPublico)
        {
            if (node is not JsonObject balance)
                return null;

            JsonObject resultado =
                balance["resultado"]?
                    .DeepClone()
                    .AsObject()
                ?? new JsonObject();

            resultado["formulaNutricionalId"] =
                idPublico;

            resultado["fechaCreacion"] =
                DateTime.UtcNow.ToString("O");

            resultado["terrenoId"] =
                balance["terrenoId"]?
                    .DeepClone();

            resultado["esComplementoFertilizacionMixta"] =
                balance[
                    "esComplementoFertilizacionMixta"]?
                    .DeepClone();

            Dictionary<string, int> elementosPorSimbolo =
                ConstruirElementosPorSimbolo(
                    requerimientoNode);

            JsonArray detalles =
                new();

            JsonArray aportes =
                new();

            JsonArray items =
                balance["items"]
                    as JsonArray
                ?? new JsonArray();

            JsonArray resultadoDetalles =
                resultado["detalle"]
                    as JsonArray
                ?? new JsonArray();

            for (
                int index = 0;
                index < resultadoDetalles.Count;
                index++)
            {
                int detalleId =
                    index + 1;

                JsonObject source =
                    resultadoDetalles[index]?
                        .AsObject()
                    ?? new JsonObject();

                JsonObject requestItem =
                    index < items.Count
                        ? items[index]?
                            .AsObject()
                          ?? new JsonObject()
                        : new JsonObject();

                detalles.Add(
                    new JsonObject
                    {
                        ["formulaNutricionalDetalleId"] =
                            detalleId,
                        ["fuenteNutrientesId"] =
                            requestItem[
                                "fuenteNutrientesId"]?
                                .DeepClone(),
                        ["elementoQuimicosId"] =
                            requestItem[
                                "elementoQuimicosId"]?
                                .DeepClone(),
                        ["libras"] =
                            source["lb"]?
                                .DeepClone(),
                        ["qq"] =
                            source["qq"]?
                                .DeepClone(),
                        ["requerimientoLibras"] =
                            source[
                                "requerimientoLibras"]?
                                .DeepClone(),
                        ["precioPorQuintal"] =
                            source[
                                "precioPorQuintal"]?
                                .DeepClone(),
                        ["subtotalFuente"] =
                            source[
                                "subtotalFuente"]?
                                .DeepClone(),
                        ["onzasAnuales"] =
                            source[
                                "onzasAnuales"]?
                                .DeepClone(),
                        ["onzasPorAplicacion"] =
                            source[
                                "onzasPorAplicacion"]?
                                .DeepClone()
                    });

                if (source["aportes"]
                    is not JsonObject aportesSource)
                {
                    continue;
                }

                foreach (KeyValuePair<
                             string,
                             JsonNode?> aporte
                         in aportesSource)
                {
                    string simbolo =
                        aporte.Key
                            .Trim()
                            .ToUpperInvariant();

                    if (!elementosPorSimbolo
                        .TryGetValue(
                            simbolo,
                            out int elementoId))
                    {
                        continue;
                    }

                    aportes.Add(
                        new JsonObject
                        {
                            ["formulaNutricionalAporteId"] =
                                0,
                            ["formulaNutricionalDetalleId"] =
                                detalleId,
                            ["elementoQuimicosId"] =
                                elementoId,
                            ["valor"] =
                                aporte.Value?
                                    .DeepClone()
                        });
                }
            }

            return new JsonObject
            {
                ["formula"] =
                    resultado,
                ["detalles"] =
                    detalles,
                ["aportes"] =
                    aportes
            };
        }

        private static Dictionary<string, int>
            ConstruirElementosPorSimbolo(
                JsonNode? requerimientoNode)
        {
            var resultado =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            if (requerimientoNode is not
                    JsonObject requerimiento ||
                requerimiento["elementos"]
                    is not JsonArray elementos)
            {
                return resultado;
            }

            foreach (JsonNode? node
                     in elementos)
            {
                if (node is not JsonObject item)
                    continue;

                string simbolo =
                    item[
                        "simboloElementoQuimico"]?
                        .GetValue<string>()?
                        .Trim()
                        .ToUpperInvariant()
                    ?? string.Empty;

                int elementoId =
                    item[
                        "elementoQuimicosId"]?
                        .GetValue<int>()
                    ?? 0;

                if (!string.IsNullOrWhiteSpace(
                        simbolo) &&
                    elementoId > 0)
                {
                    resultado[simbolo] =
                        elementoId;
                }
            }

            return resultado;
        }

        private static JsonNode? CrearEnmiendaDetalle(
            JsonNode? node,
            int idPublico)
        {
            if (node is not JsonObject enmienda)
                return null;

            JsonObject resultado =
                enmienda["resultado"]?
                    .DeepClone()
                    .AsObject()
                ?? new JsonObject();

            resultado["enmiendaCalcareaId"] =
                idPublico;

            resultado["fuenteNutrientesId"] =
                enmienda["fuenteNutrientesId"]?
                    .DeepClone();

            resultado["fechaCreacion"] =
                DateTime.UtcNow.ToString("O");

            return resultado;
        }

        private static JsonNode? CrearMixtaDetalle(
            JsonNode? node,
            int idPublico)
        {
            if (node is not JsonObject mixta)
                return null;

            JsonArray fuentes =
                new();

            if (mixta["fuentes"]
                is JsonArray sourceFuentes)
            {
                foreach (JsonNode? nodeFuente
                         in sourceFuentes)
                {
                    JsonObject source =
                        nodeFuente?
                            .AsObject()
                        ?? new JsonObject();

                    fuentes.Add(
                        new JsonObject
                        {
                            ["fertilizacionMixtaFuenteId"] =
                                0,
                            ["fuenteNutrientesId"] =
                                source[
                                    "fuenteNutrientesId"]?
                                    .DeepClone(),
                            ["cantidadQq"] =
                                source["cantidadQq"]?
                                    .DeepClone()
                        });
                }
            }

            JsonArray detalles =
                new();

            if (mixta["detalles"]
                is JsonArray sourceDetalles)
            {
                foreach (JsonNode? nodeDetalle
                         in sourceDetalles)
                {
                    JsonObject source =
                        nodeDetalle?
                            .AsObject()
                        ?? new JsonObject();

                    detalles.Add(
                        new JsonObject
                        {
                            ["fertilizacionMixtaDetalleId"] =
                                0,
                            ["elementoQuimicosId"] =
                                source[
                                    "elementoQuimicosId"]?
                                    .DeepClone(),
                            ["requerimientoOriginal"] =
                                source["exportable"]?
                                    .DeepClone(),
                            ["aporteOrganico"] =
                                source[
                                    "aporteOrganico"]?
                                    .DeepClone(),
                            ["diferencia"] =
                                source["diferencia"]?
                                    .DeepClone(),
                            ["deficit"] =
                                source["deficit"]?
                                    .DeepClone(),
                            ["sobrante"] =
                                source["sobrante"]?
                                    .DeepClone()
                        });
                }
            }

            return new JsonObject
            {
                ["mixta"] =
                    new JsonObject
                    {
                        ["fertilizacionMixtaId"] =
                            idPublico,
                        ["fechaCalculo"] =
                            DateTime.UtcNow.ToString("O"),
                        ["observacion"] =
                            mixta["observacion"]?
                                .DeepClone(),
                        ["esComplementoBalance"] =
                            mixta[
                                "esComplementoBalance"]?
                                .DeepClone()
                    },
                ["fuentes"] =
                    fuentes,
                ["detalles"] =
                    detalles
            };
        }

        private static bool EsFalloInfraestructura(
            HttpStatusCode statusCode) =>
            statusCode is
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri =
                request.RequestUri;

            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath;

            string raw =
                uri.OriginalString;

            int query =
                raw.IndexOf('?');

            if (query >= 0)
                raw = raw[..query];

            return "/" +
                raw.TrimStart('/');
        }

        private static int ObtenerUltimoEntero(
            string path)
        {
            string value =
                path.TrimEnd('/')
                    .Split('/')
                    .LastOrDefault()
                ?? string.Empty;

            return int.TryParse(
                value,
                out int result)
                    ? result
                    : 0;
        }

        private static void RestaurarContenido(
            HttpRequestMessage request,
            byte[] contenido)
        {
            if (request.Content == null)
                return;

            var restored =
                new ByteArrayContent(contenido);

            foreach (var header
                     in request.Content.Headers)
            {
                restored.Headers
                    .TryAddWithoutValidation(
                        header.Key,
                        header.Value);
            }

            request.Content = restored;
        }

        private static HttpResponseMessage CrearJson(
            HttpRequestMessage request,
            object data) =>
            CrearJsonCrudo(
                request,
                JsonSerializer.Serialize(
                    data,
                    JsonOptions));

        private static HttpResponseMessage CrearJsonCrudo(
            HttpRequestMessage request,
            string json) =>
            new(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
            };

        private static HttpResponseMessage CrearError(
            HttpRequestMessage request,
            HttpStatusCode status,
            string message) =>
            new(status)
            {
                RequestMessage = request,
                Content =
                    new StringContent(
                        JsonSerializer.Serialize(
                            new
                            {
                                success = false,
                                message
                            },
                            JsonOptions),
                        Encoding.UTF8,
                        "application/json")
            };
    }
}
