using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public partial class FertilizacionMixtaTabViewModel : BindableObject
    {
        private readonly FertilizacionMixtaApiService fertilizacionMixtaApiService = new();

        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private FertilizacionMixtaCalculoResponse? resultadoFertilizacionMixta;

        private string observacion = string.Empty;
        private string mensaje = "Seleccione las fuentes orgánicas y calcule la fertilización mixta.";
        private string errorFuentes = string.Empty;
        private string errorElementos = string.Empty;

        private bool isBusy;
        private bool suspendiendoCambiosTemporales;

        public FertilizacionMixtaTabViewModel()
        {
            ElementosExportables = new ObservableCollection<ElementoFertilizacionMixtaItemViewModel>();
            FuentesDisponibles = new ObservableCollection<FuenteFertilizacionMixtaItemViewModel>();

            CalcularCommand = new Command(
                async () => await CalcularAsync(),
                () => PuedeCalcular
            );

            ReiniciarCommand = new Command(
                async () => await ReiniciarAsync(),
                () => !IsBusy
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

        public FertilizacionMixtaCalculoResponse? ResultadoFertilizacionMixta
        {
            get => resultadoFertilizacionMixta;
            set
            {
                resultadoFertilizacionMixta = value;
                OnPropertyChanged(nameof(ResultadoFertilizacionMixta));
                OnPropertyChanged(nameof(TieneResultadoFertilizacionMixta));
                OnPropertyChanged(nameof(NoTieneResultadoFertilizacionMixta));
                OnPropertyChanged(nameof(TieneComplementoCompleto));
            }
        }

        public bool TieneResultadoFertilizacionMixta =>
            ResultadoFertilizacionMixta?.Detalles != null &&
            ResultadoFertilizacionMixta.Detalles.Count > 0;

        public bool NoTieneResultadoFertilizacionMixta => !TieneResultadoFertilizacionMixta;

        public string Observacion
        {
            get => observacion;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (observacion == nuevoValor)
                    return;

                observacion = nuevoValor;
                OnPropertyChanged(nameof(Observacion));

                _ = MarcarFertilizacionPendienteSiTieneResultadoAsync();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
            }
        }

        public string ErrorFuentes
        {
            get => errorFuentes;
            set
            {
                errorFuentes = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorFuentes));
                OnPropertyChanged(nameof(TieneErrorFuentes));
            }
        }

        public bool TieneErrorFuentes => !string.IsNullOrWhiteSpace(ErrorFuentes);

        public string ErrorElementos
        {
            get => errorElementos;
            set
            {
                errorElementos = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorElementos));
                OnPropertyChanged(nameof(TieneErrorElementos));
            }
        }

        public bool TieneErrorElementos => !string.IsNullOrWhiteSpace(ErrorElementos);

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(PuedeCalcular));
                RefrescarComandos();
            }
        }

        public ObservableCollection<ElementoFertilizacionMixtaItemViewModel> ElementosExportables { get; }

        public ObservableCollection<FuenteFertilizacionMixtaItemViewModel> FuentesDisponibles { get; }

        public bool TieneElementosExportables => ElementosExportables.Count > 0;

        public bool TieneFuentesDisponibles => FuentesDisponibles.Count > 0;

        public bool PuedeCalcular =>
            !IsBusy &&
            TieneElementosExportables &&
            TieneFuentesDisponibles;

        public Command CalcularCommand { get; }

        public Command ReiniciarCommand { get; }

        public void Inicializar(
            AnalisisSueloCalculoDataResponse? resultado,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            ResultadoCalculo = resultado;
            RequestGuardarAnalisis = requestGuardar;

            ResultadoFertilizacionMixta = null;
            PrepararNuevaInicializacion();

            LimpiarErrores();
            CargarElementosDesdeResultadoAnalisis();

            Observacion = ObtenerNombreAnalisisSuelo();

            _ = InicializarEstadoTemporalYCargarFuentesAsync();
        }

        private string ObtenerNombreAnalisisSuelo()
        {
            string nombre = RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
                return "Fertilización mixta";

            return nombre.Trim();
        }

        private async Task InicializarEstadoTemporalYCargarFuentesAsync()
        {
            try
            {
                suspendiendoCambiosTemporales = true;

                await CalculoAnalisisTemporalService.Instance.IniciarNuevoCalculoAsync(
                    ResultadoCalculo,
                    RequestGuardarAnalisis
                );

                FertilizacionMixtaCalculoResponse? resultadoTemporal =
                    CalculoAnalisisTemporalService.Instance.ObtenerResultado<FertilizacionMixtaCalculoResponse>(
                        TipoCalculoTemporal.FertilizacionMixta
                    );

                if (resultadoTemporal != null &&
                    resultadoTemporal.Detalles != null &&
                    resultadoTemporal.Detalles.Count > 0)
                {
                    NormalizarResultado(resultadoTemporal);

                    if (EsComplementoBalance)
                    {
                        recalcularComplementoPendiente = true;
                    }
                    else
                    {
                        ResultadoFertilizacionMixta = resultadoTemporal;
                        Mensaje = "Se cargó el último resultado temporal de fertilización mixta.";
                    }
                }

                await CargarFuentesFertilizacionMixtaAsync();

                FertilizacionMixtaCalcularRequest? requestTemporal =
                    CalculoAnalisisTemporalService.Instance.ObtenerRequest<FertilizacionMixtaCalcularRequest>(
                        TipoCalculoTemporal.FertilizacionMixta
                    );

                if (requestTemporal != null)
                    RestaurarFuentesDesdeRequestTemporal(requestTemporal);

                if (!EsComplementoBalance && ResultadoFertilizacionMixta != null)
                {
                    ConstruirMatrizAportesPorFuente();
                    ConstruirTablaCostosOrganicos();
                    ConstruirSugerenciaIncremento();
                }

                Observacion = ObtenerNombreAnalisisSuelo();

                if (EsComplementoBalance &&
                    contextoBalance != null &&
                    recalcularComplementoPendiente &&
                    TieneFuentesSeleccionadasValidas())
                {
                    recalcularComplementoPendiente = false;
                    await CalcularAsync();
                }
            }
            finally
            {
                suspendiendoCambiosTemporales = false;
                RefrescarComandos();
            }
        }

        private void CargarElementosDesdeResultadoAnalisis()
        {
            ElementosExportables.Clear();

            if (ResultadoCalculo?.Elementos == null || ResultadoCalculo.Elementos.Count == 0)
            {
                ErrorElementos = "No se encontraron elementos químicos en el resultado del análisis de suelo.";
                OnPropertyChanged(nameof(TieneElementosExportables));
                RefrescarComandos();
                return;
            }

            foreach (var elemento in ResultadoCalculo.Elementos)
            {
                if (elemento == null)
                    continue;

                if (elemento.ElementoQuimicosId == null || elemento.ElementoQuimicosId <= 0)
                    continue;

                decimal requerimiento = Redondear2(elemento.RequerimientoCalculado ?? 0);

                string simbolo = (elemento.SimboloElementoQuimico ?? string.Empty).Trim();
                string nombre = (elemento.NombreElementoQuimico ?? string.Empty).Trim();

                ElementosExportables.Add(new ElementoFertilizacionMixtaItemViewModel
                {
                    ElementoQuimicosId = elemento.ElementoQuimicosId,
                    SimboloElementoQuimico = simbolo,
                    NombreElementoQuimico = nombre,
                    Exportable = requerimiento
                });
            }

            if (ElementosExportables.Count == 0)
                ErrorElementos = "No hay elementos químicos válidos para enviar al cálculo de fertilización mixta.";

            OnPropertyChanged(nameof(TieneElementosExportables));
            RefrescarComandos();
        }

        private async Task CargarFuentesFertilizacionMixtaAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                LimpiarErrores();

                foreach (var fuenteActual in FuentesDisponibles)
                    fuenteActual.CambioFormulario -= Fuente_CambioFormulario;

                FuentesDisponibles.Clear();

                ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse> fuentes =
                    await fertilizacionMixtaApiService.ListarFuentesFertilizacionMixtaAsync();

                foreach (var fuente in fuentes)
                {
                    if (fuente == null)
                        continue;

                    if (fuente.FuenteNutrientesId == null || fuente.FuenteNutrientesId <= 0)
                        continue;

                    if (fuente.Activo == false)
                        continue;

                    FuenteFertilizacionMixtaItemViewModel item = new FuenteFertilizacionMixtaItemViewModel
                    {
                        FuenteNutrientesId = fuente.FuenteNutrientesId,
                        NombreFuente = fuente.NombreNutriente ?? string.Empty,
                        DescripcionFuente = fuente.DescripcionNutriente ?? string.Empty,
                        PrecioFuente = fuente.PrecioNutriente,
                        ElementosTexto = ConstruirTextoElementosFuente(fuente.ElementosQuimicos)
                    };

                    item.CambioFormulario += Fuente_CambioFormulario;

                    FuentesDisponibles.Add(item);
                }

                if (FuentesDisponibles.Count == 0)
                    ErrorFuentes = "No se encontraron fuentes configuradas para fertilización mixta.";

                OnPropertyChanged(nameof(TieneFuentesDisponibles));
            }
            catch (Exception ex)
            {
                ErrorFuentes = $"No se pudieron cargar las fuentes: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async void Fuente_CambioFormulario(object? sender, EventArgs e)
        {
            if (!suspendiendoCambiosTemporales)
                DescartarHistorialSugerencia();

            await MarcarFertilizacionPendienteSiTieneResultadoAsync();
        }

        private async Task MarcarFertilizacionPendienteSiTieneResultadoAsync()
        {
            if (suspendiendoCambiosTemporales)
                return;

            if (ResultadoFertilizacionMixta == null)
                return;

            await CalculoAnalisisTemporalService.Instance.MarcarPendienteRecalculoAsync(
                TipoCalculoTemporal.FertilizacionMixta,
                "La fertilización mixta cambió. Debe recalcular para actualizar el resultado.",
                true
            );

            ResultadoFertilizacionMixta = null;
            LimpiarResultadosPresentacion();
            Mensaje = "Hay cambios pendientes. Presione Calcular para actualizar la fertilización mixta.";
        }

        private void RestaurarFuentesDesdeRequestTemporal(FertilizacionMixtaCalcularRequest requestTemporal)
        {
            if (requestTemporal.Fuentes == null || requestTemporal.Fuentes.Count == 0)
                return;

            foreach (var fuenteRequest in requestTemporal.Fuentes)
            {
                if (fuenteRequest.FuenteNutrientesId == null)
                    continue;

                FuenteFertilizacionMixtaItemViewModel? fuente =
                    FuentesDisponibles.FirstOrDefault(x =>
                        x.FuenteNutrientesId == fuenteRequest.FuenteNutrientesId
                    );

                if (fuente == null)
                    continue;

                fuente.EstaSeleccionada = true;

                fuente.CantidadQq = Redondear2(fuenteRequest.CantidadQq ?? 0)
                    .ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private static string ConstruirTextoElementosFuente(List<ElementoFuenteNutrienteFertilizacionMixtaResponse>? elementos)
        {
            if (elementos == null || elementos.Count == 0)
                return "No tiene elementos configurados.";

            List<string> partes = new();

            foreach (var elemento in elementos)
            {
                if (elemento == null)
                    continue;

                string simbolo = (elemento.SimboloElementoQuimico ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(simbolo))
                    simbolo = (elemento.NombreElementoQuimico ?? string.Empty).Trim();

                decimal aporte = Redondear2(elemento.CantidadAporte ?? 0);

                partes.Add($"{simbolo}: {aporte.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            if (partes.Count == 0)
                return "No tiene elementos configurados.";

            return "Aporta por qq: " + string.Join(" | ", partes);
        }

        private async Task CalcularAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();
                LimpiarErrores();

                bool valido = await ValidarFormularioAsync();

                if (!valido)
                    return;

                FertilizacionMixtaCalcularRequest request = ConstruirRequest();

                FertilizacionMixtaCalculoResponse? response =
                    await fertilizacionMixtaApiService.CalcularAsync(request);

                if (response == null)
                {
                    await MostrarMensajeAsync("Error", "La API no devolvió una respuesta válida.");
                    return;
                }

                if (!response.Success)
                {
                    await MostrarMensajeAsync("Error", response.Message ?? "No se pudo calcular la fertilización mixta.");
                    return;
                }

                NormalizarResultado(response);

                ResultadoFertilizacionMixta = response;

                ConstruirMatrizAportesPorFuente();
                ConstruirTablaCostosOrganicos();
                ConstruirSugerenciaIncremento();

                bool complementoCalculado =
                    await CalcularBalanceAjustadoAsync(response);

                await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                    TipoCalculoTemporal.FertilizacionMixta,
                    request,
                    response,
                    "Fertilización mixta calculada correctamente."
                );

                Mensaje = EsComplementoBalance
                    ? complementoCalculado
                        ? "Fertilización mixta y balance comercial ajustado calculados correctamente."
                        : "La fertilización mixta se calculó, pero no fue posible completar el balance ajustado."
                    : "Cálculo de fertilización mixta realizado correctamente.";
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo calcular la fertilización mixta: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private FertilizacionMixtaCalcularRequest ConstruirRequest()
        {
            FertilizacionMixtaCalcularRequest request = new()
            {
                Observacion = ObtenerNombreAnalisisSuelo()
            };

            foreach (var elemento in ElementosExportables)
            {
                request.Elementos.Add(new ElementoFertilizacionMixtaRequest
                {
                    ElementoQuimicosId = elemento.ElementoQuimicosId,
                    Exportable = Redondear2(elemento.Exportable)
                });
            }

            foreach (var fuente in FuentesDisponibles.Where(x => x.EstaSeleccionada))
            {
                decimal cantidad = ConvertirDecimal(fuente.CantidadQq);

                request.Fuentes.Add(new FuenteFertilizacionMixtaRequest
                {
                    FuenteNutrientesId = fuente.FuenteNutrientesId,
                    CantidadQq = Redondear2(cantidad)
                });
            }

            return request;
        }

        private async Task<bool> ValidarFormularioAsync()
        {
            if (ElementosExportables.Count == 0)
            {
                ErrorElementos = "No hay elementos químicos para calcular la fertilización mixta.";
                await MostrarMensajeAsync("Validación", ErrorElementos);
                return false;
            }

            foreach (var elemento in ElementosExportables)
            {
                if (elemento.ElementoQuimicosId == null || elemento.ElementoQuimicosId <= 0)
                {
                    ErrorElementos = $"El elemento {elemento.ElementoMostrar} no tiene identificador válido.";
                    await MostrarMensajeAsync("Validación", ErrorElementos);
                    return false;
                }

                if (elemento.Exportable < 0)
                {
                    ErrorElementos = $"El requerimiento de {elemento.ElementoMostrar} no puede ser negativo.";
                    await MostrarMensajeAsync("Validación", ErrorElementos);
                    return false;
                }
            }

            List<FuenteFertilizacionMixtaItemViewModel> fuentesSeleccionadas =
                FuentesDisponibles
                    .Where(x => x.EstaSeleccionada)
                    .ToList();

            if (fuentesSeleccionadas.Count == 0)
            {
                ErrorFuentes = "Debe seleccionar al menos una fuente: Pulpa de café, Gallinaza u otra fuente configurada.";
                await MostrarMensajeAsync("Validación", ErrorFuentes);
                return false;
            }

            foreach (var fuente in FuentesDisponibles)
            {
                fuente.ErrorCantidad = string.Empty;
            }

            foreach (var fuente in fuentesSeleccionadas)
            {
                if (string.IsNullOrWhiteSpace(fuente.CantidadQq))
                {
                    fuente.ErrorCantidad = "Ingrese la cantidad en quintales.";
                    await MostrarMensajeAsync("Validación", $"Debe ingresar la cantidad en quintales para {fuente.NombreFuente}.");
                    return false;
                }

                if (!TryParseDecimal(fuente.CantidadQq, out decimal cantidad))
                {
                    fuente.ErrorCantidad = "Cantidad inválida.";
                    await MostrarMensajeAsync("Validación", $"La cantidad de {fuente.NombreFuente} debe ser numérica.");
                    return false;
                }

                if (cantidad <= 0)
                {
                    fuente.ErrorCantidad = "Debe ser mayor que cero.";
                    await MostrarMensajeAsync("Validación", $"La cantidad de {fuente.NombreFuente} debe ser mayor que cero.");
                    return false;
                }

                fuente.CantidadQq = Redondear2(cantidad).ToString("0.00", CultureInfo.InvariantCulture);
            }

            return true;
        }

        private async Task ReiniciarAsync()
        {
            if (IsBusy)
                return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Reiniciar fertilización mixta",
                "¿Desea limpiar las fuentes seleccionadas y el resultado calculado?",
                "Sí, reiniciar",
                "Cancelar"
            );

            if (!confirmar)
                return;

            try
            {
                suspendiendoCambiosTemporales = true;

                foreach (var fuente in FuentesDisponibles)
                {
                    fuente.EstaSeleccionada = false;
                    fuente.CantidadQq = string.Empty;
                    fuente.ErrorCantidad = string.Empty;
                }

                Observacion = ObtenerNombreAnalisisSuelo();
                ResultadoFertilizacionMixta = null;
                LimpiarResultadosPresentacion();
                Mensaje = "Seleccione las fuentes orgánicas y calcule la fertilización mixta.";

                await CalculoAnalisisTemporalService.Instance.ReiniciarCalculoAsync(
                    TipoCalculoTemporal.FertilizacionMixta,
                    "Fertilización mixta reiniciada por el usuario."
                );

                LimpiarErrores();
                RefrescarComandos();
            }
            finally
            {
                suspendiendoCambiosTemporales = false;
            }
        }

        private static void NormalizarResultado(FertilizacionMixtaCalculoResponse response)
        {
            if (response.Fuentes != null)
            {
                foreach (var fuente in response.Fuentes)
                {
                    fuente.CantidadQq = Redondear2(fuente.CantidadQq ?? 0);
                }
            }

            if (response.Detalles != null)
            {
                foreach (var detalle in response.Detalles)
                {
                    detalle.Exportable = Redondear2(detalle.Exportable ?? 0);
                    detalle.AporteOrganico = Redondear2(detalle.AporteOrganico ?? 0);
                    detalle.Diferencia = Redondear2(detalle.Diferencia ?? 0);
                    detalle.Deficit = Redondear2(detalle.Deficit ?? 0);
                    detalle.Sobrante = Redondear2(detalle.Sobrante ?? 0);

                    if (detalle.Fuentes == null)
                        continue;

                    foreach (var fuente in detalle.Fuentes)
                    {
                        fuente.CantidadQq = Redondear2(fuente.CantidadQq ?? 0);
                        fuente.AportePorUnidad = Redondear2(fuente.AportePorUnidad ?? 0);
                        fuente.AporteTotal = Redondear2(fuente.AporteTotal ?? 0);
                    }
                }
            }
        }

        private void LimpiarErrores()
        {
            ErrorFuentes = string.Empty;
            ErrorElementos = string.Empty;

            foreach (var fuente in FuentesDisponibles)
            {
                fuente.ErrorCantidad = string.Empty;
            }
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeCalcular));
            OnPropertyChanged(nameof(TieneElementosExportables));
            OnPropertyChanged(nameof(TieneFuentesDisponibles));
            OnPropertyChanged(nameof(PuedeAplicarSugerencia));
            OnPropertyChanged(nameof(PuedeDeshacerSugerencia));

            CalcularCommand.ChangeCanExecute();
            ReiniciarCommand.ChangeCanExecute();
            aplicarSugerenciaCommand?.ChangeCanExecute();
            deshacerSugerenciaCommand?.ChangeCanExecute();
        }

        private static decimal ConvertirDecimal(string valor)
        {
            return TryParseDecimal(valor, out decimal resultado)
                ? Redondear2(resultado)
                : 0;
        }

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            resultado = 0;

            if (string.IsNullOrWhiteSpace(valor))
                return false;

            string valorNormalizado = valor
                .Trim()
                .Replace(" ", "")
                .Replace(",", ".");

            return decimal.TryParse(
                valorNormalizado,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out resultado
            );
        }

        private static decimal Redondear2(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }

    public class ElementoFertilizacionMixtaItemViewModel : BindableObject
    {
        private int? elementoQuimicosId;
        private string simboloElementoQuimico = string.Empty;
        private string nombreElementoQuimico = string.Empty;
        private decimal exportable;

        public int? ElementoQuimicosId
        {
            get => elementoQuimicosId;
            set
            {
                elementoQuimicosId = value;
                OnPropertyChanged(nameof(ElementoQuimicosId));
            }
        }

        public string SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set
            {
                simboloElementoQuimico = value ?? string.Empty;
                OnPropertyChanged(nameof(SimboloElementoQuimico));
                OnPropertyChanged(nameof(ElementoMostrar));
            }
        }

        public string NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set
            {
                nombreElementoQuimico = value ?? string.Empty;
                OnPropertyChanged(nameof(NombreElementoQuimico));
                OnPropertyChanged(nameof(ElementoMostrar));
            }
        }

        public decimal Exportable
        {
            get => exportable;
            set
            {
                exportable = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                OnPropertyChanged(nameof(Exportable));
                OnPropertyChanged(nameof(ExportableTexto));
            }
        }

        public string ElementoMostrar
        {
            get
            {
                string simbolo = (SimboloElementoQuimico ?? string.Empty).Trim();
                string nombre = (NombreElementoQuimico ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(nombre))
                    return simbolo;

                if (string.IsNullOrWhiteSpace(simbolo))
                    return nombre;

                return $"{nombre} ({simbolo})";
            }
        }

        public string ExportableTexto =>
            Exportable.ToString("0.00", CultureInfo.InvariantCulture);
    }

    public class FuenteFertilizacionMixtaItemViewModel : BindableObject
    {
        private int? fuenteNutrientesId;
        private string nombreFuente = string.Empty;
        private string descripcionFuente = string.Empty;
        private decimal? precioFuente;
        private string elementosTexto = string.Empty;
        private bool estaSeleccionada;
        private string cantidadQq = string.Empty;
        private string errorCantidad = string.Empty;

        public event EventHandler? CambioFormulario;

        public int? FuenteNutrientesId
        {
            get => fuenteNutrientesId;
            set
            {
                fuenteNutrientesId = value;
                OnPropertyChanged(nameof(FuenteNutrientesId));
            }
        }

        public string NombreFuente
        {
            get => nombreFuente;
            set
            {
                nombreFuente = value ?? string.Empty;
                OnPropertyChanged(nameof(NombreFuente));
            }
        }

        public string DescripcionFuente
        {
            get => descripcionFuente;
            set
            {
                descripcionFuente = value ?? string.Empty;
                OnPropertyChanged(nameof(DescripcionFuente));
            }
        }

            public decimal? PrecioFuente
        {
            get => precioFuente;
            set
            {
                precioFuente = value;
                OnPropertyChanged(nameof(PrecioFuente));
                OnPropertyChanged(nameof(TextoPrecioFuente));
            }
        }

        public string ElementosTexto
        {
            get => elementosTexto;
            set
            {
                elementosTexto = value ?? string.Empty;
                OnPropertyChanged(nameof(ElementosTexto));
            }
        }

        public bool EstaSeleccionada
        {
            get => estaSeleccionada;
            set
            {
                if (estaSeleccionada == value)
                    return;

                estaSeleccionada = value;
                OnPropertyChanged(nameof(EstaSeleccionada));

                if (!estaSeleccionada)
                {
                    CantidadQq = string.Empty;
                    ErrorCantidad = string.Empty;
                }

                NotificarCambioFormulario();
            }
        }

        public string CantidadQq
        {
            get => cantidadQq;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (!TextoDecimalPermitido(nuevoValor))
                    return;

                if (cantidadQq == nuevoValor)
                    return;

                cantidadQq = nuevoValor;
                OnPropertyChanged(nameof(CantidadQq));

                NotificarCambioFormulario();
            }
        }

        public string ErrorCantidad
        {
            get => errorCantidad;
            set
            {
                errorCantidad = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorCantidad));
                OnPropertyChanged(nameof(TieneErrorCantidad));
            }
        }

        public bool TieneErrorCantidad => !string.IsNullOrWhiteSpace(ErrorCantidad);

        public string TextoPrecioFuente =>
            $"Precio por QQ: C$ {(PrecioFuente ?? 0).ToString("N2", CultureInfo.InvariantCulture)}";

        private void NotificarCambioFormulario()
        {
            CambioFormulario?.Invoke(this, EventArgs.Empty);
        }

        private static bool TextoDecimalPermitido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            int cantidadSeparadores = 0;

            foreach (char caracter in texto)
            {
                if (char.IsDigit(caracter))
                    continue;

                if (caracter == '.' || caracter == ',')
                {
                    cantidadSeparadores++;

                    if (cantidadSeparadores > 1)
                        return false;

                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
