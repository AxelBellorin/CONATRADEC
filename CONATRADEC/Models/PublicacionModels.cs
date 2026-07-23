using Microsoft.Maui.Graphics;

namespace CONATRADEC.Models
{
    public sealed class CategoriaPublicacionResponse
    {
        public int CategoriaPublicacionId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B655B";
        public int Orden { get; set; }

        public Color Color => ObtenerColor(ColorHex);

        public static CategoriaPublicacionResponse Todas() =>
            new()
            {
                CategoriaPublicacionId = 0,
                Nombre = "Todas",
                Descripcion = "Todas las categorías",
                ColorHex = "#3B655B",
                Orden = 0
            };

        private static Color ObtenerColor(string? value)
        {
            try
            {
                return Color.FromArgb(
                    string.IsNullOrWhiteSpace(value)
                        ? "#3B655B"
                        : value);
            }
            catch
            {
                return Color.FromArgb("#3B655B");
            }
        }
    }

    public class PublicacionListadoResponse
    {
        public int PublicacionId { get; set; }
        public int CategoriaPublicacionId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string ColorCategoria { get; set; } = "#3B655B";
        public string Titulo { get; set; } = string.Empty;
        public string Resumen { get; set; } = string.Empty;
        public string RutaImagenPortada { get; set; } = string.Empty;
        public string ImagenPortadaUrl { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTime? FechaEventoInicioUtc { get; set; }
        public DateTime? FechaEventoFinUtc { get; set; }
        public DateTime FechaInicioPublicacionUtc { get; set; }
        public DateTime? FechaFinPublicacionUtc { get; set; }
        public string EstadoPublicacion { get; set; } = string.Empty;
        public string EstadoVisual { get; set; } = string.Empty;
        public bool Destacada { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime FechaUltimaModificacionUtc { get; set; }
        public int UsuarioCreacionId { get; set; }
        public int UsuarioUltimaModificacionId { get; set; }
        public string Autor { get; set; } = string.Empty;
        public string UltimoEditor { get; set; } = string.Empty;

        public bool TieneImagen =>
            !string.IsNullOrWhiteSpace(ImagenPortadaUrl);

        public bool TieneUbicacion =>
            !string.IsNullOrWhiteSpace(Ubicacion);

        public bool TieneEvento => FechaEventoInicioUtc.HasValue;

        public bool TieneAutor =>
            !string.IsNullOrWhiteSpace(Autor);

        public Color CategoriaColor
        {
            get
            {
                try
                {
                    return Color.FromArgb(
                        string.IsNullOrWhiteSpace(ColorCategoria)
                            ? "#3B655B"
                            : ColorCategoria);
                }
                catch
                {
                    return Color.FromArgb("#3B655B");
                }
            }
        }

        public string FechaPublicacionTexto =>
            FechaInicioPublicacionUtc == default
                ? string.Empty
                : $"Publicado el {AFechaLocal(FechaInicioPublicacionUtc):dd/MM/yyyy}";

        public string FechaEventoTexto
        {
            get
            {
                if (!FechaEventoInicioUtc.HasValue)
                    return string.Empty;

                DateTime inicio = AFechaLocal(
                    FechaEventoInicioUtc.Value);

                if (!FechaEventoFinUtc.HasValue)
                    return $"Evento: {inicio:dd/MM/yyyy h:mm tt}";

                DateTime fin = AFechaLocal(
                    FechaEventoFinUtc.Value);

                return inicio.Date == fin.Date
                    ? $"Evento: {inicio:dd/MM/yyyy h:mm tt} - {fin:h:mm tt}"
                    : $"Evento: {inicio:dd/MM/yyyy h:mm tt} - {fin:dd/MM/yyyy h:mm tt}";
            }
        }

        public string EstadoVisualTexto =>
            string.IsNullOrWhiteSpace(EstadoVisual)
                ? EstadoPublicacion
                : EstadoVisual;

        public string AccionEstadoTexto =>
            string.Equals(
                EstadoPublicacion,
                "PUBLICADA",
                StringComparison.OrdinalIgnoreCase)
                ? "Archivar"
                : "Publicar";

        public string AccionDestacadaTexto =>
            Destacada ? "Quitar destacado" : "Destacar";

        protected static DateTime AFechaLocal(DateTime value)
        {
            DateTime utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return utc.ToLocalTime();
        }
    }

    public sealed class PublicacionDetalleResponse :
        PublicacionListadoResponse
    {
        public string Contenido { get; set; } = string.Empty;
        public string EnlaceExterno { get; set; } = string.Empty;
        public string TextoEnlace { get; set; } = string.Empty;
        public bool Activo { get; set; }

        public bool TieneEnlace =>
            !string.IsNullOrWhiteSpace(EnlaceExterno);

        public string TextoBotonEnlace =>
            string.IsNullOrWhiteSpace(TextoEnlace)
                ? "Abrir enlace"
                : TextoEnlace;

        public string VigenciaTexto
        {
            get
            {
                if (!FechaFinPublicacionUtc.HasValue)
                    return "Sin fecha de vencimiento";

                return $"Disponible hasta el " +
                       $"{AFechaLocal(FechaFinPublicacionUtc.Value):dd/MM/yyyy h:mm tt}";
            }
        }
    }

    public sealed class PublicacionPaginadaResponse
    {
        public List<PublicacionListadoResponse> Items { get; set; } = new();
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
    }

    public sealed class PublicacionGuardarRequest
    {
        public int PublicacionId { get; set; }
        public int CategoriaPublicacionId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Resumen { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string EnlaceExterno { get; set; } = string.Empty;
        public string TextoEnlace { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTimeOffset? FechaEventoInicio { get; set; }
        public DateTimeOffset? FechaEventoFin { get; set; }
        public DateTimeOffset FechaInicioPublicacion { get; set; }
        public DateTimeOffset? FechaFinPublicacion { get; set; }
        public string EstadoPublicacion { get; set; } = "BORRADOR";
        public bool Destacada { get; set; }
    }

    public sealed class PublicacionCreadaResponse
    {
        public int PublicacionId { get; set; }
    }

    public sealed class PortadaPublicacionResponse
    {
        public int PublicacionId { get; set; }
        public string RutaImagenPortada { get; set; } = string.Empty;
    }
}
