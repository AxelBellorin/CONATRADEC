using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Corrige el estado persistido de una descarga completa cuando la
    /// aplicación se cerró antes de que el proceso pudiera finalizar.
    ///
    /// Una tarea en memoria no puede continuar después de reiniciar la app.
    /// Por eso un estado guardado como SINCRONIZANDO siempre debe tratarse
    /// como una descarga interrumpida al crear una sesión nueva de la app.
    /// </summary>
    public static class SincronizacionOfflineEstadoRecuperacionService
    {
        private const string EstadoClavePrefijo =
            "offline_global_manual_estado_";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        /// <summary>
        /// Devuelve true cuando encontró y corrigió una descarga interrumpida.
        /// </summary>
        public static bool RecuperarSiInterrumpida()
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return false;
            }

            string clave =
                EstadoClavePrefijo + usuarioId.Trim();

            string json = Preferences.Get(
                clave,
                string.Empty);

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                SincronizacionOfflineGlobalEstado? estado =
                    JsonSerializer.Deserialize<
                        SincronizacionOfflineGlobalEstado>(
                            json,
                            JsonOptions);

                if (estado?.SincronizacionEnCurso != true)
                    return false;

                SincronizacionOfflineGlobalEstado recuperado = new()
                {
                    Estado = SincronizacionOfflineGlobalEstados.Error,
                    Mensaje = estado.PreparacionCompleta
                        ? "Se conserva la copia anterior"
                        : "Descarga interrumpida",
                    Detalle =
                        "La aplicación se cerró antes de finalizar la descarga. " +
                        "Puede iniciar una nueva descarga; los datos completos " +
                        "guardados anteriormente no fueron eliminados.",
                    ProgresoPorcentaje = estado.ProgresoPorcentaje,
                    PasoActual = estado.PasoActual,
                    TotalPasos = estado.TotalPasos,
                    PreparacionCompleta = estado.PreparacionCompleta,
                    UltimaSincronizacionCompletaUtc =
                        estado.UltimaSincronizacionCompletaUtc,
                    UltimaVerificacionUtc = DateTime.UtcNow,
                    TamanoTotalBytes = estado.TamanoTotalBytes,
                    MotorCalculo = RecuperarModulo(
                        estado.MotorCalculo),
                    Catalogos = RecuperarModulo(
                        estado.Catalogos),
                    Analisis = RecuperarModulo(
                        estado.Analisis),
                    Noticias = RecuperarModulo(
                        estado.Noticias),
                    Album = RecuperarModulo(
                        estado.Album)
                };

                Preferences.Set(
                    clave,
                    JsonSerializer.Serialize(
                        recuperado,
                        JsonOptions));

                return true;
            }
            catch
            {
                /*
                 * Un estado antiguo o dañado no debe impedir el inicio de la app.
                 * El servicio principal seguirá usando sus valores predeterminados.
                 */
                return false;
            }
        }

        private static ModuloOfflineResumen RecuperarModulo(
            ModuloOfflineResumen? modulo)
        {
            if (modulo == null)
                return new ModuloOfflineResumen();

            if (!string.Equals(
                    modulo.Estado,
                    ModuloOfflineEstados.Sincronizando,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ModuloOfflineResumen
                {
                    Nombre = modulo.Nombre,
                    Estado = modulo.Estado,
                    Mensaje = modulo.Mensaje,
                    Registros = modulo.Registros,
                    Imagenes = modulo.Imagenes
                };
            }

            return new ModuloOfflineResumen
            {
                Nombre = modulo.Nombre,
                Estado = ModuloOfflineEstados.Error,
                Mensaje =
                    "La aplicación se cerró antes de terminar esta etapa.",
                Registros = modulo.Registros,
                Imagenes = modulo.Imagenes
            };
        }
    }
}
