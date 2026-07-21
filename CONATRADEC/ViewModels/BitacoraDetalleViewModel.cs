using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public sealed class BitacoraDetalleViewModel : GlobalService
    {
        private readonly BitacoraApiService apiService = new();
        private BitacoraDetalleItem? registro;

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

        public async Task CargarAsync(Guid bitacoraId)
        {
            LoadPagePermissions("bitacoraPage");

            if (!CanView || IsBusy || bitacoraId == Guid.Empty)
                return;

            if (!await ValidarInternetAsync())
                return;

            IsBusy = true;

            try
            {
                ApiResult<BitacoraDetalleItem> resultado =
                    await apiService.ObtenerAsync(bitacoraId);

                if (!resultado.Success || resultado.Data == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                Registro = resultado.Data;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
