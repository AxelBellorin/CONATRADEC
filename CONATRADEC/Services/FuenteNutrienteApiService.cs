using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CONATRADEC.Services;

public class FuenteNutrienteApiService
{
    private const string RutaAdministrativa =
        "api/administracion/fuentes-nutrientes";

    private const string CodigoInactivoExistente =
        "FUENTE_NUTRIENTE_INACTIVA_EXISTENTE";

    private const string ReactivarOpcion =
        "Reactivar y reemplazar con estos datos";

    private const string CrearOpcion =
        "Crear una nueva fuente";

    private readonly HttpClient httpClient;

    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public FuenteNutrienteApiService()
        : this(ApiClientService.Client)
    {
    }

    public FuenteNutrienteApiService(HttpClient httpClient) =>
        this.httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));

    // =============================================================
    // CONTRATOS HISTÓRICOS
    // =============================================================
    public Task<ApiResult<ObservableCollection<FuenteNutrienteResponse>>>
        GetFuenteNutrienteResultAsync(
            CancellationToken cancellationToken = default) =>
        ObtenerColeccionAsync(
            "api/fuente-nutriente/listar",
            "cargar las fuentes de nutrientes",
            cancellationToken);

    public Task<ApiResult<ObservableCollection<FuenteNutrienteResponse>>>
        GetFuenteNutrienteInactivasResultAsync(
            CancellationToken cancellationToken = default) =>
        ObtenerColeccionAsync(
            "api/fuente-nutriente/listar-inactivas",
            "cargar las fuentes eliminadas",
            cancellationToken);

    public async Task<ApiResult<FuenteNutrienteResponse>>
        CreateFuenteNutrienteResultAsync(
            FuenteNutrienteRequest fuente,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        try
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/fuente-nutriente/crear-con-elementos",
                    fuente,
                    jsonOptions,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ResultadoFuente(
                    contenido,
                    "Fuente creada correctamente.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ApiDataResponse<FuenteNutrienteResponse>? conflicto =
                    DeserializarSobre(contenido);

                if (conflicto?.Data?.FuenteNutrientesId > 0 &&
                    conflicto.Data.Activo != true)
                {
                    return await ResolverInactivaHistoricaAsync(
                        fuente,
                        conflicto.Data,
                        cancellationToken);
                }
            }

            return ErrorFuente(
                response.StatusCode,
                contenido,
                "crear la fuente");
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                "crear la fuente de nutriente");
        }
    }

    public Task<ApiResult<FuenteNutrienteResponse>>
        CreateFuenteNutrienteConfirmadaResultAsync(
            FuenteNutrienteRequest fuente,
            CancellationToken cancellationToken = default) =>
        EnviarFuenteHistoricaAsync(
            HttpMethod.Post,
            "api/fuente-nutriente/crear-con-elementos-confirmado",
            fuente,
            "crear la nueva fuente",
            "Nueva fuente creada correctamente.",
            cancellationToken);

    public Task<ApiResult<FuenteNutrienteResponse>>
        ReactivarYActualizarFuenteNutrienteResultAsync(
            int id,
            FuenteNutrienteRequest fuente,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult<FuenteNutrienteResponse>.Fail(
                    "No se recibió un identificador válido para reactivar la fuente."));
        }

        return EnviarFuenteHistoricaAsync(
            HttpMethod.Put,
            $"api/fuente-nutriente/reactivar-con-elementos/{id}",
            fuente,
            "reactivar la fuente",
            "Fuente reactivada y actualizada correctamente.",
            cancellationToken);
    }

    public async Task<ApiResult<FuenteNutrienteResponse>>
        ReactivarFuenteNutrienteResultAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return ApiResult<FuenteNutrienteResponse>.Fail(
                "No se recibió un identificador válido para reactivar la fuente.");
        }

        try
        {
            using HttpResponseMessage response =
                await httpClient.PutAsync(
                    $"api/fuente-nutriente/reactivar/{id}",
                    null,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            return response.IsSuccessStatusCode
                ? ResultadoFuente(
                    contenido,
                    "Fuente reactivada correctamente.")
                : ErrorFuente(
                    response.StatusCode,
                    contenido,
                    "reactivar la fuente");
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                "reactivar la fuente");
        }
    }

    public async Task<ApiResult<bool>> UpdateFuenteNutrienteResultAsync(
        FuenteNutrienteRequest fuente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        if (fuente.FuenteNutrientesId is not > 0)
        {
            return ApiResult<bool>.Fail(
                "No se recibió un identificador de fuente válido.");
        }

        return await EnviarBooleanoAsync(
            HttpMethod.Put,
            $"api/fuente-nutriente/editar-con-elementos/{fuente.FuenteNutrientesId}",
            fuente,
            "actualizar la fuente",
            "Fuente actualizada correctamente.",
            cancellationToken);
    }

    public async Task<ApiResult<bool>> DeleteFuenteNutrienteResultAsync(
        FuenteNutrienteRequest fuente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        if (fuente.FuenteNutrientesId is not > 0)
        {
            return ApiResult<bool>.Fail(
                "No se recibió un identificador de fuente válido.");
        }

        try
        {
            using HttpResponseMessage response =
                await httpClient.DeleteAsync(
                    $"api/fuente-nutriente/eliminar/{fuente.FuenteNutrientesId}",
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(
                    true,
                    "Fuente eliminada correctamente.");
            }

            return ApiResult<bool>.Fail(
                await ApiServiceHelper.ReadResponseMessageAsync(
                    response,
                    ObtenerMensajeHttp(
                        response.StatusCode,
                        "eliminar la fuente"),
                    cancellationToken),
                (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<bool>(
                ex,
                cancellationToken,
                "eliminar la fuente");
        }
    }

    public async Task<ObservableCollection<FuenteNutrienteResponse>>
        GetFuenteNutrienteAsync()
    {
        ApiResult<ObservableCollection<FuenteNutrienteResponse>> resultado =
            await GetFuenteNutrienteResultAsync();

        return resultado.Success && resultado.Data != null
            ? resultado.Data
            : new ObservableCollection<FuenteNutrienteResponse>();
    }

    public async Task<ObservableCollection<FuenteNutrienteAporteTablaResponse>>
        GetAportesTablaAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<
                       ObservableCollection<FuenteNutrienteAporteTablaResponse>>(
                       "api/fuente-nutriente/aportes-tabla",
                       jsonOptions) ??
                   new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
        }
        catch
        {
            return new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
        }
    }

    public async Task<bool> CreateFuenteNutrienteAsync(
        FuenteNutrienteRequest fuente)
    {
        ApiResult<FuenteNutrienteResponse> resultado =
            await CreateFuenteNutrienteResultAsync(fuente);

        return resultado.Success &&
            resultado.Data?.FuenteNutrientesId > 0;
    }

    public async Task<FuenteNutrienteResponse?>
        CreateFuenteNutrienteConRespuestaAsync(
            FuenteNutrienteRequest fuente)
    {
        ApiResult<FuenteNutrienteResponse> resultado =
            await CreateFuenteNutrienteResultAsync(fuente);

        return resultado.Success
            ? resultado.Data
            : null;
    }

    public async Task<bool> UpdateFuenteNutrienteAsync(
        FuenteNutrienteRequest fuente) =>
        (await UpdateFuenteNutrienteResultAsync(fuente)).Success;

    public async Task<bool> DeleteFuenteNutrienteAsync(
        FuenteNutrienteRequest fuente) =>
        (await DeleteFuenteNutrienteResultAsync(fuente)).Success;

    public Task<bool> HabilitarEnmiendaCalcareaAsync(
        int id,
        HabilitarEnmiendaCalcareaRequest request) =>
        EjecutarSimpleAsync(
            HttpMethod.Post,
            $"api/fuente-nutriente/{id}/habilitar-enmienda-calcarea",
            request);

    public Task<bool> DeshabilitarEnmiendaCalcareaAsync(int id) =>
        EjecutarSimpleAsync(
            HttpMethod.Put,
            $"api/fuente-nutriente/deshabilitar-enmienda-calcarea/{id}");

    public Task<bool> HabilitarFertilizacionMixtaAsync(int id) =>
        EjecutarSimpleAsync(
            HttpMethod.Post,
            $"api/fuente-nutriente/habilitar-fertilizacion-mixta/{id}");

    public Task<bool> DeshabilitarFertilizacionMixtaAsync(int id) =>
        EjecutarSimpleAsync(
            HttpMethod.Put,
            $"api/fuente-nutriente/deshabilitar-fertilizacion-mixta/{id}");

    // =============================================================
    // API ADMINISTRATIVA MODERNA
    // =============================================================
    public async Task<ApiResult<FuenteNutrienteResponse>>
        CreateFuenteNutrienteAdminResultAsync(
            FuenteNutrienteAdministracionRequest fuente,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        try
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    RutaAdministrativa,
                    fuente,
                    jsonOptions,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            OperacionEnvelope<FuenteNutrienteResponse>? envelope =
                DeserializarOperacion(contenido);

            if (response.IsSuccessStatusCode)
            {
                return CrearResultadoAdminExitoso(
                    envelope,
                    "Fuente de nutriente creada correctamente.");
            }

            bool esInactivoCoincidente =
                response.StatusCode == HttpStatusCode.Conflict &&
                string.Equals(
                    envelope?.Code,
                    CodigoInactivoExistente,
                    StringComparison.OrdinalIgnoreCase) &&
                envelope?.Data?.FuenteNutrientesId is > 0;

            if (!esInactivoCoincidente)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        "No fue posible crear la fuente de nutriente."),
                    (int)response.StatusCode);
            }

            FuenteNutrienteResponse inactiva =
                envelope!.Data!;

            string? decision =
                await MostrarOpcionesInactivoAsync(
                    inactiva);

            if (decision == ReactivarOpcion)
            {
                return await EnviarYLeerFuenteAdminAsync(
                    HttpMethod.Put,
                    $"{RutaAdministrativa}/{inactiva.FuenteNutrientesId!.Value}/reactivar-con-datos",
                    fuente,
                    "No fue posible reactivar la fuente de nutriente.",
                    "Fuente de nutriente reactivada correctamente.",
                    cancellationToken);
            }

            if (decision == CrearOpcion)
            {
                return await EnviarYLeerFuenteAdminAsync(
                    HttpMethod.Post,
                    RutaAdministrativa +
                    "?crearNuevoSiExisteInactivo=true",
                    fuente,
                    "No fue posible crear la nueva fuente de nutriente.",
                    "Nueva fuente de nutriente creada correctamente.",
                    cancellationToken);
            }

            return ApiResult<FuenteNutrienteResponse>.Fail(
                "La creación fue cancelada.");
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                "crear la fuente de nutriente");
        }
    }

    public Task<ApiResult<FuenteNutrienteResponse>>
        UpdateFuenteNutrienteAdminResultAsync(
            int id,
            FuenteNutrienteAdministracionRequest fuente,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult<FuenteNutrienteResponse>.Fail(
                    "No se recibió un identificador válido para actualizar la fuente."));
        }

        return EnviarYLeerFuenteAdminAsync(
            HttpMethod.Put,
            $"{RutaAdministrativa}/{id}",
            fuente,
            "No fue posible actualizar la fuente de nutriente.",
            "Fuente de nutriente actualizada correctamente.",
            cancellationToken);
    }

    public async Task<ApiResult<bool>>
        DeleteFuenteNutrienteAdminResultAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return ApiResult<bool>.Fail(
                "No se recibió un identificador válido para eliminar la fuente.");
        }

        try
        {
            using HttpResponseMessage response =
                await httpClient.DeleteAsync(
                    $"{RutaAdministrativa}/{id}",
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Fail(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        "No fue posible eliminar la fuente de nutriente."),
                    (int)response.StatusCode);
            }

            OperacionEnvelope<FuenteNutrienteResponse>? envelope =
                DeserializarOperacion(contenido);

            return ApiResult<bool>.Ok(
                true,
                string.IsNullOrWhiteSpace(envelope?.Message)
                    ? "Fuente de nutriente eliminada correctamente."
                    : envelope!.Message);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<bool>(
                ex,
                cancellationToken,
                "eliminar la fuente de nutriente");
        }
    }

    public Task<ApiResult<FuenteNutrienteResponse>>
        ReactivarFuenteNutrienteAdminResultAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult<FuenteNutrienteResponse>.Fail(
                    "No se recibió un identificador válido para reactivar la fuente."));
        }

        return EnviarYLeerFuenteAdminSinCuerpoAsync(
            HttpMethod.Put,
            $"{RutaAdministrativa}/{id}/reactivar",
            "No fue posible reactivar la fuente de nutriente.",
            "Fuente de nutriente reactivada correctamente.",
            cancellationToken);
    }

    private async Task<ApiResult<FuenteNutrienteResponse>>
        EnviarYLeerFuenteAdminAsync(
            HttpMethod metodo,
            string ruta,
            FuenteNutrienteAdministracionRequest fuente,
            string mensajeError,
            string mensajeExito,
            CancellationToken cancellationToken)
    {
        try
        {
            using var request =
                new HttpRequestMessage(
                    metodo,
                    ruta)
                {
                    Content = JsonContent.Create(
                        fuente,
                        options: jsonOptions)
                };

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        mensajeError),
                    (int)response.StatusCode);
            }

            return CrearResultadoAdminExitoso(
                DeserializarOperacion(contenido),
                mensajeExito);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                "procesar la fuente de nutriente");
        }
    }

    private async Task<ApiResult<FuenteNutrienteResponse>>
        EnviarYLeerFuenteAdminSinCuerpoAsync(
            HttpMethod metodo,
            string ruta,
            string mensajeError,
            string mensajeExito,
            CancellationToken cancellationToken)
    {
        try
        {
            using var request =
                new HttpRequestMessage(
                    metodo,
                    ruta);

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        mensajeError),
                    (int)response.StatusCode);
            }

            return CrearResultadoAdminExitoso(
                DeserializarOperacion(contenido),
                mensajeExito);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                "reactivar la fuente de nutriente");
        }
    }

    private static ApiResult<FuenteNutrienteResponse>
        CrearResultadoAdminExitoso(
            OperacionEnvelope<FuenteNutrienteResponse>? envelope,
            string mensajeExito)
    {
        FuenteNutrienteResponse? data =
            envelope?.Data;

        if (data?.FuenteNutrientesId is not > 0)
        {
            return ApiResult<FuenteNutrienteResponse>.Fail(
                "La operación se procesó, pero el servidor no devolvió la fuente actualizada.");
        }

        return ApiResult<FuenteNutrienteResponse>.Ok(
            data,
            string.IsNullOrWhiteSpace(envelope?.Message)
                ? mensajeExito
                : envelope!.Message);
    }

    private static async Task<string?> MostrarOpcionesInactivoAsync(
        FuenteNutrienteResponse inactiva)
    {
        Page? pagina =
            Application.Current?.Windows.FirstOrDefault()?.Page ??
            Application.Current?.MainPage;

        if (pagina == null)
            return null;

        string titulo =
            string.IsNullOrWhiteSpace(inactiva.NombreNutriente)
                ? "Registro eliminado encontrado"
                : $"Registro eliminado: {inactiva.NombreNutriente}";

        return await pagina.DisplayActionSheet(
            titulo,
            "Cancelar",
            null,
            ReactivarOpcion,
            CrearOpcion);
    }

    // =============================================================
    // SOPORTE HISTÓRICO
    // =============================================================
    private async Task<ApiResult<FuenteNutrienteResponse>>
        ResolverInactivaHistoricaAsync(
            FuenteNutrienteRequest fuente,
            FuenteNutrienteResponse inactiva,
            CancellationToken cancellationToken)
    {
        Page? pagina =
            Application.Current?.Windows.FirstOrDefault()?.Page ??
            Application.Current?.MainPage;

        if (pagina == null)
        {
            return ApiResult<FuenteNutrienteResponse>.Fail(
                "Existe una fuente eliminada con ese nombre. Reactívela desde el listado de eliminados.",
                (int)HttpStatusCode.Conflict);
        }

        string opcion =
            await pagina.DisplayActionSheet(
                "Registro eliminado encontrado",
                "Cancelar",
                null,
                ReactivarOpcion,
                CrearOpcion);

        if (opcion == ReactivarOpcion)
        {
            return await ReactivarYActualizarFuenteNutrienteResultAsync(
                inactiva.FuenteNutrientesId!.Value,
                fuente,
                cancellationToken);
        }

        if (opcion == CrearOpcion)
        {
            return await CreateFuenteNutrienteConfirmadaResultAsync(
                fuente,
                cancellationToken);
        }

        return ApiResult<FuenteNutrienteResponse>.Fail(
            "No se realizaron cambios.",
            (int)HttpStatusCode.Conflict);
    }

    private async Task<ApiResult<FuenteNutrienteResponse>>
        EnviarFuenteHistoricaAsync(
            HttpMethod metodo,
            string ruta,
            FuenteNutrienteRequest fuente,
            string operacion,
            string mensajeExito,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fuente);

        try
        {
            using var request =
                new HttpRequestMessage(
                    metodo,
                    ruta)
                {
                    Content = JsonContent.Create(
                        fuente,
                        options: jsonOptions)
                };

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            return response.IsSuccessStatusCode
                ? ResultadoFuente(
                    contenido,
                    mensajeExito)
                : ErrorFuente(
                    response.StatusCode,
                    contenido,
                    operacion);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<FuenteNutrienteResponse>(
                ex,
                cancellationToken,
                operacion);
        }
    }

    private async Task<ApiResult<bool>> EnviarBooleanoAsync(
        HttpMethod metodo,
        string ruta,
        object contenido,
        string operacion,
        string mensajeExito,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request =
                new HttpRequestMessage(
                    metodo,
                    ruta)
                {
                    Content = JsonContent.Create(
                        contenido,
                        options: jsonOptions)
                };

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(
                    true,
                    mensajeExito);
            }

            return ApiResult<bool>.Fail(
                await ApiServiceHelper.ReadResponseMessageAsync(
                    response,
                    ObtenerMensajeHttp(
                        response.StatusCode,
                        operacion),
                    cancellationToken),
                (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<bool>(
                ex,
                cancellationToken,
                operacion);
        }
    }

    private async Task<ApiResult<ObservableCollection<FuenteNutrienteResponse>>>
        ObtenerColeccionAsync(
            string ruta,
            string operacion,
            CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await httpClient.GetAsync(
                    ruta,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    await ApiServiceHelper.ReadResponseMessageAsync(
                        response,
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            operacion),
                        cancellationToken),
                    (int)response.StatusCode);
            }

            ObservableCollection<FuenteNutrienteResponse>? data =
                await response.Content.ReadFromJsonAsync<
                    ObservableCollection<FuenteNutrienteResponse>>(
                    jsonOptions,
                    cancellationToken);

            return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Ok(
                data ??
                new ObservableCollection<FuenteNutrienteResponse>());
        }
        catch (Exception ex)
        {
            return ErrorExcepcion<ObservableCollection<FuenteNutrienteResponse>>(
                ex,
                cancellationToken,
                operacion);
        }
    }

    private async Task<bool> EjecutarSimpleAsync(
        HttpMethod metodo,
        string ruta,
        object? contenido = null)
    {
        try
        {
            using var request =
                new HttpRequestMessage(
                    metodo,
                    ruta);

            if (contenido != null)
            {
                request.Content =
                    JsonContent.Create(
                        contenido,
                        options: jsonOptions);
            }

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private ApiResult<FuenteNutrienteResponse> ResultadoFuente(
        string contenido,
        string mensaje)
    {
        FuenteNutrienteResponse? fuente =
            DeserializarFuente(contenido);

        return fuente?.FuenteNutrientesId > 0
            ? ApiResult<FuenteNutrienteResponse>.Ok(
                fuente,
                mensaje)
            : ApiResult<FuenteNutrienteResponse>.Fail(
                "La fuente fue procesada, pero la API no devolvió su identificador.");
    }

    private ApiResult<FuenteNutrienteResponse> ErrorFuente(
        HttpStatusCode codigo,
        string contenido,
        string operacion) =>
        ApiResult<FuenteNutrienteResponse>.Fail(
            ApiErrorMessageParser.Parse(
                codigo,
                contenido,
                ObtenerMensajeHttp(
                    codigo,
                    operacion)),
            (int)codigo);

    private FuenteNutrienteResponse? DeserializarFuente(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return null;

        ApiDataResponse<FuenteNutrienteResponse>? sobre =
            DeserializarSobre(contenido);

        return sobre?.Data ??
            JsonSerializer.Deserialize<FuenteNutrienteResponse>(
                contenido,
                jsonOptions);
    }

    private ApiDataResponse<FuenteNutrienteResponse>?
        DeserializarSobre(
            string contenido) =>
        string.IsNullOrWhiteSpace(contenido)
            ? null
            : JsonSerializer.Deserialize<
                ApiDataResponse<FuenteNutrienteResponse>>(
                contenido,
                jsonOptions);

    private OperacionEnvelope<FuenteNutrienteResponse>?
        DeserializarOperacion(
            string contenido) =>
        string.IsNullOrWhiteSpace(contenido)
            ? null
            : JsonSerializer.Deserialize<
                OperacionEnvelope<FuenteNutrienteResponse>>(
                contenido,
                jsonOptions);

    private static ApiResult<T> ErrorExcepcion<T>(
        Exception ex,
        CancellationToken cancellationToken,
        string operacion)
    {
        string mensaje = ex switch
        {
            TaskCanceledException when !cancellationToken.IsCancellationRequested =>
                "La solicitud tardó demasiado. Verifique su conexión.",

            OperationCanceledException =>
                "La operación fue cancelada.",

            HttpRequestException =>
                "No fue posible conectarse con el servidor.",

            JsonException =>
                "La respuesta del servidor no tiene el formato esperado.",

            _ =>
                $"Ocurrió un error inesperado al {operacion}."
        };

        return ApiResult<T>.Fail(mensaje);
    }

    private static string ObtenerMensajeHttp(
        HttpStatusCode codigo,
        string operacion) =>
        codigo switch
        {
            HttpStatusCode.BadRequest =>
                $"Datos inválidos al {operacion}.",

            HttpStatusCode.Unauthorized =>
                "La sesión no está autorizada.",

            HttpStatusCode.Forbidden =>
                $"No tiene permiso para {operacion}.",

            HttpStatusCode.NotFound =>
                "No se encontró el recurso solicitado.",

            HttpStatusCode.Conflict =>
                $"Existe un conflicto al {operacion}.",

            >= HttpStatusCode.InternalServerError =>
                "El servidor presentó un problema.",

            _ =>
                $"No fue posible {operacion}. Código HTTP: {(int)codigo}."
        };

    private sealed class ApiDataResponse<T>
    {
        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class OperacionEnvelope<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }
}
