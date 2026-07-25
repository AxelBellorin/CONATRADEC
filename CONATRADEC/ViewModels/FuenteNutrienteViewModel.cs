using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class FuenteNutrienteViewModel : GlobalService
    {
        private readonly FuenteNutrienteConsultaApiService
            consultaApiService;

        /*
         * Se conserva el servicio CRUD existente para eliminar.
         * Crear, editar y clasificar continúan usando exactamente
         * la misma lógica del formulario actual.
         */
        private readonly FuenteNutrienteApiService
            fuenteNutrienteApiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? composicionCts;

        private string textoBusqueda =
            string.Empty;

        private string mensaje =
            string.Empty;

        private string mensajeComposicion =
            string.Empty;

        private bool isRefreshing;
        private bool cargandoMas;
        private bool cargandoComposicion;
        private bool navegando;
        private bool pantallaCargada;
        private bool mostrarTablaComposicion;

        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int versionFiltro;

        private FuenteNutrienteCategoriaOption?
            filtroCategoriaSeleccionada;

        public FuenteNutrienteViewModel()
            : this(
                new FuenteNutrienteConsultaApiService(),
                new FuenteNutrienteApiService())
        {
        }

        public FuenteNutrienteViewModel(
            FuenteNutrienteConsultaApiService consultaApiService,
            FuenteNutrienteApiService fuenteNutrienteApiService)
        {
            this.consultaApiService =
                consultaApiService
                ?? throw new ArgumentNullException(
                    nameof(consultaApiService));

            this.fuenteNutrienteApiService =
                fuenteNutrienteApiService
                ?? throw new ArgumentNullException(
                    nameof(fuenteNutrienteApiService));

            CargarFiltrosCategoria();

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
                        "abrir el formulario de fuente"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(fuente),
                            "editar la fuente"),
                    fuente =>
                        fuente != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(fuente),
                            "consultar la fuente"),
                    fuente =>
                        fuente != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(fuente),
                            "eliminar la fuente"),
                    fuente =>
                        fuente != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(
                            reiniciar: true),
                        "buscar fuentes"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar los filtros"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar las fuentes"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            CargarMasCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(
                            reiniciar: false),
                        "cargar más fuentes"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !CargandoMas &&
                        !Navegando &&
                        PuedeCargarMas);

            ToggleTablaComposicionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        ToggleTablaComposicionAsync,
                        "cargar la composición de fuentes"),
                    () =>
                        CanView &&
                        MostrarSeccionTablaComposicion &&
                        !CargandoComposicion);
        }

        public ObservableCollection<FuenteNutrienteResponse>
            List { get; } =
                new();

        public ObservableCollection<
            FuenteNutrienteCategoriaOption>
            FiltrosCategoria { get; } =
                new();

        public ObservableCollection<string>
            ElementosTabla { get; } =
                new();

        public ObservableCollection<
            FuenteNutrienteTablaDinamicaRow>
            TablaComposicion { get; } =
                new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<FuenteNutrienteResponse> EditCommand { get; }
        public Command<FuenteNutrienteResponse> ViewCommand { get; }
        public Command<FuenteNutrienteResponse> DeleteCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }
        public Command ToggleTablaComposicionCommand { get; }

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

                InvalidarComposicion();
            }
        }

        public FuenteNutrienteCategoriaOption?
            FiltroCategoriaSeleccionada
        {
            get => filtroCategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(
                        filtroCategoriaSeleccionada,
                        value))
                {
                    return;
                }

                filtroCategoriaSeleccionada =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(MostrarSeccionTablaComposicion));

                InvalidarComposicion();
                ActualizarComandos();

                if (pantallaCargada)
                    SolicitarRecargaPorFiltro();
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

        public string MensajeComposicion
        {
            get => mensajeComposicion;
            private set
            {
                string nuevoValor =
                    value ??
                    string.Empty;

                if (mensajeComposicion == nuevoValor)
                    return;

                mensajeComposicion =
                    nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneMensajeComposicion));
            }
        }

        public bool TieneMensajeComposicion =>
            !string.IsNullOrWhiteSpace(
                MensajeComposicion);

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

        public bool CargandoComposicion
        {
            get => cargandoComposicion;
            private set
            {
                if (cargandoComposicion == value)
                    return;

                cargandoComposicion =
                    value;

                OnPropertyChanged();
                ActualizarComandos();
                NotificarEstadoComposicion();
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

        public bool MostrarTablaComposicion
        {
            get => mostrarTablaComposicion;
            private set
            {
                if (mostrarTablaComposicion == value)
                    return;

                mostrarTablaComposicion =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TextoBotonTablaComposicion));

                NotificarEstadoComposicion();
            }
        }

        public string TextoBotonTablaComposicion =>
            MostrarTablaComposicion
                ? "Ocultar matriz"
                : "Ver matriz";

        public bool MostrarSeccionTablaComposicion =>
            FiltroCategoriaSeleccionada?.Codigo !=
            FuenteNutrienteCategoriaOption
                .CodigoEnmiendaCalcarea;

        public bool MostrarTablaConDatos =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            !CargandoComposicion &&
            ElementosTabla.Count > 0 &&
            TablaComposicion.Count > 0;

        public bool MostrarMensajeTablaVacia =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            !CargandoComposicion &&
            ElementosTabla.Count == 0 &&
            !TieneMensajeComposicion;

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
                OnPropertyChanged(
                    nameof(ResumenResultados));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 fuente encontrada"
                : $"{TotalRegistros} fuentes encontradas";

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
                "fuenteNutrientePage");

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            NotificarEstadoLista();
            NotificarEstadoComposicion();
            ActualizarComandos();
        }

        /// <summary>
        /// Se recarga la primera página al volver del formulario.
        /// La consulta es paginada y no descarga toda la matriz.
        /// </summary>
        public Task InicializarAsync() =>
            CargarAsync(
                reiniciar: true);

        public async Task CargarAsync(
            bool reiniciar)
        {
            if (!CanView ||
                Navegando)
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
                    IsBusy =
                        true;

                    Mensaje =
                        string.Empty;
                }
                else
                {
                    CargandoMas =
                        true;
                }

                int paginaSolicitada =
                    reiniciar
                        ? 1
                        : paginaActual + 1;

                ApiResult<FuenteNutrientePaginaResponse>
                    resultado =
                        await consultaApiService
                            .BuscarAsync(
                                TextoBusqueda,
                                ObtenerCodigoCategoria(),
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
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar filtros.
            }
            catch (ObjectDisposedException)
            {
                // La página se cerró mientras terminaba la solicitud.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar las fuentes de nutrientes.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las fuentes de nutrientes",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    if (reiniciar)
                    {
                        IsBusy =
                            false;

                        IsRefreshing =
                            false;
                    }
                    else
                    {
                        CargandoMas =
                            false;
                    }
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public void CancelarCargas()
        {
            CancellationTokenSource? carga =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancellationTokenSource? composicion =
                Interlocked.Exchange(
                    ref composicionCts,
                    null);

            CancelarSeguro(
                carga);

            CancelarSeguro(
                composicion);

            IsBusy =
                false;

            IsRefreshing =
                false;

            CargandoMas =
                false;

            CargandoComposicion =
                false;
        }

        private void AplicarPagina(
            FuenteNutrientePaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List
                    .Where(item =>
                        item.FuenteNutrientesId.HasValue)
                    .Select(item =>
                        item.FuenteNutrientesId!.Value)
                    .ToHashSet();

            foreach (FuenteNutrienteResponse item
                     in pagina.Items)
            {
                if (!item.FuenteNutrientesId.HasValue ||
                    item.FuenteNutrientesId.Value <= 0)
                {
                    continue;
                }

                if (idsActuales.Add(
                        item.FuenteNutrientesId.Value))
                {
                    List.Add(
                        item);
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
            textoBusqueda =
                string.Empty;

            OnPropertyChanged(
                nameof(TextoBusqueda));

            filtroCategoriaSeleccionada =
                FiltrosCategoria
                    .FirstOrDefault();

            OnPropertyChanged(
                nameof(FiltroCategoriaSeleccionada));

            OnPropertyChanged(
                nameof(MostrarSeccionTablaComposicion));

            InvalidarComposicion();

            await CargarAsync(
                reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing =
                true;

            try
            {
                InvalidarComposicion();

                await CargarAsync(
                    reiniciar: true);
            }
            finally
            {
                IsRefreshing =
                    false;
            }
        }

        private async Task ToggleTablaComposicionAsync()
        {
            if (!MostrarSeccionTablaComposicion)
                return;

            if (MostrarTablaComposicion)
            {
                MostrarTablaComposicion =
                    false;

                return;
            }

            MostrarTablaComposicion =
                true;

            await CargarComposicionAsync();
        }

        private async Task CargarComposicionAsync()
        {
            CancellationTokenSource source =
                PrepararNuevaCargaComposicion();

            try
            {
                CargandoComposicion =
                    true;

                MensajeComposicion =
                    string.Empty;

                ElementosTabla.Clear();
                TablaComposicion.Clear();

                ApiResult<List<FuenteNutrienteResponse>>
                    resultado =
                        await consultaApiService
                            .ObtenerComposicionAsync(
                                TextoBusqueda,
                                ObtenerCodigoCategoria(),
                                source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaComposicionActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(
                            resultado.Message))
                    {
                        MensajeComposicion =
                            resultado.Message;
                    }

                    return;
                }

                ConstruirTablaComposicion(
                    resultado.Data);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal.
            }
            catch (ObjectDisposedException)
            {
                // La página se cerró.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaComposicionActual(source))
                {
                    MensajeComposicion =
                        "No fue posible construir la matriz de composición.";

                    await MostrarErrorInesperadoAsync(
                        "cargar la composición de fuentes",
                        ex);
                }
            }
            finally
            {
                if (EsCargaComposicionActual(source))
                {
                    CargandoComposicion =
                        false;
                }

                LiberarCargaComposicion(
                    source);

                NotificarEstadoComposicion();
            }
        }

        private void ConstruirTablaComposicion(
            IEnumerable<FuenteNutrienteResponse> fuentes)
        {
            List<FuenteNutrienteResponse> fuentesConAporte =
                fuentes
                    .Where(
                        FuenteTieneAporteElementoQuimico)
                    .OrderBy(item =>
                        item.NombreNutriente ??
                        string.Empty)
                    .ToList();

            List<string> simbolos =
                fuentesConAporte
                    .SelectMany(item =>
                        item.ElementosQuimicos ??
                        new List<
                            FuenteNutrienteElementoQuimicoResponse>())
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.SimboloElementoQuimico) &&
                        (item.CantidadAporte ?? 0) > 0)
                    .Select(item =>
                        item.SimboloElementoQuimico!
                            .Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        ObtenerOrdenElemento)
                    .ThenBy(item =>
                        item)
                    .ToList();

            foreach (string simbolo in simbolos)
            {
                ElementosTabla.Add(
                    simbolo);
            }

            foreach (FuenteNutrienteResponse fuente
                     in fuentesConAporte)
            {
                var fila =
                    new FuenteNutrienteTablaDinamicaRow
                    {
                        FuenteNutrientesId =
                            fuente.FuenteNutrientesId,

                        Fuente =
                            fuente.NombreNutriente ??
                            "Fuente sin nombre"
                    };

                foreach (string simbolo in simbolos)
                {
                    FuenteNutrienteElementoQuimicoResponse?
                        aporte =
                            fuente.ElementosQuimicos?
                                .FirstOrDefault(item =>
                                    string.Equals(
                                        item.SimboloElementoQuimico?
                                            .Trim(),
                                        simbolo,
                                        StringComparison
                                            .OrdinalIgnoreCase));

                    fila.Celdas.Add(
                        new FuenteNutrienteTablaDinamicaCell
                        {
                            SimboloElemento =
                                simbolo,

                            Valor =
                                aporte?.CantidadAporte ??
                                0
                        });
                }

                TablaComposicion.Add(
                    fila);
            }

            NotificarEstadoComposicion();
        }

        private void InvalidarComposicion()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref composicionCts,
                    null);

            CancelarSeguro(
                source);

            MostrarTablaComposicion =
                false;

            CargandoComposicion =
                false;

            MensajeComposicion =
                string.Empty;

            ElementosTabla.Clear();
            TablaComposicion.Clear();

            NotificarEstadoComposicion();
        }

        private void SolicitarRecargaPorFiltro()
        {
            int version =
                Interlocked.Increment(
                    ref versionFiltro);

            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    await Task.Delay(
                        120);

                    if (version !=
                        Volatile.Read(
                            ref versionFiltro))
                    {
                        return;
                    }

                    await EjecutarSeguroAsync(
                        () => CargarAsync(
                            reiniciar: true),
                        "aplicar el filtro de fuentes");
                });
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest()
                    }
                });

        private Task OnEditAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest(
                            fuente)
                    }
                });
        }

        private Task OnViewAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.View
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest(
                            fuente)
                    }
                });
        }

        private async Task OnDeleteAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente == null ||
                IsBusy)
            {
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar fuente",
                        $"¿Desea eliminar la fuente '{fuente.NombreNutriente}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy =
                    true;

                ActualizarComandos();

                ApiResult<bool> resultado =
                    await fuenteNutrienteApiService
                        .DeleteFuenteNutrienteResultAsync(
                            new FuenteNutrienteRequest(
                                fuente));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        resultado.Message);

                    return;
                }

                List.Remove(
                    fuente);

                TotalRegistros =
                    Math.Max(
                        0,
                        TotalRegistros - 1);

                InvalidarComposicion();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                            ? "Fuente eliminada correctamente."
                            : resultado.Message);
            }
            finally
            {
                IsBusy =
                    false;

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
                CancelarCargas();

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

        private void CargarFiltrosCategoria()
        {
            FiltrosCategoria.Clear();

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoTodas,

                    Nombre =
                        "Todas"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoBalanceNutricional,

                    Nombre =
                        "Balance nutricional"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoEnmiendaCalcarea,

                    Nombre =
                        "Enmienda calcárea"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoFertilizacionMixta,

                    Nombre =
                        "Fertilización mixta"
                });

            filtroCategoriaSeleccionada =
                FiltrosCategoria
                    .FirstOrDefault();

            OnPropertyChanged(
                nameof(FiltroCategoriaSeleccionada));

            OnPropertyChanged(
                nameof(MostrarSeccionTablaComposicion));
        }

        private string ObtenerCodigoCategoria() =>
            FiltroCategoriaSeleccionada?.Codigo ??
            FuenteNutrienteCategoriaOption
                .CodigoTodas;

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
            ToggleTablaComposicionCommand.ChangeCanExecute();
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

        private void NotificarEstadoComposicion()
        {
            OnPropertyChanged(
                nameof(MostrarTablaConDatos));

            OnPropertyChanged(
                nameof(MostrarMensajeTablaVacia));

            OnPropertyChanged(
                nameof(TextoBotonTablaComposicion));
        }

        private static bool
            FuenteTieneAporteElementoQuimico(
                FuenteNutrienteResponse fuente) =>
            fuente.ElementosQuimicos != null &&
            fuente.ElementosQuimicos.Any(item =>
                !string.IsNullOrWhiteSpace(
                    item.SimboloElementoQuimico) &&
                (item.CantidadAporte ?? 0) > 0);

        private static int ObtenerOrdenElemento(
            string simbolo) =>
            simbolo
                .Trim()
                .ToUpperInvariant() switch
            {
                "N" => 1,
                "P" => 2,
                "K" => 3,
                "CA" => 4,
                "MG" => 5,
                "ZN" => 6,
                "S" => 7,
                "B" => 8,
                _ => 100
            };

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

        private CancellationTokenSource
            PrepararNuevaCargaComposicion()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref composicionCts,
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

        private bool EsCargaComposicionActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(
                    ref composicionCts),
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

        private void LiberarCargaComposicion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref composicionCts,
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
