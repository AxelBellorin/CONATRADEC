using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(
        nameof(Propietario),
        "Propietario")]
    [QueryProperty(
        nameof(ModoSeleccionTexto),
        "ModoSeleccion")]
    public sealed class PropietarioFormViewModel :
        GlobalService
    {
        private static readonly Regex CorreoRegex =
            new(
                @"^[A-Za-z0-9._%+-]+@" +
                @"[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant);

        private readonly PropietarioApiService service =
            new();

        private FormMode.FormModeSelect mode;

        private PropietarioResponse? propietario;

        private string identificacion =
            string.Empty;

        private string nombreCompleto =
            string.Empty;

        private string telefono =
            string.Empty;

        private string correo =
            string.Empty;

        private string direccion =
            string.Empty;

        private string? modoSeleccionTexto;

        public PropietarioFormViewModel()
        {
            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => ShowSaveButton && !IsBusy);

            CancelarCommand = new Command(
                async () => await RegresarAsync(),
                () => !IsBusy);

            EditarCommand = new Command(
                () => Mode =
                    FormMode.FormModeSelect.Edit,
                () =>
                    Mode ==
                    FormMode.FormModeSelect.View &&
                    CanEdit &&
                    !IsBusy);
        }

        public Command GuardarCommand { get; }

        public Command CancelarCommand { get; }

        public Command EditarCommand { get; }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                    return;

                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(ShowEditButton));
                OnPropertyChanged(nameof(Title));

                ActualizarComandos();
            }
        }

        public PropietarioResponse? Propietario
        {
            get => propietario;
            set
            {
                propietario = value;

                if (value != null)
                {
                    Identificacion =
                        value.Identificacion;

                    NombreCompleto =
                        value.NombreCompleto;

                    Telefono =
                        value.Telefono ??
                        string.Empty;

                    Correo =
                        value.Correo ??
                        string.Empty;

                    Direccion =
                        value.Direccion ??
                        string.Empty;
                }

                OnPropertyChanged();
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            set
            {
                modoSeleccionTexto = value;
                OnPropertyChanged();
            }
        }

        public string Identificacion
        {
            get => identificacion;
            set => Asignar(
                ref identificacion,
                value);
        }

        public string NombreCompleto
        {
            get => nombreCompleto;
            set => Asignar(
                ref nombreCompleto,
                value);
        }

        public string Telefono
        {
            get => telefono;
            set => Asignar(
                ref telefono,
                value);
        }

        public string Correo
        {
            get => correo;
            set => Asignar(
                ref correo,
                value);
        }

        public string Direccion
        {
            get => direccion;
            set => Asignar(
                ref direccion,
                value);
        }

        public bool IsReadOnly =>
            Mode ==
            FormMode.FormModeSelect.View;

        public bool ShowSaveButton =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    CanAdd,

                FormMode.FormModeSelect.Edit =>
                    CanEdit,

                _ => false
            };

        public bool ShowEditButton =>
            Mode ==
            FormMode.FormModeSelect.View &&
            CanEdit;

        public new bool CanAdd =>
            PermissionService.Instance
                .HasAdd(
                    InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance
                .HasUpdate(
                    InterfazCodigos.Propietarios);

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear propietario",

                FormMode.FormModeSelect.Edit =>
                    "Editar propietario",

                FormMode.FormModeSelect.View =>
                    "Detalle del propietario",

                _ => "Propietario"
            };

        private async Task GuardarAsync()
        {
            if (IsBusy ||
                IsReadOnly ||
                !ShowSaveButton)
            {
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await MostrarAdvertenciaAsync(
                    "Esta operación requiere conexión a internet.");
                return;
            }

            string? error = Validar();

            if (error != null)
            {
                await MostrarAdvertenciaAsync(error);
                return;
            }

            /*
             * El estado no se administra desde el formulario.
             * Los propietarios nuevos y editados permanecen activos.
             * La desactivación solo se realiza mediante Eliminar y permiso.
             */
            var request =
                new PropietarioGuardarRequest
                {
                    Identificacion =
                        Identificacion.Trim(),

                    NombreCompleto =
                        NombreCompleto.Trim(),

                    Telefono =
                        Limpiar(Telefono),

                    Correo =
                        Limpiar(Correo),

                    Direccion =
                        Limpiar(Direccion),

                    Activo = true
                };

            IsBusy = true;
            ActualizarComandos();

            try
            {
                if (Mode ==
                    FormMode.FormModeSelect.Create)
                {
                    ApiResult<int> result =
                        await service
                            .CrearPropietarioResultAsync(
                                request);

                    if (!result.Success ||
                        result.Data <= 0)
                    {
                        await MostrarErrorAsync(
                            result.Message);
                        return;
                    }

                    await MostrarExitoAsync(
                        "Propietario creado correctamente.");
                }
                else
                {
                    if (Propietario?.PropietarioId
                        is null or <= 0)
                    {
                        await MostrarErrorAsync(
                            "No se encontró el propietario.");
                        return;
                    }

                    ApiResult<bool> result =
                        await service
                            .ActualizarPropietarioResultAsync(
                                Propietario.PropietarioId,
                                request);

                    if (!result.Success ||
                        result.Data != true)
                    {
                        await MostrarErrorAsync(
                            result.Message);
                        return;
                    }

                    await MostrarExitoAsync(
                        "Propietario actualizado correctamente.");
                }

                await RegresarAsync();
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task RegresarAsync()
        {
            try
            {
                /*
                 * El formulario siempre se abre desde la lista existente.
                 * Se debe retirar esta página de la pila, no navegar hacia
                 * otra lista nueva. La navegación anterior creaba una copia
                 * adicional de Propietarios en cada Guardar o Regresar y era
                 * la causa directa del ciclo del botón Atrás.
                 */
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                /*
                 * Respaldo para una apertura excepcional sin página previa.
                 */
                if (bool.TryParse(
                        ModoSeleccionTexto,
                        out bool seleccion) &&
                    seleccion)
                {
                    await GoToAsyncParameters(
                        AppRoutes.Propietarios,
                        new Dictionary<string, object>
                        {
                            ["ModoSeleccion"] = "True"
                        });
                }
                else
                {
                    await GoToAsyncParameters(
                        AppRoutes.Configuracion);
                }
            }
        }

        private string? Validar()
        {
            if (string.IsNullOrWhiteSpace(
                    Identificacion))
            {
                return
                    "La identificación es obligatoria.";
            }

            if (Identificacion.Trim().Length > 50)
            {
                return
                    "La identificación no puede superar 50 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(
                    NombreCompleto))
            {
                return
                    "El nombre completo es obligatorio.";
            }

            if (NombreCompleto.Trim().Length > 150)
            {
                return
                    "El nombre no puede superar 150 caracteres.";
            }

            if (!string.IsNullOrWhiteSpace(Correo) &&
                !CorreoRegex.IsMatch(Correo.Trim()))
            {
                return
                    "El correo electrónico no es válido.";
            }

            if (!string.IsNullOrWhiteSpace(Telefono) &&
                Telefono.Trim().Length > 25)
            {
                return
                    "El teléfono no puede superar 25 caracteres.";
            }

            return null;
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
        }

        private void Asignar(
            ref string campo,
            string? valor,
            [System.Runtime.CompilerServices
                .CallerMemberName]
            string? propertyName = null)
        {
            string nuevo =
                valor ?? string.Empty;

            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged(propertyName);
        }

        private static string? Limpiar(
            string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
    }
}
