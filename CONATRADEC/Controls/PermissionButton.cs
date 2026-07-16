using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    public class PermissionButton : Button
    {
        private bool estaSuscrito;

        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(
                nameof(Interfaz),
                typeof(string),
                typeof(PermissionButton),
                string.Empty,
                propertyChanged: OnPermissionPropertyChanged);

        public static readonly BindableProperty TipoProperty =
            BindableProperty.Create(
                nameof(Tipo),
                typeof(string),
                typeof(PermissionButton),
                string.Empty,
                propertyChanged: OnPermissionPropertyChanged);

        public string Interfaz
        {
            get => GetValue(InterfazProperty) as string ?? string.Empty;
            set => SetValue(InterfazProperty, value);
        }

        public string Tipo
        {
            get => GetValue(TipoProperty) as string ?? string.Empty;
            set => SetValue(TipoProperty, value);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent == null)
            {
                DesuscribirEventos();
                return;
            }

            SuscribirEventos();
            AplicarPermiso();
        }

        private static void OnPermissionPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is PermissionButton button)
            {
                button.AplicarPermiso();
            }
        }

        private void SuscribirEventos()
        {
            if (estaSuscrito)
                return;

            PermissionService.Instance.PermissionsChanged +=
                OnPermissionsChanged;

            estaSuscrito = true;
        }

        private void DesuscribirEventos()
        {
            if (!estaSuscrito)
                return;

            PermissionService.Instance.PermissionsChanged -=
                OnPermissionsChanged;

            estaSuscrito = false;
        }

        private void OnPermissionsChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(AplicarPermiso);
        }

        private void AplicarPermiso()
        {
            if (string.IsNullOrWhiteSpace(Interfaz) ||
                string.IsNullOrWhiteSpace(Tipo))
            {
                IsVisible = false;
                IsEnabled = false;
                InputTransparent = true;
                return;
            }

            UserPermissionDTO permiso =
                PermissionService.Instance.Get(Interfaz);

            bool puede = Tipo.Trim().ToLowerInvariant() switch
            {
                "leer" => permiso.leer,
                "agregar" => permiso.agregar,
                "actualizar" => permiso.actualizar,
                "eliminar" => permiso.eliminar,
                _ => false
            };

            IsVisible = puede;
            IsEnabled = puede;
            InputTransparent = !puede;

            // No se elimina Command. De esa manera, el botón puede volver
            // a habilitarse correctamente cuando se carguen los permisos.
        }
    }
}
