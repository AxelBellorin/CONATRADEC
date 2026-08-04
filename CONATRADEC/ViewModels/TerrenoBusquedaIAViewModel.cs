using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    public sealed class TerrenoBusquedaIAViewModel : GlobalService
    {
        private readonly TerrenoBusquedaIAApiService api = new();
        private CancellationTokenSource? cargaCts;
        private bool inicializado;
        private bool cargandoMas;
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

        public TerrenoBusquedaIAViewModel()
        {
            BuscarCommand = new Command(
                async () => await BuscarAsync(true),
                () => !IsBusy);

            LimpiarCommand = new Command(
                async () => await LimpiarAsync(),
                () => !IsBusy);

            CargarMasCommand = new Command(
                async () => await BuscarAsync(false),
                () => !IsBusy && !CargandoMas && PuedeCargarMas);

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
        public Command CargarMasCommand { get; }
        public Command<TerrenoBusquedaIAItem> SeleccionarCommand { get; }
        public Command RegresarCommand { get; }

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
        public bool PuedeCargarMas => paginaActual < totalPaginas;
        public string ResumenResultados => totalRegistros == 1
            ? "1 terreno encontrado"
            : $"{totalRegistros:N0} terrenos encontrados";

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
            }
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

            await BuscarAsync(true);
        }

        private async Task BuscarAsync(bool reiniciar)
        {
            if (IsBusy || CargandoMas)
                return;

            decimal? extensionMinima = ConvertirDecimal(ExtensionMinimaTexto);
            decimal? extensionMaxima = ConvertirDecimal(ExtensionMaximaTexto);

            if ((!string.IsNullOrWhiteSpace(ExtensionMinimaTexto) &&
                 !extensionMinima.HasValue) ||
                (!string.IsNullOrWhiteSpace(ExtensionMaximaTexto) &&
                 !extensionMaxima.HasValue))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los valores de extensión ingresados.");
                return;
            }

            if (extensionMinima.HasValue && extensionMaxima.HasValue &&
                extensionMinima.Value > extensionMaxima.Value)
            {
                await MostrarAdvertenciaAsync(
                    "La extensión mínima no puede ser mayor que la máxima.");
                return;
            }

            cargaCts?.Cancel();
            cargaCts?.Dispose();
            cargaCts = new CancellationTokenSource();

            if (reiniciar)
            {
                IsBusy = true;
                paginaActual = 0;
                totalPaginas = 0;
                totalRegistros = 0;
                MensajeEstado = "Buscando terrenos...";
            }
            else
            {
                CargandoMas = true;
            }

            ActualizarComandos();

            try
            {
                int paginaSolicitada = reiniciar
                    ? 1
                    : paginaActual + 1;

                TerrenoBusquedaIAPagina pagina = await api.BuscarAsync(
                    new TerrenoBusquedaIAFiltro
                    {
                        Texto = Texto,
                        Codigo = Codigo,
                        Propietario = Propietario,
                        IdentificacionPropietario = IdentificacionPropietario,
                        Ubicacion = Ubicacion,
                        Direccion = Direccion,
                        ExtensionMinima = extensionMinima,
                        ExtensionMaxima = extensionMaxima,
                        Pagina = paginaSolicitada,
                        TamanoPagina = 20
                    },
                    cargaCts.Token);

                if (reiniciar)
                    Resultados.Clear();

                foreach (TerrenoBusquedaIAItem item in pagina.Items)
                    Resultados.Add(item);

                paginaActual = pagina.Pagina;
                totalPaginas = pagina.TotalPaginas;
                totalRegistros = pagina.Total;
                MensajeEstado = string.Empty;

                OnPropertyChanged(nameof(TieneResultados));
                OnPropertyChanged(nameof(SinResultados));
                OnPropertyChanged(nameof(PuedeCargarMas));
                OnPropertyChanged(nameof(ResumenResultados));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MensajeEstado = string.Empty;
                await MostrarErrorAsync(ex.Message);
            }
            finally
            {
                IsBusy = false;
                CargandoMas = false;
                ActualizarComandos();
            }
        }

        private async Task LimpiarAsync()
        {
            Texto = string.Empty;
            Codigo = string.Empty;
            Propietario = string.Empty;
            IdentificacionPropietario = string.Empty;
            Ubicacion = string.Empty;
            Direccion = string.Empty;
            ExtensionMinimaTexto = string.Empty;
            ExtensionMaximaTexto = string.Empty;
            await BuscarAsync(true);
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

        private void Cambiar(ref string campo, string? valor)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged();
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            SeleccionarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }
    }
}
