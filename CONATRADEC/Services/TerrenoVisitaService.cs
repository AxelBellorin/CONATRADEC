using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado temporal exclusivo de una visita al módulo Terrenos.
    ///
    /// Una visita inicia al entrar a Terrenos desde otro módulo y termina al
    /// salir hacia Configuración u otra interfaz principal. Crear, editar, ver,
    /// mapa, galería y selectores continúan perteneciendo a la misma visita.
    /// </summary>
    public static class TerrenoVisitaService
    {
        private const string ClaveModulo = "terrenoPage";
        private const string ClavePaises = "Paises";
        private const string ClaveRecargarListado = "RecargarListado";

        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(ClaveModulo);

        public static void IniciarNuevaVisita() =>
            InterfazVisitaCacheService.IniciarNuevaVisita(ClaveModulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(ClaveModulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(ClaveModulo);

        public static void MarcarListadoParaRecargar()
        {
            if (!EstaActiva)
                return;

            InterfazVisitaCacheService.Guardar(
                ClaveModulo,
                ClaveRecargarListado,
                true);
        }

        public static bool ConsumirRecargaListado()
        {
            if (!EstaActiva)
                return false;

            return InterfazVisitaCacheService.IntentarConsumir(
                       ClaveModulo,
                       ClaveRecargarListado,
                       out bool recargar) &&
                   recargar;
        }

        public static bool IntentarObtenerPaises(
            out List<PaisResponse> paises) =>
            IntentarObtenerLista(ClavePaises, out paises);

        public static void GuardarPaises(
            IEnumerable<PaisResponse> paises) =>
            GuardarLista(ClavePaises, paises);

        public static bool IntentarObtenerDepartamentos(
            int paisId,
            out List<DepartamentoResponse> departamentos) =>
            IntentarObtenerLista(
                ClaveDepartamentos(paisId),
                out departamentos);

        public static void GuardarDepartamentos(
            int paisId,
            IEnumerable<DepartamentoResponse> departamentos)
        {
            if (paisId <= 0)
                return;

            GuardarLista(
                ClaveDepartamentos(paisId),
                departamentos);
        }

        public static bool IntentarObtenerMunicipios(
            int departamentoId,
            out List<MunicipioResponse> municipios) =>
            IntentarObtenerLista(
                ClaveMunicipios(departamentoId),
                out municipios);

        public static void GuardarMunicipios(
            int departamentoId,
            IEnumerable<MunicipioResponse> municipios)
        {
            if (departamentoId <= 0)
                return;

            GuardarLista(
                ClaveMunicipios(departamentoId),
                municipios);
        }

        public static void InvalidarUbicacion()
        {
            if (!EstaActiva)
                return;

            InterfazVisitaCacheService.Eliminar(
                ClaveModulo,
                ClavePaises);
        }

        private static string ClaveDepartamentos(int paisId) =>
            $"Departamentos/PaisId={paisId}";

        private static string ClaveMunicipios(int departamentoId) =>
            $"Municipios/DepartamentoId={departamentoId}";

        private static bool IntentarObtenerLista<T>(
            string clave,
            out List<T> items)
        {
            items = new List<T>();

            if (!EstaActiva ||
                !InterfazVisitaCacheService.IntentarObtener(
                    ClaveModulo,
                    clave,
                    out List<T> cache))
            {
                return false;
            }

            items = new List<T>(cache);
            return true;
        }

        private static void GuardarLista<T>(
            string clave,
            IEnumerable<T> items)
        {
            if (!EstaActiva)
                return;

            InterfazVisitaCacheService.Guardar(
                ClaveModulo,
                clave,
                items?.ToList() ?? new List<T>());
        }
    }
}
