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

        private int? usuarioId;
        private string inicialesUsuario = string.Empty;
        private string nombreCompletoUsuario = string.Empty;
        private string correoUsuario = string.Empty;
        private string urlImagenUsuario = string.Empty;

        private string textoBusquedaTerreno = string.Empty;
        private TerrenoAnalisisResponse? terrenoSeleccionado;

        private string tipoCultivoSeleccionado = string.Empty;
        private string tipoAnalisisSueloSeleccionado = string.Empty;
        private DateTime fechaAnalisisLaboratorio = DateTime.Today;

        private string laboratorio = string.Empty;
        private string identificadorAnalisisSuelo = string.Empty;
        private string cantidadQuintalesOro = string.Empty;
        private string tamanoFinca = string.Empty;
        private string tipoMuestra = "Suelo";

        private string estadoInicialFormulario = string.Empty;

        public NuevoAnalisisFormViewModel()
        {
            ParametrosConstantesAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();
            ElementosQuimicosAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();

            Terrenos = new ObservableCollection<TerrenoAnalisisResponse>();
            TerrenosFiltrados = new ObservableCollection<TerrenoAnalisisResponse>();

            TiposCultivo = new ObservableCollection<string>();
            TiposAnalisisSuelo = new ObservableCollection<string>();

            BuscarTerrenoCommand = new Command(FiltrarTerrenos);

            SeleccionarTerrenoCommand = new Command<TerrenoAnalisisResponse>(
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

        public ObservableCollection<TerrenoAnalisisResponse> Terrenos { get; }

        public ObservableCollection<TerrenoAnalisisResponse> TerrenosFiltrados { get; }

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

        public TerrenoAnalisisResponse? TerrenoSeleccionado
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
                }

                RefrescarComandos();
            }
        }

        public bool TieneTerrenoSeleccionado => TerrenoSeleccionado != null;

        public ObservableCollection<string> TiposCultivo { get; }

        public ObservableCollection<string> TiposAnalisisSuelo { get; }

        public string TipoCultivoSeleccionado
        {
            get => tipoCultivoSeleccionado;
            set
            {
                tipoCultivoSeleccionado = value;
                OnPropertyChanged(nameof(TipoCultivoSeleccionado));
                RefrescarComandos();
            }
        }

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

        public string TipoMuestra
        {
            get => tipoMuestra;
            set
            {
                tipoMuestra = value;
                OnPropertyChanged(nameof(TipoMuestra));
                OnPropertyChanged(nameof(TextoTipoMuestra));
            }
        }

        public string TextoTipoMuestra => $"Tipo de muestra: {TipoMuestra}";

        public ObservableCollection<ResultadoAnalisisItemViewModel> ParametrosConstantesAnalisis { get; }

        public ObservableCollection<ResultadoAnalisisItemViewModel> ElementosQuimicosAnalisis { get; }

        public Command BuscarTerrenoCommand { get; }

        public Command<TerrenoAnalisisResponse> SeleccionarTerrenoCommand { get; }

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
                    CargarCatalogosFormulario();

                if (forceReload || Terrenos.Count == 0)
                    CargarTerrenosPrueba();

                if (forceReload || ParametrosConstantesAnalisis.Count == 0)
                    CargarParametrosConstantesAnalisis();

                if (forceReload || ElementosQuimicosAnalisis.Count == 0)
                    await CargarElementosQuimicosAnalisisAsync();

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
            TipoMuestra = "Suelo";
        }

        private void CargarCatalogosFormulario()
        {
            TiposCultivo.Clear();
            TiposCultivo.Add("Café");

            TiposAnalisisSuelo.Clear();
            TiposAnalisisSuelo.Add("Análisis químico de suelo");
            TiposAnalisisSuelo.Add("Análisis físico de suelo");
            TiposAnalisisSuelo.Add("Análisis completo de suelo");

            TipoCultivoSeleccionado = "Café";
            TipoAnalisisSueloSeleccionado = "Análisis químico de suelo";
            FechaAnalisisLaboratorio = DateTime.Today;
        }

        private void CargarTerrenosPrueba()
        {
            Terrenos.Clear();

            string nombreCliente = string.IsNullOrWhiteSpace(NombreCompletoUsuario)
                ? "Cliente no definido"
                : NombreCompletoUsuario;

            Terrenos.Add(new TerrenoAnalisisResponse
            {
                TerrenoId = 1,
                UsuarioId = UsuarioId,
                CodigoTerreno = "TER-001",
                NombreTerreno = "Lote El Porvenir",
                NombreCliente = nombreCliente,
                CantidadQuintalesOro = 25,
                TamanoFinca = 3.5m
            });

            Terrenos.Add(new TerrenoAnalisisResponse
            {
                TerrenoId = 2,
                UsuarioId = UsuarioId,
                CodigoTerreno = "TER-002",
                NombreTerreno = "Lote La Esperanza",
                NombreCliente = nombreCliente,
                CantidadQuintalesOro = 40,
                TamanoFinca = 5m
            });

            FiltrarTerrenos();
        }

        private void CargarParametrosConstantesAnalisis()
        {
            ParametrosConstantesAnalisis.Clear();

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "PH",
                NombreParametro = "pH",
                PlaceholderValor = "Ejemplo: 5.8",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = new ObservableCollection<string> { "pH", "sin unidad" },
                UnidadSeleccionada = "pH"
            });

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "MATERIA_ORGANICA",
                NombreParametro = "Materia Orgánica",
                PlaceholderValor = "Ejemplo: 3.2",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = new ObservableCollection<string> { "%", "g/kg", "mg/kg", "ppm" },
                UnidadSeleccionada = "%"
            });

            ParametrosConstantesAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "ACIDEZ_TOTAL",
                NombreParametro = "Acidez Total",
                PlaceholderValor = "Ejemplo: 0.5",
                EsConstante = true,
                EsElementoQuimico = false,
                PuedeEliminar = false,
                UnidadesMedida = new ObservableCollection<string> { "meq/100g", "cmol/kg" },
                UnidadSeleccionada = "meq/100g"
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
                    UnidadesMedida = ObtenerUnidadesElementoQuimico(simbolo),
                    UnidadSeleccionada = ObtenerUnidadPredeterminadaElementoQuimico(simbolo)
                });
            }
        }

        private static ObservableCollection<string> ObtenerUnidadesElementoQuimico(string? simbolo)
        {
            string simboloNormalizado = (simbolo ?? string.Empty).Trim().ToUpper();

            if (simboloNormalizado == "N")
                return new ObservableCollection<string> { "%", "ppm", "mg/kg" };

            if (simboloNormalizado == "K" ||
                simboloNormalizado == "CA" ||
                simboloNormalizado == "MG")
                return new ObservableCollection<string> { "cmol/kg", "meq/100g", "mg/kg", "ppm", "g/kg" };

            return new ObservableCollection<string> { "mg/kg", "ppm", "g/kg", "%", "meq/100g", "cmol/kg" };
        }

        private static string ObtenerUnidadPredeterminadaElementoQuimico(string? simbolo)
        {
            string simboloNormalizado = (simbolo ?? string.Empty).Trim().ToUpper();

            if (simboloNormalizado == "N")
                return "%";

            if (simboloNormalizado == "K" ||
                simboloNormalizado == "CA" ||
                simboloNormalizado == "MG")
                return "cmol/kg";

            return "mg/kg";
        }

        private void FiltrarTerrenos()
        {
            TerrenosFiltrados.Clear();

            string texto = TextoBusquedaTerreno?.Trim().ToLower() ?? string.Empty;

            IEnumerable<TerrenoAnalisisResponse> lista = string.IsNullOrWhiteSpace(texto)
                ? Terrenos
                : Terrenos.Where(t =>
                    (t.NombreCliente ?? string.Empty).ToLower().Contains(texto) ||
                    (t.CodigoTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.NombreTerreno ?? string.Empty).ToLower().Contains(texto)
                );

            foreach (var item in lista)
                TerrenosFiltrados.Add(item);
        }

        private void SeleccionarTerreno(TerrenoAnalisisResponse? terreno)
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

                var request = new AnalisisSueloGuardarCalculoRequest
                {
                    TerrenoId = TerrenoSeleccionado?.TerrenoId,
                    TipoCultivoId = ObtenerTipoCultivoIdSeleccionado(),
                    TipoAnalisisSueloId = ObtenerTipoAnalisisSueloIdSeleccionado(),
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
                    await analisisSueloApiService.GuardarCalculoAsync(request);

                if (response == null)
                {
                    await MostrarMensajeAsync("Error", "La API no devolvió una respuesta válida.");
                    return;
                }

                if (!response.Success)
                {
                    await MostrarMensajeAsync("Error", response.Message ?? "No se pudo procesar el análisis de suelo.");
                    return;
                }

                estadoInicialFormulario = ObtenerEstadoActualFormulario();

                await MostrarMensajeAsync(
                    "Correcto",
                    response.Message ?? "Análisis de suelo calculado y guardado correctamente."
                );

                /*
                 * Siguiente paso:
                 * guardar response en un servicio de estado temporal
                 * y navegar a la pantalla de resultados.
                 */
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
            if (UsuarioId == null || UsuarioId <= 0)
            {
                await MostrarMensajeAsync("Sesión", "No se encontró el usuario autenticado.");
                return false;
            }

            if (TerrenoSeleccionado == null)
            {
                await MostrarMensajeAsync("Validación", "Debe seleccionar un cliente/terreno.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TipoCultivoSeleccionado))
            {
                await MostrarMensajeAsync("Validación", "Debe seleccionar el tipo de cultivo.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TipoAnalisisSueloSeleccionado))
            {
                await MostrarMensajeAsync("Validación", "Debe seleccionar el tipo de análisis de suelo.");
                return false;
            }

            if (FechaAnalisisLaboratorio.Date > DateTime.Today)
            {
                await MostrarMensajeAsync("Validación", "La fecha del análisis no puede ser futura.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Laboratorio) || Laboratorio.Trim().Length < 3)
            {
                await MostrarMensajeAsync("Validación", "Debe ingresar un laboratorio válido.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(IdentificadorAnalisisSuelo))
            {
                await MostrarMensajeAsync("Validación", "Debe ingresar el identificador del análisis de suelo.");
                return false;
            }

            if (!TryParseDecimal(CantidadQuintalesOro, out decimal quintalesOro) || quintalesOro <= 0)
            {
                await MostrarMensajeAsync("Validación", "La cantidad de quintales oro debe ser mayor que cero.");
                return false;
            }

            if (!TryParseDecimal(TamanoFinca, out decimal tamanoFincaDecimal) || tamanoFincaDecimal <= 0)
            {
                await MostrarMensajeAsync("Validación", "El tamaño de la finca debe ser mayor que cero.");
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

                if (item.UnidadSeleccionada == "%" && valor > 100)
                {
                    await MostrarMensajeAsync("Validación", $"El porcentaje de {item.NombreParametro} no puede ser mayor a 100.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(item.UnidadSeleccionada))
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

                if (item.UnidadSeleccionada == "%" && valor > 100)
                {
                    await MostrarMensajeAsync("Validación", $"El porcentaje de {item.NombreParametro} no puede ser mayor a 100.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(item.UnidadSeleccionada))
                {
                    await MostrarMensajeAsync("Validación", $"Debe seleccionar la unidad de {item.NombreParametro}.");
                    return false;
                }
            }

            return true;
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
            return 1;
        }

        private int ObtenerTipoAnalisisSueloIdSeleccionado()
        {
            return 1;
        }

        private int ObtenerUnidadMedidaId(string? unidad)
        {
            string unidadNormalizada = (unidad ?? string.Empty).Trim().ToUpper();

            return unidadNormalizada switch
            {
                "MG/KG" => 1,
                "PPM" => 1,
                "CMOL/KG" => 2,
                "MEQ/100G" => 2,
                "LB/MZ" => 3,
                "KG/HA" => 4,
                "G/KG" => 5,
                "%" => 6,
                _ => 1
            };
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
                $"TipoCultivo:{TipoCultivoSeleccionado?.Trim()}",
                $"TipoAnalisis:{TipoAnalisisSueloSeleccionado?.Trim()}",
                $"FechaLaboratorio:{FechaAnalisisLaboratorio:yyyy-MM-dd}",
                $"Laboratorio:{Laboratorio?.Trim()}",
                $"Identificador:{IdentificadorAnalisisSuelo?.Trim()}",
                $"Quintales:{CantidadQuintalesOro?.Trim()}",
                $"TamanoFinca:{TamanoFinca?.Trim()}",
                $"TipoMuestra:{TipoMuestra?.Trim()}"
            };

            foreach (var item in ParametrosConstantesAnalisis)
            {
                partes.Add($"CONST:{item.CodigoParametro}|{item.Valor?.Trim()}|{item.UnidadSeleccionada?.Trim()}");
            }

            foreach (var item in ElementosQuimicosAnalisis)
            {
                partes.Add($"ELEM:{item.ElementoQuimicoId}|{item.CodigoParametro}|{item.Valor?.Trim()}|{item.UnidadSeleccionada?.Trim()}");
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
    }
}