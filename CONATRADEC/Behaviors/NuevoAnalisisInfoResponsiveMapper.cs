using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Graphics;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

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

        private static readonly ConditionalWeakTable<
            MainPage,
            EstadoPaginaPrincipal> EstadosPaginaPrincipal =
                new();

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
                    if (view is MainPage paginaPrincipal)
                    {
                        MainThread.BeginInvokeOnMainThread(
                            () =>
                                AdjuntarPaginaPrincipal(
                                    paginaPrincipal));

                        return;
                    }

                    if (view is NuevoAnalisisFormPage
                        paginaAnalisis)
                    {
                        MainThread.BeginInvokeOnMainThread(
                            () => Adjuntar(
                                paginaAnalisis));
                    }
                });
        }

        private static void AdjuntarPaginaPrincipal(
            MainPage pagina)
        {
            EstadoPaginaPrincipal estado =
                EstadosPaginaPrincipal.GetValue(
                    pagina,
                    static paginaActual =>
                        new EstadoPaginaPrincipal(
                            paginaActual));

            estado.Adjuntar();
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

        /// <summary>
        /// Muestra el indicador en MainPage antes de comenzar la navegación.
        ///
        /// Esto es necesario porque en la primera apertura MAUI todavía debe
        /// construir NuevoAnalisisFormPage. Un indicador creado dentro de la
        /// página destino no puede dibujarse antes de que esa construcción
        /// finalice.
        /// </summary>
        private sealed class EstadoPaginaPrincipal
        {
            private readonly MainPage pagina;

            private bool adjuntado;

            private bool navegando;

            private ImageButton? botonNuevoAnalisis;

            private ICommand? comandoOriginal;

            private Command? comandoIntermediario;

            private Grid? overlayNavegacion;

            public EstadoPaginaPrincipal(
                MainPage pagina)
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
                pagina.Disappearing += Pagina_Disappearing;
                pagina.BindingContextChanged +=
                    Pagina_BindingContextChanged;

                CrearOverlay();
                PrepararBotonConRetraso();
            }

            private void Pagina_Loaded(
                object? sender,
                EventArgs e)
            {
                CrearOverlay();
                PrepararBotonConRetraso();
            }

            private void Pagina_Appearing(
                object? sender,
                EventArgs e)
            {
                navegando = false;
                OcultarOverlay();
                PrepararBotonConRetraso();
            }

            private void Pagina_Disappearing(
                object? sender,
                EventArgs e)
            {
                navegando = false;
                OcultarOverlay();
            }

            private void Pagina_BindingContextChanged(
                object? sender,
                EventArgs e)
            {
                if (botonNuevoAnalisis != null &&
                    ReferenceEquals(
                        botonNuevoAnalisis.Command,
                        comandoIntermediario))
                {
                    botonNuevoAnalisis.Command =
                        comandoOriginal;
                }

                RestaurarEventosComandoAnterior();
                botonNuevoAnalisis = null;
                comandoOriginal = null;
                comandoIntermediario = null;

                PrepararBotonConRetraso();
            }

            private void PrepararBotonConRetraso()
            {
                pagina.Dispatcher.Dispatch(
                    PrepararBoton);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(100),
                    PrepararBoton);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(350),
                    PrepararBoton);
            }

            private void PrepararBoton()
            {
                if (pagina.BindingContext
                    is not MainPageViewModel viewModel)
                {
                    return;
                }

                ICommand comandoEsperado =
                    viewModel.NuevoAnalisisCommand;

                ImageButton? boton =
                    BuscarBotonNuevoAnalisis(
                        pagina,
                        comandoEsperado);

                if (boton == null)
                    return;

                /*
                 * Si el binding volvió a colocar el comando original después
                 * de un cambio de contexto, se envuelve nuevamente.
                 */
                if (ReferenceEquals(
                        boton.Command,
                        comandoIntermediario))
                {
                    botonNuevoAnalisis = boton;
                    return;
                }

                RestaurarEventosComandoAnterior();

                botonNuevoAnalisis = boton;
                comandoOriginal =
                    boton.Command ??
                    comandoEsperado;

                comandoIntermediario =
                    new Command(
                        async () =>
                            await EjecutarNuevoAnalisisAsync(),
                        () =>
                            !navegando &&
                            comandoOriginal?
                                .CanExecute(null)
                            == true);

                comandoOriginal.CanExecuteChanged +=
                    ComandoOriginal_CanExecuteChanged;

                boton.Command =
                    comandoIntermediario;
            }

            private async Task
                EjecutarNuevoAnalisisAsync()
            {
                if (navegando ||
                    comandoOriginal == null ||
                    !comandoOriginal.CanExecute(null))
                {
                    return;
                }

                navegando = true;

                comandoIntermediario?
                    .ChangeCanExecute();

                MostrarOverlay();

                /*
                 * Se libera el hilo visual antes de ejecutar GoToAsync.
                 * Task.Delay permite que Windows y Android dibujen al menos
                 * un cuadro con la rueda antes de construir la página destino.
                 */
                await Task.Yield();
                await Task.Delay(90);

                if (!navegando)
                    return;

                comandoOriginal.Execute(null);

                /*
                 * Respaldo: si la navegación falla, el usuario recupera la
                 * pantalla en lugar de quedar bloqueado indefinidamente.
                 */
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromSeconds(30),
                    () =>
                    {
                        if (!navegando)
                            return;

                        navegando = false;
                        OcultarOverlay();

                        comandoIntermediario?
                            .ChangeCanExecute();
                    });
            }

            private void
                ComandoOriginal_CanExecuteChanged(
                    object? sender,
                    EventArgs e)
            {
                pagina.Dispatcher.Dispatch(
                    () =>
                        comandoIntermediario?
                            .ChangeCanExecute());
            }

            private void RestaurarEventosComandoAnterior()
            {
                if (comandoOriginal != null)
                {
                    comandoOriginal.CanExecuteChanged -=
                        ComandoOriginal_CanExecuteChanged;
                }
            }

            private void CrearOverlay()
            {
                if (overlayNavegacion != null)
                    return;

                if (pagina.Content is not Grid
                    gridPrincipal)
                {
                    return;
                }

                ActivityIndicator indicador =
                    new()
                    {
                        IsRunning = true,
                        Color =
                            Color.FromArgb("#3B655B"),
                        WidthRequest = 48,
                        HeightRequest = 48,
                        HorizontalOptions =
                            LayoutOptions.Center
                    };

                Label titulo =
                    new()
                    {
                        Text =
                            "Abriendo nuevo análisis...",
                        FontSize = 16,
                        FontAttributes =
                            FontAttributes.Bold,
                        TextColor =
                            Color.FromArgb("#111827"),
                        HorizontalTextAlignment =
                            TextAlignment.Center
                    };

                Label detalle =
                    new()
                    {
                        Text =
                            "Preparando el formulario. La primera apertura puede tardar un poco más.",
                        FontSize = 12,
                        TextColor =
                            Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment =
                            TextAlignment.Center,
                        LineBreakMode =
                            LineBreakMode.WordWrap
                    };

                Border tarjeta =
                    new()
                    {
                        BackgroundColor =
                            Colors.White,
                        Stroke =
                            Color.FromArgb("#C8DED6"),
                        StrokeThickness = 1,
                        StrokeShape =
                            new RoundRectangle
                            {
                                CornerRadius =
                                    new CornerRadius(18)
                            },
                        Padding =
                            new Thickness(24, 22),
                        Margin =
                            new Thickness(24),
                        MaximumWidthRequest = 390,
                        HorizontalOptions =
                            LayoutOptions.Center,
                        VerticalOptions =
                            LayoutOptions.Center,
                        Content =
                            new VerticalStackLayout
                            {
                                Spacing = 12,
                                Children =
                                {
                                    indicador,
                                    titulo,
                                    detalle
                                }
                            }
                    };

                overlayNavegacion =
                    new Grid
                    {
                        IsVisible = false,
                        InputTransparent = false,
                        BackgroundColor =
                            Color.FromArgb("#B3FFFFFF"),
                        ZIndex = 30000,
                        HorizontalOptions =
                            LayoutOptions.Fill,
                        VerticalOptions =
                            LayoutOptions.Fill
                    };

                overlayNavegacion.Children.Add(
                    tarjeta);

                Grid.SetRowSpan(
                    overlayNavegacion,
                    Math.Max(
                        1,
                        gridPrincipal
                            .RowDefinitions
                            .Count));

                Grid.SetColumnSpan(
                    overlayNavegacion,
                    Math.Max(
                        1,
                        gridPrincipal
                            .ColumnDefinitions
                            .Count));

                gridPrincipal.Children.Add(
                    overlayNavegacion);
            }

            private void MostrarOverlay()
            {
                CrearOverlay();

                if (overlayNavegacion == null)
                    return;

                overlayNavegacion.IsVisible =
                    true;

                overlayNavegacion.Opacity =
                    1;

                overlayNavegacion
                    .InvalidateMeasure();
            }

            private void OcultarOverlay()
            {
                if (overlayNavegacion != null)
                    overlayNavegacion.IsVisible = false;
            }

            private static ImageButton?
                BuscarBotonNuevoAnalisis(
                    IVisualTreeElement elemento,
                    ICommand comandoEsperado)
            {
                if (elemento is ImageButton boton)
                {
                    bool mismoComando =
                        ReferenceEquals(
                            boton.Command,
                            comandoEsperado);

                    string origen =
                        boton.Source?
                            .ToString() ??
                        string.Empty;

                    bool esIconoAgregar =
                        origen.Contains(
                            "iconadd",
                            StringComparison
                                .OrdinalIgnoreCase);

                    if (mismoComando ||
                        esIconoAgregar)
                    {
                        return boton;
                    }
                }

                foreach (
                    IVisualTreeElement hijo
                    in elemento.GetVisualChildren())
                {
                    ImageButton? encontrado =
                        BuscarBotonNuevoAnalisis(
                            hijo,
                            comandoEsperado);

                    if (encontrado != null)
                        return encontrado;
                }

                return null;
            }
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

            private Grid? overlayCargaInicial;

            private Label? mensajeCargaInicial;

            private INotifyPropertyChanged?
                contextoNotificable;

            private bool cargaInicialActiva;

            private bool isBusyObservado;

            private DateTime inicioCargaVisualUtc;

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
                pagina.Disappearing += Pagina_Disappearing;
                pagina.BindingContextChanged +=
                    Pagina_BindingContextChanged;
                pagina.SizeChanged += Pagina_SizeChanged;

                CrearOverlayCargaInicial();
                AdjuntarContextoNotificable();
                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_Loaded(
                object? sender,
                EventArgs e)
            {
                CrearOverlayCargaInicial();
                AdjuntarContextoNotificable();
                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_Appearing(
                object? sender,
                EventArgs e)
            {
                CrearOverlayCargaInicial();
                AdjuntarContextoNotificable();
                MostrarCargaInicial();
                PrepararCoordinadorUnidadesConRetraso();
                AjustarConRetraso();
            }

            private void Pagina_Disappearing(
                object? sender,
                EventArgs e)
            {
                cargaInicialActiva = false;
                isBusyObservado = false;

                if (overlayCargaInicial != null)
                    overlayCargaInicial.IsVisible = false;
            }

            private void Pagina_BindingContextChanged(
                object? sender,
                EventArgs e)
            {
                AdjuntarContextoNotificable();
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

            private void CrearOverlayCargaInicial()
            {
                if (overlayCargaInicial != null)
                    return;

                if (pagina.Content is not ContentView contentView ||
                    contentView.Content is not Grid gridPrincipal)
                {
                    return;
                }

                ActivityIndicator indicador =
                    new()
                    {
                        IsRunning = true,
                        Color =
                            Color.FromArgb("#3B655B"),
                        WidthRequest = 48,
                        HeightRequest = 48,
                        HorizontalOptions =
                            LayoutOptions.Center
                    };

                mensajeCargaInicial =
                    new Label
                    {
                        Text =
                            "Preparando nuevo análisis...",
                        FontSize = 16,
                        FontAttributes =
                            FontAttributes.Bold,
                        TextColor =
                            Color.FromArgb("#111827"),
                        HorizontalTextAlignment =
                            TextAlignment.Center,
                        LineBreakMode =
                            LineBreakMode.WordWrap
                    };

                Label detalle =
                    new()
                    {
                        Text =
                            "Cargando terrenos, catálogos, elementos y configuraciones. Esto puede tardar unos segundos según la conexión.",
                        FontSize = 12,
                        TextColor =
                            Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment =
                            TextAlignment.Center,
                        LineBreakMode =
                            LineBreakMode.WordWrap
                    };

                Border tarjeta =
                    new()
                    {
                        BackgroundColor =
                            Colors.White,
                        Stroke =
                            Color.FromArgb("#C8DED6"),
                        StrokeThickness = 1,
                        StrokeShape =
                            new RoundRectangle
                            {
                                CornerRadius =
                                    new CornerRadius(18)
                            },
                        Padding =
                            new Thickness(24, 22),
                        Margin =
                            new Thickness(24),
                        MaximumWidthRequest = 390,
                        HorizontalOptions =
                            LayoutOptions.Center,
                        VerticalOptions =
                            LayoutOptions.Center,
                        Content =
                            new VerticalStackLayout
                            {
                                Spacing = 12,
                                HorizontalOptions =
                                    LayoutOptions.Fill,
                                Children =
                                {
                                    indicador,
                                    mensajeCargaInicial,
                                    detalle
                                }
                            }
                    };

                overlayCargaInicial =
                    new Grid
                    {
                        IsVisible = false,
                        InputTransparent = false,
                        BackgroundColor =
                            Color.FromArgb("#B3FFFFFF"),
                        ZIndex = 10000,
                        HorizontalOptions =
                            LayoutOptions.Fill,
                        VerticalOptions =
                            LayoutOptions.Fill
                    };

                overlayCargaInicial.Children.Add(
                    tarjeta);

                Grid.SetRow(
                    overlayCargaInicial,
                    0);

                Grid.SetColumn(
                    overlayCargaInicial,
                    0);

                Grid.SetRowSpan(
                    overlayCargaInicial,
                    Math.Max(
                        1,
                        gridPrincipal
                            .RowDefinitions
                            .Count));

                Grid.SetColumnSpan(
                    overlayCargaInicial,
                    Math.Max(
                        1,
                        gridPrincipal
                            .ColumnDefinitions
                            .Count));

                gridPrincipal.Children.Add(
                    overlayCargaInicial);
            }

            private void AdjuntarContextoNotificable()
            {
                INotifyPropertyChanged?
                    nuevoContexto =
                        pagina.BindingContext
                            as INotifyPropertyChanged;

                if (ReferenceEquals(
                        contextoNotificable,
                        nuevoContexto))
                {
                    return;
                }

                if (contextoNotificable != null)
                {
                    contextoNotificable.PropertyChanged -=
                        Contexto_PropertyChanged;
                }

                contextoNotificable =
                    nuevoContexto;

                if (contextoNotificable != null)
                {
                    contextoNotificable.PropertyChanged +=
                        Contexto_PropertyChanged;
                }
            }

            private void MostrarCargaInicial()
            {
                CrearOverlayCargaInicial();

                if (overlayCargaInicial == null)
                    return;

                inicioCargaVisualUtc =
                    DateTime.UtcNow;

                cargaInicialActiva = true;
                isBusyObservado = false;

                if (mensajeCargaInicial != null)
                {
                    mensajeCargaInicial.Text =
                        pagina.BindingContext
                            is NuevoAnalisisFormEdicionViewModel
                                viewModel &&
                        viewModel.EsModoEdicion
                            ? "Cargando análisis guardado..."
                            : "Preparando nuevo análisis...";
                }

                overlayCargaInicial.IsVisible =
                    true;

                /*
                 * Respaldo de seguridad: un error inesperado nunca debe dejar
                 * la pantalla bloqueada indefinidamente.
                 */
                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromSeconds(30),
                    () =>
                    {
                        if (cargaInicialActiva)
                            OcultarCargaInicial();
                    });
            }

            private void Contexto_PropertyChanged(
                object? sender,
                PropertyChangedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(
                        e.PropertyName) &&
                    !string.Equals(
                        e.PropertyName,
                        nameof(
                            NuevoAnalisisFormEdicionViewModel
                                .IsBusy),
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (pagina.BindingContext
                    is not NuevoAnalisisFormEdicionViewModel
                        viewModel)
                {
                    return;
                }

                if (viewModel.IsBusy)
                {
                    if (!cargaInicialActiva)
                        return;

                    isBusyObservado = true;

                    if (overlayCargaInicial != null)
                        overlayCargaInicial.IsVisible = true;

                    return;
                }

                if (!cargaInicialActiva ||
                    !isBusyObservado)
                {
                    return;
                }

                OcultarCargaInicialRespetandoTiempoMinimo();
            }

            private void
                OcultarCargaInicialRespetandoTiempoMinimo()
            {
                TimeSpan transcurrido =
                    DateTime.UtcNow -
                    inicioCargaVisualUtc;

                TimeSpan minimoVisible =
                    TimeSpan.FromMilliseconds(450);

                TimeSpan restante =
                    minimoVisible -
                    transcurrido;

                if (restante <= TimeSpan.Zero)
                {
                    OcultarCargaInicial();
                    return;
                }

                pagina.Dispatcher.DispatchDelayed(
                    restante,
                    OcultarCargaInicial);
            }

            private void OcultarCargaInicial()
            {
                cargaInicialActiva = false;
                isBusyObservado = false;

                if (overlayCargaInicial != null)
                    overlayCargaInicial.IsVisible = false;
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
