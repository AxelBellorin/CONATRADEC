using CONATRADEC.Models;               // Modelos: LoginRequest, LoginResponse
using CONATRADEC.Services;             // Servicio HTTP: LoginApiService
using CONATRADEC.Views;                // Vistas para navegación (MainPage)
using System.ComponentModel;           // Soporte de notificación (heredado en GlobalService)
using System.Runtime.CompilerServices; // CallerMemberName (si lo usa GlobalService)
using System.Windows.Input;            // ICommand / Command
using System.Net.Http;                 // HttpRequestException (errores de red)
using System.Threading.Tasks;          // Task/async/await
using Microsoft.Maui.Storage;          // Preferences y SecureStorage
using Microsoft.Maui.Controls;         // Application, Page, Command
using Plugin.Fingerprint;              // Plugin de biometría (huella/Face ID)
using Plugin.Fingerprint.Abstractions; // Tipos del plugin (AuthenticationRequestConfiguration)

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// ViewModel de la pantalla de Login.
    /// - Mantiene tu flujo original de autenticación.
    /// - "Recordarme" con Preferences/SecureStorage.
    /// - Huella/Face ID con Plugin.Fingerprint.
    /// - NUEVO: oculta el botón "Iniciar Sesión" cuando Recordarme + Usar Huella estén activos
    ///   y el dispositivo soporte biometría.
    /// </summary>
    public class LoginViewModel : GlobalService
    {
        // ==================== Estado/Base ====================
        private string username;                       // Usuario que digita el cliente
        private string password;                       // Contraseña digitada
        private string message;                        // Mensajes de estado/errores
        private bool isBusy;                           // Bandera de operación en curso
        private string urlimage = "logoconatradec";    // Recurso de imagen (logo)
        private bool isPasswordHidden = true;          // Ocultar/mostrar contraseña en Entry
        private string passwordToggleIcon = "eye.png"; // Ícono del ojo
        private LoginResponse user;                    // Usuario autenticado (si lo usas)
        private bool rememberMe;                       // Guardar sesión local (user + pass/token)

        // ==================== Biometría ====================
        private bool canUseBiometrics;   // Hardware+config del SO disponibles (huella/Face ID)
        private bool useBiometrics;      // Preferencia del usuario (Switch en la UI)

        // ==================== Comandos (UI) ====================
        public Command TogglePasswordCommand { get; }   // Alternar visibilidad de password
        public Command LoginCommand { get; }            // Login normal (usuario/contraseña)
        public Command BiometricLoginCommand { get; }   // Login vía prompt biométrico
        public ICommand OnTogglePasswordClickedCommand { get; } // (Reservado si lo usas en XAML)

        // Servicio HTTP de autenticación
        private readonly LoginApiService apiServiceLogin;

        // ==================== Claves de almacenamiento ====================
        private const string KeyRemember = "login.remember";        // bool: recuerda sesión
        private const string KeyUser = "login.username";        // string: último usuario
        private const string KeyToken = "auth.token";            // string: token (recomendado)
        private const string KeyPass = "login.password";        // string: pass temporal (mientras no hay token)
        private const string KeyUseBiometrics = "login.use_biometrics";  // bool: preferencia del Switch

        // ==================== CTOR ====================
        public LoginViewModel()
        {
            apiServiceLogin = new LoginApiService();

            // Comando principal de login: habilitado cuando !IsBusy
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);

            // Alterna icono/flag de contraseña
            TogglePasswordCommand = new Command(() => OnTogglePassword());

            // Prompt biométrico (solo si se cumple la condición compuesta)
            BiometricLoginCommand = new Command(async () => await TryBiometricLoginAsync(), () => BiometricEnabled);

            // Cargar preferencias y detectar soporte de biometría (no bloquea UI)
            _ = LoadSavedAsync();
        }

        // ==================== Propiedades Bindables ====================

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

        /// <summary>
        /// Oculta la propiedad IsBusy del GlobalService para recalcular comandos locales.
        /// </summary>
        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                base.IsBusy = value;
                // Recalcular habilitación del botón "Iniciar Sesión"
                LoginCommand.ChangeCanExecute();
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
            set => user = value; // No se usa en bindings
        }

        /// <summary>
        /// Marca si se debe recordar la sesión en el dispositivo.
        /// Si se desactiva, se apaga la preferencia "UseBiometrics".
        /// </summary>
        public bool RememberMe
        {
            get => rememberMe;
            set
            {
                rememberMe = value;
                OnPropertyChanged();

                // Si el usuario desactiva "Recordarme", apagamos biometría (no tiene sentido sin sesión local)
                if (!rememberMe && UseBiometrics)
                    UseBiometrics = false;

                // Notificar propiedades dependientes
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));   // <- actualizar visibilidad del botón
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// True si el dispositivo soporta biometría y el usuario la configuró (huellas/Face ID).
        /// </summary>
        public bool CanUseBiometrics
        {
            get => canUseBiometrics;
            set
            {
                canUseBiometrics = value;
                OnPropertyChanged();
                // Propiedades calculadas dependientes:
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));   // <- actualizar visibilidad del botón
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// Preferencia del usuario (Switch): desea usar biometría.
        /// </summary>
        public bool UseBiometrics
        {
            get => useBiometrics;
            set
            {
                useBiometrics = value;
                Preferences.Set(KeyUseBiometrics, value);
                OnPropertyChanged();
                // Propiedades calculadas dependientes:
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));   // <- actualizar visibilidad del botón
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// Condición compuesta para habilitar el botón de biometría:
        /// - Hardware biométrico disponible (CanUseBiometrics)
        /// - Recordarme activo (RememberMe)
        /// - Switch "Usar huella/Face ID" activo (UseBiometrics)
        /// </summary>
        public bool BiometricEnabled => CanUseBiometrics && RememberMe && UseBiometrics;

        /// <summary>
        /// NUEVO: Visibilidad del botón "Iniciar Sesión".
        /// - Si el usuario activó Recordarme + Usar Huella y el dispositivo soporta biometría,
        ///   entonces ocultamos el botón de login (se usará el botón de huella).
        /// - En cualquier otro caso, el botón de login se muestra.
        /// </summary>
        public bool LoginButtonVisible => !(RememberMe && UseBiometrics && CanUseBiometrics);

        // ==================== Login normal (usuario/contraseña) ====================
        public async Task LoginAsync()
        {
            if (IsBusy) return; // Evita reentradas

            var userTrim = Username?.Trim();

            // Validaciones mínimas
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

            if (Password.Length < 4)
            {
                await Application.Current.MainPage.DisplayAlert("Contraseña inválida", "Debe tener al menos 4 caracteres.", "OK");
                return;
            }

            IsBusy = true;
            Message = string.Empty;

            try
            {
                // Construcción del payload de login
                var req = new LoginRequest
                {
                    Username = userTrim,
                    Password = Password,
                    ExpiresInMins = 60
                };

                // Llamada a tu API
                var resp = await apiServiceLogin.LoginAsync(req);

                // === Persistencia local de sesión (según RememberMe) ===
                try
                {
                    if (RememberMe)
                    {
                        Preferences.Set(KeyRemember, true);
                        Preferences.Set(KeyUser, userTrim);

                        // RECOMENDADO cuando la API devuelva token:
                        // await SecureStorage.SetAsync(KeyToken, resp.Token ?? string.Empty);

                        // TEMPORAL: mientras no haya token, puedes guardar password (menos seguro)
                        await SecureStorage.SetAsync(KeyPass, Password);
                    }
                    else
                    {
                        // Limpia todo si no se desea recordar
                        Preferences.Remove(KeyRemember);
                        Preferences.Remove(KeyUser);
                        SecureStorage.Remove(KeyToken);
                        SecureStorage.Remove(KeyPass);
                        Preferences.Remove(KeyUseBiometrics);
                        UseBiometrics = false; // apagar switch
                    }
                }
                catch
                {
                    // No bloquear el login si falla el almacenamiento (ej. keystore)
                }

                // Mensaje de bienvenida (tu lógica original)
                Message = $"Bienvenido {resp.FirstName} {resp.LastName}.";
                await Application.Current.MainPage.DisplayAlert("Login Correcto", $"{Message}", "OK");

                // Si no se va a recordar, limpia campos
                if (!RememberMe)
                {
                    Username = "";
                    Password = "";
                }

                // Navegación principal (tu flujo original)
                await GoToAsyncParameters("//MainPage");

                // Si MainPage tiene VM con IsBusy, lo baja (tu lógica original)
                if (Shell.Current.CurrentPage is MainPage page &&
                    page.BindingContext is MainPageViewModel vm)
                {
                    vm.IsBusy = false;
                }
            }
            catch (TaskCanceledException)
            {
                Message = "La solicitud tardó demasiado. Revise su conexión e intente nuevamente.";
                await Application.Current.MainPage.DisplayAlert("Tiempo de espera", Message, "OK");
            }
            catch (HttpRequestException)
            {
                Message = "No fue posible conectarse al servidor. Verifique su internet.";
                await Application.Current.MainPage.DisplayAlert("Conexión fallida", Message, "OK");
            }
            catch (Exception)
            {
                Message = "Correo o contraseña incorrecta. Intente nuevamente.";
                await Application.Current.MainPage.DisplayAlert("Credenciales incorrectas", Message, "OK");

                // Limpieza para nuevo intento
                Username = "";
                Password = "";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ==================== Alternar visibilidad de contraseña ====================
        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }

        // ==================== Carga de preferencias guardadas + biometría ====================
        public async Task LoadSavedAsync()
        {
            try
            {
                // 1) Carga bandera "Recordarme" y valores asociados
                RememberMe = Preferences.Get(KeyRemember, false);
                if (RememberMe)
                {
                    // Usuario guardado
                    Username = Preferences.Get(KeyUser, string.Empty);

                    // Mientras no uses token, puedes completar password desde SecureStorage
                    var savedPass = await SecureStorage.GetAsync(KeyPass);
                    if (!string.IsNullOrEmpty(savedPass))
                        Password = savedPass;
                }

                // 2) Carga preferencia del Switch biométrico
                UseBiometrics = Preferences.Get(KeyUseBiometrics, false);

                // 3) Detecta si el dispositivo realmente soporta biometría y está configurada
                CanUseBiometrics = await CrossFingerprint.Current.IsAvailableAsync(true);
            }
            catch
            {
                // Si falla (p.ej. keystore/permiso), no bloquees la UI
                CanUseBiometrics = false;
            }
            finally
            {
                // Notifica que pueden cambiar visibilidades/habilitaciones dependientes
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible)); // <- actualizar visibilidad del botón
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        // ==================== Flujo de login con huella/Face ID ====================
        private async Task TryBiometricLoginAsync()
        {
            // (Re)verifica disponibilidad por si cambió en caliente
            var available = await CrossFingerprint.Current.IsAvailableAsync(true);
            if (!available)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Biometría no disponible",
                    "El dispositivo no tiene biometría o no está configurada.",
                    "OK");
                CanUseBiometrics = false; // Actualiza para ocultar botón si aplica
                return;
            }

            // Prompt nativo del sistema (Android: BiometricPrompt, iOS: Face/Touch ID)
            var reason = new AuthenticationRequestConfiguration(
                "Confirma tu identidad en tu dispositivo",
                "Autentícate con huella/Face ID para continuar");

            // Lanza el diálogo nativo
            var result = await CrossFingerprint.Current.AuthenticateAsync(reason);
            if (!result.Authenticated) return; // Canceló o falló autenticación

            // ---- Ruta recomendada: usar token si ya lo guardas ----
            var token = await SecureStorage.GetAsync(KeyToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    // Si tu API soporta validar/renovar token, hazlo aquí:
                    // var ok = await apiServiceLogin.RefreshAsync(token);
                    // if (!ok) { await Application.Current.MainPage.DisplayAlert("Sesión", "Token inválido", "OK"); return; }

                    // Si todo OK, navega directo
                    await GoToAsyncParameters("//MainPage");
                    return;
                }
                catch
                {
                    await Application.Current.MainPage.DisplayAlert("Sesión", "No fue posible validar el token.", "OK");
                    // Si falla, intenta la ruta con contraseña guardada (abajo)
                }
            }

            // ---- Ruta temporal: usar password guardada (mientras no uses token) ----
            var savedPass = await SecureStorage.GetAsync(KeyPass);
            var savedUser = Preferences.Get(KeyUser, string.Empty);

            // Si hay credenciales guardadas, reusa tu propio flujo normal de login
            if (!string.IsNullOrWhiteSpace(savedUser) && !string.IsNullOrWhiteSpace(savedPass))
            {
                Username = savedUser;
                Password = savedPass;
                await LoginAsync(); // no duplicamos lógica
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Sesión",
                    "No hay sesión guardada para desbloquear con biometría.",
                    "OK");
            }
        }
    }
}