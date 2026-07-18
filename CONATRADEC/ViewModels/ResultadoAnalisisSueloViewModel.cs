using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class ResultadoAnalisisSueloViewModel : GlobalService, IQueryAttributable
    {
        private AnalisisSueloCalculoDataResponse? resultado;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private string tituloResultado = "Resultado del análisis de suelo";
        private string recomendacionGeneral = string.Empty;
        private string sugerenciaSiguienteCalculo = string.Empty;
        private string mensajeSeleccionCalculo = string.Empty;

        private bool tieneObservaciones;
        private bool tieneElementos;
        private bool phMuyAcido;

        private bool calcularBalanceFormula;
        private bool calcularEnmiendaCalcarea;
        private bool calcularFertilizacionMixta;
        private int? cantidadPlantas;

        private bool esModoEdicion;
        private int? analisisSueloCalculoIdEdicion;

        public ResultadoAnalisisSueloViewModel()
        {
            Elementos = new ObservableCollection<ElementoResultadoCalculoResponse>();
            Observaciones = new ObservableCollection<string>();

            ProcesarSeleccionCommand = new Command(
                async () => await ProcesarSeleccionAsync());

            VolverCommand = new Command(
                async () => await VolverAsync());
        }

        public AnalisisSueloCalculoDataResponse? Resultado
        {
            get => resultado;
            set
            {
                resultado = value;
                OnPropertyChanged(nameof(Resultado));
                OnPropertyChanged(nameof(TipoCultivo));
                OnPropertyChanged(nameof(TipoAnalisisSuelo));
                OnPropertyChanged(nameof(CantidadQuintalesOro));
                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
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

        public int? CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = value;
                OnPropertyChanged(nameof(CantidadPlantas));
            }
        }

        public bool EsModoEdicion
        {
            get => esModoEdicion;
            private set
            {
                esModoEdicion = value;
                OnPropertyChanged(nameof(EsModoEdicion));
                OnPropertyChanged(nameof(TextoBotonContinuar));
            }
        }

        public int? AnalisisSueloCalculoIdEdicion
        {
            get => analisisSueloCalculoIdEdicion;
            private set
            {
                analisisSueloCalculoIdEdicion = value;
                OnPropertyChanged(nameof(AnalisisSueloCalculoIdEdicion));
            }
        }

        public string TextoBotonContinuar =>
            EsModoEdicion ? "Continuar edición" : "Continuar";

        public string TituloResultado
        {
            get => tituloResultado;
            set
            {
                tituloResultado = value;
                OnPropertyChanged(nameof(TituloResultado));
            }
        }

        public string RecomendacionGeneral
        {
            get => recomendacionGeneral;
            set
            {
                recomendacionGeneral = value;
                OnPropertyChanged(nameof(RecomendacionGeneral));
            }
        }

        public string SugerenciaSiguienteCalculo
        {
            get => sugerenciaSiguienteCalculo;
            set
            {
                sugerenciaSiguienteCalculo = value;
                OnPropertyChanged(nameof(SugerenciaSiguienteCalculo));
            }
        }

        public string MensajeSeleccionCalculo
        {
            get => mensajeSeleccionCalculo;
            set
            {
                mensajeSeleccionCalculo = value;
                OnPropertyChanged(nameof(MensajeSeleccionCalculo));
                OnPropertyChanged(nameof(TieneMensajeSeleccionCalculo));
            }
        }

        public bool TieneMensajeSeleccionCalculo =>
            !string.IsNullOrWhiteSpace(MensajeSeleccionCalculo);

        public bool TieneObservaciones
        {
            get => tieneObservaciones;
            set
            {
                tieneObservaciones = value;
                OnPropertyChanged(nameof(TieneObservaciones));
            }
        }

        public bool TieneElementos
        {
            get => tieneElementos;
            set
            {
                tieneElementos = value;
                OnPropertyChanged(nameof(TieneElementos));
            }
        }

        public bool PhMuyAcido
        {
            get => phMuyAcido;
            set
            {
                phMuyAcido = value;
                OnPropertyChanged(nameof(PhMuyAcido));
            }
        }

        public bool CalcularBalanceFormula
        {
            get => calcularBalanceFormula;
            set
            {
                calcularBalanceFormula = value;
                OnPropertyChanged(nameof(CalcularBalanceFormula));
                NotificarSeleccion();
            }
        }

        public bool CalcularEnmiendaCalcarea
        {
            get => calcularEnmiendaCalcarea;
            set
            {
                calcularEnmiendaCalcarea = value;
                OnPropertyChanged(nameof(CalcularEnmiendaCalcarea));
                NotificarSeleccion();
            }
        }

        public bool CalcularFertilizacionMixta
        {
            get => calcularFertilizacionMixta;
            set
            {
                calcularFertilizacionMixta = value;
                OnPropertyChanged(nameof(CalcularFertilizacionMixta));
                NotificarSeleccion();
            }
        }

        public bool TieneSeleccionCalculo =>
            CalcularBalanceFormula ||
            CalcularEnmiendaCalcarea ||
            CalcularFertilizacionMixta;

        public string TextoSeleccionCalculo
        {
            get
            {
                List<string> seleccionados = ObtenerCalculosSeleccionadosTexto();

                if (seleccionados.Count == 0)
                {
                    return EsModoEdicion
                        ? "No se conservará ningún cálculo complementario."
                        : "No se ha seleccionado ningún cálculo.";
                }

                return string.Join(", ", seleccionados);
            }
        }

        public string TipoCultivo => Resultado?.TipoCultivo ?? string.Empty;
        public string TipoAnalisisSuelo => Resultado?.TipoAnalisisSuelo ?? string.Empty;
        public decimal CantidadQuintalesOro => Resultado?.CantidadQuintalesOro ?? 0;
        public decimal TamanoFinca => Resultado?.TamanoFinca ?? 0;
        public decimal Ph => Resultado?.Ph ?? 0;
        public decimal AcidezTotal => Resultado?.AcidezTotal ?? 0;

        public ObservableCollection<ElementoResultadoCalculoResponse> Elementos { get; }
        public ObservableCollection<string> Observaciones { get; }

        public Command ProcesarSeleccionCommand { get; }
        public Command VolverCommand { get; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            LimpiarPantallaTemporal();

            if (query.TryGetValue("resultadoCalculo", out object? valorResultado) &&
                valorResultado is AnalisisSueloCalculoDataResponse resultadoApi)
            {
                CargarResultado(resultadoApi);
            }

            if (query.TryGetValue("requestGuardarAnalisis", out object? valorRequest))
            {
                RequestGuardarAnalisis =
                    valorRequest as AnalisisSueloGuardarCalculoRequest;
            }

            if (query.TryGetValue("cantidadPlantas", out object? valorPlantas) &&
                int.TryParse(valorPlantas?.ToString(), out int plantas))
            {
                CantidadPlantas = plantas;
            }

            EsModoEdicion = ObtenerBoolQuery(query, "esModoEdicion");

            if (query.TryGetValue(
                    "analisisSueloCalculoIdEdicion",
                    out object? valorId) &&
                int.TryParse(valorId?.ToString(), out int idEdicion))
            {
                AnalisisSueloCalculoIdEdicion = idEdicion;
            }

            if (EsModoEdicion)
            {
                CalcularBalanceFormula =
                    ObtenerBoolQuery(query, "calcularBalanceFormula");

                CalcularEnmiendaCalcarea =
                    ObtenerBoolQuery(query, "calcularEnmiendaCalcarea");

                CalcularFertilizacionMixta =
                    ObtenerBoolQuery(query, "calcularFertilizacionMixta");

                string identificador =
                    RequestGuardarAnalisis?.IdentificadorAnalisisSuelo
                    ?? "análisis";

                TituloResultado = $"Editar - {identificador}";

                MensajeSeleccionCalculo =
                    "Los cálculos que estaban guardados aparecen seleccionados. Debe recalcular cada sección seleccionada antes de actualizar.";
            }
        }

        private void NotificarSeleccion()
        {
            OnPropertyChanged(nameof(TieneSeleccionCalculo));
            OnPropertyChanged(nameof(TextoSeleccionCalculo));

            if (CalcularBalanceFormula ||
                CalcularEnmiendaCalcarea ||
                CalcularFertilizacionMixta)
            {
                if (!EsModoEdicion)
                    MensajeSeleccionCalculo = string.Empty;
            }
        }

        private void LimpiarPantallaTemporal()
        {
            MensajeSeleccionCalculo = string.Empty;
            CalcularBalanceFormula = false;
            CalcularEnmiendaCalcarea = false;
            CalcularFertilizacionMixta = false;
            EsModoEdicion = false;
            AnalisisSueloCalculoIdEdicion = null;
        }

        private void CargarResultado(
            AnalisisSueloCalculoDataResponse resultadoApi)
        {
            Resultado = resultadoApi;

            TituloResultado = $"Resultado - {resultadoApi.TipoCultivo}";
            RecomendacionGeneral =
                resultadoApi.RecomendacionGeneral ?? string.Empty;

            Elementos.Clear();

            foreach (ElementoResultadoCalculoResponse elemento in
                     resultadoApi.Elementos
                         .OrderByDescending(x => x.RequerimientoCalculado ?? 0))
            {
                Elementos.Add(elemento);
            }

            Observaciones.Clear();

            foreach (string observacion in resultadoApi.Observaciones)
                Observaciones.Add(observacion);

            TieneElementos = Elementos.Count > 0;
            TieneObservaciones = Observaciones.Count > 0;
            PhMuyAcido = (resultadoApi.Ph ?? 0) < 5.5m;

            DefinirSugerenciaSiguienteCalculo(resultadoApi);
        }

        private void DefinirSugerenciaSiguienteCalculo(
            AnalisisSueloCalculoDataResponse resultadoApi)
        {
            decimal phActual = resultadoApi.Ph ?? 0;

            if (phActual > 0 && phActual < 5.5m)
            {
                SugerenciaSiguienteCalculo =
                    "El pH está muy ácido. Se recomienda revisar la enmienda calcárea.";
                return;
            }

            bool hayDeficiencia = resultadoApi.Elementos.Any(e =>
                string.Equals(
                    e.Clasificacion,
                    "MUY_BAJO",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    e.Clasificacion,
                    "MEDIO_BAJO",
                    StringComparison.OrdinalIgnoreCase));

            SugerenciaSiguienteCalculo = hayDeficiencia
                ? "Hay elementos con deficiencia. Puede seleccionar balance de fórmula o fertilización mixta."
                : "Seleccione los cálculos complementarios que desea procesar.";
        }

        private async Task ProcesarSeleccionAsync()
        {
            if (IsBusy)
                return;

            if (Resultado == null)
            {
                await MostrarMensajeAsync(
                    "Resultado no disponible",
                    "No se encontró el resultado del análisis de suelo.");
                return;
            }

            if (!TieneSeleccionCalculo && !EsModoEdicion)
            {
                MensajeSeleccionCalculo =
                    "Debe seleccionar al menos un cálculo para continuar.";

                await MostrarMensajeAsync(
                    "Validación",
                    MensajeSeleccionCalculo);
                return;
            }

            Dictionary<string, object> parametros = new()
            {
                ["resultadoCalculo"] = Resultado,
                ["calcularBalanceFormula"] = CalcularBalanceFormula,
                ["calcularEnmiendaCalcarea"] = CalcularEnmiendaCalcarea,
                ["calcularFertilizacionMixta"] =
                    CalcularFertilizacionMixta,
                ["esModoEdicion"] = EsModoEdicion
            };

            if (RequestGuardarAnalisis != null)
            {
                parametros["requestGuardarAnalisis"] =
                    RequestGuardarAnalisis;

                if (RequestGuardarAnalisis.TerrenoId is > 0)
                {
                    parametros["terrenoId"] =
                        RequestGuardarAnalisis.TerrenoId.Value;
                }
            }

            if (CantidadPlantas is > 0)
                parametros["cantidadPlantas"] = CantidadPlantas.Value;

            if (EsModoEdicion &&
                AnalisisSueloCalculoIdEdicion is > 0)
            {
                parametros["analisisSueloCalculoIdEdicion"] =
                    AnalisisSueloCalculoIdEdicion.Value;
            }

            await GoToAsyncParameters("//MultiCalculoPage", parametros);
        }

        private List<string> ObtenerCalculosSeleccionadosTexto()
        {
            List<string> seleccionados = new();

            if (CalcularBalanceFormula)
                seleccionados.Add("Balance de fórmula");

            if (CalcularEnmiendaCalcarea)
                seleccionados.Add("Enmienda calcárea");

            if (CalcularFertilizacionMixta)
                seleccionados.Add("Fertilización mixta");

            return seleccionados;
        }

        private async Task VolverAsync()
        {
            if (EsModoEdicion &&
                AnalisisSueloCalculoIdEdicion is > 0)
            {
                await GoToAsyncParameters(
                    AppRoutes.EditarAnalisisGuardado,
                    new Dictionary<string, object>
                    {
                        ["analisisSueloCalculoId"] =
                            AnalisisSueloCalculoIdEdicion.Value
                    });
                return;
            }

            await GoToAsyncParameters("//NuevoAnalisisFormPage");
        }

        private static bool ObtenerBoolQuery(
            IDictionary<string, object> query,
            string key)
        {
            if (!query.TryGetValue(key, out object? valor))
                return false;

            if (valor is bool booleano)
                return booleano;

            return bool.TryParse(valor?.ToString(), out bool resultado) &&
                   resultado;
        }

        private static async Task MostrarMensajeAsync(
            string titulo,
            string mensaje)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    titulo,
                    mensaje,
                    "Aceptar");
            }
        }
    }
}
