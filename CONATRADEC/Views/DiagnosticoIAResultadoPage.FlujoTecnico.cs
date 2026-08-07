using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAResultadoPage
    {
        private bool finalizandoEtapaTecnica;

        /// <summary>
        /// Selecciona únicamente fotografías que todavía pertenecen a la etapa
        /// del técnico. Las evidencias enviadas a revisión quedan excluidas.
        /// Las fotografías devueltas por el analizador sí se incluyen para que
        /// el técnico pueda atenderlas o descartarlas si fueron sustituidas.
        /// </summary>
        private void OnSeleccionarTodoTecnicoClicked(
            object sender,
            EventArgs e)
        {
            if (BindingContext is not DiagnosticoIAResultadoViewModel viewModel ||
                viewModel.IsBusy ||
                viewModel.Detalle?.EtapaTecnicaFinalizada == true)
            {
                return;
            }

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                foto.Seleccionada = foto.Estado is
                    InspeccionFotoEstados.Borrador or
                    InspeccionFotoEstados.PendienteIA or
                    InspeccionFotoEstados.ErrorIA or
                    InspeccionFotoEstados.PendienteDecisionTecnico or
                    InspeccionFotoEstadosRevision.DevueltaTecnico;
            }
        }

        /// <summary>
        /// Finaliza exclusivamente la etapa del técnico. Antes de llamar a la
        /// API se informa claramente por qué la inspección todavía no está
        /// lista, en lugar de dejar un botón deshabilitado sin respuesta.
        /// </summary>
        private async void OnFinalizarEtapaTecnicaClicked(
            object sender,
            EventArgs e)
        {
            if (finalizandoEtapaTecnica)
                return;

            if (BindingContext is not DiagnosticoIAResultadoViewModel viewModel)
                return;

            if (viewModel.IsBusy)
            {
                await DisplayAlert(
                    "Proceso en curso",
                    "Espere a que termine la operación actual antes de finalizar la inspección.",
                    "Aceptar");
                return;
            }

            if (viewModel.Detalle == null)
            {
                await DisplayAlert(
                    "Expediente no disponible",
                    "El detalle de la inspección todavía no ha terminado de cargar. Actualice e intente nuevamente.",
                    "Aceptar");
                return;
            }

            List<InspeccionFotoV2> evidenciasActivas = viewModel.Fotografias
                .Where(foto =>
                    !foto.Descartada &&
                    !string.Equals(
                        foto.Estado,
                        InspeccionFotoEstados.Descartada,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (evidenciasActivas.Count == 0)
            {
                await DisplayAlert(
                    "Sin evidencias para enviar",
                    "La inspección no contiene fotografías activas. Agregue al menos una evidencia, analícela con IA y envíela a revisión antes de finalizar la etapa técnica.",
                    "Aceptar");
                return;
            }

            bool existeResultadoIa = evidenciasActivas.Any(foto =>
                foto.ResultadoIA != null ||
                foto.Estado is
                    InspeccionFotoEstados.PendienteDecisionTecnico or
                    InspeccionFotoEstados.PendienteAnalizador or
                    InspeccionFotoEstados.EnAnalisisHumano or
                    InspeccionFotoEstados.PendienteAprobacion or
                    InspeccionFotoEstados.DevueltaAnalizador or
                    InspeccionFotoEstados.Aprobada or
                    InspeccionFotoEstados.AprobadaConCorreccion or
                    InspeccionFotoEstados.Rechazada or
                    InspeccionFotoEstados.NoConcluyente or
                    InspeccionFotoEstados.PublicadaAlbum);

            if (!existeResultadoIa)
            {
                await DisplayAlert(
                    "Análisis de IA requerido",
                    "Ninguna evidencia tiene todavía un resultado de IA. Seleccione al menos una fotografía y use «Analizar con IA» antes de intentar finalizar la inspección.",
                    "Aceptar");
                return;
            }

            if (!viewModel.PuedeCerrarInspeccion)
            {
                string motivo = string.IsNullOrWhiteSpace(
                    viewModel.MotivoNoPuedeCerrar)
                    ? "Todas las fotografías activas deben enviarse a revisión o descartarse antes de finalizar la etapa técnica."
                    : viewModel.MotivoNoPuedeCerrar.Trim();

                await DisplayAlert(
                    "Inspección aún no lista",
                    motivo,
                    "Aceptar");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Finalizar etapa técnica",
                "La inspección será enviada al analizador. Después de continuar no podrá agregar, descartar, volver a evaluar ni modificar fotografías desde la vista del técnico. ¿Desea continuar?",
                "Finalizar y enviar",
                "Permanecer");

            if (!confirmar)
                return;

            finalizandoEtapaTecnica = true;
            bool etapaFinalizada = false;

            if (sender is Button boton)
                boton.IsEnabled = false;

            try
            {
                using HttpResponseMessage respuesta =
                    await ApiClientService.Client.PostAsJsonAsync(
                        $"api/inspecciones-fitosanitarias/{viewModel.Detalle.InspeccionId}/finalizar-etapa-tecnica",
                        new { });

                string contenido = await respuesta.Content.ReadAsStringAsync();

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlert(
                        "No fue posible enviar la inspección",
                        ExtraerMensaje(contenido),
                        "Aceptar");
                    return;
                }

                etapaFinalizada = true;

                await DisplayAlert(
                    "Inspección enviada",
                    "La etapa técnica fue finalizada. La inspección ya está disponible en la bandeja del analizador y quedó bloqueada para modificaciones del técnico.",
                    "Aceptar");

                if (viewModel.ActualizarCommand.CanExecute(null))
                    viewModel.ActualizarCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "No fue posible finalizar la etapa técnica."
                        : ex.Message,
                    "Aceptar");
            }
            finally
            {
                finalizandoEtapaTecnica = false;

                if (sender is Button botonFinal)
                {
                    botonFinal.IsEnabled =
                        !etapaFinalizada && viewModel.PuedeCerrarInspeccion;
                }
            }
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "El servidor no devolvió un detalle del error.";

            try
            {
                using JsonDocument documento = JsonDocument.Parse(contenido);
                JsonElement raiz = documento.RootElement;

                if (raiz.TryGetProperty("message", out JsonElement mensaje) &&
                    mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString() ??
                           "No fue posible finalizar la etapa técnica.";
                }
            }
            catch (JsonException)
            {
                // El contenido no era JSON; se muestra el texto recibido.
            }

            return contenido.Length <= 800
                ? contenido
                : contenido[..800];
        }
    }
}
