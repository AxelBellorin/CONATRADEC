using CONATRADEC.Models;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente del paginador numerado auditado de Solicitudes/Historial.
    /// En línea utiliza el endpoint nuevo. En una sesión offline reconstruye
    /// la página desde la bandeja local existente, sin permitir tráfico real.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaNumeradaApiService
    {
        private const string RutaOnline =
            "api/revision-fitosanitaria/bandeja-pagina";
        private const string RutaOffline =
            "api/inspecciones-fitosanitarias/bandeja-paginada";

        private readonly HttpClient client;
        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public InspeccionFitosanitariaBandejaNumeradaApiService()
            : this(ApiClientService.Client)
        {
        }

        public InspeccionFitosanitariaBandejaNumeradaApiService(HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2> ObtenerAsync(
            InspeccionFitosanitariaBandejaFiltroV2 filtro,
            int pagina,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            pagina = Math.Max(1, pagina);
            filtro.TamanoPagina = Math.Clamp(filtro.TamanoPagina, 10, 50);

            return ModoSesionService.EsOffline
                ? ObtenerOfflineAsync(filtro, pagina, cancellationToken)
                : ObtenerOnlineAsync(filtro, pagina, cancellationToken);
        }

        private async Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2>
            ObtenerOnlineAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                int pagina,
                CancellationToken cancellationToken)
        {
            string ruta = ConstruirRutaOnline(filtro, pagina);
            return await ObtenerEnvelopeAsync<
                InspeccionFitosanitariaBandejaPaginaNumeradaV2>(
                    ruta,
                    cancellationToken);
        }

        private async Task<InspeccionFitosanitariaBandejaPaginaNumeradaV2>
            ObtenerOfflineAsync(
                InspeccionFitosanitariaBandejaFiltroV2 filtro,
                int pagina,
                CancellationToken cancellationToken)
        {
            string modo = DiagnosticoIARoutes.NormalizarModo(filtro.Modo);

            // Offline solo existe la cola del técnico. Historial y decisiones
            // requieren información central y permanecen vacíos por diseño.
            if (modo is DiagnosticoIARoutes.ModoHistorial or
                DiagnosticoIARoutes.ModoDecisionesPendientes)
            {
                return new InspeccionFitosanitariaBandejaPaginaNumeradaV2
                {
                    Pagina = 1,
                    TamanoPagina = filtro.TamanoPagina,
                    Total = 0,
                    TotalPaginas = 0
                };
            }

            var acumulados = new List<InspeccionFitosanitariaBandejaItemV2>();
            DateTime? cursorFecha = null;
            int? cursorId = null;

            do
            {
                string ruta = ConstruirRutaOffline(
                    filtro,
                    cursorFecha,
                    cursorId,
                    50);

                InspeccionFitosanitariaBandejaPaginaV2 respuesta =
                    await ObtenerEnvelopeAsync<InspeccionFitosanitariaBandejaPaginaV2>(
                        ruta,
                        cancellationToken);

                if (respuesta.Items is { Count: > 0 })
                    acumulados.AddRange(respuesta.Items);

                if (!respuesta.HayMas ||
                    !respuesta.SiguienteFechaUtc.HasValue ||
                    !respuesta.SiguienteId.HasValue)
                {
                    break;
                }

                cursorFecha = respuesta.SiguienteFechaUtc;
                cursorId = respuesta.SiguienteId;
            }
            while (true);

            IEnumerable<InspeccionFitosanitariaBandejaItemV2> filtrados =
                acumulados;

            if (!string.IsNullOrWhiteSpace(filtro.Propietario))
            {
                filtrados = filtrados.Where(item =>
                    Contiene(item.Propietario, filtro.Propietario));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Departamento))
            {
                filtrados = filtrados.Where(item =>
                    Contiene(item.Departamento, filtro.Departamento));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
            {
                string estado = NormalizarCodigo(filtro.Estado);
                filtrados = filtrados.Where(item =>
                    string.Equals(
                        NormalizarCodigo(item.Estado),
                        estado,
                        StringComparison.OrdinalIgnoreCase));
            }

            List<InspeccionFitosanitariaBandejaItemV2> lista = filtrados
                .OrderByDescending(item => item.FechaRegistroSistemaUtc)
                .ThenByDescending(item => item.InspeccionId)
                .ToList();

            int total = lista.Count;
            int totalPaginas = total == 0
                ? 0
                : (int)Math.Ceiling(total / (double)filtro.TamanoPagina);
            int paginaNormalizada = totalPaginas == 0
                ? 1
                : Math.Min(pagina, totalPaginas);

            List<InspeccionFitosanitariaBandejaItemV2> items = lista
                .Skip((paginaNormalizada - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina)
                .ToList();

            return new InspeccionFitosanitariaBandejaPaginaNumeradaV2
            {
                Items = items,
                Pagina = paginaNormalizada,
                TamanoPagina = filtro.TamanoPagina,
                Total = total,
                TotalPaginas = totalPaginas
            };
        }

        private async Task<T> ObtenerEnvelopeAsync<T>(
            string ruta,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await client.GetAsync(ruta, cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            ApiEnvelopeV2<T>? envelope = null;
            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<ApiEnvelopeV2<T>>(
                        contenido,
                        jsonOptions);
                }
                catch (JsonException)
                {
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                string mensaje = envelope?.Message ?? ExtraerMensaje(contenido);
                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(mensaje)
                        ? "No fue posible cargar la bandeja de inspecciones."
                        : mensaje);
            }

            if (envelope is not null)
            {
                object? data = envelope.Data;

                if (data is not null)
                    return (T)data;
            }

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una respuesta incompleta para la bandeja.");
        }

        private static string ConstruirRutaOnline(
            InspeccionFitosanitariaBandejaFiltroV2 filtro,
            int pagina)
        {
            var parametros = CrearParametrosBase(filtro);
            parametros.Add($"pagina={pagina}");
            parametros.Add($"tamanoPagina={filtro.TamanoPagina}");
            return $"{RutaOnline}?{string.Join('&', parametros)}";
        }

        private static string ConstruirRutaOffline(
            InspeccionFitosanitariaBandejaFiltroV2 filtro,
            DateTime? ultimaFechaUtc,
            int? ultimoId,
            int tamanoPagina)
        {
            var parametros = CrearParametrosBase(filtro);
            parametros.Add($"tamanoPagina={tamanoPagina}");

            if (ultimaFechaUtc.HasValue && ultimoId.HasValue)
            {
                parametros.Add(
                    $"ultimaFechaUtc={Uri.EscapeDataString(ultimaFechaUtc.Value.ToUniversalTime().ToString("O"))}");
                parametros.Add($"ultimoId={ultimoId.Value}");
            }

            return $"{RutaOffline}?{string.Join('&', parametros)}";
        }

        private static List<string> CrearParametrosBase(
            InspeccionFitosanitariaBandejaFiltroV2 filtro)
        {
            var parametros = new List<string>
            {
                $"modo={Uri.EscapeDataString(DiagnosticoIARoutes.NormalizarModo(filtro.Modo))}",
                $"desfaseHorarioMinutos={filtro.DesfaseHorarioMinutos}"
            };

            AgregarTexto(parametros, "buscar", filtro.Buscar);
            AgregarTexto(parametros, "propietario", filtro.Propietario);
            AgregarTexto(parametros, "departamento", filtro.Departamento);
            AgregarTexto(parametros, "tipoFotografia", filtro.TipoFotografia);
            AgregarTexto(parametros, "estado", filtro.Estado);

            if (filtro.TecnicoId is > 0)
                parametros.Add($"tecnicoId={filtro.TecnicoId.Value}");

            if (filtro.FechaDesde.HasValue)
            {
                parametros.Add(
                    $"fechaDesde={Uri.EscapeDataString(filtro.FechaDesde.Value.Date.ToString("yyyy-MM-dd"))}");
            }

            if (filtro.FechaHasta.HasValue)
            {
                parametros.Add(
                    $"fechaHasta={Uri.EscapeDataString(filtro.FechaHasta.Value.Date.ToString("yyyy-MM-dd"))}");
            }

            return parametros;
        }

        private static void AgregarTexto(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                parametros.Add(
                    $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
            }
        }

        private static bool Contiene(string? origen, string? valor) =>
            !string.IsNullOrWhiteSpace(origen) &&
            !string.IsNullOrWhiteSpace(valor) &&
            origen.Contains(valor.Trim(), StringComparison.OrdinalIgnoreCase);

        private static string NormalizarCodigo(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim().ToUpperInvariant().Replace(' ', '_');

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
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }
    }
}
