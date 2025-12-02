using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class UserViewModel : GlobalService
    {
        private ObservableCollection<UserResponse> usersList = new();
        private readonly UserApiService userApiService;

        public Command AddUserCommand { get; }
        public Command EditUserCommand { get; }
        public Command DeleteUserCommand { get; }
        public Command ViewUserCommand { get; }

        public ObservableCollection<UserResponse> UsersList
        {
            get => usersList;
            set { usersList = value; OnPropertyChanged(); }
        }

        public UserViewModel()
        {
            userApiService = new UserApiService();

            AddUserCommand = new Command(async () => await OnAddUser());
            EditUserCommand = new Command<UserResponse>(OnEditUser);
            DeleteUserCommand = new Command<UserResponse>(OnDeleteUser);
            ViewUserCommand = new Command<UserResponse>(OnViewUser);
        }

        public async Task LoadUsers(bool isBusy)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver usuarios.");
                return;
            }

            IsBusy = isBusy;
            UsersList.Clear();

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await userApiService.GetUsersAsync();

            foreach (var u in response.OrderBy(x => x.NombreCompletoUsuario))
                UsersList.Add(u);

            IsBusy = false;
        }

        private async Task OnAddUser()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar usuarios.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "User", new UserRequest(new UserResponse()) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }

        private async void OnEditUser(UserResponse user)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar usuarios.");
                return;
            }

            if (user == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "User", new UserRequest(user) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }

        private async void OnDeleteUser(UserResponse user)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar usuarios.");
                return;
            }

            if (user == null) return;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar",
                $"¿Seguro que deseas eliminar a {user.NombreCompletoUsuario}?",
                "Sí", "No");

            if (!confirm) return;

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                return;
            }

            var ok = await userApiService.DeleteUserAsync(new UserRequest(user));

            if (ok)
            {
                await MostrarToastAsync("Usuario eliminado.");
                await LoadUsers(true);
            }
            else
            {
                await MostrarToastAsync("No se pudo eliminar el usuario.");
            }
        }

        private async void OnViewUser(UserResponse user)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver detalles.");
                return;
            }

            if (user == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "User", new UserRequest(user) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }
    }
}
