using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    public class LoginViewModel : GlobalService
    {
        // ==================== Estado/Base ====================
        private string username;
        private string password;
        private string message;
        private bool isBusy;
        private string urlimage = "logoconatradec";
        private bool isPasswordHidden = true;
        private string passwordToggleIcon = "eye.png";
        private LoginResponse user;
        private bool rememberMe;
        private bool titlebiometric;

        // ==================== Biometría ====================
        private bool canUseBiometrics;      // HW/config del SO ok
        private bool useBiometrics;         // Preferencia del usuario (Switch)
        private bool requirePasswordRelogin; // NUEVO: bloquea biometría hasta login con password
        private bool loginViaBiometric;     // para saber si el login fue biométrico o no

        // ==================== Comandos (UI) ====================
        public Command TogglePasswordCommand { get; }
        public Command LoginCommand { get; }
        public Command BiometricLoginCommand { get; }
        public ICommand OnTogglePasswordClickedCommand { get; }

        // Servicio HTTP de autenticación
        private readonly LoginApiService apiServiceLogin;

        // ==================== Claves de almacenamiento ====================
        private const string KeyRemember = "login.remember";
        private const string KeyUser = "login.username";
        private const string KeyToken = "auth.token";
        private const string KeyPass = "login.password";
        private const string KeyUseBiometrics = "login.use_biometrics";
        private const string KeyRequireRelogin = "login.require_pwd_relogin"; // NUEVO

        // ==================== CTOR ====================
        public LoginViewModel()
        {
            apiServiceLogin = new LoginApiService();

            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            TogglePasswordCommand = new Command(() => OnTogglePassword());
            BiometricLoginCommand = new Command(async () => await TryBiometricLoginAsync(), () => BiometricEnabled);

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

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                base.IsBusy = value;
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
            set => user = value;
        }

        public bool RememberMe
        {
            get => rememberMe;
            set
            {
                rememberMe = value;
                OnPropertyChanged();

                // Si desactiva "Recordarme", apagamos biometría sin bloquear relogin forzoso
                if (!rememberMe && UseBiometrics)
                {
                    // Apaga el switch, pero NO exigimos relogin por password en este caso
                    useBiometrics = false;
                    Preferences.Remove(KeyUseBiometrics);
                    OnPropertyChanged(nameof(UseBiometrics));
                }

                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                OnPropertyChanged(nameof(TitleBiometric));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        public bool CanUseBiometrics
        {
            get => canUseBiometrics;
            set
            {
                canUseBiometrics = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// NUEVO: si estaba encendido y lo apagan, obliga a pedir contraseña de nuevo
        /// y bloquea volver a encender hasta que se haga login con password.
        /// </summary>
        public bool UseBiometrics
        {
            get => useBiometrics;
            set
            {
                // Detecta transición ON -> OFF
                bool turningOff = useBiometrics && !value;

                useBiometrics = value;
                Preferences.Set(KeyUseBiometrics, value);
                OnPropertyChanged();

                if (turningOff)
                {
                    // 1) Bloquea re-encendido hasta relogin por password
                    RequirePasswordRelogin = true;

                    // 2) Borra credenciales sensibles
                    SecureStorage.Remove(KeyPass);

                    // 3) Limpia el Entry y vuelve a ocultar
                    Password = string.Empty;
                    IsPasswordHidden = true;

                    // (opcional) aviso de UX
                    // _ = Application.Current.MainPage.DisplayAlert("Seguridad", "Vuelve a iniciar sesión con tu contraseña para reactivar la huella.", "OK");
                }

                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// NUEVO: marca si debe forzarse login por password antes de habilitar biometría otra vez.
        /// </summary>
        public bool RequirePasswordRelogin
        {
            get => requirePasswordRelogin;
            private set
            {
                requirePasswordRelogin = value;
                Preferences.Set(KeyRequireRelogin, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        /// <summary>
        /// Botón de huella visible/habilitado solo si:
        /// - HW biométrico OK
        /// - Recordarme activo
        /// - Switch ON
        /// - Y NO hay bloqueo por relogin de password pendiente
        /// </summary>
        public bool BiometricEnabled => CanUseBiometrics && RememberMe && UseBiometrics && !RequirePasswordRelogin;

        /// <summary>
        /// Botón "Iniciar Sesión" se oculta sólo si la biometría está lista para usarse.
        /// </summary>
        public bool LoginButtonVisible => !BiometricEnabled;

        /// <summary>
        /// NUEVO: Habilitación del Switch en la UI. 
        /// No se puede re-encender mientras RequirePasswordRelogin sea true.
        /// </summary>
        public bool CanToggleBiometrics => CanUseBiometrics && !RequirePasswordRelogin;

        public bool TitleBiometric => !RememberMe;

        // ==================== Login normal (usuario/contraseña) ====================
        public async Task LoginAsync()
        {
            if (IsBusy) return;

            var userTrim = Username?.Trim();

            if (string.IsNullOrWhiteSpace(userTrim))
            {
                _ = MostrarToastAsync("Ingrese su usuario.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                _ = MostrarToastAsync("Ingrese su contraseña.");
                return;
            }

            if (Password.Length < 4)
            {
                _ = MostrarToastAsync("Debe tener al menos 4 caracteres.");
                return;
            }

            IsBusy = true;
            Message = string.Empty;

            try
            {
                var req = new LoginRequest
                {
                    Username = userTrim,
                    Password = Password,
                    ExpiresInMins = 60
                };

                var resp = await apiServiceLogin.LoginAsync(req);

                // === Persistencia local de sesión (según RememberMe) ===
                try
                {
                    if (RememberMe)
                    {
                        Preferences.Set(KeyRemember, true);
                        Preferences.Set(KeyUser, userTrim);

                        // RECOMENDADO: guardar token cuando tu API lo devuelva
                        // await SecureStorage.SetAsync(KeyToken, resp.Token ?? string.Empty);

                        // Mientras tanto: guardar password (menos seguro)
                        await SecureStorage.SetAsync(KeyPass, Password);
                    }
                    else
                    {
                        Preferences.Remove(KeyRemember);
                        Preferences.Remove(KeyUser);
                        SecureStorage.Remove(KeyToken);
                        SecureStorage.Remove(KeyPass);
                        Preferences.Remove(KeyUseBiometrics);
                        UseBiometrics = false; // apagar switch
                    }
                }
                catch { /* no romper flujo si falla storage */ }

                // Si el login fue con contraseña (NO biométrico), libera el bloqueo
                if (!loginViaBiometric)
                {
                    RequirePasswordRelogin = false;
                }

                Message = $"Bienvenido {resp.FirstName} {resp.LastName}.";

                _= MostrarToastAsync(Message);

                if (!RememberMe)
                {
                    Username = "";
                    Password = "";
                }

                await GoToAsyncParameters("//MainPage");

                if (Shell.Current.CurrentPage is MainPage page &&
                    page.BindingContext is MainPageViewModel vm)
                {
                    vm.IsBusy = false;
                }
            }
            catch (TaskCanceledException)
            {
                Message = "La solicitud tardó demasiado. Revise su conexión e intente nuevamente.";
                _ = MostrarToastAsync(Message);
            }
            catch (HttpRequestException)
            {
                Message = "No fue posible conectarse al servidor. Verifique su internet.";
                _ = MostrarToastAsync(Message);
            }
            catch
            {
                Message = "Correo o contraseña incorrecta. Intente nuevamente.";
                _ = MostrarToastAsync(Message);
                Username = "";
                Password = "";
            }
            finally
            {
                loginViaBiometric = false; // reset
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
                RememberMe = Preferences.Get(KeyRemember, false);
                if (RememberMe)
                {
                    Username = Preferences.Get(KeyUser, string.Empty);

                    var savedPass = await SecureStorage.GetAsync(KeyPass);
                    if (!string.IsNullOrEmpty(savedPass))
                        Password = savedPass;
                }

                UseBiometrics = Preferences.Get(KeyUseBiometrics, false);
                RequirePasswordRelogin = Preferences.Get(KeyRequireRelogin, false);

                CanUseBiometrics = await CrossFingerprint.Current.IsAvailableAsync(true);
            }
            catch
            {
                CanUseBiometrics = false;
            }
            finally
            {
                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

        // ==================== Flujo de login con huella/Face ID ====================
        private async Task TryBiometricLoginAsync()
        {
            // Si está bloqueado por relogin de password, no permitir
            if (RequirePasswordRelogin)
                return;

            var available = await CrossFingerprint.Current.IsAvailableAsync(true);
            if (!available)
            {
                _ = MostrarToastAsync("El dispositivo no tiene biometría o no está configurada.");
                CanUseBiometrics = false;
                return;
            }

            var reason = new AuthenticationRequestConfiguration(
                "Confirma tu identidad en tu dispositivo",
                "Autentícate con huella/Face ID para continuar");

            var result = await CrossFingerprint.Current.AuthenticateAsync(reason);
            if (!result.Authenticated) return;

            // Marca que este login es biométrico (para no limpiar el bloqueo)
            loginViaBiometric = true;

            // Si ya usas token:
            var token = await SecureStorage.GetAsync(KeyToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    // var ok = await apiServiceLogin.RefreshAsync(token);
                    // if (!ok) { await Application.Current.MainPage.DisplayAlert("Sesión", "Token inválido", "OK"); return; }

                    await GoToAsyncParameters("//MainPage");
                    return;
                }
                catch
                {
                    _ = MostrarToastAsync("No fue posible validar el token.");
                }
            }

            // Ruta con password guardada (si existe)
            var savedPass = await SecureStorage.GetAsync(KeyPass);
            var savedUser = Preferences.Get(KeyUser, string.Empty);

            if (!string.IsNullOrWhiteSpace(savedUser) && !string.IsNullOrWhiteSpace(savedPass))
            {
                Username = savedUser;
                Password = savedPass;
                await LoginAsync();
            }
            else
            {
                _ = MostrarToastAsync("No hay sesión guardada para desbloquear con biometría.");
            }
        }
    }
}
