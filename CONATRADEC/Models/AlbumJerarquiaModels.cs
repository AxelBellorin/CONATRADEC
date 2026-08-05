using CONATRADEC.Services;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Models
{
    public sealed class SubcategoriaAlbumBotanicoResponse :
        INotifyPropertyChanged
    {
        private bool isSelected;
        public int SubcategoriaAlbumBotanicoId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string NombreSubcategoria { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public int TotalRegistros { get; set; }

        [JsonIgnore]
        public string EstadoTexto => Activo ? "Activa" : "Inactiva";

        [JsonIgnore]
        public string TotalTexto => TotalRegistros == 1
            ? "1 ficha"
            : $"{TotalRegistros} fichas";

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
        public string FondoSeleccion =>
            IsSelected ? "#E7F1ED" : "#FFFFFF";

        [JsonIgnore]
        public Brush BordeSeleccion => new SolidColorBrush(
            Color.FromArgb(IsSelected ? "#3B655B" : "#D6E1DC"));

        [JsonIgnore]
        public string AccionEstadoTexto =>
            Activo ? "Desactivar" : "Activar";

        public override string ToString() => NombreSubcategoria;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
    }

    public sealed class GuardarSubcategoriaAlbumRequest
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public string NombreSubcategoria { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public sealed class AsignarSubcategoriaRegistroRequest
    {
        public int SubcategoriaAlbumBotanicoId { get; set; }
    }

    public sealed class AlbumRegistroJerarquiaResponse
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public int? SubcategoriaAlbumBotanicoId { get; set; }
        public string Subcategoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }

        [JsonIgnore]
        public string RutaJerarquica => string.IsNullOrWhiteSpace(Subcategoria)
            ? $"{Categoria} → Sin subcategoría"
            : $"{Categoria} → {Subcategoria}";

        public override string ToString() => string.IsNullOrWhiteSpace(Subcategoria)
            ? Titulo
            : $"{Subcategoria} → {Titulo}";
    }

    public sealed class JerarquiaDiagnosticoFotoResponse
    {
        public int FotografiaId { get; set; }
        public int Orden { get; set; }
        public bool TieneClasificacion { get; set; }
        public int? CategoriaAlbumBotanicoId { get; set; }
        public int? SubcategoriaAlbumBotanicoId { get; set; }
        public int? AlbumBotanicoCafeId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
        public string Ficha { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public bool CategoriaEsPropuesta { get; set; }
        public bool SubcategoriaEsPropuesta { get; set; }
        public bool FichaEsPropuesta { get; set; }
        public string Estado { get; set; } = string.Empty;

        [JsonIgnore]
        public bool TieneNombreCientifico =>
            !string.IsNullOrWhiteSpace(NombreCientifico);

        [JsonIgnore]
        public string EstadoCategoria => CategoriaEsPropuesta
            ? "Categoría propuesta"
            : "Categoría existente";

        [JsonIgnore]
        public string EstadoSubcategoria => SubcategoriaEsPropuesta
            ? "Subcategoría propuesta"
            : "Subcategoría existente";

        [JsonIgnore]
        public string EstadoFicha => FichaEsPropuesta
            ? "Ficha propuesta"
            : "Ficha existente";
    }

    public sealed class ResolverJerarquiaAlbumRequest
    {
        public string Etapa { get; set; } = "ANALIZADOR";
        public int? CategoriaAlbumBotanicoId { get; set; }
        public int? SubcategoriaAlbumBotanicoId { get; set; }
        public int? AlbumBotanicoCafeId { get; set; }
        public bool ProponerCategoria { get; set; }
        public bool ProponerSubcategoria { get; set; }
        public bool ProponerFicha { get; set; }
        public string CategoriaPropuesta { get; set; } = string.Empty;
        public string SubcategoriaPropuesta { get; set; } = string.Empty;
        public string FichaPropuesta { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Sintomas { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}

namespace CONATRADEC.Models
{
    public sealed class AlbumGaleriaJerarquiaItemResponse
    {
        private string? fotoPortadaUrlOriginal;

        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public int? SubcategoriaAlbumBotanicoId { get; set; }
        public string Subcategoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string DescripcionCorta { get; set; } = string.Empty;
        public string? FotoPortada { get; set; }

        public string? FotoPortadaUrl
        {
            get => AlbumMiniaturaUrlHelper.Crear(
                fotoPortadaUrlOriginal,
                720,
                480,
                68);
            set => fotoPortadaUrlOriginal = value;
        }

        [JsonIgnore]
        public string? FotoPortadaOriginalUrl =>
            ImagenLocalCacheService.ResolverOriginal(
                fotoPortadaUrlOriginal);

        public int TotalFotos { get; set; }
        public bool Activo { get; set; }
        public bool CategoriaActiva { get; set; }
        public bool SubcategoriaActiva { get; set; }
        public DateTime FechaCreacion { get; set; }

        [JsonIgnore]
        public bool TieneFoto =>
            !string.IsNullOrWhiteSpace(FotoPortadaUrl);

        [JsonIgnore]
        public bool SinFoto => !TieneFoto;

        [JsonIgnore]
        public bool TieneNombreCientifico =>
            !string.IsNullOrWhiteSpace(NombreCientifico);

        [JsonIgnore]
        public bool TieneSubcategoria =>
            !string.IsNullOrWhiteSpace(Subcategoria);

        [JsonIgnore]
        public string SubcategoriaMostrar => TieneSubcategoria
            ? Subcategoria
            : "Sin subcategoría asignada";

        [JsonIgnore]
        public string RutaJerarquica =>
            $"{Categoria} → {SubcategoriaMostrar}";

        [JsonIgnore]
        public string TotalFotosTexto => TotalFotos == 1
            ? "1 fotografía"
            : $"{TotalFotos} fotografías";

        [JsonIgnore]
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";

        [JsonIgnore]
        public string EstadoColor => Activo ? "#3B655B" : "#9B552C";

        [JsonIgnore]
        public string AccionEstadoTexto =>
            Activo ? "Desactivar" : "Activar";
    }

    public sealed class AlbumGaleriaJerarquiaPaginaResponse
    {
        public List<AlbumGaleriaJerarquiaItemResponse> Items { get; set; } = [];
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public bool TieneMas { get; set; }
    }

    public sealed class AlbumInicioJerarquiaResponse
    {
        public List<CategoriaAlbumBotanicoResponse> Categorias { get; set; } = [];
        public List<SubcategoriaAlbumBotanicoResponse> Subcategorias { get; set; } = [];
        public AlbumGaleriaJerarquiaPaginaResponse Galeria { get; set; } = new();
    }
}
