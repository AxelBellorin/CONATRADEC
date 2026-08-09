namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza la dirección base utilizada por todos los servicios de la API.
    /// CONATRADEC consume únicamente el endpoint HTTPS publicado en la nube.
    /// </summary>
    public sealed class UrlApiService
    {
        private const string ApiBaseUrl =
            "https://conatradecnic.runasp.net/";

        public string BaseUrlApi => ApiBaseUrl;
    }
}
