using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: NuevoAnalisisRequest
    // Descripción:
    //   Representa la solicitud para registrar un nuevo análisis de suelo.
    // ===============================================================
    public class NuevoAnalisisRequest
    {
        private int? analisisId;
        private int? usuarioId;
        private int? terrenoId;

        private string? nombreCliente;
        private string? codigoTerreno;
        private string? nombreTerreno;

        private string? tipoCultivo;
        private string? tipoAnalisisSuelo;
        private DateTime? fechaAnalisisLaboratorio;

        private string? laboratorio;
        private string? identificadorAnalisisSuelo;

        private decimal? cantidadQuintalesOro;
        private decimal? tamanoFinca;

        private string? tipoMuestra;

        private List<ResultadoAnalisisRequest>? resultados;

        public int? AnalisisId
        {
            get => analisisId;
            set => analisisId = value;
        }

        public int? UsuarioId
        {
            get => usuarioId;
            set => usuarioId = value;
        }

        public int? TerrenoId
        {
            get => terrenoId;
            set => terrenoId = value;
        }

        public string? NombreCliente
        {
            get => nombreCliente;
            set => nombreCliente = value;
        }

        public string? CodigoTerreno
        {
            get => codigoTerreno;
            set => codigoTerreno = value;
        }

        public string? NombreTerreno
        {
            get => nombreTerreno;
            set => nombreTerreno = value;
        }

        public string? TipoCultivo
        {
            get => tipoCultivo;
            set => tipoCultivo = value;
        }

        public string? TipoAnalisisSuelo
        {
            get => tipoAnalisisSuelo;
            set => tipoAnalisisSuelo = value;
        }

        public DateTime? FechaAnalisisLaboratorio
        {
            get => fechaAnalisisLaboratorio;
            set => fechaAnalisisLaboratorio = value;
        }

        public string? Laboratorio
        {
            get => laboratorio;
            set => laboratorio = value;
        }

        public string? IdentificadorAnalisisSuelo
        {
            get => identificadorAnalisisSuelo;
            set => identificadorAnalisisSuelo = value;
        }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set => cantidadQuintalesOro = value;
        }

        public decimal? TamanoFinca
        {
            get => tamanoFinca;
            set => tamanoFinca = value;
        }

        public string? TipoMuestra
        {
            get => tipoMuestra;
            set => tipoMuestra = value;
        }

        public List<ResultadoAnalisisRequest>? Resultados
        {
            get => resultados;
            set => resultados = value;
        }

        public NuevoAnalisisRequest()
        {
            Resultados = new List<ResultadoAnalisisRequest>();
        }
    }

    // ===============================================================
    // Clase: ResultadoAnalisisRequest
    // Descripción:
    //   Representa cada resultado capturado dentro del análisis.
    //   Puede ser un parámetro constante o un elemento químico.
    // ===============================================================
    public class ResultadoAnalisisRequest
    {
        private int? resultadoAnalisisId;

        private int? elementoQuimicoId;

        private string? codigoParametro;

        private string? nombreParametro;

        private decimal? valor;

        private string? unidadMedida;

        private bool? esConstante;

        private bool? esElementoQuimico;

        public int? ResultadoAnalisisId
        {
            get => resultadoAnalisisId;
            set => resultadoAnalisisId = value;
        }

        public int? ElementoQuimicoId
        {
            get => elementoQuimicoId;
            set => elementoQuimicoId = value;
        }

        public string? CodigoParametro
        {
            get => codigoParametro;
            set => codigoParametro = value;
        }

        public string? NombreParametro
        {
            get => nombreParametro;
            set => nombreParametro = value;
        }

        public decimal? Valor
        {
            get => valor;
            set => valor = value;
        }

        public string? UnidadMedida
        {
            get => unidadMedida;
            set => unidadMedida = value;
        }

        public bool? EsConstante
        {
            get => esConstante;
            set => esConstante = value;
        }

        public bool? EsElementoQuimico
        {
            get => esElementoQuimico;
            set => esElementoQuimico = value;
        }

        public ResultadoAnalisisRequest() { }
    }
}