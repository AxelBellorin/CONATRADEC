using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CONATRADEC.Services
{
    public sealed class PaqueteCatalogosOfflineService
    {
        private const string HeaderBypass = "X-Offline-Bypass";

        private static readonly Lazy<PaqueteCatalogosOfflineService> lazy =
            new(() => new PaqueteCatalogosOfflineService());

        private static readonly TimeSpan IntervaloVerificacion =
            TimeSpan.FromMinutes(2);

        private readonly SemaphoreSlim downloadLock = new(1, 1);

        private readonly ContenidoLocalDatabaseService database =
            ContenidoLocalDatabaseService.Instance;

        private DateTime ultimaVerificacionUtc;
        private Task<ResultadoDescargaOffline>? tareaActual;

        public static PaqueteCatalogosOfflineService Instance => lazy.Value;

        public event EventHandler<EstadoPaqueteOfflineEventArgs>?
            EstadoCambiado;

        private PaqueteCatalogosOfflineService()
        {
        }

        public Task<ResultadoDescargaOffline> DescargarTodoAsync(
            bool forzar = true)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return Task.FromResult(
                    ResultadoDescargaOffline.Fail(
                        "Su usuario no tiene habilitado el trabajo sin conexión."));
            }

            lock (this)
            {
                if (tareaActual != null && !tareaActual.IsCompleted)
                    return tareaActual;

                tareaActual = DescargarInternoAsync(
                    forzar,
                    CancellationToken.None);

                return tareaActual;
            }
        }

        public async Task<EstadoPaqueteOffline> ObtenerEstadoAsync()
        {
            string usuarioId = ObtenerUsuarioId();

            if (usuarioId == "0")
                return new EstadoPaqueteOffline();

            CatalogoOfflineEstadoEntity? entity =
                await database.ObtenerEstadoPaqueteAsync(usuarioId);

            return ConvertirEstado(entity);
        }

        public void VerificarActualizacionEnSegundoPlano()
        {
            if (!DatosSinConexionPermisos.TienePermiso)
                return;

            if (DateTime.UtcNow - ultimaVerificacionUtc <
                IntervaloVerificacion)
            {
                return;
            }

            ultimaVerificacionUtc = DateTime.UtcNow;

            _ = Task.Run(async () =>
            {
                try
                {
                    await VerificarActualizacionAsync();
                }
                catch
                {
                }
            });
        }

        public async Task VerificarActualizacionAsync()
        {
            if (!DatosSinConexionPermisos.TienePermiso)
                return;

            string usuarioId = ObtenerUsuarioId();

            if (usuarioId == "0" ||
                !EstadoConexionService.Instance.HayInternet)
            {
                return;
            }

            CatalogoOfflineEstadoEntity? estado =
                await database.ObtenerEstadoPaqueteAsync(usuarioId);

            if (estado == null ||
                string.IsNullOrWhiteSpace(estado.PaqueteActivoId))
            {
                return;
            }

            string version = await ObtenerVersionServidorAsync(
                CancellationToken.None);

            if (string.IsNullOrWhiteSpace(version))
                return;

            estado.UltimaVerificacionUtc = DateTime.UtcNow;

            if (!string.Equals(
                    estado.VersionServidor,
                    version,
                    StringComparison.Ordinal))
            {
                estado.Estado =
                    CatalogoOfflineEstados.ActualizacionDisponible;
                estado.Mensaje =
                    "Hay datos nuevos disponibles para descargar.";
            }
            else if (estado.Estado !=
                     CatalogoOfflineEstados.Descargando)
            {
                estado.Estado = CatalogoOfflineEstados.Completo;
                estado.Mensaje =
                    "Los datos descargados están actualizados.";
            }

            await database.GuardarEstadoPaqueteAsync(estado);
            Notificar(ConvertirEstado(estado));
        }

        public async Task MarcarActualizacionPendienteAsync()
        {
            if (!DatosSinConexionPermisos.TienePermiso)
                return;

            string usuarioId = ObtenerUsuarioId();

            if (usuarioId == "0")
                return;

            CatalogoOfflineEstadoEntity? estado =
                await database.ObtenerEstadoPaqueteAsync(usuarioId);

            if (estado == null ||
                string.IsNullOrWhiteSpace(estado.PaqueteActivoId))
            {
                return;
            }

            estado.Estado =
                CatalogoOfflineEstados.ActualizacionDisponible;
            estado.Mensaje =
                "Hay cambios pendientes de descargar.";

            await database.GuardarEstadoPaqueteAsync(estado);
            Notificar(ConvertirEstado(estado));
        }

        private async Task<ResultadoDescargaOffline> DescargarInternoAsync(
            bool forzar,
            CancellationToken cancellationToken)
        {
            await downloadLock.WaitAsync(cancellationToken);

            string paqueteId = Guid.NewGuid().ToString("N");
            string usuarioId = ObtenerUsuarioId();

            try
            {
                if (usuarioId == "0")
                {
                    return ResultadoDescargaOffline.Fail(
                        "No existe una sesión válida.");
                }

                if (!DatosSinConexionPermisos.TienePermiso)
                {
                    return ResultadoDescargaOffline.Fail(
                        "Su usuario no tiene habilitado el trabajo sin conexión.");
                }

                if (!EstadoConexionService.Instance.HayInternet)
                {
                    return ResultadoDescargaOffline.Fail(
                        "Se necesita conexión para descargar los datos.");
                }

                CatalogoOfflineEstadoEntity? anterior =
                    await database.ObtenerEstadoPaqueteAsync(usuarioId);

                string version =
                    await ObtenerVersionServidorAsync(cancellationToken);

                if (!forzar &&
                    anterior != null &&
                    anterior.Estado == CatalogoOfflineEstados.Completo &&
                    string.Equals(
                        anterior.VersionServidor,
                        version,
                        StringComparison.Ordinal))
                {
                    return ResultadoDescargaOffline.Ok(
                        "Los datos descargados ya están actualizados.",
                        anterior.TotalRegistros,
                        anterior.TamanoBytes);
                }

                const int totalPasos = 14;

                var estado = anterior ??
                    new CatalogoOfflineEstadoEntity
                    {
                        Clave = usuarioId,
                        UsuarioId = usuarioId
                    };

                estado.Estado = CatalogoOfflineEstados.Descargando;
                estado.ProgresoPorcentaje = 0;
                estado.PasoActual = 0;
                estado.TotalPasos = totalPasos;
                estado.Mensaje = "Preparando la descarga...";
                estado.UltimoError = string.Empty;

                await database.GuardarEstadoPaqueteAsync(estado);
                Notificar(ConvertirEstado(estado));

                int total = 0;
                long bytes = 0;
                int paso = 0;

                JsonArray paises =
                    await DescargarArrayAsync(
                        "api/pais",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "paises", paises,
                    ++paso, totalPasos, "Descargando países...",
                    total, bytes, estado);

                JsonArray departamentos = new();

                foreach (int paisId in ObtenerIds(paises, "paisId"))
                {
                    AgregarTodos(
                        departamentos,
                        await DescargarArrayAsync(
                            $"api/departamento/por-pais/{paisId}",
                            cancellationToken));
                }

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "departamentos", departamentos,
                    ++paso, totalPasos, "Descargando departamentos...",
                    total, bytes, estado);

                JsonArray municipios = new();

                foreach (int departamentoId in
                         ObtenerIds(departamentos, "departamentoId"))
                {
                    AgregarTodos(
                        municipios,
                        await DescargarArrayAsync(
                            $"api/municipio/por-departamento/" +
                            $"{departamentoId}",
                            cancellationToken));
                }

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "municipios", municipios,
                    ++paso, totalPasos, "Descargando municipios...",
                    total, bytes, estado);

                JsonArray municipiosUbicacion =
                    await DescargarArrayAsync(
                        "api/municipio/" +
                        "listartodos-por-departamento-por-pais",
                        cancellationToken);

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "municipiosUbicacion",
                    municipiosUbicacion);

                JsonArray terrenos =
                    await DescargarArrayAsync(
                        "api/terreno/listar",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId,
                    paqueteId,
                    "terrenos",
                    terrenos,
                    ++paso,
                    totalPasos,
                    "Descargando terrenos...",
                    total,
                    bytes,
                    estado);

                JsonArray cultivos =
                    await DescargarArrayAsync(
                        "api/configuracion/tipos-cultivo",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "tiposCultivo", cultivos,
                    ++paso, totalPasos,
                    "Descargando tipos de cultivo...",
                    total, bytes, estado);

                JsonArray tiposAnalisis =
                    await DescargarArrayAsync(
                        "api/configuracion/tipos-analisis-suelo",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "tiposAnalisis", tiposAnalisis,
                    ++paso, totalPasos,
                    "Descargando tipos de análisis...",
                    total, bytes, estado);

                JsonArray elementos =
                    await DescargarArrayAsync(
                        "api/elemento-quimico/listar",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "elementosQuimicos", elementos,
                    ++paso, totalPasos,
                    "Descargando elementos químicos...",
                    total, bytes, estado);

                JsonArray fuentes =
                    await DescargarArrayAsync(
                        "api/fuente-nutriente/listar",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "fuentesNutrientes", fuentes,
                    ++paso, totalPasos,
                    "Descargando fuentes de nutrientes...",
                    total, bytes, estado);

                JsonArray fuentesMixta =
                    await DescargarArrayAsync(
                        "api/fuente-nutriente/" +
                        "listar-fertilizacion-mixta",
                        cancellationToken);

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "fuentesFertilizacionMixta",
                    fuentesMixta);

                JsonArray aportes =
                    await DescargarArrayAsync(
                        "api/fuente-nutriente/aportes-tabla",
                        cancellationToken);

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "aportesFuentes",
                    aportes);

                JsonArray composicion =
                    await DescargarArrayAsync(
                        "api/fuente-nutriente/" +
                        "composicion?categoria=",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "composicionFuentes",
                    composicion, ++paso, totalPasos,
                    "Preparando composición de fuentes...",
                    total, bytes, estado);

                JsonNode unidadesFormulario =
                    await DescargarDataAsync(
                        "api/configuracion-unidades/" +
                        "formulario-analisis",
                        cancellationToken);

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "unidadesFormulario",
                    unidadesFormulario, ++paso, totalPasos,
                    "Descargando unidades del análisis...",
                    total, bytes, estado);

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "unidadesCatalogo",
                    await DescargarDataOpcionalAsync(
                        "api/configuracion-unidades/" +
                        "catalogo-unidades?incluirInactivas=false",
                        new JsonArray(),
                        cancellationToken));

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "unidadesElementos",
                    await DescargarDataOpcionalAsync(
                        "api/configuracion-unidades/" +
                        "elementos?incluirInactivas=false",
                        new JsonArray(),
                        cancellationToken));

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "unidadesMateriaOrganica",
                    await DescargarDataOpcionalAsync(
                        "api/configuracion-unidades/" +
                        "materia-organica?incluirInactivas=false",
                        new JsonArray(),
                        cancellationToken));

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "formulasConversion",
                    await DescargarDataOpcionalAsync(
                        "api/configuracion-unidades/formulas",
                        new JsonArray(),
                        cancellationToken),
                    ++paso, totalPasos,
                    "Descargando conversiones...",
                    total, bytes, estado);

                JsonNode rangoCultivos =
                    await DescargarDataAsync(
                        "api/configuracion/" +
                        "rangos-nutrientes/cultivos" +
                        "?pagina=1&tamanoPagina=100",
                        cancellationToken);

                await GuardarSeccionAsync(
                    usuarioId,
                    paqueteId,
                    "rangoCultivosPagina",
                    rangoCultivos);

                JsonArray rangos = new();

                foreach (int cultivoId in
                         ObtenerIds(cultivos, "tipoCultivoId"))
                {
                    int pagina = 1;
                    int totalPaginas = 1;

                    do
                    {
                        JsonNode respuesta =
                            await DescargarDataAsync(
                                "api/configuracion/" +
                                "rangos-nutrientes/buscar" +
                                $"?tipoCultivoId={cultivoId}" +
                                $"&pagina={pagina}" +
                                "&tamanoPagina=100",
                                cancellationToken);

                        AgregarTodos(
                            rangos,
                            ExtraerArray(respuesta, "items"));

                        totalPaginas = ObtenerEntero(
                            respuesta,
                            "totalPaginas",
                            1);

                        pagina++;
                    }
                    while (pagina <= totalPaginas);
                }

                (total, bytes) = await GuardarPasoAsync(
                    usuarioId, paqueteId, "rangosNutrientes", rangos,
                    ++paso, totalPasos,
                    "Descargando rangos nutricionales...",
                    total, bytes, estado);

                while (paso < totalPasos)
                {
                    (total, bytes) = await GuardarPasoAsync(
                        usuarioId,
                        paqueteId,
                        "confirmacion" + paso,
                        new JsonObject { ["completo"] = true },
                        ++paso,
                        totalPasos,
                        "Finalizando descarga...",
                        total,
                        bytes,
                        estado);
                }

                var completo = new CatalogoOfflineEstadoEntity
                {
                    Clave = usuarioId,
                    UsuarioId = usuarioId,
                    PaqueteActivoId = paqueteId,
                    VersionServidor =
                        string.IsNullOrWhiteSpace(version)
                            ? paqueteId
                            : version,
                    Estado = CatalogoOfflineEstados.Completo,
                    ProgresoPorcentaje = 100,
                    PasoActual = totalPasos,
                    TotalPasos = totalPasos,
                    Mensaje =
                        "Datos listos para trabajar sin conexión.",
                    TotalRegistros = total,
                    TamanoBytes = bytes,
                    UltimaDescargaCompletaUtc = DateTime.UtcNow,
                    UltimaVerificacionUtc = DateTime.UtcNow
                };

                await database.ActivarPaqueteAsync(completo);

                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                Notificar(ConvertirEstado(completo));

                return ResultadoDescargaOffline.Ok(
                    "Todos los datos necesarios fueron descargados.",
                    total,
                    bytes);
            }
            catch (Exception ex)
            {
                await database.EliminarPaqueteTemporalAsync(
                    usuarioId,
                    paqueteId);

                CatalogoOfflineEstadoEntity? anterior =
                    await database.ObtenerEstadoPaqueteAsync(usuarioId);

                var error = anterior ??
                    new CatalogoOfflineEstadoEntity
                    {
                        Clave = usuarioId,
                        UsuarioId = usuarioId
                    };

                error.Estado = CatalogoOfflineEstados.Error;
                error.UltimoError = ex.Message;
                error.Mensaje =
                    string.IsNullOrWhiteSpace(error.PaqueteActivoId)
                        ? "No se pudo completar la descarga."
                        : "La descarga no terminó. Se conserva la copia anterior.";

                await database.GuardarEstadoPaqueteAsync(error);
                Notificar(ConvertirEstado(error));

                return ResultadoDescargaOffline.Fail(
                    error.Mensaje + " " + ex.Message);
            }
            finally
            {
                downloadLock.Release();
            }
        }

        private async Task<(int Total, long Bytes)> GuardarPasoAsync(
            string usuarioId,
            string paqueteId,
            string seccion,
            JsonNode data,
            int paso,
            int totalPasos,
            string mensaje,
            int totalActual,
            long bytesActual,
            CatalogoOfflineEstadoEntity estado)
        {
            (int count, long bytes) = await GuardarSeccionAsync(
                usuarioId,
                paqueteId,
                seccion,
                data);

            int total = totalActual + count;
            long tamano = bytesActual + bytes;

            estado.Estado = CatalogoOfflineEstados.Descargando;
            estado.PasoActual = paso;
            estado.TotalPasos = totalPasos;
            estado.ProgresoPorcentaje =
                (int)Math.Round(paso * 100d / totalPasos);
            estado.Mensaje = mensaje;
            estado.TotalRegistros = total;
            estado.TamanoBytes = tamano;

            await database.GuardarEstadoPaqueteAsync(estado);
            Notificar(ConvertirEstado(estado));

            return (total, tamano);
        }

        private async Task<(int Count, long Bytes)> GuardarSeccionAsync(
            string usuarioId,
            string paqueteId,
            string seccion,
            JsonNode data)
        {
            string json = data.ToJsonString();
            int count = data is JsonArray array ? array.Count : 1;
            long bytes = Encoding.UTF8.GetByteCount(json);

            await database.GuardarSeccionPaqueteAsync(
                new CatalogoOfflineSeccionEntity
                {
                    Clave = $"{usuarioId}|{paqueteId}|{seccion}",
                    UsuarioId = usuarioId,
                    PaqueteId = paqueteId,
                    Seccion = seccion,
                    Json = json,
                    TotalRegistros = count,
                    GuardadoUtc = DateTime.UtcNow
                });

            return (count, bytes);
        }

        private async Task<JsonArray> DescargarArrayAsync(
            string ruta,
            CancellationToken cancellationToken)
        {
            JsonNode data =
                await DescargarDataAsync(ruta, cancellationToken);

            return data as JsonArray ??
                throw new InvalidOperationException(
                    $"La ruta {ruta} no devolvió un listado válido.");
        }

        private async Task<JsonNode> DescargarDataAsync(
            string ruta,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ruta);

            request.Headers.TryAddWithoutValidation(
                HeaderBypass,
                "true");

            using HttpResponseMessage response =
                await ApiClientService.Client.SendAsync(
                    request,
                    cancellationToken);

            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"No fue posible descargar {ruta}. " +
                    $"El servidor respondió {(int)response.StatusCode}.");
            }

            JsonNode? root = JsonNode.Parse(json);

            if (root == null)
            {
                throw new InvalidOperationException(
                    $"La ruta {ruta} devolvió una respuesta vacía.");
            }

            if (root is JsonObject obj &&
                BuscarPropiedad(obj, "data") is JsonNode data)
            {
                return data.DeepClone();
            }

            return root.DeepClone();
        }

        private async Task<JsonNode> DescargarDataOpcionalAsync(
            string ruta,
            JsonNode valorPredeterminado,
            CancellationToken cancellationToken)
        {
            try
            {
                return await DescargarDataAsync(
                    ruta,
                    cancellationToken);
            }
            catch
            {
                return valorPredeterminado;
            }
        }

        private async Task<string> ObtenerVersionServidorAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                JsonNode data = await DescargarDataAsync(
                    "api/contenido-sincronizacion/" +
                    "estado?modulo=catalogos",
                    cancellationToken);

                return ObtenerTexto(data, "version");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static JsonNode? BuscarPropiedad(
            JsonObject obj,
            string nombre)
        {
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                if (string.Equals(
                        property.Key,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static string ObtenerTexto(JsonNode node, string nombre)
        {
            if (node is not JsonObject obj)
                return string.Empty;

            JsonNode? value = BuscarPropiedad(obj, nombre);
            return value?.GetValue<string>() ?? string.Empty;
        }

        private static int ObtenerEntero(
            JsonNode node,
            string nombre,
            int predeterminado)
        {
            if (node is not JsonObject obj)
                return predeterminado;

            JsonNode? value = BuscarPropiedad(obj, nombre);

            return value != null &&
                   int.TryParse(
                       value.ToJsonString().Trim('"'),
                       out int numero)
                ? numero
                : predeterminado;
        }

        private static JsonArray ExtraerArray(
            JsonNode node,
            string propiedad)
        {
            if (node is JsonArray direct)
                return direct;

            if (node is JsonObject obj &&
                BuscarPropiedad(obj, propiedad) is JsonArray array)
            {
                return array;
            }

            return new JsonArray();
        }

        private static IEnumerable<int> ObtenerIds(
            JsonArray array,
            string nombrePropiedad)
        {
            foreach (JsonNode? node in array)
            {
                if (node is not JsonObject obj)
                    continue;

                JsonNode? value = BuscarPropiedad(obj, nombrePropiedad);

                if (value != null &&
                    int.TryParse(
                        value.ToJsonString().Trim('"'),
                        out int id) &&
                    id > 0)
                {
                    yield return id;
                }
            }
        }

        private static void AgregarTodos(
            JsonArray destino,
            JsonArray origen)
        {
            foreach (JsonNode? item in origen)
            {
                if (item != null)
                    destino.Add(item.DeepClone());
            }
        }

        private void Notificar(EstadoPaqueteOffline estado)
        {
            EstadoCambiado?.Invoke(
                this,
                new EstadoPaqueteOfflineEventArgs(estado));
        }

        private static EstadoPaqueteOffline ConvertirEstado(
            CatalogoOfflineEstadoEntity? entity)
        {
            if (entity == null)
                return new EstadoPaqueteOffline();

            return new EstadoPaqueteOffline
            {
                Estado = entity.Estado,
                Mensaje = entity.Mensaje,
                ProgresoPorcentaje = entity.ProgresoPorcentaje,
                PasoActual = entity.PasoActual,
                TotalPasos = entity.TotalPasos,
                TotalRegistros = entity.TotalRegistros,
                TamanoBytes = entity.TamanoBytes,
                UltimaDescargaCompletaUtc =
                    entity.UltimaDescargaCompletaUtc,
                TienePaqueteCompleto =
                    !string.IsNullOrWhiteSpace(entity.PaqueteActivoId)
            };
        }

        private static string ObtenerUsuarioId()
        {
            string value = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            return string.IsNullOrWhiteSpace(value) ? "0" : value;
        }
    }
}
