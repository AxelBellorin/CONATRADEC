using CONATRADEC.Services;
using CONATRADEC.Models;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Linq;  

namespace CONATRADEC.ViewModels
{
    public class UserFormViewModel : GlobalService
    {
        // ==================== Estado interno ====================
        private UserRequest user;
        private bool isCancel;

        private string nombreUsuario = "";
        private string claveUsuario = "";
        private string nombreCompletoUsuario = "";
        private string identificacionUsuario = "";
        private string correoUsuario = "";
        private string telefonoUsuario = "";
        private DateOnly? fechaNacimientoUsuario;
        private string urlImagenUsuario = "";
        private string passwordToggleIcon = "eye.png";
        private bool isPasswordHidden = true;

        private DateTime fechaNacimientoDate = DateTime.Now;

        private FileResult? imagenSeleccionada;

        private FormMode.FormModeSelect mode = new();

        private readonly UserApiService userApiService = new();
        private readonly RolApiService rolApiService = new();
        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();

        // ==================== Comandos ====================
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command SeleccionarImagenCommand => new(async () => await SeleccionarImagenAsync());
        public Command TogglePasswordCommand { get; }

        public UserFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
            TogglePasswordCommand = new(() => OnTogglePassword());
        }

        // ==================== Propiedades bindables ====================
        public string NombreUsuario { get => nombreUsuario; set { nombreUsuario = value; OnPropertyChanged(); } }
        public string ClaveUsuario { get => claveUsuario; set { claveUsuario = value; OnPropertyChanged(); } }

        public string NombreCompletoUsuario
        {
            get => nombreCompletoUsuario;
            set { nombreCompletoUsuario = value; OnPropertyChanged(); }
        }

        public string CorreoUsuario
        {
            get => correoUsuario;
            set { correoUsuario = value; OnPropertyChanged(); }
        }

        public string IdentificacionUsuario
        {
            get => identificacionUsuario;
            set { identificacionUsuario = value; OnPropertyChanged(); }
        }

        public string TelefonoUsuario
        {
            get => telefonoUsuario;
            set { telefonoUsuario = value; OnPropertyChanged(); }
        }

        public DateOnly? FechaNacimientoUsuario
        {
            get => fechaNacimientoUsuario;
            set
            {
                fechaNacimientoUsuario = value;

                if (value.HasValue)
                {
                    fechaNacimientoDate = value.Value.ToDateTime(TimeOnly.MinValue);
                    OnPropertyChanged(nameof(FechaNacimientoDate));
                }

                OnPropertyChanged();
            }
        }

        public DateTime FechaNacimientoDate
        {
            get => fechaNacimientoDate;
            set
            {
                if (fechaNacimientoDate != value)
                {
                    fechaNacimientoDate = value;
                    fechaNacimientoUsuario = DateOnly.FromDateTime(value);
                    OnPropertyChanged(nameof(FechaNacimientoUsuario));
                    OnPropertyChanged();
                }
            }
        }

        public string UrlImagenUsuario
        {
            get => urlImagenUsuario;
            set
            {
                urlImagenUsuario = value;
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
                OnPropertyChanged(nameof(TieneImagenSeleccionada));
                OnPropertyChanged(nameof(ImagenPreview));
            }
        }

        public bool ShowPasswordField => Mode == FormMode.FormModeSelect.Create;
        public bool EnabledImagenField => Mode == FormMode.FormModeSelect.Create || Mode == FormMode.FormModeSelect.Edit;

        public string ImagenPreview =>
            ImagenSeleccionada != null ? ImagenSeleccionada.FullPath : UrlImagenUsuario;

        public bool TieneImagenSeleccionada => ImagenSeleccionada != null;

        public string PasswordToggleIcon
        {
            get => passwordToggleIcon;
            set { passwordToggleIcon = value; OnPropertyChanged(); }
        }

        public bool IsPasswordHidden
        {
            get => isPasswordHidden;
            set { isPasswordHidden = value; OnPropertyChanged(); }
        }

        // ==================== Entidad principal ====================
        public UserRequest User
        {
            get => user;
            set
            {
                user = value;
                ImagenSeleccionada = null;

                OnPropertyChanged(nameof(ImagenPreview));
                if (value != null)
                {
                    NombreUsuario = value.NombreUsuario ?? "";
                    NombreCompletoUsuario = value.NombreCompletoUsuario ?? "";
                    CorreoUsuario = value.CorreoUsuario ?? "";
                    TelefonoUsuario = value.TelefonoUsuario ?? "";
                    FechaNacimientoUsuario = value.FechaNacimientoUsuario;
                    UrlImagenUsuario = value.UrlImagenUsuario ?? "";
                    IdentificacionUsuario = value.IdentificacionUsuario ?? "";
                    ClaveUsuario = value.ClaveUsuario ?? "";
                }
                OnPropertyChanged();
            }
        }

        // ==================== Modo ====================
        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(ShowPasswordField));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(EnabledImagenField));
                ImagenSeleccionada = null;
                OnPropertyChanged(nameof(ImagenPreview));
                SaveCommand.ChangeCanExecute();
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEnabled => !IsReadOnly;
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Usuario",
            FormMode.FormModeSelect.Edit => "Editar Usuario",
            FormMode.FormModeSelect.View => "Detalles del Usuario",
            _ => ""
        };

        // ==================== Pickers ====================

        public ObservableCollection<RolResponse> Roles { get; } = new();
        public ObservableCollection<PaisResponse> Paises { get; } = new();
        public ObservableCollection<DepartamentoResponse> Departamentos { get; } = new();
        public ObservableCollection<MunicipioResponse> Municipios { get; } = new();

        private RolResponse rolSeleccionado;
        private PaisResponse paisSeleccionado;
        private DepartamentoResponse departamentoSeleccionado;
        private MunicipioResponse municipioSeleccionado;

        public RolResponse RolSeleccionado
        {
            get => rolSeleccionado;
            set { rolSeleccionado = value; OnPropertyChanged(); }
        }

        public PaisResponse PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                if (paisSeleccionado != value)
                {
                    paisSeleccionado = value;
                    OnPropertyChanged();
                    _ = CargarDepartamentosAsync(value?.PaisId);
                    DepartamentoSeleccionado = null;
                    MunicipioSeleccionado = null;
                    OnPropertyChanged(nameof(CanPickDepartamento));
                    OnPropertyChanged(nameof(CanPickMunicipio));
                }
            }
        }

        public DepartamentoResponse DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                if (departamentoSeleccionado != value)
                {
                    departamentoSeleccionado = value;
                    OnPropertyChanged();
                    _ = CargarMunicipiosAsync(value?.DepartamentoId);
                    MunicipioSeleccionado = null;
                    OnPropertyChanged(nameof(CanPickMunicipio));
                }
            }
        }

        public MunicipioResponse MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set { municipioSeleccionado = value; OnPropertyChanged(); }
        }

        public bool CanPickDepartamento => IsEnabled && PaisSeleccionado != null;
        public bool CanPickMunicipio => IsEnabled && DepartamentoSeleccionado != null;

        public bool IsCancel { get => isCancel; set => isCancel = value; }

        // ==================== Inicialización ====================
        public async Task InicializarAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                await CargarRolesAsync();
                await CargarPaisesAsync();

                // Seleccionar rol del usuario al editar
                if (User?.RolId > 0)
                {
                    RolSeleccionado = Roles.FirstOrDefault(r => r.RolId == User.RolId);
                    OnPropertyChanged(nameof(RolSeleccionado));
                }

                // Reconstruir País / Depto / Municipio a partir del MunicipioId
                if (User?.MunicipioId > 0)
                    await ResolverSeleccionPorMunicipioIdAsync(User.MunicipioId);
            }
            finally { IsBusy = false; }
        }

        private async Task CargarRolesAsync()
        {
            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            Roles.Clear();
            var data = await rolApiService.GetRolAsync();
            foreach (var r in data) Roles.Add(r);
        }

        private async Task CargarPaisesAsync()
        {
            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            Paises.Clear();
            var data = await paisApiService.GetPaisAsync();
            foreach (var p in data) Paises.Add(p);
            OnPropertyChanged(nameof(Paises));
            OnPropertyChanged(nameof(CanPickDepartamento));
            OnPropertyChanged(nameof(CanPickMunicipio));
        }

        private async Task CargarDepartamentosAsync(int? paisId)
        {
            Departamentos.Clear();
            Municipios.Clear();
            if (paisId == null) return;

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var data = await departamentoApiService.GetDepartamentosAsync(paisId);
            foreach (var d in data) Departamentos.Add(d);

            OnPropertyChanged(nameof(Departamentos));
            OnPropertyChanged(nameof(CanPickDepartamento));
        }

        private async Task CargarMunicipiosAsync(int? departamentoId)
        {
            Municipios.Clear();
            if (departamentoId == null) return;

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var data = await municipioApiService.GetMunicipiosAsync(departamentoId);
            foreach (var m in data) Municipios.Add(m);

            OnPropertyChanged(nameof(Municipios));
            OnPropertyChanged(nameof(CanPickMunicipio));
        }


        private async Task ResolverSeleccionPorMunicipioIdAsync(int? municipioId)
        {
            if (municipioId == null || municipioId <= 0)
                return;

            PaisResponse paisEncontrado = null;
            DepartamentoResponse depEncontrado = null;
            MunicipioResponse muniEncontrado = null;


            foreach (var pais in Paises.ToList())
            {
                // Cargar departamentos de este país (rellena Departamentos)
                await CargarDepartamentosAsync(pais.PaisId);

                foreach (var dep in Departamentos.ToList())
                {
                    // Cargar municipios de este departamento (rellena Municipios)
                    await CargarMunicipiosAsync(dep.DepartamentoId);

                    var muni = Municipios.FirstOrDefault(m => m.MunicipioId == municipioId);
                    if (muni != null)
                    {
                        paisEncontrado = pais;
                        depEncontrado = dep;
                        muniEncontrado = muni;
                        break;
                    }
                }

                if (muniEncontrado != null)
                    break;
            }

            if (muniEncontrado != null)
            {
                // Asignamos directamente a los CAMPOS para no disparar de nuevo cargas
                paisSeleccionado = paisEncontrado;
                departamentoSeleccionado = depEncontrado;
                municipioSeleccionado = muniEncontrado;

                // Y notificamos a la UI
                OnPropertyChanged(nameof(PaisSeleccionado));
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
            }
        }

        // ==================== Imagen ====================
        private async Task SeleccionarImagenAsync()
        {
            try
            {
                var resultado = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecciona una imagen",
                    FileTypes = FilePickerFileType.Images
                });

                if (resultado != null)
                {
                    var ext = Path.GetExtension(resultado.FileName)?.ToLower();
                    if (ext is not ".jpg" and not ".png")
                    {
                        _ = GlobalService.MostrarToastAsync("Formato inválido" + "Solo JPG o PNG.");
                        return;
                    }

                    ImagenSeleccionada = resultado;
                }
            }
            catch (Exception ex)
            {
                _ = GlobalService.MostrarToastAsync("Error" + ex.Message);
            }
        }

        // ==================== Guardar / Actualizar ====================
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateUserAsync();
                else
                    await UpdateUserAsync();
            }
            finally { IsBusy = false; }
        }

        private async Task CreateUserAsync()
        {
            if (!ValidateFieldsData()) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Confirmar", "¿Guardar usuario?", "Sí", "No");
            if (!confirm) return;

            var request = new UserRequest
            {
                NombreUsuario = NombreUsuario,
                NombreCompletoUsuario = NombreCompletoUsuario,
                CorreoUsuario = CorreoUsuario,
                TelefonoUsuario = TelefonoUsuario,
                FechaNacimientoUsuario = FechaNacimientoUsuario,
                IdentificacionUsuario = IdentificacionUsuario,
                ClaveUsuario = ClaveUsuario,
                RolId = RolSeleccionado?.RolId ?? 0,
                MunicipioId = MunicipioSeleccionado?.MunicipioId ?? 0,
                EsInterno = true
            };

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var (ok, usuarioCreado) = await userApiService.CreateUserAsync(request);

            if (ok && usuarioCreado?.UsuarioId > 0)
            {
                // Valida que el usaurio tenga conexion a internet
                tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                if (ImagenSeleccionada != null)
                    await userApiService.SubirImagenAsync(usuarioCreado.UsuarioId, ImagenSeleccionada);

                await GoToAsyncParameters("//UserPage");
                _ = GlobalService.MostrarToastAsync("Éxito" + "Usuario creado.");
            }
        }

        private async Task UpdateUserAsync()
        {
            if (!ValidateFieldsData()) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Confirmar", "¿Actualizar usuario?", "Sí", "No");
            if (!confirm) return;

            var request = new UserRequest
            {
                UsuarioId = User.UsuarioId,
                NombreCompletoUsuario = NombreCompletoUsuario,
                CorreoUsuario = CorreoUsuario,
                IdentificacionUsuario = IdentificacionUsuario,
                TelefonoUsuario = TelefonoUsuario,
                FechaNacimientoUsuario = FechaNacimientoUsuario,
                MunicipioId = MunicipioSeleccionado?.MunicipioId ?? User.MunicipioId,
                RolId = RolSeleccionado?.RolId ?? User.RolId,
                EsInterno = User.EsInterno,
                UrlImagenUsuario = ImagenPreview ?? ""
            };

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var (ok, usuarioActualizado) = await userApiService.UpdateUserAsync(request);

            if (ok && usuarioActualizado?.UsuarioId > 0)
            {
                // Valida que el usaurio tenga conexion a internet
                tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                if (ImagenSeleccionada != null)
                    await userApiService.SubirImagenAsync(usuarioActualizado.UsuarioId, ImagenSeleccionada);

                UrlImagenUsuario = usuarioActualizado.UrlImagenUsuario ?? UrlImagenUsuario;

                await GoToAsyncParameters("//UserPage");
                _ = GlobalService.MostrarToastAsync("Éxito" + "Usuario actualizado.");
            }
        }

        // ==================== Validaciones ====================
        private bool ValidateFieldsData()
        {
            if (string.IsNullOrWhiteSpace(NombreCompletoUsuario))
            {
                Display("El nombre completo es obligatorio.");
                return false;
            }

            if (!EsCorreoValido(CorreoUsuario))
            {
                Display("Correo inválido.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(IdentificacionUsuario))
            {
                Display("Identificación obligatoria.");
                return false;
            }

            if (!EsTelefonoValido(TelefonoUsuario))
            {
                Display("Teléfono debe ser numérico.");
                return false;
            }

            if (!EsContrasenaSegura(ClaveUsuario) && Mode == FormMode.FormModeSelect.Create)
            {
                Display("Contraseña insegura.");
                return false;
            }

            if (!EsMayorDeEdad(FechaNacimientoUsuario))
            {
                Display("Debe ser mayor de 18 años.");
                return false;
            }

            if (MunicipioSeleccionado == null)
            {
                Display("Seleccione un municipio.");
                return false;
            }

            return true;
        }

        private void Display(string msg) =>
            _ = MostrarToastAsync("Validación" + msg);

        public bool EsTelefonoValido(string telefono) =>
            !string.IsNullOrWhiteSpace(telefono) && telefono.All(char.IsDigit);

        public bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            var patron = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            return Regex.IsMatch(correo, patron);
        }

        public bool EsContrasenaSegura(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return false;
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_-]).{8,}$");
            return regex.IsMatch(clave);
        }

        public bool EsMayorDeEdad(DateOnly? fecha)
        {
            if (fecha == null) return false;
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            int edad = hoy.Year - fecha.Value.Year;
            if (hoy < fecha.Value.AddYears(edad)) edad--;
            return edad >= 18;
        }

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
                        await GoToAsyncParameters("//UserPage"); // Vuelve al listado de roles.
                    }
                }
                else
                {
                    // Si no hay cambios, regresa inmediatamente.
                    await GoToAsyncParameters("//UserPage");
                }
            }
            catch (Exception ex)
            {
                _ = GlobalService.MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false; // Limpia bandera para siguientes intentos.
            }
        }

        private bool ValidateFieldsAsync()
        {
            if (NombreCompletoUsuario != User.NombreCompletoUsuario) return true;
            if (NombreUsuario != User.NombreUsuario) return true;
            if (CorreoUsuario != User.CorreoUsuario) return true;
            if (IdentificacionUsuario != User.IdentificacionUsuario) return true;
            if (TelefonoUsuario != User.TelefonoUsuario) return true;
            if (FechaNacimientoUsuario != User.FechaNacimientoUsuario) return true;
            if (RolSeleccionado?.RolId != User.RolId) return true;
            if (MunicipioSeleccionado?.MunicipioId != User.MunicipioId) return true;
            if (ImagenPreview != User.UrlImagenUsuario) return true;

            return false;
        }

        public void OnTogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
            PasswordToggleIcon = IsPasswordHidden ? "eye.png" : "eyeoff.png";
        }
    }
}
