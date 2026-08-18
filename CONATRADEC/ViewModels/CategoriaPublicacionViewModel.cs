using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaPublicacionViewModel : GlobalService
    {
        private readonly CategoriaPublicacionApiService
            apiService = new();

        private ObservableCollection<
            CategoriaPublicacionCatalogoResponse>
            categorias = new();

        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargado;
        private long versionAplicada = -1;
        private long generacionCarga;
        private CancellationTokenSource?
            cargaCancellationTokenSource;

        public CategoriaPublicacionViewModel()
        {
            BuscarCommand = new Command(
                async () => await BuscarAsync(),
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
                    item =>
                        !IsBusy &&
                        CanEdit &&
                        item != null);

            DesactivarCommand =
                new Command<CategoriaPublicacionCatalogoResponse>(
                    async item => await DesactivarAsync(item),
                    item =>
                        !IsBusy &&
                        CanDelete &&
                        item?.Activo == true);

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<
            CategoriaPublicacionCatalogoResponse>
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
                string nuevo =
                    value ?? string.Empty;

                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
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
                string nuevo =
                    value ?? string.Empty;

                if (mensaje == nuevo)
                    return;

                mensaje = nuevo;
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

        public bool TieneCategorias =>
            Categorias.Count > 0;

        public bool MostrarVacio =>
            cargado &&
            !TieneCategorias &&
            !IsBusy &&
            !TieneMensaje;

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command NuevoCommand { get; }

        public Command<CategoriaPublicacionCatalogoResponse>
            EditarCommand { get; }

        public Command<CategoriaPublicacionCatalogoResponse>
            DesactivarCommand { get; }

        public Command RegresarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                InterfazCodigos.CategoriasPublicacion);

            ActualizarComandos();
        }

        public async Task IniciarNuevaVisitaAsync()
        {
            /*
             * Una visita nueva nunca reutiliza la búsqueda ni la colección de
             * la visita anterior. El filtro aplicado se separa del texto que el
             * usuario escribe para que escribir por sí solo no cambie la
             * consulta enviada al servidor.
             */
            Interlocked.Increment(
                ref generacionCarga);

            CancelarCarga();

            textoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            cargado = false;
            versionAplicada = -1;
            Categorias =
                new ObservableCollection<
                    CategoriaPublicacionCatalogoResponse>();

            OnPropertyChanged(nameof(TextoBusqueda));
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

        private async Task BuscarAsync()
        {
            if (!CanView || IsBusy)
                return;

            textoBusquedaAplicado =
                TextoBusqueda.Trim();

            await CargarAsync();
        }

        private async Task CargarInternoAsync(
            bool reemplazarCargaActual)
        {
            if (!CanView)
                return;

            if (!reemplazarCargaActual && IsBusy)
                return;

            long generacion =
                Interlocked.Increment(
                    ref generacionCarga);

            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                cargaCancellationTokenSource;

            cargaCancellationTokenSource =
                source;

            if (anterior != null &&
                !ReferenceEquals(anterior, source))
            {
                try
                {
                    anterior.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                ApiResult<ObservableCollection<
                    CategoriaPublicacionCatalogoResponse>> result =
                        await apiService.GetAsync(
                            textoBusquedaAplicado,
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

                foreach (
                    CategoriaPublicacionCatalogoResponse item
                    in result.Data ?? new())
                {
                    /*
                     * La pantalla principal contiene únicamente activos.
                     * Reactivar se realiza exclusivamente desde Eliminados.
                     */
                    item.PuedeDesactivar =
                        CanDelete && item.Activo;

                    item.PuedeReactivar = false;

                    nuevaLista.Add(item);
                }

                Categorias = nuevaLista;
                cargado = true;

                versionAplicada =
                    PublicacionListadoEstadoService
                        .VersionActual;
            }
            catch (OperationCanceledException)
            {
                // La consulta fue cancelada o reemplazada.
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
            CancellationTokenSource? source =
                cargaCancellationTokenSource;

            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void FinalizarVisita()
        {
            Interlocked.Increment(
                ref generacionCarga);

            CancelarCarga();
            cargaCancellationTokenSource = null;

            textoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            cargado = false;
            versionAplicada = -1;
            IsRefreshing = false;
            IsBusy = false;

            Categorias =
                new ObservableCollection<
                    CategoriaPublicacionCatalogoResponse>();

            OnPropertyChanged(nameof(TextoBusqueda));
            NotificarEstadoLista();
        }

        private async Task LimpiarAsync()
        {
            if (!CanView || IsBusy)
                return;

            textoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            OnPropertyChanged(nameof(TextoBusqueda));

            await CargarAsync();
        }

        private async Task RefrescarAsync()
        {
            if (!CanView || IsBusy)
                return;

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
             * El formulario recibe únicamente el ID y obtiene el registro
             * actual desde la API administrativa antes de habilitar Guardar.
             */
            await GoToAsyncParameters(
                AppRoutes.CategoriaPublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["CategoriaId"] =
                        item.CategoriaPublicacionId
                });
        }

        private async Task DesactivarAsync(
            CategoriaPublicacionCatalogoResponse? item)
        {
            if (item == null ||
                !item.Activo ||
                IsBusy)
            {
                return;
            }

            if (!CanDelete)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para desactivar tipos de publicación.");
                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Desactivar tipo de publicación",
                    $"¿Desea desactivar “{item.NombreCategoriaPublicacion}”?",
                    "Desactivar",
                    "Cancelar");

            if (!confirmar)
                return;

            bool eliminado = false;

            try
            {
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.DesactivarAsync(
                        item.CategoriaPublicacionId);

                if (!result.Success)
                {
                    await MostrarErrorAsync(
                        result.Message);
                    return;
                }

                eliminado = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        result.Message)
                        ? "Tipo de publicación desactivado correctamente."
                        : result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "desactivar el tipo de publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
            }

            if (eliminado)
            {
                /*
                 * El servidor vuelve a ser la fuente de verdad después de la
                 * eliminación lógica; no se usa List.Remove como resultado final.
                 */
                await CargarAsync();
            }
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            DesactivarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TieneCategorias));
            OnPropertyChanged(nameof(MostrarVacio));
        }
    }
}
