namespace CONATRADEC.Models
{
    /// <summary>
    /// Estado vivo de la relación entre una fotografía aprobada y el Álbum
    /// Botánico. Se consulta directamente al backend para reflejar también
    /// desactivaciones realizadas desde la administración del álbum.
    /// </summary>
    public sealed class EstadoAlbumAprobador
    {
        public int FotografiaId { get; set; }
        public bool Aprobada { get; set; }
        public bool Autorizada { get; set; }
        public bool PublicadaActiva { get; set; }
        public bool TuvoPublicacion { get; set; }
        public int? CategoriaAlbumBotanicoId { get; set; }
        public int? AlbumBotanicoCafeId { get; set; }
        public int? AlbumBotanicoCafeFotoId { get; set; }
        public string EstadoEvidencia { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
