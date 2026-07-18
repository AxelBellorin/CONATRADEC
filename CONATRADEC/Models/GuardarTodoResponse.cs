using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class GuardarTodoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public GuardarTodoResponseData? Data { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("inner")]
        public string? Inner { get; set; }
    }

    public sealed class GuardarTodoResponseData
    {
        [JsonPropertyName("analisisSueloId")]
        public int AnalisisSueloId { get; set; }

        [JsonPropertyName("analisisSueloCalculoId")]
        public int AnalisisSueloCalculoId { get; set; }

        [JsonPropertyName("formulaNutricionalId")]
        public int? FormulaNutricionalId { get; set; }

        [JsonPropertyName("enmiendaCalcareaId")]
        public int? EnmiendaCalcareaId { get; set; }

        [JsonPropertyName("fertilizacionMixtaId")]
        public int? FertilizacionMixtaId { get; set; }
    }

    public sealed class EliminarAnalisisResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("analisisSueloId")]
        public int? AnalisisSueloId { get; set; }

        [JsonPropertyName("calculosDesactivados")]
        public int? CalculosDesactivados { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("innerException")]
        public string? InnerException { get; set; }
    }
}
