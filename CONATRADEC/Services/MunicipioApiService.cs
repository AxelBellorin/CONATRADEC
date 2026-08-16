using CONATRADEC.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class MunicipioApiService
    {
        private readonly HttpClient httpClient;

        private sealed record CacheEntry(
            List<MunicipioResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorDepartamento = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorDepartamento = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public MunicipioApiService()
            : this(ApiClientService.Client)
        {
        }

        public MunicipioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Listado completo conservado para formularios y selectores.
        /// </summary>
        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosResultAsync(
                int? departamentoId,
                CancellationToken cancellationToken = default)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<MunicipioResponse>>.Fail(
                        "Seleccione un departamento válido."));
            }

            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                $"api/municipio/por-departamento/{departamentoId.Value}",
                "los municipios",
                cancellationToken);
        }

        /// <summary>
        /// Listado geográfico utilizado por Terreno y Usuario.
        /// </summary>
        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosConUbicacionResultAsync(
                CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                "api/municipio/listarTodos-por-departamento-por-pais",
                "los municipios",
                cancellationToken);
        }

        /// <summary>
        /// Consulta administrativa paginada con permisos backend.
        /// </summary>
        public async Task<ApiResult<MunicipioPaginaResponse>>
            BuscarMunicipiosAsync(
                int departamentoId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (departamentoId <= 0)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "No se recibió un departamento válido.");
            }

            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/ubicaciones/municipios" +
                $"?departamentoId={departamentoId}" +
                $"&pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                "&incluirInactivos=false";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(ruta, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<MunicipioPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los municipios.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PaginaAdminResponse<MunicipioAdminItem>? data =
                    await response.Content
                        .ReadFromJsonAsync<PaginaAdminResponse<MunicipioAdminItem>>(
                            cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<MunicipioPaginaResponse>.Fail(
                        "El servidor no devolvió la página de municipios esperada.");
                }

                return ApiResult<MunicipioPaginaResponse>.Ok(
                    new MunicipioPaginaResponse
                    {
                        Items = data.Items
                            .Where(item => item.MunicipioId > 0 && item.Activo)
                            .Select(item => new MunicipioResponse
                            {
                                MunicipioId = item.MunicipioId,
                                NombreMunicipio = item.Nombre,
                                DepartamentoId = item.DepartamentoId,
                                Activo = item.Activo,
                                CantidadTerrenos = item.CantidadTerrenos,
                                CantidadUsuarios = item.CantidadUsuarios
                            })
                            .ToList(),
                        PaginaActual = data.PaginaActual,
                        TamanoPagina = data.TamanoPagina,
                        TotalRegistros = data.TotalRegistros,
                        TotalPaginas = data.TotalPaginas,
                        DepartamentoId = departamentoId
                    });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "La carga de municipios tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar los municipios.");
            }
            catch (JsonException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de municipios no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los municipios.");
            }
        }

        public async Task<ApiResult<bool>> CreateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.DepartamentoId.HasValue ||
                municipio.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un departamento válido.");
            }

            // Se conserva esta ruta para mantener la resolución centralizada
            // de coincidencias con municipios eliminados.
            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/municipio/crear",
                municipio,
                "crear el municipio",
                "Municipio creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> UpdateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/administracion/ubicaciones/municipios/{municipio.MunicipioId.Value}",
                new MunicipioAdministracionRequest
                {
                    DepartamentoId = municipio.DepartamentoId ?? 0,
                    Nombre = municipio.NombreMunicipio ?? string.Empty
                },
                "actualizar el municipio",
                "Municipio actualizado correctamente.",
                cancellationToken);

            if (result.Success && municipio.DepartamentoId.HasValue)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> DeleteMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync<MunicipioAdministracionRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/administracion/ubicaciones/municipios/{municipio.MunicipioId.Value}",
                    null,
                    "eliminar el municipio",
                    "Municipio eliminado correctamente.",
                    cancellationToken);

            if (result.Success && municipio.DepartamentoId.HasValue)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ObservableCollection<MunicipioResponse>>
            GetMunicipiosAsync(int? departamentoId)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
                return new ObservableCollection<MunicipioResponse>();

            int id = departamentoId.Value;

            if (ObtenerCacheVigente(id) is List<MunicipioResponse> cache)
                return new ObservableCollection<MunicipioResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorDepartamento.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<MunicipioResponse> vigente)
                    return new ObservableCollection<MunicipioResponse>(vigente);

                ApiResult<ObservableCollection<MunicipioResponse>> result =
                    await GetMunicipiosResultAsync(id);

                List<MunicipioResponse> items = result.Data?
                    .Where(item => item.MunicipioId is > 0)
                    .ToList()
                    ?? new List<MunicipioResponse>();

                CachePorDepartamento[id] =
                    new CacheEntry(items, DateTime.UtcNow);

                return new ObservableCollection<MunicipioResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        // Métodos conservados para no afectar código existente.
        public async Task<bool> CreateMunicipioAsync(MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await CreateMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateMunicipioAsync(MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await UpdateMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteMunicipioAsync(MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await DeleteMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        private static List<MunicipioResponse>? ObtenerCacheVigente(
            int departamentoId)
        {
            if (!CachePorDepartamento.TryGetValue(
                    departamentoId,
                    out CacheEntry? entry))
            {
                return null;
            }

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorDepartamento.TryRemove(departamentoId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache(int departamentoId)
        {
            if (departamentoId > 0)
                CachePorDepartamento.TryRemove(departamentoId, out _);
        }

        private sealed class PaginaAdminResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int PaginaActual { get; set; }
            public int TamanoPagina { get; set; }
            public int TotalRegistros { get; set; }
            public int TotalPaginas { get; set; }
        }

        private sealed class MunicipioAdminItem
        {
            public int MunicipioId { get; set; }
            public int DepartamentoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public bool Activo { get; set; }
            public int CantidadTerrenos { get; set; }
            public int CantidadUsuarios { get; set; }
        }

        private sealed class MunicipioAdministracionRequest
        {
            public int DepartamentoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }
    }
}
