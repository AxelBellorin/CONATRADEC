using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoFotografiaIAViewModel : GlobalService
    {
        private readonly TipoFotografiaIAApiService api = new();
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private bool inicializado;
        private string mensajeEstado = string.Empty;

        public TipoFotografiaIAViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            NuevoCommand = new Command(
                async () => await NuevoAsync(),
                () => !IsBusy && CanAdd);

            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);

            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy && CanView);

            LimpiarCommand = new Command(
                async () => await LimpiarAsync(),
                () => !IsBusy && CanView);

            EliminadosCommand = new Command(
                async () => await GoToAsyncParameters(
                    TipoFotografiaIARoutes.PaginaEliminados),
                () => !IsBusy && CanView);

            VerCommand = new Command<TipoFotografiaIAItem>(
                async item => await AbrirFormularioAsync(item, "Ver"),
                item => item != null && !IsBusy && CanView);

            EditarCommand = new Command<TipoFotografiaIAItem>(
                async item => await AbrirFormularioAsync(item, "Editar"),
                item => item?.Activo == true && !IsBusy && CanEdit);

            EliminarCommand = new Command<TipoFotografiaIAItem>(
                async item => await EliminarAsync(item),
                item =>
                    item?.PuedeDesactivarse == true &&
                    !IsBusy &&
                    CanDelete);
        }

        public ObservableCollection<TipoFotografiaIAItem> Items { get; } = [];

        public Command RegresarCommand { get; }
        public Command NuevoCommand { get; }
        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command EliminadosCommand { get; }
        public Command<TipoFotografiaIAItem> VerCommand { get; }
        public Command<TipoFotografiaIAItem> EditarCommand { get; }
        public Command<TipoFotografiaIAItem> EliminarCommand { get; }

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
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public bool SinRegistros =>
            inicializado && !IsBusy && Items.Count == 0;

        public bool SinPermisoLectura => !CanView;

        public string Resumen => Items.Count == 1
            ? "1 tipo de fotografía activo"
            : $"{Items.Count} tipos de fotografía activos";

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

        public async Task RecargarVisitaAsync()
        {
            ActualizarPermisos();

            if (CanView)
                await CargarAsync();
        }

        public void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                TipoFotografiaIARoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            CanDelete = permiso?.eliminar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(SinPermisoLectura));
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
            MensajeEstado = "Cargando tipos de fotografía...";
            ActualizarComandos();

            try
            {
                ApiResult<List<TipoFotografiaIAItem>> result =
                    await api.ListarAdministracionV2Async(
                        textoBusquedaAplicado);

                if (!result.Success || result.Data == null)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                Items.Clear();
                foreach (TipoFotografiaIAItem item in result.Data)
                    Items.Add(item);

                OnPropertyChanged(nameof(SinRegistros));
                OnPropertyChanged(nameof(Resumen));
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinRegistros));
                ActualizarComandos();
            }
        }

        private async Task NuevoAsync()
        {
            await GoToAsyncParameters(
                TipoFotografiaIARoutes.PaginaFormulario,
                new Dictionary<string, object>
                {
                    ["Modo"] = "Crear"
                });
        }

        private async Task AbrirFormularioAsync(
            TipoFotografiaIAItem? item,
            string modo)
        {
            if (item == null || IsBusy)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando registro actualizado...";
            ActualizarComandos();

            try
            {
                ApiResult<TipoFotografiaIAItem> result =
                    await api.ObtenerV2Async(item.TipoFotografiaIAId);

                if (!result.Success || result.Data == null)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                if (string.Equals(modo, "Editar", StringComparison.OrdinalIgnoreCase) &&
                    !result.Data.Activo)
                {
                    await MostrarErrorAsync(
                        "El registro fue desactivado por otro usuario. Actualice el listado antes de editarlo.");
                    return;
                }

                IsBusy = false;
                MensajeEstado = string.Empty;
                ActualizarComandos();

                await GoToAsyncParameters(
                    TipoFotografiaIARoutes.PaginaFormulario,
                    new Dictionary<string, object>
                    {
                        ["Modo"] = modo,
                        ["Item"] = result.Data
                    });
            }
            finally
            {
                IsBusy = false;
                MensajeEstado = string.Empty;
                ActualizarComandos();
            }
        }

        private async Task EliminarAsync(TipoFotografiaIAItem? item)
        {
            if (item == null)
                return;

            bool confirmar = await ConfirmarEliminacionAsync(
                item.NombreMostrar);

            if (!confirmar)
                return;

            bool recargar = false;
            IsBusy = true;
            MensajeEstado = "Desactivando tipo de fotografía...";
            ActualizarComandos();

            try
            {
                ApiResult<bool> result =
                    await api.EliminarV2Async(
                        item.TipoFotografiaIAId,
                        item.RowVersion);

                if (!result.Success)
                {
                    if (result.StatusCode == 409)
                    {
                        recargar = true;
                        await GlobalService.MostrarAdvertenciaAsync(
                            string.IsNullOrWhiteSpace(result.Message)
                                ? "El registro cambió en el servidor. Se actualizará el listado."
                                : result.Message);
                    }
                    else
                    {
                        await MostrarErrorAsync(result.Message);
                    }
                }
                else
                {
                    recargar = true;
                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "Tipo de fotografía desactivado correctamente."
                            : result.Message);
                }
            }
            finally
            {
                IsBusy = false;
                MensajeEstado = string.Empty;
                ActualizarComandos();
            }

            if (recargar)
                await CargarAsync();
        }

        private static Task MostrarErrorAsync(string mensaje) =>
            GlobalService.MostrarErrorAsync(
                string.IsNullOrWhiteSpace(mensaje)
                    ? "No fue posible completar la operación."
                    : mensaje);

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            EliminadosCommand.ChangeCanExecute();
            VerCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
        }
    }
}
