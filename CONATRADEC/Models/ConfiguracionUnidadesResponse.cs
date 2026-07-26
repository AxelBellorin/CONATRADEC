using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Contrato común utilizado por los endpoints de configuración
    /// de unidades y conversiones.
    /// </summary>
    public sealed class ConfiguracionUnidadesApiEnvelope<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } =
            string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public sealed class ConfiguracionFormularioAnalisisResponse
    {
        [JsonPropertyName("unidadResultadoId")]
        public int UnidadResultadoId { get; set; }

        [JsonPropertyName("unidadResultado")]
        public string UnidadResultado { get; set; } =
            "lb/Mz";

        [JsonPropertyName("unidadesMateriaOrganica")]
        public List<UnidadConversionConfiguradaResponse>
            UnidadesMateriaOrganica { get; set; } = new();

        [JsonPropertyName("elementos")]
        public List<ElementoConfiguracionUnidadesResponse>
            Elementos { get; set; } = new();
    }

    public sealed class ElementoConfiguracionUnidadesResponse
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string SimboloElementoQuimico { get; set; } =
            string.Empty;

        [JsonPropertyName("nombreElementoQuimico")]
        public string NombreElementoQuimico { get; set; } =
            string.Empty;

        [JsonPropertyName("pesoEquivalenteElementoQuimico")]
        public decimal PesoEquivalenteElementoQuimico { get; set; }

        [JsonPropertyName("unidadPredeterminadaId")]
        public int? UnidadPredeterminadaId { get; set; }

        [JsonPropertyName("unidades")]
        public List<UnidadConversionConfiguradaResponse>
            Unidades { get; set; } = new();

        [JsonIgnore]
        public string NombreMostrar
        {
            get
            {
                string nombre =
                    NombreElementoQuimico.Trim();

                string simbolo =
                    SimboloElementoQuimico.Trim();

                return string.IsNullOrWhiteSpace(simbolo)
                    ? nombre
                    : $"{nombre} ({simbolo})";
            }
        }

        public override string ToString() =>
            NombreMostrar;
    }

    public sealed class UnidadConversionConfiguradaResponse
    {
        [JsonPropertyName("configuracionId")]
        public int ConfiguracionId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("nombreUnidadMedida")]
        public string NombreUnidadMedida { get; set; } =
            string.Empty;

        [JsonPropertyName("codigoFormulaConversion")]
        public string CodigoFormulaConversion { get; set; } =
            "LINEAL";

        [JsonPropertyName("factorPrincipal")]
        public decimal FactorPrincipal { get; set; } = 1m;

        [JsonPropertyName("factorSecundario")]
        public decimal FactorSecundario { get; set; } = 1m;

        [JsonPropertyName("factorTerciario")]
        public decimal FactorTerciario { get; set; } = 1m;

        [JsonPropertyName("divisor")]
        public decimal Divisor { get; set; } = 1m;

        [JsonPropertyName("desplazamiento")]
        public decimal Desplazamiento { get; set; }

        [JsonPropertyName("unidadPredeterminada")]
        public bool UnidadPredeterminada { get; set; }

        [JsonPropertyName("visibleEnFormulario")]
        public bool VisibleEnFormulario { get; set; } = true;

        [JsonPropertyName("orden")]
        public int Orden { get; set; }

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } =
            string.Empty;

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonIgnore]
        public string NombreMostrar =>
            NombreUnidadMedida.Trim();

        public override string ToString() =>
            NombreMostrar;
    }

    public sealed class FormulaConversionDisponibleResponse
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } =
            string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } =
            string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } =
            string.Empty;

        [JsonPropertyName("requiereElementoQuimico")]
        public bool RequiereElementoQuimico { get; set; }

        [JsonPropertyName("requiereMateriaOrganica")]
        public bool RequiereMateriaOrganica { get; set; }

        [JsonIgnore]
        public string NombreMostrar
        {
            get
            {
                string nombre =
                    Nombre.Trim();

                string codigo =
                    Codigo.Trim();

                return string.IsNullOrWhiteSpace(nombre)
                    ? codigo
                    : $"{nombre} ({codigo})";
            }
        }

        public override string ToString() =>
            NombreMostrar;
    }

    public sealed class UnidadMedidaCatalogoConfiguracionResponse
    {
        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("nombreUnidadMedida")]
        public string NombreUnidadMedida { get; set; } =
            string.Empty;

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonIgnore]
        public string NombreMostrar =>
            NombreUnidadMedida.Trim();

        public override string ToString() =>
            NombreMostrar;
    }

    public sealed class GuardarConfiguracionElementoUnidadesRequest
    {
        [JsonPropertyName("unidades")]
        public List<GuardarUnidadConversionRequest>
            Unidades { get; set; } = new();
    }

    public sealed class GuardarConfiguracionMateriaOrganicaRequest
    {
        [JsonPropertyName("unidades")]
        public List<GuardarUnidadConversionRequest>
            Unidades { get; set; } = new();
    }

    public sealed class GuardarUnidadConversionRequest
    {
        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("codigoFormulaConversion")]
        public string CodigoFormulaConversion { get; set; } =
            "LINEAL";

        [JsonPropertyName("factorPrincipal")]
        public decimal FactorPrincipal { get; set; } = 1m;

        [JsonPropertyName("factorSecundario")]
        public decimal FactorSecundario { get; set; } = 1m;

        [JsonPropertyName("factorTerciario")]
        public decimal FactorTerciario { get; set; } = 1m;

        [JsonPropertyName("divisor")]
        public decimal Divisor { get; set; } = 1m;

        [JsonPropertyName("desplazamiento")]
        public decimal Desplazamiento { get; set; }

        [JsonPropertyName("unidadPredeterminada")]
        public bool UnidadPredeterminada { get; set; }

        [JsonPropertyName("visibleEnFormulario")]
        public bool VisibleEnFormulario { get; set; } = true;

        [JsonPropertyName("orden")]
        public int Orden { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; } = true;
    }

    public sealed class ProbarConversionUnidadRequest
    {
        [JsonPropertyName("contexto")]
        public string Contexto { get; set; } =
            "ELEMENTO";

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("valorReportado")]
        public decimal ValorReportado { get; set; }

        [JsonPropertyName("materiaOrganicaPorcentaje")]
        public decimal? MateriaOrganicaPorcentaje { get; set; }
    }

    public sealed class ResultadoPruebaConversionResponse
    {
        [JsonPropertyName("contexto")]
        public string Contexto { get; set; } =
            string.Empty;

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("elemento")]
        public string Elemento { get; set; } =
            string.Empty;

        [JsonPropertyName("unidadOrigenId")]
        public int UnidadOrigenId { get; set; }

        [JsonPropertyName("unidadOrigen")]
        public string UnidadOrigen { get; set; } =
            string.Empty;

        [JsonPropertyName("unidadDestinoId")]
        public int UnidadDestinoId { get; set; }

        [JsonPropertyName("unidadDestino")]
        public string UnidadDestino { get; set; } =
            string.Empty;

        [JsonPropertyName("valorReportado")]
        public decimal ValorReportado { get; set; }

        [JsonPropertyName("valorConvertido")]
        public decimal ValorConvertido { get; set; }

        [JsonPropertyName("codigoFormulaConversion")]
        public string CodigoFormulaConversion { get; set; } =
            string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } =
            string.Empty;
    }

    public interface IConfiguracionUnidadesApiResult
    {
        bool Success { get; }

        string Message { get; }
    }

    /// <summary>
    /// Resultado interno del servicio HTTP. Conserva el mensaje exacto
    /// devuelto por la API.
    /// </summary>
    public sealed class ConfiguracionUnidadesApiResult<T> :
        IConfiguracionUnidadesApiResult
    {
        private ConfiguracionUnidadesApiResult(
            bool success,
            string message,
            T? data)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public bool Success { get; }

        public string Message { get; }

        public T? Data { get; }

        public static ConfiguracionUnidadesApiResult<T>
            Ok(
                T? data,
                string message = "") =>
                    new(
                        true,
                        message,
                        data);

        public static ConfiguracionUnidadesApiResult<T>
            Fail(string message) =>
                new(
                    false,
                    message,
                    default);
    }
}
