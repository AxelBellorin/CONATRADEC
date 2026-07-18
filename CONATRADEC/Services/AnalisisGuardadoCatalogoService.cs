using CONATRADEC.Models;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    public sealed class AnalisisGuardadoCatalogoService
    {
        private readonly HttpClient httpClient;

        public AnalisisGuardadoCatalogoService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisGuardadoCatalogoService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<List<CatalogoElementoAnalisis>> ListarElementosAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<List<CatalogoElementoAnalisis>>(
                           "api/elemento-quimico/listar",
                           cancellationToken)
                       ?? new List<CatalogoElementoAnalisis>();
            }
            catch
            {
                return new List<CatalogoElementoAnalisis>();
            }
        }

        public async Task<List<CatalogoFuenteAnalisis>> ListarFuentesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<List<CatalogoFuenteAnalisis>>(
                           "api/fuente-nutriente/listar",
                           cancellationToken)
                       ?? new List<CatalogoFuenteAnalisis>();
            }
            catch
            {
                return new List<CatalogoFuenteAnalisis>();
            }
        }
    }
}
