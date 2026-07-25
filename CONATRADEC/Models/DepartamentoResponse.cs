namespace CONATRADEC.Models
{
    public sealed class DepartamentoResponse
    {
        public int? DepartamentoId { get; set; }

        public string NombreDepartamento { get; set; } =
            string.Empty;

        public int? PaisId { get; set; }

        public string NombrePais { get; set; } =
            string.Empty;

        public bool Activo { get; set; }

        public int CantidadMunicipios { get; set; }

        public string ResumenMunicipios =>
            CantidadMunicipios == 1
                ? "1 municipio registrado"
                : $"{CantidadMunicipios} municipios registrados";
    }
}
