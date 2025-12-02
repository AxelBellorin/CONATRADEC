using CONATRADEC.Services;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    public class PermissionMenuItem : VerticalStackLayout
    {
        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(nameof(Interfaz), typeof(string), typeof(PermissionMenuItem), null);

        public string Interfaz
        {
            get => (string)GetValue(InterfazProperty);
            set => SetValue(InterfazProperty, value);
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent == null)
                return;

            var permiso = PermissionService.Instance.Get(Interfaz);

            bool visible = permiso.leer;

            IsVisible = visible;

            if (!visible)
                GestureRecognizers.Clear();
        }
    }
}
