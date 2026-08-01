using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Conserva una referencia inmutable a los cálculos que llegaron desde la
    /// API o desde SQLite al comenzar una edición.
    ///
    /// Durante la navegación Resultado -> MultiCálculo existe una validación
    /// que puede interpretar incorrectamente que cambió la selección de
    /// elementos y colocar BalanceNutricional y FertilizacionMixta en null.
    ///
    /// Cuando eso ocurre, las pestañas se dibujan correctamente, pero aparecen
    /// como cálculos nuevos:
    /// - fuentes del Balance vacías;
    /// - complemento desactivado;
    /// - Pulpa y Gallinaza sin seleccionar;
    /// - cantidades y resultados de Mixta vacíos.
    ///
    /// Este servicio guarda la referencia original antes de esa validación y
    /// solo la recupera cuando:
    /// 1. Es el mismo análisis.
    /// 2. El requerimiento no cambió realmente.
    /// 3. La selección actual de elementos coincide con la guardada.
    ///
    /// Por tanto, no restaura cálculos viejos cuando el usuario sí modificó el
    /// análisis o cambió los elementos que participarán.
    /// </summary>
    public sealed class
        AnalisisEdicionRestauracionRefuerzoService
    {
        private static readonly Lazy<
            AnalisisEdicionRestauracionRefuerzoService>
            instancia =
                new(() =>
                    new
                        AnalisisEdicionRestauracionRefuerzoService());

        private Shell? shellVinculado;

        private CancellationTokenSource?
            navegacionCts;

        private SnapshotEdicion?
            snapshotActual;

        private AnalisisEdicionRestauracionRefuerzoService()
        {
        }

        public static
            AnalisisEdicionRestauracionRefuerzoService
            Instance =>
                instancia.Value;

        public void VincularShell(
            Shell shell)
        {
            ArgumentNullException.ThrowIfNull(
                shell);

            if (ReferenceEquals(
                    shellVinculado,
                    shell))
            {
                CapturarSnapshotActual();
                return;
            }

            if (shellVinculado != null)
            {
                shellVinculado.Navigated -=
                    Shell_Navigated;
            }

            shellVinculado =
                shell;

            shellVinculado.Navigated +=
                Shell_Navigated;

            CapturarSnapshotActual();
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            /*
             * PrepararAsync crea ContextoActual antes de navegar al formulario.
             * La primera navegación de la edición permite capturar Balance,
             * Enmienda y Mixta antes de que otra pantalla pueda alterarlos.
             */
            CapturarSnapshotActual();
            NormalizarLineaBaseEdicion();

            CancellationTokenSource nueva =
                new();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref navegacionCts,
                    nueva);

            CancelarSeguro(
                anterior);

            _ = ProcesarPaginaActualAsync(
                nueva.Token);
        }

        private void CapturarSnapshotActual()
        {
            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
            {
                snapshotActual = null;
                return;
            }

            if (snapshotActual != null &&
                snapshotActual
                    .AnalisisSueloCalculoId ==
                contexto
                    .AnalisisSueloCalculoId)
            {
                /*
                 * Si el snapshot se creó cuando alguna sección todavía no
                 * estaba disponible, se completa sin reemplazar las referencias
                 * ya capturadas.
                 */
                snapshotActual.Balance ??=
                    contexto
                        .Detalle
                        .BalanceNutricional;

                snapshotActual.Enmienda ??=
                    contexto
                        .Detalle
                        .EnmiendaCalcarea;

                snapshotActual.Mixta ??=
                    contexto
                        .Detalle
                        .FertilizacionMixta;

                if (snapshotActual
                        .ElementosSeleccionados
                        .Count ==
                    0)
                {
                    snapshotActual
                        .ElementosSeleccionados =
                            ObtenerElementosPersistidos(
                                contexto,
                                snapshotActual);
                }

                return;
            }

            SnapshotEdicion snapshot =
                new()
                {
                    AnalisisSueloCalculoId =
                        contexto
                            .AnalisisSueloCalculoId,

                    Balance =
                        contexto
                            .Detalle
                            .BalanceNutricional,

                    Enmienda =
                        contexto
                            .Detalle
                            .EnmiendaCalcarea,

                    Mixta =
                        contexto
                            .Detalle
                            .FertilizacionMixta
                };

            snapshot.ElementosSeleccionados =
                ObtenerElementosPersistidos(
                    contexto,
                    snapshot);

            snapshotActual =
                snapshot;
        }

        private static HashSet<int>
            ObtenerElementosPersistidos(
                AnalisisEdicionContexto contexto,
                SnapshotEdicion snapshot)
        {
            HashSet<int> seleccionados =
                contexto
                    .Detalle
                    .RequerimientoAnual
                    .Elementos
                    .Where(elemento =>
                        elemento
                            .ElementoQuimicosId >
                        0 &&
                        elemento
                            .IncluirCalculosComplementarios)
                    .Select(elemento =>
                        elemento
                            .ElementoQuimicosId)
                    .ToHashSet();

            /*
             * Respaldo para registros antiguos donde la bandera de inclusión
             * no se hubiera persistido correctamente.
             */
            if (seleccionados.Count == 0)
            {
                foreach (
                    AnalisisGuardadoFormulaDetalle detalle
                    in snapshot.Balance?
                        .Detalles
                    ??
                    new List<
                        AnalisisGuardadoFormulaDetalle>())
                {
                    if (detalle
                            .ElementoQuimicosId >
                        0)
                    {
                        seleccionados.Add(
                            detalle
                                .ElementoQuimicosId);
                    }
                }

                foreach (
                    AnalisisGuardadoMixtaDetalle detalle
                    in snapshot.Mixta?
                        .Detalles
                    ??
                    new List<
                        AnalisisGuardadoMixtaDetalle>())
                {
                    if (detalle
                            .ElementoQuimicosId >
                        0)
                    {
                        seleccionados.Add(
                            detalle
                                .ElementoQuimicosId);
                    }
                }
            }

            return seleccionados;
        }

        private async Task ProcesarPaginaActualAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                Page? pagina =
                    shellVinculado?
                        .CurrentPage;

                if (pagina is
                    ResultadoAnalisisSueloPage
                        paginaResultado)
                {
                    await PrepararResultadoAsync(
                        paginaResultado,
                        cancellationToken);

                    return;
                }

                if (pagina is
                    MultiCalculoPage
                        paginaMulti)
                {
                    await PrepararMultiCalculoAsync(
                        paginaMulti,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MostrarErrorEnPaginaActual(
                    ex);
            }
        }

        private async Task PrepararResultadoAsync(
            ResultadoAnalisisSueloPage pagina,
            CancellationToken cancellationToken)
        {
            ResultadoAnalisisSueloViewModel?
                viewModel =
                    await EsperarViewModelResultadoAsync(
                        pagina,
                        cancellationToken);

            SnapshotEdicion? snapshot =
                snapshotActual;

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (viewModel == null ||
                snapshot == null ||
                contexto == null ||
                snapshot
                    .AnalisisSueloCalculoId !=
                contexto
                    .AnalisisSueloCalculoId)
            {
                return;
            }

            await MainThread
                .InvokeOnMainThreadAsync(
                    () =>
                    {
                        AplicarSeleccionPersistida(
                            viewModel,
                            snapshot
                                .ElementosSeleccionados);
                    });
        }

        private async Task PrepararMultiCalculoAsync(
            MultiCalculoPage pagina,
            CancellationToken cancellationToken)
        {
            MultiCalculoViewModel?
                viewModel =
                    await EsperarViewModelMultiAsync(
                        pagina,
                        cancellationToken);

            SnapshotEdicion? snapshot =
                snapshotActual;

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (viewModel == null ||
                snapshot == null ||
                contexto == null ||
                snapshot
                    .AnalisisSueloCalculoId !=
                contexto
                    .AnalisisSueloCalculoId ||
                viewModel
                    .AnalisisSueloCalculoIdEdicion !=
                contexto
                    .AnalisisSueloCalculoId ||
                viewModel
                    .RequestGuardarAnalisis ==
                null ||
                viewModel
                    .ResultadoCalculo ==
                null)
            {
                return;
            }

            HashSet<int> elementosActuales =
                viewModel
                    .ResultadoCalculo
                    .Elementos
                    .Where(elemento =>
                        elemento
                            .ElementoQuimicosId
                        is > 0)
                    .Select(elemento =>
                        elemento
                            .ElementoQuimicosId!
                            .Value)
                    .ToHashSet();

            bool seleccionSinCambios =
                elementosActuales.SetEquals(
                    snapshot
                        .ElementosSeleccionados);

            bool requerimientoCambio =
                AnalisisEdicionService
                    .Instance
                    .CambioRequerimiento(
                        viewModel
                            .RequestGuardarAnalisis);

            int plantas =
                viewModel
                    .CantidadPlantas
                is > 0
                    ? viewModel
                        .CantidadPlantas
                        .Value
                    : contexto
                        .CantidadPlantas;

            /*
             * Solo se recuperan secciones que fueron colocadas en null durante
             * la navegación. Una sección todavía presente nunca se reemplaza.
             */
            if (seleccionSinCambios &&
                !requerimientoCambio)
            {
                if (contexto
                        .Detalle
                        .BalanceNutricional ==
                    null &&
                    snapshot.Balance !=
                    null &&
                    !AnalisisEdicionService
                        .Instance
                        .CambioBalance(
                            viewModel
                                .RequestGuardarAnalisis,
                            plantas))
                {
                    contexto
                        .Detalle
                        .BalanceNutricional =
                            snapshot.Balance;
                }

                if (contexto
                        .Detalle
                        .FertilizacionMixta ==
                    null &&
                    snapshot.Mixta !=
                    null)
                {
                    contexto
                        .Detalle
                        .FertilizacionMixta =
                            snapshot.Mixta;
                }
            }

            if (contexto
                    .Detalle
                    .EnmiendaCalcarea ==
                null &&
                snapshot.Enmienda !=
                null &&
                !AnalisisEdicionService
                    .Instance
                    .CambioEnmienda(
                        viewModel
                            .RequestGuardarAnalisis,
                        plantas))
            {
                contexto
                    .Detalle
                    .EnmiendaCalcarea =
                        snapshot.Enmienda;
            }

            /*
             * Los servicios ya existentes reconstruyen el temporal y la UI.
             * Se invalida únicamente la bandera para que no den por terminada
             * una restauración ejecutada antes de recuperar el snapshot.
             */
            AnalisisEdicionService
                .Instance
                .RestauracionUiRealizada =
                    false;

            /*
             * MultiCalculoPage y AnalisisEdicionCalculosDeterministaService ya
             * llaman RestaurarCalculosEdicionUiService. No se inicia un tercer
             * proceso paralelo desde aquí; se limita la corrección a devolver
             * la fuente de verdad que ambos servicios necesitan.
             */
        }

        private static async Task<
            ResultadoAnalisisSueloViewModel?>
            EsperarViewModelResultadoAsync(
                ResultadoAnalisisSueloPage pagina,
                CancellationToken cancellationToken)
        {
            for (int intento = 0;
                 intento < 120;
                 intento++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (pagina.BindingContext is
                        ResultadoAnalisisSueloViewModel
                            viewModel &&
                    viewModel.EsModoEdicion &&
                    viewModel
                            .AnalisisSueloCalculoIdEdicion
                        is > 0 &&
                    viewModel.Elementos.Count >
                        0)
                {
                    return viewModel;
                }

                await Task.Delay(
                    50,
                    cancellationToken);
            }

            return null;
        }

        private static async Task<
            MultiCalculoViewModel?>
            EsperarViewModelMultiAsync(
                MultiCalculoPage pagina,
                CancellationToken cancellationToken)
        {
            for (int intento = 0;
                 intento < 120;
                 intento++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (pagina.BindingContext is
                        MultiCalculoViewModel
                            viewModel &&
                    viewModel.EsModoEdicion &&
                    viewModel
                            .AnalisisSueloCalculoIdEdicion
                        is > 0 &&
                    viewModel
                            .ResultadoCalculo !=
                        null &&
                    viewModel
                            .RequestGuardarAnalisis !=
                        null)
                {
                    return viewModel;
                }

                await Task.Delay(
                    50,
                    cancellationToken);
            }

            return null;
        }

        private static void
            AplicarSeleccionPersistida(
                ResultadoAnalisisSueloViewModel
                    viewModel,
                HashSet<int>
                    seleccionPersistida)
        {
            foreach (
                ElementoResultadoCalculoResponse elemento
                in viewModel.Elementos)
            {
                if (elemento
                        .ElementoQuimicosId
                    is not int elementoId)
                {
                    continue;
                }

                elemento
                    .IncluirEnCalculosComplementarios =
                        seleccionPersistida
                            .Contains(
                                elementoId);
            }

            foreach (
                ElementoResultadoCalculoResponse elemento
                in viewModel
                    .Resultado?
                    .Elementos
                ??
                new List<
                    ElementoResultadoCalculoResponse>())
            {
                if (elemento
                        .ElementoQuimicosId
                    is not int elementoId)
                {
                    continue;
                }

                elemento
                    .IncluirEnCalculosComplementarios =
                        seleccionPersistida
                            .Contains(
                                elementoId);
            }

            /*
             * El ViewModel compara la selección actual con este conjunto al
             * continuar. Se actualiza en el mismo ciclo de UI para evitar que
             * una colección reconstruida sea interpretada como un cambio del
             * usuario.
             */
            FieldInfo? campoInicial =
                typeof(
                    ResultadoAnalisisSueloViewModel)
                    .GetField(
                        "elementosIncluidosInicialmente",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            if (campoInicial?
                    .GetValue(
                        viewModel)
                is HashSet<int>
                    iniciales)
            {
                iniciales.Clear();

                foreach (int id
                         in seleccionPersistida)
                {
                    iniciales.Add(
                        id);
                }
            }
        }

        private static void
            NormalizarLineaBaseEdicion()
        {
            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            SnapshotEdicion? snapshot =
                Instance
                    .snapshotActual;

            if (contexto == null)
                return;

            AnalisisSueloGuardarCalculoRequest
                requestComparacion =
                    ClonarParaComparacion(
                        contexto
                            .RequestOriginal);

            AnalisisGuardadoEnmiendaCalcarea?
                enmienda =
                    contexto
                        .Detalle
                        .EnmiendaCalcarea
                    ??
                    snapshot?
                        .Enmienda;

            AplicarRespaldoEnmienda(
                requestComparacion,
                enmienda);

            string claveRequerimiento =
                AnalisisEdicionService
                    .ConstruirClaveRequerimiento(
                        requestComparacion);

            int aplicacionesBalance =
                (
                    contexto
                        .Detalle
                        .BalanceNutricional
                    ??
                    snapshot?
                        .Balance
                )?
                .Formula
                .TotalAplicaciones
                ?? 0;

            int aplicacionesEnmienda =
                enmienda?
                    .TotalAplicaciones
                ?? 0;

            contexto.ClaveRequerimientoOriginal =
                claveRequerimiento;

            contexto.ClaveBalanceOriginal =
                string.Join(
                    "|",
                    claveRequerimiento,
                    contexto.CantidadPlantas,
                    aplicacionesBalance);

            contexto.ClaveEnmiendaOriginal =
                string.Join(
                    "|",
                    requestComparacion
                        .TerrenoId,
                    Formatear(
                        requestComparacion
                            .Ph),
                    Formatear(
                        requestComparacion
                            .AcidezTotal),
                    Formatear(
                        requestComparacion
                            .CalcioCice),
                    Formatear(
                        requestComparacion
                            .MagnesioCice),
                    Formatear(
                        requestComparacion
                            .PotasioCice),
                    contexto.CantidadPlantas,
                    aplicacionesEnmienda);
        }

        private static void
            AplicarRespaldoEnmienda(
                AnalisisSueloGuardarCalculoRequest
                    request,
                AnalisisGuardadoEnmiendaCalcarea?
                    enmienda)
        {
            if (enmienda == null)
                return;

            request.Ph =
                ObtenerValorPrincipalORespaldo(
                    request.Ph,
                    enmienda.Ph);

            request.AcidezTotal =
                ObtenerValorPrincipalORespaldo(
                    request.AcidezTotal,
                    enmienda.AcidezTotal);
        }

        private static decimal?
            ObtenerValorPrincipalORespaldo(
                decimal? valorPrincipal,
                decimal? valorRespaldo)
        {
            if (valorPrincipal.HasValue &&
                valorPrincipal.Value > 0)
            {
                return valorPrincipal;
            }

            if (valorRespaldo.HasValue &&
                valorRespaldo.Value > 0)
            {
                return valorRespaldo;
            }

            return
                valorPrincipal
                ??
                valorRespaldo
                ??
                0;
        }

        private static
            AnalisisSueloGuardarCalculoRequest
            ClonarParaComparacion(
                AnalisisSueloGuardarCalculoRequest
                    origen)
        {
            return new
                AnalisisSueloGuardarCalculoRequest
                {
                    TerrenoId =
                        origen.TerrenoId,

                    TipoCultivoId =
                        origen.TipoCultivoId,

                    TipoAnalisisSueloId =
                        origen.TipoAnalisisSueloId,

                    UsuarioId =
                        origen.UsuarioId,

                    CantidadQuintalesOro =
                        origen.CantidadQuintalesOro,

                    TamanoFinca =
                        origen.TamanoFinca,

                    Ph =
                        origen.Ph,

                    MateriaOrganica =
                        origen.MateriaOrganica,

                    UnidadMedidaMateriaOrganicaId =
                        origen
                            .UnidadMedidaMateriaOrganicaId,

                    AcidezTotal =
                        origen.AcidezTotal,

                    CalcioCice =
                        origen.CalcioCice,

                    MagnesioCice =
                        origen.MagnesioCice,

                    PotasioCice =
                        origen.PotasioCice,

                    FechaAnalisisSuelo =
                        origen
                            .FechaAnalisisSuelo,

                    LaboratorioAnalasisSuelo =
                        origen
                            .LaboratorioAnalasisSuelo,

                    IdentificadorAnalisisSuelo =
                        origen
                            .IdentificadorAnalisisSuelo,

                    ElementosQuimicos =
                        origen
                            .ElementosQuimicos
                            .Select(elemento =>
                                new
                                    ElementoQuimicoAnalisisRequest
                                    {
                                        ElementoQuimicosId =
                                            elemento
                                                .ElementoQuimicosId,

                                        UnidadMedidaId =
                                            elemento
                                                .UnidadMedidaId,

                                        CantidadElemento =
                                            elemento
                                                .CantidadElemento
                                    })
                            .ToList(),

                    FuentesOrganicas =
                        origen
                            .FuentesOrganicas
                            .Select(fuente =>
                                new
                                    FuenteOrganicaAnalisisRequest
                                    {
                                        FuenteNutrientesId =
                                            fuente
                                                .FuenteNutrientesId,

                                        CantidadAplicada =
                                            fuente
                                                .CantidadAplicada
                                    })
                            .ToList()
                };
        }

        private static string Formatear(
            decimal? valor)
        {
            return (valor ?? 0)
                .ToString(
                    "0.####",
                    CultureInfo.InvariantCulture);
        }

        private void MostrarErrorEnPaginaActual(
            Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    if (shellVinculado?
                            .CurrentPage?
                            .BindingContext is
                        MultiCalculoViewModel
                            multi)
                    {
                        multi.Mensaje =
                            "No fue posible conservar los " +
                            "cálculos guardados durante la " +
                            "edición: " +
                            ex.Message;

                        return;
                    }

                    if (shellVinculado?
                            .CurrentPage?
                            .BindingContext is
                        ResultadoAnalisisSueloViewModel
                            resultado)
                    {
                        resultado
                            .MensajeSeleccionCalculo =
                                "No fue posible preparar la " +
                                "selección guardada: " +
                                ex.Message;
                    }
                });
        }

        private static void CancelarSeguro(
            CancellationTokenSource?
                source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                source.Dispose();
            }
        }

        private sealed class SnapshotEdicion
        {
            public int
                AnalisisSueloCalculoId
                { get; init; }

            public AnalisisGuardadoBalanceNutricional?
                Balance
                { get; set; }

            public AnalisisGuardadoEnmiendaCalcarea?
                Enmienda
                { get; set; }

            public AnalisisGuardadoFertilizacionMixta?
                Mixta
                { get; set; }

            public HashSet<int>
                ElementosSeleccionados
                { get; set; } =
                    new();
        }
    }
}
