using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene consistente el estado visual de la preparación offline con
    /// el archivo físico del motor de cálculo.
    ///
    /// Si una actualización de la aplicación cambia el esquema soportado,
    /// el motor anterior puede seguir existiendo en el dispositivo, pero ya
    /// no es válido. En ese caso se invalida la bandera de preparación y se
    /// guarda un estado claro para que la pantalla solicite Descargar todo.
    ///
    /// No elimina análisis pendientes, fotografías, catálogos ni historiales.
    /// </summary>
    public static class MotorCalculoCompatibilidadPreparacionService
    {
        private const string EstadoClavePrefijo =
            "offline_global_manual_estado_";

        private static readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

        public static async Task ValidarAsync(
            CancellationToken cancellationToken = default)
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return;
            }

            usuarioId = usuarioId.Trim();

            bool motorValido =
                await MotorCalculoPaqueteService.Instance
                    .TienePaqueteValidoAsync(cancellationToken);

            if (motorValido)
                return;

            bool existeArchivoAnterior =
                MotorCalculoPaqueteService.Instance
                    .ObtenerTamanoPaqueteBytes() > 0;

            InvalidarPreparacion(usuarioId);

            SincronizacionOfflineGlobalEstado anterior =
                CargarEstadoAnterior(usuarioId);

            string titulo = existeArchivoAnterior
                ? "Datos offline desactualizados"
                : "Dispositivo sin preparar";

            string detalle = existeArchivoAnterior
                ? "El motor de cálculo descargado pertenece a una versión anterior o está incompleto. Inicie una sesión en línea y ejecute Descargar todo."
                : "Este dispositivo todavía no tiene un motor de cálculo válido. Inicie una sesión en línea y ejecute Descargar todo.";

            ModuloOfflineResumen motor = new()
            {
                Nombre = "Motor de cálculo",
                Estado = ModuloOfflineEstados.Pendiente,
                Mensaje = existeArchivoAnterior
                    ? "Actualización requerida. El motor anterior ya no es compatible."
                    : "Pendiente de descarga.",
                Registros = 0,
                Imagenes = 0
            };

            SincronizacionOfflineGlobalEstado actualizado = new()
            {
                Estado = SincronizacionOfflineGlobalEstados.SinPreparar,
                Mensaje = titulo,
                Detalle = detalle,
                ProgresoPorcentaje = 0,
                PasoActual = 0,
                TotalPasos = anterior.TotalPasos > 0
                    ? anterior.TotalPasos
                    : 5,
                PreparacionCompleta = false,
                UltimaSincronizacionCompletaUtc =
                    anterior.UltimaSincronizacionCompletaUtc,
                UltimaVerificacionUtc = DateTime.UtcNow,
                TamanoTotalBytes = anterior.TamanoTotalBytes,
                MotorCalculo = motor,
                Catalogos = anterior.Catalogos,
                Analisis = anterior.Analisis,
                Noticias = anterior.Noticias,
                Album = anterior.Album
            };

            string json = JsonSerializer.Serialize(
                actualizado,
                jsonOptions);

            Preferences.Set(
                ConstruirClaveEstado(usuarioId),
                json);
        }

        private static SincronizacionOfflineGlobalEstado
            CargarEstadoAnterior(string usuarioId)
        {
            try
            {
                string json = Preferences.Get(
                    ConstruirClaveEstado(usuarioId),
                    string.Empty);

                if (string.IsNullOrWhiteSpace(json))
                    return new SincronizacionOfflineGlobalEstado();

                return JsonSerializer.Deserialize<
                           SincronizacionOfflineGlobalEstado>(
                           json,
                           jsonOptions)
                       ?? new SincronizacionOfflineGlobalEstado();
            }
            catch
            {
                return new SincronizacionOfflineGlobalEstado();
            }
        }

        private static void InvalidarPreparacion(
            string usuarioId)
        {
            /*
             * Se eliminan las versiones conocidas. La descarga completa
             * volverá a crear únicamente las claves usadas por la versión
             * instalada de SincronizacionOfflineGlobalService.
             */
            for (int version = 1; version <= 5; version++)
            {
                Preferences.Remove(
                    $"offline_global_preparado_v{version}_{usuarioId}");

                Preferences.Remove(
                    $"offline_global_preparado_fecha_v{version}_{usuarioId}");

                Preferences.Remove(
                    $"offline_global_preparado_perfil_v{version}_{usuarioId}");
            }
        }

        private static string ConstruirClaveEstado(
            string usuarioId) =>
            EstadoClavePrefijo + usuarioId;
    }
}
