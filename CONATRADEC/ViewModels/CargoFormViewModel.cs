using CONATRADEC.Services;
using System.ComponentModel;
using System.Windows.Input;
using CONATRADEC.Models;

namespace CONATRADEC.ViewModels
{
    // ViewModel del formulario de Cargos.
    // Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters/GoToCargoPage) y estado (IsBusy).
    public class CargoFormViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Objeto de trabajo que se edita/crea desde el formulario.
        private CargoRequest cargo;

        // Bandera interna para controlar confirmaciones (cancelar/guardar).
        private bool isCancel;

        // Campos editables desde la vista (Entry: Nombre/Descripción).
        private string nombreCargo;
        private string descripcionCargo;

        // Modo del formulario (Create / Edit / View).
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();

        // Servicio de API para persistir cambios de Cargo.
        private readonly CargoApiService cargoApiService = new CargoApiService();

        // Comandos expuestos a la vista (botones Guardar/Cancelar).
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        // ===========================================================
        // ========================= CTOR ============================
        // ===========================================================

        public CargoFormViewModel()
        {
            // Guarda si el formulario no está en solo lectura (IsReadOnly).
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);

            // Cancela la edición y vuelve a la página de listado.
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // ===========================================================
        // =============== PROPIEDADES CON NOTIFICACIÓN ==============
        // ===========================================================

        // Nombre del Cargo (bindeado a Entry).
        public string NombreCargo
        {
            get => nombreCargo;
            set { nombreCargo = value; OnPropertyChanged(); }
        }

        // Descripción del Cargo (bindeado a Entry).
        public string DescripcionCargo
        {
            get => descripcionCargo;
            set { descripcionCargo = value; OnPropertyChanged(); }
        }

        // Bandera de flujo para confirmar acciones (no es bindable a UI).
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // Objeto Cargo seleccionado/creado. Al asignarlo, propaga valores a los campos editables.
        public CargoRequest Cargo
        {
            get => cargo;
            set
            {
                cargo = value;
                OnPropertyChanged();
                // Sincroniza el formulario con los datos del objeto.
                NombreCargo = value.NombreCargo;
                DescripcionCargo = value.DescripcionCargo;
            }
        }

        // Modo del formulario: Create/Edit/View. Cambia flags y título dinámicos.
        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                // Notifica propiedades dependientes para refrescar la UI.
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ShowSaveButton));
                // Si quisieras, aquí podrías forzar ChangeCanExecute de SaveCommand.
                // ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        // Indica si los campos del formulario están bloqueados (solo lectura).
        public bool IsReadOnly
        {
            get => Mode == FormMode.FormModeSelect.View ? true : false;
        }

        // Controla la visibilidad del botón Guardar (oculto en modo View).
        public bool ShowSaveButton
        {
            get => Mode != FormMode.FormModeSelect.View ? true : false;
        }

        // Título dinámico mostrado arriba del formulario según el modo.
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Cargo",
            FormMode.FormModeSelect.Edit => "Editar Cargo",
            FormMode.FormModeSelect.View => "Detalles del Cargo",
            _ => "",
        };

        // ===========================================================
        // ======================= MÉTODOS UI ========================
        // ===========================================================

        // Acción del botón "Cancelar": confirma si hay cambios y navega al listado.
        private async Task CancelAsync()
        {
            try
            {
                // Verifica si hubo cambios en el formulario.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    // Pide confirmación si los campos han cambiado.
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToAsyncParameters("//CargoPage");
                    }
                }
                else
                {
                    // Si no hubo cambios, simplemente regresa.
                    await GoToAsyncParameters("//CargoPage");
                }
            }
            catch (Exception ex)
            {
                // Notifica cualquier error en la operación de cancelación.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Limpia flag para evitar efectos en flujos posteriores.
                IsCancel = false;
            }
        }

        // ===========================================================
        // ===================== LÓGICA DE GUARDADO ==================
        // ===========================================================

        // Decide si crea o actualiza según el modo del formulario.
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

        // Crea un nuevo Cargo (confirmación → persistir → navegar → feedback).
        private async Task CreateCargoAsync()
        {
            try
            {
                // Determina si hay cambios significativos para guardar.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    // Solicita confirmación antes de persistir.
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del cargo?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Propaga los valores del formulario al objeto Cargo.
                        Cargo.NombreCargo = NombreCargo;
                        Cargo.DescripcionCargo = DescripcionCargo;

                        // Llama a la API para crear el registro.
                        var response = await cargoApiService.CreateCargoAsync(Cargo);
                        if (response)
                        {
                            await GoToCargoPage(); // Navega al listado.
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

        // Actualiza un Cargo existente (confirmación → persistir → navegar → feedback).
        private async Task UpdateCargoAsync()
        {
            try
            {
                // Determina si hay cambios antes de pedir confirmación.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea actualizar?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Propaga al objeto principal los cambios del formulario.
                        Cargo.NombreCargo = NombreCargo;
                        Cargo.DescripcionCargo = DescripcionCargo;

                        // Llama a la API para actualizar.
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

        // ===========================================================
        // ===================== MÉTODOS AUXILIARES ==================
        // ===========================================================

        // Valida si los campos del formulario difieren de los del objeto original.
        private bool ValidateFields()
        {
            if (NombreCargo != Cargo.NombreCargo) return true;
            if (DescripcionCargo != Cargo.DescripcionCargo) return true;
            return false;
        }
    }
}
