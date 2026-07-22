namespace CONATRADEC.Services
{
    /// <summary>
    /// Puente temporal para los servicios antiguos que todavía convierten
    /// ApiResult en bool y, por tanto, pierden el texto del error.
    ///
    /// La app utiliza un único HttpClient y las operaciones de escritura
    /// se ejecutan de forma secuencial desde la interfaz. El mensaje expira
    /// rápidamente para evitar reutilizar un error anterior.
    /// </summary>
    internal static class ApiErrorContext
    {
        private static readonly object SyncRoot = new();
        private static readonly TimeSpan MaximumAge =
            TimeSpan.FromSeconds(30);

        private static PendingApiError? pendingError;

        public static void Set(
            string? message,
            int? statusCode = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (SyncRoot)
            {
                pendingError = new PendingApiError(
                    message.Trim(),
                    statusCode,
                    DateTimeOffset.UtcNow);
            }
        }

        public static string ResolveForDisplay(string? fallbackMessage)
        {
            string fallback = fallbackMessage?.Trim() ?? string.Empty;
            PendingApiError? current;

            lock (SyncRoot)
            {
                current = pendingError;
                pendingError = null;
            }

            if (current is null ||
                DateTimeOffset.UtcNow - current.CreatedAt > MaximumAge)
            {
                return fallback;
            }

            if (string.IsNullOrWhiteSpace(fallback) ||
                IsGenericMessage(fallback) ||
                string.Equals(
                    Normalize(fallback),
                    Normalize(current.Message),
                    StringComparison.OrdinalIgnoreCase))
            {
                return current.Message;
            }

            return fallback;
        }

        private static bool IsGenericMessage(string message)
        {
            string value = message.ToLowerInvariant();

            return value.Contains("no fue posible") ||
                   value.Contains("no se pudo") ||
                   value.Contains("ocurrió un error") ||
                   value.Contains("ocurrio un error") ||
                   value.Contains("error inesperado") ||
                   value.Contains("intente nuevamente") ||
                   value.Contains("servidor presentó") ||
                   value.Contains("servidor presento");
        }

        private static string Normalize(string value) =>
            string.Join(
                " ",
                value.Split(
                    new[] { ' ', '\r', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries));

        private sealed record PendingApiError(
            string Message,
            int? StatusCode,
            DateTimeOffset CreatedAt);
    }
}
