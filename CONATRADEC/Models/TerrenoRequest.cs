using System;

namespace CONATRADEC.Models
{
    public class TerrenoRequest
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

        public TerrenoRequest() { }

        public TerrenoRequest(TerrenoResponse t)
        {
            TerrenoId = t.TerrenoId;
            CodigoTerreno = t.CodigoTerreno;
            IdentificacionPropietarioTerreno = t.IdentificacionPropietarioTerreno;
            NombrePropietarioTerreno = t.NombrePropietarioTerreno;
            TelefonoPropietario = t.TelefonoPropietario;
            CorreoPropietario = t.CorreoPropietario;
            DireccionTerreno = t.DireccionTerreno;
            ExtensionManzanaTerreno = t.ExtensionManzanaTerreno;
            FechaIngresoTerreno = t.FechaIngresoTerreno;
            MunicipioId = t.MunicipioId;
            CantidadQuintalesOro = t.CantidadQuintalesOro;
            Latitud = t.Latitud;
            Longitud = t.Longitud;
        }
    }
}
