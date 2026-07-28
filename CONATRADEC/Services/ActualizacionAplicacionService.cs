using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consulta, descarga y valida las versiones publicadas en el portal.
    /// Utiliza un cliente independiente para que una descarga grande no pase
    /// por los manejadores de caché y trabajo sin conexión de la aplicación.
    /// </summary>
    public sealed class ActualizacionAplicacionService
    {
        private const string PreferenciaCanal = "Actualizaciones.Canal";

        private static string CanalPredeterminado
        {
            get
            {
#if DEBUG
                return "PRUEBAS";
#else
                return "PRODUCCION";
#endif
            }
        }

        private static readonly Lazy<ActualizacionAplicacionService>
            instancia = new(() => new ActualizacionAplicacionService());

        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web);

        public static ActualizacionAplicacionService Instance =>
            instancia.Value;

        private ActualizacionAplicacionService()
        {
            string baseUrl = new UrlApiService().BaseUrlApi;

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException(
                    "La URL configurada para la API no es válida.");
            }

            httpClient = new HttpClient
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromMinutes(30)
            };
        }

        public bool PlataformaCompatible =>
            ObtenerPlataforma() is not null;

        public async Task<ActualizacionDisponible?>
            ComprobarActualizacionAsync(
                CancellationToken cancellationToken = default)
        {
            string? plataforma = ObtenerPlataforma();
            if (plataforma is null)
                return null;

            long versionCodigo = ObtenerVersionCodigoInstalada();
            string canal = Preferences.Get(
                PreferenciaCanal,
                CanalPredeterminado);

            string ruta =
                "api/actualizaciones/comprobar" +
                $"?plataforma={Uri.EscapeDataString(plataforma)}" +
                $"&versionCodigo={versionCodigo}" +
                $"&canal={Uri.EscapeDataString(canal)}";

            using HttpResponseMessage response = await httpClient.GetAsync(
                ruta,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    ExtraerMensaje(contenido),
                    null,
                    response.StatusCode);
            }

            RespuestaComprobacionActualizacion? resultado =
                JsonSerializer.Deserialize<
                    RespuestaComprobacionActualizacion>(
                        contenido,
                        jsonOptions);

            return resultado?.ActualizacionDisponible == true
                ? resultado.Data
                : null;
        }

        public async Task<string> DescargarAsync(
            ActualizacionDisponible actualizacion,
            IProgress<double>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actualizacion);

            Uri url = ResolverUrl(actualizacion.UrlDescarga);

            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                throw new HttpRequestException(
                    ExtraerMensaje(error),
                    null,
                    response.StatusCode);
            }

            long? total = response.Content.Headers.ContentLength;
            string nombreSeguro = ObtenerNombreSeguro(
                actualizacion.NombreArchivo,
                actualizacion.Plataforma);

            string carpeta = Path.Combine(
                FileSystem.CacheDirectory,
                "actualizaciones");

            Directory.CreateDirectory(carpeta);

            string rutaTemporal = Path.Combine(
                carpeta,
                $"{Guid.NewGuid():N}.tmp");

            string rutaFinal = Path.Combine(
                carpeta,
                nombreSeguro);

            try
            {
                await using Stream origen = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                await using FileStream destino = new(
                    rutaTemporal,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024 * 1024,
                    useAsync: true);

                byte[] buffer = new byte[1024 * 1024];
                long descargados = 0;

                while (true)
                {
                    int leidos = await origen.ReadAsync(
                        buffer.AsMemory(0, buffer.Length),
                        cancellationToken);

                    if (leidos == 0)
                        break;

                    await destino.WriteAsync(
                        buffer.AsMemory(0, leidos),
                        cancellationToken);

                    descargados += leidos;

                    if (total.HasValue && total.Value > 0)
                    {
                        progreso?.Report(
                            Math.Clamp(
                                descargados * 100d / total.Value,
                                0,
                                100));
                    }
                }

                await destino.FlushAsync(cancellationToken);

                if (actualizacion.TamanoBytes > 0 &&
                    descargados != actualizacion.TamanoBytes)
                {
                    throw new InvalidDataException(
                        "La descarga no coincide con el tamaño publicado.");
                }

                string hash = await CalcularSha256Async(
                    rutaTemporal,
                    cancellationToken);

                if (!string.Equals(
                        hash,
                        actualizacion.HashSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "El archivo descargado no superó la validación de seguridad SHA-256.");
                }

                File.Move(rutaTemporal, rutaFinal, overwrite: true);
                progreso?.Report(100);

                return rutaFinal;
            }
            catch
            {
                EliminarSeguro(rutaTemporal);
                throw;
            }
        }

        public static long ObtenerVersionCodigoInstalada()
        {
            string valor = AppInfo.Current.BuildString ?? string.Empty;
            return long.TryParse(valor, out long build)
                ? build
                : 0;
        }

        private static string? ObtenerPlataforma()
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                return "ANDROID";

            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
                return "WINDOWS";

            return null;
        }

        private Uri ResolverUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluta))
                return absoluta;

            if (httpClient.BaseAddress is null)
            {
                throw new InvalidOperationException(
                    "No se encontró la dirección base de la API.");
            }

            return new Uri(httpClient.BaseAddress, url.TrimStart('/'));
        }

        private static string ObtenerNombreSeguro(
            string? nombre,
            string plataforma)
        {
            string extensionPredeterminada =
                plataforma.Equals(
                    "ANDROID",
                    StringComparison.OrdinalIgnoreCase)
                    ? ".apk"
                    : ".msix";

            string valor = Path.GetFileName(nombre ?? string.Empty);

            if (string.IsNullOrWhiteSpace(valor))
                valor = $"ConatraCafeSoil{extensionPredeterminada}";

            foreach (char invalido in Path.GetInvalidFileNameChars())
                valor = valor.Replace(invalido, '_');

            return valor;
        }

        private static async Task<string> CalcularSha256Async(
            string ruta,
            CancellationToken cancellationToken)
        {
            await using FileStream stream = new(
                ruta,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true);

            byte[] hash = await SHA256.HashDataAsync(
                stream,
                cancellationToken);

            return Convert.ToHexString(hash);
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "La API no devolvió detalles del error.";

            try
            {
                using JsonDocument documento = JsonDocument.Parse(contenido);
                JsonElement raiz = documento.RootElement;

                foreach (string propiedad in new[]
                         {
                             "message", "mensaje", "title", "error"
                         })
                {
                    if (raiz.TryGetProperty(
                            propiedad,
                            out JsonElement valor) &&
                        valor.ValueKind == JsonValueKind.String)
                    {
                        return valor.GetString() ?? contenido;
                    }
                }
            }
            catch (JsonException)
            {
                // La API también puede devolver texto plano.
            }

            return contenido.Trim().Trim('"');
        }

        private static void EliminarSeguro(string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch
            {
                // El sistema limpiará la carpeta temporal posteriormente.
            }
        }
    }
}
