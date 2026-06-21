using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;

namespace CONATRADEC.Controls
{
    public class PermissionSwipeItem : SwipeItem
    {
        private bool _suscrito;

        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(
                nameof(Interfaz),
                typeof(string),
                typeof(PermissionSwipeItem),
                null,
                propertyChanged: OnPermissionPropertyChanged);

        public static readonly BindableProperty TipoProperty =
            BindableProperty.Create(
                nameof(Tipo),
                typeof(string),
                typeof(PermissionSwipeItem),
                null,
                propertyChanged: OnPermissionPropertyChanged);

        public string Interfaz
        {
            get => (string)GetValue(InterfazProperty);
            set => SetValue(InterfazProperty, value);
        }

        public string Tipo
        {
            get => (string)GetValue(TipoProperty);
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

        private static void OnPermissionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PermissionSwipeItem item)
                item.AplicarPermiso();
        }

        private void SuscribirEventos()
        {
            if (_suscrito)
                return;

            PermissionService.Instance.PermissionsChanged += OnPermissionsChanged;
            _suscrito = true;
        }

        private void DesuscribirEventos()
        {
            if (!_suscrito)
                return;

            PermissionService.Instance.PermissionsChanged -= OnPermissionsChanged;
            _suscrito = false;
        }

        private void OnPermissionsChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(AplicarPermiso);
        }

        private void AplicarPermiso()
        {
            if (string.IsNullOrWhiteSpace(Interfaz) || string.IsNullOrWhiteSpace(Tipo))
            {
                IsVisible = false;
                return;
            }

            var permiso = PermissionService.Instance.Get(Interfaz);

            bool visible = Tipo.Trim().ToLowerInvariant() switch
            {
                "actualizar" => permiso.actualizar,
                "eliminar" => permiso.eliminar,
                "agregar" => permiso.agregar,
                "leer" => permiso.leer,
                _ => false
            };

            IsVisible = visible;

            if (!visible)
                Command = null;
        }
    }
}