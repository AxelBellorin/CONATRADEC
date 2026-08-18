using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoFotografiaIAEliminadosViewModel : GlobalService
    {
        private readonly TipoFotografiaIAApiService api = new();
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensajeEstado = string.Empty;
        private bool inicializado;

        public TipoFotografiaIAEliminadosViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy && CanView);

            LimpiarCommand = new Command(
                async () => await LimpiarAsync(),
                () => !IsBusy && CanView);

            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);

            RecuperarCommand = new Command<TipoFotografiaIAItem>(
                async item => await RecuperarAsync(item),
                item => item?.Inactivo == true && !IsBusy && CanEdit);
        }

        public ObservableCollection<TipoFotografiaIAItem> Items { get; } = [];

        public Command RegresarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<TipoFotografiaIAItem> RecuperarCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevo = value ?? string.Empty;
                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
            }
        }

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                if (mensajeEstado == value)
                    return;

                mensajeEstado = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool SinRegistros =>
            inicializado && CanView && !IsBusy && Items.Count == 0;

        public bool SinPermisoLectura => !CanView;

        public string BusquedaAplicadaTexto =>
            string.IsNullOrWhiteSpace(textoBusquedaAplicado)
                ? "Sin filtro de búsqueda aplicado."
                : $"Filtro aplicado: “{textoBusquedaAplicado}”.";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            inicializado = true;

            if (CanView)
                await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                TipoFotografiaIARoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanEdit = permiso?.actualizar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(SinPermisoLectura));
            OnPropertyChanged(nameof(SinRegistros));
            ActualizarComandos();
        }

        private async Task BuscarAsync()
        {
            textoBusquedaAplicado = (TextoBusqueda ?? string.Empty).Trim();
            OnPropertyChanged(nameof(BusquedaAplicadaTexto));
            await CargarAsync();
        }

        private async Task LimpiarAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            OnPropertyChanged(nameof(BusquedaAplicadaTexto));
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (IsBusy || !CanView)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando tipos eliminados...";
            ActualizarComandos();

            try
            {
                ApiResult<List<TipoFotografiaIAItem>> result =
                    await api.ListarEliminadosV2Async(
                        textoBusquedaAplicado);

                if (!result.Success || result.Data == null)
                {
                    await GlobalService.MostrarErrorAsync(result.Message);
                    return;
                }

                Items.Clear();
                foreach (TipoFotografiaIAItem item in result.Data)
                    Items.Add(item);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinRegistros));
                ActualizarComandos();
            }
        }

        private async Task RecuperarAsync(TipoFotografiaIAItem? item)
        {
            if (item == null || !CanEdit)
                return;

            bool confirmar = await ConfirmarAsync(
                "Recuperar tipo de fotografía",
                $"¿Desea recuperar {item.NombreMostrar}?",
                "Recuperar",
                "Cancelar");

            if (!confirmar)
                return;

            bool recargar = false;
            IsBusy = true;
            MensajeEstado = "Recuperando tipo de fotografía...";
            ActualizarComandos();

            try
            {
                ApiResult<bool> result =
                    await api.RecuperarV2Async(
                        item.TipoFotografiaIAId,
                        item.RowVersion);

                if (!result.Success)
                {
                    if (result.StatusCode == 409)
                    {
                        recargar = true;
                        await GlobalService.MostrarAdvertenciaAsync(
                            string.IsNullOrWhiteSpace(result.Message)
                                ? "El registro cambió en el servidor. Se actualizará la lista de eliminados."
                                : result.Message);
                    }
                    else
                    {
                        await GlobalService.MostrarErrorAsync(result.Message);
                    }
                }
                else
                {
                    recargar = true;
                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "Tipo de fotografía recuperado correctamente."
                            : result.Message);
                }
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }

            if (recargar)
                await CargarAsync();
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            RecuperarCommand.ChangeCanExecute();
        }
    }
}
