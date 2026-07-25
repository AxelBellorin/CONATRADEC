namespace CONATRADEC.Models
{
    public sealed class PaisResponse
    {
        public int PaisId { get; set; }
        public string NombrePais { get; set; } = string.Empty;
        public string CodigoISOPais { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int CantidadDepartamentos { get; set; }

        public string ResumenDepartamentos =>
            CantidadDepartamentos == 1
                ? "1 departamento registrado"
                : $"{CantidadDepartamentos} departamentos registrados";
    }
}
