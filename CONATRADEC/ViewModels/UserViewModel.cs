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
    public class UserViewModel : GlobalService
    {
        private ObservableCollection<User> usersList = new ObservableCollection<User>();
        private readonly UserApiService userApiService;

        public Command AddUserCommand { get; }
        public Command EditUserCommand { get; }
        public Command DeleteUserCommand { get; }
        public ObservableCollection<User> UsersList { get => usersList; set { usersList = value; OnPropertyChanged(); } }

        public UserViewModel()
        {

            try
            {
                userApiService = new UserApiService();
                AddUserCommand = new Command(async () => await OnAddUser());
                EditUserCommand = new Command<User>(OnEditUser);
                DeleteUserCommand = new Command<User>(OnDeleteUser);
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor en el viewmodel {ex}", "OK");
            }
        }

        public async Task LoadUsers()
        {
            var usersresponse = await userApiService.GetUsersAsync();

            if (usersresponse.Users.Count() != 0)
            {
                UsersList.Clear();
                foreach (var user in usersresponse.Users)
                {
                    usersList.Add(user);
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Información", "No se encontraron usuarios.", "OK");
            }
        }

        private async Task OnAddUser()
        {
            //await App.Current.MainPage.DisplayAlert("Agregar", "Abrir formulario para agregar usuario.", "OK");
            await GoToUserFormPage();
        }

        private async void OnEditUser(User user)
        {
            if (user == null) return;
            await App.Current.MainPage.DisplayAlert("Editar", $"Editar usuario: {user.FirstName}", "OK");
        }

        private async void OnDeleteUser(User user)
        {
            if (user == null) return;

            bool confirm = await App.Current.MainPage.DisplayAlert("Eliminar",
                $"¿Seguro que deseas eliminar a {user.FirstName + " " + user.LastName}?", "Sí", "No");

            if (confirm)
            {
                UsersList.Remove(user);
            }
        }

        private async Task GoToUserFormPage() => await Shell.Current.GoToAsync("//UserFormPage");
    }
}
