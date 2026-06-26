using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace CONATRADEC.ViewModels
{
    public class NuevoAnalisisFormViewModel : GlobalService
    {
        private readonly ElementoQuimicoApiService elementoQuimicoApiService = new();
        private readonly AnalisisSueloApiService analisisSueloApiService = new();
        private readonly UnidadMedidaApiService unidadMedidaApiService = new();
        private readonly TerrenoBusquedaApiService terrenoBusquedaApiService = new();

        private readonly PaisApiService paisApiService = new();
        private readonly DepartamentoApiService departamentoApiService = new();
        private readonly MunicipioApiService municipioApiService = new();

        private int? usuarioId;
        private string inicialesUsuario = string.Empty;
        private string nombreCompletoUsuario = string.Empty;
        private string correoUsuario = string.Empty;
        private string urlImagenUsuario = string.Empty;

        private string textoBusquedaTerreno = string.Empty;
        private TerrenoResponse? terrenoSeleccionado;

        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        private TipoCultivoResponse? tipoCultivoSeleccionado;
        private string tipoAnalisisSueloSeleccionado = string.Empty;
        private DateTime fechaAnalisisLaboratorio = DateTime.Today;

        private string laboratorio = string.Empty;
        private string identificadorAnalisisSuelo = string.Empty;
        private string cantidadQuintalesOro = string.Empty;
        private string tamanoFinca = string.Empty;
        private string cantidadPlantas = string.Empty;

        private string ph = string.Empty;
        private string acidezTotal = string.Empty;
        private string calcioCice = string.Empty;
        private string magnesioCice = string.Empty;
        private string potasioCice = string.Empty;

        private string estadoInicialFormulario = string.Empty;

        private bool debeLimpiarFormulario = true;
        private bool cargandoUbicacion;

        private string errorTerreno = string.Empty;
        private string errorTipoCultivo = string.Empty;
        private string errorTipoAnalisisSuelo = string.Empty;
        private string errorFechaAnalisisLaboratorio = string.Empty;
        private string errorLaboratorio = string.Empty;
        private string errorIdentificadorAnalisisSuelo = string.Empty;
        private string errorCantidadQuintalesOro = string.Empty;
        private string errorTamanoFinca = string.Empty;
        private string errorCantidadPlantas = string.Empty;

        private string errorPh = string.Empty;
        private string errorAcidezTotal = string.Empty;
        private string errorCalcioCice = string.Empty;
        private string errorMagnesioCice = string.Empty;
        private string errorPotasioCice = string.Empty;
        public NuevoAnalisisFormViewModel()
        {
            ParametrosConstantesAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();
            ElementosQuimicosAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();

            TerrenosFiltrados = new ObservableCollection<TerrenoResponse>();

            Paises = new ObservableCollection<PaisResponse>();
            Departamentos = new ObservableCollection<DepartamentoResponse>();
            Municipios = new ObservableCollection<MunicipioResponse>();

            TiposCultivo = new ObservableCollection<TipoCultivoResponse>();
            TiposAnalisisSuelo = new ObservableCollection<string>();

            UnidadesMedidaCatalogo = new ObservableCollection<UnidadMedidaResponse>();

            BuscarTerrenoCommand = new Command(
                async () => await BuscarTerrenosAsync(),
                () => !IsBusy
            );

            LimpiarFiltrosTerrenoCommand = new Command(
                async () => await LimpiarFiltrosTerrenoAsync(),
                () => !IsBusy
            );

            SeleccionarTerrenoCommand = new Command<TerrenoResponse>(
                terreno => SeleccionarTerreno(terreno)
            );

            QuitarElementoQuimicoCommand = new Command<ResultadoAnalisisItemViewModel>(
                async item => await QuitarElementoQuimicoAsync(item)
            );

            EnviarAnalisisCommand = new Command(
                async () => await EnviarAnalisisAsync(),
                () => PuedeEnviar
            );

            CancelarCommand = new Command(
                async () => await CancelarAsync(),
                () => !IsBusy
            );
        }

        public int? UsuarioId
        {
            get => usuarioId;
            set
            {
                usuarioId = value;
                OnPropertyChanged(nameof(UsuarioId));
            }
        }

        public string InicialesUsuario
        {
            get => inicialesUsuario;
            set
            {
                inicialesUsuario = value;
                OnPropertyChanged(nameof(InicialesUsuario));
            }
        }

        public string NombreCompletoUsuario
        {
            get => nombreCompletoUsuario;
            set
            {
                nombreCompletoUsuario = value;
                OnPropertyChanged(nameof(NombreCompletoUsuario));
            }
        }

        public string CorreoUsuario
        {
            get => correoUsuario;
            set
            {
                correoUsuario = value;
                OnPropertyChanged(nameof(CorreoUsuario));
            }
        }

        public string UrlImagenUsuario
        {
            get => urlImagenUsuario;
            set
            {
                urlImagenUsuario = value;
                OnPropertyChanged(nameof(UrlImagenUsuario));
                OnPropertyChanged(nameof(TieneImagenUsuario));
                OnPropertyChanged(nameof(NoTieneImagenUsuario));
            }
        }

        public bool TieneImagenUsuario => !string.IsNullOrWhiteSpace(UrlImagenUsuario);

        public bool NoTieneImagenUsuario => string.IsNullOrWhiteSpace(UrlImagenUsuario);

        public ObservableCollection<TerrenoResponse> TerrenosFiltrados { get; }

        public ObservableCollection<PaisResponse> Paises { get; }

        public ObservableCollection<DepartamentoResponse> Departamentos { get; }

        public ObservableCollection<MunicipioResponse> Municipios { get; }

        public ObservableCollection<TipoCultivoResponse> TiposCultivo { get; }

        public ObservableCollection<string> TiposAnalisisSuelo { get; }

        public ObservableCollection<UnidadMedidaResponse> UnidadesMedidaCatalogo { get; }

        public string TextoBusquedaTerreno
        {
            get => textoBusquedaTerreno;
            set
            {
                textoBusquedaTerreno = value ?? string.Empty;
                OnPropertyChanged(nameof(TextoBusquedaTerreno));
            }
        }

        public PaisResponse? PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                paisSeleccionado = value;
                OnPropertyChanged(nameof(PaisSeleccionado));

                if (!cargandoUbicacion)
                    _ = AlCambiarPaisAsync();
            }
        }

        public DepartamentoResponse? DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                departamentoSeleccionado = value;
                OnPropertyChanged(nameof(DepartamentoSeleccionado));

                if (!cargandoUbicacion)
                    _ = AlCambiarDepartamentoAsync();
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                municipioSeleccionado = value;
                OnPropertyChanged(nameof(MunicipioSeleccionado));
            }
        }

        public TerrenoResponse? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            set
            {
                terrenoSeleccionado = value;
                OnPropertyChanged(nameof(TerrenoSeleccionado));
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));

                if (terrenoSeleccionado != null)
                {
                    CantidadQuintalesOro = terrenoSeleccionado.CantidadQuintalesOro?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                    TamanoFinca = terrenoSeleccionado.TamanoFinca?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                    CantidadPlantas = terrenoSeleccionado.CantidadPlantasTerreno?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                }

                RefrescarComandos();
            }
        }

        public bool TieneTerrenoSeleccionado => TerrenoSeleccionado != null;

        public TipoCultivoResponse? TipoCultivoSeleccionado
        {
            get => tipoCultivoSeleccionado;
            set
            {
                tipoCultivoSeleccionado = value;
                OnPropertyChanged(nameof(TipoCultivoSeleccionado));
                OnPropertyChanged(nameof(TipoCultivoSeleccionadoTexto));
                RefrescarComandos();
            }
        }

        public string TipoCultivoSeleccionadoTexto => TipoCultivoSeleccionado?.NombreMostrar ?? string.Empty;

        public string TipoAnalisisSueloSeleccionado
        {
            get => tipoAnalisisSueloSeleccionado;
            set
            {
                tipoAnalisisSueloSeleccionado = value;
                OnPropertyChanged(nameof(TipoAnalisisSueloSeleccionado));
                RefrescarComandos();
            }
        }

        public DateTime FechaAnalisisLaboratorio
        {
            get => fechaAnalisisLaboratorio;
            set
            {
                fechaAnalisisLaboratorio = value;
                OnPropertyChanged(nameof(FechaAnalisisLaboratorio));
                RefrescarComandos();
            }
        }

        public string Laboratorio
        {
            get => laboratorio;
            set
            {
                laboratorio = value;
                OnPropertyChanged(nameof(Laboratorio));
                RefrescarComandos();
            }
        }

        public string IdentificadorAnalisisSuelo
        {
            get => identificadorAnalisisSuelo;
            set
            {
                identificadorAnalisisSuelo = value;
                OnPropertyChanged(nameof(IdentificadorAnalisisSuelo));
                RefrescarComandos();
            }
        }

        public string CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                cantidadQuintalesOro = value;
                OnPropertyChanged(nameof(CantidadQuintalesOro));
                RefrescarComandos();
            }
        }

        public string TamanoFinca
        {
            get => tamanoFinca;
            set
            {
                tamanoFinca = value;
                OnPropertyChanged(nameof(TamanoFinca));
                RefrescarComandos();
            }
        }

        public string CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = value;
                OnPropertyChanged(nameof(CantidadPlantas));
                RefrescarComandos();
            }
        }

        public string Ph
        {
            get => ph;
            set
            {
                ph = value ?? string.Empty;
                OnPropertyChanged(nameof(Ph));
                RefrescarComandos();
            }
        }

        public string AcidezTotal
        {
            get => acidezTotal;
            set
            {
                acidezTotal = value ?? string.Empty;
                OnPropertyChanged(nameof(AcidezTotal));
                RefrescarComandos();
            }
        }

        public string CalcioCice
        {
            get => calcioCice;
            set
            {
                calcioCice = value ?? string.Empty;
                OnPropertyChanged(nameof(CalcioCice));
                RefrescarComandos();
            }
        }

        public string MagnesioCice
        {
            get => magnesioCice;
            set
            {
                magnesioCice = value ?? string.Empty;
                OnPropertyChanged(nameof(MagnesioCice));
                RefrescarComandos();
            }
        }

        public string PotasioCice
        {
            get => potasioCice;
            set
            {
                potasioCice = value ?? string.Empty;
                OnPropertyChanged(nameof(PotasioCice));
                RefrescarComandos();
            }
        }

        public string ErrorTerreno
        {
            get => errorTerreno;
            set
            {
                errorTerreno = value;
                OnPropertyChanged(nameof(ErrorTerreno));
                OnPropertyChanged(nameof(TieneErrorTerreno));
            }
        }

        public bool TieneErrorTerreno => !string.IsNullOrWhiteSpace(ErrorTerreno);

        public string ErrorTipoCultivo
        {
            get => errorTipoCultivo;
            set
            {
                errorTipoCultivo = value;
                OnPropertyChanged(nameof(ErrorTipoCultivo));
                OnPropertyChanged(nameof(TieneErrorTipoCultivo));
            }
        }

        public bool TieneErrorTipoCultivo => !string.IsNullOrWhiteSpace(ErrorTipoCultivo);

        public string ErrorTipoAnalisisSuelo
        {
            get => errorTipoAnalisisSuelo;
            set
            {
                errorTipoAnalisisSuelo = value;
                OnPropertyChanged(nameof(ErrorTipoAnalisisSuelo));
                OnPropertyChanged(nameof(TieneErrorTipoAnalisisSuelo));
            }
        }

        public bool TieneErrorTipoAnalisisSuelo => !string.IsNullOrWhiteSpace(ErrorTipoAnalisisSuelo);

        public string ErrorFechaAnalisisLaboratorio
        {
            get => errorFechaAnalisisLaboratorio;
            set
            {
                errorFechaAnalisisLaboratorio = value;
                OnPropertyChanged(nameof(ErrorFechaAnalisisLaboratorio));
                OnPropertyChanged(nameof(TieneErrorFechaAnalisisLaboratorio));
            }
        }

        public bool TieneErrorFechaAnalisisLaboratorio => !string.IsNullOrWhiteSpace(ErrorFechaAnalisisLaboratorio);

        public string ErrorLaboratorio
        {
            get => errorLaboratorio;
            set
            {
                errorLaboratorio = value;
                OnPropertyChanged(nameof(ErrorLaboratorio));
                OnPropertyChanged(nameof(TieneErrorLaboratorio));
            }
        }

        public bool TieneErrorLaboratorio => !string.IsNullOrWhiteSpace(ErrorLaboratorio);

        public string ErrorIdentificadorAnalisisSuelo
        {
            get => errorIdentificadorAnalisisSuelo;
            set
            {
                errorIdentificadorAnalisisSuelo = value;
                OnPropertyChanged(nameof(ErrorIdentificadorAnalisisSuelo));
                OnPropertyChanged(nameof(TieneErrorIdentificadorAnalisisSuelo));
            }
        }

        public bool TieneErrorIdentificadorAnalisisSuelo => !string.IsNullOrWhiteSpace(ErrorIdentificadorAnalisisSuelo);

        public string ErrorCantidadQuintalesOro
        {
            get => errorCantidadQuintalesOro;
            set
            {
                errorCantidadQuintalesOro = value;
                OnPropertyChanged(nameof(ErrorCantidadQuintalesOro));
                OnPropertyChanged(nameof(TieneErrorCantidadQuintalesOro));
            }
        }

        public bool TieneErrorCantidadQuintalesOro => !string.IsNullOrWhiteSpace(ErrorCantidadQuintalesOro);

        public string ErrorTamanoFinca
        {
            get => errorTamanoFinca;
            set
            {
                errorTamanoFinca = value;
                OnPropertyChanged(nameof(ErrorTamanoFinca));
                OnPropertyChanged(nameof(TieneErrorTamanoFinca));
            }
        }

        public bool TieneErrorTamanoFinca => !string.IsNullOrWhiteSpace(ErrorTamanoFinca);

        public string ErrorCantidadPlantas
        {
            get => errorCantidadPlantas;
            set
            {
                errorCantidadPlantas = value;
                OnPropertyChanged(nameof(ErrorCantidadPlantas));
                OnPropertyChanged(nameof(TieneErrorCantidadPlantas));
            }
        }

        public bool TieneErrorCantidadPlantas => !string.IsNullOrWhiteSpace(ErrorCantidadPlantas);

        public string ErrorPh
        {
            get => errorPh;
            set
            {
                errorPh = value;
                OnPropertyChanged(nameof(ErrorPh));
                OnPropertyChanged(nameof(TieneErrorPh));
            }
        }

        public bool TieneErrorPh => !string.IsNullOrWhiteSpace(ErrorPh);

        public string ErrorAcidezTotal
        {
            get => errorAcidezTotal;
            set
            {
                errorAcidezTotal = value;
                OnPropertyChanged(nameof(ErrorAcidezTotal));
                OnPropertyChanged(nameof(TieneErrorAcidezTotal));
            }
        }

        public bool TieneErrorAcidezTotal => !string.IsNullOrWhiteSpace(ErrorAcidezTotal);

        public string ErrorCalcioCice
        {
            get => errorCalcioCice;
            set
            {
                errorCalcioCice = value;
                OnPropertyChanged(nameof(ErrorCalcioCice));
                OnPropertyChanged(nameof(TieneErrorCalcioCice));
            }
        }

        public bool TieneErrorCalcioCice => !string.IsNullOrWhiteSpace(ErrorCalcioCice);

        public string ErrorMagnesioCice
        {
            get => errorMagnesioCice;
            set
            {
                errorMagnesioCice = value;
                OnPropertyChanged(nameof(ErrorMagnesioCice));
                OnPropertyChanged(nameof(TieneErrorMagnesioCice));
            }
        }

        public string ErrorPotasioCice
        {
            get => errorPotasioCice;
            set
            {
                errorPotasioCice = value;
                OnPropertyChanged(nameof(ErrorPotasioCice));
                OnPropertyChanged(nameof(TieneErrorPotasioCice));
            }
        }

        public bool TieneErrorPotasioCice => !string.IsNullOrWhiteSpace(ErrorPotasioCice);

        public bool TieneErrorMagnesioCice => !string.IsNullOrWhiteSpace(ErrorMagnesioCice);

        public ObservableCollection<ResultadoAnalisisItemViewModel> ParametrosConstantesAnalisis { get; }

        public ObservableCollection<ResultadoAnalisisItemViewModel> ElementosQuimicosAnalisis { get; }

        public Command BuscarTerrenoCommand { get; }

        public Command LimpiarFiltrosTerrenoCommand { get; }

        public Command<TerrenoResponse> SeleccionarTerrenoCommand { get; }

        public Command<ResultadoAnalisisItemViewModel> QuitarElementoQuimicoCommand { get; }

        public Command EnviarAnalisisCommand { get; }

        public Command CancelarCommand { get; }

        public bool PuedeEnviar => !IsBusy && CanAdd;

        public async Task InicializarAsync(bool forceReload = false)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                CargarDatosUsuario();

                if (forceReload || TiposCultivo.Count == 0 || TiposAnalisisSuelo.Count == 0)
                    await CargarCatalogosFormularioAsync();

                if (forceReload || Paises.Count == 0)
                    await CargarUbicacionAsync();

                if (forceReload || UnidadesMedidaCatalogo.Count == 0)
                    await CargarUnidadesMedidaAsync();

                if (forceReload || debeLimpiarFormulario)
                {
                    await LimpiarFormularioNuevoCalculoAsync();
                    debeLimpiarFormulario = false;
                }
                else
                {
                    if (ParametrosConstantesAnalisis.Count == 0)
                        CargarParametrosConstantesAnalisis();

                    if (ElementosQuimicosAnalisis.Count == 0)
                        await CargarElementosQuimicosAnalisisAsync();
                }

                estadoInicialFormulario = ObtenerEstadoActualFormulario();
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo cargar el formulario: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task LimpiarFormularioNuevoCalculoAsync()
        {
            LimpiarErroresFormulario();

            terrenoSeleccionado = null;
            OnPropertyChanged(nameof(TerrenoSeleccionado));
            OnPropertyChanged(nameof(TieneTerrenoSeleccionado));

            textoBusquedaTerreno = string.Empty;
            OnPropertyChanged(nameof(TextoBusquedaTerreno));

            TerrenosFiltrados.Clear();

            TipoCultivoSeleccionado = TiposCultivo.FirstOrDefault();
            TipoAnalisisSueloSeleccionado = TiposAnalisisSuelo.FirstOrDefault() ?? string.Empty;

            FechaAnalisisLaboratorio = DateTime.Today;

            Laboratorio = string.Empty;
            IdentificadorAnalisisSuelo = string.Empty;
            CantidadQuintalesOro = string.Empty;
            TamanoFinca = string.Empty;
            CantidadPlantas = string.Empty;

            Ph = string.Empty;
            AcidezTotal = string.Empty;
            CalcioCice = string.Empty;
            MagnesioCice = string.Empty;
            PotasioCice = string.Empty;

            ParametrosConstantesAnalisis.Clear();
            CargarParametrosConstantesAnalisis();

            ElementosQuimicosAnalisis.Clear();
            await CargarElementosQuimicosAnalisisAsync();

            estadoInicialFormulario = ObtenerEstadoActualFormulario();

            RefrescarComandos();
        }

        private void CargarDatosUsuario()
        {
            string usuarioIdTexto = Preferences.Get(SessionKeys.KeyUserId, "0");

            UsuarioId = int.TryParse(usuarioIdTexto, out int idUsuario)
                ? idUsuario
                : 0;

            NombreCompletoUsuario = Preferences.Get(SessionKeys.KeyNombreCompletoUsuario, string.Empty);
            CorreoUsuario = Preferences.Get(SessionKeys.KeyCorreoUsuario, string.Empty);
            UrlImagenUsuario = Preferences.Get(SessionKeys.KeyUrlImagenUsuario, string.Empty);

            InicialesUsuario = ObtenerIniciales(NombreCompletoUsuario);
        }

        private async Task CargarCatalogosFormularioAsync()
        {
            await CargarTiposCultivoAsync();

            TiposAnalisisSuelo.Clear();
            TiposAnalisisSuelo.Add("Análisis químico de suelo");
            TiposAnalisisSuelo.Add("Análisis físico de suelo");
            TiposAnalisisSuelo.Add("Análisis completo de suelo");

            TipoAnalisisSueloSeleccionado = TiposAnalisisSuelo.FirstOrDefault() ?? string.Empty;
            FechaAnalisisLaboratorio = DateTime.Today;
        }

        private async Task CargarTiposCultivoAsync()
        {
            TiposCultivo.Clear();

            ObservableCollection<TipoCultivoResponse> tipos =
                await analisisSueloApiService.ListarTiposCultivoAsync();

            foreach (var tipo in tipos)
            {
                if (tipo == null)
                    continue;

                if (tipo.TipoCultivoId == null || tipo.TipoCultivoId <= 0)
                    continue;

                if (tipo.Activo == false)
                    continue;

                TiposCultivo.Add(tipo);
            }

            TipoCultivoSeleccionado = TiposCultivo.FirstOrDefault();

            if (TiposCultivo.Count == 0)
            {
                await MostrarMensajeAsync(
                    "Tipo de cultivo",
                    "No se encontraron tipos de cultivo activos para seleccionar."
                );
            }
        }

        private async Task CargarUbicacionAsync()
        {
            try
            {
                cargandoUbicacion = true;

                Paises.Clear();
                Departamentos.Clear();
                Municipios.Clear();

                ObservableCollection<PaisResponse> paises =
                    await paisApiService.GetPaisAsync();

                foreach (var pais in paises)
                {
                    if (pais == null)
                        continue;

                    if (pais.PaisId == null || pais.PaisId <= 0)
                        continue;

                    Paises.Add(pais);
                }

                PaisResponse? paisNicaragua = Paises.FirstOrDefault(x =>
                    (x.NombrePais ?? string.Empty).Trim().Equals("Nicaragua", StringComparison.OrdinalIgnoreCase)
                );

                paisSeleccionado = paisNicaragua ?? Paises.FirstOrDefault();
                OnPropertyChanged(nameof(PaisSeleccionado));
            }
            finally
            {
                cargandoUbicacion = false;
            }

            await CargarDepartamentosPorPaisAsync();
        }

        private async Task AlCambiarPaisAsync()
        {
            departamentoSeleccionado = null;
            municipioSeleccionado = null;

            OnPropertyChanged(nameof(DepartamentoSeleccionado));
            OnPropertyChanged(nameof(MunicipioSeleccionado));

            Departamentos.Clear();
            Municipios.Clear();

            await CargarDepartamentosPorPaisAsync();
        }

        private async Task CargarDepartamentosPorPaisAsync()
        {
            Departamentos.Clear();
            Municipios.Clear();

            departamentoSeleccionado = null;
            municipioSeleccionado = null;

            OnPropertyChanged(nameof(DepartamentoSeleccionado));
            OnPropertyChanged(nameof(MunicipioSeleccionado));

            if (PaisSeleccionado?.PaisId == null || PaisSeleccionado.PaisId <= 0)
                return;

            ObservableCollection<DepartamentoResponse> departamentos =
                await departamentoApiService.GetDepartamentosAsync(PaisSeleccionado.PaisId);

            foreach (var departamento in departamentos)
            {
                if (departamento == null)
                    continue;

                if (departamento.DepartamentoId == null || departamento.DepartamentoId <= 0)
                    continue;

                Departamentos.Add(departamento);
            }
        }

        private async Task AlCambiarDepartamentoAsync()
        {
            municipioSeleccionado = null;
            OnPropertyChanged(nameof(MunicipioSeleccionado));

            Municipios.Clear();

            await CargarMunicipiosPorDepartamentoAsync();
        }

        private async Task CargarMunicipiosPorDepartamentoAsync()
        {
            Municipios.Clear();

            municipioSeleccionado = null;
            OnPropertyChanged(nameof(MunicipioSeleccionado));

            if (DepartamentoSeleccionado?.DepartamentoId == null || DepartamentoSeleccionado.DepartamentoId <= 0)
                return;

            ObservableCollection<MunicipioResponse> municipios =
                await municipioApiService.GetMunicipiosAsync(DepartamentoSeleccionado.DepartamentoId);

            foreach (var municipio in municipios)
            {
                if (municipio == null)
                    continue;

                if (municipio.MunicipioId == null || municipio.MunicipioId <= 0)
                    continue;

                Municipios.Add(municipio);
            }
        }

        private async Task BuscarTerrenosAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ErrorTerreno = string.Empty;

                TerrenosFiltrados.Clear();

                ObservableCollection<TerrenoResponse> terrenos =
                    await terrenoBusquedaApiService.BuscarTerrenosAsync(
                        texto: TextoBusquedaTerreno,
                        paisId: PaisSeleccionado?.PaisId,
                        departamentoId: DepartamentoSeleccionado?.DepartamentoId,
                        municipioId: MunicipioSeleccionado?.MunicipioId,
                        page: 1,
                        pageSize: 50
                    );

                foreach (var terreno in terrenos)
                {
                    if (terreno == null)
                        continue;

                    if (terreno.TerrenoId == null || terreno.TerrenoId <= 0)
                        continue;

                    if (terreno.Activo == false)
                        continue;

                    TerrenosFiltrados.Add(terreno);
                }

                if (TerrenosFiltrados.Count == 0)
                    ErrorTerreno = "No se encontraron terrenos con los filtros ingresados.";
            }
            catch (Exception ex)
            {
                ErrorTerreno = $"No se pudo buscar terrenos: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CargarUnidadesMedidaAsync()
        {
            UnidadesMedidaCatalogo.Clear();

            ObservableCollection<UnidadMedidaResponse> unidades =
                await unidadMedidaApiService.GetUnidadMedidaAsync();

            foreach (var unidad in unidades)
            {
                if (unidad == null)
                    continue;

                if (unidad.UnidadMedidaId == null || unidad.UnidadMedidaId <= 0)
                    continue;

                if (unidad.Activo == false)
                    continue;

                UnidadesMedidaCatalogo.Add(unidad);
            }

            if (UnidadesMedidaCatalogo.Count == 0)
            {
                await MostrarMensajeAsync(
                    "Unidades de medida",
                    "No se encontraron unidades de medida activas para cargar en el formulario."
                );
            }
        }

        private void CargarParametrosConstantesAnalisis()
        {
            ParametrosConstantesAnalisis.Clear();

            ObservableCollection<UnidadMedidaResponse> unidadesMateriaOrganica = ClonarUnidadesMedida();

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "MATERIA_ORGANICA",
                NombreParametro = "Materia Orgánica",
                PlaceholderValor = "Ejemplo: 3.2",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = unidadesMateriaOrganica,
                UnidadSeleccionada = BuscarUnidadMedidaEnLista(unidadesMateriaOrganica, "PPM", "%", "PORCENTAJE")
            });
        }

        private async Task CargarElementosQuimicosAnalisisAsync()
        {
            ElementosQuimicosAnalisis.Clear();

            ObservableCollection<ElementoQuimicoResponse> elementos =
                await elementoQuimicoApiService.GetElementoQuimicoAsync();

            if (elementos == null || elementos.Count == 0)
            {
                await MostrarMensajeAsync(
                    "Elementos químicos",
                    "No se encontraron elementos químicos activos para cargar en el análisis."
                );

                return;
            }

            foreach (var elemento in elementos)
            {
                if (elemento == null)
                    continue;

                int? elementoQuimicoId = elemento.ElementoQuimicosId;
                string simbolo = (elemento.SimboloElementoQuimico ?? string.Empty).Trim();
                string nombre = (elemento.NombreElementoQuimico ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(simbolo) && string.IsNullOrWhiteSpace(nombre))
                    continue;

                ObservableCollection<UnidadMedidaResponse> unidadesElemento = ClonarUnidadesMedida();

                ElementosQuimicosAnalisis.Add(new ResultadoAnalisisItemViewModel
                {
                    ElementoQuimicoId = elementoQuimicoId,
                    CodigoParametro = simbolo,
                    NombreParametro = string.IsNullOrWhiteSpace(simbolo)
                        ? nombre
                        : $"{nombre} ({simbolo})",
                    PlaceholderValor = "Valor reportado",
                    EsConstante = false,
                    EsElementoQuimico = true,
                    PuedeEliminar = true,
                    UnidadesMedida = unidadesElemento,
                    UnidadSeleccionada = ObtenerUnidadPredeterminadaElementoQuimico(unidadesElemento, simbolo)
                });
            }
        }

        private ObservableCollection<UnidadMedidaResponse> ClonarUnidadesMedida()
        {
            return new ObservableCollection<UnidadMedidaResponse>(UnidadesMedidaCatalogo);
        }

        private UnidadMedidaResponse? ObtenerUnidadPredeterminadaElementoQuimico(
            ObservableCollection<UnidadMedidaResponse> unidades,
            string? simbolo)
        {
            string simboloNormalizado = (simbolo ?? string.Empty).Trim().ToUpper();

            if (simboloNormalizado == "N")
                return BuscarUnidadMedidaEnLista(unidades, "%", "PORCENTAJE", "PPM", "MG/KG");

            if (simboloNormalizado == "K" ||
                simboloNormalizado == "CA" ||
                simboloNormalizado == "MG")
                return BuscarUnidadMedidaEnLista(unidades, "CMOL/KG", "MEQ/100G", "PPM", "MG/KG");

            return BuscarUnidadMedidaEnLista(unidades, "MG/KG", "PPM", "G/KG", "%");
        }

        private UnidadMedidaResponse? BuscarUnidadMedidaEnLista(
            IEnumerable<UnidadMedidaResponse> unidades,
            params string[] posiblesValores)
        {
            foreach (string valor in posiblesValores)
            {
                string valorNormalizado = NormalizarTextoUnidad(valor);

                UnidadMedidaResponse? unidad = unidades.FirstOrDefault(x =>
                    NormalizarTextoUnidad(x.TextoBusqueda).Contains(valorNormalizado)
                );

                if (unidad != null)
                    return unidad;
            }

            return unidades.FirstOrDefault();
        }

        private static string NormalizarTextoUnidad(string? texto)
        {
            return (texto ?? string.Empty)
                .Trim()
                .ToUpper()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
        }

        private void SeleccionarTerreno(TerrenoResponse? terreno)
        {
            if (terreno == null)
                return;

            TerrenoSeleccionado = terreno;

            textoBusquedaTerreno = $"{terreno.CodigoTerreno} - {terreno.NombreTerreno}";
            OnPropertyChanged(nameof(TextoBusquedaTerreno));

            TerrenosFiltrados.Clear();
            TerrenosFiltrados.Add(terreno);
        }

        private async Task LimpiarFiltrosTerrenoAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ErrorTerreno = string.Empty;

                textoBusquedaTerreno = string.Empty;
                OnPropertyChanged(nameof(TextoBusquedaTerreno));

                terrenoSeleccionado = null;
                OnPropertyChanged(nameof(TerrenoSeleccionado));
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));

                CantidadQuintalesOro = string.Empty;
                TamanoFinca = string.Empty;
                CantidadPlantas = string.Empty;

                TerrenosFiltrados.Clear();

                cargandoUbicacion = true;

                municipioSeleccionado = null;
                departamentoSeleccionado = null;

                OnPropertyChanged(nameof(MunicipioSeleccionado));
                OnPropertyChanged(nameof(DepartamentoSeleccionado));

                Municipios.Clear();
                Departamentos.Clear();

                if (Paises.Count == 0)
                {
                    ObservableCollection<PaisResponse> paises =
                        await paisApiService.GetPaisAsync();

                    foreach (var pais in paises)
                    {
                        if (pais == null)
                            continue;

                        if (pais.PaisId == null || pais.PaisId <= 0)
                            continue;

                        Paises.Add(pais);
                    }
                }

                PaisResponse? paisNicaragua = Paises.FirstOrDefault(x =>
                    (x.NombrePais ?? string.Empty).Trim().Equals("Nicaragua", StringComparison.OrdinalIgnoreCase)
                );

                paisSeleccionado = paisNicaragua ?? Paises.FirstOrDefault();
                OnPropertyChanged(nameof(PaisSeleccionado));
            }
            finally
            {
                cargandoUbicacion = false;
                IsBusy = false;
                RefrescarComandos();
            }

            await CargarDepartamentosPorPaisAsync();
        }

        private async Task QuitarElementoQuimicoAsync(ResultadoAnalisisItemViewModel? item)
        {
            if (item == null)
                return;

            if (!item.PuedeEliminar)
            {
                await MostrarMensajeAsync("Acción no permitida", "Este parámetro no puede quitarse del análisis.");
                return;
            }

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Quitar elemento",
                $"¿Desea quitar {item.NombreParametro} de este análisis?",
                "Sí, quitar",
                "Cancelar"
            );

            if (!confirmar)
                return;

            ElementosQuimicosAnalisis.Remove(item);
        }

        private async Task EnviarAnalisisAsync()
        {
            if (IsBusy)
                return;

            if (!CanAdd)
            {
                await MostrarMensajeAsync("Acceso denegado", "No tiene permisos para registrar análisis.");
                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                bool formularioValido = await ValidarFormularioAsync();

                if (!formularioValido)
                    return;

                decimal quintalesOro = ConvertirDecimal(CantidadQuintalesOro);
                decimal tamanoFincaDecimal = ConvertirDecimal(TamanoFinca);
                int cantidadPlantasValidada = int.Parse(CantidadPlantas);

                decimal phDecimal = ConvertirDecimal(Ph);
                decimal acidezTotalDecimal = ConvertirDecimal(AcidezTotal);
                decimal calcioCiceDecimal = ConvertirDecimal(CalcioCice);
                decimal magnesioCiceDecimal = ConvertirDecimal(MagnesioCice);
                decimal potasioCiceDecimal = ConvertirDecimal(PotasioCice);

                decimal materiaOrganica = ObtenerValorParametroConstante("MATERIA_ORGANICA");

                var elementosQuimicosRequest = new List<ElementoQuimicoAnalisisRequest>();

                foreach (var item in ElementosQuimicosAnalisis)
                {
                    elementosQuimicosRequest.Add(new ElementoQuimicoAnalisisRequest
                    {
                        ElementoQuimicosId = item.ElementoQuimicoId,
                        UnidadMedidaId = ObtenerUnidadMedidaId(item.UnidadSeleccionada),
                        CantidadElemento = ConvertirDecimal(item.Valor)
                    });
                }

                int tipoCultivoId = ObtenerTipoCultivoIdSeleccionado();
                int tipoAnalisisSueloId = ObtenerTipoAnalisisSueloIdSeleccionado();

                var calcularRequest = new AnalisisSueloCalcularRequest
                {
                    TerrenoId = TerrenoSeleccionado?.TerrenoId,
                    TipoCultivoId = tipoCultivoId,
                    TipoAnalisisSueloId = tipoAnalisisSueloId,
                    UsuarioId = UsuarioId,
                    CantidadQuintalesOro = quintalesOro,
                    TamanoFinca = tamanoFincaDecimal,
                    Ph = phDecimal,
                    MateriaOrganica = materiaOrganica,
                    AcidezTotal = acidezTotalDecimal,
                    CalcioCice = calcioCiceDecimal,
                    MagnesioCice = magnesioCiceDecimal,
                    PotasioCice = potasioCiceDecimal,
                    ElementosQuimicos = elementosQuimicosRequest,
                    FuentesOrganicas = new List<FuenteOrganicaAnalisisRequest>()
                };

                var guardarRequest = new AnalisisSueloGuardarCalculoRequest
                {
                    TerrenoId = TerrenoSeleccionado?.TerrenoId,
                    TipoCultivoId = tipoCultivoId,
                    TipoAnalisisSueloId = tipoAnalisisSueloId,
                    UsuarioId = UsuarioId,
                    CantidadQuintalesOro = quintalesOro,
                    TamanoFinca = tamanoFincaDecimal,
                    Ph = phDecimal,
                    MateriaOrganica = materiaOrganica,
                    AcidezTotal = acidezTotalDecimal,
                    CalcioCice = calcioCiceDecimal,
                    MagnesioCice = magnesioCiceDecimal,
                    PotasioCice = potasioCiceDecimal,
                    ElementosQuimicos = elementosQuimicosRequest,
                    FuentesOrganicas = new List<FuenteOrganicaAnalisisRequest>(),
                    FechaAnalisisSuelo = FechaAnalisisLaboratorio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    LaboratorioAnalasisSuelo = Laboratorio.Trim(),
                    IdentificadorAnalisisSuelo = IdentificadorAnalisisSuelo.Trim()
                };

                AnalisisSueloCalculoResponse? response =
                    await analisisSueloApiService.CalcularAsync(calcularRequest);

                if (response == null)
                {
                    await MostrarMensajeAsync("Error", "La API no devolvió una respuesta válida.");
                    return;
                }

                if (!response.Success || response.Data == null)
                {
                    await MostrarMensajeAsync("Error", response.Message ?? "No se pudo calcular el análisis de suelo.");
                    return;
                }

                estadoInicialFormulario = ObtenerEstadoActualFormulario();

                var parametros = new Dictionary<string, object>
                {
                    { "resultadoCalculo", response.Data },
                    { "requestGuardarAnalisis", guardarRequest },
                    { "cantidadPlantas", cantidadPlantasValidada }
                };

                debeLimpiarFormulario = true;

                await GoToAsyncParameters("//ResultadoAnalisisSueloPage", parametros);
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo enviar el análisis: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task<bool> ValidarFormularioAsync()
        {
            LimpiarErroresFormulario();

            if (UsuarioId == null || UsuarioId <= 0)
            {
                await MostrarMensajeAsync("Sesión", "No se encontró el usuario autenticado.");
                return false;
            }

            if (TerrenoSeleccionado == null)
            {
                ErrorTerreno = "Debe seleccionar un cliente/terreno.";
                await MostrarMensajeAsync("Validación", ErrorTerreno);
                return false;
            }

            if (TipoCultivoSeleccionado == null ||
                TipoCultivoSeleccionado.TipoCultivoId == null ||
                TipoCultivoSeleccionado.TipoCultivoId <= 0)
            {
                ErrorTipoCultivo = "Debe seleccionar el tipo de cultivo.";
                await MostrarMensajeAsync("Validación", ErrorTipoCultivo);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TipoAnalisisSueloSeleccionado))
            {
                ErrorTipoAnalisisSuelo = "Debe seleccionar el tipo de análisis de suelo.";
                await MostrarMensajeAsync("Validación", ErrorTipoAnalisisSuelo);
                return false;
            }

            if (FechaAnalisisLaboratorio.Date > DateTime.Today)
            {
                ErrorFechaAnalisisLaboratorio = "La fecha del análisis no puede ser futura.";
                await MostrarMensajeAsync("Validación", ErrorFechaAnalisisLaboratorio);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Laboratorio))
            {
                ErrorLaboratorio = "Debe ingresar el laboratorio del análisis.";
                await MostrarMensajeAsync("Validación", ErrorLaboratorio);
                return false;
            }

            if (Laboratorio.Trim().Length < 3)
            {
                ErrorLaboratorio = "El laboratorio debe tener al menos 3 caracteres.";
                await MostrarMensajeAsync("Validación", ErrorLaboratorio);
                return false;
            }

            if (Laboratorio.Trim().Length > 150)
            {
                ErrorLaboratorio = "El laboratorio no puede tener más de 150 caracteres.";
                await MostrarMensajeAsync("Validación", ErrorLaboratorio);
                return false;
            }

            if (string.IsNullOrWhiteSpace(IdentificadorAnalisisSuelo))
            {
                ErrorIdentificadorAnalisisSuelo = "Debe ingresar el identificador del análisis de suelo.";
                await MostrarMensajeAsync("Validación", ErrorIdentificadorAnalisisSuelo);
                return false;
            }

            if (IdentificadorAnalisisSuelo.Trim().Length > 50)
            {
                ErrorIdentificadorAnalisisSuelo = "El identificador no puede tener más de 50 caracteres.";
                await MostrarMensajeAsync("Validación", ErrorIdentificadorAnalisisSuelo);
                return false;
            }

            if (!TryParseDecimal(CantidadQuintalesOro, out decimal quintalesOro))
            {
                ErrorCantidadQuintalesOro = "La cantidad de quintales oro debe ser numérica.";
                await MostrarMensajeAsync("Validación", ErrorCantidadQuintalesOro);
                return false;
            }

            if (quintalesOro <= 0)
            {
                ErrorCantidadQuintalesOro = "La cantidad de quintales oro debe ser mayor que cero.";
                await MostrarMensajeAsync("Validación", ErrorCantidadQuintalesOro);
                return false;
            }

            if (!TryParseDecimal(TamanoFinca, out decimal tamanoFincaDecimal))
            {
                ErrorTamanoFinca = "El tamaño de la finca debe ser numérico.";
                await MostrarMensajeAsync("Validación", ErrorTamanoFinca);
                return false;
            }

            if (tamanoFincaDecimal <= 0)
            {
                ErrorTamanoFinca = "El tamaño de la finca debe ser mayor que cero.";
                await MostrarMensajeAsync("Validación", ErrorTamanoFinca);
                return false;
            }

            if (!int.TryParse(CantidadPlantas, out int cantidadPlantasValidada))
            {
                ErrorCantidadPlantas = "La cantidad de plantas debe ser numérica.";
                await MostrarMensajeAsync("Validación", ErrorCantidadPlantas);
                return false;
            }

            if (cantidadPlantasValidada <= 0)
            {
                ErrorCantidadPlantas = "La cantidad de plantas debe ser mayor que cero.";
                await MostrarMensajeAsync("Validación", ErrorCantidadPlantas);
                return false;
            }

            if (!ValidarDecimalOpcional(Ph, out decimal phDecimal))
            {
                ErrorPh = "El pH debe ser numérico.";
                await MostrarMensajeAsync("Validación", ErrorPh);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Ph) && (phDecimal <= 0 || phDecimal > 14))
            {
                ErrorPh = "El pH debe ser mayor que cero y menor o igual a 14.";
                await MostrarMensajeAsync("Validación", ErrorPh);
                return false;
            }

            if (!ValidarDecimalOpcional(AcidezTotal, out decimal acidezTotalDecimal))
            {
                ErrorAcidezTotal = "La acidez total debe ser numérica.";
                await MostrarMensajeAsync("Validación", ErrorAcidezTotal);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(AcidezTotal) && acidezTotalDecimal < 0)
            {
                ErrorAcidezTotal = "La acidez total no puede ser negativa.";
                await MostrarMensajeAsync("Validación", ErrorAcidezTotal);
                return false;
            }

            if (!ValidarDecimalOpcional(CalcioCice, out decimal calcioCiceDecimal))
            {
                ErrorCalcioCice = "El calcio CICE debe ser numérico.";
                await MostrarMensajeAsync("Validación", ErrorCalcioCice);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CalcioCice) && calcioCiceDecimal < 0)
            {
                ErrorCalcioCice = "El calcio CICE no puede ser negativo.";
                await MostrarMensajeAsync("Validación", ErrorCalcioCice);
                return false;
            }

            if (!ValidarDecimalOpcional(MagnesioCice, out decimal magnesioCiceDecimal))
            {
                ErrorMagnesioCice = "El magnesio CICE debe ser numérico.";
                await MostrarMensajeAsync("Validación", ErrorMagnesioCice);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MagnesioCice) && magnesioCiceDecimal < 0)
            {
                ErrorMagnesioCice = "El magnesio CICE no puede ser negativo.";
                await MostrarMensajeAsync("Validación", ErrorMagnesioCice);
                return false;
            }

            if (!ValidarDecimalOpcional(PotasioCice, out decimal potasioCiceDecimal))
            {
                ErrorPotasioCice = "El potasio CICE debe ser numérico.";
                await MostrarMensajeAsync("Validación", ErrorPotasioCice);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PotasioCice) && potasioCiceDecimal < 0)
            {
                ErrorPotasioCice = "El potasio CICE no puede ser negativo.";
                await MostrarMensajeAsync("Validación", ErrorPotasioCice);
                return false;
            }

            foreach (var item in ParametrosConstantesAnalisis)
            {
                if (string.IsNullOrWhiteSpace(item.Valor))
                {
                    await MostrarMensajeAsync("Validación", $"Debe ingresar el valor para {item.NombreParametro}.");
                    return false;
                }

                if (!TryParseDecimal(item.Valor, out decimal valor) || valor < 0)
                {
                    await MostrarMensajeAsync("Validación", $"El valor de {item.NombreParametro} no es válido.");
                    return false;
                }

                if (EsUnidadPorcentaje(item.UnidadSeleccionada) && valor > 100)
                {
                    await MostrarMensajeAsync("Validación", $"El porcentaje de {item.NombreParametro} no puede ser mayor a 100.");
                    return false;
                }

                if (!UnidadMedidaValida(item.UnidadSeleccionada))
                {
                    await MostrarMensajeAsync("Validación", $"Debe seleccionar la unidad de {item.NombreParametro}.");
                    return false;
                }
            }

            if (ElementosQuimicosAnalisis.Count == 0)
            {
                await MostrarMensajeAsync("Validación", "Debe existir al menos un elemento químico para calcular el análisis.");
                return false;
            }

            foreach (var item in ElementosQuimicosAnalisis)
            {
                if (item.ElementoQuimicoId == null || item.ElementoQuimicoId <= 0)
                {
                    await MostrarMensajeAsync("Validación", $"El elemento {item.NombreParametro} no tiene un identificador válido.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(item.Valor))
                {
                    await MostrarMensajeAsync("Validación", $"Debe ingresar el valor para {item.NombreParametro}.");
                    return false;
                }

                if (!TryParseDecimal(item.Valor, out decimal valor) || valor < 0)
                {
                    await MostrarMensajeAsync("Validación", $"El valor de {item.NombreParametro} no es válido.");
                    return false;
                }

                if (EsUnidadPorcentaje(item.UnidadSeleccionada) && valor > 100)
                {
                    await MostrarMensajeAsync("Validación", $"El porcentaje de {item.NombreParametro} no puede ser mayor a 100.");
                    return false;
                }

                if (!UnidadMedidaValida(item.UnidadSeleccionada))
                {
                    await MostrarMensajeAsync("Validación", $"Debe seleccionar la unidad de {item.NombreParametro}.");
                    return false;
                }
            }

            return true;
        }

        private static bool UnidadMedidaValida(UnidadMedidaResponse? unidad)
        {
            return unidad != null &&
                   unidad.UnidadMedidaId != null &&
                   unidad.UnidadMedidaId > 0;
        }

        private static bool EsUnidadPorcentaje(UnidadMedidaResponse? unidad)
        {
            if (unidad == null)
                return false;

            string texto = NormalizarTextoUnidad(unidad.TextoBusqueda);

            return texto.Contains("%") || texto.Contains("PORCENTAJE");
        }

        private decimal ObtenerValorParametroConstante(string codigoParametro)
        {
            var item = ParametrosConstantesAnalisis.FirstOrDefault(x =>
                string.Equals(x.CodigoParametro, codigoParametro, StringComparison.OrdinalIgnoreCase));

            if (item == null || !TryParseDecimal(item.Valor, out decimal valor))
                return 0;

            return valor;
        }

        private decimal ConvertirDecimal(string valor)
        {
            return TryParseDecimal(valor, out decimal resultado) ? resultado : 0;
        }

        private int ObtenerTipoCultivoIdSeleccionado()
        {
            return TipoCultivoSeleccionado?.TipoCultivoId ?? 0;
        }

        private int ObtenerTipoAnalisisSueloIdSeleccionado()
        {
            return 1;
        }

        private int ObtenerUnidadMedidaId(UnidadMedidaResponse? unidad)
        {
            return unidad?.UnidadMedidaId ?? 0;
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            if (HayCambiosPendientes())
            {
                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "Cancelar análisis",
                    "Hay cambios sin guardar. ¿Está seguro que desea salir?",
                    "Sí, salir",
                    "No, continuar"
                );

                if (!confirmar)
                    return;
            }

            await NavegarAtrasAsync();
        }

        private bool HayCambiosPendientes()
        {
            string estadoActual = ObtenerEstadoActualFormulario();

            return !string.Equals(
                estadoInicialFormulario,
                estadoActual,
                StringComparison.Ordinal
            );
        }

        private string ObtenerEstadoActualFormulario()
        {
            var partes = new List<string>
            {
                $"TerrenoId:{TerrenoSeleccionado?.TerrenoId}",
                $"TextoBusqueda:{TextoBusquedaTerreno?.Trim()}",
                $"PaisId:{PaisSeleccionado?.PaisId}",
                $"DepartamentoId:{DepartamentoSeleccionado?.DepartamentoId}",
                $"MunicipioId:{MunicipioSeleccionado?.MunicipioId}",
                $"TipoCultivoId:{TipoCultivoSeleccionado?.TipoCultivoId}",
                $"TipoCultivo:{TipoCultivoSeleccionado?.NombreMostrar}",
                $"TipoAnalisis:{TipoAnalisisSueloSeleccionado?.Trim()}",
                $"FechaLaboratorio:{FechaAnalisisLaboratorio:yyyy-MM-dd}",
                $"Laboratorio:{Laboratorio?.Trim()}",
                $"Identificador:{IdentificadorAnalisisSuelo?.Trim()}",
                $"Quintales:{CantidadQuintalesOro?.Trim()}",
                $"TamanoFinca:{TamanoFinca?.Trim()}",
                $"CantidadPlantas:{CantidadPlantas?.Trim()}",
                $"Ph:{Ph?.Trim()}",
                $"AcidezTotal:{AcidezTotal?.Trim()}",
                $"CalcioCice:{CalcioCice?.Trim()}",
                $"MagnesioCice:{MagnesioCice?.Trim()}",
                $"PotasioCice:{PotasioCice?.Trim()}"
            };

            foreach (var item in ParametrosConstantesAnalisis)
            {
                partes.Add($"CONST:{item.CodigoParametro}|{item.Valor?.Trim()}|{item.UnidadSeleccionada?.UnidadMedidaId}");
            }

            foreach (var item in ElementosQuimicosAnalisis)
            {
                partes.Add($"ELEM:{item.ElementoQuimicoId}|{item.CodigoParametro}|{item.Valor?.Trim()}|{item.UnidadSeleccionada?.UnidadMedidaId}");
            }

            return string.Join(";", partes);
        }

        private async Task NavegarAtrasAsync()
        {
            try
            {
                if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                {
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                await Shell.Current.GoToAsync("//MainPage");
            }
            catch
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeEnviar));
            EnviarAnalisisCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
            BuscarTerrenoCommand.ChangeCanExecute();
            LimpiarFiltrosTerrenoCommand.ChangeCanExecute();
        }

        private static bool ValidarDecimalOpcional(string valor, out decimal resultado)
        {
            resultado = 0;

            if (string.IsNullOrWhiteSpace(valor))
                return true;

            return TryParseDecimal(valor, out resultado);
        }

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out resultado))
                return true;

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
                return true;

            string valorNormalizado = (valor ?? string.Empty).Replace(",", ".");

            return decimal.TryParse(
                valorNormalizado,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out resultado
            );
        }

        private static string ObtenerIniciales(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return "US";

            var partes = nombreCompleto
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 1)
                return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();

            string inicialNombre = partes[0].Substring(0, 1);
            string inicialApellido = partes[1].Substring(0, 1);

            return $"{inicialNombre}{inicialApellido}".ToUpper();
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }

        private void LimpiarErroresFormulario()
        {
            ErrorTerreno = string.Empty;
            ErrorTipoCultivo = string.Empty;
            ErrorTipoAnalisisSuelo = string.Empty;
            ErrorFechaAnalisisLaboratorio = string.Empty;
            ErrorLaboratorio = string.Empty;
            ErrorIdentificadorAnalisisSuelo = string.Empty;
            ErrorCantidadQuintalesOro = string.Empty;
            ErrorTamanoFinca = string.Empty;
            ErrorCantidadPlantas = string.Empty;
            ErrorPh = string.Empty;
            ErrorAcidezTotal = string.Empty;
            ErrorCalcioCice = string.Empty;
            ErrorMagnesioCice = string.Empty;
            ErrorPotasioCice = string.Empty;
        }
    }
}