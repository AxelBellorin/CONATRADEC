using CONATRADEC.Services;
using System.ComponentModel;
using System.Windows.Input;
using CONATRADEC.Models;

namespace CONATRADEC.ViewModels
{
    public class RolFormViewModel : GlobalService
    {
        private RolRequest rol;
        private bool isBusy;
        private bool isCancel;
        private string nombreRol;
        private string descripcionRol;
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();
        private readonly RolApiService rolApiService= new RolApiService();
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RolFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public string NombreRol
        {
            get => nombreRol;
            set { nombreRol = value; OnPropertyChanged(); }
        }
        public string DescripcionRol
        {
            get => descripcionRol;
            set { descripcionRol = value; OnPropertyChanged(); }
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

        public RolRequest Rol
        {
            get => rol;
            set { rol = value; OnPropertyChanged(); NombreRol = value.NombreRol; DescripcionRol = value.DescripcionRol;}
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
            FormMode.FormModeSelect.Create => "Crear Rol",
            FormMode.FormModeSelect.Edit => "Editar Rol",
            FormMode.FormModeSelect.View => "Detalles del Rol",
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
                        await Shell.Current.GoToAsync("//RolPage");
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync("//RolPage");
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
            if (NombreRol != Rol.NombreRol) return true;
            if (DescripcionRol != Rol.DescripcionRol) return true;
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
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert("Confirmar", "¿Desea guardar los datos del usuario?", "Aceptar", "Cancelar");

                    if (confirm)
                    {
                        Rol.NombreRol = NombreRol;
                        Rol.DescripcionRol = DescripcionRol;
                        // Aquí podrías llamar a una API o guardar en base de datos
                        var response = await rolApiService.CreateRolAsync(Rol);
                        if (response) 
                        {
                            await GoToRolPage();
                            await Application.Current.MainPage.DisplayAlert("Éxito", "Rol guardado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El rol no se pudo guardar, intente nuevamente", "OK");
                        }                       
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
        }

        private async Task UpdateUserAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert("Confirmar", "¿Desea actualizar?", "Aceptar", "Cancelar");

                    if (confirm)
                    {
                        //Asigna los cambios realizados al objeto Rol
                        Rol.NombreRol = NombreRol;
                        Rol.DescripcionRol = DescripcionRol;
                        // Aquí podrías llamar a una API o guardar en base de datos
                        var response = await rolApiService.UpdateRolAsync(Rol);
                        if (response)
                        {
                            await GoToRolPage();
                            await Application.Current.MainPage.DisplayAlert("Éxito", "Rol actualizado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El rol no se pudo actualizar, intente nuevamente", "OK");
                        }
                        
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
        }
    }
}
