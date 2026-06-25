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

        public ResultadoAnalisisSueloViewModel()
        {
            Elementos = new ObservableCollection<ElementoResultadoCalculoResponse>();
            Observaciones = new ObservableCollection<string>();

            ProcesarSeleccionCommand = new Command(
                async () => await ProcesarSeleccionAsync()
            );

            VolverCommand = new Command(
                async () => await VolverAsync()
            );
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

        public bool TieneMensajeSeleccionCalculo => !string.IsNullOrWhiteSpace(MensajeSeleccionCalculo);

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
                OnPropertyChanged(nameof(TieneSeleccionCalculo));
                OnPropertyChanged(nameof(TextoSeleccionCalculo));

                if (value)
                    MensajeSeleccionCalculo = string.Empty;
            }
        }

        public bool CalcularEnmiendaCalcarea
        {
            get => calcularEnmiendaCalcarea;
            set
            {
                calcularEnmiendaCalcarea = value;
                OnPropertyChanged(nameof(CalcularEnmiendaCalcarea));
                OnPropertyChanged(nameof(TieneSeleccionCalculo));
                OnPropertyChanged(nameof(TextoSeleccionCalculo));

                if (value)
                    MensajeSeleccionCalculo = string.Empty;
            }
        }

        public bool CalcularFertilizacionMixta
        {
            get => calcularFertilizacionMixta;
            set
            {
                calcularFertilizacionMixta = value;
                OnPropertyChanged(nameof(CalcularFertilizacionMixta));
                OnPropertyChanged(nameof(TieneSeleccionCalculo));
                OnPropertyChanged(nameof(TextoSeleccionCalculo));

                if (value)
                    MensajeSeleccionCalculo = string.Empty;
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
                    return "No se ha seleccionado ningún cálculo.";

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

            if (query.ContainsKey("resultadoCalculo"))
            {
                AnalisisSueloCalculoDataResponse? resultadoApi =
                    query["resultadoCalculo"] as AnalisisSueloCalculoDataResponse;

                if (resultadoApi != null)
                {
                    CargarResultado(resultadoApi);
                }
            }

            if (query.ContainsKey("requestGuardarAnalisis"))
            {
                RequestGuardarAnalisis =
                    query["requestGuardarAnalisis"] as AnalisisSueloGuardarCalculoRequest;
            }

            if (query.ContainsKey("cantidadPlantas"))
            {
                if (int.TryParse(query["cantidadPlantas"]?.ToString(), out int plantas))
                    CantidadPlantas = plantas;
            }
        }

        private void LimpiarPantallaTemporal()
        {
            MensajeSeleccionCalculo = string.Empty;

            CalcularBalanceFormula = false;
            CalcularEnmiendaCalcarea = false;
            CalcularFertilizacionMixta = false;
        }

        private void CargarResultado(AnalisisSueloCalculoDataResponse resultadoApi)
        {
            Resultado = resultadoApi;

            TituloResultado = $"Resultado - {resultadoApi.TipoCultivo}";
            RecomendacionGeneral = resultadoApi.RecomendacionGeneral ?? string.Empty;

            Elementos.Clear();

            if (resultadoApi.Elementos != null)
            {
                var elementosOrdenados = resultadoApi.Elementos
                    .OrderByDescending(x => x.RequerimientoCalculado ?? 0)
                    .ToList();

                foreach (var elemento in elementosOrdenados)
                {
                    Elementos.Add(elemento);
                }
            }

            Observaciones.Clear();

            if (resultadoApi.Observaciones != null)
            {
                foreach (var observacion in resultadoApi.Observaciones)
                {
                    Observaciones.Add(observacion);
                }
            }

            TieneElementos = Elementos.Count > 0;
            TieneObservaciones = Observaciones.Count > 0;
            PhMuyAcido = (resultadoApi.Ph ?? 0) < 5.5m;

            DefinirSugerenciaSiguienteCalculo(resultadoApi);
        }

        private void DefinirSugerenciaSiguienteCalculo(AnalisisSueloCalculoDataResponse resultadoApi)
        {
            decimal ph = resultadoApi.Ph ?? 0;

            if (ph > 0 && ph < 5.5m)
            {
                SugerenciaSiguienteCalculo =
                    "El pH está muy ácido. Se recomienda revisar la enmienda calcárea como uno de los cálculos opcionales.";
                return;
            }

            bool hayDeficiencia = resultadoApi.Elementos != null &&
                                  resultadoApi.Elementos.Any(e =>
                                      string.Equals(e.Clasificacion, "MUY_BAJO", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(e.Clasificacion, "MEDIO_BAJO", StringComparison.OrdinalIgnoreCase));

            if (hayDeficiencia)
            {
                SugerenciaSiguienteCalculo =
                    "Hay elementos con deficiencia. Puede seleccionar balance de fórmula o fertilización mixta.";
                return;
            }

            SugerenciaSiguienteCalculo =
                "Seleccione uno o varios cálculos opcionales según lo que desea procesar.";
        }

        private async Task ProcesarSeleccionAsync()
        {
            if (IsBusy)
                return;

            if (Resultado == null)
            {
                await MostrarMensajeAsync(
                    "Resultado no disponible",
                    "No se encontró el resultado del análisis de suelo."
                );

                return;
            }

            if (!TieneSeleccionCalculo)
            {
                MensajeSeleccionCalculo = "Debe seleccionar al menos un cálculo para continuar.";
                await MostrarMensajeAsync("Validación", MensajeSeleccionCalculo);
                return;
            }

            var parametros = new Dictionary<string, object>
            {
                { "resultadoCalculo", Resultado },
                { "calcularBalanceFormula", CalcularBalanceFormula },
                { "calcularEnmiendaCalcarea", CalcularEnmiendaCalcarea },
                { "calcularFertilizacionMixta", CalcularFertilizacionMixta }
            };

            if (RequestGuardarAnalisis != null)
            {
                parametros.Add("requestGuardarAnalisis", RequestGuardarAnalisis);

                if (RequestGuardarAnalisis.TerrenoId != null && RequestGuardarAnalisis.TerrenoId > 0)
                    parametros.Add("terrenoId", RequestGuardarAnalisis.TerrenoId.Value);
            }

            if (CantidadPlantas != null && CantidadPlantas > 0)
                parametros.Add("cantidadPlantas", CantidadPlantas.Value);

            await GoToAsyncParameters("//MultiCalculoPage", parametros);
        }

        private List<string> ObtenerCalculosSeleccionadosTexto()
        {
            var seleccionados = new List<string>();

            if (CalcularBalanceFormula)
                seleccionados.Add("Balance de fórmula");

            if (CalcularEnmiendaCalcarea)
                seleccionados.Add("Enmienda calcárea");

            if (CalcularFertilizacionMixta)
                seleccionados.Add("Fertilización mixta");

            return seleccionados;
        }

        private List<string> ObtenerCalculosSeleccionadosCodigo()
        {
            var seleccionados = new List<string>();

            if (CalcularBalanceFormula)
                seleccionados.Add("BALANCE_FORMULA");

            if (CalcularEnmiendaCalcarea)
                seleccionados.Add("ENMIENDA_CALCAREA");

            if (CalcularFertilizacionMixta)
                seleccionados.Add("FERTILIZACION_MIXTA");

            return seleccionados;
        }

        private async Task VolverAsync()
        {
            await GoToAsyncParameters("//NuevoAnalisisFormPage");
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }
}