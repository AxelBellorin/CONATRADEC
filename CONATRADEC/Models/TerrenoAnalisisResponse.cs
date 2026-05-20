namespace CONATRADEC.Models
{
    public class TerrenoAnalisisResponse
    {
        private int? terrenoId;
        private int? usuarioId;
        private string? codigoTerreno;
        private string? nombreTerreno;
        private string? nombreCliente;
        private decimal? cantidadQuintalesOro;
        private decimal? tamanoFinca;

        public int? TerrenoId
        {
            get => terrenoId;
            set => terrenoId = value;
        }

        public int? UsuarioId
        {
            get => usuarioId;
            set => usuarioId = value;
        }

        public string? CodigoTerreno
        {
            get => codigoTerreno;
            set => codigoTerreno = value;
        }

        public string? NombreTerreno
        {
            get => nombreTerreno;
            set => nombreTerreno = value;
        }

        public string? NombreCliente
        {
            get => nombreCliente;
            set => nombreCliente = value;
        }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set => cantidadQuintalesOro = value;
        }

        public decimal? TamanoFinca
        {
            get => tamanoFinca;
            set => tamanoFinca = value;
        }

        public TerrenoAnalisisResponse() { }
    }
}