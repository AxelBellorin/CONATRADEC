using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaPublicacionViewModel : GlobalService
    {
        private readonly CategoriaPublicacionApiService apiService = new();

        private ObservableCollection<CategoriaPublicacionCatalogoResponse>
            categorias = new();

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool incluirInactivas = true;
        private bool isRefreshing;
        private bool cargado;

        public CategoriaPublicacionViewModel()
        {
            BuscarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);

            LimpiarCommand = new Command(
                async () => await LimpiarAsync(),
                () => !IsBusy && CanView);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => !IsBusy && CanView);

            NuevoCommand = new Command(
                async () => await NuevoAsync(),
                () => !IsBusy && CanAdd);

            EditarCommand = new Command<CategoriaPublicacionCatalogoResponse>(
                async item => await EditarAsync(item),
                item => !IsBusy && CanEdit && item != null);

            DesactivarCommand =
                new Command<CategoriaPublicacionCatalogoResponse>(
                    async item => await CambiarEstadoAsync(item, false),
                    item => !IsBusy && CanDelete && item?.Activo == true);

            ReactivarCommand =
                new Command<CategoriaPublicacionCatalogoResponse>(
                    async item => await CambiarEstadoAsync(item, true),
                    item => !IsBusy && CanEdit && item?.Activo == false);

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<CategoriaPublicacionCatalogoResponse>
            Categorias
        {
            get => categorias;
            private set
            {
                categorias = value;
                OnPropertyChanged();
                NotificarEstadoLista();
            }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IncluirInactivas
        {
            get => incluirInactivas;
            set
            {
                if (incluirInactivas == value)
                    return;

                incluirInactivas = value;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public bool TieneCategorias => Categorias.Count > 0;

        public bool MostrarVacio =>
            cargado && !TieneCategorias && !IsBusy;

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command NuevoCommand { get; }
        public Command<CategoriaPublicacionCatalogoResponse> EditarCommand { get; }
        public Command<CategoriaPublicacionCatalogoResponse> DesactivarCommand { get; }
        public Command<CategoriaPublicacionCatalogoResponse> ReactivarCommand { get; }
        public Command RegresarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("categoriaPublicacionPage");
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView)
                return;

            await CargarAsync();
        }

        public async Task CargarAsync()
        {
            if (!CanView || IsBusy)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                ApiResult<ObservableCollection<
                    CategoriaPublicacionCatalogoResponse>> result =
                    await apiService.GetAsync(
                        IncluirInactivas,
                        TextoBusqueda);

                if (!result.Success)
                {
                    Mensaje = result.Message;
                    return;
                }

                ObservableCollection<CategoriaPublicacionCatalogoResponse>
                    nuevaLista = new();

                foreach (CategoriaPublicacionCatalogoResponse item
                         in result.Data ?? new())
                {
                    item.PuedeDesactivar = CanDelete && item.Activo;
                    item.PuedeReactivar = CanEdit && !item.Activo;
                    nuevaLista.Add(item);
                }

                Categorias = nuevaLista;
                cargado = true;
            }
            catch (Exception ex)
            {
                Mensaje = "No fue posible cargar los tipos de publicación.";
                await MostrarErrorInesperadoAsync(
                    "cargar los tipos de publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private async Task LimpiarAsync()
        {
            TextoBusqueda = string.Empty;
            IncluirInactivas = true;
            await CargarAsync();
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;
            await CargarAsync();
        }

        private async Task NuevoAsync()
        {
            if (!CanAdd)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para crear tipos de publicación.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.CategoriaPublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["Categoria"] = new CategoriaPublicacionCatalogoResponse()
                });
        }

        private async Task EditarAsync(
            CategoriaPublicacionCatalogoResponse? item)
        {
            if (item == null)
                return;

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para editar tipos de publicación.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.CategoriaPublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["Categoria"] = item
                });
        }

        private async Task CambiarEstadoAsync(
            CategoriaPublicacionCatalogoResponse? item,
            bool activo)
        {
            if (item == null || IsBusy)
                return;

            if (activo && !CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para reactivar tipos de publicación.");
                return;
            }

            if (!activo && !CanDelete)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para desactivar tipos de publicación.");
                return;
            }

            string accion = activo ? "reactivar" : "desactivar";

            bool confirmar = await ConfirmarAsync(
                activo
                    ? "Reactivar tipo de publicación"
                    : "Desactivar tipo de publicación",
                $"¿Desea {accion} “{item.NombreCategoriaPublicacion}”?",
                activo ? "Reactivar" : "Desactivar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.CambiarEstadoAsync(
                        item.CategoriaPublicacionId,
                        activo);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(result.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }

            await CargarAsync();
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            DesactivarCommand.ChangeCanExecute();
            ReactivarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TieneCategorias));
            OnPropertyChanged(nameof(MostrarVacio));
        }
    }
}
