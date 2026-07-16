using System.Net.Http.Headers;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Proporciona una única instancia de HttpClient para los servicios de la aplicación.
    /// Es reutilizado por todos los servicios que consumen la API.
    /// </summary>
    public static class ApiClientService
    {
        private static readonly Lazy<HttpClient> lazyClient =
            new Lazy<HttpClient>(CrearCliente);

        public static HttpClient Client => lazyClient.Value;

        private static HttpClient CrearCliente()
        {
            var urlApiService = new UrlApiService();

            if (!Uri.TryCreate(
                    urlApiService.BaseUrlApi,
                    UriKind.Absolute,
                    out Uri? baseAddress))
            {
                throw new InvalidOperationException(
                    $"La URL configurada para la API no es válida: {urlApiService.BaseUrlApi}");
            }

            var client = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromMinutes(2)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
