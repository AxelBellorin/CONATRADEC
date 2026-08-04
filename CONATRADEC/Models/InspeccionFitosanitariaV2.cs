using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Models
{
    public static class InspeccionFotoEstados
    {
        public const string Borrador = "BORRADOR";
        public const string PendienteIA = "PENDIENTE_IA";
        public const string AnalizandoIA = "ANALIZANDO_IA";
        public const string ErrorIA = "ERROR_IA";
        public const string PendienteDecisionTecnico = "PENDIENTE_DECISION_TECNICO";
        public const string PendienteAnalizador = "PENDIENTE_ANALIZADOR";
        public const string EnAnalisisHumano = "EN_ANALISIS_HUMANO";
        public const string PendienteAprobacion = "PENDIENTE_APROBACION";
        public const string DevueltaAnalizador = "DEVUELTA_AL_ANALIZADOR";
        public const string Aprobada = "APROBADA";
        public const string AprobadaConCorreccion = "APROBADA_CON_CORRECCION";
        public const string Rechazada = "RECHAZADA";
        public const string NoConcluyente = "NO_CONCLUYENTE";
        public const string Descartada = "DESCARTADA";
        public const string PublicadaAlbum = "PUBLICADA_ALBUM";
    }

    public sealed class InspeccionFotoLocal : INotifyPropertyChanged
    {
        private DateTime fechaIdentificacionCampo = DateTime.Today;
        private string tipoFotografia = "EVIDENCIA";

        public string RutaLocal { get; init; } = string.Empty;
        public string NombreArchivo { get; init; } = string.Empty;
        public string TipoContenido { get; init; } = "image/jpeg";

        public DateTime FechaIdentificacionCampo
        {
            get => fechaIdentificacionCampo;
            set
            {
                if (fechaIdentificacionCampo == value.Date)
                    return;

                fechaIdentificacionCampo = value.Date;
                OnPropertyChanged();
            }
        }

        public string TipoFotografia
        {
            get => tipoFotografia;
            set
            {
                string nuevo = string.IsNullOrWhiteSpace(value)
                    ? "EVIDENCIA"
                    : value.Trim().ToUpperInvariant().Replace(' ', '_');

                if (tipoFotografia == nuevo)
                    return;

                tipoFotografia = nuevo;
                OnPropertyChanged();
            }
        }

        public ImageSource? Miniatura => string.IsNullOrWhiteSpace(RutaLocal)
            ? null
            : ImageSource.FromFile(RutaLocal);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class InspeccionFitosanitariaListaItemV2
    {
        public int InspeccionId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TotalFotografias { get; set; }
        public int Pendientes { get; set; }
        public int ConError { get; set; }
        public int Finalizadas { get; set; }
        public string UrlMiniatura { get; set; } = string.Empty;

        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Sin terreno vinculado"
            : $"Terreno {CodigoTerreno}";

        public string Resumen =>
            $"{TotalFotografias} fotos · {Pendientes} pendientes · " +
            $"{ConError} con error · {Finalizadas} finalizadas";

        public string FechaTexto =>
            FechaRegistroSistemaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    public sealed class InspeccionFotoResultadoIAV2
    {
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
        public string ResumenImagen { get; set; } = string.Empty;
        public List<string> SintomasVisibles { get; set; } = [];
        public List<string> EvidenciasObservadas { get; set; } = [];
        public List<string> EvidenciasNoObservadas { get; set; } = [];
        public List<string> DiagnosticosAlternativos { get; set; } = [];
        public List<string> InformacionFaltante { get; set; } = [];
        public List<string> RecomendacionesCaptura { get; set; } = [];
        public List<string> Advertencias { get; set; } = [];
        public DateTime? FechaAnalisisIAUtc { get; set; }

        public string DiagnosticoVisible => string.IsNullOrWhiteSpace(DiagnosticoProbable)
            ? "Sin diagnóstico preliminar"
            : DiagnosticoProbable;
    }

    public sealed class InspeccionFotoAnalisisHumanoV2
    {
        public int AnalisisHumanoId { get; set; }
        public int Version { get; set; }
        public int UsuarioAnalizadorId { get; set; }
        public string UsuarioAnalizador { get; set; } = string.Empty;
        public string EstadoRegistro { get; set; } = string.Empty;
        public string CalidadEvaluacion { get; set; } = string.Empty;
        public string EstadoGeneral { get; set; } = string.Empty;
        public string CategoriaPrincipal { get; set; } = string.Empty;
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string Diagnostico { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
        public string NivelCerteza { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime? FechaEnvioUtc { get; set; }
    }

    public sealed class InspeccionFotoAprobacionV2
    {
        public int AprobacionId { get; set; }
        public int UsuarioAprobadorId { get; set; }
        public string UsuarioAprobador { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool AutorizaPublicacionAlbum { get; set; }
        public bool MismoUsuarioQueAnalizo { get; set; }
        public DateTime FechaAprobacionUtc { get; set; }
    }

    public sealed class InspeccionFotoHistorialV2
    {
        public int HistorialId { get; set; }
        public int UsuarioId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }
    }

    public sealed class InspeccionFotoV2 : INotifyPropertyChanged
    {
        private bool seleccionada;

        public int FotografiaId { get; set; }
        public int Orden { get; set; }
        public string TipoFotografia { get; set; } = string.Empty;
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string UrlImagen { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaIdentificacionCampo { get; set; }
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public DateTime? FechaAnalisisIAUtc { get; set; }
        public DateTime? FechaAnalisisHumanoUtc { get; set; }
        public DateTime? FechaAprobacionUtc { get; set; }
        public string ModeloIAUtilizado { get; set; } = string.Empty;
        public int IntentosIA { get; set; }
        public string ErrorProcesamiento { get; set; } = string.Empty;
        public bool Descartada { get; set; }
        public string MotivoDescarte { get; set; } = string.Empty;
        public bool PublicadaAlbum { get; set; }
        public InspeccionFotoResultadoIAV2? ResultadoIA { get; set; }
        public InspeccionFotoAnalisisHumanoV2? UltimoAnalisisHumano { get; set; }
        public InspeccionFotoAprobacionV2? UltimaAprobacion { get; set; }
        public List<InspeccionFotoHistorialV2> Historial { get; set; } = [];

        public bool Seleccionada
        {
            get => seleccionada;
            set
            {
                if (seleccionada == value)
                    return;

                seleccionada = value;
                OnPropertyChanged();
            }
        }

        public bool TieneResultadoIA => ResultadoIA != null;
        public bool TieneError => !string.IsNullOrWhiteSpace(ErrorProcesamiento);
        public bool TieneAnalisisHumano => UltimoAnalisisHumano != null;
        public bool TieneAprobacion => UltimaAprobacion != null;
        public bool PuedeSeleccionarse => !Descartada;
        public string Titulo => $"Fotografía {Orden} · {TipoFotografia.Replace('_', ' ')}";
        public string FechaCampoTexto => FechaIdentificacionCampo.HasValue
            ? $"Identificación en campo: {FechaIdentificacionCampo:dd/MM/yyyy}"
            : "Fecha de campo no indicada";
        public string DiagnosticoTexto => ResultadoIA?.DiagnosticoVisible ??
            "Pendiente de análisis IA";
        public string EstadoTexto => Estado.Replace('_', ' ');

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class InspeccionFitosanitariaDetalleV2
    {
        public int InspeccionId { get; set; }
        public int? TerrenoId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public int UsuarioSolicitanteId { get; set; }
        public string UsuarioSolicitante { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public List<InspeccionFotoV2> Fotografias { get; set; } = [];
        public bool PuedeGestionarSolicitud { get; set; }
        public bool PuedeAnalizar { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedePublicarAlbum { get; set; }

        public string Titulo => $"Inspección #{InspeccionId}";
        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Sin terreno vinculado"
            : $"Terreno {CodigoTerreno}";
    }

    public sealed class InspeccionOperacionItemV2
    {
        public int FotografiaId { get; set; }
        public bool Exitoso { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public sealed class InspeccionOperacionMasivaV2
    {
        public int TotalSolicitadas { get; set; }
        public int TotalExitosas { get; set; }
        public int TotalConError { get; set; }
        public List<InspeccionOperacionItemV2> Resultados { get; set; } = [];

        public string Resumen => TotalConError == 0
            ? $"{TotalExitosas} fotografías procesadas correctamente."
            : $"{TotalExitosas} correctas y {TotalConError} con error.";
    }

    public sealed class InspeccionFotoAnalisisHumanoRequestV2
    {
        public int FotografiaId { get; set; }
        public string CalidadEvaluacion { get; set; } = "NO_EVALUABLE";
        public string EstadoGeneral { get; set; } = "INDETERMINADA";
        public string CategoriaPrincipal { get; set; } = "NO_APLICA";
        public List<string> CategoriasSecundarias { get; set; } = [];
        public string Diagnostico { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public string Severidad { get; set; } = "NO_EVALUABLE";
        public string NivelCerteza { get; set; } = "NO_DETERMINADO";
        public string Observaciones { get; set; } = string.Empty;
    }

    public sealed class InspeccionFotoAprobacionRequestV2
    {
        public int FotografiaId { get; set; }
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
    }

    public sealed class InspeccionAlbumFichaV2
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;

        public string TextoSeleccion => string.IsNullOrWhiteSpace(NombreCientifico)
            ? $"{AlbumBotanicoCafeId} · {Titulo}"
            : $"{AlbumBotanicoCafeId} · {Titulo} ({NombreCientifico})";
    }

    public sealed class InspeccionAlbumCategoriaV2
    {
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<InspeccionAlbumFichaV2> Fichas { get; set; } = [];
        public string TextoSeleccion =>
            $"{CategoriaAlbumBotanicoId} · {Nombre}";
    }

    public sealed class ProveedorIAConfiguracionV2
    {
        public string Proveedor { get; set; } = "GEMINI";
        public string Protocolo { get; set; } = "GEMINI_NATIVO";
        public string BaseUrl { get; set; } =
            "https://generativelanguage.googleapis.com/";
        public string Endpoint { get; set; } =
            "v1beta/models/{model}:generateContent";
        public string ApiKey { get; set; } = string.Empty;
        public string ApiKeyMascara { get; set; } = string.Empty;
        public bool TieneApiKey { get; set; }
        public string ModeloPrincipal { get; set; } = "gemini-3.6-flash";
        public string ModeloRespaldo { get; set; } = "gemini-3.5-flash";
        public int TimeoutSegundos { get; set; } = 180;
        public bool Activo { get; set; } = true;
        public DateTime FechaModificacionUtc { get; set; }
        public int? UsuarioModificacionId { get; set; }
    }

    public sealed class ProveedorIAPruebaV2
    {
        public bool Exitoso { get; set; }
        public int CodigoHttp { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public long Milisegundos { get; set; }
    }

    internal sealed class ApiEnvelopeV2<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
