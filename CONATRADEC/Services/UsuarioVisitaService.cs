using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado temporal del módulo Usuarios durante una única visita.
    ///
    /// Los listados grandes no se guardan aquí: permanecen únicamente en el
    /// ViewModel de la página actual. Este servicio conserva solo catálogos
    /// pequeños reutilizables por los formularios y un cambio pendiente de
    /// aplicar al regresar al listado.
    /// </summary>
    public static class UsuarioVisitaService
    {
        private const string Modulo = "usuarios";
        private const string ClaveRoles = "catalogo:roles";
        private const string ClavePaises = "catalogo:paises";
        private const string ClaveCambio = "cambio:pendiente";
        private const string ClaveRecargarListado = "listado:recargar";

        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(Modulo);

        public static void IniciarNuevaVisita() =>
            InterfazVisitaCacheService.IniciarNuevaVisita(Modulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(Modulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(Modulo);

        /// <summary>
        /// Fuerza que la próxima apertura de Crear/Editar consulte nuevamente
        /// los catálogos, sin finalizar la visita ni tocar la página cargada.
        /// </summary>
        public static void InvalidarCatalogos()
        {
            InterfazVisitaCacheService.Eliminar(Modulo, ClaveRoles);
            InterfazVisitaCacheService.Eliminar(Modulo, ClavePaises);

            // Departamentos y municipios usan claves dependientes. Al ser una
            // caché pequeña por visita, limpiar todos los datos es más seguro,
            // preservando únicamente un cambio pendiente si existiera.
            UsuarioVisitaCambio? cambio = ConsumirCambio();
            bool recargarListado = ConsumirRecargaListado();

            InterfazVisitaCacheService.LimpiarDatos(Modulo);

            if (cambio != null)
                RegistrarCambio(cambio);

            if (recargarListado)
                MarcarListadoParaRecargar();
        }

        public static bool IntentarObtenerRoles(
            out List<RolResponse>? roles) =>
            InterfazVisitaCacheService.IntentarObtener(
                Modulo,
                ClaveRoles,
                out roles);

        public static void GuardarRoles(IEnumerable<RolResponse> roles) =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRoles,
                roles.ToList());

        public static bool IntentarObtenerPaises(
            out List<PaisResponse>? paises) =>
            InterfazVisitaCacheService.IntentarObtener(
                Modulo,
                ClavePaises,
                out paises);

        public static void GuardarPaises(IEnumerable<PaisResponse> paises) =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClavePaises,
                paises.ToList());

        public static bool IntentarObtenerDepartamentos(
            int paisId,
            out List<DepartamentoResponse>? departamentos) =>
            InterfazVisitaCacheService.IntentarObtener(
                Modulo,
                ClaveDepartamentos(paisId),
                out departamentos);

        public static void GuardarDepartamentos(
            int paisId,
            IEnumerable<DepartamentoResponse> departamentos) =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveDepartamentos(paisId),
                departamentos.ToList());

        public static bool IntentarObtenerMunicipios(
            int departamentoId,
            out List<MunicipioResponse>? municipios) =>
            InterfazVisitaCacheService.IntentarObtener(
                Modulo,
                ClaveMunicipios(departamentoId),
                out municipios);

        public static void GuardarMunicipios(
            int departamentoId,
            IEnumerable<MunicipioResponse> municipios) =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveMunicipios(departamentoId),
                municipios.ToList());

        public static void RegistrarCambio(
            UsuarioVisitaCambio cambio) =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveCambio,
                cambio);

        public static UsuarioVisitaCambio? ConsumirCambio()
        {
            return InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveCambio,
                out UsuarioVisitaCambio? cambio)
                    ? cambio
                    : null;
        }

        /// <summary>
        /// Marca que el listado activo debe consultar nuevamente su página
        /// actual. Se utiliza para cambios realizados desde Usuarios inactivos,
        /// donde el registro reactivado puede alterar la composición global de
        /// una página ordenada y no se puede ubicar con certeza solo en memoria.
        /// </summary>
        public static void MarcarListadoParaRecargar() =>
            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveRecargarListado,
                true);

        public static bool ConsumirRecargaListado() =>
            InterfazVisitaCacheService.IntentarConsumir(
                Modulo,
                ClaveRecargarListado,
                out bool recargar) &&
            recargar;

        private static string ClaveDepartamentos(int paisId) =>
            $"catalogo:departamentos:{paisId}";

        private static string ClaveMunicipios(int departamentoId) =>
            $"catalogo:municipios:{departamentoId}";
    }
}
