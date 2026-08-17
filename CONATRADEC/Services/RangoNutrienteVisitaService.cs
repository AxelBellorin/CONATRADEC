using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene únicamente el estado mínimo de coordinación de la visita al
    /// módulo Rangos nutricionales. Las colecciones permanecen en sus
    /// ViewModels; aquí solo se registran invalidaciones confirmadas por CRUD.
    /// </summary>
    public static class RangoNutrienteVisitaService
    {
        private const string Modulo =
            "rangoNutrientePage";

        private const string ClaveRecargaPrincipal =
            "ListadoPrincipal:Recargar";

        private const string PrefijoRecargaDetalle =
            "Detalle:Recargar:";

        private static long contadorGeneraciones;
        private static long generacionActiva;

        public static bool AsegurarVisita()
        {
            bool nuevaVisita =
                InterfazVisitaCacheService.AsegurarVisita(Modulo);

            if (!nuevaVisita)
                return false;

            Volatile.Write(
                ref generacionActiva,
                Interlocked.Increment(ref contadorGeneraciones));

            return true;
        }

        public static bool EstaActiva() =>
            InterfazVisitaCacheService.EstaActiva(Modulo);

        public static long GeneracionActual =>
            Volatile.Read(ref generacionActiva);

        public static void FinalizarVisita()
        {
            InterfazVisitaCacheService.FinalizarVisita(Modulo);
            Volatile.Write(ref generacionActiva, 0);
        }

        public static void MarcarListadoPrincipalParaRecargar()
        {
            AsegurarVisitaSiHaceFalta();

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRecargaPrincipal,
                true);
        }

        public static bool ConsumirRecargaListadoPrincipal() =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveRecargaPrincipal,
                out bool recargar) &&
            recargar;

        public static void MarcarDetalleParaRecargar(
            int tipoCultivoId)
        {
            if (tipoCultivoId <= 0)
                return;

            AsegurarVisitaSiHaceFalta();

            InterfazVisitaCacheService.Guardar(
                Modulo,
                CrearClaveDetalle(tipoCultivoId),
                true);
        }

        public static bool ConsumirRecargaDetalle(
            int tipoCultivoId)
        {
            if (tipoCultivoId <= 0)
                return false;

            return InterfazVisitaCacheService.IntentarConsumir(
                       Modulo,
                       CrearClaveDetalle(tipoCultivoId),
                       out bool recargar) &&
                   recargar;
        }

        private static string CrearClaveDetalle(
            int tipoCultivoId) =>
            $"{PrefijoRecargaDetalle}{tipoCultivoId}";

        private static void AsegurarVisitaSiHaceFalta()
        {
            if (InterfazVisitaCacheService.EstaActiva(Modulo))
                return;

            AsegurarVisita();
        }
    }
}
