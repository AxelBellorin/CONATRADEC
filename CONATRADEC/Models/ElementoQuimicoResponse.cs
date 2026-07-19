using System;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Modelo de respuesta para Elemento Químico.
    /// Representa lo que devuelve la API al listar o consultar elementos químicos.
    /// </summary>
    public class ElementoQuimicoResponse
    {
        private int? elementoQuimicosId;
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;
        private decimal? pesoEquivalenteElementoQuimico;

        public int? ElementoQuimicosId
        {
            get => elementoQuimicosId;
            set => elementoQuimicosId = value;
        }

        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set => simboloElementoQuimico = LimpiarTexto(value);
        }

        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set => nombreElementoQuimico = LimpiarTexto(value);
        }

        /// <summary>
        /// Peso equivalente del elemento químico.
        /// </summary>
        [JsonPropertyName("pesoEquivalenteElementoQuimico")]
        public decimal? PesoEquivalenteElementoQuimico
        {
            get => pesoEquivalenteElementoQuimico;
            set => pesoEquivalenteElementoQuimico = value;
        }

        public ElementoQuimicoResponse()
        {
        }

        private static string? LimpiarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }
    }
}
