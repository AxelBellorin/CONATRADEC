using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    public class UserFormViewModel : GlobalService
    {
        private static readonly Regex CorreoRegex = new(
            @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled);

        private static readonly Regex IdentificacionRegex = new(
            @"^\d{3}-\d{6}-\d{4}[A-Za-z]$",
            RegexOptions.Compiled);

        private static readonly Regex ContrasenaRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_\-]).{8,}$",
            RegexOptions.Compiled);

        private UserRequest user = new();
        private FormMode.FormModeSelect mode;
        private bool initialized;
        private bool suppressLocationEvents;
        private bool initialStateCaptured;

        private string originalNombreCompleto = string.Empty;
        private string originalIdentificacion = string.Empty;
        private string originalCorreo = string.Empty;
        private string originalTelefono = string.Empty;
        private DateOnly? originalFechaNacimiento;
        private int? originalRolId;
        private int? originalMunicipioId;

        private string nombreUsuario = string.Empty;
        private string claveUsuario = string.Empty;
        private string nombreCompletoUsuario = string.Empty;
        private string identificacionUsuario = string.Empty;
        private string correoUsuario = string.Empty;
        private string telefonoUsuario = string.Empty;
        private DateOnly? fechaNacimientoUsuario;
        private DateTime fechaNacimientoDate = DateTime.Today.AddYears(-18);
        private string urlImagenUsuario = string.Empty;
        private string passwordToggleIcon = "eye.png";
        private bool isPasswordHidden = true;
        private FileResult? imagenSeleccionada;

        private readonly UserApiService userApiService = new();
        private readonly RolApiService rolApiService = new();
        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();

        public UserFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsBusy && !IsReadOnly);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);

            SeleccionarImagenCommand = new Command(
                async () => await SeleccionarImagenAsync(),
                () => !IsBusy && EnabledImagenField);

            TogglePasswordCommand = new Command(OnTogglePassword);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command SeleccionarImagenCommand { get; }
        public Command TogglePasswordCommand { get; }

        public ObservableCollection<RolResponse> Roles { get; } = new();
        public ObservableCollection<PaisResponse> Paises { get; } = new();
        public ObservableCollection<DepartamentoResponse> Departamentos { get; } = new();
        public ObservableCollection<MunicipioResponse> Municipios { get; } = new();

        private RolResponse? rolSeleccionado;
        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        public string NombreUsuario
        {
            get => nombreUsuario;
            set
            {
                nombreUsuario = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ClaveUsuario
        {
            get => claveUsuario;
            set
            {
                claveUsuario = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordHelpText));
            }
        }

        public string NombreCompletoUsuario
        {
            get => nombreCompletoUsuario;
            set
            {
                nombreCompletoUsuario = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string IdentificacionUsuario
        {
            get => identificacionUsuario;
            set
            {
                identificacionUsuario = (value ?? string.Empty).Trim().ToUpperInvariant();
                OnPropertyChanged();
            }
        }

        public string CorreoUsuario
        {
            get => correoUsuario;
            set
            {
                correoUsuario = (value ?? string.Empty).Trim();
                OnPropertyChanged();
            }
        }

        public string TelefonoUsuario
        {
            get => telefonoUsuario;
            set
            {
                telefonoUsuario = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public DateOnly? FechaNacimientoUsuario
        {
            get => fechaNacimientoUsuario;
            set
            {
                fechaNacimientoUsuario = value;

                if (value.HasValue)
                    fechaNacimientoDate = value.Value.ToDateTime(TimeOnly.MinValue);

                OnPropertyChanged();
                OnPropertyChanged(nameof(FechaNacimientoDate));
            }
        }

        public DateTime FechaNacimientoDate
        {
            get => fechaNacimientoDate;
            set
            {
                if (fechaNacimientoDate == value)
                    return;

                fechaNacimientoDate = value;
                fechaNacimientoUsuario = DateOnly.FromDateTime(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(FechaNacimientoUsuario));
            }
        }

        public string UrlImagenUsuario
        {
            get => urlImagenUsuario;
            set
            {
                urlImagenUsuario = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagenPreview));
            }
        }

        public FileResult? ImagenSeleccionada
        {
            get => imagenSeleccionada;
            set
            {
                imagenSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagenPreview));
            }
        }

        public string ImagenPreview =>
            ImagenSeleccionada?.FullPath ?? UrlImagenUsuario;

        public string PasswordToggleIcon
        {
            get => passwordToggleIcon;
            set
            {
                passwordToggleIcon = value;
                OnPropertyChanged();
            }
        }

        public bool IsPasswordHidden
        {
            get => isPasswordHidden;
            set
            {
                isPasswordHidden = value;
                OnPropertyChanged();
            }
        }

        public RolResponse? RolSeleccionado
        {
            get => rolSeleccionado;
            set
            {
                rolSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public PaisResponse? PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                if (ReferenceEquals(paisSeleccionado, value))
                    return;

                paisSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPickDepartamento));

                if (!suppressLocationEvents)
                    _ = OnPaisChangedAsync(value);
            }
        }

        public DepartamentoResponse? DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                if (ReferenceEquals(departamentoSeleccionado, value))
                    return;

                departamentoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPickMunicipio));

                if (!suppressLocationEvents)
                    _ = OnDepartamentoChangedAsync(value);
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                municipioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public UserRequest User
        {
            get => user;
            set
            {
                user = value ?? new UserRequest();
                ApplyUserToForm(user);
                OnPropertyChanged();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;

                if (mode == FormMode.FormModeSelect.Create)
                    ResetForm();

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(ShowPasswordField));
                OnPropertyChanged(nameof(IsUserNameReadOnly));
                OnPropertyChanged(nameof(PasswordPlaceholder));
                OnPropertyChanged(nameof(PasswordHelpText));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(EnabledImagenField));
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
                RefreshCommands();
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEnabled => !IsReadOnly;
        public bool IsUserNameReadOnly => IsReadOnly || Mode == FormMode.FormModeSelect.Edit;
        public bool ShowSaveButton => !IsReadOnly;
        public bool ShowPasswordField => !IsReadOnly;
        public bool EnabledImagenField => !IsReadOnly;
        public bool CanPickDepartamento => IsEnabled && PaisSeleccionado != null;
        public bool CanPickMunicipio => IsEnabled && DepartamentoSeleccionado != null;

        public string PasswordPlaceholder =>
            Mode == FormMode.FormModeSelect.Create
                ? "Contraseña"
                : "Nueva contraseña (opcional)";

        public string PasswordHelpText =>
            Mode == FormMode.FormModeSelect.Edit
                ? "Déjelo vacío para conservar la contraseña actual."
                : "Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.";

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear usuario",
            FormMode.FormModeSelect.Edit => "Editar usuario",
            FormMode.FormModeSelect.View => "Detalles del usuario",
            _ => "Usuario"
        };

        /// <summary>
        /// Inicializa una única vez la instancia actual del formulario.
        ///
        /// Ver utiliza exclusivamente los datos ya recibidos por el listado.
        /// Crear/Editar cargan catálogos pequeños bajo demanda y los reutilizan
        /// durante toda la visita al módulo Usuarios.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (IsBusy || initialized)
                return;

            IsBusy = true;
            RefreshCommands();

            try
            {
                UsuarioVisitaService.AsegurarVisita();

                if (IsReadOnly)
                {
                    PrepararCatalogosSoloLectura();
                    initialized = true;
                    CaptureInitialState();
                    return;
                }

                var rolesTask = ObtenerRolesVisitaAsync();
                var paisesTask = ObtenerPaisesVisitaAsync();

                await Task.WhenAll(rolesTask, paisesTask);

                ApiResult<List<RolResponse>> rolesResult = await rolesTask;
                ApiResult<List<PaisResponse>> paisesResult = await paisesTask;

                if (!rolesResult.Success)
                {
                    await MostrarToastAsync(rolesResult.Message);
                    return;
                }

                if (!paisesResult.Success)
                {
                    await MostrarToastAsync(paisesResult.Message);
                    return;
                }

                ReplaceCollection(Roles, rolesResult.Data);
                ReplaceCollection(Paises, paisesResult.Data);

                if (User.RolId > 0)
                {
                    RolSeleccionado = Roles.FirstOrDefault(
                        item => item.RolId == User.RolId);
                }

                if (User.MunicipioId > 0)
                    await ResolverUbicacionAsync();

                initialized = true;
                CaptureInitialState();
            }
            catch
            {
                await MostrarToastAsync(
                    "No fue posible cargar el formulario de usuario. Intente nuevamente.");
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private void PrepararCatalogosSoloLectura()
        {
            suppressLocationEvents = true;

            try
            {
                Roles.Clear();
                Paises.Clear();
                Departamentos.Clear();
                Municipios.Clear();

                if (User.RolId is > 0)
                {
                    var rol = new RolResponse
                    {
                        RolId = User.RolId,
                        NombreRol = User.RolNombre ?? string.Empty
                    };

                    Roles.Add(rol);
                    RolSeleccionado = rol;
                }

                if (User.PaisId is > 0 ||
                    !string.IsNullOrWhiteSpace(User.PaisNombre))
                {
                    var pais = new PaisResponse
                    {
                        PaisId = User.PaisId ?? 0,
                        NombrePais = User.PaisNombre ?? string.Empty,
                        Activo = true
                    };

                    Paises.Add(pais);
                    PaisSeleccionado = pais;
                }

                if (User.DepartamentoId is > 0 ||
                    !string.IsNullOrWhiteSpace(User.DepartamentoNombre))
                {
                    var departamento = new DepartamentoResponse
                    {
                        DepartamentoId = User.DepartamentoId,
                        PaisId = User.PaisId,
                        NombreDepartamento =
                            User.DepartamentoNombre ?? string.Empty,
                        NombrePais = User.PaisNombre ?? string.Empty,
                        Activo = true
                    };

                    Departamentos.Add(departamento);
                    DepartamentoSeleccionado = departamento;
                }

                if (User.MunicipioId is > 0)
                {
                    var municipio = new MunicipioResponse
                    {
                        MunicipioId = User.MunicipioId,
                        DepartamentoId = User.DepartamentoId,
                        PaisId = User.PaisId,
                        NombreMunicipio = User.MunicipioNombre ?? string.Empty,
                        NombreDepartamento =
                            User.DepartamentoNombre ?? string.Empty,
                        NombrePais = User.PaisNombre ?? string.Empty,
                        Activo = true
                    };

                    Municipios.Add(municipio);
                    MunicipioSeleccionado = municipio;
                }
            }
            finally
            {
                suppressLocationEvents = false;
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
            }
        }

        private async Task ResolverUbicacionAsync()
        {
            if (User.MunicipioId is not > 0)
                return;

            /*
             * El endpoint paginado de Usuarios ya entrega la jerarquía de la
             * ubicación. Así evitamos descargar todos los municipios del país
             * solamente para resolver uno.
             */
            if (User.PaisId is not > 0 ||
                User.DepartamentoId is not > 0)
            {
                await MostrarToastAsync(
                    "No fue posible resolver la ubicación del usuario con los datos recibidos.");
                return;
            }

            PaisResponse? pais = Paises.FirstOrDefault(
                item => item.PaisId == User.PaisId);

            if (pais == null)
            {
                pais = new PaisResponse
                {
                    PaisId = User.PaisId.Value,
                    NombrePais = User.PaisNombre ?? string.Empty,
                    Activo = true
                };

                Paises.Add(pais);
            }

            ApiResult<List<DepartamentoResponse>> departamentosResult =
                await ObtenerDepartamentosVisitaAsync(pais.PaisId);

            if (!departamentosResult.Success)
            {
                await MostrarToastAsync(departamentosResult.Message);
                return;
            }

            DepartamentoResponse? departamento =
                departamentosResult.Data?.FirstOrDefault(
                    item => item.DepartamentoId == User.DepartamentoId);

            if (departamento == null)
            {
                departamento = new DepartamentoResponse
                {
                    DepartamentoId = User.DepartamentoId,
                    PaisId = User.PaisId,
                    NombreDepartamento =
                        User.DepartamentoNombre ?? string.Empty,
                    NombrePais = User.PaisNombre ?? string.Empty,
                    Activo = true
                };
            }

            ApiResult<List<MunicipioResponse>> municipiosResult =
                await ObtenerMunicipiosVisitaAsync(
                    departamento.DepartamentoId ?? 0);

            if (!municipiosResult.Success)
            {
                await MostrarToastAsync(municipiosResult.Message);
                return;
            }

            MunicipioResponse? municipio =
                municipiosResult.Data?.FirstOrDefault(
                    item => item.MunicipioId == User.MunicipioId);

            municipio ??= new MunicipioResponse
            {
                MunicipioId = User.MunicipioId,
                DepartamentoId = User.DepartamentoId,
                PaisId = User.PaisId,
                NombreMunicipio = User.MunicipioNombre ?? string.Empty,
                NombreDepartamento =
                    User.DepartamentoNombre ?? string.Empty,
                NombrePais = User.PaisNombre ?? string.Empty,
                Activo = true
            };

            suppressLocationEvents = true;

            try
            {
                ReplaceCollection(
                    Departamentos,
                    departamentosResult.Data);

                if (!Departamentos.Any(
                        item => item.DepartamentoId == departamento.DepartamentoId))
                {
                    Departamentos.Add(departamento);
                }

                ReplaceCollection(
                    Municipios,
                    municipiosResult.Data);

                if (!Municipios.Any(
                        item => item.MunicipioId == municipio.MunicipioId))
                {
                    Municipios.Add(municipio);
                }

                PaisSeleccionado = pais;
                DepartamentoSeleccionado = departamento;
                MunicipioSeleccionado = municipio;
            }
            finally
            {
                suppressLocationEvents = false;
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
            }
        }

        private async Task OnPaisChangedAsync(PaisResponse? pais)
        {
            DepartamentoSeleccionado = null;
            MunicipioSeleccionado = null;
            Departamentos.Clear();
            Municipios.Clear();

            if (pais?.PaisId is not > 0)
                return;

            ApiResult<List<DepartamentoResponse>> result =
                await ObtenerDepartamentosVisitaAsync(pais.PaisId);

            if (!result.Success)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            if (PaisSeleccionado?.PaisId != pais.PaisId)
                return;

            ReplaceCollection(Departamentos, result.Data);
        }

        private async Task OnDepartamentoChangedAsync(
            DepartamentoResponse? departamento)
        {
            MunicipioSeleccionado = null;
            Municipios.Clear();

            if (departamento?.DepartamentoId is not > 0)
                return;

            int departamentoId = departamento.DepartamentoId.Value;

            ApiResult<List<MunicipioResponse>> result =
                await ObtenerMunicipiosVisitaAsync(departamentoId);

            if (!result.Success)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            if (DepartamentoSeleccionado?.DepartamentoId != departamentoId)
                return;

            ReplaceCollection(Municipios, result.Data);
        }

        private async Task<ApiResult<List<RolResponse>>>
            ObtenerRolesVisitaAsync()
        {
            if (UsuarioVisitaService.IntentarObtenerRoles(
                    out List<RolResponse>? cache) &&
                cache != null)
            {
                return ApiResult<List<RolResponse>>.Ok(cache);
            }

            ApiResult<ObservableCollection<RolResponse>> result =
                await rolApiService.GetRolResultAsync();

            if (!result.Success)
            {
                return ApiResult<List<RolResponse>>.Fail(
                    result.Message,
                    result.StatusCode);
            }

            List<RolResponse> items = result.Data?
                .Where(item => item.RolId is > 0)
                .ToList()
                ?? new List<RolResponse>();

            UsuarioVisitaService.GuardarRoles(items);
            return ApiResult<List<RolResponse>>.Ok(items);
        }

        private async Task<ApiResult<List<PaisResponse>>>
            ObtenerPaisesVisitaAsync()
        {
            if (UsuarioVisitaService.IntentarObtenerPaises(
                    out List<PaisResponse>? cache) &&
                cache != null)
            {
                return ApiResult<List<PaisResponse>>.Ok(cache);
            }

            ApiResult<ObservableCollection<PaisResponse>> result =
                await paisApiService.GetPaisResultAsync();

            if (!result.Success)
            {
                return ApiResult<List<PaisResponse>>.Fail(
                    result.Message,
                    result.StatusCode);
            }

            List<PaisResponse> items = result.Data?
                .Where(item => item.PaisId > 0)
                .ToList()
                ?? new List<PaisResponse>();

            UsuarioVisitaService.GuardarPaises(items);
            return ApiResult<List<PaisResponse>>.Ok(items);
        }

        private async Task<ApiResult<List<DepartamentoResponse>>>
            ObtenerDepartamentosVisitaAsync(int paisId)
        {
            if (paisId <= 0)
            {
                return ApiResult<List<DepartamentoResponse>>.Fail(
                    "Seleccione un país válido.");
            }

            if (UsuarioVisitaService.IntentarObtenerDepartamentos(
                    paisId,
                    out List<DepartamentoResponse>? cache) &&
                cache != null)
            {
                return ApiResult<List<DepartamentoResponse>>.Ok(cache);
            }

            ApiResult<ObservableCollection<DepartamentoResponse>> result =
                await departamentoApiService
                    .GetDepartamentosResultAsync(paisId);

            if (!result.Success)
            {
                return ApiResult<List<DepartamentoResponse>>.Fail(
                    result.Message,
                    result.StatusCode);
            }

            List<DepartamentoResponse> items = result.Data?
                .Where(item => item.DepartamentoId is > 0)
                .ToList()
                ?? new List<DepartamentoResponse>();

            UsuarioVisitaService.GuardarDepartamentos(paisId, items);
            return ApiResult<List<DepartamentoResponse>>.Ok(items);
        }

        private async Task<ApiResult<List<MunicipioResponse>>>
            ObtenerMunicipiosVisitaAsync(int departamentoId)
        {
            if (departamentoId <= 0)
            {
                return ApiResult<List<MunicipioResponse>>.Fail(
                    "Seleccione un departamento válido.");
            }

            if (UsuarioVisitaService.IntentarObtenerMunicipios(
                    departamentoId,
                    out List<MunicipioResponse>? cache) &&
                cache != null)
            {
                return ApiResult<List<MunicipioResponse>>.Ok(cache);
            }

            ApiResult<ObservableCollection<MunicipioResponse>> result =
                await municipioApiService
                    .GetMunicipiosResultAsync(departamentoId);

            if (!result.Success)
            {
                return ApiResult<List<MunicipioResponse>>.Fail(
                    result.Message,
                    result.StatusCode);
            }

            List<MunicipioResponse> items = result.Data?
                .Where(item => item.MunicipioId is > 0)
                .ToList()
                ?? new List<MunicipioResponse>();

            UsuarioVisitaService.GuardarMunicipios(
                departamentoId,
                items);

            return ApiResult<List<MunicipioResponse>>.Ok(items);
        }

        private async Task SaveAsync()
        {
            if (IsBusy || IsReadOnly)
                return;

            string? validationMessage = ValidateFields();

            if (validationMessage != null)
            {
                await MostrarToastAsync(validationMessage);
                return;
            }

            string action = Mode == FormMode.FormModeSelect.Create
                ? "guardar"
                : "actualizar";

            bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                Mode == FormMode.FormModeSelect.Create
                    ? "Guardar usuario"
                    : "Actualizar usuario",
                $"¿Desea {action} la información del usuario?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;
            RefreshCommands();

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateUserAsync();
                else
                    await UpdateUserAsync();
            }
            finally
            {
                IsBusy = false;
                RefreshCommands();
            }
        }

        private async Task CreateUserAsync()
        {
            UserRequest request = BuildRequestForCreate();
            ApiResult<UserRequest> result =
                await userApiService.CreateUserResultAsync(request);

            if (!result.Success || result.Data?.UsuarioId is not > 0)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            string? imagenActual = result.Data.UrlImagenUsuario;

            if (ImagenSeleccionada != null)
            {
                ApiResult<bool> imageResult =
                    await userApiService.SubirImagenResultAsync(
                        result.Data.UsuarioId,
                        ImagenSeleccionada);

                if (!imageResult.Success)
                {
                    await MostrarToastAsync(
                        $"El usuario fue creado, pero la imagen no se pudo guardar: {imageResult.Message}");
                }
                else if (!string.IsNullOrWhiteSpace(
                             ImagenSeleccionada.FullPath))
                {
                    // La próxima visita recibirá la URL del servidor. Durante la
                    // visita actual puede mostrarse la misma imagen local elegida.
                    imagenActual = ImagenSeleccionada.FullPath;
                }
            }

            UsuarioVisitaService.RegistrarCambio(
                new UsuarioVisitaCambio
                {
                    Tipo = UsuarioVisitaCambioTipo.Creado,
                    Usuario = ConstruirRespuestaVisita(
                        result.Data,
                        imagenActual)
                });

            ResetForm();
            await GoToAsyncParameters("//UserPage");
            await MostrarToastAsync("Usuario creado correctamente.");
        }

        private async Task UpdateUserAsync()
        {
            if (User.UsuarioId is not > 0)
            {
                await MostrarToastAsync(
                    "No se encontró el identificador del usuario que desea actualizar.");
                return;
            }

            UserRequest request = BuildRequestForUpdate();
            ApiResult<UserRequest> result =
                await userApiService.UpdateUserResultAsync(request);

            if (!result.Success || result.Data?.UsuarioId is not > 0)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            string? imagenActual =
                result.Data.UrlImagenUsuario ?? UrlImagenUsuario;

            if (ImagenSeleccionada != null)
            {
                ApiResult<bool> imageResult =
                    await userApiService.SubirImagenResultAsync(
                        result.Data.UsuarioId,
                        ImagenSeleccionada);

                if (!imageResult.Success)
                {
                    await MostrarToastAsync(
                        $"Los datos fueron actualizados, pero la imagen no se pudo guardar: {imageResult.Message}");
                }
                else if (!string.IsNullOrWhiteSpace(
                             ImagenSeleccionada.FullPath))
                {
                    imagenActual = ImagenSeleccionada.FullPath;
                }
            }

            UsuarioVisitaService.RegistrarCambio(
                new UsuarioVisitaCambio
                {
                    Tipo = UsuarioVisitaCambioTipo.Actualizado,
                    Usuario = ConstruirRespuestaVisita(
                        result.Data,
                        imagenActual)
                });

            ClaveUsuario = string.Empty;
            await GoToAsyncParameters("//UserPage");
            await MostrarToastAsync("Usuario actualizado correctamente.");
        }

        private UserResponse ConstruirRespuestaVisita(
            UserRequest resultado,
            string? imagenActual)
        {
            bool esInterno = resultado.EsInterno ?? User.EsInterno ?? true;

            return new UserResponse
            {
                UsuarioId = resultado.UsuarioId ?? User.UsuarioId,
                NombreUsuario = resultado.NombreUsuario ?? User.NombreUsuario,
                IdentificacionUsuario =
                    resultado.IdentificacionUsuario ?? IdentificacionUsuario,
                NombreCompletoUsuario =
                    resultado.NombreCompletoUsuario ?? NombreCompletoUsuario,
                CorreoUsuario = resultado.CorreoUsuario ?? CorreoUsuario,
                TelefonoUsuario = resultado.TelefonoUsuario ?? TelefonoUsuario,
                FechaNacimientoUsuario =
                    resultado.FechaNacimientoUsuario ?? FechaNacimientoUsuario,
                RolId = RolSeleccionado?.RolId ?? resultado.RolId ?? User.RolId,
                RolNombre =
                    RolSeleccionado?.NombreRol ??
                    resultado.RolNombre ??
                    User.RolNombre,
                ProcedenciaId =
                    resultado.ProcedenciaId ?? User.ProcedenciaId,
                ProcedenciaNombre =
                    resultado.ProcedenciaNombre ??
                    User.ProcedenciaNombre ??
                    (esInterno ? "Interno" : "Externo"),
                EsInterno = esInterno,
                MunicipioId =
                    MunicipioSeleccionado?.MunicipioId ??
                    resultado.MunicipioId ??
                    User.MunicipioId,
                MunicipioNombre =
                    MunicipioSeleccionado?.NombreMunicipio ??
                    User.MunicipioNombre,
                DepartamentoId =
                    DepartamentoSeleccionado?.DepartamentoId ??
                    User.DepartamentoId,
                DepartamentoNombre =
                    DepartamentoSeleccionado?.NombreDepartamento ??
                    User.DepartamentoNombre,
                PaisId =
                    PaisSeleccionado?.PaisId ??
                    User.PaisId,
                PaisNombre =
                    PaisSeleccionado?.NombrePais ??
                    User.PaisNombre,
                UrlImagenUsuario = imagenActual ?? string.Empty
            };
        }

        private UserRequest BuildRequestForCreate() => new()
        {
            NombreUsuario = NombreUsuario.Trim(),
            NombreCompletoUsuario = NombreCompletoUsuario.Trim(),
            CorreoUsuario = CorreoUsuario.Trim(),
            TelefonoUsuario = TelefonoUsuario.Trim(),
            FechaNacimientoUsuario = FechaNacimientoUsuario,
            IdentificacionUsuario = IdentificacionUsuario.Trim().ToUpperInvariant(),
            ClaveUsuario = ClaveUsuario,
            NuevaClaveUsuario = null,
            RolId = RolSeleccionado?.RolId,
            MunicipioId = MunicipioSeleccionado?.MunicipioId,
            EsInterno = true,
            UrlImagenUsuario = string.Empty
        };

        private UserRequest BuildRequestForUpdate() => new()
        {
            UsuarioId = User.UsuarioId,
            NombreUsuario = User.NombreUsuario,
            NombreCompletoUsuario = NombreCompletoUsuario.Trim(),
            CorreoUsuario = CorreoUsuario.Trim(),
            TelefonoUsuario = TelefonoUsuario.Trim(),
            FechaNacimientoUsuario = FechaNacimientoUsuario,
            IdentificacionUsuario = IdentificacionUsuario.Trim().ToUpperInvariant(),
            ClaveUsuario = null,
            NuevaClaveUsuario = string.IsNullOrWhiteSpace(ClaveUsuario)
                ? null
                : ClaveUsuario,
            RolId = RolSeleccionado?.RolId ?? User.RolId,
            MunicipioId = MunicipioSeleccionado?.MunicipioId ?? User.MunicipioId,
            EsInterno = User.EsInterno ?? true,
            UrlImagenUsuario = UrlImagenUsuario ?? string.Empty
        };

        private string? ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(NombreUsuario))
                return "Ingrese el nombre de usuario.";

            if (string.IsNullOrWhiteSpace(NombreCompletoUsuario))
                return "Ingrese el nombre completo del usuario.";

            if (string.IsNullOrWhiteSpace(IdentificacionUsuario))
                return "Ingrese la identificación del usuario.";

            if (!IdentificacionRegex.IsMatch(IdentificacionUsuario.Trim()))
            {
                return "La identificación debe tener el formato 001-080701-1050R.";
            }

            if (string.IsNullOrWhiteSpace(CorreoUsuario))
                return "Ingrese el correo electrónico del usuario.";

            if (!CorreoRegex.IsMatch(CorreoUsuario.Trim()))
                return "Ingrese un correo electrónico válido, por ejemplo: usuario@dominio.com.";

            if (string.IsNullOrWhiteSpace(TelefonoUsuario))
                return "Ingrese el número de teléfono.";

            if (!Regex.IsMatch(TelefonoUsuario.Trim(), @"^\d{8}$"))
                return "El teléfono debe contener exactamente 8 dígitos.";

            if (FechaNacimientoUsuario == null)
                return "Seleccione la fecha de nacimiento.";

            if (!EsMayorDeEdad(FechaNacimientoUsuario))
                return "El usuario debe tener al menos 18 años.";

            if (RolSeleccionado == null)
                return "Seleccione un rol.";

            if (PaisSeleccionado == null)
                return "Seleccione un país.";

            if (DepartamentoSeleccionado == null)
                return "Seleccione un departamento.";

            if (MunicipioSeleccionado == null)
                return "Seleccione un municipio.";

            bool passwordRequired = Mode == FormMode.FormModeSelect.Create;
            bool passwordProvided = !string.IsNullOrWhiteSpace(ClaveUsuario);

            if (passwordRequired && !passwordProvided)
                return "Ingrese una contraseña.";

            if (passwordProvided && !ContrasenaRegex.IsMatch(ClaveUsuario))
            {
                return "La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.";
            }

            return null;
        }

        private static bool EsMayorDeEdad(DateOnly? fecha)
        {
            if (!fecha.HasValue)
                return false;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            int edad = hoy.Year - fecha.Value.Year;

            if (hoy < fecha.Value.AddYears(edad))
                edad--;

            return edad >= 18;
        }

        private async Task SeleccionarImagenAsync()
        {
            if (IsBusy || !EnabledImagenField)
                return;

            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Seleccione una imagen",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                    return;

                string extension = Path.GetExtension(result.FileName).ToLowerInvariant();

                if (extension is not ".jpg" and not ".jpeg" and not ".png")
                {
                    await MostrarToastAsync(
                        "La imagen debe tener formato JPG, JPEG o PNG.");
                    return;
                }

                ImagenSeleccionada = result;
            }
            catch
            {
                await MostrarToastAsync(
                    "No fue posible seleccionar la imagen.");
            }
        }

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            bool hasChanges = HasChanges();

            if (hasChanges)
            {
                bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "Cancelar cambios",
                    "Hay información sin guardar. ¿Desea salir y descartarla?",
                    "Sí, salir",
                    "Continuar editando");

                if (!confirm)
                    return;
            }

            ResetForm();
            await GoToAsyncParameters("//UserPage");
        }

        private bool HasChanges()
        {
            // En modo consulta no existe información editable.
            if (Mode == FormMode.FormModeSelect.View)
                return false;

            if (Mode == FormMode.FormModeSelect.Create)
            {
                return !string.IsNullOrWhiteSpace(NombreUsuario) ||
                       !string.IsNullOrWhiteSpace(ClaveUsuario) ||
                       !string.IsNullOrWhiteSpace(
                           NombreCompletoUsuario) ||
                       !string.IsNullOrWhiteSpace(
                           IdentificacionUsuario) ||
                       !string.IsNullOrWhiteSpace(CorreoUsuario) ||
                       !string.IsNullOrWhiteSpace(TelefonoUsuario) ||
                       RolSeleccionado != null ||
                       PaisSeleccionado != null ||
                       DepartamentoSeleccionado != null ||
                       MunicipioSeleccionado != null ||
                       ImagenSeleccionada != null;
            }

            // Si todavía no terminó la carga inicial, no se debe informar
            // falsamente que el usuario modificó el formulario.
            if (!initialStateCaptured)
                return false;

            return !SonIguales(
                       NombreCompletoUsuario,
                       originalNombreCompleto) ||
                   !SonIguales(
                       IdentificacionUsuario,
                       originalIdentificacion) ||
                   !SonIguales(CorreoUsuario, originalCorreo) ||
                   !SonIguales(TelefonoUsuario, originalTelefono) ||
                   FechaNacimientoUsuario !=
                       originalFechaNacimiento ||
                   RolSeleccionado?.RolId != originalRolId ||
                   MunicipioSeleccionado?.MunicipioId !=
                       originalMunicipioId ||
                   !string.IsNullOrWhiteSpace(ClaveUsuario) ||
                   ImagenSeleccionada != null;
        }

        private void CaptureInitialState()
        {
            originalNombreCompleto =
                NormalizarTexto(NombreCompletoUsuario);

            originalIdentificacion =
                NormalizarTexto(IdentificacionUsuario);

            originalCorreo = NormalizarTexto(CorreoUsuario);
            originalTelefono = NormalizarTexto(TelefonoUsuario);
            originalFechaNacimiento = FechaNacimientoUsuario;
            originalRolId = RolSeleccionado?.RolId;
            originalMunicipioId =
                MunicipioSeleccionado?.MunicipioId;

            initialStateCaptured = true;
        }

        private static bool SonIguales(
            string? value1,
            string? value2)
        {
            return string.Equals(
                NormalizarTexto(value1),
                NormalizarTexto(value2),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizarTexto(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private void ApplyUserToForm(UserRequest source)
        {
            NombreUsuario = source.NombreUsuario ?? string.Empty;
            NombreCompletoUsuario = source.NombreCompletoUsuario ?? string.Empty;
            IdentificacionUsuario = source.IdentificacionUsuario ?? string.Empty;
            CorreoUsuario = source.CorreoUsuario ?? string.Empty;
            TelefonoUsuario = source.TelefonoUsuario ?? string.Empty;
            FechaNacimientoUsuario = source.FechaNacimientoUsuario;
            UrlImagenUsuario = source.UrlImagenUsuario ?? string.Empty;
            ClaveUsuario = string.Empty;
            ImagenSeleccionada = null;
        }

        private void ResetForm()
        {
            initialStateCaptured = false;
            initialized = false;
            user = new UserRequest();
            NombreUsuario = string.Empty;
            ClaveUsuario = string.Empty;
            NombreCompletoUsuario = string.Empty;
            IdentificacionUsuario = string.Empty;
            CorreoUsuario = string.Empty;
            TelefonoUsuario = string.Empty;
            FechaNacimientoDate = DateTime.Today.AddYears(-18);
            FechaNacimientoUsuario = DateOnly.FromDateTime(FechaNacimientoDate);
            UrlImagenUsuario = string.Empty;
            ImagenSeleccionada = null;
            RolSeleccionado = null;

            suppressLocationEvents = true;
            try
            {
                PaisSeleccionado = null;
                DepartamentoSeleccionado = null;
                MunicipioSeleccionado = null;
                Roles.Clear();
                Paises.Clear();
                Departamentos.Clear();
                Municipios.Clear();
            }
            finally
            {
                suppressLocationEvents = false;
            }

            IsPasswordHidden = true;
            PasswordToggleIcon = "eye.png";
        }

        private static void ReplaceCollection<T>(
            ObservableCollection<T> target,
            IEnumerable<T>? source)
        {
            target.Clear();

            if (source == null)
                return;

            foreach (T item in source)
                target.Add(item);
        }

        private void RefreshCommands()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
            SeleccionarImagenCommand.ChangeCanExecute();
        }

        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }
    }
}
