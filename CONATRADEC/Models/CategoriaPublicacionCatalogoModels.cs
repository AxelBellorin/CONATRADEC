using Microsoft.Maui.Graphics;

namespace CONATRADEC.Models
{
    public sealed class CategoriaPublicacionCatalogoResponse
    {
        public int CategoriaPublicacionId { get; set; }
        public string NombreCategoriaPublicacion { get; set; } = string.Empty;
        public string DescripcionCategoriaPublicacion { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B655B";
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public int CantidadPublicaciones { get; set; }

        public bool PuedeDesactivar { get; set; }
        public bool PuedeReactivar { get; set; }

        public string EstadoTexto => Activo ? "Activo" : "Inactivo";

        public Color EstadoFondo => Activo
            ? Color.FromArgb("#EEF5F2")
            : Color.FromArgb("#FEE2E2");

        public Color EstadoTextoColor => Activo
            ? Color.FromArgb("#3B655B")
            : Color.FromArgb("#991B1B");

        public string PublicacionesTexto => CantidadPublicaciones == 1
            ? "1 publicación relacionada"
            : $"{CantidadPublicaciones} publicaciones relacionadas";

        public Color ColorMuestra
        {
            get
            {
                try
                {
                    return Color.FromArgb(
                        string.IsNullOrWhiteSpace(ColorHex)
                            ? "#3B655B"
                            : ColorHex);
                }
                catch
                {
                    return Color.FromArgb("#3B655B");
                }
            }
        }
    }

    public sealed class CategoriaPublicacionGuardarRequest
    {
        public string NombreCategoriaPublicacion { get; set; } = string.Empty;
        public string DescripcionCategoriaPublicacion { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B655B";
        public int Orden { get; set; }
    }

    public sealed class ColorPublicacionOption
    {
        public string Nombre { get; init; } = string.Empty;
        public string Hex { get; init; } = "#3B655B";

        public string Texto => $"{Nombre} ({Hex})";

        public override string ToString() => Texto;
    }
}
