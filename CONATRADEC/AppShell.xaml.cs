using CONATRADEC.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {            
            InitializeComponent();
            BindingContext = new AppShellViewModel();
        }
    }

    
}
