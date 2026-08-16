namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado temporal del módulo Propietarios durante una visita.
    ///
    /// La administración y el selector utilizado por Terrenos mantienen
    /// visitas independientes para evitar que un flujo reutilice estado del
    /// otro. Los listados grandes permanecen únicamente en sus ViewModels.
    /// </summary>
    public static class PropietarioVisitaService
    {
        private const string ModuloAdministracion =
            "propietarios:administracion";

        private const string ModuloSeleccion =
            "propietarios:seleccion";

        private const string ClaveRecargarListado =
            "listado:recargar";

        public static bool AsegurarVisita(
            bool modoSeleccion) =>
            InterfazVisitaCacheService.AsegurarVisita(
                ObtenerModulo(modoSeleccion));

        public static void IniciarNuevaVisita(
            bool modoSeleccion) =>
            InterfazVisitaCacheService.IniciarNuevaVisita(
                ObtenerModulo(modoSeleccion));

        public static void FinalizarVisita(
            bool modoSeleccion) =>
            InterfazVisitaCacheService.FinalizarVisita(
                ObtenerModulo(modoSeleccion));

        public static bool EstaActiva(
            bool modoSeleccion) =>
            InterfazVisitaCacheService.EstaActiva(
                ObtenerModulo(modoSeleccion));

        /// <summary>
        /// Marca que el listado debe consultar nuevamente la página visible.
        /// Se usa después de Crear/Editar, reactivar un eliminado o modificar
        /// relaciones que cambian la cantidad de terrenos del propietario.
        /// </summary>
        public static void MarcarListadoParaRecargar(
            bool modoSeleccion)
        {
            string modulo =
                ObtenerModulo(modoSeleccion);

            if (!InterfazVisitaCacheService.EstaActiva(modulo))
                return;

            InterfazVisitaCacheService.Guardar(
                modulo,
                ClaveRecargarListado,
                true);
        }

        public static bool ConsumirRecargaListado(
            bool modoSeleccion)
        {
            string modulo =
                ObtenerModulo(modoSeleccion);

            if (!InterfazVisitaCacheService.EstaActiva(modulo))
                return false;

            return InterfazVisitaCacheService.IntentarConsumir(
                       modulo,
                       ClaveRecargarListado,
                       out bool recargar) &&
                   recargar;
        }

        /// <summary>
        /// Una reactivación desde Propietarios eliminados modifica
        /// exclusivamente el listado administrativo activo.
        /// </summary>
        public static void MarcarAdministracionParaRecargar() =>
            MarcarListadoParaRecargar(false);

        private static string ObtenerModulo(
            bool modoSeleccion) =>
            modoSeleccion
                ? ModuloSeleccion
                : ModuloAdministracion;
    }
}
