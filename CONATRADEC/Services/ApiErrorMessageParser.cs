using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Lee mensajes tanto del nuevo contrato de errores como de las
    /// respuestas antiguas: texto plano, message, mensaje, detail,
    /// ValidationProblemDetails y listas usadoEn.
    /// </summary>
    internal static class ApiErrorMessageParser
    {
        public static string Parse(
            HttpStatusCode statusCode,
            string? content,
            string fallback)
        {
            string defaultMessage =
                GetDefaultMessage(statusCode, fallback);

            if (string.IsNullOrWhiteSpace(content))
                return defaultMessage;

            string value = content.Trim();

            if (!LooksLikeJson(value))
            {
                if (value.StartsWith("<", StringComparison.Ordinal))
                    return defaultMessage;

                string plainText = value.Trim('"').Trim();

                return string.IsNullOrWhiteSpace(plainText)
                    ? defaultMessage
                    : plainText;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(value);
                JsonElement root = document.RootElement;

                string? primaryMessage = FindMessage(root, 0);

                var additionalMessages = new List<string>();
                CollectValidationErrors(root, additionalMessages, 0);
                CollectUsedIn(root, additionalMessages, 0);

                additionalMessages = additionalMessages
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => message.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(primaryMessage))
                {
                    additionalMessages.RemoveAll(message =>
                        string.Equals(
                            Normalize(message),
                            Normalize(primaryMessage),
                            StringComparison.OrdinalIgnoreCase));

                    return additionalMessages.Count == 0
                        ? primaryMessage
                        : $"{primaryMessage} {string.Join(" ", additionalMessages)}";
                }

                return additionalMessages.Count > 0
                    ? string.Join(" ", additionalMessages)
                    : defaultMessage;
            }
            catch (JsonException)
            {
                return defaultMessage;
            }
        }

        public static string GetDefaultMessage(
            HttpStatusCode statusCode,
            string fallback)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest =>
                    "Revise los datos ingresados.",
                HttpStatusCode.Unauthorized =>
                    "La sesión no está autorizada. Inicie sesión nuevamente.",
                HttpStatusCode.Forbidden =>
                    "No tiene permisos para realizar esta operación.",
                HttpStatusCode.NotFound =>
                    "No se encontró el registro solicitado.",
                HttpStatusCode.Conflict =>
                    "La información ingresada entra en conflicto con un registro existente.",
                HttpStatusCode.RequestEntityTooLarge =>
                    "El archivo o contenido enviado supera el tamaño permitido.",
                HttpStatusCode.UnsupportedMediaType =>
                    "El formato del contenido enviado no es compatible.",
                HttpStatusCode.TooManyRequests =>
                    "Se realizaron demasiadas solicitudes. Intente nuevamente.",
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout =>
                    "El servidor presentó un problema. Intente nuevamente.",
                _ when !string.IsNullOrWhiteSpace(fallback) =>
                    fallback,
                _ =>
                    "No fue posible completar la operación."
            };
        }

        private static string? FindMessage(
            JsonElement element,
            int depth)
        {
            if (depth > 4)
                return null;

            if (element.ValueKind == JsonValueKind.String)
                return Clean(element.GetString());

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (string propertyName in new[]
                     {
                         "message",
                         "mensaje",
                         "detail",
                         "descripcion",
                         "description",
                         "error_description"
                     })
            {
                if (TryGetPropertyIgnoreCase(
                        element,
                        propertyName,
                        out JsonElement propertyValue) &&
                    propertyValue.ValueKind == JsonValueKind.String)
                {
                    string? message = Clean(propertyValue.GetString());

                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
            }

            foreach (string nestedProperty in new[]
                     {
                         "error",
                         "details",
                         "detalle",
                         "data"
                     })
            {
                if (TryGetPropertyIgnoreCase(
                        element,
                        nestedProperty,
                        out JsonElement nested))
                {
                    string? nestedMessage = FindMessage(nested, depth + 1);

                    if (!string.IsNullOrWhiteSpace(nestedMessage))
                        return nestedMessage;
                }
            }

            if (TryGetPropertyIgnoreCase(
                    element,
                    "title",
                    out JsonElement title) &&
                title.ValueKind == JsonValueKind.String)
            {
                return Clean(title.GetString());
            }

            return null;
        }

        private static void CollectValidationErrors(
            JsonElement element,
            ICollection<string> messages,
            int depth)
        {
            if (depth > 4 || element.ValueKind != JsonValueKind.Object)
                return;

            if (TryGetPropertyIgnoreCase(
                    element,
                    "errors",
                    out JsonElement errors))
            {
                if (errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement item in property.Value.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    string? message = Clean(item.GetString());

                                    if (!string.IsNullOrWhiteSpace(message))
                                        messages.Add(message);
                                }
                            }
                        }
                        else if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            string? message = Clean(property.Value.GetString());

                            if (!string.IsNullOrWhiteSpace(message))
                                messages.Add(message);
                        }
                    }
                }
                else if (errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in errors.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            string? message = Clean(item.GetString());

                            if (!string.IsNullOrWhiteSpace(message))
                                messages.Add(message);
                        }
                    }
                }
            }

            foreach (string nestedProperty in new[]
                     {
                         "details",
                         "detalle",
                         "error"
                     })
            {
                if (TryGetPropertyIgnoreCase(
                        element,
                        nestedProperty,
                        out JsonElement nested))
                {
                    CollectValidationErrors(
                        nested,
                        messages,
                        depth + 1);
                }
            }
        }

        private static void CollectUsedIn(
            JsonElement element,
            ICollection<string> messages,
            int depth)
        {
            if (depth > 4 || element.ValueKind != JsonValueKind.Object)
                return;

            foreach (string propertyName in new[]
                     {
                         "usadoEn",
                         "usedIn",
                         "dependencias",
                         "dependencies"
                     })
            {
                if (!TryGetPropertyIgnoreCase(
                        element,
                        propertyName,
                        out JsonElement values) ||
                    values.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                string[] items = values
                    .EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()?.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (items.Length > 0)
                {
                    messages.Add(
                        $"Está siendo utilizado en: {string.Join(", ", items)}.");
                }
            }

            foreach (string nestedProperty in new[]
                     {
                         "details",
                         "detalle",
                         "error"
                     })
            {
                if (TryGetPropertyIgnoreCase(
                        element,
                        nestedProperty,
                        out JsonElement nested))
                {
                    CollectUsedIn(
                        nested,
                        messages,
                        depth + 1);
                }
            }
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            value = default;

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeJson(string value) =>
            value.StartsWith("{", StringComparison.Ordinal) ||
            value.StartsWith("[", StringComparison.Ordinal) ||
            value.StartsWith("\"", StringComparison.Ordinal);

        private static string? Clean(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            string value = string.Join(
                " ",
                message.Split(
                    new[] { ' ', '\r', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries));

            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().Trim('"');
        }

        private static string Normalize(string value) =>
            string.Join(
                " ",
                value.Split(
                    new[] { ' ', '\r', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries));
    }
}
