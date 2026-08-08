using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        private decimal? cantidadQuintalesOro;

        private List<ElementoResultadoCalculoResponse> elementos = new();

        public int? TerrenoId { get; set; }

        public int? TipoCultivoId { get; set; }

        public string? TipoCultivo { get; set; }

        public int? TipoAnalisisSueloId { get; set; }

        public string? TipoAnalisisSuelo { get; set; }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                cantidadQuintalesOro = value;
                ActualizarContextoElementos();
            }
        }

        public decimal? TamanoFinca { get; set; }

        public decimal? Ph { get; set; }

        public decimal? AcidezTotal { get; set; }

        public List<ElementoResultadoCalculoResponse> Elementos
        {
            get => elementos;
            set
            {
                elementos = value ?? new List<ElementoResultadoCalculoResponse>();
                ActualizarContextoElementos();
            }
        }

        public List<object> FuentesFertilizantes { get; set; } = new();

        public object? EnmiendaCalcarea { get; set; }

        public List<object> FuentesOrganicas { get; set; } = new();

        public string? RecomendacionGeneral { get; set; }

        public List<string> Observaciones { get; set; } = new();

        private void ActualizarContextoElementos()
        {
            foreach (ElementoResultadoCalculoResponse elemento in elementos)
            {
                elemento.CantidadQuintalesOroContexto = cantidadQuintalesOro;
            }
        }
    }

    /// <summary>
    /// Resultado de un elemento químico expresado finalmente en lb/Mz.
    ///
    /// IncluirEnCalculosComplementarios controla únicamente si el elemento
    /// participa en Balance de fórmula y Fertilización mixta. El elemento
    /// siempre permanece dentro del requerimiento anual y del historial.
    ///
    /// También conserva compatibilidad con respuestas antiguas de la API que
    /// incluían el rango convertido únicamente dentro de Observacion.
    /// </summary>
    public class ElementoResultadoCalculoResponse : INotifyPropertyChanged
    {
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;
        private string? clasificacion;
        private string? observacion;

        private decimal? extraccionPorQQOro;
        private decimal? extraccionPorProduccion;
        private decimal? rangoMinimoLbMz;
        private decimal? rangoMaximoLbMz;
        private decimal? requerimientoCalculado;
        private decimal? cantidadQuintalesOroContexto;

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

        public decimal? ExtraccionPorQQOro
        {
            get
            {
                if (extraccionPorQQOro.HasValue)
                    return extraccionPorQQOro;

                decimal? produccion = ExtraccionPorProduccion;

                if (!produccion.HasValue ||
                    !cantidadQuintalesOroContexto.HasValue ||
                    cantidadQuintalesOroContexto.Value <= 0)
                {
                    return null;
                }

                return Math.Round(
                    produccion.Value /
                    cantidadQuintalesOroContexto.Value,
                    4);
            }
            set
            {
                extraccionPorQQOro = value;
                OnPropertyChanged();
            }
        }

        public decimal? ExtraccionPorProduccion
        {
            get
            {
                if (extraccionPorProduccion.HasValue)
                    return extraccionPorProduccion;

                decimal? requerimiento = RequerimientoCalculado;
                decimal? rangoMaximo = RangoMaximoLbMz;

                if (!requerimiento.HasValue ||
                    !rangoMaximo.HasValue)
                {
                    return null;
                }

                decimal valor =
                    requerimiento.Value - rangoMaximo.Value;

                return Math.Round(
                    Math.Max(0m, valor),
                    4);
            }
            set
            {
                extraccionPorProduccion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraccionPorQQOro));
            }
        }

        public decimal? RangoMinimo { get; set; }

        public decimal? RangoMaximo { get; set; }

        public decimal? RangoMinimoLbMz
        {
            get
            {
                if (rangoMinimoLbMz.HasValue)
                    return rangoMinimoLbMz;

                return ObtenerRangoDesdeObservacion().Minimo;
            }
            set
            {
                rangoMinimoLbMz = value;
                OnPropertyChanged();
            }
        }

        public decimal? RangoMaximoLbMz
        {
            get
            {
                if (rangoMaximoLbMz.HasValue)
                    return rangoMaximoLbMz;

                return ObtenerRangoDesdeObservacion().Maximo;
            }
            set
            {
                rangoMaximoLbMz = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraccionPorProduccion));
                OnPropertyChanged(nameof(ExtraccionPorQQOro));
            }
        }

        public decimal? RequerimientoCalculado
        {
            get => requerimientoCalculado;
            set
            {
                requerimientoCalculado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraccionPorProduccion));
                OnPropertyChanged(nameof(ExtraccionPorQQOro));
            }
        }

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

        public string? Observacion
        {
            get => observacion;
            set
            {
                observacion = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(RangoMinimoLbMz));
                OnPropertyChanged(nameof(RangoMaximoLbMz));
                OnPropertyChanged(nameof(ExtraccionPorProduccion));
                OnPropertyChanged(nameof(ExtraccionPorQQOro));
            }
        }

        [JsonIgnore]
        public decimal? CantidadQuintalesOroContexto
        {
            get => cantidadQuintalesOroContexto;
            set
            {
                cantidadQuintalesOroContexto = value;
                OnPropertyChanged(nameof(ExtraccionPorQQOro));
            }
        }

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

        private (decimal? Minimo, decimal? Maximo)
            ObtenerRangoDesdeObservacion()
        {
            if (string.IsNullOrWhiteSpace(Observacion))
                return (null, null);

            const string marcadorInicio =
                "Rango de referencia:";

            int inicio =
                Observacion.IndexOf(
                    marcadorInicio,
                    StringComparison.OrdinalIgnoreCase);

            if (inicio < 0)
                return (null, null);

            inicio += marcadorInicio.Length;

            int fin =
                Observacion.IndexOf(
                    "lb/Mz",
                    inicio,
                    StringComparison.OrdinalIgnoreCase);

            if (fin <= inicio)
                return (null, null);

            string textoRango =
                Observacion
                    .Substring(inicio, fin - inicio)
                    .Trim()
                    .TrimEnd('.');

            string[] partes =
                textoRango.Split(
                    '-',
                    2,
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length != 2 ||
                !TryParseDecimalFlexible(
                    partes[0],
                    out decimal minimo) ||
                !TryParseDecimalFlexible(
                    partes[1],
                    out decimal maximo))
            {
                return (null, null);
            }

            return (minimo, maximo);
        }

        private static bool TryParseDecimalFlexible(
            string texto,
            out decimal valor)
        {
            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out valor))
            {
                return true;
            }

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out valor))
            {
                return true;
            }

            return decimal.TryParse(
                texto.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out valor);
        }

        private static bool EsClasificacionExcesiva(
            string? valor)
        {
            return string.Equals(
                valor?.Trim(),
                "EXCESIVO",
                StringComparison.OrdinalIgnoreCase);
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
