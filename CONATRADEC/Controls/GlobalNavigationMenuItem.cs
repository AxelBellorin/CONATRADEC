using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Opción del menú principal disponible para cualquier usuario que ya
    /// inició sesión. No depende de permisos administrativos.
    /// </summary>
    public sealed class GlobalNavigationMenuItem :
        Border
    {
        private static readonly SemaphoreSlim
            NavigationLock =
                new(1, 1);

        private readonly Grid desktopLayout;
        private readonly VerticalStackLayout mobileLayout;
        private readonly Image desktopIcon;
        private readonly Image mobileIcon;
        private readonly Label desktopLabel;
        private readonly Label mobileLabel;

        private bool suscritoNavegacion;

        public static readonly BindableProperty TextoProperty =
            BindableProperty.Create(
                nameof(Texto),
                typeof(string),
                typeof(GlobalNavigationMenuItem),
                string.Empty,
                propertyChanged:
                    OnVisualPropertyChanged);

        public static readonly BindableProperty IconoProperty =
            BindableProperty.Create(
                nameof(Icono),
                typeof(string),
                typeof(GlobalNavigationMenuItem),
                string.Empty,
                propertyChanged:
                    OnVisualPropertyChanged);

        public static readonly BindableProperty RutaProperty =
            BindableProperty.Create(
                nameof(Ruta),
                typeof(string),
                typeof(GlobalNavigationMenuItem),
                string.Empty);

        public static readonly BindableProperty EsMovilProperty =
            BindableProperty.Create(
                nameof(EsMovil),
                typeof(bool),
                typeof(GlobalNavigationMenuItem),
                false,
                propertyChanged:
                    OnVisualPropertyChanged);

        public string Texto
        {
            get =>
                (string)GetValue(
                    TextoProperty);

            set =>
                SetValue(
                    TextoProperty,
                    value);
        }

        public string Icono
        {
            get =>
                (string)GetValue(
                    IconoProperty);

            set =>
                SetValue(
                    IconoProperty,
                    value);
        }

        public string Ruta
        {
            get =>
                (string)GetValue(
                    RutaProperty);

            set =>
                SetValue(
                    RutaProperty,
                    value);
        }

        public bool EsMovil
        {
            get =>
                (bool)GetValue(
                    EsMovilProperty);

            set =>
                SetValue(
                    EsMovilProperty,
                    value);
        }

        public GlobalNavigationMenuItem()
        {
            Padding = 0;
            Margin = 0;
            HorizontalOptions =
                LayoutOptions.Fill;
            VerticalOptions =
                LayoutOptions.Start;
            StrokeThickness = 1;
            Stroke =
                new SolidColorBrush(
                    Colors.Transparent);
            StrokeShape =
                new RoundRectangle
                {
                    CornerRadius =
                        new CornerRadius(12)
                };

            desktopIcon =
                CrearIcono(24);

            mobileIcon =
                CrearIcono(26);

            desktopLabel =
                new Label
                {
                    FontFamily =
                        "MontserratMedium",
                    FontSize = 15,
                    VerticalOptions =
                        LayoutOptions.Center,
                    LineBreakMode =
                        LineBreakMode.NoWrap
                };

            mobileLabel =
                new Label
                {
                    FontFamily =
                        "MontserratMedium",
                    FontSize = 11,
                    HorizontalTextAlignment =
                        TextAlignment.Center,
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    LineBreakMode =
                        LineBreakMode.NoWrap
                };

            desktopLayout =
                new Grid
                {
                    HeightRequest = 50,
                    MinimumHeightRequest = 50,
                    Padding =
                        new Thickness(
                            14,
                            11),
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(
                            new GridLength(26)),
                        new ColumnDefinition(
                            GridLength.Star)
                    },
                    ColumnSpacing = 12
                };

            desktopLayout.Add(
                desktopIcon);

            desktopLayout.Add(
                desktopLabel,
                1,
                0);

            mobileLayout =
                new VerticalStackLayout
                {
                    HeightRequest = 58,
                    MinimumHeightRequest = 58,
                    Padding =
                        new Thickness(2, 5),
                    Spacing = 4,
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Center
                };

            mobileLayout.Add(
                mobileIcon);

            mobileLayout.Add(
                mobileLabel);

            var contentGrid =
                new Grid();

            contentGrid.Add(
                desktopLayout);

            contentGrid.Add(
                mobileLayout);

            Content = contentGrid;

            GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command =
                        new Command(
                            async () =>
                                await NavegarAsync())
                });

            Loaded += OnLoaded;

            AplicarPropiedadesVisuales();
            ActualizarEstadoActivo();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent == null)
            {
                DesuscribirNavegacion();
                return;
            }

            SuscribirNavegacion();
        }

        private static Image CrearIcono(
            double size) =>
            new()
            {
                HeightRequest = size,
                WidthRequest = size,
                MinimumHeightRequest = size,
                MinimumWidthRequest = size,
                HorizontalOptions =
                    LayoutOptions.Center,
                VerticalOptions =
                    LayoutOptions.Center,
                Aspect =
                    Aspect.AspectFit
            };

        private void OnLoaded(
            object? sender,
            EventArgs e)
        {
            SuscribirNavegacion();
            ActualizarEstadoActivo();

            Dispatcher.Dispatch(
                ActualizarEstadoActivo);
        }

        private void SuscribirNavegacion()
        {
            if (suscritoNavegacion ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigated +=
                OnShellNavigated;

            suscritoNavegacion = true;
        }

        private void DesuscribirNavegacion()
        {
            if (!suscritoNavegacion ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigated -=
                OnShellNavigated;

            suscritoNavegacion = false;
        }

        private void OnShellNavigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                ActualizarEstadoActivo);
        }

        private static void OnVisualPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable
                is not GlobalNavigationMenuItem item)
            {
                return;
            }

            item.AplicarPropiedadesVisuales();
            item.ActualizarEstadoActivo();
        }

        private void AplicarPropiedadesVisuales()
        {
            string texto =
                Texto ??
                string.Empty;

            desktopLabel.Text = texto;
            mobileLabel.Text = texto;

            ImageSource? source =
                string.IsNullOrWhiteSpace(
                    Icono)
                    ? null
                    : ImageSource.FromFile(
                        Icono);

            desktopIcon.Source = source;
            mobileIcon.Source = source;

            desktopLayout.IsVisible =
                !EsMovil;

            mobileLayout.IsVisible =
                EsMovil;

            double altura =
                EsMovil
                    ? 58
                    : 50;

            HeightRequest = altura;
            MinimumHeightRequest = altura;

            VerticalOptions =
                EsMovil
                    ? LayoutOptions.Fill
                    : LayoutOptions.Start;
        }

        private void ActualizarEstadoActivo()
        {
            string paginaActual =
                Shell.Current?
                    .CurrentPage?
                    .GetType()
                    .Name ??
                string.Empty;

            string rutaNormalizada =
                (Ruta ?? string.Empty)
                    .Trim('/');

            bool activo =
                string.Equals(
                    paginaActual,
                    rutaNormalizada,
                    StringComparison
                        .OrdinalIgnoreCase) ||
                paginaActual.Equals(
                    "datosSinConexionPage",
                    StringComparison
                        .OrdinalIgnoreCase);

            BackgroundColor =
                activo
                    ? Color.FromArgb(
                        "#EEF5F2")
                    : Colors.Transparent;

            Stroke =
                new SolidColorBrush(
                    activo
                        ? Color.FromArgb(
                            "#BFD8CF")
                        : Colors.Transparent);

            Color texto =
                activo
                    ? Color.FromArgb(
                        "#3B655B")
                    : Color.FromArgb(
                        "#111827");

            desktopLabel.TextColor =
                texto;

            mobileLabel.TextColor =
                texto;
        }

        private async Task NavegarAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    Ruta) ||
                !await NavigationLock
                    .WaitAsync(0))
            {
                return;
            }

            try
            {
                if (EsPaginaActual())
                    return;

                await KeyboardService
                    .HideAsync();

                if (Shell.Current != null)
                {
                    await Shell.Current
                        .GoToAsync(
                            Ruta,
                            false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                        "No fue posible abrir Datos sin conexión: " +
                        ex);

                await GlobalService
                    .MostrarErrorAsync(
                        "No fue posible abrir Datos sin conexión.");
            }
            finally
            {
                NavigationLock.Release();
            }
        }

        private bool EsPaginaActual()
        {
            string paginaActual =
                Shell.Current?
                    .CurrentPage?
                    .GetType()
                    .Name ??
                string.Empty;

            return string.Equals(
                paginaActual,
                (Ruta ?? string.Empty)
                    .Trim('/'),
                StringComparison
                    .OrdinalIgnoreCase) ||
                paginaActual.Equals(
                    "datosSinConexionPage",
                    StringComparison
                        .OrdinalIgnoreCase);
        }
    }
}
