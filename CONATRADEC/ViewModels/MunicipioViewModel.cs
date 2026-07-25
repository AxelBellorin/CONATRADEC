using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class MunicipioViewModel : GlobalService
    {
        private readonly MunicipioApiService municipioApiService;
        private CancellationTokenSource? cargaCts;

        private DepartamentoRequest departamentoRequest = new();
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
        private int departamentoCargadoId;
        private int versionAplicada = -1;

        public MunicipioViewModel()
            : this(new MunicipioApiService())
        {
        }

        public MunicipioViewModel(MunicipioApiService municipioApiService)
        {
            this.municipioApiService = municipioApiService
                ?? throw new ArgumentNullException(nameof(municipioApiService));

            ReturnCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RegresarADepartamentosAsync,
                    "regresar a departamentos"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de municipio"),
                () => CanAdd && UbicacionValida && !IsBusy && !Navegando);

            EditCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnEditAsync(municipio),
                    "editar el municipio"),
                municipio =>
                    municipio != null && CanEdit && !IsBusy && !Navegando);

            DeleteCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(municipio),
                    "eliminar el municipio"),
                municipio =>
                    municipio != null && CanDelete && !IsBusy && !Navegando);

            ViewCommand = new Command<MunicipioResponse>(
                async municipio => await EjecutarSeguroAsync(
                    () => OnViewAsync(municipio),
                    "consultar el municipio"),
                municipio =>
                    municipio != null && CanView && !IsBusy && !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: true),
                    "buscar municipios"),
                () => CanView && UbicacionValida && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && UbicacionValida && !IsBusy && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los municipios"),
                () => CanView && UbicacionValida && !IsBusy && !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(reiniciar: false),
                    "cargar más municipios"),
                () =>
                    CanView &&
                    UbicacionValida &&
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas);
        }

        public ObservableCollection<MunicipioResponse> List { get; } = new();

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command<MunicipioResponse> EditCommand { get; }
        public Command<MunicipioResponse> DeleteCommand { get; }
        public Command<MunicipioResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                DepartamentoRequest nuevoValor = value ?? new DepartamentoRequest();
                int idAnterior = departamentoRequest.DepartamentoId ?? 0;

                departamentoRequest = nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreDepartamento));
                OnPropertyChanged(nameof(DepartamentoValido));
                OnPropertyChanged(nameof(UbicacionValida));
                OnPropertyChanged(nameof(TitlePage));

                int idActual = departamentoRequest.DepartamentoId ?? 0;

                if (idAnterior != idActual)
                {
                    CancelarCarga();
                    ReiniciarEstado();
                }

                ActualizarComandos();
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                PaisRequest nuevoValor = value ?? new PaisRequest();
                int idAnterior = paisRequest.PaisId;

                paisRequest = nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombrePais));
                OnPropertyChanged(nameof(CodigoPais));
                OnPropertyChanged(nameof(MostrarCodigoPais));
                OnPropertyChanged(nameof(PaisValido));
                OnPropertyChanged(nameof(UbicacionValida));
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
                ? $"Municipios de {NombreDepartamento}"
                : titlePage;
            set
            {
                titlePage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NombreDepartamento =>
            string.IsNullOrWhiteSpace(DepartamentoRequest.NombreDepartamento)
                ? "Departamento seleccionado"
                : DepartamentoRequest.NombreDepartamento;

        public string NombrePais =>
            string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                ? "País seleccionado"
                : PaisRequest.NombrePais;

        public string CodigoPais => PaisRequest.CodigoISOPais ?? string.Empty;
        public bool MostrarCodigoPais => !string.IsNullOrWhiteSpace(CodigoPais);
        public bool DepartamentoValido => DepartamentoRequest.DepartamentoId is > 0;
        public bool PaisValido => PaisRequest.PaisId > 0;
        public bool UbicacionValida => DepartamentoValido && PaisValido;

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

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

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
                ? "1 municipio encontrado"
                : $"{TotalRegistros} municipios encontrados";

        public bool PuedeCargarMas => paginaActual < totalPaginas;

        public bool MostrarVacio =>
            CanView &&
            UbicacionValida &&
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

        public bool MostrarAccesoDenegado => !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("municipioPage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanView || !UbicacionValida || Navegando)
                return;

            int departamentoId = DepartamentoRequest.DepartamentoId!.Value;
            int versionActual =
                MunicipioListadoEstadoService.ObtenerVersion(departamentoId);

            bool debeRecargar =
                !pantallaCargada ||
                departamentoCargadoId != departamentoId ||
                versionAplicada != versionActual ||
                forzarRecargaAlAparecer;

            if (!debeRecargar)
                return;

            forzarRecargaAlAparecer = false;
            await CargarAsync(reiniciar: true);
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || !UbicacionValida || Navegando)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar && (CargandoMas || !PuedeCargarMas))
                return;

            int departamentoId = DepartamentoRequest.DepartamentoId!.Value;
            CancellationTokenSource source = PrepararNuevaCarga();

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

                int paginaSolicitada = reiniciar ? 1 : paginaActual + 1;

                ApiResult<MunicipioPaginaResponse> resultado =
                    await municipioApiService.BuscarMunicipiosAsync(
                        departamentoId,
                        TextoBusqueda,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source) ||
                    DepartamentoRequest.DepartamentoId != departamentoId)
                {
                    return;
                }

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                AplicarPagina(resultado.Data, reiniciar);

                pantallaCargada = true;
                departamentoCargadoId = departamentoId;
                versionAplicada =
                    MunicipioListadoEstadoService.ObtenerVersion(departamentoId);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o cambiar de ubicación.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras la pantalla se cerraba.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested && EsCargaActual(source))
                {
                    Mensaje = "No fue posible cargar los municipios.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los municipios",
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
                Interlocked.Exchange(ref cargaCts, null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
        }

        private void AplicarPagina(
            MunicipioPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales = List
                .Where(item => item.MunicipioId.HasValue)
                .Select(item => item.MunicipioId!.Value)
                .ToHashSet();

            foreach (MunicipioResponse municipio in pagina.Items)
            {
                if (!municipio.MunicipioId.HasValue)
                    continue;

                if (idsActuales.Add(municipio.MunicipioId.Value))
                    List.Add(municipio);
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

        private Task RegresarADepartamentosAsync()
        {
            var parametros = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                { "TitlePage", $"Departamentos de {NombrePais}" }
            };

            return NavegarAsync("//DepartamentoPage", parametros);
        }

        private Task OnAddAsync()
        {
            if (!CanAdd || !UbicacionValida)
                return Task.CompletedTask;

            forzarRecargaAlAparecer = true;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },
                    { "Pais", PaisRequest },
                    { "Departamento", DepartamentoRequest },
                    {
                        "Municipio",
                        new MunicipioRequest
                        {
                            DepartamentoId = DepartamentoRequest.DepartamentoId
                        }
                    }
                });
        }

        private Task OnEditAsync(MunicipioResponse? municipio)
        {
            if (!CanEdit || municipio == null)
                return Task.CompletedTask;

            forzarRecargaAlAparecer = true;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Pais", PaisRequest },
                    { "Departamento", DepartamentoRequest },
                    { "Municipio", new MunicipioRequest(municipio) }
                });
        }

        private Task OnViewAsync(MunicipioResponse? municipio)
        {
            if (!CanView || municipio == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//MunicipioFormPage",
                new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.View },
                    { "Pais", PaisRequest },
                    { "Departamento", DepartamentoRequest },
                    { "Municipio", new MunicipioRequest(municipio) }
                });
        }

        private async Task OnDeleteAsync(MunicipioResponse? municipio)
        {
            if (!CanDelete || municipio == null || IsBusy)
                return;

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar municipio",
                $"¿Desea eliminar el municipio '{municipio.NombreMunicipio}'?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                var request = new MunicipioRequest(municipio)
                {
                    DepartamentoId = DepartamentoRequest.DepartamentoId
                };

                ApiResult<bool> resultado =
                    await municipioApiService.DeleteMunicipioResultAsync(request);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(municipio);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);

                int departamentoId = DepartamentoRequest.DepartamentoId!.Value;

                versionAplicada =
                    MunicipioListadoEstadoService.MarcarCambio(departamentoId);

                // La tarjeta de Departamento muestra el número de municipios.
                DepartamentoListadoEstadoService.MarcarCambio(PaisRequest.PaisId);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Municipio eliminado correctamente."
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
                    await GoToAsyncParameters(ruta);
                else
                    await GoToAsyncParameters(ruta, parametros);
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
                await MostrarErrorInesperadoAsync(descripcion, ex);
            }
        }

        private void ReiniciarEstado()
        {
            List.Clear();
            pantallaCargada = false;
            forzarRecargaAlAparecer = false;
            paginaActual = 0;
            totalPaginas = 1;
            departamentoCargadoId = 0;
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
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI ? 40 : 20;

        private CancellationTokenSource PrepararNuevaCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref cargaCts), source);

        private void LiberarCarga(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref cargaCts, null, source);
            source.Dispose();
        }

        private static void CancelarSeguro(CancellationTokenSource? source)
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

        private static bool EsMensajeCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    }
}
