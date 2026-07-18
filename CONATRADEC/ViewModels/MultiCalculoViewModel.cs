using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class MultiCalculoViewModel : GlobalService, IQueryAttributable
    {
        private const string TabBalanceFormula = "BALANCE_FORMULA";
        private const string TabEnmiendaCalcarea = "ENMIENDA_CALCAREA";
        private const string TabFertilizacionMixta = "FERTILIZACION_MIXTA";

        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private bool mostrarBalanceFormula;
        private bool mostrarEnmiendaCalcarea;
        private bool mostrarFertilizacionMixta;

        private string tabSeleccionada = string.Empty;
        private string mensaje = string.Empty;

        private int? terrenoId;
        private int? cantidadPlantas;
        private bool fertilizacionMixtaInicializada;

        public MultiCalculoViewModel()
        {
            BalanceFormula = new BalanceFormulaViewModel();
            EnmiendaCalcarea = new EnmiendaCalcareaTabViewModel();
            FertilizacionMixta = new FertilizacionMixtaTabViewModel();

            BalanceFormula.ComplementoFertilizacionMixtaCambiado +=
                BalanceFormula_ComplementoFertilizacionMixtaCambiado;

            SeleccionarBalanceCommand = new Command(SeleccionarBalance);
            SeleccionarEnmiendaCommand = new Command(SeleccionarEnmienda);
            SeleccionarFertilizacionCommand = new Command(SeleccionarFertilizacion);

            VolverCommand = new Command(
                async () => await VolverAsync()
            );

            FinalizarCommand = new Command(
                async () => await FinalizarAsync()
            );
        }

        public BalanceFormulaViewModel BalanceFormula { get; }

        public EnmiendaCalcareaTabViewModel EnmiendaCalcarea { get; }

        public FertilizacionMixtaTabViewModel FertilizacionMixta { get; }

        public AnalisisSueloCalculoDataResponse? ResultadoCalculo
        {
            get => resultadoCalculo;
            set
            {
                resultadoCalculo = value;
                OnPropertyChanged(nameof(ResultadoCalculo));
                OnPropertyChanged(nameof(TipoCultivo));
                OnPropertyChanged(nameof(TipoAnalisisSuelo));
                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(CantidadQuintalesOro));
            }
        }

        public AnalisisSueloGuardarCalculoRequest? RequestGuardarAnalisis
        {
            get => requestGuardarAnalisis;
            set
            {
                requestGuardarAnalisis = value;
                OnPropertyChanged(nameof(RequestGuardarAnalisis));

                OnPropertyChanged(nameof(NombreAnalisisSuelo));
                OnPropertyChanged(nameof(TieneNombreAnalisisSuelo));

                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
                OnPropertyChanged(nameof(CalcioCice));
                OnPropertyChanged(nameof(MagnesioCice));
                OnPropertyChanged(nameof(PotasioCice));

                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(CantidadQuintalesOro));
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

        public int? CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = value;
                OnPropertyChanged(nameof(CantidadPlantas));
                OnPropertyChanged(nameof(CantidadPlantasTexto));
            }
        }

        public bool MostrarBalanceFormula
        {
            get => mostrarBalanceFormula;
            set
            {
                mostrarBalanceFormula = value;
                OnPropertyChanged(nameof(MostrarBalanceFormula));
            }
        }

        public bool MostrarEnmiendaCalcarea
        {
            get => mostrarEnmiendaCalcarea;
            set
            {
                mostrarEnmiendaCalcarea = value;
                OnPropertyChanged(nameof(MostrarEnmiendaCalcarea));
            }
        }

        public bool MostrarFertilizacionMixta
        {
            get => mostrarFertilizacionMixta;
            set
            {
                mostrarFertilizacionMixta = value;
                OnPropertyChanged(nameof(MostrarFertilizacionMixta));
            }
        }

        public string TabSeleccionada
        {
            get => tabSeleccionada;
            set
            {
                tabSeleccionada = value ?? string.Empty;

                OnPropertyChanged(nameof(TabSeleccionada));
                OnPropertyChanged(nameof(EsBalanceSeleccionado));
                OnPropertyChanged(nameof(EsEnmiendaSeleccionada));
                OnPropertyChanged(nameof(EsFertilizacionSeleccionada));
                OnPropertyChanged(nameof(NombreTabActual));
                OnPropertyChanged(nameof(TextoEncabezado));
            }
        }

        public bool EsBalanceSeleccionado =>
            string.Equals(TabSeleccionada, TabBalanceFormula, StringComparison.OrdinalIgnoreCase);

        public bool EsEnmiendaSeleccionada =>
            string.Equals(TabSeleccionada, TabEnmiendaCalcarea, StringComparison.OrdinalIgnoreCase);

        public bool EsFertilizacionSeleccionada =>
            string.Equals(TabSeleccionada, TabFertilizacionMixta, StringComparison.OrdinalIgnoreCase);

        public string NombreTabActual
        {
            get
            {
                if (EsBalanceSeleccionado)
                    return "Balance";

                if (EsEnmiendaSeleccionada)
                    return "Enmienda";

                if (EsFertilizacionSeleccionada)
                    return "Mixta";

                return "Sin selección";
            }
        }

        public string TextoEncabezado
        {
            get
            {
                string nombreAnalisis = NombreAnalisisSuelo;

                if (EsBalanceSeleccionado)
                    return $"Está trabajando en el balance de fórmula del análisis {nombreAnalisis}. Puede cambiar de pestaña sin perder los datos.";

                if (EsEnmiendaSeleccionada)
                    return $"Está trabajando en la enmienda calcárea del análisis {nombreAnalisis}. Puede volver al balance sin perder los datos.";

                if (EsFertilizacionSeleccionada)
                    return $"Está trabajando en fertilización mixta del análisis {nombreAnalisis}. Puede cambiar de pestaña sin perder los datos.";

                return "Cambie entre los cálculos seleccionados desde las pestañas inferiores.";
            }
        }

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

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public string NombreAnalisisSuelo
        {
            get
            {
                string nombre = RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nombre))
                    return "No disponible";

                return nombre.Trim();
            }
        }

        public bool TieneNombreAnalisisSuelo =>
            !string.IsNullOrWhiteSpace(RequestGuardarAnalisis?.IdentificadorAnalisisSuelo);

        public string TipoCultivo =>
            ResultadoCalculo?.TipoCultivo ?? "No disponible";

        public string TipoAnalisisSuelo =>
            ResultadoCalculo?.TipoAnalisisSuelo ?? "No disponible";

        public decimal Ph =>
            RequestGuardarAnalisis?.Ph ??
            ResultadoCalculo?.Ph ??
            0;

        public decimal AcidezTotal =>
            RequestGuardarAnalisis?.AcidezTotal ??
            ResultadoCalculo?.AcidezTotal ??
            0;

        public decimal CalcioCice =>
            RequestGuardarAnalisis?.CalcioCice ?? 0;

        public decimal MagnesioCice =>
            RequestGuardarAnalisis?.MagnesioCice ?? 0;

        public decimal PotasioCice =>
            RequestGuardarAnalisis?.PotasioCice ?? 0;

        public decimal TamanoFinca =>
            RequestGuardarAnalisis?.TamanoFinca ??
            ResultadoCalculo?.TamanoFinca ??
            0;

        public decimal CantidadQuintalesOro =>
            RequestGuardarAnalisis?.CantidadQuintalesOro ??
            ResultadoCalculo?.CantidadQuintalesOro ??
            0;

        public string CantidadPlantasTexto =>
            CantidadPlantas.HasValue && CantidadPlantas.Value > 0
                ? $"{CantidadPlantas.Value:N0} plantas"
                : "No disponible";

        public Command SeleccionarBalanceCommand { get; }

        public Command SeleccionarEnmiendaCommand { get; }

        public Command SeleccionarFertilizacionCommand { get; }

        public Command VolverCommand { get; }

        public Command FinalizarCommand { get; }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Limpiar();

            if (query.ContainsKey("resultadoCalculo"))
                ResultadoCalculo = query["resultadoCalculo"] as AnalisisSueloCalculoDataResponse;

            if (query.ContainsKey("requestGuardarAnalisis"))
                RequestGuardarAnalisis = query["requestGuardarAnalisis"] as AnalisisSueloGuardarCalculoRequest;

            if (query.ContainsKey("terrenoId"))
            {
                if (int.TryParse(query["terrenoId"]?.ToString(), out int idTerreno))
                    TerrenoId = idTerreno;
            }

            if (TerrenoId == null &&
                RequestGuardarAnalisis?.TerrenoId != null &&
                RequestGuardarAnalisis.TerrenoId > 0)
            {
                TerrenoId = RequestGuardarAnalisis.TerrenoId;
            }

            if (query.ContainsKey("cantidadPlantas"))
            {
                if (int.TryParse(query["cantidadPlantas"]?.ToString(), out int plantas))
                    CantidadPlantas = plantas;
            }

            MostrarBalanceFormula = ObtenerBoolQuery(query, "calcularBalanceFormula");
            MostrarEnmiendaCalcarea = ObtenerBoolQuery(query, "calcularEnmiendaCalcarea");
            MostrarFertilizacionMixta = ObtenerBoolQuery(query, "calcularFertilizacionMixta");

            // =======================================================
            // ESTADO TEMPORAL GENERAL DEL ANÁLISIS
            // =======================================================
            // Aquí se guarda el resultado base del análisis de suelo.
            // Si es el mismo análisis, conserva los cálculos anteriores.
            // Si es un nuevo análisis, reemplaza todo el temporal.
            // =======================================================
            await CalculoAnalisisTemporalService.Instance.IniciarNuevoCalculoAsync(
                ResultadoCalculo,
                RequestGuardarAnalisis
            );

            InicializarTabs();
            SeleccionarPrimeraPestanaDisponible();

            Mensaje = "Seleccione una pestaña inferior para continuar con los cálculos complementarios.";
        }

        private void InicializarTabs()
        {
            var parametros = CrearParametrosBase();

            if (MostrarBalanceFormula)
                BalanceFormula.ApplyQueryAttributes(parametros);

            if (MostrarEnmiendaCalcarea)
            {
                EnmiendaCalcarea.Inicializar(
                    ResultadoCalculo,
                    RequestGuardarAnalisis,
                    TerrenoId,
                    CantidadPlantas
                );
            }

            if (MostrarFertilizacionMixta)
                InicializarFertilizacionMixtaSiEsNecesario();
        }

        private void InicializarFertilizacionMixtaSiEsNecesario()
        {
            if (fertilizacionMixtaInicializada)
                return;

            FertilizacionMixta.Inicializar(ResultadoCalculo, RequestGuardarAnalisis);
            fertilizacionMixtaInicializada = true;
        }

        private async void BalanceFormula_ComplementoFertilizacionMixtaCambiado(
            object? sender,
            BalanceFertilizacionMixtaChangedEventArgs e)
        {
            if (e.Activado)
            {
                MostrarFertilizacionMixta = true;
                InicializarFertilizacionMixtaSiEsNecesario();
            }

            if (!fertilizacionMixtaInicializada)
                return;

            await FertilizacionMixta.ConfigurarComplementoBalanceAsync(
                e.Activado,
                e.Contexto
            );

            if (e.Activado && e.Contexto == null)
            {
                Mensaje = "El complemento está activo. Complete el balance y luego continúe en la pestaña Mixta.";
            }
            else if (e.Activado)
            {
                Mensaje = "Balance vinculado con fertilización mixta. Complete el cálculo obligatorio en la pestaña Mixta.";
            }
        }

        private void Limpiar()
        {
            ResultadoCalculo = null;
            RequestGuardarAnalisis = null;

            MostrarBalanceFormula = false;
            MostrarEnmiendaCalcarea = false;
            MostrarFertilizacionMixta = false;

            TabSeleccionada = string.Empty;
            Mensaje = string.Empty;

            TerrenoId = null;
            CantidadPlantas = null;
            fertilizacionMixtaInicializada = false;

            // =======================================================
            // LIMPIEZA DE ENMIENDA CALCÁREA
            // =======================================================
            // Este método se ejecuta cuando MultiCalculoPage recibe
            // nuevamente parámetros desde ResultadoAnalisisSueloPage.
            //
            // Por eso limpia la pestaña de Enmienda cuando el usuario:
            // Enmienda -> Volver -> Resultado -> Entrar otra vez.
            //
            // No se ejecuta al cambiar entre pestañas internas,
            // entonces no borra datos al moverse entre:
            // Balance -> Enmienda -> Mixta.
            // =======================================================
            EnmiendaCalcarea.ReiniciarParaNuevoCalculo();
        }

        private static bool ObtenerBoolQuery(IDictionary<string, object> query, string key)
        {
            if (!query.ContainsKey(key))
                return false;

            object? valor = query[key];

            if (valor is bool boolValor)
                return boolValor;

            if (bool.TryParse(valor?.ToString(), out bool resultado))
                return resultado;

            return false;
        }

        private void SeleccionarPrimeraPestanaDisponible()
        {
            if (MostrarBalanceFormula)
            {
                TabSeleccionada = TabBalanceFormula;
                return;
            }

            if (MostrarEnmiendaCalcarea)
            {
                TabSeleccionada = TabEnmiendaCalcarea;
                return;
            }

            if (MostrarFertilizacionMixta)
            {
                TabSeleccionada = TabFertilizacionMixta;
                return;
            }

            Mensaje = "No se recibió ningún cálculo seleccionado.";
        }

        private void SeleccionarBalance()
        {
            if (!MostrarBalanceFormula)
                return;

            TabSeleccionada = TabBalanceFormula;
        }

        private void SeleccionarEnmienda()
        {
            if (!MostrarEnmiendaCalcarea)
                return;

            TabSeleccionada = TabEnmiendaCalcarea;
        }

        private void SeleccionarFertilizacion()
        {
            if (!MostrarFertilizacionMixta)
                return;

            TabSeleccionada = TabFertilizacionMixta;
        }

        private async Task VolverAsync()
        {
            var parametros = CrearParametrosBase();

            await GoToAsyncParameters("//ResultadoAnalisisSueloPage", parametros);
        }

        private async Task FinalizarAsync()
        {
            if (BalanceFormula.ComplementarConFertilizacionMixta &&
                !FertilizacionMixta.TieneComplementoCompleto)
            {
                MostrarFertilizacionMixta = true;
                TabSeleccionada = TabFertilizacionMixta;

                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Fertilización mixta pendiente",
                        "Activó el complemento del balance. Debe calcular la fertilización mixta y el balance ajustado antes de finalizar.",
                        "Aceptar"
                    );
                }

                return;
            }

            await GoToAsyncParameters("//MainPage");
        }

        private Dictionary<string, object> CrearParametrosBase()
        {
            var parametros = new Dictionary<string, object>();

            if (ResultadoCalculo != null)
                parametros.Add("resultadoCalculo", ResultadoCalculo);

            if (RequestGuardarAnalisis != null)
                parametros.Add("requestGuardarAnalisis", RequestGuardarAnalisis);

            if (TerrenoId != null && TerrenoId > 0)
                parametros.Add("terrenoId", TerrenoId.Value);

            if (CantidadPlantas != null && CantidadPlantas > 0)
                parametros.Add("cantidadPlantas", CantidadPlantas.Value);

            parametros.Add("calcularBalanceFormula", MostrarBalanceFormula);
            parametros.Add("calcularEnmiendaCalcarea", MostrarEnmiendaCalcarea);
            parametros.Add("calcularFertilizacionMixta", MostrarFertilizacionMixta);

            return parametros;
        }
    }
}
