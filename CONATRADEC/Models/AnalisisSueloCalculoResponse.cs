using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class AnalisisSueloCalculoResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public AnalisisSueloCalculoDataResponse? Data { get; set; }
    }

    public class AnalisisSueloCalculoDataResponse
    {
        public int? TerrenoId { get; set; }

        public int? TipoCultivoId { get; set; }

        public string? TipoCultivo { get; set; }

        public int? TipoAnalisisSueloId { get; set; }

        public string? TipoAnalisisSuelo { get; set; }

        public decimal? CantidadQuintalesOro { get; set; }

        public decimal? TamanoFinca { get; set; }

        public decimal? Ph { get; set; }

        public decimal? AcidezTotal { get; set; }

        public List<ElementoResultadoCalculoResponse> Elementos { get; set; } = new();

        public List<object> FuentesFertilizantes { get; set; } = new();

        public object? EnmiendaCalcarea { get; set; }

        public List<object> FuentesOrganicas { get; set; } = new();

        public string? RecomendacionGeneral { get; set; }

        public List<string> Observaciones { get; set; } = new();
    }

    /// <summary>
    /// Resultado de un elemento químico expresado finalmente en lb/Mz.
    ///
    /// IncluirEnCalculosComplementarios controla únicamente si el elemento
    /// participa en Balance de fórmula y Fertilización mixta. El elemento
    /// siempre permanece dentro del requerimiento anual y del historial.
    /// </summary>
    public class ElementoResultadoCalculoResponse : INotifyPropertyChanged
    {
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;
        private string? clasificacion;

        private bool incluirEnCalculosComplementarios = true;
        private bool inclusionDefinidaPorRespuesta;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int? ElementoQuimicosId { get; set; }

        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set
            {
                simboloElementoQuimico = LimpiarTexto(value);
                OnPropertyChanged();
            }
        }

        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set
            {
                nombreElementoQuimico = LimpiarTexto(value);
                OnPropertyChanged();
            }
        }

        public decimal? CantidadIngresada { get; set; }

        public decimal? CantidadConvertidaLbMz { get; set; }

        public decimal? ExtraccionPorQQOro { get; set; }

        public decimal? ExtraccionPorProduccion { get; set; }

        public decimal? RangoMinimo { get; set; }

        public decimal? RangoMaximo { get; set; }

        public decimal? RangoMinimoLbMz { get; set; }

        public decimal? RangoMaximoLbMz { get; set; }

        public decimal? RequerimientoCalculado { get; set; }

        public string? UnidadBase { get; set; }

        public int? UnidadMedidaResultadoId { get; set; }

        public string? UnidadResultado { get; set; }

        public string? Clasificacion
        {
            get => clasificacion;
            set
            {
                clasificacion = LimpiarTexto(value);

                /*
                 * Compatibilidad con respuestas anteriores de la API:
                 * cuando todavía no venga la bandera persistida, los
                 * elementos EXCESIVO comienzan excluidos por defecto.
                 */
                if (!inclusionDefinidaPorRespuesta)
                {
                    incluirEnCalculosComplementarios =
                        !EsClasificacionExcesiva(clasificacion);

                    OnPropertyChanged(
                        nameof(IncluirEnCalculosComplementarios));
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(EsExcesivo));
            }
        }

        public string? Observacion { get; set; }

        [JsonPropertyName("incluirCalculosComplementarios")]
        public bool IncluirEnCalculosComplementarios
        {
            get => incluirEnCalculosComplementarios;
            set
            {
                inclusionDefinidaPorRespuesta = true;

                if (incluirEnCalculosComplementarios == value)
                    return;

                incluirEnCalculosComplementarios = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool EsExcesivo =>
            EsClasificacionExcesiva(Clasificacion);

        private static bool EsClasificacionExcesiva(
            string? valor)
        {
            return string.Equals(
                valor?.Trim(),
                "EXCESIVO",
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static string? LimpiarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
