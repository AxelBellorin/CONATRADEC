using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public enum RolMutacionListadoTipo
    {
        Creado,
        Actualizado
    }

    /// <summary>
    /// Cambio confirmado por el servidor durante la visita actual de Roles.
    /// El listado decide si puede aplicarlo localmente o si la composición de
    /// la página obliga a realizar un único GET justificado.
    /// </summary>
    public sealed record RolMutacionListado(
        RolMutacionListadoTipo Tipo,
        RolResponse Actual,
        RolResponse? Anterior = null);

    /// <summary>
    /// Estado temporal del módulo Roles con alcance exclusivo de una visita.
    /// Al abandonar el módulo se liberan las referencias almacenadas.
    /// </summary>
    public static class RolVisitaService
    {
        private const string Modulo =
            "roles:administracion";

        private const string ClaveRecargarListado =
            "listado:recargar";

        private const string ClaveMutacionListado =
            "listado:mutacion";

        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(
                Modulo);

        public static void IniciarNuevaVisita() =>
            InterfazVisitaCacheService.IniciarNuevaVisita(
                Modulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(
                Modulo);

        public static bool EstaActiva() =>
            InterfazVisitaCacheService.EstaActiva(
                Modulo);

        public static void MarcarListadoParaRecargar()
        {
            if (!EstaActiva())
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRecargarListado,
                true);
        }

        public static bool ConsumirRecargaListado() =>
            EstaActiva() &&
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveRecargarListado,
                out bool recargar) &&
            recargar;

        public static void RegistrarMutacion(
            RolMutacionListado mutacion)
        {
            ArgumentNullException.ThrowIfNull(mutacion);
            ArgumentNullException.ThrowIfNull(mutacion.Actual);

            if (!EstaActiva())
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveMutacionListado,
                mutacion);
        }

        public static bool ConsumirMutacion(
            out RolMutacionListado mutacion)
        {
            if (!EstaActiva())
            {
                mutacion = null!;
                return false;
            }

            return InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveMutacionListado,
                out mutacion);
        }

        public static void DescartarMutacion() =>
            InterfazVisitaCacheService.Eliminar(
                Modulo,
                ClaveMutacionListado);
    }
}
