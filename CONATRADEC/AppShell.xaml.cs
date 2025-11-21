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

            // Register routes for Shell navigation
            Routing.RegisterRoute(nameof(BalanceFormulasPage), typeof(BalanceFormulasPage));
        }
    }
}
