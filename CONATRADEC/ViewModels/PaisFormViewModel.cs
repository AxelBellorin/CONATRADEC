using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CONATRADEC.Models;
using CONATRADEC.Services;
using System.ComponentModel;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    // ViewModel del formulario de País.
    // Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters) y estado (IsBusy).
    public class PaisFormViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Objeto de trabajo que se edita/crea desde el formulario.
        private PaisRequest pais;

        // Bandera interna para controlar confirmaciones (cancelar/guardar).
        private bool isCancel;

        // Campos editables desde la vista (Entry: Nombre/Código ISO).
        private string nombrePais;
        private string codigoISOPais;

        // Modo del formulario (Create / Edit / View).
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();

        // Servicio de API para persistir cambios de País.
        private readonly PaisApiService paisApiService = new PaisApiService();

        // Comandos expuestos a la vista (botones Guardar/Cancelar).
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        // ===========================================================
        // ========================= CTOR ============================
        // ===========================================================

        public PaisFormViewModel()
        {
            // Guarda si el formulario no está en solo lectura (IsReadOnly).
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);

            // Cancela la edición y vuelve a la página de listado.
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // ===========================================================
        // =============== PROPIEDADES CON NOTIFICACIÓN ==============
        // ===========================================================

        // Nombre del País (bindeado a Entry).
        public string NombrePais
        {
            get => nombrePais;
            set { nombrePais = value; OnPropertyChanged(); }
        }

        // Código ISO del País (bindeado a Entry).
        public string CodigoISOPais
        {
            get => codigoISOPais;
            set
            {
                codigoISOPais = value;
                OnPropertyChanged();
            }
        }

        // Bandera de flujo para confirmar acciones (no es bindable a UI).
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // Objeto País seleccionado/creado. Al asignarlo, propaga valores a los campos editables.
        public PaisRequest Pais
        {
            get => pais;
            set
            {
                pais = value;
                OnPropertyChanged();
                // Sincroniza el formulario con los datos del objeto.
                NombrePais = value.NombrePais;
                CodigoISOPais = value.CodigoISOPais;
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
                // ((Command)SaveCommand).ChangeCanExecute(); // opcional
            }
        }

        // Indica si los campos del formulario están bloqueados (solo lectura).
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;

        // Controla la visibilidad del botón Guardar (oculto en modo View).
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        // Título dinámico mostrado arriba del formulario según el modo.
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear País",
            FormMode.FormModeSelect.Edit => "Editar País",
            FormMode.FormModeSelect.View => "Detalles del País",
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
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToAsyncParameters("//PaisPage");
                    }
                }
                else
                {
                    // Si no hubo cambios, simplemente regresa.
                    await GoToAsyncParameters("//PaisPage");
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
                    await CreatePaisAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdatePaisAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Crea un nuevo País (confirmación → persistir → navegar → feedback).
        private async Task CreatePaisAsync()
        {
            try
            {                
                // Determina si hay cambios significativos para guardar.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    // Solicita confirmación antes de persistir.
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del país?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Propaga los valores del formulario al objeto País.
                        Pais.NombrePais = NombrePais;
                        Pais.CodigoISOPais = CodigoISOPais;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        // Llama a la API para crear el registro.
                        var response = await paisApiService.CreatePaisAsync(Pais);

                        if (response)
                        {
                            await GoToAsyncParameters("//PaisPage"); // Navega al listado.
                            await Application.Current.MainPage.DisplayAlert("Éxito", "País guardado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El país no se pudo guardar, intente nuevamente", "OK");
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

        // Actualiza un País existente (confirmación → persistir → navegar → feedback).
        private async Task UpdatePaisAsync()
        {
            try
            {
                // Determina si hay cambios antes de pedir confirmación.
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
                        // Propaga al objeto principal los cambios del formulario.
                        Pais.NombrePais = NombrePais;
                        Pais.CodigoISOPais = CodigoISOPais;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        // Llama a la API para actualizar.
                        var response = await paisApiService.UpdatePaisAsync(Pais);

                        if (response)
                        {
                            await GoToAsyncParameters("//PaisPage");
                            await Application.Current.MainPage.DisplayAlert("Éxito", "País actualizado correctamente", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "El país no se pudo actualizar, intente nuevamente", "OK");
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
            if (NombrePais != Pais.NombrePais) return true;
            if (CodigoISOPais != Pais.CodigoISOPais) return true;
            
            return false;
        }

        //// Valida si los campos del formulario estan correctos.
        //private bool ValidateFieldsData()
        //{
        //    if (CodigoISOPais.Length > 3 || string.IsNullOrWhiteSpace(CodigoISOPais))
        //    {
        //        codigoISOPais = CodigoISOPais; // Mantiene el valor anterior si excede 3 caracteres.
        //        _ = Snackbar.Make(
        //                        "El código ISO debe tener exactamente 3 letras.",
        //                        duration: TimeSpan.FromSeconds(3),
        //                        visualOptions: new SnackbarOptions
        //                        {
        //                            BackgroundColor = Colors.Red,
        //                            TextColor = Colors.White
        //                        }).Show();
        //    }
        //    else
        //    {
        //        codigoISOPais = codigoISOPais.ToUpper();
        //        return true;
        //    }
        //    return false;
        //}
    }
}
