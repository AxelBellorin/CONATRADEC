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
        private readonly AnalisisSueloApiService analisisSueloApiService = new();

        private AnalisisSueloCalculoDataResponse? resultado;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private string tituloResultado = "Resultado del análisis de suelo";
        private string recomendacionGeneral = string.Empty;
        private string sugerenciaSiguienteCalculo = string.Empty;
        private string mensajeGuardado = string.Empty;

        private bool tieneObservaciones;
        private bool tieneElementos;
        private bool phMuyAcido;
        private bool analisisGuardado;

        public ResultadoAnalisisSueloViewModel()
        {
            Elementos = new ObservableCollection<ElementoResultadoCalculoResponse>();
            Observaciones = new ObservableCollection<string>();

            GuardarAnalisisCommand = new Command(async () => await GuardarAnalisisAsync(), () => PuedeGuardarAnalisis);
            IrBalanceFormulaCommand = new Command(async () => await IrBalanceFormulaAsync());
            IrEnmiendaCalcareaCommand = new Command(async () => await IrEnmiendaCalcareaAsync());
            IrFertilizacionMixtaCommand = new Command(async () => await IrFertilizacionMixtaAsync());
            VolverCommand = new Command(async () => await VolverAsync());
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
                OnPropertyChanged(nameof(PuedeGuardarAnalisis));
                GuardarAnalisisCommand.ChangeCanExecute();
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

        public string MensajeGuardado
        {
            get => mensajeGuardado;
            set
            {
                mensajeGuardado = value;
                OnPropertyChanged(nameof(MensajeGuardado));
                OnPropertyChanged(nameof(TieneMensajeGuardado));
            }
        }

        public bool TieneMensajeGuardado => !string.IsNullOrWhiteSpace(MensajeGuardado);

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

        public bool AnalisisGuardado
        {
            get => analisisGuardado;
            set
            {
                analisisGuardado = value;
                OnPropertyChanged(nameof(AnalisisGuardado));
                OnPropertyChanged(nameof(PuedeGuardarAnalisis));
                GuardarAnalisisCommand.ChangeCanExecute();
            }
        }

        public bool PuedeGuardarAnalisis =>
            !IsBusy &&
            !AnalisisGuardado &&
            RequestGuardarAnalisis != null;

        public string TipoCultivo => Resultado?.TipoCultivo ?? string.Empty;

        public string TipoAnalisisSuelo => Resultado?.TipoAnalisisSuelo ?? string.Empty;

        public decimal CantidadQuintalesOro => Resultado?.CantidadQuintalesOro ?? 0;

        public decimal TamanoFinca => Resultado?.TamanoFinca ?? 0;

        public decimal Ph => Resultado?.Ph ?? 0;

        public decimal AcidezTotal => Resultado?.AcidezTotal ?? 0;

        public ObservableCollection<ElementoResultadoCalculoResponse> Elementos { get; }

        public ObservableCollection<string> Observaciones { get; }

        public Command GuardarAnalisisCommand { get; }

        public Command IrBalanceFormulaCommand { get; }

        public Command IrEnmiendaCalcareaCommand { get; }

        public Command IrFertilizacionMixtaCommand { get; }

        public Command VolverCommand { get; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
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
        }

        private void CargarResultado(AnalisisSueloCalculoDataResponse resultadoApi)
        {
            Resultado = resultadoApi;

            TituloResultado = $"Resultado - {resultadoApi.TipoCultivo}";
            RecomendacionGeneral = resultadoApi.RecomendacionGeneral ?? string.Empty;

            Elementos.Clear();

            if (resultadoApi.Elementos != null)
            {
                foreach (var elemento in resultadoApi.Elementos)
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
                    "El pH está muy ácido. Se recomienda revisar primero la enmienda calcárea.";
                return;
            }

            bool hayDeficiencia = resultadoApi.Elementos != null &&
                                  resultadoApi.Elementos.Any(e =>
                                      string.Equals(e.Clasificacion, "MUY_BAJO", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(e.Clasificacion, "MEDIO_BAJO", StringComparison.OrdinalIgnoreCase));

            if (hayDeficiencia)
            {
                SugerenciaSiguienteCalculo =
                    "Hay elementos con deficiencia. Puede continuar con balance de fórmula o fertilización mixta.";
                return;
            }

            SugerenciaSiguienteCalculo =
                "Seleccione el tipo de cálculo que desea realizar con base en el resultado obtenido.";
        }

        private async Task GuardarAnalisisAsync()
        {
            if (IsBusy)
                return;

            if (RequestGuardarAnalisis == null)
            {
                await MostrarMensajeAsync("Guardar análisis", "No se encontró la información necesaria para guardar el análisis.");
                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                AnalisisSueloCalculoResponse? response =
                    await analisisSueloApiService.GuardarCalculoAsync(RequestGuardarAnalisis);

                if (response == null)
                {
                    await MostrarMensajeAsync("Error", "La API no devolvió una respuesta válida.");
                    return;
                }

                if (!response.Success)
                {
                    await MostrarMensajeAsync("Error", response.Message ?? "No se pudo guardar el análisis de suelo.");
                    return;
                }

                AnalisisGuardado = true;
                MensajeGuardado = response.Message ?? "Análisis de suelo guardado correctamente.";

                await MostrarMensajeAsync("Correcto", MensajeGuardado);
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo guardar el análisis: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task IrBalanceFormulaAsync()
        {
            if (Resultado == null)
                return;

            var parametros = CrearParametrosNavegacion();

            await GoToAsyncParameters("//BalanceFormulaPage", parametros);
        }

        private async Task IrEnmiendaCalcareaAsync()
        {
            if (Resultado == null)
                return;

            var parametros = CrearParametrosNavegacion();

            await GoToAsyncParameters("//EnmiendaCalcareaPage", parametros);
        }

        private async Task IrFertilizacionMixtaAsync()
        {
            if (Resultado == null)
                return;

            var parametros = CrearParametrosNavegacion();

            await GoToAsyncParameters("//FertilizacionMixtaPage", parametros);
        }

        private Dictionary<string, object> CrearParametrosNavegacion()
        {
            var parametros = new Dictionary<string, object>
            {
                { "resultadoCalculo", Resultado! }
            };

            if (RequestGuardarAnalisis != null)
                parametros.Add("requestGuardarAnalisis", RequestGuardarAnalisis);

            return parametros;
        }

        private async Task VolverAsync()
        {
            await GoToAsyncParameters("..");
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeGuardarAnalisis));
            GuardarAnalisisCommand.ChangeCanExecute();
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }
}