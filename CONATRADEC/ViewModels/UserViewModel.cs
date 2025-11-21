using CONATRADEC.Models;             // Modelos: UserRP, UserRequest, FormMode, etc.
using CONATRADEC.Services;           // Servicios de red: UserApiService y GlobalService.
using System.Collections.ObjectModel; // Colecciones observables para la UI (CollectionView/ListView).
using System.Windows.Input;          // Command e ICommand para enlazar acciones a la UI.

namespace CONATRADEC.ViewModels
{
    // ===========================================================
    // ===================== UserViewModel =======================
    // ===========================================================
    // ViewModel principal para la página de listado de usuarios.
    // Se encarga de cargar la lista, manejar eventos de CRUD
    // (Crear, Editar, Eliminar, Ver) y coordinar la navegación.
    public class UserViewModel : GlobalService
    {
        // ===========================================================
        // ============ CAMPOS PRIVADOS Y DEPENDENCIAS ===============
        // ===========================================================

        private ObservableCollection<UserResponse> usersList = new();  // Lista observable enlazada a la UI.
        private readonly UserApiService userApiService;          // Servicio API encargado de obtener los usuarios.

        // ===========================================================
        // ===================== COMANDOS UI =========================
        // ===========================================================
        // Cada comando representa una acción en la interfaz (botón, gesto, etc.).
        public Command AddUserCommand { get; }      // Crear nuevo usuario.
        public Command EditUserCommand { get; }     // Editar usuario existente.
        public Command DeleteUserCommand { get; }   // Eliminar usuario.
        public Command ViewUserCommand { get; }     // Ver detalles de usuario.

        // Lista observable que se muestra en la interfaz (CollectionView, ListView, etc.).
        public ObservableCollection<UserResponse> UsersList
        {
            get => usersList;
            set { usersList = value; OnPropertyChanged(); } // Notifica a la UI cuando cambia.
        }

        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================
        public UserViewModel()
        {
            // Instancia el servicio de usuarios (simula o consume un API real).
            userApiService = new UserApiService();

            // Inicializa comandos con sus respectivos métodos.
            AddUserCommand = new Command(async () => await OnAddUser());
            EditUserCommand = new Command<UserResponse>(OnEditUser);
            DeleteUserCommand = new Command<UserResponse>(OnDeleteUser);
            ViewUserCommand = new Command<UserResponse>(OnViewUser);
        }

        // ===========================================================
        // ==================== CARGAR USUARIOS ======================
        // ===========================================================
        // Método llamado al cargar la página. Trae los usuarios desde el API.
        public async Task LoadUsers(bool isBusy)
        {
            IsBusy = isBusy; // Activa indicador visual (binding IsBusy en la vista).

            UsersList.Clear(); // Limpia lista anterior.

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            // Llama al servicio para obtener los usuarios.
            var usersresponse = await userApiService.GetUsersAsync();

            // Si la respuesta contiene usuarios, los ordena y actualiza la lista.
            if (usersresponse.Count() != 0)
            {
                // Ordena por nombre y agrega uno a uno.
                foreach (var user in usersresponse.OrderBy(r => r.NombreCompletoUsuario).ToList())
                {
                    UsersList.Add(user);
                }
            }
            else
            {
                // Si la lista viene vacía, muestra mensaje informativo.
                _ = MostrarToastAsync("Información" + "No se encontraron usuarios.");
            }

            IsBusy = false; // Libera el estado ocupado.
        }

        // ===========================================================
        // ====================== AGREGAR USUARIO ====================
        // ===========================================================
        private async Task OnAddUser()
        {
            try
            {
                // Define los parámetros que se enviarán al formulario de usuario.
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },     // Modo de creación.
                    { "User", new UserRequest(new UserResponse()) }       // Objeto vacío para inicializar.
                };

                // Navega a la página de formulario (UserFormPage) pasando los parámetros.
                await GoToAsyncParameters("//UserFormPage", parameters);
            }
            catch (Exception ex)
            {
                // Muestra error si no logra conectar con el servicio o Shell.
                _ = MostrarToastAsync("Error" + $"No se pudo conectar al servidor {ex}");
            }
        }

        // ===========================================================
        // ====================== EDITAR USUARIO =====================
        // ===========================================================
        private async void OnEditUser(UserResponse user)
        {
            try
            {
                // Si no se seleccionó ningún usuario, termina.
                if (user == null) return;

                // Define los parámetros con el modo "Editar" y los datos del usuario.
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "User", new UserRequest(user) } // Convierte el UserRP en UserRequest.
                };

                // Navega al formulario de usuario en modo edición.
                await GoToAsyncParameters("//UserFormPage", parameters);
            }
            catch (Exception ex)
            {
                // Captura y muestra errores (por ejemplo, problemas con Shell o datos corruptos).
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        // ===========================================================
        // ===================== ELIMINAR USUARIO ====================
        // ===========================================================
        private async void OnDeleteUser(UserResponse user)
        {
            try
            {
                // Si no hay usuario seleccionado, finaliza.
                if (user == null) return;

                // Solicita confirmación al usuario antes de eliminar.
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar a {user.NombreCompletoUsuario} ?",
                    "Sí",
                    "No");

                // Si confirma, elimina el usuario de la lista local (sin API real).
                if (confirm)
                {
                    // Valida que el usaurio tenga conexion a internet
                    bool tieneInternet = await TieneInternetAsync();

                    if (!tieneInternet)
                    {
                        _ = MostrarToastAsync("Sin conexión a internet.");
                        IsBusy = false;
                        return;
                    }

                    // Llama al servicio de eliminación. (Asegúrate que el método exista con el nombre exacto.)
                    var response = await userApiService.DeleteUserAsync(new UserRequest(user));

                    if (response)
                    {
                        _ = MostrarToastAsync("Éxito" + "Usuario eliminado correctamente");
                        _= LoadUsers(IsBusy); // Recarga la lista. (IsBusy es true en este punto.)
                    }
                    else
                    {
                        _ = MostrarToastAsync("Error" + "El usuario no se pudo eliminar, intente nuevamente");
                    }
                }
                else
                {
                    IsBusy = false; // Si canceló, restablece la UI manualmente.
                }
            }
            catch (Exception ex)
            {
                // Muestra cualquier error ocurrido durante el proceso.
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        // ===========================================================
        // ====================== VER DETALLES =======================
        // ===========================================================
        private async void OnViewUser(UserResponse user)
        {
            // Prepara parámetros para abrir la vista de detalles en modo "View".
            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "User", new UserRequest(user) }
            };

            // Navega al formulario en modo solo lectura.
            await GoToAsyncParameters("//UserFormPage", parameters);
        }
    }
}
