using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class PublicacionesAdminViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();

        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string estadoSeleccionado = "TODOS";
        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargado;

        public PublicacionesAdminViewModel()
        {
            Categorias = new ObservableCollection<
                CategoriaPublicacionResponse>();

            Estados = new ObservableCollection<string>
            {
                "TODOS",
                "BORRADOR",
                "PUBLICADA",
                "PROGRAMADA",
                "VENCIDA",
                "ARCHIVADA"
            };

            Publicaciones = new ObservableCollection<
                PublicacionListadoResponse>();

            BuscarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanAdministrar);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && CanAdministrar);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => !IsBusy && CanAdministrar);

            NuevaCommand = new Command(
                async () => await NuevaAsync(),
                () => !IsBusy && CanAdd);

            EditarCommand = new Command<PublicacionListadoResponse>(
                async item => await EditarAsync(item),
                item => !IsBusy && CanEdit && item != null);

            CambiarEstadoCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await CambiarEstadoAsync(item),
                    item => !IsBusy && CanEdit && item != null);

            CambiarDestacadaCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await CambiarDestacadaAsync(item),
                    item => !IsBusy && CanEdit && item != null);

            EliminarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EliminarAsync(item),
                    item => !IsBusy && CanDelete && item != null);

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<CategoriaPublicacionResponse>
            Categorias { get; }

        public ObservableCollection<string> Estados { get; }

        public ObservableCollection<PublicacionListadoResponse>
            Publicaciones { get; }

        public CategoriaPublicacionResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                estadoSeleccionado = string.IsNullOrWhiteSpace(value)
                    ? "TODOS"
                    : value;

                OnPropertyChanged();
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

        public bool TienePublicaciones =>
            Publicaciones.Count > 0;

        public bool MostrarVacio =>
            cargado && !TienePublicaciones && !IsBusy;

        public bool CanAdministrar =>
            CanAdd || CanEdit || CanDelete;

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command NuevaCommand { get; }
        public Command<PublicacionListadoResponse> EditarCommand { get; }
        public Command<PublicacionListadoResponse> CambiarEstadoCommand { get; }
        public Command<PublicacionListadoResponse> CambiarDestacadaCommand { get; }
        public Command<PublicacionListadoResponse> EliminarCommand { get; }
        public Command RegresarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(CanAdministrar));
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanAdministrar)
                return;

            if (!cargado ||
                PublicacionListadoEstadoService.HayActualizacionPendiente)
            {
                await CargarAsync();
                PublicacionListadoEstadoService.ConfirmarActualizacion();
            }
        }

        public async Task CargarAsync()
        {
            if (!CanAdministrar || IsBusy)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                if (Categorias.Count == 0)
                    await CargarCategoriasAsync();

                ApiResult<PublicacionPaginadaResponse> result =
                    await apiService.GetAdministracionAsync(
                        CategoriaSeleccionada?.CategoriaPublicacionId,
                        EstadoSeleccionado,
                        TextoBusqueda,
                        1,
                        50);

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                Publicaciones.Clear();

                foreach (PublicacionListadoResponse item
                         in result.Data.Items)
                {
                    Publicaciones.Add(item);
                }

                cargado = true;
                NotificarLista();
            }
            catch (Exception ex)
            {
                Mensaje = "No fue posible cargar las publicaciones.";
                await MostrarErrorInesperadoAsync(
                    "cargar las publicaciones",
                    ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                ActualizarComandos();
                NotificarLista();
            }
        }

        private async Task CargarCategoriasAsync()
        {
            ApiResult<List<CategoriaPublicacionResponse>> result =
                await apiService.GetCategoriasAsync();

            if (!result.Success || result.Data == null)
            {
                Mensaje = result.Message;
                return;
            }

            Categorias.Clear();
            CategoriaPublicacionResponse todas =
                CategoriaPublicacionResponse.Todas();

            Categorias.Add(todas);

            foreach (CategoriaPublicacionResponse categoria
                     in result.Data.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada ??= todas;
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            EstadoSeleccionado = "TODOS";
            CategoriaSeleccionada = Categorias.FirstOrDefault();
            await CargarAsync();
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;
            await CargarAsync();
        }

        private async Task NuevaAsync()
        {
            if (!CanAdd)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = 0
                });
        }

        private async Task EditarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private async Task CambiarEstadoAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null)
                return;

            string nuevoEstado = string.Equals(
                item.EstadoPublicacion,
                "PUBLICADA",
                StringComparison.OrdinalIgnoreCase)
                ? "ARCHIVADA"
                : "PUBLICADA";

            bool confirmar = await ConfirmarAsync(
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                nuevoEstado == "PUBLICADA"
                    ? $"¿Desea publicar “{item.Titulo}”?"
                    : $"¿Desea archivar “{item.Titulo}”?",
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                "Cancelar");

            if (!confirmar)
                return;

            ApiResult<bool> result =
                await apiService.CambiarEstadoAsync(
                    item.PublicacionId,
                    nuevoEstado);

            if (!result.Success)
            {
                await MostrarErrorAsync(result.Message);
                return;
            }

            await MostrarExitoAsync(result.Message);
            PublicacionListadoEstadoService.MarcarActualizacion();
            await CargarAsync();
        }

        private async Task CambiarDestacadaAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null)
                return;

            ApiResult<bool> result =
                await apiService.CambiarDestacadaAsync(
                    item.PublicacionId,
                    !item.Destacada);

            if (!result.Success)
            {
                await MostrarErrorAsync(result.Message);
                return;
            }

            await MostrarExitoAsync(result.Message);
            PublicacionListadoEstadoService.MarcarActualizacion();
            await CargarAsync();
        }

        private async Task EliminarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanDelete || item == null)
                return;

            bool confirmar = await ConfirmarEliminacionAsync(
                $"la publicación “{item.Titulo}”");

            if (!confirmar)
                return;

            ApiResult<bool> result =
                await apiService.EliminarAsync(item.PublicacionId);

            if (!result.Success)
            {
                await MostrarErrorAsync(result.Message);
                return;
            }

            await MostrarExitoAsync(result.Message);
            PublicacionListadoEstadoService.MarcarActualizacion();
            await CargarAsync();
        }

        private void NotificarLista()
        {
            OnPropertyChanged(nameof(TienePublicaciones));
            OnPropertyChanged(nameof(MostrarVacio));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            NuevaCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            CambiarEstadoCommand.ChangeCanExecute();
            CambiarDestacadaCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }
    }
}
