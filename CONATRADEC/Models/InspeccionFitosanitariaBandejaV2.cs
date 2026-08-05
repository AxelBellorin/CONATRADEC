namespace CONATRADEC.Models
{
    /// <summary>
    /// Opción visible de un filtro. El código se envía al backend y el nombre
    /// se presenta al usuario.
    /// </summary>
    public sealed class FiltroCodigoOpcionV2
    {
        public FiltroCodigoOpcionV2(string codigo, string nombre)
        {
            Codigo = codigo ?? string.Empty;
            Nombre = nombre ?? string.Empty;
        }

        public string Codigo { get; }
        public string Nombre { get; }
    }

    /// <summary>
    /// Parámetros de búsqueda para una página de inspecciones. La siguiente
    /// página se solicita con la fecha y el identificador del último registro
    /// recibido, evitando OFFSET y el costo creciente de páginas profundas.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaFiltroV2
    {
        public string Modo { get; set; } = "mis";
        public string Buscar { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string TipoFotografia { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        /// <summary>
        /// Diferencia del dispositivo respecto de UTC. Permite que un filtro
        /// por fecha abarque el día local completo y no el día UTC del servidor.
        /// </summary>
        public int DesfaseHorarioMinutos { get; set; }

        public DateTime? UltimaFechaUtc { get; set; }
        public int? UltimoId { get; set; }
        public int TamanoPagina { get; set; } = 20;
    }

    /// <summary>
    /// Resumen liviano de una inspección. No contiene las fotografías ni sus
    /// resultados completos; solo los datos requeridos por la bandeja.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaItemV2
    {
        public int InspeccionId { get; set; }
        public string NombreInspeccion { get; set; } = string.Empty;
        public bool CerradaTecnico { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TotalFotografias { get; set; }
        public int Pendientes { get; set; }
        public int ConError { get; set; }
        public int Finalizadas { get; set; }
        public string UrlMiniatura { get; set; } = string.Empty;

        public string NombreInspeccionTexto =>
            string.IsNullOrWhiteSpace(NombreInspeccion)
                ? $"Inspección #{InspeccionId}"
                : NombreInspeccion.Trim();

        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Terreno no disponible (registro anterior)"
            : $"Terreno {CodigoTerreno}";

        public string PropietarioTexto => string.IsNullOrWhiteSpace(Propietario)
            ? "Sin propietario vinculado"
            : Propietario;

        public string UbicacionTexto
        {
            get
            {
                string[] partes =
                [
                    Municipio?.Trim() ?? string.Empty,
                    Departamento?.Trim() ?? string.Empty
                ];

                string ubicacion = string.Join(
                    " · ",
                    partes.Where(item => !string.IsNullOrWhiteSpace(item)));

                return string.IsNullOrWhiteSpace(ubicacion)
                    ? "Ubicación no disponible"
                    : ubicacion;
            }
        }

        public string EstadoTexto => CerradaTecnico
            ? "Cerrada"
            : Estado switch
        {
            "BORRADOR" => "Borrador",
            "EN_PROCESO" => "En proceso",
            "EN_PROCESO_CON_ERRORES" => "En proceso con errores",
            "PENDIENTE_REVISION" => "Pendiente de revisión",
            "PENDIENTE_APROBACION" => "Pendiente de aprobación",
            "FINALIZADA" => "Finalizada",
            "FINALIZADA_PARCIALMENTE" => "Finalizada parcialmente",
            _ => (Estado ?? string.Empty).Replace('_', ' ')
        };

        public string TextoAbrir => CerradaTecnico
            ? "Ver inspección"
            : "Abrir inspección";

        public string Resumen =>
            $"{TotalFotografias} fotos · {Pendientes} pendientes · " +
            $"{ConError} con error · {Finalizadas} finalizadas";

        public string FechaTexto =>
            FechaRegistroSistemaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class InspeccionFitosanitariaBandejaPaginaV2
    {
        public List<InspeccionFitosanitariaBandejaItemV2> Items { get; set; } = [];
        public bool HayMas { get; set; }
        public DateTime? SiguienteFechaUtc { get; set; }
        public int? SiguienteId { get; set; }
    }
}
