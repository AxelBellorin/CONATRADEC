using Microsoft.Maui.Controls;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Rutas del módulo de inspección fitosanitaria. Los nombres internos se
    /// conservan para no invalidar permisos ni datos de instalaciones previas.
    /// </summary>
    public static class DiagnosticoIARoutes
    {
        public const string InterfazSolicitud =
            "diagnosticoIASolicitudPage";
        public const string InterfazAnalizador =
            "diagnosticoIAAnalizadorPage";
        public const string InterfazAprobador =
            "diagnosticoIAAprobadorPage";
        public const string InterfazConfiguracion =
            "diagnosticoIAConfiguracionPage";
        public const string Interfaz = InterfazSolicitud;

        public const string ModoNuevaInspeccion = "nueva";
        public const string ModoMisInspecciones = "mis";
        public const string ModoDecisionesPendientes = "decisiones";
        public const string ModoHistorial = "historial";
        public const string ModoAnalizador = "analizador";
        public const string ModoAnalizadorRevisadas = "analizador-revisadas";
        public const string ModoAprobador = "aprobador";
        public const string ModoAprobadorRevisadas = "aprobador-revisadas";

        public static readonly string Pagina = Registrar(
            nameof(Views.DiagnosticoIAPage),
            typeof(Views.DiagnosticoIAPage));

        public static readonly string PaginaSolicitud = Registrar(
            nameof(Views.DiagnosticoIASolicitudPage),
            typeof(Views.DiagnosticoIASolicitudPage));

        public static readonly string PaginaResultado = Registrar(
            nameof(Views.DiagnosticoIAResultadoPage),
            typeof(Views.DiagnosticoIAResultadoPage));

        public static readonly string PaginaAnalizador = Registrar(
            nameof(Views.DiagnosticoIAAnalizadorPage),
            typeof(Views.DiagnosticoIAAnalizadorPage));

        public static readonly string PaginaAprobador = Registrar(
            nameof(Views.DiagnosticoIAAprobadorPage),
            typeof(Views.DiagnosticoIAAprobadorPage));

        public static readonly string PaginaBusquedaTerreno = Registrar(
            nameof(Views.TerrenoBusquedaIAPage),
            typeof(Views.TerrenoBusquedaIAPage));

        public static readonly string PaginaConfiguracion = Registrar(
            nameof(Views.DiagnosticoIAConfiguracionPage),
            typeof(Views.DiagnosticoIAConfiguracionPage));

        public static string RutaModulo => Pagina;

        public static void AsegurarRegistro()
        {
            _ = Pagina;
            _ = PaginaSolicitud;
            _ = PaginaResultado;
            _ = PaginaAnalizador;
            _ = PaginaAprobador;
            _ = PaginaBusquedaTerreno;
            _ = PaginaConfiguracion;
        }

        public static string CrearRutaSolicitud(string modo)
        {
            string normalizado = NormalizarModo(modo);
            return $"{PaginaSolicitud}?modo=" +
                   Uri.EscapeDataString(normalizado);
        }

        public static string CrearRutaResultado(
            int diagnosticoId,
            string? origen = null)
        {
            string origenNormalizado = NormalizarModo(origen);

            return $"{PaginaResultado}?diagnosticoId={diagnosticoId}" +
                   $"&origen={Uri.EscapeDataString(origenNormalizado)}";
        }

        /// <summary>
        /// Ruta de respaldo cuando Shell no conserva la página que abrió el
        /// resultado. Evita regresar a una bandeja diferente de la real.
        /// </summary>
        public static string CrearRutaRegresoResultado(string? origen)
        {
            string normalizado = NormalizarModo(origen);

            return normalizado switch
            {
                ModoAnalizador or ModoAnalizadorRevisadas => PaginaAnalizador,
                ModoAprobador or ModoAprobadorRevisadas => PaginaAprobador,
                _ => CrearRutaSolicitud(normalizado)
            };
        }

        public static string NormalizarModo(string? modo)
        {
            string valor = (modo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            return valor switch
            {
                ModoNuevaInspeccion => ModoNuevaInspeccion,
                ModoDecisionesPendientes => ModoDecisionesPendientes,
                ModoHistorial => ModoHistorial,
                ModoAnalizador => ModoAnalizador,
                ModoAnalizadorRevisadas => ModoAnalizadorRevisadas,
                ModoAprobador => ModoAprobador,
                ModoAprobadorRevisadas => ModoAprobadorRevisadas,
                _ => ModoMisInspecciones
            };
        }

        private static string Registrar(string ruta, Type tipoPagina)
        {
            try
            {
                Routing.RegisterRoute(ruta, tipoPagina);
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            return ruta;
        }
    }
}
