using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Representación común de un registro eliminado lógicamente.
    /// Permite utilizar una sola ventana para todos los catálogos.
    /// </summary>
    public sealed class CatalogoEliminadoItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("catalogo")]
        public string Catalogo { get; set; } = string.Empty;

        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [JsonPropertyName("subtitulo")]
        public string Subtitulo { get; set; } = string.Empty;

        [JsonPropertyName("detalle")]
        public string Detalle { get; set; } = string.Empty;

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonIgnore]
        public bool TieneSubtitulo =>
            !string.IsNullOrWhiteSpace(Subtitulo);

        [JsonIgnore]
        public bool TieneDetalle =>
            !string.IsNullOrWhiteSpace(Detalle);

        [JsonIgnore]
        public bool TieneCodigo =>
            !string.IsNullOrWhiteSpace(Codigo);

        [JsonIgnore]
        public string Iniciales
        {
            get
            {
                string[] partes = (Titulo ?? string.Empty)
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

                if (partes.Length == 0)
                    return "ER";

                if (partes.Length == 1)
                {
                    string valor = partes[0].ToUpperInvariant();
                    return valor.Length <= 2
                        ? valor
                        : valor[..2];
                }

                return string.Concat(
                    partes[0][0],
                    partes[1][0])
                    .ToUpperInvariant();
            }
        }
    }

    public sealed class CatalogoEliminadoEnvelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<CatalogoEliminadoItem> Data { get; set; } = new();
    }

    public sealed class CatalogoConflictoData
    {
        [JsonPropertyName("registro")]
        public CatalogoEliminadoItem? Registro { get; set; }

        [JsonPropertyName("puedeCrearNuevo")]
        public bool PuedeCrearNuevo { get; set; }
    }

    public sealed class CatalogoConflictoEnvelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public CatalogoConflictoData? Data { get; set; }
    }

public sealed class CatalogoOperacionEnvelope<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
}
