using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class ConfiguracionViewModel : GlobalService
    {
        public bool MostrarSeguridadUsuarios =>
            TieneLectura(
                "userPage",
                "rolPage",
                "matrizPermisosPage");

        public bool MostrarUbicacionFincas =>
            TieneLectura(
                "paisPage",
                "terrenoPage");

        public bool MostrarCatalogosAgronomicos =>
            TieneLectura(
                "tipoCultivoPage",
                "tipoAnalisisSueloPage",
                "elementoQuimicoPage",
                "fuenteNutrientePage");

        public bool MostrarParametrosNutricionales =>
            TieneLectura(
                "extraccionNutrientePage",
                "rangoNutrientePage");

        public bool MostrarContenidoComunicacion =>
            TieneLectura("categoriaPublicacionPage");

        public bool MostrarSinOpciones =>
            !MostrarSeguridadUsuarios &&
            !MostrarUbicacionFincas &&
            !MostrarCatalogosAgronomicos &&
            !MostrarParametrosNutricionales &&
            !MostrarContenidoComunicacion;

        public void ActualizarVisibilidad()
        {
            OnPropertyChanged(nameof(MostrarSeguridadUsuarios));
            OnPropertyChanged(nameof(MostrarUbicacionFincas));
            OnPropertyChanged(nameof(MostrarCatalogosAgronomicos));
            OnPropertyChanged(nameof(MostrarParametrosNutricionales));
            OnPropertyChanged(nameof(MostrarContenidoComunicacion));
            OnPropertyChanged(nameof(MostrarSinOpciones));
        }

        private static bool TieneLectura(params string[] interfaces)
        {
            return interfaces.Any(
                PermissionService.Instance.HasRead);
        }
    }
}
