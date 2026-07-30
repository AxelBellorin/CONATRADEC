using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Identifica las consultas que no pudieron ser resueltas por SQLite o por
    /// alguno de los manejadores locales durante una sesión sin conexión.
    ///
    /// Estas respuestas no significan que el servidor falló, porque en modo
    /// offline la solicitud nunca llega realmente a la red.
    /// </summary>
    public static class OfflineReadResponseService
    {
        public const string HeaderOrigen =
            "X-CONATRADEC-Origen";

        public const string OrigenSinDatos =
            "LOCAL-SIN-DATOS";

        public const string MensajeSinDatosLocales =
            "Esta información adicional no está disponible en la copia local. " +
            "La pantalla continuará utilizando los datos descargados.";

        /// <summary>
        /// Indica si la respuesta pertenece a una lectura GET no disponible en
        /// la copia local del dispositivo.
        /// </summary>
        public static bool EsLecturaSinDatosLocales(
            HttpResponseMessage? response)
        {
            if (response == null ||
                !ModoSesionService.EsOffline ||
                response.RequestMessage?.Method != HttpMethod.Get ||
                response.StatusCode != HttpStatusCode.ServiceUnavailable)
            {
                return false;
            }

            return response.Headers.TryGetValues(
                       HeaderOrigen,
                       out IEnumerable<string>? valores) &&
                   valores.Any(
                       valor =>
                           string.Equals(
                               valor,
                               OrigenSinDatos,
                               StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Evita mostrar una notificación roja cuando una consulta auxiliar no
        /// forma parte del paquete descargado.
        ///
        /// Solamente se omite el mensaje interno creado expresamente por la
        /// barrera offline. Los errores reales del servidor, SQLite,
        /// validaciones y operaciones de escritura continúan mostrándose.
        /// </summary>
        public static bool DebeOmitirNotificacionError(
            string? mensaje)
        {
            if (!ModoSesionService.EsOffline ||
                string.IsNullOrWhiteSpace(mensaje))
            {
                return false;
            }

            return string.Equals(
                Normalizar(mensaje),
                Normalizar(MensajeSinDatosLocales),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalizar(
            string valor) =>
            string.Join(
                " ",
                valor.Split(
                    new[] { ' ', '\r', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .TrimEnd('.');
    }
}
