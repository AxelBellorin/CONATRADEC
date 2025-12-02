using CONATRADEC.Services;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    public class PermissionSwipeItem : SwipeItem
    {
        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(nameof(Interfaz), typeof(string), typeof(PermissionSwipeItem), null);

        public static readonly BindableProperty TipoProperty =
            BindableProperty.Create(nameof(Tipo), typeof(string), typeof(PermissionSwipeItem), null);

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
                return;

            var permiso = PermissionService.Instance.Get(Interfaz);

            bool visible = Tipo.ToLower() switch
            {
                "actualizar" => permiso.actualizar,
                "eliminar" => permiso.eliminar,
                _ => false
            };

            IsVisible = visible;
            if (!visible)
                Command = null;
        }
    }
}
