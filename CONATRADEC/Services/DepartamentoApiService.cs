using CONATRADEC.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class DepartamentoApiService
    {
        private readonly HttpClient httpClient;

        private sealed record CacheEntry(
            List<DepartamentoResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorPais = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorPais = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public DepartamentoApiService()
            : this(ApiClientService.Client)
        {
        }

        public DepartamentoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Endpoint completo conservado para pickers y formularios.
        /// </summary>
        public Task<ApiResult<ObservableCollection<DepartamentoResponse>>>
            GetDepartamentosResultAsync(
                int? paisId,
                CancellationToken cancellationToken = default)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<DepartamentoResponse>>.Fail(
                        "No se recibió un país válido para cargar sus departamentos."));
            }

            return ApiServiceHelper.GetCollectionAsync<DepartamentoResponse>(
                httpClient,
                $"api/departamento/por-pais/{paisId.Value}",
                "los departamentos",
                cancellationToken);
        }

        /// <summary>
        /// Endpoint administrativo paginado con validación de permisos backend.
        /// </summary>
        public async Task<ApiResult<DepartamentoPaginaResponse>>
            BuscarDepartamentosAsync(
                int paisId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (paisId <= 0)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "No se recibió un país válido.");
            }

            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/ubicaciones/departamentos" +
                $"?paisId={paisId}" +
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
                    return ApiResult<DepartamentoPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los departamentos.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PaginaAdminResponse<DepartamentoAdminItem>? data =
                    await response.Content
                        .ReadFromJsonAsync<PaginaAdminResponse<DepartamentoAdminItem>>(
                            cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<DepartamentoPaginaResponse>.Fail(
                        "El servidor no devolvió la página de departamentos esperada.");
                }

                return ApiResult<DepartamentoPaginaResponse>.Ok(
                    new DepartamentoPaginaResponse
                    {
                        Items = data.Items
                            .Where(item =>
                                item.DepartamentoId > 0 &&
                                item.Activo)
                            .Select(item => new DepartamentoResponse
                            {
                                DepartamentoId = item.DepartamentoId,
                                NombreDepartamento = item.Nombre,
                                PaisId = item.PaisId,
                                Activo = item.Activo,
                                CantidadMunicipios =
                                    item.CantidadDependencias
                            })
                            .ToList(),
                        PaginaActual = data.PaginaActual,
                        TamanoPagina = data.TamanoPagina,
                        TotalRegistros = data.TotalRegistros,
                        TotalPaginas = data.TotalPaginas,
                        PaisId = paisId
                    });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "La carga de departamentos tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar los departamentos.");
            }
            catch (JsonException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de departamentos no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los departamentos.");
            }
        }

        public async Task<ApiResult<bool>> CreateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.PaisId.HasValue || departamento.PaisId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un país válido.");
            }

            // Se conserva esta ruta para mantener la detección/reactivación
            // centralizada de registros eliminados en ApiServiceHelper.
            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/departamento/crear",
                departamento,
                "crear el departamento",
                "Departamento creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> UpdateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/administracion/ubicaciones/departamentos/{departamento.DepartamentoId.Value}",
                new DepartamentoAdministracionRequest
                {
                    PaisId = departamento.PaisId ?? 0,
                    Nombre = departamento.NombreDepartamento ?? string.Empty
                },
                "actualizar el departamento",
                "Departamento actualizado correctamente.",
                cancellationToken);

            if (result.Success && departamento.PaisId.HasValue)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> DeleteDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync<DepartamentoAdministracionRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/administracion/ubicaciones/departamentos/{departamento.DepartamentoId.Value}",
                    null,
                    "eliminar el departamento",
                    "Departamento eliminado correctamente.",
                    cancellationToken);

            if (result.Success && departamento.PaisId.HasValue)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ObservableCollection<DepartamentoResponse>>
            GetDepartamentosAsync(int? paisId)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
                return new ObservableCollection<DepartamentoResponse>();

            int id = paisId.Value;

            if (ObtenerCacheVigente(id) is List<DepartamentoResponse> cache)
                return new ObservableCollection<DepartamentoResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorPais.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<DepartamentoResponse> vigente)
                {
                    return new ObservableCollection<DepartamentoResponse>(
                        vigente);
                }

                ApiResult<ObservableCollection<DepartamentoResponse>> result =
                    await GetDepartamentosResultAsync(id);

                List<DepartamentoResponse> items = result.Data?
                    .Where(item => item.DepartamentoId is > 0)
                    .ToList()
                    ?? new List<DepartamentoResponse>();

                CachePorPais[id] = new CacheEntry(items, DateTime.UtcNow);

                return new ObservableCollection<DepartamentoResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        // Métodos conservados para no afectar código existente.
        public async Task<bool> CreateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await CreateDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await UpdateDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await DeleteDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        private static List<DepartamentoResponse>? ObtenerCacheVigente(
            int paisId)
        {
            if (!CachePorPais.TryGetValue(paisId, out CacheEntry? entry))
                return null;

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorPais.TryRemove(paisId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache(int paisId)
        {
            if (paisId > 0)
                CachePorPais.TryRemove(paisId, out _);
        }

        private sealed class PaginaAdminResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int PaginaActual { get; set; }
            public int TamanoPagina { get; set; }
            public int TotalRegistros { get; set; }
            public int TotalPaginas { get; set; }
        }

        private sealed class DepartamentoAdminItem
        {
            public int DepartamentoId { get; set; }
            public int PaisId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public bool Activo { get; set; }
            public int CantidadDependencias { get; set; }
        }

        private sealed class DepartamentoAdministracionRequest
        {
            public int PaisId { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }
    }
}
