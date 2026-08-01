using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System.Reflection;
using System.Windows.Input;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Agrega una flecha fija de regreso en la parte superior de los
    /// formularios antiguos de CONATRADEC que todavía no poseen un
    /// encabezado propio.
    ///
    /// Municipio y Propietario utilizan navegación definida directamente
    /// en su XAML, por lo que se excluyen del encabezado global.
    /// </summary>
    public static class FormNavigationHeaderService
    {
        private const string MarcaContenedor =
            "CONATRADEC_FORM_BACK_WRAPPER";

        private static readonly string[]
            nombresComandosNavegacion =
            [
                "CancelCommand",
                "CancelarCommand",
                "RegresarCommand",
                "VolverCommand",
                "BackCommand"
            ];

        public static void AsegurarEnPaginaActual()
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    if (Shell.Current?.CurrentPage
                        is not ContentPage pagina)
                    {
                        return;
                    }

                    AsegurarEnPagina(pagina);
                });
        }

        private static void AsegurarEnPagina(
            ContentPage pagina)
        {
            /*
             * Estas páginas ya poseen navegación propia.
             * Si una instancia fue envuelta anteriormente, también se retira
             * el encabezado global que haya quedado guardado en memoria.
             */
            if (UsaNavegacionPropia(pagina))
            {
                QuitarEncabezadoGlobalSiExiste(pagina);
                return;
            }

            if (!EsFormulario(pagina))
                return;

            if (pagina.Content == null)
            {
                pagina.Loaded += Pagina_Loaded;
                return;
            }

            if (string.Equals(
                    pagina.Content.StyleId,
                    MarcaContenedor,
                    StringComparison.Ordinal))
            {
                return;
            }

            View contenidoOriginal =
                pagina.Content;

            var botonRegresar =
                CrearBotonRegresar(pagina);

            var encabezado =
                new Grid
                {
                    Padding = ObtenerPaddingEncabezado(),
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Start
                };

            encabezado.Children.Add(
                botonRegresar);

            var contenedor =
                new Grid
                {
                    StyleId = MarcaContenedor,
                    RowDefinitions =
                    {
                        new RowDefinition(
                            GridLength.Auto),
                        new RowDefinition(
                            GridLength.Star)
                    },
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Fill
                };

            Grid.SetRow(
                encabezado,
                0);

            Grid.SetRow(
                contenidoOriginal,
                1);

            contenedor.Children.Add(
                encabezado);

            contenedor.Children.Add(
                contenidoOriginal);

            pagina.Content = contenedor;
        }

        private static void QuitarEncabezadoGlobalSiExiste(
            ContentPage pagina)
        {
            if (pagina.Content is not Grid contenedor ||
                !string.Equals(
                    contenedor.StyleId,
                    MarcaContenedor,
                    StringComparison.Ordinal))
            {
                return;
            }

            View? contenidoOriginal =
                contenedor.Children
                    .OfType<View>()
                    .FirstOrDefault(
                        vista =>
                            Grid.GetRow(vista) == 1);

            if (contenidoOriginal == null)
                return;

            contenedor.Children.Remove(
                contenidoOriginal);

            pagina.Content =
                contenidoOriginal;
        }

        private static void Pagina_Loaded(
            object? sender,
            EventArgs e)
        {
            if (sender is not ContentPage pagina)
                return;

            pagina.Loaded -= Pagina_Loaded;
            AsegurarEnPagina(pagina);
        }

        private static Button CrearBotonRegresar(
            ContentPage pagina)
        {
            var boton =
                new Button
                {
                    Text = "←",
                    WidthRequest = 48,
                    HeightRequest = 48,
                    MinimumWidthRequest = 48,
                    MinimumHeightRequest = 48,
                    Padding = 0,
                    CornerRadius = 14,
                    FontSize = 23,
                    FontAttributes =
                        FontAttributes.Bold,
                    BackgroundColor =
                        Color.FromArgb(
                            "#F3F5F4"),
                    TextColor =
                        Color.FromArgb(
                            "#263238"),
                    HorizontalOptions =
                        LayoutOptions.Start,
                    VerticalOptions =
                        LayoutOptions.Center,
                    AutomationId =
                        "BotonRegresarFormulario"
                };

            SemanticProperties.SetDescription(
                boton,
                "Regresar al listado anterior");

            boton.Clicked +=
                async (_, _) =>
                {
                    await EjecutarRegresoAsync(
                        pagina,
                        boton);
                };

            return boton;
        }

        private static async Task EjecutarRegresoAsync(
            ContentPage pagina,
            Button boton)
        {
            if (!boton.IsEnabled)
                return;

            boton.IsEnabled = false;

            try
            {
                object? contexto =
                    pagina.BindingContext;

                if (EstaOcupado(contexto))
                    return;

                ICommand? comando =
                    ObtenerComandoNavegacion(
                        contexto);

                if (comando != null)
                {
                    if (comando.CanExecute(null))
                        comando.Execute(null);

                    return;
                }

                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(
                        "..",
                        false);
                }
            }
            catch
            {
                /*
                 * La navegación de respaldo nunca debe cerrar la aplicación.
                 */
            }
            finally
            {
                if (ReferenceEquals(
                        Shell.Current?.CurrentPage,
                        pagina))
                {
                    boton.IsEnabled = true;
                }
            }
        }

        private static ICommand?
            ObtenerComandoNavegacion(
                object? contexto)
        {
            if (contexto == null)
                return null;

            Type tipo =
                contexto.GetType();

            foreach (string nombre
                     in nombresComandosNavegacion)
            {
                PropertyInfo? propiedad =
                    tipo.GetProperty(
                        nombre,
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (propiedad?.GetValue(
                        contexto)
                    is ICommand comando)
                {
                    return comando;
                }
            }

            return null;
        }

        private static bool EstaOcupado(
            object? contexto)
        {
            if (contexto == null)
                return false;

            PropertyInfo? propiedad =
                contexto
                    .GetType()
                    .GetProperty(
                        "IsBusy",
                        BindingFlags.Instance |
                        BindingFlags.Public);

            return propiedad?.GetValue(
                       contexto)
                   is bool ocupado &&
                   ocupado;
        }

        private static bool UsaNavegacionPropia(
            ContentPage pagina)
        {
            string nombre =
                pagina.GetType().Name;

            return
                string.Equals(
                    nombre,
                    "municipioFormPage",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    nombre,
                    "propietarioFormPage",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsFormulario(
            ContentPage pagina)
        {
            string nombre =
                pagina.GetType().Name;

            return
                nombre.EndsWith(
                    "FormPage",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    nombre,
                    "EditarAnalisisGuardadoPage",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static Thickness
            ObtenerPaddingEncabezado()
        {
            if (DeviceInfo.Idiom ==
                DeviceIdiom.Desktop)
            {
                return new Thickness(
                    24,
                    16,
                    24,
                    2);
            }

            if (DeviceInfo.Idiom ==
                DeviceIdiom.Tablet)
            {
                return new Thickness(
                    22,
                    14,
                    22,
                    2);
            }

            return new Thickness(
                14,
                12,
                14,
                2);
        }
    }
}
