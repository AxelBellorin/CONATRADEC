using CONATRADEC.ViewModels;
using Microsoft.Maui.Graphics;
using CONATRADEC.Views;
using Microsoft.Maui.Controls;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas ya existentes
            Routing.RegisterRoute(nameof(BalanceFormulasPage), typeof(BalanceFormulasPage));
            Routing.RegisterRoute(nameof(terrenoFormPage), typeof(terrenoFormPage));
            Routing.RegisterRoute(nameof(MapaSeleccionPage), typeof(MapaSeleccionPage));
        }
    }
}
