using System;
using System.Collections.Generic;

namespace CONATRADEC.Services
{
    public class PermissionService
    {
        private static PermissionService _instance;
        public static PermissionService Instance => _instance ??= new PermissionService();

        private readonly Dictionary<string, UserPermissionDTO> _permissions =
            new Dictionary<string, UserPermissionDTO>();

        public event EventHandler? PermissionsChanged;

        private PermissionService() { }

        public void Load(IEnumerable<UserPermissionDTO>? permisos)
        {
            _permissions.Clear();

            if (permisos != null)
            {
                foreach (var p in permisos)
                {
                    string key = p.nombreInterfaz?.Trim().ToUpperInvariant() ?? "";

                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (!_permissions.ContainsKey(key))
                        _permissions.Add(key, p);
                }
            }

            PermissionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearPermissions()
        {
            _permissions.Clear();
            PermissionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public UserPermissionDTO Get(string interfaz)
        {
            string key = interfaz?.Trim().ToUpperInvariant() ?? "";

            if (_permissions.TryGetValue(key, out var p))
                return p;

            return new UserPermissionDTO
            {
                nombreInterfaz = key,
                leer = false,
                agregar = false,
                actualizar = false,
                eliminar = false
            };
        }

        public bool HasRead(string interfaz) => Get(interfaz).leer;
        public bool HasAdd(string interfaz) => Get(interfaz).agregar;
        public bool HasUpdate(string interfaz) => Get(interfaz).actualizar;
        public bool HasDelete(string interfaz) => Get(interfaz).eliminar;
    }

    public class UserPermissionDTO
    {
        public int interfazId { get; set; }
        public string nombreInterfaz { get; set; }
        public bool leer { get; set; }
        public bool agregar { get; set; }
        public bool actualizar { get; set; }
        public bool eliminar { get; set; }
    }
}