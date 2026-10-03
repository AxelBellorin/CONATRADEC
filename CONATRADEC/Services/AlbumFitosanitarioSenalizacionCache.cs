using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Caché exclusiva de la visita actual al detalle del Álbum Botánico.
    /// Se limpia al abandonar la página, por lo que una nueva visita vuelve a
    /// consultar al servidor y nunca reutiliza señalizaciones antiguas.
    /// </summary>
    public static class AlbumFitosanitarioSenalizacionCache
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<int, AlbumFotoSenalizacionResponse>
            PorFoto = [];

        public static event EventHandler<int>? Actualizado;

        public static bool TryObtener(
            int albumBotanicoCafeFotoId,
            out AlbumFotoSenalizacionResponse? resultado)
        {
            lock (Sync)
            {
                if (PorFoto.TryGetValue(
                        albumBotanicoCafeFotoId,
                        out AlbumFotoSenalizacionResponse? valor))
                {
                    resultado = valor;
                    return true;
                }
            }

            resultado = null;
            return false;
        }

        public static void ReemplazarAlbum(
            int albumBotanicoCafeId,
            IEnumerable<AlbumFotoSenalizacionResponse>? datos)
        {
            if (albumBotanicoCafeId <= 0)
                return;

            lock (Sync)
            {
                int[] eliminar = PorFoto
                    .Where(item =>
                        item.Value.AlbumBotanicoCafeId == albumBotanicoCafeId)
                    .Select(item => item.Key)
                    .ToArray();

                foreach (int id in eliminar)
                    PorFoto.Remove(id);

                foreach (AlbumFotoSenalizacionResponse item in datos ?? [])
                {
                    if (item.AlbumBotanicoCafeFotoId > 0)
                        PorFoto[item.AlbumBotanicoCafeFotoId] = item;
                }
            }

            Actualizado?.Invoke(null, albumBotanicoCafeId);
        }

        public static void LimpiarAlbum(int albumBotanicoCafeId)
        {
            if (albumBotanicoCafeId <= 0)
                return;

            lock (Sync)
            {
                int[] eliminar = PorFoto
                    .Where(item =>
                        item.Value.AlbumBotanicoCafeId == albumBotanicoCafeId)
                    .Select(item => item.Key)
                    .ToArray();

                foreach (int id in eliminar)
                    PorFoto.Remove(id);
            }

            Actualizado?.Invoke(null, albumBotanicoCafeId);
        }
    }
}
