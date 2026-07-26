using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Aplica en el formulario de análisis las unidades permitidas que
    /// devuelve la API.
    ///
    /// Se conecta a las colecciones ya existentes del ViewModel, por lo que
    /// también procesa elementos agregados, restaurados o cargados durante
    /// una edición sin duplicar la lógica del formulario.
    /// </summary>
    public sealed class
        ConfiguracionUnidadesFormularioCoordinator
    {
        private readonly NuevoAnalisisFormEdicionViewModel
            viewModel;

        private readonly ConfiguracionUnidadesApiService
            apiService = new();

        private readonly SemaphoreSlim cargaLock =
            new(1, 1);

        private ConfiguracionFormularioAnalisisResponse?
            configuracion;

        private bool adjuntado;

        public ConfiguracionUnidadesFormularioCoordinator(
            NuevoAnalisisFormEdicionViewModel viewModel)
        {
            this.viewModel =
                viewModel ??
                throw new ArgumentNullException(
                    nameof(viewModel));
        }

        public void Adjuntar()
        {
            if (adjuntado)
                return;

            adjuntado = true;

            viewModel.ParametrosConstantesAnalisis
                .CollectionChanged +=
                    Parametros_CollectionChanged;

            viewModel.ElementosQuimicosAnalisis
                .CollectionChanged +=
                    Elementos_CollectionChanged;

            AplicarConfiguracionActual();
        }

        public async Task CargarYAplicarAsync(
            bool forzarRecarga = false,
            CancellationToken cancellationToken =
                default)
        {
            await cargaLock.WaitAsync(
                cancellationToken);

            try
            {
                ConfiguracionUnidadesApiResult<
                    ConfiguracionFormularioAnalisisResponse>
                    resultado =
                        await apiService
                            .ObtenerConfiguracionFormularioAsync(
                                forzarRecarga,
                                cancellationToken);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    Debug.WriteLine(
                        "No se pudo cargar la configuración " +
                        $"de unidades: {resultado.Message}");

                    /*
                     * El formulario conserva las unidades cargadas por el
                     * catálogo anterior. De esta forma una interrupción
                     * temporal del endpoint no bloquea la captura.
                     */
                    return;
                }

                configuracion =
                    resultado.Data;

                AplicarConfiguracionActual();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No se pudo aplicar la configuración " +
                    $"de unidades al formulario: {ex}");
            }
            finally
            {
                cargaLock.Release();
            }
        }

        public void AplicarConfiguracionActual()
        {
            ConfiguracionFormularioAnalisisResponse?
                actual =
                    configuracion;

            if (actual == null)
                return;

            foreach (
                ResultadoAnalisisItemViewModel item
                in viewModel.ParametrosConstantesAnalisis)
            {
                AplicarAParametroConstante(
                    item,
                    actual);
            }

            foreach (
                ResultadoAnalisisItemViewModel item
                in viewModel.ElementosQuimicosAnalisis)
            {
                AplicarAElemento(
                    item,
                    actual);
            }
        }

        private void Parametros_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (configuracion == null)
            {
                _ = CargarYAplicarAsync();
                return;
            }

            if (e.NewItems == null)
            {
                AplicarConfiguracionActual();
                return;
            }

            foreach (
                object nuevo
                in e.NewItems)
            {
                if (nuevo is
                    ResultadoAnalisisItemViewModel item)
                {
                    AplicarAParametroConstante(
                        item,
                        configuracion);
                }
            }
        }

        private void Elementos_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (configuracion == null)
            {
                _ = CargarYAplicarAsync();
                return;
            }

            if (e.NewItems == null)
            {
                AplicarConfiguracionActual();
                return;
            }

            foreach (
                object nuevo
                in e.NewItems)
            {
                if (nuevo is
                    ResultadoAnalisisItemViewModel item)
                {
                    AplicarAElemento(
                        item,
                        configuracion);
                }
            }
        }

        private static void AplicarAParametroConstante(
            ResultadoAnalisisItemViewModel item,
            ConfiguracionFormularioAnalisisResponse
                configuracion)
        {
            if (!string.Equals(
                    item.CodigoParametro,
                    "MATERIA_ORGANICA",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (configuracion
                    .UnidadesMateriaOrganica
                    .Count == 0)
            {
                LimpiarUnidadesItem(item);
                return;
            }

            AplicarUnidades(
                item,
                configuracion.UnidadesMateriaOrganica,
                configuracion
                    .UnidadesMateriaOrganica
                    .FirstOrDefault(x =>
                        x.UnidadPredeterminada)?.UnidadMedidaId);
        }

        private static void AplicarAElemento(
            ResultadoAnalisisItemViewModel item,
            ConfiguracionFormularioAnalisisResponse
                configuracion)
        {
            if (!item.ElementoQuimicoId.HasValue)
                return;

            ElementoConfiguracionUnidadesResponse?
                elemento =
                    configuracion.Elementos
                        .FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                                item
                                    .ElementoQuimicoId
                                    .Value);

            if (elemento == null ||
                elemento.Unidades.Count == 0)
            {
                LimpiarUnidadesItem(item);
                return;
            }

            AplicarUnidades(
                item,
                elemento.Unidades,
                elemento.UnidadPredeterminadaId);
        }

        private static void LimpiarUnidadesItem(
            ResultadoAnalisisItemViewModel item)
        {
            item.UnidadesMedida =
                new ObservableCollection<
                    UnidadMedidaResponse>();

            item.UnidadSeleccionada =
                null;

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));
        }

        private static void AplicarUnidades(
            ResultadoAnalisisItemViewModel item,
            IEnumerable<
                UnidadConversionConfiguradaResponse>
                    configuraciones,
            int? unidadPredeterminadaId)
        {
            int? unidadSeleccionadaId =
                item.UnidadSeleccionada?.UnidadMedidaId;

            UnidadMedidaResponse?
                unidadSeleccionadaAnterior =
                    item.UnidadSeleccionada;

            List<UnidadMedidaResponse>
                unidadesPermitidas =
                    configuraciones
                        .Where(x =>
                            x.Activo &&
                            x.VisibleEnFormulario &&
                            x.UnidadMedidaId > 0)
                        .OrderBy(x =>
                            x.Orden)
                        .ThenBy(x =>
                            x.NombreUnidadMedida)
                        .Select(x =>
                            new UnidadMedidaResponse
                            {
                                UnidadMedidaId =
                                    x.UnidadMedidaId,
                                NombreUnidadMedida =
                                    x.NombreUnidadMedida,
                                SimboloUnidadMedida =
                                    null,
                                AbreviaturaUnidadMedida =
                                    null,
                                DescripcionUnidadMedida =
                                    x.Observacion,
                                Activo =
                                    x.Activo
                            })
                        .GroupBy(x =>
                            x.UnidadMedidaId)
                        .Select(x =>
                            x.First())
                        .ToList();

            if (unidadesPermitidas.Count == 0)
                return;

            /*
             * Cuando se edita un análisis, la unidad guardada se restaura
             * antes de que este coordinador filtre la lista. Si continúa
             * permitida, se conserva exactamente esa selección.
             */
            UnidadMedidaResponse?
                nuevaSeleccion =
                    unidadSeleccionadaId.HasValue
                        ? unidadesPermitidas
                            .FirstOrDefault(x =>
                                x.UnidadMedidaId ==
                                    unidadSeleccionadaId)
                        : null;

            /*
             * Respaldo para datos históricos: si una unidad guardada dejó de
             * estar visible, se mantiene temporalmente dentro del Picker para
             * no cambiar silenciosamente el análisis al abrirlo para editar.
             */
            if (nuevaSeleccion == null &&
                unidadSeleccionadaAnterior?.UnidadMedidaId is > 0 &&
                !unidadesPermitidas.Any(x =>
                    x.UnidadMedidaId ==
                        unidadSeleccionadaAnterior
                            .UnidadMedidaId))
            {
                UnidadMedidaResponse historica =
                    new()
                    {
                        UnidadMedidaId =
                            unidadSeleccionadaAnterior
                                .UnidadMedidaId,
                        NombreUnidadMedida =
                            unidadSeleccionadaAnterior
                                .NombreUnidadMedida,
                        SimboloUnidadMedida =
                            unidadSeleccionadaAnterior
                                .SimboloUnidadMedida,
                        AbreviaturaUnidadMedida =
                            unidadSeleccionadaAnterior
                                .AbreviaturaUnidadMedida,
                        DescripcionUnidadMedida =
                            "Unidad histórica del análisis. " +
                            "Ya no está visible en la configuración actual.",
                        Activo = false
                    };

                unidadesPermitidas.Add(
                    historica);

                nuevaSeleccion =
                    historica;
            }

            nuevaSeleccion ??=
                unidadPredeterminadaId.HasValue
                    ? unidadesPermitidas
                        .FirstOrDefault(x =>
                            x.UnidadMedidaId ==
                                unidadPredeterminadaId)
                    : null;

            nuevaSeleccion ??=
                unidadesPermitidas.FirstOrDefault();

            item.UnidadesMedida =
                new ObservableCollection<
                    UnidadMedidaResponse>(
                        unidadesPermitidas);

            item.UnidadSeleccionada =
                nuevaSeleccion;

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));
        }
    }
}
