namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza la dirección base utilizada por los servicios de la API.
    /// </summary>
    public sealed class UrlApiService
    {
        /*
         * SERVIDOR ACTUAL DE DESARROLLO
         *
         * Se mantiene HTTP porque el alojamiento de desarrollo todavía no
         * dispone de certificado SSL. Android debe permitir tráfico HTTP
         * mientras se utilice este servidor.
         */ 
        private const string DevelopmentBaseUrl =
            "http://conatradecnic.runasp.net/";

        //private const string DevelopmentBaseUrl =
        //    "https://localhost:7176/";

        /*
         * PRODUCCIÓN
         *
         * Cuando se disponga del dominio definitivo con certificado SSL,
         * se reemplaza DevelopmentBaseUrl por la URL HTTPS de producción
         * y se desactiva el tráfico HTTP en Android.
         *
         * Ejemplo:
         * private const string ProductionBaseUrl =
         *     "https://api.tudominio.com/";
         */

        public string BaseUrlApi => DevelopmentBaseUrl;
    }
}