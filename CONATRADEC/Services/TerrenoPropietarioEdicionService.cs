using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Sincroniza el formulario de terreno con la relación propietario-terreno
    /// vigente antes de mostrar Ver o Editar.
    ///
    /// La lista puede conservar una tarjeta cargada antes de una vinculación
    /// realizada desde la Web u otro equipo. Por eso el formulario consulta el
    /// terreno por ID y no confía únicamente en el objeto recibido al navegar.
    /// </summary>
    public sealed class TerrenoPropietarioEdicionService
    {
        private static readonly Lazy<
            TerrenoPropietarioEdicionService> instancia =
                new(() =>
                    new TerrenoPropietarioEdicionService());

        private readonly TerrenoDetalleActualApiService
            terrenoDetalleApiService =
                new();

        private readonly PropietarioApiService
            propietarioApiService =
                new();

        private Shell? shellVinculado;
        private CancellationTokenSource? cargaCts;

        private TerrenoPropietarioEdicionService()
        {
        }

        public static TerrenoPropietarioEdicionService Instance =>
            instancia.Value;

        public void VincularShell(
            Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

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
            /*
             * Cuando regresamos desde una pantalla secundaria, como el selector
             * de propietarios o el mapa, el formulario ya contiene cambios
             * realizados por el usuario.
             *
             * No debemos consultar nuevamente el terreno porque la API todavía
             * contiene el propietario anterior y reemplazaría la nueva selección.
             */
            bool regresandoAlFormulario =
                (e.Source == ShellNavigationSource.Pop ||
                 e.Source == ShellNavigationSource.PopToRoot) &&
                shellVinculado?.CurrentPage
                    is terrenoFormPage;

            if (regresandoAlFormulario)
            {
                CancellationTokenSource? operacionAnterior =
                    Interlocked.Exchange(
                        ref cargaCts,
                        null);

                CancelarYLiberar(
                    operacionAnterior);

                return;
            }

            var nueva =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    nueva);

            CancelarYLiberar(
                anterior);

            _ = RestaurarAsync(
                nueva.Token);
        }

        private async Task RestaurarAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                /*
                 * Shell dispara Navigated antes de que todas las QueryProperty
                 * queden necesariamente aplicadas. Se espera el terreno real
                 * en lugar de salir inmediatamente con propietarioId nulo.
                 */
                (terrenoFormPage Pagina,
                 TerrenoFormViewModel ViewModel,
                 int TerrenoId)? contexto =
                    await EsperarContextoAsync(
                        cancellationToken);

                if (!contexto.HasValue)
                    return;

                terrenoFormPage pagina =
                    contexto.Value.Pagina;

                TerrenoFormViewModel viewModel =
                    contexto.Value.ViewModel;

                int terrenoId =
                    contexto.Value.TerrenoId;

                ApiResult<TerrenoResponse> resultado =
                    await terrenoDetalleApiService
                        .ObtenerAsync(
                            terrenoId,
                            cancellationToken);

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (resultado.Success &&
                    resultado.Data != null)
                {
                    await AplicarTerrenoActualAsync(
                        pagina,
                        viewModel,
                        terrenoId,
                        resultado.Data,
                        cancellationToken);

                    return;
                }

                /*
                 * Respaldo compatible con respuestas antiguas:
                 * cuando el objeto recibido sí trae propietarioId, se completa
                 * el objeto visual aunque no haya sido posible consultar el
                 * detalle del terreno.
                 */
                await CompletarPropietarioPorIdAsync(
                    pagina,
                    viewModel,
                    terrenoId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Se abrió otra página o se inició una navegación nueva.
            }
            catch
            {
                /*
                 * La recuperación es auxiliar. Ante un fallo, el formulario
                 * continúa disponible y el usuario no pierde su navegación.
                 */
            }
        }

        private async Task<(
            terrenoFormPage Pagina,
            TerrenoFormViewModel ViewModel,
            int TerrenoId)?>
            EsperarContextoAsync(
                CancellationToken cancellationToken)
        {
            /*
             * Hasta 12 segundos en intervalos pequeños. Normalmente el terreno
             * está disponible durante los primeros 100-300 ms.
             */
            for (int intento = 0;
                 intento < 120;
                 intento++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (shellVinculado?.CurrentPage
                        is not terrenoFormPage pagina ||
                    pagina.BindingContext
                        is not TerrenoFormViewModel viewModel)
                {
                    return null;
                }

                int terrenoId =
                    viewModel.Terreno?.TerrenoId ??
                    0;

                if (terrenoId > 0)
                {
                    return (
                        pagina,
                        viewModel,
                        terrenoId);
                }

                /*
                 * En Crear, Shell también necesita tiempo para aplicar Mode y
                 * Terreno. Solo se concluye que es una creación real después
                 * de varias iteraciones, evitando confundir temporalmente una
                 * navegación Ver/Editar con el valor predeterminado del enum.
                 */
                if (intento >= 15 &&
                    viewModel.Mode ==
                        FormMode.FormModeSelect.Create &&
                    viewModel.Terreno != null &&
                    viewModel.Terreno.TerrenoId
                        is null or <= 0)
                {
                    return null;
                }

                await Task.Delay(
                    100,
                    cancellationToken);
            }

            return null;
        }

        private async Task AplicarTerrenoActualAsync(
            terrenoFormPage pagina,
            TerrenoFormViewModel viewModel,
            int terrenoIdEsperado,
            TerrenoResponse terrenoActual,
            CancellationToken cancellationToken)
        {
            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    if (cancellationToken
                            .IsCancellationRequested ||
                        !ReferenceEquals(
                            shellVinculado?.CurrentPage,
                            pagina) ||
                        viewModel.Terreno?.TerrenoId !=
                            terrenoIdEsperado)
                    {
                        return;
                    }

                    /*
                     * TerrenoRequest conserva la relación completa solamente
                     * en memoria. El setter del ViewModel actualiza de inmediato
                     * los textos Identificación, Nombre, Teléfono y Correo.
                     */
                    viewModel.Terreno =
                        new TerrenoRequest(
                            terrenoActual);
                });
        }

        private async Task CompletarPropietarioPorIdAsync(
            terrenoFormPage pagina,
            TerrenoFormViewModel viewModel,
            int terrenoIdEsperado,
            CancellationToken cancellationToken)
        {
            int? propietarioId =
                viewModel.Terreno?
                    .Propietario?
                    .PropietarioId ??
                viewModel.Terreno?
                    .PropietarioId;

            if (propietarioId is null or <= 0)
                return;

            ApiResult<
                ObservableCollection<PropietarioResponse>>
                resultado =
                    await propietarioApiService
                        .GetPropietariosResultAsync(
                            paraSeleccionTerreno: true,
                            cancellationToken:
                                cancellationToken);

            if (!resultado.Success ||
                resultado.Data == null)
            {
                return;
            }

            PropietarioResponse? propietario =
                resultado.Data.FirstOrDefault(
                    item =>
                        item.PropietarioId ==
                        propietarioId.Value);

            if (propietario == null)
                return;

            var propietarioTerreno =
                new TerrenoPropietarioResponse
                {
                    PropietarioId =
                        propietario.PropietarioId,

                    Identificacion =
                        propietario.Identificacion,

                    NombreCompleto =
                        propietario.NombreCompleto,

                    Telefono =
                        propietario.Telefono,

                    Correo =
                        propietario.Correo,

                    Direccion =
                        propietario.Direccion
                };

            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    if (cancellationToken
                            .IsCancellationRequested ||
                        !ReferenceEquals(
                            shellVinculado?.CurrentPage,
                            pagina) ||
                        viewModel.Terreno?.TerrenoId !=
                            terrenoIdEsperado)
                    {
                        return;
                    }

                    if (viewModel.Terreno != null)
                    {
                        viewModel.Terreno.PropietarioId =
                            propietarioId;

                        viewModel.Terreno.Propietario =
                            propietarioTerreno;
                    }

                    viewModel.PropietarioSeleccionado =
                        propietarioTerreno;
                });
        }

        private static void CancelarYLiberar(
            CancellationTokenSource? source)
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
    }
}
