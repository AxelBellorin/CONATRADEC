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
        private bool canUseBiometrics;
        private bool useBiometrics;
        private bool requirePasswordRelogin;
        private bool loginViaBiometric;

        // ==================== Comandos ====================
        public Command TogglePasswordCommand { get; }
        public Command LoginCommand { get; }
        public Command BiometricLoginCommand { get; }
        public ICommand OnTogglePasswordClickedCommand { get; }

        private readonly LoginApiService apiServiceLogin;

        // ==================== Storage Keys ====================
        private const string KeyRemember = "login.remember";
        private const string KeyUser = "login.username";
        private const string KeyToken = "auth.token";
        private const string KeyPass = "login.password";
        private const string KeyUseBiometrics = "login.use_biometrics";
        private const string KeyRequireRelogin = "login.require_pwd_relogin";

        // ==================== CTOR ====================
        public LoginViewModel()
        {
            apiServiceLogin = new LoginApiService();

            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            TogglePasswordCommand = new Command(() => OnTogglePassword());
            BiometricLoginCommand = new Command(async () => await TryBiometricLoginAsync(), () => BiometricEnabled);

            // ❌ ESTA LÍNEA CAUSABA LAG EN ANDROID — ELIMINADA
            // _ = LoadSavedAsync();
        }

        // ==================== Propiedades ====================
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

                if (!rememberMe && UseBiometrics)
                {
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

        public bool UseBiometrics
        {
            get => useBiometrics;
            set
            {
                bool turningOff = useBiometrics && !value;

                useBiometrics = value;
                Preferences.Set(KeyUseBiometrics, value);
                OnPropertyChanged();

                if (turningOff)
                {
                    RequirePasswordRelogin = true;
                    SecureStorage.Remove(KeyPass);
                    Password = string.Empty;
                    IsPasswordHidden = true;
                }

                OnPropertyChanged(nameof(BiometricEnabled));
                OnPropertyChanged(nameof(LoginButtonVisible));
                OnPropertyChanged(nameof(CanToggleBiometrics));
                BiometricLoginCommand.ChangeCanExecute();
            }
        }

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

        public bool BiometricEnabled => CanUseBiometrics && RememberMe && UseBiometrics && !RequirePasswordRelogin;
        public bool LoginButtonVisible => !BiometricEnabled;
        public bool CanToggleBiometrics => CanUseBiometrics && !RequirePasswordRelogin;
        public bool TitleBiometric => !RememberMe;

        // ==================== LOGIN NORMAL ====================
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
                    NombreUsuario = userTrim,
                    ClaveUsuario = Password,
                };

                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                var resp = await apiServiceLogin.LoginAsync(req);

                try
                {
                    if (RememberMe)
                    {
                        Preferences.Set(KeyRemember, true);
                        Preferences.Set(KeyUser, userTrim);

                        await SecureStorage.SetAsync(KeyPass, Password);
                    }
                    else
                    {
                        Preferences.Remove(KeyRemember);
                        Preferences.Remove(KeyUser);
                        SecureStorage.Remove(KeyToken);
                        SecureStorage.Remove(KeyPass);
                        Preferences.Remove(KeyUseBiometrics);
                        UseBiometrics = false;
                    }
                }
                catch { }

                if (!loginViaBiometric)
                    RequirePasswordRelogin = false;

                Message = $"Bienvenido {resp.NombreCompletoUsuario}";
                _ = MostrarToastAsync(Message);

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
                loginViaBiometric = false;
                IsBusy = false;
            }
        }

        // ==================== TOGGLE PASSWORD ====================
        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }

        // ==================== LOAD SAVED SETTINGS ====================
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

        // ==================== LOGIN BIOMÉTRICO ====================
        private async Task TryBiometricLoginAsync()
        {
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
            if (!result.Authenticated)
                return;

            loginViaBiometric = true;

            var token = await SecureStorage.GetAsync(KeyToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await GoToAsyncParameters("//MainPage");
                    return;
                }
                catch
                {
                    _ = MostrarToastAsync("No fue posible validar el token.");
                }
            }

            var savedPass = await SecureStorage.GetAsync(KeyPass);
            var savedUser = Preferences.Get(KeyUser, string.Empty);

            if (!string.IsNullOrWhiteSpace(savedUser) &&
                !string.IsNullOrWhiteSpace(savedPass))
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
