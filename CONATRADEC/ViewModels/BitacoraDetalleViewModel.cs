using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class BitacoraDetalleViewModel : GlobalService
    {
        private readonly BitacoraApiService apiService = new();
        private BitacoraDetalleItem? registro;
        private Guid bitacoraId;
        private bool cargado;
        private bool inicializando;
        private CancellationTokenSource? cargaCts;

        public BitacoraDetalleItem? Registro
        {
            get => registro;
            private set
            {
                registro = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneRegistro));
            }
        }

        public bool TieneRegistro => Registro != null;

        public BitacoraDetalleViewModel()
        {
            LoadPagePermissions("bitacoraPage");
        }

        public void AplicarId(Guid id)
        {
            if (bitacoraId == id)
                return;

            bitacoraId = id;
            cargado = false;
            Registro = null;
        }

        public async Task InicializarAsync()
        {
            LoadPagePermissions("bitacoraPage");
            OnPropertyChanged(nameof(CanView));

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para consultar la bitácora.");
                await GoToAsyncParameters(AppRoutes.Regresar);
                return;
            }

            if (cargado || inicializando || IsBusy || bitacoraId == Guid.Empty)
                return;

            inicializando = true;
            CancellationTokenSource? cts = null;

            try
            {
                if (!await ValidarInternetAsync())
                    return;

                CancelarCarga();
                cts = new CancellationTokenSource();
                cargaCts = cts;
                IsBusy = true;

                ApiResult<BitacoraDetalleItem> resultado =
                    await apiService.ObtenerAsync(
                        bitacoraId,
                        cts.Token);

                if (!resultado.Success || resultado.Data == null)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible cargar el detalle de bitácora."
                            : resultado.Message);
                    return;
                }

                Registro = resultado.Data;
                cargado = true;
            }
            catch (OperationCanceledException)
                when (cts?.IsCancellationRequested == true)
            {
                // La página se abandonó durante la carga.
            }
            finally
            {
                if (cts != null && ReferenceEquals(cargaCts, cts))
                    cargaCts = null;

                cts?.Dispose();
                IsBusy = false;
                inicializando = false;
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? cts = cargaCts;
            cargaCts = null;

            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch
            {
            }
        }
    }
}
