using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: NuevoAnalisisRequest
    // Descripción:
    //   Representa la estructura de solicitud utilizada para enviar
    //   la información capturada en el formulario de análisis de suelo
    //   hacia la API.
    //   Se usa principalmente para registrar un nuevo análisis y sus
    //   resultados nutricionales.
    // ===============================================================
    public class NuevoAnalisisRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador del análisis.
        // Se declara como nullable porque al crear un nuevo análisis
        // todavía no existe un ID generado por la base de datos.
        private int? analisisId;

        // Campo que almacena el nombre del laboratorio donde se realizó el análisis.
        private string? laboratorio;

        // Campo que almacena el tipo de muestra, por ejemplo: "Suelo".
        private string? tipoMuestra;

        // Campo que almacena el identificador del usuario que registra el análisis.
        private int? usuarioId;

        // Campo que almacena el listado de resultados capturados por parámetro.
        private List<ResultadoAnalisisRequest>? resultados;

        private int? terrenoId;
        private string? nombreCliente;
        private string? codigoTerreno;
        private string? nombreTerreno;
        private string? tipoCultivo;
        private string? tipoAnalisisSuelo;
        private DateTime? fechaAnalisisLaboratorio;
        private string? identificadorAnalisisSuelo;
        private decimal? cantidadQuintalesOro;
        private decimal? tamanoFinca;   


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del análisis.
        public int? AnalisisId
        {
            get => analisisId;
            set => analisisId = value;
        }

        // Propiedad pública para acceder o modificar el laboratorio.
        public string? Laboratorio
        {
            get => laboratorio;
            set => laboratorio = value;
        }

        // Propiedad pública para acceder o modificar el tipo de muestra.
        public string? TipoMuestra
        {
            get => tipoMuestra;
            set => tipoMuestra = value;
        }

        // Propiedad pública para acceder o modificar el ID del usuario.
        public int? UsuarioId
        {
            get => usuarioId;
            set => usuarioId = value;
        }

        // Propiedad pública para acceder o modificar la lista de resultados.
        public List<ResultadoAnalisisRequest>? Resultados
        {
            get => resultados;
            set => resultados = value;
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

        // ===========================================================
        // ================ CONSTRUCTORES DE LA CLASE ================
        // ===========================================================

        // Constructor vacío.
        // Permite crear una instancia sin valores iniciales.
        public NuevoAnalisisRequest()
        {
            Resultados = new List<ResultadoAnalisisRequest>();
        }

        // Constructor que inicializa una nueva instancia de NuevoAnalisisRequest
        // tomando como base un objeto de tipo NuevoAnalisisResponse.
        // Es útil cuando se desea convertir una respuesta de la API
        // en una solicitud para actualizar o reenviar información.
        public NuevoAnalisisRequest(NuevoAnalisisResponse analisisRP)
        {
            AnalisisId = analisisRP.AnalisisId;
            Laboratorio = analisisRP.Laboratorio;
            TipoMuestra = analisisRP.TipoMuestra;

            Resultados = analisisRP.Resultados?
                .Select(resultado => new ResultadoAnalisisRequest(resultado))
                .ToList() ?? new List<ResultadoAnalisisRequest>();
        }
    }


    // ===============================================================
    // Clase: ResultadoAnalisisRequest
    // Descripción:
    //   Representa cada resultado individual capturado dentro de un
    //   análisis de suelo.
    //   Ejemplo: pH, Materia Orgánica, Potasio, Fósforo disponible,
    //   Calcio, Magnesio, etc.
    // ===============================================================
    public class ResultadoAnalisisRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador del resultado.
        private int? resultadoAnalisisId;

        // Campo que almacena el código interno del parámetro.
        // Ejemplo: PH, MATERIA_ORGANICA, POTASIO.
        private string? codigoParametro;

        // Campo que almacena el nombre visible del parámetro.
        private string? nombreParametro;

        // Campo que almacena el valor digitado por el usuario.
        private decimal? valor;

        // Campo que almacena la unidad de medida seleccionada.
        // Ejemplo: pH, %, mg/kg, ppm, g/kg.
        private string? unidadMedida;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        public int? ResultadoAnalisisId
        {
            get => resultadoAnalisisId;
            set => resultadoAnalisisId = value;
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


        // ===========================================================
        // ================ CONSTRUCTORES DE LA CLASE ================
        // ===========================================================

        // Constructor vacío.
        public ResultadoAnalisisRequest() { }

        // Constructor que inicializa una nueva instancia de ResultadoAnalisisRequest
        // tomando como base un objeto de tipo ResultadoAnalisisResponse.
        public ResultadoAnalisisRequest(ResultadoAnalisisResponse resultadoRP)
        {
            ResultadoAnalisisId = resultadoRP.ResultadoAnalisisId;
            CodigoParametro = resultadoRP.CodigoParametro;
            NombreParametro = resultadoRP.NombreParametro;
            Valor = resultadoRP.ValorRecibido;
            UnidadMedida = resultadoRP.UnidadRecibida;
        }
    }
}