using CONATRADEC.Services;
using System.ComponentModel;
using System.Windows.Input;
//using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;

namespace CONATRADEC.ViewModels
{
    public class UserFormViewModel : GlobalService
    {
        private UserRequest user;
        private bool isBusy;
        private bool isCancel;
        private string firstName="";
        private string lastName;    
        private string email;       
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public UserFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public string FirstName 
        { 
            get => firstName; 
            set { firstName = value; OnPropertyChanged();}
        }
        public string LastName 
        { 
            get => lastName;
            set { lastName = value; OnPropertyChanged();} 
        }
        public string Email 
        { 
            get => email; 
            set { email = value; OnPropertyChanged();}
        }
        public bool IsCancel 
        { 
            get => isCancel; 
            set => isCancel = value; 
        }
        public bool IsBusy 
        { 
            get => isBusy; 
            set { isBusy = value; OnPropertyChanged(); } 
        }

        public UserRequest User
        {
            get => user;
            set { user = value; OnPropertyChanged(); FirstName = value.FirstName ; LastName = value.LastName; Email = value.Email; }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ShowSaveButton));
            }
        }
        public bool IsReadOnly
        {
            get => Mode == FormMode.FormModeSelect.View ? true : false;
        }

        public bool ShowSaveButton
        {
            get => Mode != FormMode.FormModeSelect.View ? true : false;
        }

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Usuario",
            FormMode.FormModeSelect.Edit => "Editar Usuario",
            FormMode.FormModeSelect.View => "Detalles del Usuario",
            _ => "",
        };

        private async Task CancelAsync()
        {               
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert("Cancelar", "Desea no guardar los cambios", "Aceptar", "Cancelar");

                    if (confirm)
                    {
                        await Shell.Current.GoToAsync("//UserPage");
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync("//UserPage");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool ValidateFieldsAsync()
        {
            if (FirstName != User.FirstName) return true;
            if (LastName != User.LastName) return true;
            if (Email != User.Email) return true;
            return false;
        }
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateUserAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateUserAsync();

                await Application.Current.MainPage.DisplayAlert("Éxito", "Usuario guardado correctamente", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateUserAsync()
        {
            // Aquí podrías llamar a una API o guardar en base de datos
            //Console.WriteLine($"Creando usuario: {User.FirstName}");
            await Shell.Current.GoToAsync("//UserPage");
        }

        private async Task UpdateUserAsync()
        {
            try 
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert("Confirmar", "Desea actualizar", "Aceptar", "Cancelar");

                    if (confirm)
                    {
                        await Shell.Current.GoToAsync("//UserPage");
                    }                        
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsCancel = false;
            }
            //Console.WriteLine($"Actualizando usuario {User.Id}");
            await Shell.Current.GoToAsync("//UserPage");
        }
    }
}
