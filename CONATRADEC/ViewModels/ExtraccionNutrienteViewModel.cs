using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ExtraccionNutrienteViewModel : GlobalService
    {
        private readonly ExtraccionNutrienteConsultaApiService consultaApiService = new();
        private readonly ExtraccionNutrienteApiService apiService = new();

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

        public ObservableCollection<ExtraccionNutrienteResponse> List { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<ExtraccionNutrienteResponse> EditCommand { get; }
        public Command<ExtraccionNutrienteResponse> ViewCommand { get; }
        public Command<ExtraccionNutrienteResponse> DeleteCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public ExtraccionNutrienteViewModel()
        {
            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => NavegarAsync(AppRoutes.Configuracion),
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    AddAsync,
                    "abrir el formulario de extracción"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OpenAsync(item, FormMode.FormModeSelect.Edit),
                    "editar el parámetro de extracción"),
                item => item != null && CanEdit && !IsBusy && !Navegando);

            ViewCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OpenAsync(item, FormMode.FormModeSelect.View),
                    "consultar el parámetro de extracción"),
                item => item != null && CanView && !IsBusy && !Navegando);

            DeleteCommand = new Command<ExtraccionNutrienteResponse>(
                async item => await EjecutarSeguroAsync(
                    () => DeleteAsync(item),
                    "eliminar el parámetro de extracción"),
                item => item != null && CanDelete && !IsBusy && !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(true),
                    "buscar parámetros de extracción"),
                () => CanView && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar la búsqueda"),
                () => CanView && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los parámetros"),
                () => CanView && !Navegando);

            CargarMasCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    () => CargarAsync(false),
                    "cargar más parámetros"),
                () =>
                    CanView &&
                    !IsBusy &&
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
                ? "1 parámetro encontrado"
                : $"{TotalRegistros} parámetros encontrados";

        public bool PuedeCargarMas => paginaActual < totalPaginas;

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

        public bool MostrarAccesoDenegado => !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("extraccionNutrientePage");
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstadoLista();
        }

        public Task InicializarAsync() => CargarAsync(true);

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || Navegando)
                return;

            if (!reiniciar && (CargandoMas || !PuedeCargarMas))
                return;

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

                int paginaSolicitada = reiniciar
                    ? 1
                    : paginaActual + 1;

                ApiResult<ExtraccionNutrientePaginaResponse> resultado =
                    await consultaApiService.BuscarAsync(
                        TextoBusqueda,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!resultado.Success || resultado.Data == null)
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
                // Cancelación normal al reemplazar una consulta.
            }
            catch (ObjectDisposedException)
            {
                // La pantalla se cerró durante la solicitud.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested && EsCargaActual(source))
                {
                    Mensaje = "No fue posible cargar los parámetros de extracción.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los parámetros de extracción",
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
            ExtraccionNutrientePaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                List.Clear();

            HashSet<int> idsActuales =
                List.Select(x => x.ParametroExtraccionNutrienteCafeId)
                    .ToHashSet();

            foreach (ExtraccionNutrienteResponse item in pagina.Items)
            {
                if (item.ParametroExtraccionNutrienteCafeId <= 0)
                    continue;

                if (idsActuales.Add(item.ParametroExtraccionNutrienteCafeId))
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
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task AddAsync() =>
            NavegarAsync(
                AppRoutes.ExtraccionNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new ExtraccionNutrienteRequest()
                });

        private Task OpenAsync(
            ExtraccionNutrienteResponse? item,
            FormMode.FormModeSelect mode)
        {
            if (item == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.ExtraccionNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Item"] = new ExtraccionNutrienteRequest(item)
                });
        }

        private async Task DeleteAsync(ExtraccionNutrienteResponse? item)
        {
            if (item == null || IsBusy)
                return;

            bool confirmar = await Application.Current!
                .MainPage!
                .DisplayAlert(
                    "Eliminar parámetro de extracción",
                    $"¿Desea eliminar la extracción configurada para '{item.ElementoTexto}'?",
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
                        item.ParametroExtraccionNutrienteCafeId);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(item);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Parámetro eliminado correctamente."
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
            }
        }

        private static bool EsMensajeCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
