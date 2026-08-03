using System;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza las reglas de visibilidad y navegación de las secciones
    /// principales de la aplicación.
    ///
    /// Configuración es una sección contenedora: se muestra cuando el usuario
    /// puede consultar al menos una de las interfaces incluidas en ella.
    /// </summary>
    public static class NavigationPermissionService
    {
        public const string GrupoConfiguracion = "Configuracion";

        private static readonly string[] InterfacesConfiguracion =
        {
            InterfazCodigos.Usuarios,
            InterfazCodigos.Roles,
            InterfazCodigos.MatrizPermisos,
            InterfazCodigos.Paises,
            InterfazCodigos.Terrenos,
            InterfazCodigos.TiposCultivo,
            InterfazCodigos.TiposAnalisisSuelo,
            InterfazCodigos.ElementosQuimicos,
            InterfazCodigos.FuentesNutrientes,
            InterfazCodigos.ExtraccionNutrientes,
            InterfazCodigos.RangosNutrientes,
            InterfazCodigos.CategoriasPublicacion,
            DiagnosticoIARoutes.Interfaz,
            InterfazCodigos.Bitacora
        };

        public static bool PuedeVerOpcion(
            string? interfaz,
            string? grupoPermisos)
        {
            if (!string.IsNullOrWhiteSpace(grupoPermisos))
                return PuedeVerGrupo(grupoPermisos);

            return !string.IsNullOrWhiteSpace(interfaz) &&
                   PermissionService.Instance.HasRead(interfaz);
        }

        public static bool PuedeVerGrupo(string? grupoPermisos)
        {
            if (string.Equals(
                    grupoPermisos,
                    GrupoConfiguracion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PuedeVerConfiguracion();
            }

            return false;
        }

        public static bool PuedeVerConfiguracion()
        {
            foreach (string interfaz in InterfacesConfiguracion)
            {
                if (PermissionService.Instance.HasRead(interfaz))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Devuelve la primera sección principal que el usuario puede abrir.
        /// Se utiliza después del login y cuando se intenta entrar a una
        /// sección no autorizada.
        /// </summary>
        public static string ObtenerRutaInicialPermitida()
        {
            if (PermissionService.Instance.HasRead(
                    InterfazCodigos.AnalisisSuelo))
            {
                return AppRoutes.Principal;
            }

            if (PermissionService.Instance.HasRead(
                    InterfazCodigos.AlbumFotos))
            {
                return AppRoutes.AlbumFotos;
            }

            if (PermissionService.Instance.HasRead(
                    InterfazCodigos.Noticias))
            {
                return AppRoutes.NoticiasPrincipal;
            }

            if (PuedeVerConfiguracion())
                return AppRoutes.Configuracion;

            return AppRoutes.SinPermisos;
        }
    }
}
