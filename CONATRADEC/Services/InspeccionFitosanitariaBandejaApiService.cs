using CONATRADEC.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente exclusivo de la bandeja paginada. También administra el filtro
    /// contextual por técnico para reutilizarlo desde las páginas existentes
    /// sin duplicar la lógica de búsqueda de sus viewmodels.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaApiService
    {
        private static readonly Lazy<InspeccionFitosanitariaBandejaApiService>
            lazy = new(() => new InspeccionFitosanitariaBandejaApiService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;
        private readonly object filtrosSync = new();
        private readonly Dictionary<string, int?> tecnicoContextualPorModo =
            new(StringComparer.OrdinalIgnoreCase);

        public static InspeccionFitosanitariaBandejaApiService Instance =>
            lazy.Value;

        private InspeccionFitosanitariaBandejaApiService()
        {
            client = ApiClientService.Client;
        }

        public void EstablecerTecnicoContextual(
            string? modo,
            int? tecnicoId)
        {
            string clave = NormalizarModo(modo);

            lock (filtrosSync)
            {
                tecnicoContextualPorModo[clave] = tecnicoId is > 0
                    ? tecnicoId
                    : null;
            }
        }

        public int? ObtenerTecnicoContextual(string? modo)
        {
            string clave = NormalizarModo(modo);

            lock (filtrosSync)
            {
                return tecnicoContextualPorModo.TryGetValue(
                    clave,
                    out int? tecnicoId)
                        ? tecnicoId
                        : null;
            }
        }

        public async Task<InspeccionFitosanitariaBandejaPaginaV2>
            ObtenerAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            string modo = NormalizarModo(filtro.Modo);

            /*
             * Al volver de una jornada sin conexión, Mis inspecciones intenta
             * enviar primero la cola fitosanitaria. Un error de sincronización
             * no bloquea la consulta central: la copia local se conserva para
             * reintentar desde Datos sin conexión.
             */
            if (ModoSesionService.EsEnLinea &&
                (modo is DiagnosticoIARoutes.ModoMisInspecciones or
                    DiagnosticoIARoutes.ModoDecisionesPendientes))
            {
                try
                {
                    await FitosanitariaOfflineService.Instance
                        .SincronizarPendientesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // La bandeja en línea continúa con los datos del servidor.
                }
            }

            /*
             * Mis inspecciones, Decisiones pendientes e Historial comparten la
             * misma consulta moderna por fotografía. Así una misma inspección
             * conserva exactamente el mismo estado funcional sin importar la
             * bandeja desde la que se consulte.
             */
            bool usaBandejaUnificada = modo is
                DiagnosticoIARoutes.ModoMisInspecciones or
                DiagnosticoIARoutes.ModoDecisionesPendientes or
                DiagnosticoIARoutes.ModoHistorial;

            if (usaBandejaUnificada)
            {
                if (modo == DiagnosticoIARoutes.ModoHistorial)
                {
                    filtro.TecnicoId ??=
                        ObtenerTecnicoContextual(filtro.Modo);
                }

                var operativa = new
                    InspeccionFitosanitariaBandejaOperativaApiService();

                return await operativa.ObtenerPaginaAsync(
                    filtro,
                    cancellationToken);
            }

            string ruta = ConstruirRuta(filtro);
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(HttpMethod.Get, ruta);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            RespuestaApi<InspeccionFitosanitariaBandejaPaginaV2>? envelope =
                Deserializar<InspeccionFitosanitariaBandejaPaginaV2>(contenido);

            if (!response.IsSuccessStatusCode)
            {
                string mensaje = envelope?.Message ??
                    ExtraerMensaje(contenido);

                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(mensaje)
                        ? "El servidor rechazó la búsqueda de inspecciones."
                        : mensaje);
            }

            if (envelope?.Data != null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una página de inspecciones incompleta.");
        }

        public async Task<TecnicoInspeccionFiltroRespuesta>
            ObtenerTecnicosAsync(
                string modo,
                CancellationToken cancellationToken = default)
        {
            string normalizado = NormalizarModo(modo);

            /*
             * Las tres vistas operativas de cada etapa pertenecen a la bandeja
             * de revisión. Los modos *-disponibles no se normalizan a la vista
             * personal porque el backend debe devolver únicamente los técnicos
             * que realmente tienen expedientes sin responsable.
             */
            bool usaBandejaRevision = normalizado is
                DiagnosticoIARoutes.ModoAnalizador or
                "analizador-disponibles" or
                DiagnosticoIARoutes.ModoAnalizadorRevisadas or
                DiagnosticoIARoutes.ModoAprobador or
                "aprobador-disponibles" or
                DiagnosticoIARoutes.ModoAprobadorRevisadas or
                DiagnosticoIARoutes.ModoHistorial;

            string ruta = usaBandejaRevision
                ? "api/revision-fitosanitaria/tecnicos?modo=" +
                  Uri.EscapeDataString(normalizado)
                : "api/inspecciones-fitosanitarias/bandeja-tecnicos?modo=" +
                  Uri.EscapeDataString(normalizado);

            TecnicoInspeccionFiltroRespuesta respuesta =
                await GetDataAsync<TecnicoInspeccionFiltroRespuesta>(
                    ruta,
                    "No fue posible cargar los técnicos responsables.",
                    cancellationToken);

            TecnicoInspeccionCacheService.Establecer(
                respuesta.Asignaciones);

            respuesta.Tecnicos = respuesta.Tecnicos
                .Where(item => item.UsuarioTecnicoId > 0)
                .OrderBy(item => item.NombreCompleto)
                .ThenBy(item => item.NombreUsuario)
                .ToList();

            return respuesta;
        }

        public async Task<TecnicoInspeccionFiltroItem>
            ObtenerTecnicoResponsableAsync(
                int inspeccionId,
                CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(inspeccionId));

            TecnicoInspeccionFiltroItem tecnico =
                await GetDataAsync<TecnicoInspeccionFiltroItem>(
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/tecnico-responsable",
                    "No fue posible cargar el técnico responsable.",
                    cancellationToken);

            TecnicoInspeccionCacheService.Establecer(
                inspeccionId,
                tecnico);

            return tecnico;
        }

        private async Task<T> GetDataAsync<T>(
            string ruta,
            string mensajePredeterminado,
            CancellationToken cancellationToken)
            where T : class
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(HttpMethod.Get, ruta);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            RespuestaApi<T>? envelope = Deserializar<T>(contenido);

            if (!response.IsSuccessStatusCode)
            {
                string mensaje = envelope?.Message ?? string.Empty;

                if (string.IsNullOrWhiteSpace(mensaje))
                    mensaje = ExtraerMensaje(contenido);

                if (string.IsNullOrWhiteSpace(mensaje))
                    mensaje = mensajePredeterminado;

                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    mensaje);
            }

            if (envelope?.Data is not null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                mensajePredeterminado);
        }

        private string ConstruirRuta(
            InspeccionFitosanitariaBandejaFiltroV2 filtro)
        {
            var parametros = new List<string>();

            Agregar(parametros, "modo", filtro.Modo);
            Agregar(parametros, "buscar", filtro.Buscar);
            Agregar(parametros, "propietario", filtro.Propietario);
            Agregar(parametros, "departamento", filtro.Departamento);
            Agregar(parametros, "tipoFotografia", filtro.TipoFotografia);
            Agregar(parametros, "estado", filtro.Estado);

            int? tecnicoId = filtro.TecnicoId ??
                ObtenerTecnicoContextual(filtro.Modo);

            if (tecnicoId is > 0)
            {
                Agregar(
                    parametros,
                    "tecnicoId",
                    tecnicoId.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (filtro.FechaDesde.HasValue)
            {
                Agregar(
                    parametros,
                    "fechaDesde",
                    filtro.FechaDesde.Value.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }

            if (filtro.FechaHasta.HasValue)
            {
                Agregar(
                    parametros,
                    "fechaHasta",
                    filtro.FechaHasta.Value.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }

            Agregar(
                parametros,
                "desfaseHorarioMinutos",
                Math.Clamp(
                    filtro.DesfaseHorarioMinutos,
                    -840,
                    840).ToString(CultureInfo.InvariantCulture));

            if (filtro.UltimaFechaUtc.HasValue)
            {
                Agregar(
                    parametros,
                    "ultimaFechaUtc",
                    filtro.UltimaFechaUtc.Value.ToUniversalTime().ToString(
                        "O",
                        CultureInfo.InvariantCulture));
            }

            if (filtro.UltimoId.HasValue)
            {
                Agregar(
                    parametros,
                    "ultimoId",
                    filtro.UltimoId.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            Agregar(
                parametros,
                "tamanoPagina",
                Math.Clamp(filtro.TamanoPagina, 10, 50).ToString(
                    CultureInfo.InvariantCulture));

            return "api/inspecciones-fitosanitarias/bandeja-paginada?" +
                   string.Join("&", parametros);
        }

        private static RespuestaApi<T>? Deserializar<T>(string contenido)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            try
            {
                return JsonSerializer.Deserialize<RespuestaApi<T>>(
                    contenido,
                    JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static void Agregar(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                Uri.EscapeDataString(nombre) + "=" +
                Uri.EscapeDataString(valor.Trim()));
        }

        private static string NormalizarModo(string? modo) =>
            string.IsNullOrWhiteSpace(modo)
                ? DiagnosticoIARoutes.ModoMisInspecciones
                : modo.Trim().ToLowerInvariant();

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return string.Empty;

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);

                if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    return message.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty(
                        "title",
                        out JsonElement title))
                {
                    return title.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }

        private sealed class RespuestaApi<T>
            where T : class
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
