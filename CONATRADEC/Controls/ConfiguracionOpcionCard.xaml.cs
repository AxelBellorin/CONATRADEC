using CONATRADEC.Models;
using System.Windows.Input;

namespace CONATRADEC.Controls
{
    public partial class ConfiguracionOpcionCard : ContentView
    {
        public static readonly BindableProperty OpcionProperty =
            BindableProperty.Create(
                nameof(Opcion),
                typeof(ConfiguracionOpcion),
                typeof(ConfiguracionOpcionCard),
                default(ConfiguracionOpcion));

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(
                nameof(Command),
                typeof(ICommand),
                typeof(ConfiguracionOpcionCard),
                default(ICommand));

        public ConfiguracionOpcionCard()
        {
            InitializeComponent();
        }

        public ConfiguracionOpcion? Opcion
        {
            get =>
                (ConfiguracionOpcion?)GetValue(OpcionProperty);

            set =>
                SetValue(OpcionProperty, value);
        }

        public ICommand? Command
        {
            get =>
                (ICommand?)GetValue(CommandProperty);

            set =>
                SetValue(CommandProperty, value);
        }
    }
}
