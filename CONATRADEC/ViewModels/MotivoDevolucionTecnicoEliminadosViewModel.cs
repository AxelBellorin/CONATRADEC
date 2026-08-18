using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class MotivoDevolucionTecnicoEliminadosViewModel : GlobalService
    {
        private readonly MotivoDevolucionTecnicoApiService api = new();
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensajeEstado = string.Empty;
        private bool inicializado;

        public MotivoDevolucionTecnicoEliminadosViewModel()
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

            RecuperarCommand = new Command<MotivoDevolucionTecnicoItem>(
                async item => await RecuperarAsync(item),
                item => item?.Inactivo == true && !IsBusy && CanEdit);
        }

        public ObservableCollection<MotivoDevolucionTecnicoItem> Items { get; } = [];

        public Command RegresarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<MotivoDevolucionTecnicoItem> RecuperarCommand { get; }

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
                string nuevo = value ?? string.Empty;
                if (mensajeEstado == nuevo)
                    return;

                mensajeEstado = nuevo;
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

        public string Resumen => Items.Count == 1
            ? "1 motivo eliminado"
            : $"{Items.Count} motivos eliminados";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            inicializado = true;

            if (CanView)
                await CargarAsync();
            else
                NotificarLista();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                MotivoDevolucionTecnicoRoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanEdit = permiso?.actualizar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(SinPermisoLectura));
            NotificarLista();
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
            MensajeEstado = "Cargando motivos eliminados...";
            ActualizarComandos();
            NotificarLista();

            try
            {
                ApiResult<List<MotivoDevolucionTecnicoItem>> resultado =
                    await api.ListarEliminadosV2Async(
                        textoBusquedaAplicado);

                if (!resultado.Success || resultado.Data == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                Items.Clear();
                foreach (MotivoDevolucionTecnicoItem item in resultado.Data)
                    Items.Add(item);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarLista();
                ActualizarComandos();
            }
        }

        private async Task RecuperarAsync(
            MotivoDevolucionTecnicoItem? item)
        {
            if (item == null || !CanEdit || IsBusy)
                return;

            bool confirmar = await ConfirmarAsync(
                "Recuperar motivo",
                $"¿Desea recuperar «{item.NombreMostrar}»? Volverá a estar disponible para nuevas devoluciones.",
                "Recuperar",
                "Cancelar");

            if (!confirmar)
                return;

            bool recargar = false;
            IsBusy = true;
            MensajeEstado = "Recuperando motivo...";
            ActualizarComandos();

            try
            {
                ApiResult<bool> resultado =
                    await api.RecuperarV2Async(
                        item.MotivoDevolucionTecnicoId,
                        item.RowVersion);

                if (!resultado.Success)
                {
                    if (resultado.StatusCode == 409)
                    {
                        recargar = true;
                        await MostrarAdvertenciaAsync(
                            string.IsNullOrWhiteSpace(resultado.Message)
                                ? "El motivo cambió en el servidor. Se actualizará la lista de eliminados."
                                : resultado.Message);
                    }
                    else
                    {
                        await MostrarErrorAsync(resultado.Message);
                    }

                }
                else
                {
                    recargar = true;
                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Motivo recuperado correctamente."
                            : resultado.Message);
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

        private void NotificarLista()
        {
            OnPropertyChanged(nameof(SinRegistros));
            OnPropertyChanged(nameof(Resumen));
        }

        private static Task MostrarErrorAsync(string mensaje) =>
            GlobalService.MostrarErrorAsync(
                string.IsNullOrWhiteSpace(mensaje)
                    ? "No fue posible completar la operación."
                    : mensaje);

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
