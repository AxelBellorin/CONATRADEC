using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

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

        private void AplicarAParametroConstante(
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
                        x.UnidadPredeterminada)?.UnidadMedidaId,
                preservarSeleccionGuardada:
                    viewModel.EsModoEdicion);
        }

        private void AplicarAElemento(
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
                elemento.UnidadPredeterminadaId,
                preservarSeleccionGuardada:
                    viewModel.EsModoEdicion);
        }

        private static void LimpiarUnidadesItem(
            ResultadoAnalisisItemViewModel item)
        {
            /*
             * Primero se limpia la selección y luego se reemplaza el origen
             * del Picker. El orden evita que MAUI conserve una selección de
             * la colección anterior.
             */
            item.UnidadSeleccionada =
                null;

            item.UnidadesMedida =
                new ObservableCollection<
                    UnidadMedidaResponse>();

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadSeleccionada));
        }

        private static void AplicarUnidades(
            ResultadoAnalisisItemViewModel item,
            IEnumerable<
                UnidadConversionConfiguradaResponse>
                    configuraciones,
            int? unidadPredeterminadaId,
            bool preservarSeleccionGuardada)
        {
            int? unidadSeleccionadaId =
                preservarSeleccionGuardada
                    ? item.UnidadSeleccionada?.UnidadMedidaId
                    : null;

            UnidadMedidaResponse?
                unidadSeleccionadaAnterior =
                    preservarSeleccionGuardada
                        ? item.UnidadSeleccionada
                        : null;

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
                                    true
                            })
                        .GroupBy(x =>
                            x.UnidadMedidaId)
                        .Select(x =>
                            x.First())
                        .ToList();

            /*
             * Si todas las asociaciones están inactivas u ocultas, el Picker
             * debe quedar vacío. Antes se conservaba el catálogo general y por
             * eso parecía que Activa y Visible no tenían ningún efecto.
             */
            if (unidadesPermitidas.Count == 0)
            {
                LimpiarUnidadesItem(item);
                return;
            }

            UnidadMedidaResponse?
                nuevaSeleccion = null;

            /*
             * Únicamente en edición se conserva la unidad realmente guardada.
             * En un análisis nuevo se ignora la selección antigua y se utiliza
             * la unidad predeterminada configurada en el backend.
             */
            if (preservarSeleccionGuardada &&
                unidadSeleccionadaId.HasValue)
            {
                nuevaSeleccion =
                    unidadesPermitidas
                        .FirstOrDefault(x =>
                            x.UnidadMedidaId ==
                                unidadSeleccionadaId);
            }

            /*
             * Respaldo histórico exclusivo para edición. Una unidad inactiva
             * u oculta puede mostrarse temporalmente si el análisis antiguo la
             * utilizó, evitando modificar datos históricos al abrirlos.
             */
            if (preservarSeleccionGuardada &&
                nuevaSeleccion == null &&
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
                            "Ya no está activa o visible en la configuración actual.",
                        Activo = false
                    };

                unidadesPermitidas.Add(
                    historica);

                nuevaSeleccion =
                    historica;
            }

            /*
             * En creación esta es la primera opción evaluada. En edición actúa
             * como respaldo cuando no existe una unidad histórica guardada.
             */
            nuevaSeleccion ??=
                unidadPredeterminadaId.HasValue
                    ? unidadesPermitidas
                        .FirstOrDefault(x =>
                            x.UnidadMedidaId ==
                                unidadPredeterminadaId)
                    : null;

            /*
             * No se selecciona la primera unidad como respaldo. Cuando no
             * existe una predeterminada configurada, el Picker debe quedar
             * vacío para que el usuario seleccione conscientemente.
             */
            item.UnidadSeleccionada =
                null;

            item.UnidadesMedida =
                new ObservableCollection<
                    UnidadMedidaResponse>(
                        unidadesPermitidas);

            /*
             * El ItemsSource debe notificarse antes del SelectedItem. Si se
             * hace al revés, WinUI puede descartar la selección porque el
             * objeto todavía no forma parte de la colección visible.
             */
            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));

            AplicarSeleccionDespuesDeActualizarLista(
                item,
                nuevaSeleccion);
        }

        private static void
            AplicarSeleccionDespuesDeActualizarLista(
                ResultadoAnalisisItemViewModel item,
                UnidadMedidaResponse? seleccion)
        {
            /*
             * La primera asignación resuelve la mayoría de plataformas.
             * La segunda, enviada a la cola principal, cubre el ciclo de
             * medición de Picker en WinUI sin introducir esperas fijas.
             */
            item.UnidadSeleccionada =
                seleccion;

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadSeleccionada));

            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    if (seleccion == null)
                    {
                        item.UnidadSeleccionada =
                            null;

                        return;
                    }

                    UnidadMedidaResponse?
                        seleccionVigente =
                            item.UnidadesMedida
                                .FirstOrDefault(x =>
                                    x.UnidadMedidaId ==
                                        seleccion
                                            .UnidadMedidaId);

                    item.UnidadSeleccionada =
                        seleccionVigente;
                });
        }
    }
}
