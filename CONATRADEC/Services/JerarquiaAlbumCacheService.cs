using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Conserva temporalmente la clasificación jerárquica cargada en una sola
    /// solicitud por inspección. Los controles de cada tarjeta leen este mapa
    /// y no realizan consultas individuales.
    /// </summary>
    public static class JerarquiaAlbumCacheService
    {
        private static readonly object SyncRoot = new();

        private static readonly Dictionary<int,
            IReadOnlyDictionary<int, JerarquiaDiagnosticoFotoResponse>> Cache =
            new();

        public static event EventHandler<int>? DiagnosticoActualizado;

        public static void Establecer(
            int diagnosticoId,
            IEnumerable<JerarquiaDiagnosticoFotoResponse> items)
        {
            if (diagnosticoId <= 0)
                return;

            Dictionary<int, JerarquiaDiagnosticoFotoResponse> mapa = items
                .Where(item => item.FotografiaId > 0)
                .GroupBy(item => item.FotografiaId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Last());

            lock (SyncRoot)
                Cache[diagnosticoId] = mapa;

            DiagnosticoActualizado?.Invoke(null, diagnosticoId);
        }

        public static JerarquiaDiagnosticoFotoResponse? Obtener(
            int diagnosticoId,
            int fotografiaId)
        {
            lock (SyncRoot)
            {
                return Cache.TryGetValue(
                           diagnosticoId,
                           out IReadOnlyDictionary<int,
                               JerarquiaDiagnosticoFotoResponse>? mapa) &&
                       mapa.TryGetValue(
                           fotografiaId,
                           out JerarquiaDiagnosticoFotoResponse? item)
                    ? item
                    : null;
            }
        }

        public static void Limpiar(int diagnosticoId)
        {
            lock (SyncRoot)
                Cache.Remove(diagnosticoId);
        }
    }
}
