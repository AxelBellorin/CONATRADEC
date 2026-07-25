using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Encabezado reutilizable para catálogos.
    ///
    /// Teléfono:
    /// - título y subtítulo a todo el ancho;
    /// - botones en una segunda fila.
    ///
    /// Tablet y escritorio:
    /// - regreso, título y acción principal en una sola fila.
    /// </summary>
    public partial class ResponsiveCatalogHeader : ContentView
    {
        private enum LayoutMode
        {
            Phone,
            Tablet,
            Desktop
        }

        private LayoutMode? currentMode;

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                nameof(Title),
                typeof(string),
                typeof(ResponsiveCatalogHeader),
                string.Empty);

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
                true);

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

        public static readonly BindableProperty PrimaryBackgroundColorProperty =
            BindableProperty.Create(
                nameof(PrimaryBackgroundColor),
                typeof(Color),
                typeof(ResponsiveCatalogHeader),
                Color.FromArgb("#3B655B"));

        public static readonly BindableProperty PrimaryTextColorProperty =
            BindableProperty.Create(
                nameof(PrimaryTextColor),
                typeof(Color),
                typeof(ResponsiveCatalogHeader),
                Colors.White);

        public static readonly BindableProperty IsPrimaryVisibleProperty =
            BindableProperty.Create(
                nameof(IsPrimaryVisible),
                typeof(bool),
                typeof(ResponsiveCatalogHeader),
                true,
                propertyChanged: OnActionVisibilityChanged);

        private static readonly BindablePropertyKey HasSubtitlePropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(HasSubtitle),
                typeof(bool),
                typeof(ResponsiveCatalogHeader),
                false);

        public static readonly BindableProperty HasSubtitleProperty =
            HasSubtitlePropertyKey.BindableProperty;

        public ResponsiveCatalogHeader()
        {
            InitializeComponent();
            UpdateSubtitleState();
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

            switch (mode)
            {
                case LayoutMode.Phone:
                    HeaderGrid.ColumnDefinitions.Add(
                        new ColumnDefinition(GridLength.Star));
                    HeaderGrid.ColumnDefinitions.Add(
                        new ColumnDefinition(GridLength.Star));
                    HeaderGrid.RowDefinitions.Add(
                        new RowDefinition(GridLength.Auto));
                    HeaderGrid.RowDefinitions.Add(
                        new RowDefinition(GridLength.Auto));

                    Grid.SetRow(TitleContainer, 0);
                    Grid.SetColumn(TitleContainer, 0);
                    Grid.SetColumnSpan(TitleContainer, 2);

                    Grid.SetRow(BackButton, 1);
                    Grid.SetColumn(BackButton, 0);
                    Grid.SetColumnSpan(
                        BackButton,
                        IsPrimaryVisible ? 1 : 2);

                    Grid.SetRow(PrimaryButton, 1);
                    Grid.SetColumn(PrimaryButton, 1);
                    Grid.SetColumnSpan(PrimaryButton, 1);

                    TitleLabel.FontSize = 25;
                    BackButton.Padding = new Thickness(10, 9);
                    PrimaryButton.Padding = new Thickness(10, 9);
                    BackButton.FontSize = 12;
                    PrimaryButton.FontSize = 12;
                    SubtitleLabel.MaxLines = 3;
                    break;

                case LayoutMode.Tablet:
                    ConfigureHorizontalGrid();

                    TitleLabel.FontSize = 27;
                    BackButton.Padding = new Thickness(13, 9);
                    PrimaryButton.Padding = new Thickness(14, 10);
                    BackButton.FontSize = 12;
                    PrimaryButton.FontSize = 12;
                    SubtitleLabel.MaxLines = 2;
                    break;

                default:
                    ConfigureHorizontalGrid();

                    TitleLabel.FontSize = 30;
                    BackButton.Padding = new Thickness(16, 10);
                    PrimaryButton.Padding = new Thickness(18, 11);
                    BackButton.FontSize = 13;
                    PrimaryButton.FontSize = 13;
                    SubtitleLabel.MaxLines = 2;
                    break;
            }
        }

        private void ConfigureHorizontalGrid()
        {
            HeaderGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            HeaderGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            HeaderGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            HeaderGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(BackButton, 0);
            Grid.SetColumn(BackButton, 0);
            Grid.SetColumnSpan(BackButton, 1);

            Grid.SetRow(TitleContainer, 0);
            Grid.SetColumn(TitleContainer, 1);
            Grid.SetColumnSpan(TitleContainer, 1);

            Grid.SetRow(PrimaryButton, 0);
            Grid.SetColumn(PrimaryButton, 2);
            Grid.SetColumnSpan(PrimaryButton, 1);
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
