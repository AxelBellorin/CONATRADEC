using CONATRADEC.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public sealed class ResultadoAnalisisSueloEdicionViewModel :
        ResultadoAnalisisSueloViewModel,
        IQueryAttributable
    {
        private bool esEdicionActual;

        public ResultadoAnalisisSueloEdicionViewModel()
        {
            VolverCommand = new Command(
                async () => await VolverSegunModoAsync());

            ProcesarSeleccionCommand = new Command(
                ProcesarSeleccionSegunModo);
        }

        public new Command VolverCommand { get; }

        public new Command ProcesarSeleccionCommand { get; }

        public new void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            base.ApplyQueryAttributes(query);

            esEdicionActual =
                query.TryGetValue("esModoEdicion", out object? valor) &&
                bool.TryParse(valor?.ToString(), out bool edicion) &&
                edicion;

            if (esEdicionActual)
            {
                MensajeSeleccionCalculo =
                    "Los cálculos guardados ya están cargados. " +
                    "Puede revisar o modificar cualquier sección; " +
                    "solo deberá actualizar el cálculo que cambie.";
            }
        }

        private void ProcesarSeleccionSegunModo()
        {
            if (esEdicionActual)
            {
                AnalisisEdicionService.Instance.RestauracionUiRealizada =
                    false;
            }

            if (base.ProcesarSeleccionCommand.CanExecute(null))
                base.ProcesarSeleccionCommand.Execute(null);
        }

        private async Task VolverSegunModoAsync()
        {
            if (esEdicionActual &&
                AnalisisEdicionService.Instance.EsModoEdicion)
            {
                await GoToAsyncParameters("//NuevoAnalisisFormPage");
                return;
            }

            if (base.VolverCommand.CanExecute(null))
                base.VolverCommand.Execute(null);
        }
    }
}
