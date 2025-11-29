using System;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Modelo de respuesta para Elemento Químico.
    /// Representa lo que devuelve la API al listar/consultar elementos químicos.
    /// </summary>
    public class ElementoQuimicoResponse
    {
        // Campos privados
        private int? elementoQuimicosId;
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;
        private decimal? pesoEquivalentElementoQuimico;

        // Propiedades públicas
        public int? ElementoQuimicosId
        {
            get => elementoQuimicosId;
            set => elementoQuimicosId = value;
        }

        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set => simboloElementoQuimico = value;
        }

        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set => nombreElementoQuimico = value;
        }

        /// <summary>
        /// Peso equivalente del elemento químico (decimal(10,4) en la base de datos).
        /// </summary>
        /// 
        [JsonPropertyName("pesoEquivalentEelementoQuimico")]
        public decimal? PesoEquivalentElementoQuimico
        {
            get => pesoEquivalentElementoQuimico;
            set => pesoEquivalentElementoQuimico = value;
        }

        public ElementoQuimicoResponse() { }
    }
}
