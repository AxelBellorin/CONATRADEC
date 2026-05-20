using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: NuevoAnalisisResponse
    // Descripción:
    //   Representa la estructura de respuesta utilizada para recibir
    //   desde la API la información de un análisis de suelo registrado
    //   o procesado.
    //   Este modelo puede incluir valores originales, valores convertidos
    //   y clasificación nutricional.
    // ===============================================================
    public class NuevoAnalisisResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del análisis.
        private int? analisisId;

        // Campo que almacena el identificador del usuario que registra el análisis.
        private int? usuarioId;

        // Campo que almacena el nombre del laboratorio.
        private string? laboratorio;

        // Campo que almacena el tipo de muestra.
        private string? tipoMuestra;

        // Campo que indica si la operación fue exitosa.
        private bool? exito;

        // Campo que almacena el mensaje devuelto por la API.
        private string? mensaje;

        // Campo que almacena el listado de resultados del análisis.
        private List<ResultadoAnalisisResponse>? resultados;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        public int? AnalisisId
        {
            get => analisisId;
            set => analisisId = value;
        }

        public string? Laboratorio
        {
            get => laboratorio;
            set => laboratorio = value;
        }

        public string? TipoMuestra
        {
            get => tipoMuestra;
            set => tipoMuestra = value;
        }

        public bool? Exito
        {
            get => exito;
            set => exito = value;
        }

        public string? Mensaje
        {
            get => mensaje;
            set => mensaje = value;
        }

        // Propiedad pública para acceder o modificar el ID del usuario.
        public int? UsuarioId
        {
            get => usuarioId;
            set => usuarioId = value;
        }

        public List<ResultadoAnalisisResponse>? Resultados
        {
            get => resultados;
            set => resultados = value;
        }


        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================

        // Constructor vacío.
        // Se usa al crear instancias sin datos iniciales.
        public NuevoAnalisisResponse()
        {
            Resultados = new List<ResultadoAnalisisResponse>();
        }
    }


    // ===============================================================
    // Clase: ResultadoAnalisisResponse
    // Descripción:
    //   Representa cada resultado individual devuelto por la API
    //   después de registrar o procesar un análisis de suelo.
    //   Puede contener el valor recibido, la unidad original,
    //   el valor convertido, la unidad final y la clasificación.
    // ===============================================================
    public class ResultadoAnalisisResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del resultado.
        private int? resultadoAnalisisId;

        // Campo que almacena el código interno del parámetro.
        private string? codigoParametro;

        // Campo que almacena el nombre visible del parámetro.
        private string? nombreParametro;

        // Campo que almacena el valor recibido desde el formulario.
        private decimal? valorRecibido;

        // Campo que almacena la unidad recibida desde el formulario.
        private string? unidadRecibida;

        // Campo que almacena el valor convertido por el backend.
        private decimal? valorConvertido;

        // Campo que almacena la unidad final después de la conversión.
        private string? unidadConvertida;

        // Campo que almacena la clasificación del resultado.
        // Ejemplo: Bajo, Medio, Alto.
        private string? clasificacion;


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

        public decimal? ValorRecibido
        {
            get => valorRecibido;
            set => valorRecibido = value;
        }

        public string? UnidadRecibida
        {
            get => unidadRecibida;
            set => unidadRecibida = value;
        }

        public decimal? ValorConvertido
        {
            get => valorConvertido;
            set => valorConvertido = value;
        }

        public string? UnidadConvertida
        {
            get => unidadConvertida;
            set => unidadConvertida = value;
        }

        public string? Clasificacion
        {
            get => clasificacion;
            set => clasificacion = value;
        }


        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================

        // Constructor vacío.
        public ResultadoAnalisisResponse() { }
    }
}