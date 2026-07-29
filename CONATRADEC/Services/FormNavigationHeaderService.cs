using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System.Reflection;
using System.Windows.Input;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Agrega una flecha fija de regreso en la parte superior de los
    /// formularios de CONATRADEC.
    ///
    /// Municipio utiliza una flecha propia enlazada directamente a
    /// CancelCommand, por lo que se excluye del encabezado global.
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

        /// <summary>
        /// Busca la página actualmente visible y agrega o corrige el encabezado
        /// de navegación cuando corresponde.
        /// </summary>
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
             * Municipio posee su propia flecha en el XAML.
             *
             * ShellContent conserva la instancia de la página. Por eso, si la
             * página ya había sido envuelta anteriormente por este servicio,
             * no basta con excluirla: también debemos retirar el encabezado
             * global que quedó montado en memoria.
             */
            if (EsFormularioMunicipio(pagina))
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

        /// <summary>
        /// Retira exclusivamente el contenedor creado por este servicio.
        /// No modifica el contenido original ni las demás páginas.
        /// </summary>
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

            /*
             * Primero se separa del contenedor anterior para que MAUI permita
             * asignarlo nuevamente como contenido principal de la página.
             */
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
                    {
                        /*
                         * Se utiliza el comando original del formulario.
                         * De esta forma se conservan limpieza, confirmaciones
                         * y rutas ya implementadas por cada ViewModel.
                         */
                        comando.Execute(null);
                    }

                    return;
                }

                /*
                 * Respaldo para formularios antiguos que todavía no poseen
                 * un comando público de cancelación.
                 */
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
                 * El botón de navegación no debe provocar el cierre de la app.
                 * Los formularios modernos utilizan su comando propio, por lo
                 * que este bloque únicamente cubre una ruta de respaldo.
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

        private static bool EsFormularioMunicipio(
            ContentPage pagina)
        {
            return string.Equals(
                pagina.GetType().Name,
                "municipioFormPage",
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
            /*
             * DeviceIdiom es una estructura y sus valores estáticos no pueden
             * utilizarse como patrones constantes dentro de un switch.
             * Las comparaciones directas funcionan correctamente en todos los
             * destinos de .NET MAUI.
             */
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