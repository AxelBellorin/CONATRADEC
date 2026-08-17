using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class RangoNutrienteViewModel : GlobalService
    {
        private readonly RangoNutrienteConsultaApiService
            consultaApiService = new();

        private readonly TipoCultivoApiService
            cultivoApiService = new();

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

        public ObservableCollection<RangoNutrienteCategoriaItem>
            List { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCategoryCommand { get; }
        public Command<RangoNutrienteCategoriaItem> OpenCategoryCommand
        {
            get;
        }
        public Command<RangoNutrienteCategoriaItem> EditCategoryCommand
        {
            get;
        }
        public Command<RangoNutrienteCategoriaItem> DeleteCategoryCommand
        {
            get;
        }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public RangoNutrienteViewModel()
        {
            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => NavegarAsync(AppRoutes.Configuracion),
                        "regresar a configuración"),
                    () => !IsBusy && !Navegando);

            AddCategoryCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AddCategoryAsync,
                        "abrir el formulario de tipo de cultivo"),
                    () => CanAdd && !IsBusy && !Navegando);

            OpenCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item => await EjecutarSeguroAsync(
                        () => OpenCategoryAsync(item),
                        "abrir los rangos del cultivo"),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            EditCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item => await EjecutarSeguroAsync(
                        () => EditCategoryAsync(item),
                        "editar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            DeleteCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item => await EjecutarSeguroAsync(
                        () => DeleteCategoryAsync(item),
                        "eliminar el tipo de cultivo"),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(true),
                        "buscar tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !CargandoMas &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !CargandoMas &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !CargandoMas &&
                        !Navegando);

            CargarMasCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(false),
                        "cargar más tipos de cultivo"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !IsRefreshing &&
                        !CargandoMas &&
                        !Navegando &&
                        PuedeCargarMas);
        }

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
                ? "1 cultivo encontrado"
                : $"{TotalRegistros} cultivos encontrados";

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

        public bool TienePaginaCargada =>
            pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("rangoNutrientePage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstadoLista();
        }

        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            CancelarCarga();

            TextoBusqueda = string.Empty;
            Mensaje = string.Empty;
            List.Clear();
            paginaActual = 0;
            totalPaginas = 1;
            TotalRegistros = 0;
            pantallaCargada = false;

            OnPropertyChanged(nameof(PuedeCargarMas));
            NotificarEstadoLista();

            await CargarAsync(true);
        }

        public Task InicializarAsync()
        {
            if (TienePaginaCargada)
                return Task.CompletedTask;

            return CargarAsync(true);
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || Navegando)
                return;

            if (IsBusy || CargandoMas)
                return;

            if (!reiniciar && !PuedeCargarMas)
                return;

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

                ApiResult<RangoNutrienteCategoriaPaginaResponse>
                    resultado =
                        await consultaApiService.BuscarCultivosAsync(
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

                AplicarPagina(resultado.Data, reiniciar);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al reemplazar o abandonar la consulta.
            }
            catch (ObjectDisposedException)
            {
                // La pantalla se cerró durante la consulta.
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

        /// <summary>
        /// Reconstruye únicamente las páginas que ya estaban cargadas dentro
        /// de la visita. Se usa después de un CRUD confirmado para conservar
        /// filtro y profundidad de scroll sin dejar huecos por el corrimiento
        /// natural de la paginación del servidor.
        /// </summary>
        public async Task RecargarVentanaActualAsync()
        {
            if (!CanView || Navegando || IsBusy || CargandoMas)
                return;

            if (!TienePaginaCargada)
            {
                await CargarAsync(true);
                return;
            }

            int paginasObjetivo =
                Math.Max(1, paginaActual);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            /*
             * El gesto de RefreshView ya comunica la actualización. Durante
             * ese flujo conservamos los datos visibles y evitamos superponer
             * el bloqueo de carga inicial. Las recargas causadas por un CRUD
             * sí mantienen el indicador bloqueante para impedir acciones
             * concurrentes mientras se reconcilia la ventana paginada.
             */
            bool mostrarCargaBloqueante = !IsRefreshing;

            try
            {
                if (mostrarCargaBloqueante)
                    IsBusy = true;

                Mensaje = string.Empty;

                int tamanoPagina = ObtenerTamanoPagina();
                var nuevosItems =
                    new List<RangoNutrienteCategoriaItem>();
                var ids = new HashSet<int>();

                RangoNutrienteCategoriaPaginaResponse?
                    primeraPagina = null;
                int paginasAConsultar = paginasObjetivo;

                for (int numeroPagina = 1;
                     numeroPagina <= paginasAConsultar;
                     numeroPagina++)
                {
                    ApiResult<RangoNutrienteCategoriaPaginaResponse>
                        resultado =
                            await consultaApiService.BuscarCultivosAsync(
                                TextoBusqueda,
                                numeroPagina,
                                tamanoPagina,
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

                    RangoNutrienteCategoriaPaginaResponse pagina =
                        resultado.Data;

                    primeraPagina ??= pagina;

                    if (numeroPagina == 1)
                    {
                        int paginasServidor =
                            Math.Max(1, pagina.TotalPaginas);

                        paginasAConsultar =
                            Math.Min(
                                paginasObjetivo,
                                paginasServidor);
                    }

                    foreach (RangoNutrienteCategoriaItem item
                             in pagina.Items)
                    {
                        if (item.TipoCultivoId > 0 &&
                            ids.Add(item.TipoCultivoId))
                        {
                            nuevosItems.Add(item);
                        }
                    }
                }

                if (primeraPagina == null)
                    return;

                List.Clear();

                foreach (RangoNutrienteCategoriaItem item
                         in nuevosItems)
                {
                    List.Add(item);
                }

                paginaActual = paginasAConsultar;
                totalPaginas =
                    Math.Max(1, primeraPagina.TotalPaginas);
                TotalRegistros =
                    Math.Max(0, primeraPagina.TotalRegistros);
                Mensaje = string.Empty;
                pantallaCargada = true;

                OnPropertyChanged(nameof(PuedeCargarMas));
                NotificarEstadoLista();
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible actualizar los tipos de cultivo.";

                    await MostrarErrorInesperadoAsync(
                        "actualizar los tipos de cultivo",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    if (mostrarCargaBloqueante)
                        IsBusy = false;

                    IsRefreshing = false;
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
            RangoNutrienteCategoriaPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List.Select(item => item.TipoCultivoId).ToHashSet();

            foreach (RangoNutrienteCategoriaItem item in pagina.Items)
            {
                if (item.TipoCultivoId <= 0)
                    continue;

                if (idsActuales.Add(item.TipoCultivoId))
                    List.Add(item);
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
            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await RecargarVentanaActualAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task AddCategoryAsync() =>
            NavegarAsync(
                AppRoutes.RangoNutrienteCategoriaFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new TipoCultivoRequest()
                });

        private Task OpenCategoryAsync(
            RangoNutrienteCategoriaItem? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RangoNutrienteDetalle,
                new Dictionary<string, object>
                {
                    ["Categoria"] = item
                });
        }

        private Task EditCategoryAsync(
            RangoNutrienteCategoriaItem? item)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RangoNutrienteCategoriaFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Item"] =
                        new TipoCultivoRequest(
                            item.ToTipoCultivoResponse())
                });
        }

        private async Task DeleteCategoryAsync(
            RangoNutrienteCategoriaItem? item)
        {
            if (item == null || IsBusy)
                return;

            string dependencia =
                item.CantidadAportes > 0
                    ? "\n\nEste cultivo tiene rangos configurados. " +
                      "El servidor impedirá su eliminación para " +
                      "proteger las relaciones."
                    : string.Empty;

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar tipo de cultivo",
                        $"¿Desea eliminar '{item.NombreCategoria}'?" +
                        dependencia,
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            bool eliminado = false;
            string mensajeExito = string.Empty;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await cultivoApiService.DeleteDesdeRangosAsync(
                        item.TipoCultivoId);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(item);
                TotalRegistros =
                    Math.Max(0, TotalRegistros - 1);

                eliminado = true;
                mensajeExito =
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Tipo de cultivo eliminado correctamente."
                        : resultado.Message;
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstadoLista();
            }

            if (!eliminado)
                return;

            await RecargarVentanaActualAsync();
            await MostrarToastAsync(mensajeExito);
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

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCategoryCommand.ChangeCanExecute();
            OpenCategoryCommand.ChangeCanExecute();
            EditCategoryCommand.ChangeCanExecute();
            DeleteCategoryCommand.ChangeCanExecute();
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
                Interlocked.Exchange(ref cargaCts, source);

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
            }
        }

        private static bool EsMensajeCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
