using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class BalanceFormulaViewModel : GlobalService, IQueryAttributable
    {
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService = new();
        private readonly BalanceNutricionalApiService balanceNutricionalApiService = new();

        private CancellationTokenSource? recalculoCancellationTokenSource;

        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;
        private BalanceNutricionalResponse? resultadoBalance;
        private BalanceFormulaTablaFilaViewModel? filaFormula;
        private BalanceFormulaAplicacionViewModel? filaTotalAplicaciones;
        private BalanceFormulaAplicacionViewModel? filaOnzasPlanta;
        private BalanceFormulaPrecioViewModel? filaTotalPrecio;

        private string nombreFormula = "Fórmula balance nutricional";
        private string totalPlantas = string.Empty;
        private string totalAplicaciones = "3";
        private string mensaje = string.Empty;
        private string tituloColumnaAplicaciones = "Aplicaciones";
        private string etiquetaDosisPlantaSeleccionada = "Dosis por planta";
        private string errorTotalAplicaciones = string.Empty;

        private decimal dosisPlantaSeleccionada;

        private int? terrenoId;

        private bool tieneResultadoBalance;
        private bool suspenderRecalculoAutomatico;
        private bool complementarConFertilizacionMixta;

        private static readonly string[] SimbolosBalanceFormulaActuales =
        {
            "N",
            "P",
            "K",
            "CA",
            "MG"
        };

        public BalanceFormulaViewModel()
        {
            ElementosBalance = new ObservableCollection<BalanceFormulaElementoViewModel>();
            FuentesNutrientes = new ObservableCollection<FuenteNutrienteResponse>();
            FilasResultado = new ObservableCollection<BalanceFormulaFilaResultadoViewModel>();
            TotalesAportes = new ObservableCollection<BalanceFormulaAporteViewModel>();

            EncabezadosElementosTabla = new ObservableCollection<string>();
            FilasTablaBalance = new ObservableCollection<BalanceFormulaTablaFilaViewModel>();
            FilasAplicaciones = new ObservableCollection<BalanceFormulaAplicacionViewModel>();
            FilasPrecio = new ObservableCollection<BalanceFormulaPrecioViewModel>();
            FormulaComercialVisual = new ObservableCollection<BalanceFormulaComercialViewModel>();

            RecalcularCommand = new Command(
                async () => await ReiniciarBalanceAsync(),
                () => PuedeRecalcular
            );

            VolverCommand = new Command(
                async () => await VolverAsync()
            );
        }

        public event EventHandler<BalanceFertilizacionMixtaChangedEventArgs>?
            ComplementoFertilizacionMixtaCambiado;

        public AnalisisSueloCalculoDataResponse? ResultadoCalculo
        {
            get => resultadoCalculo;
            set
            {
                resultadoCalculo = value;
                OnPropertyChanged(nameof(ResultadoCalculo));
            }
        }

        public AnalisisSueloGuardarCalculoRequest? RequestGuardarAnalisis
        {
            get => requestGuardarAnalisis;
            set
            {
                requestGuardarAnalisis = value;
                OnPropertyChanged(nameof(RequestGuardarAnalisis));

                AsignarNombreFormulaDesdeAnalisis();
            }
        }

        public BalanceNutricionalResponse? ResultadoBalance
        {
            get => resultadoBalance;
            set
            {
                resultadoBalance = value;
                OnPropertyChanged(nameof(ResultadoBalance));
            }
        }

        public BalanceFormulaTablaFilaViewModel? FilaFormula
        {
            get => filaFormula;
            set
            {
                filaFormula = value;
                OnPropertyChanged(nameof(FilaFormula));
                OnPropertyChanged(nameof(TieneTablaBalance));
            }
        }

        public BalanceFormulaAplicacionViewModel? FilaTotalAplicaciones
        {
            get => filaTotalAplicaciones;
            set
            {
                filaTotalAplicaciones = value;
                OnPropertyChanged(nameof(FilaTotalAplicaciones));
                OnPropertyChanged(nameof(TieneTablaAplicaciones));
            }
        }

        public BalanceFormulaAplicacionViewModel? FilaOnzasPlanta
        {
            get => filaOnzasPlanta;
            set
            {
                filaOnzasPlanta = value;
                OnPropertyChanged(nameof(FilaOnzasPlanta));
                OnPropertyChanged(nameof(TieneTablaAplicaciones));
            }
        }

        public BalanceFormulaPrecioViewModel? FilaTotalPrecio
        {
            get => filaTotalPrecio;
            set
            {
                filaTotalPrecio = value;
                OnPropertyChanged(nameof(FilaTotalPrecio));
                OnPropertyChanged(nameof(TieneTablaPrecios));
            }
        }

        public string TituloColumnaAplicaciones
        {
            get => tituloColumnaAplicaciones;
            set
            {
                tituloColumnaAplicaciones = value;
                OnPropertyChanged(nameof(TituloColumnaAplicaciones));
            }
        }

        public string EtiquetaDosisPlantaSeleccionada
        {
            get => etiquetaDosisPlantaSeleccionada;
            set
            {
                etiquetaDosisPlantaSeleccionada = value;
                OnPropertyChanged(nameof(EtiquetaDosisPlantaSeleccionada));
            }
        }

        public decimal DosisPlantaSeleccionada
        {
            get => dosisPlantaSeleccionada;
            set
            {
                dosisPlantaSeleccionada = value;
                OnPropertyChanged(nameof(DosisPlantaSeleccionada));
                OnPropertyChanged(nameof(TextoDosisPlantaSeleccionada));
            }
        }

        public string TextoDosisPlantaSeleccionada =>
            $"{DosisPlantaSeleccionada.ToString("N4", CultureInfo.InvariantCulture)} oz/planta";

        public string NombreFormula
        {
            get => nombreFormula;
            set
            {
                nombreFormula = value ?? string.Empty;
                OnPropertyChanged(nameof(NombreFormula));
                OnPropertyChanged(nameof(NombreAnalisisSuelo));
            }
        }

        public string NombreAnalisisSuelo
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(NombreFormula))
                    return NombreFormula.Trim();

                return "Fórmula balance nutricional";
            }
        }

        public string TotalPlantas
        {
            get => totalPlantas;
            set
            {
                totalPlantas = value ?? string.Empty;
                OnPropertyChanged(nameof(TotalPlantas));
                RefrescarComandos();
                ProgramarRecalculoAutomatico();
            }
        }

        public string TotalAplicaciones
        {
            get => totalAplicaciones;
            set
            {
                string valorIngresado = value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(valorIngresado))
                {
                    totalAplicaciones = string.Empty;
                    ErrorTotalAplicaciones = "Ingrese una cantidad de aplicaciones entre 1 y 4.";

                    OnPropertyChanged(nameof(TotalAplicaciones));
                    RefrescarComandos();
                    return;
                }

                bool contieneCaracterNoNumerico = valorIngresado.Any(c => !char.IsDigit(c));

                if (contieneCaracterNoNumerico)
                {
                    totalAplicaciones = string.Empty;
                    ErrorTotalAplicaciones = "Solo se permiten números en la cantidad de aplicaciones.";

                    OnPropertyChanged(nameof(TotalAplicaciones));
                    RefrescarComandos();
                    return;
                }

                if (!int.TryParse(valorIngresado, out int aplicaciones))
                {
                    totalAplicaciones = string.Empty;
                    ErrorTotalAplicaciones = "Ingrese una cantidad de aplicaciones válida.";

                    OnPropertyChanged(nameof(TotalAplicaciones));
                    RefrescarComandos();
                    return;
                }

                if (aplicaciones < 1)
                {
                    aplicaciones = 1;
                    ErrorTotalAplicaciones = "La cantidad mínima de aplicaciones es 1.";
                }
                else if (aplicaciones > 4)
                {
                    aplicaciones = 4;
                    ErrorTotalAplicaciones = "La cantidad máxima de aplicaciones permitida es 4.";
                }
                else
                {
                    ErrorTotalAplicaciones = string.Empty;
                }

                string valorFinal = aplicaciones.ToString();

                if (totalAplicaciones == valorFinal)
                {
                    OnPropertyChanged(nameof(TotalAplicaciones));
                    RefrescarComandos();
                    return;
                }

                totalAplicaciones = valorFinal;

                OnPropertyChanged(nameof(TotalAplicaciones));
                RefrescarComandos();
                ProgramarRecalculoAutomatico();
            }
        }

        public string ErrorTotalAplicaciones
        {
            get => errorTotalAplicaciones;
            set
            {
                errorTotalAplicaciones = value;
                OnPropertyChanged(nameof(ErrorTotalAplicaciones));
                OnPropertyChanged(nameof(TieneErrorTotalAplicaciones));
            }
        }

        public bool TieneErrorTotalAplicaciones =>
            !string.IsNullOrWhiteSpace(ErrorTotalAplicaciones);

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        public int? TerrenoId
        {
            get => terrenoId;
            set
            {
                terrenoId = value;
                OnPropertyChanged(nameof(TerrenoId));
            }
        }

        public bool TieneResultadoBalance
        {
            get => tieneResultadoBalance;
            set
            {
                tieneResultadoBalance = value;
                OnPropertyChanged(nameof(TieneResultadoBalance));
            }
        }

        public bool ComplementarConFertilizacionMixta
        {
            get => complementarConFertilizacionMixta;
            set
            {
                if (complementarConFertilizacionMixta == value)
                    return;

                complementarConFertilizacionMixta = value;

                OnPropertyChanged(nameof(ComplementarConFertilizacionMixta));
                OnPropertyChanged(nameof(TextoEstadoComplemento));

                NotificarCambioComplemento(
                    complementarConFertilizacionMixta && TieneResultadoBalance
                        ? ConstruirContextoComplemento()
                        : null
                );
            }
        }

        public string TextoEstadoComplemento
        {
            get
            {
                if (!ComplementarConFertilizacionMixta)
                    return "El balance continuará funcionando de forma independiente.";

                if (!TieneResultadoBalance)
                    return "Complete el balance para continuar obligatoriamente en fertilización mixta.";

                return "El balance está listo y sus datos se enviarán a fertilización mixta.";
            }
        }

        public bool TieneElementosBalance => ElementosBalance.Count > 0;

        public bool TieneTablaBalance =>
            FilasTablaBalance.Count > 0 &&
            FilaFormula != null;

        public bool TieneTablaAplicaciones =>
            FilasAplicaciones.Count > 0 &&
            FilaTotalAplicaciones != null &&
            FilaOnzasPlanta != null;

        public bool TieneTablaPrecios =>
            FilasPrecio.Count > 0 &&
            FilaTotalPrecio != null;

        public bool TieneFormulaComercial =>
            FormulaComercialVisual.Count > 0;

        public bool PuedeRecalcular =>
            !IsBusy &&
            ElementosBalance.Any(x => x.FuenteSeleccionada != null);

        public ObservableCollection<BalanceFormulaElementoViewModel> ElementosBalance { get; }

        public ObservableCollection<FuenteNutrienteResponse> FuentesNutrientes { get; }

        public ObservableCollection<BalanceFormulaFilaResultadoViewModel> FilasResultado { get; }

        public ObservableCollection<BalanceFormulaAporteViewModel> TotalesAportes { get; }

        public ObservableCollection<string> EncabezadosElementosTabla { get; }

        public ObservableCollection<BalanceFormulaTablaFilaViewModel> FilasTablaBalance { get; }

        public ObservableCollection<BalanceFormulaAplicacionViewModel> FilasAplicaciones { get; }

        public ObservableCollection<BalanceFormulaPrecioViewModel> FilasPrecio { get; }

        public ObservableCollection<BalanceFormulaComercialViewModel> FormulaComercialVisual { get; }

        public Command RecalcularCommand { get; }

        public Command VolverCommand { get; }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            LimpiarPantalla();

            if (query.ContainsKey("resultadoCalculo"))
                ResultadoCalculo = query["resultadoCalculo"] as AnalisisSueloCalculoDataResponse;

            if (query.ContainsKey("requestGuardarAnalisis"))
                RequestGuardarAnalisis = query["requestGuardarAnalisis"] as AnalisisSueloGuardarCalculoRequest;

            if (RequestGuardarAnalisis?.TerrenoId != null && RequestGuardarAnalisis.TerrenoId > 0)
                TerrenoId = RequestGuardarAnalisis.TerrenoId;

            if (query.ContainsKey("terrenoId"))
            {
                if (int.TryParse(query["terrenoId"]?.ToString(), out int idTerreno))
                    TerrenoId = idTerreno;
            }

            if (query.ContainsKey("cantidadPlantas"))
                TotalPlantas = query["cantidadPlantas"]?.ToString() ?? string.Empty;

            AsignarNombreFormulaDesdeAnalisis();

            await InicializarAsync();
        }

        private void AsignarNombreFormulaDesdeAnalisis()
        {
            string identificador = RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identificador))
                identificador = "Fórmula balance nutricional";

            nombreFormula = identificador.Trim();

            OnPropertyChanged(nameof(NombreFormula));
            OnPropertyChanged(nameof(NombreAnalisisSuelo));
        }

        private async Task InicializarAsync()
        {
            try
            {
                IsBusy = true;
                RefrescarComandos();

                await CargarFuentesNutrientesAsync();
                CargarElementosDesdeResultado();

                Mensaje = "Seleccione una fuente de nutriente para cada elemento.";
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo cargar el balance de fórmula: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CargarFuentesNutrientesAsync()
        {
            FuentesNutrientes.Clear();

            ObservableCollection<FuenteNutrienteResponse> fuentes =
                await fuenteNutrienteApiService.GetFuenteNutrienteAsync();

            foreach (var fuente in fuentes)
            {
                if (fuente == null)
                    continue;

                if (fuente.FuenteNutrientesId == null || fuente.FuenteNutrientesId <= 0)
                    continue;

                if (fuente.Activo == false)
                    continue;

                if (!fuente.EsBalanceNutricional)
                    continue;

                if (!FuenteTieneAportesBalanceables(fuente))
                    continue;

                FuentesNutrientes.Add(fuente);
            }

            if (FuentesNutrientes.Count == 0)
            {
                await MostrarMensajeAsync(
                    "Fuentes de nutrientes",
                    "No se encontraron fuentes activas con aportes para balance de fórmula."
                );
            }
        }

        private static bool FuenteTieneAportesBalanceables(FuenteNutrienteResponse fuente)
        {
            if (fuente.ElementosQuimicos == null || fuente.ElementosQuimicos.Count == 0)
                return false;

            return fuente.ElementosQuimicos.Any(x =>
                EsElementoBalanceableActual(x.SimboloElementoQuimico)
            );
        }

        private void CargarElementosDesdeResultado()
        {
            ElementosBalance.Clear();

            if (ResultadoCalculo?.Elementos == null || ResultadoCalculo.Elementos.Count == 0)
            {
                Mensaje = "No se encontraron elementos en el resultado del análisis de suelo.";
                OnPropertyChanged(nameof(TieneElementosBalance));
                return;
            }

            var elementos = ResultadoCalculo.Elementos
                .Where(x => EsElementoBalanceableActual(x.SimboloElementoQuimico))
                .Where(x => (x.RequerimientoCalculado ?? 0) > 0)
                .OrderByDescending(x => x.RequerimientoCalculado ?? 0)
                .ToList();

            foreach (var elemento in elementos)
            {
                ObservableCollection<FuenteNutrienteResponse> fuentesElemento =
                    ObtenerFuentesParaElemento(elemento.SimboloElementoQuimico);

                ElementosBalance.Add(new BalanceFormulaElementoViewModel
                {
                    ElementoQuimicosId = elemento.ElementoQuimicosId,
                    SimboloElementoQuimico = elemento.SimboloElementoQuimico ?? string.Empty,
                    NombreElementoQuimico = elemento.NombreElementoQuimico ?? string.Empty,
                    RequerimientoLibras = elemento.RequerimientoCalculado ?? 0,
                    FuentesDisponibles = fuentesElemento,
                    FuenteCambiada = ProgramarRecalculoAutomatico
                });
            }

            OnPropertyChanged(nameof(TieneElementosBalance));
            RefrescarComandos();

            if (ElementosBalance.Count == 0)
            {
                Mensaje = "No hay requerimientos positivos para N, P, K, Ca o Mg.";
            }
        }

        private ObservableCollection<FuenteNutrienteResponse> ObtenerFuentesParaElemento(string? simboloElemento)
        {
            string simboloNormalizado = NormalizarSimbolo(simboloElemento);

            var fuentesFiltradas = FuentesNutrientes
                .Where(fuente =>
                    fuente.ElementosQuimicos != null &&
                    fuente.ElementosQuimicos.Any(elemento =>
                        NormalizarSimbolo(elemento.SimboloElementoQuimico) == simboloNormalizado &&
                        (elemento.CantidadAporte ?? 0) > 0
                    )
                )
                .OrderBy(fuente => fuente.NombreNutriente)
                .ToList();

            return new ObservableCollection<FuenteNutrienteResponse>(fuentesFiltradas);
        }

        private static bool EsElementoBalanceableActual(string? simbolo)
        {
            string simboloNormalizado = NormalizarSimbolo(simbolo);

            return SimbolosBalanceFormulaActuales.Contains(simboloNormalizado);
        }

        private static string NormalizarSimbolo(string? simbolo)
        {
            return (simbolo ?? string.Empty)
                .Trim()
                .ToUpper()
                .Replace(" ", "");
        }

        private void ProgramarRecalculoAutomatico()
        {
            RefrescarComandos();

            if (ComplementarConFertilizacionMixta)
                NotificarCambioComplemento(null);

            if (TieneResultadoBalance)
            {
                _ = CalculoAnalisisTemporalService.Instance.MarcarPendienteRecalculoAsync(
                    TipoCalculoTemporal.BalanceFormula,
                    "El balance de fórmula cambió. Se actualizará al recalcular automáticamente.",
                    true
                );
            }

            if (suspenderRecalculoAutomatico)
                return;

            recalculoCancellationTokenSource?.Cancel();
            recalculoCancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = recalculoCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(450, token);

                    if (token.IsCancellationRequested)
                        return;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await RecalcularBalanceAsync(false);
                    });
                }
                catch
                {
                }
            }, token);
        }

        private async Task RecalcularBalanceAsync(bool mostrarValidacion)
        {
            if (IsBusy)
                return;

            if (!ValidarDatosBase(mostrarValidacion, out int plantas, out int aplicaciones))
                return;

            var elementosSeleccionados = ElementosBalance
                .Where(x => x.FuenteSeleccionada != null)
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            if (elementosSeleccionados.Count == 0)
            {
                LimpiarResultadoBalance();

                await CalculoAnalisisTemporalService.Instance.ReiniciarCalculoAsync(
                    TipoCalculoTemporal.BalanceFormula,
                    "Balance de fórmula reiniciado porque no hay fuentes seleccionadas."
                );

                if (mostrarValidacion)
                    await MostrarMensajeAsync("Balance de fórmula", "Debe seleccionar al menos una fuente de nutriente.");

                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                BalanceNutricionalRequest request = ConstruirRequestBalance(
                    elementosSeleccionados,
                    plantas,
                    aplicaciones
                );

                BalanceNutricionalResponse? resultadoApi =
                    await balanceNutricionalApiService.CalcularAsync(request);

                if (resultadoApi == null)
                {
                    LimpiarResultadoBalance();
                    Mensaje = "La API no devolvió resultado para el balance de fórmula.";
                    return;
                }

                if (!resultadoApi.Success)
                {
                    LimpiarResultadoBalance();
                    Mensaje = resultadoApi.Message ?? "No se pudo calcular el balance de fórmula.";
                    return;
                }

                ProcesarResultadoApi(resultadoApi, plantas, aplicaciones);

                await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                    TipoCalculoTemporal.BalanceFormula,
                    request,
                    resultadoApi,
                    resultadoApi.Message ?? "Balance de fórmula calculado correctamente."
                );

                Mensaje = resultadoApi.Message ?? "Balance calculado correctamente.";
            }
            catch (Exception ex)
            {
                LimpiarResultadoBalance();
                await MostrarMensajeAsync("Error", $"No se pudo calcular el balance: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private BalanceNutricionalRequest ConstruirRequestBalance(
            List<BalanceFormulaElementoViewModel> elementosSeleccionados,
            int plantas,
            int aplicaciones)
        {
            var request = new BalanceNutricionalRequest
            {
                NombreFormula = ObtenerNombreFormulaResultado(),
                TerrenoId = TerrenoId,
                TotalPlantas = plantas,
                TotalAplicaciones = aplicaciones
            };

            foreach (var elemento in elementosSeleccionados)
            {
                if (elemento.FuenteSeleccionada == null)
                    continue;

                request.Items.Add(new BalanceNutricionalItemRequest
                {
                    FuenteNutrientesId = elemento.FuenteSeleccionada.FuenteNutrientesId,
                    ElementoQuimicosId = elemento.ElementoQuimicosId,
                    RequerimientoLibras = elemento.RequerimientoLibras
                });
            }

            return request;
        }

        private void ProcesarResultadoApi(
            BalanceNutricionalResponse resultadoApi,
            int plantas,
            int aplicaciones)
        {
            LimpiarResultadoBalance();

            resultadoApi.NombreFormula = ObtenerNombreFormulaResultado();

            ResultadoBalance = resultadoApi;

            ConstruirFormulaComercialVisual(resultadoApi);

            DosisPlantaSeleccionada = Redondear(resultadoApi.DosisPlantaPorAplicacionOz ?? 0, 4);

            EtiquetaDosisPlantaSeleccionada = aplicaciones == 1
                ? "Dosis por planta en 1 aplicación"
                : $"Dosis por planta en {aplicaciones} aplicaciones";

            TituloColumnaAplicaciones = aplicaciones == 1
                ? "1 aplicación"
                : $"{aplicaciones} aplicaciones";

            ConstruirFilasResultadoDesdeApi(resultadoApi);
            ConstruirTotalesAportesDesdeApi(resultadoApi);
            ConstruirTablaFormulaVisualDesdeApi(resultadoApi);
            ConstruirTablaAplicacionesDesdeApi(resultadoApi, plantas, aplicaciones);
            ConstruirTablaPreciosDesdeApi(resultadoApi);

            TieneResultadoBalance = true;

            OnPropertyChanged(nameof(ResultadoBalance));
            OnPropertyChanged(nameof(TieneResultadoBalance));
            OnPropertyChanged(nameof(TieneTablaBalance));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
            OnPropertyChanged(nameof(TieneTablaPrecios));
            OnPropertyChanged(nameof(TieneFormulaComercial));
            OnPropertyChanged(nameof(TextoEstadoComplemento));

            if (ComplementarConFertilizacionMixta)
                NotificarCambioComplemento(ConstruirContextoComplemento());
        }

        public BalanceFertilizacionMixtaContext? ConstruirContextoComplemento()
        {
            if (!TieneResultadoBalance || ResultadoBalance == null)
                return null;

            if (!int.TryParse(TotalPlantas, out int plantas) || plantas <= 0)
                return null;

            if (!int.TryParse(TotalAplicaciones, out int aplicaciones) || aplicaciones <= 0)
                return null;

            List<BalanceFormulaElementoViewModel> elementosSeleccionados =
                ElementosBalance
                    .Where(x => x.FuenteSeleccionada != null)
                    .ToList();

            if (elementosSeleccionados.Count == 0)
                return null;

            BalanceFertilizacionMixtaContext contexto = new()
            {
                NombreFormula = ObtenerNombreFormulaResultado(),
                TerrenoId = TerrenoId,
                TotalPlantas = plantas,
                TotalAplicaciones = aplicaciones,
                ResultadoOriginal = ResultadoBalance,
                CostoCompraOriginal = ResultadoBalance.Detalle.Sum(x =>
                    Math.Ceiling(x.QuintalesAnuales ?? 0) *
                    (x.PrecioPorQuintal ?? 0)
                )
            };

            foreach (BalanceFormulaElementoViewModel elemento in elementosSeleccionados)
            {
                contexto.Items.Add(new BalanceFertilizacionMixtaItem
                {
                    FuenteNutrientesId = elemento.FuenteSeleccionada?.FuenteNutrientesId,
                    NombreFuente = elemento.FuenteSeleccionada?.NombreNutriente ?? string.Empty,
                    ElementoQuimicosId = elemento.ElementoQuimicosId,
                    SimboloElementoQuimico = elemento.SimboloElementoQuimico,
                    RequerimientoOriginal = elemento.RequerimientoLibras
                });
            }

            return contexto;
        }

        private void NotificarCambioComplemento(
            BalanceFertilizacionMixtaContext? contexto)
        {
            ComplementoFertilizacionMixtaCambiado?.Invoke(
                this,
                new BalanceFertilizacionMixtaChangedEventArgs(
                    ComplementarConFertilizacionMixta,
                    contexto
                )
            );
        }

        private void ConstruirFilasResultadoDesdeApi(BalanceNutricionalResponse resultadoApi)
        {
            FilasResultado.Clear();

            foreach (var detalle in resultadoApi.Detalle)
            {
                FilasResultado.Add(new BalanceFormulaFilaResultadoViewModel
                {
                    Fuente = detalle.Fuente ?? "Fuente",
                    Elemento = FormatearSimboloTabla(detalle.Elemento),
                    RequerimientoLibras = detalle.RequerimientoLibras ?? 0,
                    RequerimientoPendienteLibras = detalle.RequerimientoLibras ?? 0,
                    LibrasAnuales = detalle.LibrasAnuales ?? 0,
                    OnzasAnuales = detalle.OnzasAnuales ?? 0,
                    OnzasPorAplicacion = detalle.OnzasPorAplicacion ?? 0,
                    LibrasPorAplicacion = detalle.LibrasPorAplicacion ?? 0,
                    QuintalesAnuales = detalle.QuintalesAnuales ?? 0,
                    PrecioPorQuintal = detalle.PrecioPorQuintal ?? 0,
                    SubtotalFuente = detalle.SubtotalFuente ?? 0,
                    Aportes = detalle.Aportes ?? new Dictionary<string, decimal>()
                });
            }

            var ordenadas = FilasResultado
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            FilasResultado.Clear();

            foreach (var fila in ordenadas)
                FilasResultado.Add(fila);
        }

        private void ConstruirTotalesAportesDesdeApi(BalanceNutricionalResponse resultadoApi)
        {
            TotalesAportes.Clear();

            foreach (var item in resultadoApi.FormulaComercial)
            {
                TotalesAportes.Add(new BalanceFormulaAporteViewModel
                {
                    Simbolo = FormatearSimboloTabla(item.Key),
                    Nombre = FormatearSimboloTabla(item.Key),
                    Libras = item.Value
                });
            }
        }

        private void ConstruirFormulaComercialVisual(BalanceNutricionalResponse resultadoApi)
        {
            FormulaComercialVisual.Clear();

            List<string> elementosTabla = ObtenerElementosTablaDesdeApi(resultadoApi);

            foreach (string simbolo in elementosTabla)
            {
                decimal valor = ObtenerValorDiccionarioAporte(resultadoApi.FormulaComercial, simbolo);

                FormulaComercialVisual.Add(new BalanceFormulaComercialViewModel
                {
                    Simbolo = FormatearSimboloTabla(simbolo),
                    Valor = valor
                });
            }

            OnPropertyChanged(nameof(FormulaComercialVisual));
            OnPropertyChanged(nameof(TieneFormulaComercial));
        }

        private void ConstruirTablaFormulaVisualDesdeApi(BalanceNutricionalResponse resultadoApi)
        {
            EncabezadosElementosTabla.Clear();
            FilasTablaBalance.Clear();
            FilaFormula = null;

            List<string> elementosTabla = ObtenerElementosTablaDesdeApi(resultadoApi);

            foreach (string simbolo in elementosTabla)
                EncabezadosElementosTabla.Add(simbolo);

            foreach (var detalle in resultadoApi.Detalle)
            {
                var fila = new BalanceFormulaTablaFilaViewModel
                {
                    Fuente = detalle.Fuente ?? "Fuente",
                    Libras = detalle.LibrasAnuales ?? 0,
                    Quintales = detalle.QuintalesAnuales ?? 0
                };

                foreach (string simbolo in elementosTabla)
                {
                    decimal valor = ObtenerValorDiccionarioAporte(detalle.Aportes, simbolo);

                    fila.Celdas.Add(new BalanceFormulaTablaCeldaViewModel
                    {
                        Simbolo = simbolo,
                        Valor = valor
                    });
                }

                FilasTablaBalance.Add(fila);
            }

            var filaTotal = new BalanceFormulaTablaFilaViewModel
            {
                Fuente = "Total",
                Libras = resultadoApi.TotalMezclaLb ?? 0,
                Quintales = resultadoApi.TotalMezclaQq ?? 0,
                TextoUltimaColumna = "Fórmula"
            };

            foreach (string simbolo in elementosTabla)
            {
                decimal valorFormula = ObtenerValorDiccionarioAporte(resultadoApi.FormulaComercial, simbolo);

                filaTotal.Celdas.Add(new BalanceFormulaTablaCeldaViewModel
                {
                    Simbolo = simbolo,
                    Valor = valorFormula
                });
            }

            FilaFormula = filaTotal;

            OnPropertyChanged(nameof(EncabezadosElementosTabla));
            OnPropertyChanged(nameof(FilasTablaBalance));
            OnPropertyChanged(nameof(TieneTablaBalance));
        }

        private void ConstruirTablaAplicacionesDesdeApi(
            BalanceNutricionalResponse resultadoApi,
            int plantas,
            int aplicaciones)
        {
            FilasAplicaciones.Clear();
            FilaTotalAplicaciones = null;
            FilaOnzasPlanta = null;

            foreach (var detalle in resultadoApi.Detalle)
            {
                FilasAplicaciones.Add(new BalanceFormulaAplicacionViewModel
                {
                    Formula = detalle.Fuente ?? "Fuente",
                    LibrasAnuales = detalle.LibrasAnuales ?? 0,
                    OnzasAnuales = detalle.OnzasAnuales ?? 0,
                    OnzasPorAplicacion = detalle.OnzasPorAplicacion ?? 0
                });
            }

            decimal totalOnzas = resultadoApi.TotalMezclaOz ?? 0;
            decimal totalOnzasPorAplicacion = aplicaciones > 0
                ? totalOnzas / aplicaciones
                : 0;

            FilaTotalAplicaciones = new BalanceFormulaAplicacionViewModel
            {
                Formula = "Total",
                LibrasAnuales = resultadoApi.TotalMezclaLb ?? 0,
                OnzasAnuales = totalOnzas,
                OnzasPorAplicacion = totalOnzasPorAplicacion,
                EsFilaTotal = true
            };

            FilaOnzasPlanta = new BalanceFormulaAplicacionViewModel
            {
                Formula = "Onz/planta",
                LibrasAnuales = resultadoApi.TotalMezclaQq ?? 0,
                OnzasAnuales = resultadoApi.DosisPlantaAnualOz ?? 0,
                OnzasPorAplicacion = resultadoApi.DosisPlantaPorAplicacionOz ?? 0,
                EsFilaOnzasPlanta = true
            };

            OnPropertyChanged(nameof(FilasAplicaciones));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
        }

        private void ConstruirTablaPreciosDesdeApi(BalanceNutricionalResponse resultadoApi)
        {
            FilasPrecio.Clear();
            FilaTotalPrecio = null;

            foreach (var detalle in resultadoApi.Detalle)
            {
                decimal quintalesAnuales = detalle.QuintalesAnuales ?? 0;
                decimal precioPorQuintal = detalle.PrecioPorQuintal ?? 0;
                decimal quintalesComprar = Math.Ceiling(quintalesAnuales);

                FilasPrecio.Add(new BalanceFormulaPrecioViewModel
                {
                    Fuente = detalle.Fuente ?? "Fuente",
                    LibrasAnuales = detalle.LibrasAnuales ?? 0,
                    QuintalesAnuales = quintalesAnuales,
                    PrecioPorQuintal = precioPorQuintal,
                    SubtotalFuente = detalle.SubtotalFuente ?? 0,
                    QuintalesComprar = quintalesComprar,
                    CostoCompra = quintalesComprar * precioPorQuintal
                });
            }

            FilaTotalPrecio = new BalanceFormulaPrecioViewModel
            {
                Fuente = "Total",
                LibrasAnuales = resultadoApi.TotalMezclaLb ?? 0,
                QuintalesAnuales = resultadoApi.TotalMezclaQq ?? 0,
                PrecioPorQuintal = 0,
                SubtotalFuente = resultadoApi.PrecioTotalFormula ?? 0,
                QuintalesComprar = FilasPrecio.Sum(x => x.QuintalesComprar),
                CostoCompra = FilasPrecio.Sum(x => x.CostoCompra),
                EsTotal = true
            };

            OnPropertyChanged(nameof(FilasPrecio));
            OnPropertyChanged(nameof(TieneTablaPrecios));
        }

        private List<string> ObtenerElementosTablaDesdeApi(BalanceNutricionalResponse resultadoApi)
        {
            var elementos = new List<string>();

            foreach (var item in resultadoApi.FormulaComercial.Keys)
            {
                string simbolo = FormatearSimboloTabla(item);

                if (!elementos.Contains(simbolo))
                    elementos.Add(simbolo);
            }

            foreach (var detalle in resultadoApi.Detalle)
            {
                foreach (var aporte in detalle.Aportes.Keys)
                {
                    string simbolo = FormatearSimboloTabla(aporte);

                    if (!elementos.Contains(simbolo))
                        elementos.Add(simbolo);
                }
            }

            return elementos;
        }

        private static decimal ObtenerValorDiccionarioAporte(Dictionary<string, decimal>? diccionario, string simbolo)
        {
            if (diccionario == null || diccionario.Count == 0)
                return 0;

            string simboloNormalizado = NormalizarSimbolo(simbolo);

            foreach (var item in diccionario)
            {
                if (NormalizarSimbolo(item.Key) == simboloNormalizado)
                    return item.Value;
            }

            return 0;
        }

        private string ObtenerNombreFormulaResultado()
        {
            string nombreAnalisis = RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombreAnalisis))
                nombreAnalisis = NombreAnalisisSuelo;

            if (string.IsNullOrWhiteSpace(nombreAnalisis))
                return "Fórmula balance nutricional";

            nombreAnalisis = nombreAnalisis.Trim();

            if (nombreAnalisis.StartsWith("Fórmula balance nutricional", StringComparison.OrdinalIgnoreCase))
                return nombreAnalisis;

            return $"Fórmula balance nutricional - {nombreAnalisis}";
        }

        private async Task ReiniciarBalanceAsync()
        {
            if (IsBusy)
                return;

            try
            {
                suspenderRecalculoAutomatico = true;

                recalculoCancellationTokenSource?.Cancel();

                foreach (var elemento in ElementosBalance)
                {
                    elemento.FuenteSeleccionada = null;
                }

                LimpiarResultadoBalance();

                Mensaje = "Seleccione nuevamente las fuentes de nutrientes para volver a balancear la fórmula.";
            }
            finally
            {
                suspenderRecalculoAutomatico = false;
                RefrescarComandos();
            }

            await Task.CompletedTask;
        }

        private bool ValidarDatosBase(bool mostrarMensaje, out int plantas, out int aplicaciones)
        {
            plantas = 0;
            aplicaciones = 0;

            if (TerrenoId == null || TerrenoId <= 0)
            {
                if (mostrarMensaje)
                    _ = MostrarMensajeAsync("Validación", "No se encontró el terreno seleccionado.");

                return false;
            }

            if (!int.TryParse(TotalPlantas, out plantas) || plantas <= 0)
            {
                if (mostrarMensaje)
                    _ = MostrarMensajeAsync("Validación", "La cantidad de plantas debe ser mayor a cero.");

                return false;
            }

            if (!int.TryParse(TotalAplicaciones, out aplicaciones) || aplicaciones <= 0)
            {
                if (mostrarMensaje)
                    _ = MostrarMensajeAsync("Validación", "El total de aplicaciones debe ser mayor a cero.");

                return false;
            }

            if (aplicaciones > 4)
            {
                TotalAplicaciones = "4";

                if (mostrarMensaje)
                    _ = MostrarMensajeAsync("Validación", "El total de aplicaciones no puede ser mayor a 4.");

                return false;
            }

            return true;
        }

        private static string FormatearSimboloTabla(string? simbolo)
        {
            string s = NormalizarSimbolo(simbolo);

            return s switch
            {
                "CA" => "Ca",
                "MG" => "Mg",
                "ZN" => "Zn",
                _ => s
            };
        }

        private static decimal Redondear(decimal valor, int decimales = 4)
        {
            return Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
        }

        private void LimpiarResultadoBalance()
        {
            FilasResultado.Clear();
            TotalesAportes.Clear();
            EncabezadosElementosTabla.Clear();
            FilasTablaBalance.Clear();
            FilasAplicaciones.Clear();
            FilasPrecio.Clear();
            FormulaComercialVisual.Clear();

            FilaTotalAplicaciones = null;
            FilaOnzasPlanta = null;
            FilaTotalPrecio = null;

            TituloColumnaAplicaciones = "Aplicaciones";
            EtiquetaDosisPlantaSeleccionada = "Dosis por planta";
            DosisPlantaSeleccionada = 0;

            FilaFormula = null;
            ResultadoBalance = null;
            TieneResultadoBalance = false;

            OnPropertyChanged(nameof(ResultadoBalance));
            OnPropertyChanged(nameof(TieneResultadoBalance));
            OnPropertyChanged(nameof(TieneTablaBalance));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
            OnPropertyChanged(nameof(TieneTablaPrecios));
            OnPropertyChanged(nameof(TieneFormulaComercial));
            OnPropertyChanged(nameof(TextoEstadoComplemento));

            if (ComplementarConFertilizacionMixta)
                NotificarCambioComplemento(null);
        }

        private void LimpiarPantalla()
        {
            recalculoCancellationTokenSource?.Cancel();

            Mensaje = string.Empty;
            ErrorTotalAplicaciones = string.Empty;

            ElementosBalance.Clear();
            FilasResultado.Clear();
            TotalesAportes.Clear();

            EncabezadosElementosTabla.Clear();
            FilasTablaBalance.Clear();
            FilasAplicaciones.Clear();
            FilasPrecio.Clear();
            FormulaComercialVisual.Clear();

            FilaTotalAplicaciones = null;
            FilaOnzasPlanta = null;
            FilaTotalPrecio = null;

            TituloColumnaAplicaciones = "Aplicaciones";
            EtiquetaDosisPlantaSeleccionada = "Dosis por planta";
            DosisPlantaSeleccionada = 0;

            FilaFormula = null;

            ResultadoBalance = null;
            TieneResultadoBalance = false;

            complementarConFertilizacionMixta = false;
            OnPropertyChanged(nameof(ComplementarConFertilizacionMixta));
            OnPropertyChanged(nameof(TextoEstadoComplemento));

            nombreFormula = "Fórmula balance nutricional";
            OnPropertyChanged(nameof(NombreFormula));
            OnPropertyChanged(nameof(NombreAnalisisSuelo));

            OnPropertyChanged(nameof(ResultadoBalance));
            OnPropertyChanged(nameof(TieneTablaBalance));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
            OnPropertyChanged(nameof(TieneTablaPrecios));
            OnPropertyChanged(nameof(TieneFormulaComercial));
        }

        private async Task VolverAsync()
        {
            await GoToAsyncParameters("//ResultadoAnalisisSueloPage");
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeRecalcular));
            RecalcularCommand.ChangeCanExecute();
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }

    public class BalanceFormulaElementoViewModel : BindableObject
    {
        private FuenteNutrienteResponse? fuenteSeleccionada;

        public int? ElementoQuimicosId { get; set; }

        public string SimboloElementoQuimico { get; set; } = string.Empty;

        public string NombreElementoQuimico { get; set; } = string.Empty;

        public decimal RequerimientoLibras { get; set; }

        public ObservableCollection<FuenteNutrienteResponse> FuentesDisponibles { get; set; } = new();

        public Action? FuenteCambiada { get; set; }

        public FuenteNutrienteResponse? FuenteSeleccionada
        {
            get => fuenteSeleccionada;
            set
            {
                fuenteSeleccionada = value;
                OnPropertyChanged(nameof(FuenteSeleccionada));
                OnPropertyChanged(nameof(TieneFuenteSeleccionada));
                OnPropertyChanged(nameof(TextoFuenteSeleccionada));
                OnPropertyChanged(nameof(TextoAporteFuenteSeleccionada));
                FuenteCambiada?.Invoke();
            }
        }

        public bool TieneFuenteSeleccionada => FuenteSeleccionada != null;

        public string TextoRequerimiento =>
            $"{RequerimientoLibras.ToString("N2", CultureInfo.InvariantCulture)} lb";

        public string TextoCantidadFuentes =>
            FuentesDisponibles.Count == 1
                ? "1 fuente disponible para este elemento"
                : $"{FuentesDisponibles.Count} fuentes disponibles para este elemento";

        public string TextoFuenteSeleccionada
        {
            get
            {
                if (FuenteSeleccionada == null)
                    return string.Empty;

                return FuenteSeleccionada.NombreMostrar;
            }
        }

        public string TextoAporteFuenteSeleccionada
        {
            get
            {
                if (FuenteSeleccionada == null)
                    return string.Empty;

                string simboloActual = NormalizarSimboloLocal(SimboloElementoQuimico);

                var aporte = FuenteSeleccionada.ElementosQuimicos?
                    .FirstOrDefault(x => NormalizarSimboloLocal(x.SimboloElementoQuimico) == simboloActual);

                if (aporte == null)
                    return string.Empty;

                return $"Aporta {aporte.CantidadAporte:N0}% de {SimboloElementoQuimico.Trim()}";
            }
        }

        private static string NormalizarSimboloLocal(string? simbolo)
        {
            return (simbolo ?? string.Empty)
                .Trim()
                .ToUpper()
                .Replace(" ", "");
        }
    }

    public class BalanceFormulaFilaResultadoViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public string Elemento { get; set; } = string.Empty;

        public decimal RequerimientoLibras { get; set; }

        public decimal RequerimientoPendienteLibras { get; set; }

        public decimal LibrasAnuales { get; set; }

        public decimal OnzasAnuales { get; set; }

        public decimal OnzasPorAplicacion { get; set; }

        public decimal LibrasPorAplicacion { get; set; }

        public decimal QuintalesAnuales { get; set; }

        public decimal PrecioPorQuintal { get; set; }

        public decimal SubtotalFuente { get; set; }

        public Dictionary<string, decimal> Aportes { get; set; } = new();
    }

    public class BalanceFormulaAporteViewModel
    {
        public string Simbolo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public decimal Libras { get; set; }
    }

    public class BalanceFormulaComercialViewModel
    {
        public string Simbolo { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string TextoFormula =>
            $"{Simbolo} {Valor.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    public class BalanceFormulaTablaFilaViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public decimal Libras { get; set; }

        public decimal Quintales { get; set; }

        public string TextoUltimaColumna { get; set; } = string.Empty;

        public ObservableCollection<BalanceFormulaTablaCeldaViewModel> Celdas { get; set; } = new();

        public string TextoLibras => Libras.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoQuintales => Quintales.ToString("N2", CultureInfo.InvariantCulture);
    }

    public class BalanceFormulaTablaCeldaViewModel
    {
        public string Simbolo { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string TextoValor => Valor == 0
            ? "0"
            : Valor.ToString("N2", CultureInfo.InvariantCulture);
    }

    public class BalanceFormulaAplicacionViewModel
    {
        public string Formula { get; set; } = string.Empty;

        public decimal LibrasAnuales { get; set; }

        public decimal OnzasAnuales { get; set; }

        public decimal OnzasPorAplicacion { get; set; }

        public bool EsFilaTotal { get; set; }

        public bool EsFilaOnzasPlanta { get; set; }

        public string TextoFormula => Formula;

        public string TextoLibrasAnuales
        {
            get
            {
                if (EsFilaOnzasPlanta)
                    return LibrasAnuales.ToString("N4", CultureInfo.InvariantCulture);

                return LibrasAnuales.ToString("N2", CultureInfo.InvariantCulture);
            }
        }

        public string TextoOnzasAnuales =>
            OnzasAnuales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoOnzasPorAplicacion =>
            OnzasPorAplicacion.ToString("N2", CultureInfo.InvariantCulture);
    }

    public class BalanceFormulaPrecioViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public decimal LibrasAnuales { get; set; }

        public decimal QuintalesAnuales { get; set; }

        public decimal PrecioPorQuintal { get; set; }

        public decimal SubtotalFuente { get; set; }

        public decimal QuintalesComprar { get; set; }

        public decimal CostoCompra { get; set; }

        public bool EsTotal { get; set; }

        public string TextoFuente => Fuente;

        public string TextoLibrasAnuales =>
            LibrasAnuales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoQuintalesAnuales =>
            QuintalesAnuales.ToString("N3", CultureInfo.InvariantCulture);

        public string TextoPrecioPorQuintal =>
            EsTotal
                ? "-"
                : $"C$ {PrecioPorQuintal.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoSubtotalFuente =>
            $"C$ {SubtotalFuente.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoQuintalesComprar =>
            QuintalesComprar.ToString("N0", CultureInfo.InvariantCulture);

        public string TextoCostoCompra =>
            $"C$ {CostoCompra.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
