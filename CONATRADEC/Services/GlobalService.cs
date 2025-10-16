using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public class GlobalService : INotifyPropertyChanged
    {
        public Command goToMainPageCommand { get; }
        public Command goToUserPageButtonCommand { get; }
        public Command goToRolPageButtonCommand { get; }
        public Command goToBack { get; }

        private bool isBusy;


        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsBusy
        {
            get =>  isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
                ((Command)goToMainPageCommand).ChangeCanExecute();
                ((Command)goToUserPageButtonCommand).ChangeCanExecute();
                ((Command)goToRolPageButtonCommand).ChangeCanExecute();
            }
        }

        public GlobalService()
        {
            goToMainPageCommand = new Command(async () => await GoToMainPage(), () => !IsBusy);
            goToUserPageButtonCommand = new Command(async () => await GoToUserPage(), () => !IsBusy);
            goToRolPageButtonCommand = new Command(async () => await GoToRolPage(), () => !IsBusy);
            goToBack = new Command(async () => await Shell.Current.GoToAsync("//.."));
        }

        public async Task GoToAsyncParameters(string route, IDictionary<string, object>? parameters = null)
        {
            if (parameters == null)
                await Shell.Current.GoToAsync(route);
            else
                await Shell.Current.GoToAsync(route, parameters);
        }

        private async Task GoToUserPage()
        {
            if (IsBusy) return;
            IsBusy = true;  
            
            await Shell.Current.GoToAsync("//UserPage");   
            
            // Buscar la página actual después de navegar
            if (Shell.Current.CurrentPage is userPage page &&
                page.BindingContext is UserViewModel vm)
            {
                await vm.LoadUsers(IsBusy);
            }
                
               
        }

        private async Task GoToMainPage()
        {
            if (IsBusy) return;
            //IsBusy = true;

            await Shell.Current.GoToAsync("//MainPage");

            if (Shell.Current.CurrentPage is MainPage page &&
                 page.BindingContext is MainPageViewModel vm)
            {
                //await vm.LoadUsers(IsBusy);
               // vm.IsBusy = false;
            }
        }

        public async Task GoToRolPage()
        {
            if (IsBusy) return;
            IsBusy = true;

            await Shell.Current.GoToAsync("//RolPage");

            // Buscar la página actual después de navegar
            if (Shell.Current.CurrentPage is rolPage page &&
                page.BindingContext is RolViewModel vm)
                await vm.LoadRol(IsBusy);
        }
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
