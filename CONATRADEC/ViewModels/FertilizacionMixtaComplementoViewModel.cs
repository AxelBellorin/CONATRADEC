using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CONATRADEC.ViewModels
{
    public partial class FertilizacionMixtaTabViewModel
    {
        private readonly BalanceNutricionalApiService balanceComplementoApiService = new();

        private BalanceFertilizacionMixtaContext? contextoBalance;
        private BalanceNutricionalResponse? resultadoBalanceAjustado;

        private bool esComplementoBalance;
        private bool complementoCalculado;
        private bool recalcularComplementoPendiente;

        private decimal costoComercialOriginal;
        private decimal costoFertilizacionMixta;
        private decimal costoComercialAjustado;
        private decimal costoTotalFinal;
        private decimal diferenciaEconomica;

        private string elementosNoCubiertos = string.Empty;
        private string mensajeSugerencia = string.Empty;
        private Command? aplicarSugerenciaCommand;
        private Command? deshacerSugerenciaCommand;
        private List<EstadoFuenteAntesSugerencia>? estadoAntesSugerencia;

        public ObservableCollection<CostoFuenteOrganicaViewModel> FilasCostoOrganico { get; } = new();

        public ObservableCollection<CompraComercialAjustadaViewModel> FilasCompraAjustada { get; } = new();

        public ObservableCollection<EncabezadoAporteMatrizViewModel> EncabezadosAporteMatriz { get; } = new();

        public ObservableCollection<FuenteAporteMatrizViewModel> FilasAporteMatriz { get; } = new();

        public ObservableCollection<SugerenciaIncrementoFuenteViewModel> FilasSugerenciaIncremento { get; } = new();

        public BalanceNutricionalResponse? ResultadoBalanceAjustado
        {
            get => resultadoBalanceAjustado;
            private set
            {
                resultadoBalanceAjustado = value;
                OnPropertyChanged(nameof(ResultadoBalanceAjustado));
            }
        }

        public bool EsComplementoBalance
        {
            get => esComplementoBalance;
            private set
            {
                if (esComplementoBalance == value)
                    return;

                esComplementoBalance = value;
                OnPropertyChanged(nameof(EsComplementoBalance));
                OnPropertyChanged(nameof(EsFertilizacionIndependiente));
                OnPropertyChanged(nameof(TituloModoCalculo));
                OnPropertyChanged(nameof(DescripcionModoCalculo));
                OnPropertyChanged(nameof(TieneComplementoCompleto));
            }
        }

        public bool EsFertilizacionIndependiente => !EsComplementoBalance;

        public bool TieneContextoBalance => contextoBalance != null;

        public bool ComplementoCalculado
        {
            get => complementoCalculado;
            private set
            {
                complementoCalculado = value;
                OnPropertyChanged(nameof(ComplementoCalculado));
                OnPropertyChanged(nameof(TieneResultadoBalanceAjustado));
                OnPropertyChanged(nameof(TieneComplementoCompleto));
            }
        }

        public bool TieneResultadoBalanceAjustado =>
            EsComplementoBalance &&
            ComplementoCalculado &&
            FilasCompraAjustada.Count > 0;

        public bool TieneComplementoCompleto =>
            !EsComplementoBalance ||
            (TieneContextoBalance &&
             TieneResultadoFertilizacionMixta &&
             ComplementoCalculado);

        public bool TieneTablaCostosOrganicos => FilasCostoOrganico.Count > 0;

        public bool TieneMatrizAportes =>
            EncabezadosAporteMatriz.Count > 0 &&
            FilasAporteMatriz.Count > 0;

        public bool TieneSugerenciaIncremento =>
            FilasSugerenciaIncremento.Any(x => x.IncrementarQq > 0);

        public bool TieneElementosNoCubiertos => !string.IsNullOrWhiteSpace(ElementosNoCubiertos);

        public bool MostrarAvisoElementosNoCubiertosSinSugerencia =>
            TieneElementosNoCubiertos &&
            !TieneSugerenciaIncremento &&
            estadoAntesSugerencia == null;

        public bool PuedeDeshacerSugerencia =>
            !IsBusy &&
            estadoAntesSugerencia != null &&
            estadoAntesSugerencia.Count > 0;

        public bool PuedeAplicarSugerencia =>
            !IsBusy &&
            TieneSugerenciaIncremento;

        public string ElementosNoCubiertos
        {
            get => elementosNoCubiertos;
            private set
            {
                elementosNoCubiertos = value ?? string.Empty;
                OnPropertyChanged(nameof(ElementosNoCubiertos));
                OnPropertyChanged(nameof(TieneElementosNoCubiertos));
                OnPropertyChanged(nameof(MostrarAvisoElementosNoCubiertosSinSugerencia));
            }
        }

        public string MensajeSugerencia
        {
            get => mensajeSugerencia;
            private set
            {
                mensajeSugerencia = value ?? string.Empty;
                OnPropertyChanged(nameof(MensajeSugerencia));
            }
        }

        public Command AplicarSugerenciaCommand =>
            aplicarSugerenciaCommand ??= new Command(
                async () => await AplicarSugerenciaAsync(),
                () => PuedeAplicarSugerencia
            );

        public Command DeshacerSugerenciaCommand =>
            deshacerSugerenciaCommand ??= new Command(
                async () => await DeshacerSugerenciaAsync(),
                () => PuedeDeshacerSugerencia
            );

        public bool NoRequiereCompraComercial =>
            EsComplementoBalance &&
            ComplementoCalculado &&
            FilasCompraAjustada.Count > 0 &&
            FilasCompraAjustada.All(x => x.QuintalesComprar <= 0);

        public string TituloModoCalculo =>
            EsComplementoBalance
                ? "Complemento del balance de fórmula"
                : "Fertilización mixta independiente";

        public string DescripcionModoCalculo =>
            EsComplementoBalance
                ? "El aporte orgánico se descontará del balance original y se calculará la compra comercial restante."
                : "El aporte orgánico se comparará directamente con el requerimiento anual del análisis de suelo.";

        public decimal CostoComercialOriginal
        {
            get => costoComercialOriginal;
            private set
            {
                costoComercialOriginal = Redondear2(value);
                OnPropertyChanged(nameof(CostoComercialOriginal));
                OnPropertyChanged(nameof(TextoCostoComercialOriginal));
            }
        }

        public decimal CostoFertilizacionMixta
        {
            get => costoFertilizacionMixta;
            private set
            {
                costoFertilizacionMixta = Redondear2(value);
                OnPropertyChanged(nameof(CostoFertilizacionMixta));
                OnPropertyChanged(nameof(TextoCostoFertilizacionMixta));
            }
        }

        public decimal CostoComercialAjustado
        {
            get => costoComercialAjustado;
            private set
            {
                costoComercialAjustado = Redondear2(value);
                OnPropertyChanged(nameof(CostoComercialAjustado));
                OnPropertyChanged(nameof(TextoCostoComercialAjustado));
            }
        }

        public decimal CostoTotalFinal
        {
            get => costoTotalFinal;
            private set
            {
                costoTotalFinal = Redondear2(value);
                OnPropertyChanged(nameof(CostoTotalFinal));
                OnPropertyChanged(nameof(TextoCostoTotalFinal));
            }
        }

        public decimal DiferenciaEconomica
        {
            get => diferenciaEconomica;
            private set
            {
                diferenciaEconomica = Redondear2(value);
                OnPropertyChanged(nameof(DiferenciaEconomica));
                OnPropertyChanged(nameof(TextoDiferenciaEconomica));
                OnPropertyChanged(nameof(EtiquetaDiferenciaEconomica));
                OnPropertyChanged(nameof(ColorDiferenciaEconomica));
            }
        }

        public string TextoCostoComercialOriginal => FormatearMoneda(CostoComercialOriginal);

        public string TextoCostoFertilizacionMixta => FormatearMoneda(CostoFertilizacionMixta);

        public string TextoCostoComercialAjustado => FormatearMoneda(CostoComercialAjustado);

        public string TextoCostoTotalFinal => FormatearMoneda(CostoTotalFinal);

        public string TextoDiferenciaEconomica => FormatearMoneda(Math.Abs(DiferenciaEconomica));

        public string EtiquetaDiferenciaEconomica =>
            DiferenciaEconomica >= 0
                ? "Ahorro frente al balance original"
                : "Incremento frente al balance original";

        public string ColorDiferenciaEconomica =>
            DiferenciaEconomica >= 0 ? "#3B655B" : "#DC2626";

        public async Task ConfigurarComplementoBalanceAsync(
            bool activado,
            BalanceFertilizacionMixtaContext? nuevoContexto)
        {
            if (!activado)
            {
                EsComplementoBalance = false;
                contextoBalance = null;
                recalcularComplementoPendiente = false;

                RestaurarRequerimientosDesdeAnalisis();
                LimpiarSoloBalanceAjustado();

                Mensaje = TieneResultadoFertilizacionMixta
                    ? "El complemento se desactivó. La fertilización mixta conserva su resultado independiente."
                    : "Seleccione las fuentes orgánicas y calcule la fertilización mixta.";

                NotificarEstadoComplemento();
                return;
            }

            EsComplementoBalance = true;

            if (nuevoContexto == null)
            {
                if (ResultadoFertilizacionMixta != null)
                    recalcularComplementoPendiente = true;

                contextoBalance = null;
                ResultadoFertilizacionMixta = null;
                LimpiarResultadosPresentacion();

                Mensaje = "Complete o recalcule el balance de fórmula para continuar con el complemento.";
                NotificarEstadoComplemento();
                return;
            }

            bool cambioContexto = !ContextosEquivalentes(contextoBalance, nuevoContexto);

            if (cambioContexto && ResultadoFertilizacionMixta != null)
            {
                recalcularComplementoPendiente = true;
                ResultadoFertilizacionMixta = null;
                LimpiarResultadosPresentacion();
            }

            contextoBalance = nuevoContexto;
            RestaurarRequerimientosDesdeAnalisis();
            AplicarRequerimientosDelBalance(nuevoContexto);

            OnPropertyChanged(nameof(TieneContextoBalance));
            OnPropertyChanged(nameof(TieneComplementoCompleto));

            if (recalcularComplementoPendiente && TieneFuentesSeleccionadasValidas())
            {
                if (IsBusy)
                    return;

                recalcularComplementoPendiente = false;
                await CalcularAsync();
                return;
            }

            Mensaje = "Balance vinculado. Seleccione las fuentes orgánicas y presione Calcular para obtener la compra ajustada.";
            NotificarEstadoComplemento();
        }

        private void PrepararNuevaInicializacion()
        {
            contextoBalance = null;
            recalcularComplementoPendiente = false;
            EsComplementoBalance = false;
            LimpiarResultadosPresentacion();
            NotificarEstadoComplemento();
        }

        private void ConstruirTablaCostosOrganicos()
        {
            FilasCostoOrganico.Clear();

            foreach (FuenteFertilizacionMixtaItemViewModel fuente in
                     FuentesDisponibles.Where(x => x.EstaSeleccionada))
            {
                decimal cantidad = ConvertirDecimal(fuente.CantidadQq);
                decimal precio = fuente.PrecioFuente ?? 0;

                FilasCostoOrganico.Add(new CostoFuenteOrganicaViewModel
                {
                    Fuente = fuente.NombreFuente,
                    CantidadQq = cantidad,
                    PrecioPorQq = precio,
                    Costo = cantidad * precio
                });
            }

            CostoFertilizacionMixta = FilasCostoOrganico.Sum(x => x.Costo);

            OnPropertyChanged(nameof(FilasCostoOrganico));
            OnPropertyChanged(nameof(TieneTablaCostosOrganicos));
        }

        private void ConstruirMatrizAportesPorFuente()
        {
            EncabezadosAporteMatriz.Clear();
            FilasAporteMatriz.Clear();

            if (ResultadoFertilizacionMixta?.Detalles == null ||
                ResultadoFertilizacionMixta.Detalles.Count == 0)
            {
                NotificarMatrizAportes();
                return;
            }

            foreach (DetalleFertilizacionMixtaResultadoResponse detalle in
                     ResultadoFertilizacionMixta.Detalles)
            {
                string simbolo = FormatearSimbolo(detalle.Elemento);

                if (string.IsNullOrWhiteSpace(simbolo))
                    continue;

                if (EncabezadosAporteMatriz.Any(x =>
                    string.Equals(x.Simbolo, simbolo, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                EncabezadosAporteMatriz.Add(new EncabezadoAporteMatrizViewModel
                {
                    ElementoQuimicosId = detalle.ElementoQuimicosId,
                    Simbolo = simbolo
                });
            }

            foreach (FuenteFertilizacionMixtaResultadoResponse fuente in
                     ResultadoFertilizacionMixta.Fuentes)
            {
                FuenteAporteMatrizViewModel fila = new()
                {
                    FuenteNutrientesId = fuente.FuenteNutrientesId,
                    Fuente = fuente.NombreFuente ?? "Fuente",
                    CantidadQq = fuente.CantidadQq ?? 0
                };

                foreach (EncabezadoAporteMatrizViewModel encabezado in EncabezadosAporteMatriz)
                {
                    DetalleFertilizacionMixtaResultadoResponse? detalle =
                        ResultadoFertilizacionMixta.Detalles.FirstOrDefault(x =>
                            x.ElementoQuimicosId == encabezado.ElementoQuimicosId
                        );

                    FuenteDetalleFertilizacionMixtaResponse? aporteFuente =
                        detalle?.Fuentes.FirstOrDefault(x =>
                            x.FuenteNutrientesId == fuente.FuenteNutrientesId
                        );

                    fila.Aportes.Add(new CeldaAporteMatrizViewModel
                    {
                        Simbolo = encabezado.Simbolo,
                        AporteLibras = aporteFuente?.AporteTotal ?? 0
                    });
                }

                FilasAporteMatriz.Add(fila);
            }

            NotificarMatrizAportes();
        }

        private void ConstruirSugerenciaIncremento()
        {
            FilasSugerenciaIncremento.Clear();
            ElementosNoCubiertos = string.Empty;

            if (ResultadoFertilizacionMixta?.Detalles == null ||
                ResultadoFertilizacionMixta.Detalles.Count == 0)
            {
                MensajeSugerencia = string.Empty;
                NotificarSugerencia();
                return;
            }

            List<DetalleFertilizacionMixtaResultadoResponse> deficitarios =
                ResultadoFertilizacionMixta.Detalles
                    .Where(x => (x.Deficit ?? 0) > 0)
                    .ToList();

            if (deficitarios.Count == 0)
            {
                MensajeSugerencia = "Las cantidades actuales cubren todos los requerimientos evaluados.";
                NotificarSugerencia();
                return;
            }

            List<FuenteOptimizacionLocal> fuentes =
                ConstruirFuentesOptimizacion(deficitarios);

            List<string> noCubiertos = deficitarios
                .Where(detalle => !fuentes.Any(fuente =>
                    fuente.Aportes.TryGetValue(
                        NormalizarSimbolo(detalle.Elemento),
                        out decimal aporte
                    ) && aporte > 0
                ))
                .Select(x => FormatearSimbolo(x.Elemento))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ElementosNoCubiertos = string.Join(", ", noCubiertos);

            Dictionary<string, decimal> deficits = deficitarios
                .Where(x => !noCubiertos.Contains(
                    FormatearSimbolo(x.Elemento),
                    StringComparer.OrdinalIgnoreCase
                ))
                .GroupBy(
                    x => NormalizarSimbolo(x.Elemento),
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Sum(x => x.Deficit ?? 0),
                    StringComparer.OrdinalIgnoreCase
                );

            if (deficits.Count > 0 && fuentes.Count > 0)
            {
                CalcularIncrementosAproximados(fuentes, deficits);

                foreach (FuenteOptimizacionLocal fuente in fuentes)
                {
                    decimal incrementoRedondeado = fuente.IncrementoQq > 0.0001m
                        ? Math.Ceiling(fuente.IncrementoQq * 100m) / 100m
                        : 0;

                    FilasSugerenciaIncremento.Add(
                        new SugerenciaIncrementoFuenteViewModel
                        {
                            FuenteNutrientesId = fuente.FuenteNutrientesId,
                            Fuente = fuente.Fuente,
                            CantidadActualQq = fuente.CantidadActualQq,
                            IncrementarQq = incrementoRedondeado,
                            TotalSugeridoQq = fuente.CantidadActualQq + incrementoRedondeado,
                            PrecioPorQq = fuente.PrecioPorQq,
                            CostoAdicional = incrementoRedondeado * fuente.PrecioPorQq
                        }
                    );
                }
            }

            MensajeSugerencia = TieneSugerenciaIncremento
                ? "Sugerencia aproximada priorizando el menor sobrante. Puede generar excesos porque cada fuente tiene una composición fija."
                : "Las fuentes seleccionadas no permiten calcular un incremento útil para los déficits actuales.";

            NotificarSugerencia();
        }

        private List<FuenteOptimizacionLocal> ConstruirFuentesOptimizacion(
            List<DetalleFertilizacionMixtaResultadoResponse> deficitarios)
        {
            List<FuenteOptimizacionLocal> fuentes = new();

            foreach (FuenteFertilizacionMixtaResultadoResponse fuenteResultado in
                     ResultadoFertilizacionMixta!.Fuentes)
            {
                FuenteFertilizacionMixtaItemViewModel? fuenteFormulario =
                    FuentesDisponibles.FirstOrDefault(x =>
                        x.FuenteNutrientesId == fuenteResultado.FuenteNutrientesId
                    );

                FuenteOptimizacionLocal fuente = new()
                {
                    FuenteNutrientesId = fuenteResultado.FuenteNutrientesId,
                    Fuente = fuenteResultado.NombreFuente ?? "Fuente",
                    CantidadActualQq = fuenteResultado.CantidadQq ?? 0,
                    PrecioPorQq = fuenteFormulario?.PrecioFuente ?? 0
                };

                foreach (DetalleFertilizacionMixtaResultadoResponse detalle in deficitarios)
                {
                    FuenteDetalleFertilizacionMixtaResponse? aporte =
                        detalle.Fuentes.FirstOrDefault(x =>
                            x.FuenteNutrientesId == fuenteResultado.FuenteNutrientesId
                        );

                    fuente.Aportes[NormalizarSimbolo(detalle.Elemento)] =
                        aporte?.AportePorUnidad ?? 0;
                }

                fuentes.Add(fuente);
            }

            return fuentes;
        }

        private static void CalcularIncrementosAproximados(
            List<FuenteOptimizacionLocal> fuentes,
            Dictionary<string, decimal> deficits)
        {
            Dictionary<string, decimal> restantes = deficits.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase
            );

            int limiteIteraciones = Math.Max(deficits.Count * fuentes.Count * 3, 12);

            for (int iteracion = 0; iteracion < limiteIteraciones; iteracion++)
            {
                if (restantes.Values.All(x => x <= 0.0001m))
                    break;

                FuenteOptimizacionLocal? mejorFuente = null;
                decimal mejorIncremento = 0;
                decimal mejorPuntaje = decimal.MaxValue;

                foreach (FuenteOptimizacionLocal fuente in fuentes)
                {
                    foreach (KeyValuePair<string, decimal> restante in restantes
                        .Where(x => x.Value > 0.0001m))
                    {
                        if (!fuente.Aportes.TryGetValue(restante.Key, out decimal aporte) ||
                            aporte <= 0)
                        {
                            continue;
                        }

                        decimal incremento = restante.Value / aporte;
                        decimal deficitNormalizado = 0;
                        decimal sobranteNormalizado = 0;

                        foreach (KeyValuePair<string, decimal> objetivo in deficits)
                        {
                            decimal restanteActual = restantes[objetivo.Key];
                            decimal aporteAdicional = fuente.Aportes.TryGetValue(
                                objetivo.Key,
                                out decimal valorAporte
                            ) ? valorAporte * incremento : 0;

                            decimal nuevoRestante = restanteActual - aporteAdicional;
                            decimal baseNormalizacion = Math.Max(objetivo.Value, 0.01m);

                            if (nuevoRestante > 0)
                                deficitNormalizado += nuevoRestante / baseNormalizacion;
                            else
                                sobranteNormalizado += Math.Abs(nuevoRestante) / baseNormalizacion;
                        }

                        decimal puntaje =
                            (deficitNormalizado * 1000m) +
                            sobranteNormalizado +
                            (incremento * 0.0001m);

                        if (puntaje < mejorPuntaje)
                        {
                            mejorPuntaje = puntaje;
                            mejorFuente = fuente;
                            mejorIncremento = incremento;
                        }
                    }
                }

                if (mejorFuente == null || mejorIncremento <= 0)
                    break;

                mejorFuente.IncrementoQq += mejorIncremento;

                foreach (string simbolo in restantes.Keys.ToList())
                {
                    decimal aporte = mejorFuente.Aportes.TryGetValue(
                        simbolo,
                        out decimal valor
                    ) ? valor : 0;

                    restantes[simbolo] = Math.Max(
                        restantes[simbolo] - (aporte * mejorIncremento),
                        0
                    );
                }
            }

            RefinarIncrementos(fuentes, deficits);
        }

        private static void RefinarIncrementos(
            List<FuenteOptimizacionLocal> fuentes,
            Dictionary<string, decimal> deficits)
        {
            for (int vuelta = 0; vuelta < 8; vuelta++)
            {
                foreach (FuenteOptimizacionLocal fuente in fuentes)
                {
                    List<decimal> candidatos = new() { 0 };

                    foreach (KeyValuePair<string, decimal> objetivo in deficits)
                    {
                        if (!fuente.Aportes.TryGetValue(objetivo.Key, out decimal aporteFuente) ||
                            aporteFuente <= 0)
                        {
                            continue;
                        }

                        decimal aporteOtras = fuentes
                            .Where(x => !ReferenceEquals(x, fuente))
                            .Sum(x =>
                                x.Aportes.TryGetValue(objetivo.Key, out decimal aporte)
                                    ? aporte * x.IncrementoQq
                                    : 0
                            );

                        candidatos.Add(Math.Max(
                            (objetivo.Value - aporteOtras) / aporteFuente,
                            0
                        ));
                    }

                    decimal mejorCantidad = fuente.IncrementoQq;
                    decimal mejorPuntaje = EvaluarSolucion(fuentes, deficits);

                    foreach (decimal candidato in candidatos)
                    {
                        decimal cantidadAnterior = fuente.IncrementoQq;
                        fuente.IncrementoQq = candidato;

                        decimal puntaje = EvaluarSolucion(fuentes, deficits);

                        if (puntaje < mejorPuntaje)
                        {
                            mejorPuntaje = puntaje;
                            mejorCantidad = candidato;
                        }

                        fuente.IncrementoQq = cantidadAnterior;
                    }

                    fuente.IncrementoQq = mejorCantidad;
                }
            }
        }

        private static decimal EvaluarSolucion(
            List<FuenteOptimizacionLocal> fuentes,
            Dictionary<string, decimal> deficits)
        {
            decimal deficitNormalizado = 0;
            decimal sobranteNormalizado = 0;
            decimal costo = 0;

            foreach (KeyValuePair<string, decimal> objetivo in deficits)
            {
                decimal aporte = fuentes.Sum(fuente =>
                    fuente.Aportes.TryGetValue(objetivo.Key, out decimal valor)
                        ? valor * fuente.IncrementoQq
                        : 0
                );

                decimal diferencia = aporte - objetivo.Value;
                decimal baseNormalizacion = Math.Max(objetivo.Value, 0.01m);

                if (diferencia < 0)
                    deficitNormalizado += Math.Abs(diferencia) / baseNormalizacion;
                else
                    sobranteNormalizado += diferencia / baseNormalizacion;
            }

            costo = fuentes.Sum(x => x.IncrementoQq * x.PrecioPorQq);

            return (deficitNormalizado * 1000000m) +
                   sobranteNormalizado +
                   (costo * 0.0000001m);
        }

        private async Task AplicarSugerenciaAsync()
        {
            if (!PuedeAplicarSugerencia)
                return;

            estadoAntesSugerencia = FuentesDisponibles
                .Select(x => new EstadoFuenteAntesSugerencia
                {
                    FuenteNutrientesId = x.FuenteNutrientesId,
                    EstaSeleccionada = x.EstaSeleccionada,
                    CantidadQq = x.CantidadQq
                })
                .ToList();

            NotificarHistorialSugerencia();

            List<SugerenciaIncrementoFuenteViewModel> sugerencias =
                FilasSugerenciaIncremento
                    .Where(x => x.IncrementarQq > 0)
                    .ToList();

            try
            {
                suspendiendoCambiosTemporales = true;

                foreach (SugerenciaIncrementoFuenteViewModel sugerencia in sugerencias)
                {
                    FuenteFertilizacionMixtaItemViewModel? fuente =
                        FuentesDisponibles.FirstOrDefault(x =>
                            x.FuenteNutrientesId == sugerencia.FuenteNutrientesId
                        );

                    if (fuente == null)
                        continue;

                    fuente.EstaSeleccionada = true;
                    fuente.CantidadQq = sugerencia.TotalSugeridoQq
                        .ToString("0.00", CultureInfo.InvariantCulture);
                }
            }
            finally
            {
                suspendiendoCambiosTemporales = false;
            }

            await CalcularAsync();
        }

        private async Task DeshacerSugerenciaAsync()
        {
            if (!PuedeDeshacerSugerencia || estadoAntesSugerencia == null)
                return;

            List<EstadoFuenteAntesSugerencia> estadoAnterior =
                estadoAntesSugerencia.ToList();

            estadoAntesSugerencia = null;
            NotificarHistorialSugerencia();

            try
            {
                suspendiendoCambiosTemporales = true;

                foreach (FuenteFertilizacionMixtaItemViewModel fuente in FuentesDisponibles)
                {
                    EstadoFuenteAntesSugerencia? anterior =
                        estadoAnterior.FirstOrDefault(x =>
                            x.FuenteNutrientesId == fuente.FuenteNutrientesId
                        );

                    if (anterior == null)
                        continue;

                    fuente.EstaSeleccionada = anterior.EstaSeleccionada;
                    fuente.CantidadQq = anterior.CantidadQq;
                }
            }
            finally
            {
                suspendiendoCambiosTemporales = false;
            }

            await CalcularAsync();
        }

        private void DescartarHistorialSugerencia()
        {
            if (estadoAntesSugerencia == null)
                return;

            estadoAntesSugerencia = null;
            NotificarHistorialSugerencia();
        }

        private void NotificarMatrizAportes()
        {
            OnPropertyChanged(nameof(EncabezadosAporteMatriz));
            OnPropertyChanged(nameof(FilasAporteMatriz));
            OnPropertyChanged(nameof(TieneMatrizAportes));
        }

        private void NotificarSugerencia()
        {
            OnPropertyChanged(nameof(FilasSugerenciaIncremento));
            OnPropertyChanged(nameof(TieneSugerenciaIncremento));
            OnPropertyChanged(nameof(PuedeAplicarSugerencia));
            OnPropertyChanged(nameof(MostrarAvisoElementosNoCubiertosSinSugerencia));

            aplicarSugerenciaCommand?.ChangeCanExecute();
        }

        private void NotificarHistorialSugerencia()
        {
            OnPropertyChanged(nameof(PuedeDeshacerSugerencia));
            OnPropertyChanged(nameof(MostrarAvisoElementosNoCubiertosSinSugerencia));
            deshacerSugerenciaCommand?.ChangeCanExecute();
        }

        private async Task<bool> CalcularBalanceAjustadoAsync(
            FertilizacionMixtaCalculoResponse resultadoMixta)
        {
            LimpiarSoloBalanceAjustado();

            if (!EsComplementoBalance)
                return true;

            if (contextoBalance == null)
                return false;

            BalanceNutricionalRequest request = new()
            {
                NombreFormula = $"{contextoBalance.NombreFormula} - Ajustado",
                TerrenoId = contextoBalance.TerrenoId,
                TotalPlantas = contextoBalance.TotalPlantas,
                TotalAplicaciones = contextoBalance.TotalAplicaciones
            };

            foreach (BalanceFertilizacionMixtaItem item in contextoBalance.Items)
            {
                DetalleFertilizacionMixtaResultadoResponse? detalleMixta =
                    resultadoMixta.Detalles.FirstOrDefault(x =>
                        x.ElementoQuimicosId == item.ElementoQuimicosId
                    );

                decimal aporteOrganico = detalleMixta?.AporteOrganico ?? 0;
                decimal restante = Math.Max(item.RequerimientoOriginal - aporteOrganico, 0);

                if (restante <= 0)
                    continue;

                request.Items.Add(new BalanceNutricionalItemRequest
                {
                    FuenteNutrientesId = item.FuenteNutrientesId,
                    ElementoQuimicosId = item.ElementoQuimicosId,
                    RequerimientoLibras = Redondear2(restante)
                });
            }

            if (request.Items.Count == 0)
            {
                ResultadoBalanceAjustado = CrearResultadoAjustadoVacio(contextoBalance);
                ConstruirCompraAjustada();
                ComplementoCalculado = true;
                NotificarEstadoComplemento();
                return true;
            }

            BalanceNutricionalResponse? resultado =
                await balanceComplementoApiService.CalcularAsync(request);

            if (resultado == null || !resultado.Success)
            {
                ComplementoCalculado = false;
                ErrorElementos = resultado?.Message ??
                    "No fue posible calcular el balance comercial ajustado.";

                await MostrarMensajeAsync(
                    "Balance ajustado",
                    ErrorElementos
                );

                return false;
            }

            ResultadoBalanceAjustado = resultado;
            ConstruirCompraAjustada();
            ComplementoCalculado = true;
            NotificarEstadoComplemento();

            return true;
        }

        private void ConstruirCompraAjustada()
        {
            FilasCompraAjustada.Clear();

            if (contextoBalance == null || ResultadoBalanceAjustado == null)
                return;

            foreach (BalanceFertilizacionMixtaItem item in contextoBalance.Items)
            {
                BalanceNutricionalDetalleResponse? original =
                    BuscarDetalleBalance(
                        contextoBalance.ResultadoOriginal,
                        item.NombreFuente,
                        item.SimboloElementoQuimico
                    );

                BalanceNutricionalDetalleResponse? ajustado =
                    BuscarDetalleBalance(
                        ResultadoBalanceAjustado,
                        item.NombreFuente,
                        item.SimboloElementoQuimico
                    );

                decimal qqOriginal = original?.QuintalesAnuales ?? 0;
                decimal qqAjustado = ajustado?.QuintalesAnuales ?? 0;
                decimal precio = ajustado?.PrecioPorQuintal ??
                                  original?.PrecioPorQuintal ??
                                  0;

                decimal qqComprar = Math.Ceiling(qqAjustado);

                FilasCompraAjustada.Add(new CompraComercialAjustadaViewModel
                {
                    Fuente = item.NombreFuente,
                    Elemento = FormatearSimbolo(item.SimboloElementoQuimico),
                    QuintalesOriginales = qqOriginal,
                    QuintalesAjustados = qqAjustado,
                    ReduccionQuintales = Math.Max(qqOriginal - qqAjustado, 0),
                    PrecioPorQq = precio,
                    QuintalesComprar = qqComprar,
                    CostoCompra = qqComprar * precio
                });
            }

            CostoComercialOriginal = contextoBalance.CostoCompraOriginal;
            CostoComercialAjustado = FilasCompraAjustada.Sum(x => x.CostoCompra);
            CostoTotalFinal = CostoComercialAjustado + CostoFertilizacionMixta;
            DiferenciaEconomica = CostoComercialOriginal - CostoTotalFinal;

            OnPropertyChanged(nameof(FilasCompraAjustada));
            OnPropertyChanged(nameof(TieneResultadoBalanceAjustado));
            OnPropertyChanged(nameof(NoRequiereCompraComercial));
        }

        private void RestaurarRequerimientosDesdeAnalisis()
        {
            if (ResultadoCalculo?.Elementos == null)
                return;

            foreach (ElementoFertilizacionMixtaItemViewModel item in ElementosExportables)
            {
                ElementoResultadoCalculoResponse? original =
                    ResultadoCalculo.Elementos.FirstOrDefault(x =>
                        x.ElementoQuimicosId == item.ElementoQuimicosId
                    );

                if (original != null)
                    item.Exportable = original.RequerimientoCalculado ?? 0;
            }
        }

        private void AplicarRequerimientosDelBalance(
            BalanceFertilizacionMixtaContext contexto)
        {
            foreach (BalanceFertilizacionMixtaItem balanceItem in contexto.Items)
            {
                ElementoFertilizacionMixtaItemViewModel? elemento =
                    ElementosExportables.FirstOrDefault(x =>
                        x.ElementoQuimicosId == balanceItem.ElementoQuimicosId
                    );

                if (elemento != null)
                    elemento.Exportable = balanceItem.RequerimientoOriginal;
            }
        }

        private bool TieneFuentesSeleccionadasValidas()
        {
            return FuentesDisponibles
                .Where(x => x.EstaSeleccionada)
                .Any(x =>
                    TryParseDecimal(x.CantidadQq, out decimal cantidad) &&
                    cantidad > 0
                );
        }

        private void LimpiarResultadosPresentacion()
        {
            DescartarHistorialSugerencia();

            EncabezadosAporteMatriz.Clear();
            FilasAporteMatriz.Clear();
            FilasSugerenciaIncremento.Clear();
            ElementosNoCubiertos = string.Empty;
            MensajeSugerencia = string.Empty;

            FilasCostoOrganico.Clear();
            CostoFertilizacionMixta = 0;
            LimpiarSoloBalanceAjustado();

            NotificarMatrizAportes();
            NotificarSugerencia();
            OnPropertyChanged(nameof(FilasCostoOrganico));
            OnPropertyChanged(nameof(TieneTablaCostosOrganicos));
        }

        private void LimpiarSoloBalanceAjustado()
        {
            ResultadoBalanceAjustado = null;
            FilasCompraAjustada.Clear();
            ComplementoCalculado = false;

            CostoComercialOriginal = 0;
            CostoComercialAjustado = 0;
            CostoTotalFinal = 0;
            DiferenciaEconomica = 0;

            OnPropertyChanged(nameof(FilasCompraAjustada));
            OnPropertyChanged(nameof(TieneResultadoBalanceAjustado));
            OnPropertyChanged(nameof(NoRequiereCompraComercial));
        }

        private void NotificarEstadoComplemento()
        {
            OnPropertyChanged(nameof(TieneContextoBalance));
            OnPropertyChanged(nameof(TieneComplementoCompleto));
            OnPropertyChanged(nameof(TituloModoCalculo));
            OnPropertyChanged(nameof(DescripcionModoCalculo));
        }

        private static BalanceNutricionalResponse CrearResultadoAjustadoVacio(
            BalanceFertilizacionMixtaContext contexto)
        {
            return new BalanceNutricionalResponse
            {
                NombreFormula = $"{contexto.NombreFormula} - Ajustado",
                TotalLibras = 0,
                MezclaTotalQq = 0,
                TotalOnzas = 0,
                TotalPlantas = contexto.TotalPlantas,
                TotalAplicaciones = contexto.TotalAplicaciones,
                PrecioTotalFormula = 0,
                PrecioPorAplicacion = 0,
                DosisPlantaAnualOz = 0,
                DosisPlantaPorAplicacionOz = 0
            };
        }

        private static BalanceNutricionalDetalleResponse? BuscarDetalleBalance(
            BalanceNutricionalResponse? resultado,
            string fuente,
            string simbolo)
        {
            if (resultado?.Detalle == null)
                return null;

            string fuenteNormalizada = (fuente ?? string.Empty).Trim();
            string simboloNormalizado = NormalizarSimbolo(simbolo);

            return resultado.Detalle.FirstOrDefault(x =>
                string.Equals(
                    (x.Fuente ?? string.Empty).Trim(),
                    fuenteNormalizada,
                    StringComparison.OrdinalIgnoreCase
                ) &&
                NormalizarSimbolo(x.Elemento) == simboloNormalizado
            );
        }

        private static bool ContextosEquivalentes(
            BalanceFertilizacionMixtaContext? actual,
            BalanceFertilizacionMixtaContext nuevo)
        {
            if (actual == null)
                return false;

            if (actual.TerrenoId != nuevo.TerrenoId ||
                actual.TotalPlantas != nuevo.TotalPlantas ||
                actual.TotalAplicaciones != nuevo.TotalAplicaciones ||
                actual.Items.Count != nuevo.Items.Count)
            {
                return false;
            }

            return actual.Items.All(item =>
                nuevo.Items.Any(nuevoItem =>
                    nuevoItem.FuenteNutrientesId == item.FuenteNutrientesId &&
                    nuevoItem.ElementoQuimicosId == item.ElementoQuimicosId &&
                    nuevoItem.RequerimientoOriginal == item.RequerimientoOriginal
                )
            );
        }

        private static string NormalizarSimbolo(string? simbolo)
        {
            return (simbolo ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", string.Empty);
        }

        private static string FormatearSimbolo(string? simbolo)
        {
            string normalizado = NormalizarSimbolo(simbolo);

            return normalizado switch
            {
                "CA" => "Ca",
                "MG" => "Mg",
                "ZN" => "Zn",
                _ => normalizado
            };
        }

        private static string FormatearMoneda(decimal valor)
        {
            return $"C$ {valor.ToString("N2", CultureInfo.InvariantCulture)}";
        }
    }

    public class CostoFuenteOrganicaViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public decimal CantidadQq { get; set; }

        public decimal PrecioPorQq { get; set; }

        public decimal Costo { get; set; }

        public string TextoCantidadQq => CantidadQq.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoPrecioPorQq => $"C$ {PrecioPorQq.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoCosto => $"C$ {Costo.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    public class CompraComercialAjustadaViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public string Elemento { get; set; } = string.Empty;

        public decimal QuintalesOriginales { get; set; }

        public decimal QuintalesAjustados { get; set; }

        public decimal ReduccionQuintales { get; set; }

        public decimal PrecioPorQq { get; set; }

        public decimal QuintalesComprar { get; set; }

        public decimal CostoCompra { get; set; }

        public string TextoQuintalesOriginales => QuintalesOriginales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoQuintalesAjustados => QuintalesAjustados.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoReduccionQuintales => ReduccionQuintales.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoPrecioPorQq => $"C$ {PrecioPorQq.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoQuintalesComprar => QuintalesComprar.ToString("N0", CultureInfo.InvariantCulture);

        public string TextoCostoCompra => $"C$ {CostoCompra.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    public class EncabezadoAporteMatrizViewModel
    {
        public int? ElementoQuimicosId { get; set; }

        public string Simbolo { get; set; } = string.Empty;

        public string Titulo => $"{Simbolo} (lb)";
    }

    public class FuenteAporteMatrizViewModel
    {
        public int? FuenteNutrientesId { get; set; }

        public string Fuente { get; set; } = string.Empty;

        public decimal CantidadQq { get; set; }

        public ObservableCollection<CeldaAporteMatrizViewModel> Aportes { get; } = new();

        public string TextoCantidadQq => CantidadQq.ToString("N2", CultureInfo.InvariantCulture);
    }

    public class CeldaAporteMatrizViewModel
    {
        public string Simbolo { get; set; } = string.Empty;

        public decimal AporteLibras { get; set; }

        public string TextoAporteLibras =>
            AporteLibras.ToString("N2", CultureInfo.InvariantCulture);
    }

    public class SugerenciaIncrementoFuenteViewModel
    {
        public int? FuenteNutrientesId { get; set; }

        public string Fuente { get; set; } = string.Empty;

        public decimal CantidadActualQq { get; set; }

        public decimal IncrementarQq { get; set; }

        public decimal TotalSugeridoQq { get; set; }

        public decimal PrecioPorQq { get; set; }

        public decimal CostoAdicional { get; set; }

        public string TextoCantidadActualQq => CantidadActualQq.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoIncrementarQq => $"+{IncrementarQq.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoTotalSugeridoQq => TotalSugeridoQq.ToString("N2", CultureInfo.InvariantCulture);

        public string TextoCostoAdicional =>
            $"C$ {CostoAdicional.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    internal class FuenteOptimizacionLocal
    {
        public int? FuenteNutrientesId { get; set; }

        public string Fuente { get; set; } = string.Empty;

        public decimal CantidadActualQq { get; set; }

        public decimal PrecioPorQq { get; set; }

        public decimal IncrementoQq { get; set; }

        public Dictionary<string, decimal> Aportes { get; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    internal class EstadoFuenteAntesSugerencia
    {
        public int? FuenteNutrientesId { get; set; }

        public bool EstaSeleccionada { get; set; }

        public string CantidadQq { get; set; } = string.Empty;
    }
}
