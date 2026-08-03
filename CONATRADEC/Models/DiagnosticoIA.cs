using Microsoft.Maui.Controls;

namespace CONATRADEC.Models
{
    public sealed class DiagnosticoIAItem
    {
        public int DiagnosticoIAId { get; set; }
        public int? TerrenoId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public int UsuarioSolicitanteId { get; set; }
        public string UsuarioSolicitante { get; set; } = string.Empty;
        public DateTime FechaSolicitudUtc { get; set; }
        public DateTime? FechaRespuestaIAUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string ModeloGemini { get; set; } = string.Empty;
        public string ObservacionUsuario { get; set; } = string.Empty;
        public bool ImagenValida { get; set; }
        public bool ParecePlantaCafe { get; set; }
        public bool ResultadoConcluyente { get; set; }
        public bool PosibleDanoNoBiotico { get; set; }
        public string DiagnosticoSugerido { get; set; } = string.Empty;
        public string NivelCoincidencia { get; set; } = string.Empty;
        public string Resumen { get; set; } = string.Empty;
        public string PosibleCausaNoBiotica { get; set; } = string.Empty;
        public List<string> SintomasVisibles { get; set; } = [];
        public List<string> DiagnosticosAlternativos { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public string ErrorAnalisis { get; set; } = string.Empty;
        public bool RequiereValidacionHumana { get; set; }
        public List<DiagnosticoIAImagenItem> Imagenes { get; set; } = [];
        public List<DiagnosticoIARevisionItem> RevisionesIA { get; set; } = [];
        public DiagnosticoIARevisionItem? UltimaRevisionIA { get; set; }
        public DiagnosticoIARevisionItem? RevisionVigenteIA { get; set; }
        public DiagnosticoIAValidacionItem? UltimaValidacion { get; set; }

        public string FechaSolicitudTexto =>
            FechaSolicitudUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        public string TerrenoTexto =>
            string.IsNullOrWhiteSpace(CodigoTerreno)
                ? "Sin terreno asociado"
                : $"Terreno: {CodigoTerreno}";

        public string DiagnosticoMostrar =>
            string.IsNullOrWhiteSpace(DiagnosticoSugerido)
                ? "Sin veredicto disponible"
                : DiagnosticoSugerido.Replace('_', ' ');

        public string DiagnosticoVigenteMostrar =>
            RevisionVigenteIA?.Completada == true &&
            !string.IsNullOrWhiteSpace(
                RevisionVigenteIA.DiagnosticoRevisado)
                ? RevisionVigenteIA.DiagnosticoMostrar
                : DiagnosticoMostrar;

        public string ResumenVigente =>
            RevisionVigenteIA?.Completada == true &&
            !string.IsNullOrWhiteSpace(
                RevisionVigenteIA.ResumenRevision)
                ? RevisionVigenteIA.ResumenRevision
                : Resumen;

        public string EstadoMostrar => Estado switch
        {
            "ANALIZANDO_IA" => "Analizando con IA",
            "PENDIENTE_VALIDACION" => "Pendiente de validación",
            "CONFIRMADO" => "Confirmado",
            "CORREGIDO" => "Corregido",
            "NO_CONCLUYENTE" => "No concluyente",
            "IMAGEN_RECHAZADA" => "Imágenes rechazadas",
            "ERROR_ANALISIS" => "Error del análisis",
            _ => Estado.Replace('_', ' ')
        };

        public Color ColorEstado => Estado switch
        {
            "CONFIRMADO" => Color.FromArgb("#1B7F5A"),
            "CORREGIDO" => Color.FromArgb("#2563EB"),
            "PENDIENTE_VALIDACION" => Color.FromArgb("#9B552C"),
            "ERROR_ANALISIS" or "IMAGEN_RECHAZADA" =>
                Color.FromArgb("#B91C1C"),
            _ => Color.FromArgb("#4B5563")
        };

        public bool TieneErrorAnalisis =>
            Estado.Equals(
                "ERROR_ANALISIS",
                StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(ErrorAnalisis);

        public bool TieneValidacionHumana =>
            !RequiereValidacionHumana && UltimaValidacion != null;

        public bool TieneRevisionIA =>
            UltimaRevisionIA != null;

        public bool TieneRevisionCompletada =>
            RevisionVigenteIA?.Completada == true;

        public int CantidadRevisionesCompletadas =>
            RevisionesIA.Count(item => item.Completada);

        public bool PuedeSolicitarOtraRevision =>
            Estado.Equals(
                "PENDIENTE_VALIDACION",
                StringComparison.OrdinalIgnoreCase) &&
            CantidadRevisionesCompletadas < 3 &&
            !RevisionesIA.Any(item => item.Analizando);

        public string ResumenRevisiones =>
            CantidadRevisionesCompletadas == 1
                ? "1 segunda revisión realizada"
                : $"{CantidadRevisionesCompletadas} segundas revisiones realizadas";

        public string ValidacionFinalTexto
        {
            get
            {
                if (UltimaValidacion == null)
                    return string.Empty;

                return string.IsNullOrWhiteSpace(
                        UltimaValidacion.DiagnosticoFinal)
                    ? UltimaValidacion.Decision.Replace('_', ' ')
                    : UltimaValidacion.DiagnosticoFinal;
            }
        }
    }

    public sealed class DiagnosticoIAImagenItem
    {
        public int DiagnosticoIAImagenId { get; set; }
        public string UrlImagen { get; set; } = string.Empty;
        public string TipoFotografia { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public sealed class DiagnosticoIARevisionItem
    {
        public int DiagnosticoIARevisionId { get; set; }
        public int UsuarioClasificadorId { get; set; }
        public string UsuarioClasificador { get; set; } = string.Empty;
        public string RetroalimentacionClasificador { get; set; } = string.Empty;
        public string DiagnosticoPropuestoClasificador { get; set; } = string.Empty;
        public DateTime FechaSolicitudRevisionUtc { get; set; }
        public DateTime? FechaRespuestaRevisionUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool ImagenValida { get; set; }
        public bool ResultadoConcluyente { get; set; }
        public bool MantieneVeredictoOriginal { get; set; }
        public string RelacionConCriterioTecnico { get; set; } = string.Empty;
        public string DiagnosticoRevisado { get; set; } = string.Empty;
        public string NivelCoincidencia { get; set; } = string.Empty;
        public string ResumenRevision { get; set; } = string.Empty;
        public List<string> EvidenciasApoyo { get; set; } = [];
        public List<string> EvidenciasContradiccion { get; set; } = [];
        public List<string> InformacionFaltante { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public string ErrorRevision { get; set; } = string.Empty;

        public bool Completada =>
            Estado.Equals(
                "COMPLETADA",
                StringComparison.OrdinalIgnoreCase);

        public bool Analizando =>
            Estado.Equals(
                "ANALIZANDO_IA",
                StringComparison.OrdinalIgnoreCase);

        public bool TieneError =>
            Estado.Equals(
                "ERROR_REVISION",
                StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(ErrorRevision);

        public bool TieneEvidenciasApoyo =>
            EvidenciasApoyo.Count > 0;

        public bool TieneEvidenciasContradiccion =>
            EvidenciasContradiccion.Count > 0;

        public bool TieneInformacionFaltante =>
            InformacionFaltante.Count > 0;

        public bool TieneRecomendacionesCaptura =>
            RecomendacionesCaptura.Count > 0;

        public string DiagnosticoMostrar =>
            string.IsNullOrWhiteSpace(DiagnosticoRevisado)
                ? "NO DETERMINADO"
                : DiagnosticoRevisado.Replace('_', ' ');

        public string RelacionMostrar =>
            RelacionConCriterioTecnico switch
            {
                "COINCIDE" => "Coincide con el criterio del clasificador",
                "NO_COINCIDE" => "No coincide con el criterio del clasificador",
                "PARCIAL" => "Coincide parcialmente con el criterio del clasificador",
                _ => "No fue posible comparar con un diagnóstico técnico"
            };

        public string CambioVeredictoTexto =>
            MantieneVeredictoOriginal
                ? "Gemini mantiene su primer veredicto"
                : "Gemini modificó su primer veredicto";

        public string FechaRevisionTexto =>
            (FechaRespuestaRevisionUtc ??
             FechaSolicitudRevisionUtc)
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class DiagnosticoIAValidacionItem
    {
        public int DiagnosticoIAValidacionId { get; set; }
        public int UsuarioClasificadorId { get; set; }
        public string UsuarioClasificador { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public bool? CoincideConGemini { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaValidacionUtc { get; set; }
    }

    public sealed class FotoDiagnosticoSeleccionada
    {
        public string RutaLocal { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string TipoContenido { get; set; } = "image/jpeg";
        public ImageSource VistaPrevia =>
            ImageSource.FromFile(RutaLocal);
    }

    public sealed class DiagnosticoIAPaginaRespuesta
    {
        public bool Success { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int Total { get; set; }
        public List<DiagnosticoIAItem> Data { get; set; } = [];
    }

    public sealed class DiagnosticoIADetalleRespuesta
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DiagnosticoIAItem? Data { get; set; }
    }
}
