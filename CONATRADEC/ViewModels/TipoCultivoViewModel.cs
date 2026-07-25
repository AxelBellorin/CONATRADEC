using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class TipoCultivoViewModel : GlobalService
    {
        private readonly TipoCultivoApiService apiService;
        private CancellationTokenSource? cargaCts;

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoMas;
        private bool navegando;
        private bool pantallaCargada;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int versionAplicada = -1;

        public TipoCultivoViewModel()
            : this(new TipoCultivoApiService())
        {
        }

        public TipoCultivoViewModel(
            TipoCultivoApiService apiService)
        {
            this.apiService =
                apiService
                ?? throw new ArgumentNullException(
                    nameof(apiService));

            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => NavegarAsync(
                            AppRoutes.Configuracion),
                        "regresar a configuración"),
                    () =>
                        !IsBusy &&
                        !Navegando);

            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddAsync,
                        "abrir el formulario de tipo de cultivo"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(item),
                            "editar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(item),
                            "consultar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<TipoCultivoResponse>(
                    async item =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(item),
                            "eliminar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(
                            reiniciar: true),
                        "buscar tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            CargarMasCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(
                            reiniciar: false),
                        "cargar más tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !CargandoMas &&
                        !Navegando &&
                        PuedeCargarMas);
        }

        public ObservableCollection<TipoCultivoResponse>
            List { get; } =
                new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<TipoCultivoResponse> EditCommand { get; }
        public Command<TipoCultivoResponse> ViewCommand { get; }
        public Command<TipoCultivoResponse> DeleteCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor =
                    value ??
                    string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda =
                    nuevoValor;

                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor =
                    value ??
                    string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje =
                    nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(
                Mensaje);

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing =
                    value;

                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public bool CargandoMas
        {
            get => cargandoMas;
            private set
            {
                if (cargandoMas == value)
                    return;

                cargandoMas =
                    value;

                OnPropertyChanged();
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public bool Navegando
        {
            get => navegando;
            private set
            {
                if (navegando == value)
                    return;

                navegando =
                    value;

                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 tipo de cultivo encontrado"
                : $"{TotalRegistros} tipos de cultivo encontrados";

        public bool PuedeCargarMas =>
            paginaActual <
            totalPaginas;

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            !CargandoMas &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarFinLista =>
            CanView &&
            pantallaCargada &&
            List.Count > 0 &&
            !PuedeCargarMas &&
            !IsBusy &&
            !CargandoMas;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                "tipoCultivoPage");

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            NotificarEstadoLista();
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            int versionActual =
                TipoCultivoListadoEstadoService
                    .VersionActual;

            if (pantallaCargada &&
                versionAplicada ==
                    versionActual)
            {
                return;
            }

            await CargarAsync(
                reiniciar: true);
        }

        public async Task CargarAsync(
            bool reiniciar)
        {
            if (!CanView ||
                Navegando)
            {
                return;
            }

            if (reiniciar &&
                IsBusy)
            {
                return;
            }

            if (!reiniciar &&
                (CargandoMas ||
                 !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                if (reiniciar)
                {
                    IsBusy = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int paginaSolicitada =
                    reiniciar
                        ? 1
                        : paginaActual + 1;

                ApiResult<TipoCultivoPaginaResponse>
                    resultado =
                        await apiService
                            .BuscarAsync(
                                TextoBusqueda,
                                paginaSolicitada,
                                ObtenerTamanoPagina(),
                                source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(
                            resultado.Message))
                    {
                        Mensaje =
                            resultado.Message;
                    }

                    return;
                }

                AplicarPagina(
                    resultado.Data,
                    reiniciar);

                pantallaCargada =
                    true;

                versionAplicada =
                    TipoCultivoListadoEstadoService
                        .VersionActual;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar la búsqueda.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras se abandonaba la pantalla.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar los tipos de cultivo.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los tipos de cultivo",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    if (reiniciar)
                    {
                        IsBusy = false;
                        IsRefreshing = false;
                    }
                    else
                    {
                        CargandoMas = false;
                    }
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
        }

        private void AplicarPagina(
            TipoCultivoPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List
                    .Select(item =>
                        item.TipoCultivoId)
                    .ToHashSet();

            foreach (TipoCultivoResponse item
                     in pagina.Items)
            {
                if (item.TipoCultivoId <= 0)
                    continue;

                if (idsActuales.Add(
                        item.TipoCultivoId))
                {
                    List.Add(item);
                }
            }

            paginaActual =
                Math.Max(
                    1,
                    pagina.PaginaActual);

            totalPaginas =
                Math.Max(
                    1,
                    pagina.TotalPaginas);

            TotalRegistros =
                Math.Max(
                    0,
                    pagina.TotalRegistros);

            Mensaje =
                string.Empty;

            OnPropertyChanged(
                nameof(PuedeCargarMas));

            NotificarEstadoLista();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda =
                string.Empty;

            await CargarAsync(
                reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing =
                true;

            try
            {
                await CargarAsync(
                    reiniciar: true);
            }
            finally
            {
                IsRefreshing =
                    false;
            }
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Item",
                        new TipoCultivoRequest()
                    }
                });

        private Task OnEditAsync(
            TipoCultivoResponse? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Item",
                        new TipoCultivoRequest(item)
                    }
                });
        }

        private Task OnViewAsync(
            TipoCultivoResponse? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.View
                    },
                    {
                        "Item",
                        new TipoCultivoRequest(item)
                    }
                });
        }

        private async Task OnDeleteAsync(
            TipoCultivoResponse? item)
        {
            if (item == null ||
                IsBusy)
            {
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar tipo de cultivo",
                        $"¿Desea eliminar el tipo de cultivo '{item.NombreMostrar}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await apiService
                        .DeleteAsync(
                            item.TipoCultivoId);

                if (!resultado.Success)
                {
                    if (resultado.StatusCode == 409)
                    {
                        await Application.Current!
                            .MainPage!
                            .DisplayAlert(
                                "No se puede eliminar",
                                resultado.Message,
                                "Aceptar");
                    }
                    else
                    {
                        await MostrarToastAsync(
                            resultado.Message);
                    }

                    return;
                }

                List.Remove(item);

                TotalRegistros =
                    Math.Max(
                        0,
                        TotalRegistros - 1);

                versionAplicada =
                    TipoCultivoListadoEstadoService
                        .MarcarCambio();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Tipo de cultivo eliminado correctamente."
                            : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando =
                true;

            try
            {
                CancelarCarga();

                if (parametros == null)
                {
                    await GoToAsyncParameters(
                        ruta);
                }
                else
                {
                    await GoToAsyncParameters(
                        ruta,
                        parametros);
                }
            }
            finally
            {
                Navegando =
                    false;
            }
        }

        private async Task EjecutarSeguroAsync(
            Func<Task> accion,
            string descripcion)
        {
            try
            {
                await accion();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    descripcion,
                    ex);
            }
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(
                nameof(MostrarVacio));

            OnPropertyChanged(
                nameof(MostrarFinLista));

            OnPropertyChanged(
                nameof(PuedeCargarMas));

            OnPropertyChanged(
                nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform ==
            DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource
            PrepararNuevaCarga()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    source);

            CancelarSeguro(
                anterior);

            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(
                    ref cargaCts),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCts,
                null,
                source);

            source.Dispose();
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // La solicitud ya había terminado.
            }
        }

        private static bool EsMensajeCancelacion(
            string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
