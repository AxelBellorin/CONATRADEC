using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Encabezado reutilizable para catálogos.
    ///
    /// Además de la acción principal, detecta automáticamente los
    /// catálogos que admiten reactivación y muestra "Eliminados".
    /// La distribución se adapta al ancho disponible sin modificar
    /// comandos, permisos ni navegación.
    /// </summary>
    public partial class ResponsiveCatalogHeader : ContentView
    {
        private enum LayoutMode
        {
            Phone,
            Tablet,
            Desktop
        }

        private const string TituloFuenteNutriente =
            "Fuentes de nutrientes";

        private LayoutMode? currentMode;
        private CatalogoEliminadoConfiguracion?
            catalogoEliminados;

        private bool esFuenteNutriente;

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                nameof(Title),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                string.Empty,
                propertyChanged: OnTitleChanged);

        public static readonly BindableProperty SubtitleProperty =
            BindableProperty.Create(
                nameof(Subtitle),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                string.Empty,
                propertyChanged: OnSubtitleChanged);

        public static readonly BindableProperty ContextTextProperty =
            BindableProperty.Create(
                nameof(ContextText),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                string.Empty);

        public static readonly BindableProperty IsContextVisibleProperty =
            BindableProperty.Create(
                nameof(IsContextVisible),
                typeof(bool),
                typeof(ResponsiveCatalogHeader),
                false);

        /// <summary>
        /// Identificador opcional del padre jerárquico utilizado al abrir
        /// Eliminados. Departamento recibe PaisId y Municipio recibe
        /// DepartamentoId. Los demás catálogos ignoran este valor.
        /// </summary>
        public static readonly BindableProperty DeletedParentIdProperty =
            BindableProperty.Create(
                nameof(DeletedParentId),
                typeof(int?),
                typeof(ResponsiveCatalogHeader),
                default(int?));

        public static readonly BindableProperty BackTextProperty =
            BindableProperty.Create(
                nameof(BackText),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                "← Configuración");

        public static readonly BindableProperty BackCommandProperty =
            BindableProperty.Create(
                nameof(BackCommand),
                typeof(ICommand),
                typeof(ResponsiveCatalogHeader));

        public static readonly BindableProperty IsBackVisibleProperty =
            BindableProperty.Create(
                nameof(IsBackVisible),
                typeof(bool),
                typeof(ResponsiveCatalogHeader),
                true,
                propertyChanged: OnActionVisibilityChanged);

        public static readonly BindableProperty PrimaryTextProperty =
            BindableProperty.Create(
                nameof(PrimaryText),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                string.Empty);

        public static readonly BindableProperty PrimaryCommandProperty =
            BindableProperty.Create(
                nameof(PrimaryCommand),
                typeof(ICommand),
                typeof(ResponsiveCatalogHeader));

        public static readonly BindableProperty
            PrimaryBackgroundColorProperty =
                BindableProperty.Create(
                    nameof(PrimaryBackgroundColor),
                    typeof(Color),
                    typeof(ResponsiveCatalogHeader),
                    Color.FromArgb("#3B655B"));

        public static readonly BindableProperty
            PrimaryTextColorProperty =
                BindableProperty.Create(
                    nameof(PrimaryTextColor),
                    typeof(Color),
                    typeof(ResponsiveCatalogHeader),
                    Colors.White);

        public static readonly BindableProperty
            IsPrimaryVisibleProperty =
                BindableProperty.Create(
                    nameof(IsPrimaryVisible),
                    typeof(bool),
                    typeof(ResponsiveCatalogHeader),
                    true,
                    propertyChanged:
                        OnActionVisibilityChanged);

        private static readonly BindablePropertyKey
            HasSubtitlePropertyKey =
                BindableProperty.CreateReadOnly(
                    nameof(HasSubtitle),
                    typeof(bool),
                    typeof(ResponsiveCatalogHeader),
                    false);

        public static readonly BindableProperty
            HasSubtitleProperty =
                HasSubtitlePropertyKey.BindableProperty;

        public ResponsiveCatalogHeader()
        {
            InitializeComponent();
            UpdateSubtitleState();
            UpdateDeletedState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public string ContextText
        {
            get => (string)GetValue(ContextTextProperty);
            set => SetValue(ContextTextProperty, value);
        }

        public bool IsContextVisible
        {
            get => (bool)GetValue(IsContextVisibleProperty);
            set => SetValue(IsContextVisibleProperty, value);
        }

        public int? DeletedParentId
        {
            get => (int?)GetValue(DeletedParentIdProperty);
            set => SetValue(DeletedParentIdProperty, value);
        }

        public string BackText
        {
            get => (string)GetValue(BackTextProperty);
            set => SetValue(BackTextProperty, value);
        }

        public ICommand? BackCommand
        {
            get => (ICommand?)GetValue(BackCommandProperty);
            set => SetValue(BackCommandProperty, value);
        }

        public bool IsBackVisible
        {
            get => (bool)GetValue(IsBackVisibleProperty);
            set => SetValue(IsBackVisibleProperty, value);
        }

        public string PrimaryText
        {
            get => (string)GetValue(PrimaryTextProperty);
            set => SetValue(PrimaryTextProperty, value);
        }

        public ICommand? PrimaryCommand
        {
            get => (ICommand?)GetValue(PrimaryCommandProperty);
            set => SetValue(PrimaryCommandProperty, value);
        }

        public Color PrimaryBackgroundColor
        {
            get => (Color)GetValue(PrimaryBackgroundColorProperty);
            set => SetValue(PrimaryBackgroundColorProperty, value);
        }

        public Color PrimaryTextColor
        {
            get => (Color)GetValue(PrimaryTextColorProperty);
            set => SetValue(PrimaryTextColorProperty, value);
        }

        public bool IsPrimaryVisible
        {
            get => (bool)GetValue(IsPrimaryVisibleProperty);
            set => SetValue(IsPrimaryVisibleProperty, value);
        }

        public bool HasSubtitle =>
            (bool)GetValue(HasSubtitleProperty);

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0)
                return;

            LayoutMode mode =
                width < 600
                    ? LayoutMode.Phone
                    : width < 1000
                        ? LayoutMode.Tablet
                        : LayoutMode.Desktop;

            if (currentMode == mode)
                return;

            currentMode = mode;
            ApplyLayout(mode);
        }

        private void ApplyLayout(LayoutMode mode)
        {
            HeaderGrid.RowDefinitions.Clear();
            HeaderGrid.ColumnDefinitions.Clear();

            RestablecerOpcionesAcciones();

            switch (mode)
            {
                case LayoutMode.Phone:
                    ConfigurePhoneGrid();
                    TitleLabel.FontSize = 24;

                    /*
                     * Las tres acciones deben caber en una sola fila.
                     * Se reduce únicamente su presentación; comandos, rutas y
                     * permisos permanecen intactos.
                     */
                    ConfigurarBotonMovil(BackButton);
                    ConfigurarBotonMovil(DeletedButton);
                    ConfigurarBotonMovil(PrimaryButton);
                    SubtitleLabel.MaxLines = 3;
                    break;

                case LayoutMode.Tablet:
                    ConfigureHorizontalGrid();
                    TitleLabel.FontSize = 28;
                    BackButton.FontSize = 12;
                    DeletedButton.FontSize = 12;
                    PrimaryButton.FontSize = 12;
                    SubtitleLabel.MaxLines = 2;
                    break;

                default:
                    ConfigureHorizontalGrid();
                    TitleLabel.FontSize = 30;
                    BackButton.FontSize = 12.5;
                    DeletedButton.FontSize = 12.5;
                    PrimaryButton.FontSize = 12.5;
                    SubtitleLabel.MaxLines = 2;
                    break;
            }
        }

        /// <summary>
        /// En teléfono el título ocupa la primera fila y todas las acciones
        /// visibles comparten una segunda fila. Esto evita que el encabezado
        /// consuma media pantalla antes de que el usuario llegue al listado.
        /// </summary>
        private void ConfigurePhoneGrid()
        {
            List<View> acciones = ObtenerAccionesVisibles();

            int columnas = Math.Max(1, acciones.Count);

            for (int indice = 0; indice < columnas; indice++)
            {
                HeaderGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            HeaderGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(TitleContainer, 0);
            Grid.SetColumn(TitleContainer, 0);
            Grid.SetColumnSpan(TitleContainer, columnas);

            if (acciones.Count == 0)
                return;

            HeaderGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            for (int indice = 0; indice < acciones.Count; indice++)
            {
                View accion = acciones[indice];
                Grid.SetRow(accion, 1);
                Grid.SetColumn(accion, indice);
                Grid.SetColumnSpan(accion, 1);
            }
        }

        private static void ConfigurarBotonMovil(Button button)
        {
            button.FontSize = 9.4;
            button.HeightRequest = 40;
            button.MinimumHeightRequest = 40;
            button.MinimumWidthRequest = 0;
            button.Padding = new Thickness(5, 7);
            button.CornerRadius = 10;
            button.HorizontalOptions = LayoutOptions.Fill;
        }

        private void ConfigureHorizontalGrid()
        {
            HeaderGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            int columna = 0;

            if (IsBackVisible)
            {
                HeaderGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(BackButton, 0);
                Grid.SetColumn(BackButton, columna++);
                Grid.SetColumnSpan(BackButton, 1);
            }

            HeaderGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            Grid.SetRow(TitleContainer, 0);
            Grid.SetColumn(TitleContainer, columna++);
            Grid.SetColumnSpan(TitleContainer, 1);

            if (DeletedButton.IsVisible)
            {
                HeaderGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(DeletedButton, 0);
                Grid.SetColumn(DeletedButton, columna++);
                Grid.SetColumnSpan(DeletedButton, 1);
            }

            if (IsPrimaryVisible)
            {
                HeaderGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(PrimaryButton, 0);
                Grid.SetColumn(PrimaryButton, columna);
                Grid.SetColumnSpan(PrimaryButton, 1);
            }
        }

        private void RestablecerOpcionesAcciones()
        {
            BackButton.HorizontalOptions = LayoutOptions.Fill;
            DeletedButton.HorizontalOptions = LayoutOptions.Fill;
            PrimaryButton.HorizontalOptions = LayoutOptions.Fill;

            BackButton.MinimumWidthRequest = 0;
            DeletedButton.MinimumWidthRequest = 0;
            PrimaryButton.MinimumWidthRequest = 0;
        }

        private List<View> ObtenerAccionesVisibles()
        {
            var acciones = new List<View>();

            if (IsBackVisible)
                acciones.Add(BackButton);

            if (DeletedButton.IsVisible)
                acciones.Add(DeletedButton);

            if (IsPrimaryVisible)
                acciones.Add(PrimaryButton);

            return acciones;
        }

        private async void OnDeletedClicked(
            object? sender,
            EventArgs e)
        {
            if (!esFuenteNutriente &&
                catalogoEliminados == null)
            {
                return;
            }

            DeletedButton.IsEnabled = false;

            try
            {
                /*
                 * Fuente de Nutriente conserva su pantalla especializada,
                 * porque muestra composición, clasificación y precio.
                 */
                if (esFuenteNutriente)
                {
                    INavigation? navigation =
                        Shell.Current?.Navigation;

                    if (navigation == null)
                        return;

                    await navigation.PushModalAsync(
                        new NavigationPage(
                            new FuenteNutrienteEliminadasPage()));

                    return;
                }

                await CatalogoEliminadosLauncher
                    .AbrirAsync(
                        catalogoEliminados!,
                        DeletedParentId);
            }
            finally
            {
                DeletedButton.IsEnabled = true;
            }
        }

        private void UpdateDeletedState()
        {
            bool disponible =
                CatalogoEliminadoCodigos
                    .TryGetPorTitulo(
                        Title,
                        out CatalogoEliminadoConfiguracion
                            configuracion);

            catalogoEliminados =
                disponible
                    ? configuracion
                    : null;

            esFuenteNutriente =
                string.Equals(
                    Title?.Trim(),
                    TituloFuenteNutriente,
                    StringComparison.OrdinalIgnoreCase);

            /*
             * Los catálogos comunes utilizan CatalogoEliminadosPage.
             * Fuente de Nutriente abre su pantalla especializada.
             */
            DeletedButton.IsVisible =
                disponible || esFuenteNutriente;

            if (currentMode is LayoutMode mode)
                ApplyLayout(mode);
        }

        private static void OnTitleChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ((ResponsiveCatalogHeader)bindable)
                .UpdateDeletedState();
        }

        private static void OnActionVisibilityChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            var control =
                (ResponsiveCatalogHeader)bindable;

            if (control.currentMode is LayoutMode mode)
                control.ApplyLayout(mode);
        }

        private static void OnSubtitleChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ((ResponsiveCatalogHeader)bindable)
                .UpdateSubtitleState();
        }

        private void UpdateSubtitleState()
        {
            SetValue(
                HasSubtitlePropertyKey,
                !string.IsNullOrWhiteSpace(Subtitle));
        }
    }
}
