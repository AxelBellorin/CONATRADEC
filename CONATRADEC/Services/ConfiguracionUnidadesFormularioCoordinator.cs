using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Aplica en el formulario de análisis únicamente las unidades permitidas
    /// por la configuración del backend. También conserva la unidad histórica
    /// cuando se está editando un análisis anterior.
    ///
    /// Esta versión evita encolar una carga completa por cada elemento agregado
    /// al formulario. Todas las solicitudes simultáneas comparten una única
    /// tarea y la configuración solo vuelve a descargarse cuando su caché fue
    /// invalidada o se solicita expresamente una recarga.
    /// </summary>
    public sealed class ConfiguracionUnidadesFormularioCoordinator
    {
        private readonly NuevoAnalisisFormEdicionViewModel viewModel;
        private readonly ConfiguracionUnidadesApiService apiService = new();

        private readonly object cargaSync = new();

        private ConfiguracionFormularioAnalisisResponse? configuracion;
        private Task? cargaActual;
        private long versionConfiguracionAplicada = -1;
        private bool adjuntado;

        public ConfiguracionUnidadesFormularioCoordinator(
            NuevoAnalisisFormEdicionViewModel viewModel)
        {
            this.viewModel = viewModel ??
                throw new ArgumentNullException(nameof(viewModel));
        }

        public void Adjuntar()
        {
            if (adjuntado)
                return;

            adjuntado = true;

            viewModel.ParametrosConstantesAnalisis.CollectionChanged +=
                Parametros_CollectionChanged;

            viewModel.ElementosQuimicosAnalisis.CollectionChanged +=
                Elementos_CollectionChanged;

            /*
             * Si la configuración ya se cargó anteriormente, se reutiliza
             * inmediatamente. No se crea una solicitud adicional.
             */
            if (ConfiguracionVigente())
                AplicarConfiguracionActual();
        }

        /// <summary>
        /// Libera las suscripciones cuando la página deja de utilizar el
        /// coordinador.
        /// </summary>
        public void Desadjuntar()
        {
            if (!adjuntado)
                return;

            viewModel.ParametrosConstantesAnalisis.CollectionChanged -=
                Parametros_CollectionChanged;

            viewModel.ElementosQuimicosAnalisis.CollectionChanged -=
                Elementos_CollectionChanged;

            adjuntado = false;
        }

        /// <summary>
        /// Devuelve la misma tarea cuando ya existe una carga en curso.
        /// Esto impide que cada CollectionChanged agregue otra operación a una
        /// cola de SemaphoreSlim.
        /// </summary>
        public Task CargarYAplicarAsync(
            bool forzarRecarga = false,
            CancellationToken cancellationToken = default)
        {
            lock (cargaSync)
            {
                if (!forzarRecarga && ConfiguracionVigente())
                    return Task.CompletedTask;

                if (cargaActual != null &&
                    !cargaActual.IsCompleted)
                {
                    return cargaActual;
                }

                cargaActual = CargarYAplicarInternoAsync(
                    forzarRecarga,
                    cancellationToken);

                return cargaActual;
            }
        }

        private async Task CargarYAplicarInternoAsync(
            bool forzarRecarga,
            CancellationToken cancellationToken)
        {
            try
            {
                ConfiguracionUnidadesApiResult<
                    ConfiguracionFormularioAnalisisResponse> resultado =
                        await apiService.ObtenerConfiguracionFormularioAsync(
                            forzarRecarga,
                            cancellationToken);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    Debug.WriteLine(
                        "No se pudo cargar la configuración de unidades: " +
                        resultado.Message);

                    return;
                }

                configuracion = resultado.Data;
                versionConfiguracionAplicada =
                    ConfiguracionUnidadesApiService.CacheVersion;

                /*
                 * Las colecciones están enlazadas con controles MAUI.
                 * La aplicación final siempre se ejecuta en el hilo principal.
                 */
                if (adjuntado)
                {
                    await MainThread.InvokeOnMainThreadAsync(
                        AplicarConfiguracionActual);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No se pudo aplicar la configuración de unidades al " +
                    $"formulario: {ex}");
            }
            finally
            {
                lock (cargaSync)
                {
                    cargaActual = null;
                }
            }
        }

        public void AplicarConfiguracionActual()
        {
            ConfiguracionFormularioAnalisisResponse? actual =
                configuracion;

            if (!adjuntado ||
                actual == null)
            {
                return;
            }

            foreach (ResultadoAnalisisItemViewModel item in
                     viewModel.ParametrosConstantesAnalisis)
            {
                AplicarAParametroConstante(
                    item,
                    actual);
            }

            foreach (ResultadoAnalisisItemViewModel item in
                     viewModel.ElementosQuimicosAnalisis)
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
            if (!adjuntado)
                return;

            ConfiguracionFormularioAnalisisResponse? actual =
                configuracion;

            if (actual == null ||
                !ConfiguracionVigente())
            {
                /*
                 * CargarYAplicarAsync comparte la misma tarea. Aunque se
                 * agreguen varios elementos seguidos, solo habrá una carga.
                 */
                _ = CargarYAplicarAsync();
                return;
            }

            /*
             * Clear genera Reset y no contiene elementos nuevos. No se vuelve
             * a recorrer toda la colección vacía; cada Add posterior aplicará
             * únicamente al elemento agregado.
             */
            if (e.NewItems == null)
                return;

            foreach (object nuevo in e.NewItems)
            {
                if (nuevo is ResultadoAnalisisItemViewModel item)
                {
                    AplicarAParametroConstante(
                        item,
                        actual);
                }
            }
        }

        private void Elementos_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (!adjuntado)
                return;

            ConfiguracionFormularioAnalisisResponse? actual =
                configuracion;

            if (actual == null ||
                !ConfiguracionVigente())
            {
                _ = CargarYAplicarAsync();
                return;
            }

            if (e.NewItems == null)
                return;

            foreach (object nuevo in e.NewItems)
            {
                if (nuevo is ResultadoAnalisisItemViewModel item)
                {
                    AplicarAElemento(
                        item,
                        actual);
                }
            }
        }

        private void AplicarAParametroConstante(
            ResultadoAnalisisItemViewModel item,
            ConfiguracionFormularioAnalisisResponse configuracionActual)
        {
            if (!string.Equals(
                    item.CodigoParametro,
                    "MATERIA_ORGANICA",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (configuracionActual.UnidadesMateriaOrganica.Count == 0)
            {
                LimpiarUnidadesItem(item);
                return;
            }

            AplicarUnidades(
                item,
                configuracionActual.UnidadesMateriaOrganica,
                configuracionActual.UnidadesMateriaOrganica
                    .FirstOrDefault(x => x.UnidadPredeterminada)?
                    .UnidadMedidaId,
                preservarSeleccionGuardada:
                    viewModel.EsModoEdicion);
        }

        private void AplicarAElemento(
            ResultadoAnalisisItemViewModel item,
            ConfiguracionFormularioAnalisisResponse configuracionActual)
        {
            if (!item.ElementoQuimicoId.HasValue)
                return;

            ElementoConfiguracionUnidadesResponse? elemento =
                configuracionActual.Elementos.FirstOrDefault(x =>
                    x.ElementoQuimicosId ==
                    item.ElementoQuimicoId.Value);

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
            if (item.UnidadesMedida.Count == 0 &&
                item.UnidadSeleccionada == null)
            {
                return;
            }

            item.UnidadesMedida =
                new ObservableCollection<UnidadMedidaResponse>();

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));

            item.UnidadSeleccionada = null;
        }

        private static void AplicarUnidades(
            ResultadoAnalisisItemViewModel item,
            IEnumerable<UnidadConversionConfiguradaResponse>
                configuraciones,
            int? unidadPredeterminadaId,
            bool preservarSeleccionGuardada)
        {
            int? unidadSeleccionadaId =
                preservarSeleccionGuardada
                    ? item.UnidadSeleccionada?.UnidadMedidaId
                    : null;

            UnidadMedidaResponse? unidadSeleccionadaAnterior =
                preservarSeleccionGuardada
                    ? item.UnidadSeleccionada
                    : null;

            List<UnidadMedidaResponse> unidadesPermitidas =
                configuraciones
                    .Where(x =>
                        x.Activo &&
                        x.VisibleEnFormulario &&
                        x.UnidadMedidaId > 0)
                    .OrderBy(x => x.Orden)
                    .ThenBy(x => x.NombreUnidadMedida)
                    .Select(x => new UnidadMedidaResponse
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

            if (unidadesPermitidas.Count == 0)
            {
                LimpiarUnidadesItem(item);
                return;
            }

            int? nuevaSeleccionId = null;

            if (preservarSeleccionGuardada &&
                unidadSeleccionadaId.HasValue &&
                unidadesPermitidas.Any(x =>
                    x.UnidadMedidaId ==
                    unidadSeleccionadaId.Value))
            {
                nuevaSeleccionId =
                    unidadSeleccionadaId.Value;
            }

            if (preservarSeleccionGuardada &&
                !nuevaSeleccionId.HasValue &&
                unidadSeleccionadaAnterior?
                    .UnidadMedidaId is > 0 &&
                !unidadesPermitidas.Any(x =>
                    x.UnidadMedidaId ==
                    unidadSeleccionadaAnterior
                        .UnidadMedidaId))
            {
                UnidadMedidaResponse historica = new()
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
                        "Unidad histórica del análisis. Ya no está activa o " +
                        "visible en la configuración actual.",
                    Activo =
                        false
                };

                unidadesPermitidas.Add(historica);

                nuevaSeleccionId =
                    historica.UnidadMedidaId;
            }

            if (!nuevaSeleccionId.HasValue &&
                unidadPredeterminadaId.HasValue &&
                unidadesPermitidas.Any(x =>
                    x.UnidadMedidaId ==
                    unidadPredeterminadaId.Value))
            {
                nuevaSeleccionId =
                    unidadPredeterminadaId.Value;
            }

            bool mismaLista =
                item.UnidadesMedida.Count ==
                    unidadesPermitidas.Count &&
                item.UnidadesMedida
                    .Zip(
                        unidadesPermitidas,
                        MismaUnidad)
                    .All(x => x);

            /*
             * Si la lista no cambió, no se reemplaza el ItemsSource del
             * Picker. Solo se corrige la selección cuando realmente difiere.
             */
            if (mismaLista)
            {
                UnidadMedidaResponse? seleccionExistente =
                    nuevaSeleccionId.HasValue
                        ? item.UnidadesMedida
                            .FirstOrDefault(x =>
                                x.UnidadMedidaId ==
                                nuevaSeleccionId.Value)
                        : null;

                if (!ReferenceEquals(
                        item.UnidadSeleccionada,
                        seleccionExistente))
                {
                    item.UnidadSeleccionada =
                        seleccionExistente;
                }

                return;
            }

            item.UnidadesMedida =
                new ObservableCollection<UnidadMedidaResponse>(
                    unidadesPermitidas);

            item.OnPropertyChanged(
                nameof(
                    ResultadoAnalisisItemViewModel
                        .UnidadesMedida));

            UnidadMedidaResponse? nuevaSeleccion =
                nuevaSeleccionId.HasValue
                    ? item.UnidadesMedida
                        .FirstOrDefault(x =>
                            x.UnidadMedidaId ==
                            nuevaSeleccionId.Value)
                    : null;

            item.UnidadSeleccionada =
                nuevaSeleccion;

            /*
             * Respaldo de un solo ciclo visual. No crea una cadena recursiva:
             * únicamente actúa si el Picker no conservó el ID esperado.
             */
            if (nuevaSeleccionId.HasValue)
            {
                int idEsperado =
                    nuevaSeleccionId.Value;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (item.UnidadSeleccionada?
                            .UnidadMedidaId ==
                        idEsperado)
                    {
                        return;
                    }

                    item.UnidadSeleccionada =
                        item.UnidadesMedida
                            .FirstOrDefault(x =>
                                x.UnidadMedidaId ==
                                idEsperado);
                });
            }
        }

        private bool ConfiguracionVigente()
        {
            return
                configuracion != null &&
                versionConfiguracionAplicada ==
                    ConfiguracionUnidadesApiService
                        .CacheVersion;
        }

        private static bool MismaUnidad(
            UnidadMedidaResponse actual,
            UnidadMedidaResponse nueva)
        {
            return
                actual.UnidadMedidaId ==
                    nueva.UnidadMedidaId &&
                string.Equals(
                    actual.NombreUnidadMedida,
                    nueva.NombreUnidadMedida,
                    StringComparison.Ordinal) &&
                string.Equals(
                    actual.SimboloUnidadMedida,
                    nueva.SimboloUnidadMedida,
                    StringComparison.Ordinal) &&
                string.Equals(
                    actual.AbreviaturaUnidadMedida,
                    nueva.AbreviaturaUnidadMedida,
                    StringComparison.Ordinal) &&
                string.Equals(
                    actual.DescripcionUnidadMedida,
                    nueva.DescripcionUnidadMedida,
                    StringComparison.Ordinal) &&
                actual.Activo ==
                    nueva.Activo;
        }
    }
}
