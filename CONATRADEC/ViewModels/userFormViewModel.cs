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

        public async Task InicializarAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            RefreshCommands();

            try
            {
                if (!initialized)
                {
                    var rolesTask = rolApiService.GetRolResultAsync();
                    var paisesTask = paisApiService.GetPaisResultAsync();

                    await Task.WhenAll(rolesTask, paisesTask);

                    var rolesResult = await rolesTask;
                    var paisesResult = await paisesTask;

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
                    initialized = true;
                }

                if (User.RolId > 0)
                    RolSeleccionado = Roles.FirstOrDefault(x => x.RolId == User.RolId);

                if (User.MunicipioId > 0)
                    await ResolverUbicacionAsync(User.MunicipioId);

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

        private async Task ResolverUbicacionAsync(int? municipioId)
        {
            if (municipioId is not > 0)
                return;

            var result =
                await municipioApiService.GetMunicipiosConUbicacionResultAsync();

            if (!result.Success)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            MunicipioResponse? target = result.Data?
                .FirstOrDefault(x => x.MunicipioId == municipioId);

            if (target == null)
                return;

            // El servidor publicado puede devolver solamente NombrePais y
            // NombreDepartamento. Por eso primero se intenta por ID y, si
            // no viene, se resuelve por nombre.
            PaisResponse? pais = null;

            if (target.PaisId is > 0)
            {
                pais = Paises.FirstOrDefault(
                    x => x.PaisId == target.PaisId);
            }

            if (pais == null &&
                !string.IsNullOrWhiteSpace(target.NombrePais))
            {
                pais = Paises.FirstOrDefault(
                    x => SonIguales(
                        x.NombrePais,
                        target.NombrePais));
            }

            if (pais?.PaisId is not > 0)
                return;

            suppressLocationEvents = true;

            try
            {
                PaisSeleccionado = pais;

                var departamentosResult =
                    await departamentoApiService
                        .GetDepartamentosResultAsync(pais.PaisId);

                if (!departamentosResult.Success)
                {
                    await MostrarToastAsync(
                        departamentosResult.Message);
                    return;
                }

                ReplaceCollection(
                    Departamentos,
                    departamentosResult.Data);

                DepartamentoResponse? departamento = null;

                if (target.DepartamentoId is > 0)
                {
                    departamento = Departamentos.FirstOrDefault(
                        x => x.DepartamentoId ==
                             target.DepartamentoId);
                }

                if (departamento == null &&
                    !string.IsNullOrWhiteSpace(
                        target.NombreDepartamento))
                {
                    departamento = Departamentos.FirstOrDefault(
                        x => SonIguales(
                            x.NombreDepartamento,
                            target.NombreDepartamento));
                }

                if (departamento?.DepartamentoId is not > 0)
                    return;

                DepartamentoSeleccionado = departamento;

                var municipiosResult =
                    await municipioApiService.GetMunicipiosResultAsync(
                        departamento.DepartamentoId);

                if (!municipiosResult.Success)
                {
                    await MostrarToastAsync(
                        municipiosResult.Message);
                    return;
                }

                ReplaceCollection(
                    Municipios,
                    municipiosResult.Data);

                MunicipioSeleccionado = Municipios.FirstOrDefault(
                    x => x.MunicipioId == municipioId);
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

            var result = await departamentoApiService.GetDepartamentosResultAsync(pais.PaisId);

            if (!result.Success)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            ReplaceCollection(Departamentos, result.Data);
        }

        private async Task OnDepartamentoChangedAsync(DepartamentoResponse? departamento)
        {
            MunicipioSeleccionado = null;
            Municipios.Clear();

            if (departamento?.DepartamentoId is not > 0)
                return;

            var result = await municipioApiService.GetMunicipiosResultAsync(
                departamento.DepartamentoId);

            if (!result.Success)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            ReplaceCollection(Municipios, result.Data);
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
            var request = BuildRequestForCreate();
            var result = await userApiService.CreateUserResultAsync(request);

            if (!result.Success || result.Data?.UsuarioId is not > 0)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            if (ImagenSeleccionada != null)
            {
                var imageResult = await userApiService.SubirImagenResultAsync(
                    result.Data.UsuarioId,
                    ImagenSeleccionada);

                if (!imageResult.Success)
                {
                    await MostrarToastAsync(
                        $"El usuario fue creado, pero la imagen no se pudo guardar: {imageResult.Message}");
                }
            }

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

            var request = BuildRequestForUpdate();
            var result = await userApiService.UpdateUserResultAsync(request);

            if (!result.Success || result.Data?.UsuarioId is not > 0)
            {
                await MostrarToastAsync(result.Message);
                return;
            }

            if (ImagenSeleccionada != null)
            {
                var imageResult = await userApiService.SubirImagenResultAsync(
                    result.Data.UsuarioId,
                    ImagenSeleccionada);

                if (!imageResult.Success)
                {
                    await MostrarToastAsync(
                        $"Los datos fueron actualizados, pero la imagen no se pudo guardar: {imageResult.Message}");
                }
            }

            ClaveUsuario = string.Empty;
            await GoToAsyncParameters("//UserPage");
            await MostrarToastAsync("Usuario actualizado correctamente.");
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
            // Se guarda exactamente lo que quedó cargado en la interfaz.
            // Si un catálogo no pudo resolverse, un valor nulo no se
            // considera una modificación realizada por el usuario.
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