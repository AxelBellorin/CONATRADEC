using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Recupera el propietario del terreno durante la primera apertura de la
    /// edición cuando el objeto Terreno trae PropietarioId, pero no incluye el
    /// objeto Propietario completo.
    /// </summary>
    public sealed class TerrenoPropietarioEdicionService
    {
        private static readonly Lazy<
            TerrenoPropietarioEdicionService> instancia =
                new(() => new TerrenoPropietarioEdicionService());

        private readonly PropietarioApiService propietarioApiService =
            new();

        private Shell? shellVinculado;
        private CancellationTokenSource? cargaCts;

        private TerrenoPropietarioEdicionService()
        {
        }

        public static TerrenoPropietarioEdicionService Instance =>
            instancia.Value;

        public void VincularShell(Shell shell)
        {
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
            CancellationTokenSource nueva = new();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    nueva);

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

            _ = RestaurarAsync(nueva.Token);
        }

        private async Task RestaurarAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                for (int intento = 0; intento < 120; intento++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (shellVinculado?.CurrentPage
                            is not terrenoFormPage pagina ||
                        pagina.BindingContext
                            is not TerrenoFormViewModel viewModel)
                    {
                        return;
                    }

                    int? propietarioId =
                        viewModel.Terreno?.Propietario?.PropietarioId ??
                        viewModel.Terreno?.PropietarioId;

                    if (viewModel.Mode ==
                            FormMode.FormModeSelect.Create ||
                        propietarioId is null or <= 0)
                    {
                        return;
                    }

                    if (viewModel.PropietarioSeleccionado?
                            .PropietarioId == propietarioId)
                    {
                        return;
                    }

                    if (viewModel.IsBusy)
                    {
                        await Task.Delay(50, cancellationToken);
                        continue;
                    }

                    ApiResult<
                        System.Collections.ObjectModel.ObservableCollection<
                            PropietarioResponse>> resultado =
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
                        resultado.Data.FirstOrDefault(x =>
                            x.PropietarioId == propietarioId.Value);

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

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if ((viewModel.Terreno?.PropietarioId ??
                             viewModel.Terreno?.Propietario?
                                 .PropietarioId) !=
                            propietarioId)
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

                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                /*
                 * No se bloquea el formulario. El usuario todavía puede usar
                 * Seleccionar para escoger manualmente un propietario.
                 */
            }
        }
    }
}
