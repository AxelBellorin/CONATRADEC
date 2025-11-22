using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace CONATRADEC.ViewModels
{
    // Recibe parámetros desde Shell (latitud / longitud / Terreno / Mode)
    [QueryProperty(nameof(LatitudParam), "latitud")]
    [QueryProperty(nameof(LongitudParam), "longitud")]
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Terreno), "Terreno")]
    public class TerrenoFormViewModel : GlobalService
    {
        // ==================== Estado interno ====================
        private TerrenoRequest terreno;
        private bool isCancel;

        private string codigoTerreno;
        private string identificacionPropietarioTerreno;
        private string nombrePropietarioTerreno;
        private string telefonoPropietarioTexto;
        private string correoPropietario;
        private string direccionTerreno;
        private decimal? extensionManzanaTerreno;
        private decimal? cantidadQuintalesOro;
        private double? latitud;
        private double? longitud;
        private string coordenadasTexto;

        private DateOnly? fechaIngresoTerreno;
        private DateTime fechaIngresoDate = DateTime.Today;

        private string latitudParam;
        private string longitudParam;

        private FormMode.FormModeSelect mode = new();

        private readonly TerrenoApiService terrenoApiService = new();
        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();

        // Bandera para no re-ejecutar InicializarAsync al volver del mapa
        private bool inicializado = false;

        // Delegate para que la vista pueda refrescar el mapa cuando cambian las coordenadas
        public Action<double?, double?> RefrescarMapaAction { get; set; }

        // ==================== Comandos ====================

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command ObtenerGpsCommand { get; }
        public Command SeleccionarMapaCommand { get; }

        public TerrenoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
            ObtenerGpsCommand = new Command(async () => await ObtenerGpsAsync(), () => !IsReadOnly);
            SeleccionarMapaCommand = new Command(async () => await SeleccionarMapaAsync(), () => !IsReadOnly);
        }

        // ==================== Query properties ====================

        public string LatitudParam
        {
            get => latitudParam;
            set
            {
                latitudParam = value;

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                {
                    Latitud = lat;
                    RefrescarMapaAction?.Invoke(Latitud, Longitud); // <- NUEVO
                }
            }
        }

        public string LongitudParam
        {
            get => longitudParam;
            set
            {
                longitudParam = value;

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    Longitud = lon;
                    RefrescarMapaAction?.Invoke(Latitud, Longitud); // <- NUEVO
                }
            }
        }

        public string CoordenadasTexto
        {
            get => coordenadasTexto;
            set
            {
                coordenadasTexto = value;
                OnPropertyChanged();
                ProcesarCoordenadas(value); // ← convierte y asigna Latitud/Longitud
            }
        }

        public TerrenoRequest Terreno
        {
            get => terreno;
            set
            {
                terreno = value;

                if (value != null)
                {
                    CodigoTerreno = value.CodigoTerreno ?? "";
                    IdentificacionPropietarioTerreno = value.IdentificacionPropietarioTerreno ?? "";
                    NombrePropietarioTerreno = value.NombrePropietarioTerreno ?? "";
                    TelefonoPropietarioTexto = value.TelefonoPropietario?.ToString() ?? "";
                    CorreoPropietario = value.CorreoPropietario ?? "";
                    DireccionTerreno = value.DireccionTerreno ?? "";
                    ExtensionManzanaTerreno = value.ExtensionManzanaTerreno;
                    CantidadQuintalesOro = value.CantidadQuintalesOro;
                    FechaIngresoTerreno = value.FechaIngresoTerreno ?? DateOnly.FromDateTime(DateTime.Today);

                    // ⭐ IMPORTANTE:
                    if (LatitudParam == null && LongitudParam == null)
                    {
                        Latitud = value.Latitud;
                        Longitud = value.Longitud;
                    }
                }

                OnPropertyChanged();

                // SOLO en actualización
                if (Mode == FormMode.FormModeSelect.Edit)
                {
                    _ = ReasignarSeleccionPickersAsync();
                }

            }
        }

        public async Task ReasignarSeleccionPickersAsync()
        {
            try
            {
                if (Terreno?.MunicipioId > 0)
                {
                    await ResolverSeleccionPorMunicipioIdAsync(Terreno.MunicipioId);
                }
            }
            catch { }
        }



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
                OnPropertyChanged(nameof(AllowEdit));
                OnPropertyChanged(nameof(Title));

                SaveCommand.ChangeCanExecute();
                ObtenerGpsCommand.ChangeCanExecute();
                SeleccionarMapaCommand.ChangeCanExecute();
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEnabled => !IsReadOnly;
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;
        public bool AllowEdit => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Terreno",
            FormMode.FormModeSelect.Edit => "Editar Terreno",
            FormMode.FormModeSelect.View => "Detalles del Terreno",
            _ => ""
        };

        // ==================== Propiedades bindables ====================

        public string CodigoTerreno
        {
            get => codigoTerreno;
            set { codigoTerreno = value; OnPropertyChanged(); }
        }

        public string IdentificacionPropietarioTerreno
        {
            get => identificacionPropietarioTerreno;
            set { identificacionPropietarioTerreno = value; OnPropertyChanged(); }
        }

        public string NombrePropietarioTerreno
        {
            get => nombrePropietarioTerreno;
            set { nombrePropietarioTerreno = value; OnPropertyChanged(); }
        }

        public string TelefonoPropietarioTexto
        {
            get => telefonoPropietarioTexto;
            set { telefonoPropietarioTexto = value; OnPropertyChanged(); }
        }

        public string CorreoPropietario
        {
            get => correoPropietario;
            set { correoPropietario = value; OnPropertyChanged(); }
        }

        public string DireccionTerreno
        {
            get => direccionTerreno;
            set { direccionTerreno = value; OnPropertyChanged(); }
        }

        public decimal? ExtensionManzanaTerreno
        {
            get => extensionManzanaTerreno;
            set { extensionManzanaTerreno = value; OnPropertyChanged(); }
        }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set { cantidadQuintalesOro = value; OnPropertyChanged(); }
        }

        public double? Latitud
        {
            get => latitud;
            set
            {
                latitud = value;
                OnPropertyChanged();
                RefrescarMapaAction?.Invoke(latitud, longitud);
            }
        }

        public double? Longitud
        {
            get => longitud;
            set
            {
                longitud = value;
                OnPropertyChanged();
                RefrescarMapaAction?.Invoke(latitud, longitud);
            }
        }

        public DateOnly? FechaIngresoTerreno
        {
            get => fechaIngresoTerreno;
            set
            {
                fechaIngresoTerreno = value;

                if (value.HasValue)
                {
                    fechaIngresoDate = value.Value.ToDateTime(TimeOnly.MinValue);
                    OnPropertyChanged(nameof(FechaIngresoDate));
                }

                OnPropertyChanged();
            }
        }

        public DateTime FechaIngresoDate
        {
            get => fechaIngresoDate;
            set
            {
                if (fechaIngresoDate != value)
                {
                    fechaIngresoDate = value;
                    fechaIngresoTerreno = DateOnly.FromDateTime(value);
                    OnPropertyChanged(nameof(FechaIngresoTerreno));
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        // ==================== Pickers País / Depto / Municipio ====================

        public ObservableCollection<PaisResponse> Paises { get; } = new();
        public ObservableCollection<DepartamentoResponse> Departamentos { get; } = new();
        public ObservableCollection<MunicipioResponse> Municipios { get; } = new();

        private PaisResponse paisSeleccionado;
        private DepartamentoResponse departamentoSeleccionado;
        private MunicipioResponse municipioSeleccionado;

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

        // ==================== Inicialización ====================

        public async Task InicializarAsync()
        {
            // 🔒 Evitamos recargar pickers cuando volvemos del mapa
            if (inicializado) return;

            try
            {
                IsBusy = true;
                inicializado = true;

                await CargarPaisesAsync();

                if (Terreno?.MunicipioId > 0)
                    await ResolverSeleccionPorMunicipioIdAsync(Terreno.MunicipioId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarPaisesAsync()
        {
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
                await CargarDepartamentosAsync(pais.PaisId);

                foreach (var dep in Departamentos.ToList())
                {
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
                paisSeleccionado = paisEncontrado;
                departamentoSeleccionado = depEncontrado;
                municipioSeleccionado = muniEncontrado;

                OnPropertyChanged(nameof(PaisSeleccionado));
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
            }
        }

        // ==================== GPS ====================

        private async Task ObtenerGpsAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    _ = MostrarToastAsync("Permiso de ubicación denegado.");
                    return;
                }

                var location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium));

                if (location != null)
                {
                    Latitud = location.Latitude;
                    Longitud = location.Longitude;
                }
                else
                {
                    _ = MostrarToastAsync("No se pudo obtener la ubicación actual.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error al obtener GPS: " + ex.Message);
            }
        }

        private async Task SeleccionarMapaAsync()
        {
            if (!AllowEdit) return;

            // Guardamos temporalmente el objeto
            Terreno.CodigoTerreno = CodigoTerreno;
            Terreno.IdentificacionPropietarioTerreno = IdentificacionPropietarioTerreno;
            Terreno.NombrePropietarioTerreno = NombrePropietarioTerreno;
            Terreno.TelefonoPropietario = ParseTelefono(TelefonoPropietarioTexto);
            Terreno.CorreoPropietario = CorreoPropietario;
            Terreno.DireccionTerreno = DireccionTerreno;
            Terreno.ExtensionManzanaTerreno = ExtensionManzanaTerreno;
            Terreno.CantidadQuintalesOro = CantidadQuintalesOro;
            Terreno.FechaIngresoTerreno = FechaIngresoTerreno;
            Terreno.MunicipioId = MunicipioSeleccionado?.MunicipioId ?? 0;

            await Shell.Current.GoToAsync("MapaSeleccionPage", true, new Dictionary<string, object>
            {
                {
                    "latitudActual",
                    (Latitud ?? 12.1364).ToString(CultureInfo.InvariantCulture)
                },
                {
                    "longitudActual",
                    (Longitud ?? -86.2510).ToString(CultureInfo.InvariantCulture)
},
                { "Mode", Mode },
                { "Terreno", Terreno }
            });

        }


        // ==================== Guardar / Actualizar ====================

        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateTerrenoAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateTerrenoAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateTerrenoAsync()
        {
            if (!ValidateFieldsData()) return;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Confirmar",
                "¿Desea guardar los datos del terreno?",
                "Aceptar",
                "Cancelar");

            if (!confirm) return;

            var request = new TerrenoRequest
            {
                CodigoTerreno = CodigoTerreno,
                IdentificacionPropietarioTerreno = IdentificacionPropietarioTerreno,
                NombrePropietarioTerreno = NombrePropietarioTerreno,
                TelefonoPropietario = ParseTelefono(TelefonoPropietarioTexto),
                CorreoPropietario = CorreoPropietario,
                DireccionTerreno = DireccionTerreno,
                ExtensionManzanaTerreno = ExtensionManzanaTerreno,
                CantidadQuintalesOro = CantidadQuintalesOro,
                FechaIngresoTerreno = FechaIngresoTerreno,
                MunicipioId = MunicipioSeleccionado?.MunicipioId ?? 0,
                Latitud = Latitud,
                Longitud = Longitud
            };

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await terrenoApiService.CreateTerrenoAsync(request);

            if (response)
            {
                await GoToTerrenoPage();
                _ = MostrarToastAsync("Éxito \nTerreno guardado correctamente");
            }
            else
            {
                _ = MostrarToastAsync("Error \nEl terreno no se pudo guardar, intente nuevamente");
            }
        }

        private async Task UpdateTerrenoAsync()
        {
            if (!ValidateFieldsData()) return;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Confirmar",
                "¿Desea actualizar el terreno?",
                "Aceptar",
                "Cancelar");

            if (!confirm) return;

            var request = new TerrenoRequest
            {
                TerrenoId = Terreno.TerrenoId,
                CodigoTerreno = CodigoTerreno,
                IdentificacionPropietarioTerreno = IdentificacionPropietarioTerreno,
                NombrePropietarioTerreno = NombrePropietarioTerreno,
                TelefonoPropietario = ParseTelefono(TelefonoPropietarioTexto),
                CorreoPropietario = CorreoPropietario,
                DireccionTerreno = DireccionTerreno,
                ExtensionManzanaTerreno = ExtensionManzanaTerreno,
                CantidadQuintalesOro = CantidadQuintalesOro,
                FechaIngresoTerreno = FechaIngresoTerreno,
                MunicipioId = MunicipioSeleccionado?.MunicipioId ?? Terreno.MunicipioId,
                Latitud = Latitud,
                Longitud = Longitud
            };

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await terrenoApiService.UpdateTerrenoAsync(request);

            if (response)
            {
                await GoToTerrenoPage();
                _ = MostrarToastAsync("Éxito \nTerreno actualizado correctamente");
            }
            else
            {
                _ = MostrarToastAsync("Error \nEl terreno no se pudo actualizar, intente nuevamente");
            }
        }

        public async Task AbrirEnGoogleMaps(double lat, double lon)
        {
            try
            {
                string url = $"https://www.google.com/maps?q={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";

                await Launcher.OpenAsync(url);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public void ConvertirDesdeGoogleMaps(string texto)
        {
            try
            {
                // Ejemplo esperado:
                // 12°13'35.8"N 86°28'06.9"W

                var partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length != 2)
                    throw new Exception("Formato inválido");

                Latitud = ConvertDmsToDecimal(partes[0]);
                Longitud = ConvertDmsToDecimal(partes[1]);
            }
            catch
            {
                _ = MostrarToastAsync("Formato inválido. Ejemplo: 12°13'35.8\"N 86°28'06.9\"W");
            }
        }

        private double ConvertDmsToDecimal(string dms)
        {
            // Extraer grados, minutos, segundos y símbolo N/S/E/W

            var grados = dms.Split('°')[0];
            var minutos = dms.Split('°')[1].Split('\'')[0];
            var segundosParte = dms.Split('\'')[1];
            var segundos = new string(segundosParte.TakeWhile(char.IsDigit).ToArray());
            var direccion = new string(dms.Where(char.IsLetter).ToArray());

            double d = double.Parse(grados, CultureInfo.InvariantCulture);
            double m = double.Parse(minutos, CultureInfo.InvariantCulture);
            double s = double.Parse(segundos, CultureInfo.InvariantCulture);

            double decimalValue = d + (m / 60.0) + (s / 3600.0);

            if (direccion == "S" || direccion == "W")
                decimalValue *= -1;

            return decimalValue;
        }


        private Task GoToTerrenoPage()
        {
            return GoToAsyncParameters("//TerrenoPage");
        }

        // ==================== Validaciones ====================

        private bool ValidateFieldsData()
        {
            if (string.IsNullOrWhiteSpace(CodigoTerreno))
            {
                Display("El código del terreno es obligatorio.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NombrePropietarioTerreno))
            {
                Display("El nombre del propietario es obligatorio.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CorreoPropietario) && !EsCorreoValido(CorreoPropietario))
            {
                Display("Correo del propietario inválido.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TelefonoPropietarioTexto) && !TelefonoPropietarioTexto.All(char.IsDigit))
            {
                Display("El teléfono solo debe contener números.");
                return false;
            }

            if (ExtensionManzanaTerreno == null || ExtensionManzanaTerreno <= 0)
            {
                Display("La extensión del terreno debe ser mayor a cero.");
                return false;
            }

            if (MunicipioSeleccionado == null && Terreno?.MunicipioId == null)
            {
                Display("Debe seleccionar un municipio.");
                return false;
            }

            if (Latitud == null || Longitud == null)
            {
                Display("Debe definir la ubicación del terreno.");
                return false;
            }

            return true;
        }

        private void Display(string msg) =>
            _ = MostrarToastAsync("Validación: " + msg);

        private bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            var patron = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            return Regex.IsMatch(correo, patron);
        }

        private int? ParseTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return null;
            if (int.TryParse(telefono, out var t)) return t;
            return null;
        }

        private async Task CancelAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToTerrenoPage();
                    }
                }
                else
                {
                    await GoToTerrenoPage();
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

        private bool ValidateFieldsAsync()
        {
            if (Terreno == null) return false;

            if (CodigoTerreno != Terreno.CodigoTerreno) return true;
            if (IdentificacionPropietarioTerreno != Terreno.IdentificacionPropietarioTerreno) return true;
            if (NombrePropietarioTerreno != Terreno.NombrePropietarioTerreno) return true;
            if (TelefonoPropietarioTexto != Terreno.TelefonoPropietario?.ToString()) return true;
            if (CorreoPropietario != Terreno.CorreoPropietario) return true;
            if (DireccionTerreno != Terreno.DireccionTerreno) return true;
            if (ExtensionManzanaTerreno != Terreno.ExtensionManzanaTerreno) return true;
            if (CantidadQuintalesOro != Terreno.CantidadQuintalesOro) return true;
            if (FechaIngresoTerreno != Terreno.FechaIngresoTerreno) return true;
            if (Latitud != Terreno.Latitud) return true;
            if (Longitud != Terreno.Longitud) return true;
            if (MunicipioSeleccionado?.MunicipioId != Terreno.MunicipioId) return true;

            return false;
        }

        private void ProcesarCoordenadas(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            texto = texto.Trim();

            // Formato decimal: "12.1234, -86.1234" o "12,1234 -86,1234"
            var numeros = Regex.Matches(texto.Replace(",", "."), @"-?\d+(\.\d+)?");

            if (numeros.Count == 2)
            {
                if (double.TryParse(numeros[0].Value, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(numeros[1].Value, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out var lon))
                {
                    Latitud = lat;
                    Longitud = lon;
                    return;
                }
            }

            // Formato DMS "12°13'35.8"N 86°28'06.9"W"
            if (texto.Contains("°"))
            {
                var partes = texto.Split(new[] { 'N', 'S', 'E', 'W' }, StringSplitOptions.RemoveEmptyEntries);

                if (partes.Length >= 2)
                {
                    var latDms = partes[0].Trim();
                    var lonDms = partes[1].Trim();

                    var latDecimal = ConvertirDMSToDecimal(latDms);
                    var lonDecimal = ConvertirDMSToDecimal(lonDms);

                    // identificar hemisferio
                    if (texto.Contains("S")) latDecimal *= -1;
                    if (texto.Contains("W")) lonDecimal *= -1;

                    Latitud = latDecimal;
                    Longitud = lonDecimal;
                }
            }
        }       

        private double ConvertirDMSToDecimal(string dms)
        {
            // Extrae grados, minutos y segundos correctamente (incluyendo decimales)
            var regex = new Regex(@"(\d+)°(\d+)'(\d+(?:\.\d+)?)""?");
            var match = regex.Match(dms);

            if (!match.Success)
                return 0;

            double grados = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            double minutos = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double segundos = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            return grados + (minutos / 60) + (segundos / 3600);
        }

    }
}