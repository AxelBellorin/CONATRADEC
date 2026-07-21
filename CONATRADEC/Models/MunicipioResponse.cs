namespace CONATRADEC.Models
{
    public class MunicipioResponse
    {
        public int? MunicipioId { get; set; }
        public string? NombreMunicipio { get; set; }
        public int? DepartamentoId { get; set; }
        public string? NombreDepartamento { get; set; }
        public int? PaisId { get; set; }
        public string? NombrePais { get; set; }
        public bool Activo { get; set; }
    }
}
