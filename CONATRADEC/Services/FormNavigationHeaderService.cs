using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Administra el encabezado superior agregado dinámicamente a las páginas.
    ///
    /// Funciones:
    /// 1. Mantiene deshabilitada la flecha automática de regreso; la navegación
    ///    queda a cargo de los botones propios de cada pantalla.
    /// 2. Muestra "Abandonar edición" durante todo el flujo de edición de un
    ///    análisis de suelo.
    /// 3. Retira el encabezado cuando la página ya no necesita controles,
    ///    evitando que una instancia reutilizada conserve botones.
    /// </summary>
    public static class FormNavigationHeaderService
    {
        private const string MarcaContenedor =
            "CONATRADEC_FORM_BACK_WRAPPER";

        private const string AutomationIdRegresar =
            "BotonRegresarFormulario";

        private const string AutomationIdAbandonarEdicion =
            "BotonAbandonarEdicionAnalisis";

        private static readonly string[]
            nombresComandosNavegacion =
            [
                "CancelCommand",
                "CancelarCommand",
                "RegresarCommand",
                "VolverCommand",
                "BackCommand"
            ];

        private static readonly string[]
            paginasProcesoAnalisis =
            [
                "EditarAnalisisGuardadoPage",
                "NuevoAnalisisFormPage",
                "ResultadoAnalisisSueloPage",
                "BalanceFormulaPage",
                "FertilizacionMixtaPage",
                "MultiCalculoPage"
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

                    /*
                     * Elimina únicamente el símbolo visual de regreso. Si el
                     * botón además contiene texto (por ejemplo "← Regresar"),
                     * conserva el botón, el comando y muestra "Regresar".
                     * Los botones cuyo único contenido era la flecha se ocultan.
                     */
                    QuitarFlechasVisuales(
                        pagina);

                    AsegurarEnPagina(pagina);
                });
        }

        private static void AsegurarEnPagina(
            ContentPage pagina)
        {
            /*
             * La flecha dinámica usada anteriormente por formularios queda
             * deshabilitada de forma global. Las páginas ya tienen navegación
             * propia y Shell también oculta su botón nativo de retroceso.
             */
            bool requiereBotonRegresar = false;

            bool requiereBotonAbandonar =
                AnalisisEdicionService
                    .Instance
                    .EsModoEdicion &&
                EsPaginaProcesoAnalisis(pagina);

            /*
             * Las páginas declaradas como ShellContent se reutilizan.
             * Si una pantalla ya no está editando, se restaura su contenido
             * original para que el botón no aparezca en un análisis nuevo.
             */
            if (!requiereBotonRegresar &&
                !requiereBotonAbandonar)
            {
                QuitarEncabezadoGlobalSiExiste(
                    pagina);

                return;
            }

            if (pagina.Content == null)
            {
                pagina.Loaded -= Pagina_Loaded;
                pagina.Loaded += Pagina_Loaded;
                return;
            }

            if (pagina.Content is Grid contenedorExistente &&
                string.Equals(
                    contenedorExistente.StyleId,
                    MarcaContenedor,
                    StringComparison.Ordinal))
            {
                ActualizarEncabezado(
                    pagina,
                    contenedorExistente,
                    requiereBotonRegresar,
                    requiereBotonAbandonar);

                return;
            }

            View contenidoOriginal =
                pagina.Content;

            Grid encabezado =
                CrearEncabezado(
                    pagina,
                    requiereBotonRegresar,
                    requiereBotonAbandonar);

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

            pagina.Content =
                contenedor;
        }

        private static void ActualizarEncabezado(
            ContentPage pagina,
            Grid contenedor,
            bool requiereBotonRegresar,
            bool requiereBotonAbandonar)
        {
            Grid? encabezadoAnterior =
                contenedor.Children
                    .OfType<Grid>()
                    .FirstOrDefault(
                        vista =>
                            Grid.GetRow(vista) == 0);

            Grid encabezadoNuevo =
                CrearEncabezado(
                    pagina,
                    requiereBotonRegresar,
                    requiereBotonAbandonar);

            if (encabezadoAnterior != null)
            {
                contenedor.Children.Remove(
                    encabezadoAnterior);
            }

            Grid.SetRow(
                encabezadoNuevo,
                0);

            contenedor.Children.Insert(
                0,
                encabezadoNuevo);
        }

        private static Grid CrearEncabezado(
            ContentPage pagina,
            bool mostrarRegresar,
            bool mostrarAbandonar)
        {
            var encabezado =
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(
                            GridLength.Auto),
                        new ColumnDefinition(
                            GridLength.Star),
                        new ColumnDefinition(
                            GridLength.Auto)
                    },
                    ColumnSpacing = 12,
                    Padding =
                        ObtenerPaddingEncabezado(),
                    BackgroundColor =
                        Colors.Transparent,
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Start
                };

            if (mostrarRegresar)
            {
                Button botonRegresar =
                    CrearBotonRegresar(pagina);

                Grid.SetColumn(
                    botonRegresar,
                    0);

                encabezado.Children.Add(
                    botonRegresar);
            }

            if (mostrarAbandonar)
            {
                Button botonAbandonar =
                    CrearBotonAbandonarEdicion(
                        pagina);

                Grid.SetColumn(
                    botonAbandonar,
                    2);

                encabezado.Children.Add(
                    botonAbandonar);
            }

            return encabezado;
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

            QuitarFlechasVisuales(
                pagina);

            AsegurarEnPagina(pagina);
        }

        /// <summary>
        /// Retira el símbolo ← de los botones visibles sin cambiar sus comandos.
        /// Se hace en tiempo de ejecución para cubrir también encabezados
        /// reutilizables y páginas antiguas que aún conservan ese texto en XAML.
        /// </summary>
        private static void QuitarFlechasVisuales(
            IVisualTreeElement elemento)
        {
            if (elemento is Button boton)
            {
                string original =
                    boton.Text ??
                    string.Empty;

                string limpio =
                    QuitarFlechaInicial(original);

                if (!string.Equals(
                        original,
                        limpio,
                        StringComparison.Ordinal))
                {
                    boton.Text = limpio;

                    if (string.IsNullOrWhiteSpace(limpio))
                        boton.IsVisible = false;
                }
            }

            foreach (
                IVisualTreeElement hijo
                in elemento.GetVisualChildren())
            {
                QuitarFlechasVisuales(hijo);
            }
        }

        private static string QuitarFlechaInicial(
            string texto)
        {
            string resultado =
                texto.TrimStart();

            while (resultado.StartsWith(
                       "←",
                       StringComparison.Ordinal))
            {
                resultado =
                    resultado[1..]
                        .TrimStart();
            }

            return resultado;
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
                        AutomationIdRegresar
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

        private static Button
            CrearBotonAbandonarEdicion(
                ContentPage pagina)
        {
            bool esEscritorio =
                DeviceInfo.Idiom ==
                DeviceIdiom.Desktop;

            var boton =
                new Button
                {
                    Text =
                        esEscritorio
                            ? "Abandonar edición"
                            : "Salir de edición",
                    HeightRequest = 46,
                    MinimumHeightRequest = 46,
                    Padding =
                        new Thickness(
                            16,
                            0),
                    CornerRadius = 13,
                    FontSize = 14,
                    FontAttributes =
                        FontAttributes.Bold,
                    BackgroundColor =
                        Color.FromArgb(
                            "#FEE2E2"),
                    TextColor =
                        Color.FromArgb(
                            "#B91C1C"),
                    BorderColor =
                        Color.FromArgb(
                            "#EF4444"),
                    BorderWidth = 1,
                    HorizontalOptions =
                        LayoutOptions.End,
                    VerticalOptions =
                        LayoutOptions.Center,
                    AutomationId =
                        AutomationIdAbandonarEdicion
                };

            SemanticProperties.SetDescription(
                boton,
                "Abandonar la edición del análisis sin guardar los cambios");

            boton.Clicked +=
                async (_, _) =>
                {
                    await EjecutarAbandonoEdicionAsync(
                        pagina,
                        boton);
                };

            return boton;
        }

        private static async Task
            EjecutarAbandonoEdicionAsync(
                ContentPage pagina,
                Button boton)
        {
            if (!boton.IsEnabled)
                return;

            boton.IsEnabled = false;

            try
            {
                bool confirmar =
                    await pagina.DisplayAlert(
                        "Abandonar edición",
                        "Se perderán los cambios que todavía no haya guardado. " +
                        "¿Desea salir de la edición de este análisis?",
                        "Salir de edición",
                        "Continuar editando");

                if (!confirmar)
                    return;

                /*
                 * Primero se elimina el contexto de edición. Los servicios de
                 * restauración que todavía estén esperando detectarán que la
                 * edición terminó y dejarán de modificar las pantallas.
                 */
                AnalisisEdicionService
                    .Instance
                    .Limpiar();

                SeleccionElementosComplementariosService
                    .Limpiar();

                /*
                 * La navegación cancela los trabajos asociados a la pantalla
                 * anterior. Después se elimina cualquier cálculo temporal para
                 * que no se reutilice al iniciar o editar otro análisis.
                 */
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(
                        "//MainPage",
                        false);
                }

                await CalculoAnalisisTemporalService
                    .Instance
                    .LimpiarTodoAsync();
            }
            catch (Exception ex)
            {
                await pagina.DisplayAlert(
                    "No fue posible salir",
                    "Ocurrió un error al abandonar la edición: " +
                    ex.Message,
                    "Aceptar");
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

        private static bool EsPaginaProcesoAnalisis(
            ContentPage pagina)
        {
            string nombre =
                pagina.GetType().Name;

            return paginasProcesoAnalisis.Any(
                paginaProceso =>
                    string.Equals(
                        paginaProceso,
                        nombre,
                        StringComparison.OrdinalIgnoreCase));
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
                    6);
            }

            if (DeviceInfo.Idiom ==
                DeviceIdiom.Tablet)
            {
                return new Thickness(
                    22,
                    14,
                    22,
                    5);
            }

            return new Thickness(
                14,
                12,
                14,
                5);
        }
    }
}
