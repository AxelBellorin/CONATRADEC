using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public sealed class AnalisisEdicionContexto
    {
        public AnalisisGuardadoResumen Resumen { get; set; } = new();

        public AnalisisGuardadoDetalleData Detalle { get; set; } = new();

        public bool FormularioPrecargado { get; set; }

        public bool SeleccionesAplicadas { get; set; }
    }

    /// <summary>
    /// Mantiene temporalmente el análisis que se está editando mientras el
    /// usuario recorre NuevoAnalisisFormPage, ResultadoAnalisisSueloPage y
    /// MultiCalculoPage. Se limpia al actualizar, cancelar o iniciar un análisis nuevo.
    /// </summary>
    public sealed class AnalisisEdicionService
    {
        private static readonly Lazy<AnalisisEdicionService> instancia =
            new(() => new AnalisisEdicionService());

        public static AnalisisEdicionService Instance => instancia.Value;

        public AnalisisEdicionContexto? ContextoActual { get; private set; }

        public bool TieneEdicionActiva => ContextoActual != null;

        private AnalisisEdicionService()
        {
        }

        public void Preparar(
            AnalisisGuardadoResumen resumen,
            AnalisisGuardadoDetalleData detalle)
        {
            ContextoActual = new AnalisisEdicionContexto
            {
                Resumen = resumen,
                Detalle = detalle,
                FormularioPrecargado = false,
                SeleccionesAplicadas = false
            };
        }

        public void Limpiar()
        {
            ContextoActual = null;
        }
    }
}
