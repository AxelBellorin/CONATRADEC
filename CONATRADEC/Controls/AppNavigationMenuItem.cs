using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Elemento reutilizable de la navegación principal. Oculta opciones sin
    /// permiso y conserva seleccionada la sección a la que pertenece cada
    /// página secundaria.
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

        private static readonly HashSet<string> InspectionPages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "DiagnosticoIAPage",
                "DiagnosticoIASolicitudPage",
                "DiagnosticoIAResultadoPage",
                "DiagnosticoIAAnalizadorPage",
                "DiagnosticoIAAprobadorPage",
                "TerrenoBusquedaIAPage"
            };

        private static readonly HashSet<string> OfflinePages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "datosSinConexionPage",
                "DatosSinConexionPage"
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
                "categoriaPublicacionFormPage",
                "DiagnosticoIAConfiguracionPage"
            };

        private readonly Grid desktopLayout;
        private readonly VerticalStackLayout mobileLayout;
        private readonly Image desktopIcon;
        private readonly Image mobileIcon;
        private readonly Label desktopLabel;
        private readonly Label mobileLabel;

        private bool suscritoPermisos;
        private bool suscritoNavegacion;

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

        public static readonly BindableProperty GrupoPermisosProperty =
            BindableProperty.Create(
                nameof(GrupoPermisos),
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

        public string GrupoPermisos
        {
            get => (string)GetValue(GrupoPermisosProperty);
            set => SetValue(GrupoPermisosProperty, value);
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

            desktopIcon = CrearIcono(24);
            mobileIcon = CrearIcono(26);

            desktopLabel = new Label
            {
                FontFamily = "MontserratMedium",
                FontSize = 15,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            mobileLabel = new Label
            {
                FontFamily = "MontserratMedium",
                FontSize = 10.5,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2,
                MinimumHeightRequest = 26
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
                HeightRequest = 68,
                MinimumHeightRequest = 68,
                Padding = new Thickness(2, 5, 2, 3),
                Spacing = 2,
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
                    Command = new Command(async () => await NavegarAsync())
                });

            Loaded += OnLoaded;
            AplicarPropiedadesVisuales();
            AplicarPermiso();
            ActualizarEstadoActivo();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (Parent == null)
            {
                DesuscribirPermisos();
                DesuscribirNavegacion();
                return;
            }

            SuscribirPermisos();
            SuscribirNavegacion();
            AplicarPermiso();
            ActualizarEstadoActivo();
        }

        private static Image CrearIcono(double size) =>
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
            SuscribirPermisos();
            SuscribirNavegacion();
            AplicarPermiso();
            ActualizarEstadoActivo();
            Dispatcher.Dispatch(ActualizarEstadoActivo);
        }

        private void SuscribirPermisos()
        {
            if (suscritoPermisos)
                return;

            PermissionService.Instance.PermissionsChanged +=
                OnPermissionsChanged;
            suscritoPermisos = true;
        }

        private void DesuscribirPermisos()
        {
            if (!suscritoPermisos)
                return;

            PermissionService.Instance.PermissionsChanged -=
                OnPermissionsChanged;
            suscritoPermisos = false;
        }

        private void SuscribirNavegacion()
        {
            if (suscritoNavegacion || Shell.Current == null)
                return;

            Shell.Current.Navigated += OnShellNavigated;
            suscritoNavegacion = true;
        }

        private void DesuscribirNavegacion()
        {
            if (!suscritoNavegacion || Shell.Current == null)
                return;

            Shell.Current.Navigated -= OnShellNavigated;
            suscritoNavegacion = false;
        }

        private void OnPermissionsChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    AplicarPermiso();
                    ActualizarEstadoActivo();
                });
        }

        private void OnShellNavigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(ActualizarEstadoActivo);
        }

        private static void OnVisualPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is not AppNavigationMenuItem item)
                return;

            item.AplicarPropiedadesVisuales();
            item.ActualizarEstadoActivo();
        }

        private static void OnPermissionPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AppNavigationMenuItem item)
                item.AplicarPermiso();
        }

        private static void OnSectionPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AppNavigationMenuItem item)
                item.ActualizarEstadoActivo();
        }

        private void AplicarPropiedadesVisuales()
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

            double fixedHeight = EsMovil ? 68 : 50;
            HeightRequest = fixedHeight;
            MinimumHeightRequest = fixedHeight;
            VerticalOptions = EsMovil
                ? LayoutOptions.Fill
                : LayoutOptions.Start;
        }

        private void AplicarPermiso()
        {
            bool visible = NavigationPermissionService.PuedeVerOpcion(
                Interfaz,
                GrupoPermisos);

            IsVisible = visible;
            IsEnabled = visible;
            InputTransparent = !visible;
        }

        private void ActualizarEstadoActivo()
        {
            bool active = string.Equals(
                ObtenerSeccionActual(),
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

        private async Task NavegarAsync()
        {
            if (!IsVisible ||
                InputTransparent ||
                string.IsNullOrWhiteSpace(Ruta))
            {
                return;
            }

            if (!NavigationPermissionService.PuedeVerOpcion(
                    Interfaz,
                    GrupoPermisos))
            {
                AplicarPermiso();
                await GlobalService.MostrarInformacionAsync(
                    "No tiene permisos para acceder a esta sección.");
                return;
            }

            if (EsRutaActual(Ruta) ||
                !await NavigationLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                await KeyboardService.HideAsync();
                DiagnosticoIARoutes.AsegurarRegistro();

                if (Shell.Current != null)
                    await Shell.Current.GoToAsync(Ruta, false);
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

        private static bool EsRutaActual(string route)
        {
            string routeName = route
                .Split('?', 2)[0]
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;

            string currentPage =
                Shell.Current?.CurrentPage?.GetType().Name ?? string.Empty;

            return string.Equals(
                       routeName,
                       currentPage,
                       StringComparison.OrdinalIgnoreCase) ||
                   (OfflinePages.Contains(currentPage) &&
                    OfflinePages.Contains(routeName));
        }

        private static string ObtenerSeccionActual()
        {
            string pageName =
                Shell.Current?.CurrentPage?.GetType().Name ?? string.Empty;

            if (OfflinePages.Contains(pageName))
                return "DatosOffline";
            if (InspectionPages.Contains(pageName))
                return "Inspeccion";
            if (NewsPages.Contains(pageName))
                return "Noticias";
            if (AlbumPages.Contains(pageName))
                return "Album";
            if (ConfigurationPages.Contains(pageName))
                return "Configuracion";

            string location =
                Shell.Current?.CurrentState?.Location?.OriginalString ??
                string.Empty;

            if (ContieneAlguno(location, OfflinePages))
                return "DatosOffline";
            if (ContieneAlguno(location, InspectionPages))
                return "Inspeccion";
            if (ContieneAlguno(location, NewsPages))
                return "Noticias";
            if (ContieneAlguno(location, AlbumPages))
                return "Album";
            if (ContieneAlguno(location, ConfigurationPages))
                return "Configuracion";

            return "Inicio";
        }

        private static bool ContieneAlguno(
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
