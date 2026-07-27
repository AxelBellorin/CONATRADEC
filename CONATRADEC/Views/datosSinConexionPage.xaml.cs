using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;

namespace CONATRADEC.Views
{
    public partial class datosSinConexionPage : ContentPage
    {
        private bool suscrito;
        private bool redireccionando;

        public datosSinConexionPage()
        {
            InitializeComponent();

            SizeChanged += (_, _) =>
                AplicarDisenoResponsivo(Width);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (Shell.Current != null)
            {
                /*
                 * Esta página no debe alterar globalmente el menú lateral.
                 * Se asegura que el Flyout continúe disponible en Windows y
                 * dispositivos que lo soporten.
                 */
                Shell.Current.FlyoutBehavior =
                    FlyoutBehavior.Flyout;
            }

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                await RedirigirSinPermisoAsync();
                return;
            }

            Suscribir();

            SincronizacionOfflineGlobalEstado estado =
                await SincronizacionOfflineGlobalService.Instance
                    .ObtenerEstadoAsync();

            ActualizarVista(estado);
            await ActualizarResumenPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            Desuscribir();
            base.OnDisappearing();
        }

        private async void VolverButton_Clicked(
            object? sender,
            EventArgs e)
        {
            string ruta =
                NavigationPermissionService
                    .ObtenerRutaInicialPermitida();

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(
                    ruta,
                    false);
            }
        }

        private async void DescargarTodoButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                await RedirigirSinPermisoAsync();
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await DisplayAlert(
                    "Sesión sin conexión",
                    "Para descargar o actualizar los datos debe cerrar sesión e ingresar en modo En línea.",
                    "Aceptar");
                return;
            }

            if (DebeConfirmarDatosMoviles())
            {
                bool continuar = await DisplayAlert(
                    "Uso de datos móviles",
                    "La descarga incluye análisis y fotografías. ¿Desea continuar sin Wi-Fi?",
                    "Continuar",
                    "Cancelar");

                if (!continuar)
                    return;
            }

            ResultadoSincronizacionOfflineGlobal resultado =
                await SincronizacionOfflineGlobalService.Instance
                    .DescargarOActualizarTodoAsync();

            if (!resultado.Success)
            {
                await DisplayAlert(
                    resultado.ConservaCopiaAnterior
                        ? "Se conserva la copia anterior"
                        : "Descarga incompleta",
                    resultado.Message,
                    "Aceptar");
                return;
            }

            await DisplayAlert(
                "Dispositivo preparado",
                resultado.Message,
                "Aceptar");

            await ActualizarResumenPendientesAsync();
        }

        private async Task RedirigirSinPermisoAsync()
        {
            if (redireccionando)
                return;

            redireccionando = true;

            try
            {
                await DisplayAlert(
                    "Acceso no habilitado",
                    "Su usuario no tiene habilitados los datos sin conexión.",
                    "Aceptar");

                string ruta =
                    NavigationPermissionService
                        .ObtenerRutaInicialPermitida();

                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(
                        ruta,
                        false);
                }
            }
            finally
            {
                redireccionando = false;
            }
        }

        private void Suscribir()
        {
            if (suscrito)
                return;

            SincronizacionOfflineGlobalService.Instance
                .EstadoCambiado += OnEstadoCambiado;

            AnalisisOfflineSincronizacionService.Instance
                .ColaCambiada += OnColaCambiada;

            suscrito = true;
        }

        private void Desuscribir()
        {
            if (!suscrito)
                return;

            SincronizacionOfflineGlobalService.Instance
                .EstadoCambiado -= OnEstadoCambiado;

            AnalisisOfflineSincronizacionService.Instance
                .ColaCambiada -= OnColaCambiada;

            suscrito = false;
        }

        private void OnEstadoCambiado(
            object? sender,
            SincronizacionOfflineGlobalEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                () => ActualizarVista(e.Estado));
        }

        private void OnColaCambiada(
            object? sender,
            EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                async () =>
                    await ActualizarResumenPendientesAsync());
        }

        private async Task ActualizarResumenPendientesAsync()
        {
            try
            {
                AnalisisOfflineResumenCola resumen =
                    await AnalisisOfflineDatabaseService
                        .Instance
                        .ObtenerResumenColaAsync();

                int pendientes =
                    resumen.TotalPorEnviar;

                PendientesAnalisisLabel.Text =
                    pendientes.ToString();

                RevisionAnalisisLabel.Text =
                    resumen.RequierenRevision
                        .ToString();

                ResumenPendientesBorder.BackgroundColor =
                    resumen.TieneIncidencias
                        ? Color.FromArgb("#FFF8E8")
                        : Color.FromArgb("#F7F9F8");

                ResumenPendientesBorder.Stroke =
                    new SolidColorBrush(
                        Color.FromArgb(
                            resumen.TieneIncidencias
                                ? "#EBCB78"
                                : "#DDE7E3"));
            }
            catch
            {
                PendientesAnalisisLabel.Text = "—";
                RevisionAnalisisLabel.Text = "—";
            }
        }

        private void ActualizarVista(
            SincronizacionOfflineGlobalEstado estado)
        {
            ModoSesionLabel.Text = ModoSesionService.EsEnLinea
                ? "Sesión en línea"
                : "Sesión sin conexión";

            EstadoTituloLabel.Text = estado.Mensaje;
            EstadoDetalleLabel.Text = estado.Detalle;

            ProgresoGlobal.IsVisible =
                estado.SincronizacionEnCurso;
            ProgresoGlobal.Progress = Math.Clamp(
                estado.ProgresoPorcentaje / 100d,
                0,
                1);

            DescargarTodoButton.IsEnabled =
                !estado.SincronizacionEnCurso &&
                ModoSesionService.EsEnLinea;

            DescargarTodoButton.Text =
                estado.SincronizacionEnCurso
                    ? $"Descargando {estado.ProgresoPorcentaje}%"
                    : estado.PreparacionCompleta
                        ? "Actualizar todo"
                        : "Descargar todo";

            ActualizarModulo(
                estado.MotorCalculo,
                MotorCalculoBorder,
                MotorCalculoEstadoLabel,
                MotorCalculoDetalleLabel);

            ActualizarModulo(
                estado.Catalogos,
                CatalogosBorder,
                CatalogosEstadoLabel,
                CatalogosDetalleLabel);

            ActualizarModulo(
                estado.Analisis,
                AnalisisBorder,
                AnalisisEstadoLabel,
                AnalisisDetalleLabel);

            ActualizarModulo(
                estado.Noticias,
                NoticiasBorder,
                NoticiasEstadoLabel,
                NoticiasDetalleLabel);

            ActualizarModulo(
                estado.Album,
                AlbumBorder,
                AlbumEstadoLabel,
                AlbumDetalleLabel);

            FechaSincronizacionLabel.Text =
                estado.UltimaSincronizacionCompletaUtc?
                    .ToLocalTime()
                    .ToString("dd/MM/yyyy h:mm tt")
                ?? "Todavía no disponible";

            TamanoTotalLabel.Text =
                FormatearTamano(estado.TamanoTotalBytes);

            AplicarEstadoPrincipal(estado.Estado);
        }

        private static void ActualizarModulo(
            ModuloOfflineResumen modulo,
            Border border,
            Label estadoLabel,
            Label detalleLabel)
        {
            estadoLabel.Text = modulo.Estado switch
            {
                ModuloOfflineEstados.Listo => "Listo",
                ModuloOfflineEstados.Sincronizando => "Descargando...",
                ModuloOfflineEstados.NoHabilitado => "No habilitado",
                ModuloOfflineEstados.Error => "Error",
                _ => "Pendiente"
            };

            detalleLabel.Text = string.IsNullOrWhiteSpace(modulo.Mensaje)
                ? "Pendiente."
                : modulo.Mensaje;

            string fondo;
            string borde;

            switch (modulo.Estado)
            {
                case ModuloOfflineEstados.Listo:
                    fondo = "#EEF8F2";
                    borde = "#B7DDC5";
                    break;
                case ModuloOfflineEstados.Sincronizando:
                    fondo = "#FFF8E8";
                    borde = "#F2D48A";
                    break;
                case ModuloOfflineEstados.Error:
                    fondo = "#FFF1F1";
                    borde = "#F2B8B8";
                    break;
                default:
                    fondo = "#FFFFFF";
                    borde = "#DDE7E3";
                    break;
            }

            border.BackgroundColor = Color.FromArgb(fondo);
            border.Stroke = new SolidColorBrush(
                Color.FromArgb(borde));
        }

        private void AplicarEstadoPrincipal(string estado)
        {
            string fondo = estado switch
            {
                SincronizacionOfflineGlobalEstados.Listo => "#EEF8F2",
                SincronizacionOfflineGlobalEstados.Error => "#FFF1F1",
                SincronizacionOfflineGlobalEstados.Sincronizando => "#FFF8E8",
                _ => "#F3F7FF"
            };

            string borde = estado switch
            {
                SincronizacionOfflineGlobalEstados.Listo => "#B7DDC5",
                SincronizacionOfflineGlobalEstados.Error => "#F2B8B8",
                SincronizacionOfflineGlobalEstados.Sincronizando => "#F2D48A",
                _ => "#C9D7F2"
            };

            EstadoPrincipalBorder.BackgroundColor =
                Color.FromArgb(fondo);
            EstadoPrincipalBorder.Stroke =
                new SolidColorBrush(Color.FromArgb(borde));
        }

        private static string FormatearTamano(long bytes)
        {
            if (bytes <= 0)
                return "0 MB";

            double mb = bytes / 1024d / 1024d;
            return mb < 1024
                ? $"{mb:N1} MB"
                : $"{mb / 1024d:N2} GB";
        }

        private void AplicarDisenoResponsivo(double width)
        {
            bool compacto = width > 0 && width < 760;

            ModulosGrid.ColumnDefinitions.Clear();
            ModulosGrid.RowDefinitions.Clear();

            if (compacto)
            {
                ModulosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                for (int index = 0; index < 5; index++)
                {
                    ModulosGrid.RowDefinitions.Add(
                        new RowDefinition(GridLength.Auto));
                }

                Grid.SetRow(MotorCalculoBorder, 0);
                Grid.SetColumn(MotorCalculoBorder, 0);
                Grid.SetColumnSpan(MotorCalculoBorder, 1);

                Grid.SetRow(CatalogosBorder, 1);
                Grid.SetColumn(CatalogosBorder, 0);
                Grid.SetColumnSpan(CatalogosBorder, 1);

                Grid.SetRow(AnalisisBorder, 2);
                Grid.SetColumn(AnalisisBorder, 0);
                Grid.SetColumnSpan(AnalisisBorder, 1);

                Grid.SetRow(NoticiasBorder, 3);
                Grid.SetColumn(NoticiasBorder, 0);
                Grid.SetColumnSpan(NoticiasBorder, 1);

                Grid.SetRow(AlbumBorder, 4);
                Grid.SetColumn(AlbumBorder, 0);
                Grid.SetColumnSpan(AlbumBorder, 1);
                return;
            }

            ModulosGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            ModulosGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            for (int index = 0; index < 3; index++)
            {
                ModulosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            Grid.SetRow(MotorCalculoBorder, 0);
            Grid.SetColumn(MotorCalculoBorder, 0);
            Grid.SetColumnSpan(MotorCalculoBorder, 1);

            Grid.SetRow(CatalogosBorder, 0);
            Grid.SetColumn(CatalogosBorder, 1);
            Grid.SetColumnSpan(CatalogosBorder, 1);

            Grid.SetRow(AnalisisBorder, 1);
            Grid.SetColumn(AnalisisBorder, 0);
            Grid.SetColumnSpan(AnalisisBorder, 2);

            Grid.SetRow(NoticiasBorder, 2);
            Grid.SetColumn(NoticiasBorder, 0);
            Grid.SetColumnSpan(NoticiasBorder, 1);

            Grid.SetRow(AlbumBorder, 2);
            Grid.SetColumn(AlbumBorder, 1);
            Grid.SetColumnSpan(AlbumBorder, 1);
        }

        private static bool DebeConfirmarDatosMoviles()
        {
            if (DeviceInfo.Platform != DevicePlatform.Android &&
                DeviceInfo.Platform != DevicePlatform.iOS)
            {
                return false;
            }

            return !Connectivity.Current.ConnectionProfiles
                .Contains(ConnectionProfile.WiFi);
        }
    }
}
