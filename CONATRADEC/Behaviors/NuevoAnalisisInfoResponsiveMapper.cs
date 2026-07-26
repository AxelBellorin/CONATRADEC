using CONATRADEC.Views;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Devices;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Corrige el ancho del texto informativo ubicado antes de los botones
    /// del formulario de análisis. En teléfonos evita que el mensaje quede
    /// cortado dentro del HorizontalStackLayout existente.
    ///
    /// No cambia el XAML ni la apariencia validada en Windows y tablet.
    /// </summary>
    internal static class
        NuevoAnalisisInfoResponsiveMapper
    {
        private const string MapperKey =
            "CONATRADEC.NuevoAnalisisInfoResponsive";

        private const string InicioMensaje =
            "Al enviar el análisis";

        private static readonly ConditionalWeakTable<
            NuevoAnalisisFormPage,
            EstadoPagina> Estados = new();

        private static bool registrado;

        internal static void Register()
        {
            if (registrado)
                return;

            registrado = true;

            PageHandler.Mapper.AppendToMapping(
                MapperKey,
                static (_, view) =>
                {
                    if (view
                        is not NuevoAnalisisFormPage
                            pagina)
                    {
                        return;
                    }

                    MainThread.BeginInvokeOnMainThread(
                        () => Adjuntar(pagina));
                });
        }

        private static void Adjuntar(
            NuevoAnalisisFormPage pagina)
        {
            EstadoPagina estado =
                Estados.GetValue(
                    pagina,
                    static actual =>
                        new EstadoPagina(actual));

            estado.Adjuntar();
        }

        private sealed class EstadoPagina
        {
            private readonly
                NuevoAnalisisFormPage pagina;

            private bool adjuntado;
            private Label? mensaje;

            public EstadoPagina(
                NuevoAnalisisFormPage pagina)
            {
                this.pagina = pagina;
            }

            public void Adjuntar()
            {
                if (adjuntado)
                    return;

                adjuntado = true;

                pagina.Loaded += Pagina_Loaded;
                pagina.Appearing += Pagina_Appearing;
                pagina.SizeChanged +=
                    Pagina_SizeChanged;

                Ajustar();
            }

            private void Pagina_Loaded(
                object? sender,
                EventArgs e)
            {
                Ajustar();
            }

            private void Pagina_Appearing(
                object? sender,
                EventArgs e)
            {
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(80),
                    Ajustar);
            }

            private void Pagina_SizeChanged(
                object? sender,
                EventArgs e)
            {
                Ajustar();
            }

            private void Ajustar()
            {
                mensaje ??=
                    BuscarMensaje(pagina);

                if (mensaje == null)
                    return;

                mensaje.LineBreakMode =
                    LineBreakMode.WordWrap;

                mensaje.MaxLines = 8;

                mensaje.HorizontalOptions =
                    LayoutOptions.FillAndExpand;

                mensaje.VerticalOptions =
                    LayoutOptions.Center;

                /*
                 * En pantallas estrechas, HorizontalStackLayout no entrega
                 * automáticamente al Label el espacio restante. Se calcula
                 * un ancho seguro descontando márgenes, padding, icono y
                 * separación. En tablet y Windows se conserva un máximo
                 * razonable sin modificar el diseño general.
                 */
                double anchoPagina =
                    pagina.Width;

                if (anchoPagina <= 0)
                    return;

                /*
                 * La corrección de ancho se limita a teléfonos.
                 * Tablet y Windows conservan exactamente la medición
                 * que ya tenía el diseño validado.
                 */
                if (DeviceInfo.Idiom != DeviceIdiom.Phone)
                {
                    mensaje.WidthRequest = -1;
                    mensaje.MaximumWidthRequest =
                        double.PositiveInfinity;
                    return;
                }

                double anchoDisponible =
                    Math.Max(
                        180,
                        anchoPagina - 130);

                mensaje.WidthRequest =
                    anchoDisponible;

                mensaje.MaximumWidthRequest =
                    anchoDisponible;
            }

            private static Label?
                BuscarMensaje(
                    IVisualTreeElement elemento)
            {
                if (elemento is Label label &&
                    label.Text?
                        .TrimStart()
                        .StartsWith(
                            InicioMensaje,
                            StringComparison
                                .OrdinalIgnoreCase)
                        == true)
                {
                    return label;
                }

                foreach (
                    IVisualTreeElement hijo
                    in elemento.GetVisualChildren())
                {
                    Label? encontrado =
                        BuscarMensaje(hijo);

                    if (encontrado != null)
                        return encontrado;
                }

                return null;
            }
        }
    }
}
