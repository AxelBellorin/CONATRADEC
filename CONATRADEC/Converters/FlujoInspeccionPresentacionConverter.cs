using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Centraliza la visibilidad de acciones y selección de fotografías según
    /// la pantalla de origen, la etapa técnica y el estado de cada evidencia.
    /// No modifica estados internos ni sustituye las validaciones del backend.
    ///
    /// El analizador puede trabajar con las fotografías que el técnico ya
    /// envió, aunque la etapa técnica todavía continúe abierta. El aprobador sí
    /// permanece bloqueado hasta que la revisión humana se complete.
    /// </summary>
    public sealed class FlujoInspeccionPresentacionConverter :
        IValueConverter,
        IMultiValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            string origen = value?.ToString()?.Trim() ?? string.Empty;
            string opcion = parameter?.ToString()?.Trim() ?? string.Empty;

            if (opcion == "SubtituloFlujo" &&
                value is DiagnosticoIAResultadoViewModel viewModel)
            {
                if (viewModel.Detalle == null)
                    return "Cargando expediente...";

                string etapa = viewModel.Detalle.CerradaTecnico
                    ? "Etapa técnica finalizada"
                    : "Etapa técnica abierta";

                return $"{viewModel.Detalle.TerrenoTexto} · " +
                       $"Estado: {viewModel.Detalle.EstadoTexto} · {etapa}";
            }

            return opcion switch
            {
                "VistaTecnico" => EsVistaTecnico(origen),
                "VistaAnalizador" => EsVistaAnalizador(origen),
                "VistaAprobador" => EsVistaAprobador(origen),
                "VistaHistorial" => EsVistaHistorial(origen),
                _ => false
            };
        }

        public object Convert(
            object[] values,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            string opcion = parameter?.ToString()?.Trim() ?? string.Empty;
            string origen = ObtenerTexto(values, 0);

            return opcion switch
            {
                "PanelAcciones" => MostrarPanelAcciones(
                    origen,
                    ObtenerBooleano(values, 1)),

                "EtapaTecnicaFinalizadaTecnico" =>
                    EsVistaTecnico(origen) && ObtenerBooleano(values, 1),

                "AccionTecnico" =>
                    EsVistaTecnico(origen) && ObtenerBooleano(values, 1),

                "AccionAnalizador" =>
                    EsVistaAnalizador(origen) && ObtenerBooleano(values, 1),

                "AccionAprobador" =>
                    EsVistaAprobador(origen) && ObtenerBooleano(values, 1),

                "PuedeSeleccionarFotografia" => PuedeSeleccionarFotografia(
                    origen,
                    ObtenerBooleano(values, 1),
                    ObtenerTexto(values, 2)),

                _ => false
            };
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();

        private static bool MostrarPanelAcciones(
            string origen,
            bool etapaTecnicaFinalizada)
        {
            if (EsVistaTecnico(origen))
                return !etapaTecnicaFinalizada;

            /*
             * El analizador recibe las evidencias de forma progresiva. Por eso
             * su panel se mantiene disponible aun cuando el técnico todavía no
             * haya finalizado toda la etapa. Las reglas del estado individual
             * determinan qué fotografía puede seleccionar.
             */
            if (EsVistaAnalizador(origen))
                return true;

            if (EsVistaAprobador(origen))
                return etapaTecnicaFinalizada;

            return false;
        }

        private static bool PuedeSeleccionarFotografia(
            string origen,
            bool etapaTecnicaFinalizada,
            string estado)
        {
            estado = estado.Trim().ToUpperInvariant();

            if (EsVistaTecnico(origen))
            {
                if (etapaTecnicaFinalizada)
                    return false;

                return estado is
                    "BORRADOR" or
                    "PENDIENTE_IA" or
                    "ERROR_IA" or
                    "PENDIENTE_DECISION_TECNICO";
            }

            if (EsVistaAnalizador(origen))
            {
                return estado is
                    "PENDIENTE_ANALIZADOR" or
                    "EN_ANALISIS_HUMANO" or
                    "DEVUELTO_PARA_CORRECCION" or
                    "DEVUELTA_AL_ANALIZADOR";
            }

            if (EsVistaAprobador(origen))
            {
                return etapaTecnicaFinalizada &&
                       estado == "PENDIENTE_APROBACION";
            }

            return false;
        }

        private static bool EsVistaTecnico(string origen) =>
            origen.Equals(
                "Mis inspecciones",
                StringComparison.OrdinalIgnoreCase) ||
            origen.Equals(
                "Decisiones pendientes",
                StringComparison.OrdinalIgnoreCase);

        private static bool EsVistaAnalizador(string origen) =>
            origen.Contains(
                "analizador",
                StringComparison.OrdinalIgnoreCase);

        private static bool EsVistaAprobador(string origen) =>
            origen.Contains(
                "aprobador",
                StringComparison.OrdinalIgnoreCase);

        private static bool EsVistaHistorial(string origen) =>
            origen.Contains(
                "historial",
                StringComparison.OrdinalIgnoreCase);

        private static string ObtenerTexto(object[] values, int indice)
        {
            if (indice < 0 || indice >= values.Length)
                return string.Empty;

            object? valor = values[indice];
            if (valor == null || valor == BindableProperty.UnsetValue)
                return string.Empty;

            return valor.ToString()?.Trim() ?? string.Empty;
        }

        private static bool ObtenerBooleano(object[] values, int indice)
        {
            if (indice < 0 || indice >= values.Length)
                return false;

            object? valor = values[indice];
            if (valor == null || valor == BindableProperty.UnsetValue)
                return false;

            return valor is bool booleano
                ? booleano
                : bool.TryParse(valor.ToString(), out bool resultado) &&
                  resultado;
        }
    }
}
