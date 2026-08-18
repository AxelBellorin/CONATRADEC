using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaPublicacionViewModel : GlobalService
    {
        private readonly CategoriaPublicacionApiService apiService = new();
        private ObservableCollection<CategoriaPublicacionCatalogoResponse>
            categorias = new();
        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool incluirInactivas;
        private bool isRefreshing;
        private bool cargado;
        private long versionAplicada = -1;
        private long generacionCarga;
        private CancellationTokenSource? cargaCancellationTokenSource;

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

            EditarCommand =
                new Command<CategoriaPublicacionCatalogoResponse>(
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
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar),
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

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                ActualizarComandos();
                NotificarEstadoLista();
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
                if (isRefreshing == value)
                    return;

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

        public async Task IniciarNuevaVisitaAsync()
        {
            /*
             * Una visita nueva no conserva el filtro ni el resultado de la
             * anterior. La consulta se reemplaza de forma determinista si una
             * carga antigua todavía estuviera terminando en segundo plano.
             */
            textoBusqueda = string.Empty;
            incluirInactivas = false;
            Mensaje = string.Empty;
            cargado = false;
            versionAplicada = -1;
            Categorias = new ObservableCollection<
                CategoriaPublicacionCatalogoResponse>();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(IncluirInactivas));
            NotificarEstadoLista();

            await CargarInternoAsync(
                reemplazarCargaActual: true);
        }

        public async Task InicializarAsync()
        {
            if (!CanView)
                return;

            if (cargado &&
                !PublicacionListadoEstadoService
                    .HayCambiosDesde(versionAplicada))
            {
                return;
            }

            await CargarAsync();
        }

        public Task CargarAsync() =>
            CargarInternoAsync(
                reemplazarCargaActual: false);

        private async Task CargarInternoAsync(
            bool reemplazarCargaActual)
        {
            if (!CanView)
                return;

            if (!reemplazarCargaActual && IsBusy)
                return;

            long generacion = Interlocked.Increment(
                ref generacionCarga);

            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                cargaCancellationTokenSource;

            cargaCancellationTokenSource = source;

            if (anterior != null &&
                !ReferenceEquals(anterior, source))
            {
                anterior.Cancel();
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                ApiResult<ObservableCollection<
                    CategoriaPublicacionCatalogoResponse>> result =
                        await apiService.GetAsync(
                            IncluirInactivas,
                            TextoBusqueda,
                            source.Token);

                if (source.IsCancellationRequested ||
                    generacion != Interlocked.Read(
                        ref generacionCarga))
                {
                    return;
                }

                if (!result.Success)
                {
                    if (!string.Equals(
                            result.Message,
                            "La operación fue cancelada.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Mensaje = result.Message;
                    }

                    return;
                }

                var nuevaLista =
                    new ObservableCollection<
                        CategoriaPublicacionCatalogoResponse>();

                foreach (CategoriaPublicacionCatalogoResponse item
                         in result.Data ?? new())
                {
                    item.PuedeDesactivar = CanDelete && item.Activo;
                    item.PuedeReactivar = CanEdit && !item.Activo;
                    nuevaLista.Add(item);
                }

                Categorias = nuevaLista;
                cargado = true;
                versionAplicada =
                    PublicacionListadoEstadoService.VersionActual;
            }
            catch (OperationCanceledException)
            {
                // La consulta fue cancelada o reemplazada por una más reciente.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    generacion == Interlocked.Read(
                        ref generacionCarga))
                {
                    Mensaje =
                        "No fue posible cargar los tipos de publicación.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los tipos de publicación",
                        ex);
                }
            }
            finally
            {
                bool esCargaActual =
                    generacion == Interlocked.Read(
                        ref generacionCarga);

                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        source))
                {
                    cargaCancellationTokenSource = null;
                }

                source.Dispose();

                if (esCargaActual)
                {
                    IsBusy = false;
                    IsRefreshing = false;
                    ActualizarComandos();
                    NotificarEstadoLista();
                }
            }
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
        }

        public void FinalizarVisita()
        {
            Interlocked.Increment(ref generacionCarga);
            CancelarCarga();

            cargaCancellationTokenSource = null;
            cargado = false;
            versionAplicada = -1;
            IsRefreshing = false;
            IsBusy = false;
        }

        private async Task LimpiarAsync()
        {
            textoBusqueda = string.Empty;
            incluirInactivas = false;

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(IncluirInactivas));

            await CargarAsync();
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
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
                    ["CategoriaId"] = 0
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

            /*
             * El formulario recibe únicamente el identificador. De esa forma
             * obtiene el registro fresco desde la API y no edita una copia
             * potencialmente desactualizada de la tarjeta del listado.
             */
            await GoToAsyncParameters(
                AppRoutes.CategoriaPublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["CategoriaId"] =
                        item.CategoriaPublicacionId
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
