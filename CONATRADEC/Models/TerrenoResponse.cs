using System;

namespace CONATRADEC.Models
{
    public class TerrenoUbicacionResponse
    {
        private int? paisId;
        private string? nombrePais;
        private int? departamentoId;
        private string? nombreDepartamento;
        private int? municipioId;
        private string? nombreMunicipio;

        public int? PaisId { get => paisId; set => paisId = value; }
        public string? NombrePais { get => nombrePais; set => nombrePais = value; }
        public int? DepartamentoId { get => departamentoId; set => departamentoId = value; }
        public string? NombreDepartamento { get => nombreDepartamento; set => nombreDepartamento = value; }
        public int? MunicipioId { get => municipioId; set => municipioId = value; }
        public string? NombreMunicipio { get => nombreMunicipio; set => nombreMunicipio = value; }

        public TerrenoUbicacionResponse() { }
    }

    public class TerrenoResponse
    {
        private int? terrenoId;
        private string? codigoTerreno;
        private string? identificacionPropietarioTerreno;
        private string? nombrePropietarioTerreno;
        private int? telefonoPropietario;
        private string? correoPropietario;
        private string? direccionTerreno;
        private decimal? extensionManzanaTerreno;
        private DateOnly? fechaIngresoTerreno;
        private int? municipioId;
        private decimal? cantidadQuintalesOro;
        private double? latitud;
        private double? longitud;
        private TerrenoUbicacionResponse? ubicacion;

        public int? TerrenoId { get => terrenoId; set => terrenoId = value; }
        public string? CodigoTerreno { get => codigoTerreno; set => codigoTerreno = value; }
        public string? IdentificacionPropietarioTerreno { get => identificacionPropietarioTerreno; set => identificacionPropietarioTerreno = value; }
        public string? NombrePropietarioTerreno { get => nombrePropietarioTerreno; set => nombrePropietarioTerreno = value; }
        public int? TelefonoPropietario { get => telefonoPropietario; set => telefonoPropietario = value; }
        public string? CorreoPropietario { get => correoPropietario; set => correoPropietario = value; }
        public string? DireccionTerreno { get => direccionTerreno; set => direccionTerreno = value; }
        public decimal? ExtensionManzanaTerreno { get => extensionManzanaTerreno; set => extensionManzanaTerreno = value; }
        public DateOnly? FechaIngresoTerreno { get => fechaIngresoTerreno; set => fechaIngresoTerreno = value; }
        public int? MunicipioId { get => municipioId; set => municipioId = value; }
        public decimal? CantidadQuintalesOro { get => cantidadQuintalesOro; set => cantidadQuintalesOro = value; }
        public double? Latitud { get => latitud; set => latitud = value; }
        public double? Longitud { get => longitud; set => longitud = value; }
        public TerrenoUbicacionResponse? Ubicacion { get => ubicacion; set => ubicacion = value; }

        public TerrenoResponse() { }
    }
}
