using System;

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
        private decimal? pesoEquivalentEelementoQuimico; // decimal(10,4)

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

        public decimal? PesoEquivalentEelementoQuimico
        {
            get => pesoEquivalentEelementoQuimico;
            set => pesoEquivalentEelementoQuimico = value;
        }

        public ElementoQuimicoRequest() { }

        public ElementoQuimicoRequest(ElementoQuimicoResponse elementoRP)
        {
            ElementoQuimicosId = elementoRP.ElementoQuimicosId;
            SimboloElementoQuimico = elementoRP.SimboloElementoQuimico;
            NombreElementoQuimico = elementoRP.NombreElementoQuimico;
            PesoEquivalentEelementoQuimico = elementoRP.PesoEquivalentEelementoQuimico;
        }
    }
}
