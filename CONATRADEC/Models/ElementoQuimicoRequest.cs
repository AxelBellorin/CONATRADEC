using System;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Modelo de solicitud para Elemento Químico.
    /// Representa los datos que se envían a la API para crear/editar.
    /// </summary>
    public class ElementoQuimicoRequest
    {
        // Campos privados
        private int? elementoQuimicosId;
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;
        private decimal? pesoEquivalentElementoQuimico; // decimal(10,4)

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

        [JsonPropertyName("pesoEquivalentEelementoQuimico")]
        public decimal? PesoEquivalentElementoQuimico
        {
            get => pesoEquivalentElementoQuimico;
            set => pesoEquivalentElementoQuimico = value;
        }

        public ElementoQuimicoRequest() { }

        public ElementoQuimicoRequest(ElementoQuimicoResponse elementoRP)
        {
            ElementoQuimicosId = elementoRP.ElementoQuimicosId;
            SimboloElementoQuimico = elementoRP.SimboloElementoQuimico;
            NombreElementoQuimico = elementoRP.NombreElementoQuimico;
            PesoEquivalentElementoQuimico = elementoRP.PesoEquivalentElementoQuimico;
        }
    }
}
