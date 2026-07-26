namespace CONATRADEC.Services
{
    /// <summary>
    /// Código estable del permiso que habilita las funciones sin conexión.
    ///
    /// Leer = permite descargar y utilizar datos guardados, además de iniciar
    /// sesión sin conexión. Los demás permisos no se utilizan.
    /// </summary>
    public static class DatosSinConexionPermisos
    {
        public const string Interfaz =
            InterfazCodigos.DatosSinConexion;

        public static bool TienePermiso =>
            PermissionService.Instance.HasRead(
                Interfaz);
    }
}
