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

            RecalcularCommand = new Command(
                async () => await ReiniciarBalanceAsync(),
                () => PuedeRecalcular
            );

            VolverCommand = new Command(
                async () => await VolverAsync()
            );
        }

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
                nombreFormula = value;
                OnPropertyChanged(nameof(NombreFormula));
                RefrescarComandos();
                ProgramarRecalculoAutomatico();
            }
        }

        public string TotalPlantas
        {
            get => totalPlantas;
            set
            {
                totalPlantas = value;
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

            await InicializarAsync();
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

                if (mostrarValidacion)
                    await MostrarMensajeAsync("Balance de fórmula", "Debe seleccionar al menos una fuente de nutriente.");

                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                CalcularBalanceLocal(elementosSeleccionados, plantas, aplicaciones);

                Mensaje = "Balance calculado correctamente.";
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo calcular el balance: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
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

        private void CalcularBalanceLocal(
            List<BalanceFormulaElementoViewModel> elementosSeleccionados,
            int plantas,
            int aplicaciones)
        {
            FilasResultado.Clear();
            TotalesAportes.Clear();
            EncabezadosElementosTabla.Clear();
            FilasTablaBalance.Clear();
            FilasAplicaciones.Clear();
            FilasPrecio.Clear();

            FilaTotalAplicaciones = null;
            FilaOnzasPlanta = null;
            FilaTotalPrecio = null;

            TituloColumnaAplicaciones = "Aplicaciones";
            EtiquetaDosisPlantaSeleccionada = "Dosis por planta";
            DosisPlantaSeleccionada = 0;

            FilaFormula = null;
            ResultadoBalance = null;
            TieneResultadoBalance = false;

            foreach (var elemento in elementosSeleccionados)
            {
                FuenteNutrienteResponse? fuente = elemento.FuenteSeleccionada;

                if (fuente == null)
                    continue;

                string simboloElementoPrincipal = NormalizarSimbolo(elemento.SimboloElementoQuimico);

                decimal porcentajePrincipal = ObtenerPorcentajeAporteFuente(fuente, simboloElementoPrincipal);

                if (porcentajePrincipal <= 0)
                    continue;

                decimal librasFuente = elemento.RequerimientoLibras;
                decimal onzasAnuales = librasFuente * 16m;
                decimal quintalesAnuales = librasFuente / 100m;
                decimal precioPorQuintal = fuente.PrecioNutriente ?? 0;
                decimal subtotalFuente = quintalesAnuales * precioPorQuintal;

                FilasResultado.Add(new BalanceFormulaFilaResultadoViewModel
                {
                    FuenteNutriente = fuente,
                    Fuente = fuente.NombreNutriente ?? "Fuente",
                    Elemento = FormatearSimboloTabla(simboloElementoPrincipal),
                    RequerimientoLibras = elemento.RequerimientoLibras,
                    RequerimientoPendienteLibras = elemento.RequerimientoLibras,
                    LibrasAnuales = librasFuente,
                    OnzasAnuales = onzasAnuales,
                    DosAplicaciones = onzasAnuales / 2m,
                    TresAplicaciones = onzasAnuales / 3m,
                    QuintalesAnuales = quintalesAnuales,
                    PrecioPorQuintal = precioPorQuintal,
                    SubtotalFuente = subtotalFuente
                });
            }

            FilasResultadoOrdenadas();

            decimal totalMezclaLb = FilasResultado.Sum(x => x.LibrasAnuales);
            decimal totalMezclaOz = totalMezclaLb * 16m;
            decimal totalMezclaQq = totalMezclaLb / 100m;
            decimal precioTotal = FilasResultado.Sum(x => x.SubtotalFuente);

            decimal onzasPorAplicacion = aplicaciones > 0
                ? totalMezclaOz / aplicaciones
                : 0;

            decimal dosisPorPlantaSeleccionada = plantas > 0
                ? onzasPorAplicacion / plantas
                : 0;

            EtiquetaDosisPlantaSeleccionada = aplicaciones == 1
                ? "Dosis por planta en 1 aplicación"
                : $"Dosis por planta en {aplicaciones} aplicaciones";

            DosisPlantaSeleccionada = Redondear(dosisPorPlantaSeleccionada, 4);

            ResultadoBalance = new BalanceNutricionalResponse
            {
                Success = true,
                Message = "Balance calculado.",
                BalanceNutricionalId = null,
                NombreFormula = string.IsNullOrWhiteSpace(NombreFormula)
                    ? "Fórmula balance nutricional"
                    : NombreFormula.Trim(),

                TotalMezclaLb = Redondear(totalMezclaLb),
                TotalMezclaOz = Redondear(totalMezclaOz),
                LibrasPorDosAplicaciones = Redondear(totalMezclaLb / 2m),
                LibrasPorTresAplicaciones = Redondear(totalMezclaLb / 3m),
                TotalPlantas = plantas,
                DosisPlantaAnualOz = plantas > 0 ? Redondear(totalMezclaOz / plantas, 4) : 0,
                TotalMezclaQq = Redondear(totalMezclaQq),
                PrecioTotalFormula = Redondear(precioTotal),
                PrecioPorAplicacion = aplicaciones > 0 ? Redondear(precioTotal / aplicaciones) : 0,

                DosAplicaciones = new BalanceNutricionalAplicacionResponse
                {
                    DosisPlantaOz = plantas > 0 ? Redondear((totalMezclaOz / 2m) / plantas, 4) : 0
                },

                TresAplicaciones = new BalanceNutricionalAplicacionResponse
                {
                    DosisPlantaOz = plantas > 0 ? Redondear((totalMezclaOz / 3m) / plantas, 4) : 0
                },

                Detalle = new List<BalanceNutricionalDetalleResponse>()
            };

            foreach (var fila in FilasResultado)
            {
                ResultadoBalance.Detalle.Add(new BalanceNutricionalDetalleResponse
                {
                    Fuente = fila.Fuente,
                    Elemento = fila.Elemento,
                    RequerimientoLibras = fila.RequerimientoLibras,
                    LibrasAnuales = fila.LibrasAnuales,
                    OnzasAnuales = fila.OnzasAnuales,
                    DosAplicaciones = fila.DosAplicaciones,
                    TresAplicaciones = fila.TresAplicaciones,
                    QuintalesAnuales = fila.QuintalesAnuales,
                    PrecioPorQuintal = fila.PrecioPorQuintal,
                    SubtotalFuente = fila.SubtotalFuente
                });
            }

            CalcularTotalesAportes();
            ConstruirTablaFormulaVisual();
            ConstruirTablaAplicaciones(plantas, aplicaciones);
            ConstruirTablaPrecios();

            TieneResultadoBalance = FilasResultado.Count > 0;

            OnPropertyChanged(nameof(ResultadoBalance));
            OnPropertyChanged(nameof(TieneResultadoBalance));
            OnPropertyChanged(nameof(TieneTablaBalance));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
            OnPropertyChanged(nameof(TieneTablaPrecios));
        }

        private void FilasResultadoOrdenadas()
        {
            var ordenadas = FilasResultado
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            FilasResultado.Clear();

            foreach (var fila in ordenadas)
                FilasResultado.Add(fila);
        }

        private void CalcularTotalesAportes()
        {
            TotalesAportes.Clear();

            var elementosTabla = ObtenerElementosTablaFormula();

            foreach (string simbolo in elementosTabla)
            {
                decimal totalAporte = FilasResultado
                    .Sum(fila =>
                    {
                        decimal porcentaje = ObtenerPorcentajeAporteFuente(fila.FuenteNutriente, simbolo);
                        return fila.LibrasAnuales * (porcentaje / 100m);
                    });

                TotalesAportes.Add(new BalanceFormulaAporteViewModel
                {
                    Simbolo = simbolo,
                    Nombre = simbolo,
                    Libras = totalAporte
                });
            }
        }

        private void ConstruirTablaFormulaVisual()
        {
            EncabezadosElementosTabla.Clear();
            FilasTablaBalance.Clear();
            FilaFormula = null;

            var elementosTabla = ObtenerElementosTablaFormula();

            foreach (string simbolo in elementosTabla)
                EncabezadosElementosTabla.Add(simbolo);

            var filasOrdenadas = FilasResultado
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            foreach (var detalle in filasOrdenadas)
            {
                var fila = new BalanceFormulaTablaFilaViewModel
                {
                    Fuente = detalle.Fuente,
                    Libras = detalle.LibrasAnuales,
                    Quintales = detalle.QuintalesAnuales
                };

                foreach (string simbolo in elementosTabla)
                {
                    decimal porcentajeAporte = ObtenerPorcentajeAporteFuente(detalle.FuenteNutriente, simbolo);
                    decimal aporteLibras = detalle.LibrasAnuales * (porcentajeAporte / 100m);

                    fila.Celdas.Add(new BalanceFormulaTablaCeldaViewModel
                    {
                        Simbolo = simbolo,
                        Valor = aporteLibras
                    });
                }

                FilasTablaBalance.Add(fila);
            }

            decimal totalLibras = FilasTablaBalance.Sum(x => x.Libras);
            decimal totalQuintales = FilasTablaBalance.Sum(x => x.Quintales);

            var filaTotal = new BalanceFormulaTablaFilaViewModel
            {
                Fuente = "Total",
                Libras = totalLibras,
                Quintales = totalQuintales,
                TextoUltimaColumna = "→ Fórmula"
            };

            foreach (string simbolo in elementosTabla)
            {
                decimal totalAporteElemento = FilasTablaBalance
                    .SelectMany(x => x.Celdas)
                    .Where(x => NormalizarSimbolo(x.Simbolo) == NormalizarSimbolo(simbolo))
                    .Sum(x => x.Valor);

                decimal valorFormula = totalQuintales > 0
                    ? totalAporteElemento / totalQuintales
                    : 0m;

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

        private void ConstruirTablaAplicaciones(int plantas, int aplicaciones)
        {
            FilasAplicaciones.Clear();
            FilaTotalAplicaciones = null;
            FilaOnzasPlanta = null;

            if (plantas <= 0 || aplicaciones <= 0)
            {
                OnPropertyChanged(nameof(TieneTablaAplicaciones));
                return;
            }

            TituloColumnaAplicaciones = aplicaciones == 1
                ? "1 aplicación"
                : $"{aplicaciones} aplicaciones";

            var filasOrdenadas = FilasResultado
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            foreach (var fila in filasOrdenadas)
            {
                decimal librasAnuales = fila.LibrasAnuales;
                decimal onzasAnuales = librasAnuales * 16m;
                decimal onzasPorAplicacion = onzasAnuales / aplicaciones;

                FilasAplicaciones.Add(new BalanceFormulaAplicacionViewModel
                {
                    Formula = fila.Fuente,
                    LibrasAnuales = librasAnuales,
                    OnzasAnuales = onzasAnuales,
                    OnzasPorAplicacion = onzasPorAplicacion
                });
            }

            decimal totalLibras = FilasAplicaciones.Sum(x => x.LibrasAnuales);
            decimal totalOnzas = FilasAplicaciones.Sum(x => x.OnzasAnuales);
            decimal totalOnzasPorAplicacion = totalOnzas / aplicaciones;
            decimal totalQuintales = totalLibras / 100m;

            FilaTotalAplicaciones = new BalanceFormulaAplicacionViewModel
            {
                Formula = "Total",
                LibrasAnuales = totalLibras,
                OnzasAnuales = totalOnzas,
                OnzasPorAplicacion = totalOnzasPorAplicacion,
                EsFilaTotal = true
            };

            FilaOnzasPlanta = new BalanceFormulaAplicacionViewModel
            {
                Formula = "Onz/planta",
                LibrasAnuales = totalQuintales,
                OnzasAnuales = totalOnzas / plantas,
                OnzasPorAplicacion = totalOnzasPorAplicacion / plantas,
                EsFilaOnzasPlanta = true
            };

            OnPropertyChanged(nameof(FilasAplicaciones));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
        }

        private void ConstruirTablaPrecios()
        {
            FilasPrecio.Clear();
            FilaTotalPrecio = null;

            var filasOrdenadas = FilasResultado
                .OrderByDescending(x => x.RequerimientoLibras)
                .ToList();

            foreach (var fila in filasOrdenadas)
            {
                FilasPrecio.Add(new BalanceFormulaPrecioViewModel
                {
                    Fuente = fila.Fuente,
                    LibrasAnuales = fila.LibrasAnuales,
                    QuintalesAnuales = fila.QuintalesAnuales,
                    PrecioPorQuintal = fila.PrecioPorQuintal,
                    SubtotalFuente = fila.SubtotalFuente
                });
            }

            FilaTotalPrecio = new BalanceFormulaPrecioViewModel
            {
                Fuente = "Total",
                LibrasAnuales = FilasPrecio.Sum(x => x.LibrasAnuales),
                QuintalesAnuales = FilasPrecio.Sum(x => x.QuintalesAnuales),
                PrecioPorQuintal = 0,
                SubtotalFuente = FilasPrecio.Sum(x => x.SubtotalFuente),
                EsTotal = true
            };

            OnPropertyChanged(nameof(FilasPrecio));
            OnPropertyChanged(nameof(TieneTablaPrecios));
        }

        private List<string> ObtenerElementosTablaFormula()
        {
            var elementosOrdenadosPorNecesidad = ElementosBalance
                .Where(x => !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico))
                .Where(x => x.RequerimientoLibras > 0)
                .OrderByDescending(x => x.RequerimientoLibras)
                .Select(x => NormalizarSimbolo(x.SimboloElementoQuimico))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var elementosSecundarios = FilasResultado
                .Where(fila => fila.FuenteNutriente?.ElementosQuimicos != null)
                .SelectMany(fila => fila.FuenteNutriente!.ElementosQuimicos!
                    .Where(aporte => (aporte.CantidadAporte ?? 0) > 0)
                    .Select(aporte => new
                    {
                        Simbolo = NormalizarSimbolo(aporte.SimboloElementoQuimico),
                        AporteTotal = fila.LibrasAnuales * ((aporte.CantidadAporte ?? 0) / 100m)
                    }))
                .Where(x => !string.IsNullOrWhiteSpace(x.Simbolo))
                .GroupBy(x => x.Simbolo)
                .Select(g => new
                {
                    Simbolo = g.Key,
                    AporteTotal = g.Sum(x => x.AporteTotal)
                })
                .Where(x => !elementosOrdenadosPorNecesidad.Contains(x.Simbolo))
                .OrderByDescending(x => x.AporteTotal)
                .Select(x => x.Simbolo)
                .ToList();

            var elementosFinales = new List<string>();

            elementosFinales.AddRange(elementosOrdenadosPorNecesidad);
            elementosFinales.AddRange(elementosSecundarios);

            return elementosFinales
                .Select(FormatearSimboloTabla)
                .ToList();
        }

        private static decimal ObtenerPorcentajeAporteFuente(FuenteNutrienteResponse? fuente, string? simboloElemento)
        {
            if (fuente?.ElementosQuimicos == null || fuente.ElementosQuimicos.Count == 0)
                return 0;

            string simboloNormalizado = NormalizarSimbolo(simboloElemento);

            var aporte = fuente.ElementosQuimicos.FirstOrDefault(x =>
                NormalizarSimbolo(x.SimboloElementoQuimico) == simboloNormalizado
            );

            return aporte?.CantidadAporte ?? 0;
        }

        private static string FormatearSimboloTabla(string simbolo)
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
            OnPropertyChanged(nameof(TieneTablaBalance));
            OnPropertyChanged(nameof(TieneTablaAplicaciones));
            OnPropertyChanged(nameof(TieneTablaPrecios));
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
        public FuenteNutrienteResponse? FuenteNutriente { get; set; }

        public string Fuente { get; set; } = string.Empty;

        public string Elemento { get; set; } = string.Empty;

        public decimal RequerimientoLibras { get; set; }

        public decimal RequerimientoPendienteLibras { get; set; }

        public decimal LibrasAnuales { get; set; }

        public decimal OnzasAnuales { get; set; }

        public decimal DosAplicaciones { get; set; }

        public decimal TresAplicaciones { get; set; }

        public decimal QuintalesAnuales { get; set; }

        public decimal PrecioPorQuintal { get; set; }

        public decimal SubtotalFuente { get; set; }
    }

    public class BalanceFormulaAporteViewModel
    {
        public string Simbolo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public decimal Libras { get; set; }
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

        public bool EsTotal { get; set; }

        public string TextoFuente => Fuente;

        public string TextoLibrasAnuales =>
            LibrasAnuales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoQuintalesAnuales =>
            QuintalesAnuales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoPrecioPorQuintal =>
            EsTotal
                ? "-"
                : $"C$ {PrecioPorQuintal.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoSubtotalFuente =>
            $"C$ {SubtotalFuente.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}