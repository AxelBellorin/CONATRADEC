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
                    "Los cálculos guardados aparecen seleccionados. " +
                    "Desmarque los que ya no desea conservar y marque " +
                    "únicamente los que deben guardarse al actualizar. " +
                    "Si vuelve a marcar un cálculo sin haberlo reiniciado " +
                    "ni modificado, se recuperará su resultado guardado.";
            }
        }

        private void ProcesarSeleccionSegunModo()
        {
            if (esEdicionActual)
            {
                /*
                 * Cada vez que el usuario cambia la selección y vuelve a
                 * MultiCálculo, la interfaz debe restaurar únicamente los
                 * módulos que continúan seleccionados.
                 */
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
