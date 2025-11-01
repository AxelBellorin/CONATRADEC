using CONATRADEC.Models;         // Importa los modelos (LoginRequest, LoginResponse, etc.).
using CONATRADEC.Services;       // Importa los servicios (LoginApiService).
using CONATRADEC.Views;          // Importa las Views (para navegación basada en Shell).
using CONATRADEC.ViewModels;     // Importa otros ViewModels (p.ej., MainPageViewModel).
using System.ComponentModel;     // Soporte para INotifyPropertyChanged (implementado en GlobalService).
using System.Runtime.CompilerServices; // CallerMemberName en OnPropertyChanged (heredado).
using System.Windows.Input;      // Tipos de Command/ICommand para la UI.
using System.Net.Http;           // Para HttpRequestException (manejo de errores de red).
using System.Threading.Tasks;    // Para TaskCanceledException (timeouts/cancelaciones).

namespace CONATRADEC.ViewModels
{
    // ===========================================================
    // ===================== LoginViewModel =======================
    // ===========================================================
    // ViewModel para la pantalla de Login.
    // Hereda de GlobalService para reutilizar:
    // - Navegación con Shell (GoToAsyncParameters).
    // - Notificación de propiedades (OnPropertyChanged).
    public class LoginViewModel : GlobalService
    {
        // ===========================================================
        // ============= ESTADO / PROPIEDADES BINDABLE ===============
        // ===========================================================

        private string username;                        // Usuario (capturado desde la vista).
        private string password;                        // Contraseña (capturada desde la vista).
        private string message;                         // Mensaje informativo o de error.
        private bool isBusy;                            // (No se usa directamente: se delega a base.IsBusy con 'new').
        private string urlimage = "logoconatradec";     // Recurso de imagen para la UI (logo).
        private bool isPasswordHidden = true;           // Control de visibilidad del campo Password.
        private string passwordToggleIcon = "eye.png";  // Ícono para alternar visibilidad (ojo abierto/cerrado).
        private LoginResponse user;                     // Respuesta del login (datos del usuario autenticado).

        // Comandos que la vista puede invocar (botones).
        public Command TogglePasswordCommand { get; }            // Alterna visibilidad del Password.
        public Command LoginCommand { get; }                     // Ejecuta el flujo de autenticación.
        public ICommand OnTogglePasswordClickedCommand { get; }  // (Declarado para posibles bindings; no usado aquí).

        // Servicio HTTP responsable de la autenticación.
        private readonly LoginApiService apiServiceLogin;

        // ===========================================================
        // ========================= CTOR ============================
        // ===========================================================
        public LoginViewModel()
        {
            // Crea la instancia del servicio (futuro: inyectarlo por DI con AddHttpClient).
            apiServiceLogin = new LoginApiService();

            // Comando de login: ejecuta LoginAsync y se habilita si !IsBusy.
            // El CanExecute se recalcula cuando cambia IsBusy (ver propiedad 'new IsBusy').
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);

            // Comando para alternar visibilidad de la contraseña.
            TogglePasswordCommand = new Command(() => OnTogglePassword());
        }

        // ===========================================================
        // ========= PROPIEDADES PÚBLICAS CON NOTIFICACIÓN ===========
        // ===========================================================

        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => message;
            set { message = value; OnPropertyChanged(); }
        }

        public string urlImage
        {
            get => urlimage;
            set { urlimage = value; OnPropertyChanged(); }
        }

        public string PasswordToggleIcon
        {
            get => passwordToggleIcon;
            set { passwordToggleIcon = value; OnPropertyChanged(); }
        }

        // Oculta la propiedad IsBusy de GlobalService con 'new' para añadir lógica local
        // sin perder la del GlobalService (ChangeCanExecute de comandos globales).
        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                base.IsBusy = value;             // Mantiene comportamiento base (OnPropertyChanged + comandos globales).
                LoginCommand.ChangeCanExecute(); // Además recalcula el CanExecute del botón Login.
            }
        }

        public bool IsPasswordHidden
        {
            get => isPasswordHidden;
            set { isPasswordHidden = value; OnPropertyChanged(); }
        }

        public LoginResponse User
        {
            get => user;
            set => user = value; // No hay binding directo en la vista; no se notifica.
        }

        // ===========================================================
        // ===================== ACCIÓN: LOGIN =======================
        // ===========================================================
        // Ejecuta el inicio de sesión invocando el servicio HTTP.
        public async Task LoginAsync()
        {
            if (IsBusy) return; // Evita reentradas (doble tap rápido).

            // -------------------------------------------------------
            // ✅ VALIDACIONES PREVIAS (ANTES DE LLAMAR AL SERVICIO)
            // -------------------------------------------------------
            // Nota: estas validaciones brindan feedback inmediato y evitan
            // hacer la llamada HTTP cuando faltan datos.
            var userTrim = Username?.Trim();  // Limpieza mínima de espacios.

            if (string.IsNullOrWhiteSpace(userTrim))
            {
                await Application.Current.MainPage.DisplayAlert("Faltan datos", "Ingrese su usuario.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Faltan datos", "Ingrese su contraseña.", "OK");
                return;
            }

            // (Opcional) Regla mínima de seguridad para contraseña.
            if (Password.Length < 4)
            {
                await Application.Current.MainPage.DisplayAlert("Contraseña inválida", "Debe tener al menos 4 caracteres.", "OK");
                return;
            }

            IsBusy = true;          // Marca comienzo de operación; deshabilita el botón Login.
            Message = string.Empty; // Limpia mensaje previo.

            try
            {
                // Construye el payload para la API de autenticación.
                var req = new LoginRequest
                {
                    Username = userTrim,  // Usa el valor ya sanitizado.
                    Password = Password,
                    ExpiresInMins = 60
                };

                // Llama al servicio que realiza el POST /auth/login.
                var resp = await apiServiceLogin.LoginAsync(req);

                // Éxito: mensaje de bienvenida y limpieza de campos.
                Message = $"Bienvenido {resp.FirstName} {resp.LastName}.";
                Username = "";
                Password = "";

                await Application.Current.MainPage.DisplayAlert("Login Correcto", $"{Message}", "OK");

                // Navega a la pantalla principal.
                await GoToAsyncParameters("//MainPage");

                // Si MainPage tiene VM con IsBusy, lo ajusta al finalizar.
                if (Shell.Current.CurrentPage is MainPage page &&
                    page.BindingContext is MainPageViewModel vm)
                {
                    vm.IsBusy = false;
                }
            }
            // -------------------------------------------------------
            // ✅ MANEJO DE ERRORES MÁS CLARO (CATCH ESPECÍFICOS)
            // -------------------------------------------------------
            catch (TaskCanceledException)
            {
                // Puede indicar timeout si el HttpClient.Timeout se alcanzó.
                Message = "La solicitud tardó demasiado. Revise su conexión e intente nuevamente.";
                await Application.Current.MainPage.DisplayAlert("Tiempo de espera", Message, "OK");
            }
            catch (HttpRequestException)
            {
                // Problemas de conectividad o DNS/servidor no disponible.
                Message = "No fue posible conectarse al servidor. Verifique su internet.";
                await Application.Current.MainPage.DisplayAlert("Conexión fallida", Message, "OK");
            }
            catch (Exception)
            {
                // Error genérico (por ejemplo, credenciales incorrectas).
                Message = "Correo o contraseña incorrecta. Intente nuevamente.";
                await Application.Current.MainPage.DisplayAlert("Credenciales incorrectas", Message, "OK");

                // Limpia los campos tras error de autenticación.
                Username = "";
                Password = "";
            }
            finally
            {
                IsBusy = false; // Libera el estado ocupado siempre (éxito o error).
            }
        }

        // ===========================================================
        // ========= ACCIÓN: ALTERNAR VISIBILIDAD PASSWORD ===========
        // ===========================================================
        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;                 // Alterna true/false.
            PasswordToggleIcon = IsPasswordHidden ? "eye.png"     // Ícono si está oculto.
                                                  : "eyeoff.png"; // Ícono si está visible.
        }
    }
}
