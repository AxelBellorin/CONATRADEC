using CONATRADEC.Services;        // Servicios globales, navegación y utilidades compartidas.
using System.ComponentModel;      // Para INotifyPropertyChanged (heredado desde GlobalService).
using System.Windows.Input;       // Para usar Command e ICommand (enlace entre UI y lógica).
using CONATRADEC.Models;          // Modelos de datos: UserRequest, FormMode, etc.

namespace CONATRADEC.ViewModels
{
    // ===========================================================
    // ================ UserFormViewModel =========================
    // ===========================================================
    // ViewModel encargado de la lógica del formulario de Usuario.
    // Permite Crear, Editar y Visualizar usuarios, controlando
    // la navegación, validaciones y los estados de la interfaz.
    public class UserFormViewModel : GlobalService
    {
        // =======================================================
        // ============ CAMPOS PRIVADOS Y ESTADO INTERNO ==========
        // =======================================================

        private UserRequest user;             // Entidad principal en edición (recibida desde otra página).
        private bool isBusy;                  // Bandera que indica si hay una operación en curso (evita doble clic).
        private bool isCancel;                // Bandera usada para detectar si el usuario desea cancelar cambios.

        // Campos del formulario, vinculados a la interfaz (Entry/TextBox).
        private string firstName = "";
        private string lastName;
        private string email;

        // Define el modo del formulario (Crear, Editar, Ver).
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();

        // =======================================================
        // ================== COMANDOS PRINCIPALES ===============
        // =======================================================

        // Comando para guardar (Crear o Editar según el modo).
        public Command SaveCommand { get; }

        // Comando para cancelar y volver a la pantalla principal.
        public Command CancelCommand { get; }

        // =======================================================
        // ===================== CONSTRUCTOR =====================
        // =======================================================

        public UserFormViewModel()
        {
            // Inicializa comandos con sus métodos asincrónicos correspondientes.
            // SaveCommand solo está habilitado cuando el formulario no está en modo de solo lectura.
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);

            // CancelCommand siempre disponible.
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // =======================================================
        // ============ PROPIEDADES CON NOTIFICACIÓN =============
        // =======================================================

        // Campo "Nombre" del usuario.
        public string FirstName
        {
            get => firstName;
            set { firstName = value; OnPropertyChanged(); }  // Notifica cambios a la UI.
        }

        // Campo "Apellido" del usuario.
        public string LastName
        {
            get => lastName;
            set { lastName = value; OnPropertyChanged(); }
        }

        // Campo "Correo electrónico".
        public string Email
        {
            get => email;
            set { email = value; OnPropertyChanged(); }
        }

        // Indica si el usuario decidió cancelar los cambios.
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // Indica si hay una operación en curso (usado para deshabilitar botones y evitar dobles clics).
        public bool IsBusy
        {
            get => isBusy;
            set { isBusy = value; OnPropertyChanged(); }
        }

        // Entidad principal que contiene la información del usuario.
        // Al asignarse, precarga los campos del formulario.
        public UserRequest User
        {
            get => user;
            set
            {
                user = value;
                OnPropertyChanged();

                // Carga de valores iniciales en los campos del formulario.
                FirstName = value.FirstName;
                LastName = value.LastName;
                Email = value.Email;
            }
        }

        // =======================================================
        // ============ CONTROL DE MODO DE FORMULARIO ============
        // =======================================================

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();

                // Notifica propiedades dependientes del modo.
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ShowSaveButton));
            }
        }

        // Si el formulario está en modo "Ver", los campos son de solo lectura.
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View ? true : false;

        // Muestra el botón Guardar solo si no está en modo "Ver".
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View ? true : false;

        // Título dinámico según el modo del formulario.
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Usuario",
            FormMode.FormModeSelect.Edit => "Editar Usuario",
            FormMode.FormModeSelect.View => "Detalles del Usuario",
            _ => "",
        };

        // =======================================================
        // ================== LÓGICA DE CANCELAR ==================
        // =======================================================
        private async Task CancelAsync()
        {
            try
            {
                // Valida si existen cambios sin guardar.
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    // Solicita confirmación antes de descartar los cambios.
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Navega de vuelta a la página de usuarios.
                        await GoToAsyncParameters("//UserPage");
                    }
                }
                else
                {
                    // Si no hay cambios, vuelve directamente.
                    await GoToAsyncParameters("//UserPage");
                }
            }
            catch (Exception ex)
            {
                // Muestra error genérico en caso de excepción.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Restablece bandera de cancelación.
                IsCancel = false;
            }
        }

        // =======================================================
        // ================= VALIDACIÓN DE CAMPOS =================
        // =======================================================
        private bool ValidateFieldsAsync()
        {
            // Compara los valores actuales con los originales.
            // Si alguno difiere, hay cambios sin guardar.
            if (FirstName != User.FirstName) return true;
            if (LastName != User.LastName) return true;
            if (Email != User.Email) return true;

            return false;
        }

        // =======================================================
        // ===================== GUARDAR (MAIN) ===================
        // =======================================================
        private async Task SaveAsync()
        {
            // Evita ejecutar si ya hay un proceso en curso.
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Decide qué método ejecutar según el modo actual.
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
                // Libera bandera Busy al finalizar.
                IsBusy = false;
            }
        }

        // =======================================================
        // =================== CREAR NUEVO USUARIO ================
        // =======================================================
        private async Task CreateUserAsync()
        {
            try
            {
                // Verifica si hay datos modificados antes de guardar.
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    // Solicita confirmación al usuario.
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del usuario?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Aquí podrías llamar al servicio API para crear el usuario.
                        // Por ahora, simplemente navega y muestra mensaje de éxito.
                        await GoToAsyncParameters("//UserPage");
                        await Application.Current.MainPage.DisplayAlert("Éxito", "Usuario guardado correctamente", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo genérico de error.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Limpia bandera de cancelación.
                IsCancel = false;
            }
        }

        // =======================================================
        // =================== ACTUALIZAR USUARIO =================
        // =======================================================
        private async Task UpdateUserAsync()
        {
            try
            {
                // Verifica si hay datos modificados.
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    // Pide confirmación al usuario.
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea actualizar?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Aquí podrías llamar a la API para actualizar el registro.
                        await GoToAsyncParameters("//UserPage");
                        await Application.Current.MainPage.DisplayAlert("Éxito", "Usuario guardado correctamente", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                // Muestra error en caso de excepción.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                // Limpia bandera de cancelación.
                IsCancel = false;
            }
        }
    }
}
