using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class EnmiendaCalcareaTabViewModel : BindableObject
    {
        // ===========================================================
        // ===== VIEWMODEL: EnmiendaCalcareaTabViewModel =============
        // ===========================================================
        // ViewModel usado dentro de MultiCalculoPage para calcular
        // la enmienda calcárea con datos CICE.
        //
        // Regla actual:
        // - El nombre del análisis NO se escribe aquí.
        // - Se toma de RequestGuardarAnalisis.IdentificadorAnalisisSuelo.
        // - pH, AT, Ca CICE y Mg CICE se precargan si vienen del
        //   formulario inicial.
        // - Si vienen vacíos o 0, el usuario puede escribirlos aquí.
        // - Si no escribe datos CICE, se envían como 0.
        // ===========================================================

        private readonly EnmiendaCalcareaApiService enmiendaCalcareaApiService = new();

        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private ParametroEnmiendaCalcareaResponse? enmiendaSeleccionada;
        private EnmiendaCalcareaCalcularResponse? resultadoEnmienda;

        private string nombreAnalisis = "Enmienda calcárea";
        private string ph = string.Empty;
        private string ca = string.Empty;
        private string mg = string.Empty;
        private string k = string.Empty;
        private string acidezTotal = string.Empty;
        private string totalPlantas = string.Empty;
        private string totalAplicaciones = "3";
        private string mensaje = "Ingrese los datos CICE y seleccione el tipo de cal.";
        private string errorFormulario = string.Empty;

        private int? terrenoId;
        private bool isBusy;
        private bool enmiendasCargadas;

        public EnmiendaCalcareaTabViewModel()
        {
            EnmiendasCalcareas = new ObservableCollection<ParametroEnmiendaCalcareaResponse>();

            CalcularCommand = new Command(
                async () => await CalcularAsync(),
                () => PuedeCalcular
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

                PrecargarDatosDesdeAnalisis();
            }
        }

        public int? TerrenoId
        {
            get => terrenoId;
            set
            {
                terrenoId = value;
                OnPropertyChanged(nameof(TerrenoId));
            }
        }

        public ObservableCollection<ParametroEnmiendaCalcareaResponse> EnmiendasCalcareas { get; }

        public ParametroEnmiendaCalcareaResponse? EnmiendaSeleccionada
        {
            get => enmiendaSeleccionada;
            set
            {
                enmiendaSeleccionada = value;
                OnPropertyChanged(nameof(EnmiendaSeleccionada));
                OnPropertyChanged(nameof(TieneEnmiendaSeleccionada));
                OnPropertyChanged(nameof(TextoEnmiendaSeleccionada));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public bool TieneEnmiendaSeleccionada => EnmiendaSeleccionada != null;

        public string TextoEnmiendaSeleccionada
        {
            get
            {
                if (EnmiendaSeleccionada == null)
                    return string.Empty;

                return $"Seleccionado: {EnmiendaSeleccionada.NombreMostrar}";
            }
        }

        public EnmiendaCalcareaCalcularResponse? ResultadoEnmienda
        {
            get => resultadoEnmienda;
            set
            {
                resultadoEnmienda = value;
                OnPropertyChanged(nameof(ResultadoEnmienda));
                OnPropertyChanged(nameof(TieneResultado));
            }
        }

        public bool TieneResultado => ResultadoEnmienda != null;

        // ===========================================================
        // Nombre del análisis usado para la enmienda.
        // Ya NO se escribe en esta pestaña.
        // Se toma del identificador capturado una sola vez en
        // NuevoAnalisisFormPage.
        // ===========================================================
        public string NombreAnalisis
        {
            get => nombreAnalisis;
            set
            {
                nombreAnalisis = value ?? string.Empty;
                OnPropertyChanged(nameof(NombreAnalisis));
                OnPropertyChanged(nameof(NombreAnalisisTexto));
                RefrescarComandos();
            }
        }

        public string NombreAnalisisTexto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NombreAnalisis))
                    return "Enmienda calcárea";

                return NombreAnalisis.Trim();
            }
        }

        public string Ph
        {
            get => ph;
            set
            {
                ph = value ?? string.Empty;
                OnPropertyChanged(nameof(Ph));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string Ca
        {
            get => ca;
            set
            {
                ca = value ?? string.Empty;
                OnPropertyChanged(nameof(Ca));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string Mg
        {
            get => mg;
            set
            {
                mg = value ?? string.Empty;
                OnPropertyChanged(nameof(Mg));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string K
        {
            get => k;
            set
            {
                k = value ?? string.Empty;
                OnPropertyChanged(nameof(K));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string AcidezTotal
        {
            get => acidezTotal;
            set
            {
                acidezTotal = value ?? string.Empty;
                OnPropertyChanged(nameof(AcidezTotal));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string TotalPlantas
        {
            get => totalPlantas;
            set
            {
                totalPlantas = value ?? string.Empty;
                OnPropertyChanged(nameof(TotalPlantas));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string TotalAplicaciones
        {
            get => totalAplicaciones;
            set
            {
                totalAplicaciones = value ?? string.Empty;
                OnPropertyChanged(nameof(TotalAplicaciones));
                MarcarEnmiendaPendienteSiTieneResultado();
                RefrescarComandos();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        public string ErrorFormulario
        {
            get => errorFormulario;
            set
            {
                errorFormulario = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorFormulario));
                OnPropertyChanged(nameof(TieneErrorFormulario));
            }
        }

        public bool TieneErrorFormulario => !string.IsNullOrWhiteSpace(ErrorFormulario);

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

        public bool PuedeCalcular =>
            !IsBusy &&
            EnmiendaSeleccionada != null;

        public Command CalcularCommand { get; }

        // ===========================================================
        // ===================== INICIALIZACIÓN ======================
        // ===========================================================

        public void Inicializar(
            AnalisisSueloCalculoDataResponse? resultado,
            AnalisisSueloGuardarCalculoRequest? requestGuardar,
            int? idTerreno,
            int? plantas)
        {
            ResultadoCalculo = resultado;
            RequestGuardarAnalisis = requestGuardar;
            TerrenoId = idTerreno;

            if (string.IsNullOrWhiteSpace(TotalPlantas) && plantas.HasValue && plantas.Value > 0)
                TotalPlantas = plantas.Value.ToString(CultureInfo.InvariantCulture);

            if (!enmiendasCargadas)
                _ = CargarEnmiendasCalcareasAsync();
        }

        private void PrecargarDatosDesdeAnalisis()
        {
            string identificador = RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ?? string.Empty;

            NombreAnalisis = string.IsNullOrWhiteSpace(identificador)
                ? "Enmienda calcárea"
                : identificador.Trim();

            decimal phAnalisis =
                RequestGuardarAnalisis?.Ph ??
                ResultadoCalculo?.Ph ??
                0;

            decimal acidezAnalisis =
                RequestGuardarAnalisis?.AcidezTotal ??
                ResultadoCalculo?.AcidezTotal ??
                0;

            decimal calcioCice =
                RequestGuardarAnalisis?.CalcioCice ?? 0;

            decimal magnesioCice =
                RequestGuardarAnalisis?.MagnesioCice ?? 0;

            decimal potasioCice =
                RequestGuardarAnalisis?.PotasioCice ?? 0;

            if (string.IsNullOrWhiteSpace(Ph) && phAnalisis > 0)
                Ph = phAnalisis.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(AcidezTotal) && acidezAnalisis > 0)
                AcidezTotal = acidezAnalisis.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(Ca) && calcioCice > 0)
                Ca = calcioCice.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(Mg) && magnesioCice > 0)
                Mg = magnesioCice.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(K) && potasioCice > 0)
                K = potasioCice.ToString(CultureInfo.InvariantCulture);

            OnPropertyChanged(nameof(NombreAnalisisTexto));
        }

        // ===========================================================
        // =============== REINICIO PARA NUEVO CÁLCULO ===============
        // ===========================================================

        public void ReiniciarParaNuevoCalculo()
        {
            ResultadoCalculo = null;
            RequestGuardarAnalisis = null;

            EnmiendaSeleccionada = null;
            ResultadoEnmienda = null;

            NombreAnalisis = "Enmienda calcárea";
            Ph = string.Empty;
            Ca = string.Empty;
            Mg = string.Empty;
            K = string.Empty;
            AcidezTotal = string.Empty;
            TotalPlantas = string.Empty;
            TotalAplicaciones = "3";

            Mensaje = "Ingrese los datos CICE y seleccione el tipo de cal.";
            ErrorFormulario = string.Empty;

            TerrenoId = null;

            EnmiendasCalcareas.Clear();
            enmiendasCargadas = false;

            IsBusy = false;

            RefrescarComandos();
        }

        // ===========================================================
        // ================= CARGA DE TIPOS DE CAL ===================
        // ===========================================================

        private async Task CargarEnmiendasCalcareasAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ErrorFormulario = string.Empty;
                Mensaje = "Cargando tipos de cal...";

                EnmiendasCalcareas.Clear();

                ObservableCollection<ParametroEnmiendaCalcareaResponse> lista =
                    await enmiendaCalcareaApiService.GetEnmiendasCalcareasAsync();

                foreach (ParametroEnmiendaCalcareaResponse item in lista)
                {
                    if (item == null)
                        continue;

                    if (item.FuenteNutrientesId == null || item.FuenteNutrientesId <= 0)
                        continue;

                    EnmiendasCalcareas.Add(item);
                }

                enmiendasCargadas = true;

                if (EnmiendasCalcareas.Count > 0 && EnmiendaSeleccionada == null)
                    EnmiendaSeleccionada = EnmiendasCalcareas.FirstOrDefault();

                Mensaje = EnmiendasCalcareas.Count > 0
                    ? "Ingrese o revise los datos CICE y calcule la enmienda."
                    : "No se encontraron tipos de cal disponibles.";
            }
            catch (Exception ex)
            {
                ErrorFormulario = ex.Message;
                Mensaje = "No se pudieron cargar los tipos de cal.";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        // ===========================================================
        // ===================== CALCULAR ENMIENDA ===================
        // ===========================================================

        private async Task CalcularAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarFormulario(
                    out decimal phValidado,
                    out decimal caValidado,
                    out decimal mgValidado,
                    out decimal kValidado,
                    out decimal acidezValidada,
                    out int plantasValidadas,
                    out int aplicacionesValidadas))
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorFormulario = string.Empty;
                ResultadoEnmienda = null;
                Mensaje = "Calculando enmienda calcárea...";

                var request = new EnmiendaCalcareaCalcularRequest
                {
                    NombreAnalisis = NombreAnalisisTexto,
                    FuenteNutrientesId = EnmiendaSeleccionada!.FuenteNutrientesId!.Value,
                    Ph = phValidado,
                    Ca = caValidado,
                    Mg = mgValidado,
                    K = kValidado,
                    AcidezTotal = acidezValidada,
                    TerrenoId = TerrenoId!.Value,
                    TotalPlantas = plantasValidadas,
                    TotalAplicaciones = aplicacionesValidadas
                };

                ResultadoEnmienda =
                    await enmiendaCalcareaApiService.CalcularEnmiendaCalcareaAsync(request);

                if (ResultadoEnmienda == null)
                {
                    await CalculoAnalisisTemporalService.Instance.MarcarPendienteRecalculoAsync(
                        TipoCalculoTemporal.EnmiendaCalcarea,
                        "No se pudo completar la enmienda calcárea. Debe recalcular.",
                        true
                    );

                    ErrorFormulario = "La API no devolvió resultado o no se pudo completar el cálculo.";
                    Mensaje = "No se pudo obtener el resultado de la enmienda.";
                    return;
                }

                await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                    TipoCalculoTemporal.EnmiendaCalcarea,
                    request,
                    ResultadoEnmienda,
                    "Enmienda calcárea calculada correctamente."
                );

                Mensaje = "Enmienda calcárea calculada correctamente.";
            }
            catch (Exception ex)
            {
                ErrorFormulario = ex.Message;
                Mensaje = "No se pudo calcular la enmienda calcárea.";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        // ===========================================================
        // ======================== VALIDACIONES =====================
        // ===========================================================

        private bool ValidarFormulario(
            out decimal phValidado,
            out decimal caValidado,
            out decimal mgValidado,
            out decimal kValidado,
            out decimal acidezValidada,
            out int plantasValidadas,
            out int aplicacionesValidadas)
        {
            phValidado = 0;
            caValidado = 0;
            mgValidado = 0;
            kValidado = 0;
            acidezValidada = 0;
            plantasValidadas = 0;
            aplicacionesValidadas = 0;

            ErrorFormulario = string.Empty;

            if (TerrenoId == null || TerrenoId <= 0)
            {
                ErrorFormulario = "No se encontró el terreno seleccionado.";
                return false;
            }

            if (EnmiendaSeleccionada?.FuenteNutrientesId == null || EnmiendaSeleccionada.FuenteNutrientesId <= 0)
            {
                ErrorFormulario = "Seleccione el tipo de cal.";
                return false;
            }

            if (!TryParseDecimalOpcional(Ph, out phValidado))
            {
                ErrorFormulario = "Ingrese un valor numérico para pH.";
                return false;
            }

            if (phValidado < 0 || phValidado > 14)
            {
                ErrorFormulario = "El pH debe estar entre 0 y 14.";
                return false;
            }

            if (!TryParseDecimalOpcional(Ca, out caValidado) || caValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para Ca.";
                return false;
            }

            if (!TryParseDecimalOpcional(Mg, out mgValidado) || mgValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para Mg.";
                return false;
            }

            if (!TryParseDecimalOpcional(K, out kValidado) || kValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para K.";
                return false;
            }

            if (!TryParseDecimalOpcional(AcidezTotal, out acidezValidada) || acidezValidada < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para AT / Acidez total.";
                return false;
            }

            if (!int.TryParse(TotalPlantas, out plantasValidadas) || plantasValidadas <= 0)
            {
                ErrorFormulario = "La cantidad de plantas debe ser mayor a cero.";
                return false;
            }

            if (!int.TryParse(TotalAplicaciones, out aplicacionesValidadas) || aplicacionesValidadas <= 0)
            {
                ErrorFormulario = "El total de aplicaciones debe ser mayor a cero.";
                return false;
            }

            if (aplicacionesValidadas > 4)
            {
                ErrorFormulario = "El total de aplicaciones no puede ser mayor a 4.";
                return false;
            }

            return true;
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================

        private static bool TryParseDecimalOpcional(string valor, out decimal resultado)
        {
            resultado = 0;

            if (string.IsNullOrWhiteSpace(valor))
                return true;

            string normalizado = valor.Trim().Replace(",", ".");

            return decimal.TryParse(
                normalizado,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out resultado
            );
        }

        private void MarcarEnmiendaPendienteSiTieneResultado()
        {
            if (ResultadoEnmienda == null)
                return;

            _ = CalculoAnalisisTemporalService.Instance.MarcarPendienteRecalculoAsync(
                TipoCalculoTemporal.EnmiendaCalcarea,
                "La enmienda calcárea cambió. Debe recalcular para actualizar el resultado.",
                true
            );

            ResultadoEnmienda = null;
            Mensaje = "Hay cambios pendientes. Presione Calcular para actualizar la enmienda.";
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeCalcular));
            CalcularCommand.ChangeCanExecute();
        }
    }
}