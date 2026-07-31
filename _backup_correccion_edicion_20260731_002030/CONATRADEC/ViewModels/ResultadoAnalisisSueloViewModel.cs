using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class ResultadoAnalisisSueloViewModel :
        GlobalService,
        IQueryAttributable
    {
        private readonly GuardarTodoApiService
            guardarTodoApiService = new();

        private readonly AnalisisReporteService
            analisisReporteService = new();

        private AnalisisSueloCalculoDataResponse? resultado;
        private AnalisisSueloGuardarCalculoRequest?
            requestGuardarAnalisis;

        private string tituloResultado =
            "Resultado del análisis de suelo";

        private string recomendacionGeneral = string.Empty;
        private string sugerenciaSiguienteCalculo =
            string.Empty;

        private string mensajeSeleccionCalculo =
            string.Empty;

        private bool tieneObservaciones;
        private bool tieneElementos;
        private bool phMuyAcido;

        private bool calcularBalanceFormula;
        private bool calcularEnmiendaCalcarea;
        private bool calcularFertilizacionMixta;

        private int? cantidadPlantas;

        private readonly HashSet<int>
            elementosIncluidosInicialmente = new();

        private bool esModoEdicion;
        private int? analisisSueloCalculoIdEdicion;

        public ResultadoAnalisisSueloViewModel()
        {
            Elementos =
                new ObservableCollection<
                    ElementoResultadoCalculoResponse>();

            Observaciones =
                new ObservableCollection<string>();

            ProcesarSeleccionCommand =
                new Command(
                    async () =>
                        await ProcesarSeleccionAsync(),
                    () => !IsBusy);

            VolverCommand =
                new Command(
                    async () =>
                        await VolverAsync(),
                    () => !IsBusy);

            IncluirTodosElementosCommand =
                new Command(
                    () =>
                        CambiarSeleccionTodosElementos(true),
                    () => !IsBusy &&
                          Elementos.Count > 0);

            ExcluirTodosElementosCommand =
                new Command(
                    () =>
                        CambiarSeleccionTodosElementos(false),
                    () => !IsBusy &&
                          Elementos.Count > 0);
        }

        public AnalisisSueloCalculoDataResponse?
            Resultado
        {
            get => resultado;
            set
            {
                resultado = value;

                OnPropertyChanged(nameof(Resultado));
                OnPropertyChanged(nameof(TipoCultivo));
                OnPropertyChanged(nameof(TipoAnalisisSuelo));
                OnPropertyChanged(
                    nameof(CantidadQuintalesOro));
                OnPropertyChanged(nameof(TamanoFinca));
                OnPropertyChanged(nameof(Ph));
                OnPropertyChanged(nameof(AcidezTotal));
            }
        }

        public AnalisisSueloGuardarCalculoRequest?
            RequestGuardarAnalisis
        {
            get => requestGuardarAnalisis;
            set
            {
                requestGuardarAnalisis = value;

                OnPropertyChanged(
                    nameof(RequestGuardarAnalisis));
            }
        }

        public int? CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = value;
                OnPropertyChanged(nameof(CantidadPlantas));
            }
        }

        public bool EsModoEdicion
        {
            get => esModoEdicion;
            private set
            {
                if (esModoEdicion == value)
                    return;

                esModoEdicion = value;

                OnPropertyChanged(nameof(EsModoEdicion));
                OnPropertyChanged(
                    nameof(TextoBotonContinuar));
            }
        }

        public int? AnalisisSueloCalculoIdEdicion
        {
            get => analisisSueloCalculoIdEdicion;
            private set
            {
                analisisSueloCalculoIdEdicion = value;

                OnPropertyChanged(
                    nameof(
                        AnalisisSueloCalculoIdEdicion));
            }
        }

        /// <summary>
        /// Sin cálculos opcionales, la misma acción guarda o
        /// actualiza únicamente el requerimiento anual.
        /// </summary>
        public string TextoBotonContinuar
        {
            get
            {
                if (!TieneSeleccionCalculo)
                {
                    return EsModoEdicion
                        ? "Actualizar requerimiento"
                        : "Guardar requerimiento";
                }

                return EsModoEdicion
                    ? "Continuar edición"
                    : "Continuar con cálculos";
            }
        }

        public string TituloResultado
        {
            get => tituloResultado;
            set
            {
                tituloResultado =
                    value ?? string.Empty;

                OnPropertyChanged(
                    nameof(TituloResultado));
            }
        }

        public string RecomendacionGeneral
        {
            get => recomendacionGeneral;
            set
            {
                recomendacionGeneral =
                    value ?? string.Empty;

                OnPropertyChanged(
                    nameof(RecomendacionGeneral));
            }
        }

        public string SugerenciaSiguienteCalculo
        {
            get => sugerenciaSiguienteCalculo;
            set
            {
                sugerenciaSiguienteCalculo =
                    value ?? string.Empty;

                OnPropertyChanged(
                    nameof(
                        SugerenciaSiguienteCalculo));
            }
        }

        public string MensajeSeleccionCalculo
        {
            get => mensajeSeleccionCalculo;
            set
            {
                mensajeSeleccionCalculo =
                    value ?? string.Empty;

                OnPropertyChanged(
                    nameof(MensajeSeleccionCalculo));

                OnPropertyChanged(
                    nameof(
                        TieneMensajeSeleccionCalculo));
            }
        }

        public bool TieneMensajeSeleccionCalculo =>
            !string.IsNullOrWhiteSpace(
                MensajeSeleccionCalculo);

        public bool TieneObservaciones
        {
            get => tieneObservaciones;
            set
            {
                tieneObservaciones = value;

                OnPropertyChanged(
                    nameof(TieneObservaciones));
            }
        }

        public bool TieneElementos
        {
            get => tieneElementos;
            set
            {
                tieneElementos = value;

                OnPropertyChanged(
                    nameof(TieneElementos));
            }
        }

        public bool PhMuyAcido
        {
            get => phMuyAcido;
            set
            {
                phMuyAcido = value;

                OnPropertyChanged(nameof(PhMuyAcido));
            }
        }

        public bool CalcularBalanceFormula
        {
            get => calcularBalanceFormula;
            set
            {
                if (calcularBalanceFormula == value)
                    return;

                calcularBalanceFormula = value;

                OnPropertyChanged(
                    nameof(CalcularBalanceFormula));

                NotificarSeleccion();
            }
        }

        public bool CalcularEnmiendaCalcarea
        {
            get => calcularEnmiendaCalcarea;
            set
            {
                if (calcularEnmiendaCalcarea == value)
                    return;

                calcularEnmiendaCalcarea = value;

                OnPropertyChanged(
                    nameof(CalcularEnmiendaCalcarea));

                NotificarSeleccion();
            }
        }

        public bool CalcularFertilizacionMixta
        {
            get => calcularFertilizacionMixta;
            set
            {
                if (calcularFertilizacionMixta == value)
                    return;

                calcularFertilizacionMixta = value;

                OnPropertyChanged(
                    nameof(
                        CalcularFertilizacionMixta));

                NotificarSeleccion();
            }
        }

        public bool TieneSeleccionCalculo =>
            CalcularBalanceFormula ||
            CalcularEnmiendaCalcarea ||
            CalcularFertilizacionMixta;

        public string TextoSeleccionCalculo
        {
            get
            {
                List<string> seleccionados =
                    ObtenerCalculosSeleccionadosTexto();

                if (seleccionados.Count == 0)
                {
                    return EsModoEdicion
                        ? "No se conservará ningún cálculo complementario. Se actualizará solamente el requerimiento anual."
                        : "No seleccionó cálculos opcionales. Se guardará solamente el requerimiento anual.";
                }

                return string.Join(
                    ", ",
                    seleccionados);
            }
        }

        public string TipoCultivo =>
            Resultado?.TipoCultivo ??
            string.Empty;

        public string TipoAnalisisSuelo =>
            Resultado?.TipoAnalisisSuelo ??
            string.Empty;

        public decimal CantidadQuintalesOro =>
            Resultado?.CantidadQuintalesOro ?? 0;

        public decimal TamanoFinca =>
            Resultado?.TamanoFinca ?? 0;

        public decimal Ph =>
            Resultado?.Ph ?? 0;

        public decimal AcidezTotal =>
            Resultado?.AcidezTotal ?? 0;

        public int TotalElementosIncluidos =>
            Elementos.Count(x =>
                x.IncluirEnCalculosComplementarios);

        public string TextoElementosIncluidos =>
            $"{TotalElementosIncluidos} de " +
            $"{Elementos.Count} elemento(s) " +
            "participarán en Balance y Mixta.";

        public ObservableCollection<
            ElementoResultadoCalculoResponse>
                Elementos { get; }

        public ObservableCollection<string>
            Observaciones { get; }

        public Command ProcesarSeleccionCommand { get; }

        public Command VolverCommand { get; }

        public Command
            IncluirTodosElementosCommand { get; }

        public Command
            ExcluirTodosElementosCommand { get; }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;

                ProcesarSeleccionCommand
                    .ChangeCanExecute();

                VolverCommand
                    .ChangeCanExecute();

                IncluirTodosElementosCommand
                    .ChangeCanExecute();

                ExcluirTodosElementosCommand
                    .ChangeCanExecute();
            }
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            LimpiarPantallaTemporal();

            if (query.TryGetValue(
                    "resultadoCalculo",
                    out object? valorResultado) &&
                valorResultado
                    is AnalisisSueloCalculoDataResponse
                        resultadoApi)
            {
                CargarResultado(resultadoApi);
            }

            if (query.TryGetValue(
                    "requestGuardarAnalisis",
                    out object? valorRequest))
            {
                RequestGuardarAnalisis =
                    valorRequest
                    as AnalisisSueloGuardarCalculoRequest;
            }

            if (query.TryGetValue(
                    "cantidadPlantas",
                    out object? valorPlantas) &&
                int.TryParse(
                    valorPlantas?.ToString(),
                    out int plantas))
            {
                CantidadPlantas = plantas;
            }

            EsModoEdicion =
                ObtenerBoolQuery(
                    query,
                    "esModoEdicion");

            if (query.TryGetValue(
                    "analisisSueloCalculoIdEdicion",
                    out object? valorId) &&
                int.TryParse(
                    valorId?.ToString(),
                    out int idEdicion))
            {
                AnalisisSueloCalculoIdEdicion =
                    idEdicion;
            }

            if (EsModoEdicion)
            {
                CalcularBalanceFormula =
                    ObtenerBoolQuery(
                        query,
                        "calcularBalanceFormula");

                CalcularEnmiendaCalcarea =
                    ObtenerBoolQuery(
                        query,
                        "calcularEnmiendaCalcarea");

                CalcularFertilizacionMixta =
                    ObtenerBoolQuery(
                        query,
                        "calcularFertilizacionMixta");

                RestaurarSeleccionElementosEdicion();

                string identificador =
                    RequestGuardarAnalisis?
                        .IdentificadorAnalisisSuelo
                    ?? "análisis";

                TituloResultado =
                    $"Editar - {identificador}";

                MensajeSeleccionCalculo =
                    "Los cálculos guardados aparecen seleccionados. Puede conservarlos, quitarlos o actualizar únicamente el requerimiento anual.";
            }

            NotificarElementosIncluidos();
        }

        private void RestaurarSeleccionElementosEdicion()
        {
            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            HashSet<int> elementosUsados =
                new();

            if (contexto
                    .Detalle
                    .BalanceNutricional?
                    .Detalles != null)
            {
                foreach (
                    AnalisisGuardadoFormulaDetalle
                        detalle
                    in contexto
                        .Detalle
                        .BalanceNutricional
                        .Detalles)
                {
                    if (detalle.ElementoQuimicosId > 0)
                    {
                        elementosUsados.Add(
                            detalle.ElementoQuimicosId);
                    }
                }
            }

            if (contexto
                    .Detalle
                    .FertilizacionMixta?
                    .Detalles != null)
            {
                foreach (
                    AnalisisGuardadoMixtaDetalle
                        detalle
                    in contexto
                        .Detalle
                        .FertilizacionMixta
                        .Detalles)
                {
                    if (detalle.ElementoQuimicosId > 0)
                    {
                        elementosUsados.Add(
                            detalle.ElementoQuimicosId);
                    }
                }
            }

            bool tieneCalculoQueUsaElementos =
                contexto.Detalle.BalanceNutricional != null ||
                contexto.Detalle.FertilizacionMixta != null;

            /*
             * Balance y Mixta guardan exactamente los elementos que
             * participaron. Por eso, cuando alguno existe, la selección
             * puede reconstruirse sin agregar una columna nueva:
             * primero se excluyen todos y luego se activan los IDs que
             * aparecen en los detalles guardados.
             *
             * Si el análisis solo tiene requerimiento o enmienda,
             * se conserva la selección predeterminada por clasificación.
             */
            foreach (
                ElementoResultadoCalculoResponse
                    elemento in Elementos)
            {
                if (elemento.ElementoQuimicosId
                    is not int elementoId)
                {
                    continue;
                }

                if (tieneCalculoQueUsaElementos)
                {
                    elemento
                        .IncluirEnCalculosComplementarios =
                        elementosUsados.Contains(
                            elementoId);
                }
                else if (elementosUsados.Contains(
                             elementoId))
                {
                    elemento
                        .IncluirEnCalculosComplementarios =
                        true;
                }
            }

            elementosIncluidosInicialmente.Clear();

            foreach (
                ElementoResultadoCalculoResponse
                    elemento
                in Elementos.Where(x =>
                    x.ElementoQuimicosId.HasValue &&
                    x.IncluirEnCalculosComplementarios))
            {
                elementosIncluidosInicialmente.Add(
                    elemento
                        .ElementoQuimicosId!
                        .Value);
            }

            NotificarElementosIncluidos();
        }

        private void NotificarSeleccion()
        {
            OnPropertyChanged(
                nameof(TieneSeleccionCalculo));

            OnPropertyChanged(
                nameof(TextoSeleccionCalculo));

            OnPropertyChanged(
                nameof(TextoBotonContinuar));

            if (TieneSeleccionCalculo &&
                !EsModoEdicion)
            {
                MensajeSeleccionCalculo =
                    string.Empty;
            }
        }

        private void LimpiarPantallaTemporal()
        {
            SeleccionElementosComplementariosService
                .Limpiar();

            MensajeSeleccionCalculo =
                string.Empty;

            CalcularBalanceFormula = false;
            CalcularEnmiendaCalcarea = false;
            CalcularFertilizacionMixta = false;

            EsModoEdicion = false;

            AnalisisSueloCalculoIdEdicion =
                null;
        }

        private void CargarResultado(
            AnalisisSueloCalculoDataResponse
                resultadoApi)
        {
            Resultado = resultadoApi;

            TituloResultado =
                $"Resultado - " +
                $"{resultadoApi.TipoCultivo}";

            RecomendacionGeneral =
                resultadoApi
                    .RecomendacionGeneral ??
                string.Empty;

            foreach (
                ElementoResultadoCalculoResponse
                    elementoActual
                in Elementos)
            {
                elementoActual.PropertyChanged -=
                    Elemento_PropertyChanged;
            }

            Elementos.Clear();

            foreach (
                ElementoResultadoCalculoResponse
                    elemento
                in resultadoApi.Elementos
                    .OrderByDescending(x =>
                        x.RequerimientoCalculado ??
                        0))
            {
                elemento.PropertyChanged +=
                    Elemento_PropertyChanged;

                Elementos.Add(elemento);
            }

            elementosIncluidosInicialmente.Clear();

            foreach (
                ElementoResultadoCalculoResponse elemento
                in Elementos.Where(x =>
                    x.ElementoQuimicosId.HasValue &&
                    x.IncluirEnCalculosComplementarios))
            {
                elementosIncluidosInicialmente.Add(
                    elemento.ElementoQuimicosId!.Value);
            }

            Observaciones.Clear();

            foreach (
                string observacion
                in resultadoApi.Observaciones)
            {
                Observaciones.Add(observacion);
            }

            TieneElementos =
                Elementos.Count > 0;

            TieneObservaciones =
                Observaciones.Count > 0;

            PhMuyAcido =
                (resultadoApi.Ph ?? 0) < 5.5m;

            DefinirSugerenciaSiguienteCalculo(
                resultadoApi);

            NotificarElementosIncluidos();
        }

        private void DefinirSugerenciaSiguienteCalculo(
            AnalisisSueloCalculoDataResponse
                resultadoApi)
        {
            decimal phActual =
                resultadoApi.Ph ?? 0;

            if (phActual > 0 &&
                phActual < 5.5m)
            {
                SugerenciaSiguienteCalculo =
                    "El pH está muy ácido. Se recomienda revisar la enmienda calcárea.";

                return;
            }

            bool hayDeficiencia =
                resultadoApi.Elementos.Any(
                    elemento =>
                        string.Equals(
                            elemento.Clasificacion,
                            "MUY_BAJO",
                            StringComparison
                                .OrdinalIgnoreCase) ||
                        string.Equals(
                            elemento.Clasificacion,
                            "MEDIO_BAJO",
                            StringComparison
                                .OrdinalIgnoreCase));

            SugerenciaSiguienteCalculo =
                hayDeficiencia
                    ? "Hay elementos con deficiencia. Puede seleccionar balance de fórmula o fertilización mixta."
                    : "Seleccione los cálculos complementarios que desea procesar, o guarde solamente el requerimiento anual.";
        }


        private void Elemento_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                nameof(
                    ElementoResultadoCalculoResponse
                        .IncluirEnCalculosComplementarios))
            {
                NotificarElementosIncluidos();
            }
        }

        private void CambiarSeleccionTodosElementos(
            bool incluir)
        {
            foreach (
                ElementoResultadoCalculoResponse
                    elemento in Elementos)
            {
                elemento
                    .IncluirEnCalculosComplementarios =
                    incluir;
            }

            NotificarElementosIncluidos();
        }

        private void NotificarElementosIncluidos()
        {
            OnPropertyChanged(
                nameof(TotalElementosIncluidos));

            OnPropertyChanged(
                nameof(TextoElementosIncluidos));
        }

        private async Task ProcesarSeleccionAsync()
        {
            if (IsBusy)
                return;

            if (Resultado == null)
            {
                await MostrarMensajeAsync(
                    "Resultado no disponible",
                    "No se encontró el resultado del análisis de suelo.");

                return;
            }

            if (RequestGuardarAnalisis == null)
            {
                await MostrarMensajeAsync(
                    "Datos no disponibles",
                    "No se encontraron los datos originales del análisis.");

                return;
            }

            /*
             * La enmienda calcárea no depende de esta selección.
             * Balance y Mixta sí necesitan al menos un elemento.
             */
            if ((CalcularBalanceFormula ||
                 CalcularFertilizacionMixta) &&
                !Elementos.Any(x =>
                    x.IncluirEnCalculosComplementarios))
            {
                MensajeSeleccionCalculo =
                    "Debe incluir al menos un elemento para realizar Balance de fórmula o Fertilización mixta.";

                await MostrarMensajeAsync(
                    "Elementos no seleccionados",
                    MensajeSeleccionCalculo);

                return;
            }

            if (!TieneSeleccionCalculo)
            {
                await GuardarSoloRequerimientoAsync();
                return;
            }

            await PrepararEdicionSegunSeleccionElementosAsync();

            /*
             * Se conserva el requerimiento anual completo para guardarlo,
             * mientras las pantallas complementarias reciben únicamente
             * los elementos que el usuario decidió incluir.
             */
            SeleccionElementosComplementariosService
                .GuardarRequerimientoCompleto(
                    Resultado,
                    RequestGuardarAnalisis
                        .IdentificadorAnalisisSuelo);

            AnalisisSueloCalculoDataResponse
                resultadoParaComplementarios =
                    SeleccionElementosComplementariosService
                        .CrearResultadoParaCalculosComplementarios(
                            Resultado);

            Dictionary<string, object>
                parametros = new()
                {
                    ["resultadoCalculo"] =
                        resultadoParaComplementarios,

                    ["calcularBalanceFormula"] =
                        CalcularBalanceFormula,

                    ["calcularEnmiendaCalcarea"] =
                        CalcularEnmiendaCalcarea,

                    ["calcularFertilizacionMixta"] =
                        CalcularFertilizacionMixta,

                    ["esModoEdicion"] =
                        EsModoEdicion
                };

            parametros[
                "requestGuardarAnalisis"] =
                    RequestGuardarAnalisis;

            if (RequestGuardarAnalisis
                    .TerrenoId is > 0)
            {
                parametros["terrenoId"] =
                    RequestGuardarAnalisis
                        .TerrenoId.Value;
            }

            if (CantidadPlantas is > 0)
            {
                parametros["cantidadPlantas"] =
                    CantidadPlantas.Value;
            }

            if (EsModoEdicion &&
                AnalisisSueloCalculoIdEdicion
                    is > 0)
            {
                parametros[
                    "analisisSueloCalculoIdEdicion"] =
                        AnalisisSueloCalculoIdEdicion
                            .Value;
            }

            await GoToAsyncParameters(
                "//MultiCalculoPage",
                parametros);
        }

        private async Task
            PrepararEdicionSegunSeleccionElementosAsync()
        {
            if (!EsModoEdicion)
                return;

            HashSet<int> seleccionActual =
                Elementos
                    .Where(x =>
                        x.ElementoQuimicosId.HasValue &&
                        x.IncluirEnCalculosComplementarios)
                    .Select(x =>
                        x.ElementoQuimicosId!.Value)
                    .ToHashSet();

            if (seleccionActual.SetEquals(
                    elementosIncluidosInicialmente))
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            /*
             * El balance y la fertilización guardados fueron
             * calculados con otra selección de elementos.
             * No deben restaurarse como si siguieran vigentes.
             */
            if (contexto.Detalle.BalanceNutricional != null)
            {
                contexto.Detalle.BalanceNutricional =
                    null;

                await CalculoAnalisisTemporalService
                    .Instance
                    .ReiniciarCalculoAsync(
                        TipoCalculoTemporal
                            .BalanceFormula,
                        "La selección de elementos cambió. Debe recalcular el balance.");
            }

            if (contexto.Detalle.FertilizacionMixta != null)
            {
                contexto.Detalle.FertilizacionMixta =
                    null;

                await CalculoAnalisisTemporalService
                    .Instance
                    .ReiniciarCalculoAsync(
                        TipoCalculoTemporal
                            .FertilizacionMixta,
                        "La selección de elementos cambió. Debe recalcular la fertilización mixta.");
            }

            AnalisisEdicionService
                .Instance
                .RestauracionUiRealizada = false;

            MensajeSeleccionCalculo =
                "La selección de elementos cambió. Balance y Fertilización mixta deberán recalcularse antes de actualizar.";
        }

        private async Task
            GuardarSoloRequerimientoAsync()
        {
            if (Resultado == null ||
                RequestGuardarAnalisis == null)
            {
                return;
            }

            string accion =
                EsModoEdicion
                    ? "actualizar"
                    : "guardar";

            bool confirmar =
                await MostrarConfirmacionAsync(
                    EsModoEdicion
                        ? "Actualizar requerimiento anual"
                        : "Guardar requerimiento anual",
                    "No seleccionó cálculos opcionales. " +
                    $"Se procederá a {accion} únicamente " +
                    "el análisis original y su " +
                    "requerimiento anual.",
                    EsModoEdicion
                        ? "Actualizar"
                        : "Guardar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;

                GuardarTodoRequest solicitud =
                    GuardarRequerimientoAnualRequestFactory
                        .Crear(
                            RequestGuardarAnalisis,
                            Resultado);

                GuardarTodoResponse respuesta;

                if (EsModoEdicion)
                {
                    if (AnalisisSueloCalculoIdEdicion
                        is null or <= 0)
                    {
                        throw new
                            InvalidOperationException(
                                "No se encontró el identificador del análisis que se debe actualizar.");
                    }

                    respuesta =
                        await guardarTodoApiService
                            .EditarAsync(
                                AnalisisSueloCalculoIdEdicion
                                    .Value,
                                solicitud);
                }
                else
                {
                    respuesta =
                        await guardarTodoApiService
                            .GuardarAsync(solicitud);
                }

                if (!respuesta.Success)
                {
                    MensajeSeleccionCalculo =
                        string.IsNullOrWhiteSpace(
                            respuesta.Message)
                            ? "No fue posible guardar el requerimiento anual."
                            : respuesta.Message;

                    await MostrarMensajeAsync(
                        EsModoEdicion
                            ? "No se pudo actualizar"
                            : "No se pudo guardar",
                        MensajeSeleccionCalculo);

                    return;
                }

                AnalisisListadoEstadoService
                    .MarcarActualizacionPendiente();

                await CalculoAnalisisTemporalService
                    .Instance
                    .LimpiarTodoAsync();

                bool fueEdicion =
                    EsModoEdicion;

                string mensaje =
                    string.IsNullOrWhiteSpace(
                        respuesta.Message)
                        ? fueEdicion
                            ? "El requerimiento anual fue actualizado correctamente."
                            : "El requerimiento anual fue guardado correctamente."
                        : respuesta.Message;

                await MostrarMensajeAsync(
                    fueEdicion
                        ? "Requerimiento actualizado"
                        : "Requerimiento guardado",
                    mensaje);

                IsBusy = false;

                await OfrecerReporteAsync(
                    respuesta,
                    fueEdicion);

                AnalisisEdicionService
                    .Instance
                    .Limpiar();

                SeleccionElementosComplementariosService
                    .Limpiar();

                await GoToAsyncParameters(
                    "//MainPage");
            }
            catch (Exception ex)
            {
                MensajeSeleccionCalculo =
                    "No fue posible guardar el " +
                    $"requerimiento anual: {ex.Message}";

                await MostrarMensajeAsync(
                    "Error",
                    MensajeSeleccionCalculo);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OfrecerReporteAsync(
            GuardarTodoResponse respuesta,
            bool fueEdicion)
        {
            Page? pagina =
                Application.Current?.MainPage;

            if (pagina == null)
                return;

            string? opcion =
                await pagina.DisplayActionSheet(
                    fueEdicion
                        ? "¿Desea generar el reporte actualizado?"
                        : "¿Desea generar el reporte del requerimiento anual?",
                    "Ahora no",
                    null,
                    "Guardar PDF",
                    "Guardar Excel",
                    "Compartir PDF",
                    "Compartir Excel",
                    "Abrir / imprimir PDF");

            if (string.IsNullOrWhiteSpace(opcion) ||
                string.Equals(
                    opcion,
                    "Ahora no",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int id =
                respuesta.Data?
                    .AnalisisSueloCalculoId ??
                AnalisisSueloCalculoIdEdicion ??
                0;

            if (id <= 0)
            {
                await MostrarMensajeAsync(
                    "Reporte no disponible",
                    "La API no devolvió el identificador del cálculo guardado.");

                return;
            }

            try
            {
                IsBusy = true;

                AnalisisReporteArchivoResult
                    resultadoArchivo =
                        opcion switch
                        {
                            "Guardar PDF" =>
                                await analisisReporteService
                                    .GuardarPdfAsync(id),

                            "Guardar Excel" =>
                                await analisisReporteService
                                    .GuardarExcelAsync(id),

                            "Compartir PDF" =>
                                await analisisReporteService
                                    .CompartirPdfAsync(id),

                            "Compartir Excel" =>
                                await analisisReporteService
                                    .CompartirExcelAsync(id),

                            "Abrir / imprimir PDF" =>
                                await analisisReporteService
                                    .AbrirPdfParaImprimirAsync(
                                        id),

                            _ =>
                                AnalisisReporteArchivoResult
                                    .Cancelado()
                        };

                if (!resultadoArchivo.FueCancelado &&
                    !resultadoArchivo.Success)
                {
                    await MostrarMensajeAsync(
                        "No se pudo generar el reporte",
                        resultadoArchivo.Message);
                }
                else if (
                    !resultadoArchivo.FueCancelado)
                {
                    await MostrarToastAsync(
                        resultadoArchivo.Message);
                }
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync(
                    "Error de reporte",
                    "No fue posible preparar el " +
                    $"reporte: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<string>
            ObtenerCalculosSeleccionadosTexto()
        {
            List<string> seleccionados = new();

            if (CalcularBalanceFormula)
                seleccionados.Add(
                    "Balance de fórmula");

            if (CalcularEnmiendaCalcarea)
                seleccionados.Add(
                    "Enmienda calcárea");

            if (CalcularFertilizacionMixta)
                seleccionados.Add(
                    "Fertilización mixta");

            return seleccionados;
        }

        private async Task VolverAsync()
        {
            if (EsModoEdicion &&
                AnalisisSueloCalculoIdEdicion
                    is > 0)
            {
                await GoToAsyncParameters(
                    AppRoutes
                        .EditarAnalisisGuardado,
                    new Dictionary<string, object>
                    {
                        [
                            "analisisSueloCalculoId"
                        ] =
                            AnalisisSueloCalculoIdEdicion
                                .Value
                    });

                return;
            }

            await GoToAsyncParameters(
                "//NuevoAnalisisFormPage");
        }

        private static bool ObtenerBoolQuery(
            IDictionary<string, object> query,
            string key)
        {
            if (!query.TryGetValue(
                    key,
                    out object? valor))
            {
                return false;
            }

            if (valor is bool booleano)
                return booleano;

            return bool.TryParse(
                       valor?.ToString(),
                       out bool resultado) &&
                   resultado;
        }

        private static async Task<bool>
            MostrarConfirmacionAsync(
                string titulo,
                string mensaje,
                string aceptar)
        {
            if (Application.Current?.MainPage ==
                null)
            {
                return false;
            }

            return await Application
                .Current
                .MainPage
                .DisplayAlert(
                    titulo,
                    mensaje,
                    aceptar,
                    "Cancelar");
        }

        private static async Task
            MostrarMensajeAsync(
                string titulo,
                string mensaje)
        {
            if (Application.Current?.MainPage !=
                null)
            {
                await Application
                    .Current
                    .MainPage
                    .DisplayAlert(
                        titulo,
                        mensaje,
                        "Aceptar");
            }
        }
    }
}
