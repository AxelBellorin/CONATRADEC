using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;


namespace CONATRADEC.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string username;
        private string password;
        private string message;
        private bool isBusy;
        private string urlimage= "loginicon";
        private LoginResponse user;

        private readonly ApiService apiService;

        public event PropertyChangedEventHandler PropertyChanged;

        public LoginViewModel()
        {
            apiService = new ApiService();
            LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
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
                Message = $"Error: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Título", Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
