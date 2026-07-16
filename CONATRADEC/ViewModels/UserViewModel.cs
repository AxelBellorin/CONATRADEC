using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class UserViewModel : GlobalService
    {
        private ObservableCollection<UserResponse> usersList = new();
        private readonly UserApiService userApiService;
        private bool cargandoUsuarios;
        private bool eliminandoUsuario;

        public Command AddUserCommand { get; }
        public Command EditUserCommand { get; }
        public Command DeleteUserCommand { get; }
        public Command ViewUserCommand { get; }

        public ObservableCollection<UserResponse> UsersList
        {
            get => usersList;
            set
            {
                if (ReferenceEquals(usersList, value))
                    return;

                usersList = value;
                OnPropertyChanged();
            }
        }

        public UserViewModel()
            : this(new UserApiService())
        {
        }

        public UserViewModel(UserApiService userApiService)
        {
            this.userApiService = userApiService
                ?? throw new ArgumentNullException(nameof(userApiService));

            AddUserCommand = new Command(
                async () => await OnAddUserAsync());

            EditUserCommand = new Command<UserResponse>(
                async user => await OnEditUserAsync(user));

            DeleteUserCommand = new Command<UserResponse>(
                async user => await OnDeleteUserAsync(user));

            ViewUserCommand = new Command<UserResponse>(
                async user => await OnViewUserAsync(user));
        }

        public async Task LoadUsers(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver usuarios.");
                return;
            }

            if (cargandoUsuarios)
                return;

            cargandoUsuarios = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await userApiService.GetUsersResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                UsersList = new ObservableCollection<UserResponse>(
                    (resultado.Data ?? new ObservableCollection<UserResponse>())
                    .OrderBy(x => x.NombreCompletoUsuario ?? string.Empty));

                if (UsersList.Count == 0)
                    await MostrarToastAsync("No se encontraron usuarios.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los usuarios.");
            }
            finally
            {
                cargandoUsuarios = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private async Task OnAddUserAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar usuarios.");
                return;
            }

            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "User", new UserRequest(new UserResponse()) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }

        private async Task OnEditUserAsync(UserResponse? user)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar usuarios.");
                return;
            }

            if (IsBusy || user == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "User", new UserRequest(user) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }

        private async Task OnViewUserAsync(UserResponse? user)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || user == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "User", new UserRequest(user) }
            };

            await GoToAsyncParameters("//UserFormPage", parameters);
        }

        private async Task OnDeleteUserAsync(UserResponse? user)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar usuarios.");
                return;
            }

            if (IsBusy || eliminandoUsuario || user == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
                "Eliminar usuario",
                $"¿Desea eliminar a '{user.NombreCompletoUsuario}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            eliminandoUsuario = true;
            IsBusy = true;

            try
            {
                var resultado = await userApiService.DeleteUserResultAsync(
                    new UserRequest(user));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                UsersList.Remove(user);
                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Usuario eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el usuario.");
            }
            finally
            {
                eliminandoUsuario = false;
                IsBusy = false;
            }
        }
    }
}
