using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente del flujo por fotografía. Todas las operaciones masivas envían
    /// IDs individuales y el backend devuelve un resultado por cada evidencia.
    /// </summary>
    public sealed class InspeccionFitosanitariaApiService
    {
        private static readonly Lazy<InspeccionFitosanitariaApiService> lazy =
            new(() => new InspeccionFitosanitariaApiService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public static InspeccionFitosanitariaApiService Instance => lazy.Value;

        private InspeccionFitosanitariaApiService()
        {
            client = ApiClientService.Client;
        }

        public async Task<InspeccionFitosanitariaDetalleV2> CrearAsync(
            IReadOnlyCollection<InspeccionFotoLocal> fotos,
            string? codigoTerreno,
            string? observacion,
            string? nombreInspeccion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codigoTerreno))
            {
                throw new ArgumentException(
                    "Debe seleccionar un terreno antes de crear la inspección.",
                    nameof(codigoTerreno));
            }

            using var contenido = new MultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(nombreInspeccion))
            {
                contenido.Add(
                    new StringContent(nombreInspeccion.Trim(), Encoding.UTF8),
                    "NombreInspeccion");
            }

            contenido.Add(
                new StringContent(codigoTerreno.Trim(), Encoding.UTF8),
                "CodigoTerreno");

            if (!string.IsNullOrWhiteSpace(observacion))
            {
                contenido.Add(
                    new StringContent(observacion.Trim(), Encoding.UTF8),
                    "Observacion");
            }

            foreach (InspeccionFotoLocal foto in fotos)
            {
                var stream = new FileStream(
                    foto.RutaLocal,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                var archivo = new StreamContent(stream);
                archivo.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(foto.TipoContenido)
                        ? "image/jpeg"
                        : foto.TipoContenido);

                contenido.Add(
                    archivo,
                    "Fotos",
                    foto.NombreArchivo);
                contenido.Add(
                    new StringContent(
                        foto.TipoFotografia,
                        Encoding.UTF8),
                    "TiposFotografia");
                contenido.Add(
                    new StringContent(
                        foto.FechaIdentificacionCampo.ToString("yyyy-MM-dd"),
                        Encoding.UTF8),
                    "FechasIdentificacionCampo");
            }

            return await SendAsync<InspeccionFitosanitariaDetalleV2>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/inspecciones-fitosanitarias")
                {
                    Content = contenido
                },
                cancellationToken);
        }

        public async Task<InspeccionFitosanitariaDetalleV2> AgregarFotosAsync(
            int inspeccionId,
            IReadOnlyCollection<InspeccionFotoLocal> fotos,
            CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(inspeccionId));

            if (fotos == null || fotos.Count == 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar al menos una fotografía.",
                    nameof(fotos));
            }

            using var contenido = new MultipartFormDataContent();

            foreach (InspeccionFotoLocal foto in fotos)
            {
                var stream = new FileStream(
                    foto.RutaLocal,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                var archivo = new StreamContent(stream);
                archivo.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(foto.TipoContenido)
                        ? "image/jpeg"
                        : foto.TipoContenido);

                contenido.Add(
                    archivo,
                    "Fotos",
                    foto.NombreArchivo);
                contenido.Add(
                    new StringContent(
                        foto.TipoFotografia,
                        Encoding.UTF8),
                    "TiposFotografia");
                contenido.Add(
                    new StringContent(
                        foto.FechaIdentificacionCampo.ToString("yyyy-MM-dd"),
                        Encoding.UTF8),
                    "FechasIdentificacionCampo");
            }

            return await SendAsync<InspeccionFitosanitariaDetalleV2>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/fotografias")
                {
                    Content = contenido
                },
                cancellationToken);
        }

        public Task<List<InspeccionFitosanitariaListaItemV2>> ObtenerBandejaAsync(
            string modo,
            CancellationToken cancellationToken = default) =>
            GetAsync<List<InspeccionFitosanitariaListaItemV2>>(
                "api/inspecciones-fitosanitarias-flujo/bandeja?modo=" +
                Uri.EscapeDataString(modo ?? "mis"),
                cancellationToken);

        public Task<InspeccionFitosanitariaDetalleV2> ObtenerDetalleAsync(
            int inspeccionId,
            CancellationToken cancellationToken = default) =>
            GetAsync<InspeccionFitosanitariaDetalleV2>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}",
                cancellationToken);

        /// <summary>
        /// Normaliza resultados aparentemente sanos para vincularlos con el
        /// capítulo Plantas sanas. No crea categorías ni fichas: únicamente
        /// registra una coincidencia existente o deja una propuesta pendiente
        /// para el analizador humano.
        /// </summary>
        public async Task NormalizarPlantasSanasAsync(
            int inspeccionId,
            CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0)
                return;

            await PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{inspeccionId}/normalizar-plantas-sanas",
                new { },
                cancellationToken);
        }

        public Task<InspeccionOperacionMasivaV2> ProcesarFotosAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            CancellationToken cancellationToken = default) =>
            PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/procesar-fotografias",
                new { fotografiaIds },
                cancellationToken);

        public Task<InspeccionOperacionMasivaV2> SolicitarRevisionIAAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            string retroalimentacion,
            string? diagnosticoPropuesto,
            CancellationToken cancellationToken = default)
        {
            ValidarUnaFotografia(
                fotografiaIds,
                "Cada solicitud de revisión IA debe corresponder a una sola fotografía.");

            return PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/solicitar-revision-ia",
                new
                {
                    fotografiaIds,
                    retroalimentacion = retroalimentacion.Trim(),
                    diagnosticoPropuesto = diagnosticoPropuesto?.Trim()
                },
                cancellationToken);
        }

        public Task<InspeccionOperacionMasivaV2> EnviarAnalizadorAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            CancellationToken cancellationToken = default) =>
            PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/enviar-analizador",
                new { fotografiaIds },
                cancellationToken);

        public async Task<InspeccionFitosanitariaDetalleV2> CerrarInspeccionAsync(
            int inspeccionId,
            CancellationToken cancellationToken = default)
        {
            await PostAsync<JsonElement>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/cerrar-definitivo",
                new { },
                cancellationToken);

            return await ObtenerDetalleAsync(inspeccionId, cancellationToken);
        }

        public Task<InspeccionOperacionMasivaV2> DescartarFotosAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            ValidarUnaFotografia(
                fotografiaIds,
                "Cada descarte debe registrar el motivo de una sola fotografía.");

            return PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/descartar-fotografias",
                new
                {
                    fotografiaIds,
                    motivo = motivo.Trim()
                },
                cancellationToken);
        }

        public Task<InspeccionOperacionMasivaV2> GuardarAnalisisHumanoAsync(
            int inspeccionId,
            IReadOnlyCollection<InspeccionFotoAnalisisHumanoRequestV2> fotografias,
            bool enviarAprobacion,
            CancellationToken cancellationToken = default)
        {
            ValidarUnaFotografia(
                fotografias,
                "Cada clasificación humana debe guardarse para una sola fotografía.");

            return PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias-flujo/{inspeccionId}/analisis-humano-individual",
                new
                {
                    fotografias,
                    enviarAprobacion
                },
                cancellationToken);
        }

        public Task<InspeccionOperacionMasivaV2> RegistrarAprobacionesAsync(
            int inspeccionId,
            IReadOnlyCollection<InspeccionFotoAprobacionRequestV2> fotografias,
            CancellationToken cancellationToken = default)
        {
            ValidarUnaFotografia(
                fotografias,
                "Cada decisión del aprobador debe corresponder a una sola fotografía.");

            return PostAsync<InspeccionOperacionMasivaV2>(
                $"api/inspecciones-fitosanitarias-flujo/{inspeccionId}/aprobacion-individual",
                new { fotografias },
                cancellationToken);
        }

        public Task<JsonElement> ResolverClasificacionExistenteAsync(
            int inspeccionId,
            int fotografiaId,
            int albumBotanicoCafeId,
            CancellationToken cancellationToken = default) =>
            PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{inspeccionId}/imagen/{fotografiaId}/usar-existente",
                new { albumBotanicoCafeId },
                cancellationToken);

        public Task<JsonElement> ProponerClasificacionAlbumAsync(
            int inspeccionId,
            int fotografiaId,
            int categoriaAlbumBotanicoId,
            string titulo,
            string? nombreCientifico,
            string motivo,
            CancellationToken cancellationToken = default) =>
            PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{inspeccionId}/imagen/{fotografiaId}/proponer-nueva",
                new
                {
                    categoriaAlbumBotanicoId,
                    titulo = titulo.Trim(),
                    nombreCientifico = nombreCientifico?.Trim(),
                    motivo = motivo.Trim()
                },
                cancellationToken);

        public Task<JsonElement> CrearClasificacionAlbumAsync(
            int inspeccionId,
            int fotografiaId,
            int categoriaAlbumBotanicoId,
            string titulo,
            string? nombreCientifico,
            string descripcion,
            string? sintomas,
            CancellationToken cancellationToken = default) =>
            PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{inspeccionId}/imagen/{fotografiaId}/crear-clasificacion",
                new
                {
                    categoriaAlbumBotanicoId,
                    titulo = titulo.Trim(),
                    nombreCientifico = nombreCientifico?.Trim(),
                    descripcion = descripcion.Trim(),
                    sintomas = sintomas?.Trim()
                },
                cancellationToken);

        public Task<JsonElement> PublicarAlbumAsync(
            int inspeccionId,
            int fotografiaId,
            int categoriaAlbumBotanicoId,
            int albumBotanicoCafeId,
            string? descripcion,
            bool esPortada,
            int orden,
            CancellationToken cancellationToken = default) =>
            PostAsync<JsonElement>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}/fotografias/{fotografiaId}/publicar-album",
                new
                {
                    categoriaAlbumBotanicoId,
                    albumBotanicoCafeId,
                    descripcion = descripcion?.Trim() ?? string.Empty,
                    esPortada,
                    orden
                },
                cancellationToken);

        public Task<List<InspeccionAlbumCategoriaV2>>
            ObtenerCatalogoAlbumAsync(
                CancellationToken cancellationToken = default) =>
            GetAsync<List<InspeccionAlbumCategoriaV2>>(
                "api/inspecciones-fitosanitarias/catalogo-album",
                cancellationToken);

        public Task<ProveedorIAConfiguracionV2> ObtenerProveedorIAAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<ProveedorIAConfiguracionV2>(
                "api/inspecciones-fitosanitarias/proveedor-ia",
                cancellationToken);

        public Task<ProveedorIAConfiguracionV2> GuardarProveedorIAAsync(
            ProveedorIAConfiguracionV2 configuracion,
            CancellationToken cancellationToken = default) =>
            PutAsync<ProveedorIAConfiguracionV2>(
                "api/inspecciones-fitosanitarias/proveedor-ia",
                CrearPayloadProveedor(configuracion),
                cancellationToken);

        public Task<ProveedorIAPruebaV2> ProbarProveedorIAAsync(
            ProveedorIAConfiguracionV2 configuracion,
            CancellationToken cancellationToken = default) =>
            PostAsync<ProveedorIAPruebaV2>(
                "api/inspecciones-fitosanitarias/proveedor-ia/probar",
                CrearPayloadProveedor(configuracion),
                cancellationToken);

        private static void ValidarUnaFotografia<T>(
            IReadOnlyCollection<T>? elementos,
            string mensaje)
        {
            if (elementos == null || elementos.Count != 1)
                throw new ArgumentException(mensaje, nameof(elementos));
        }

        private static object CrearPayloadProveedor(
            ProveedorIAConfiguracionV2 configuracion) =>
            new
            {
                configuracion.Proveedor,
                configuracion.Protocolo,
                configuracion.BaseUrl,
                configuracion.Endpoint,
                apiKey = string.IsNullOrWhiteSpace(configuracion.ApiKey)
                    ? null
                    : configuracion.ApiKey.Trim(),
                configuracion.ModeloPrincipal,
                configuracion.ModeloRespaldo,
                configuracion.TimeoutSegundos,
                configuracion.Activo
            };

        private Task<T> GetAsync<T>(
            string ruta,
            CancellationToken cancellationToken) =>
            SendAsync<T>(
                new HttpRequestMessage(HttpMethod.Get, ruta),
                cancellationToken);

        private Task<T> PostAsync<T>(
            string ruta,
            object payload,
            CancellationToken cancellationToken) =>
            SendAsync<T>(
                new HttpRequestMessage(HttpMethod.Post, ruta)
                {
                    Content = JsonContent.Create(
                        payload,
                        options: JsonOptions)
                },
                cancellationToken);

        private Task<T> PutAsync<T>(
            string ruta,
            object payload,
            CancellationToken cancellationToken) =>
            SendAsync<T>(
                new HttpRequestMessage(HttpMethod.Put, ruta)
                {
                    Content = JsonContent.Create(
                        payload,
                        options: JsonOptions)
                },
                cancellationToken);

        private async Task<T> SendAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                ApiEnvelopeV2<T>? envelope = null;

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<ApiEnvelopeV2<T>>(
                            contenido,
                            JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje = envelope?.Message;

                    if (string.IsNullOrWhiteSpace(mensaje))
                        mensaje = ExtraerMensaje(contenido);

                    throw new InspeccionFitosanitariaApiException(
                        response.StatusCode,
                        string.IsNullOrWhiteSpace(mensaje)
                            ? "El servidor rechazó la operación."
                            : mensaje);
                }

                if (envelope is not null)
                {
                    object? data = envelope.Data;

                    if (data is not null)
                        return (T)data;
                }

                if (typeof(T) == typeof(JsonElement) &&
                    !string.IsNullOrWhiteSpace(contenido))
                {
                    using JsonDocument document = JsonDocument.Parse(contenido);
                    return (T)(object)document.RootElement.Clone();
                }

                throw new InspeccionFitosanitariaApiException(
                    HttpStatusCode.BadGateway,
                    "El servidor devolvió una respuesta incompleta.");
            }
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return string.Empty;

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);

                if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    return message.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty(
                        "title",
                        out JsonElement title))
                {
                    return title.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }
    }

    public sealed class InspeccionFitosanitariaApiException : Exception
    {
        public InspeccionFitosanitariaApiException(
            HttpStatusCode statusCode,
            string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }

        public bool EsSesionInvalidada =>
            StatusCode == HttpStatusCode.Unauthorized;
    }
}
