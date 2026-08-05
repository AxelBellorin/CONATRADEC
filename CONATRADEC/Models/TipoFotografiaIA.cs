using Microsoft.Maui.Graphics;

namespace CONATRADEC.Models
{
    public sealed class TipoFotografiaIAItem
    {
        public int TipoFotografiaIAId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string InstruccionIA { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime FechaModificacionUtc { get; set; }

        public bool Inactivo => !Activo;
        public bool PuedeDesactivarse =>
            Activo &&
            !string.Equals(
                Codigo,
                "EVIDENCIA",
                StringComparison.OrdinalIgnoreCase);

        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public Color EstadoColor => Activo
            ? Color.FromArgb("#3B655B")
            : Color.FromArgb("#B42318");

        public string NombreMostrar => string.IsNullOrWhiteSpace(Nombre)
            ? Codigo
            : Nombre;

        public string CodigoTexto => $"Código: {Codigo}";
        public string OrdenTexto => $"Orden: {Orden}";
        public string InstruccionVisible =>
            $"La IA priorizará: {InstruccionIA}";

        public override string ToString() => NombreMostrar;
    }

    public sealed class TipoFotografiaIARequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string InstruccionIA { get; set; } = string.Empty;
        public int Orden { get; set; } = 1;
    }
}
