using CONATRADEC.Services;
using System.ComponentModel;
using System.Windows.Input;
using CONATRADEC.Models;

namespace CONATRADEC.ViewModels
{
    public class CargoFormViewModel : GlobalService
    {
        private CargoRequest cargo;
        private bool isCancel;
        private string nombreCargo;
        private string descripcionCargo;
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();
        private readonly CargoApiService cargoApiService= new CargoApiService();
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public CargoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public string NombreCargo
        {
            get => nombreCargo;
            set { nombreCargo = value; OnPropertyChanged(); }
        }
        public string DescripcionCargo
        {
            get => descripcionCargo;
            set { descripcionCargo = value; OnPropertyChanged(); }
        }
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        public CargoRequest Cargo
        {
            get => cargo;
            set { cargo = value; OnPropertyChanged(); NombreCargo = value.NombreCargo; DescripcionCargo = value.DescripcionCargo;}
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
            FormMode.FormModeSelect.Create => "Crear Cargo",
            FormMode.FormModeSelect.Edit => "Editar Cargo",
            FormMode.FormModeSelect.View => "Detalles del Cargo",
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
                        await Shell.Current.GoToAsync("//CargoPage");
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync("//CargoPage");
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
            if (NombreCargo != Cargo.NombreCargo) return true;
            if (DescripcionCargo != Cargo.DescripcionCargo) return true;
            return false;
        }
        private async Task SaveAsync()
        {
            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateCargoAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateCargoAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task CreateCargoAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert("Confirmar", "¿Desea guardar los datos del cargo?", "Aceptar", "Cancelar");

                    if (confirm)
                    {
                        Cargo.NombreCargo = NombreCargo;
                        Cargo.DescripcionCargo = DescripcionCargo;
                        // Aquí podrías llamar a una API o guardar en base de datos
                        var response = await cargoApiService.CreateCargoAsync(Cargo);
                        if (response) 
                        {
                            await GoToCargoPage();
                            await Application.Current.MainPage.DisplayAlert("Éxito", "Cargo guardado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El cargo no se pudo guardar, intente nuevamente", "OK");
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

        private async Task UpdateCargoAsync()
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
                        Cargo.NombreCargo = NombreCargo;
                        Cargo.DescripcionCargo = DescripcionCargo;
                        // Aquí podrías llamar a una API o guardar en base de datos
                        var response = await cargoApiService.UpdateCargoAsync(Cargo);
                        if (response)
                        {
                            await GoToCargoPage();
                            await Application.Current.MainPage.DisplayAlert("Éxito", "Cargo actualizado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El cargo no se pudo actualizar, intente nuevamente", "OK");
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
