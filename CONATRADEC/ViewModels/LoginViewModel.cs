using CONATRADEC.Models;
using CONATRADEC.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace CONATRADEC.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string username;
        private string password;
        private string message;
        private bool isBusy;
        private string urlimage = "logoconatradec";
        private bool isPasswordHidden = true;
        private string passwordToggleIcon = "eye.png";
        private LoginResponse user;

        private readonly ApiService apiService;
        public event PropertyChangedEventHandler PropertyChanged;

        public LoginViewModel()
        {
            apiService = new ApiService();
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
            TogglePasswordCommand = new Command(void () => OnTogglePassword());
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
                ((Command)LoginCommand).ChangeCanExecute();
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

        public ICommand TogglePasswordCommand { get; }
        public LoginResponse User
        {
            get => user;
            set { user = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

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

                var resp = await apiService.LoginAsync(req);

                User = resp;
                Message = $"Bienvenido {resp.FirstName} {resp.LastName}. Token: {resp.AccessToken}. URL: {resp.Image}";

                urlImage = resp.Image;


                Preferences.Default.Set("user_image_path", urlImage);
                // Aquí podrías navegar a otra página, guardar token, etc.

                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                //Message = $"Error: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Credenciales incorrecta", "Correo o contraseña incorrecta, favor revise e intente nuevamente", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ICommand OnTogglePasswordClickedCommand { get; }

        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
