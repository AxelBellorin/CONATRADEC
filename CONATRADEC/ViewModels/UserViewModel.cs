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

        private ObservableCollection<UserRP> usersList = new();  // Lista observable enlazada a la UI.
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
        public ObservableCollection<UserRP> UsersList
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
            EditUserCommand = new Command<UserRP>(OnEditUser);
            DeleteUserCommand = new Command<UserRP>(OnDeleteUser);
            ViewUserCommand = new Command<UserRP>(OnViewUser);
        }

        // ===========================================================
        // ==================== CARGAR USUARIOS ======================
        // ===========================================================
        // Método llamado al cargar la página. Trae los usuarios desde el API.
        public async Task LoadUsers(bool isBusy)
        {
            IsBusy = isBusy; // Activa indicador visual (binding IsBusy en la vista).

            // Llama al servicio para obtener los usuarios.
            var usersresponse = await userApiService.GetUsersAsync();

            // Si la respuesta contiene usuarios, los ordena y actualiza la lista.
            if (usersresponse.Users.Count() != 0)
            {
                UsersList.Clear(); // Limpia lista anterior.

                // Ordena por nombre y agrega uno a uno.
                foreach (var user in usersresponse.Users.OrderBy(r => r.FirstName).ToList())
                {
                    UsersList.Add(user);
                }
            }
            else
            {
                // Si la lista viene vacía, muestra mensaje informativo.
                await App.Current.MainPage.DisplayAlert("Información", "No se encontraron usuarios.", "OK");
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
                    { "User", new UserRequest(new UserRP()) }       // Objeto vacío para inicializar.
                };

                // Navega a la página de formulario (UserFormPage) pasando los parámetros.
                await GoToAsyncParameters("//UserFormPage", parameters);
            }
            catch (Exception ex)
            {
                // Muestra error si no logra conectar con el servicio o Shell.
                await App.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor {ex}", "OK");
            }
        }

        // ===========================================================
        // ====================== EDITAR USUARIO =====================
        // ===========================================================
        private async void OnEditUser(UserRP user)
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
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        // ===========================================================
        // ===================== ELIMINAR USUARIO ====================
        // ===========================================================
        private async void OnDeleteUser(UserRP user)
        {
            try
            {
                // Si no hay usuario seleccionado, finaliza.
                if (user == null) return;

                // Solicita confirmación al usuario antes de eliminar.
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar a {user.FirstName + " " + user.LastName}?",
                    "Sí",
                    "No");

                // Si confirma, elimina el usuario de la lista local (sin API real).
                if (confirm)
                    UsersList.Remove(user);
            }
            catch (Exception ex)
            {
                // Muestra cualquier error ocurrido durante el proceso.
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        // ===========================================================
        // ====================== VER DETALLES =======================
        // ===========================================================
        private async void OnViewUser(UserRP user)
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
