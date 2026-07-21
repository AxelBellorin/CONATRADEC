using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CONATRADEC.Models
{
    public sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public sealed class CategoriaAlbumBotanicoResponse :
        INotifyPropertyChanged
    {
        private bool isSelected;

        public int CategoriaAlbumBotanicoId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? RutaImagenPortada { get; set; }
        public string? ImagenPortadaUrl { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalRegistrosActivos { get; set; }
        public bool Activo { get; set; }

        [JsonIgnore]
        public bool TienePortada =>
            !string.IsNullOrWhiteSpace(ImagenPortadaUrl);

        [JsonIgnore]
        public bool SinPortada => !TienePortada;

        [JsonIgnore]
        public string TotalTexto =>
            TotalRegistros == 1
                ? "1 registro"
                : $"{TotalRegistros} registros";

        [JsonIgnore]
        public string EstadoTexto =>
            Activo ? "Activa" : "Inactiva";

        [JsonIgnore]
        public string EstadoColor =>
            Activo ? "#3B655B" : "#9B552C";

        [JsonIgnore]
        public string AccionEstadoTexto =>
            Activo ? "Desactivar" : "Activar";

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
                OnPropertyChanged(nameof(BordeSeleccion));
                OnPropertyChanged(nameof(FondoSeleccion));
            }
        }

        private static readonly Brush BordeSeleccionado =
            new SolidColorBrush(Color.FromArgb("#3B655B"));

        private static readonly Brush BordeNormal =
            new SolidColorBrush(Color.FromArgb("#DDE7E3"));

        [JsonIgnore]
        public Brush BordeSeleccion =>
            IsSelected ? BordeSeleccionado : BordeNormal;

        [JsonIgnore]
        public string FondoSeleccion =>
            IsSelected ? "#EEF5F2" : "#FFFFFF";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }

    public sealed class CategoriaAlbumBotanicoRequest
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? RutaImagenPortadaActual { get; set; }

        public CategoriaAlbumBotanicoRequest()
        {
        }

        public CategoriaAlbumBotanicoRequest(
            CategoriaAlbumBotanicoResponse response)
        {
            CategoriaAlbumBotanicoId =
                response.CategoriaAlbumBotanicoId;
            NombreCategoria = response.NombreCategoria;
            Descripcion = response.Descripcion;
            RutaImagenPortadaActual =
                response.ImagenPortadaUrl;
        }
    }

    public sealed class AlbumGaleriaItemResponse
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string DescripcionCorta { get; set; } = string.Empty;
        public string? FotoPortada { get; set; }
        public string? FotoPortadaUrl { get; set; }
        public int TotalFotos { get; set; }
        public bool Activo { get; set; }
        public bool CategoriaActiva { get; set; }
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
        public string TotalFotosTexto =>
            TotalFotos == 1
                ? "1 fotografía"
                : $"{TotalFotos} fotografías";

        [JsonIgnore]
        public string EstadoTexto =>
            Activo ? "Activo" : "Inactivo";

        [JsonIgnore]
        public string EstadoColor =>
            Activo ? "#3B655B" : "#9B552C";

        [JsonIgnore]
        public string AccionEstadoTexto =>
            Activo ? "Desactivar" : "Activar";
    }

    public sealed class AlbumDetalleResponse
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public bool CategoriaActiva { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Caracteristicas { get; set; }
        public string? Sintomas { get; set; }
        public string? Causas { get; set; }
        public string? Recomendaciones { get; set; }
        public string? Observaciones { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<AlbumFotoResponse> Fotos { get; set; } = new();

        [JsonIgnore]
        public bool TieneNombreCientifico =>
            !string.IsNullOrWhiteSpace(NombreCientifico);

        [JsonIgnore]
        public bool TieneCaracteristicas =>
            !string.IsNullOrWhiteSpace(Caracteristicas);

        [JsonIgnore]
        public bool TieneSintomas =>
            !string.IsNullOrWhiteSpace(Sintomas);

        [JsonIgnore]
        public bool TieneCausas =>
            !string.IsNullOrWhiteSpace(Causas);

        [JsonIgnore]
        public bool TieneRecomendaciones =>
            !string.IsNullOrWhiteSpace(Recomendaciones);

        [JsonIgnore]
        public bool TieneObservaciones =>
            !string.IsNullOrWhiteSpace(Observaciones);

        [JsonIgnore]
        public bool TieneFotos => Fotos.Count > 0;

        [JsonIgnore]
        public bool SinFotos => !TieneFotos;

        [JsonIgnore]
        public bool Inactivo => !Activo;

        [JsonIgnore]
        public string EstadoTexto =>
            Activo ? "Activo" : "Inactivo";

        [JsonIgnore]
        public string EstadoColor =>
            Activo ? "#3B655B" : "#9B552C";

        [JsonIgnore]
        public string AccionEstadoTexto =>
            Activo ? "Desactivar" : "Activar";
    }

    public sealed class AlbumFotoResponse :
        INotifyPropertyChanged
    {
        private string? descripcionFoto;
        private int orden;
        private bool esPortada;

        public int AlbumBotanicoCafeFotoId { get; set; }
        public string RutaFoto { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }

        public string? DescripcionFoto
        {
            get => descripcionFoto;
            set
            {
                if (descripcionFoto == value)
                    return;

                descripcionFoto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DescripcionMostrar));
            }
        }

        public bool EsPortada
        {
            get => esPortada;
            set
            {
                if (esPortada == value)
                    return;

                esPortada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PortadaTexto));
            }
        }

        public int Orden
        {
            get => orden;
            set
            {
                if (orden == value)
                    return;

                orden = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OrdenTexto));
            }
        }

        [JsonIgnore]
        public string OrdenTexto
        {
            get => Orden.ToString();
            set
            {
                if (int.TryParse(value, out int nuevoOrden))
                    Orden = nuevoOrden;
            }
        }

        [JsonIgnore]
        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionFoto)
                ? "Sin descripción"
                : DescripcionFoto;

        [JsonIgnore]
        public string PortadaTexto =>
            EsPortada ? "Portada actual" : "Establecer portada";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }

    public sealed class AlbumRegistroRequest
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Caracteristicas { get; set; }
        public string? Sintomas { get; set; }
        public string? Causas { get; set; }
        public string? Recomendaciones { get; set; }
        public string? Observaciones { get; set; }

        public AlbumRegistroRequest()
        {
        }

        public AlbumRegistroRequest(AlbumDetalleResponse response)
        {
            AlbumBotanicoCafeId =
                response.AlbumBotanicoCafeId;
            CategoriaAlbumBotanicoId =
                response.CategoriaAlbumBotanicoId;
            Titulo = response.Titulo;
            NombreCientifico = response.NombreCientifico;
            Descripcion = response.Descripcion;
            Caracteristicas = response.Caracteristicas;
            Sintomas = response.Sintomas;
            Causas = response.Causas;
            Recomendaciones = response.Recomendaciones;
            Observaciones = response.Observaciones;
        }
    }

    public sealed class ActualizarFotoAlbumRequest
    {
        public string? DescripcionFoto { get; set; }
        public int Orden { get; set; }
    }

    public sealed class CategoriaCreadaData
    {
        public int CategoriaAlbumBotanicoId { get; set; }
    }

    public sealed class RegistroAlbumCreadoData
    {
        public int AlbumBotanicoCafeId { get; set; }
    }

    public sealed class FotoAlbumCreadaData
    {
        public int AlbumBotanicoCafeFotoId { get; set; }
        public string RutaFoto { get; set; } = string.Empty;
    }

    public sealed class PortadaCategoriaData
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public string RutaImagenPortada { get; set; } = string.Empty;
    }
}
