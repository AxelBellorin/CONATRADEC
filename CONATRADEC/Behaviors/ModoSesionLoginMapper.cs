using CONATRADEC.Controls;
using Microsoft.Maui.Handlers;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Inserta el selector global dentro del login existente sin reemplazar su
    /// XAML, animaciones, biometría ni comportamiento responsive.
    /// </summary>
    public static class ModoSesionLoginMapper
    {
        private static int registrado;

        private static readonly BindableProperty ConfiguradoProperty =
            BindableProperty.CreateAttached(
                "ModoSesionConfigurado",
                typeof(bool),
                typeof(ModoSesionLoginMapper),
                false);

        public static void Register()
        {
            if (Interlocked.Exchange(ref registrado, 1) == 1)
                return;

            PageHandler.Mapper.AppendToMapping(
                nameof(ModoSesionLoginMapper),
                static (_, view) =>
                {
                    if (view is not ContentPage page ||
                        !string.Equals(
                            page.GetType().Name,
                            "loginPage",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    page.Loaded -= OnLoaded;
                    page.Loaded += OnLoaded;

                    page.Dispatcher.Dispatch(() => Configurar(page));
                });
        }

        private static void OnLoaded(object? sender, EventArgs e)
        {
            if (sender is not ContentPage page)
                return;

            Configurar(page);
        }

        private static void Configurar(ContentPage page)
        {
            if ((bool)page.GetValue(ConfiguradoProperty))
                return;

            VerticalStackLayout? contenedor =
                page.FindByName<VerticalStackLayout>(
                    "LoginCardContent");

            if (contenedor == null)
                return;

            if (contenedor.Children
                .OfType<ModoSesionLoginView>()
                .Any())
            {
                page.SetValue(ConfiguradoProperty, true);
                return;
            }

            /*
             * Se coloca antes de la zona de recordar/biometría y del botón.
             * En caso de cambios futuros en el XAML, el índice se limita para
             * no producir errores.
             */
            int index = Math.Clamp(
                contenedor.Children.Count - 3,
                1,
                contenedor.Children.Count);

            contenedor.Children.Insert(
                index,
                new ModoSesionLoginView());

            page.SetValue(ConfiguradoProperty, true);
        }
    }
}
