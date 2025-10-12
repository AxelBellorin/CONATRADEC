using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static CONATRADEC.Models.FormMode;

namespace MiApp.ViewModels
{
    public class UserFormViewModel : GlobalService
    {

        private string firstName, lastName, email, phone, password;
        private string message;
        private bool isBusy;

        public FormMode1 Mode { get; private set; }

        public UserFormViewModel()
        {
            CancelCommand = new Command(Cancel);

            UpdateModeBehavior();
        }

        // ====== PROPIEDADES ======
        public string FirstName
        {
            get => firstName;
            set { firstName = value; OnPropertyChanged(); RaiseCanExecuteChanged(); }
        }

        public string LastName
        {
            get => lastName;
            set { lastName = value; OnPropertyChanged(); RaiseCanExecuteChanged(); }
        }

        public string Email
        {
            get => email;
            set { email = value; OnPropertyChanged(); RaiseCanExecuteChanged(); }
        }

        public string Phone
        {
            get => phone;
            set { phone = value; OnPropertyChanged(); RaiseCanExecuteChanged(); }
        }

        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); RaiseCanExecuteChanged(); }
        }

        public string Message { get => message; set { message = value; OnPropertyChanged(); } }

        public bool IsBusy
        {
            get => isBusy;
            set { isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(IsNotBusy)); RaiseCanExecuteChanged(); }
        }

        public bool IsNotBusy => !IsBusy;

        // ====== UI PROPERTIES SEGÚN MODO ======
        public bool IsEditable => Mode != FormMode1.View;
        public string SaveButtonText => Mode == FormMode1.Create ? "Guardar" :
                                        Mode == FormMode1.Edit ? "Actualizar" :
                                        "Aceptar";

        public bool CanSave => IsEditable && !IsBusy;

        // ====== COMANDOS ======
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task SaveAsync()
        {
            if (!CanSave) return;
            IsBusy = true;

            try
            {
                await Task.Delay(1000); // simulación

                if (Mode == FormMode1.Create)
                    Message = "Usuario creado correctamente.";
                else if (Mode == FormMode1.Edit)
                    Message = "Usuario actualizado correctamente.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Cancel()
        {
            Message = "Acción cancelada.";
            await Shell.Current.GoToAsync("//UserPage");
        }

        private void UpdateModeBehavior()
        {
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(SaveButtonText));
            RaiseCanExecuteChanged();
        }

        private void RaiseCanExecuteChanged()
        {
            if (SaveCommand is Command s) s.ChangeCanExecute();
            if (CancelCommand is Command c) c.ChangeCanExecute();
        }
    }
}
