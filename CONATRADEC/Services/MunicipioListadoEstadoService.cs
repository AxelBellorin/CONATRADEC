using System.Collections.Concurrent;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene una versión independiente del listado de municipios
    /// para cada departamento.
    /// </summary>
    public static class MunicipioListadoEstadoService
    {
        private static readonly ConcurrentDictionary<int, int>
            VersionesPorDepartamento = new();

        public static int ObtenerVersion(int departamentoId)
        {
            if (departamentoId <= 0)
                return 0;

            return VersionesPorDepartamento.TryGetValue(
                departamentoId,
                out int version)
                    ? version
                    : 0;
        }

        public static int MarcarCambio(int departamentoId)
        {
            if (departamentoId <= 0)
                return 0;

            return VersionesPorDepartamento.AddOrUpdate(
                departamentoId,
                1,
                (_, versionActual) => versionActual + 1);
        }
    }
}
