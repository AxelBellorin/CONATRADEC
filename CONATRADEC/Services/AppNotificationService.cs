using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio centralizado para mostrar mensajes de la aplicación.
    ///
    /// Reglas:
    /// - Success: operaciones completadas correctamente.
    /// - Error: fallos de API, validaciones que impiden guardar y operaciones rechazadas.
    /// - Warning: conexión, operaciones parciales o estados que requieren atención.
    /// - Information: permisos, estados y mensajes informativos.
    /// </summary>
    public static class AppNotificationService
    {
        private static readonly SemaphoreSlim NotificationLock =
            new(1, 1);

        private static readonly Color SuccessBackground =
            Color.FromArgb("#2E7D32");

        private static readonly Color ErrorBackground =
            Color.FromArgb("#C62828");

        private static readonly Color WarningBackground =
            Color.FromArgb("#ED6C02");

        private static readonly Color InformationBackground =
            Color.FromArgb("#1565C0");

        public static Task ShowSuccessAsync(string message) =>
            ShowAsync(message, AppMessageType.Success);

        public static Task ShowErrorAsync(string message) =>
            ShowAsync(message, AppMessageType.Error);

        public static Task ShowWarningAsync(string message) =>
            ShowAsync(message, AppMessageType.Warning);

        public static Task ShowInformationAsync(string message) =>
            ShowAsync(message, AppMessageType.Information);

        /// <summary>
        /// Compatibilidad con los mensajes existentes.
        /// Determina automáticamente el tipo y limpia prefijos como:
        /// "Éxito", "Error", saltos de línea y concatenaciones sin espacios.
        /// </summary>
        public static Task ShowAutoAsync(string message)
        {
            AppMessageType type = InferType(message);
            return ShowAsync(message, type);
        }

        public static async Task ShowAsync(
            string message,
            AppMessageType type)
        {
            if (type == AppMessageType.Error)
            {
                // Reemplaza mensajes genéricos del ViewModel por el motivo
                // real devuelto por la API, incluso cuando un servicio
                // antiguo todavía entrega únicamente true/false.
                message =
                    ApiErrorContext.ResolveForDisplay(
                        message);

                /*
                 * Durante una sesión offline la API nunca fue consultada.
                 * Cuando una lectura auxiliar no existe en la copia local, la
                 * pantalla continúa con los datos disponibles y no se muestra
                 * un falso error del servidor.
                 */
                if (OfflineReadResponseService
                    .DebeOmitirNotificacionError(message))
                {
                    return;
                }
            }

            string normalizedMessage =
                NormalizeMessage(message, type);

            if (string.IsNullOrWhiteSpace(
                    normalizedMessage))
            {
                return;
            }

            // El Snackbar se muestra después de que Android haya cerrado
            // el teclado y reajustado el área visible.
            await KeyboardService.HideAsync();

            await NotificationLock.WaitAsync();

            try
            {
                string displayMessage =
                    $"{GetIcon(type)}  {normalizedMessage}";

                var visualOptions =
                    new SnackbarOptions
                    {
                        BackgroundColor =
                            GetBackgroundColor(type),
                        TextColor =
                            Colors.White,
                        ActionButtonTextColor =
                            Colors.White
                    };

                TimeSpan duration =
                    type is AppMessageType.Error
                        or AppMessageType.Warning
                            ? normalizedMessage.Length > 120
                                ? TimeSpan.FromSeconds(7)
                                : TimeSpan.FromSeconds(5)
                            : TimeSpan.FromSeconds(3);

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        await Snackbar
                            .Make(
                                displayMessage,
                                duration: duration,
                                visualOptions:
                                    visualOptions)
                            .Show();
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible mostrar la notificación: {ex}");
            }
            finally
            {
                NotificationLock.Release();
            }
        }

        public static async Task<bool> ConfirmAsync(
            string title,
            string message,
            string acceptText,
            string cancelText)
        {
            // Evita que DisplayAlert quede visualmente limitado por
            // el teclado virtual.
            await KeyboardService.HideAsync();

            Page? page =
                Application.Current?.MainPage;

            if (page == null)
                return false;

            return await MainThread.InvokeOnMainThreadAsync(
                () => page.DisplayAlert(
                    title,
                    message,
                    acceptText,
                    cancelText));
        }

        public static Task<bool> ConfirmSaveAsync(
            string entityName) =>
            ConfirmAsync(
                $"Guardar {entityName}",
                $"¿Desea guardar los datos de {entityName}?",
                "Guardar",
                "Cancelar");

        public static Task<bool> ConfirmUpdateAsync(
            string entityName) =>
            ConfirmAsync(
                $"Actualizar {entityName}",
                $"¿Desea guardar los cambios realizados en {entityName}?",
                "Actualizar",
                "Cancelar");

        public static Task<bool> ConfirmDeleteAsync(
            string entityName) =>
            ConfirmAsync(
                $"Eliminar {entityName}",
                $"¿Está seguro de que desea eliminar {entityName}? Esta acción no se puede deshacer.",
                "Eliminar",
                "Cancelar");

        public static Task<bool>
            ConfirmDiscardChangesAsync() =>
            ConfirmAsync(
                "Salir sin guardar",
                "Hay cambios sin guardar. ¿Desea salir y descartarlos?",
                "Salir sin guardar",
                "Continuar editando");

        public static string NormalizeMessage(
            string? message,
            AppMessageType type)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            string value = message
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " ");

            // Corrige prefijos usados en mensajes anteriores, incluso cuando
            // fueron concatenados sin espacio: "ErrorNo fue posible...".
            value = Regex.Replace(
                value,
                @"^(Éxito|Exito|Error|Advertencia|Información|Informacion)\s*[:\-–—]*\s*",
                string.Empty,
                RegexOptions.IgnoreCase);

            // Terminología acordada para la aplicación.
            value = Regex.Replace(
                value,
                @"\bph\b",
                "PH",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

            value = Regex.Replace(
                value,
                @",\s*intente nuevamente",
                ". Intente nuevamente",
                RegexOptions.IgnoreCase);

            value = Regex.Replace(
                value,
                @"\s+([,.;:!?])",
                "$1");

            if (LooksLikeTechnicalException(
                    value))
            {
                value =
                    "Ocurrió un error inesperado. Intente nuevamente.";
            }

            value =
                value.Trim(
                    ' ',
                    '-',
                    ':');

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                value = type switch
                {
                    AppMessageType.Success =>
                        "La operación se completó correctamente.",
                    AppMessageType.Error =>
                        "No fue posible completar la operación. Intente nuevamente.",
                    AppMessageType.Warning =>
                        "Revise la información ingresada.",
                    _ =>
                        "Operación procesada."
                };
            }

            value =
                char.ToUpper(value[0]) +
                value[1..];

            if (!value.EndsWith(".") &&
                !value.EndsWith("?") &&
                !value.EndsWith("!"))
            {
                value += ".";
            }

            return value;
        }

        private static AppMessageType InferType(
            string? message)
        {
            string value =
                message?.Trim().ToLowerInvariant() ??
                string.Empty;

            /*
             * Una operación parcialmente completada continúa siendo advertencia.
             * Ejemplo: el usuario fue creado, pero la imagen no pudo guardarse.
             */
            if (value.Contains("pero") &&
                (value.Contains("guardad") ||
                 value.Contains("cread") ||
                 value.Contains("actualizad") ||
                 value.Contains("eliminad")))
            {
                return AppMessageType.Warning;
            }

            /*
             * Los problemas de conexión y la ausencia de cambios se mantienen
             * en naranja porque no representan una validación rechazada.
             */
            if (value.Contains("sin conexión") ||
                value.Contains("no hay conexión") ||
                value.Contains("verifique su red") ||
                value.Contains("no hay cambios"))
            {
                return AppMessageType.Warning;
            }

            /*
             * Las validaciones que impiden guardar se evalúan antes que los
             * mensajes exitosos.
             */
            if (LooksLikeBlockingValidation(
                    value))
            {
                return AppMessageType.Error;
            }

            if (value.StartsWith("error") ||
                value.Contains("no fue posible") ||
                value.Contains("no se pudo") ||
                value.Contains("error inesperado") ||
                value.Contains("servidor presentó") ||
                value.Contains("servidor presento") ||
                value.Contains("intente nuevamente"))
            {
                return AppMessageType.Error;
            }

            if (value.StartsWith("éxito") ||
                value.StartsWith("exito") ||
                value.Contains("correctamente") ||
                value.Contains("completada con éxito") ||
                value.Contains("completado con éxito") ||
                Regex.IsMatch(
                    value,
                    @"\b(creado|creada|guardado|guardada|actualizado|actualizada|eliminado|eliminada|registrado|registrada|reactivado|reactivada)\b",
                    RegexOptions.IgnoreCase) ||
                value.Contains("se guardó") ||
                value.Contains("se guardo") ||
                value.Contains("se actualizó") ||
                value.Contains("se actualizo") ||
                value.Contains("se eliminó") ||
                value.Contains("se elimino") ||
                value.Contains("se reactivó") ||
                value.Contains("se reactivo"))
            {
                return AppMessageType.Success;
            }

            if (value.Contains("permiso") ||
                value.Contains("sesión") ||
                value.Contains("sesion") ||
                value.Contains("información") ||
                value.Contains("informacion"))
            {
                return AppMessageType.Information;
            }

            return AppMessageType.Information;
        }

        /// <summary>
        /// Detecta mensajes que indican que la operación fue rechazada y que,
        /// por tanto, los datos no fueron guardados.
        /// </summary>
        private static bool LooksLikeBlockingValidation(
            string value)
        {
            return
                value.Contains("ya existe") ||
                value.Contains("ya se encuentra registrad") ||
                value.Contains("ya está registrad") ||
                value.Contains("ya esta registrad") ||
                value.Contains("registro duplicad") ||
                value.Contains("dato duplicad") ||
                value.Contains("está duplicad") ||
                value.Contains("esta duplicad") ||
                value.Contains("mismos datos") ||
                value.Contains("mismo nombre") ||
                value.Contains("mismo código") ||
                value.Contains("mismo codigo") ||
                value.Contains("no se guardó") ||
                value.Contains("no se guardo") ||
                value.Contains("no fue guardad") ||
                value.Contains("no se registró") ||
                value.Contains("no se registro") ||
                value.Contains("no fue registrad") ||
                value.Contains("no se recibieron") ||
                value.Contains("no es válido") ||
                value.Contains("no es valido") ||
                value.Contains("no es válida") ||
                value.Contains("no es valida") ||
                value.Contains("no existe") ||
                value.Contains("no se puede") ||
                value.Contains("no puede") ||
                value.Contains("está inactivo") ||
                value.Contains("esta inactivo") ||
                value.Contains("está inactiva") ||
                value.Contains("esta inactiva") ||
                value.Contains("está desactivado") ||
                value.Contains("esta desactivado") ||
                value.Contains("está desactivada") ||
                value.Contains("esta desactivada") ||
                value.Contains("seleccione") ||
                value.Contains("ingrese") ||
                value.Contains("debe ") ||
                value.Contains("revise ") ||
                value.Contains("faltan ") ||
                value.Contains("obligatorio") ||
                value.Contains("obligatoria") ||
                value.Contains("requerido") ||
                value.Contains("requerida") ||
                value.Contains("inválido") ||
                value.Contains("invalido") ||
                value.Contains("inválida") ||
                value.Contains("invalida") ||
                value.Contains("fuera de rango") ||
                value.Contains("no coincide") ||
                value.Contains("no cumplen") ||
                value.Contains("no cumple");
        }

        private static bool LooksLikeTechnicalException(
            string value)
        {
            string lower =
                value.ToLowerInvariant();

            return
                lower.StartsWith("object reference") ||
                lower.StartsWith("value cannot be null") ||
                lower.StartsWith("sequence contains") ||
                lower.StartsWith("index was outside") ||
                lower.StartsWith("nullable object") ||
                lower.StartsWith("response status code") ||
                lower.StartsWith("the given key") ||
                lower.Contains(
                    "system.nullreferenceexception") ||
                lower.Contains(
                    "system.invalidoperationexception") ||
                lower.Contains("stack trace") ||
                lower.Contains(" at conatradec.");
        }

        private static string GetIcon(
            AppMessageType type) =>
            type switch
            {
                AppMessageType.Success => "✓",
                AppMessageType.Error => "✕",
                AppMessageType.Warning => "⚠",
                _ => "ℹ"
            };

        private static Color GetBackgroundColor(
            AppMessageType type) =>
            type switch
            {
                AppMessageType.Success =>
                    SuccessBackground,
                AppMessageType.Error =>
                    ErrorBackground,
                AppMessageType.Warning =>
                    WarningBackground,
                _ =>
                    InformationBackground
            };
    }
}
