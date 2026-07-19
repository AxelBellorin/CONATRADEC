using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public sealed class AnalisisGuardadoCatalogoService
    {
        private readonly ElementoQuimicoApiService elementoQuimicoApiService;
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService;

        public AnalisisGuardadoCatalogoService()
        {
            elementoQuimicoApiService = new ElementoQuimicoApiService();
            fuenteNutrienteApiService = new FuenteNutrienteApiService();
        }

        public async Task<List<CatalogoElementoAnalisis>> ListarElementosAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                ObservableCollection<ElementoQuimicoResponse> elementos =
                    await elementoQuimicoApiService.GetElementoQuimicoAsync();

                return elementos
                    .Where(x =>
                        x != null &&
                        x.ElementoQuimicosId.HasValue &&
                        x.ElementoQuimicosId.Value > 0)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .Select(grupo =>
                    {
                        ElementoQuimicoResponse elemento = grupo.First();

                        return new CatalogoElementoAnalisis
                        {
                            ElementoQuimicosId = elemento.ElementoQuimicosId,
                            SimboloElementoQuimico =
                                elemento.SimboloElementoQuimico ?? string.Empty,
                            NombreElementoQuimico =
                                elemento.NombreElementoQuimico ?? string.Empty
                        };
                    })
                    .ToList();
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
                ObservableCollection<FuenteNutrienteResponse> fuentes =
                    await fuenteNutrienteApiService.GetFuenteNutrienteAsync();

                return fuentes
                    .Where(x =>
                        x != null &&
                        x.FuenteNutrientesId.HasValue &&
                        x.FuenteNutrientesId.Value > 0)
                    .GroupBy(x => x.FuenteNutrientesId!.Value)
                    .Select(grupo =>
                    {
                        FuenteNutrienteResponse fuente = grupo.First();

                        return new CatalogoFuenteAnalisis
                        {
                            FuenteNutrientesId = fuente.FuenteNutrientesId,
                            NombreNutriente =
                                fuente.NombreNutriente ?? string.Empty
                        };
                    })
                    .ToList();
            }
            catch
            {
                return new List<CatalogoFuenteAnalisis>();
            }
        }
    }
}
