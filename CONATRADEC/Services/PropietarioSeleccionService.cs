using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Intercambio temporal entre el catálogo de propietarios y
    /// el formulario de terreno.
    /// </summary>
    public static class PropietarioSeleccionService
    {
        private static readonly object sync = new();

        private static PropietarioResponse? seleccionado;

        public static void Seleccionar(
            PropietarioResponse propietario)
        {
            ArgumentNullException.ThrowIfNull(propietario);

            lock (sync)
            {
                seleccionado = propietario;
            }
        }

        public static PropietarioResponse? Consumir()
        {
            lock (sync)
            {
                PropietarioResponse? resultado =
                    seleccionado;

                seleccionado = null;
                return resultado;
            }
        }

        public static void Limpiar()
        {
            lock (sync)
            {
                seleccionado = null;
            }
        }
    }
}
