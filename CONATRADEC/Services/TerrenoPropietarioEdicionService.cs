using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Completa el contexto de edición de un terreno únicamente cuando el DTO
    /// recibido por navegación no trae información suficiente.
    ///
    /// El listado paginado ya incluye propietario y ubicación administrativa,
    /// por lo que Ver/Editar no deben ejecutar otro GET durante la misma visita.
    /// Los GET dirigidos se conservan solamente como recuperación para flujos
    /// históricos que todavía puedan enviar un TerrenoRequest incompleto.
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
                TerrenoRequest? terreno = viewModel.Terreno;

                /*
                 * El DTO del listado administrativo ya transporta todo lo que
                 * necesita el formulario para propietario y ubicación. Durante
                 * la misma visita esa información es la fuente correcta y evita
                 * un GET /api/terreno/{id} por cada Ver/Editar.
                 */
                if (TienePropietarioCompleto(terreno) &&
                    TieneUbicacionCompleta(terreno))
                {
                    return;
                }

                /*
                 * Si solamente falta materializar el propietario y ya tenemos
                 * su ID, se consulta ese registro puntual en vez de descargar
                 * o refrescar nuevamente el terreno completo.
                 */
                if (TieneUbicacionCompleta(terreno) &&
                    ObtenerPropietarioId(terreno) is > 0)
                {
                    await CompletarPropietarioPorIdAsync(
                        pagina,
                        viewModel,
                        terrenoId,
                        cancellationToken);
                    return;
                }

                /*
                 * Compatibilidad con consumidores históricos que todavía
                 * pudieran navegar con un TerrenoRequest incompleto. Es un GET
                 * dirigido por ID y nunca una descarga del catálogo completo.
                 */
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
                ObtenerPropietarioId(viewModel.Terreno);

            if (propietarioId is null or <= 0)
                return;

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

        private static int? ObtenerPropietarioId(
            TerrenoRequest? terreno) =>
            terreno?.Propietario?.PropietarioId ??
            terreno?.PropietarioId;

        private static bool TienePropietarioCompleto(
            TerrenoRequest? terreno) =>
            terreno?.Propietario?.PropietarioId is > 0;

        private static bool TieneUbicacionCompleta(
            TerrenoRequest? terreno) =>
            terreno?.MunicipioId is > 0 &&
            terreno.Ubicacion?.MunicipioId is > 0 &&
            terreno.Ubicacion.DepartamentoId is > 0 &&
            terreno.Ubicacion.PaisId is > 0;

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
