using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public static class DiagnosticoIAEstados
    {
        public const string AnalizandoIA = "ANALIZANDO_IA";
        public const string ErrorAnalisis = "ERROR_ANALISIS";
        public const string PendienteDecisionTecnico =
            "PENDIENTE_DECISION_TECNICO";
        public const string CanceladoPorTecnico =
            "CANCELADO_POR_TECNICO";
        public const string PendienteAnalizador = "PENDIENTE_ANALIZADOR";
        public const string EnAnalisisHumano = "EN_ANALISIS_HUMANO";
        public const string PendienteAprobacion = "PENDIENTE_APROBACION";
        public const string DevueltoCorreccion = "DEVUELTO_PARA_CORRECCION";
        public const string Aprobado = "APROBADO";
        public const string AprobadoConCorreccion = "APROBADO_CON_CORRECCION";
        public const string Rechazado = "RECHAZADO";
        public const string NoConcluyente = "NO_CONCLUYENTE";
        public const string PublicadoAlbum = "PUBLICADO_EN_ALBUM";
        public const string Anulado = "ANULADO";

        public static string Mostrar(string? estado) =>
            estado switch
            {
                AnalizandoIA => "Analizando con Gemini",
                ErrorAnalisis => "Error del análisis",
                PendienteDecisionTecnico =>
                    "Pendiente de decisión del técnico",
                CanceladoPorTecnico =>
                    "Cerrado por el técnico",
                PendienteAnalizador => "Pendiente del analizador",
                EnAnalisisHumano => "En análisis humano",
                PendienteAprobacion => "Pendiente de aprobación",
                DevueltoCorreccion => "Devuelto para corrección",
                Aprobado => "Aprobado",
                AprobadoConCorreccion => "Aprobado con corrección",
                Rechazado => "Rechazado",
                NoConcluyente => "No concluyente",
                PublicadoAlbum => "Publicado en el álbum",
                Anulado => "Anulado",
                _ => (estado ?? "SIN_ESTADO").Replace('_', ' ')
            };

        public static Color Color(string? estado) =>
            estado switch
            {
                Aprobado or PublicadoAlbum =>
                    Microsoft.Maui.Graphics.Color.FromArgb("#1B7F5A"),
                AprobadoConCorreccion =>
                    Microsoft.Maui.Graphics.Color.FromArgb("#2563EB"),
                PendienteDecisionTecnico or
                PendienteAnalizador or EnAnalisisHumano or
                PendienteAprobacion or DevueltoCorreccion =>
                    Microsoft.Maui.Graphics.Color.FromArgb("#9B552C"),
                ErrorAnalisis or CanceladoPorTecnico or
                Rechazado or Anulado =>
                    Microsoft.Maui.Graphics.Color.FromArgb("#B91C1C"),
                _ => Microsoft.Maui.Graphics.Color.FromArgb("#4B5563")
            };
    }

    public sealed class DiagnosticoIAApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Detail { get; set; }
    }

    public sealed class DiagnosticoIAListaItem
    {
        public int DiagnosticoIAId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public string UsuarioSolicitante { get; set; } = string.Empty;
        public DateTime FechaSolicitudUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string DiagnosticoSugerido { get; set; } = string.Empty;
        public string CategoriaPrincipalIA { get; set; } = string.Empty;
        public string EstadoGeneralIA { get; set; } = string.Empty;
        public string NivelCoincidencia { get; set; } = string.Empty;
        public int TotalImagenes { get; set; }
        public string? UrlMiniatura { get; set; }
        public int? VersionAnalisisActual { get; set; }
        public string? DiagnosticoPropuesto { get; set; }
        public string? Analizador { get; set; }
        public string? Aprobador { get; set; }
        public bool PuedePublicarAlbum { get; set; }
        public int TotalPublicadasAlbum { get; set; }

        [JsonIgnore]
        public string FechaTexto =>
            FechaSolicitudUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        [JsonIgnore]
        public string EstadoTexto =>
            DiagnosticoIAEstados.Mostrar(Estado);

        [JsonIgnore]
        public Color EstadoColor =>
            DiagnosticoIAEstados.Color(Estado);

        [JsonIgnore]
        public string TerrenoTexto =>
            string.IsNullOrWhiteSpace(CodigoTerreno)
                ? "Sin terreno asociado"
                : $"Terreno {CodigoTerreno}";

        [JsonIgnore]
        public string DiagnosticoTexto =>
            string.IsNullOrWhiteSpace(DiagnosticoPropuesto)
                ? (string.IsNullOrWhiteSpace(DiagnosticoSugerido)
                    ? "Sin diagnóstico disponible"
                    : DiagnosticoSugerido)
                : DiagnosticoPropuesto;

        [JsonIgnore]
        public bool TieneMiniatura =>
            !string.IsNullOrWhiteSpace(UrlMiniatura);
    }

    public sealed class DiagnosticoIADetalle
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
        public string CalidadEvaluacionIA { get; set; } = string.Empty;
        public string EstadoGeneralIA { get; set; } = string.Empty;
        public string CategoriaPrincipalIA { get; set; } = string.Empty;
        public List<string> CategoriasSecundariasIA { get; set; } = [];
        public string DiagnosticoSugerido { get; set; } = string.Empty;
        public string TipoDiagnosticoIA { get; set; } = string.Empty;
        public string SeveridadVisualIA { get; set; } = string.Empty;
        public string NivelCoincidencia { get; set; } = string.Empty;
        public string Resumen { get; set; } = string.Empty;
        public List<string> PartesAfectadas { get; set; } = [];
        public List<string> SintomasVisibles { get; set; } = [];
        public List<string> EvidenciasNoObservadas { get; set; } = [];
        public List<string> DiagnosticosAlternativos { get; set; } = [];
        public List<string> InformacionFaltante { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public bool PosibleDanoNoBiotico { get; set; }
        public string PosibleCausaNoBiotica { get; set; } = string.Empty;
        public string ErrorAnalisis { get; set; } = string.Empty;
        public bool RequiereValidacionHumana { get; set; }
        public List<DiagnosticoIAImagenItem> Imagenes { get; set; } = [];
        public List<DiagnosticoIARevisionItem> RevisionesIA { get; set; } = [];
        public DiagnosticoIARevisionItem? UltimaRevisionIA { get; set; }
        public List<DiagnosticoIAAnalisisHumanoItem> AnalisisHumanos { get; set; } = [];
        public DiagnosticoIAAnalisisHumanoItem? AnalisisHumanoActual { get; set; }
        public List<DiagnosticoIAAprobacionItem> Aprobaciones { get; set; } = [];
        public DiagnosticoIAAprobacionItem? UltimaAprobacion { get; set; }
        public List<DiagnosticoIAAlbumPublicacionItem> PublicacionesAlbum { get; set; } = [];
        public List<DiagnosticoIAHistorialItem> Historial { get; set; } = [];
        public bool EsPropietarioSolicitud { get; set; }
        public bool PuedeAnalizar { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedePublicarAlbum { get; set; }
        public int MaximoRevisionesGemini { get; set; } = 2;
        public bool RevisionesGeminiIlimitadas { get; set; }
        public int RevisionesGeminiCompletadas { get; set; }
        public bool PuedeSolicitarRevisionGemini { get; set; }

        [JsonIgnore]
        public string ResumenLimiteRevisiones =>
            RevisionesGeminiIlimitadas
                ? $"Revisiones completadas: {RevisionesGeminiCompletadas}. El sistema permite revisiones ilimitadas."
                : $"Revisiones completadas: {RevisionesGeminiCompletadas} de {MaximoRevisionesGemini}.";

        [JsonIgnore]
        public string FechaTexto =>
            FechaSolicitudUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        [JsonIgnore]
        public string EstadoTexto =>
            DiagnosticoIAEstados.Mostrar(Estado);

        [JsonIgnore]
        public Color EstadoColor =>
            DiagnosticoIAEstados.Color(Estado);

        [JsonIgnore]
        public string TerrenoTexto =>
            string.IsNullOrWhiteSpace(CodigoTerreno)
                ? "Sin terreno asociado"
                : $"Terreno {CodigoTerreno}";

        [JsonIgnore]
        public bool TieneError =>
            !string.IsNullOrWhiteSpace(ErrorAnalisis);

        [JsonIgnore]
        public bool TieneRevision => UltimaRevisionIA != null;

        [JsonIgnore]
        public bool TieneAnalisisHumano => AnalisisHumanoActual != null;

        [JsonIgnore]
        public bool TieneAprobacion => UltimaAprobacion != null;

        [JsonIgnore]
        public bool TieneClasificacionesPendientes =>
            Imagenes.Any(item =>
                item.ResultadoIA?.ClasificacionAlbumPendiente == true);
    }

    public sealed class DiagnosticoIAImagenItem :
        INotifyPropertyChanged
    {
        private bool seleccionadaParaAlbum;
        private string calidadTecnica = "MEDIA";
        private bool esEvidenciaValida = true;
        private bool aptaParaAlbum;
        private string observacionAprobador = string.Empty;
        private string descripcionAlbum = string.Empty;
        private bool esPortada;
        private int ordenAlbum;

        public int DiagnosticoIAImagenId { get; set; }
        public string UrlImagen { get; set; } = string.Empty;
        public string TipoFotografia { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public DiagnosticoIAImagenResultadoItem? ResultadoIA { get; set; }
        public DiagnosticoIAImagenEvaluacionItem? UltimaEvaluacion { get; set; }
        public DiagnosticoIAAlbumPublicacionItem? PublicacionAlbum { get; set; }

        [JsonIgnore]
        public bool Publicada => PublicacionAlbum != null;

        [JsonIgnore]
        public bool TieneResultadoIA => ResultadoIA != null;

        [JsonIgnore]
        public bool SinResultadoIA => !TieneResultadoIA;

        [JsonIgnore]
        public string NumeroFotoTexto => $"Fotografía {Orden}";

        [JsonIgnore]
        public string TipoFotografiaTexto =>
            string.IsNullOrWhiteSpace(TipoFotografia)
                ? "Tipo no indicado"
                : TipoFotografia.Replace('_', ' ');

        [JsonIgnore]
        public string PublicacionTexto => Publicada
            ? $"Publicada en {PublicacionAlbum!.CategoriaAlbum} → {PublicacionAlbum.RegistroAlbum}"
            : "No publicada";

        [JsonIgnore]
        public bool SeleccionadaParaAlbum
        {
            get => seleccionadaParaAlbum;
            set => SetField(ref seleccionadaParaAlbum, value);
        }

        [JsonIgnore]
        public string CalidadTecnica
        {
            get => calidadTecnica;
            set => SetField(ref calidadTecnica, value ?? "NO_EVALUABLE");
        }

        [JsonIgnore]
        public bool EsEvidenciaValida
        {
            get => esEvidenciaValida;
            set => SetField(ref esEvidenciaValida, value);
        }

        [JsonIgnore]
        public bool AptaParaAlbum
        {
            get => aptaParaAlbum;
            set
            {
                if (SetField(ref aptaParaAlbum, value) && !value)
                    SeleccionadaParaAlbum = false;
            }
        }

        [JsonIgnore]
        public string ObservacionAprobador
        {
            get => observacionAprobador;
            set => SetField(ref observacionAprobador, value ?? string.Empty);
        }

        [JsonIgnore]
        public string DescripcionAlbum
        {
            get => descripcionAlbum;
            set => SetField(ref descripcionAlbum, value ?? string.Empty);
        }

        [JsonIgnore]
        public bool EsPortada
        {
            get => esPortada;
            set => SetField(ref esPortada, value);
        }

        [JsonIgnore]
        public int OrdenAlbum
        {
            get => ordenAlbum;
            set => SetField(ref ordenAlbum, value);
        }

        public void AplicarEvaluacionExistente()
        {
            if (UltimaEvaluacion == null)
                return;

            CalidadTecnica = UltimaEvaluacion.CalidadTecnica;
            EsEvidenciaValida = UltimaEvaluacion.EsEvidenciaValida;
            AptaParaAlbum = UltimaEvaluacion.AptaParaAlbum;
            ObservacionAprobador = UltimaEvaluacion.Observacion;
            SeleccionadaParaAlbum = AptaParaAlbum && !Publicada;
            OrdenAlbum = Orden;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public sealed class DiagnosticoIAImagenResultadoItem
    {
        public int DiagnosticoIAImagenResultadoIAId { get; set; }
        public bool ImagenValida { get; set; }
        public bool ParecePlantaCafe { get; set; }
        public bool ResultadoConcluyente { get; set; }
        public string PartePlanta { get; set; } = string.Empty;
        public string CalidadEvaluacion { get; set; } = string.Empty;
        public string EstadoGeneral { get; set; } = string.Empty;
        public string CategoriaPrincipal { get; set; } = string.Empty;
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string DiagnosticoProbable { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string SeveridadVisual { get; set; } = string.Empty;
        public string NivelCerteza { get; set; } = string.Empty;
        public int? CategoriaAlbumBotanicoIdSugerida { get; set; }
        public int? AlbumBotanicoCafeIdSugerido { get; set; }
        public string CategoriaAlbumSugerida { get; set; } = string.Empty;
        public string ClasificacionAlbumSugerida { get; set; } = string.Empty;
        public string NombreCientificoSugerido { get; set; } = string.Empty;
        public bool CoincideCatalogoAlbum { get; set; }
        public bool RequiereDecisionClasificacion { get; set; }
        public string MotivoClasificacionAlbum { get; set; } = string.Empty;
        public int? CategoriaAlbumBotanicoIdSeleccionada { get; set; }
        public int? AlbumBotanicoCafeIdSeleccionado { get; set; }
        public string CategoriaAlbumSeleccionada { get; set; } = string.Empty;
        public string ClasificacionAlbumSeleccionada { get; set; } = string.Empty;
        public string EstadoClasificacionAlbum { get; set; } = string.Empty;
        public string ResumenImagen { get; set; } = string.Empty;
        public List<string> SintomasVisibles { get; set; } = [];
        public List<string> EvidenciasObservadas { get; set; } = [];
        public List<string> EvidenciasNoObservadas { get; set; } = [];
        public List<string> DiagnosticosAlternativos { get; set; } = [];
        public List<string> InformacionFaltante { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public DateTime FechaResultadoUtc { get; set; }

        [JsonIgnore]
        public string DiagnosticoTexto =>
            string.IsNullOrWhiteSpace(DiagnosticoProbable)
                ? "Sin resultado individual"
                : DiagnosticoProbable;

        [JsonIgnore]
        public string ParteTexto =>
            string.IsNullOrWhiteSpace(PartePlanta)
                ? "Parte no identificada"
                : PartePlanta.Replace('_', ' ');

        [JsonIgnore]
        public string ClasificacionTexto =>
            $"{EstadoGeneral.Replace('_', ' ')} · " +
            $"{CategoriaPrincipal.Replace('_', ' ')}";

        [JsonIgnore]
        public string CertezaTexto =>
            $"Certeza: {NivelCerteza.Replace('_', ' ')}";

        [JsonIgnore]
        public bool ClasificacionAlbumPendiente =>
            RequiereDecisionClasificacion &&
            EstadoClasificacionAlbum is
                "PENDIENTE_ANALIZADOR" or
                "PENDIENTE_DECISION_TECNICO";

        [JsonIgnore]
        public bool ClasificacionAlbumPropuesta =>
            string.Equals(
                EstadoClasificacionAlbum,
                "PROPUESTA_ANALIZADOR",
                StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool ClasificacionAlbumResuelta =>
            !ClasificacionAlbumPendiente &&
            !ClasificacionAlbumPropuesta &&
            !string.IsNullOrWhiteSpace(ClasificacionAlbumSeleccionada);

        [JsonIgnore]
        public bool PuedeClasificarPorAnalizador =>
            ClasificacionAlbumPendiente ||
            ClasificacionAlbumPropuesta;

        [JsonIgnore]
        public string ClasificacionAlbumTexto =>
            ClasificacionAlbumResuelta
                ? $"{CategoriaAlbumSeleccionada} → {ClasificacionAlbumSeleccionada}"
                : string.IsNullOrWhiteSpace(ClasificacionAlbumSugerida)
                    ? "Sin coincidencia con el catálogo activo."
                    : ClasificacionAlbumPropuesta
                        ? $"Propuesta del analizador: {CategoriaAlbumSugerida} → {ClasificacionAlbumSugerida}"
                        : $"Sugerencia de Gemini: {CategoriaAlbumSugerida} → {ClasificacionAlbumSugerida}";

        [JsonIgnore]
        public string EstadoClasificacionAlbumTexto =>
            EstadoClasificacionAlbum switch
            {
                "RESUELTA_AUTOMATICA" =>
                    "Coincidencia automática con el Álbum Botánico",
                "RESUELTA_POR_ANALIZADOR" =>
                    "Ficha confirmada por el analizador",
                "PROPUESTA_ANALIZADOR" =>
                    "Propuesta pendiente de decisión del aprobador",
                "RESUELTA_POR_APROBADOR" =>
                    "Ficha existente seleccionada por el aprobador",
                "CREADA_POR_APROBADOR" =>
                    "Ficha nueva autorizada por el aprobador",
                "RESUELTA_POR_TECNICO" =>
                    "Clasificación histórica confirmada por el técnico",
                "CREADA_DESDE_INSPECCION" =>
                    "Ficha histórica creada desde la inspección",
                "NO_APLICA" =>
                    "No requiere clasificación en el álbum",
                _ =>
                    "Pendiente de clasificación humana por el analizador"
            };

        [JsonIgnore]
        public bool TieneResumen =>
            !string.IsNullOrWhiteSpace(ResumenImagen);
    }

    public sealed class DiagnosticoIARevisionItem
    {
        public int DiagnosticoIARevisionId { get; set; }
        public int UsuarioAnalizadorId { get; set; }
        public string UsuarioAnalizador { get; set; } = string.Empty;
        public string RetroalimentacionAnalizador { get; set; } = string.Empty;
        public string DiagnosticoPropuestoAnalizador { get; set; } = string.Empty;
        public DateTime FechaSolicitudRevisionUtc { get; set; }
        public DateTime? FechaRespuestaRevisionUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool ImagenValida { get; set; }
        public bool ResultadoConcluyente { get; set; }
        public bool MantieneVeredictoOriginal { get; set; }
        public string RelacionConCriterioTecnico { get; set; } = string.Empty;
        public string CalidadEvaluacion { get; set; } = string.Empty;
        public string EstadoGeneral { get; set; } = string.Empty;
        public string CategoriaPrincipal { get; set; } = string.Empty;
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string DiagnosticoRevisado { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string SeveridadVisual { get; set; } = string.Empty;
        public string NivelCoincidencia { get; set; } = string.Empty;
        public string ResumenRevision { get; set; } = string.Empty;
        public List<string> PartesAfectadas { get; set; } = [];
        public List<string> EvidenciasApoyo { get; set; } = [];
        public List<string> EvidenciasContradiccion { get; set; } = [];
        public List<string> InformacionFaltante { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public string ErrorRevision { get; set; } = string.Empty;

        [JsonIgnore]
        public string FechaTexto =>
            (FechaRespuestaRevisionUtc ?? FechaSolicitudRevisionUtc)
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");

        [JsonIgnore]
        public string CambioTexto => MantieneVeredictoOriginal
            ? "Gemini mantiene el primer veredicto"
            : "Gemini modificó el primer veredicto";
    }

    public sealed class DiagnosticoIAAnalisisHumanoItem
    {
        public int DiagnosticoIAAnalisisHumanoId { get; set; }
        public int UsuarioAnalizadorId { get; set; }
        public string UsuarioAnalizador { get; set; } = string.Empty;
        public int Version { get; set; }
        public string EstadoRegistro { get; set; } = string.Empty;
        public string CalidadEvaluacion { get; set; } = string.Empty;
        public string EstadoGeneral { get; set; } = string.Empty;
        public string CategoriaPrincipal { get; set; } = string.Empty;
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string DiagnosticoPropuesto { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string SeveridadPropuesta { get; set; } = string.Empty;
        public string NivelCerteza { get; set; } = string.Empty;
        public List<string> PartesAfectadas { get; set; } = [];
        public List<string> EvidenciasObservadas { get; set; } = [];
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime FechaActualizacionUtc { get; set; }
        public DateTime? FechaEnvioUtc { get; set; }

        [JsonIgnore]
        public string Encabezado =>
            $"Versión {Version} · {EstadoRegistro.Replace('_', ' ')}";

        [JsonIgnore]
        public string FechaTexto =>
            FechaActualizacionUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class DiagnosticoIAAprobacionItem
    {
        public int DiagnosticoIAAprobacionId { get; set; }
        public int DiagnosticoIAAnalisisHumanoId { get; set; }
        public int UsuarioAprobadorId { get; set; }
        public string UsuarioAprobador { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string CalidadEvaluacionFinal { get; set; } = string.Empty;
        public string EstadoGeneralFinal { get; set; } = string.Empty;
        public string CategoriaPrincipalFinal { get; set; } = string.Empty;
        public List<string> CategoriasSecundariasFinales { get; set; } = [];
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public string TipoDiagnosticoFinal { get; set; } = string.Empty;
        public string SeveridadFinal { get; set; } = string.Empty;
        public string NivelCertezaFinal { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool AutorizaPublicacionAlbum { get; set; }
        public bool MismoUsuarioQueAnalizo { get; set; }
        public DateTime FechaAprobacionUtc { get; set; }
        public List<DiagnosticoIAImagenEvaluacionItem> EvaluacionesImagen { get; set; } = [];

        [JsonIgnore]
        public string FechaTexto =>
            FechaAprobacionUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        [JsonIgnore]
        public string SeparacionTexto => MismoUsuarioQueAnalizo
            ? "La misma persona analizó y aprobó; permitido por sus permisos."
            : "Análisis y aprobación realizados por personas distintas.";
    }

    public sealed class DiagnosticoIAImagenEvaluacionItem
    {
        public int DiagnosticoIAImagenEvaluacionId { get; set; }
        public int DiagnosticoIAAprobacionId { get; set; }
        public int DiagnosticoIAImagenId { get; set; }
        public int UsuarioAprobadorId { get; set; }
        public string UsuarioAprobador { get; set; } = string.Empty;
        public string CalidadTecnica { get; set; } = string.Empty;
        public bool EsEvidenciaValida { get; set; }
        public bool AptaParaAlbum { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaEvaluacionUtc { get; set; }
    }

    public sealed class DiagnosticoIAAlbumPublicacionItem
    {
        public int DiagnosticoIAAlbumPublicacionId { get; set; }
        public int DiagnosticoIAImagenId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string CategoriaAlbum { get; set; } = string.Empty;
        public int AlbumBotanicoCafeId { get; set; }
        public string RegistroAlbum { get; set; } = string.Empty;
        public int AlbumBotanicoCafeFotoId { get; set; }
        public int UsuarioPublicacionId { get; set; }
        public string UsuarioPublicacion { get; set; } = string.Empty;
        public DateTime FechaPublicacionUtc { get; set; }
        public string DescripcionPublicacion { get; set; } = string.Empty;
        public string RutaFotoAlbum { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public sealed class DiagnosticoIAHistorialItem
    {
        public int DiagnosticoIAHistorialId { get; set; }
        public int UsuarioId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }

        [JsonIgnore]
        public string FechaTexto =>
            FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class DiagnosticoIACatalogos
    {
        public List<string> CalidadEvaluacion { get; set; } = [];
        public List<string> EstadosGenerales { get; set; } = [];
        public List<string> Categorias { get; set; } = [];
        public List<string> Severidades { get; set; } = [];
        public List<string> NivelesCerteza { get; set; } = [];
        public List<string> DecisionesAprobacion { get; set; } = [];
        public List<string> CalidadesImagen { get; set; } = [];
        public List<string> PartesPlantaSugeridas { get; set; } = [];
        public int MaximoFotografiasPorInspeccion { get; set; } = 40;
        public int TamanoBloqueIA { get; set; } = 6;
    }

    public sealed class DiagnosticoIAAlbumCategoria
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;

        public override string ToString() => NombreCategoria;
    }

    public sealed class DiagnosticoIAAlbumRegistro
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Titulo { get; set; } = string.Empty;

        public override string ToString() => Titulo;
    }

    public sealed class DiagnosticoIAAlbumCatalogo
    {
        public List<DiagnosticoIAAlbumCategoria> Categorias { get; set; } = [];
        public List<DiagnosticoIAAlbumRegistro> Registros { get; set; } = [];
    }

    public sealed class FotoDiagnosticoSeleccionada :
        INotifyPropertyChanged
    {
        private string tipoFotografia = "EVIDENCIA";

        public string RutaLocal { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string TipoContenido { get; set; } = "image/jpeg";

        public string TipoFotografia
        {
            get => tipoFotografia;
            set
            {
                string nuevo = string.IsNullOrWhiteSpace(value)
                    ? "EVIDENCIA"
                    : value;

                if (tipoFotografia == nuevo)
                    return;

                tipoFotografia = nuevo;
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        nameof(TipoFotografia)));
            }
        }

        [JsonIgnore]
        public ImageSource VistaPrevia =>
            ImageSource.FromFile(RutaLocal);

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class DiagnosticoIAAnalisisHumanoRequest
    {
        public string CalidadEvaluacion { get; set; } = string.Empty;
        public string EstadoGeneral { get; set; } = string.Empty;
        public string CategoriaPrincipal { get; set; } = string.Empty;
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string DiagnosticoPropuesto { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string SeveridadPropuesta { get; set; } = string.Empty;
        public string NivelCerteza { get; set; } = string.Empty;
        public List<string> PartesAfectadas { get; set; } = [];
        public List<string> EvidenciasObservadas { get; set; } = [];
        public string Observaciones { get; set; } = string.Empty;
    }

    public sealed class DiagnosticoIAImagenEvaluacionRequest
    {
        public int DiagnosticoIAImagenId { get; set; }
        public string CalidadTecnica { get; set; } = string.Empty;
        public bool EsEvidenciaValida { get; set; }
        public bool AptaParaAlbum { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }

    public sealed class DiagnosticoIAAprobacionRequest
    {
        public string Decision { get; set; } = string.Empty;
        public string CalidadEvaluacionFinal { get; set; } = string.Empty;
        public string EstadoGeneralFinal { get; set; } = string.Empty;
        public string CategoriaPrincipalFinal { get; set; } = string.Empty;
        public List<string> CategoriasSecundariasFinales { get; set; } = [];
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public string TipoDiagnosticoFinal { get; set; } = string.Empty;
        public string SeveridadFinal { get; set; } = string.Empty;
        public string NivelCertezaFinal { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool AutorizaPublicacionAlbum { get; set; }
        public List<DiagnosticoIAImagenEvaluacionRequest> EvaluacionesImagen { get; set; } = [];
    }

    public sealed class DiagnosticoIAPublicarAlbumImagenRequest
    {
        public int DiagnosticoIAImagenId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool EsPortada { get; set; }
        public int Orden { get; set; }
    }

    public sealed class DiagnosticoIAPublicarAlbumRequest
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public int AlbumBotanicoCafeId { get; set; }
        public List<DiagnosticoIAPublicarAlbumImagenRequest> Imagenes { get; set; } = [];
    }

    public sealed class DiagnosticoIAPublicacionResultado
    {
        public int TotalPublicadas { get; set; }
        public int AlbumBotanicoCafeId { get; set; }
        public List<int> AlbumBotanicoCafeFotoIds { get; set; } = [];
    }

    public sealed class DiagnosticoIAProcesamientoEstado
    {
        public int DiagnosticoIAId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public int FotografiasProcesadas { get; set; }
        public int TotalFotografias { get; set; }
        public int Porcentaje { get; set; }
        public bool Finalizado { get; set; }
        public bool TieneError { get; set; }
        public DateTime FechaActualizacionUtc { get; set; }

        [JsonIgnore]
        public string ProgresoTexto => TotalFotografias <= 0
            ? Mensaje
            : $"{Mensaje} ({FotografiasProcesadas} de {TotalFotografias})";
    }

}
