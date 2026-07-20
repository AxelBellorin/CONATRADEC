using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    public partial class AnalisisGuardadoDetallePage : ContentPage
    {
        private enum SeccionVisualizacion
        {
            Ninguna,
            Balance,
            Enmienda,
            Mixta
        }

        private readonly AnalisisGuardadoDetalleViewModel viewModel =
            new();

        private readonly Command editarMismaInterfazCommand;

        private readonly FertilizacionMixtaApiService
            fertilizacionMixtaVisualApiService = new();

        private SeccionVisualizacion seccionActual =
            SeccionVisualizacion.Ninguna;

        private int versionCargaComplemento;
        private bool complementoMixtaPreparado;
        private bool cargandoComplementoMixta;

        private decimal costoComercialOriginalVisual;
        private decimal costoFertilizacionMixtaVisual;
        private decimal costoComercialAjustadoVisual;
        private decimal costoTotalFinalVisual;
        private decimal diferenciaEconomicaVisual;
        private string mensajeComplementoMixtaVisual = string.Empty;

        public ObservableCollection<CostoFuenteOrganicaViewModel>
            FilasCostoOrganicoVisual { get; } = new();

        public ObservableCollection<CompraComercialComplementoVisualViewModel>
            FilasCompraAjustadaVisual { get; } = new();

        public bool TieneComplementoMixtaVisual =>
            viewModel.Detalle?.FertilizacionMixta?.Mixta
                .EsComplementoBalance == true &&
            viewModel.Detalle?.BalanceNutricional != null;

        public bool TieneTablaCostosOrganicosVisual =>
            TieneComplementoMixtaVisual &&
            FilasCostoOrganicoVisual.Count > 0;

        public bool TieneCompraAjustadaVisual =>
            TieneComplementoMixtaVisual &&
            FilasCompraAjustadaVisual.Count > 0 &&
            FilasCompraAjustadaVisual.Any(x =>
                x.QuintalesComprar > 0);

        public bool NoRequiereCompraComercialVisual =>
            TieneComplementoMixtaVisual &&
            FilasCompraAjustadaVisual.Count > 0 &&
            FilasCompraAjustadaVisual.All(x =>
                x.QuintalesComprar <= 0);

        public bool TieneResumenEconomicoComplementoVisual =>
            TieneComplementoMixtaVisual &&
            complementoMixtaPreparado;

        public bool TieneMensajeComplementoMixtaVisual =>
            !string.IsNullOrWhiteSpace(
                MensajeComplementoMixtaVisual);

        public bool CargandoComplementoMixta
        {
            get => cargandoComplementoMixta;
            private set
            {
                if (cargandoComplementoMixta == value)
                    return;

                cargandoComplementoMixta = value;
                OnPropertyChanged();
            }
        }

        public string MensajeComplementoMixtaVisual
        {
            get => mensajeComplementoMixtaVisual;
            private set
            {
                mensajeComplementoMixtaVisual =
                    value ?? string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(
                        TieneMensajeComplementoMixtaVisual));
            }
        }

        public decimal CostoComercialOriginalVisual
        {
            get => costoComercialOriginalVisual;
            private set
            {
                costoComercialOriginalVisual =
                    RedondearMoneda(value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(
                        TextoCostoComercialOriginalVisual));
            }
        }

        public decimal CostoFertilizacionMixtaVisual
        {
            get => costoFertilizacionMixtaVisual;
            private set
            {
                costoFertilizacionMixtaVisual =
                    RedondearMoneda(value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(
                        TextoCostoFertilizacionMixtaVisual));
            }
        }

        public decimal CostoComercialAjustadoVisual
        {
            get => costoComercialAjustadoVisual;
            private set
            {
                costoComercialAjustadoVisual =
                    RedondearMoneda(value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(
                        TextoCostoComercialAjustadoVisual));
            }
        }

        public decimal CostoTotalFinalVisual
        {
            get => costoTotalFinalVisual;
            private set
            {
                costoTotalFinalVisual =
                    RedondearMoneda(value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TextoCostoTotalFinalVisual));
            }
        }

        public decimal DiferenciaEconomicaVisual
        {
            get => diferenciaEconomicaVisual;
            private set
            {
                diferenciaEconomicaVisual =
                    RedondearMoneda(value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TextoDiferenciaEconomicaVisual));
                OnPropertyChanged(
                    nameof(EtiquetaDiferenciaEconomicaVisual));
                OnPropertyChanged(
                    nameof(ColorDiferenciaEconomicaVisual));
            }
        }

        public string TextoCostoComercialOriginalVisual =>
            FormatearMoneda(
                CostoComercialOriginalVisual);

        public string TextoCostoFertilizacionMixtaVisual =>
            FormatearMoneda(
                CostoFertilizacionMixtaVisual);

        public string TextoCostoComercialAjustadoVisual =>
            FormatearMoneda(
                CostoComercialAjustadoVisual);

        public string TextoCostoTotalFinalVisual =>
            FormatearMoneda(
                CostoTotalFinalVisual);

        public string TextoDiferenciaEconomicaVisual =>
            FormatearMoneda(
                Math.Abs(DiferenciaEconomicaVisual));

        public string EtiquetaDiferenciaEconomicaVisual =>
            DiferenciaEconomicaVisual >= 0
                ? "Ahorro frente al balance original"
                : "Incremento frente al balance original";

        public string ColorDiferenciaEconomicaVisual =>
            DiferenciaEconomicaVisual >= 0
                ? "#3B655B"
                : "#DC2626";

        public AnalisisGuardadoDetallePage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            editarMismaInterfazCommand =
                new Command(
                    async () =>
                        await EditarMismaInterfazAsync());

            InitializeComponent();

            BindingContext = viewModel;
            EditarButton.Command =
                editarMismaInterfazCommand;

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para visualizar análisis.",
                    "Aceptar");

                await Shell.Current.GoToAsync("..");
                return;
            }

            ActualizarSeccionesDisponibles();

            if (!complementoMixtaPreparado &&
                TieneComplementoMixtaVisual)
            {
                _ = PrepararComplementoMixtaVisualAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            bool esCambioDetalle =
                e.PropertyName ==
                nameof(
                    AnalisisGuardadoDetalleViewModel.Detalle);

            if (!esCambioDetalle &&
                e.PropertyName !=
                    nameof(
                        AnalisisGuardadoDetalleViewModel
                            .TieneBalance) &&
                e.PropertyName !=
                    nameof(
                        AnalisisGuardadoDetalleViewModel
                            .TieneEnmienda) &&
                e.PropertyName !=
                    nameof(
                        AnalisisGuardadoDetalleViewModel
                            .TieneFertilizacionMixta))
            {
                return;
            }

            Dispatcher.Dispatch(
                ActualizarSeccionesDisponibles);

            if (!esCambioDetalle)
                return;

            versionCargaComplemento++;
            complementoMixtaPreparado = false;
            LimpiarComplementoMixtaVisual();

            if (TieneComplementoMixtaVisual)
                _ = PrepararComplementoMixtaVisualAsync();
        }

        private void ActualizarSeccionesDisponibles()
        {
            bool tieneBalance =
                viewModel.TieneBalance;

            bool tieneEnmienda =
                viewModel.TieneEnmienda;

            bool tieneMixta =
                viewModel.TieneFertilizacionMixta;

            bool tieneCalculos =
                tieneBalance ||
                tieneEnmienda ||
                tieneMixta;

            ContenedorCalculos.IsVisible =
                tieneCalculos;

            SinCalculosCard.IsVisible =
                viewModel.TieneDetalle &&
                !tieneCalculos;

            BalanceTabButton.IsVisible =
                tieneBalance;

            EnmiendaTabButton.IsVisible =
                tieneEnmienda;

            MixtaTabButton.IsVisible =
                tieneMixta;

            bool seccionActualValida =
                seccionActual switch
                {
                    SeccionVisualizacion.Balance =>
                        tieneBalance,

                    SeccionVisualizacion.Enmienda =>
                        tieneEnmienda,

                    SeccionVisualizacion.Mixta =>
                        tieneMixta,

                    _ => false
                };

            if (seccionActualValida)
            {
                MostrarSeccion(seccionActual);
                return;
            }

            if (tieneBalance)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Balance);
            }
            else if (tieneEnmienda)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Enmienda);
            }
            else if (tieneMixta)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Mixta);
            }
            else
            {
                MostrarSeccion(
                    SeccionVisualizacion.Ninguna);
            }
        }

        private void BalanceTabButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.TieneBalance)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Balance);
            }
        }

        private void EnmiendaTabButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.TieneEnmienda)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Enmienda);
            }
        }

        private void MixtaTabButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.TieneFertilizacionMixta)
            {
                MostrarSeccion(
                    SeccionVisualizacion.Mixta);
            }
        }

        private void MostrarSeccion(
            SeccionVisualizacion seccion)
        {
            seccionActual = seccion;

            BalancePanel.IsVisible =
                seccion ==
                SeccionVisualizacion.Balance;

            EnmiendaPanel.IsVisible =
                seccion ==
                SeccionVisualizacion.Enmienda;

            MixtaPanel.IsVisible =
                seccion ==
                SeccionVisualizacion.Mixta;

            AplicarEstiloTab(
                BalanceTabButton,
                seccion ==
                    SeccionVisualizacion.Balance,
                Color.FromArgb("#3B655B"),
                Colors.White);

            AplicarEstiloTab(
                EnmiendaTabButton,
                seccion ==
                    SeccionVisualizacion.Enmienda,
                Color.FromArgb("#9B552C"),
                Colors.White);

            AplicarEstiloTab(
                MixtaTabButton,
                seccion ==
                    SeccionVisualizacion.Mixta,
                Color.FromArgb("#F2C94C"),
                Colors.Black);
        }

        private static void AplicarEstiloTab(
            Button boton,
            bool activo,
            Color colorActivo,
            Color textoActivo)
        {
            boton.BackgroundColor =
                activo
                    ? colorActivo
                    : Color.FromArgb("#E5E7EB");

            boton.TextColor =
                activo
                    ? textoActivo
                    : Colors.Black;
        }

        private async Task PrepararComplementoMixtaVisualAsync()
        {
            AnalisisGuardadoDetalleData? detalleActual =
                viewModel.Detalle;

            if (detalleActual?.FertilizacionMixta?.Mixta
                    .EsComplementoBalance != true ||
                detalleActual.BalanceNutricional == null)
            {
                LimpiarComplementoMixtaVisual();
                return;
            }

            int versionActual = ++versionCargaComplemento;

            try
            {
                CargandoComplementoMixta = true;
                MensajeComplementoMixtaVisual = string.Empty;

                ObservableCollection<
                    FuenteNutrienteFertilizacionMixtaResponse>
                        catalogoFuentes =
                            await fertilizacionMixtaVisualApiService
                                .ListarFuentesFertilizacionMixtaAsync();

                if (versionActual != versionCargaComplemento ||
                    !ReferenceEquals(
                        detalleActual,
                        viewModel.Detalle))
                {
                    return;
                }

                Dictionary<int, decimal> preciosPorFuente =
                    catalogoFuentes
                        .Where(x =>
                            x.FuenteNutrientesId is > 0)
                        .GroupBy(x =>
                            x.FuenteNutrientesId!.Value)
                        .ToDictionary(
                            x => x.Key,
                            x => x.First()
                                .PrecioNutriente ?? 0m);

                ConstruirCostosOrganicosVisuales(
                    detalleActual,
                    preciosPorFuente);

                ConstruirCompraComercialAjustadaVisual(
                    detalleActual);

                CostoComercialOriginalVisual =
                    detalleActual
                        .BalanceNutricional
                        .Detalles
                        .Sum(x =>
                            Math.Ceiling(
                                Math.Max(0m, x.Qq)) *
                            Math.Max(
                                0m,
                                x.PrecioPorQuintal));

                CostoComercialAjustadoVisual =
                    FilasCompraAjustadaVisual
                        .Sum(x => x.CostoCompra);

                CostoFertilizacionMixtaVisual =
                    FilasCostoOrganicoVisual
                        .Sum(x => x.Costo);

                CostoTotalFinalVisual =
                    CostoComercialAjustadoVisual +
                    CostoFertilizacionMixtaVisual;

                DiferenciaEconomicaVisual =
                    CostoComercialOriginalVisual -
                    CostoTotalFinalVisual;

                complementoMixtaPreparado = true;

                if (detalleActual
                        .FertilizacionMixta
                        .Fuentes.Count > 0 &&
                    preciosPorFuente.Count == 0)
                {
                    MensajeComplementoMixtaVisual =
                        "Se reconstruyó el ajuste comercial, pero no " +
                        "fue posible obtener los precios actuales de " +
                        "las fuentes orgánicas.";
                }

                NotificarComplementoMixtaVisual();
            }
            catch (Exception ex)
            {
                if (versionActual != versionCargaComplemento)
                    return;

                complementoMixtaPreparado = false;
                MensajeComplementoMixtaVisual =
                    "No fue posible reconstruir todos los cálculos " +
                    $"complementarios: {ex.Message}";

                NotificarComplementoMixtaVisual();
            }
            finally
            {
                if (versionActual == versionCargaComplemento)
                    CargandoComplementoMixta = false;
            }
        }

        private void ConstruirCostosOrganicosVisuales(
            AnalisisGuardadoDetalleData detalle,
            IReadOnlyDictionary<int, decimal> preciosPorFuente)
        {
            FilasCostoOrganicoVisual.Clear();

            AnalisisGuardadoFertilizacionMixta mixta =
                detalle.FertilizacionMixta!;

            foreach (AnalisisGuardadoMixtaFuente fuente in
                     mixta.Fuentes)
            {
                decimal precio =
                    preciosPorFuente.TryGetValue(
                        fuente.FuenteNutrientesId,
                        out decimal precioFuente)
                            ? precioFuente
                            : 0m;

                FilasCostoOrganicoVisual.Add(
                    new CostoFuenteOrganicaViewModel
                    {
                        Fuente = fuente.FuenteMostrar,
                        CantidadQq = fuente.CantidadQq,
                        PrecioPorQq = precio,
                        Costo = fuente.CantidadQq * precio
                    });
            }
        }

        private void ConstruirCompraComercialAjustadaVisual(
            AnalisisGuardadoDetalleData detalle)
        {
            FilasCompraAjustadaVisual.Clear();

            AnalisisGuardadoFertilizacionMixta mixta =
                detalle.FertilizacionMixta!;

            foreach (AnalisisGuardadoFormulaDetalle original in
                     detalle.BalanceNutricional!.Detalles)
            {
                AnalisisGuardadoMixtaDetalle? resultadoOrganico =
                    mixta.Detalles.FirstOrDefault(x =>
                        x.ElementoQuimicosId ==
                        original.ElementoQuimicosId);

                decimal requerimientoOriginal =
                    original.RequerimientoLibras > 0
                        ? original.RequerimientoLibras
                        : resultadoOrganico?
                            .RequerimientoOriginal ?? 0m;

                decimal aporteOrganico =
                    resultadoOrganico?.AporteOrganico ?? 0m;

                decimal restante =
                    resultadoOrganico != null
                        ? Math.Max(
                            0m,
                            resultadoOrganico.Deficit)
                        : requerimientoOriginal;

                decimal proporcionRestante =
                    requerimientoOriginal > 0
                        ? Math.Clamp(
                            restante /
                            requerimientoOriginal,
                            0m,
                            1m)
                        : 0m;

                decimal qqAjustados =
                    RedondearCantidad(
                        Math.Max(0m, original.Qq) *
                        proporcionRestante);

                decimal qqComprar =
                    Math.Ceiling(qqAjustados);

                decimal precio =
                    Math.Max(
                        0m,
                        original.PrecioPorQuintal);

                FilasCompraAjustadaVisual.Add(
                    new CompraComercialComplementoVisualViewModel
                    {
                        Fuente = original.FuenteMostrar,
                        Elemento = original.ElementoMostrar,
                        RequerimientoOriginal =
                            RedondearCantidad(
                                requerimientoOriginal),
                        AporteOrganico =
                            RedondearCantidad(
                                aporteOrganico),
                        RequerimientoRestante =
                            RedondearCantidad(restante),
                        QuintalesOriginales =
                            RedondearCantidad(
                                original.Qq),
                        QuintalesAjustados =
                            qqAjustados,
                        ReduccionQuintales =
                            RedondearCantidad(
                                Math.Max(
                                    0m,
                                    original.Qq -
                                    qqAjustados)),
                        PrecioPorQq = precio,
                        QuintalesComprar = qqComprar,
                        CostoCompra =
                            RedondearMoneda(
                                qqComprar * precio)
                    });
            }
        }

        private void LimpiarComplementoMixtaVisual()
        {
            FilasCostoOrganicoVisual.Clear();
            FilasCompraAjustadaVisual.Clear();

            CostoComercialOriginalVisual = 0m;
            CostoFertilizacionMixtaVisual = 0m;
            CostoComercialAjustadoVisual = 0m;
            CostoTotalFinalVisual = 0m;
            DiferenciaEconomicaVisual = 0m;

            complementoMixtaPreparado = false;
            MensajeComplementoMixtaVisual = string.Empty;

            NotificarComplementoMixtaVisual();
        }

        private void NotificarComplementoMixtaVisual()
        {
            OnPropertyChanged(
                nameof(TieneComplementoMixtaVisual));
            OnPropertyChanged(
                nameof(TieneTablaCostosOrganicosVisual));
            OnPropertyChanged(
                nameof(TieneCompraAjustadaVisual));
            OnPropertyChanged(
                nameof(NoRequiereCompraComercialVisual));
            OnPropertyChanged(
                nameof(
                    TieneResumenEconomicoComplementoVisual));
            OnPropertyChanged(
                nameof(FilasCostoOrganicoVisual));
            OnPropertyChanged(
                nameof(FilasCompraAjustadaVisual));
        }

        private static decimal RedondearCantidad(
            decimal valor)
        {
            return Math.Round(
                valor,
                4,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RedondearMoneda(
            decimal valor)
        {
            return Math.Round(
                valor,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string FormatearMoneda(
            decimal valor)
        {
            return $"C$ {valor.ToString("N2", CultureInfo.InvariantCulture)}";
        }

        private async Task EditarMismaInterfazAsync()
        {
            if (viewModel.IsBusy ||
                viewModel.Resumen == null)
            {
                return;
            }

            if (!viewModel.CanEdit)
            {
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para editar análisis.");
                return;
            }

            try
            {
                viewModel.IsBusy = true;

                (bool success, string message) =
                    await AnalisisEdicionService.Instance
                        .PrepararAsync(
                            viewModel
                                .Resumen
                                .AnalisisSueloCalculoId,
                            viewModel.Resumen);

                if (!success)
                {
                    await DisplayAlert(
                        "No se pudo abrir",
                        message,
                        "Aceptar");

                    return;
                }

                await Shell.Current.GoToAsync(
                    "//NuevoAnalisisFormPage");
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }
    }
}
