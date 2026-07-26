using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;

namespace CONATRADEC.Views
{
    public partial class datosSinConexionPage :
        ContentPage
    {
        private bool suscrito;
        private bool redireccionando;

        public datosSinConexionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                await RedirigirSinPermisoAsync();
                return;
            }

            Suscribir();

            SincronizacionOfflineGlobalEstado estado =
                await SincronizacionOfflineGlobalService
                    .Instance
                    .ObtenerEstadoAsync();

            ActualizarVista(estado);

            SincronizacionOfflineGlobalService.Instance
                .VerificarActualizacionesEnSegundoPlano();
        }

        protected override void OnDisappearing()
        {
            Desuscribir();
            base.OnDisappearing();
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

            if (!EstadoConexionService.Instance.HayInternet)
            {
                await DisplayAlert(
                    "Conexión necesaria",
                    "Conecte el dispositivo a internet para descargar o actualizar todos los datos.",
                    "Aceptar");

                return;
            }

            if (DebeConfirmarDatosMoviles())
            {
                bool continuar =
                    await DisplayAlert(
                        "Uso de datos móviles",
                        "La descarga puede incluir muchas fotografías. ¿Desea continuar sin una conexión Wi-Fi?",
                        "Continuar",
                        "Cancelar");

                if (!continuar)
                    return;
            }

            ResultadoSincronizacionOfflineGlobal resultado =
                await SincronizacionOfflineGlobalService
                    .Instance
                    .DescargarOActualizarTodoAsync();

            if (!resultado.Success)
            {
                await DisplayAlert(
                    resultado.ConservaCopiaAnterior
                        ? "Se conserva la copia anterior"
                        : "Descarga incompleta",
                    resultado.Message,
                    "Aceptar");
            }
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
                    "Su usuario trabaja únicamente en línea y no tiene habilitados los datos sin conexión.",
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
                .EstadoCambiado +=
                OnEstadoCambiado;

            suscrito = true;
        }

        private void Desuscribir()
        {
            if (!suscrito)
                return;

            SincronizacionOfflineGlobalService.Instance
                .EstadoCambiado -=
                OnEstadoCambiado;

            suscrito = false;
        }

        private void OnEstadoCambiado(
            object? sender,
            SincronizacionOfflineGlobalEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                () => ActualizarVista(e.Estado));
        }

        private void ActualizarVista(
            SincronizacionOfflineGlobalEstado estado)
        {
            EstadoTituloLabel.Text =
                estado.Mensaje;

            EstadoDetalleLabel.Text =
                estado.Detalle;

            ProgresoGlobal.IsVisible =
                estado.SincronizacionEnCurso;

            ProgresoGlobal.Progress =
                Math.Clamp(
                    estado.ProgresoPorcentaje /
                    100d,
                    0,
                    1);

            DescargarTodoButton.IsEnabled =
                !estado.SincronizacionEnCurso;

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
                estado
                    .UltimaSincronizacionCompletaUtc?
                    .ToLocalTime()
                    .ToString(
                        "dd/MM/yyyy h:mm tt")
                ?? "Todavía no disponible";

            TamanoTotalLabel.Text =
                FormatearTamano(
                    estado.TamanoTotalBytes);

            AplicarEstadoPrincipal(
                estado.Estado);
        }

        private static void ActualizarModulo(
            ModuloOfflineResumen modulo,
            Border border,
            Label estadoLabel,
            Label detalleLabel)
        {
            estadoLabel.Text =
                ObtenerEstadoVisible(
                    modulo.Estado);

            detalleLabel.Text =
                string.IsNullOrWhiteSpace(
                    modulo.Mensaje)
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

                case ModuloOfflineEstados.NoHabilitado:
                    fondo = "#F8FAF9";
                    borde = "#DDE7E3";
                    break;

                case ModuloOfflineEstados.Error:
                    fondo = "#FFF1F1";
                    borde = "#F2B8B8";
                    break;

                default:
                    fondo = "White";
                    borde = "#DDE7E3";
                    break;
            }

            border.BackgroundColor =
                Color.FromArgb(fondo);

            border.Stroke =
                new SolidColorBrush(
                    Color.FromArgb(borde));
        }

        private void AplicarEstadoPrincipal(
            string estado)
        {
            string fondo;
            string borde;

            switch (estado)
            {
                case SincronizacionOfflineGlobalEstados.Listo:
                    fondo = "#EEF8F2";
                    borde = "#B7DDC5";
                    break;

                case SincronizacionOfflineGlobalEstados.Sincronizando:
                case SincronizacionOfflineGlobalEstados
                    .ActualizacionDisponible:
                    fondo = "#FFF8E8";
                    borde = "#F2D48A";
                    break;

                case SincronizacionOfflineGlobalEstados.ListoConAviso:
                    fondo = "#FFF8E8";
                    borde = "#F2D48A";
                    break;

                case SincronizacionOfflineGlobalEstados.Error:
                    fondo = "#FFF1F1";
                    borde = "#F2B8B8";
                    break;

                default:
                    fondo = "#F3F7FF";
                    borde = "#C9D7F2";
                    break;
            }

            EstadoPrincipalBorder.BackgroundColor =
                Color.FromArgb(fondo);

            EstadoPrincipalBorder.Stroke =
                new SolidColorBrush(
                    Color.FromArgb(borde));
        }

        private static string ObtenerEstadoVisible(
            string estado) =>
            estado switch
            {
                ModuloOfflineEstados.Listo =>
                    "Listo",

                ModuloOfflineEstados.Sincronizando =>
                    "Descargando...",

                ModuloOfflineEstados.NoHabilitado =>
                    "No habilitado",

                ModuloOfflineEstados.Error =>
                    "Error",

                _ =>
                    "Pendiente"
            };

        private static string FormatearTamano(
            long bytes)
        {
            if (bytes <= 0)
                return "0 MB";

            double megabytes =
                bytes /
                1024d /
                1024d;

            if (megabytes < 1024)
                return $"{megabytes:N1} MB";

            return $"{megabytes / 1024d:N2} GB";
        }

        private static bool DebeConfirmarDatosMoviles()
        {
            if (DeviceInfo.Platform !=
                    DevicePlatform.Android &&
                DeviceInfo.Platform !=
                    DevicePlatform.iOS)
            {
                return false;
            }

            IEnumerable<ConnectionProfile> perfiles =
                Connectivity.Current
                    .ConnectionProfiles;

            return !perfiles.Contains(
                ConnectionProfile.WiFi);
        }
    }
}
