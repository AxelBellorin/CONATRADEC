using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoFotografiaIAViewModel : GlobalService
    {
        private readonly TipoFotografiaIAApiService api = new();
        private string textoBusqueda = string.Empty;
        private bool mostrarInactivos;
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
                async () => await CargarAsync(),
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
                    item?.Activo == true &&
                    !string.Equals(
                        item.Codigo,
                        "EVIDENCIA",
                        StringComparison.OrdinalIgnoreCase) &&
                    !IsBusy &&
                    CanDelete);

            RecuperarCommand = new Command<TipoFotografiaIAItem>(
                async item => await RecuperarAsync(item),
                item => item?.Inactivo == true && !IsBusy && CanEdit);
        }

        public ObservableCollection<TipoFotografiaIAItem> Items { get; } = [];

        public Command RegresarCommand { get; }
        public Command NuevoCommand { get; }
        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command<TipoFotografiaIAItem> VerCommand { get; }
        public Command<TipoFotografiaIAItem> EditarCommand { get; }
        public Command<TipoFotografiaIAItem> EliminarCommand { get; }
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

        public bool MostrarInactivos
        {
            get => mostrarInactivos;
            set
            {
                if (mostrarInactivos == value)
                    return;

                mostrarInactivos = value;
                OnPropertyChanged();

                if (inicializado && !IsBusy)
                    _ = CargarAsync();
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

        public string Resumen => Items.Count == 1
            ? "1 tipo de fotografía"
            : $"{Items.Count} tipos de fotografía";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            inicializado = true;

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
            ActualizarComandos();
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
                    await api.ListarAdministracionAsync(
                        MostrarInactivos,
                        TextoBusqueda);

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
            if (item == null)
                return;

            await GoToAsyncParameters(
                TipoFotografiaIARoutes.PaginaFormulario,
                new Dictionary<string, object>
                {
                    ["Modo"] = modo,
                    ["Item"] = item
                });
        }

        private async Task EliminarAsync(TipoFotografiaIAItem? item)
        {
            if (item == null)
                return;

            bool confirmar = await ConfirmarEliminacionAsync(
                item.NombreMostrar);

            if (!confirmar)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                ApiResult<bool> result =
                    await api.EliminarAsync(item.TipoFotografiaIAId);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Tipo de fotografía desactivado correctamente."
                        : result.Message);

                await CargarDespuesDeOperacionAsync();
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task RecuperarAsync(TipoFotografiaIAItem? item)
        {
            if (item == null)
                return;

            bool confirmar = await ConfirmarAsync(
                "Recuperar tipo de fotografía",
                $"¿Desea recuperar {item.NombreMostrar}?",
                "Recuperar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                ApiResult<bool> result =
                    await api.RecuperarAsync(item.TipoFotografiaIAId);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Tipo de fotografía recuperado correctamente."
                        : result.Message);

                await CargarDespuesDeOperacionAsync();
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task CargarDespuesDeOperacionAsync()
        {
            IsBusy = false;
            await CargarAsync();
            IsBusy = true;
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
            VerCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            RecuperarCommand.ChangeCanExecute();
        }
    }
}
