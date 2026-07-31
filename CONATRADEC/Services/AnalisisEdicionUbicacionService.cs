using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Restaura los filtros País, Departamento y Municipio del terreno
    /// seleccionado cuando se abre por primera vez un análisis en edición.
    ///
    /// El formulario carga sus catálogos de forma asíncrona. Por eso este
    /// servicio espera a que la página y sus colecciones estén listas antes
    /// de asignar la ubicación guardada, evitando que el primer ingreso quede
    /// con País vacío o con el país predeterminado.
    /// </summary>
    public sealed class AnalisisEdicionUbicacionService
    {
        private static readonly Lazy<AnalisisEdicionUbicacionService>
            instancia =
                new(() =>
                    new AnalisisEdicionUbicacionService());

        private Shell? shellVinculado;
        private CancellationTokenSource? restauracionCts;

        private AnalisisEdicionUbicacionService()
        {
        }

        public static AnalisisEdicionUbicacionService Instance =>
            instancia.Value;

        public void VincularShell(
            Shell shell)
        {
            if (ReferenceEquals(
                    shellVinculado,
                    shell))
            {
                return;
            }

            if (shellVinculado != null)
            {
                shellVinculado.Navigated -=
                    Shell_Navigated;
            }

            shellVinculado = shell;

            shellVinculado.Navigated +=
                Shell_Navigated;
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            CancellationTokenSource nuevaCts =
                new();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref restauracionCts,
                    nuevaCts);

            if (anterior != null)
            {
                try
                {
                    anterior.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    anterior.Dispose();
                }
            }

            _ = RestaurarPaginaActualAsync(
                nuevaCts.Token);
        }

        private async Task RestaurarPaginaActualAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                /*
                 * Navigated puede dispararse antes de que OnAppearing termine
                 * de cargar el contexto y los catálogos locales.
                 */
                for (int intento = 0;
                     intento < 120;
                     intento++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Shell? shell =
                        shellVinculado;

                    if (shell?.CurrentPage
                            is not NuevoAnalisisFormPage pagina ||
                        pagina.BindingContext
                            is not NuevoAnalisisFormEdicionViewModel viewModel)
                    {
                        return;
                    }

                    if (!viewModel.EsModoEdicion)
                        return;

                    TerrenoResponse? terreno =
                        viewModel.TerrenoSeleccionado;

                    TerrenoUbicacionResponse? ubicacion =
                        terreno?.Ubicacion;

                    bool formularioListo =
                        !viewModel.IsBusy &&
                        terreno?.TerrenoId is > 0 &&
                        ubicacion != null &&
                        viewModel.Paises.Count > 0;

                    if (!formularioListo)
                    {
                        await Task.Delay(
                                50,
                                cancellationToken);

                        continue;
                    }

                    await RestaurarUbicacionAsync(
                            viewModel,
                            terreno!,
                            ubicacion!,
                            cancellationToken);

                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                /*
                 * La ubicación es un apoyo visual del buscador. Una falla al
                 * restaurarla nunca debe impedir que el análisis se edite.
                 */
            }
        }

        private static async Task RestaurarUbicacionAsync(
            NuevoAnalisisFormEdicionViewModel viewModel,
            TerrenoResponse terrenoEsperado,
            TerrenoUbicacionResponse ubicacion,
            CancellationToken cancellationToken)
        {
            PaisResponse? pais =
                viewModel.Paises.FirstOrDefault(x =>
                    x.PaisId == ubicacion.PaisId);

            pais ??=
                viewModel.Paises.FirstOrDefault(x =>
                    string.Equals(
                        x.NombrePais?.Trim(),
                        ubicacion.NombrePais?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (pais == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (viewModel.TerrenoSeleccionado?.TerrenoId !=
                    terrenoEsperado.TerrenoId)
                {
                    return;
                }

                viewModel.PaisSeleccionado =
                    pais;
            });

            if (ubicacion.DepartamentoId is not > 0)
                return;

            bool departamentosListos =
                await EsperarAsync(
                        () =>
                            viewModel.Departamentos.Any(x =>
                                x.DepartamentoId ==
                                    ubicacion.DepartamentoId),
                        cancellationToken);

            if (!departamentosListos)
                return;

            DepartamentoResponse? departamento =
                viewModel.Departamentos.FirstOrDefault(x =>
                    x.DepartamentoId ==
                        ubicacion.DepartamentoId);

            if (departamento == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (viewModel.TerrenoSeleccionado?.TerrenoId !=
                    terrenoEsperado.TerrenoId)
                {
                    return;
                }

                viewModel.DepartamentoSeleccionado =
                    departamento;
            });

            int? municipioId =
                ubicacion.MunicipioId ??
                terrenoEsperado.MunicipioId;

            if (municipioId is not > 0)
                return;

            bool municipiosListos =
                await EsperarAsync(
                        () =>
                            viewModel.Municipios.Any(x =>
                                x.MunicipioId ==
                                    municipioId),
                        cancellationToken);

            if (!municipiosListos)
                return;

            MunicipioResponse? municipio =
                viewModel.Municipios.FirstOrDefault(x =>
                    x.MunicipioId ==
                        municipioId);

            if (municipio == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (viewModel.TerrenoSeleccionado?.TerrenoId !=
                    terrenoEsperado.TerrenoId)
                {
                    return;
                }

                viewModel.MunicipioSeleccionado =
                    municipio;
            });
        }

        private static async Task<bool> EsperarAsync(
            Func<bool> condicion,
            CancellationToken cancellationToken)
        {
            for (int intento = 0;
                 intento < 100;
                 intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (condicion())
                    return true;

                await Task.Delay(
                        50,
                        cancellationToken);
            }

            return condicion();
        }
    }
}
