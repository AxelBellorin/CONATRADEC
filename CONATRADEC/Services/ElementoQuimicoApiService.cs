using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class ElementoQuimicoApiService
    {
        private readonly HttpClient httpClient;

        public ElementoQuimicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public ElementoQuimicoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<ElementoQuimicoResponse>>> GetElementoQuimicoResultAsync(
            CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<ElementoQuimicoResponse>(
                httpClient,
                "api/elemento-quimico/listar",
                "los elementos químicos",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CreateElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/elemento-quimico/crear",
                elemento,
                "crear el elemento químico",
                "Elemento químico creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de elemento químico válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/elemento-quimico/editar/{elemento.ElementoQuimicosId.Value}",
                elemento,
                "actualizar el elemento químico",
                "Elemento químico actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de elemento químico válido."));
            }

            return ApiServiceHelper.SendAsync<ElementoQuimicoRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/elemento-quimico/eliminar/{elemento.ElementoQuimicosId.Value}",
                null,
                "eliminar el elemento químico",
                "Elemento químico eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<ElementoQuimicoResponse>> GetElementoQuimicoAsync()
        {
            var result = await GetElementoQuimicoResultAsync();
            return result.Data ?? new ObservableCollection<ElementoQuimicoResponse>();
        }

        public async Task<bool> CreateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            var result = await CreateElementoQuimicoResultAsync(elemento);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            var result = await UpdateElementoQuimicoResultAsync(elemento);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            var result = await DeleteElementoQuimicoResultAsync(elemento);
            return result.Success && result.Data == true;
        }
    }
}
