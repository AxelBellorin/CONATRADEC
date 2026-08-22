using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Clasificación persistida para una afectación individual de una
    /// fotografía fitosanitaria.
    /// </summary>
    public sealed class InspeccionClasificacionDiagnosticoV2
    {
        public int Id { get; set; }
        public int FotografiaId { get; set; }
        public string DiagnosticoClave { get; set; } = string.Empty;
        public string DiagnosticoIdOrigenIA { get; set; } = string.Empty;
        public int OrdenDiagnostico { get; set; }
        public bool EsPrincipal { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string CategoriaIA { get; set; } = string.Empty;
        public string TipoDiagnosticoIA { get; set; } = string.Empty;

        public int? CategoriaAlbumBotanicoIdSugerida { get; set; }
        public int? AlbumBotanicoCafeIdSugerido { get; set; }
        public string CategoriaSugerida { get; set; } = string.Empty;
        public string SubcategoriaSugerida { get; set; } = string.Empty;
        public string NombreCientificoSugerido { get; set; } = string.Empty;
        public bool CoincideCatalogo { get; set; }
        public bool RequiereDecision { get; set; }

        public int? CategoriaAlbumBotanicoIdSeleccionada { get; set; }
        public int? AlbumBotanicoCafeIdSeleccionado { get; set; }
        public string CategoriaSeleccionada { get; set; } = string.Empty;
        public string SubcategoriaSeleccionada { get; set; } = string.Empty;

        public string AccionHumana { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FuenteVigente { get; set; } = string.Empty;
        public int? UsuarioActualizacionId { get; set; }
        public DateTime FechaActualizacionUtc { get; set; }
        public bool Activo { get; set; }

        [JsonIgnore]
        public string Rol =>
            EsPrincipal
                ? "Diagnóstico principal"
                : "Diagnóstico adicional";

        [JsonIgnore]
        public string CategoriaMostrar =>
            !string.IsNullOrWhiteSpace(CategoriaSeleccionada)
                ? CategoriaSeleccionada
                : CategoriaSugerida;

        [JsonIgnore]
        public string SubcategoriaMostrar =>
            !string.IsNullOrWhiteSpace(SubcategoriaSeleccionada)
                ? SubcategoriaSeleccionada
                : SubcategoriaSugerida;

        [JsonIgnore]
        public bool TieneSeleccionOficial =>
            CategoriaAlbumBotanicoIdSeleccionada is > 0 &&
            AlbumBotanicoCafeIdSeleccionado is > 0;

        [JsonIgnore]
        public bool EstaDescartada =>
            string.Equals(
                AccionHumana,
                "DESCARTAR",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                Estado,
                "DESCARTADA_DIAGNOSTICO",
                StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public string EstadoCategoriaTexto =>
            TieneSeleccionOficial || CategoriaAlbumBotanicoIdSugerida is > 0
                ? "Categoría existente"
                : "Categoría propuesta";

        [JsonIgnore]
        public string EstadoSubcategoriaTexto =>
            TieneSeleccionOficial || AlbumBotanicoCafeIdSugerido is > 0
                ? "Subcategoría existente"
                : "Subcategoría propuesta";
    }

    public sealed class ResolverInspeccionClasificacionDiagnosticoRequest
    {
        public string DiagnosticoClave { get; set; } = string.Empty;
        public string Etapa { get; set; } = "ANALIZADOR";
        public string Accion { get; set; } = "CONFIRMAR";
        public int? CategoriaAlbumBotanicoId { get; set; }
        public int? AlbumBotanicoCafeId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
    }
}
