using CONATRADEC.Services;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using System.Threading;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Opción estable del menú principal.
    ///
    /// No usa Flyout, animaciones, opacidad ni eventos de navegación globales.
    /// Mantiene medidas fijas para que seleccionar una opción no desplace las
    /// demás opciones del menú.
    /// </summary>
    public sealed class AppNavigationMenuItem : Border
    {
        private static readonly SemaphoreSlim NavigationLock = new(1, 1);

        private static readonly HashSet<string> NewsPages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "noticiasPage",
                "noticiaDetallePage",
                "publicacionesAdminPage",
                "publicacionFormPage"
            };

        private static readonly HashSet<string> AlbumPages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "albumFotosPage",
                "albumDetallePage",
                "categoriaAlbumFormPage",
                "albumRegistroFormPage",
                "albumFotosAdminPage",
                "albumFotoVisorPage"
            };

        private static readonly HashSet<string> ConfigurationPages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "configuracionPage",
                "userPage",
                "userFormPage",
                "rolPage",
                "rolFormPage",
                "matrizPermisosPage",
                "paisPage",
                "paisFormPage",
                "departamentoPage",
                "departamentoFormPage",
                "municipioPage",
                "municipioFormPage",
                "terrenoPage",
                "terrenoFormPage",
                "elementoQuimicoPage",
                "elementoQuimicoFormPage",
                "fuenteNutrientePage",
                "fuenteNutrienteFormPage",
                "tipoCultivoPage",
                "tipoCultivoFormPage",
                "tipoAnalisisSueloPage",
                "tipoAnalisisSueloFormPage",
                "extraccionNutrientePage",
                "extraccionNutrienteFormPage",
                "rangoNutrientePage",
                "rangoNutrienteDetallePage",
                "rangoNutrienteCategoriaFormPage",
                "rangoNutrienteFormPage",
                "bitacoraPage",
                "bitacoraDetallePage",
                "categoriaPublicacionPage",
                "categoriaPublicacionFormPage"
            };

        private readonly Grid desktopLayout;
        private readonly VerticalStackLayout mobileLayout;
        private readonly Image desktopIcon;
        private readonly Image mobileIcon;
        private readonly Label desktopLabel;
        private readonly Label mobileLabel;

        public static readonly BindableProperty TextoProperty =
            BindableProperty.Create(
                nameof(Texto),
                typeof(string),
                typeof(AppNavigationMenuItem),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconoProperty =
            BindableProperty.Create(
                nameof(Icono),
                typeof(string),
                typeof(AppNavigationMenuItem),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty InterfazProperty =
            BindableProperty.Create(
                nameof(Interfaz),
                typeof(string),
                typeof(AppNavigationMenuItem),
                string.Empty,
                propertyChanged: OnPermissionPropertyChanged);

        public static readonly BindableProperty RutaProperty =
            BindableProperty.Create(
                nameof(Ruta),
                typeof(string),
                typeof(AppNavigationMenuItem),
                string.Empty);

        public static readonly BindableProperty SeccionProperty =
            BindableProperty.Create(
                nameof(Seccion),
                typeof(string),
                typeof(AppNavigationMenuItem),
                string.Empty,
                propertyChanged: OnSectionPropertyChanged);

        public static readonly BindableProperty EsMovilProperty =
            BindableProperty.Create(
                nameof(EsMovil),
                typeof(bool),
                typeof(AppNavigationMenuItem),
                false,
                propertyChanged: OnVisualPropertyChanged);

        public string Texto
        {
            get => (string)GetValue(TextoProperty);
            set => SetValue(TextoProperty, value);
        }

        public string Icono
        {
            get => (string)GetValue(IconoProperty);
            set => SetValue(IconoProperty, value);
        }

        public string Interfaz
        {
            get => (string)GetValue(InterfazProperty);
            set => SetValue(InterfazProperty, value);
        }

        public string Ruta
        {
            get => (string)GetValue(RutaProperty);
            set => SetValue(RutaProperty, value);
        }

        public string Seccion
        {
            get => (string)GetValue(SeccionProperty);
            set => SetValue(SeccionProperty, value);
        }

        public bool EsMovil
        {
            get => (bool)GetValue(EsMovilProperty);
            set => SetValue(EsMovilProperty, value);
        }

        public AppNavigationMenuItem()
        {
            Padding = 0;
            Margin = 0;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Start;
            StrokeThickness = 1;
            Stroke = new SolidColorBrush(Colors.Transparent);
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12)
            };

            desktopIcon = CreateIcon(24);
            mobileIcon = CreateIcon(26);

            desktopLabel = new Label
            {
                FontFamily = "MontserratMedium",
                FontSize = 15,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            };

            mobileLabel = new Label
            {
                FontFamily = "MontserratMedium",
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill,
                LineBreakMode = LineBreakMode.NoWrap
            };

            desktopLayout = new Grid
            {
                HeightRequest = 50,
                MinimumHeightRequest = 50,
                Padding = new Thickness(14, 11),
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(26)),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12
            };

            desktopLayout.Add(desktopIcon);
            desktopLayout.Add(desktopLabel, 1, 0);

            mobileLayout = new VerticalStackLayout
            {
                HeightRequest = 58,
                MinimumHeightRequest = 58,
                Padding = new Thickness(2, 5),
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };

            mobileLayout.Add(mobileIcon);
            mobileLayout.Add(mobileLabel);

            var contentGrid = new Grid();
            contentGrid.Add(desktopLayout);
            contentGrid.Add(mobileLayout);
            Content = contentGrid;

            GestureRecognizers.Add(
                new TapGestureRecognizer
                {
                    Command = new Command(
                        async () => await NavigateAsync())
                });

            Loaded += OnLoaded;

            ApplyVisualProperties();
            ApplyPermission();
            UpdateActiveState();
        }

        private static Image CreateIcon(double size) =>
            new()
            {
                HeightRequest = size,
                WidthRequest = size,
                MinimumHeightRequest = size,
                MinimumWidthRequest = size,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Aspect = Aspect.AspectFit
            };

        private void OnLoaded(object? sender, EventArgs e)
        {
            ApplyPermission();
            UpdateActiveState();
            Dispatcher.Dispatch(UpdateActiveState);
        }

        private static void OnVisualPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is not AppNavigationMenuItem item)
                return;

            item.ApplyVisualProperties();
            item.UpdateActiveState();
        }

        private static void OnPermissionPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AppNavigationMenuItem item)
                item.ApplyPermission();
        }

        private static void OnSectionPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AppNavigationMenuItem item)
                item.UpdateActiveState();
        }

        private void ApplyVisualProperties()
        {
            string text = Texto ?? string.Empty;
            desktopLabel.Text = text;
            mobileLabel.Text = text;

            ImageSource? source = string.IsNullOrWhiteSpace(Icono)
                ? null
                : ImageSource.FromFile(Icono);

            desktopIcon.Source = source;
            mobileIcon.Source = source;

            desktopLayout.IsVisible = !EsMovil;
            mobileLayout.IsVisible = EsMovil;

            double fixedHeight = EsMovil ? 58 : 50;
            HeightRequest = fixedHeight;
            MinimumHeightRequest = fixedHeight;
            VerticalOptions = EsMovil
                ? LayoutOptions.Fill
                : LayoutOptions.Start;
        }

        private void ApplyPermission()
        {
            bool visible = string.IsNullOrWhiteSpace(Interfaz) ||
                           PermissionService.Instance.HasRead(Interfaz);

            IsVisible = visible;
            InputTransparent = !visible;
        }

        private void UpdateActiveState()
        {
            bool active = string.Equals(
                GetCurrentSection(),
                Seccion,
                StringComparison.OrdinalIgnoreCase);

            BackgroundColor = active
                ? Color.FromArgb("#EEF5F2")
                : Colors.Transparent;

            Stroke = new SolidColorBrush(
                active
                    ? Color.FromArgb("#BFD8CF")
                    : Colors.Transparent);

            Color textColor = active
                ? Color.FromArgb("#3B655B")
                : Color.FromArgb("#111827");

            desktopLabel.TextColor = textColor;
            mobileLabel.TextColor = textColor;
        }

        private async Task NavigateAsync()
        {
            if (!IsVisible || InputTransparent ||
                string.IsNullOrWhiteSpace(Ruta))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Interfaz) &&
                !PermissionService.Instance.HasRead(Interfaz))
            {
                await GlobalService.MostrarInformacionAsync(
                    "No tiene permisos para acceder a esta sección.");
                return;
            }

            if (IsCurrentPageRoute(Ruta) ||
                !await NavigationLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                await KeyboardService.HideAsync();

                Shell? shell = Shell.Current;
                if (shell != null)
                    await shell.GoToAsync(Ruta, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"No fue posible navegar a '{Ruta}': {ex}");

                await GlobalService.MostrarErrorAsync(
                    "No fue posible abrir la opción seleccionada.");
            }
            finally
            {
                NavigationLock.Release();
            }
        }

        private static bool IsCurrentPageRoute(string route)
        {
            string normalizedRoute = route.Trim('/');
            string currentPage =
                Shell.Current?.CurrentPage?.GetType().Name ??
                string.Empty;

            return string.Equals(
                normalizedRoute,
                currentPage,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentSection()
        {
            string pageName =
                Shell.Current?.CurrentPage?.GetType().Name ??
                string.Empty;

            if (NewsPages.Contains(pageName))
                return "Noticias";

            if (AlbumPages.Contains(pageName))
                return "Album";

            if (ConfigurationPages.Contains(pageName))
                return "Configuracion";

            string location =
                Shell.Current?.CurrentState?.Location?.OriginalString ??
                string.Empty;

            if (ContainsAny(location, NewsPages))
                return "Noticias";

            if (ContainsAny(location, AlbumPages))
                return "Album";

            if (ContainsAny(location, ConfigurationPages))
                return "Configuracion";

            return "Inicio";
        }

        private static bool ContainsAny(
            string value,
            IEnumerable<string> candidates)
        {
            foreach (string candidate in candidates)
            {
                if (value.Contains(
                        candidate,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
