using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Convierte las rutas completas de las imágenes del álbum en rutas de
    /// miniatura. La imagen original se conserva internamente para abrir el
    /// visor de pantalla completa.
    /// </summary>
    internal static class AlbumMiniaturaUrlHelper
    {
        public static string? Crear(
            string? urlOriginal,
            int ancho,
            int alto,
            int calidad)
        {
            if (string.IsNullOrWhiteSpace(urlOriginal))
                return urlOriginal;

            if (!Uri.TryCreate(
                    urlOriginal,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return urlOriginal;
            }

            if (uri.AbsolutePath.StartsWith(
                    "/imagenes/miniatura",
                    StringComparison.OrdinalIgnoreCase))
            {
                return urlOriginal;
            }

            if (!uri.AbsolutePath.StartsWith(
                    "/resources/uploads/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return urlOriginal;
            }

            string autoridad =
                uri.GetLeftPart(UriPartial.Authority)
                    .TrimEnd('/');

            string ruta = Uri.EscapeDataString(
                uri.AbsolutePath);

            return
                $"{autoridad}/imagenes/miniatura" +
                $"?ruta={ruta}" +
                $"&ancho={ancho}" +
                $"&alto={alto}" +
                $"&calidad={calidad}";
        }
    }

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
        private string? imagenPortadaUrlOriginal;

        public int CategoriaAlbumBotanicoId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? RutaImagenPortada { get; set; }

        /// <summary>
        /// En las tarjetas se utiliza una miniatura de 420 x 260.
        /// La URL completa recibida desde la API queda almacenada en el
        /// campo privado para no perder la referencia original.
        /// </summary>
        public string? ImagenPortadaUrl
        {
            get => AlbumMiniaturaUrlHelper.Crear(
                imagenPortadaUrlOriginal,
                420,
                260,
                65);
            set => imagenPortadaUrlOriginal = value;
        }

        [JsonIgnore]
        public string? ImagenPortadaOriginalUrl =>
            imagenPortadaUrlOriginal;

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

            /*
             * Para el formulario basta la miniatura. Así editar una
             * categoría no vuelve a descargar la portada completa.
             */
            RutaImagenPortadaActual =
                response.ImagenPortadaUrl;
        }
    }

    public sealed class AlbumGaleriaItemResponse
    {
        private string? fotoPortadaUrlOriginal;

        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? NombreCientifico { get; set; }
        public string DescripcionCorta { get; set; } = string.Empty;
        public string? FotoPortada { get; set; }

        /// <summary>
        /// La galería utiliza una miniatura de 720 x 480 en lugar de la
        /// fotografía original de hasta 1600 x 1600.
        /// </summary>
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
            fotoPortadaUrlOriginal;

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
        private string? fotoUrlOriginal;

        public int AlbumBotanicoCafeFotoId { get; set; }
        public string RutaFoto { get; set; } = string.Empty;

        /// <summary>
        /// Las cuadrículas de detalle y administración usan la miniatura.
        /// El visor usa FotoOriginalUrl para descargar la imagen completa
        /// únicamente cuando el usuario decide abrirla.
        /// </summary>
        public string? FotoUrl
        {
            get => AlbumMiniaturaUrlHelper.Crear(
                fotoUrlOriginal,
                720,
                480,
                68);
            set => fotoUrlOriginal = value;
        }

        [JsonIgnore]
        public string? FotoOriginalUrl =>
            fotoUrlOriginal;

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
            EsPortada
                ? "Portada actual"
                : "Establecer portada";

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

        public AlbumRegistroRequest(
            AlbumDetalleResponse response)
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
