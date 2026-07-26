namespace CONATRADEC.Models
{
    public static class SincronizacionOfflineGlobalEstados
    {
        public const string SinPreparar = "SIN_PREPARAR";
        public const string Sincronizando = "SINCRONIZANDO";
        public const string Listo = "LISTO";
        public const string ListoConAviso = "LISTO_CON_AVISO";
        public const string ActualizacionDisponible =
            "ACTUALIZACION_DISPONIBLE";
        public const string Error = "ERROR";
    }

    public static class ModuloOfflineEstados
    {
        public const string Pendiente = "PENDIENTE";
        public const string Sincronizando = "SINCRONIZANDO";
        public const string Listo = "LISTO";
        public const string NoHabilitado = "NO_HABILITADO";
        public const string Error = "ERROR";
    }

    public sealed class ModuloOfflineResumen
    {
        public string Nombre { get; init; } = string.Empty;
        public string Estado { get; init; } =
            ModuloOfflineEstados.Pendiente;
        public string Mensaje { get; init; } = string.Empty;
        public int Registros { get; init; }
        public int Imagenes { get; init; }

        public bool EstaListo =>
            Estado == ModuloOfflineEstados.Listo;

        public bool EstaOmitido =>
            Estado == ModuloOfflineEstados.NoHabilitado;
    }

    public sealed class SincronizacionOfflineGlobalEstado
    {
        public string Estado { get; init; } =
            SincronizacionOfflineGlobalEstados.SinPreparar;

        public string Mensaje { get; init; } =
            "Los datos completos todavía no se han preparado.";

        public string Detalle { get; init; } =
            "Use Descargar todo antes de trabajar sin conexión.";

        public int ProgresoPorcentaje { get; init; }
        public int PasoActual { get; init; }
        public int TotalPasos { get; init; } = 4;

        public bool PreparacionCompleta { get; init; }
        public bool SincronizacionEnCurso =>
            Estado ==
            SincronizacionOfflineGlobalEstados.Sincronizando;

        public DateTime? UltimaSincronizacionCompletaUtc {
            get;
            init;
        }

        public DateTime? UltimaVerificacionUtc { get; init; }

        public long TamanoTotalBytes { get; init; }

        public ModuloOfflineResumen MotorCalculo { get; init; } =
            new()
            {
                Nombre = "Motor de cálculo"
            };

        public ModuloOfflineResumen Catalogos { get; init; } =
            new()
            {
                Nombre = "Catálogos"
            };

        public ModuloOfflineResumen Noticias { get; init; } =
            new()
            {
                Nombre = "Noticias"
            };

        public ModuloOfflineResumen Album { get; init; } =
            new()
            {
                Nombre = "Álbum de fotos"
            };
    }

    public sealed class SincronizacionOfflineGlobalEventArgs :
        EventArgs
    {
        public SincronizacionOfflineGlobalEstado Estado {
            get;
        }

        public SincronizacionOfflineGlobalEventArgs(
            SincronizacionOfflineGlobalEstado estado)
        {
            Estado = estado;
        }
    }

    public sealed class ResultadoSincronizacionOfflineGlobal
    {
        public bool Success { get; init; }
        public bool ConservaCopiaAnterior { get; init; }
        public string Message { get; init; } = string.Empty;

        public static ResultadoSincronizacionOfflineGlobal Ok(
            string message) =>
            new()
            {
                Success = true,
                Message = message
            };

        public static ResultadoSincronizacionOfflineGlobal Fail(
            string message,
            bool conservaCopiaAnterior) =>
            new()
            {
                Success = false,
                ConservaCopiaAnterior =
                    conservaCopiaAnterior,
                Message = message
            };
    }
}
