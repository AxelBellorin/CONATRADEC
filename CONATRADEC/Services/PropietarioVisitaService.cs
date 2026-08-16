using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public enum PropietarioMutacionListadoTipo
    {
        Creado,
        Actualizado
    }

    /// <summary>
    /// Cambio confirmado por el servidor que puede aplicarse al listado al
    /// regresar del formulario sin ejecutar un GET cuando la composición de
    /// la página visible puede determinarse con seguridad.
    /// </summary>
    public sealed record PropietarioMutacionListado(
        PropietarioMutacionListadoTipo Tipo,
        PropietarioResponse Actual,
        PropietarioResponse? Anterior = null);

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

        private const string ClaveMutacionListado =
            "listado:mutacion";

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
        /// Se utiliza únicamente cuando el cambio no puede reconstruirse de
        /// forma segura con los datos disponibles durante la visita.
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
        /// El formulario regresa inmediatamente al listado después de guardar,
        /// por lo que basta conservar una única mutación pendiente.
        /// </summary>
        public static void RegistrarMutacion(
            bool modoSeleccion,
            PropietarioMutacionListado mutacion)
        {
            ArgumentNullException.ThrowIfNull(mutacion);
            ArgumentNullException.ThrowIfNull(mutacion.Actual);

            string modulo =
                ObtenerModulo(modoSeleccion);

            if (!InterfazVisitaCacheService.EstaActiva(modulo))
                return;

            InterfazVisitaCacheService.Guardar(
                modulo,
                ClaveMutacionListado,
                mutacion);
        }

        public static bool ConsumirMutacion(
            bool modoSeleccion,
            out PropietarioMutacionListado mutacion)
        {
            string modulo =
                ObtenerModulo(modoSeleccion);

            if (!InterfazVisitaCacheService.EstaActiva(modulo))
            {
                mutacion = null!;
                return false;
            }

            return InterfazVisitaCacheService.IntentarConsumir(
                modulo,
                ClaveMutacionListado,
                out mutacion);
        }

        public static void DescartarMutacion(
            bool modoSeleccion)
        {
            InterfazVisitaCacheService.Eliminar(
                ObtenerModulo(modoSeleccion),
                ClaveMutacionListado);
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
