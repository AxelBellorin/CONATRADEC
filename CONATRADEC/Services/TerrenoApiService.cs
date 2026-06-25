using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public class TerrenoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public TerrenoApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<ObservableCollection<TerrenoResponse>> GetTerrenosAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<TerrenoResponse>>(
                    "api/terreno/listar");

                if (response == null)
                    return new ObservableCollection<TerrenoResponse>();

                return new ObservableCollection<TerrenoResponse>(
                    response.Where(t => t.Activo != false)
                );
            }
            catch
            {
                return new ObservableCollection<TerrenoResponse>();
            }
        }

        public async Task<bool> CreateTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/terreno/crear", terreno);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TerrenoResponse?> CreateTerrenoRetornandoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/terreno/crear", terreno);

                if (!response.IsSuccessStatusCode)
                    return null;

                string contenido = await response.Content.ReadAsStringAsync();

                var terrenoCreado = IntentarLeerTerrenoCreado(contenido);

                if (terrenoCreado != null &&
                    terrenoCreado.TerrenoId.HasValue &&
                    terrenoCreado.TerrenoId.Value > 0)
                {
                    return terrenoCreado;
                }

                if (!string.IsNullOrWhiteSpace(terreno.CodigoTerreno))
                {
                    var terrenos = await GetTerrenosAsync();

                    return terrenos
                        .Where(t => string.Equals(
                            t.CodigoTerreno?.Trim(),
                            terreno.CodigoTerreno.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(t => t.TerrenoId ?? 0)
                        .FirstOrDefault();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/terreno/editar/{terreno.TerrenoId}",
                    terreno);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/terreno/eliminar/{terreno.TerrenoId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private TerrenoResponse? IntentarLeerTerrenoCreado(string contenido)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contenido))
                    return null;

                using var document = JsonDocument.Parse(contenido);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                var directo = JsonSerializer.Deserialize<TerrenoResponse>(
                    root.GetRawText(),
                    jsonOptions);

                if (directo != null &&
                    directo.TerrenoId.HasValue &&
                    directo.TerrenoId.Value > 0)
                {
                    return directo;
                }

                string[] posiblesNodos =
                {
                    "terreno",
                    "data",
                    "registro",
                    "resultado",
                    "item"
                };

                foreach (var nodo in posiblesNodos)
                {
                    if (TryGetPropertyIgnoreCase(root, nodo, out var elemento) &&
                        elemento.ValueKind == JsonValueKind.Object)
                    {
                        var desdeNodo = JsonSerializer.Deserialize<TerrenoResponse>(
                            elemento.GetRawText(),
                            jsonOptions);

                        if (desdeNodo != null &&
                            desdeNodo.TerrenoId.HasValue &&
                            desdeNodo.TerrenoId.Value > 0)
                        {
                            return desdeNodo;
                        }
                    }
                }

                if (TryGetIntIgnoreCase(root, "terrenoId", out int terrenoId) ||
                    TryGetIntIgnoreCase(root, "id", out terrenoId))
                {
                    return new TerrenoResponse
                    {
                        TerrenoId = terrenoId
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private bool TryGetIntIgnoreCase(JsonElement element, string propertyName, out int value)
        {
            value = 0;

            if (!TryGetPropertyIgnoreCase(element, propertyName, out var property))
                return false;

            if (property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt32(out value))
                return true;

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out value))
                return true;

            return false;
        }
    }
}