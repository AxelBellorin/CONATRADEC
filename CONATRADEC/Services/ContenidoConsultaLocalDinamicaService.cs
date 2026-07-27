using CONATRADEC.Models;
using SQLite;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Resuelve búsquedas y paginación variables usando las páginas completas
    /// ya descargadas. No realiza solicitudes HTTP.
    /// </summary>
    public static class ContenidoConsultaLocalDinamicaService
    {
        private static readonly SemaphoreSlim initLock =
            new(1, 1);

        private static SQLiteAsyncConnection? database;

        public static async Task<string?>
            IntentarCrearRespuestaAsync(
                HttpRequestMessage request,
                string usuarioId,
                string modulo,
                CancellationToken cancellationToken)
        {
            string path =
                ObtenerPath(request);

            if (modulo == "noticias" &&
                string.Equals(
                    path,
                    "/api/publicacion/feed",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await CrearPaginaAsync(
                    request,
                    usuarioId,
                    modulo,
                    idProperty:
                        "publicacionId",
                    cancellationToken);
            }

            if (modulo == "album" &&
                (
                    string.Equals(
                        path,
                        "/api/album-botanico/galeria-paginada",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        path,
                        "/api/album-botanico/galeria",
                        StringComparison.OrdinalIgnoreCase)
                ))
            {
                return await CrearPaginaAsync(
                    request,
                    usuarioId,
                    modulo,
                    idProperty:
                        "albumBotanicoCafeId",
                    cancellationToken);
            }

            return null;
        }

        private static async Task<string?>
            CrearPaginaAsync(
                HttpRequestMessage request,
                string usuarioId,
                string modulo,
                string idProperty,
                CancellationToken cancellationToken)
        {
            await InicializarAsync();

            ContenidoModuloEstadoEntity? estado =
                await ContenidoLocalDatabaseService
                    .Instance
                    .ObtenerEstadoAsync(
                        $"{usuarioId}|{modulo}");

            if (estado == null ||
                string.IsNullOrWhiteSpace(
                    estado.Version))
            {
                return null;
            }

            Dictionary<string, string> target =
                ParseQuery(
                    ObtenerPathYQuery(request));

            int pagina =
                GetInt(target, "pagina", 1);

            int tamano =
                Math.Clamp(
                    GetInt(
                        target,
                        "tamanoPagina",
                        12),
                    1,
                    50);

            string buscar =
                GetValue(target, "buscar");

            List<ContenidoRespuestaCacheEntity> responses =
                await database!
                    .Table<ContenidoRespuestaCacheEntity>()
                    .Where(item =>
                        item.UsuarioId == usuarioId &&
                        item.Modulo == modulo &&
                        item.Version ==
                            estado.Version)
                    .ToListAsync();

            string targetPath =
                ObtenerPath(request);

            List<ContenidoRespuestaCacheEntity> candidates =
                responses
                    .Where(item =>
                        string.Equals(
                            ObtenerPath(item.Ruta),
                            targetPath,
                            StringComparison.OrdinalIgnoreCase))
                    .Where(item =>
                        CoincidenFiltros(
                            target,
                            ParseQuery(item.Ruta)))
                    .ToList();

            /*
             * Se prefiere el mismo tamaño de página para conservar el orden
             * exacto descargado por esa plataforma.
             */
            List<ContenidoRespuestaCacheEntity> sameSize =
                candidates
                    .Where(item =>
                        GetInt(
                            ParseQuery(item.Ruta),
                            "tamanoPagina",
                            tamano) ==
                        tamano)
                    .ToList();

            if (sameSize.Count > 0)
                candidates = sameSize;

            candidates = candidates
                .OrderBy(item =>
                    GetInt(
                        ParseQuery(item.Ruta),
                        "pagina",
                        1))
                .ThenBy(item => item.Ruta)
                .ToList();

            if (candidates.Count == 0)
                return null;

            var unique =
                new Dictionary<string, JsonNode>(
                    StringComparer.OrdinalIgnoreCase);

            JsonObject? templateRoot = null;
            JsonObject? templateData = null;

            foreach (ContenidoRespuestaCacheEntity response
                     in candidates)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                JsonObject? root =
                    JsonNode.Parse(response.Json)
                        as JsonObject;

                JsonObject? data =
                    GetObject(root, "data");

                JsonArray? items =
                    GetArray(data, "items");

                if (root == null ||
                    data == null ||
                    items == null)
                {
                    continue;
                }

                templateRoot ??=
                    root.DeepClone()
                        .AsObject();

                templateData ??=
                    data.DeepClone()
                        .AsObject();

                foreach (JsonNode? item
                         in items)
                {
                    if (item == null)
                        continue;

                    string serialized =
                        item.ToJsonString();

                    if (!string.IsNullOrWhiteSpace(
                            buscar) &&
                        !ContieneTexto(
                            ConstruirTextoBusqueda(
                                item),
                            buscar))
                    {
                        continue;
                    }

                    string id =
                        GetNodeValue(
                            item as JsonObject,
                            idProperty);

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        id = Convert.ToHexString(
                            System.Security.Cryptography
                                .SHA256.HashData(
                                    Encoding.UTF8.GetBytes(
                                        serialized)));
                    }

                    unique[id] =
                        item.DeepClone();
                }
            }

            if (templateRoot == null ||
                templateData == null)
            {
                return null;
            }

            List<JsonNode> all =
                unique.Values
                    .ToList();

            int total =
                all.Count;

            int totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        total /
                        (double)tamano));

            int paginaNormalizada =
                Math.Clamp(
                    pagina,
                    1,
                    totalPaginas);

            JsonArray pageItems =
                new(
                    all
                        .Skip(
                            (paginaNormalizada - 1) *
                            tamano)
                        .Take(tamano)
                        .Select(item =>
                            item.DeepClone())
                        .ToArray());

            SetProperty(
                templateData,
                "items",
                pageItems);

            SetProperty(
                templateData,
                "pagina",
                paginaNormalizada);

            SetProperty(
                templateData,
                "tamanoPagina",
                tamano);

            SetProperty(
                templateData,
                "totalRegistros",
                total);

            SetProperty(
                templateData,
                "totalPaginas",
                totalPaginas);

            SetProperty(
                templateData,
                "tieneMas",
                paginaNormalizada <
                totalPaginas);

            SetProperty(
                templateRoot,
                "success",
                true);

            SetProperty(
                templateRoot,
                "message",
                "Datos consultados desde la copia local.");

            SetProperty(
                templateRoot,
                "data",
                templateData);

            return templateRoot.ToJsonString();
        }

        private static bool CoincidenFiltros(
            IReadOnlyDictionary<string, string> target,
            IReadOnlyDictionary<string, string> source)
        {
            string[] keys =
            {
                "categoriaId",
                "soloDestacadas",
                "soloEventos",
                "incluirInactivos"
            };

            foreach (string key in keys)
            {
                string targetValue =
                    NormalizarValorFiltro(
                        key,
                        GetValue(target, key));

                string sourceValue =
                    NormalizarValorFiltro(
                        key,
                        GetValue(source, key));

                if (!string.Equals(
                        targetValue,
                        sourceValue,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            /*
             * Las respuestas base se descargan sin búsqueda; el texto se
             * filtra localmente sobre todos los elementos recuperados.
             */
            return string.IsNullOrWhiteSpace(
                GetValue(source, "buscar"));
        }

        private static string NormalizarValorFiltro(
            string key,
            string? value)
        {
            string normalized =
                value?.Trim() ??
                string.Empty;

            if (key is
                "soloDestacadas" or
                "soloEventos" or
                "incluirInactivos")
            {
                return bool.TryParse(
                    normalized,
                    out bool result) &&
                    result
                        ? "true"
                        : "false";
            }

            return normalized;
        }

        private static string ConstruirTextoBusqueda(
            JsonNode node)
        {
            var builder =
                new StringBuilder();

            AgregarTextos(
                node,
                builder);

            return builder.ToString();
        }

        private static void AgregarTextos(
            JsonNode? node,
            StringBuilder builder)
        {
            if (node == null)
                return;

            if (node is JsonObject obj)
            {
                foreach (KeyValuePair<
                             string,
                             JsonNode?> property
                         in obj)
                {
                    AgregarTextos(
                        property.Value,
                        builder);
                }

                return;
            }

            if (node is JsonArray array)
            {
                foreach (JsonNode? item
                         in array)
                {
                    AgregarTextos(
                        item,
                        builder);
                }

                return;
            }

            if (node is JsonValue value)
            {
                if (value.TryGetValue<string>(
                        out string? text) &&
                    !string.IsNullOrWhiteSpace(
                        text))
                {
                    builder.Append(text);
                    builder.Append(' ');
                    return;
                }

                builder.Append(value.ToString());
                builder.Append(' ');
            }
        }

        private static bool ContieneTexto(
            string source,
            string term)
        {
            string normalizedSource =
                QuitarDiacriticos(source);

            string normalizedTerm =
                QuitarDiacriticos(term);

            return normalizedSource.Contains(
                normalizedTerm,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string QuitarDiacriticos(
            string value)
        {
            string normalized =
                value.Normalize(
                    NormalizationForm.FormD);

            var builder =
                new StringBuilder(
                    normalized.Length);

            foreach (char character
                     in normalized)
            {
                UnicodeCategory category =
                    CharUnicodeInfo
                        .GetUnicodeCategory(character);

                if (category !=
                    UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder
                .ToString()
                .Normalize(
                    NormalizationForm.FormC);
        }

        private static JsonObject? GetObject(
            JsonObject? source,
            string name) =>
            GetNode(source, name)
                as JsonObject;

        private static JsonArray? GetArray(
            JsonObject? source,
            string name) =>
            GetNode(source, name)
                as JsonArray;

        private static JsonNode? GetNode(
            JsonObject? source,
            string name)
        {
            if (source == null)
                return null;

            foreach (KeyValuePair<
                         string,
                         JsonNode?> property
                     in source)
            {
                if (string.Equals(
                        property.Key,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static string GetNodeValue(
            JsonObject? source,
            string name)
        {
            JsonNode? value =
                GetNode(source, name);

            return value?.ToString() ??
                string.Empty;
        }

        private static void SetProperty(
            JsonObject source,
            string name,
            JsonNode? value)
        {
            string? existing =
                source
                    .Select(item => item.Key)
                    .FirstOrDefault(key =>
                        string.Equals(
                            key,
                            name,
                            StringComparison.OrdinalIgnoreCase));

            source[existing ?? name] =
                value;
        }

        private static Dictionary<string, string>
            ParseQuery(
                string pathAndQuery)
        {
            var result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            int queryIndex =
                pathAndQuery.IndexOf('?');

            if (queryIndex < 0 ||
                queryIndex ==
                    pathAndQuery.Length - 1)
            {
                return result;
            }

            string query =
                pathAndQuery[
                    (queryIndex + 1)..];

            foreach (string part
                     in query.Split(
                         '&',
                         StringSplitOptions
                             .RemoveEmptyEntries))
            {
                string[] pair =
                    part.Split(
                        '=',
                        2);

                string key =
                    Uri.UnescapeDataString(
                        pair[0]);

                string value =
                    pair.Length > 1
                        ? Uri.UnescapeDataString(
                            pair[1]
                                .Replace(
                                    "+",
                                    " "))
                        : string.Empty;

                result[key] = value;
            }

            return result;
        }

        private static int GetInt(
            IReadOnlyDictionary<string, string> values,
            string key,
            int fallback) =>
            values.TryGetValue(
                    key,
                    out string? value) &&
                int.TryParse(
                    value,
                    out int result)
                ? result
                : fallback;

        private static string GetValue(
            IReadOnlyDictionary<string, string> values,
            string key) =>
            values.TryGetValue(
                    key,
                    out string? value)
                ? value
                : string.Empty;

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri =
                request.RequestUri;

            if (uri == null)
                return string.Empty;

            return uri.IsAbsoluteUri
                ? uri.AbsolutePath
                : ObtenerPath(
                    uri.OriginalString);
        }

        private static string ObtenerPath(
            string pathAndQuery)
        {
            string value =
                pathAndQuery;

            int query =
                value.IndexOf('?');

            if (query >= 0)
                value = value[..query];

            return "/" +
                value.TrimStart('/');
        }

        private static string ObtenerPathYQuery(
            HttpRequestMessage request)
        {
            Uri? uri =
                request.RequestUri;

            if (uri == null)
                return string.Empty;

            return uri.IsAbsoluteUri
                ? uri.PathAndQuery
                : "/" +
                  uri.OriginalString
                      .TrimStart('/');
        }

        private static async Task InicializarAsync()
        {
            if (database != null)
                return;

            await initLock.WaitAsync();

            try
            {
                if (database != null)
                    return;

                database =
                    new SQLiteAsyncConnection(
                        ContenidoLocalDatabaseService
                            .Instance
                            .DatabasePath,
                        SQLiteOpenFlags.ReadWrite |
                        SQLiteOpenFlags.Create |
                        SQLiteOpenFlags.SharedCache);

                await database.CreateTableAsync<
                    ContenidoRespuestaCacheEntity>();
            }
            finally
            {
                initLock.Release();
            }
        }
    }
}
