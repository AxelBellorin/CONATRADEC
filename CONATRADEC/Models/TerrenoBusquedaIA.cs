using System.Globalization;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class TerrenoBusquedaIAItem
    {
        public int TerrenoId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public string DireccionTerreno { get; set; } = string.Empty;
        public decimal ExtensionManzanaTerreno { get; set; }
        public DateTime FechaIngresoTerreno { get; set; }
        public int CantidadPlantasTerreno { get; set; }
        public decimal CantidadQuintalesOro { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int? PropietarioId { get; set; }
        public string IdentificacionPropietario { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;

        [JsonIgnore]
        public string UbicacionTexto =>
            string.Join(
                ", ",
                new[] { Municipio, Departamento, Pais }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

        [JsonIgnore]
        public string ExtensionTexto =>
            $"{ExtensionManzanaTerreno.ToString("N2", CultureInfo.CurrentCulture)} manzanas";

        [JsonIgnore]
        public string PropietarioTexto =>
            string.IsNullOrWhiteSpace(IdentificacionPropietario)
                ? Propietario
                : $"{Propietario} · {IdentificacionPropietario}";

        [JsonIgnore]
        public string ResumenSeleccion =>
            $"{CodigoTerreno} · {Propietario} · {UbicacionTexto}";
    }

    public sealed class TerrenoBusquedaIAPagina
    {
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int Total { get; set; }
        public int TotalPaginas { get; set; }
        public List<TerrenoBusquedaIAItem> Items { get; set; } = [];
    }

    public sealed class TerrenoBusquedaIAFiltro
    {
        public string Texto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string IdentificacionPropietario { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public decimal? ExtensionMinima { get; set; }
        public decimal? ExtensionMaxima { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 20;
    }
}
