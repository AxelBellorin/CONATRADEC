using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado de la visita administrativa de Fuentes de nutrientes.
    /// Conserva la página visible durante los subflujos y obliga a iniciar
    /// limpio cuando el usuario abandona realmente el módulo.
    /// </summary>
    public static class FuenteNutrienteListadoEstadoService
    {
        private const string ClaveModulo =
            "fuenteNutrientePage";

        private const string ClaveMutacionPendiente =
            "mutacionPendiente";

        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(ref versionActual);

        public static int MarcarCambio() =>
            Interlocked.Increment(ref versionActual);

        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(
                ClaveModulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(
                ClaveModulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(
                ClaveModulo);

        public static int RegistrarEdicionLocal(
            FuenteNutrienteResponse fuente)
        {
            ArgumentNullException.ThrowIfNull(fuente);

            int version = MarcarCambio();

            if (EstaActiva &&
                fuente.FuenteNutrientesId is > 0)
            {
                InterfazVisitaCacheService.Guardar(
                    ClaveModulo,
                    ClaveMutacionPendiente,
                    Copiar(fuente));
            }

            return version;
        }

        public static int MarcarParaRecargar()
        {
            InterfazVisitaCacheService.Eliminar(
                ClaveModulo,
                ClaveMutacionPendiente);

            return MarcarCambio();
        }

        public static bool IntentarConsumirEdicion(
            out FuenteNutrienteResponse fuente) =>
            InterfazVisitaCacheService.IntentarConsumir(
                ClaveModulo,
                ClaveMutacionPendiente,
                out fuente);

        private static FuenteNutrienteResponse Copiar(
            FuenteNutrienteResponse fuente) =>
            new()
            {
                FuenteNutrientesId = fuente.FuenteNutrientesId,
                NombreNutriente = fuente.NombreNutriente,
                DescripcionNutriente = fuente.DescripcionNutriente,
                PrecioNutriente = fuente.PrecioNutriente,
                Activo = fuente.Activo,
                HabilitadaEnmiendaCalcarea =
                    fuente.HabilitadaEnmiendaCalcarea,
                HabilitadaFertilizacionMixta =
                    fuente.HabilitadaFertilizacionMixta,
                Prnt = fuente.Prnt,
                DescripcionParametro = fuente.DescripcionParametro,
                ElementosQuimicos =
                    fuente.ElementosQuimicos?
                        .Select(item =>
                            new FuenteNutrienteElementoQuimicoResponse
                            {
                                FuenteNutrienteElementoQuimicoId =
                                    item.FuenteNutrienteElementoQuimicoId,
                                ElementoQuimicosId =
                                    item.ElementoQuimicosId,
                                NombreElementoQuimico =
                                    item.NombreElementoQuimico,
                                SimboloElementoQuimico =
                                    item.SimboloElementoQuimico,
                                CantidadAporte =
                                    item.CantidadAporte
                            })
                        .ToList() ??
                    new List<FuenteNutrienteElementoQuimicoResponse>()
            };
    }
}
