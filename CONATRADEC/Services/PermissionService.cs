using System;
using System.Collections.Generic;
namespace CONATRADEC.Services
{
    public sealed class PermissionService
    {
        private static PermissionService? instance;

        public static PermissionService Instance =>
            instance ??= new PermissionService();

        private readonly Dictionary<string, UserPermissionDTO>
            permissions =
                new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? PermissionsChanged;

        private PermissionService()
        {
        }

        /// <summary>
        /// Carga los permisos recibidos durante el inicio de sesión.
        ///
        /// También consolida permisos históricos de formularios o pantallas
        /// internas en la página principal correspondiente. Esto permite
        /// actualizar primero el frontend o primero la base de datos sin
        /// dejar al usuario temporalmente sin acceso.
        /// </summary>
        public void Load(
            IEnumerable<UserPermissionDTO>? permisos)
        {
            permissions.Clear();

            if (permisos != null)
            {
                foreach (UserPermissionDTO permiso in permisos)
                {
                    string codigoCanonico =
                        InterfazCodigos.Normalizar(
                            permiso.nombreInterfaz);

                    if (string.IsNullOrWhiteSpace(
                            codigoCanonico))
                    {
                        continue;
                    }

                    if (!permissions.TryGetValue(
                            codigoCanonico,
                            out UserPermissionDTO? existente))
                    {
                        permissions[codigoCanonico] =
                            new UserPermissionDTO
                            {
                                interfazId =
                                    permiso.interfazId,
                                nombreInterfaz =
                                    codigoCanonico,
                                nombreAmigableInterfaz =
                                    permiso
                                        .nombreAmigableInterfaz ??
                                    string.Empty,
                                leer = permiso.leer,
                                agregar = permiso.agregar,
                                actualizar =
                                    permiso.actualizar,
                                eliminar = permiso.eliminar
                            };

                        continue;
                    }

                    /*
                     * Cuando existen registros históricos duplicados,
                     * se conserva el permiso más amplio.
                     */
                    existente.leer =
                        existente.leer ||
                        permiso.leer;

                    existente.agregar =
                        existente.agregar ||
                        permiso.agregar;

                    existente.actualizar =
                        existente.actualizar ||
                        permiso.actualizar;

                    existente.eliminar =
                        existente.eliminar ||
                        permiso.eliminar;

                    if (string.IsNullOrWhiteSpace(
                            existente
                                .nombreAmigableInterfaz) &&
                        !string.IsNullOrWhiteSpace(
                            permiso
                                .nombreAmigableInterfaz))
                    {
                        existente.nombreAmigableInterfaz =
                            permiso.nombreAmigableInterfaz;
                    }
                }
            }

            PermissionsChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        public void ClearPermissions()
        {
            permissions.Clear();

            PermissionsChanged?.Invoke(
                this,
                EventArgs.Empty);
        }

        public UserPermissionDTO Get(string? interfaz)
        {
            string codigoCanonico =
                InterfazCodigos.Normalizar(interfaz);

            if (permissions.TryGetValue(
                    codigoCanonico,
                    out UserPermissionDTO? permiso))
            {
                return permiso;
            }

            return new UserPermissionDTO
            {
                nombreInterfaz = codigoCanonico,
                leer = false,
                agregar = false,
                actualizar = false,
                eliminar = false
            };
        }

        public bool HasRead(string interfaz) =>
            Get(interfaz).leer;

        public bool HasAdd(string interfaz) =>
            Get(interfaz).agregar;

        public bool HasUpdate(string interfaz) =>
            Get(interfaz).actualizar;

        public bool HasDelete(string interfaz) =>
            Get(interfaz).eliminar;
    }

    public sealed class UserPermissionDTO
    {
        public int interfazId { get; set; }

        /// <summary>
        /// Código interno utilizado para validar.
        /// </summary>
        public string nombreInterfaz { get; set; } =
            string.Empty;

        /// <summary>
        /// Texto visible opcional.
        /// La autorización nunca depende de este valor.
        /// </summary>
        public string? nombreAmigableInterfaz { get; set; }

        public bool leer { get; set; }
        public bool agregar { get; set; }
        public bool actualizar { get; set; }
        public bool eliminar { get; set; }
    }
}
