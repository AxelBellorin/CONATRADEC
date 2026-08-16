using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class PropietarioApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient httpClient;

        public PropietarioApiService()
            : this(ApiClientService.Client)
        {
        }

        public PropietarioApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Método compatible con versiones anteriores. Conserva el endpoint
        /// sin paginación para los puntos del sistema que todavía lo necesiten.
        /// Los listados visuales nuevos deben utilizar BuscarPaginadoAsync.
        /// </summary>
        public async Task<ApiResult<
            ObservableCollection<PropietarioResponse>>>
            GetPropietariosResultAsync(
                string? buscar = null,
                bool incluirInactivos = false,
                bool paraSeleccionTerreno = false,
                CancellationToken cancellationToken = default)
        {
            try
            {
                string baseRuta =
                    paraSeleccionTerreno
                        ? "api/terreno/propietarios-disponibles"
                        : "api/parametrizacion-acceso/propietarios";

                string ruta =
                    baseRuta +
                    $"?buscar={Uri.EscapeDataString(
                        buscar?.Trim() ??
                        string.Empty)}";

                if (!paraSeleccionTerreno)
                {
                    ruta +=
                        "&incluirInactivos=" +
                        incluirInactivos
                            .ToString()
                            .ToLowerInvariant();
                }

                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<
                        ObservableCollection<PropietarioResponse>>
                        .Fail(
                            await ObtenerMensajeAsync(
                                response,
                                "No fue posible cargar los propietarios.",
                                cancellationToken),
                            (int)response.StatusCode);
                }

                List<PropietarioResponse>? items =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<PropietarioResponse>>(
                            JsonOptions,
                            cancellationToken);

                return ApiResult<
                    ObservableCollection<PropietarioResponse>>
                    .Ok(
                        new ObservableCollection<
                            PropietarioResponse>(
                            items ?? []));
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    ObservableCollection<PropietarioResponse>>
                    .Fail(
                        "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    ObservableCollection<PropietarioResponse>>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    ObservableCollection<PropietarioResponse>>
                    .Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    ObservableCollection<PropietarioResponse>>
                    .Fail(
                        "El servidor devolvió propietarios con un formato no reconocido.");
            }
        }

        /// <summary>
        /// Obtiene únicamente la página visible. En modo selección utiliza un
        /// endpoint que no exige permisos administrativos de propietarios.
        ///
        /// excluirPropietarioId se usa únicamente en el selector de
        /// "Terrenos del propietario" para que la persona actual no forme parte
        /// del total ni genere páginas vacías después de filtrarla en cliente.
        /// </summary>
        public async Task<ApiResult<PropietarioPaginaResponse>>
            BuscarPaginadoAsync(
                string? buscar,
                bool incluirInactivos,
                bool paraSeleccionTerreno,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default,
                int? excluirPropietarioId = null)
        {
            pagina =
                Math.Max(
                    1,
                    pagina);

            tamanoPagina =
                Math.Clamp(
                    tamanoPagina,
                    6,
                    100);

            string baseRuta =
                paraSeleccionTerreno
                    ? "api/terreno/propietarios-disponibles/paginado"
                    : "api/parametrizacion-acceso/propietarios/paginado";

            string ruta =
                baseRuta +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                $"&buscar={Uri.EscapeDataString(
                    buscar?.Trim() ??
                    string.Empty)}";

            if (paraSeleccionTerreno)
            {
                if (excluirPropietarioId is > 0)
                {
                    ruta +=
                        $"&excluirPropietarioId={excluirPropietarioId.Value}";
                }
            }
            else
            {
                ruta +=
                    "&incluirInactivos=" +
                    incluirInactivos
                        .ToString()
                        .ToLowerInvariant();
            }

            return await ConsultarPaginaAsync(
                ruta,
                cancellationToken);
        }

        /// <summary>
        /// Página exclusiva de propietarios eliminados utilizada por la ventana
        /// común de Eliminados. No mezcla registros activos con inactivos.
        /// </summary>
        public Task<ApiResult<PropietarioPaginaResponse>>
            BuscarInactivosPaginadoAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina =
                Math.Max(
                    1,
                    pagina);

            tamanoPagina =
                Math.Clamp(
                    tamanoPagina,
                    6,
                    100);

            string ruta =
                "api/parametrizacion-acceso/propietarios/paginado" +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                $"&buscar={Uri.EscapeDataString(
                    buscar?.Trim() ??
                    string.Empty)}" +
                "&incluirInactivos=true" +
                "&soloInactivos=true";

            return ConsultarPaginaAsync(
                ruta,
                cancellationToken);
        }

        private async Task<ApiResult<PropietarioPaginaResponse>>
            ConsultarPaginaAsync(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PropietarioPaginaResponse>
                        .Fail(
                            await ObtenerMensajeAsync(
                                response,
                                "No fue posible cargar los propietarios.",
                                cancellationToken),
                            (int)response.StatusCode);
                }

                PropietarioPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<
                            PropietarioPaginaResponse>(
                            JsonOptions,
                            cancellationToken);

                if (data == null)
                {
                    return ApiResult<PropietarioPaginaResponse>
                        .Fail(
                            "El servidor no devolvió la página de propietarios esperada.");
                }

                data.Items ??= [];

                return ApiResult<PropietarioPaginaResponse>
                    .Ok(data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PropietarioPaginaResponse>
                    .Fail(
                        "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PropietarioPaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PropietarioPaginaResponse>
                    .Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PropietarioPaginaResponse>
                    .Fail(
                        "El servidor devolvió una página con formato no reconocido.");
            }
            catch
            {
                return ApiResult<PropietarioPaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los propietarios.");
            }
        }

        /// <summary>
        /// Recupera un solo propietario. Evita descargar el catálogo completo
        /// cuando el formulario necesita completar una relación existente.
        /// </summary>
        public async Task<ApiResult<PropietarioResponse>>
            ObtenerDisponiblePorIdAsync(
                int propietarioId,
                CancellationToken cancellationToken = default)
        {
            if (propietarioId <= 0)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/terreno/propietarios-disponibles/" +
                        propietarioId,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PropietarioResponse>
                        .Fail(
                            await ObtenerMensajeAsync(
                                response,
                                "No fue posible cargar el propietario.",
                                cancellationToken),
                            (int)response.StatusCode);
                }

                PropietarioResponse? propietario =
                    await response.Content
                        .ReadFromJsonAsync<
                            PropietarioResponse>(
                            JsonOptions,
                            cancellationToken);

                return propietario == null
                    ? ApiResult<PropietarioResponse>
                        .Fail(
                            "El servidor no devolvió el propietario solicitado.")
                    : ApiResult<PropietarioResponse>
                        .Ok(
                            propietario);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "El servidor devolvió un formato no reconocido.");
            }
        }

        /// <summary>
        /// Recupera un propietario desde la ruta administrativa. Se conserva
        /// para formularios que ya tienen permiso de propietarios.
        /// </summary>
        public async Task<ApiResult<PropietarioResponse>>
            ObtenerPorIdAsync(
                int propietarioId,
                CancellationToken cancellationToken = default)
        {
            if (propietarioId <= 0)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioId}",
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PropietarioResponse>
                        .Fail(
                            await ObtenerMensajeAsync(
                                response,
                                "No fue posible cargar el propietario.",
                                cancellationToken),
                            (int)response.StatusCode);
                }

                PropietarioDetalleResponse? detalle =
                    await response.Content
                        .ReadFromJsonAsync<
                            PropietarioDetalleResponse>(
                            JsonOptions,
                            cancellationToken);

                return detalle?.Propietario == null
                    ? ApiResult<PropietarioResponse>
                        .Fail(
                            "El servidor no devolvió el propietario solicitado.")
                    : ApiResult<PropietarioResponse>
                        .Ok(
                            detalle.Propietario);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PropietarioResponse>
                    .Fail(
                        "El servidor devolvió un formato no reconocido.");
            }
        }

        public async Task<ApiResult<int>>
            CrearPropietarioResultAsync(
                PropietarioGuardarRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/parametrizacion-acceso/propietarios",
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<int>
                        .Fail(
                            await ObtenerMensajeAsync(
                                response,
                                "No fue posible crear el propietario.",
                                cancellationToken),
                            (int)response.StatusCode);
                }

                string contenido =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                int propietarioId =
                    ExtraerPropietarioId(
                        contenido);

                return propietarioId > 0
                    ? ApiResult<int>.Ok(
                        propietarioId,
                        "Propietario creado correctamente.")
                    : ApiResult<int>.Fail(
                        "El propietario se procesó, pero no se recibió su identificador.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<int>.Fail(
                    "La solicitud tardó demasiado.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<int>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch
            {
                return ApiResult<int>.Fail(
                    "Ocurrió un error inesperado al crear el propietario.");
            }
        }

        public async Task<ApiResult<bool>>
            ActualizarPropietarioResultAsync(
                int propietarioId,
                PropietarioGuardarRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (propietarioId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PutAsJsonAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioId}",
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ObtenerMensajeAsync(
                            response,
                            "No fue posible actualizar el propietario.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Propietario actualizado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al actualizar el propietario.");
            }
        }

        private static async Task<string>
            ObtenerMensajeAsync(
                HttpResponseMessage response,
                string predeterminado,
                CancellationToken cancellationToken)
        {
            try
            {
                string contenido =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!string.IsNullOrWhiteSpace(
                        contenido))
                {
                    using JsonDocument documento =
                        JsonDocument.Parse(
                            contenido);

                    JsonElement raiz =
                        documento.RootElement;

                    foreach (string nombre in new[]
                    {
                        "message",
                        "mensaje",
                        "error"
                    })
                    {
                        if (TryGetPropertyIgnoreCase(
                                raiz,
                                nombre,
                                out JsonElement valor) &&
                            valor.ValueKind ==
                                JsonValueKind.String)
                        {
                            string? texto =
                                valor.GetString();

                            if (!string.IsNullOrWhiteSpace(
                                    texto))
                            {
                                return texto;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Se conserva el mensaje predeterminado.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",

                HttpStatusCode.Forbidden =>
                    "No tiene permiso para realizar esta operación.",

                HttpStatusCode.Conflict =>
                    "Ya existe un propietario con esos datos.",

                HttpStatusCode.NotFound =>
                    "No se encontró el propietario.",

                _ =>
                    predeterminado
            };
        }

        private static int ExtraerPropietarioId(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return 0;
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(
                        contenido);

                JsonElement raiz =
                    documento.RootElement;

                if (TryGetPropertyIgnoreCase(
                        raiz,
                        "data",
                        out JsonElement data) &&
                    data.ValueKind ==
                        JsonValueKind.Object)
                {
                    raiz = data;
                }

                if (TryGetPropertyIgnoreCase(
                        raiz,
                        "propietarioId",
                        out JsonElement id))
                {
                    if (id.ValueKind ==
                            JsonValueKind.Number &&
                        id.TryGetInt32(
                            out int numero))
                    {
                        return numero;
                    }

                    if (id.ValueKind ==
                            JsonValueKind.String &&
                        int.TryParse(
                            id.GetString(),
                            out numero))
                    {
                        return numero;
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement elemento,
            string nombre,
            out JsonElement valor)
        {
            if (elemento.ValueKind !=
                JsonValueKind.Object)
            {
                valor = default;
                return false;
            }

            foreach (JsonProperty propiedad
                     in elemento.EnumerateObject())
            {
                if (string.Equals(
                        propiedad.Name,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    valor =
                        propiedad.Value;

                    return true;
                }
            }

            valor = default;
            return false;
        }
    }
}
