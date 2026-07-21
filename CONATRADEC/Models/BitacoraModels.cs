using System.Text.Json;

namespace CONATRADEC.Models
{
    public class BitacoraListadoItem
    {
        public Guid BitacoraId { get; set; }
        public DateTime FechaHoraUtc { get; set; }
        public int? UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string RolNombre { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string MetodoHttp { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string PaginaOrigen { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int CodigoEstado { get; set; }
        public bool Exitoso { get; set; }
        public long DuracionMs { get; set; }
        public int CantidadCambios { get; set; }

        public DateTime FechaHoraLocal =>
            DateTime.SpecifyKind(FechaHoraUtc, DateTimeKind.Utc)
                .ToLocalTime();

        public string FechaHoraTexto =>
            FechaHoraLocal.ToString("dd/MM/yyyy hh:mm:ss tt");

        public string UsuarioTexto =>
            string.IsNullOrWhiteSpace(UsuarioNombre)
                ? "Usuario no identificado"
                : UsuarioNombre;

        public string RolTexto =>
            string.IsNullOrWhiteSpace(RolNombre)
                ? "Sin rol informado"
                : RolNombre;

        public string EstadoTexto =>
            Exitoso
                ? $"Correcto · HTTP {CodigoEstado}"
                : $"Error · HTTP {CodigoEstado}";

        public string DuracionTexto => $"{DuracionMs:N0} ms";

        public string CambiosTexto => CantidadCambios switch
        {
            0 => "Sin cambios de datos",
            1 => "1 cambio de datos",
            _ => $"{CantidadCambios} cambios de datos"
        };
    }

    public sealed class BitacoraDetalleItem : BitacoraListadoItem
    {
        public string Parametros { get; set; } = string.Empty;
        public string DireccionIp { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;
        public string Plataforma { get; set; } = string.Empty;
        public string VersionApp { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public List<BitacoraCambioItem> Cambios { get; set; } = new();

        public bool TieneParametros => !string.IsNullOrWhiteSpace(Parametros) &&
                                       Parametros != "{}";
        public bool TieneError => !string.IsNullOrWhiteSpace(Error);
        public bool TieneCambios => Cambios.Count > 0;
        public bool SinCambios => !TieneCambios;
        public string ParametrosFormateados =>
            BitacoraJsonHelper.Formatear(Parametros);
    }

    public sealed class BitacoraCambioItem
    {
        public long BitacoraDetalleId { get; set; }
        public DateTime FechaHoraUtc { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public string EntidadId { get; set; } = string.Empty;
        public string Operacion { get; set; } = string.Empty;
        public string ValoresAnteriores { get; set; } = string.Empty;
        public string ValoresNuevos { get; set; } = string.Empty;
        public string PropiedadesModificadas { get; set; } = string.Empty;

        public string ValoresAnterioresFormateados =>
            BitacoraJsonHelper.Formatear(ValoresAnteriores);
        public string ValoresNuevosFormateados =>
            BitacoraJsonHelper.Formatear(ValoresNuevos);
        public string PropiedadesFormateadas =>
            BitacoraJsonHelper.Formatear(PropiedadesModificadas);
        public bool TieneAnteriores =>
            !string.IsNullOrWhiteSpace(ValoresAnteriores) &&
            ValoresAnteriores != "{}";
        public bool TieneNuevos =>
            !string.IsNullOrWhiteSpace(ValoresNuevos) &&
            ValoresNuevos != "{}";
    }

    public sealed class BitacoraPaginadaResponse
    {
        public List<BitacoraListadoItem> Items { get; set; } = new();
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
    }

    public sealed class BitacoraUsuarioFiltro
    {
        public int? UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public override string ToString() => Nombre;
    }

    public sealed class BitacoraCatalogosResponse
    {
        public List<string> Acciones { get; set; } = new();
        public List<string> Modulos { get; set; } = new();
        public List<BitacoraUsuarioFiltro> Usuarios { get; set; } = new();
    }

    internal static class BitacoraJsonHelper
    {
        public static string Formatear(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "Sin información";

            try
            {
                using JsonDocument documento = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(
                    documento.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            }
            catch
            {
                return json;
            }
        }
    }
}
