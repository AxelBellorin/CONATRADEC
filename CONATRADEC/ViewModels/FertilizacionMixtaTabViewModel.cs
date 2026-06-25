using CONATRADEC.Models;
using Microsoft.Maui.Controls;

namespace CONATRADEC.ViewModels
{
    public class FertilizacionMixtaTabViewModel : BindableObject
    {
        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private string nombrePlan = "Plan fertilización mixta";
        private string observacion = string.Empty;
        private string mensaje = "Pendiente de configuración de lógica o API de fertilización mixta.";

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

        public string NombrePlan
        {
            get => nombrePlan;
            set
            {
                nombrePlan = value ?? string.Empty;
                OnPropertyChanged(nameof(NombrePlan));
            }
        }

        public string Observacion
        {
            get => observacion;
            set
            {
                observacion = value ?? string.Empty;
                OnPropertyChanged(nameof(Observacion));
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

        public void Inicializar(
            AnalisisSueloCalculoDataResponse? resultado,
            AnalisisSueloGuardarCalculoRequest? requestGuardar)
        {
            ResultadoCalculo = resultado;
            RequestGuardarAnalisis = requestGuardar;
        }
    }
}