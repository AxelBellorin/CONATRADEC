using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Panel de búsqueda adaptable para teléfono, tablet y escritorio.
    /// Mantiene los mismos comandos y únicamente reorganiza la presentación.
    /// </summary>
    public partial class ResponsiveSearchPanel : ContentView
    {
        private bool? compactMode;

        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.Create(
                nameof(Placeholder),
                typeof(string),
                typeof(ResponsiveSearchPanel),
                "Buscar");

        public static readonly BindableProperty SearchTextProperty =
            BindableProperty.Create(
                nameof(SearchText),
                typeof(string),
                typeof(ResponsiveSearchPanel),
                string.Empty,
                BindingMode.TwoWay);

        public static readonly BindableProperty SummaryTextProperty =
            BindableProperty.Create(
                nameof(SummaryText),
                typeof(string),
                typeof(ResponsiveSearchPanel),
                string.Empty);

        public static readonly BindableProperty SearchCommandProperty =
            BindableProperty.Create(
                nameof(SearchCommand),
                typeof(ICommand),
                typeof(ResponsiveSearchPanel));

        public static readonly BindableProperty ClearCommandProperty =
            BindableProperty.Create(
                nameof(ClearCommand),
                typeof(ICommand),
                typeof(ResponsiveSearchPanel));

        public static readonly BindableProperty SearchButtonTextProperty =
            BindableProperty.Create(
                nameof(SearchButtonText),
                typeof(string),
                typeof(ResponsiveSearchPanel),
                "Buscar");

        public static readonly BindableProperty ClearButtonTextProperty =
            BindableProperty.Create(
                nameof(ClearButtonText),
                typeof(string),
                typeof(ResponsiveSearchPanel),
                "Limpiar");

        public ResponsiveSearchPanel()
        {
            InitializeComponent();
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public string SummaryText
        {
            get => (string)GetValue(SummaryTextProperty);
            set => SetValue(SummaryTextProperty, value);
        }

        public ICommand? SearchCommand
        {
            get => (ICommand?)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public ICommand? ClearCommand
        {
            get => (ICommand?)GetValue(ClearCommandProperty);
            set => SetValue(ClearCommandProperty, value);
        }

        public string SearchButtonText
        {
            get => (string)GetValue(SearchButtonTextProperty);
            set => SetValue(SearchButtonTextProperty, value);
        }

        public string ClearButtonText
        {
            get => (string)GetValue(ClearButtonTextProperty);
            set => SetValue(ClearButtonTextProperty, value);
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0)
                return;

            bool compact = width < 600;

            ContainerBorder.Padding =
                width < 600
                    ? new Thickness(12)
                    : width < 1000
                        ? new Thickness(14)
                        : new Thickness(16);

            if (compactMode == compact)
                return;

            compactMode = compact;
            ApplyLayout(compact);
        }

        private void ApplyLayout(bool compact)
        {
            ActionsGrid.ColumnDefinitions.Clear();
            ActionsGrid.RowDefinitions.Clear();

            if (compact)
            {
                ActionsGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                ActionsGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                ActionsGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                ActionsGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(SummaryLabel, 0);
                Grid.SetColumn(SummaryLabel, 0);
                Grid.SetColumnSpan(SummaryLabel, 2);

                Grid.SetRow(SearchButton, 1);
                Grid.SetColumn(SearchButton, 0);
                Grid.SetColumnSpan(SearchButton, 1);

                Grid.SetRow(ClearButton, 1);
                Grid.SetColumn(ClearButton, 1);
                Grid.SetColumnSpan(ClearButton, 1);

                SearchButton.HorizontalOptions = LayoutOptions.Fill;
                ClearButton.HorizontalOptions = LayoutOptions.Fill;
                return;
            }

            ActionsGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            ActionsGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            ActionsGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            ActionsGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(SummaryLabel, 0);
            Grid.SetColumn(SummaryLabel, 0);
            Grid.SetColumnSpan(SummaryLabel, 1);

            Grid.SetRow(SearchButton, 0);
            Grid.SetColumn(SearchButton, 1);
            Grid.SetColumnSpan(SearchButton, 1);

            Grid.SetRow(ClearButton, 0);
            Grid.SetColumn(ClearButton, 2);
            Grid.SetColumnSpan(ClearButton, 1);

            SearchButton.HorizontalOptions = LayoutOptions.End;
            ClearButton.HorizontalOptions = LayoutOptions.End;
        }
    }
}
