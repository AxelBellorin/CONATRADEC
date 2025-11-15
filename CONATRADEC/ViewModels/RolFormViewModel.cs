using CONATRADEC.Services;          // Servicios de acceso a API (RolApiService) y utilidades de navegación (GlobalService).
using System.ComponentModel;        // INotifyPropertyChanged (lo hereda de GlobalService).
using System.Windows.Input;         // Command / ICommand para enlazar con la UI (botones).
using CONATRADEC.Models;            // Modelos de datos (RolRequest, FormMode, etc.).

namespace CONATRADEC.ViewModels
{
    // ViewModel del formulario de Rol (crear/editar/ver).
    // Hereda de GlobalService para reutilizar navegación con Shell y notificación de propiedades.
    public class RolFormViewModel : GlobalService
    {
        // ===========================================================
        // ================== ESTADO DEL FORMULARIO ==================
        // ===========================================================

        private RolRequest rol;                 // Entidad Rol a editar/crear (se recibe por navegación).
        private bool isCancel;                  // Bandera interna para lógica de confirmación al cancelar.
        private string nombreRol;               // Campo editable: NombreRol (bindeado al Entry).
        private string descripcionRol;          // Campo editable: DescripcionRol (bindeado al Entry).

        // Modo del formulario (Create/Edit/View); controla título, lectura y visibilidad del botón Guardar.
        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();

        // Servicio para operaciones CRUD de Rol contra la API.
        private readonly RolApiService rolApiService = new RolApiService();

        // Comandos expuestos a la vista.
        public Command SaveCommand { get; }     // Guardar (crea o actualiza según Mode).
        public Command CancelCommand { get; }   // Cancelar (con o sin confirmación si hay cambios).

        // ===========================================================
        // ======================= CTOR ==============================
        // ===========================================================
        public RolFormViewModel()
        {
            // Guardar habilitado solo cuando NO es lectura (IsReadOnly == false).
            // Nota: si cambias Mode en tiempo de ejecución, podría interesar llamar a:
            // ((Command)SaveCommand).ChangeCanExecute(); (ver comentarios en set de Mode)
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);

            // Cancelar siempre disponible.
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // ===========================================================
        // =============== PROPIEDADES BINDABLE (UI) =================
        // ===========================================================

        // Nombre del rol (Entry en la vista).
        public string NombreRol
        {
            get => nombreRol;
            set { nombreRol = value; OnPropertyChanged(); }
        }

        // Descripción del rol (Entry en la vista).
        public string DescripcionRol
        {
            get => descripcionRol;
            set { descripcionRol = value; OnPropertyChanged(); }
        }

        // Bandera usada internamente durante validaciones/cancelación.
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // Entidad Rol que edita el formulario.
        // Al asignarse, se rellenan los campos editables (NombreRol, DescripcionRol) para mostrar en la UI.
        public RolRequest Rol
        {
            get => rol;
            set
            {
                rol = value;
                OnPropertyChanged();
                // Precarga de los campos del formulario desde el objeto Rol recibido.
                NombreRol = value.NombreRol;
                DescripcionRol = value.DescripcionRol;
            }
        }

        // Modo del formulario: Create / Edit / View
        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();                    // Notifica cambio general.
                OnPropertyChanged(nameof(IsReadOnly));  // Actualiza la UI de solo lectura.
                OnPropertyChanged(nameof(Title));       // Refresca el título.
                OnPropertyChanged(nameof(ShowSaveButton)); // Controla visibilidad del botón Guardar.

                // 💡 Sugerencia (no cambiamos lógica): aquí podría recalcularse CanExecute de Save:
                // ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        // Indica si el formulario está en modo lectura.
        public bool IsReadOnly
        {
            get => Mode == FormMode.FormModeSelect.View ? true : false;
        }

        // Controla la visibilidad del botón Guardar (oculto en modo View).
        public bool ShowSaveButton
        {
            get => Mode != FormMode.FormModeSelect.View ? true : false;
        }

        // Título de la pantalla según el modo actual.
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Rol",
            FormMode.FormModeSelect.Edit => "Editar Rol",
            FormMode.FormModeSelect.View => "Detalles del Rol",
            _ => "",
        };

        // ===========================================================
        // ======================= ACCIONES ===========================
        // ===========================================================

        // Acción de cancelar: si detecta cambios, pregunta confirmación; si no, simplemente navega.
        private async Task CancelAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync(); // True si hay diferencias entre campos y Rol original.

                if (IsCancel)
                {
                    // Si hay cambios, confirma con el usuario.
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToAsyncParameters("//RolPage"); // Vuelve al listado de roles.
                    }
                }
                else
                {
                    // Si no hay cambios, regresa inmediatamente.
                    await GoToAsyncParameters("//RolPage");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsCancel = false; // Limpia bandera para siguientes intentos.
            }
        }

        // Comparación de campos para detectar si hay cambios sin guardar.
        // (NombreRol/DescripcionRol vs. los valores del objeto Rol original).
        private bool ValidateFieldsAsync()
        {
            if (NombreRol != Rol.NombreRol) return true;
            if (DescripcionRol != Rol.DescripcionRol) return true;
            return false;
        }

        // Decide si crea o actualiza según el Mode actual.
        private async Task SaveAsync()
        {
            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateRolAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateRolAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Crea un nuevo rol (usa RolApiService.CreateRolAsync).
        private async Task CreateRolAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync(); // Reutiliza la bandera como "hay algo que guardar".

                if (IsCancel)
                {
                    // 📝 Texto original decía "datos del usuario", lo mantengo para no alterar tu UI.
                    // (Sugerencia futura: cambiar a "datos del rol")
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del rol?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Sincroniza los campos del formulario hacia el objeto Rol antes de enviar.
                        Rol.NombreRol = NombreRol;
                        Rol.DescripcionRol = DescripcionRol;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        // Invoca creación en la API.
                        var response = await rolApiService.CreateRolAsync(Rol);

                        if (response)
                        {
                            await GoToRolPage(); // Navega al listado.
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
                IsCancel = false; // Limpia bandera.
            }
        }

        // Actualiza un rol existente (usa RolApiService.UpdateRolAsync).
        private async Task UpdateRolAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync(); // True si hay algo modificado.

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea actualizar?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Pasa al objeto Rol los cambios del formulario.
                        Rol.NombreRol = NombreRol;
                        Rol.DescripcionRol = DescripcionRol;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        // Invoca actualización en la API.
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
                IsCancel = false; // Limpia bandera.
            }
        }
    }
}
