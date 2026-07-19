using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class LoginViewModel : GlobalService
    {
        private string username = string.Empty;
        private string password = string.Empty;
        private string message = string.Empty;
        private string urlimage = "logoconatradec";
        private bool isPasswordHidden = true;
        private string passwordToggleIcon = "eye.png";
        private LoginResponse? user;
        private bool rememberMe;

        private bool canUseBiometrics;
        private bool useBiometrics;
        private bool requirePasswordRelogin;
        private bool loginViaBiometric;

        private readonly LoginApiService apiServiceLogin;

        public Command TogglePasswordCommand { get; }
        public Command LoginCommand { get; }
        public Command BiometricLoginCommand { get; }

        private const string KeyRemember = "login.remember";
        private const string KeyUser = "login.username";
        private const string KeyPass = "login.password";
        private const string KeyUseBiometrics = "login.use_biometrics";
        private const string KeyRequireRelogin = "login.require_pwd_relogin";

        public LoginViewModel()
        {
            apiServiceLogin = new LoginApiService();

            LoginCommand = new Command(
                async () => await LoginAsync(),
                () => !IsBusy);

            TogglePasswordCommand = new Command(OnTogglePassword);

            BiometricLoginCommand = new Command(
                async () => await TryBiometricLoginAsync(),
                () => BiometricEnabled);
        }

        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => message;
            set
            {
                message = value;
                OnPropertyChanged();
            }
        }

        public string urlImage
        {
            get => urlimage;
            set
            {
                urlimage = value;
                OnPropertyChanged();
            }
        }

        public string PasswordToggleIcon
        {
            get => passwordToggleIcon;
            set
            {
                passwordToggleIcon = value;
                OnPropertyChanged();
            }
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
            set
            {
                isPasswordHidden = value;
                OnPropertyChanged();
            }
        }

        public LoginResponse? User
        {
            get => user;
            set
            {
                user = value;
                OnPropertyChanged();
            }
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

                ActualizarEstadoBiometrico();
                OnPropertyChanged(nameof(TitleBiometric));
            }
        }

        public bool CanUseBiometrics
        {
            get => canUseBiometrics;
            set
            {
                canUseBiometrics = value;
                OnPropertyChanged();
                ActualizarEstadoBiometrico();
            }
        }

        public bool UseBiometrics
        {
            get => useBiometrics;
            set
            {
                bool seEstaDesactivando = useBiometrics && !value;

                useBiometrics = value;
                Preferences.Set(KeyUseBiometrics, value);
                OnPropertyChanged();

                if (seEstaDesactivando)
                {
                    RequirePasswordRelogin = true;
                    SecureStorage.Remove(KeyPass);
                    Password = string.Empty;
                    IsPasswordHidden = true;
                }

                ActualizarEstadoBiometrico();
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
                ActualizarEstadoBiometrico();
            }
        }

        public bool BiometricEnabled =>
            CanUseBiometrics &&
            RememberMe &&
            UseBiometrics &&
            !RequirePasswordRelogin;

        public bool LoginButtonVisible => !BiometricEnabled;

        public bool CanToggleBiometrics =>
            CanUseBiometrics && !RequirePasswordRelogin;

        public bool TitleBiometric => !RememberMe;

        public async Task LoginAsync()
        {
            if (IsBusy)
                return;

            string? userTrim = Username?.Trim();

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
                _ = MostrarToastAsync(
                    "La contraseña debe tener al menos 4 caracteres.");
                return;
            }

            IsBusy = true;
            Message = string.Empty;

            try
            {
                var request = new LoginRequest
                {
                    NombreUsuario = userTrim,
                    ClaveUsuario = Password
                };

                LoginResponse response =
                    await apiServiceLogin.LoginAsync(request);

                User = response;

                PermissionService.Instance.Load(
                    response.permisos ??
                    new List<UserPermissionDTO>());

                await GuardarSesionAsync(userTrim, response);

                if (!loginViaBiometric)
                    RequirePasswordRelogin = false;

                Message =
                    $"Bienvenido {response.NombreCompletoUsuario}";

                _ = MostrarToastAsync(Message);

                if (!RememberMe)
                {
                    Username = string.Empty;
                    Password = string.Empty;
                }

                await GoToAsyncParameters("//MainPage");

                if (Shell.Current.CurrentPage is MainPage page &&
                    page.BindingContext is MainPageViewModel viewModel)
                {
                    viewModel.IsBusy = false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Message = "Usuario o contraseña incorrectos.";
                _ = MostrarToastAsync(Message);
                Password = string.Empty;
            }
            catch (TaskCanceledException)
            {
                Message =
                    "La solicitud tardó demasiado. " +
                    "Revise su conexión e intente nuevamente.";

                _ = MostrarToastAsync(Message);
            }
            catch (HttpRequestException ex)
            {
                Message = ex.StatusCode.HasValue
                    ? "El servidor presentó un problema. Intente nuevamente."
                    : "No fue posible conectarse al servidor. " +
                      "Verifique su conexión.";

                _ = MostrarToastAsync(Message);
            }
            catch (InvalidOperationException)
            {
                Message =
                    "El servidor respondió, pero no devolvió correctamente " +
                    "los datos del usuario.";

                _ = MostrarToastAsync(Message);
            }
            catch (Exception)
            {
                Message =
                    "Ocurrió un error inesperado al iniciar sesión.";

                _ = MostrarToastAsync(Message);
            }
            finally
            {
                loginViaBiometric = false;
                IsBusy = false;
            }
        }

        private async Task GuardarSesionAsync(
            string usuario,
            LoginResponse response)
        {
            try
            {
                // Estos datos pertenecen a la sesión activa y se guardan
                // siempre, aunque el usuario no marque "Recordarme".
                Preferences.Set(
                    SessionKeys.KeyUserId,
                    response.UsuarioId?.ToString() ?? string.Empty);

                Preferences.Set(
                    SessionKeys.KeyNombreCompletoUsuario,
                    response.NombreCompletoUsuario ?? string.Empty);

                Preferences.Set(
                    SessionKeys.KeyCorreoUsuario,
                    response.CorreoUsuario ?? string.Empty);

                Preferences.Set(
                    SessionKeys.KeyUrlImagenUsuario,
                    response.UrlImagenUsuario ?? string.Empty);

                Preferences.Set(
                    SessionKeys.KeyRolId,
                    response.RolId?.ToString() ?? string.Empty);

                Preferences.Set(
                    SessionKeys.KeyRolNombre,
                    response.RolNombre ?? string.Empty);

                if (RememberMe)
                {
                    Preferences.Set(KeyRemember, true);
                    Preferences.Set(KeyUser, usuario);

                    await SecureStorage.SetAsync(
                        KeyPass,
                        Password);
                }
                else
                {
                    LimpiarCredencialesRecordadas();
                }
            }
            catch
            {
                // Un problema de almacenamiento local no debe impedir
                // que el usuario inicie sesión.
            }
        }

        private void LimpiarCredencialesRecordadas()
        {
            Preferences.Remove(KeyRemember);
            Preferences.Remove(KeyUser);
            Preferences.Remove(KeyUseBiometrics);
            Preferences.Remove(KeyRequireRelogin);

            SecureStorage.Remove(KeyPass);

            useBiometrics = false;
            requirePasswordRelogin = false;

            OnPropertyChanged(nameof(UseBiometrics));
            OnPropertyChanged(nameof(RequirePasswordRelogin));
            ActualizarEstadoBiometrico();
        }

        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;

            PasswordToggleIcon =
                IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }

        public async Task LoadSavedAsync()
        {
            try
            {
                rememberMe =
                    Preferences.Get(KeyRemember, false);

                OnPropertyChanged(nameof(RememberMe));

                if (RememberMe)
                {
                    Username =
                        Preferences.Get(KeyUser, string.Empty);

                    string? savedPassword =
                        await SecureStorage.GetAsync(KeyPass);

                    if (!string.IsNullOrWhiteSpace(savedPassword))
                        Password = savedPassword;
                }

                useBiometrics =
                    Preferences.Get(KeyUseBiometrics, false);

                requirePasswordRelogin =
                    Preferences.Get(KeyRequireRelogin, false);

                OnPropertyChanged(nameof(UseBiometrics));
                OnPropertyChanged(nameof(RequirePasswordRelogin));

                CanUseBiometrics =
                    await CrossFingerprint.Current
                        .IsAvailableAsync(true);
            }
            catch
            {
                CanUseBiometrics = false;
            }
            finally
            {
                OnPropertyChanged(nameof(TitleBiometric));
                ActualizarEstadoBiometrico();
            }
        }

        private async Task TryBiometricLoginAsync()
        {
            if (RequirePasswordRelogin || IsBusy)
                return;

            try
            {
                bool available =
                    await CrossFingerprint.Current
                        .IsAvailableAsync(true);

                if (!available)
                {
                    _ = MostrarToastAsync(
                        "El dispositivo no tiene biometría " +
                        "o no está configurado.");

                    CanUseBiometrics = false;
                    return;
                }

                var reason =
                    new AuthenticationRequestConfiguration(
                        "Confirma tu identidad en tu dispositivo",
                        "Autentícate con huella/Face ID para continuar");

                FingerprintAuthenticationResult result =
                    await CrossFingerprint.Current
                        .AuthenticateAsync(reason);

                if (!result.Authenticated)
                    return;

                string? savedPassword =
                    await SecureStorage.GetAsync(KeyPass);

                string savedUser =
                    Preferences.Get(KeyUser, string.Empty);

                if (string.IsNullOrWhiteSpace(savedUser) ||
                    string.IsNullOrWhiteSpace(savedPassword))
                {
                    _ = MostrarToastAsync(
                        "No hay una sesión guardada para desbloquear " +
                        "con biometría.");

                    return;
                }

                loginViaBiometric = true;
                Username = savedUser;
                Password = savedPassword;

                await LoginAsync();
            }
            catch (Exception)
            {
                loginViaBiometric = false;

                _ = MostrarToastAsync(
                    "No fue posible completar la autenticación biométrica.");
            }
        }

        private void ActualizarEstadoBiometrico()
        {
            OnPropertyChanged(nameof(BiometricEnabled));
            OnPropertyChanged(nameof(LoginButtonVisible));
            OnPropertyChanged(nameof(CanToggleBiometrics));

            BiometricLoginCommand.ChangeCanExecute();
        }
    }
}
