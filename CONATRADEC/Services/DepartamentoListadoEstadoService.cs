using System.Collections.Concurrent;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Lleva una versión independiente del listado de departamentos
    /// para cada país. La pantalla solo se recarga cuando hubo cambios.
    /// </summary>
    public static class DepartamentoListadoEstadoService
    {
        private static readonly ConcurrentDictionary<int, int>
            VersionesPorPais = new();

        public static int ObtenerVersion(int paisId)
        {
            if (paisId <= 0)
                return 0;

            return VersionesPorPais.TryGetValue(
                paisId,
                out int version)
                    ? version
                    : 0;
        }

        public static int MarcarCambio(int paisId)
        {
            if (paisId <= 0)
                return 0;

            return VersionesPorPais.AddOrUpdate(
                paisId,
                1,
                (_, versionActual) =>
                    versionActual + 1);
        }
    }
}
