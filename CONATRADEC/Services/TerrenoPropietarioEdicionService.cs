using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Sincroniza el formulario de terreno con la relación propietario-terreno
    /// vigente antes de mostrar Ver o Editar.
    /// </summary>
    public sealed class TerrenoPropietarioEdicionService
    {
        private static readonly Lazy<TerrenoPropietarioEdicionService>
            instancia = new(() => new TerrenoPropietarioEdicionService());

        private readonly TerrenoDetalleActualApiService
            terrenoDetalleApiService = new();
        private readonly PropietarioApiService
            propietarioApiService = new();

        private Shell? shellVinculado;
        private CancellationTokenSource? cargaCts;

        private TerrenoPropietarioEdicionService()
        {
        }

        public static TerrenoPropietarioEdicionService Instance =>
            instancia.Value;

        public void VincularShell(Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            if (ReferenceEquals(shellVinculado, shell))
                return;

            if (shellVinculado != null)
                shellVinculado.Navigated -= Shell_Navigated;

            shellVinculado = shell;
            shellVinculado.Navigated += Shell_Navigated;
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            bool regresandoAlFormulario =
                (e.Source == ShellNavigationSource.Pop ||
                 e.Source == ShellNavigationSource.PopToRoot) &&
                shellVinculado?.CurrentPage is terrenoFormPage;

            if (regresandoAlFormulario)
            {
                CancellationTokenSource? anterior =
                    Interlocked.Exchange(ref cargaCts, null);
                CancelarYLiberar(anterior);
                return;
            }

            var nueva = new CancellationTokenSource();
            CancellationTokenSource? operacionAnterior =
                Interlocked.Exchange(ref cargaCts, nueva);
            CancelarYLiberar(operacionAnterior);
            _ = RestaurarAsync(nueva.Token);
        }

        private async Task RestaurarAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                (terrenoFormPage Pagina,
                 TerrenoFormViewModel ViewModel,
                 int TerrenoId)? contexto =
                    await EsperarContextoAsync(cancellationToken);

                if (!contexto.HasValue)
                    return;

                terrenoFormPage pagina = contexto.Value.Pagina;
                TerrenoFormViewModel viewModel = contexto.Value.ViewModel;
                int terrenoId = contexto.Value.TerrenoId;

                ApiResult<TerrenoResponse> resultado =
                    await terrenoDetalleApiService.ObtenerAsync(
                        terrenoId,
                        cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (resultado.Success && resultado.Data != null)
                {
                    await AplicarTerrenoActualAsync(
                        pagina,
                        viewModel,
                        terrenoId,
                        resultado.Data,
                        cancellationToken);
                    return;
                }

                await CompletarPropietarioPorIdAsync(
                    pagina,
                    viewModel,
                    terrenoId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Es una recuperación auxiliar; no bloquea el formulario.
            }
        }

        private async Task<(
            terrenoFormPage Pagina,
            TerrenoFormViewModel ViewModel,
            int TerrenoId)?> EsperarContextoAsync(
                CancellationToken cancellationToken)
        {
            for (int intento = 0; intento < 120; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (shellVinculado?.CurrentPage
                        is not terrenoFormPage pagina ||
                    pagina.BindingContext
                        is not TerrenoFormViewModel viewModel)
                {
                    return null;
                }

                int terrenoId = viewModel.Terreno?.TerrenoId ?? 0;

                if (terrenoId > 0)
                    return (pagina, viewModel, terrenoId);

                if (intento >= 15 &&
                    viewModel.Mode == FormMode.FormModeSelect.Create &&
                    viewModel.Terreno != null &&
                    viewModel.Terreno.TerrenoId is null or <= 0)
                {
                    return null;
                }

                await Task.Delay(100, cancellationToken);
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
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested ||
                    !ReferenceEquals(
                        shellVinculado?.CurrentPage,
                        pagina) ||
                    viewModel.Terreno?.TerrenoId != terrenoIdEsperado)
                {
                    return;
                }

                viewModel.Terreno = new TerrenoRequest(terrenoActual);
            });
        }

        private async Task CompletarPropietarioPorIdAsync(
            terrenoFormPage pagina,
            TerrenoFormViewModel viewModel,
            int terrenoIdEsperado,
            CancellationToken cancellationToken)
        {
            int? propietarioId =
                viewModel.Terreno?.Propietario?.PropietarioId ??
                viewModel.Terreno?.PropietarioId;

            if (propietarioId is null or <= 0)
                return;

            /*
             * Antes se descargaba el catálogo completo de propietarios para
             * localizar uno. Ahora se consulta únicamente el propietario por ID.
             */
            ApiResult<PropietarioResponse> resultado =
                await propietarioApiService.ObtenerDisponiblePorIdAsync(
                    propietarioId.Value,
                    cancellationToken);

            if (!resultado.Success || resultado.Data == null)
                return;

            PropietarioResponse propietario = resultado.Data;

            var propietarioTerreno = new TerrenoPropietarioResponse
            {
                PropietarioId = propietario.PropietarioId,
                Identificacion = propietario.Identificacion,
                NombreCompleto = propietario.NombreCompleto,
                Telefono = propietario.Telefono,
                Correo = propietario.Correo,
                Direccion = propietario.Direccion
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested ||
                    !ReferenceEquals(
                        shellVinculado?.CurrentPage,
                        pagina) ||
                    viewModel.Terreno?.TerrenoId != terrenoIdEsperado)
                {
                    return;
                }

                if (viewModel.Terreno != null)
                {
                    viewModel.Terreno.PropietarioId = propietarioId;
                    viewModel.Terreno.Propietario = propietarioTerreno;
                }

                viewModel.PropietarioSeleccionado = propietarioTerreno;
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
