using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;

namespace CONATRADEC.Controls
{
    public class PermissionMenuItem : VerticalStackLayout
    {
        private bool _suscrito;

        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(
                nameof(Interfaz),
                typeof(string),
                typeof(PermissionMenuItem),
                null,
                propertyChanged: OnInterfazChanged);

        public string Interfaz
        {
            get => (string)GetValue(InterfazProperty);
            set => SetValue(InterfazProperty, value);
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

        private static void OnInterfazChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PermissionMenuItem item)
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
            if (string.IsNullOrWhiteSpace(Interfaz))
            {
                IsVisible = false;
                return;
            }

            var permiso = PermissionService.Instance.Get(Interfaz);

            bool visible = permiso.leer;

            IsVisible = visible;
            InputTransparent = !visible;
        }
    }
}