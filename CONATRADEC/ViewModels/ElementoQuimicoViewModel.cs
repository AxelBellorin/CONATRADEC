using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ElementoQuimicoViewModel : GlobalService
    {
        private readonly ElementoQuimicoApiService elementoApiService;
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

        public ElementoQuimicoViewModel()
            : this(new ElementoQuimicoApiService())
        {
        }

        public ElementoQuimicoViewModel(
            ElementoQuimicoApiService elementoApiService)
        {
            this.elementoApiService = elementoApiService
                ?? throw new ArgumentNullException(
                    nameof(elementoApiService));

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => NavegarAsync(AppRoutes.Configuracion),
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de elemento químico"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnEditAsync(elemento),
                    "editar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanEdit &&
                    !IsBusy &&
                    !Navegando);

            DeleteCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(elemento),
                    "eliminar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanDelete &&
                    !IsBusy &&
                    !Navegando);

            ViewCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await EjecutarSeguroAsync(
                    () => OnViewAsync(elemento),
                    "consultar el elemento químico"),
                elemento =>
                    elemento != null &&
                    CanView &&
                    !IsBusy &&
                    !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(true),
                    "buscar elementos químicos"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los elementos químicos"),
                () => CanView && !IsBusy && !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(false),
                    "cargar más elementos químicos"),
                () =>
                    CanView &&
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas);
        }

        public ObservableCollection<ElementoQuimicoResponse> List { get; } =
            new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<ElementoQuimicoResponse> EditCommand { get; }
        public Command<ElementoQuimicoResponse> DeleteCommand { get; }
        public Command<ElementoQuimicoResponse> ViewCommand { get; }
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
                ? "1 elemento encontrado"
                : $"{TotalRegistros} elementos encontrados";

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
            LoadPagePermissions("elementoQuimicoPage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView || Navegando)
                return;

            int versionActual =
                ElementoQuimicoListadoEstadoService.VersionActual;

            if (pantallaCargada &&
                versionAplicada == versionActual)
            {
                return;
            }

            await CargarAsync(true);
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

                ApiResult<ElementoQuimicoPaginaResponse> resultado =
                    await elementoApiService.BuscarElementosAsync(
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
                    ElementoQuimicoListadoEstadoService.VersionActual;
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
                        "No fue posible cargar los elementos químicos.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los elementos químicos",
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
            ElementoQuimicoPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List
                    .Where(elemento =>
                        elemento.ElementoQuimicosId.HasValue)
                    .Select(elemento =>
                        elemento.ElementoQuimicosId!.Value)
                    .ToHashSet();

            foreach (ElementoQuimicoResponse elemento
                     in pagina.Items)
            {
                if (!elemento.ElementoQuimicosId.HasValue)
                    continue;

                if (idsActuales.Add(
                        elemento.ElementoQuimicosId.Value))
                {
                    List.Add(elemento);
                }
            }

            paginaActual = Math.Max(
                1,
                pagina.PaginaActual);

            totalPaginas = Math.Max(
                1,
                pagina.TotalPaginas);

            TotalRegistros = Math.Max(
                0,
                pagina.TotalRegistros);

            Mensaje = string.Empty;

            OnPropertyChanged(nameof(PuedeCargarMas));
            NotificarEstadoLista();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                "//ElementoQuimicoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "ElementoQuimico",
                        new ElementoQuimicoRequest()
                    }
                });

        private Task OnEditAsync(
            ElementoQuimicoResponse? elemento)
        {
            if (elemento == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//ElementoQuimicoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "ElementoQuimico",
                        new ElementoQuimicoRequest(elemento)
                    }
                });
        }

        private Task OnViewAsync(
            ElementoQuimicoResponse? elemento)
        {
            if (elemento == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//ElementoQuimicoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.View
                    },
                    {
                        "ElementoQuimico",
                        new ElementoQuimicoRequest(elemento)
                    }
                });
        }

        private async Task OnDeleteAsync(
            ElementoQuimicoResponse? elemento)
        {
            if (elemento == null || IsBusy)
                return;

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar elemento químico",
                        $"¿Desea eliminar el elemento '{elemento.NombreElementoQuimico}' ({elemento.SimboloElementoQuimico})?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await elementoApiService
                        .DeleteElementoQuimicoResultAsync(
                            new ElementoQuimicoRequest(elemento));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(elemento);

                TotalRegistros = Math.Max(
                    0,
                    TotalRegistros - 1);

                versionAplicada =
                    ElementoQuimicoListadoEstadoService
                        .MarcarCambio();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Elemento químico eliminado correctamente."
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
            var source =
                new CancellationTokenSource();

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
