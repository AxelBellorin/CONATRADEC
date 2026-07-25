using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class DepartamentoRequest
    {
        [JsonIgnore]
        public int? DepartamentoId { get; set; }

        public string NombreDepartamento { get; set; } =
            string.Empty;

        public int? PaisId { get; set; }

        public DepartamentoRequest()
        {
        }

        public DepartamentoRequest(
            DepartamentoResponse departamento)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            DepartamentoId =
                departamento.DepartamentoId;

            NombreDepartamento =
                departamento.NombreDepartamento ??
                string.Empty;

            PaisId =
                departamento.PaisId;
        }
    }
}
