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

            /*
             * GuardarYNotificar() publica primero el estado visual de la
             * descarga y el marcador persistente de "preparado" puede quedar
             * disponible unas milésimas después.
             *
             * La captura fitosanitaria depende de ese marcador. Si intentamos
             * prepararla dentro de esa pequeña ventana, PrepararAsync() cree
             * que la descarga general todavía no terminó y lanza una excepción.
             *
             * Se espera de forma breve y acotada a que la marca persistente
             * quede disponible antes de continuar.
             */
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    bool preparacionPersistida =
                        await EsperarPreparacionGlobalPersistidaAsync();

                    if (!preparacionPersistida)
                    {
                        await ActualizarPanelFitosanitarioAsync();
                        return;
                    }

                    await PrepararFitosanitariaSiCorrespondeAsync();
                    await ActualizarPanelFitosanitarioAsync();
                }
                catch
                {
                    /*
                     * Un callback de estado nunca debe derribar la interfaz.
                     * El panel reflejará el estado real y el usuario puede
                     * reintentar manualmente si fuese necesario.
                     */
                    await ActualizarPanelFitosanitarioAsync();
                }
            });
        }

        /// <summary>
        /// Espera únicamente la pequeña ventana existente entre la publicación
        /// del estado "Preparación completa" y la persistencia del marcador
        /// que consumen los módulos offline dependientes.
        ///
        /// No inicia descargas, no hace llamadas a la API y no espera de forma
        /// indefinida.
        /// </summary>
        private static async Task<bool>
            EsperarPreparacionGlobalPersistidaAsync()
        {
            const int maxIntentos = 20;
            const int esperaMilisegundos = 25;

            for (int intento = 0; intento < maxIntentos; intento++)
            {
                if (FitosanitariaOfflineService.Instance
                        .EstaPreparadoUsuarioActual)
                {
                    return true;
                }

                if (intento + 1 < maxIntentos)
                    await Task.Delay(esperaMilisegundos);
            }

            return false;
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

            /*
             * El estado global puede haberse guardado justo antes que el
             * marcador persistente utilizado por FitosanitariaOfflineService.
             * Esperamos esa persistencia para evitar una falsa condición de
             * "descarga general incompleta".
             */
            if (!FitosanitariaOfflineService.Instance
                    .EstaPreparadoUsuarioActual)
            {
                bool preparacionPersistida =
                    await EsperarPreparacionGlobalPersistidaAsync();

                if (!preparacionPersistida)
                    return;
            }

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
