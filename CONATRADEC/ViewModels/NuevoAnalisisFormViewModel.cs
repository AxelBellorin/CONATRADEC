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
        private readonly TerrenoApiService terrenoApiService = new();

        private int? usuarioId;
        private string inicialesUsuario = string.Empty;
        private string nombreCompletoUsuario = string.Empty;
        private string correoUsuario = string.Empty;
        private string urlImagenUsuario = string.Empty;

        private string textoBusquedaTerreno = string.Empty;
        private TerrenoResponse? terrenoSeleccionado;

        private TipoCultivoResponse? tipoCultivoSeleccionado;
        private string tipoAnalisisSueloSeleccionado = string.Empty;
        private DateTime fechaAnalisisLaboratorio = DateTime.Today;

        private string laboratorio = string.Empty;
        private string identificadorAnalisisSuelo = string.Empty;
        private string cantidadQuintalesOro = string.Empty;
        private string tamanoFinca = string.Empty;
        private string cantidadPlantas = string.Empty;

        private string estadoInicialFormulario = string.Empty;

        private bool debeLimpiarFormulario = true;

        private string errorTerreno = string.Empty;
        private string errorTipoCultivo = string.Empty;
        private string errorTipoAnalisisSuelo = string.Empty;
        private string errorFechaAnalisisLaboratorio = string.Empty;
        private string errorLaboratorio = string.Empty;
        private string errorIdentificadorAnalisisSuelo = string.Empty;
        private string errorCantidadQuintalesOro = string.Empty;
        private string errorTamanoFinca = string.Empty;
        private string errorCantidadPlantas = string.Empty;

        public NuevoAnalisisFormViewModel()
        {
            ParametrosConstantesAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();
            ElementosQuimicosAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();

            Terrenos = new ObservableCollection<TerrenoResponse>();
            TerrenosFiltrados = new ObservableCollection<TerrenoResponse>();

            TiposCultivo = new ObservableCollection<TipoCultivoResponse>();
            TiposAnalisisSuelo = new ObservableCollection<string>();

            UnidadesMedidaCatalogo = new ObservableCollection<UnidadMedidaResponse>();

            BuscarTerrenoCommand = new Command(FiltrarTerrenos);

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

        public ObservableCollection<TerrenoResponse> Terrenos { get; }

        public ObservableCollection<TerrenoResponse> TerrenosFiltrados { get; }

        public ObservableCollection<TipoCultivoResponse> TiposCultivo { get; }

        public ObservableCollection<string> TiposAnalisisSuelo { get; }

        public ObservableCollection<UnidadMedidaResponse> UnidadesMedidaCatalogo { get; }

        public string TextoBusquedaTerreno
        {
            get => textoBusquedaTerreno;
            set
            {
                textoBusquedaTerreno = value;
                OnPropertyChanged(nameof(TextoBusquedaTerreno));
                FiltrarTerrenos();
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

        public ObservableCollection<ResultadoAnalisisItemViewModel> ParametrosConstantesAnalisis { get; }

        public ObservableCollection<ResultadoAnalisisItemViewModel> ElementosQuimicosAnalisis { get; }

        public Command BuscarTerrenoCommand { get; }

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

                if (forceReload || Terrenos.Count == 0)
                    await CargarTerrenosAsync();

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

            TipoCultivoSeleccionado = TiposCultivo.FirstOrDefault();
            TipoAnalisisSueloSeleccionado = TiposAnalisisSuelo.FirstOrDefault() ?? string.Empty;

            FechaAnalisisLaboratorio = DateTime.Today;

            Laboratorio = string.Empty;
            IdentificadorAnalisisSuelo = string.Empty;
            CantidadQuintalesOro = string.Empty;
            TamanoFinca = string.Empty;
            CantidadPlantas = string.Empty;

            ParametrosConstantesAnalisis.Clear();
            CargarParametrosConstantesAnalisis();

            ElementosQuimicosAnalisis.Clear();
            await CargarElementosQuimicosAnalisisAsync();

            TerrenosFiltrados.Clear();

            foreach (var terreno in Terrenos)
                TerrenosFiltrados.Add(terreno);

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

        private async Task CargarTerrenosAsync()
        {
            Terrenos.Clear();
            TerrenosFiltrados.Clear();

            ObservableCollection<TerrenoResponse> terrenos =
                await terrenoApiService.GetTerrenosAsync();

            foreach (var terreno in terrenos)
            {
                if (terreno == null)
                    continue;

                if (terreno.TerrenoId == null || terreno.TerrenoId <= 0)
                    continue;

                if (terreno.Activo == false)
                    continue;

                Terrenos.Add(terreno);
            }

            FiltrarTerrenos();

            if (Terrenos.Count == 0)
            {
                await MostrarMensajeAsync(
                    "Terrenos",
                    "No se encontraron terrenos activos para seleccionar."
                );
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

            ObservableCollection<UnidadMedidaResponse> unidadesPh = ClonarUnidadesMedida();
            ObservableCollection<UnidadMedidaResponse> unidadesMateriaOrganica = ClonarUnidadesMedida();
            ObservableCollection<UnidadMedidaResponse> unidadesAcidezTotal = ClonarUnidadesMedida();

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "PH",
                NombreParametro = "pH",
                PlaceholderValor = "Ejemplo: 5.8",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = unidadesPh,
                UnidadSeleccionada = BuscarUnidadMedidaEnLista(unidadesPh, "PH", "SIN UNIDAD", "S/M")
            });

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "MATERIA_ORGANICA",
                NombreParametro = "Materia Orgánica",
                PlaceholderValor = "Ejemplo: 3.2",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = unidadesMateriaOrganica,
                UnidadSeleccionada = BuscarUnidadMedidaEnLista(unidadesMateriaOrganica, "PPM", "%","PORCENTAJE")
            });

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "ACIDEZ_TOTAL",
                NombreParametro = "Acidez Total",
                PlaceholderValor = "Ejemplo: 0.5",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = unidadesAcidezTotal,
                UnidadSeleccionada = BuscarUnidadMedidaEnLista(unidadesAcidezTotal, "MEQ/100G", "CMOL/KG")
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

        private void FiltrarTerrenos()
        {
            TerrenosFiltrados.Clear();

            string texto = TextoBusquedaTerreno?.Trim().ToLower() ?? string.Empty;

            IEnumerable<TerrenoResponse> lista = string.IsNullOrWhiteSpace(texto)
                ? Terrenos
                : Terrenos.Where(t =>
                    (t.NombreCliente ?? string.Empty).ToLower().Contains(texto) ||
                    (t.CodigoTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.NombreTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.NombrePropietarioTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.DireccionTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.TextoUbicacion ?? string.Empty).ToLower().Contains(texto)
                );

            foreach (var item in lista)
                TerrenosFiltrados.Add(item);
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

                decimal ph = ObtenerValorParametroConstante("PH");
                decimal materiaOrganica = ObtenerValorParametroConstante("MATERIA_ORGANICA");
                decimal acidezTotal = ObtenerValorParametroConstante("ACIDEZ_TOTAL");

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
                    Ph = ph,
                    MateriaOrganica = materiaOrganica,
                    AcidezTotal = acidezTotal,
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
                    Ph = ph,
                    MateriaOrganica = materiaOrganica,
                    AcidezTotal = acidezTotal,
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

                if (string.Equals(item.CodigoParametro, "PH", StringComparison.OrdinalIgnoreCase) &&
                    (valor < 0 || valor > 14))
                {
                    await MostrarMensajeAsync("Validación", "El pH debe estar entre 0 y 14.");
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
                $"TipoCultivoId:{TipoCultivoSeleccionado?.TipoCultivoId}",
                $"TipoCultivo:{TipoCultivoSeleccionado?.NombreMostrar}",
                $"TipoAnalisis:{TipoAnalisisSueloSeleccionado?.Trim()}",
                $"FechaLaboratorio:{FechaAnalisisLaboratorio:yyyy-MM-dd}",
                $"Laboratorio:{Laboratorio?.Trim()}",
                $"Identificador:{IdentificadorAnalisisSuelo?.Trim()}",
                $"Quintales:{CantidadQuintalesOro?.Trim()}",
                $"TamanoFinca:{TamanoFinca?.Trim()}",
                $"CantidadPlantas:{CantidadPlantas?.Trim()}"
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
        }

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out resultado))
                return true;

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
                return true;

            string valorNormalizado = valor.Replace(",", ".");

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
        }
    }
}