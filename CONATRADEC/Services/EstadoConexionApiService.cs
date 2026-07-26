namespace CONATRADEC.Services
{
    /// <summary>
    /// Realiza una comprobación HTTP pequeña contra el endpoint de versión.
    /// No descarga el feed, los detalles ni las imágenes.
    /// </summary>
    public sealed class EstadoConexionApiService
    {
        private static readonly Lazy<EstadoConexionApiService> lazy =
            new(() => new EstadoConexionApiService());

        private static readonly TimeSpan TiempoMaximo =
            TimeSpan.FromSeconds(5);

        public static EstadoConexionApiService Instance => lazy.Value;

        private EstadoConexionApiService()
        {
        }

        public async Task<bool> ComprobarAsync(
            string modulo,
            CancellationToken cancellationToken = default)
        {
            string moduloNormalizado = (modulo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            if (moduloNormalizado is not ("noticias" or "album"))
                return false;

            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeout.CancelAfter(TiempoMaximo);

            try
            {
                string route =
                    "api/contenido-sincronizacion/estado" +
                    $"?modulo={Uri.EscapeDataString(moduloNormalizado)}";

                using HttpResponseMessage response =
                    await ApiClientService.Client.SendAsync(
                        new HttpRequestMessage(HttpMethod.Get, route),
                        HttpCompletionOption.ResponseHeadersRead,
                        timeout.Token);

                /*
                 * La API respondió. El código HTTP puede representar permisos
                 * o un error funcional, pero sí existe comunicación de red.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                /*
                 * La página se cerró o la aplicación canceló el proceso.
                 * En este caso sí se respeta la cancelación solicitada.
                 */
                throw;
            }
            catch (OperationCanceledException)
            {
                /*
                 * El tiempo máximo de la comprobación se agotó. Esto indica
                 * que la API no está disponible, pero NO debe cancelar el
                 * ciclo periódico de comprobación.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return false;
            }
            catch (HttpRequestException)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return false;
            }
            catch (IOException)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return false;
            }
            catch
            {
                /*
                 * Una comprobación de conectividad nunca debe detener el
                 * temporizador automático. Se marcará como no disponible y
                 * volverá a intentarse en el siguiente intervalo.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return false;
            }
        }
    }
}
