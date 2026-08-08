using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Encabezado compacto y estable para el flujo fitosanitario.
    /// Mantiene la acción de regreso separada del título para impedir que
    /// ambos controles se superpongan en pantallas angostas.
    /// </summary>
    public partial class InspectionHeader : ContentView
    {
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                nameof(Title),
                typeof(string),
                typeof(InspectionHeader),
                string.Empty);

        public static readonly BindableProperty SubtitleProperty =
            BindableProperty.Create(
                nameof(Subtitle),
                typeof(string),
                typeof(InspectionHeader),
                string.Empty,
                propertyChanged: OnSubtitleChanged);

        public static readonly BindableProperty BackTextProperty =
            BindableProperty.Create(
                nameof(BackText),
                typeof(string),
                typeof(InspectionHeader),
                "← Volver");

        public static readonly BindableProperty BackCommandProperty =
            BindableProperty.Create(
                nameof(BackCommand),
                typeof(ICommand),
                typeof(InspectionHeader));

        public static readonly BindableProperty IsBackVisibleProperty =
            BindableProperty.Create(
                nameof(IsBackVisible),
                typeof(bool),
                typeof(InspectionHeader),
                true);

        public static readonly BindableProperty PrimaryTextProperty =
            BindableProperty.Create(
                nameof(PrimaryText),
                typeof(string),
                typeof(InspectionHeader),
                string.Empty);

        public static readonly BindableProperty PrimaryCommandProperty =
            BindableProperty.Create(
                nameof(PrimaryCommand),
                typeof(ICommand),
                typeof(InspectionHeader));

        public static readonly BindableProperty IsPrimaryVisibleProperty =
            BindableProperty.Create(
                nameof(IsPrimaryVisible),
                typeof(bool),
                typeof(InspectionHeader),
                false);

        public static readonly BindableProperty PrimaryBackgroundColorProperty =
            BindableProperty.Create(
                nameof(PrimaryBackgroundColor),
                typeof(Color),
                typeof(InspectionHeader),
                Color.FromArgb("#3B655B"));

        public static readonly BindableProperty PrimaryTextColorProperty =
            BindableProperty.Create(
                nameof(PrimaryTextColor),
                typeof(Color),
                typeof(InspectionHeader),
                Colors.White);

        private static readonly BindablePropertyKey HasSubtitlePropertyKey =
            BindableProperty.CreateReadOnly(
                nameof(HasSubtitle),
                typeof(bool),
                typeof(InspectionHeader),
                false);

        public static readonly BindableProperty HasSubtitleProperty =
            HasSubtitlePropertyKey.BindableProperty;

        public InspectionHeader()
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

        public bool IsPrimaryVisible
        {
            get => (bool)GetValue(IsPrimaryVisibleProperty);
            set => SetValue(IsPrimaryVisibleProperty, value);
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

        public bool HasSubtitle =>
            (bool)GetValue(HasSubtitleProperty);

        private static void OnSubtitleChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ((InspectionHeader)bindable).UpdateSubtitleState();
        }

        private void UpdateSubtitleState()
        {
            SetValue(
                HasSubtitlePropertyKey,
                !string.IsNullOrWhiteSpace(Subtitle));
        }
    }
}
