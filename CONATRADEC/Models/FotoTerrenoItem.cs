using Microsoft.Maui.Controls;

namespace CONATRADEC.Models
{
    public class FotoTerrenoItem
    {
        public int? FotoTerrenoId { get; set; }

        public int? TerrenoId { get; set; }

        public string? UrlFotoTerreno { get; set; }

        public string? LocalPath { get; set; }

        public string? NombreArchivo { get; set; }

        public bool EsNueva { get; set; }

        public ImageSource? Imagen { get; set; }
    }
}