using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class RangoNutrienteDetalleViewModel : GlobalService
    {
        private readonly RangoNutrienteConsultaApiService
            consultaApiService = new();

        private readonly RangoNutrienteApiService
            apiService = new();

        private CancellationTokenSource? cargaCts;

        private RangoNutrienteCategoriaItem? categoria;
        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoMas;
        private bool navegando;
        private bool pantallaCargada;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;

        public ObservableCollection<RangoNutrienteResponse>
            Aportes { get; } = new();

        public Command AddCommand { get; }
        public Command<RangoNutrienteResponse> EditCommand { get; }
        public Command<RangoNutrienteResponse> ViewCommand { get; }
        public Command<RangoNutrienteResponse> DeleteCommand { get; }
        public Command BackCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public RangoNutrienteDetalleViewModel()
        {
            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AddAsync,
                        "abrir el formulario de rango"),
                    () =>
                        Categoria != null &&
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<RangoNutrienteResponse>(
                    async item => await EjecutarSeguroAsync(
                        () => OpenAsync(
                            item,
                            FormMode.FormModeSelect.Edit),
                        "editar el rango nutricional"),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<RangoNutrienteResponse>(
                    async item => await EjecutarSeguroAsync(
                        () => OpenAsync(
                            item,
                            FormMode.FormModeSelect.View),
                        "consultar el rango nutricional"),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<RangoNutrienteResponse>(
                    async item => await EjecutarSeguroAsync(
                        () => DeleteAsync(item),
                        "eliminar el rango nutricional"),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            BackCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => NavegarAsync(AppRoutes.RangosNutrientes),
                        "regresar a los cultivos"),
                    () => !IsBusy && !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(true),
                        "buscar rangos nutricionales"),
                    () => CanView && !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () => CanView && !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los rangos"),
                    () => CanView && !Navegando);

            CargarMasCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(false),
                        "cargar más rangos"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !CargandoMas &&
                        !Navegando &&
                        PuedeCargarMas);
        }

        public RangoNutrienteCategoriaItem? Categoria
        {
            get => categoria;
            set
            {
                categoria = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreCategoria));
                OnPropertyChanged(nameof(DescripcionCategoria));
                OnPropertyChanged(nameof(Titulo));
                ActualizarComandos();
            }
        }

        public string NombreCategoria =>
            Categoria?.NombreCategoria ?? string.Empty;

        public string DescripcionCategoria =>
            string.IsNullOrWhiteSpace(
                Categoria?.DescripcionCategoria)
                    ? "Sin descripción registrada."
                    : Categoria!.DescripcionCategoria.Trim();

        public string Titulo =>
            string.IsNullOrWhiteSpace(NombreCategoria)
                ? "Rangos nutricionales"
                : $"Rangos de {NombreCategoria}";

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
                OnPropertyChanged(nameof(ResumenAportes));
            }
        }

        public string ResumenAportes =>
            TotalRegistros == 1
                ? "1 rango configurado"
                : $"{TotalRegistros} rangos configurados";

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            !CargandoMas &&
            Aportes.Count == 0 &&
            !TieneMensaje;

        public bool MostrarFinLista =>
            CanView &&
            pantallaCargada &&
            Aportes.Count > 0 &&
            !PuedeCargarMas &&
            !IsBusy &&
            !CargandoMas;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("rangoNutrientePage");
            ActualizarComandos();
            NotificarEstadoLista();
        }

        public Task InicializarAsync() =>
            CargarAsync(true);

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView ||
                Categoria == null ||
                Categoria.TipoCultivoId <= 0 ||
                Navegando)
            {
                return;
            }

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

                ApiResult<RangoNutrientePaginaResponse> resultado =
                    await consultaApiService.BuscarRangosAsync(
                        Categoria.TipoCultivoId,
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
                        "No fue posible cargar los rangos nutricionales.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los rangos nutricionales",
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
            RangoNutrientePaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                Aportes.Clear();

            HashSet<int> idsActuales =
                Aportes
                    .Select(item =>
                        item.ParametroRangoNutrienteCultivoId)
                    .ToHashSet();

            foreach (RangoNutrienteResponse item in pagina.Items)
            {
                if (item.ParametroRangoNutrienteCultivoId <= 0)
                    continue;

                if (idsActuales.Add(
                        item.ParametroRangoNutrienteCultivoId))
                {
                    Aportes.Add(item);
                }
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
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task AddAsync()
        {
            if (Categoria == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Categoria"] = Categoria,
                    ["Item"] = new RangoNutrienteRequest
                    {
                        TipoCultivoId = Categoria.TipoCultivoId
                    }
                });
        }

        private Task OpenAsync(
            RangoNutrienteResponse? item,
            FormMode.FormModeSelect mode)
        {
            if (item == null || Categoria == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Categoria"] = Categoria,
                    ["Item"] = new RangoNutrienteRequest(item)
                });
        }

        private async Task DeleteAsync(
            RangoNutrienteResponse? item)
        {
            if (item == null || IsBusy)
                return;

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar rango nutricional",
                        $"¿Desea eliminar el rango de " +
                        $"'{item.ElementoTexto}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await apiService.DeleteAsync(
                        item.ParametroRangoNutrienteCultivoId);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                Aportes.Remove(item);
                TotalRegistros =
                    Math.Max(0, TotalRegistros - 1);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Rango eliminado correctamente."
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

        private void ActualizarComandos()
        {
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            BackCommand.ChangeCanExecute();
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
            OnPropertyChanged(nameof(ResumenAportes));
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
