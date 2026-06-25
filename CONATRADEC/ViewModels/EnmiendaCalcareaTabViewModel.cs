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
        // la enmienda calcárea con datos CICE:
        // pH, Ca, Mg, K y AT / Acidez total.
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

        public string NombreAnalisis
        {
            get => nombreAnalisis;
            set
            {
                nombreAnalisis = value ?? string.Empty;
                OnPropertyChanged(nameof(NombreAnalisis));
                RefrescarComandos();
            }
        }

        public string Ph
        {
            get => ph;
            set
            {
                ph = value ?? string.Empty;
                OnPropertyChanged(nameof(Ph));
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
        // Este método se ejecuta cuando MultiCalculoPage inicializa
        // la pestaña de enmienda calcárea.
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

        // ===========================================================
        // =============== REINICIO PARA NUEVO CÁLCULO ===============
        // ===========================================================
        // Este método limpia la pestaña cuando el usuario sale de
        // MultiCalculoPage y vuelve a entrar desde ResultadoAnalisis.
        //
        // Importante:
        // NO se llama al cambiar entre pestañas internas.
        // Por eso no borra datos al moverse entre Balance, Enmienda
        // y Fertilización Mixta.
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
                    ? "Ingrese los datos CICE y calcule la enmienda."
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
                    NombreAnalisis = string.IsNullOrWhiteSpace(NombreAnalisis)
                        ? "Enmienda calcárea"
                        : NombreAnalisis.Trim(),

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
                    ErrorFormulario = "La API no devolvió resultado o no se pudo completar el cálculo.";
                    Mensaje = "No se pudo obtener el resultado de la enmienda.";
                    return;
                }

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

            if (!TryParseDecimal(Ph, out phValidado) || phValidado <= 0 || phValidado > 14)
            {
                ErrorFormulario = "Ingrese un pH válido entre 0 y 14.";
                return false;
            }

            if (!TryParseDecimal(Ca, out caValidado) || caValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para Ca.";
                return false;
            }

            if (!TryParseDecimal(Mg, out mgValidado) || mgValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para Mg.";
                return false;
            }

            if (!TryParseDecimal(K, out kValidado) || kValidado < 0)
            {
                ErrorFormulario = "Ingrese un valor válido para K.";
                return false;
            }

            if (!TryParseDecimal(AcidezTotal, out acidezValidada) || acidezValidada <= 0)
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

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            resultado = 0;

            if (string.IsNullOrWhiteSpace(valor))
                return false;

            string normalizado = valor.Trim().Replace(",", ".");

            return decimal.TryParse(
                normalizado,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out resultado
            );
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeCalcular));
            CalcularCommand.ChangeCanExecute();
        }
    }
}