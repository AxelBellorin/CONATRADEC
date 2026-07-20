using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CONATRADEC.ViewModels
{
    public sealed class AnalisisGuardadoDetalleViewModel :
        GlobalService,
        IQueryAttributable
    {
        private readonly GuardarTodoApiService
            guardarTodoApiService = new();

        private readonly ElementoQuimicoApiService
            elementoQuimicoApiService = new();

        private readonly FuenteNutrienteApiService
            fuenteNutrienteApiService = new();

        private readonly FertilizacionMixtaApiService
            fertilizacionMixtaApiService = new();

        private readonly UnidadMedidaApiService
            unidadMedidaApiService = new();

        private readonly AnalisisReporteService
            analisisReporteService = new();

        private static readonly
            Dictionary<int, (string Nombre, string Simbolo)>
                ElementosBase = new()
                {
                    [1] = ("Potasio", "K"),
                    [2] = ("Calcio", "Ca"),
                    [3] = ("Magnesio", "Mg"),
                    [4] = ("Fósforo", "P"),
                    [5] = ("Nitrógeno", "N")
                };

        private AnalisisGuardadoResumen? resumen;
        private AnalisisGuardadoDetalleData? detalle;

        private FertilizacionMixtaCalculoResponse?
            fertilizacionMixtaRecalculada;

        private string mensaje = string.Empty;

        public AnalisisGuardadoDetalleViewModel()
        {
            VolverCommand = new Command(
                async () =>
                    await GoToAsyncParameters(
                        AppRoutes.Regresar));

            EditarCommand = new Command(
                async () => await EditarAsync(),
                () => !IsBusy &&
                      Detalle != null &&
                      Resumen != null);

            GuardarPdfCommand = new Command(
                async () => await GuardarPdfAsync(),
                PuedeGenerarReporte);

            GuardarExcelCommand = new Command(
                async () => await GuardarExcelAsync(),
                PuedeGenerarReporte);

            ImprimirPdfCommand = new Command(
                async () => await ImprimirPdfAsync(),
                PuedeGenerarReporte);

            CompartirPdfCommand = new Command(
                async () => await CompartirPdfAsync(),
                PuedeGenerarReporte);

            CompartirExcelCommand = new Command(
                async () => await CompartirExcelAsync(),
                PuedeGenerarReporte);
        }

        public AnalisisGuardadoResumen? Resumen
        {
            get => resumen;
            private set
            {
                resumen = value;

                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(ClienteMostrar));
                OnPropertyChanged(nameof(TerrenoMostrar));

                EditarCommand.ChangeCanExecute();
            }
        }

        public AnalisisGuardadoDetalleData? Detalle
        {
            get => detalle;
            private set
            {
                detalle = value;

                OnPropertyChanged(nameof(Detalle));
                OnPropertyChanged(nameof(TieneDetalle));
                OnPropertyChanged(nameof(TieneBalance));
                OnPropertyChanged(nameof(TieneEnmienda));
                OnPropertyChanged(
                    nameof(TieneFertilizacionMixta));

                EditarCommand.ChangeCanExecute();
                ActualizarComandosReporte();
            }
        }

        public FertilizacionMixtaCalculoResponse?
            FertilizacionMixtaRecalculada
        {
            get => fertilizacionMixtaRecalculada;
            private set
            {
                fertilizacionMixtaRecalculada = value;

                OnPropertyChanged(
                    nameof(FertilizacionMixtaRecalculada));

                OnPropertyChanged(
                    nameof(TieneMixtaRecalculada));
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

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool TieneDetalle =>
            Detalle != null;

        public bool TieneBalance =>
            Detalle?.BalanceNutricional != null;

        public bool TieneEnmienda =>
            Detalle?.EnmiendaCalcarea != null;

        public bool TieneFertilizacionMixta =>
            Detalle?.FertilizacionMixta != null;

        public bool TieneMixtaRecalculada =>
            FertilizacionMixtaRecalculada?
                .Detalles?
                .Count > 0;

        public string ClienteMostrar =>
            Resumen?.ClienteMostrar ??
            "Cliente no disponible";

        public string TerrenoMostrar =>
            Resumen?.TerrenoMostrar ??
            "Terreno no disponible";

        public Command VolverCommand { get; }

        public Command EditarCommand { get; }

        public Command GuardarPdfCommand { get; }

        public Command GuardarExcelCommand { get; }

        public Command ImprimirPdfCommand { get; }

        public Command CompartirPdfCommand { get; }

        public Command CompartirExcelCommand { get; }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                EditarCommand.ChangeCanExecute();
                ActualizarComandosReporte();
            }
        }

        public async void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int id = 0;

            if (query.TryGetValue(
                    "analisisSueloCalculoId",
                    out object? valorId))
            {
                int.TryParse(
                    valorId?.ToString(),
                    out id);
            }

            if (query.TryGetValue(
                    "resumenAnalisis",
                    out object? valorResumen))
            {
                Resumen =
                    valorResumen
                    as AnalisisGuardadoResumen;
            }

            await CargarAsync(id);
        }

        private async Task CargarAsync(
            int analisisSueloCalculoId)
        {
            if (analisisSueloCalculoId <= 0 ||
                IsBusy)
            {
                Mensaje =
                    "No se recibió un identificador válido " +
                    "para cargar el análisis.";

                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                Detalle = null;
                FertilizacionMixtaRecalculada = null;

                AnalisisGuardadoDetalleResponse response =
                    await guardarTodoApiService
                        .ObtenerDetalleAsync(
                            analisisSueloCalculoId);

                if (!response.Success ||
                    response.Data == null)
                {
                    Mensaje =
                        string.IsNullOrWhiteSpace(
                            response.Message)
                            ? "No fue posible cargar el análisis."
                            : response.Message;

                    return;
                }

                /*
                 * El detalle se mantiene en una variable local
                 * hasta completar nombres y fuentes.
                 *
                 * Antes se asignaba Detalle primero. BindableLayout
                 * renderizaba "Elemento #X" y los modelos internos no
                 * notificaban el cambio posterior.
                 */
                AnalisisGuardadoDetalleData data =
                    response.Data;

                await CompletarNombresAsync(data);
                await RecalcularFertilizacionMixtaAsync(data);

                // Se entrega a la vista cuando ya contiene los nombres.
                Detalle = data;
            }
            catch (Exception ex)
            {
                Mensaje =
                    $"No fue posible cargar el detalle: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CompletarNombresAsync(
            AnalisisGuardadoDetalleData data)
        {
            Task<ApiResult<
                ObservableCollection<ElementoQuimicoResponse>>>
                    tareaElementos =
                        elementoQuimicoApiService
                            .GetElementoQuimicoResultAsync();

            Task<ApiResult<
                ObservableCollection<FuenteNutrienteResponse>>>
                    tareaFuentes =
                        fuenteNutrienteApiService
                            .GetFuenteNutrienteResultAsync();

            Task<ObservableCollection<
                FuenteNutrienteFertilizacionMixtaResponse>>
                    tareaFuentesMixtas =
                        fertilizacionMixtaApiService
                            .ListarFuentesFertilizacionMixtaAsync();

            Task<ObservableCollection<UnidadMedidaResponse>>
                tareaUnidades =
                    unidadMedidaApiService.GetUnidadMedidaAsync();

            await Task.WhenAll(
                tareaElementos,
                tareaFuentes,
                tareaFuentesMixtas,
                tareaUnidades);

            ApiResult<ObservableCollection<
                ElementoQuimicoResponse>>
                    respuestaElementos =
                        await tareaElementos;

            ApiResult<ObservableCollection<
                FuenteNutrienteResponse>>
                    respuestaFuentes =
                        await tareaFuentes;

            ObservableCollection<
                FuenteNutrienteFertilizacionMixtaResponse>
                    fuentesMixtas =
                        await tareaFuentesMixtas;

            ObservableCollection<UnidadMedidaResponse>
                unidades = await tareaUnidades;

            Dictionary<
                int,
                (string Nombre, string Simbolo)>
                    elementosPorId =
                        new(ElementosBase);

            if (respuestaElementos.Success &&
                respuestaElementos.Data != null)
            {
                foreach (
                    ElementoQuimicoResponse elemento
                    in respuestaElementos.Data.Where(x =>
                        x.ElementoQuimicosId.HasValue &&
                        x.ElementoQuimicosId.Value > 0))
                {
                    elementosPorId[
                        elemento.ElementoQuimicosId!.Value] =
                        (
                            elemento
                                .NombreElementoQuimico ??
                            string.Empty,

                            elemento
                                .SimboloElementoQuimico ??
                            string.Empty
                        );
                }
            }

            Dictionary<int, string>
                fuentesPorId = new();

            if (respuestaFuentes.Success &&
                respuestaFuentes.Data != null)
            {
                foreach (
                    FuenteNutrienteResponse fuente
                    in respuestaFuentes.Data.Where(x =>
                        x.FuenteNutrientesId.HasValue &&
                        x.FuenteNutrientesId.Value > 0))
                {
                    fuentesPorId[
                        fuente.FuenteNutrientesId!.Value] =
                        fuente.NombreNutriente ??
                        string.Empty;
                }
            }

            foreach (
                FuenteNutrienteFertilizacionMixtaResponse
                    fuente
                in fuentesMixtas.Where(x =>
                    x.FuenteNutrientesId.HasValue &&
                    x.FuenteNutrientesId.Value > 0))
            {
                fuentesPorId[
                    fuente.FuenteNutrientesId!.Value] =
                    fuente.NombreNutriente ??
                    string.Empty;
            }

            Dictionary<int, string> unidadesPorId = unidades
                .Where(x =>
                    x.UnidadMedidaId.HasValue &&
                    x.UnidadMedidaId.Value > 0)
                .GroupBy(x => x.UnidadMedidaId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().TextoMostrar);

            foreach (
                AnalisisGuardadoElementoOriginal item
                in data.DatosAnalisis.ElementosQuimicos)
            {
                AsignarElemento(
                    item.ElementoQuimicosId,
                    elementosPorId,
                    out string nombre,
                    out string simbolo);

                item.NombreElemento = nombre;
                item.SimboloElemento = simbolo;

                if (unidadesPorId.TryGetValue(
                        item.UnidadMedidaId,
                        out string? unidad))
                {
                    item.NombreUnidad = unidad;
                }
            }

            foreach (
                AnalisisGuardadoRequerimientoElemento item
                in data.RequerimientoAnual.Elementos)
            {
                AsignarElemento(
                    item.ElementoQuimicosId,
                    elementosPorId,
                    out string nombre,
                    out string simbolo);

                item.NombreElemento = nombre;
                item.SimboloElemento = simbolo;

                if (item.UnidadMedidaId.HasValue &&
                    unidadesPorId.TryGetValue(
                        item.UnidadMedidaId.Value,
                        out string? unidad))
                {
                    item.NombreUnidad = unidad;
                }
            }

            if (data.BalanceNutricional != null)
            {
                foreach (
                    AnalisisGuardadoFormulaDetalle item
                    in data.BalanceNutricional.Detalles)
                {
                    if (fuentesPorId.TryGetValue(
                            item.FuenteNutrientesId,
                            out string? fuente) &&
                        !string.IsNullOrWhiteSpace(fuente))
                    {
                        item.NombreFuente = fuente;
                    }

                    AsignarElemento(
                        item.ElementoQuimicosId,
                        elementosPorId,
                        out string nombre,
                        out string simbolo);

                    item.NombreElemento =
                        FormatearElemento(
                            nombre,
                            simbolo);
                }
            }

            if (data.EnmiendaCalcarea != null &&
                fuentesPorId.TryGetValue(
                    data.EnmiendaCalcarea
                        .FuenteNutrientesId,
                    out string? nombreEnmienda) &&
                !string.IsNullOrWhiteSpace(
                    nombreEnmienda))
            {
                data.EnmiendaCalcarea.NombreFuente =
                    nombreEnmienda;
            }

            if (data.FertilizacionMixta != null)
            {
                foreach (
                    AnalisisGuardadoMixtaFuente item
                    in data.FertilizacionMixta.Fuentes)
                {
                    if (fuentesPorId.TryGetValue(
                            item.FuenteNutrientesId,
                            out string? fuente) &&
                        !string.IsNullOrWhiteSpace(
                            fuente))
                    {
                        item.NombreFuente = fuente;
                    }
                }

                foreach (
                    AnalisisGuardadoMixtaDetalle item
                    in data.FertilizacionMixta.Detalles)
                {
                    AsignarElemento(
                        item.ElementoQuimicosId,
                        elementosPorId,
                        out string nombre,
                        out string simbolo);

                    item.NombreElemento =
                        FormatearElemento(
                            nombre,
                            simbolo);
                }
            }
        }

        private async Task
            RecalcularFertilizacionMixtaAsync(
                AnalisisGuardadoDetalleData data)
        {
            AnalisisGuardadoFertilizacionMixta?
                mixta = data.FertilizacionMixta;

            if (mixta == null ||
                mixta.Fuentes.Count == 0 ||
                mixta.Detalles.Count == 0)
            {
                return;
            }

            FertilizacionMixtaCalcularRequest request =
                new()
                {
                    Observacion =
                        mixta.Mixta.Observacion,

                    Elementos =
                        mixta.Detalles
                            .Select(x =>
                                new ElementoFertilizacionMixtaRequest
                                {
                                    ElementoQuimicosId =
                                        x.ElementoQuimicosId,

                                    Exportable =
                                        x.RequerimientoOriginal
                                })
                            .ToList(),

                    Fuentes =
                        mixta.Fuentes
                            .Select(x =>
                                new FuenteFertilizacionMixtaRequest
                                {
                                    FuenteNutrientesId =
                                        x.FuenteNutrientesId,

                                    CantidadQq =
                                        x.CantidadQq
                                })
                            .ToList()
                };

            FertilizacionMixtaCalculoResponse? resultado =
                await fertilizacionMixtaApiService
                    .CalcularAsync(request);

            if (resultado?.Success == true)
            {
                FertilizacionMixtaRecalculada =
                    resultado;

                foreach (
                    AnalisisGuardadoMixtaFuente
                        fuenteGuardada
                    in mixta.Fuentes)
                {
                    FuenteFertilizacionMixtaResultadoResponse?
                        fuenteApi =
                            resultado.Fuentes
                                .FirstOrDefault(x =>
                                    x.FuenteNutrientesId ==
                                    fuenteGuardada
                                        .FuenteNutrientesId);

                    if (!string.IsNullOrWhiteSpace(
                            fuenteApi?.NombreFuente))
                    {
                        fuenteGuardada.NombreFuente =
                            fuenteApi.NombreFuente!;
                    }
                }
            }
            else if (
                resultado != null &&
                !string.IsNullOrWhiteSpace(
                    resultado.Message))
            {
                Mensaje = resultado.Message;
            }
        }

        private static void AsignarElemento(
            int elementoId,
            IReadOnlyDictionary<
                int,
                (string Nombre, string Simbolo)>
                    elementos,
            out string nombre,
            out string simbolo)
        {
            if (elementos.TryGetValue(
                    elementoId,
                    out (
                        string Nombre,
                        string Simbolo
                    ) elemento))
            {
                nombre = elemento.Nombre;
                simbolo = elemento.Simbolo;
                return;
            }

            nombre = $"Elemento #{elementoId}";
            simbolo = string.Empty;
        }

        private static string FormatearElemento(
            string nombre,
            string simbolo)
        {
            if (!string.IsNullOrWhiteSpace(nombre) &&
                !string.IsNullOrWhiteSpace(simbolo))
            {
                return $"{nombre} ({simbolo})";
            }

            return
                !string.IsNullOrWhiteSpace(nombre)
                    ? nombre
                    : simbolo;
        }

        private bool PuedeGenerarReporte() =>
            !IsBusy && Detalle != null;

        private void ActualizarComandosReporte()
        {
            GuardarPdfCommand?.ChangeCanExecute();
            GuardarExcelCommand?.ChangeCanExecute();
            ImprimirPdfCommand?.ChangeCanExecute();
            CompartirPdfCommand?.ChangeCanExecute();
            CompartirExcelCommand?.ChangeCanExecute();
        }

        private async Task GuardarPdfAsync()
        {
            if (!PuedeGenerarReporte())
                return;

            await EjecutarReporteAsync(
                "Descargando el reporte PDF...",
                () => analisisReporteService.GuardarPdfAsync(
                    ObtenerAnalisisSueloCalculoId()));
        }

        private async Task GuardarExcelAsync()
        {
            if (!PuedeGenerarReporte())
                return;

            await EjecutarReporteAsync(
                "Generando el archivo Excel...",
                () => analisisReporteService.GuardarExcelAsync(
                    ObtenerAnalisisSueloCalculoId()));
        }

        private async Task ImprimirPdfAsync()
        {
            if (!PuedeGenerarReporte())
                return;

            await EjecutarReporteAsync(
                "Preparando el PDF para imprimir...",
                () => analisisReporteService
                    .AbrirPdfParaImprimirAsync(
                        ObtenerAnalisisSueloCalculoId()));
        }

        private async Task CompartirPdfAsync()
        {
            if (!PuedeGenerarReporte())
                return;

            await EjecutarReporteAsync(
                "Preparando el PDF para compartir...",
                () => analisisReporteService.CompartirPdfAsync(
                    ObtenerAnalisisSueloCalculoId()));
        }

        private async Task CompartirExcelAsync()
        {
            if (!PuedeGenerarReporte())
                return;

            await EjecutarReporteAsync(
                "Preparando el Excel para compartir...",
                () => analisisReporteService.CompartirExcelAsync(
                    ObtenerAnalisisSueloCalculoId()));
        }

        private async Task EjecutarReporteAsync(
            string mensajeProceso,
            Func<Task<AnalisisReporteArchivoResult>> accion)
        {
            string mensajeAnterior = Mensaje;

            try
            {
                IsBusy = true;
                Mensaje = mensajeProceso;

                AnalisisReporteArchivoResult resultado = await accion();

                if (resultado.FueCancelado)
                    return;

                if (!resultado.Success)
                {
                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "No se pudo generar el reporte",
                            resultado.Message,
                            "Aceptar");
                    return;
                }

                await MostrarToastAsync(resultado.Message);
            }
            catch (Exception ex)
            {
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Error de reporte",
                        ex.Message,
                        "Aceptar");
            }
            finally
            {
                Mensaje = mensajeAnterior;
                IsBusy = false;
            }
        }

        private int ObtenerAnalisisSueloCalculoId()
        {
            int id = Detalle?.RequerimientoAnual
                .AnalisisSueloCalculoId ??
                Resumen?.AnalisisSueloCalculoId ??
                0;

            if (id <= 0)
            {
                throw new InvalidOperationException(
                    "No se encontró el identificador del cálculo guardado.");
            }

            return id;
        }

        private async Task EditarAsync()
        {
            if (Detalle == null ||
                Resumen == null ||
                IsBusy)
            {
                return;
            }

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar análisis.");

                return;
            }

            try
            {
                IsBusy = true;
                Mensaje =
                    "Cargando el análisis para edición...";

                (bool success, string message) =
                    await AnalisisEdicionService.Instance
                        .PrepararAsync(
                            Resumen
                                .AnalisisSueloCalculoId,
                            Resumen);

                if (!success)
                {
                    Mensaje = message;

                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "No se pudo abrir",
                            message,
                            "Aceptar");

                    return;
                }

                Mensaje = string.Empty;

                await GoToAsyncParameters(
                    "//NuevoAnalisisFormPage");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
