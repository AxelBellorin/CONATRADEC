using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class GuardarTodoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("data")]
        public GuardarTodoResponseData? Data { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("inner")]
        public string? Inner { get; set; }

        [JsonPropertyName("versionActual")]
        public int? VersionActual { get; set; }

        [JsonPropertyName("etagActual")]
        public string? ETagActual { get; set; }

        [JsonPropertyName("fechaUltimaModificacionUtc")]
        public DateTime? FechaUltimaModificacionUtc { get; set; }

        /// <summary>
        /// Código HTTP recibido. Es información local del cliente y no forma
        /// parte del contrato JSON que se envía al servidor.
        /// </summary>
        [JsonIgnore]
        public int? StatusCode { get; set; }
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
