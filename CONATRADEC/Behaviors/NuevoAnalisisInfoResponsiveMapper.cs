using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Corrige el ancho del texto informativo ubicado antes de los botones
    /// del formulario de análisis.
    ///
    /// La adaptación se basa en el ancho real disponible y no en
    /// DeviceInfo.Idiom. Esto permite que funcione correctamente en:
    ///
    /// - Android.
    /// - Teléfonos.
    /// - Ventanas estrechas de Windows.
    /// - Modo dividido.
    /// - Cambios de orientación.
    /// </summary>
    internal static class NuevoAnalisisInfoResponsiveMapper
    {
        private const string MapperKey =
            "CONATRADEC.NuevoAnalisisInfoResponsive";

        private const string InicioMensaje =
            "Al enviar el análisis";

        /*
         * A partir de este ancho el diseño tiene suficiente espacio para
         * utilizar la medición natural del Label.
         */
        private const double AnchoMaximoModoEstrecho =
            900d;

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
                    if (view is not NuevoAnalisisFormPage pagina)
                        return;

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
                    static paginaActual =>
                        new EstadoPagina(paginaActual));

            estado.Adjuntar();
        }

        private sealed class EstadoPagina
        {
            private readonly NuevoAnalisisFormPage pagina;

            private bool adjuntado;

            private Label? mensaje;

            private HorizontalStackLayout?
                contenedorMensaje;

            private ConfiguracionUnidadesFormularioCoordinator?
                coordinadorUnidades;

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
                pagina.SizeChanged += Pagina_SizeChanged;

                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_Loaded(
                object? sender,
                EventArgs e)
            {
                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_Appearing(
                object? sender,
                EventArgs e)
            {
                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_SizeChanged(
                object? sender,
                EventArgs e)
            {
                Ajustar();
            }

            private void ContenedorMensaje_SizeChanged(
                object? sender,
                EventArgs e)
            {
                Ajustar();
            }

            private void PrepararCoordinadorUnidadesConRetraso()
            {
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(40),
                    PrepararCoordinadorUnidades);

                /*
                 * La página inicializa catálogos y, en modo edición,
                 * restaura los valores guardados de forma asincrónica.
                 * Una segunda aplicación garantiza que la unidad histórica
                 * quede seleccionada después de terminar ese proceso.
                 */
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(450),
                    PrepararCoordinadorUnidades);
            }

            private void PrepararCoordinadorUnidades()
            {
                if (pagina.BindingContext
                    is not NuevoAnalisisFormEdicionViewModel
                        viewModel)
                {
                    return;
                }

                coordinadorUnidades ??=
                    new ConfiguracionUnidadesFormularioCoordinator(
                        viewModel);

                coordinadorUnidades.Adjuntar();

                _ = coordinadorUnidades
                    .CargarYAplicarAsync();
            }

            private void AjustarConRetraso()
            {
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(80),
                    Ajustar);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(250),
                    Ajustar);
            }

            private void Ajustar()
            {
                mensaje ??= BuscarMensaje(pagina);

                if (mensaje == null)
                    return;

                AdjuntarContenedor();

                mensaje.LineBreakMode =
                    LineBreakMode.WordWrap;

                /*
                 * Se permiten varias líneas para que el mensaje nunca quede
                 * truncado en ventanas estrechas.
                 */
                mensaje.MaxLines = 8;

                mensaje.HorizontalOptions =
                    LayoutOptions.FillAndExpand;

                mensaje.VerticalOptions =
                    LayoutOptions.Center;

                double anchoPagina =
                    pagina.Width;

                if (anchoPagina <= 0)
                    return;

                if (anchoPagina > AnchoMaximoModoEstrecho)
                {
                    RestablecerMedicionNatural();
                    return;
                }

                double anchoContenedor =
                    contenedorMensaje?.Width ?? 0;

                /*
                 * Si el contenedor todavía no terminó de medirse se usa el
                 * ancho de la página como respaldo. Posteriormente,
                 * SizeChanged vuelve a ejecutar este cálculo.
                 */
                double anchoBase =
                    anchoContenedor > 0
                        ? anchoContenedor
                        : anchoPagina - 36;

                double anchoIcono =
                    ObtenerAnchoIcono();

                double separacion =
                    contenedorMensaje?.Spacing ?? 12;

                double anchoDisponible =
                    Math.Max(
                        160,
                        anchoBase -
                        anchoIcono -
                        separacion -
                        2);

                mensaje.MinimumWidthRequest = 0;
                mensaje.WidthRequest =
                    anchoDisponible;

                mensaje.MaximumWidthRequest =
                    anchoDisponible;

                mensaje.InvalidateMeasure();
            }

            private void AdjuntarContenedor()
            {
                if (contenedorMensaje != null)
                    return;

                contenedorMensaje =
                    mensaje?.Parent as
                        HorizontalStackLayout;

                if (contenedorMensaje == null)
                    return;

                contenedorMensaje.SizeChanged +=
                    ContenedorMensaje_SizeChanged;

                contenedorMensaje.HorizontalOptions =
                    LayoutOptions.Fill;

                contenedorMensaje.VerticalOptions =
                    LayoutOptions.Center;
            }

            private double ObtenerAnchoIcono()
            {
                if (contenedorMensaje == null ||
                    contenedorMensaje.Children.Count == 0)
                {
                    return 34;
                }

                /*
                 * Layout.Children expone elementos como IView.
                 * Se convierte de forma segura a View porque necesitamos
                 * consultar WidthRequest, propiedad propia del control visual.
                 */
                if (contenedorMensaje.Children[0] is not View icono)
                    return 34;

                if (icono.Width > 0)
                    return icono.Width;

                if (icono.WidthRequest > 0)
                    return icono.WidthRequest;

                return 34;
            }

            private void RestablecerMedicionNatural()
            {
                if (mensaje == null)
                    return;

                mensaje.WidthRequest = -1;
                mensaje.MaximumWidthRequest =
                    double.PositiveInfinity;

                mensaje.InvalidateMeasure();
            }

            private static Label? BuscarMensaje(
                IVisualTreeElement elemento)
            {
                if (elemento is Label label &&
                    label.Text?.TrimStart()
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
