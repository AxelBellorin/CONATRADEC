using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class MultiCalculoViewModel : GlobalService, IQueryAttributable
    {
        private const string TabBalanceFormula = "BALANCE_FORMULA";
        private const string TabEnmiendaCalcarea = "ENMIENDA_CALCAREA";
        private const string TabFertilizacionMixta = "FERTILIZACION_MIXTA";

        private readonly GuardarTodoApiService guardarTodoApiService = new();

        private AnalisisSueloCalculoDataResponse? resultadoCalculo;
        private AnalisisSueloGuardarCalculoRequest? requestGuardarAnalisis;

        private bool mostrarBalanceFormula;
        private bool mostrarEnmiendaCalcarea;
        private bool mostrarFertilizacionMixta;

        private string tabSeleccionada = string.Empty;
        private string mensaje = string.Empty;

        private int? terrenoId;
        private int? cantidadPlantas;
        private bool fertilizacionMixtaInicializada;
        private bool esModoEdicion;
        private int? analisisSueloCalculoIdEdicion;

        public MultiCalculoViewModel()
        {
            BalanceFormula = new BalanceFormulaViewModel();
            EnmiendaCalcarea = new EnmiendaCalcareaTabViewModel();
            FertilizacionMixta = new FertilizacionMixtaTabViewModel();

            BalanceFormula.ComplementoFertilizacionMixtaCambiado +=
                BalanceFormula_ComplementoFertilizacionMixtaCambiado;

            SeleccionarBalanceCommand = new Command(SeleccionarBalance);
            SeleccionarEnmiendaCommand = new Command(SeleccionarEnmienda);
            SeleccionarFertilizacionCommand = new Command(SeleccionarFertilizacion);

            VolverCommand = new Command(
                async () => await VolverAsync());

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy);
        }

        public BalanceFormulaViewModel BalanceFormula { get; }

        public EnmiendaCalcareaTabViewModel EnmiendaCalcarea { get; }

        public FertilizacionMixtaTabViewModel FertilizacionMixta { get; }

        public AnalisisSueloCalculoDataResponse? ResultadoCalculo
        {
            get => resultadoCalculo;
            set
            {
                resultadoCalculo = value;
                OnPropertyChanged(nameof(ResultadoCalculo));
                OnPropertyChanged(nameof(TipoCultivo));
                OnPropertyChanged(nameof(TipoAnalisisSuelo));
                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(CantidadQuintalesOro));
            }
        }

        public AnalisisSueloGuardarCalculoRequest? RequestGuardarAnalisis
        {
            get => requestGuardarAnalisis;
            set
            {
                requestGuardarAnalisis = value;
                OnPropertyChanged(nameof(RequestGuardarAnalisis));

                OnPropertyChanged(nameof(NombreAnalisisSuelo));
                OnPropertyChanged(nameof(TieneNombreAnalisisSuelo));

                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
                OnPropertyChanged(nameof(CalcioCice));
                OnPropertyChanged(nameof(MagnesioCice));
                OnPropertyChanged(nameof(PotasioCice));

                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(CantidadQuintalesOro));
            }
        }

        public int? TerrenoId
        {
            get => terrenoId;
            set
            {
                terrenoId = value;
                OnPropertyChanged(nameof(TerrenoId));
            }
        }

        public int? CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = value;
                OnPropertyChanged(nameof(CantidadPlantas));
                OnPropertyChanged(nameof(CantidadPlantasTexto));
            }
        }

        public bool MostrarBalanceFormula
        {
            get => mostrarBalanceFormula;
            set
            {
                mostrarBalanceFormula = value;
                OnPropertyChanged(nameof(MostrarBalanceFormula));
            }
        }

        public bool MostrarEnmiendaCalcarea
        {
            get => mostrarEnmiendaCalcarea;
            set
            {
                mostrarEnmiendaCalcarea = value;
                OnPropertyChanged(nameof(MostrarEnmiendaCalcarea));
            }
        }

        public bool MostrarFertilizacionMixta
        {
            get => mostrarFertilizacionMixta;
            set
            {
                mostrarFertilizacionMixta = value;
                OnPropertyChanged(nameof(MostrarFertilizacionMixta));
            }
        }

        public string TabSeleccionada
        {
            get => tabSeleccionada;
            set
            {
                tabSeleccionada = value ?? string.Empty;

                OnPropertyChanged(nameof(TabSeleccionada));
                OnPropertyChanged(nameof(EsBalanceSeleccionado));
                OnPropertyChanged(nameof(EsEnmiendaSeleccionada));
                OnPropertyChanged(nameof(EsFertilizacionSeleccionada));
                OnPropertyChanged(nameof(NombreTabActual));
                OnPropertyChanged(nameof(TextoEncabezado));
            }
        }

        public bool EsBalanceSeleccionado =>
            string.Equals(
                TabSeleccionada,
                TabBalanceFormula,
                StringComparison.OrdinalIgnoreCase);

        public bool EsEnmiendaSeleccionada =>
            string.Equals(
                TabSeleccionada,
                TabEnmiendaCalcarea,
                StringComparison.OrdinalIgnoreCase);

        public bool EsFertilizacionSeleccionada =>
            string.Equals(
                TabSeleccionada,
                TabFertilizacionMixta,
                StringComparison.OrdinalIgnoreCase);

        public string NombreTabActual
        {
            get
            {
                if (EsBalanceSeleccionado)
                    return "Balance";

                if (EsEnmiendaSeleccionada)
                    return "Enmienda";

                if (EsFertilizacionSeleccionada)
                    return "Mixta";

                return "Sin selección";
            }
        }

        public string TextoEncabezado
        {
            get
            {
                string nombreAnalisis = NombreAnalisisSuelo;

                if (EsBalanceSeleccionado)
                    return $"Está trabajando en el balance de fórmula del análisis {nombreAnalisis}. Puede cambiar de pestaña sin perder los datos.";

                if (EsEnmiendaSeleccionada)
                    return $"Está trabajando en la enmienda calcárea del análisis {nombreAnalisis}. Puede volver al balance sin perder los datos.";

                if (EsFertilizacionSeleccionada)
                    return $"Está trabajando en fertilización mixta del análisis {nombreAnalisis}. Puede cambiar de pestaña sin perder los datos.";

                return "Cambie entre los cálculos seleccionados desde las pestañas inferiores.";
            }
        }

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public string NombreAnalisisSuelo
        {
            get
            {
                string nombre =
                    RequestGuardarAnalisis?.IdentificadorAnalisisSuelo ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(nombre))
                    return "No disponible";

                return nombre.Trim();
            }
        }

        public bool TieneNombreAnalisisSuelo =>
            !string.IsNullOrWhiteSpace(
                RequestGuardarAnalisis?.IdentificadorAnalisisSuelo);

        public string TipoCultivo =>
            ResultadoCalculo?.TipoCultivo ?? "No disponible";

        public string TipoAnalisisSuelo =>
            ResultadoCalculo?.TipoAnalisisSuelo ?? "No disponible";

        public decimal Ph =>
            RequestGuardarAnalisis?.Ph ??
            ResultadoCalculo?.Ph ??
            0;

        public decimal AcidezTotal =>
            RequestGuardarAnalisis?.AcidezTotal ??
            ResultadoCalculo?.AcidezTotal ??
            0;

        public decimal CalcioCice =>
            RequestGuardarAnalisis?.CalcioCice ?? 0;

        public decimal MagnesioCice =>
            RequestGuardarAnalisis?.MagnesioCice ?? 0;

        public decimal PotasioCice =>
            RequestGuardarAnalisis?.PotasioCice ?? 0;

        public decimal TamanoFinca =>
            RequestGuardarAnalisis?.TamanoFinca ??
            ResultadoCalculo?.TamanoFinca ??
            0;

        public decimal CantidadQuintalesOro =>
            RequestGuardarAnalisis?.CantidadQuintalesOro ??
            ResultadoCalculo?.CantidadQuintalesOro ??
            0;

        public string CantidadPlantasTexto =>
            CantidadPlantas.HasValue && CantidadPlantas.Value > 0
                ? $"{CantidadPlantas.Value:N0} plantas"
                : "No disponible";

        public bool EsModoEdicion
        {
            get => esModoEdicion;
            private set
            {
                if (esModoEdicion == value)
                    return;

                esModoEdicion = value;
                OnPropertyChanged(nameof(EsModoEdicion));
                OnPropertyChanged(nameof(TextoBotonGuardar));
            }
        }

        public int? AnalisisSueloCalculoIdEdicion
        {
            get => analisisSueloCalculoIdEdicion;
            private set
            {
                analisisSueloCalculoIdEdicion = value;
                OnPropertyChanged(nameof(AnalisisSueloCalculoIdEdicion));
            }
        }

        public string TextoBotonGuardar =>
            EsModoEdicion ? "Actualizar" : "Guardar";

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                GuardarCommand?.ChangeCanExecute();
            }
        }

        public Command SeleccionarBalanceCommand { get; }

        public Command SeleccionarEnmiendaCommand { get; }

        public Command SeleccionarFertilizacionCommand { get; }

        public Command VolverCommand { get; }

        public Command GuardarCommand { get; }

        public async void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            Limpiar();

            EsModoEdicion =
                ObtenerBoolQuery(query, "esModoEdicion");

            if (query.ContainsKey("analisisSueloCalculoIdEdicion") &&
                int.TryParse(
                    query["analisisSueloCalculoIdEdicion"]?.ToString(),
                    out int idEdicion))
            {
                AnalisisSueloCalculoIdEdicion = idEdicion;
            }

            if (query.ContainsKey("resultadoCalculo"))
            {
                ResultadoCalculo =
                    query["resultadoCalculo"] as
                    AnalisisSueloCalculoDataResponse;
            }

            if (query.ContainsKey("requestGuardarAnalisis"))
            {
                RequestGuardarAnalisis =
                    query["requestGuardarAnalisis"] as
                    AnalisisSueloGuardarCalculoRequest;
            }

            if (query.ContainsKey("terrenoId"))
            {
                if (int.TryParse(
                        query["terrenoId"]?.ToString(),
                        out int idTerreno))
                {
                    TerrenoId = idTerreno;
                }
            }

            if (TerrenoId == null &&
                RequestGuardarAnalisis?.TerrenoId != null &&
                RequestGuardarAnalisis.TerrenoId > 0)
            {
                TerrenoId = RequestGuardarAnalisis.TerrenoId;
            }

            if (query.ContainsKey("cantidadPlantas"))
            {
                if (int.TryParse(
                        query["cantidadPlantas"]?.ToString(),
                        out int plantas))
                {
                    CantidadPlantas = plantas;
                }
            }

            MostrarBalanceFormula =
                ObtenerBoolQuery(query, "calcularBalanceFormula");

            MostrarEnmiendaCalcarea =
                ObtenerBoolQuery(query, "calcularEnmiendaCalcarea");

            MostrarFertilizacionMixta =
                ObtenerBoolQuery(query, "calcularFertilizacionMixta");

            await CalculoAnalisisTemporalService.Instance
                .IniciarNuevoCalculoAsync(
                    ResultadoCalculo,
                    RequestGuardarAnalisis);

            InicializarTabs();
            SeleccionarPrimeraPestanaDisponible();

            Mensaje =
                "Seleccione una pestaña inferior para continuar con los cálculos complementarios.";
        }

        private void InicializarTabs()
        {
            Dictionary<string, object> parametros = CrearParametrosBase();

            if (MostrarBalanceFormula)
                BalanceFormula.ApplyQueryAttributes(parametros);

            if (MostrarEnmiendaCalcarea)
            {
                EnmiendaCalcarea.Inicializar(
                    ResultadoCalculo,
                    RequestGuardarAnalisis,
                    TerrenoId,
                    CantidadPlantas);
            }

            if (MostrarFertilizacionMixta)
                InicializarFertilizacionMixtaSiEsNecesario();
        }

        private void InicializarFertilizacionMixtaSiEsNecesario()
        {
            if (fertilizacionMixtaInicializada)
                return;

            FertilizacionMixta.Inicializar(
                ResultadoCalculo,
                RequestGuardarAnalisis);

            fertilizacionMixtaInicializada = true;
        }

        private async void BalanceFormula_ComplementoFertilizacionMixtaCambiado(
            object? sender,
            BalanceFertilizacionMixtaChangedEventArgs e)
        {
            if (e.Activado)
            {
                MostrarFertilizacionMixta = true;
                InicializarFertilizacionMixtaSiEsNecesario();
            }

            if (!fertilizacionMixtaInicializada)
                return;

            await FertilizacionMixta.ConfigurarComplementoBalanceAsync(
                e.Activado,
                e.Contexto);

            if (e.Activado && e.Contexto == null)
            {
                Mensaje =
                    "El complemento está activo. Complete el balance y luego continúe en la pestaña Mixta.";
            }
            else if (e.Activado)
            {
                Mensaje =
                    "Balance vinculado con fertilización mixta. Complete el cálculo obligatorio en la pestaña Mixta.";
            }
        }

        private void Limpiar()
        {
            ResultadoCalculo = null;
            RequestGuardarAnalisis = null;

            MostrarBalanceFormula = false;
            MostrarEnmiendaCalcarea = false;
            MostrarFertilizacionMixta = false;

            TabSeleccionada = string.Empty;
            Mensaje = string.Empty;

            TerrenoId = null;
            CantidadPlantas = null;
            fertilizacionMixtaInicializada = false;
            EsModoEdicion = false;
            AnalisisSueloCalculoIdEdicion = null;

            EnmiendaCalcarea.ReiniciarParaNuevoCalculo();
        }

        private static bool ObtenerBoolQuery(
            IDictionary<string, object> query,
            string key)
        {
            if (!query.ContainsKey(key))
                return false;

            object? valor = query[key];

            if (valor is bool boolValor)
                return boolValor;

            if (bool.TryParse(
                    valor?.ToString(),
                    out bool resultado))
            {
                return resultado;
            }

            return false;
        }

        private void SeleccionarPrimeraPestanaDisponible()
        {
            if (MostrarBalanceFormula)
            {
                TabSeleccionada = TabBalanceFormula;
                return;
            }

            if (MostrarEnmiendaCalcarea)
            {
                TabSeleccionada = TabEnmiendaCalcarea;
                return;
            }

            if (MostrarFertilizacionMixta)
            {
                TabSeleccionada = TabFertilizacionMixta;
                return;
            }

            Mensaje = "No se recibió ningún cálculo seleccionado.";
        }

        private void SeleccionarBalance()
        {
            if (!MostrarBalanceFormula || IsBusy)
                return;

            TabSeleccionada = TabBalanceFormula;
        }

        private void SeleccionarEnmienda()
        {
            if (!MostrarEnmiendaCalcarea || IsBusy)
                return;

            TabSeleccionada = TabEnmiendaCalcarea;
        }

        private void SeleccionarFertilizacion()
        {
            if (!MostrarFertilizacionMixta || IsBusy)
                return;

            TabSeleccionada = TabFertilizacionMixta;
        }

        private async Task VolverAsync()
        {
            if (IsBusy)
                return;

            Dictionary<string, object> parametros = CrearParametrosBase();

            await GoToAsyncParameters(
                "//ResultadoAnalisisSueloPage",
                parametros);
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (BalanceFormula.ComplementarConFertilizacionMixta &&
                !FertilizacionMixta.TieneComplementoCompleto)
            {
                MostrarFertilizacionMixta = true;
                TabSeleccionada = TabFertilizacionMixta;

                await MostrarAlertaAsync(
                    "Fertilización mixta pendiente",
                    "Activó el complemento del balance. Debe calcular la fertilización mixta y el balance ajustado antes de guardar.");

                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = EsModoEdicion
                    ? "Actualizando el análisis y sus cálculos..."
                    : "Guardando el análisis y sus cálculos...";

                bool valido = await ValidarAntesDeGuardarAsync();

                if (!valido)
                    return;

                GuardarTodoRequest request =
                    await Task.Run(
                        ConstruirSolicitudGuardar);

                GuardarTodoResponse response;

                if (EsModoEdicion)
                {
                    if (AnalisisSueloCalculoIdEdicion == null ||
                        AnalisisSueloCalculoIdEdicion <= 0)
                    {
                        throw new InvalidOperationException(
                            "No se encontró el identificador del análisis que se debe actualizar.");
                    }

                    response = await guardarTodoApiService.EditarAsync(
                        AnalisisSueloCalculoIdEdicion.Value,
                        request);
                }
                else
                {
                    response = await guardarTodoApiService.GuardarAsync(request);
                }

                if (!response.Success)
                {
                    Mensaje = string.IsNullOrWhiteSpace(response.Message)
                        ? (EsModoEdicion
                            ? "No fue posible actualizar el análisis."
                            : "No fue posible guardar el análisis.")
                        : response.Message;

                    await MostrarAlertaAsync(
                        EsModoEdicion
                            ? "No se pudo actualizar"
                            : "No se pudo guardar",
                        Mensaje);
                    return;
                }

                bool fueEdicion = EsModoEdicion;

                await CalculoAnalisisTemporalService.Instance
                    .LimpiarTodoAsync();


                string mensajeExito =
                    string.IsNullOrWhiteSpace(response.Message)
                        ? (fueEdicion
                            ? "El análisis fue actualizado correctamente."
                            : "El análisis fue guardado correctamente.")
                        : response.Message;

                await MostrarAlertaAsync(
                    fueEdicion ? "Análisis actualizado" : "Análisis guardado",
                    mensajeExito);

                await GoToAsyncParameters("//MainPage");
            }
            catch (InvalidOperationException ex)
            {
                Mensaje = ex.Message;

                await MostrarAlertaAsync(
                    "Datos pendientes",
                    ex.Message);
            }
            catch (Exception ex)
            {
                Mensaje =
                    $"Ocurrió un error al preparar el guardado: {ex.Message}";

                await MostrarAlertaAsync(
                    "Error",
                    Mensaje);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool> ValidarAntesDeGuardarAsync()
        {
            CalculoAnalisisTemporalService temporal =
                CalculoAnalisisTemporalService.Instance;

            CalculoAnalisisTemporalState estado =
                temporal.ObtenerEstadoActual();

            AnalisisSueloGuardarCalculoRequest? datosAnalisis =
                estado.RequestGuardarAnalisis ??
                RequestGuardarAnalisis;

            AnalisisSueloCalculoDataResponse? requerimientoAnual =
                estado.ResultadoAnalisisSuelo ??
                ResultadoCalculo;

            if (datosAnalisis == null)
            {
                await MostrarAlertaAsync(
                    "Datos incompletos",
                    "No se encontraron los datos originales del análisis de suelo.");
                return false;
            }

            if (requerimientoAnual == null ||
                requerimientoAnual.Elementos == null ||
                requerimientoAnual.Elementos.Count == 0)
            {
                await MostrarAlertaAsync(
                    "Requerimiento pendiente",
                    "No se encontró un requerimiento anual válido para guardar.");
                return false;
            }

            if (!estado.RequerimientoAnual.TieneResultadoValido)
            {
                await MostrarAlertaAsync(
                    "Requerimiento pendiente",
                    "El requerimiento anual no está listo para guardarse.");
                return false;
            }

            if (MostrarBalanceFormula &&
                !await ValidarSeccionSeleccionadaAsync(
                    estado.BalanceFormula,
                    "balance de fórmula"))
            {
                TabSeleccionada = TabBalanceFormula;
                return false;
            }

            if (MostrarEnmiendaCalcarea &&
                !await ValidarSeccionSeleccionadaAsync(
                    estado.EnmiendaCalcarea,
                    "enmienda calcárea"))
            {
                TabSeleccionada = TabEnmiendaCalcarea;
                return false;
            }

            if (MostrarFertilizacionMixta &&
                !await ValidarSeccionSeleccionadaAsync(
                    estado.FertilizacionMixta,
                    "fertilización mixta"))
            {
                TabSeleccionada = TabFertilizacionMixta;
                return false;
            }

            return true;
        }

        private static async Task<bool> ValidarSeccionSeleccionadaAsync(
            CalculoSeccionTemporalState seccion,
            string nombreCalculo)
        {
            if (seccion.Estado == EstadoCalculoTemporal.PendienteRecalculo)
            {
                await MostrarAlertaAsync(
                    "Recalculo pendiente",
                    $"El cálculo de {nombreCalculo} cambió. Debe recalcularlo antes de guardar.");
                return false;
            }

            if (!seccion.TieneResultadoValido)
            {
                await MostrarAlertaAsync(
                    "Cálculo pendiente",
                    $"Debe completar el cálculo de {nombreCalculo} antes de guardar.");
                return false;
            }

            return true;
        }

        private GuardarTodoRequest ConstruirSolicitudGuardar()
        {
            CalculoAnalisisTemporalService temporal =
                CalculoAnalisisTemporalService.Instance;

            CalculoAnalisisTemporalState estado =
                temporal.ObtenerEstadoActual();

            AnalisisSueloGuardarCalculoRequest datosOrigen =
                estado.RequestGuardarAnalisis ??
                RequestGuardarAnalisis ??
                throw new InvalidOperationException(
                    "No se encontraron los datos originales del análisis.");

            AnalisisSueloCalculoDataResponse requerimientoOrigen =
                estado.ResultadoAnalisisSuelo ??
                ResultadoCalculo ??
                throw new InvalidOperationException(
                    "No se encontró el resultado del requerimiento anual.");

            GuardarTodoRequest request = new()
            {
                DatosAnalisis = ConstruirDatosAnalisis(datosOrigen),
                RequerimientoAnual = ConstruirRequerimientoAnual(
                    datosOrigen,
                    requerimientoOrigen),
                BalanceNutricional = ConstruirBalanceNutricional(temporal),
                EnmiendaCalcarea = ConstruirEnmiendaCalcarea(temporal),
                FertilizacionMixta = ConstruirFertilizacionMixta(temporal)
            };

            return request;
        }

        private static GuardarTodoDatosAnalisisRequest ConstruirDatosAnalisis(
            AnalisisSueloGuardarCalculoRequest origen)
        {
            if (origen.ElementosQuimicos == null ||
                origen.ElementosQuimicos.Count == 0)
            {
                throw new InvalidOperationException(
                    "El análisis no contiene elementos químicos originales.");
            }

            GuardarTodoDatosAnalisisRequest destino = new()
            {
                TerrenoId = origen.TerrenoId ?? 0,
                TipoCultivoId = origen.TipoCultivoId ?? 0,
                TipoAnalisisSueloId = origen.TipoAnalisisSueloId ?? 0,
                UsuarioId = origen.UsuarioId,
                CantidadQuintalesOro = origen.CantidadQuintalesOro ?? 0,
                TamanoFinca = origen.TamanoFinca ?? 0,
                Ph = origen.Ph ?? 0,
                MateriaOrganica = origen.MateriaOrganica ?? 0,
                UnidadMedidaMateriaOrganicaId =
                    origen.UnidadMedidaMateriaOrganicaId ?? 0,
                AcidezTotal = origen.AcidezTotal,
                FechaAnalisisSuelo = NormalizarFecha(
                    origen.FechaAnalisisSuelo),
                LaboratorioAnalasisSuelo =
                    origen.LaboratorioAnalasisSuelo?.Trim() ??
                    string.Empty,
                IdentificadorAnalisisSuelo =
                    origen.IdentificadorAnalisisSuelo?.Trim() ??
                    string.Empty
            };

            foreach (ElementoQuimicoAnalisisRequest elemento in
                     origen.ElementosQuimicos)
            {
                if (elemento.ElementoQuimicosId == null ||
                    elemento.ElementoQuimicosId <= 0 ||
                    elemento.UnidadMedidaId == null ||
                    elemento.UnidadMedidaId <= 0)
                {
                    throw new InvalidOperationException(
                        "Uno de los elementos originales no tiene identificadores válidos.");
                }

                destino.ElementosQuimicos.Add(
                    new GuardarTodoElementoAnalisisRequest
                    {
                        ElementoQuimicosId =
                            elemento.ElementoQuimicosId.Value,
                        UnidadMedidaId =
                            elemento.UnidadMedidaId.Value,
                        CantidadElemento =
                            elemento.CantidadElemento ?? 0
                    });
            }

            if (destino.TerrenoId <= 0 ||
                destino.TipoCultivoId <= 0 ||
                destino.TipoAnalisisSueloId <= 0)
            {
                throw new InvalidOperationException(
                    "El terreno, el cultivo o el tipo de análisis no son válidos.");
            }

            if (string.IsNullOrWhiteSpace(
                    destino.IdentificadorAnalisisSuelo))
            {
                throw new InvalidOperationException(
                    "El identificador del análisis es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(
                    destino.LaboratorioAnalasisSuelo))
            {
                throw new InvalidOperationException(
                    "El laboratorio del análisis es obligatorio.");
            }

            return destino;
        }

        private static GuardarTodoRequerimientoAnualRequest
            ConstruirRequerimientoAnual(
                AnalisisSueloGuardarCalculoRequest datosAnalisis,
                AnalisisSueloCalculoDataResponse origen)
        {
            if (origen.Elementos == null || origen.Elementos.Count == 0)
            {
                throw new InvalidOperationException(
                    "El requerimiento anual no contiene elementos calculados.");
            }

            GuardarTodoRequerimientoAnualRequest destino = new()
            {
                TerrenoId =
                    origen.TerrenoId ??
                    datosAnalisis.TerrenoId ??
                    0,
                TipoCultivoId =
                    origen.TipoCultivoId ??
                    datosAnalisis.TipoCultivoId ??
                    0,
                TipoCultivo = origen.TipoCultivo?.Trim() ?? string.Empty,
                TipoAnalisisSueloId =
                    origen.TipoAnalisisSueloId ??
                    datosAnalisis.TipoAnalisisSueloId ??
                    0,
                TipoAnalisisSuelo =
                    origen.TipoAnalisisSuelo?.Trim() ??
                    string.Empty,
                CantidadQuintalesOro =
                    origen.CantidadQuintalesOro ??
                    datosAnalisis.CantidadQuintalesOro ??
                    0,
                TamanoFinca =
                    origen.TamanoFinca ??
                    datosAnalisis.TamanoFinca ??
                    0,
                Ph =
                    origen.Ph ??
                    datosAnalisis.Ph ??
                    0,
                AcidezTotal =
                    origen.AcidezTotal ??
                    datosAnalisis.AcidezTotal,
                MateriaOrganica =
                    datosAnalisis.MateriaOrganica ?? 0,
                UnidadMedidaMateriaOrganicaId =
                    datosAnalisis.UnidadMedidaMateriaOrganicaId ?? 0,
                RecomendacionGeneral =
                    origen.RecomendacionGeneral?.Trim() ??
                    string.Empty,
                Observaciones =
                    origen.Observaciones?.ToList() ??
                    new List<string>()
            };

            foreach (ElementoResultadoCalculoResponse elemento in
                     origen.Elementos)
            {
                if (elemento.ElementoQuimicosId == null ||
                    elemento.ElementoQuimicosId <= 0)
                {
                    throw new InvalidOperationException(
                        "Uno de los elementos calculados no tiene identificador válido.");
                }

                destino.Elementos.Add(
                    new GuardarTodoRequerimientoElementoRequest
                    {
                        ElementoQuimicosId =
                            elemento.ElementoQuimicosId.Value,
                        SimboloElementoQuimico =
                            elemento.SimboloElementoQuimico?.Trim() ??
                            string.Empty,
                        NombreElementoQuimico =
                            elemento.NombreElementoQuimico?.Trim() ??
                            string.Empty,
                        CantidadIngresada =
                            elemento.CantidadIngresada ?? 0,
                        CantidadConvertidaLbMz =
                            elemento.CantidadConvertidaLbMz,
                        ExtraccionPorQQOro =
                            elemento.ExtraccionPorQQOro,
                        ExtraccionPorProduccion =
                            elemento.ExtraccionPorProduccion,
                        RangoMinimo = elemento.RangoMinimo,
                        RangoMaximo = elemento.RangoMaximo,
                        RangoMinimoLbMz = elemento.RangoMinimoLbMz,
                        RangoMaximoLbMz = elemento.RangoMaximoLbMz,
                        RequerimientoCalculado =
                            elemento.RequerimientoCalculado,
                        UnidadBase =
                            elemento.UnidadBase?.Trim() ??
                            string.Empty,
                        UnidadMedidaResultadoId =
                            elemento.UnidadMedidaResultadoId,
                        UnidadResultado =
                            elemento.UnidadResultado?.Trim() ??
                            string.Empty,
                        Clasificacion =
                            elemento.Clasificacion?.Trim() ??
                            string.Empty,
                        Observacion =
                            elemento.Observacion?.Trim() ??
                            string.Empty
                    });
            }

            return destino;
        }

        private GuardarTodoBalanceNutricionalRequest?
            ConstruirBalanceNutricional(
                CalculoAnalisisTemporalService temporal)
        {
            if (!MostrarBalanceFormula)
                return null;

            BalanceNutricionalRequest requestOriginal =
                temporal.ObtenerRequest<BalanceNutricionalRequest>(
                    TipoCalculoTemporal.BalanceFormula) ??
                throw new InvalidOperationException(
                    "No se encontró la selección de fuentes del balance.");

            BalanceNutricionalResponse resultadoOriginal =
                temporal.ObtenerResultado<BalanceNutricionalResponse>(
                    TipoCalculoTemporal.BalanceFormula) ??
                throw new InvalidOperationException(
                    "No se encontró el resultado del balance de fórmula.");

            if (requestOriginal.Items == null ||
                requestOriginal.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "El balance no contiene fuentes seleccionadas.");
            }

            if (resultadoOriginal.Detalle == null ||
                resultadoOriginal.Detalle.Count == 0)
            {
                throw new InvalidOperationException(
                    "El balance no contiene detalles calculados.");
            }

            if (requestOriginal.Items.Count !=
                resultadoOriginal.Detalle.Count)
            {
                throw new InvalidOperationException(
                    "Las fuentes seleccionadas no coinciden con los detalles del balance.");
            }

            return new GuardarTodoBalanceNutricionalRequest
            {
                TerrenoId =
                    requestOriginal.TerrenoId ??
                    TerrenoId ??
                    0,
                Resultado = MapearResultadoBalance(resultadoOriginal),
                Items = requestOriginal.Items
                    .Select(MapearItemBalance)
                    .ToList()
            };
        }

        private static GuardarTodoBalanceItemRequest MapearItemBalance(
            BalanceNutricionalItemRequest origen)
        {
            if (origen.FuenteNutrientesId == null ||
                origen.FuenteNutrientesId <= 0 ||
                origen.ElementoQuimicosId == null ||
                origen.ElementoQuimicosId <= 0)
            {
                throw new InvalidOperationException(
                    "Una fuente del balance no tiene identificadores válidos.");
            }

            return new GuardarTodoBalanceItemRequest
            {
                FuenteNutrientesId = origen.FuenteNutrientesId.Value,
                ElementoQuimicosId = origen.ElementoQuimicosId.Value,
                Libras = origen.RequerimientoLibras ?? 0
            };
        }

        private static GuardarTodoBalanceResultadoRequest
            MapearResultadoBalance(
                BalanceNutricionalResponse origen)
        {
            GuardarTodoBalanceResultadoRequest destino = new()
            {
                NombreFormula =
                    origen.NombreFormula?.Trim() ??
                    string.Empty,
                TotalLibras = origen.TotalLibras ?? 0,
                MezclaTotalQq = origen.MezclaTotalQq ?? 0,
                TotalPlantas = origen.TotalPlantas ?? 0,
                TotalAplicaciones = origen.TotalAplicaciones ?? 0,
                TotalOnzas = origen.TotalOnzas ?? 0,
                PrecioTotalFormula = origen.PrecioTotalFormula ?? 0,
                PrecioPorAplicacion = origen.PrecioPorAplicacion ?? 0,
                DosisPlantaAnualOz = origen.DosisPlantaAnualOz ?? 0,
                DosisPlantaPorAplicacionOz =
                    origen.DosisPlantaPorAplicacionOz ?? 0,
                FormulaComercial =
                    origen.FormulaComercial != null
                        ? new Dictionary<string, decimal>(
                            origen.FormulaComercial)
                        : new Dictionary<string, decimal>()
            };

            foreach (BalanceNutricionalDetalleResponse detalle in
                     origen.Detalle ??
                     new List<BalanceNutricionalDetalleResponse>())
            {
                destino.Detalle.Add(
                    new GuardarTodoBalanceDetalleRequest
                    {
                        Fuente =
                            detalle.Fuente?.Trim() ??
                            string.Empty,
                        Elemento =
                            detalle.Elemento?.Trim() ??
                            string.Empty,
                        Lb = detalle.Lb ?? 0,
                        Qq = detalle.Qq ?? 0,
                        RequerimientoLibras =
                            detalle.RequerimientoLibras ?? 0,
                        LibrasPorAplicacion =
                            detalle.LibrasPorAplicacion ?? 0,
                        OnzasAnuales =
                            detalle.OnzasAnuales ?? 0,
                        OnzasPorAplicacion =
                            detalle.OnzasPorAplicacion ?? 0,
                        PrecioPorQuintal =
                            detalle.PrecioPorQuintal ?? 0,
                        SubtotalFuente =
                            detalle.SubtotalFuente ?? 0,
                        Aportes =
                            detalle.Aportes != null
                                ? new Dictionary<string, decimal>(
                                    detalle.Aportes)
                                : new Dictionary<string, decimal>()
                    });
            }

            return destino;
        }

        private GuardarTodoEnmiendaCalcareaRequest?
            ConstruirEnmiendaCalcarea(
                CalculoAnalisisTemporalService temporal)
        {
            if (!MostrarEnmiendaCalcarea)
                return null;

            EnmiendaCalcareaCalcularRequest request =
                temporal.ObtenerRequest<EnmiendaCalcareaCalcularRequest>(
                    TipoCalculoTemporal.EnmiendaCalcarea) ??
                throw new InvalidOperationException(
                    "No se encontró la fuente seleccionada para la enmienda.");

            EnmiendaCalcareaCalcularResponse resultado =
                temporal.ObtenerResultado<EnmiendaCalcareaCalcularResponse>(
                    TipoCalculoTemporal.EnmiendaCalcarea) ??
                throw new InvalidOperationException(
                    "No se encontró el resultado de la enmienda calcárea.");

            if (request.FuenteNutrientesId <= 0)
            {
                throw new InvalidOperationException(
                    "La fuente seleccionada para la enmienda no es válida.");
            }

            return new GuardarTodoEnmiendaCalcareaRequest
            {
                FuenteNutrientesId = request.FuenteNutrientesId,
                Resultado = new GuardarTodoEnmiendaResultadoRequest
                {
                    EnmiendaCalcareaId =
                        resultado.EnmiendaCalcareaId ?? 0,
                    NombreAnalisis =
                        resultado.NombreAnalisis?.Trim() ??
                        request.NombreAnalisis.Trim(),
                    FuenteNutriente =
                        resultado.FuenteNutriente?.Trim() ??
                        string.Empty,
                    Ph = resultado.Ph ?? request.Ph,
                    Ca = resultado.Ca ?? request.Ca,
                    Mg = resultado.Mg ?? request.Mg,
                    K = resultado.K ?? request.K,
                    AcidezTotal =
                        resultado.AcidezTotal ??
                        request.AcidezTotal,
                    SaturacionDeseada =
                        resultado.SaturacionDeseada ?? 0,
                    Prnt = resultado.Prnt ?? 0,
                    SumaBases = resultado.SumaBases ?? 0,
                    Cice = resultado.Cice ?? 0,
                    SaturacionActual =
                        resultado.SaturacionActual ?? 0,
                    NecesidadEncaladoTonHa =
                        resultado.NecesidadEncaladoTonHa ?? 0,
                    NecesidadEncaladoKgHa =
                        resultado.NecesidadEncaladoKgHa ?? 0,
                    NecesidadEncaladoLbHa =
                        resultado.NecesidadEncaladoLbHa ?? 0,
                    TerrenoId =
                        resultado.TerrenoId ??
                        request.TerrenoId,
                    TotalPlantas =
                        resultado.TotalPlantas ??
                        request.TotalPlantas,
                    TotalAplicaciones =
                        resultado.TotalAplicaciones ??
                        request.TotalAplicaciones,
                    NecesidadEncaladoLbMz =
                        resultado.NecesidadEncaladoLbMz ?? 0,
                    NecesidadEncaladoOzMz =
                        resultado.NecesidadEncaladoOzMz ?? 0,
                    DosisPlantaAnualOz =
                        resultado.DosisPlantaAnualOz ?? 0,
                    DosisPlantaPorAplicacionOz =
                        resultado.DosisPlantaPorAplicacionOz ?? 0
                }
            };
        }

        private GuardarTodoFertilizacionMixtaRequest?
            ConstruirFertilizacionMixta(
                CalculoAnalisisTemporalService temporal)
        {
            if (!MostrarFertilizacionMixta)
                return null;

            FertilizacionMixtaCalculoResponse resultado =
                temporal.ObtenerResultado<FertilizacionMixtaCalculoResponse>(
                    TipoCalculoTemporal.FertilizacionMixta) ??
                throw new InvalidOperationException(
                    "No se encontró el resultado de la fertilización mixta.");

            if (resultado.Fuentes == null ||
                resultado.Fuentes.Count == 0 ||
                resultado.Detalles == null ||
                resultado.Detalles.Count == 0)
            {
                throw new InvalidOperationException(
                    "La fertilización mixta no contiene fuentes o detalles para guardar.");
            }

            GuardarTodoFertilizacionMixtaRequest destino = new()
            {
                Observacion =
                    resultado.Observacion?.Trim() ??
                    string.Empty
            };

            foreach (FuenteFertilizacionMixtaResultadoResponse fuente in
                     resultado.Fuentes)
            {
                if (fuente.FuenteNutrientesId == null ||
                    fuente.FuenteNutrientesId <= 0)
                {
                    throw new InvalidOperationException(
                        "Una fuente de fertilización mixta no tiene identificador válido.");
                }

                destino.Fuentes.Add(
                    new GuardarTodoFertilizacionMixtaFuenteRequest
                    {
                        FuenteNutrientesId =
                            fuente.FuenteNutrientesId.Value,
                        NombreFuente =
                            fuente.NombreFuente?.Trim() ??
                            string.Empty,
                        CantidadQq = fuente.CantidadQq ?? 0
                    });
            }

            foreach (DetalleFertilizacionMixtaResultadoResponse detalle in
                     resultado.Detalles)
            {
                if (detalle.ElementoQuimicosId == null ||
                    detalle.ElementoQuimicosId <= 0)
                {
                    throw new InvalidOperationException(
                        "Un elemento de fertilización mixta no tiene identificador válido.");
                }

                GuardarTodoFertilizacionMixtaDetalleRequest detalleDestino =
                    new()
                    {
                        ElementoQuimicosId =
                            detalle.ElementoQuimicosId.Value,
                        Elemento =
                            detalle.Elemento?.Trim() ??
                            string.Empty,
                        Exportable = detalle.Exportable ?? 0,
                        AporteOrganico =
                            detalle.AporteOrganico ?? 0,
                        Diferencia = detalle.Diferencia ?? 0,
                        Deficit = detalle.Deficit ?? 0,
                        Sobrante = detalle.Sobrante ?? 0
                    };

                foreach (FuenteDetalleFertilizacionMixtaResponse fuente in
                         detalle.Fuentes ??
                         new List<FuenteDetalleFertilizacionMixtaResponse>())
                {
                    detalleDestino.Fuentes.Add(
                        new GuardarTodoFertilizacionMixtaFuenteDetalleRequest
                        {
                            FuenteNutrientesId =
                                fuente.FuenteNutrientesId ?? 0,
                            NombreFuente =
                                fuente.NombreFuente?.Trim() ??
                                string.Empty,
                            CantidadQq = fuente.CantidadQq ?? 0,
                            AportePorUnidad =
                                fuente.AportePorUnidad ?? 0,
                            AporteTotal = fuente.AporteTotal ?? 0
                        });
                }

                destino.Detalles.Add(detalleDestino);
            }

            return destino;
        }

        private static string NormalizarFecha(string? fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha))
            {
                throw new InvalidOperationException(
                    "La fecha del análisis es obligatoria.");
            }

            string valor = fecha.Trim();

            string[] formatos =
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "MM/dd/yyyy",
                "M/d/yyyy"
            };

            if (DateTime.TryParseExact(
                    valor,
                    formatos,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fechaExacta))
            {
                return fechaExacta.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(
                    valor,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None,
                    out DateTime fechaActual))
            {
                return fechaActual.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException(
                "La fecha del análisis no tiene un formato válido.");
        }

        private static async Task MostrarAlertaAsync(
            string titulo,
            string mensaje)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    titulo,
                    mensaje,
                    "Aceptar");
            }
        }

        private Dictionary<string, object> CrearParametrosBase()
        {
            Dictionary<string, object> parametros = new();

            if (ResultadoCalculo != null)
            {
                parametros.Add(
                    "resultadoCalculo",
                    ResultadoCalculo);
            }

            if (RequestGuardarAnalisis != null)
            {
                parametros.Add(
                    "requestGuardarAnalisis",
                    RequestGuardarAnalisis);
            }

            if (TerrenoId != null && TerrenoId > 0)
                parametros.Add("terrenoId", TerrenoId.Value);

            if (CantidadPlantas != null && CantidadPlantas > 0)
            {
                parametros.Add(
                    "cantidadPlantas",
                    CantidadPlantas.Value);
            }

            parametros.Add(
                "calcularBalanceFormula",
                MostrarBalanceFormula);

            parametros.Add(
                "calcularEnmiendaCalcarea",
                MostrarEnmiendaCalcarea);

            parametros.Add(
                "calcularFertilizacionMixta",
                MostrarFertilizacionMixta);

            parametros.Add(
                "esModoEdicion",
                EsModoEdicion);

            if (EsModoEdicion &&
                AnalisisSueloCalculoIdEdicion is > 0)
            {
                parametros.Add(
                    "analisisSueloCalculoIdEdicion",
                    AnalisisSueloCalculoIdEdicion.Value);
            }

            return parametros;
        }

    }
}
