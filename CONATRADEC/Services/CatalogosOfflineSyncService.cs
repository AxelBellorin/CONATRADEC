using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public sealed class CatalogosOfflineSyncService
    {
        private static readonly Lazy<CatalogosOfflineSyncService> lazy =
            new(() => new CatalogosOfflineSyncService());

        public static CatalogosOfflineSyncService Instance => lazy.Value;

        private CatalogosOfflineSyncService()
        {
        }

        public void SolicitarSincronizacionEnSegundoPlano()
        {
            PaqueteCatalogosOfflineService.Instance
                .VerificarActualizacionEnSegundoPlano();
        }

        public void MarcarPendiente()
        {
            _ = PaqueteCatalogosOfflineService.Instance
                .MarcarActualizacionPendienteAsync();
        }

        public Task<ResultadoDescargaOffline> SincronizarSiNecesarioAsync(
            bool forzarDescargaCompleta,
            CancellationToken cancellationToken = default)
        {
            return PaqueteCatalogosOfflineService.Instance
                .DescargarTodoAsync(forzarDescargaCompleta);
        }
    }
}
