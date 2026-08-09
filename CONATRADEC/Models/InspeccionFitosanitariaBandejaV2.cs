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
        public int? TecnicoId { get; set; }
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

        public bool EtapaTecnicaFinalizada { get; set; }
        public bool CerradaDefinitiva { get; set; }
        public string CodigoTerreno { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public int UsuarioTecnicoId { get; set; }
        public string TecnicoNombreCompleto { get; set; } = string.Empty;
        public string TecnicoUsuario { get; set; } = string.Empty;
        public DateTime FechaRegistroSistemaUtc { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TotalFotografias { get; set; }
        public int Pendientes { get; set; }
        public int ConError { get; set; }
        public int Finalizadas { get; set; }
        public int RequierenDecisionTecnico { get; set; }
        public int EnviadasRevision { get; set; }
        public int PendientesAprobacion { get; set; }
        public int EnviadasAprobador { get; set; }
        public int Procesando { get; set; }
        public int Descartadas { get; set; }
        public string UrlMiniatura { get; set; } = string.Empty;
        public int? UsuarioAnalizadorAsignadoId { get; set; }
        public string AnalizadorAsignado { get; set; } = string.Empty;
        public int? UsuarioAprobadorAsignadoId { get; set; }
        public string AprobadorAsignado { get; set; } = string.Empty;
        public string VersionAsignacion { get; set; } = string.Empty;

        /// <summary>
        /// Identifica una inspección creada en el dispositivo sin conexión. Su
        /// identificador es temporal y negativo hasta que la cola la envíe al
        /// servidor. Nunca se mezcla con identificadores reales del backend.
        /// </summary>
        public bool EsLocalPendiente { get; set; }

        public string AsignacionTexto
        {
            get
            {
                if (EsLocalPendiente)
                    return "Pendiente de sincronización con el servidor";

                if (!string.IsNullOrWhiteSpace(AprobadorAsignado))
                    return $"Aprobador asignado: {AprobadorAsignado.Trim()}";

                if (!string.IsNullOrWhiteSpace(AnalizadorAsignado))
                    return $"Analizador asignado: {AnalizadorAsignado.Trim()}";

                return "Sin asignación; el primer usuario que actúe tomará el expediente";
            }
        }

        public bool TieneAsignacion =>
            UsuarioAnalizadorAsignadoId is > 0 ||
            UsuarioAprobadorAsignadoId is > 0;

        public bool TieneDecisionesTecnicas =>
            !EsLocalPendiente &&
            !CerradaDefinitiva &&
            !EtapaTecnicaFinalizada &&
            RequierenDecisionTecnico > 0;

        public bool TieneProcesamientoActivo =>
            !EsLocalPendiente &&
            !CerradaDefinitiva && Procesando > 0;

        /// <summary>
        /// En la bandeja del aprobador, una revisión puede haber terminado su
        /// decisión técnica y aun conservar administración posterior del Álbum.
        /// </summary>
        public bool TieneGestionPosteriorAlbum =>
            !EsLocalPendiente &&
            EtapaTecnicaFinalizada &&
            PendientesAprobacion == 0 &&
            (Estado is "FINALIZADA" or "FINALIZADA_PARCIALMENTE");

        public string NombreInspeccionTexto =>
            string.IsNullOrWhiteSpace(NombreInspeccion)
                ? EsLocalPendiente
                    ? "Inspección guardada sin conexión"
                    : $"Inspección #{InspeccionId}"
                : NombreInspeccion.Trim();

        public string TerrenoTexto => string.IsNullOrWhiteSpace(CodigoTerreno)
            ? "Terreno no disponible (registro anterior)"
            : $"Terreno {CodigoTerreno}";

        public string PropietarioTexto => string.IsNullOrWhiteSpace(Propietario)
            ? EsLocalPendiente
                ? "Propietario disponible al sincronizar"
                : "Sin propietario vinculado"
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

        /// <summary>
        /// Nombre del usuario que registró la inspección. La tarjeta muestra el
        /// nombre completo sin anteponer un rol, porque el responsable es el
        /// usuario creador del expediente y no necesariamente un rol llamado
        /// técnico.
        /// </summary>
        public string UsuarioCreadorTexto
        {
            get
            {
                string nombre = TecnicoNombreCompleto?.Trim() ?? string.Empty;
                string usuario = TecnicoUsuario?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(nombre))
                    return nombre;

                if (!string.IsNullOrWhiteSpace(usuario))
                    return usuario;

                return UsuarioTecnicoId > 0
                    ? $"Usuario #{UsuarioTecnicoId}"
                    : "Usuario no disponible";
            }
        }

        /// <summary>
        /// Alias de compatibilidad para vistas anteriores.
        /// </summary>
        public string TecnicoTexto => UsuarioCreadorTexto;

        public string EstadoTexto
        {
            get
            {
                if (EsLocalPendiente)
                    return "Guardada sin conexión";

                if (CerradaDefinitiva)
                    return "Cerrada definitivamente";

                if (EtapaTecnicaFinalizada)
                {
                    return Estado switch
                    {
                        "PENDIENTE_APROBACION" => "Pendiente de aprobación",
                        "FINALIZADA" => "Decisión técnica completada",
                        "FINALIZADA_PARCIALMENTE" =>
                            "Decisión técnica completada parcialmente",
                        _ => "En revisión humana"
                    };
                }

                if (RequierenDecisionTecnico > 0)
                {
                    return RequierenDecisionTecnico == 1
                        ? "1 decisión técnica pendiente"
                        : $"{RequierenDecisionTecnico} decisiones técnicas pendientes";
                }

                if (Procesando > 0)
                    return "Análisis IA en proceso";

                return Estado switch
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
            }
        }

        public string EstadoFondo =>
            EsLocalPendiente
                ? "#FFF8E2"
                : CerradaDefinitiva
                    ? "#EEF2F0"
                    : TieneDecisionesTecnicas
                        ? "#FFF5D6"
                        : ConError > 0
                            ? "#FDECEC"
                            : EtapaTecnicaFinalizada
                                ? "#EAF3EF"
                                : TieneProcesamientoActivo
                                    ? "#EDF4FF"
                                    : "#FFF4EA";

        public string EstadoColor =>
            EsLocalPendiente
                ? "#705A19"
                : CerradaDefinitiva
                    ? "#52625D"
                    : TieneDecisionesTecnicas
                        ? "#7A5A13"
                        : ConError > 0
                            ? "#B42318"
                            : EtapaTecnicaFinalizada
                                ? "#315E52"
                                : TieneProcesamientoActivo
                                    ? "#315B86"
                                    : "#9B552C";

        public string TextoAbrir =>
            EsLocalPendiente
                ? "Pendiente de sincronizar"
                : CerradaDefinitiva
                    ? "Consultar expediente"
                    : TieneDecisionesTecnicas
                        ? "Atender decisiones"
                        : EtapaTecnicaFinalizada
                            ? "Ver avance de revisión"
                            : "Abrir inspección";

        /// <summary>
        /// Texto exclusivo de la bandeja del aprobador. Una decisión técnica
        /// terminada sigue permitiendo administrar clasificación, autorización
        /// y publicación del Álbum sin reabrir la aprobación original.
        /// </summary>
        public string TextoAbrirAprobador => TieneGestionPosteriorAlbum
            ? "Administrar resultado"
            : "Abrir revisión";

        public string AyudaAlbumAprobador => TieneGestionPosteriorAlbum
            ? "Decisión técnica cerrada · clasificación, autorización y publicación del Álbum se administran por separado."
            : string.Empty;

        public string Resumen =>
            EsLocalPendiente
                ? $"{TotalFotografias} fotos · guardada en el dispositivo"
                : $"{TotalFotografias} fotos · " +
                  $"{RequierenDecisionTecnico} por decidir · " +
                  $"{EnviadasRevision} enviadas · " +
                  $"{Finalizadas} finalizadas";

        public string ProgresoTexto
        {
            get
            {
                if (EsLocalPendiente)
                    return "Se enviará automáticamente al servidor durante una sesión en línea";

                if (TotalFotografias <= 0)
                    return "Sin fotografías registradas";

                if (CerradaDefinitiva)
                    return "Expediente finalizado y disponible solo para consulta";

                if (TieneDecisionesTecnicas)
                {
                    string decisiones = RequierenDecisionTecnico == 1
                        ? "1 fotografía requiere una decisión"
                        : $"{RequierenDecisionTecnico} fotografías requieren decisión";

                    return decisiones;
                }

                if (EtapaTecnicaFinalizada)
                    return "La etapa técnica ya fue enviada al analizador";

                if (Procesando > 0)
                {
                    return Procesando == 1
                        ? "1 fotografía está siendo procesada por la IA"
                        : $"{Procesando} fotografías están siendo procesadas por la IA";
                }

                return "Abra la inspección para continuar el flujo por fotografía";
            }
        }

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
