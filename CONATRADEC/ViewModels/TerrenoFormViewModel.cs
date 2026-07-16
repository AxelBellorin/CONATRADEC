using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(nameof(LatitudParam), "latitud")]
    [QueryProperty(nameof(LongitudParam), "longitud")]
    [QueryProperty(nameof(Mode), "Mode")]
    [QueryProperty(nameof(Terreno), "Terreno")]
    public class TerrenoFormViewModel : GlobalService
    {
        // ==================== Estado interno ====================

        private TerrenoRequest? terreno;
        private bool isCancel;

        private string? codigoTerreno;
        private string? identificacionPropietarioTerreno;
        private string? nombrePropietarioTerreno;
        private string? telefonoPropietarioTexto;
        private string? correoPropietario;
        private string? direccionTerreno;
        private decimal? extensionManzanaTerreno;
        private decimal? cantidadQuintalesOro;
        private double? latitud;
        private double? longitud;
        private string? coordenadasTexto;
        private int? cantidadPlantasTerreno;

        private DateOnly? fechaIngresoTerreno;
        private DateTime fechaIngresoDate = DateTime.Today;

        private string? latitudParam;
        private string? longitudParam;

        private FormMode.FormModeSelect mode = new();

        private readonly TerrenoApiService terrenoApiService = new();
        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();
        private readonly FotoTerrenoApiService fotoTerrenoApiService = new();

        private bool inicializado = false;
        private bool actualizandoSeleccionInterna;

        private readonly SemaphoreSlim inicializacionLock = new(1, 1);
        private CancellationTokenSource? inicializacionCts;
        private CancellationTokenSource? departamentoCts;
        private CancellationTokenSource? municipioCts;
        private CancellationTokenSource? fotosCts;
        private CancellationTokenSource? guardadoCts;

        private int? fotosCargadasTerrenoId = null;

        public Action<double?, double?>? RefrescarMapaAction { get; set; }

        // ==================== Comandos ====================

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command ObtenerGpsCommand { get; }
        public Command SeleccionarMapaCommand { get; }

        public Command SeleccionarFotosCommand { get; }
        public Command<FotoTerrenoItem> QuitarFotoCommand { get; }
        public Command<FotoTerrenoItem> AbrirGaleriaFotosCommand { get; }

        public TerrenoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
            ObtenerGpsCommand = new Command(async () => await ObtenerGpsAsync(), () => !IsReadOnly);
            SeleccionarMapaCommand = new Command(async () => await SeleccionarMapaAsync(), () => !IsReadOnly);

            SeleccionarFotosCommand = new Command(async () => await SeleccionarFotosAsync(), () => !IsReadOnly);
            QuitarFotoCommand = new Command<FotoTerrenoItem>(async foto => await QuitarFotoAsync(foto), foto => !IsReadOnly);
            AbrirGaleriaFotosCommand = new Command<FotoTerrenoItem>(async foto => await AbrirGaleriaFotosAsync(foto));

            FotosTerreno.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TieneFotosTerreno));
                OnPropertyChanged(nameof(NoTieneFotosTerreno));
            };
        }

        // ==================== Query properties ====================

        public string? LatitudParam
        {
            get => latitudParam;
            set
            {
                latitudParam = value;

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                {
                    Latitud = lat;
                    RefrescarMapaAction?.Invoke(Latitud, Longitud);
                }
            }
        }

        public string? LongitudParam
        {
            get => longitudParam;
            set
            {
                longitudParam = value;

                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    Longitud = lon;
                    RefrescarMapaAction?.Invoke(Latitud, Longitud);
                }
            }
        }

        public string? CoordenadasTexto
        {
            get => coordenadasTexto;
            set
            {
                coordenadasTexto = value;
                OnPropertyChanged();
                ProcesarCoordenadas(value);
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
                {
                    LimpiarFotosTerreno();
                }

                if (Mode == FormMode.FormModeSelect.Create && terrenoNuevoId <= 0)
                {
                    LimpiarFotosSiSonDeTerrenoAnterior();
                }

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
                    CantidadPlantasTerreno = value.CantidadPlantasTerreno;

                    if (LatitudParam == null && LongitudParam == null)
                    {
                        Latitud = value.Latitud;
                        Longitud = value.Longitud;
                    }
                }

                OnPropertyChanged();

                if (Mode == FormMode.FormModeSelect.Edit && inicializado)
                {
                    _ = ReasignarSeleccionPickersAsync();
                }
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;

                if (mode == FormMode.FormModeSelect.Create)
                {
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

                SaveCommand.ChangeCanExecute();
                ObtenerGpsCommand.ChangeCanExecute();
                SeleccionarMapaCommand.ChangeCanExecute();
                SeleccionarFotosCommand.ChangeCanExecute();
                QuitarFotoCommand.ChangeCanExecute();
                AbrirGaleriaFotosCommand.ChangeCanExecute();
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

        public string? CodigoTerreno
        {
            get => codigoTerreno;
            set
            {
                codigoTerreno = value;
                OnPropertyChanged();
            }
        }

        public string? IdentificacionPropietarioTerreno
        {
            get => identificacionPropietarioTerreno;
            set
            {
                identificacionPropietarioTerreno = value;
                OnPropertyChanged();
            }
        }

        public string? NombrePropietarioTerreno
        {
            get => nombrePropietarioTerreno;
            set
            {
                nombrePropietarioTerreno = value;
                OnPropertyChanged();
            }
        }

        public string? TelefonoPropietarioTexto
        {
            get => telefonoPropietarioTexto;
            set
            {
                telefonoPropietarioTexto = value;
                OnPropertyChanged();
            }
        }

        public string? CorreoPropietario
        {
            get => correoPropietario;
            set
            {
                correoPropietario = value;
                OnPropertyChanged();
            }
        }

        public string? DireccionTerreno
        {
            get => direccionTerreno;
            set
            {
                direccionTerreno = value;
                OnPropertyChanged();
            }
        }

        public decimal? ExtensionManzanaTerreno
        {
            get => extensionManzanaTerreno;
            set
            {
                extensionManzanaTerreno = value;
                OnPropertyChanged();
            }
        }

        public decimal? CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                cantidadQuintalesOro = value;
                OnPropertyChanged();
            }
        }

        public int? CantidadPlantasTerreno
        {
            get => cantidadPlantasTerreno;
            set
            {
                cantidadPlantasTerreno = value;
                OnPropertyChanged();
            }
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

        // ==================== Fotos ====================

        public ObservableCollection<FotoTerrenoItem> FotosTerreno { get; } = new();

        public bool TieneFotosTerreno => FotosTerreno.Count > 0;

        public bool NoTieneFotosTerreno => FotosTerreno.Count == 0;

        private void LimpiarFotosSiSonDeTerrenoAnterior()
        {
            bool hayFotosCargadasDesdeApi = fotosCargadasTerrenoId != null;
            bool hayFotosExistentes = FotosTerreno.Any(f => !f.EsNueva);

            if (hayFotosCargadasDesdeApi || hayFotosExistentes)
            {
                LimpiarFotosTerreno();
            }
        }

        private void LimpiarFotosTerreno()
        {
            FotosTerreno.Clear();
            fotosCargadasTerrenoId = null;

            OnPropertyChanged(nameof(TieneFotosTerreno));
            OnPropertyChanged(nameof(NoTieneFotosTerreno));
        }

        // ==================== Pickers ====================

        public ObservableCollection<PaisResponse> Paises { get; } = new();
        public ObservableCollection<DepartamentoResponse> Departamentos { get; } = new();
        public ObservableCollection<MunicipioResponse> Municipios { get; } = new();

        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        public PaisResponse? PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                if (paisSeleccionado == value)
                    return;

                paisSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPickDepartamento));
                OnPropertyChanged(nameof(CanPickMunicipio));

                if (!actualizandoSeleccionInterna)
                    _ = CambiarPaisAsync(value);
            }
        }

        public DepartamentoResponse? DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                if (departamentoSeleccionado == value)
                    return;

                departamentoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPickMunicipio));

                if (!actualizandoSeleccionInterna)
                    _ = CambiarDepartamentoAsync(value);
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                if (municipioSeleccionado == value)
                    return;

                municipioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public bool CanPickDepartamento =>
            IsEnabled && PaisSeleccionado != null && !IsBusy;

        public bool CanPickMunicipio =>
            IsEnabled && DepartamentoSeleccionado != null && !IsBusy;

        // ==================== Inicialización ====================

        public async Task InicializarAsync()
        {
            await inicializacionLock.WaitAsync();

            CancelarYRenovar(ref inicializacionCts);
            CancellationToken cancellationToken = inicializacionCts.Token;

            try
            {
                IsBusy = true;
                NotificarDisponibilidadPickers();

                if (Mode == FormMode.FormModeSelect.Create)
                    LimpiarFotosSiSonDeTerrenoAnterior();

                if (!inicializado)
                {
                    bool paisesCargados =
                        await CargarPaisesAsync(cancellationToken);

                    if (!paisesCargados)
                        return;

                    inicializado = true;

                    if (Terreno?.MunicipioId > 0)
                    {
                        await ResolverSeleccionPorMunicipioIdAsync(
                            Terreno.MunicipioId,
                            cancellationToken);
                    }
                }

                if (Terreno?.TerrenoId > 0 &&
                    fotosCargadasTerrenoId != Terreno.TerrenoId)
                {
                    await CargarFotosTerrenoAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // La pantalla cambió o comenzó una solicitud más reciente.
            }
            finally
            {
                IsBusy = false;
                NotificarDisponibilidadPickers();
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
                // Se seleccionó otro registro o se abandonó la pantalla.
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
                OnPropertyChanged(nameof(Departamentos));
                OnPropertyChanged(nameof(Municipios));
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
                OnPropertyChanged(nameof(Municipios));
                NotificarDisponibilidadPickers();
            }
            finally
            {
                actualizandoSeleccionInterna = false;
            }

            if (departamento?.DepartamentoId > 0)
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
            var result = await paisApiService.GetPaisResultAsync(
                cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return false;
            }

            Paises.Clear();

            foreach (var pais in result.Data)
                Paises.Add(pais);

            OnPropertyChanged(nameof(Paises));
            NotificarDisponibilidadPickers();

            return true;
        }

        private async Task<bool> CargarDepartamentosAsync(
            int? paisId,
            CancellationToken cancellationToken,
            bool mostrarError)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
                return false;

            var result =
                await departamentoApiService.GetDepartamentosResultAsync(
                    paisId,
                    cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (mostrarError && !cancellationToken.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return false;
            }

            if (PaisSeleccionado?.PaisId != paisId.Value &&
                !actualizandoSeleccionInterna)
            {
                return false;
            }

            Departamentos.Clear();

            foreach (var departamento in result.Data)
                Departamentos.Add(departamento);

            OnPropertyChanged(nameof(Departamentos));
            NotificarDisponibilidadPickers();

            return true;
        }

        private async Task<bool> CargarMunicipiosAsync(
            int? departamentoId,
            CancellationToken cancellationToken,
            bool mostrarError)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
                return false;

            var result =
                await municipioApiService.GetMunicipiosResultAsync(
                    departamentoId,
                    cancellationToken);

            if (!result.Success || result.Data == null)
            {
                if (mostrarError && !cancellationToken.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return false;
            }

            if (DepartamentoSeleccionado?.DepartamentoId != departamentoId.Value &&
                !actualizandoSeleccionInterna)
            {
                return false;
            }

            Municipios.Clear();

            foreach (var municipio in result.Data)
                Municipios.Add(municipio);

            OnPropertyChanged(nameof(Municipios));
            NotificarDisponibilidadPickers();

            return true;
        }

        private async Task ResolverSeleccionPorMunicipioIdAsync(
            int? municipioId,
            CancellationToken cancellationToken)
        {
            if (!municipioId.HasValue || municipioId.Value <= 0)
                return;

            PaisResponse? paisEncontrado = null;
            DepartamentoResponse? departamentoEncontrado = null;
            MunicipioResponse? municipioEncontrado = null;
            List<DepartamentoResponse>? departamentosEncontrados = null;
            List<MunicipioResponse>? municipiosEncontrados = null;

            foreach (var pais in Paises.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var departamentosResult =
                    await departamentoApiService.GetDepartamentosResultAsync(
                        pais.PaisId,
                        cancellationToken);

                if (!departamentosResult.Success ||
                    departamentosResult.Data == null)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        await MostrarToastAsync(departamentosResult.Message);

                    return;
                }

                foreach (var departamento in departamentosResult.Data)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var municipiosResult =
                        await municipioApiService.GetMunicipiosResultAsync(
                            departamento.DepartamentoId,
                            cancellationToken);

                    if (!municipiosResult.Success ||
                        municipiosResult.Data == null)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            await MostrarToastAsync(municipiosResult.Message);

                        return;
                    }

                    var municipio = municipiosResult.Data.FirstOrDefault(
                        item => item.MunicipioId == municipioId.Value);

                    if (municipio == null)
                        continue;

                    paisEncontrado = pais;
                    departamentoEncontrado = departamento;
                    municipioEncontrado = municipio;
                    departamentosEncontrados =
                        departamentosResult.Data.ToList();
                    municipiosEncontrados = municipiosResult.Data.ToList();
                    break;
                }

                if (municipioEncontrado != null)
                    break;
            }

            if (municipioEncontrado == null ||
                paisEncontrado == null ||
                departamentoEncontrado == null ||
                departamentosEncontrados == null ||
                municipiosEncontrados == null)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await MostrarToastAsync(
                        "No fue posible determinar la ubicación administrativa del terreno.");
                }

                return;
            }

            try
            {
                actualizandoSeleccionInterna = true;

                Departamentos.Clear();
                foreach (var departamento in departamentosEncontrados)
                    Departamentos.Add(departamento);

                Municipios.Clear();
                foreach (var municipio in municipiosEncontrados)
                    Municipios.Add(municipio);

                paisSeleccionado = paisEncontrado;
                departamentoSeleccionado = departamentoEncontrado;
                municipioSeleccionado = municipioEncontrado;

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

        private void NotificarDisponibilidadPickers()
        {
            OnPropertyChanged(nameof(CanPickDepartamento));
            OnPropertyChanged(nameof(CanPickMunicipio));
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
            catch
            {
            }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        // ==================== Fotos API / Local ====================

        private async Task AbrirGaleriaFotosAsync(FotoTerrenoItem foto)
        {
            try
            {
                if (foto == null)
                    return;

                if (FotosTerreno == null || FotosTerreno.Count == 0)
                    return;

                await Shell.Current.GoToAsync(AppRoutes.FotosTerrenoGaleria, true, new Dictionary<string, object>
                {
                    { "Fotos", FotosTerreno.ToList() },
                    { "FotoInicial", foto }
                });
            }
            catch
            {
                await MostrarToastAsync("No fue posible abrir la galería de fotografías.");
            }
        }

        private async Task CargarFotosTerrenoAsync(
            CancellationToken cancellationToken = default)
        {
            if (Terreno?.TerrenoId is null or <= 0)
                return;

            int terrenoIdActual = Terreno.TerrenoId.Value;

            CancelarYRenovar(ref fotosCts);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                fotosCts.Token);

            var result = await fotoTerrenoApiService.GetFotosPorTerrenoResultAsync(
                terrenoIdActual,
                linkedCts.Token);

            if (!result.Success || result.Data == null)
            {
                if (!linkedCts.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return;
            }

            var fotosPreparadas = new List<FotoTerrenoItem>();

            foreach (var foto in result.Data)
            {
                string urlCompleta =
                    fotoTerrenoApiService.ConstruirUrlCompleta(
                        foto.UrlFotoTerreno);

                if (string.IsNullOrWhiteSpace(urlCompleta) ||
                    !Uri.TryCreate(urlCompleta, UriKind.Absolute, out var uri))
                {
                    continue;
                }

                fotosPreparadas.Add(new FotoTerrenoItem
                {
                    FotoTerrenoId = foto.FotoTerrenoId,
                    TerrenoId = foto.TerrenoId,
                    UrlFotoTerreno = urlCompleta,
                    LocalPath = null,
                    NombreArchivo = Path.GetFileName(uri.LocalPath),
                    EsNueva = false,
                    Imagen = ImageSource.FromUri(uri)
                });
            }

            if (Terreno?.TerrenoId != terrenoIdActual)
                return;

            LimpiarFotosTerreno();

            foreach (var foto in fotosPreparadas)
                FotosTerreno.Add(foto);

            fotosCargadasTerrenoId = terrenoIdActual;

            OnPropertyChanged(nameof(TieneFotosTerreno));
            OnPropertyChanged(nameof(NoTieneFotosTerreno));
        }

        private async Task SeleccionarFotosAsync()
        {
            try
            {
                if (!AllowEdit)
                    return;

                var opciones = new PickOptions
                {
                    PickerTitle = "Seleccione fotos del terreno",
                    FileTypes = FilePickerFileType.Images
                };

                var archivos = await FilePicker.PickMultipleAsync(opciones);

                if (archivos == null)
                    return;

                foreach (var archivo in archivos)
                {
                    string extension = Path.GetExtension(archivo.FileName);

                    if (string.IsNullOrWhiteSpace(extension))
                        extension = ".jpg";

                    string nombreTemporal = $"{Guid.NewGuid()}{extension}";
                    string rutaTemporal = Path.Combine(FileSystem.CacheDirectory, nombreTemporal);

                    await using var origen = await archivo.OpenReadAsync();
                    await using var destino = File.Create(rutaTemporal);

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
                await MostrarToastAsync("No fue posible seleccionar las fotografías.");
            }
        }

        private async Task QuitarFotoAsync(FotoTerrenoItem foto)
        {
            if (foto == null || !AllowEdit || IsBusy)
                return;

            if (foto.EsNueva ||
                foto.FotoTerrenoId is null or <= 0)
            {
                FotosTerreno.Remove(foto);
                return;
            }

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar foto",
                "¿Desea eliminar esta foto del terreno?",
                "Aceptar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                NotificarDisponibilidadPickers();

                var result = await fotoTerrenoApiService.EliminarFotoResultAsync(
                    foto.FotoTerrenoId.Value);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                FotosTerreno.Remove(foto);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Foto eliminada correctamente."
                        : result.Message);
            }
            finally
            {
                IsBusy = false;
                NotificarDisponibilidadPickers();
            }
        }

        private async Task<ApiResult<bool>> SubirFotosPendientesAsync(
            int terrenoId,
            CancellationToken cancellationToken)
        {
            var fotosNuevas = FotosTerreno
                .Where(f => f.EsNueva &&
                            !string.IsNullOrWhiteSpace(f.LocalPath))
                .ToList();

            if (!fotosNuevas.Any())
            {
                return ApiResult<bool>.Ok(
                    true,
                    "No hay fotografías pendientes de subir.");
            }

            var result = await fotoTerrenoApiService.SubirFotosResultAsync(
                terrenoId,
                fotosNuevas,
                cancellationToken);

            if (!result.Success)
                return result;

            foreach (var foto in fotosNuevas)
            {
                foto.EsNueva = false;
                foto.TerrenoId = terrenoId;
            }

            fotosCargadasTerrenoId = terrenoId;

            return result;
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
            catch (FeatureNotEnabledException)
            {
                await MostrarToastAsync(
                    "Active la ubicación del dispositivo e intente nuevamente.");
            }
            catch (PermissionException)
            {
                await MostrarToastAsync(
                    "No fue posible acceder a la ubicación del dispositivo.");
            }
            catch
            {
                await MostrarToastAsync(
                    "No fue posible obtener la ubicación actual.");
            }
        }

        private async Task SeleccionarMapaAsync()
        {
            if (!AllowEdit)
                return;

            Terreno ??= new TerrenoRequest();

            Terreno.CodigoTerreno = CodigoTerreno;
            Terreno.IdentificacionPropietarioTerreno = IdentificacionPropietarioTerreno;
            Terreno.NombrePropietarioTerreno = NombrePropietarioTerreno;
            Terreno.TelefonoPropietario = ParseTelefono(TelefonoPropietarioTexto);
            Terreno.CorreoPropietario = CorreoPropietario;
            Terreno.DireccionTerreno = DireccionTerreno;
            Terreno.ExtensionManzanaTerreno = ExtensionManzanaTerreno;
            Terreno.CantidadQuintalesOro = CantidadQuintalesOro;
            Terreno.CantidadPlantasTerreno = CantidadPlantasTerreno;
            Terreno.FechaIngresoTerreno = FechaIngresoTerreno;
            Terreno.MunicipioId = MunicipioSeleccionado?.MunicipioId ?? 0;
            Terreno.Latitud = Latitud;
            Terreno.Longitud = Longitud;

            await Shell.Current.GoToAsync(AppRoutes.MapaSeleccion, true, new Dictionary<string, object>
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
            if (IsBusy || IsReadOnly)
                return;

            CancelarYRenovar(ref guardadoCts);
            CancellationToken cancellationToken = guardadoCts.Token;

            try
            {
                IsBusy = true;
                NotificarDisponibilidadPickers();

                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateTerrenoAsync(cancellationToken);
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateTerrenoAsync(cancellationToken);
            }
            finally
            {
                IsBusy = false;
                NotificarDisponibilidadPickers();
            }
        }

        private async Task CreateTerrenoAsync(
            CancellationToken cancellationToken)
        {
            if (!ValidateFieldsData())
                return;

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Confirmar",
                "¿Desea guardar los datos del terreno?",
                "Aceptar",
                "Cancelar");

            if (!confirmar)
                return;

            var request = CrearRequestFormulario();

            var result =
                await terrenoApiService.CreateTerrenoRetornandoResultAsync(
                    request,
                    cancellationToken);

            if (!result.Success ||
                result.Data?.TerrenoId is null or <= 0)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return;
            }

            int terrenoId = result.Data.TerrenoId.Value;

            var fotosResult = await SubirFotosPendientesAsync(
                terrenoId,
                cancellationToken);

            await GoToTerrenoPage();

            if (fotosResult.Success)
            {
                await MostrarToastAsync(
                    "Terreno guardado correctamente.");
            }
            else
            {
                await MostrarToastAsync(
                    "Terreno guardado correctamente, pero no se pudieron subir todas las fotografías. Puede intentarlo nuevamente al editar el terreno.");
            }
        }

        private async Task UpdateTerrenoAsync(
            CancellationToken cancellationToken)
        {
            if (!ValidateFieldsData())
                return;

            if (Terreno?.TerrenoId is null or <= 0)
            {
                await MostrarToastAsync(
                    "No se encontró el terreno que se desea actualizar.");

                return;
            }

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Confirmar",
                "¿Desea actualizar el terreno?",
                "Aceptar",
                "Cancelar");

            if (!confirmar)
                return;

            var request = CrearRequestFormulario();
            request.TerrenoId = Terreno.TerrenoId;

            var result = await terrenoApiService.UpdateTerrenoResultAsync(
                request,
                cancellationToken);

            if (!result.Success || result.Data != true)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await MostrarToastAsync(result.Message);

                return;
            }

            var fotosResult = await SubirFotosPendientesAsync(
                Terreno.TerrenoId.Value,
                cancellationToken);

            await GoToTerrenoPage();

            if (fotosResult.Success)
            {
                await MostrarToastAsync(
                    "Terreno actualizado correctamente.");
            }
            else
            {
                await MostrarToastAsync(
                    "Terreno actualizado correctamente, pero no se pudieron subir todas las fotografías. Puede intentarlo nuevamente al editar el terreno.");
            }
        }

        private TerrenoRequest CrearRequestFormulario()
        {
            return new TerrenoRequest
            {
                CodigoTerreno = CodigoTerreno?.Trim(),
                IdentificacionPropietarioTerreno =
                    IdentificacionPropietarioTerreno?.Trim(),
                NombrePropietarioTerreno =
                    NombrePropietarioTerreno?.Trim(),
                TelefonoPropietario =
                    ParseTelefono(TelefonoPropietarioTexto),
                CorreoPropietario = CorreoPropietario?.Trim(),
                DireccionTerreno = DireccionTerreno?.Trim(),
                ExtensionManzanaTerreno = ExtensionManzanaTerreno,
                CantidadQuintalesOro = CantidadQuintalesOro,
                CantidadPlantasTerreno = CantidadPlantasTerreno,
                FechaIngresoTerreno = FechaIngresoTerreno,
                MunicipioId =
                    MunicipioSeleccionado?.MunicipioId ??
                    Terreno?.MunicipioId ??
                    0,
                Latitud = Latitud,
                Longitud = Longitud
            };
        }

        private Task GoToTerrenoPage()
        {
            return GoToAsyncParameters(AppRoutes.Terrenos);
        }

        // ==================== Google Maps ====================

        public async Task AbrirEnGoogleMaps(double lat, double lon)
        {
            try
            {
                string url = $"https://www.google.com/maps?q={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";

                await Launcher.OpenAsync(url);
            }
            catch
            {
                await MostrarToastAsync(
                    "No fue posible abrir la ubicación en Google Maps.");
            }
        }

        public void ConvertirDesdeGoogleMaps(string texto)
        {
            try
            {
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
            var grados = dms.Split('°')[0];
            var minutos = dms.Split('°')[1].Split('\'')[0];
            var segundosParte = dms.Split('\'')[1];

            var segundos = new string(
                segundosParte
                    .TakeWhile(c => char.IsDigit(c) || c == '.')
                    .ToArray());

            var direccion = new string(dms.Where(char.IsLetter).ToArray());

            double d = double.Parse(grados, CultureInfo.InvariantCulture);
            double m = double.Parse(minutos, CultureInfo.InvariantCulture);
            double s = double.Parse(segundos, CultureInfo.InvariantCulture);

            double decimalValue = d + (m / 60.0) + (s / 3600.0);

            if (direccion == "S" || direccion == "W")
                decimalValue *= -1;

            return decimalValue;
        }

        private void ProcesarCoordenadas(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            texto = texto.Trim();

            var numeros = Regex.Matches(texto.Replace(",", "."), @"-?\d+(\.\d+)?");

            if (numeros.Count == 2)
            {
                if (double.TryParse(numeros[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(numeros[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    Latitud = lat;
                    Longitud = lon;
                    return;
                }
            }

            if (texto.Contains("°"))
            {
                var partes = texto.Split(new[] { 'N', 'S', 'E', 'W' }, StringSplitOptions.RemoveEmptyEntries);

                if (partes.Length >= 2)
                {
                    var latDms = partes[0].Trim();
                    var lonDms = partes[1].Trim();

                    var latDecimal = ConvertirDMSToDecimal(latDms);
                    var lonDecimal = ConvertirDMSToDecimal(lonDms);

                    if (texto.Contains("S")) latDecimal *= -1;
                    if (texto.Contains("W")) lonDecimal *= -1;

                    Latitud = latDecimal;
                    Longitud = lonDecimal;
                }
            }
        }

        private double ConvertirDMSToDecimal(string dms)
        {
            var regex = new Regex(@"(\d+)°(\d+)'(\d+(?:\.\d+)?)""?");
            var match = regex.Match(dms);

            if (!match.Success)
                return 0;

            double grados = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            double minutos = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double segundos = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            return grados + (minutos / 60) + (segundos / 3600);
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

            if (!string.IsNullOrWhiteSpace(CorreoPropietario) &&
                !EsCorreoValido(CorreoPropietario))
            {
                Display("Correo del propietario inválido.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TelefonoPropietarioTexto) &&
                !TelefonoPropietarioTexto.All(char.IsDigit))
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

        private void Display(string msg)
        {
            _ = MostrarToastAsync("Validación: " + msg);
        }

        private bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            var patron = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

            return Regex.IsMatch(correo, patron);
        }

        private int? ParseTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return null;

            if (int.TryParse(telefono, out var t))
                return t;

            return null;
        }

        private async Task CancelAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert(
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
            catch
            {
                await MostrarToastAsync(
                    "No fue posible cancelar el formulario.");
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool ValidateFieldsAsync()
        {
            if (Terreno == null)
            {
                if (!string.IsNullOrWhiteSpace(CodigoTerreno)) return true;
                if (!string.IsNullOrWhiteSpace(IdentificacionPropietarioTerreno)) return true;
                if (!string.IsNullOrWhiteSpace(NombrePropietarioTerreno)) return true;
                if (!string.IsNullOrWhiteSpace(TelefonoPropietarioTexto)) return true;
                if (!string.IsNullOrWhiteSpace(CorreoPropietario)) return true;
                if (!string.IsNullOrWhiteSpace(DireccionTerreno)) return true;
                if (ExtensionManzanaTerreno != null) return true;
                if (CantidadQuintalesOro != null) return true;
                if (CantidadPlantasTerreno != null) return true;
                if (Latitud != null) return true;
                if (Longitud != null) return true;
                if (FotosTerreno.Any(f => f.EsNueva)) return true;

                return false;
            }

            if (CodigoTerreno != Terreno.CodigoTerreno) return true;
            if (IdentificacionPropietarioTerreno != Terreno.IdentificacionPropietarioTerreno) return true;
            if (NombrePropietarioTerreno != Terreno.NombrePropietarioTerreno) return true;
            if (TelefonoPropietarioTexto != Terreno.TelefonoPropietario?.ToString()) return true;
            if (CorreoPropietario != Terreno.CorreoPropietario) return true;
            if (DireccionTerreno != Terreno.DireccionTerreno) return true;
            if (ExtensionManzanaTerreno != Terreno.ExtensionManzanaTerreno) return true;
            if (CantidadQuintalesOro != Terreno.CantidadQuintalesOro) return true;
            if (CantidadPlantasTerreno != Terreno.CantidadPlantasTerreno) return true;
            if (FechaIngresoTerreno != Terreno.FechaIngresoTerreno) return true;
            if (Latitud != Terreno.Latitud) return true;
            if (Longitud != Terreno.Longitud) return true;
            if (MunicipioSeleccionado?.MunicipioId != Terreno.MunicipioId) return true;
            if (FotosTerreno.Any(f => f.EsNueva)) return true;

            return false;
        }
    }
}