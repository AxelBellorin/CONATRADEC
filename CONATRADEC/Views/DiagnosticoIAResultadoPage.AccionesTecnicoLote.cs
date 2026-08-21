using CONATRADEC.Controls;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Presentación de las acciones masivas del técnico.
    ///
    /// La selección es global, pero cada botón trabaja únicamente con el
    /// subconjunto de fotografías que cumple las reglas de su acción. De esta
    /// forma una fotografía pendiente de IA y otra lista para enviar pueden
    /// permanecer seleccionadas al mismo tiempo sin bloquearse entre sí.
    /// </summary>
    public partial class DiagnosticoIAResultadoPage
    {
        private Button? procesarSeleccionIAButton;
        private Button? enviarSeleccionAnalizadorButton;
        private bool accionesTecnicoLoteIntegradas;

        private void IntegrarAccionesTecnicoLote()
        {
            if (accionesTecnicoLoteIntegradas)
                return;

            procesarSeleccionIAButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    item =>
                        ReferenceEquals(
                            item.Command,
                            viewModel.ProcesarSeleccionCommand) ||
                        string.Equals(
                            item.Text,
                            "Analizar con IA",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            item.Text,
                            "Procesar selección con IA",
                            StringComparison.Ordinal));

            enviarSeleccionAnalizadorButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    item =>
                        ReferenceEquals(
                            item.Command,
                            viewModel.EnviarAnalizadorCommand) ||
                        string.Equals(
                            item.Text,
                            "Enviar al analizador",
                            StringComparison.Ordinal));

            if (procesarSeleccionIAButton == null ||
                enviarSeleccionAnalizadorButton == null)
            {
                return;
            }

            /*
             * El XAML conserva los MultiBinding históricos para compatibilidad
             * visual con versiones anteriores. En esta pantalla actual la
             * autoridad para las acciones del técnico es el ViewModel, porque
             * una selección puede contener estados diferentes.
             */
            procesarSeleccionIAButton.RemoveBinding(
                VisualElement.IsVisibleProperty);
            enviarSeleccionAnalizadorButton.RemoveBinding(
                VisualElement.IsVisibleProperty);

            accionesTecnicoLoteIntegradas = true;
        }

        private void ActualizarAccionesTecnicoLote()
        {
            IntegrarAccionesTecnicoLote();

            bool vistaTecnico = string.Equals(
                viewModel.TextoRegresar,
                "Mis inspecciones",
                StringComparison.OrdinalIgnoreCase);

            if (procesarSeleccionIAButton != null)
            {
                procesarSeleccionIAButton.Text =
                    viewModel.TextoBotonProcesarIA;
                procesarSeleccionIAButton.IsVisible =
                    vistaTecnico &&
                    viewModel.PuedeProcesarSeleccion;
                procesarSeleccionIAButton.IsEnabled =
                    procesarSeleccionIAButton.IsVisible &&
                    !viewModel.IsBusy;
            }

            if (enviarSeleccionAnalizadorButton != null)
            {
                enviarSeleccionAnalizadorButton.Text =
                    viewModel.TextoBotonEnviarAnalizador;
                enviarSeleccionAnalizadorButton.IsVisible =
                    vistaTecnico &&
                    viewModel.PuedeEnviarSeleccion;
                enviarSeleccionAnalizadorButton.IsEnabled =
                    enviarSeleccionAnalizadorButton.IsVisible &&
                    !viewModel.IsBusy;
            }

            ActualizarTextosSeguimientoIA();
        }

        /// <summary>
        /// Evita que "0 reevaluaciones" parezca significar que el análisis
        /// inicial nunca se ejecutó. Cuando ya existe ResultadoIA se informa de
        /// forma explícita que el análisis inicial está completado y el contador
        /// se identifica como reevaluaciones adicionales.
        /// </summary>
        private void ActualizarTextosSeguimientoIA()
        {
            foreach (Label label in
                     ResponsiveLayoutUtility.FindDescendants<Label>(this))
            {
                if (label.BindingContext is not InspeccionFotoV2 foto ||
                    foto.ResultadoIA == null)
                {
                    continue;
                }

                string textoActual = label.Text?.Trim() ?? string.Empty;

                if (!textoActual.StartsWith(
                        "Reevaluaciones IA",
                        StringComparison.OrdinalIgnoreCase) &&
                    !textoActual.StartsWith(
                        "Análisis inicial completado",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                label.RemoveBinding(Label.TextProperty);
                label.Text = CrearTextoSeguimientoIA(foto);
            }
        }

        private static string CrearTextoSeguimientoIA(
            InspeccionFotoV2 foto)
        {
            int reevaluaciones = Math.Max(
                0,
                foto.RevisionesIACompletadas);

            if (foto.RevisionesIAIlimitadas)
            {
                return
                    $"Análisis inicial completado · " +
                    $"Reevaluaciones adicionales: {reevaluaciones} · sin límite";
            }

            int maximo = Math.Max(1, foto.MaximoRevisionesIA);

            return
                $"Análisis inicial completado · " +
                $"Reevaluaciones adicionales: {reevaluaciones} de {maximo}";
        }
    }
}
