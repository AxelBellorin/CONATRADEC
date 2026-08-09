using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene separadas la responsabilidad persistente de una etapa y su
    /// bloqueo temporal de edición. Una inspección debe tomarse explícitamente
    /// antes de solicitar el bloqueo cuando todavía no tiene responsable.
    /// </summary>
    public sealed class InspeccionRevisionBloqueoApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public Task<InspeccionRevisionAsignacion> ObtenerAsignacionAsync(
            int inspeccionId,
            string modo,
            CancellationToken cancellationToken = default) =>
            EnviarAsignacionAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/asignacion?modo={Uri.EscapeDataString(NormalizarModo(modo))}"),
                "No fue posible consultar la asignación de esta inspección.",
                cancellationToken);

        public Task<InspeccionRevisionAsignacion> TomarAsync(
            int inspeccionId,
            string modo,
            CancellationToken cancellationToken = default) =>
            EnviarAsignacionAsync(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/tomar?modo={Uri.EscapeDataString(NormalizarModo(modo))}"),
                "No fue posible tomar la inspección para esta etapa.",
                cancellationToken);

        public Task<InspeccionRevisionBloqueo> AdquirirAsync(
            int inspeccionId,
            string modo,
            CancellationToken cancellationToken = default) =>
            EnviarBloqueoAsync(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/adquirir?modo={Uri.EscapeDataString(NormalizarModo(modo))}"),
                cancellationToken);

        public Task<InspeccionRevisionBloqueo> RenovarAsync(
            int inspeccionId,
            string modo,
            string token,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/renovar?modo={Uri.EscapeDataString(NormalizarModo(modo))}")
            {
                Content = JsonContent.Create(new { token })
            };

            return EnviarBloqueoAsync(request, cancellationToken);
        }

        public async Task LiberarAsync(
            int inspeccionId,
            string modo,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0 || string.IsNullOrWhiteSpace(token))
                return;

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/liberar?modo={Uri.EscapeDataString(NormalizarModo(modo))}")
            {
                Content = JsonContent.Create(new { token })
            };

            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                /*
                 * La liberación es de mejor esfuerzo. Si la red se corta, el
                 * backend libera automáticamente el bloqueo cuando vence.
                 */
            }
        }

        /// <summary>
        /// Devuelve únicamente usuarios que poseen el permiso real de
        /// actualización de la etapa. Este endpoint requiere permiso de lectura
        /// del Centro de Control Fitosanitario.
        /// </summary>
        public Task<List<InspeccionRevisionUsuarioAsignable>>
            ObtenerUsuariosAsignablesAsync(
                string modo,
                CancellationToken cancellationToken = default)
        {
            string etapa = NormalizarModo(modo).ToUpperInvariant();
            return EnviarControlAsync<List<InspeccionRevisionUsuarioAsignable>>(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/control-fitosanitario/usuarios?etapa={Uri.EscapeDataString(etapa)}"),
                "No fue posible cargar los usuarios autorizados para esta etapa.",
                cancellationToken);
        }

        /// <summary>
        /// Reasigna una etapa mediante el Centro de Control. La API valida el
        /// permiso administrativo, el permiso del usuario destino y registra la
        /// operación en auditoría.
        /// </summary>
        public Task<InspeccionRevisionOperacionAsignacion> ReasignarAsync(
            int inspeccionId,
            string modo,
            int usuarioNuevoId,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            string etapa = NormalizarModo(modo).ToUpperInvariant();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/control-fitosanitario/{inspeccionId}/reasignar")
            {
                Content = JsonContent.Create(
                    new InspeccionRevisionReasignacionRequest
                    {
                        Etapa = etapa,
                        UsuarioNuevoId = usuarioNuevoId,
                        Motivo = motivo?.Trim() ?? string.Empty
                    })
            };

            return EnviarControlAsync<InspeccionRevisionOperacionAsignacion>(
                request,
                "No fue posible reasignar la inspección.",
                cancellationToken);
        }

        private async Task<InspeccionRevisionBloqueo> EnviarBloqueoAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                RespuestaApi<InspeccionRevisionBloqueo>? envelope =
                    Deserializar<InspeccionRevisionBloqueo>(contenido);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InspeccionRevisionBloqueoException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? "No fue posible reservar la inspección para esta sesión."
                            : envelope.Message,
                        envelope?.Data);
                }

                if (envelope?.Data != null)
                    return envelope.Data;

                throw new InspeccionRevisionBloqueoException(
                    "El servidor devolvió un bloqueo de inspección incompleto.",
                    null);
            }
        }

        private async Task<InspeccionRevisionAsignacion> EnviarAsignacionAsync(
            HttpRequestMessage request,
            string mensajePredeterminado,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                RespuestaApi<InspeccionRevisionAsignacion>? envelope =
                    Deserializar<InspeccionRevisionAsignacion>(contenido);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InspeccionRevisionAsignacionException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? mensajePredeterminado
                            : envelope.Message,
                        envelope?.Data);
                }

                if (envelope?.Data != null)
                    return envelope.Data;

                throw new InspeccionRevisionAsignacionException(
                    "El servidor devolvió una asignación incompleta.",
                    null);
            }
        }

        private async Task<T> EnviarControlAsync<T>(
            HttpRequestMessage request,
            string mensajePredeterminado,
            CancellationToken cancellationToken)
            where T : class
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                RespuestaApi<T>? envelope = Deserializar<T>(contenido);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InspeccionRevisionAsignacionException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? mensajePredeterminado
                            : envelope.Message,
                        null);
                }

                if (envelope?.Data != null)
                    return envelope.Data;

                throw new InspeccionRevisionAsignacionException(
                    mensajePredeterminado,
                    null);
            }
        }

        private static RespuestaApi<T>? Deserializar<T>(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            try
            {
                return JsonSerializer.Deserialize<RespuestaApi<T>>(
                    contenido,
                    JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string NormalizarModo(string? modo)
        {
            string valor = (modo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            return valor switch
            {
                "analizador" => "analizador",
                "aprobador" => "aprobador",
                _ => valor
            };
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }

    public sealed class InspeccionRevisionBloqueoException : Exception
    {
        public InspeccionRevisionBloqueoException(
            string mensaje,
            InspeccionRevisionBloqueo? bloqueo)
            : base(mensaje)
        {
            Bloqueo = bloqueo;
        }

        public InspeccionRevisionBloqueo? Bloqueo { get; }
    }

    public sealed class InspeccionRevisionAsignacionException : Exception
    {
        public InspeccionRevisionAsignacionException(
            string mensaje,
            InspeccionRevisionAsignacion? asignacion)
            : base(mensaje)
        {
            Asignacion = asignacion;
        }

        public InspeccionRevisionAsignacion? Asignacion { get; }
    }
}
