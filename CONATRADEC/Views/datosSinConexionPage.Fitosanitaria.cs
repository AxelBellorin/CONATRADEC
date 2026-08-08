using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Views
{
    public partial class datosSinConexionPage
    {
        private bool fitosanitariaOfflineSuscrita;
        private int actualizandoPanelFitosanitario;

        private async void FitosanitariaOfflineBorder_Loaded(
            object? sender,
            EventArgs e)
        {
            SuscribirFitosanitariaOffline();
            await PrepararFitosanitariaSiCorrespondeAsync();
            await ActualizarPanelFitosanitarioAsync();
        }

        private void FitosanitariaOfflineBorder_Unloaded(
            object? sender,
            EventArgs e)
        {
            DesuscribirFitosanitariaOffline();
        }

        private async void FitosanitariaOfflineActualizarButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (!FitosanitariaOfflineService.Instance.TienePermisoModulo)
            {
                await DisplayAlert(
                    "Inspección fitosanitaria",
                    "Su usuario no tiene habilitada la captura fitosanitaria sin conexión.",
                    "Aceptar");
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await DisplayAlert(
                    "Sesión sin conexión",
                    "La copia ya descargada puede utilizarse para capturar inspecciones. Para preparar o sincronizar datos debe iniciar una sesión en línea.",
                    "Aceptar");
                return;
            }

            FitosanitariaOfflineActualizarButton.IsEnabled = false;
            try
            {
                await PrepararFitosanitariaSiCorrespondeAsync(forzar: true);
                FitosanitariaOfflineSincronizacionResultado resultado =
                    await FitosanitariaOfflineService.Instance
                        .SincronizarPendientesAsync();

                if (!string.IsNullOrWhiteSpace(resultado.Message))
                {
                    await DisplayAlert(
                        "Inspección fitosanitaria",
                        resultado.Message,
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Inspección fitosanitaria",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                FitosanitariaOfflineActualizarButton.IsEnabled = true;
                await ActualizarPanelFitosanitarioAsync();
            }
        }

        private void SuscribirFitosanitariaOffline()
        {
            if (fitosanitariaOfflineSuscrita)
                return;

            FitosanitariaOfflineService.Instance.ColaCambiada +=
                OnColaFitosanitariaCambiada;
            SincronizacionOfflineGlobalService.Instance.EstadoCambiado +=
                OnEstadoGlobalParaFitosanitaria;
            fitosanitariaOfflineSuscrita = true;
        }

        private void DesuscribirFitosanitariaOffline()
        {
            if (!fitosanitariaOfflineSuscrita)
                return;

            FitosanitariaOfflineService.Instance.ColaCambiada -=
                OnColaFitosanitariaCambiada;
            SincronizacionOfflineGlobalService.Instance.EstadoCambiado -=
                OnEstadoGlobalParaFitosanitaria;
            fitosanitariaOfflineSuscrita = false;
        }

        private void OnColaFitosanitariaCambiada(
            object? sender,
            EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                async () => await ActualizarPanelFitosanitarioAsync());
        }

        private void OnEstadoGlobalParaFitosanitaria(
            object? sender,
            SincronizacionOfflineGlobalEventArgs e)
        {
            if (!e.Estado.PreparacionCompleta || !ModoSesionService.EsEnLinea)
                return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await PrepararFitosanitariaSiCorrespondeAsync();
                await ActualizarPanelFitosanitarioAsync();
            });
        }

        /// <summary>
        /// Descargar todo ya contiene los terrenos requeridos por la captura.
        /// Cuando esa descarga queda completa se habilita también la cola
        /// fitosanitaria, sin duplicar catálogos ni fotografías de otros módulos.
        /// </summary>
        private async Task PrepararFitosanitariaSiCorrespondeAsync(
            bool forzar = false)
        {
            if (!ModoSesionService.EsEnLinea ||
                !FitosanitariaOfflineService.Instance.TienePermisoModulo)
            {
                return;
            }

            SincronizacionOfflineGlobalEstado global =
                await SincronizacionOfflineGlobalService.Instance
                    .ObtenerEstadoAsync();

            if (!global.PreparacionCompleta)
                return;

            if (forzar ||
                !FitosanitariaOfflineService.Instance.EstaPreparadoUsuarioActual)
            {
                await FitosanitariaOfflineService.Instance.PrepararAsync();
            }

            FitosanitariaOfflineService.Instance
                .SolicitarSincronizacionEnSegundoPlano();
        }

        private async Task ActualizarPanelFitosanitarioAsync()
        {
            if (Interlocked.Exchange(ref actualizandoPanelFitosanitario, 1) == 1)
                return;

            try
            {
                FitosanitariaOfflineResumen resumen =
                    await FitosanitariaOfflineService.Instance
                        .ObtenerResumenAsync();

                if (!FitosanitariaOfflineService.Instance.TienePermisoModulo)
                {
                    FitosanitariaOfflineEstadoLabel.Text = "No habilitado";
                    FitosanitariaOfflineDetalleLabel.Text = resumen.Mensaje;
                    AplicarEstiloFitosanitaria("#FFFFFF", "#DDE7E3");
                    FitosanitariaOfflineActualizarButton.IsVisible = false;
                    return;
                }

                FitosanitariaOfflineActualizarButton.IsVisible = true;
                FitosanitariaOfflineActualizarButton.IsEnabled =
                    ModoSesionService.EsEnLinea;

                if (!resumen.Preparado)
                {
                    FitosanitariaOfflineEstadoLabel.Text = "Pendiente";
                    FitosanitariaOfflineDetalleLabel.Text = resumen.Mensaje;
                    AplicarEstiloFitosanitaria("#FFF8E8", "#EBCB78");
                    return;
                }

                FitosanitariaOfflineEstadoLabel.Text =
                    resumen.InspeccionesPendientes == 0
                        ? "Listo"
                        : $"{resumen.InspeccionesPendientes} pendiente(s) de enviar";

                FitosanitariaOfflineDetalleLabel.Text = resumen.Mensaje;
                AplicarEstiloFitosanitaria(
                    resumen.InspeccionesPendientes == 0 ? "#EEF8F2" : "#FFF8E8",
                    resumen.InspeccionesPendientes == 0 ? "#B7DDC5" : "#EBCB78");
            }
            catch
            {
                FitosanitariaOfflineEstadoLabel.Text = "Error";
                FitosanitariaOfflineDetalleLabel.Text =
                    "No fue posible comprobar la cola fitosanitaria local.";
                AplicarEstiloFitosanitaria("#FFF1F1", "#F2B8B8");
            }
            finally
            {
                Interlocked.Exchange(ref actualizandoPanelFitosanitario, 0);
            }
        }

        private void AplicarEstiloFitosanitaria(string fondo, string borde)
        {
            FitosanitariaOfflineBorder.BackgroundColor = Color.FromArgb(fondo);
            FitosanitariaOfflineBorder.Stroke =
                new SolidColorBrush(Color.FromArgb(borde));
        }
    }
}
