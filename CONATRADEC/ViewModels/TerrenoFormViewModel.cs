using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(nameof(LatitudParam), "latitud")]
    [QueryProperty(nameof(LongitudParam), "longitud")]
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Terreno), "Terreno")]
    public class TerrenoFormViewModel : GlobalService
    {
        private static readonly Regex CedulaRegex = new(
            @"^\d{3}-\d{6}-\d{4}[A-Z]$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant |
            RegexOptions.IgnoreCase);

        private static readonly Regex CorreoRegex = new(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant);

        private readonly TerrenoApiService terrenoApiService = new();
        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();
        private readonly FotoTerrenoApiService fotoTerrenoApiService = new();

        private readonly SemaphoreSlim inicializacionLock = new(1, 1);
        private CancellationTokenSource? inicializacionCts;
        private CancellationTokenSource? departamentoCts;
        private CancellationTokenSource? municipioCts;
        private CancellationTokenSource? fotosCts;
        private CancellationTokenSource? guardadoCts;

        private TerrenoRequest? terreno;
        private FormMode.FormModeSelect mode;
        private bool inicializado;
        private bool actualizandoSeleccionInterna;
        private bool isCancel;
        private int? fotosCargadasTerrenoId;

        private string? codigoTerreno;
        private string? identificacionPropietarioTerreno;
        private string? nombrePropietarioTerreno;
        private string? telefonoPropietarioTexto;
        private string? correoPropietario;
        private string? direccionTerreno;
        private decimal? extensionManzanaTerreno;
        private decimal? cantidadQuintalesOro;
        private int? cantidadPlantasTerreno;
        private string extensionManzanaTexto = string.Empty;
        private string cantidadQuintalesOroTexto = string.Empty;
        private string cantidadPlantasTerrenoTexto = string.Empty;
        private DateOnly? fechaIngresoTerreno;
        private DateTime fechaIngresoDate = DateTime.Today;
        private double? latitud;
        private double? longitud;
        private string? coordenadasTexto;
        private string? latitudParam;
        private string? longitudParam;

        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        public TerrenoFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);

            ObtenerGpsCommand = new Command(
                async () => await ObtenerGpsAsync(),
                () => !IsReadOnly && !IsBusy);

            SeleccionarMapaCommand = new Command(
                async () => await SeleccionarMapaAsync(),
                () => !IsReadOnly && !IsBusy);

            SeleccionarFotosCommand = new Command(
                async () => await SeleccionarFotosAsync(),
                () => !IsReadOnly && !IsBusy);

            QuitarFotoCommand = new Command<FotoTerrenoItem>(
                async foto => await QuitarFotoAsync(foto),
                foto => foto != null && !IsReadOnly && !IsBusy);

            AbrirGaleriaFotosCommand = new Command<FotoTerrenoItem>(
                async foto => await AbrirGaleriaFotosAsync(foto),
                foto => foto != null && !IsBusy);

            FotosTerreno.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(TieneFotosTerreno));
                OnPropertyChanged(nameof(NoTieneFotosTerreno));
            };
        }

        public Action<double?, double?>? RefrescarMapaAction { get; set; }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command ObtenerGpsCommand { get; }
        public Command SeleccionarMapaCommand { get; }
        public Command SeleccionarFotosCommand { get; }
        public Command<FotoTerrenoItem> QuitarFotoCommand { get; }
        public Command<FotoTerrenoItem> AbrirGaleriaFotosCommand { get; }

        public ObservableCollection<PaisResponse> Paises { get; } = new();
        public ObservableCollection<DepartamentoResponse> Departamentos { get; } = new();
        public ObservableCollection<MunicipioResponse> Municipios { get; } = new();
        public ObservableCollection<FotoTerrenoItem> FotosTerreno { get; } = new();

        public string? LatitudParam
        {
            get => latitudParam;
            set
            {
                latitudParam = value;

                if (double.TryParse(
                        value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double latitudRecibida))
                {
                    Latitud = latitudRecibida;
                }
            }
        }

        public string? LongitudParam
        {
            get => longitudParam;
            set
            {
                longitudParam = value;

                if (double.TryParse(
                        value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double longitudRecibida))
                {
                    Longitud = longitudRecibida;
                }
            }
        }

        public TerrenoRequest? Terreno
        {
            get => terreno;
            set
            {
                int terrenoAnteriorId = terreno?.TerrenoId ?? 0;
                int terrenoNuevoId = value?.TerrenoId ?? 0;

                terreno = value;

                if (terrenoAnteriorId != terrenoNuevoId)
                    LimpiarFotosTerreno();

                if (value != null)
                {
                    CodigoTerreno = value.CodigoTerreno ?? string.Empty;
                    IdentificacionPropietarioTerreno =
                        value.IdentificacionPropietarioTerreno ?? string.Empty;
                    NombrePropietarioTerreno =
                        value.NombrePropietarioTerreno ?? string.Empty;
                    TelefonoPropietarioTexto =
                        value.TelefonoPropietario?.ToString(
                            CultureInfo.InvariantCulture) ?? string.Empty;
                    CorreoPropietario = value.CorreoPropietario ?? string.Empty;
                    DireccionTerreno = value.DireccionTerreno ?? string.Empty;
                    ExtensionManzanaTerreno = value.ExtensionManzanaTerreno;
                    CantidadQuintalesOro = value.CantidadQuintalesOro;
                    CantidadPlantasTerreno = value.CantidadPlantasTerreno;
                    FechaIngresoTerreno = value.FechaIngresoTerreno ??
                        DateOnly.FromDateTime(DateTime.Today);

                    if (LatitudParam == null && LongitudParam == null)
                    {
                        Latitud = value.Latitud;
                        Longitud = value.Longitud;
                    }
                }

                OnPropertyChanged();

                if (inicializado && terrenoNuevoId > 0)
                    _ = ReasignarSeleccionPickersAsync();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                    return;

                mode = value;

                if (mode == FormMode.FormModeSelect.Create &&
                    Terreno?.TerrenoId is null or <= 0)
                {
                    CodigoTerreno = string.Empty;
                    LimpiarFotosSiSonDeTerrenoAnterior();
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(AllowEdit));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));
                ActualizarComandosFormulario();
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEnabled => !IsReadOnly;
        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;
        public bool AllowEdit => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear terreno",
            FormMode.FormModeSelect.Edit => "Editar terreno",
            FormMode.FormModeSelect.View => "Detalles del terreno",
            _ => "Terreno"
        };

        public string? CodigoTerreno
        {
            get => codigoTerreno;
            set => AsignarCampo(ref codigoTerreno, value);
        }

        public string? IdentificacionPropietarioTerreno
        {
            get => identificacionPropietarioTerreno;
            set => AsignarCampo(ref identificacionPropietarioTerreno, value);
        }

        public string? NombrePropietarioTerreno
        {
            get => nombrePropietarioTerreno;
            set => AsignarCampo(ref nombrePropietarioTerreno, value);
        }

        public string? TelefonoPropietarioTexto
        {
            get => telefonoPropietarioTexto;
            set => AsignarCampo(ref telefonoPropietarioTexto, value);
        }

        public string? CorreoPropietario
        {
            get => correoPropietario;
            set => AsignarCampo(ref correoPropietario, value);
        }

        public string? DireccionTerreno
        {
            get => direccionTerreno;
            set => AsignarCampo(ref direccionTerreno, value);
        }

        public decimal? ExtensionManzanaTerreno
        {
            get => extensionManzanaTerreno;
            set
            {
                if (extensionManzanaTerreno == value)
                    return;

                extensionManzanaTerreno = value;
                extensionManzanaTexto = FormatearDecimal(value);

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtensionManzanaTexto));
            }
        }

        public string ExtensionManzanaTexto
        {
            get => extensionManzanaTexto;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (extensionManzanaTexto == nuevoValor)
                    return;

                extensionManzanaTexto = nuevoValor;
                extensionManzanaTerreno = ParseDecimalNullable(nuevoValor);

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtensionManzanaTerreno));
            }
        }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                if (cantidadQuintalesOro == value)
                    return;

                cantidadQuintalesOro = value;
                cantidadQuintalesOroTexto = FormatearDecimal(value);

                OnPropertyChanged();
                OnPropertyChanged(nameof(CantidadQuintalesOroTexto));
            }
        }

        public string CantidadQuintalesOroTexto
        {
            get => cantidadQuintalesOroTexto;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (cantidadQuintalesOroTexto == nuevoValor)
                    return;

                cantidadQuintalesOroTexto = nuevoValor;
                cantidadQuintalesOro = ParseDecimalNullable(nuevoValor);

                OnPropertyChanged();
                OnPropertyChanged(nameof(CantidadQuintalesOro));
            }
        }

        public int? CantidadPlantasTerreno
        {
            get => cantidadPlantasTerreno;
            set
            {
                if (cantidadPlantasTerreno == value)
                    return;

                cantidadPlantasTerreno = value;
                cantidadPlantasTerrenoTexto =
                    value?.ToString(CultureInfo.InvariantCulture) ??
                    string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CantidadPlantasTerrenoTexto));
            }
        }

        public string CantidadPlantasTerrenoTexto
        {
            get => cantidadPlantasTerrenoTexto;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (cantidadPlantasTerrenoTexto == nuevoValor)
                    return;

                cantidadPlantasTerrenoTexto = nuevoValor;
                cantidadPlantasTerreno = ParseEnteroNullable(nuevoValor);

                OnPropertyChanged();
                OnPropertyChanged(nameof(CantidadPlantasTerreno));
            }
        }

        public DateOnly? FechaIngresoTerreno
        {
            get => fechaIngresoTerreno;
            set
            {
                if (fechaIngresoTerreno == value)
                    return;

                fechaIngresoTerreno = value;

                if (value.HasValue)
                {
                    fechaIngresoDate =
                        value.Value.ToDateTime(TimeOnly.MinValue);
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
                DateTime fechaNormalizada = value.Date;

                if (fechaIngresoDate.Date == fechaNormalizada)
                    return;

                fechaIngresoDate = fechaNormalizada;
                fechaIngresoTerreno =
                    DateOnly.FromDateTime(fechaNormalizada);

                OnPropertyChanged();
                OnPropertyChanged(nameof(FechaIngresoTerreno));
            }
        }

        public double? Latitud
        {
            get => latitud;
            set
            {
                if (latitud == value)
                    return;

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
                if (longitud == value)
                    return;

                longitud = value;
                OnPropertyChanged();
                RefrescarMapaAction?.Invoke(latitud, longitud);
            }
        }

        public string? CoordenadasTexto
        {
            get => coordenadasTexto;
            set
            {
                if (coordenadasTexto == value)
                    return;

                coordenadasTexto = value;
                OnPropertyChanged();
                ProcesarCoordenadas(value);
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
                NotificarDisponibilidadPickers();

                if (!actualizandoSeleccionInterna)
                    _ = CambiarPaisAsync(value);
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
                NotificarDisponibilidadPickers();

                if (!actualizandoSeleccionInterna)
                    _ = CambiarDepartamentoAsync(value);
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                if (ReferenceEquals(municipioSeleccionado, value))
                    return;

                municipioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public bool CanPickDepartamento =>
            IsEnabled && PaisSeleccionado != null && !IsBusy;

        public bool CanPickMunicipio =>
            IsEnabled && DepartamentoSeleccionado != null && !IsBusy;

        public bool TieneFotosTerreno => FotosTerreno.Count > 0;
        public bool NoTieneFotosTerreno => FotosTerreno.Count == 0;

        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        public async Task InicializarAsync()
        {
            await inicializacionLock.WaitAsync();
            CancelarYRenovar(ref inicializacionCts);
            CancellationToken cancellationToken = inicializacionCts.Token;

            try
            {
                CambiarEstadoOcupado(true);

                if (Mode == FormMode.FormModeSelect.Create)
                    LimpiarFotosSiSonDeTerrenoAnterior();

                if (!inicializado)
                {
                    bool paisesCargados =
                        await CargarPaisesAsync(cancellationToken);

                    if (!paisesCargados)
                        return;

                    inicializado = true;
                }

                if (Terreno?.MunicipioId is > 0)
                {
                    await ResolverSeleccionPorMunicipioIdAsync(
                        Terreno.MunicipioId,
                        cancellationToken);
                }

                if (Terreno?.TerrenoId is > 0 &&
                    fotosCargadasTerrenoId != Terreno.TerrenoId)
                {
                    await CargarFotosTerrenoAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al reemplazar una solicitud o navegar.
            }
            finally
            {
                CambiarEstadoOcupado(false);
                inicializacionLock.Release();
            }
        }

        public async Task ReasignarSeleccionPickersAsync()
        {
            if (!inicializado || Terreno?.MunicipioId is null or <= 0)
                return;

            CancelarYRenovar(ref inicializacionCts);
            CancellationToken cancellationToken = inicializacionCts.Token;

            try
            {
                await ResolverSeleccionPorMunicipioIdAsync(
                    Terreno.MunicipioId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Se seleccionó otro terreno o se abandonó la pantalla.
            }
        }

        public void CancelarOperaciones()
        {
            Cancelar(ref inicializacionCts);
            Cancelar(ref departamentoCts);
            Cancelar(ref municipioCts);
            Cancelar(ref fotosCts);
            Cancelar(ref guardadoCts);
        }

        public async Task AbrirEnGoogleMaps(double lat, double lon)
        {
            try
            {
                string url =
                    $"https://www.google.com/maps?q=" +
                    $"{lat.ToString(CultureInfo.InvariantCulture)}," +
                    $"{lon.ToString(CultureInfo.InvariantCulture)}";

                await Launcher.OpenAsync(url);
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible abrir la ubicación en Google Maps.");
            }
        }

        public void ConvertirDesdeGoogleMaps(string texto)
        {
            ProcesarCoordenadas(texto);
        }

        private async Task CambiarPaisAsync(PaisResponse? pais)
        {
            CancelarYRenovar(ref departamentoCts);
            CancellationToken cancellationToken = departamentoCts.Token;

            try
            {
                actualizandoSeleccionInterna = true;
                departamentoSeleccionado = null;
                municipioSeleccionado = null;
                Departamentos.Clear();
                Municipios.Clear();
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
                NotificarDisponibilidadPickers();
            }
            finally
            {
                actualizandoSeleccionInterna = false;
            }

            if (pais?.PaisId > 0)
            {
                await CargarDepartamentosAsync(
                    pais.PaisId,
                    cancellationToken,
                    mostrarError: true);
            }
        }

        private async Task CambiarDepartamentoAsync(
            DepartamentoResponse? departamento)
        {
            CancelarYRenovar(ref municipioCts);
            CancellationToken cancellationToken = municipioCts.Token;

            try
            {
                actualizandoSeleccionInterna = true;
                municipioSeleccionado = null;
                Municipios.Clear();
                OnPropertyChanged(nameof(MunicipioSeleccionado));
                NotificarDisponibilidadPickers();
            }
            finally
            {
                actualizandoSeleccionInterna = false;
            }

            if (departamento?.DepartamentoId is > 0)
            {
                await CargarMunicipiosAsync(
                    departamento.DepartamentoId,
                    cancellationToken,
                    mostrarError: true);
            }
        }

        private async Task<bool> CargarPaisesAsync(
            CancellationToken cancellationToken)
        {
            ApiResult<ObservableCollection<PaisResponse>> result =
                await paisApiService.GetPaisResultAsync(cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return false;
            }

            Paises.Clear();

            foreach (PaisResponse pais in result.Data)
            {
                if (pais.PaisId > 0)
                    Paises.Add(pais);
            }

            OnPropertyChanged(nameof(Paises));
            return true;
        }

        private async Task<bool> CargarDepartamentosAsync(
            int? paisId,
            CancellationToken cancellationToken,
            bool mostrarError)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
                return false;

            ApiResult<ObservableCollection<DepartamentoResponse>> result =
                await departamentoApiService.GetDepartamentosResultAsync(
                    paisId,
                    cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (mostrarError && !cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return false;
            }

            if (!actualizandoSeleccionInterna &&
                PaisSeleccionado?.PaisId != paisId.Value)
            {
                return false;
            }

            Departamentos.Clear();

            foreach (DepartamentoResponse departamento in result.Data)
                Departamentos.Add(departamento);

            OnPropertyChanged(nameof(Departamentos));
            return true;
        }

        private async Task<bool> CargarMunicipiosAsync(
            int? departamentoId,
            CancellationToken cancellationToken,
            bool mostrarError)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
                return false;

            ApiResult<ObservableCollection<MunicipioResponse>> result =
                await municipioApiService.GetMunicipiosResultAsync(
                    departamentoId,
                    cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (mostrarError && !cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return false;
            }

            if (!actualizandoSeleccionInterna &&
                DepartamentoSeleccionado?.DepartamentoId !=
                departamentoId.Value)
            {
                return false;
            }

            Municipios.Clear();

            foreach (MunicipioResponse municipio in result.Data)
                Municipios.Add(municipio);

            OnPropertyChanged(nameof(Municipios));
            return true;
        }

        private async Task ResolverSeleccionPorMunicipioIdAsync(
            int? municipioId,
            CancellationToken cancellationToken)
        {
            if (!municipioId.HasValue || municipioId.Value <= 0)
                return;

            ApiResult<ObservableCollection<MunicipioResponse>> ubicacionesResult =
                await municipioApiService
                    .GetMunicipiosConUbicacionResultAsync(cancellationToken);

            if (!ubicacionesResult.Success ||
                ubicacionesResult.Data == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(ubicacionesResult.Message);

                return;
            }

            MunicipioResponse? ubicacion =
                ubicacionesResult.Data.FirstOrDefault(
                    item => item.MunicipioId == municipioId.Value);

            if (ubicacion?.PaisId is null or <= 0 ||
                ubicacion.DepartamentoId is null or <= 0)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MostrarErrorAsync(
                        "No fue posible determinar la ubicación administrativa del terreno.");
                }

                return;
            }

            PaisResponse? pais = Paises.FirstOrDefault(
                item => item.PaisId == ubicacion.PaisId.Value);

            if (pais == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MostrarErrorAsync(
                        "El país del terreno no está disponible o se encuentra inactivo.");
                }

                return;
            }

            ApiResult<ObservableCollection<DepartamentoResponse>>
                departamentosResult =
                    await departamentoApiService.GetDepartamentosResultAsync(
                        pais.PaisId,
                        cancellationToken);

            if (!departamentosResult.Success ||
                departamentosResult.Data == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(departamentosResult.Message);

                return;
            }

            DepartamentoResponse? departamento =
                departamentosResult.Data.FirstOrDefault(
                    item => item.DepartamentoId ==
                        ubicacion.DepartamentoId.Value);

            if (departamento == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MostrarErrorAsync(
                        "El departamento del terreno no está disponible o se encuentra inactivo.");
                }

                return;
            }

            ApiResult<ObservableCollection<MunicipioResponse>> municipiosResult =
                await municipioApiService.GetMunicipiosResultAsync(
                    departamento.DepartamentoId,
                    cancellationToken);

            if (!municipiosResult.Success || municipiosResult.Data == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(municipiosResult.Message);

                return;
            }

            MunicipioResponse? municipio =
                municipiosResult.Data.FirstOrDefault(
                    item => item.MunicipioId == municipioId.Value);

            if (municipio == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MostrarErrorAsync(
                        "El municipio del terreno no está disponible o se encuentra inactivo.");
                }

                return;
            }

            try
            {
                actualizandoSeleccionInterna = true;

                Departamentos.Clear();
                foreach (DepartamentoResponse item in departamentosResult.Data)
                    Departamentos.Add(item);

                Municipios.Clear();
                foreach (MunicipioResponse item in municipiosResult.Data)
                    Municipios.Add(item);

                paisSeleccionado = pais;
                departamentoSeleccionado = departamento;
                municipioSeleccionado = municipio;

                OnPropertyChanged(nameof(PaisSeleccionado));
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
                OnPropertyChanged(nameof(Departamentos));
                OnPropertyChanged(nameof(Municipios));
                NotificarDisponibilidadPickers();
            }
            finally
            {
                actualizandoSeleccionInterna = false;
            }
        }

        private async Task AbrirGaleriaFotosAsync(FotoTerrenoItem? foto)
        {
            if (foto == null || FotosTerreno.Count == 0)
                return;

            try
            {
                await Shell.Current.GoToAsync(
                    AppRoutes.FotosTerrenoGaleria,
                    true,
                    new Dictionary<string, object>
                    {
                        ["Fotos"] = FotosTerreno.ToList(),
                        ["FotoInicial"] = foto
                    });
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible abrir la galería de fotografías.");
            }
        }

        private async Task CargarFotosTerrenoAsync(
            CancellationToken cancellationToken = default)
        {
            if (Terreno?.TerrenoId is null or <= 0)
                return;

            int terrenoIdActual = Terreno.TerrenoId.Value;
            CancelarYRenovar(ref fotosCts);

            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    fotosCts.Token);

            ApiResult<List<FotoTerrenoResponse>> result =
                await fotoTerrenoApiService.GetFotosPorTerrenoResultAsync(
                    terrenoIdActual,
                    linkedCts.Token);

            if (!result.Success || result.Data == null)
            {
                if (!linkedCts.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return;
            }

            var fotosPreparadas = new List<FotoTerrenoItem>();

            foreach (FotoTerrenoResponse foto in result.Data)
            {
                string urlCompleta =
                    fotoTerrenoApiService.ConstruirUrlCompleta(
                        foto.UrlFotoTerreno);

                if (string.IsNullOrWhiteSpace(urlCompleta) ||
                    !Uri.TryCreate(urlCompleta, UriKind.Absolute, out Uri? uri))
                {
                    continue;
                }

                string urlMiniatura = ImagenMiniaturaUrlService.Crear(
                    urlCompleta,
                    ancho: 420,
                    alto: 420,
                    calidad: 68);

                Uri imagenUri = Uri.TryCreate(
                    urlMiniatura,
                    UriKind.Absolute,
                    out Uri? miniaturaUri)
                        ? miniaturaUri
                        : uri;

                fotosPreparadas.Add(new FotoTerrenoItem
                {
                    FotoTerrenoId = foto.FotoTerrenoId,
                    TerrenoId = foto.TerrenoId,
                    UrlFotoTerreno = urlCompleta,
                    LocalPath = null,
                    NombreArchivo = Path.GetFileName(uri.LocalPath),
                    EsNueva = false,
                    Imagen = ImageSource.FromUri(imagenUri)
                });
            }

            if (Terreno?.TerrenoId != terrenoIdActual)
                return;

            LimpiarFotosTerreno();

            foreach (FotoTerrenoItem foto in fotosPreparadas)
                FotosTerreno.Add(foto);

            fotosCargadasTerrenoId = terrenoIdActual;
        }

        private async Task SeleccionarFotosAsync()
        {
            if (!AllowEdit || IsBusy)
                return;

            try
            {
                var opciones = new PickOptions
                {
                    PickerTitle = "Seleccione fotos del terreno",
                    FileTypes = FilePickerFileType.Images
                };

                IEnumerable<FileResult>? archivos =
                    await FilePicker.PickMultipleAsync(opciones);

                if (archivos == null)
                    return;

                foreach (FileResult archivo in archivos)
                {
                    string extension = Path.GetExtension(archivo.FileName);

                    if (string.IsNullOrWhiteSpace(extension))
                        extension = ".jpg";

                    string rutaTemporal = Path.Combine(
                        FileSystem.CacheDirectory,
                        $"{Guid.NewGuid():N}{extension}");

                    await using Stream origen = await archivo.OpenReadAsync();
                    await using FileStream destino = File.Create(rutaTemporal);
                    await origen.CopyToAsync(destino);

                    FotosTerreno.Add(new FotoTerrenoItem
                    {
                        FotoTerrenoId = null,
                        TerrenoId = Terreno?.TerrenoId,
                        UrlFotoTerreno = null,
                        LocalPath = rutaTemporal,
                        NombreArchivo = archivo.FileName,
                        EsNueva = true,
                        Imagen = ImageSource.FromFile(rutaTemporal)
                    });
                }
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible seleccionar las fotografías.");
            }
        }

        private async Task QuitarFotoAsync(FotoTerrenoItem? foto)
        {
            if (foto == null || !AllowEdit || IsBusy)
                return;

            if (foto.EsNueva || foto.FotoTerrenoId is null or <= 0)
            {
                FotosTerreno.Remove(foto);
                EliminarArchivoTemporal(foto.LocalPath);
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Eliminar foto",
                "¿Desea eliminar esta foto del terreno?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<bool> result =
                    await fotoTerrenoApiService.EliminarFotoResultAsync(
                        foto.FotoTerrenoId.Value);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                FotosTerreno.Remove(foto);
                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Foto eliminada correctamente."
                        : result.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
        }

        private async Task<ApiResult<bool>> SubirFotosPendientesAsync(
            int terrenoId,
            CancellationToken cancellationToken)
        {
            List<FotoTerrenoItem> fotosNuevas = FotosTerreno
                .Where(f => f.EsNueva &&
                            !string.IsNullOrWhiteSpace(f.LocalPath))
                .ToList();

            if (fotosNuevas.Count == 0)
            {
                return ApiResult<bool>.Ok(
                    true,
                    "No hay fotografías pendientes de subir.");
            }

            ApiResult<bool> result =
                await fotoTerrenoApiService.SubirFotosResultAsync(
                    terrenoId,
                    fotosNuevas,
                    cancellationToken);

            if (!result.Success)
                return result;

            foreach (FotoTerrenoItem foto in fotosNuevas)
            {
                foto.EsNueva = false;
                foto.TerrenoId = terrenoId;
            }

            fotosCargadasTerrenoId = terrenoId;
            return result;
        }

        private async Task ObtenerGpsAsync()
        {
            if (!AllowEdit || IsBusy)
                return;

            try
            {
                PermissionStatus status =
                    await Permissions.CheckStatusAsync<
                        Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<
                        Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await MostrarAdvertenciaAsync(
                        "Permiso de ubicación denegado.");
                    return;
                }

                Location? location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(
                        GeolocationAccuracy.Medium,
                        TimeSpan.FromSeconds(20)));

                if (location == null)
                {
                    await MostrarAdvertenciaAsync(
                        "No se pudo obtener la ubicación actual.");
                    return;
                }

                Latitud = location.Latitude;
                Longitud = location.Longitude;
            }
            catch (FeatureNotEnabledException)
            {
                await MostrarAdvertenciaAsync(
                    "Active la ubicación del dispositivo e intente nuevamente.");
            }
            catch (PermissionException)
            {
                await MostrarAdvertenciaAsync(
                    "No fue posible acceder a la ubicación del dispositivo.");
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible obtener la ubicación actual.");
            }
        }

        private async Task SeleccionarMapaAsync()
        {
            if (!AllowEdit || IsBusy)
                return;

            Terreno ??= new TerrenoRequest();
            CopiarFormularioATerrenoTemporal(Terreno);

            await Shell.Current.GoToAsync(
                AppRoutes.MapaSeleccion,
                true,
                new Dictionary<string, object>
                {
                    ["latitudActual"] =
                        (Latitud ?? 12.1364)
                            .ToString(CultureInfo.InvariantCulture),
                    ["longitudActual"] =
                        (Longitud ?? -86.2510)
                            .ToString(CultureInfo.InvariantCulture),
                    ["Mode"] = Mode,
                    ["Terreno"] = Terreno
                });
        }

        private async Task SaveAsync()
        {
            if (IsBusy || IsReadOnly)
                return;

            CancelarYRenovar(ref guardadoCts);
            CancellationToken cancellationToken = guardadoCts.Token;
            CambiarEstadoOcupado(true);

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateTerrenoAsync(cancellationToken);
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateTerrenoAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar el guardado.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el terreno",
                    ex);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
        }

        private async Task CreateTerrenoAsync(
            CancellationToken cancellationToken)
        {
            if (!ValidateFieldsData())
                return;

            bool confirmar = await ConfirmarGuardadoAsync("el terreno");

            if (!confirmar)
                return;

            TerrenoRequest request = CrearRequestFormulario();

            ApiResult<TerrenoResponse> result =
                await terrenoApiService.CreateTerrenoRetornandoResultAsync(
                    request,
                    cancellationToken);

            if (!result.Success || result.Data?.TerrenoId is null or <= 0)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return;
            }

            int terrenoId = result.Data.TerrenoId.Value;
            CodigoTerreno = result.Data.CodigoTerreno;

            ApiResult<bool> fotosResult =
                await SubirFotosPendientesAsync(
                    terrenoId,
                    cancellationToken);

            await GoToTerrenoPage();

            if (fotosResult.Success)
            {
                await MostrarExitoAsync(
                    $"Terreno {CodigoTerreno} guardado correctamente.");
            }
            else
            {
                await MostrarAdvertenciaAsync(
                    "El terreno se guardó correctamente, pero no se pudieron subir todas las fotografías. Puede intentarlo nuevamente al editarlo.");
            }
        }

        private async Task UpdateTerrenoAsync(
            CancellationToken cancellationToken)
        {
            if (!ValidateFieldsData())
                return;

            if (Terreno?.TerrenoId is null or <= 0)
            {
                await MostrarErrorAsync(
                    "No se encontró el terreno que se desea actualizar.");
                return;
            }

            bool confirmar = await ConfirmarActualizacionAsync("el terreno");

            if (!confirmar)
                return;

            TerrenoRequest request = CrearRequestFormulario();
            request.TerrenoId = Terreno.TerrenoId;
            request.CodigoTerreno = Terreno.CodigoTerreno;

            ApiResult<bool> result =
                await terrenoApiService.UpdateTerrenoResultAsync(
                    request,
                    cancellationToken);

            if (!result.Success || result.Data != true)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarErrorAsync(result.Message);

                return;
            }

            ApiResult<bool> fotosResult =
                await SubirFotosPendientesAsync(
                    Terreno.TerrenoId.Value,
                    cancellationToken);

            await GoToTerrenoPage();

            if (fotosResult.Success)
            {
                await MostrarExitoAsync(
                    "Terreno actualizado correctamente.");
            }
            else
            {
                await MostrarAdvertenciaAsync(
                    "El terreno se actualizó correctamente, pero no se pudieron subir todas las fotografías. Puede intentarlo nuevamente al editarlo.");
            }
        }

        private TerrenoRequest CrearRequestFormulario()
        {
            return new TerrenoRequest
            {
                // En creación queda vacío: el backend genera el código.
                // En edición se conserva únicamente por compatibilidad;
                // el backend nunca permite modificarlo.
                CodigoTerreno = Mode == FormMode.FormModeSelect.Create
                    ? null
                    : Terreno?.CodigoTerreno,
                IdentificacionPropietarioTerreno =
                    IdentificacionPropietarioTerreno?
                        .Trim()
                        .ToUpperInvariant(),
                NombrePropietarioTerreno =
                    NombrePropietarioTerreno?.Trim(),
                TelefonoPropietario =
                    ParseTelefono(TelefonoPropietarioTexto),
                CorreoPropietario =
                    string.IsNullOrWhiteSpace(CorreoPropietario)
                        ? null
                        : CorreoPropietario.Trim(),
                DireccionTerreno = DireccionTerreno?.Trim(),
                ExtensionManzanaTerreno = ExtensionManzanaTerreno,
                CantidadQuintalesOro = CantidadQuintalesOro ?? 0,
                CantidadPlantasTerreno = CantidadPlantasTerreno ?? 0,
                // Se envía solo por compatibilidad con clientes existentes.
                // La API asigna la fecha real al crear y la conserva al editar.
                FechaIngresoTerreno = Terreno?.FechaIngresoTerreno ??
                    DateOnly.FromDateTime(DateTime.Today),
                MunicipioId = MunicipioSeleccionado?.MunicipioId ??
                    Terreno?.MunicipioId ?? 0,
                Latitud = Latitud,
                Longitud = Longitud
            };
        }

        private void CopiarFormularioATerrenoTemporal(
            TerrenoRequest destino)
        {
            destino.CodigoTerreno =
                Mode == FormMode.FormModeSelect.Create
                    ? null
                    : Terreno?.CodigoTerreno;
            destino.IdentificacionPropietarioTerreno =
                IdentificacionPropietarioTerreno;
            destino.NombrePropietarioTerreno = NombrePropietarioTerreno;
            destino.TelefonoPropietario =
                ParseTelefono(TelefonoPropietarioTexto);
            destino.CorreoPropietario = CorreoPropietario;
            destino.DireccionTerreno = DireccionTerreno;
            destino.ExtensionManzanaTerreno = ExtensionManzanaTerreno;
            destino.CantidadQuintalesOro = CantidadQuintalesOro;
            destino.CantidadPlantasTerreno = CantidadPlantasTerreno;
            destino.FechaIngresoTerreno = FechaIngresoTerreno;
            destino.MunicipioId =
                MunicipioSeleccionado?.MunicipioId ??
                Terreno?.MunicipioId ?? 0;
            destino.Latitud = Latitud;
            destino.Longitud = Longitud;
        }

        private bool ValidateFieldsData()
        {
            string cedula =
                IdentificacionPropietarioTerreno?
                    .Trim()
                    .ToUpperInvariant() ?? string.Empty;

            if (!CedulaRegex.IsMatch(cedula))
            {
                MostrarValidacion(
                    "La identificación debe tener el formato 001-080701-1050R.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NombrePropietarioTerreno))
            {
                MostrarValidacion(
                    "El nombre del propietario es obligatorio.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DireccionTerreno))
            {
                MostrarValidacion(
                    "La dirección del terreno es obligatoria.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CorreoPropietario) &&
                !CorreoRegex.IsMatch(CorreoPropietario.Trim()))
            {
                MostrarValidacion(
                    "El correo del propietario no tiene un formato válido.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TelefonoPropietarioTexto) &&
                !TelefonoPropietarioTexto.All(char.IsDigit))
            {
                MostrarValidacion(
                    "El teléfono solo debe contener números.");
                return false;
            }

            if (ExtensionManzanaTerreno is null or <= 0)
            {
                MostrarValidacion(
                    "La extensión del terreno debe ser mayor que cero.");
                return false;
            }

            if (CantidadQuintalesOro is < 0)
            {
                MostrarValidacion(
                    "La cantidad de quintales no puede ser negativa.");
                return false;
            }

            if (CantidadPlantasTerreno is < 0)
            {
                MostrarValidacion(
                    "La cantidad de plantas no puede ser negativa.");
                return false;
            }

            int municipioId = MunicipioSeleccionado?.MunicipioId ??
                Terreno?.MunicipioId ?? 0;

            if (municipioId <= 0)
            {
                MostrarValidacion("Debe seleccionar un municipio.");
                return false;
            }

            if (!Latitud.HasValue || !Longitud.HasValue)
            {
                MostrarValidacion(
                    "Debe definir la ubicación del terreno.");
                return false;
            }

            if (Latitud.Value is < -90 or > 90)
            {
                MostrarValidacion(
                    "La latitud debe estar entre -90 y 90.");
                return false;
            }

            if (Longitud.Value is < -180 or > 180)
            {
                MostrarValidacion(
                    "La longitud debe estar entre -180 y 180.");
                return false;
            }

            return true;
        }

        private void MostrarValidacion(string mensaje)
        {
            _ = MostrarAdvertenciaAsync(mensaje);
        }

        private async Task CancelAsync()
        {
            try
            {
                IsCancel = HayCambiosSinGuardar();

                if (IsCancel)
                {
                    bool confirmar = await ConfirmarSalidaSinGuardarAsync();

                    if (!confirmar)
                        return;
                }

                await GoToTerrenoPage();
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible cancelar el formulario.");
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool HayCambiosSinGuardar()
        {
            if (Mode == FormMode.FormModeSelect.View)
                return false;

            if (Terreno == null || Terreno.TerrenoId is null or <= 0)
            {
                return !string.IsNullOrWhiteSpace(
                           IdentificacionPropietarioTerreno) ||
                       !string.IsNullOrWhiteSpace(
                           NombrePropietarioTerreno) ||
                       !string.IsNullOrWhiteSpace(
                           TelefonoPropietarioTexto) ||
                       !string.IsNullOrWhiteSpace(CorreoPropietario) ||
                       !string.IsNullOrWhiteSpace(DireccionTerreno) ||
                       ExtensionManzanaTerreno.HasValue ||
                       CantidadQuintalesOro.HasValue ||
                       CantidadPlantasTerreno.HasValue ||
                       PaisSeleccionado != null ||
                       DepartamentoSeleccionado != null ||
                       MunicipioSeleccionado != null ||
                       Latitud.HasValue ||
                       Longitud.HasValue ||
                       FotosTerreno.Any(f => f.EsNueva);
            }

            return !string.Equals(
                       IdentificacionPropietarioTerreno,
                       Terreno.IdentificacionPropietarioTerreno,
                       StringComparison.Ordinal) ||
                   !string.Equals(
                       NombrePropietarioTerreno,
                       Terreno.NombrePropietarioTerreno,
                       StringComparison.Ordinal) ||
                   TelefonoPropietarioTexto !=
                       Terreno.TelefonoPropietario?.ToString(
                           CultureInfo.InvariantCulture) ||
                   !string.Equals(
                       CorreoPropietario,
                       Terreno.CorreoPropietario,
                       StringComparison.Ordinal) ||
                   !string.Equals(
                       DireccionTerreno,
                       Terreno.DireccionTerreno,
                       StringComparison.Ordinal) ||
                   ExtensionManzanaTerreno !=
                       Terreno.ExtensionManzanaTerreno ||
                   CantidadQuintalesOro !=
                       Terreno.CantidadQuintalesOro ||
                   CantidadPlantasTerreno !=
                       Terreno.CantidadPlantasTerreno ||
                   Latitud != Terreno.Latitud ||
                   Longitud != Terreno.Longitud ||
                   (MunicipioSeleccionado?.MunicipioId ??
                    Terreno.MunicipioId) != Terreno.MunicipioId ||
                   FotosTerreno.Any(f => f.EsNueva);
        }

        private void ProcesarCoordenadas(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            string valor = texto.Trim();

            MatchCollection numeros = Regex.Matches(
                valor.Replace(',', '.'),
                @"-?\d+(?:\.\d+)?");

            if (numeros.Count == 2 &&
                double.TryParse(
                    numeros[0].Value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double latitudDecimal) &&
                double.TryParse(
                    numeros[1].Value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double longitudDecimal) &&
                latitudDecimal is >= -90 and <= 90 &&
                longitudDecimal is >= -180 and <= 180)
            {
                Latitud = latitudDecimal;
                Longitud = longitudDecimal;
                return;
            }

            Match dms = Regex.Match(
                valor,
                "(?<latG>\\d{1,2})°(?<latM>\\d{1,2})'(?<latS>\\d+(?:\\.\\d+)?)\"?(?<latD>[NS])\\s+" +
                "(?<lonG>\\d{1,3})°(?<lonM>\\d{1,2})'(?<lonS>\\d+(?:\\.\\d+)?)\"?(?<lonD>[EW])",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

            if (!dms.Success)
                return;

            double lat = ConvertirDms(
                dms.Groups["latG"].Value,
                dms.Groups["latM"].Value,
                dms.Groups["latS"].Value,
                dms.Groups["latD"].Value);

            double lon = ConvertirDms(
                dms.Groups["lonG"].Value,
                dms.Groups["lonM"].Value,
                dms.Groups["lonS"].Value,
                dms.Groups["lonD"].Value);

            Latitud = lat;
            Longitud = lon;
        }

        private static double ConvertirDms(
            string grados,
            string minutos,
            string segundos,
            string direccion)
        {
            double valor =
                double.Parse(grados, CultureInfo.InvariantCulture) +
                double.Parse(minutos, CultureInfo.InvariantCulture) / 60d +
                double.Parse(segundos, CultureInfo.InvariantCulture) / 3600d;

            if (direccion.Equals("S", StringComparison.OrdinalIgnoreCase) ||
                direccion.Equals("W", StringComparison.OrdinalIgnoreCase))
            {
                valor *= -1d;
            }

            return valor;
        }

        private Task GoToTerrenoPage() =>
            GoToAsyncParameters(AppRoutes.Terrenos);

        private static string FormatearDecimal(decimal? valor)
        {
            return valor?.ToString(
                       "0.##",
                       CultureInfo.CurrentCulture) ??
                   string.Empty;
        }

        private static decimal? ParseDecimalNullable(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal valorActual))
            {
                return valorActual;
            }

            string normalizado = texto.Trim().Replace(',', '.');

            return decimal.TryParse(
                normalizado,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorInvariante)
                    ? valorInvariante
                    : null;
        }

        private static int? ParseEnteroNullable(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            return int.TryParse(
                texto,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int valor)
                    ? valor
                    : null;
        }

        private static int? ParseTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return null;

            return int.TryParse(
                telefono,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int valor)
                    ? valor
                    : null;
        }

        private void LimpiarFotosSiSonDeTerrenoAnterior()
        {
            bool hayFotosCargadasDesdeApi =
                fotosCargadasTerrenoId.HasValue;
            bool hayFotosExistentes = FotosTerreno.Any(f => !f.EsNueva);

            if (hayFotosCargadasDesdeApi || hayFotosExistentes)
                LimpiarFotosTerreno();
        }

        private void LimpiarFotosTerreno()
        {
            foreach (FotoTerrenoItem foto in FotosTerreno.Where(f => f.EsNueva))
                EliminarArchivoTemporal(foto.LocalPath);

            FotosTerreno.Clear();
            fotosCargadasTerrenoId = null;
            OnPropertyChanged(nameof(TieneFotosTerreno));
            OnPropertyChanged(nameof(NoTieneFotosTerreno));
        }

        private static void EliminarArchivoTemporal(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            try
            {
                if (File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch
            {
                // La limpieza del caché no debe interrumpir el formulario.
            }
        }

        private void CambiarEstadoOcupado(bool valor)
        {
            IsBusy = valor;
            NotificarDisponibilidadPickers();
            ActualizarComandosFormulario();
        }

        private void NotificarDisponibilidadPickers()
        {
            OnPropertyChanged(nameof(CanPickDepartamento));
            OnPropertyChanged(nameof(CanPickMunicipio));
        }

        private void ActualizarComandosFormulario()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
            ObtenerGpsCommand.ChangeCanExecute();
            SeleccionarMapaCommand.ChangeCanExecute();
            SeleccionarFotosCommand.ChangeCanExecute();
            QuitarFotoCommand.ChangeCanExecute();
            AbrirGaleriaFotosCommand.ChangeCanExecute();
        }

        private void AsignarCampo(
            ref string? campo,
            string? valor,
            [System.Runtime.CompilerServices.CallerMemberName]
            string? propertyName = null)
        {
            if (campo == valor)
                return;

            campo = valor;
            OnPropertyChanged(propertyName);
        }

        private static void CancelarYRenovar(
            ref CancellationTokenSource? cancellationTokenSource)
        {
            Cancelar(ref cancellationTokenSource);
            cancellationTokenSource = new CancellationTokenSource();
        }

        private static void Cancelar(
            ref CancellationTokenSource? cancellationTokenSource)
        {
            if (cancellationTokenSource == null)
                return;

            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }
    }
}
