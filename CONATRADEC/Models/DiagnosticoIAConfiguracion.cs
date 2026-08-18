using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class DiagnosticoIAConfiguracion
    {
        public int MaximoRevisionesGemini { get; set; } = 2;
        public bool RevisionesIlimitadas { get; set; }
        public DateTime FechaModificacionUtc { get; set; }
        public int? UsuarioModificacionId { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
        public string RowVersion { get; set; } = string.Empty;
        public List<DiagnosticoIAConfiguracionHistorialItem>
            Historial { get; set; } = [];

        [JsonIgnore]
        public string Resumen => RevisionesIlimitadas
            ? "Las revisiones adicionales de Gemini no tienen límite."
            : $"Cada diagnóstico permite hasta {MaximoRevisionesGemini} revisiones adicionales de Gemini.";

        [JsonIgnore]
        public string FechaModificacionTexto =>
            FechaModificacionUtc == default
                ? "Sin modificaciones registradas"
                : FechaModificacionUtc
                    .ToLocalTime()
                    .ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class DiagnosticoIAConfiguracionHistorialItem
    {
        public int DiagnosticoIAConfiguracionHistorialId { get; set; }
        public int MaximoAnterior { get; set; }
        public bool IlimitadasAnterior { get; set; }
        public int MaximoNuevo { get; set; }
        public bool IlimitadasNuevo { get; set; }
        public int UsuarioId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }

        [JsonIgnore]
        public string ValorAnteriorTexto => IlimitadasAnterior
            ? "Ilimitadas"
            : $"{MaximoAnterior} revisiones";

        [JsonIgnore]
        public string ValorNuevoTexto => IlimitadasNuevo
            ? "Ilimitadas"
            : $"{MaximoNuevo} revisiones";

        [JsonIgnore]
        public string CambioTexto =>
            $"{ValorAnteriorTexto} → {ValorNuevoTexto}";

        [JsonIgnore]
        public string UsuarioTexto =>
            $"Modificado por: {Usuario}";

        [JsonIgnore]
        public string FechaTexto =>
            FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class DiagnosticoIAConfiguracionActualizarRequest
    {
        public int MaximoRevisionesGemini { get; set; } = 2;
        public bool RevisionesIlimitadas { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
