namespace CONATRADEC.Models
{
    public static class InspeccionFotoEstadosRevision
    {
        public const string DevueltaTecnico = "DEVUELTA_AL_TECNICO";
    }

    public sealed class MotivoDevolucionTecnicoItem
    {
        public int MotivoDevolucionTecnicoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string InstruccionSugerida { get; set; } = string.Empty;
        public bool RequiereNuevaFotografia { get; set; }
        public bool PermiteCorregirMetadatos { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime FechaModificacionUtc { get; set; }

        public string NombreMostrar => string.IsNullOrWhiteSpace(Nombre)
            ? Codigo.Replace('_', ' ')
            : Nombre;

        public string TipoCorreccionTexto => RequiereNuevaFotografia
            ? "Requiere una nueva fotografía"
            : PermiteCorregirMetadatos
                ? "Puede corregirse sobre la evidencia actual"
                : "Requiere atención técnica";

        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    }

    public sealed class MotivoDevolucionTecnicoRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string InstruccionSugerida { get; set; } = string.Empty;
        public bool RequiereNuevaFotografia { get; set; }
        public bool PermiteCorregirMetadatos { get; set; } = true;
        public int Orden { get; set; } = 1;
    }

    public sealed class DevolucionTecnicoFotografiaV2
    {
        public int DevolucionTecnicoId { get; set; }
        public int FotografiaId { get; set; }
        public int MotivoDevolucionTecnicoId { get; set; }
        public string MotivoCodigo { get; set; } = string.Empty;
        public string MotivoNombre { get; set; } = string.Empty;
        public string MotivoDescripcion { get; set; } = string.Empty;
        public string InstruccionSugerida { get; set; } = string.Empty;
        public string InstruccionesAnalizador { get; set; } = string.Empty;
        public bool RequiereNuevaFotografia { get; set; }
        public bool PermiteCorregirMetadatos { get; set; }
        public int UsuarioAnalizadorId { get; set; }
        public DateTime FechaDevolucionUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string RespuestaTecnico { get; set; } = string.Empty;
        public int? UsuarioTecnicoId { get; set; }
        public DateTime? FechaResolucionUtc { get; set; }

        public bool EstaPendiente => string.Equals(
            Estado,
            "PENDIENTE",
            StringComparison.OrdinalIgnoreCase);

        public string FechaTexto =>
            FechaDevolucionUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        public string InstruccionCompleta
        {
            get
            {
                string sugerida = InstruccionSugerida?.Trim() ?? string.Empty;
                string especifica =
                    InstruccionesAnalizador?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(sugerida))
                    return especifica;

                if (string.IsNullOrWhiteSpace(especifica) ||
                    string.Equals(
                        sugerida,
                        especifica,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return sugerida;
                }

                return $"{sugerida}\n\nIndicación específica: {especifica}";
            }
        }
    }

    public sealed class ResumenRevisionAnalizadorV2
    {
        public int InspeccionId { get; set; }
        public int TotalRegistradas { get; set; }
        public int TotalEvaluables { get; set; }
        public int TotalDescartadasTecnico { get; set; }
        public int TotalRecibidasAnalizador { get; set; }
        public int TotalPendientesTecnico { get; set; }
        public int TotalDevueltasTecnico { get; set; }
        public int TotalErroresIA { get; set; }
        public int TotalProcesandoIA { get; set; }
        public int TotalPendienteDecisionTecnico { get; set; }
        public int TotalClasificadasHumano { get; set; }
        public int TotalPendientesClasificacionHumana { get; set; }
        public bool EtapaTecnicaFinalizada { get; set; }
        public bool EtapaAnalizadorFinalizada { get; set; }
        public DateTime? FechaFinEtapaAnalizadorUtc { get; set; }
        public bool PuedeFinalizarRevision { get; set; }
        public string MotivoNoPuedeFinalizarRevision { get; set; } = string.Empty;

        public string RecepcionTexto => EtapaTecnicaFinalizada
            ? "Recepción técnica completa"
            : "Recepción parcial";

        public string ConteosTexto =>
            $"{TotalRecibidasAnalizador} recibidas · " +
            $"{TotalClasificadasHumano} clasificadas · " +
            $"{TotalPendientesTecnico} pendientes del técnico · " +
            $"{TotalDescartadasTecnico} descartadas";
    }

    public sealed class ContextoRevisionAnalizadorV2
    {
        public ResumenRevisionAnalizadorV2 Resumen { get; set; } = new();
        public List<DevolucionTecnicoFotografiaV2> Devoluciones { get; set; } = [];
    }

    public sealed class DevolucionTecnicoFormularioResultado
    {
        public int MotivoId { get; set; }
        public string Instrucciones { get; set; } = string.Empty;
    }

    public sealed class CorreccionTecnicoFormularioResultado
    {
        public string TipoFotografia { get; set; } = string.Empty;
        public DateTime FechaIdentificacionCampo { get; set; }
        public string RespuestaTecnico { get; set; } = string.Empty;
    }
}
