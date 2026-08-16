using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Formulario de Propietarios con recepción atómica de parámetros.
    /// IQueryAttributable evita que View/Edit puedan observar temporalmente
    /// el valor por defecto del enum y terminar en Create por accidente.
    /// </summary>
    public sealed class PropietarioFormViewModel :
        GlobalService,
        IQueryAttributable
    {
        private static readonly Regex CorreoRegex =
            new(
                @"^[A-Za-z0-9._%+-]+@" +
                @"[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant);

        private readonly PropietarioApiService service =
            new();

        /*
         * Se inicia deliberadamente en View. Create solo se habilita después
         * de recibir explícitamente Mode=Create en la navegación.
         */
        private FormMode.FormModeSelect mode =
            FormMode.FormModeSelect.View;

        private PropietarioResponse? propietario;
        private string identificacion = string.Empty;
        private string nombreCompleto = string.Empty;
        private string telefono = string.Empty;
        private string correo = string.Empty;
        private string direccion = string.Empty;
        private string? modoSeleccionTexto;
        private bool parametrosRecibidos;
        private bool parametrosValidos;
        private bool validacionNavegacionEjecutada;

        public PropietarioFormViewModel()
        {
            GuardarCommand =
                new Command(
                    async () =>
                        await GuardarAsync(),
                    () =>
                        ParametrosValidos &&
                        ShowSaveButton &&
                        !IsBusy);

            CancelarCommand =
                new Command(
                    async () =>
                        await RegresarAsync(),
                    () =>
                        !IsBusy);

            EditarCommand =
                new Command(
                    () =>
                        Mode =
                            FormMode
                                .FormModeSelect
                                .Edit,
                    () =>
                        ParametrosValidos &&
                        Mode ==
                            FormMode
                                .FormModeSelect
                                .View &&
                        Propietario?.PropietarioId > 0 &&
                        CanEdit &&
                        !IsBusy);
        }

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Command EditarCommand { get; }

        public bool ParametrosValidos =>
            parametrosValidos;

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            private set
            {
                if (mode == value)
                    return;

                mode = value;
                OnPropertyChanged();
                NotificarModo();
            }
        }

        public PropietarioResponse? Propietario
        {
            get => propietario;
            private set
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
                ActualizarComandos();
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            private set
            {
                if (modoSeleccionTexto == value)
                    return;

                modoSeleccionTexto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsModoSeleccion));
            }
        }

        public bool EsModoSeleccion =>
            bool.TryParse(
                ModoSeleccionTexto,
                out bool seleccion) &&
            seleccion;

        public string Identificacion
        {
            get => identificacion;
            set =>
                Asignar(
                    ref identificacion,
                    value);
        }

        public string NombreCompleto
        {
            get => nombreCompleto;
            set =>
                Asignar(
                    ref nombreCompleto,
                    value);
        }

        public string Telefono
        {
            get => telefono;
            set =>
                Asignar(
                    ref telefono,
                    value);
        }

        public string Correo
        {
            get => correo;
            set =>
                Asignar(
                    ref correo,
                    value);
        }

        public string Direccion
        {
            get => direccion;
            set =>
                Asignar(
                    ref direccion,
                    value);
        }

        public bool IsReadOnly =>
            !ParametrosValidos ||
            Mode ==
                FormMode
                    .FormModeSelect
                    .View;

        public bool ShowSaveButton =>
            ParametrosValidos &&
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    CanAdd,

                FormMode.FormModeSelect.Edit =>
                    CanEdit,

                _ => false
            };

        public bool ShowEditButton =>
            ParametrosValidos &&
            Mode ==
                FormMode
                    .FormModeSelect
                    .View &&
            Propietario?.PropietarioId > 0 &&
            CanEdit;

        public new bool CanAdd =>
            PermissionService.Instance.HasAdd(
                InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance.HasUpdate(
                InterfazCodigos.Propietarios);

        public string Title
        {
            get
            {
                if (!ParametrosValidos)
                    return "Propietario";

                return Mode switch
                {
                    FormMode.FormModeSelect.Create =>
                        "Crear propietario",

                    FormMode.FormModeSelect.Edit =>
                        "Editar propietario",

                    FormMode.FormModeSelect.View =>
                        "Detalle del propietario",

                    _ =>
                        "Propietario"
                };
            }
        }

        /// <summary>
        /// Shell entrega todos los parámetros de la navegación en un único
        /// diccionario. Se validan como una unidad antes de habilitar acciones.
        /// </summary>
        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            parametrosRecibidos = true;

            bool modoValido =
                TryObtenerModo(
                    query,
                    out FormMode.FormModeSelect modoSolicitado);

            bool seleccion =
                TryObtenerBool(
                    query,
                    "ModoSeleccion");

            PropietarioResponse? recibido =
                TryObtenerPropietario(
                    query);

            bool requierePropietario =
                modoValido &&
                (modoSolicitado ==
                    FormMode.FormModeSelect.View ||
                 modoSolicitado ==
                    FormMode.FormModeSelect.Edit);

            bool propietarioValido =
                !requierePropietario ||
                recibido?.PropietarioId > 0;

            parametrosValidos =
                modoValido &&
                propietarioValido;

            ModoSeleccionTexto =
                seleccion.ToString();

            if (!parametrosValidos)
            {
                Mode =
                    FormMode
                        .FormModeSelect
                        .View;

                Propietario = null;
                LimpiarFormulario();
                NotificarModo();
                return;
            }

            if (modoSolicitado ==
                FormMode.FormModeSelect.Create)
            {
                Propietario = null;
                LimpiarFormulario();
            }
            else
            {
                Propietario =
                    ClonarPropietario(
                        recibido!);
            }

            Mode =
                modoSolicitado;

            OnPropertyChanged(nameof(ParametrosValidos));
            NotificarModo();
        }

        /// <summary>
        /// Segunda barrera contra rutas incompletas. View/Edit nunca se
        /// reinterpretan como Create; si faltan datos se informa y se regresa.
        /// </summary>
        public async Task<bool>
            ValidarNavegacionAsync()
        {
            if (ParametrosValidos)
                return true;

            if (validacionNavegacionEjecutada)
                return false;

            validacionNavegacionEjecutada = true;

            // Permite que Shell termine de entregar los atributos.
            await Task.Yield();

            if (ParametrosValidos)
                return true;

            string mensaje =
                parametrosRecibidos
                    ? "No se recibieron todos los datos necesarios para abrir el propietario."
                    : "No fue posible recibir los parámetros de navegación del propietario.";

            await MostrarErrorAsync(
                mensaje);

            await RegresarAsync();
            return false;
        }

        private async Task GuardarAsync()
        {
            if (IsBusy ||
                !ParametrosValidos ||
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

            string? error =
                Validar();

            if (error != null)
            {
                await MostrarAdvertenciaAsync(
                    error);
                return;
            }

            var request =
                new PropietarioGuardarRequest
                {
                    Identificacion =
                        Identificacion.Trim(),

                    NombreCompleto =
                        NombreCompleto.Trim(),

                    Telefono =
                        Limpiar(
                            Telefono),

                    Correo =
                        Limpiar(
                            Correo),

                    Direccion =
                        Limpiar(
                            Direccion),

                    Activo =
                        true
                };

            IsBusy = true;
            ActualizarComandos();

            PropietarioMutacionListado? mutacion = null;
            string mensajeExito = string.Empty;

            try
            {
                if (Mode ==
                    FormMode
                        .FormModeSelect
                        .Create)
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

                    PropietarioResponse creado =
                        ConstruirPropietario(
                            result.Data,
                            request,
                            null);

                    mutacion =
                        new PropietarioMutacionListado(
                            PropietarioMutacionListadoTipo.Creado,
                            creado);

                    mensajeExito =
                        string.IsNullOrWhiteSpace(
                            result.Message)
                            ? "Propietario creado correctamente."
                            : result.Message;
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

                    PropietarioResponse anterior =
                        ClonarPropietario(
                            Propietario);

                    ApiResult<bool> result =
                        await service
                            .ActualizarPropietarioResultAsync(
                                Propietario
                                    .PropietarioId,
                                request);

                    if (!result.Success ||
                        result.Data != true)
                    {
                        await MostrarErrorAsync(
                            result.Message);
                        return;
                    }

                    PropietarioResponse actualizado =
                        ConstruirPropietario(
                            Propietario.PropietarioId,
                            request,
                            anterior);

                    Propietario =
                        ClonarPropietario(
                            actualizado);

                    mutacion =
                        new PropietarioMutacionListado(
                            PropietarioMutacionListadoTipo.Actualizado,
                            actualizado,
                            anterior);

                    mensajeExito =
                        string.IsNullOrWhiteSpace(
                            result.Message)
                            ? "Propietario actualizado correctamente."
                            : result.Message;
                }
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }

            if (mutacion == null)
                return;

            /*
             * El listado decidirá si puede aplicar el DTO localmente o si un
             * cambio de orden/filtro exige un único GET de la página visible.
             */
            PropietarioVisitaService
                .RegistrarMutacion(
                    EsModoSeleccion,
                    mutacion);

            await MostrarExitoAsync(
                mensajeExito);

            await RegresarAsync();
        }

        private async Task RegresarAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(
                    "..");
            }
            catch
            {
                if (EsModoSeleccion)
                {
                    await GoToAsyncParameters(
                        AppRoutes.Propietarios,
                        new Dictionary<string, object>
                        {
                            ["ModoSeleccion"] =
                                "True"
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

            if (Identificacion
                .Trim()
                .Length > 50)
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

            if (NombreCompleto
                .Trim()
                .Length > 150)
            {
                return
                    "El nombre no puede superar 150 caracteres.";
            }

            if (!string.IsNullOrWhiteSpace(
                    Correo) &&
                !CorreoRegex.IsMatch(
                    Correo.Trim()))
            {
                return
                    "El correo electrónico no es válido.";
            }

            if (!string.IsNullOrWhiteSpace(
                    Telefono) &&
                Telefono.Trim().Length >
                    25)
            {
                return
                    "El teléfono no puede superar 25 caracteres.";
            }

            return null;
        }

        private void NotificarModo()
        {
            OnPropertyChanged(nameof(ParametrosValidos));
            OnPropertyChanged(nameof(IsReadOnly));
            OnPropertyChanged(nameof(ShowSaveButton));
            OnPropertyChanged(nameof(ShowEditButton));
            OnPropertyChanged(nameof(Title));
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
        }

        private void LimpiarFormulario()
        {
            Identificacion = string.Empty;
            NombreCompleto = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Direccion = string.Empty;
        }

        private void Asignar(
            ref string campo,
            string? valor,
            [System.Runtime.CompilerServices
                .CallerMemberName]
            string? propertyName = null)
        {
            string nuevo =
                valor ??
                string.Empty;

            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged(
                propertyName);
        }

        private static PropietarioResponse
            ConstruirPropietario(
                int propietarioId,
                PropietarioGuardarRequest request,
                PropietarioResponse? anterior)
        {
            return new PropietarioResponse
            {
                PropietarioId =
                    propietarioId,

                Identificacion =
                    request.Identificacion,

                NombreCompleto =
                    request.NombreCompleto,

                Telefono =
                    request.Telefono,

                Correo =
                    request.Correo,

                Direccion =
                    request.Direccion,

                Activo =
                    true,

                FechaRegistroUtc =
                    anterior?.FechaRegistroUtc ??
                    DateTime.UtcNow,

                TotalTerrenos =
                    anterior?.TotalTerrenos ??
                    0,

                UsuarioPortalId =
                    anterior?.UsuarioPortalId,

                UsuarioPortal =
                    anterior?.UsuarioPortal
            };
        }

        private static PropietarioResponse
            ClonarPropietario(
                PropietarioResponse item)
        {
            return new PropietarioResponse
            {
                PropietarioId =
                    item.PropietarioId,

                Identificacion =
                    item.Identificacion,

                NombreCompleto =
                    item.NombreCompleto,

                Telefono =
                    item.Telefono,

                Correo =
                    item.Correo,

                Direccion =
                    item.Direccion,

                Activo =
                    item.Activo,

                FechaRegistroUtc =
                    item.FechaRegistroUtc,

                TotalTerrenos =
                    item.TotalTerrenos,

                UsuarioPortalId =
                    item.UsuarioPortalId,

                UsuarioPortal =
                    item.UsuarioPortal
            };
        }

        private static bool TryObtenerModo(
            IDictionary<string, object> query,
            out FormMode.FormModeSelect modo)
        {
            modo =
                FormMode
                    .FormModeSelect
                    .View;

            if (!query.TryGetValue(
                    "Mode",
                    out object? valor) ||
                valor == null)
            {
                return false;
            }

            if (valor is FormMode.FormModeSelect tipado)
            {
                modo = tipado;
                return Enum.IsDefined(modo);
            }

            return Enum.TryParse(
                       valor.ToString(),
                       true,
                       out modo) &&
                   Enum.IsDefined(modo);
        }

        private static bool TryObtenerBool(
            IDictionary<string, object> query,
            string clave)
        {
            if (!query.TryGetValue(
                    clave,
                    out object? valor) ||
                valor == null)
            {
                return false;
            }

            if (valor is bool booleano)
                return booleano;

            return bool.TryParse(
                       valor.ToString(),
                       out bool resultado) &&
                   resultado;
        }

        private static PropietarioResponse?
            TryObtenerPropietario(
                IDictionary<string, object> query)
        {
            if (!query.TryGetValue(
                    "Propietario",
                    out object? valor))
            {
                return null;
            }

            return valor as PropietarioResponse;
        }

        private static string? Limpiar(
            string? valor) =>
            string.IsNullOrWhiteSpace(
                valor)
                ? null
                : valor.Trim();
    }
}
