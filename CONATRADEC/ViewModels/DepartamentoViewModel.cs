using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class DepartamentoViewModel : GlobalService
    {
        private readonly DepartamentoApiService departamentoApiService;

        private CancellationTokenSource? cargaCts;

        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;

        private bool isRefreshing;
        private bool cargandoMas;
        private bool navegando;
        private bool pantallaCargada;
        private bool forzarRecargaAlAparecer;

        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int paisCargadoId;
        private int versionAplicada = -1;

        public DepartamentoViewModel()
            : this(new DepartamentoApiService())
        {
        }

        public DepartamentoViewModel(
            DepartamentoApiService departamentoApiService)
        {
            this.departamentoApiService =
                departamentoApiService
                ?? throw new ArgumentNullException(
                    nameof(departamentoApiService));

            ReturnCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarAPaisesAsync,
                    "regresar a países"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de departamento"),
                () =>
                    CanAdd &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            EditCommand =
                new Command<DepartamentoResponse>(
                    async departamento =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(departamento),
                            "editar el departamento"),
                    departamento =>
                        departamento != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<DepartamentoResponse>(
                    async departamento =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(departamento),
                            "eliminar el departamento"),
                    departamento =>
                        departamento != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<DepartamentoResponse>(
                    async departamento =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(departamento),
                            "consultar los municipios"),
                    departamento =>
                        departamento != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: true),
                    "buscar departamentos"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los departamentos"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: false),
                    "cargar más departamentos"),
                () =>
                    CanView &&
                    PaisValido &&
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas);
        }

        public ObservableCollection<DepartamentoResponse>
            List { get; } = new();

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command<DepartamentoResponse> EditCommand { get; }
        public Command<DepartamentoResponse> DeleteCommand { get; }
        public Command<DepartamentoResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                PaisRequest nuevoValor =
                    value ?? new PaisRequest();

                int idAnterior =
                    paisRequest.PaisId;

                paisRequest = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombrePais));
                OnPropertyChanged(nameof(CodigoPais));
                OnPropertyChanged(nameof(MostrarCodigoPais));
                OnPropertyChanged(nameof(PaisValido));
                OnPropertyChanged(nameof(TitlePage));

                if (idAnterior != paisRequest.PaisId)
                {
                    CancelarCarga();
                    ReiniciarEstado();
                }

                ActualizarComandos();
            }
        }

        public string TitlePage
        {
            get => string.IsNullOrWhiteSpace(titlePage)
                ? $"Departamentos de {NombrePais}"
                : titlePage;

            set
            {
                titlePage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NombrePais =>
            string.IsNullOrWhiteSpace(
                PaisRequest.NombrePais)
                    ? "País seleccionado"
                    : PaisRequest.NombrePais;

        public string CodigoPais =>
            PaisRequest.CodigoISOPais ??
            string.Empty;

        public bool MostrarCodigoPais =>
            !string.IsNullOrWhiteSpace(CodigoPais);

        public bool PaisValido =>
            PaisRequest.PaisId > 0;

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor =
                    value ?? string.Empty;

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
                string nuevoValor =
                    value ?? string.Empty;

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
                ? "1 departamento encontrado"
                : $"{TotalRegistros} departamentos encontrados";

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool MostrarVacio =>
            CanView &&
            PaisValido &&
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
                "departamentoPage");

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            NotificarEstadoLista();
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView ||
                !PaisValido ||
                Navegando)
            {
                return;
            }

            int paisId =
                PaisRequest.PaisId;

            int versionActual =
                DepartamentoListadoEstadoService
                    .ObtenerVersion(paisId);

            bool debeRecargar =
                !pantallaCargada ||
                paisCargadoId != paisId ||
                versionAplicada != versionActual ||
                forzarRecargaAlAparecer;

            if (!debeRecargar)
                return;

            forzarRecargaAlAparecer = false;

            await CargarAsync(
                reiniciar: true);
        }

        public async Task CargarAsync(
            bool reiniciar)
        {
            if (!CanView ||
                !PaisValido ||
                Navegando)
            {
                return;
            }

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas ||
                 !PuedeCargarMas))
            {
                return;
            }

            int paisId =
                PaisRequest.PaisId;

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

                ApiResult<DepartamentoPaginaResponse>
                    resultado =
                        await departamentoApiService
                            .BuscarDepartamentosAsync(
                                paisId,
                                TextoBusqueda,
                                paginaSolicitada,
                                ObtenerTamanoPagina(),
                                source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source) ||
                    PaisRequest.PaisId != paisId)
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

                pantallaCargada = true;
                paisCargadoId = paisId;

                versionAplicada =
                    DepartamentoListadoEstadoService
                        .ObtenerVersion(paisId);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al cambiar de país o navegar.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras la página se cerraba.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar los departamentos.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los departamentos",
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
            DepartamentoPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List
                    .Where(item =>
                        item.DepartamentoId.HasValue)
                    .Select(item =>
                        item.DepartamentoId!.Value)
                    .ToHashSet();

            foreach (DepartamentoResponse departamento
                     in pagina.Items)
            {
                if (!departamento.DepartamentoId.HasValue)
                    continue;

                if (idsActuales.Add(
                        departamento.DepartamentoId.Value))
                {
                    List.Add(departamento);
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

            Mensaje = string.Empty;

            OnPropertyChanged(
                nameof(PuedeCargarMas));

            NotificarEstadoLista();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;

            await CargarAsync(
                reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarAsync(
                    reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task RegresarAPaisesAsync() =>
            NavegarAsync(
                AppRoutes.Paises);

        private Task OnAddAsync()
        {
            if (!CanAdd ||
                !PaisValido)
            {
                return Task.CompletedTask;
            }

            forzarRecargaAlAparecer = true;

            return NavegarAsync(
                "//DepartamentoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Pais",
                        PaisRequest
                    },
                    {
                        "Departamento",
                        new DepartamentoRequest
                        {
                            PaisId =
                                PaisRequest.PaisId
                        }
                    }
                });
        }

        private Task OnEditAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanEdit ||
                departamento == null)
            {
                return Task.CompletedTask;
            }

            forzarRecargaAlAparecer = true;

            return NavegarAsync(
                "//DepartamentoFormPage",
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Pais",
                        PaisRequest
                    },
                    {
                        "Departamento",
                        new DepartamentoRequest(
                            departamento)
                    }
                });
        }

        private Task OnViewAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanView ||
                departamento == null)
            {
                return Task.CompletedTask;
            }

            /*
             * Al regresar desde Municipios se realiza una única recarga
             * para actualizar el conteo de municipios de las tarjetas.
             */
            forzarRecargaAlAparecer = true;

            return NavegarAsync(
                "//MunicipioPage",
                new Dictionary<string, object>
                {
                    {
                        "Pais",
                        PaisRequest
                    },
                    {
                        "Departamento",
                        new DepartamentoRequest(
                            departamento)
                    },
                    {
                        "TitlePage",
                        $"Municipios de {departamento.NombreDepartamento} - {NombrePais}"
                    }
                });
        }

        private async Task OnDeleteAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanDelete ||
                departamento == null ||
                IsBusy)
            {
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar departamento",
                        $"¿Desea eliminar el departamento '{departamento.NombreDepartamento}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                var request =
                    new DepartamentoRequest(
                        departamento)
                    {
                        PaisId =
                            PaisRequest.PaisId
                    };

                ApiResult<bool> resultado =
                    await departamentoApiService
                        .DeleteDepartamentoResultAsync(
                            request);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        resultado.Message);

                    return;
                }

                List.Remove(departamento);

                TotalRegistros =
                    Math.Max(
                        0,
                        TotalRegistros - 1);

                versionAplicada =
                    DepartamentoListadoEstadoService
                        .MarcarCambio(
                            PaisRequest.PaisId);

                /*
                 * La tarjeta del país muestra la cantidad de departamentos.
                 * Se marca País para que actualice su conteo al regresar.
                 */
                PaisListadoEstadoService.MarcarCambio();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Departamento eliminado correctamente."
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

        private void ReiniciarEstado()
        {
            List.Clear();

            pantallaCargada = false;
            forzarRecargaAlAparecer = false;

            paginaActual = 0;
            totalPaginas = 1;
            paisCargadoId = 0;
            versionAplicada = -1;

            TotalRegistros = 0;
            Mensaje = string.Empty;

            NotificarEstadoLista();
        }

        private void ActualizarComandos()
        {
            ReturnCommand.ChangeCanExecute();
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

            CancelarSeguro(anterior);

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
