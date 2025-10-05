using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CONATRADEC.ViewModels
{
    internal class UserViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<User> _users;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public UserViewModel()
        {
            // Datos de prueba
            Users = new ObservableCollection<User>
            {
                new User { Name = "Alice Johnson", Email = "alice.johnson@example.com", Avatar="alice.png" },
                new User { Name = "Bob Smith", Email = "bob.smith@example.com", Avatar="bob.png" },
                new User { Name = "Carol Williams", Email = "carol.williams@example.com", Avatar="carol.png" },
                new User { Name = "David Brown", Email = "david.brown@example.com", Avatar="david.png" }
            };

            AddUserCommand = new Command(OnAddUser);
            EditUserCommand = new Command<User>(OnEditUser);
            DeleteUserCommand = new Command<User>(OnDeleteUser);
        }

        private async void OnAddUser()
        {
            await App.Current.MainPage.DisplayAlert("Agregar", "Abrir formulario para agregar usuario.", "OK");
        }

        private async void OnEditUser(User user)
        {
            if (user == null) return;
            await App.Current.MainPage.DisplayAlert("Editar", $"Editar usuario: {user.Name}", "OK");
        }

        private async void OnDeleteUser(User user)
        {
            if (user == null) return;

            bool confirm = await App.Current.MainPage.DisplayAlert("Eliminar",
                $"¿Seguro que deseas eliminar a {user.Name}?", "Sí", "No");

            if (confirm)
            {
                Users.Remove(user);
            }
        }

        private async Task GoToMainPageButtonCommand()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async Task GoToUserPageButtonCommand()
        {
            await Shell.Current.GoToAsync("//UserPage");
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
