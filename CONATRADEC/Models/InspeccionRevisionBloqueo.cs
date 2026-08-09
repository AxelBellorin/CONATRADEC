namespace CONATRADEC.Models
{
    /// <summary>
    /// Estado de la reserva temporal de una inspección para una sesión de
    /// analizador o aprobador.
    /// </summary>
    public sealed class InspeccionRevisionBloqueo
    {
        public bool Adquirido { get; set; }
        public int InspeccionId { get; set; }
        public string Modo { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? FechaAdquisicionUtc { get; set; }
        public DateTime? UltimoHeartbeatUtc { get; set; }
        public DateTime? ExpiraUtc { get; set; }
        public int VigenciaSegundos { get; set; }
    }

    /// <summary>
    /// Responsable persistente de una etapa. La asignación no representa un
    /// bloqueo: continúa vigente aunque el usuario cierre la pantalla.
    /// </summary>
    public sealed class InspeccionRevisionAsignacion
    {
        public int InspeccionId { get; set; }
        public string Modo { get; set; } = string.Empty;
        public int? UsuarioAsignadoId { get; set; }
        public string UsuarioAsignadoNombre { get; set; } = string.Empty;
        public bool AsignadaAlUsuarioActual { get; set; }
        public bool DisponibleParaTomar { get; set; }
        public bool AsignadaAOtroUsuario { get; set; }

        public string ResponsableTexto =>
            DisponibleParaTomar
                ? "Sin asignar · disponible para tomar"
                : AsignadaAlUsuarioActual
                    ? string.IsNullOrWhiteSpace(UsuarioAsignadoNombre)
                        ? "Asignada a ti"
                        : $"Asignada a ti · {UsuarioAsignadoNombre}"
                    : string.IsNullOrWhiteSpace(UsuarioAsignadoNombre)
                        ? $"Asignada al usuario #{UsuarioAsignadoId}"
                        : $"Asignada a {UsuarioAsignadoNombre}";
    }

    /// <summary>
    /// Usuario que posee el permiso real de actualización para una etapa y,
    /// por tanto, puede ser elegido por un supervisor autorizado.
    /// </summary>
    public sealed class InspeccionRevisionUsuarioAsignable
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;

        public string TextoMostrar =>
            !string.IsNullOrWhiteSpace(NombreCompleto)
                ? string.IsNullOrWhiteSpace(NombreUsuario)
                    ? NombreCompleto.Trim()
                    : $"{NombreCompleto.Trim()} ({NombreUsuario.Trim()})"
                : !string.IsNullOrWhiteSpace(NombreUsuario)
                    ? NombreUsuario.Trim()
                    : $"Usuario #{UsuarioId}";
    }

    public sealed class InspeccionRevisionReasignacionRequest
    {
        public string Etapa { get; set; } = string.Empty;
        public int UsuarioNuevoId { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class InspeccionRevisionOperacionAsignacion
    {
        public int InspeccionId { get; set; }
        public string Etapa { get; set; } = string.Empty;
        public int? UsuarioAnteriorId { get; set; }
        public string UsuarioAnterior { get; set; } = string.Empty;
        public int? UsuarioNuevoId { get; set; }
        public string UsuarioNuevo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }
    }
}
