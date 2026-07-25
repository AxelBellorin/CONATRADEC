namespace CONATRADEC.Models
{
    public sealed class MunicipioResponse
    {
        public int? MunicipioId { get; set; }
        public string NombreMunicipio { get; set; } = string.Empty;
        public int? DepartamentoId { get; set; }
        public string NombreDepartamento { get; set; } = string.Empty;
        public int? PaisId { get; set; }
        public string NombrePais { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int CantidadTerrenos { get; set; }
        public int CantidadUsuarios { get; set; }

        public string ResumenTerrenos =>
            CantidadTerrenos == 1
                ? "1 terreno"
                : $"{CantidadTerrenos} terrenos";

        public string ResumenUsuarios =>
            CantidadUsuarios == 1
                ? "1 usuario"
                : $"{CantidadUsuarios} usuarios";
    }
}
