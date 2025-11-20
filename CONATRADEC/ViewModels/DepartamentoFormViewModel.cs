using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    // ViewModel del formulario de Departamento (sin Snackbar).
    // Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters) y estado (IsBusy).
    public class DepartamentoFormViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Objeto de trabajo que se edita/crea desde el formulario.
        private DepartamentoRequest departamento;
        private PaisRequest paisRequest;
        private MunicipioRequest municipioRequest;

        // Bandera interna para controlar confirmaciones (cancelar/guardar).
        private bool isCancel;

        // Campos editables desde la vista.
        private string nombreDepartamento = string.Empty;

        // Modo del formulario (Create / Edit / View).
        private FormMode.FormModeSelect mode = new();

        // Servicio de API para persistir cambios de Departamento.
        private readonly DepartamentoApiService departamentoApiService = new();

        // Comandos expuestos a la vista (botones Guardar/Cancelar).
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        // ===========================================================
        // ========================= CTOR ============================
        // ===========================================================

        public DepartamentoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // ===========================================================
        // =============== PROPIEDADES CON NOTIFICACIÓN ==============
        // ===========================================================

        public string NombreDepartamento
        {
            get => nombreDepartamento;
            set { nombreDepartamento = value; OnPropertyChanged(); }
        }

        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        public DepartamentoRequest Departamento
        {
            get => departamento;
            set
            {
                departamento = value;
                OnPropertyChanged();

                // Sincroniza el formulario con los datos del objeto.
                NombreDepartamento = value.NombreDepartamento ?? string.Empty;
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set { paisRequest = value; OnPropertyChanged(); }
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
                // ((Command)SaveCommand).ChangeCanExecute(); // opcional
            }
        }

        public MunicipioRequest MunicipioRequest 
        { 
            get => municipioRequest; 
            set 
            { 
                municipioRequest = value; 
                OnPropertyChanged(); 
            } 
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Departamento",
            FormMode.FormModeSelect.Edit => "Editar Departamento",
            FormMode.FormModeSelect.View => "Detalles del Departamento",
            _ => "",
        };

        // ===========================================================
        // ======================= MÉTODOS UI ========================
        // ===========================================================

        private async Task CancelAsync()
        {
            try
            {
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            { "Pais", PaisRequest },
                            { "TitlePage", $"Departamento de {PaisRequest.NombrePais.ToString()}"}
                        };

                        await GoToAsyncParameters("//DepartamentoPage", parameters);
                    }

                }
                else
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "Pais", PaisRequest },
                        { "TitlePage", $"Departamento de {PaisRequest.NombrePais.ToString()}"}
                    };

                    await GoToAsyncParameters("//DepartamentoPage", parameters);
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        // ===========================================================
        // ===================== LÓGICA DE GUARDADO ==================
        // ===========================================================

        private async Task SaveAsync()
        {
            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateDepartamentoAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateDepartamentoAsync();
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        private async Task CreateDepartamentoAsync()
        {
            try
            {
                // Validación de datos
                if (!ValidateFieldsData()) return;

                // Determina si hay cambios significativos para guardar.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del departamento?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Propaga los valores del formulario al objeto.
                        Departamento.NombreDepartamento = NombreDepartamento;
                        Departamento.PaisId = PaisRequest.PaisId;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        var response = await departamentoApiService.CreateDepartamentoAsync(Departamento);

                        if (response)
                        {
                            var parameters = new Dictionary<string, object>
                            {
                                { "Pais", PaisRequest },
                                { "TitlePage", $"Departamento de {PaisRequest.NombrePais.ToString()}"}
                            };
                            await GoToAsyncParameters("//DepartamentoPage", parameters);

                            _ = MostrarToastAsync("Éxito\nDepartamento guardado correctamente.");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error" + "\nNo se pudo guardar el departamento.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {   
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private async Task UpdateDepartamentoAsync()
        {
            try
            {
                if (!ValidateFieldsData()) return;

                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea actualizar?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        Departamento.NombreDepartamento = NombreDepartamento;
                        Departamento.PaisId = PaisRequest.PaisId;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        var response = await departamentoApiService.UpdateDepartamentoAsync(Departamento);

                        if (response)
                        {
                            var parameters = new Dictionary<string, object>
                            {
                                { "Pais", PaisRequest },
                                { "TitlePage", $"Departamento de {PaisRequest.NombrePais.ToString()}"}
                            };
                            await GoToAsyncParameters("//DepartamentoPage", parameters);

                            _ = MostrarToastAsync("Éxito" + "Departamento actualizado correctamente.");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error" + "No se pudo actualizar el departamento.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        // ===========================================================
        // ===================== MÉTODOS AUXILIARES ==================
        // ===========================================================

        // ¿Hay cambios respecto al objeto original?
        private bool ValidateFields()
        {
            if (Departamento is null) return true;

            if ((NombreDepartamento ?? string.Empty) != (Departamento.NombreDepartamento ?? string.Empty)) return true;

            return false;
        }

        // ¿Los datos del formulario son válidos?
        private bool ValidateFieldsData()
        {
            if (string.IsNullOrWhiteSpace(NombreDepartamento))
            {
                _ = App.Current.MainPage.DisplayAlert("Validación", "Ingrese el nombre del departamento.", "OK");
                return false;
            }

            return true;
        }
    }
}
