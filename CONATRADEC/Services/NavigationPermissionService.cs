using System;
using System.Collections.Generic;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza las reglas de visibilidad y navegación de las secciones
    /// principales de la aplicación.
    /// </summary>
    public static class NavigationPermissionService
    {
        public const string GrupoConfiguracion = "Configuracion";

        /*
         * En móvil Configuración también funciona como acceso a herramientas
         * generales y cierre de sesión, por lo que debe permanecer visible
         * aunque el rol no tenga permisos sobre catálogos administrativos.
         *
         * Esto no modifica la regla de permisos del menú lateral de Windows,
         * que continúa utilizando GrupoConfiguracion.
         */
        public const string GrupoConfiguracionMovil =
            "ConfiguracionMovil";

        public const string GrupoInspeccionFitosanitaria =
            "InspeccionFitosanitaria";

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
            DiagnosticoIARoutes.InterfazConfiguracion,
            InterfazCodigos.Bitacora
        };

        private static readonly string[] InterfacesInspeccionFitosanitaria =
        {
            DiagnosticoIARoutes.InterfazSolicitud,
            DiagnosticoIARoutes.InterfazAnalizador,
            DiagnosticoIARoutes.InterfazAprobador
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
                    GrupoConfiguracionMovil,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    grupoPermisos,
                    GrupoConfiguracion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PuedeVerConfiguracion();
            }

            if (string.Equals(
                    grupoPermisos,
                    GrupoInspeccionFitosanitaria,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PuedeVerInspeccionFitosanitaria();
            }

            return false;
        }

        public static bool PuedeVerConfiguracion() =>
            TieneLecturaEnAlguna(InterfacesConfiguracion);

        public static bool PuedeVerInspeccionFitosanitaria()
        {
            DiagnosticoIARoutes.AsegurarRegistro();
            return TieneLecturaEnAlguna(InterfacesInspeccionFitosanitaria);
        }

        private static bool TieneLecturaEnAlguna(
            IEnumerable<string> interfaces)
        {
            foreach (string interfaz in interfaces)
            {
                if (PermissionService.Instance.HasRead(interfaz))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Devuelve la primera sección principal que el usuario puede abrir.
        /// </summary>
        public static string ObtenerRutaInicialPermitida()
        {
            if (PermissionService.Instance.HasRead(
                    InterfazCodigos.AnalisisSuelo))
            {
                return AppRoutes.Principal;
            }

            if (PuedeVerInspeccionFitosanitaria())
                return AppRoutes.InspeccionFitosanitaria;

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
