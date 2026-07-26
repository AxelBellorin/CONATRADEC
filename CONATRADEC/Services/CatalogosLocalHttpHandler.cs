using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CONATRADEC.Services
{
    public sealed class CatalogosLocalHttpHandler : DelegatingHandler
    {
        private const string HeaderBypass = "X-Offline-Bypass";

        private readonly ContenidoLocalDatabaseService database =
            ContenidoLocalDatabaseService.Instance;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (TieneBypass(request))
            {
                request.Headers.Remove(HeaderBypass);
                return await base.SendAsync(request, cancellationToken);
            }

            if (request.Method == HttpMethod.Get &&
                EsRutaCatalogo(request))
            {
                HttpResponseMessage? local =
                    await CrearRespuestaLocalAsync(
                        request,
                        cancellationToken);

                if (local != null)
                {
                    PaqueteCatalogosOfflineService.Instance
                        .VerificarActualizacionEnSegundoPlano();

                    return local;
                }
            }

            HttpResponseMessage response =
                await base.SendAsync(request, cancellationToken);

            if (request.Method != HttpMethod.Get &&
                response.IsSuccessStatusCode &&
                EsRutaCatalogo(request))
            {
                await PaqueteCatalogosOfflineService.Instance
                    .MarcarActualizacionPendienteAsync();

                await SincronizacionOfflineGlobalService.Instance
                    .MarcarActualizacionDisponibleAsync(
                        "Los catálogos fueron modificados y deben actualizarse.");
            }

            return response;
        }

        private async Task<HttpResponseMessage?> CrearRespuestaLocalAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            if (string.IsNullOrWhiteSpace(usuarioId))
                return null;

            CatalogoOfflineEstadoEntity? estado =
                await database.ObtenerEstadoPaqueteAsync(usuarioId);

            if (estado == null ||
                string.IsNullOrWhiteSpace(estado.PaqueteActivoId))
            {
                return null;
            }

            /*
             * Cuando se detectó un cambio y existe conexión, se consulta
             * temporalmente el servidor para que el usuario vea los datos
             * nuevos antes de actualizar el paquete completo.
             */
            if (estado.Estado ==
                    CatalogoOfflineEstados.ActualizacionDisponible &&
                EstadoConexionService.Instance.HayInternet)
            {
                return null;
            }

            string path = ObtenerPath(request).ToLowerInvariant();
            Dictionary<string, string> query = LeerQuery(request);

            if (path == "/api/terreno/listar")
            {
                return await RespuestaSeccionAsync(
                    request,
                    usuarioId,
                    "terrenos");
            }

            if (path == "/api/terreno/buscar")
            {
                return await RespuestaTerrenosPaginadaAsync(
                    request,
                    usuarioId,
                    query);
            }

            if (path == "/api/pais")
                return await RespuestaSeccionAsync(
                    request, usuarioId, "paises");

            if (path == "/api/pais/buscar")
                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "paises",
                    query,
                    new[] { "nombrePais", "codigoISOPais" });

            if (path.StartsWith("/api/departamento/por-pais/"))
            {
                return await RespuestaFiltradaAsync(
                    request,
                    usuarioId,
                    "departamentos",
                    "paisId",
                    LeerUltimoId(path));
            }

            if (path == "/api/departamento/buscar")
            {
                int paisId = LeerEntero(query, "paisId");

                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "departamentos",
                    query,
                    new[] { "nombreDepartamento", "nombrePais" },
                    ("paisId", paisId),
                    new Dictionary<string, object?>
                    {
                        ["paisId"] = paisId,
                        ["nombrePais"] =
                            await BuscarNombreRelacionadoAsync(
                                usuarioId,
                                "paises",
                                "paisId",
                                paisId,
                                "nombrePais")
                    });
            }

            if (path.StartsWith(
                    "/api/municipio/por-departamento/"))
            {
                return await RespuestaFiltradaAsync(
                    request,
                    usuarioId,
                    "municipios",
                    "departamentoId",
                    LeerUltimoId(path));
            }

            if (path ==
                "/api/municipio/" +
                "listartodos-por-departamento-por-pais")
            {
                return await RespuestaSeccionAsync(
                    request,
                    usuarioId,
                    "municipiosUbicacion");
            }

            if (path == "/api/municipio/buscar")
            {
                int departamentoId =
                    LeerEntero(query, "departamentoId");

                string nombreDepartamento =
                    await BuscarNombreRelacionadoAsync(
                        usuarioId,
                        "departamentos",
                        "departamentoId",
                        departamentoId,
                        "nombreDepartamento");

                int paisId =
                    await BuscarEnteroRelacionadoAsync(
                        usuarioId,
                        "departamentos",
                        "departamentoId",
                        departamentoId,
                        "paisId");

                string nombrePais =
                    await BuscarNombreRelacionadoAsync(
                        usuarioId,
                        "paises",
                        "paisId",
                        paisId,
                        "nombrePais");

                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "municipios",
                    query,
                    new[]
                    {
                        "nombreMunicipio",
                        "nombreDepartamento",
                        "nombrePais"
                    },
                    ("departamentoId", departamentoId),
                    new Dictionary<string, object?>
                    {
                        ["departamentoId"] = departamentoId,
                        ["nombreDepartamento"] = nombreDepartamento,
                        ["paisId"] = paisId,
                        ["nombrePais"] = nombrePais
                    });
            }

            if (path == "/api/configuracion/tipos-cultivo")
                return await RespuestaSeccionAsync(
                    request, usuarioId, "tiposCultivo");

            if (path ==
                "/api/configuracion/tipos-cultivo/buscar")
            {
                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "tiposCultivo",
                    query,
                    new[]
                    {
                        "nombreTipoCultivo",
                        "codigoTipoCultivo",
                        "descripcionTipoCultivo"
                    });
            }

            if (path ==
                "/api/configuracion/tipos-analisis-suelo")
            {
                return await RespuestaSeccionAsync(
                    request, usuarioId, "tiposAnalisis");
            }

            if (path ==
                "/api/configuracion/" +
                "tipos-analisis-suelo/buscar")
            {
                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "tiposAnalisis",
                    query,
                    new[]
                    {
                        "nombreTipoAnalisisSuelo",
                        "codigoTipoAnalisisSuelo",
                        "descripcionTipoAnalisisSuelo"
                    });
            }

            if (path == "/api/elemento-quimico/listar")
                return await RespuestaSeccionAsync(
                    request, usuarioId, "elementosQuimicos");

            if (path == "/api/elemento-quimico/buscar")
            {
                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "elementosQuimicos",
                    query,
                    new[]
                    {
                        "nombreElementoQuimico",
                        "simboloElementoQuimico"
                    });
            }

            if (path == "/api/fuente-nutriente/listar")
                return await RespuestaSeccionAsync(
                    request, usuarioId, "fuentesNutrientes");

            if (path ==
                "/api/fuente-nutriente/" +
                "listar-fertilizacion-mixta")
            {
                return await RespuestaSeccionAsync(
                    request,
                    usuarioId,
                    "fuentesFertilizacionMixta");
            }

            if (path == "/api/fuente-nutriente/aportes-tabla")
                return await RespuestaSeccionAsync(
                    request, usuarioId, "aportesFuentes");

            if (path == "/api/fuente-nutriente/composicion")
            {
                return await RespuestaFuentesAsync(
                    request,
                    usuarioId,
                    "composicionFuentes",
                    query,
                    false);
            }

            if (path == "/api/fuente-nutriente/buscar")
            {
                return await RespuestaFuentesAsync(
                    request,
                    usuarioId,
                    "fuentesNutrientes",
                    query,
                    true);
            }

            if (path ==
                "/api/configuracion-unidades/formulario-analisis")
            {
                return await RespuestaEnvelopeAsync(
                    request,
                    usuarioId,
                    "unidadesFormulario");
            }

            if (path ==
                "/api/configuracion-unidades/catalogo-unidades")
            {
                return await RespuestaEnvelopeAsync(
                    request,
                    usuarioId,
                    "unidadesCatalogo");
            }

            if (path == "/api/configuracion-unidades/elementos")
                return await RespuestaEnvelopeAsync(
                    request, usuarioId, "unidadesElementos");

            if (path.StartsWith(
                    "/api/configuracion-unidades/elemento/",
                    StringComparison.Ordinal))
            {
                return await RespuestaDetalleEnvelopeAsync(
                    request,
                    usuarioId,
                    "unidadesElementos",
                    "elementoQuimicosId",
                    LeerUltimoId(path));
            }

            if (path ==
                "/api/configuracion-unidades/materia-organica")
            {
                return await RespuestaEnvelopeAsync(
                    request,
                    usuarioId,
                    "unidadesMateriaOrganica");
            }

            if (path == "/api/configuracion-unidades/formulas")
                return await RespuestaEnvelopeAsync(
                    request, usuarioId, "formulasConversion");

            if (path ==
                "/api/configuracion/rangos-nutrientes")
            {
                return await RespuestaSeccionAsync(
                    request,
                    usuarioId,
                    "rangosNutrientes");
            }

            if (path ==
                "/api/configuracion/rangos-nutrientes/cultivos")
            {
                return await RespuestaPaginaGuardadaAsync(
                    request,
                    usuarioId,
                    "rangoCultivosPagina",
                    query);
            }

            if (path ==
                "/api/configuracion/rangos-nutrientes/buscar")
            {
                return await RespuestaPaginadaAsync(
                    request,
                    usuarioId,
                    "rangosNutrientes",
                    query,
                    new[]
                    {
                        "nombreElementoQuimico",
                        "simboloElementoQuimico",
                        "descripcionParametro"
                    },
                    ("tipoCultivoId",
                        LeerEntero(query, "tipoCultivoId")));
            }

            if (path ==
                "/api/configuracion/rangos-nutrientes/" +
                "elementos-disponibles")
            {
                return await RespuestaSeccionAsync(
                    request, usuarioId, "elementosQuimicos");
            }

            return null;
        }

        private async Task<HttpResponseMessage?> RespuestaSeccionAsync(
            HttpRequestMessage request,
            string usuarioId,
            string seccion)
        {
            CatalogoOfflineSeccionEntity? entity =
                await database.ObtenerSeccionPaqueteActivoAsync(
                    usuarioId,
                    seccion);

            return entity == null
                ? null
                : CrearJsonResponse(request, entity.Json);
        }

        private async Task<HttpResponseMessage?> RespuestaEnvelopeAsync(
            HttpRequestMessage request,
            string usuarioId,
            string seccion)
        {
            CatalogoOfflineSeccionEntity? entity =
                await database.ObtenerSeccionPaqueteActivoAsync(
                    usuarioId,
                    seccion);

            if (entity == null)
                return null;

            JsonNode? data = JsonNode.Parse(entity.Json);

            var envelope = new JsonObject
            {
                ["success"] = true,
                ["message"] =
                    "Datos sincronizados anteriormente.",
                ["data"] = data?.DeepClone()
            };

            return CrearJsonResponse(
                request,
                envelope.ToJsonString());
        }

        private async Task<HttpResponseMessage?>
            RespuestaDetalleEnvelopeAsync(
                HttpRequestMessage request,
                string usuarioId,
                string seccion,
                string propiedadId,
                int id)
        {
            JsonArray? data =
                await LeerArrayDesdeSeccionFlexibleAsync(
                    usuarioId,
                    seccion);

            if (data == null)
                return null;

            JsonObject? item = data
                .OfType<JsonObject>()
                .FirstOrDefault(x =>
                    ObtenerEntero(x, propiedadId) == id);

            if (item == null)
                return null;

            var envelope = new JsonObject
            {
                ["success"] = true,
                ["message"] =
                    "Datos sincronizados anteriormente.",
                ["data"] = item.DeepClone()
            };

            return CrearJsonResponse(
                request,
                envelope.ToJsonString());
        }

        private async Task<JsonArray?>
            LeerArrayDesdeSeccionFlexibleAsync(
                string usuarioId,
                string seccion)
        {
            CatalogoOfflineSeccionEntity? entity =
                await database.ObtenerSeccionPaqueteActivoAsync(
                    usuarioId,
                    seccion);

            if (entity == null)
                return null;

            JsonNode? root = JsonNode.Parse(entity.Json);

            if (root is JsonArray direct)
                return direct;

            if (root is JsonObject obj &&
                BuscarPropiedad(obj, "items") is JsonArray items)
            {
                return items;
            }

            return null;
        }

        private async Task<HttpResponseMessage?> RespuestaFiltradaAsync(
            HttpRequestMessage request,
            string usuarioId,
            string seccion,
            string propiedad,
            int valor)
        {
            JsonArray? data =
                await LeerArrayAsync(usuarioId, seccion);

            if (data == null)
                return null;

            var result = new JsonArray();

            foreach (JsonObject item in data.OfType<JsonObject>())
            {
                if (ObtenerEntero(item, propiedad) == valor)
                    result.Add(item.DeepClone());
            }

            OrdenarPorNombre(result);
            return CrearJsonResponse(request, result.ToJsonString());
        }

        private async Task<HttpResponseMessage?> RespuestaPaginadaAsync(
            HttpRequestMessage request,
            string usuarioId,
            string seccion,
            Dictionary<string, string> query,
            string[] propiedadesBusqueda,
            (string Propiedad, int Valor)? filtro = null,
            Dictionary<string, object?>? adicionales = null)
        {
            JsonArray? data =
                await LeerArrayAsync(usuarioId, seccion);

            if (data == null)
                return null;

            string buscar = ObtenerQuery(query, "buscar");

            List<JsonObject> items = data
                .OfType<JsonObject>()
                .Where(item =>
                    !filtro.HasValue ||
                    filtro.Value.Valor <= 0 ||
                    ObtenerEntero(
                        item,
                        filtro.Value.Propiedad) ==
                    filtro.Value.Valor)
                .Where(item =>
                    CoincideBusqueda(
                        item,
                        buscar,
                        propiedadesBusqueda))
                .OrderBy(
                    ObtenerNombreOrden,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return CrearRespuestaPagina(
                request,
                items,
                query,
                adicionales);
        }

        private async Task<HttpResponseMessage?> RespuestaFuentesAsync(
            HttpRequestMessage request,
            string usuarioId,
            string seccion,
            Dictionary<string, string> query,
            bool paginar)
        {
            JsonArray? data =
                await LeerArrayAsync(usuarioId, seccion);

            if (data == null)
                return null;

            string buscar = ObtenerQuery(query, "buscar");
            string categoria = ObtenerQuery(query, "categoria");

            List<JsonObject> items = data
                .OfType<JsonObject>()
                .Where(item =>
                    CoincideBusqueda(
                        item,
                        buscar,
                        new[]
                        {
                            "nombreNutriente",
                            "descripcionNutriente"
                        }))
                .Where(item =>
                    CoincideCategoriaFuente(item, categoria))
                .OrderBy(
                    item => ObtenerTexto(item, "nombreNutriente"),
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (paginar)
            {
                return CrearRespuestaPagina(
                    request,
                    items,
                    query,
                    null);
            }

            var array = new JsonArray();

            foreach (JsonObject item in items)
                array.Add(item.DeepClone());

            return CrearJsonResponse(request, array.ToJsonString());
        }

        private async Task<HttpResponseMessage?>
            RespuestaPaginaGuardadaAsync(
                HttpRequestMessage request,
                string usuarioId,
                string seccion,
                Dictionary<string, string> query)
        {
            CatalogoOfflineSeccionEntity? entity =
                await database.ObtenerSeccionPaqueteActivoAsync(
                    usuarioId,
                    seccion);

            if (entity == null)
                return null;

            JsonNode? root = JsonNode.Parse(entity.Json);

            JsonArray source =
                root is JsonObject obj &&
                BuscarPropiedad(obj, "items") is JsonArray array
                    ? array
                    : new JsonArray();

            string buscar = ObtenerQuery(query, "buscar");

            List<JsonObject> items = source
                .OfType<JsonObject>()
                .Where(item =>
                    CoincideBusqueda(
                        item,
                        buscar,
                        new[]
                        {
                            "nombreTipoCultivo",
                            "codigoTipoCultivo"
                        }))
                .OrderBy(
                    ObtenerNombreOrden,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return CrearRespuestaPagina(
                request,
                items,
                query,
                null);
        }

        private async Task<HttpResponseMessage?>
            RespuestaTerrenosPaginadaAsync(
                HttpRequestMessage request,
                string usuarioId,
                Dictionary<string, string> query)
        {
            JsonArray? source =
                await LeerArrayAsync(
                    usuarioId,
                    "terrenos");

            if (source == null)
                return null;

            string texto =
                ObtenerQuery(query, "texto");

            string codigo =
                ObtenerQuery(
                    query,
                    "codigoTerreno");

            string propietario =
                ObtenerQuery(
                    query,
                    "nombrePropietario");

            string identificacion =
                ObtenerQuery(
                    query,
                    "identificacionPropietario");

            string direccion =
                ObtenerQuery(
                    query,
                    "direccion");

            int paisId =
                LeerEntero(
                    query,
                    "paisId");

            int departamentoId =
                LeerEntero(
                    query,
                    "departamentoId");

            int municipioId =
                LeerEntero(
                    query,
                    "municipioId");

            DateOnly? fechaDesde =
                LeerFecha(
                    query,
                    "fechaDesde");

            DateOnly? fechaHasta =
                LeerFecha(
                    query,
                    "fechaHasta");

            decimal? extensionMinima =
                LeerDecimal(
                    query,
                    "extensionMinima");

            decimal? extensionMaxima =
                LeerDecimal(
                    query,
                    "extensionMaxima");

            string ordenarPor =
                ObtenerQuery(
                    query,
                    "ordenarPor");

            bool descendente =
                LeerBooleano(
                    query,
                    "descendente");

            IEnumerable<JsonObject> filtrados =
                source
                    .OfType<JsonObject>()
                    .Where(item =>
                        !EsFalso(
                            BuscarPropiedad(
                                item,
                                "activo")))
                    .Where(item =>
                        CoincideTerrenoTexto(
                            item,
                            texto))
                    .Where(item =>
                        CoincideTextoExactoParcial(
                            item,
                            "codigoTerreno",
                            codigo))
                    .Where(item =>
                        CoincideTextoExactoParcial(
                            item,
                            "nombrePropietarioTerreno",
                            propietario))
                    .Where(item =>
                        CoincideTextoExactoParcial(
                            item,
                            "identificacionPropietarioTerreno",
                            identificacion))
                    .Where(item =>
                        CoincideTextoExactoParcial(
                            item,
                            "direccionTerreno",
                            direccion))
                    .Where(item =>
                        paisId <= 0 ||
                        ObtenerEnteroUbicacion(
                            item,
                            "paisId") ==
                        paisId)
                    .Where(item =>
                        departamentoId <= 0 ||
                        ObtenerEnteroUbicacion(
                            item,
                            "departamentoId") ==
                        departamentoId)
                    .Where(item =>
                        municipioId <= 0 ||
                        ObtenerMunicipioId(item) ==
                        municipioId)
                    .Where(item =>
                        !fechaDesde.HasValue ||
                        (
                            ObtenerFecha(
                                item,
                                "fechaIngresoTerreno")
                            is DateOnly fecha &&
                            fecha >= fechaDesde.Value
                        ))
                    .Where(item =>
                        !fechaHasta.HasValue ||
                        (
                            ObtenerFecha(
                                item,
                                "fechaIngresoTerreno")
                            is DateOnly fecha &&
                            fecha <= fechaHasta.Value
                        ))
                    .Where(item =>
                        !extensionMinima.HasValue ||
                        ObtenerDecimal(
                            item,
                            "extensionManzanaTerreno") >=
                        extensionMinima.Value)
                    .Where(item =>
                        !extensionMaxima.HasValue ||
                        ObtenerDecimal(
                            item,
                            "extensionManzanaTerreno") <=
                        extensionMaxima.Value);

            IOrderedEnumerable<JsonObject> ordenados =
                ordenarPor
                    .Trim()
                    .ToLowerInvariant()
                switch
                {
                    "propietario" when descendente =>
                        filtrados.OrderByDescending(
                            item =>
                                ObtenerTexto(
                                    item,
                                    "nombrePropietarioTerreno"),
                            StringComparer
                                .CurrentCultureIgnoreCase),

                    "propietario" =>
                        filtrados.OrderBy(
                            item =>
                                ObtenerTexto(
                                    item,
                                    "nombrePropietarioTerreno"),
                            StringComparer
                                .CurrentCultureIgnoreCase),

                    "fecha" when descendente =>
                        filtrados.OrderByDescending(
                            item =>
                                ObtenerFecha(
                                    item,
                                    "fechaIngresoTerreno")),

                    "fecha" =>
                        filtrados.OrderBy(
                            item =>
                                ObtenerFecha(
                                    item,
                                    "fechaIngresoTerreno")),

                    "extension" when descendente =>
                        filtrados.OrderByDescending(
                            item =>
                                ObtenerDecimal(
                                    item,
                                    "extensionManzanaTerreno")),

                    "extension" =>
                        filtrados.OrderBy(
                            item =>
                                ObtenerDecimal(
                                    item,
                                    "extensionManzanaTerreno")),

                    _ when descendente =>
                        filtrados.OrderByDescending(
                            item =>
                                ObtenerTexto(
                                    item,
                                    "codigoTerreno"),
                            StringComparer
                                .CurrentCultureIgnoreCase),

                    _ =>
                        filtrados.OrderBy(
                            item =>
                                ObtenerTexto(
                                    item,
                                    "codigoTerreno"),
                            StringComparer
                                .CurrentCultureIgnoreCase)
                };

            List<JsonObject> items =
                ordenados
                    .ThenBy(
                        item =>
                            ObtenerEntero(
                                item,
                                "terrenoId"))
                    .ToList();

            int pagina =
                Math.Max(
                    1,
                    LeerEntero(
                        query,
                        "page",
                        1));

            int tamanoPagina =
                Math.Clamp(
                    LeerEntero(
                        query,
                        "pageSize",
                        20),
                    1,
                    100);

            int total =
                items.Count;

            int totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        total /
                        (double)tamanoPagina));

            var data =
                new JsonArray();

            foreach (JsonObject item
                     in items
                         .Skip(
                             (pagina - 1) *
                             tamanoPagina)
                         .Take(
                             tamanoPagina))
            {
                data.Add(
                    item.DeepClone());
            }

            var resultado =
                new JsonObject
                {
                    ["total"] = total,
                    ["page"] = pagina,
                    ["pageSize"] =
                        tamanoPagina,
                    ["totalPages"] =
                        totalPaginas,
                    ["data"] = data
                };

            return CrearJsonResponse(
                request,
                resultado.ToJsonString());
        }

        private static bool CoincideTerrenoTexto(
            JsonObject item,
            string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            string[] propiedades =
            {
                "codigoTerreno",
                "nombrePropietarioTerreno",
                "identificacionPropietarioTerreno",
                "direccionTerreno",
                "correoPropietario"
            };

            if (propiedades.Any(propiedad =>
                    ObtenerTexto(
                        item,
                        propiedad)
                    .Contains(
                        texto,
                        StringComparison
                            .CurrentCultureIgnoreCase)))
            {
                return true;
            }

            JsonObject? ubicacion =
                BuscarPropiedad(
                    item,
                    "ubicacion")
                as JsonObject;

            if (ubicacion == null)
                return false;

            return new[]
            {
                "nombrePais",
                "nombreDepartamento",
                "nombreMunicipio"
            }
            .Any(propiedad =>
                ObtenerTexto(
                    ubicacion,
                    propiedad)
                .Contains(
                    texto,
                    StringComparison
                        .CurrentCultureIgnoreCase));
        }

        private static bool
            CoincideTextoExactoParcial(
                JsonObject item,
                string propiedad,
                string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ||
                   ObtenerTexto(
                       item,
                       propiedad)
                   .Contains(
                       valor,
                       StringComparison
                           .CurrentCultureIgnoreCase);
        }

        private static int ObtenerEnteroUbicacion(
            JsonObject item,
            string propiedad)
        {
            return BuscarPropiedad(
                       item,
                       "ubicacion")
                   is JsonObject ubicacion
                ? ObtenerEntero(
                    ubicacion,
                    propiedad)
                : 0;
        }

        private static int ObtenerMunicipioId(
            JsonObject item)
        {
            int directo =
                ObtenerEntero(
                    item,
                    "municipioId");

            return directo > 0
                ? directo
                : ObtenerEnteroUbicacion(
                    item,
                    "municipioId");
        }

        private static decimal ObtenerDecimal(
            JsonObject item,
            string propiedad)
        {
            JsonNode? value =
                BuscarPropiedad(
                    item,
                    propiedad);

            if (value == null)
                return 0m;

            string texto =
                value
                    .ToJsonString()
                    .Trim('"');

            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out decimal result)
                ? result
                : 0m;
        }

        private static DateOnly? ObtenerFecha(
            JsonObject item,
            string propiedad)
        {
            string texto =
                ObtenerTexto(
                    item,
                    propiedad);

            if (string.IsNullOrWhiteSpace(texto))
                return null;

            return DateOnly.TryParse(
                       texto,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out DateOnly fecha)
                ? fecha
                : null;
        }

        private static DateOnly? LeerFecha(
            IReadOnlyDictionary<string, string>
                query,
            string key)
        {
            string texto =
                ObtenerQuery(
                    query,
                    key);

            return DateOnly.TryParse(
                       texto,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out DateOnly fecha)
                ? fecha
                : null;
        }

        private static decimal? LeerDecimal(
            IReadOnlyDictionary<string, string>
                query,
            string key)
        {
            string texto =
                ObtenerQuery(
                    query,
                    key);

            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out decimal valor)
                ? valor
                : null;
        }

        private static bool LeerBooleano(
            IReadOnlyDictionary<string, string>
                query,
            string key)
        {
            return query.TryGetValue(
                       key,
                       out string? texto) &&
                   bool.TryParse(
                       texto,
                       out bool valor) &&
                   valor;
        }

        private static bool EsFalso(
            JsonNode? node)
        {
            return node != null &&
                   bool.TryParse(
                       node
                           .ToJsonString()
                           .Trim('"'),
                       out bool valor) &&
                   !valor;
        }

        private static HttpResponseMessage CrearRespuestaPagina(
            HttpRequestMessage request,
            List<JsonObject> items,
            Dictionary<string, string> query,
            Dictionary<string, object?>? adicionales)
        {
            int pagina = Math.Max(1, LeerEntero(query, "pagina", 1));
            int tamano = Math.Clamp(
                LeerEntero(query, "tamanoPagina", 10),
                1,
                100);

            int total = items.Count;
            int totalPaginas = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)tamano));

            var pageItems = new JsonArray();

            foreach (JsonObject item in items
                         .Skip((pagina - 1) * tamano)
                         .Take(tamano))
            {
                pageItems.Add(item.DeepClone());
            }

            var result = new JsonObject
            {
                ["items"] = pageItems,
                ["paginaActual"] = pagina,
                ["tamanoPagina"] = tamano,
                ["totalRegistros"] = total,
                ["totalPaginas"] = totalPaginas
            };

            if (adicionales != null)
            {
                foreach (KeyValuePair<string, object?> item in adicionales)
                    result[item.Key] =
                        JsonSerializer.SerializeToNode(item.Value);
            }

            return CrearJsonResponse(request, result.ToJsonString());
        }

        private async Task<JsonArray?> LeerArrayAsync(
            string usuarioId,
            string seccion)
        {
            CatalogoOfflineSeccionEntity? entity =
                await database.ObtenerSeccionPaqueteActivoAsync(
                    usuarioId,
                    seccion);

            return entity == null
                ? null
                : JsonNode.Parse(entity.Json) as JsonArray;
        }

        private async Task<string> BuscarNombreRelacionadoAsync(
            string usuarioId,
            string seccion,
            string propiedadId,
            int id,
            string propiedadNombre)
        {
            JsonArray? data = await LeerArrayAsync(usuarioId, seccion);

            JsonObject? item = data?
                .OfType<JsonObject>()
                .FirstOrDefault(x =>
                    ObtenerEntero(x, propiedadId) == id);

            return item == null
                ? string.Empty
                : ObtenerTexto(item, propiedadNombre);
        }

        private async Task<int> BuscarEnteroRelacionadoAsync(
            string usuarioId,
            string seccion,
            string propiedadId,
            int id,
            string propiedadResultado)
        {
            JsonArray? data = await LeerArrayAsync(usuarioId, seccion);

            JsonObject? item = data?
                .OfType<JsonObject>()
                .FirstOrDefault(x =>
                    ObtenerEntero(x, propiedadId) == id);

            return item == null
                ? 0
                : ObtenerEntero(item, propiedadResultado);
        }

        private static bool CoincideBusqueda(
            JsonObject item,
            string buscar,
            IEnumerable<string> propiedades)
        {
            if (string.IsNullOrWhiteSpace(buscar))
                return true;

            return propiedades.Any(propiedad =>
                ObtenerTexto(item, propiedad).Contains(
                    buscar,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        private static bool CoincideCategoriaFuente(
            JsonObject item,
            string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria))
                return true;

            string normalizada = categoria.Trim().ToUpperInvariant();
            bool enmienda =
                ObtenerBooleano(item, "habilitadaEnmiendaCalcarea");
            bool mixta =
                ObtenerBooleano(item, "habilitadaFertilizacionMixta");

            if (normalizada.Contains("ENMIENDA"))
                return enmienda;

            if (normalizada.Contains("MIXTA"))
                return mixta;

            if (normalizada.Contains("BALANCE"))
                return !enmienda && !mixta;

            return true;
        }

        private static string ObtenerNombreOrden(JsonObject item)
        {
            string[] propiedades =
            {
                "nombrePais",
                "nombreDepartamento",
                "nombreMunicipio",
                "nombreTipoCultivo",
                "nombreTipoAnalisisSuelo",
                "nombreElementoQuimico",
                "nombreNutriente"
            };

            foreach (string propiedad in propiedades)
            {
                string value = ObtenerTexto(item, propiedad);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private static void OrdenarPorNombre(JsonArray array)
        {
            List<JsonObject> ordered = array
                .OfType<JsonObject>()
                .OrderBy(
                    ObtenerNombreOrden,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            array.Clear();

            foreach (JsonObject item in ordered)
                array.Add(item.DeepClone());
        }

        private static JsonNode? BuscarPropiedad(
            JsonObject obj,
            string nombre)
        {
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                if (string.Equals(
                        property.Key,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static string ObtenerTexto(
            JsonObject obj,
            string nombre)
        {
            JsonNode? value = BuscarPropiedad(obj, nombre);

            return value == null
                ? string.Empty
                : value.ToJsonString().Trim('"');
        }

        private static int ObtenerEntero(
            JsonObject obj,
            string nombre)
        {
            JsonNode? value = BuscarPropiedad(obj, nombre);

            return value != null &&
                   int.TryParse(
                       value.ToJsonString().Trim('"'),
                       out int result)
                ? result
                : 0;
        }

        private static bool ObtenerBooleano(
            JsonObject obj,
            string nombre)
        {
            JsonNode? value = BuscarPropiedad(obj, nombre);

            return value != null &&
                   bool.TryParse(
                       value.ToJsonString().Trim('"'),
                       out bool result) &&
                   result;
        }

        private static bool TieneBypass(HttpRequestMessage request) =>
            request.Headers.TryGetValues(
                HeaderBypass,
                out IEnumerable<string>? values) &&
            values.Any(value =>
                string.Equals(
                    value,
                    "true",
                    StringComparison.OrdinalIgnoreCase));

        private static bool EsRutaCatalogo(HttpRequestMessage request)
        {
            string path = ObtenerPath(request).ToLowerInvariant();

            return path.StartsWith("/api/terreno") ||
                   path.StartsWith("/api/pais") ||
                   path.StartsWith("/api/departamento") ||
                   path.StartsWith("/api/municipio") ||
                   path.StartsWith("/api/elemento-quimico") ||
                   path.StartsWith("/api/configuracion/tipos-cultivo") ||
                   path.StartsWith(
                       "/api/configuracion/tipos-analisis-suelo") ||
                   path.StartsWith("/api/fuente-nutriente") ||
                   path.StartsWith("/api/configuracion-unidades") ||
                   path.StartsWith(
                       "/api/configuracion/rangos-nutrientes");
        }

        private static string ObtenerPath(HttpRequestMessage request)
        {
            if (request.RequestUri == null)
                return string.Empty;

            if (request.RequestUri.IsAbsoluteUri)
                return request.RequestUri.AbsolutePath;

            string raw = request.RequestUri.OriginalString;
            int question = raw.IndexOf('?');

            if (question >= 0)
                raw = raw[..question];

            return "/" + raw.TrimStart('/');
        }

        private static Dictionary<string, string> LeerQuery(
            HttpRequestMessage request)
        {
            string original =
                request.RequestUri?.OriginalString ?? string.Empty;

            string query = request.RequestUri?.IsAbsoluteUri == true
                ? request.RequestUri.Query
                : original.Contains('?')
                    ? original[(original.IndexOf('?') + 1)..]
                    : string.Empty;

            query = query.TrimStart('?');

            var result = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string pair in query.Split(
                         '&',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);

                string key = Uri.UnescapeDataString(parts[0]);

                string value = parts.Length > 1
                    ? Uri.UnescapeDataString(
                        parts[1].Replace("+", " "))
                    : string.Empty;

                result[key] = value;
            }

            return result;
        }

        private static string ObtenerQuery(
            IReadOnlyDictionary<string, string> query,
            string key) =>
            query.TryGetValue(key, out string? value)
                ? value
                : string.Empty;

        private static int LeerEntero(
            IReadOnlyDictionary<string, string> query,
            string key,
            int predeterminado = 0) =>
            query.TryGetValue(key, out string? value) &&
            int.TryParse(value, out int number)
                ? number
                : predeterminado;

        private static int LeerUltimoId(string path)
        {
            string value = path
                .TrimEnd('/')
                .Split('/')
                .LastOrDefault() ?? string.Empty;

            return int.TryParse(value, out int id) ? id : 0;
        }

        private static HttpResponseMessage CrearJsonResponse(
            HttpRequestMessage request,
            string json)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                "X-Datos-Origen",
                "sincronizados-anteriormente");

            return response;
        }
    }
}
