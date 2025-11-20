using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    // ===========================================================
    // ViewModel del formulario de Municipio (sin Snackbar).
    // - Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters)
    //   y estado (IsBusy).
    // - Lógica de Crear / Editar / Ver con validaciones básicas.
    // - Flujo en cascada: País -> Departamento -> Municipio (Departamento viene por contexto).
    // ===========================================================
    public class MunicipioFormViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Contexto de navegación: Departamento al que pertenece el Municipio.
        private DepartamentoRequest departamentoRequest;

        // (Opcional) País en contexto, si querés componer títulos o navegar más arriba.
        private PaisRequest paisRequest;

        // Objeto de trabajo que se edita/crea desde el formulario (Municipio).
        private MunicipioRequest municipioRequest;

        // Bandera interna para controlar confirmaciones (cancelar/guardar).
        private bool isCancel;

        // Campo editable desde la vista.
        private string nombreMunicipio = string.Empty;

        // Modo del formulario (Create / Edit / View).
        private FormMode.FormModeSelect mode = new();

        // Servicio de API para persistir cambios de Municipio.
        private readonly MunicipioApiService municipioApiService = new();

        // Comandos expuestos a la vista (botones Guardar/Cancelar).
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        // ===========================================================
        // ========================= CTOR ============================
        // ===========================================================

        public MunicipioFormViewModel()
        {
            // Guarda si el formulario no está en solo lectura (IsReadOnly).
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);

            // Cancela la edición y vuelve a la página de listado (MunicipioPage).
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // ===========================================================
        // =============== PROPIEDADES CON NOTIFICACIÓN ==============
        // ===========================================================

        // Nombre del Municipio (bindeado a Entry).
        public string NombreMunicipio
        {
            get => nombreMunicipio;
            set { nombreMunicipio = value; OnPropertyChanged(); }
        }

        // Bandera de flujo para confirmar acciones (no es bindable a UI).
        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // Objeto Municipio seleccionado/creado. Al asignarlo, propaga valores a los campos editables.
        public MunicipioRequest MunicipioRequest
        {
            get => municipioRequest;
            set
            {
                municipioRequest = value;
                OnPropertyChanged();

                // Sincroniza el formulario con los datos del objeto.
                NombreMunicipio = value?.NombreMunicipio ?? string.Empty;
            }
        }

        // Contexto: Departamento recibido desde la navegación (cascada).
        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set { departamentoRequest = value; OnPropertyChanged(); }
        }

        // (Opcional) País en contexto si querés usarlo para títulos o navegación.
        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set { paisRequest = value; OnPropertyChanged(); }
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
                // ((Command)SaveCommand).ChangeCanExecute(); // opcional si querés reevaluar CanExecute
            }
        }

        // Indica si los campos del formulario están bloqueados (solo lectura).
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;

        // Controla la visibilidad del botón Guardar (oculto en modo View).
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        // Título dinámico mostrado arriba del formulario según el modo.
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Municipio",
            FormMode.FormModeSelect.Edit => "Editar Municipio",
            FormMode.FormModeSelect.View => "Detalles del Municipio",
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
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Parámetros de regreso a la lista de Municipios del Departamento en contexto.
                        var parameters = new Dictionary<string, object>
                        {
                            { "Pais", PaisRequest},
                            { "Departamento", DepartamentoRequest },
                            { "TitlePage", $"Municipios de {DepartamentoRequest.NombreDepartamento.ToString()} - {PaisRequest.NombrePais.ToString()}"}
                        };
                        await GoToAsyncParameters("//MunicipioPage", parameters);
                    }                        
                }
                else
                {
                    // Parámetros de regreso a la lista de Municipios del Departamento en contexto.
                    var parameters = new Dictionary<string, object>
                    {
                        { "Pais", PaisRequest},
                        { "Departamento", DepartamentoRequest },
                        { "TitlePage", $"Municipios de {DepartamentoRequest.NombreDepartamento.ToString()} - {PaisRequest.NombrePais.ToString()}"}
                    };
                    await GoToAsyncParameters("//MunicipioPage", parameters);
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
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
                    await CreateMunicipioAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateMunicipioAsync();
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        // Crea un nuevo Municipio (confirmación → persistir → navegar → feedback).
        private async Task CreateMunicipioAsync()
        {
            try
            {
                // Validación de datos (formulario).
                if (!ValidateFieldsData()) return;

                // Determina si hay cambios significativos para guardar.
                IsCancel = ValidateFields();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del municipio?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        // Propaga los valores del formulario al objeto Municipio.
                        MunicipioRequest.NombreMunicipio = NombreMunicipio;
                        MunicipioRequest.DepartamentoId = DepartamentoRequest?.DepartamentoId;

                        // Valida que el usaurio tenga conexion a internet
                        bool tieneInternet = await TieneInternetAsync();

                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        // Llama a la API para crear el registro.
                        var response = await municipioApiService.CreateMunicipioAsync(MunicipioRequest);

                        if (response)
                        {
                            var parameters = new Dictionary<string, object>
                            {
                                { "Pais", PaisRequest},
                                { "Departamento", DepartamentoRequest },
                                { "TitlePage", $"Municipios de {DepartamentoRequest?.NombreDepartamento ?? "Departamento"}" }
                            };

                            await GoToAsyncParameters("//MunicipioPage", parameters);
                            _ = MostrarToastAsync("Éxito" + "Municipio guardado correctamente.");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error" + "No se pudo guardar el municipio.");
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

        // Actualiza un Municipio existente (confirmación → persistir → navegar → feedback).
        private async Task UpdateMunicipioAsync()
        {
            try
            {
                // Validación de datos (formulario).
                if (!ValidateFieldsData()) return;

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
                        MunicipioRequest.NombreMunicipio = NombreMunicipio;
                        MunicipioRequest.DepartamentoId = DepartamentoRequest?.DepartamentoId;

                        // Llama a la API para actualizar.
                        var response = await municipioApiService.UpdateMunicipioAsync(MunicipioRequest);
                        if (response)
                        {
                            var parameters = new Dictionary<string, object>
                            {
                                { "Pais", PaisRequest},
                                { "Departamento", DepartamentoRequest },
                                { "TitlePage", $"Municipios de {DepartamentoRequest?.NombreDepartamento ?? "Departamento"}" }
                            };

                            await GoToAsyncParameters("//MunicipioPage", parameters);
                            _ = MostrarToastAsync("Éxito" + "Municipio actualizado correctamente.");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error" + "No se pudo actualizar el municipio.");
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
            if (MunicipioRequest is null) return true;

            // Cambios detectables:
            // - Nombre del municipio
            if ((NombreMunicipio ?? string.Empty) != (MunicipioRequest.NombreMunicipio ?? string.Empty))
                return true;

            return false;
        }

        // ¿Los datos del formulario son válidos?
        private bool ValidateFieldsData()
        {
            // Nombre requerido
            if (string.IsNullOrWhiteSpace(NombreMunicipio))
            {
                _ = App.Current.MainPage.DisplayAlert("Validación", "Ingrese el nombre del municipio.", "OK");
                return false;
            }

            // Departamento en contexto requerido (cascada)
            if (DepartamentoRequest is null || DepartamentoRequest.DepartamentoId is null)
            {
                _ = App.Current.MainPage.DisplayAlert("Validación", "No se recibió un departamento válido.", "OK");
                return false;
            }

            // Objeto de trabajo requerido
            if (MunicipioRequest is null)
            {
                _ = App.Current.MainPage.DisplayAlert("Validación", "No se recibió el objeto municipio.", "OK");
                return false;
            }

            return true;
        }
    }
}
