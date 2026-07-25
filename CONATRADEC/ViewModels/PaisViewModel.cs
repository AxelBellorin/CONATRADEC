using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class PaisViewModel : GlobalService
    {
        private readonly PaisApiService paisApiService;
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

        public PaisViewModel()
            : this(new PaisApiService())
        {
        }

        public PaisViewModel(PaisApiService paisApiService)
        {
            this.paisApiService = paisApiService
                ?? throw new ArgumentNullException(
                    nameof(paisApiService));

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarConfiguracionAsync,
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de país"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnEditAsync(pais),
                    "editar el país"),
                pais =>
                    pais != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(pais),
                    "eliminar el país"),
                pais =>
                    pais != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<PaisResponse>(
                async pais => await EjecutarSeguroAsync(
                    () => OnViewAsync(pais),
                    "consultar los departamentos"),
                pais =>
                    pais != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: true),
                    "buscar países"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los países"),
                () => CanView && !IsBusy && !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: false),
                    "cargar más países"),
                () =>
                    CanView &&
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas);
        }

        public ObservableCollection<PaisResponse> List { get; } =
            new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<PaisResponse> EditCommand { get; }
        public Command<PaisResponse> DeleteCommand { get; }
        public Command<PaisResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
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

                cargandoMas = value;
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

                navegando = value;
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

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 país encontrado"
                : $"{TotalRegistros} países encontrados";

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

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
            LoadPagePermissions("paisPage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView || Navegando)
                return;

            int versionActual =
                PaisListadoEstadoService.VersionActual;

            if (pantallaCargada &&
                versionAplicada == versionActual)
            {
                return;
            }

            await CargarAsync(reiniciar: true);

            if (pantallaCargada)
                versionAplicada = versionActual;
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || Navegando)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
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

                ApiResult<PaisPaginaResponse> resultado =
                    await paisApiService.BuscarPaisesAsync(
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
                    if (!EsMensajeCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                AplicarPagina(
                    resultado.Data,
                    reiniciar);

                pantallaCargada = true;
                versionAplicada =
                    PaisListadoEstadoService.VersionActual;
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
                        "No fue posible cargar los países.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los países",
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
            PaisPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List.Select(pais => pais.PaisId)
                    .ToHashSet();

            foreach (PaisResponse pais in pagina.Items)
            {
                if (idsActuales.Add(pais.PaisId))
                    List.Add(pais);
            }

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            TotalRegistros = Math.Max(0, pagina.TotalRegistros);
            Mensaje = string.Empty;

            OnPropertyChanged(nameof(PuedeCargarMas));
            NotificarEstadoLista();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            await CargarAsync(reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarAsync(reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task RegresarConfiguracionAsync() =>
            NavegarAsync(AppRoutes.Configuracion);

        private Task OnAddAsync() =>
            NavegarAsync(
                "//PaisFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Pais",
                        new PaisRequest()
                    }
                });

        private Task OnEditAsync(PaisResponse? pais)
        {
            if (!CanEdit || pais == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//PaisFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Pais",
                        new PaisRequest(pais)
                    }
                });
        }

        private Task OnViewAsync(PaisResponse? pais)
        {
            if (!CanView || pais == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//DepartamentoPage",
                new Dictionary<string, object>
                {
                    {
                        "Pais",
                        new PaisRequest(pais)
                    },
                    {
                        "TitlePage",
                        $"Departamentos de {pais.NombrePais}"
                    }
                });
        }

        private async Task OnDeleteAsync(PaisResponse? pais)
        {
            if (!CanDelete || pais == null || IsBusy)
                return;

            bool confirmar =
                await Application.Current!.MainPage!.DisplayAlert(
                    "Eliminar país",
                    $"¿Desea eliminar el país '{pais.NombrePais}'?",
                    "Eliminar",
                    "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await paisApiService.DeletePaisResultAsync(
                        new PaisRequest(pais));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(pais);
                TotalRegistros = Math.Max(
                    0,
                    TotalRegistros - 1);

                versionAplicada =
                    PaisListadoEstadoService.MarcarCambio();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "País eliminado correctamente."
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

            Navegando = true;

            try
            {
                CancelarCarga();

                if (parametros == null)
                {
                    await GoToAsyncParameters(ruta);
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
                Navegando = false;
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
            DeleteCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararNuevaCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCts),
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
