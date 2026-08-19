using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    public sealed class TerrenoBusquedaIAViewModel : GlobalService
    {
        private const int TamanoPagina = 20;

        private readonly TerrenoBusquedaIAApiService api = new();
        private CancellationTokenSource? cargaCts;
        private bool inicializado;
        private bool paginaActiva;
        private int paginaActual;
        private int totalPaginas;
        private int totalRegistros;
        private string texto = string.Empty;
        private string codigo = string.Empty;
        private string propietario = string.Empty;
        private string identificacionPropietario = string.Empty;
        private string ubicacion = string.Empty;
        private string direccion = string.Empty;
        private string extensionMinimaTexto = string.Empty;
        private string extensionMaximaTexto = string.Empty;
        private string mensajeEstado = string.Empty;

        // Los filtros aplicados se conservan separados de lo que el usuario
        // continúa escribiendo hasta que vuelva a pulsar Buscar.
        private string textoAplicado = string.Empty;
        private string codigoAplicado = string.Empty;
        private string propietarioAplicado = string.Empty;
        private string identificacionPropietarioAplicada = string.Empty;
        private string ubicacionAplicada = string.Empty;
        private string direccionAplicada = string.Empty;
        private decimal? extensionMinimaAplicada;
        private decimal? extensionMaximaAplicada;

        public TerrenoBusquedaIAViewModel()
        {
            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy);

            LimpiarCommand = new Command(
                async () => await LimpiarAsync(),
                () => !IsBusy);

            PaginaAnteriorCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual - 1),
                () => !IsBusy && PuedeIrAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual + 1),
                () => !IsBusy && PuedeIrSiguiente);

            SeleccionarCommand = new Command<TerrenoBusquedaIAItem>(
                async item => await SeleccionarAsync(item),
                item => item != null && !IsBusy);

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<TerrenoBusquedaIAItem> Resultados { get; } = [];

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command<TerrenoBusquedaIAItem> SeleccionarCommand { get; }
        public Command RegresarCommand { get; }

        public event EventHandler? PaginaCargada;

        public string Texto
        {
            get => texto;
            set => Cambiar(ref texto, value);
        }

        public string Codigo
        {
            get => codigo;
            set => Cambiar(ref codigo, value);
        }

        public string Propietario
        {
            get => propietario;
            set => Cambiar(ref propietario, value);
        }

        public string IdentificacionPropietario
        {
            get => identificacionPropietario;
            set => Cambiar(ref identificacionPropietario, value);
        }

        public string Ubicacion
        {
            get => ubicacion;
            set => Cambiar(ref ubicacion, value);
        }

        public string Direccion
        {
            get => direccion;
            set => Cambiar(ref direccion, value);
        }

        public string ExtensionMinimaTexto
        {
            get => extensionMinimaTexto;
            set => Cambiar(ref extensionMinimaTexto, value);
        }

        public string ExtensionMaximaTexto
        {
            get => extensionMaximaTexto;
            set => Cambiar(ref extensionMaximaTexto, value);
        }

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                if (mensajeEstado == value)
                    return;

                mensajeEstado = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public bool TieneResultados => Resultados.Count > 0;
        public bool SinResultados => !TieneResultados && !IsBusy;
        public bool PuedeIrAnterior => paginaActual > 1;
        public bool PuedeIrSiguiente =>
            totalPaginas > 0 && paginaActual < totalPaginas;
        public bool MostrarPaginador => totalRegistros > 0 && totalPaginas > 0;

        public string TextoPaginacion => totalPaginas <= 0
            ? "Página 0 de 0"
            : $"Página {paginaActual:N0} de {totalPaginas:N0}";

        public string ResumenResultados => totalRegistros == 1
            ? "1 terreno encontrado"
            : $"{totalRegistros:N0} terrenos encontrados";

        public void ActivarPagina()
        {
            paginaActiva = true;
        }

        public void CancelarOperaciones()
        {
            paginaActiva = false;
            cargaCts?.Cancel();
        }

        public async Task InicializarAsync()
        {
            if (inicializado)
                return;

            inicializado = true;

            if (!PermissionService.Instance.HasRead("terrenoPage"))
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para consultar terrenos.");
                await GoToAsyncParameters(AppRoutes.Regresar);
                return;
            }

            AplicarFiltrosEscritos(
                extensionMinima: null,
                extensionMaxima: null);

            await CargarPaginaAsync(1);
        }

        private async Task BuscarAsync()
        {
            if (IsBusy || !paginaActiva)
                return;

            (bool valido, decimal? extensionMinima, decimal? extensionMaxima) =
                await ValidarExtensionesAsync();

            if (!valido)
                return;

            AplicarFiltrosEscritos(extensionMinima, extensionMaxima);
            await CargarPaginaAsync(1);
        }

        private async Task CambiarPaginaAsync(int paginaSolicitada)
        {
            if (IsBusy || !paginaActiva || paginaSolicitada < 1 ||
                (totalPaginas > 0 && paginaSolicitada > totalPaginas))
            {
                return;
            }

            await CargarPaginaAsync(paginaSolicitada);
        }

        private async Task CargarPaginaAsync(int paginaSolicitada)
        {
            if (IsBusy || !paginaActiva)
                return;

            cargaCts?.Cancel();
            cargaCts?.Dispose();
            cargaCts = new CancellationTokenSource();
            CancellationToken cancellationToken = cargaCts.Token;

            IsBusy = true;
            MensajeEstado = "Buscando terrenos...";
            ActualizarComandos();

            try
            {
                TerrenoBusquedaIAPagina pagina = await api.BuscarAsync(
                    new TerrenoBusquedaIAFiltro
                    {
                        Texto = textoAplicado,
                        Codigo = codigoAplicado,
                        Propietario = propietarioAplicado,
                        IdentificacionPropietario =
                            identificacionPropietarioAplicada,
                        Ubicacion = ubicacionAplicada,
                        Direccion = direccionAplicada,
                        ExtensionMinima = extensionMinimaAplicada,
                        ExtensionMaxima = extensionMaximaAplicada,
                        Pagina = Math.Max(1, paginaSolicitada),
                        TamanoPagina = TamanoPagina
                    },
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested || !paginaActiva)
                    return;

                Resultados.Clear();
                foreach (TerrenoBusquedaIAItem item in pagina.Items)
                    Resultados.Add(item);

                paginaActual = pagina.Pagina;
                totalPaginas = pagina.TotalPaginas;
                totalRegistros = pagina.Total;
                MensajeEstado = string.Empty;

                NotificarEstadoResultados();
                PaginaCargada?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MensajeEstado = string.Empty;

                if (paginaActiva)
                    await MostrarErrorAsync(ex.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                OnPropertyChanged(nameof(SinResultados));
            }
        }

        private async Task LimpiarAsync()
        {
            if (IsBusy || !paginaActiva)
                return;

            Texto = string.Empty;
            Codigo = string.Empty;
            Propietario = string.Empty;
            IdentificacionPropietario = string.Empty;
            Ubicacion = string.Empty;
            Direccion = string.Empty;
            ExtensionMinimaTexto = string.Empty;
            ExtensionMaximaTexto = string.Empty;

            AplicarFiltrosEscritos(
                extensionMinima: null,
                extensionMaxima: null);

            await CargarPaginaAsync(1);
        }

        private async Task SeleccionarAsync(TerrenoBusquedaIAItem? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.Regresar,
                new Dictionary<string, object>
                {
                    ["TerrenoSeleccionado"] = item
                });
        }

        private async Task<(bool Valido, decimal? ExtensionMinima, decimal? ExtensionMaxima)>
            ValidarExtensionesAsync()
        {
            decimal? extensionMinima = ConvertirDecimal(ExtensionMinimaTexto);
            decimal? extensionMaxima = ConvertirDecimal(ExtensionMaximaTexto);

            if ((!string.IsNullOrWhiteSpace(ExtensionMinimaTexto) &&
                 !extensionMinima.HasValue) ||
                (!string.IsNullOrWhiteSpace(ExtensionMaximaTexto) &&
                 !extensionMaxima.HasValue))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los valores de extensión ingresados.");
                return (false, extensionMinima, extensionMaxima);
            }

            if (extensionMinima.HasValue && extensionMaxima.HasValue &&
                extensionMinima.Value > extensionMaxima.Value)
            {
                await MostrarAdvertenciaAsync(
                    "La extensión mínima no puede ser mayor que la máxima.");
                return (false, extensionMinima, extensionMaxima);
            }

            return (true, extensionMinima, extensionMaxima);
        }

        private void AplicarFiltrosEscritos(
            decimal? extensionMinima,
            decimal? extensionMaxima)
        {
            textoAplicado = Texto.Trim();
            codigoAplicado = Codigo.Trim();
            propietarioAplicado = Propietario.Trim();
            identificacionPropietarioAplicada =
                IdentificacionPropietario.Trim();
            ubicacionAplicada = Ubicacion.Trim();
            direccionAplicada = Direccion.Trim();
            extensionMinimaAplicada = extensionMinima;
            extensionMaximaAplicada = extensionMaxima;
        }

        private static decimal? ConvertirDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return decimal.TryParse(
                    valor,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal resultado)
                ? resultado
                : null;
        }

        private void Cambiar(
            ref string campo,
            string? valor,
            [CallerMemberName] string? nombrePropiedad = null)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged(nombrePropiedad);
        }

        private void NotificarEstadoResultados()
        {
            OnPropertyChanged(nameof(TieneResultados));
            OnPropertyChanged(nameof(SinResultados));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginador));
            OnPropertyChanged(nameof(TextoPaginacion));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            SeleccionarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }
    }
}
