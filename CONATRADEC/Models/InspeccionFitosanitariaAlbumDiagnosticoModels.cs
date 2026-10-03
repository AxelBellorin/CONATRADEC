using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Clasificación y estado de publicación de una afectación concreta dentro
    /// de una fotografía fitosanitaria. Una fotografía puede contener varias
    /// instancias de este modelo sin duplicar el archivo físico de evidencia.
    /// </summary>
    public sealed class ClasificacionDiagnosticoAlbumItem :
        INotifyPropertyChanged
    {
        private bool publicarEnAlbum;
        private bool isSelected;

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
        public bool Activo { get; set; }

        public bool PublicadaActiva { get; set; }
        public bool TuvoPublicacion { get; set; }
        public int? DiagnosticoIAAlbumPublicacionDiagnosticoId { get; set; }
        public int? AlbumBotanicoCafeFotoId { get; set; }
        public DateTime? FechaPublicacionUtc { get; set; }
        public string ColorMarcador { get; set; } = "#E53935";
        public List<AlbumFitosanitarioLesion> Lesiones { get; set; } = [];
        public int? AnchoImagen { get; set; }
        public int? AltoImagen { get; set; }

        [JsonIgnore]
        public bool PublicarEnAlbum
        {
            get => publicarEnAlbum;
            set
            {
                if (publicarEnAlbum == value)
                    return;

                publicarEnAlbum = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PublicacionSeleccionTexto));
            }
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FondoSeleccion));
                OnPropertyChanged(nameof(BordeSeleccion));
            }
        }

        [JsonIgnore]
        public string Rol => EsPrincipal
            ? "Diagnóstico principal"
            : "Diagnóstico adicional";

        [JsonIgnore]
        public bool EsDescartada => string.Equals(
            Estado,
            "DESCARTADA_DIAGNOSTICO",
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                AccionHumana,
                "DESCARTAR",
                StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool EsResueltaAprobador => string.Equals(
            Estado,
            "RESUELTA_APROBADOR",
            StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool EsResueltaAnalizador => string.Equals(
            Estado,
            "RESUELTA_ANALIZADOR",
            StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool EsPropuestaAnalizador => string.Equals(
            Estado,
            "PROPUESTA_ANALIZADOR",
            StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public int? CategoriaAlbumIdVigente =>
            CategoriaAlbumBotanicoIdSeleccionada is > 0
                ? CategoriaAlbumBotanicoIdSeleccionada
                : CategoriaAlbumBotanicoIdSugerida;

        [JsonIgnore]
        public int? SubcategoriaAlbumIdVigente =>
            AlbumBotanicoCafeIdSeleccionado is > 0
                ? AlbumBotanicoCafeIdSeleccionado
                : AlbumBotanicoCafeIdSugerido;

        [JsonIgnore]
        public string CategoriaMostrar =>
            !string.IsNullOrWhiteSpace(CategoriaSeleccionada)
                ? CategoriaSeleccionada
                : !string.IsNullOrWhiteSpace(CategoriaSugerida)
                    ? CategoriaSugerida
                    : "Categoría pendiente";

        [JsonIgnore]
        public string SubcategoriaMostrar =>
            !string.IsNullOrWhiteSpace(SubcategoriaSeleccionada)
                ? SubcategoriaSeleccionada
                : !string.IsNullOrWhiteSpace(SubcategoriaSugerida)
                    ? SubcategoriaSugerida
                    : "Subcategoría pendiente";

        [JsonIgnore]
        public string RutaClasificacion =>
            EsDescartada
                ? "No se publicará en el Álbum Botánico"
                : $"{CategoriaMostrar} → {SubcategoriaMostrar}";

        [JsonIgnore]
        public string EstadoClasificacionTexto => Estado switch
        {
            "RESUELTA_APROBADOR" => "Clasificación oficial confirmada",
            "RESUELTA_ANALIZADOR" => "Clasificación confirmada por analizador",
            "PROPUESTA_ANALIZADOR" => "Subcategoría propuesta por analizador",
            "RESUELTA_TECNICO" => "Clasificación revisada por técnico",
            "DESCARTADA_DIAGNOSTICO" => "Diagnóstico descartado",
            "SUGERIDA_IA" => CoincideCatalogo
                ? "Coincidencia sugerida por IA"
                : "Clasificación sugerida por IA",
            _ => string.IsNullOrWhiteSpace(Estado)
                ? "Pendiente de clasificación"
                : Estado.Replace('_', ' ')
        };

        [JsonIgnore]
        public bool PuedePublicarse =>
            !EsDescartada &&
            EsResueltaAprobador &&
            CategoriaAlbumBotanicoIdSeleccionada is > 0 &&
            AlbumBotanicoCafeIdSeleccionado is > 0;

        [JsonIgnore]
        public bool PuedeAdministrarPublicacion =>
            PuedePublicarse || PublicadaActiva;

        [JsonIgnore]
        public bool TieneSenalizacion => Lesiones?.Any(item =>
            item.Box2d is { Count: 4 }) == true;

        [JsonIgnore]
        public int TotalLesiones => Lesiones?.Count(item =>
            item.Box2d is { Count: 4 }) ?? 0;

        [JsonIgnore]
        public string SenalizacionTexto => TieneSenalizacion
            ? TotalLesiones == 1
                ? "Con señalización · 1 región"
                : $"Con señalización · {TotalLesiones} regiones"
            : "Sin señalización";

        [JsonIgnore]
        public string PublicacionTexto => PublicadaActiva
            ? "Publicada actualmente en el Álbum"
            : TuvoPublicacion
                ? "Publicación retirada"
                : "Todavía no publicada";

        [JsonIgnore]
        public string PublicacionSeleccionTexto => PublicarEnAlbum
            ? "Se publicará"
            : PublicadaActiva
                ? "Se retirará"
                : "No se publicará";

        [JsonIgnore]
        public string FondoSeleccion => IsSelected
            ? "#EEF5F2"
            : "#FFFFFF";

        [JsonIgnore]
        public string BordeSeleccion => IsSelected
            ? "#3B655B"
            : "#D7E2DD";

        [JsonIgnore]
        public string EstadoColor => EsDescartada
            ? "#8A9490"
            : EsResueltaAprobador
                ? "#315E52"
                : EsPropuestaAnalizador
                    ? "#9B552C"
                    : "#6B5710";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }

    public sealed class ResolverClasificacionDiagnosticoAlbumRequest
    {
        public string DiagnosticoClave { get; set; } = string.Empty;
        public string Etapa { get; set; } = "ANALIZADOR";
        public string Accion { get; set; } = "CONFIRMAR";
        public int? CategoriaAlbumBotanicoId { get; set; }
        public int? AlbumBotanicoCafeId { get; set; }
        public bool ProponerSubcategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Sintomas { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class SincronizarPublicacionesDiagnosticosAlbumRequest
    {
        public List<PublicacionDiagnosticoSeleccionAlbumRequest> Diagnosticos
        {
            get;
            set;
        } = [];
    }

    public sealed class PublicacionDiagnosticoSeleccionAlbumRequest
    {
        public string DiagnosticoClave { get; set; } = string.Empty;
        public bool Publicar { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public sealed class AlbumFitosanitarioLesion
    {
        public string Id { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public List<int> Box2d { get; set; } = [];
    }

    public sealed class AlbumFitosanitarioCapa
    {
        public string DiagnosticoClave { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
        public string ColorMarcador { get; set; } = "#E53935";
        public List<AlbumFitosanitarioLesion> Lesiones { get; set; } = [];

        [JsonIgnore]
        public bool TieneSenalizacion => Lesiones?.Any(item =>
            item.Box2d is { Count: 4 }) == true;
    }

    public sealed class AlbumFotoSenalizacionResponse
    {
        public int AlbumBotanicoCafeFotoId { get; set; }
        public int AlbumBotanicoCafeId { get; set; }
        public int DiagnosticoIAId { get; set; }
        public int DiagnosticoIAImagenId { get; set; }
        public int? TerrenoId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public int? AnchoImagen { get; set; }
        public int? AltoImagen { get; set; }
        public List<AlbumFitosanitarioCapa> Capas { get; set; } = [];

        [JsonIgnore]
        public bool EsFitosanitaria => DiagnosticoIAImagenId > 0;

        [JsonIgnore]
        public bool TieneSenalizacion => Capas?.Any(item =>
            item.TieneSenalizacion) == true;
    }
}
