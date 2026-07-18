using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public sealed class EditarAnalisisGuardadoViewModel : GlobalService, IQueryAttributable
    {
        private readonly GuardarTodoApiService guardarTodoApiService = new();
        private readonly TerrenoApiService terrenoApiService = new();
        private readonly AnalisisSueloApiService analisisSueloApiService = new();
        private readonly UnidadMedidaApiService unidadMedidaApiService = new();
        private readonly ElementoQuimicoApiService elementoQuimicoApiService = new();

        private int analisisSueloCalculoId;
        private AnalisisGuardadoResumen? resumen;
        private AnalisisGuardadoDetalleData? detalle;

        private TerrenoResponse? terrenoSeleccionado;
        private TipoCultivoResponse? tipoCultivoSeleccionado;
        private UnidadMedidaResponse? unidadMateriaOrganicaSeleccionada;

        private DateTime fechaAnalisisLaboratorio = DateTime.Today;
        private string laboratorio = string.Empty;
        private string identificadorAnalisisSuelo = string.Empty;
        private string cantidadQuintalesOro = string.Empty;
        private string tamanoFinca = string.Empty;
        private string cantidadPlantas = string.Empty;
        private string ph = string.Empty;
        private string materiaOrganica = string.Empty;
        private string acidezTotal = string.Empty;
        private string calcioCice = string.Empty;
        private string magnesioCice = string.Empty;
        private string potasioCice = string.Empty;
        private string mensaje = string.Empty;

        private bool teniaBalanceFormula;
        private bool teniaEnmiendaCalcarea;
        private bool teniaFertilizacionMixta;

        public EditarAnalisisGuardadoViewModel()
        {
            Terrenos = new ObservableCollection<TerrenoResponse>();
            TiposCultivo = new ObservableCollection<TipoCultivoResponse>();
            UnidadesMedida = new ObservableCollection<UnidadMedidaResponse>();
            ElementosQuimicos = new ObservableCollection<EditarElementoAnalisisItem>();

            RecalcularCommand = new Command(
                async () => await RecalcularAsync(),
                () => !IsBusy);

            CancelarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Principal),
                () => !IsBusy);

            QuitarElementoCommand = new Command<EditarElementoAnalisisItem>(
                QuitarElemento,
                item => !IsBusy && item != null);
        }

        public ObservableCollection<TerrenoResponse> Terrenos { get; }
        public ObservableCollection<TipoCultivoResponse> TiposCultivo { get; }
        public ObservableCollection<UnidadMedidaResponse> UnidadesMedida { get; }
        public ObservableCollection<EditarElementoAnalisisItem> ElementosQuimicos { get; }

        public Command RecalcularCommand { get; }
        public Command CancelarCommand { get; }
        public Command<EditarElementoAnalisisItem> QuitarElementoCommand { get; }

        public AnalisisGuardadoResumen? Resumen
        {
            get => resumen;
            private set
            {
                resumen = value;
                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(TituloPantalla));
            }
        }

        public string TituloPantalla =>
            Resumen == null
                ? "Editar análisis"
                : $"Editar {Resumen.IdentificadorMostrar}";

        public TerrenoResponse? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            set
            {
                terrenoSeleccionado = value;
                OnPropertyChanged(nameof(TerrenoSeleccionado));
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));

                if (value != null)
                {
                    if (string.IsNullOrWhiteSpace(CantidadPlantas))
                    {
                        CantidadPlantas = value.CantidadPlantasTerreno?
                            .ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                    }
                }
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
            }
        }

        public UnidadMedidaResponse? UnidadMateriaOrganicaSeleccionada
        {
            get => unidadMateriaOrganicaSeleccionada;
            set
            {
                unidadMateriaOrganicaSeleccionada = value;
                OnPropertyChanged(nameof(UnidadMateriaOrganicaSeleccionada));
            }
        }

        public DateTime FechaAnalisisLaboratorio
        {
            get => fechaAnalisisLaboratorio;
            set
            {
                fechaAnalisisLaboratorio = value;
                OnPropertyChanged(nameof(FechaAnalisisLaboratorio));
            }
        }

        public string Laboratorio
        {
            get => laboratorio;
            set
            {
                laboratorio = value ?? string.Empty;
                OnPropertyChanged(nameof(Laboratorio));
            }
        }

        public string IdentificadorAnalisisSuelo
        {
            get => identificadorAnalisisSuelo;
            set
            {
                identificadorAnalisisSuelo = value ?? string.Empty;
                OnPropertyChanged(nameof(IdentificadorAnalisisSuelo));
            }
        }

        public string CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                cantidadQuintalesOro = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(CantidadQuintalesOro));
            }
        }

        public string TamanoFinca
        {
            get => tamanoFinca;
            set
            {
                tamanoFinca = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(TamanoFinca));
            }
        }

        public string CantidadPlantas
        {
            get => cantidadPlantas;
            set
            {
                cantidadPlantas = NormalizarEntradaEntera(value);
                OnPropertyChanged(nameof(CantidadPlantas));
            }
        }

        public string Ph
        {
            get => ph;
            set
            {
                ph = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(Ph));
            }
        }

        public string MateriaOrganica
        {
            get => materiaOrganica;
            set
            {
                materiaOrganica = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(MateriaOrganica));
            }
        }

        public string AcidezTotal
        {
            get => acidezTotal;
            set
            {
                acidezTotal = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(AcidezTotal));
            }
        }

        public string CalcioCice
        {
            get => calcioCice;
            set
            {
                calcioCice = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(CalcioCice));
            }
        }

        public string MagnesioCice
        {
            get => magnesioCice;
            set
            {
                magnesioCice = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(MagnesioCice));
            }
        }

        public string PotasioCice
        {
            get => potasioCice;
            set
            {
                potasioCice = NormalizarEntradaDecimal(value);
                OnPropertyChanged(nameof(PotasioCice));
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        public bool TeniaBalanceFormula
        {
            get => teniaBalanceFormula;
            private set
            {
                teniaBalanceFormula = value;
                OnPropertyChanged(nameof(TeniaBalanceFormula));
            }
        }

        public bool TeniaEnmiendaCalcarea
        {
            get => teniaEnmiendaCalcarea;
            private set
            {
                teniaEnmiendaCalcarea = value;
                OnPropertyChanged(nameof(TeniaEnmiendaCalcarea));
            }
        }

        public bool TeniaFertilizacionMixta
        {
            get => teniaFertilizacionMixta;
            private set
            {
                teniaFertilizacionMixta = value;
                OnPropertyChanged(nameof(TeniaFertilizacionMixta));
            }
        }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                RecalcularCommand.ChangeCanExecute();
                CancelarCommand.ChangeCanExecute();
                QuitarElementoCommand.ChangeCanExecute();
            }
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            int id = 0;

            if (query.TryGetValue("analisisSueloCalculoId", out object? valorId))
                int.TryParse(valorId?.ToString(), out id);

            if (query.TryGetValue("resumenAnalisis", out object? valorResumen))
                Resumen = valorResumen as AnalisisGuardadoResumen;

            await CargarAsync(id);
        }

        private async Task CargarAsync(int id)
        {
            if (id <= 0 || IsBusy)
            {
                Mensaje = "No se recibió un identificador válido para editar.";
                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                analisisSueloCalculoId = id;

                Task<AnalisisGuardadoDetalleResponse> tareaDetalle =
                    guardarTodoApiService.ObtenerDetalleAsync(id);

                Task<ObservableCollection<TerrenoResponse>> tareaTerrenos =
                    terrenoApiService.GetTerrenosAsync();

                Task<ObservableCollection<TipoCultivoResponse>> tareaCultivos =
                    analisisSueloApiService.ListarTiposCultivoAsync();

                Task<ObservableCollection<UnidadMedidaResponse>> tareaUnidades =
                    unidadMedidaApiService.GetUnidadMedidaAsync();

                Task<ObservableCollection<ElementoQuimicoResponse>> tareaElementos =
                    elementoQuimicoApiService.GetElementoQuimicoAsync();

                await Task.WhenAll(
                    tareaDetalle,
                    tareaTerrenos,
                    tareaCultivos,
                    tareaUnidades,
                    tareaElementos);

                AnalisisGuardadoDetalleResponse respuestaDetalle =
                    await tareaDetalle;

                if (!respuestaDetalle.Success || respuestaDetalle.Data == null)
                {
                    Mensaje = string.IsNullOrWhiteSpace(respuestaDetalle.Message)
                        ? "No fue posible cargar el análisis que se debe editar."
                        : respuestaDetalle.Message;
                    return;
                }

                detalle = respuestaDetalle.Data;

                CargarColeccion(Terrenos, await tareaTerrenos);
                CargarColeccion(
                    TiposCultivo,
                    (await tareaCultivos).Where(x => x.Activo != false));
                CargarColeccion(
                    UnidadesMedida,
                    (await tareaUnidades).Where(x => x.Activo != false));

                ObservableCollection<ElementoQuimicoResponse> catalogoElementos =
                    await tareaElementos;

                CargarDatosGenerales(detalle);
                CargarElementosOriginales(detalle, catalogoElementos);

                TeniaBalanceFormula = detalle.BalanceNutricional != null;
                TeniaEnmiendaCalcarea = detalle.EnmiendaCalcarea != null;
                TeniaFertilizacionMixta = detalle.FertilizacionMixta != null;
            }
            catch (Exception ex)
            {
                Mensaje = $"No fue posible cargar la edición: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CargarDatosGenerales(AnalisisGuardadoDetalleData data)
        {
            AnalisisGuardadoDatosAnalisis datos = data.DatosAnalisis;
            AnalisisGuardadoRequerimientoAnual anual = data.RequerimientoAnual;

            TerrenoSeleccionado = Terrenos.FirstOrDefault(x =>
                x.TerrenoId == anual.TerrenoId);

            TipoCultivoSeleccionado = TiposCultivo.FirstOrDefault(x =>
                x.TipoCultivoId == anual.TipoCultivoId);

            FechaAnalisisLaboratorio = datos.FechaAnalisisValor ?? DateTime.Today;
            Laboratorio = datos.LaboratorioAnalasisSuelo;
            IdentificadorAnalisisSuelo = datos.IdentificadorAnalisisSuelo;
            CantidadQuintalesOro = FormatearDecimal(anual.CantidadQuintalesOro);
            TamanoFinca = FormatearDecimal(anual.TamanoFinca);
            Ph = FormatearDecimal(anual.Ph);
            MateriaOrganica = FormatearDecimal(anual.MateriaOrganica ?? 0);
            AcidezTotal = FormatearDecimal(anual.AcidezTotal ?? 0);

            UnidadMateriaOrganicaSeleccionada = UnidadesMedida.FirstOrDefault(x =>
                x.UnidadMedidaId == anual.UnidadMedidaMateriaOrganicaId)
                ?? UnidadesMedida.FirstOrDefault();

            AnalisisGuardadoEnmiendaCalcarea? enmienda = data.EnmiendaCalcarea;

            CalcioCice = enmienda == null
                ? string.Empty
                : FormatearDecimal(enmienda.Ca);

            MagnesioCice = enmienda == null
                ? string.Empty
                : FormatearDecimal(enmienda.Mg);

            PotasioCice = enmienda == null
                ? string.Empty
                : FormatearDecimal(enmienda.K);

            int plantas = 0;

            if (enmienda?.TotalPlantas > 0)
                plantas = enmienda.TotalPlantas;
            else if (data.BalanceNutricional?.Formula.TotalPlantas > 0)
                plantas = data.BalanceNutricional.Formula.TotalPlantas;
            else if (TerrenoSeleccionado?.CantidadPlantasTerreno > 0)
                plantas = TerrenoSeleccionado.CantidadPlantasTerreno.Value;

            CantidadPlantas = plantas > 0
                ? plantas.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private void CargarElementosOriginales(
            AnalisisGuardadoDetalleData data,
            IEnumerable<ElementoQuimicoResponse> catalogoElementos)
        {
            Dictionary<int, ElementoQuimicoResponse> catalogoPorId =
                catalogoElementos
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            ElementosQuimicos.Clear();

            foreach (AnalisisGuardadoElementoOriginal original in
                     data.DatosAnalisis.ElementosQuimicos)
            {
                catalogoPorId.TryGetValue(
                    original.ElementoQuimicosId,
                    out ElementoQuimicoResponse? catalogo);

                ObservableCollection<UnidadMedidaResponse> unidades =
                    new(UnidadesMedida);

                ElementosQuimicos.Add(new EditarElementoAnalisisItem
                {
                    ElementoQuimicosId = original.ElementoQuimicosId,
                    NombreElemento = catalogo?.NombreElementoQuimico
                        ?? $"Elemento #{original.ElementoQuimicosId}",
                    SimboloElemento = catalogo?.SimboloElementoQuimico
                        ?? string.Empty,
                    Valor = FormatearDecimal(original.CantidadElemento),
                    UnidadesMedida = unidades,
                    UnidadSeleccionada = unidades.FirstOrDefault(x =>
                        x.UnidadMedidaId == original.UnidadMedidaId)
                        ?? unidades.FirstOrDefault()
                });
            }
        }

        private void QuitarElemento(EditarElementoAnalisisItem? item)
        {
            if (item == null || IsBusy)
                return;

            ElementosQuimicos.Remove(item);
        }

        private async Task RecalcularAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarFormulario(out string error))
            {
                Mensaje = error;
                await MostrarAlertaAsync("Validación", error);
                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = "Recalculando el requerimiento anual...";

                int usuarioId = detalle?.DatosAnalisis.UsuarioId
                    ?? ObtenerUsuarioActual();

                int tipoAnalisisSueloId =
                    detalle?.RequerimientoAnual.TipoAnalisisSueloId ?? 1;

                decimal materiaOrganicaValor = ConvertirDecimal(MateriaOrganica);
                decimal acidezTotalValor = ConvertirDecimal(AcidezTotal);
                decimal calcio = ConvertirDecimalOpcional(CalcioCice);
                decimal magnesio = ConvertirDecimalOpcional(MagnesioCice);
                decimal potasio = ConvertirDecimalOpcional(PotasioCice);

                List<ElementoQuimicoAnalisisRequest> elementosRequest =
                    ElementosQuimicos.Select(x =>
                        new ElementoQuimicoAnalisisRequest
                        {
                            ElementoQuimicosId = x.ElementoQuimicosId,
                            UnidadMedidaId = x.UnidadSeleccionada?.UnidadMedidaId,
                            CantidadElemento = ConvertirDecimal(x.Valor)
                        })
                    .ToList();

                AnalisisSueloCalcularRequest calcularRequest = new()
                {
                    TerrenoId = TerrenoSeleccionado!.TerrenoId,
                    TipoCultivoId = TipoCultivoSeleccionado!.TipoCultivoId,
                    TipoAnalisisSueloId = tipoAnalisisSueloId,
                    UsuarioId = usuarioId,
                    CantidadQuintalesOro = ConvertirDecimal(CantidadQuintalesOro),
                    TamanoFinca = ConvertirDecimal(TamanoFinca),
                    Ph = ConvertirDecimal(Ph),
                    MateriaOrganica = materiaOrganicaValor,
                    UnidadMedidaMateriaOrganicaId =
                        UnidadMateriaOrganicaSeleccionada!.UnidadMedidaId,
                    AcidezTotal = acidezTotalValor,
                    CalcioCice = calcio,
                    MagnesioCice = magnesio,
                    PotasioCice = potasio,
                    ElementosQuimicos = elementosRequest,
                    FuentesOrganicas = new List<FuenteOrganicaAnalisisRequest>()
                };

                AnalisisSueloGuardarCalculoRequest guardarRequest = new()
                {
                    TerrenoId = calcularRequest.TerrenoId,
                    TipoCultivoId = calcularRequest.TipoCultivoId,
                    TipoAnalisisSueloId = calcularRequest.TipoAnalisisSueloId,
                    UsuarioId = calcularRequest.UsuarioId,
                    CantidadQuintalesOro = calcularRequest.CantidadQuintalesOro,
                    TamanoFinca = calcularRequest.TamanoFinca,
                    Ph = calcularRequest.Ph,
                    MateriaOrganica = calcularRequest.MateriaOrganica,
                    UnidadMedidaMateriaOrganicaId =
                        calcularRequest.UnidadMedidaMateriaOrganicaId,
                    AcidezTotal = calcularRequest.AcidezTotal,
                    CalcioCice = calcularRequest.CalcioCice,
                    MagnesioCice = calcularRequest.MagnesioCice,
                    PotasioCice = calcularRequest.PotasioCice,
                    ElementosQuimicos = elementosRequest,
                    FuentesOrganicas = new List<FuenteOrganicaAnalisisRequest>(),
                    FechaAnalisisSuelo = FechaAnalisisLaboratorio.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),
                    LaboratorioAnalasisSuelo = Laboratorio.Trim(),
                    IdentificadorAnalisisSuelo =
                        IdentificadorAnalisisSuelo.Trim()
                };

                AnalisisSueloCalculoResponse? respuesta =
                    await analisisSueloApiService.CalcularAsync(calcularRequest);

                if (respuesta?.Success != true || respuesta.Data == null)
                {
                    Mensaje = respuesta?.Message
                        ?? "La API no devolvió un requerimiento anual válido.";

                    await MostrarAlertaAsync(
                        "No se pudo recalcular",
                        Mensaje);
                    return;
                }

                await CalculoAnalisisTemporalService.Instance.LimpiarTodoAsync();

                Dictionary<string, object> parametros = new()
                {
                    ["resultadoCalculo"] = respuesta.Data,
                    ["requestGuardarAnalisis"] = guardarRequest,
                    ["cantidadPlantas"] = ConvertirEntero(CantidadPlantas),
                    ["esModoEdicion"] = true,
                    ["analisisSueloCalculoIdEdicion"] =
                        analisisSueloCalculoId,
                    ["calcularBalanceFormula"] = TeniaBalanceFormula,
                    ["calcularEnmiendaCalcarea"] = TeniaEnmiendaCalcarea,
                    ["calcularFertilizacionMixta"] =
                        TeniaFertilizacionMixta
                };

                await GoToAsyncParameters(
                    "//ResultadoAnalisisSueloPage",
                    parametros);
            }
            catch (Exception ex)
            {
                Mensaje = $"No fue posible recalcular el análisis: {ex.Message}";
                await MostrarAlertaAsync("Error", Mensaje);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ValidarFormulario(out string error)
        {
            error = string.Empty;

            if (TerrenoSeleccionado?.TerrenoId is null or <= 0)
                error = "Debe seleccionar el cliente y terreno.";
            else if (TipoCultivoSeleccionado?.TipoCultivoId is null or <= 0)
                error = "Debe seleccionar el tipo de cultivo.";
            else if (FechaAnalisisLaboratorio.Date > DateTime.Today)
                error = "La fecha del análisis no puede ser futura.";
            else if (string.IsNullOrWhiteSpace(Laboratorio))
                error = "Debe ingresar el laboratorio.";
            else if (string.IsNullOrWhiteSpace(IdentificadorAnalisisSuelo))
                error = "Debe ingresar el identificador del análisis.";
            else if (!EsDecimalPositivo(CantidadQuintalesOro))
                error = "La cantidad de quintales oro debe ser mayor que cero.";
            else if (!EsDecimalPositivo(TamanoFinca))
                error = "El tamaño de la finca debe ser mayor que cero.";
            else if (!EsEnteroPositivo(CantidadPlantas))
                error = "La cantidad de plantas debe ser un entero mayor que cero.";
            else if (!EsDecimalEnRango(Ph, 0, 14))
                error = "El pH debe estar entre 0 y 14.";
            else if (!EsDecimalNoNegativo(MateriaOrganica))
                error = "La materia orgánica debe ser un número no negativo.";
            else if (UnidadMateriaOrganicaSeleccionada?.UnidadMedidaId is null or <= 0)
                error = "Debe seleccionar la unidad de materia orgánica.";
            else if (!EsDecimalNoNegativo(AcidezTotal))
                error = "La acidez total debe ser un número no negativo.";
            else if (!EsDecimalOpcionalNoNegativo(CalcioCice) ||
                     !EsDecimalOpcionalNoNegativo(MagnesioCice) ||
                     !EsDecimalOpcionalNoNegativo(PotasioCice))
                error = "Los valores CICE deben ser números no negativos.";
            else if (ElementosQuimicos.Count == 0)
                error = "Debe conservar al menos un elemento químico.";
            else if (ElementosQuimicos.Any(x =>
                         x.ElementoQuimicosId <= 0 ||
                         x.UnidadSeleccionada?.UnidadMedidaId is null or <= 0 ||
                         !EsDecimalNoNegativo(x.Valor)))
                error = "Revise el valor y la unidad de cada elemento químico.";

            return string.IsNullOrWhiteSpace(error);
        }

        private static void CargarColeccion<T>(
            ObservableCollection<T> destino,
            IEnumerable<T> origen)
        {
            destino.Clear();

            foreach (T item in origen)
                destino.Add(item);
        }

        private static int ObtenerUsuarioActual()
        {
            string valor = Preferences.Get(SessionKeys.KeyUserId, "0");
            return int.TryParse(valor, out int id) ? id : 0;
        }

        private static string NormalizarEntradaDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto = new string(valor
                .Where(x => char.IsDigit(x) || x == '.' || x == ',')
                .ToArray());

            int primerSeparador = texto.IndexOfAny(new[] { '.', ',' });

            if (primerSeparador < 0)
                return texto;

            string entero = texto[..primerSeparador];
            string decimales = new string(texto[(primerSeparador + 1)..]
                .Where(char.IsDigit)
                .ToArray());

            return $"{entero}.{decimales}";
        }

        private static string NormalizarEntradaEntera(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : new string(valor.Where(char.IsDigit).ToArray());

        private static decimal ConvertirDecimal(string? valor)
        {
            string texto = (valor ?? string.Empty).Replace(',', '.');

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal resultado))
            {
                return resultado;
            }

            return 0;
        }

        private static decimal ConvertirDecimalOpcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? 0
                : ConvertirDecimal(valor);

        private static int ConvertirEntero(string? valor) =>
            int.TryParse(valor, out int resultado) ? resultado : 0;

        private static bool EsDecimalPositivo(string? valor) =>
            ConvertirDecimal(valor) > 0;

        private static bool EsDecimalNoNegativo(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            string texto = valor.Replace(',', '.');

            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out decimal resultado) &&
                   resultado >= 0;
        }

        private static bool EsDecimalOpcionalNoNegativo(string? valor) =>
            string.IsNullOrWhiteSpace(valor) || EsDecimalNoNegativo(valor);

        private static bool EsDecimalEnRango(
            string? valor,
            decimal minimo,
            decimal maximo)
        {
            decimal numero = ConvertirDecimal(valor);
            return numero >= minimo && numero <= maximo;
        }

        private static bool EsEnteroPositivo(string? valor) =>
            int.TryParse(valor, out int numero) && numero > 0;

        private static string FormatearDecimal(decimal valor) =>
            valor.ToString("0.####", CultureInfo.InvariantCulture);

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
    }

    public sealed class EditarElementoAnalisisItem : GlobalService
    {
        private string valor = string.Empty;
        private UnidadMedidaResponse? unidadSeleccionada;

        public int ElementoQuimicosId { get; set; }
        public string NombreElemento { get; set; } = string.Empty;
        public string SimboloElemento { get; set; } = string.Empty;

        public string NombreMostrar =>
            string.IsNullOrWhiteSpace(SimboloElemento)
                ? NombreElemento
                : $"{NombreElemento} ({SimboloElemento})";

        public string Valor
        {
            get => valor;
            set
            {
                valor = value ?? string.Empty;
                OnPropertyChanged(nameof(Valor));
            }
        }

        public ObservableCollection<UnidadMedidaResponse> UnidadesMedida { get; set; }
            = new();

        public UnidadMedidaResponse? UnidadSeleccionada
        {
            get => unidadSeleccionada;
            set
            {
                unidadSeleccionada = value;
                OnPropertyChanged(nameof(UnidadSeleccionada));
            }
        }
    }
}
