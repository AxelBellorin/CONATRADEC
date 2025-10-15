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

        private bool isBusyUser;
        private bool isBusyMain;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsBusyMain
        {
            get => isBusyMain;
            set
            {
                isBusyMain = value;
                OnPropertyChanged();
                ((Command)goToMainPageCommand).ChangeCanExecute();
            }
        }
        public bool IsBusyUser
        {
            get => isBusyUser;
            set
            {
                isBusyUser = value;
                OnPropertyChanged();
                ((Command)goToUserPageButtonCommand).ChangeCanExecute();
            }
        }



        public GlobalService()
        {
            goToMainPageCommand = new Command(async () => await GoToMainPage(), () => !isBusyMain);
            goToUserPageButtonCommand = new Command(async () => await GoToUserPage(), () => !IsBusyUser);
            goToRolPageButtonCommand = new Command(async () => await Shell.Current.GoToAsync("//RolPage"));
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
            if (IsBusyUser) return;
            IsBusyUser = true;

            await Shell.Current.GoToAsync("//UserPage");

            // Buscar la página actual después de navegar
            if (Shell.Current.CurrentPage is userPage page &&
                page.BindingContext is UserViewModel vm)
                await vm.LoadUsers();
            IsBusyUser = false;
        }

        private async Task GoToMainPage()
        {
            if (IsBusyUser) return;
            IsBusyUser = true;

            await Shell.Current.GoToAsync("//MainPage");

            // Buscar la página actual después de navegar
            if (Shell.Current.CurrentPage is userPage page &&
                page.BindingContext is UserViewModel vm)
                await vm.LoadUsers();
            IsBusyUser = false;
        }

        private async Task GoToRolPage()
        {
            if (IsBusyUser) return;
            IsBusyUser = true;

            await Shell.Current.GoToAsync("//UserPage");

            // Buscar la página actual después de navegar
            if (Shell.Current.CurrentPage is rolPage page &&
                page.BindingContext is RolViewModel vm)
                await vm.LoadRol();
            IsBusyUser = false;
        }
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
