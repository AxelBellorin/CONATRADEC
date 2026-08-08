using CONATRADEC.Models;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class FitosanitariaOfflineResumen
    {
        public bool Preparado { get; init; }
        public DateTime? FechaPreparacionUtc { get; init; }
        public int InspeccionesPendientes { get; init; }
        public int FotografiasPendientes { get; init; }
        public string Mensaje { get; init; } = string.Empty;
    }

    public sealed class FitosanitariaOfflineSincronizacionResultado
    {
        public bool Success { get; init; }
        public int Enviadas { get; init; }
        public int Pendientes { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>
    /// Conserva la captura fitosanitaria realizada por el técnico durante una
    /// sesión sin conexión. La IA, el analizador y el aprobador continúan siendo
    /// operaciones centrales del servidor; únicamente la captura de campo queda
    /// en cola y se envía cuando el usuario vuelve a trabajar en línea.
    /// </summary>
    public sealed class FitosanitariaOfflineService
    {
        private const int VersionPreparacion = 1;
        private const string PrefijoPreparado =
            "fitosanitaria.offline.preparada.v1.";
        private const string PrefijoFecha =
            "fitosanitaria.offline.fecha.v1.";
        private const string PrefijoSiguienteId =
            "fitosanitaria.offline.siguiente_id.v1.";

        private static readonly Lazy<FitosanitariaOfflineService> lazy =
            new(() => new FitosanitariaOfflineService());

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

        private readonly SemaphoreSlim almacenamientoLock = new(1, 1);
        private readonly SemaphoreSlim sincronizacionLock = new(1, 1);
        private readonly object idLock = new();

        public static FitosanitariaOfflineService Instance => lazy.Value;

        public event EventHandler? ColaCambiada;

        private FitosanitariaOfflineService()
        {
        }

        public bool TienePermisoModulo =>
            DatosSinConexionPermisos.TienePermiso &&
            PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazSolicitud);

        public bool EstaPreparadoUsuarioActual
        {
            get
            {
                string usuarioId = ObtenerUsuarioId();
                if (string.IsNullOrWhiteSpace(usuarioId))
                    return false;

                /*
                 * La captura fitosanitaria reutiliza los terrenos del paquete
                 * global. Por ello una descarga general válida ya prepara el
                 * dispositivo, incluso si fue realizada con una versión previa
                 * de la aplicación antes de existir este marcador específico.
                 */
                return TienePermisoModulo &&
                       SincronizacionOfflineGlobalService
                           .EstaPreparadoParaUsuario(usuarioId);
            }
        }

        public async Task PrepararAsync(
            CancellationToken cancellationToken = default)
        {
            if (!TienePermisoModulo)
                return;

            string usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new InvalidOperationException(
                    "No fue posible identificar al usuario para preparar la captura fitosanitaria sin conexión.");
            }

            if (!SincronizacionOfflineGlobalService
                    .EstaPreparadoParaUsuario(usuarioId))
            {
                throw new InvalidOperationException(
                    "Primero debe completarse la descarga general de datos sin conexión.");
            }

            await almacenamientoLock.WaitAsync(cancellationToken);
            try
            {
                Directory.CreateDirectory(ObtenerDirectorioRegistros(usuarioId));
                Directory.CreateDirectory(ObtenerDirectorioFotos(usuarioId));

                DateTime ahora = DateTime.UtcNow;
                Preferences.Set(
                    ConstruirClave(PrefijoPreparado, usuarioId),
                    true);
                Preferences.Set(
                    ConstruirClave(PrefijoFecha, usuarioId),
                    ahora.ToString("O"));
            }
            finally
            {
                almacenamientoLock.Release();
            }
        }

        public async Task<FitosanitariaOfflineResumen> ObtenerResumenAsync(
            CancellationToken cancellationToken = default)
        {
            string usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return new FitosanitariaOfflineResumen
                {
                    Mensaje = "Usuario no disponible."
                };
            }

            List<InspeccionFitosanitariaOfflineRegistro> registros =
                await ObtenerRegistrosAsync(usuarioId, cancellationToken);

            bool preparado = EstaPreparadoUsuarioActual;
            DateTime? fecha = ObtenerFechaPreparacion(usuarioId);
            int fotos = registros.Sum(item => item.Fotos.Count);

            string mensaje;
            if (!TienePermisoModulo)
            {
                mensaje = "El usuario no tiene habilitado este módulo sin conexión.";
            }
            else if (!preparado)
            {
                mensaje = "Use Descargar todo para habilitar la captura de campo fitosanitaria.";
            }
            else if (registros.Count == 0)
            {
                mensaje = "Captura de campo preparada. No hay inspecciones pendientes de enviar.";
            }
            else
            {
                mensaje = registros.Count == 1
                    ? $"1 inspección y {fotos} fotografía(s) esperan sincronización."
                    : $"{registros.Count} inspecciones y {fotos} fotografía(s) esperan sincronización.";
            }

            return new FitosanitariaOfflineResumen
            {
                Preparado = preparado,
                FechaPreparacionUtc = fecha,
                InspeccionesPendientes = registros.Count,
                FotografiasPendientes = fotos,
                Mensaje = mensaje
            };
        }

        /// <summary>
        /// Atiende únicamente las rutas que pueden resolverse de forma local.
        /// Devuelve null para que la barrera general del modo sin conexión emita
        /// su respuesta normal en cualquier otra operación.
        /// </summary>
        public async Task<HttpResponseMessage?> IntentarProcesarSolicitudAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            string path = ObtenerPath(request);

            if (request.Method == HttpMethod.Post &&
                path == "/api/inspecciones-fitosanitarias")
            {
                if (!EstaPreparadoUsuarioActual)
                {
                    return CrearRespuestaError(
                        request,
                        HttpStatusCode.Conflict,
                        "La captura fitosanitaria sin conexión no está preparada. Inicie en línea y use Descargar todo.");
                }

                return await GuardarInspeccionDesdeHttpAsync(
                    request,
                    cancellationToken);
            }

            if (request.Method == HttpMethod.Get &&
                path == "/api/inspecciones-fitosanitarias/bandeja-paginada")
            {
                if (!EstaPreparadoUsuarioActual)
                {
                    return CrearRespuestaError(
                        request,
                        HttpStatusCode.Conflict,
                        "La captura fitosanitaria sin conexión no está preparada en este dispositivo.");
                }

                return await CrearRespuestaBandejaAsync(
                    request,
                    cancellationToken);
            }

            return null;
        }

        public void SolicitarSincronizacionEnSegundoPlano()
        {
            if (!ModoSesionService.EsEnLinea)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await SincronizarPendientesAsync();
                }
                catch
                {
                    // La cola permanece intacta y se reintentará al volver al módulo.
                }
            });
        }

        public async Task<FitosanitariaOfflineSincronizacionResultado>
            SincronizarPendientesAsync(
                CancellationToken cancellationToken = default)
        {
            if (!ModoSesionService.EsEnLinea)
            {
                return new FitosanitariaOfflineSincronizacionResultado
                {
                    Success = false,
                    Message = "La sincronización fitosanitaria requiere una sesión en línea."
                };
            }

            NetworkAccess acceso = Connectivity.Current.NetworkAccess;
#if WINDOWS
            bool hayRed = acceso != NetworkAccess.None;
#else
            bool hayRed = acceso == NetworkAccess.Internet;
#endif
            if (!hayRed)
            {
                return new FitosanitariaOfflineSincronizacionResultado
                {
                    Success = false,
                    Message = "No hay conexión disponible para enviar las inspecciones fitosanitarias pendientes."
                };
            }

            bool entered = await sincronizacionLock.WaitAsync(
                TimeSpan.Zero,
                cancellationToken);

            if (!entered)
            {
                FitosanitariaOfflineResumen actual =
                    await ObtenerResumenAsync(cancellationToken);

                return new FitosanitariaOfflineSincronizacionResultado
                {
                    Success = true,
                    Pendientes = actual.InspeccionesPendientes,
                    Message = "La sincronización fitosanitaria ya se encuentra en curso."
                };
            }

            int enviadas = 0;
            try
            {
                string usuarioId = ObtenerUsuarioId();
                List<InspeccionFitosanitariaOfflineRegistro> registros =
                    await ObtenerRegistrosAsync(usuarioId, cancellationToken);

                foreach (InspeccionFitosanitariaOfflineRegistro registro in registros)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        List<InspeccionFotoLocal> fotos = registro.Fotos
                            .OrderBy(item => item.Orden)
                            .Select(item => new InspeccionFotoLocal
                            {
                                RutaLocal = item.RutaLocal,
                                NombreArchivo = item.NombreArchivo,
                                TipoContenido = item.TipoContenido,
                                TipoFotografia = item.TipoFotografia,
                                FechaIdentificacionCampo =
                                    item.FechaIdentificacionCampo.Date
                            })
                            .ToList();

                        if (fotos.Count == 0 ||
                            fotos.Any(item => !File.Exists(item.RutaLocal)))
                        {
                            throw new InvalidOperationException(
                                "Una o más fotografías locales ya no están disponibles en el dispositivo.");
                        }

                        await InspeccionFitosanitariaApiService.Instance.CrearAsync(
                            fotos,
                            registro.CodigoTerreno,
                            registro.Observacion,
                            registro.NombreInspeccion,
                            cancellationToken);

                        await EliminarRegistroAsync(
                            usuarioId,
                            registro,
                            cancellationToken);
                        enviadas++;
                        ColaCambiada?.Invoke(this, EventArgs.Empty);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        registro.UltimoIntentoUtc = DateTime.UtcNow;
                        registro.UltimoError = ex.Message;
                        await GuardarRegistroAsync(
                            usuarioId,
                            registro,
                            cancellationToken);
                    }
                }

                FitosanitariaOfflineResumen resumen =
                    await ObtenerResumenAsync(cancellationToken);

                return new FitosanitariaOfflineSincronizacionResultado
                {
                    Success = resumen.InspeccionesPendientes == 0,
                    Enviadas = enviadas,
                    Pendientes = resumen.InspeccionesPendientes,
                    Message = resumen.InspeccionesPendientes == 0
                        ? enviadas == 0
                            ? "No había inspecciones fitosanitarias pendientes de enviar."
                            : $"Se enviaron {enviadas} inspección(es) fitosanitaria(s) al servidor."
                        : $"Se enviaron {enviadas}; quedan {resumen.InspeccionesPendientes} inspección(es) pendiente(s) para reintentar."
                };
            }
            finally
            {
                sincronizacionLock.Release();
            }
        }

        private async Task<HttpResponseMessage> GuardarInspeccionDesdeHttpAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not MultipartFormDataContent multipart)
            {
                return CrearRespuestaError(
                    request,
                    HttpStatusCode.BadRequest,
                    "La inspección sin conexión no contiene el formulario de fotografías esperado.");
            }

            List<HttpContent> partes = multipart.ToList();
            string nombre = await LeerPrimeroAsync(
                partes,
                "NombreInspeccion",
                cancellationToken);
            string codigoTerreno = await LeerPrimeroAsync(
                partes,
                "CodigoTerreno",
                cancellationToken);
            string observacion = await LeerPrimeroAsync(
                partes,
                "Observacion",
                cancellationToken);

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(codigoTerreno))
            {
                return CrearRespuestaError(
                    request,
                    HttpStatusCode.BadRequest,
                    "La inspección requiere nombre y terreno antes de guardarse sin conexión.");
            }

            List<string> tipos = await LeerTodosAsync(
                partes,
                "TiposFotografia",
                cancellationToken);
            List<string> fechas = await LeerTodosAsync(
                partes,
                "FechasIdentificacionCampo",
                cancellationToken);
            List<HttpContent> archivos = partes
                .Where(item => EsParte(item, "Fotos"))
                .ToList();

            if (archivos.Count == 0)
            {
                return CrearRespuestaError(
                    request,
                    HttpStatusCode.BadRequest,
                    "Debe agregar al menos una fotografía a la inspección.");
            }

            string usuarioId = ObtenerUsuarioId();
            int usuarioNumerico = int.TryParse(usuarioId, out int idUsuario)
                ? idUsuario
                : 0;
            int localId = ReservarIdLocal(usuarioId);
            string directorioFoto = Path.Combine(
                ObtenerDirectorioFotos(usuarioId),
                Math.Abs(localId).ToString());

            Directory.CreateDirectory(directorioFoto);

            var registro = new InspeccionFitosanitariaOfflineRegistro
            {
                Version = VersionPreparacion,
                LocalId = localId,
                UsuarioId = usuarioId,
                UsuarioNombre = Preferences.Get(
                    SessionKeys.KeyNombreCompletoUsuario,
                    string.Empty),
                NombreInspeccion = nombre.Trim(),
                CodigoTerreno = codigoTerreno.Trim(),
                Observacion = observacion.Trim(),
                FechaRegistroUtc = DateTime.UtcNow
            };

            try
            {
                for (int index = 0; index < archivos.Count; index++)
                {
                    HttpContent parte = archivos[index];
                    string nombreArchivo = ObtenerNombreArchivo(parte, index + 1);
                    string nombreSeguro =
                        $"{index + 1:00}_{Guid.NewGuid():N}_{nombreArchivo}";
                    string ruta = Path.Combine(directorioFoto, nombreSeguro);

                    await using Stream origen = await parte.ReadAsStreamAsync(
                        cancellationToken);
                    await using FileStream destino = new(
                        ruta,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        useAsync: true);
                    await origen.CopyToAsync(destino, cancellationToken);

                    DateTime fechaCampo = DateTime.Today;
                    if (index < fechas.Count &&
                        DateTime.TryParse(
                            fechas[index],
                            out DateTime fechaParseada))
                    {
                        fechaCampo = fechaParseada.Date;
                    }

                    string tipo = index < tipos.Count &&
                                  !string.IsNullOrWhiteSpace(tipos[index])
                        ? tipos[index].Trim().ToUpperInvariant().Replace(' ', '_')
                        : "EVIDENCIA";

                    registro.Fotos.Add(new InspeccionFitosanitariaOfflineFoto
                    {
                        Orden = index + 1,
                        RutaLocal = ruta,
                        NombreArchivo = nombreArchivo,
                        TipoContenido = parte.Headers.ContentType?.MediaType ??
                            "image/jpeg",
                        TipoFotografia = tipo,
                        FechaIdentificacionCampo = fechaCampo
                    });
                }

                await GuardarRegistroAsync(
                    usuarioId,
                    registro,
                    cancellationToken);
            }
            catch
            {
                EliminarDirectorioSeguro(directorioFoto);
                throw;
            }

            ColaCambiada?.Invoke(this, EventArgs.Empty);

            InspeccionFitosanitariaDetalleV2 detalle =
                CrearDetalleLocal(registro, usuarioNumerico);

            return CrearRespuestaOk(
                request,
                detalle,
                "Inspección guardada en el dispositivo. Se enviará al servidor cuando vuelva a trabajar en línea.");
        }

        private async Task<HttpResponseMessage> CrearRespuestaBandejaAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> query = LeerQuery(request);
            string modo = ObtenerQuery(query, "modo").ToLowerInvariant();

            if (modo is "historial" or "decisiones")
            {
                return CrearRespuestaOk(
                    request,
                    new InspeccionFitosanitariaBandejaPaginaV2(),
                    "No hay registros locales para esta vista.");
            }

            string usuarioId = ObtenerUsuarioId();
            List<InspeccionFitosanitariaOfflineRegistro> registros =
                await ObtenerRegistrosAsync(usuarioId, cancellationToken);

            string buscar = ObtenerQuery(query, "buscar");
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                registros = registros.Where(item =>
                    Contiene(item.NombreInspeccion, buscar) ||
                    Contiene(item.CodigoTerreno, buscar) ||
                    Contiene(item.Observacion, buscar) ||
                    item.Fotos.Any(foto => Contiene(foto.NombreArchivo, buscar)))
                    .ToList();
            }

            DateTime? fechaDesde = LeerFechaQuery(query, "fechaDesde");
            DateTime? fechaHasta = LeerFechaQuery(query, "fechaHasta");
            if (fechaDesde.HasValue)
            {
                registros = registros
                    .Where(item => item.FechaRegistroUtc.ToLocalTime().Date >=
                                   fechaDesde.Value.Date)
                    .ToList();
            }

            if (fechaHasta.HasValue)
            {
                registros = registros
                    .Where(item => item.FechaRegistroUtc.ToLocalTime().Date <=
                                   fechaHasta.Value.Date)
                    .ToList();
            }

            DateTime? ultimaFecha = LeerFechaHoraQuery(query, "ultimaFechaUtc");
            int? ultimoId = LeerEnteroQuery(query, "ultimoId");
            if (ultimaFecha.HasValue && ultimoId.HasValue)
            {
                DateTime cursor = ultimaFecha.Value.ToUniversalTime();
                registros = registros.Where(item =>
                        item.FechaRegistroUtc < cursor ||
                        (item.FechaRegistroUtc == cursor &&
                         item.LocalId < ultimoId.Value))
                    .ToList();
            }

            int tamanoPagina = Math.Clamp(
                LeerEnteroQuery(query, "tamanoPagina") ?? 20,
                10,
                50);

            registros = registros
                .OrderByDescending(item => item.FechaRegistroUtc)
                .ThenByDescending(item => item.LocalId)
                .ToList();

            bool hayMas = registros.Count > tamanoPagina;
            List<InspeccionFitosanitariaOfflineRegistro> paginaRegistros =
                registros.Take(tamanoPagina).ToList();

            int usuarioNumerico = int.TryParse(usuarioId, out int idUsuario)
                ? idUsuario
                : 0;

            List<InspeccionFitosanitariaBandejaItemV2> items = paginaRegistros
                .Select(item => CrearItemLocal(item, usuarioNumerico))
                .ToList();

            InspeccionFitosanitariaOfflineRegistro? ultimo =
                paginaRegistros.LastOrDefault();

            var pagina = new InspeccionFitosanitariaBandejaPaginaV2
            {
                Items = items,
                HayMas = hayMas,
                SiguienteFechaUtc = hayMas ? ultimo?.FechaRegistroUtc : null,
                SiguienteId = hayMas ? ultimo?.LocalId : null
            };

            return CrearRespuestaOk(
                request,
                pagina,
                "Inspecciones guardadas sin conexión obtenidas correctamente.");
        }

        private InspeccionFitosanitariaDetalleV2 CrearDetalleLocal(
            InspeccionFitosanitariaOfflineRegistro registro,
            int usuarioId)
        {
            var detalle = new InspeccionFitosanitariaDetalleV2
            {
                InspeccionId = registro.LocalId,
                NombreInspeccion = registro.NombreInspeccion,
                CodigoTerreno = registro.CodigoTerreno,
                UsuarioSolicitanteId = usuarioId,
                UsuarioSolicitante = registro.UsuarioNombre,
                Observacion = registro.Observacion,
                Estado = InspeccionEstadosV2.Borrador,
                FechaRegistroSistemaUtc = registro.FechaRegistroUtc,
                EtapaTecnicaFinalizada = false,
                CerradaDefinitiva = false,
                PuedeGestionarSolicitud = false,
                PuedeCerrarInspeccion = false,
                PuedeAnalizar = false,
                PuedeAprobar = false,
                PuedePublicarAlbum = false
            };

            foreach (InspeccionFitosanitariaOfflineFoto foto in
                     registro.Fotos.OrderBy(item => item.Orden))
            {
                detalle.Fotografias.Add(new InspeccionFotoV2
                {
                    FotografiaId = registro.LocalId * 100 - foto.Orden,
                    Orden = foto.Orden,
                    TipoFotografia = foto.TipoFotografia,
                    NombreArchivoOriginal = foto.NombreArchivo,
                    UrlImagen = foto.RutaLocal,
                    Estado = InspeccionFotoEstados.Borrador,
                    FechaIdentificacionCampo = foto.FechaIdentificacionCampo,
                    FechaRegistroSistemaUtc = registro.FechaRegistroUtc
                });
            }

            return detalle;
        }

        private InspeccionFitosanitariaBandejaItemV2 CrearItemLocal(
            InspeccionFitosanitariaOfflineRegistro registro,
            int usuarioId) =>
            new()
            {
                InspeccionId = registro.LocalId,
                NombreInspeccion = registro.NombreInspeccion,
                CodigoTerreno = registro.CodigoTerreno,
                UsuarioTecnicoId = usuarioId,
                TecnicoNombreCompleto = registro.UsuarioNombre,
                FechaRegistroSistemaUtc = registro.FechaRegistroUtc,
                Estado = InspeccionEstadosV2.Borrador,
                TotalFotografias = registro.Fotos.Count,
                Pendientes = registro.Fotos.Count,
                UrlMiniatura = registro.Fotos
                    .OrderBy(item => item.Orden)
                    .FirstOrDefault()?.RutaLocal ?? string.Empty,
                EsLocalPendiente = true
            };

        private async Task<List<InspeccionFitosanitariaOfflineRegistro>>
            ObtenerRegistrosAsync(
                string usuarioId,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
                return [];

            string directorio = ObtenerDirectorioRegistros(usuarioId);
            if (!Directory.Exists(directorio))
                return [];

            var registros = new List<InspeccionFitosanitariaOfflineRegistro>();

            await almacenamientoLock.WaitAsync(cancellationToken);
            try
            {
                foreach (string archivo in Directory.EnumerateFiles(
                             directorio,
                             "*.json",
                             SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string json = await File.ReadAllTextAsync(
                            archivo,
                            cancellationToken);
                        InspeccionFitosanitariaOfflineRegistro? registro =
                            JsonSerializer.Deserialize<
                                InspeccionFitosanitariaOfflineRegistro>(
                                json,
                                jsonOptions);

                        if (registro != null && registro.LocalId < 0)
                            registros.Add(registro);
                    }
                    catch
                    {
                        // Un registro aislado dañado no invalida la cola restante.
                    }
                }
            }
            finally
            {
                almacenamientoLock.Release();
            }

            return registros;
        }

        private async Task GuardarRegistroAsync(
            string usuarioId,
            InspeccionFitosanitariaOfflineRegistro registro,
            CancellationToken cancellationToken)
        {
            string directorio = ObtenerDirectorioRegistros(usuarioId);
            Directory.CreateDirectory(directorio);
            string archivo = ObtenerRutaRegistro(usuarioId, registro.LocalId);
            string temporal = archivo + ".tmp";
            string json = JsonSerializer.Serialize(registro, jsonOptions);

            await almacenamientoLock.WaitAsync(cancellationToken);
            try
            {
                await File.WriteAllTextAsync(
                    temporal,
                    json,
                    Encoding.UTF8,
                    cancellationToken);
                File.Move(temporal, archivo, overwrite: true);
            }
            finally
            {
                almacenamientoLock.Release();
                EliminarArchivoSeguro(temporal);
            }
        }

        private async Task EliminarRegistroAsync(
            string usuarioId,
            InspeccionFitosanitariaOfflineRegistro registro,
            CancellationToken cancellationToken)
        {
            await almacenamientoLock.WaitAsync(cancellationToken);
            try
            {
                EliminarArchivoSeguro(
                    ObtenerRutaRegistro(usuarioId, registro.LocalId));
                EliminarDirectorioSeguro(
                    Path.Combine(
                        ObtenerDirectorioFotos(usuarioId),
                        Math.Abs(registro.LocalId).ToString()));
            }
            finally
            {
                almacenamientoLock.Release();
            }
        }

        private int ReservarIdLocal(string usuarioId)
        {
            lock (idLock)
            {
                string key = ConstruirClave(PrefijoSiguienteId, usuarioId);
                int actual = Preferences.Get(key, -1);
                if (actual >= 0)
                    actual = -1;

                int siguiente = actual == int.MinValue
                    ? -1
                    : actual - 1;
                Preferences.Set(key, siguiente);
                return actual;
            }
        }

        private static async Task<string> LeerPrimeroAsync(
            IEnumerable<HttpContent> partes,
            string nombre,
            CancellationToken cancellationToken)
        {
            HttpContent? parte = partes.FirstOrDefault(item =>
                EsParte(item, nombre));

            return parte == null
                ? string.Empty
                : await parte.ReadAsStringAsync(cancellationToken);
        }

        private static async Task<List<string>> LeerTodosAsync(
            IEnumerable<HttpContent> partes,
            string nombre,
            CancellationToken cancellationToken)
        {
            var valores = new List<string>();
            foreach (HttpContent parte in partes.Where(item =>
                         EsParte(item, nombre)))
            {
                valores.Add(await parte.ReadAsStringAsync(cancellationToken));
            }

            return valores;
        }

        private static bool EsParte(HttpContent contenido, string nombre)
        {
            string value = contenido.Headers.ContentDisposition?.Name?
                .Trim('"') ?? string.Empty;
            return string.Equals(value, nombre, StringComparison.OrdinalIgnoreCase);
        }

        private static string ObtenerNombreArchivo(
            HttpContent contenido,
            int indice)
        {
            string value = contenido.Headers.ContentDisposition?.FileNameStar ??
                           contenido.Headers.ContentDisposition?.FileName ??
                           $"foto_{indice}.jpg";
            value = Path.GetFileName(value.Trim('"'));

            foreach (char invalido in Path.GetInvalidFileNameChars())
                value = value.Replace(invalido, '_');

            return string.IsNullOrWhiteSpace(value)
                ? $"foto_{indice}.jpg"
                : value;
        }

        private static string ObtenerPath(HttpRequestMessage request)
        {
            string raw = request.RequestUri?.ToString() ?? string.Empty;
            if (request.RequestUri?.IsAbsoluteUri == true)
                return request.RequestUri.AbsolutePath.TrimEnd('/').ToLowerInvariant();

            int queryIndex = raw.IndexOf('?');
            string path = queryIndex >= 0 ? raw[..queryIndex] : raw;
            if (!path.StartsWith('/'))
                path = "/" + path;
            return path.TrimEnd('/').ToLowerInvariant();
        }

        private static Dictionary<string, string> LeerQuery(
            HttpRequestMessage request)
        {
            string raw = request.RequestUri?.ToString() ?? string.Empty;
            string query;

            if (request.RequestUri?.IsAbsoluteUri == true)
                query = request.RequestUri.Query.TrimStart('?');
            else
            {
                int index = raw.IndexOf('?');
                query = index >= 0 ? raw[(index + 1)..] : string.Empty;
            }

            var resultado = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string parte in query.Split(
                         '&',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                int igual = parte.IndexOf('=');
                string nombre = igual >= 0 ? parte[..igual] : parte;
                string valor = igual >= 0 ? parte[(igual + 1)..] : string.Empty;
                resultado[WebUtility.UrlDecode(nombre)] =
                    WebUtility.UrlDecode(valor);
            }

            return resultado;
        }

        private static string ObtenerQuery(
            IReadOnlyDictionary<string, string> query,
            string nombre) =>
            query.TryGetValue(nombre, out string? value)
                ? value?.Trim() ?? string.Empty
                : string.Empty;

        private static int? LeerEnteroQuery(
            IReadOnlyDictionary<string, string> query,
            string nombre) =>
            int.TryParse(ObtenerQuery(query, nombre), out int value)
                ? value
                : null;

        private static DateTime? LeerFechaQuery(
            IReadOnlyDictionary<string, string> query,
            string nombre) =>
            DateTime.TryParse(ObtenerQuery(query, nombre), out DateTime value)
                ? value.Date
                : null;

        private static DateTime? LeerFechaHoraQuery(
            IReadOnlyDictionary<string, string> query,
            string nombre) =>
            DateTime.TryParse(ObtenerQuery(query, nombre), out DateTime value)
                ? value
                : null;

        private static bool Contiene(string? origen, string buscar) =>
            !string.IsNullOrWhiteSpace(origen) &&
            origen.Contains(buscar, StringComparison.OrdinalIgnoreCase);

        private DateTime? ObtenerFechaPreparacion(string usuarioId)
        {
            string value = Preferences.Get(
                ConstruirClave(PrefijoFecha, usuarioId),
                string.Empty);
            return DateTime.TryParse(value, out DateTime fecha)
                ? fecha
                : null;
        }

        private static string ObtenerUsuarioId() =>
            Preferences.Get(SessionKeys.KeyUserId, string.Empty).Trim();

        private static string ObtenerDirectorioUsuario(string usuarioId) =>
            Path.Combine(
                FileSystem.AppDataDirectory,
                "fitosanitaria-offline",
                usuarioId);

        private static string ObtenerDirectorioRegistros(string usuarioId) =>
            Path.Combine(ObtenerDirectorioUsuario(usuarioId), "registros");

        private static string ObtenerDirectorioFotos(string usuarioId) =>
            Path.Combine(ObtenerDirectorioUsuario(usuarioId), "fotos");

        private static string ObtenerRutaRegistro(string usuarioId, int localId) =>
            Path.Combine(
                ObtenerDirectorioRegistros(usuarioId),
                $"inspeccion_{Math.Abs(localId)}.json");

        private static string ConstruirClave(string prefijo, string usuarioId) =>
            prefijo + usuarioId.Trim();

        private HttpResponseMessage CrearRespuestaOk<T>(
            HttpRequestMessage request,
            T data,
            string mensaje)
        {
            string json = JsonSerializer.Serialize(
                new
                {
                    success = true,
                    message = mensaje,
                    data
                },
                jsonOptions);

            return CrearRespuestaJson(
                request,
                HttpStatusCode.OK,
                json,
                "LOCAL-FITOSANITARIA");
        }

        private HttpResponseMessage CrearRespuestaError(
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            string mensaje)
        {
            string json = JsonSerializer.Serialize(
                new
                {
                    success = false,
                    message = mensaje
                },
                jsonOptions);

            return CrearRespuestaJson(
                request,
                statusCode,
                json,
                "LOCAL-FITOSANITARIA-ERROR");
        }

        private static HttpResponseMessage CrearRespuestaJson(
            HttpRequestMessage request,
            HttpStatusCode statusCode,
            string json,
            string origen)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                OfflineReadResponseService.HeaderOrigen,
                origen);
            return response;
        }

        private static void EliminarArchivoSeguro(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void EliminarDirectorioSeguro(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
            }
        }

        private sealed class InspeccionFitosanitariaOfflineRegistro
        {
            public int Version { get; set; }
            public int LocalId { get; set; }
            public string UsuarioId { get; set; } = string.Empty;
            public string UsuarioNombre { get; set; } = string.Empty;
            public string NombreInspeccion { get; set; } = string.Empty;
            public string CodigoTerreno { get; set; } = string.Empty;
            public string Observacion { get; set; } = string.Empty;
            public DateTime FechaRegistroUtc { get; set; }
            public List<InspeccionFitosanitariaOfflineFoto> Fotos { get; set; } = [];
            public DateTime? UltimoIntentoUtc { get; set; }
            public string UltimoError { get; set; } = string.Empty;
        }

        private sealed class InspeccionFitosanitariaOfflineFoto
        {
            public int Orden { get; set; }
            public string RutaLocal { get; set; } = string.Empty;
            public string NombreArchivo { get; set; } = string.Empty;
            public string TipoContenido { get; set; } = "image/jpeg";
            public string TipoFotografia { get; set; } = "EVIDENCIA";
            public DateTime FechaIdentificacionCampo { get; set; }
        }
    }
}
