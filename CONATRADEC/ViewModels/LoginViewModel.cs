using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using CONATRADEC.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;



namespace CONATRADEC.ViewModels
{
    public class LoginViewModel : GlobalService
    {
        private string username;
        private string password;
        private string message;
        private bool isBusy;
        private string urlimage = "logoconatradec";
        private bool isPasswordHidden = true;
        private string passwordToggleIcon = "eye.png";
        private LoginResponse user;

        public Command TogglePasswordCommand { get; }
        public Command LoginCommand { get; }
        public ICommand OnTogglePasswordClickedCommand { get; }

        private readonly LoginApiService apiServiceLogin;

        public LoginViewModel()
        {
            apiServiceLogin = new LoginApiService();
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            TogglePasswordCommand = new Command(() => OnTogglePassword());
        }

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


        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
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

        public LoginResponse User
        {
            get => user;
            set => user = value;
        }

        public async Task LoginAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            Message = string.Empty;
            try
            {
                var req = new LoginRequest
                {
                    Username = Username,
                    Password = Password,
                    ExpiresInMins = 60
                };

                var resp = await apiServiceLogin.LoginAsync(req);

                Message = $"Bienvenido {resp.FirstName} {resp.LastName}.";
                Username = "";
                Password = "";
                await Application.Current.MainPage.DisplayAlert("Login Correcto", $"{Message}", "OK");

                // Aquí podrías navegar a otra página, guardar token, etc.

                await Shell.Current.GoToAsync("//MainPage");

                // Buscar la página actual después de navegar
                if (Shell.Current.CurrentPage is MainPage page &&
                    page.BindingContext is MainPageViewModel vm)
                    vm.IsBusy = false;
                // Guardar datos en preferencias
                //Preferences.Default.Set("user_image_path", urlImage);
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Credenciales incorrecta", "Correo o contraseña incorrecta, favor revise e intente nuevamente", "OK");
                Username = "";
                Password = "";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }
    }

}
