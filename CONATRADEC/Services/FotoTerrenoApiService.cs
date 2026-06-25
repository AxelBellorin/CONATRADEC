using CONATRADEC.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    public class FotoTerrenoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public FotoTerrenoApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<List<FotoTerrenoResponse>> GetFotosPorTerrenoAsync(int terrenoId)
        {
            try
            {
                var response = await httpClient.GetAsync($"api/fotoTerreno/por-terreno/{terrenoId}");

                if (!response.IsSuccessStatusCode)
                    return new List<FotoTerrenoResponse>();

                var fotos = await response.Content.ReadFromJsonAsync<List<FotoTerrenoResponse>>();

                return fotos ?? new List<FotoTerrenoResponse>();
            }
            catch
            {
                return new List<FotoTerrenoResponse>();
            }
        }

        public async Task<bool> SubirFotosAsync(int terrenoId, IEnumerable<FotoTerrenoItem> fotos)
        {
            try
            {
                var fotosNuevas = fotos
                    .Where(f => f.EsNueva &&
                                !string.IsNullOrWhiteSpace(f.LocalPath) &&
                                File.Exists(f.LocalPath))
                    .ToList();

                if (!fotosNuevas.Any())
                    return true;

                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(terrenoId.ToString()), "terrenoId");

                foreach (var foto in fotosNuevas)
                {
                    string nombreArchivo = !string.IsNullOrWhiteSpace(foto.NombreArchivo)
                        ? foto.NombreArchivo
                        : Path.GetFileName(foto.LocalPath);

                    var fileStream = File.OpenRead(foto.LocalPath!);
                    var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType =
                        new MediaTypeHeaderValue(ObtenerContentType(nombreArchivo));

                    form.Add(streamContent, "fotos", nombreArchivo);
                }

                var response = await httpClient.PostAsync("api/fotoTerreno/subir", form);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EditarFotoAsync(int fotoTerrenoId, string localPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                    return false;

                using var form = new MultipartFormDataContent();

                string nombreArchivo = Path.GetFileName(localPath);

                var fileStream = File.OpenRead(localPath);
                var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(ObtenerContentType(nombreArchivo));

                form.Add(streamContent, "foto", nombreArchivo);

                var response = await httpClient.PutAsync($"api/fotoTerreno/editar/{fotoTerrenoId}", form);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarFotoAsync(int fotoTerrenoId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/fotoTerreno/eliminar/{fotoTerrenoId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public string ConstruirUrlCompleta(string? urlFotoTerreno)
        {
            if (string.IsNullOrWhiteSpace(urlFotoTerreno))
                return string.Empty;

            if (urlFotoTerreno.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return urlFotoTerreno;

            return $"{urlApiService.BaseUrlApi.TrimEnd('/')}/{urlFotoTerreno.TrimStart('/')}";
        }

        private string ObtenerContentType(string nombreArchivo)
        {
            string extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}