using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado de la visita administrativa de Elementos químicos.
    ///
    /// La versión detecta cambios confirmados por CRUD o reactivación.
    /// La visita distingue entre regresar desde el formulario y entrar
    /// nuevamente desde otra interfaz. Además permite comunicar una edición
    /// local al listado sin ejecutar un GET cuando no cambia su composición.
    /// </summary>
    public static class ElementoQuimicoListadoEstadoService
    {
        private const string ClaveModulo =
            "elementoQuimicoPage";

        private const string ClaveMutacionPendiente =
            "mutacionPendiente";

        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(
                ref versionActual);

        public static int MarcarCambio() =>
            Interlocked.Increment(
                ref versionActual);

        /// <summary>
        /// Devuelve true solamente cuando acaba de iniciar una visita nueva.
        /// </summary>
        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService
                .AsegurarVisita(
                    ClaveModulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService
                .EstaActiva(
                    ClaveModulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService
                .FinalizarVisita(
                    ClaveModulo);

        /// <summary>
        /// Registra una edición que puede aplicarse localmente al regresar.
        /// El listado decide si realmente es seguro hacerlo; por ejemplo,
        /// una búsqueda aplicada obliga a consultar nuevamente al servidor.
        /// </summary>
        public static int RegistrarEdicionLocal(
            ElementoQuimicoResponse elemento)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            int version =
                MarcarCambio();

            if (EstaActiva &&
                elemento.ElementoQuimicosId is > 0)
            {
                InterfazVisitaCacheService.Guardar(
                    ClaveModulo,
                    ClaveMutacionPendiente,
                    Copiar(elemento));
            }

            return version;
        }

        /// <summary>
        /// Para creación, reactivación o una edición que puede cambiar el orden
        /// solamente se incrementa la versión. Así el listado recompone la página
        /// actual desde el servidor en lugar de insertar datos en una posición
        /// potencialmente incorrecta.
        /// </summary>
        public static int MarcarParaRecargar()
        {
            InterfazVisitaCacheService.Eliminar(
                ClaveModulo,
                ClaveMutacionPendiente);

            return MarcarCambio();
        }

        public static bool IntentarConsumirEdicion(
            out ElementoQuimicoResponse elemento) =>
            InterfazVisitaCacheService.IntentarConsumir(
                ClaveModulo,
                ClaveMutacionPendiente,
                out elemento);

        private static ElementoQuimicoResponse Copiar(
            ElementoQuimicoResponse elemento) =>
            new()
            {
                ElementoQuimicosId =
                    elemento.ElementoQuimicosId,
                SimboloElementoQuimico =
                    elemento.SimboloElementoQuimico,
                NombreElementoQuimico =
                    elemento.NombreElementoQuimico,
                PesoEquivalenteElementoQuimico =
                    elemento.PesoEquivalenteElementoQuimico,
                Activo =
                    elemento.Activo
            };
    }
}
