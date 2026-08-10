using System.Collections.Concurrent;
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
        public const string PendienteDecisionTecnico =
            "PENDIENTE_DECISION_TECNICO";
        public const string PendienteAnalizador = "PENDIENTE_ANALIZADOR";
        public const string EnAnalisisHumano = "EN_ANALISIS_HUMANO";
        public const string PendienteAprobacion = "PENDIENTE_APROBACION";
        public const string DevueltaAnalizador = "DEVUELTA_AL_ANALIZADOR";
        public const string DevueltaTecnico = "DEVUELTA_AL_TECNICO";
        public const string Aprobada = "APROBADA";
        public const string AprobadaConCorreccion =
            "APROBADA_CON_CORRECCION";
        public const string Rechazada = "RECHAZADA";
        public const string NoConcluyente = "NO_CONCLUYENTE";
        public const string Descartada = "DESCARTADA";
        public const string PublicadaAlbum = "PUBLICADA_ALBUM";
    }

    public static class InspeccionEstadosV2
    {
        public const string Borrador = "BORRADOR";
        public const string EnProceso = "EN_PROCESO";
        public const string Parcial = "PARCIAL";
        public const string PendienteRevision = "PENDIENTE_REVISION";
        public const string PendienteAprobacion = "PENDIENTE_APROBACION";
        public const string Finalizada = "FINALIZADA";
        public const string FinalizadaParcialmente =
            "FINALIZADA_PARCIALMENTE";

        public static string ObtenerTexto(string? estado) => estado switch
        {
            Borrador => "Borrador",
            EnProceso => "En proceso",
            Parcial => "Parcial",
            PendienteRevision => "Pendiente de revisión",
            PendienteAprobacion => "Pendiente de aprobación",
            Finalizada => "Finalizada",
            FinalizadaParcialmente => "Finalizada parcialmente",
            _ => (estado ?? string.Empty).Replace('_', ' ')
        };

        public static string ObtenerTextoFotografia(string? estado) => estado switch
        {
            InspeccionFotoEstados.Borrador => "Borrador",
            InspeccionFotoEstados.PendienteIA => "Pendiente de análisis IA",
            InspeccionFotoEstados.AnalizandoIA => "Analizando con IA",
            InspeccionFotoEstados.ErrorIA => "Error en análisis IA",
            InspeccionFotoEstados.PendienteDecisionTecnico => "Pendiente de decisión técnica",
            InspeccionFotoEstados.PendienteAnalizador => "Pendiente de analizador",
            InspeccionFotoEstados.EnAnalisisHumano => "En análisis humano",
            InspeccionFotoEstados.PendienteAprobacion => "Pendiente de aprobación",
            InspeccionFotoEstados.DevueltaAnalizador => "Devuelta al analizador",
            InspeccionFotoEstados.DevueltaTecnico => "Devuelta al técnico",
            InspeccionFotoEstados.Aprobada => "Aprobada",
            InspeccionFotoEstados.AprobadaConCorreccion => "Aprobada con corrección",
            InspeccionFotoEstados.Rechazada => "Rechazada",
            InspeccionFotoEstados.NoConcluyente => "No concluyente",
            InspeccionFotoEstados.Descartada => "Descartada",
            InspeccionFotoEstados.PublicadaAlbum => "Publicada en Álbum",
            _ => (estado ?? string.Empty).Replace('_', ' ')
        };

        public static string ObtenerTextoInspeccion(
            string? estado,
            bool etapaTecnicaFinalizada,
            IEnumerable<InspeccionFotoV2>? fotografias)
        {
            if (fotografias != null)
            {
                List<InspeccionFotoV2> fotos = fotografias.ToList();

                if (!etapaTecnicaFinalizada)
                {
                    if (fotos.Any(item => item.Estado is InspeccionFotoEstados.PendienteDecisionTecnico or InspeccionFotoEstados.DevueltaTecnico or InspeccionFotoEstados.ErrorIA))
                        return "Pendiente de decisión técnica";

                    if (fotos.Any(item => item.Estado is InspeccionFotoEstados.AnalizandoIA or InspeccionFotoEstados.PendienteIA))
                        return "Análisis IA en proceso";

                    if (fotos.Any(item => item.Estado is InspeccionFotoEstados.PendienteAnalizador or InspeccionFotoEstados.EnAnalisisHumano or InspeccionFotoEstados.DevueltaAnalizador))
                        return "Pendiente de revisión";
                }

                if (fotos.Any(item => item.Estado == InspeccionFotoEstados.PendienteAprobacion))
                    return "Pendiente de aprobación";
            }

            return ObtenerTexto(estado);
        }
    }

    public sealed class InspeccionFotoLocal : INotifyPropertyChanged
    {
        private DateTime fechaIdentificacionCampo = DateTime.Today;
        private string tipoFotografia = "EVIDENCIA";
        private TipoFotografiaIAItem? tipoFotografiaSeleccionada;

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

        public TipoFotografiaIAItem? TipoFotografiaSeleccionada
        {
            get => tipoFotografiaSeleccionada;
            set
            {
                if (ReferenceEquals(tipoFotografiaSeleccionada, value))
                    return;

                tipoFotografiaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InstruccionTipoFotografia));

                if (value != null)
                    TipoFotografia = value.Codigo;
            }
        }

        public string InstruccionTipoFotografia =>
            string.IsNullOrWhiteSpace(
                TipoFotografiaSeleccionada?.InstruccionIA)
                ? "Seleccione un tipo de fotografía para conocer qué detalles priorizará la IA."
                : TipoFotografiaSeleccionada!.InstruccionIA;

        public ImageSource? Miniatura => string.IsNullOrWhiteSpace(RutaLocal)
            ? null
            : ImageSource.FromFile(RutaLocal);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
    }

    public sealed class InspeccionFitosanitariaListaItemV2
    {
        public int InspeccionId { get; set; }
        public string NombreInspeccion { get; set; } = string.Empty;
        public string CodigoTerreno { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool EtapaTecnicaFinalizada { get; set; }
        public DateTime? FechaFinEtapaTecnicaUtc { get; set; }
        public bool CerradaDefinitiva { get; set; }
        public DateTime? FechaCierreDefinitivoUtc { get; set; }
        public int TotalFotografias { get; set; }
        public int Pendientes { get; set; }
        public int ConError { get; set; }
        public int Finalizadas { get; set; }
        public string UrlMiniatura { get; set; } = string.Empty;
        public int? UsuarioAnalizadorAsignadoId { get; set; }
        public string AnalizadorAsignado { get; set; } = string.Empty;
        public int? UsuarioAprobadorAsignadoId { get; set; }
        public string AprobadorAsignado { get; set; } = string.Empty;

        public string NombreInspeccionTexto =>
            string.IsNullOrWhiteSpace(NombreInspeccion)
                ? $"Inspección #{InspeccionId}"
                : NombreInspeccion.Trim();

        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Terreno no disponible (registro anterior)"
            : $"Terreno {CodigoTerreno}";

        public string EstadoTexto =>
            InspeccionEstadosV2.ObtenerTexto(Estado);

        public string CierreTexto => CerradaDefinitiva
            ? "Inspección cerrada definitivamente"
            : EtapaTecnicaFinalizada
                ? "Etapa técnica finalizada"
                : "Etapa técnica abierta";

        public string Resumen =>
            $"{TotalFotografias} fotos · {Pendientes} pendientes · " +
            $"{ConError} con error · {Finalizadas} finalizadas";

        public string FechaTexto =>
            FechaRegistroSistemaUtc.ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Lesión localizada en coordenadas normalizadas 0..1000 con el formato
    /// [ymin, xmin, ymax, xmax].
    /// </summary>
    public sealed class InspeccionLesionVisualV2
    {
        public string Id { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public List<int> Box2d { get; set; } = [];
    }

    /// <summary>
    /// Afectación individual dentro del único expediente de la fotografía.
    /// AccionHumana se utiliza durante revisión: CONFIRMAR, CORREGIR,
    /// DESCARTAR o AGREGAR.
    /// </summary>
    public sealed class InspeccionDiferencialVisualV2
    {
        public string Diagnostico { get; set; } = string.Empty;
        public string ColorMarcador { get; set; } = "#1E88E5";
        public List<InspeccionLesionVisualV2> Lesiones { get; set; } = [];
        public int TotalLesiones => Lesiones?.Count ?? 0;
    }

    public sealed class InspeccionDiagnosticoVisualV2
    {
        public string Id { get; set; } = string.Empty;
        public string IdOrigenIA { get; set; } = string.Empty;
        public string AccionHumana { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string TipoDiagnostico { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
        public string NivelCerteza { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
        public List<string> DiagnosticosDiferenciales { get; set; } = [];
        public List<InspeccionDiferencialVisualV2> DiferencialesLocalizados { get; set; } = [];
        public List<InspeccionLesionVisualV2> Lesiones { get; set; } = [];
        public string ColorMarcador { get; set; } = "#E53935";

        public int TotalLesiones => Lesiones?.Count ?? 0;

        public string PrincipalTexto => EsPrincipal ? "Principal" : "Secundaria";

        public string Resumen =>
            $"{Diagnostico} · {TotalLesiones} " +
            $"{(TotalLesiones == 1 ? "lesión" : "lesiones")} · " +
            $"certeza {NivelCerteza.ToLowerInvariant()}";
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
        public List<InspeccionDiagnosticoVisualV2> Diagnosticos { get; set; } = [];
        public bool LocalizacionVisualDisponible { get; set; }
        public int? VersionVisual { get; set; }

        public string DiagnosticoVisible =>
            string.IsNullOrWhiteSpace(DiagnosticoProbable)
                ? Diagnosticos.FirstOrDefault(item => item.EsPrincipal)?.Diagnostico ??
                  Diagnosticos.FirstOrDefault()?.Diagnostico ??
                  "Sin diagnóstico preliminar"
                : DiagnosticoProbable;

        public string ResumenDiagnosticos => Diagnosticos.Count switch
        {
            0 => DiagnosticoVisible,
            1 => Diagnosticos[0].Resumen,
            _ => $"{Diagnosticos.Count} afectaciones diferenciadas por IA"
        };

        public bool TieneDiagnosticos => Diagnosticos.Count > 0;

        public bool TieneMultiplesDiagnosticos => Diagnosticos.Count > 1;

        /// <summary>
        /// Resume todos los diagnósticos simultáneos para la tarjeta principal.
        /// Los diagnósticos diferenciales no se mezclan aquí porque continúan
        /// siendo posibilidades no confirmadas y se explican en el visor.
        /// </summary>
        public string DiagnosticosTarjeta
        {
            get
            {
                if (Diagnosticos.Count == 0)
                    return DiagnosticoVisible;

                if (Diagnosticos.Count == 1)
                    return Diagnosticos[0].Diagnostico;

                bool existePrincipal = Diagnosticos.Any(item => item.EsPrincipal);
                var lineas = new List<string>
                {
                    $"{Diagnosticos.Count} diagnósticos detectados por IA"
                };

                foreach (InspeccionDiagnosticoVisualV2 diagnostico in Diagnosticos)
                {
                    string rol = diagnostico.EsPrincipal
                        ? "Principal"
                        : existePrincipal
                            ? "Adicional"
                            : "Diagnóstico";

                    string nombre = string.IsNullOrWhiteSpace(diagnostico.Diagnostico)
                        ? "Afectación sin nombre"
                        : diagnostico.Diagnostico.Trim();

                    lineas.Add($"• {rol}: {nombre}");
                }

                return string.Join(Environment.NewLine, lineas);
            }
        }

        public bool EsAparentementeSana
        {
            get
            {
                if (string.Equals(
                        EstadoGeneral,
                        "APARENTEMENTE_SANA",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                bool categoriaNoAplica = string.Equals(
                    CategoriaPrincipal,
                    "NO_APLICA",
                    StringComparison.OrdinalIgnoreCase);

                bool diagnosticoSano =
                    DiagnosticoProbable.Contains(
                        "aparentemente sana",
                        StringComparison.OrdinalIgnoreCase) ||
                    DiagnosticoProbable.Contains(
                        "aparentemente sano",
                        StringComparison.OrdinalIgnoreCase);

                return categoriaNoAplica && diagnosticoSano;
            }
        }

        public bool TieneFichaAlbumCoincidente =>
            CoincideCatalogoAlbum &&
            AlbumBotanicoCafeIdSugerido is > 0;

        public bool RequiereGestionAlbum =>
            RequiereDecisionClasificacion ||
            (EsAparentementeSana && !TieneFichaAlbumCoincidente);

        public string CategoriaAlbumPropuesta =>
            !string.IsNullOrWhiteSpace(CategoriaAlbumSugerida)
                ? CategoriaAlbumSugerida.Trim()
                : EsAparentementeSana
                    ? "Plantas sanas"
                    : string.IsNullOrWhiteSpace(CategoriaPrincipal)
                        ? "Clasificación pendiente"
                        : CategoriaPrincipal.Replace('_', ' ');

        public string ClasificacionAlbumPropuesta =>
            !string.IsNullOrWhiteSpace(ClasificacionAlbumSugerida)
                ? ClasificacionAlbumSugerida.Trim()
                : !string.IsNullOrWhiteSpace(DiagnosticoProbable)
                    ? DiagnosticoProbable.Trim()
                    : EsAparentementeSana
                        ? "Planta de café aparentemente sana"
                        : "Nueva subcategoría por definir";

        public string MotivoAlbumPropuesta =>
            !string.IsNullOrWhiteSpace(MotivoClasificacionAlbum)
                ? MotivoClasificacionAlbum.Trim()
                : EsAparentementeSana
                    ? "La fotografía corresponde a una planta de café aparentemente sana, pero no existe una subcategoría compatible dentro de Plantas sanas."
                    : "La IA no encontró una subcategoría activa del Álbum Botánico que represente de forma segura este hallazgo.";
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
        public List<InspeccionDiagnosticoVisualV2> Diagnosticos { get; set; } = [];
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
        public List<InspeccionDiagnosticoVisualV2> DiagnosticosFinales { get; set; } = [];
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
        private JerarquiaDiagnosticoFotoResponse? jerarquiaAlbum;

        public int FotografiaId { get; set; }
        public int Orden { get; set; }
        public string TipoFotografia { get; set; } = string.Empty;
        public string NombreArchivoOriginal { get; set; } = string.Empty;
        public string UrlImagen { get; set; } = string.Empty;
        public string UrlImagenMarcadaIA { get; set; } = string.Empty;
        public bool TieneImagenMarcadaIA { get; set; }
        public int? VersionImagenMarcadaIA { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaIdentificacionCampo { get; set; }
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public DateTime? FechaAnalisisIAUtc { get; set; }
        public DateTime? FechaAnalisisHumanoUtc { get; set; }
        public DateTime? FechaAprobacionUtc { get; set; }
        public string ModeloIAUtilizado { get; set; } = string.Empty;
        public int IntentosIA { get; set; }

        // El análisis inicial no consume el límite configurado. Solo se cuentan
        // reevaluaciones adicionales completadas correctamente.
        public int RevisionesIACompletadas { get; set; }
        public int MaximoRevisionesIA { get; set; } = 2;
        public bool RevisionesIAIlimitadas { get; set; }
        public int RevisionesIARestantes { get; set; }
        public bool PuedeSolicitarRevisionIA { get; set; } = true;

        public string RevisionesIATexto
        {
            get
            {
                if (RevisionesIAIlimitadas)
                {
                    return RevisionesIACompletadas == 1
                        ? "Reevaluaciones IA ilimitadas · 1 completada"
                        : $"Reevaluaciones IA ilimitadas · {RevisionesIACompletadas} completadas";
                }

                int maximo = Math.Max(1, MaximoRevisionesIA);
                int utilizadas = Math.Max(0, RevisionesIACompletadas);
                string limite = utilizadas >= maximo
                    ? " · límite alcanzado"
                    : $" · {Math.Max(0, maximo - utilizadas)} restante(s)";

                return $"Reevaluaciones IA: {utilizadas} de {maximo} utilizadas{limite}";
            }
        }

        public string ErrorProcesamiento { get; set; } = string.Empty;
        public bool Descartada { get; set; }
        public string MotivoDescarte { get; set; } = string.Empty;
        public bool PublicadaAlbum { get; set; }
        public InspeccionFotoResultadoIAV2? ResultadoIA { get; set; }
        public InspeccionFotoAnalisisHumanoV2? UltimoAnalisisHumano { get; set; }
        public InspeccionFotoAprobacionV2? UltimaAprobacion { get; set; }
        public List<InspeccionFotoHistorialV2> Historial { get; set; } = [];

        public JerarquiaDiagnosticoFotoResponse? JerarquiaAlbum
        {
            get => jerarquiaAlbum;
            set
            {
                if (ReferenceEquals(jerarquiaAlbum, value))
                    return;

                jerarquiaAlbum = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneJerarquiaAlbum));
                OnPropertyChanged(nameof(TieneClasificacionAlbumCompleta));
                OnPropertyChanged(nameof(TieneClasificacionAlbumOficial));
            }
        }

        public bool TieneJerarquiaAlbum =>
            JerarquiaAlbum?.TieneClasificacion == true;

        public bool TieneClasificacionAlbumCompleta =>
            JerarquiaAlbum?.CategoriaAlbumBotanicoId is > 0 &&
            JerarquiaAlbum?.AlbumBotanicoCafeId is > 0 &&
            JerarquiaAlbum.CategoriaEsPropuesta == false &&
            JerarquiaAlbum.FichaEsPropuesta == false;

        public bool TieneClasificacionAlbumOficial =>
            TieneClasificacionAlbumCompleta &&
            string.Equals(
                JerarquiaAlbum?.Estado,
                "RESUELTA_APROBADOR",
                StringComparison.OrdinalIgnoreCase);

        public bool Seleccionada
        {
            get => seleccionada;
            set
            {
                bool nuevoValor = value && PuedeSeleccionarse;

                if (seleccionada == nuevoValor)
                    return;

                seleccionada = nuevoValor;
                OnPropertyChanged();
            }
        }

        public bool TieneResultadoIA => ResultadoIA != null;
        public bool TienePropuestaAlbum =>
            !Descartada &&
            !PublicadaAlbum &&
            !TieneAprobacion &&
            ResultadoIA?.RequiereGestionAlbum == true;
        public bool TieneError =>
            !string.IsNullOrWhiteSpace(ErrorProcesamiento);
        public bool TieneAnalisisHumano => UltimoAnalisisHumano != null;
        public bool TieneAprobacion => UltimaAprobacion != null;
        public bool TieneMarcadaIA =>
            TieneImagenMarcadaIA &&
            !string.IsNullOrWhiteSpace(UrlImagenMarcadaIA);

        public string LocalizacionVisualTexto => TieneMarcadaIA
            ? $"Localización IA disponible · revisión {VersionImagenMarcadaIA ?? ResultadoIA?.VersionVisual ?? 0}"
            : TieneResultadoIA
                ? "Localización visual no disponible para esta valoración."
                : string.Empty;

        public bool EsEstadoFinal => Estado is
            InspeccionFotoEstados.Aprobada or
            InspeccionFotoEstados.AprobadaConCorreccion or
            InspeccionFotoEstados.Rechazada or
            InspeccionFotoEstados.NoConcluyente or
            InspeccionFotoEstados.Descartada or
            InspeccionFotoEstados.PublicadaAlbum;

        public bool EstaAprobadaTecnicamente => Estado is
            InspeccionFotoEstados.Aprobada or
            InspeccionFotoEstados.AprobadaConCorreccion or
            InspeccionFotoEstados.PublicadaAlbum;

        public bool EstaProcesando =>
            Estado == InspeccionFotoEstados.AnalizandoIA;

        public bool PuedePublicarseEnAlbum =>
            !PublicadaAlbum &&
            UltimaAprobacion?.AutorizaPublicacionAlbum == true &&
            Estado is
                InspeccionFotoEstados.Aprobada or
                InspeccionFotoEstados.AprobadaConCorreccion;

        public bool EsSoloConsulta =>
            Descartada || EstaProcesando ||
            (EsEstadoFinal && !PuedePublicarseEnAlbum);

        public bool PuedeSeleccionarse => !EsSoloConsulta;

        public string DisponibilidadTexto => PublicadaAlbum ||
                                              Estado == InspeccionFotoEstados.PublicadaAlbum
            ? "Decisión técnica finalizada · publicada en el Álbum Botánico"
            : EstaAprobadaTecnicamente
                ? TieneClasificacionAlbumOficial
                    ? "Decisión técnica finalizada · clasificación oficial del Álbum confirmada"
                    : "Decisión técnica finalizada · clasificación del Álbum pendiente de confirmar"
                : EsEstadoFinal || Descartada
                    ? "Proceso finalizado · solo consulta"
                    : EstaProcesando
                        ? "Procesamiento en curso"
                        : Estado is
                            InspeccionFotoEstados.PendienteDecisionTecnico or
                            InspeccionFotoEstados.ErrorIA
                            ? RevisionesIATexto
                            : string.Empty;

        public bool TieneMensajeDisponibilidad =>
            !string.IsNullOrWhiteSpace(DisponibilidadTexto);

        public string Titulo =>
            $"Fotografía {Orden} · {TipoFotografia.Replace('_', ' ')}";

        public string FechaCampoTexto => FechaIdentificacionCampo.HasValue
            ? $"Identificación en campo: {FechaIdentificacionCampo:dd/MM/yyyy}"
            : "Fecha de campo no indicada";

        public string DiagnosticoTexto => ResultadoIA?.DiagnosticosTarjeta ??
            "Pendiente de análisis IA";

        public string EstadoTexto =>
            InspeccionEstadosV2.ObtenerTextoFotografia(Estado);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
    }

    public sealed class InspeccionFitosanitariaDetalleV2
    {
        public int InspeccionId { get; set; }
        public string NombreInspeccion { get; set; } = string.Empty;
        public int? TerrenoId { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public int UsuarioSolicitanteId { get; set; }
        public string UsuarioSolicitante { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public bool EtapaTecnicaFinalizada { get; set; }
        public DateTime? FechaFinEtapaTecnicaUtc { get; set; }
        public int? UsuarioFinEtapaTecnicaId { get; set; }
        public bool CerradaDefinitiva { get; set; }
        public DateTime? FechaCierreDefinitivoUtc { get; set; }
        public int? UsuarioCierreDefinitivoId { get; set; }
        public int? UsuarioAnalizadorAsignadoId { get; set; }
        public int? UsuarioAprobadorAsignadoId { get; set; }
        public string VersionAsignacion { get; set; } = string.Empty;
        public List<InspeccionFotoV2> Fotografias { get; set; } = [];
        public bool PuedeGestionarSolicitud { get; set; }
        public bool PuedeCerrarInspeccion { get; set; }
        public string MotivoNoPuedeCerrar { get; set; } = string.Empty;
        public bool PuedeAnalizar { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedePublicarAlbum { get; set; }

        public string Titulo => string.IsNullOrWhiteSpace(NombreInspeccion)
            ? $"Inspección #{InspeccionId}"
            : NombreInspeccion.Trim();

        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Terreno no disponible (registro anterior)"
            : $"Terreno {CodigoTerreno}";

        public string EstadoTexto =>
            InspeccionEstadosV2.ObtenerTextoInspeccion(
                Estado,
                EtapaTecnicaFinalizada,
                Fotografias);

        public string CierreTexto => CerradaDefinitiva
            ? "Inspección cerrada definitivamente"
            : EtapaTecnicaFinalizada
                ? "Etapa técnica finalizada"
                : "Etapa técnica abierta";
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

    /// <summary>
    /// Puente temporal entre la revisión guiada y el request existente. Permite
    /// que las acciones por diagnóstico viajen al backend sin cambiar la firma
    /// pública del flujo principal. Cada conjunto se consume una sola vez.
    /// </summary>
    public static class InspeccionDiagnosticosRevisionStore
    {
        private static readonly ConcurrentDictionary<
            int,
            List<InspeccionDiagnosticoVisualV2>> Pendientes = new();

        public static void Guardar(
            int fotografiaId,
            IEnumerable<InspeccionDiagnosticoVisualV2>? diagnosticos)
        {
            if (fotografiaId <= 0)
                return;

            List<InspeccionDiagnosticoVisualV2> copia =
                (diagnosticos ?? [])
                    .Select(Copiar)
                    .ToList();

            if (copia.Count == 0)
            {
                Pendientes.TryRemove(fotografiaId, out _);
                return;
            }

            Pendientes[fotografiaId] = copia;
        }

        public static List<InspeccionDiagnosticoVisualV2> Tomar(
            int fotografiaId)
        {
            if (fotografiaId <= 0 ||
                !Pendientes.TryRemove(fotografiaId, out var diagnosticos))
            {
                return [];
            }

            return diagnosticos.Select(Copiar).ToList();
        }

        public static void Limpiar(int fotografiaId)
        {
            if (fotografiaId > 0)
                Pendientes.TryRemove(fotografiaId, out _);
        }

        private static InspeccionDiagnosticoVisualV2 Copiar(
            InspeccionDiagnosticoVisualV2 origen) =>
            new()
            {
                Id = origen.Id,
                IdOrigenIA = origen.IdOrigenIA,
                AccionHumana = origen.AccionHumana,
                Diagnostico = origen.Diagnostico,
                Categoria = origen.Categoria,
                TipoDiagnostico = origen.TipoDiagnostico,
                EsPrincipal = origen.EsPrincipal,
                NivelCerteza = origen.NivelCerteza,
                Severidad = origen.Severidad,
                DiagnosticosDiferenciales =
                    (origen.DiagnosticosDiferenciales ?? []).ToList(),
                Lesiones = (origen.Lesiones ?? [])
                    .Select(lesion => new InspeccionLesionVisualV2
                    {
                        Id = lesion.Id,
                        Descripcion = lesion.Descripcion,
                        Box2d = (lesion.Box2d ?? []).ToList()
                    })
                    .ToList(),
                ColorMarcador = origen.ColorMarcador
            };
    }

    public sealed class InspeccionFotoAnalisisHumanoRequestV2
    {
        private List<InspeccionDiagnosticoVisualV2>? diagnosticos;

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

        public List<InspeccionDiagnosticoVisualV2> Diagnosticos
        {
            get => diagnosticos ??=
                InspeccionDiagnosticosRevisionStore.Tomar(FotografiaId);
            set => diagnosticos = value ?? [];
        }
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
        public List<InspeccionDiagnosticoVisualV2> DiagnosticosFinales { get; set; } = [];
    }

    public sealed class InspeccionAlbumFichaV2
    {
        public int AlbumBotanicoCafeId { get; set; }
        public int CategoriaAlbumBotanicoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;

        public string TextoSeleccion =>
            string.IsNullOrWhiteSpace(NombreCientifico)
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
