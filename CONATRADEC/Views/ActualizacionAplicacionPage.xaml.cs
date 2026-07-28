using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Views
{
    public partial class ActualizacionAplicacionPage :
        ContentPage,
        IQueryAttributable
    {
        private ActualizacionDisponible? actualizacion;
        private CancellationTokenSource? operacionCts;
        private string? rutaDescargada;
        private bool ocupada;
        private bool esperandoPermisoInstalacion;
        private double progresoDescarga;
        private string mensajeEstado = string.Empty;

        public ActualizacionAplicacionPage()
        {
            InitializeComponent();
            BindingContext = this;

            DescargarEInstalarCommand = new Command(
                async () => await DescargarEInstalarAsync(),
                () => PuedeEjecutar);
        }

        public Command DescargarEInstalarCommand { get; }

        public string VersionInstalada =>
            $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

        public string VersionNueva =>
            actualizacion is null
                ? "Consultando..."
                : $"{actualizacion.VersionNombre} ({actualizacion.VersionCodigo})";

        public string NuevaVersionTitulo =>
            actualizacion is null
                ? "Buscando actualización"
                : $"ConatraCafé Soil {actualizacion.VersionNombre}";

        public string ResumenVersion =>
            actualizacion is null
                ? "Conectando con el servidor..."
                : $"Canal {actualizacion.Canal.ToLowerInvariant()} · compilación {actualizacion.VersionCodigo}";

        public string PlataformaVisible =>
            actualizacion is null
                ? "—"
                : actualizacion.Plataforma.Equals(
                    "ANDROID",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Android"
                    : "Windows";

        public string TamanoVisible =>
            actualizacion?.TamanoVisible ?? "—";

        public string NotasVersion =>
            string.IsNullOrWhiteSpace(actualizacion?.NotasVersion)
                ? "Esta versión incluye mejoras generales y correcciones de estabilidad."
                : actualizacion.NotasVersion;

        public bool EsObligatoria =>
            actualizacion?.Obligatoria == true;

        public bool PuedeCerrar =>
            !EsObligatoria && !Ocupada;

        public bool Ocupada
        {
            get => ocupada;
            private set
            {
                if (ocupada == value)
                    return;

                ocupada = value;
                Notificar();
                Notificar(nameof(PuedeEjecutar));
                Notificar(nameof(PuedeCerrar));
                Notificar(nameof(MostrandoProgreso));
                DescargarEInstalarCommand.ChangeCanExecute();
            }
        }

        public bool PuedeEjecutar =>
            actualizacion is not null && !Ocupada;

        public bool MostrandoProgreso =>
            Ocupada && rutaDescargada is null;

        public double ProgresoDescarga
        {
            get => progresoDescarga;
            private set
            {
                double valor = Math.Clamp(value, 0, 1);
                if (Math.Abs(progresoDescarga - valor) < 0.0001)
                    return;

                progresoDescarga = valor;
                Notificar();
                Notificar(nameof(PorcentajeTexto));
            }
        }

        public string PorcentajeTexto =>
            $"{ProgresoDescarga * 100:0}%";

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                if (mensajeEstado == value)
                    return;

                mensajeEstado = value;
                Notificar();
                Notificar(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public string TextoBotonPrincipal =>
            esperandoPermisoInstalacion
                ? "Continuar instalación"
                : rutaDescargada is not null
                    ? "Instalar actualización"
                    : "Descargar e instalar";

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue(
                    "Actualizacion",
                    out object? valor) &&
                valor is ActualizacionDisponible disponible)
            {
                EstablecerActualizacion(disponible);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (actualizacion is null && !Ocupada)
                await ConsultarAsync();
        }

        protected override void OnDisappearing()
        {
            if (!esperandoPermisoInstalacion)
                operacionCts?.Cancel();

            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            if (EsObligatoria || Ocupada)
                return true;

            return base.OnBackButtonPressed();
        }

        private async Task ConsultarAsync()
        {
            Ocupada = true;
            MensajeEstado = "Consultando la versión más reciente...";

            try
            {
                actualizacion = await ActualizacionAplicacionService
                    .Instance
                    .ComprobarActualizacionAsync();

                if (actualizacion is null)
                {
                    MensajeEstado = "La aplicación ya está actualizada.";
                    await GlobalService.MostrarInformacionAsync(
                        "ConatraCafé Soil ya tiene la versión más reciente.");

                    await Shell.Current.GoToAsync("..");
                    return;
                }

                MensajeEstado = string.Empty;
                NotificarTodo();
            }
            catch (Exception ex)
            {
                MensajeEstado =
                    "No fue posible consultar las actualizaciones.";

                await GlobalService.MostrarErrorAsync(
                    $"No fue posible consultar las actualizaciones. {ex.Message}");
            }
            finally
            {
                Ocupada = false;
            }
        }

        private async Task DescargarEInstalarAsync()
        {
            if (actualizacion is null || Ocupada)
                return;

            Ocupada = true;
            operacionCts?.Cancel();
            operacionCts?.Dispose();
            operacionCts = new CancellationTokenSource();

            try
            {
                if (rutaDescargada is null)
                {
                    MensajeEstado =
                        "Descargando y validando el archivo...";
                    ProgresoDescarga = 0;

                    var progreso = new Progress<double>(valor =>
                    {
                        ProgresoDescarga = valor / 100d;
                    });

                    rutaDescargada = await ActualizacionAplicacionService
                        .Instance
                        .DescargarAsync(
                            actualizacion,
                            progreso,
                            operacionCts.Token);

                    Notificar(nameof(TextoBotonPrincipal));
                }

                MensajeEstado = "Abriendo el instalador del sistema...";

                ResultadoInstalacionActualizacion resultado =
                    await ActualizacionInstaladorService
                        .IniciarInstalacionAsync(rutaDescargada);

                esperandoPermisoInstalacion = resultado.RequierePermiso;
                MensajeEstado = resultado.Mensaje;
                Notificar(nameof(TextoBotonPrincipal));

                if (resultado.RequierePermiso)
                {
                    await GlobalService.MostrarAdvertenciaAsync(
                        resultado.Mensaje);
                }
                else if (!resultado.Iniciado)
                {
                    await GlobalService.MostrarErrorAsync(
                        resultado.Mensaje);
                }
            }
            catch (OperationCanceledException)
            {
                MensajeEstado = "La descarga fue cancelada.";
            }
            catch (Exception ex)
            {
                rutaDescargada = null;
                esperandoPermisoInstalacion = false;
                ProgresoDescarga = 0;
                MensajeEstado = "No fue posible completar la actualización.";
                Notificar(nameof(TextoBotonPrincipal));

                await GlobalService.MostrarErrorAsync(
                    $"No fue posible completar la actualización. {ex.Message}");
            }
            finally
            {
                Ocupada = false;
            }
        }

        private async void Volver_Clicked(
            object sender,
            EventArgs e)
        {
            if (!PuedeCerrar)
                return;

            await Shell.Current.GoToAsync("..");
        }

        private void EstablecerActualizacion(
            ActualizacionDisponible disponible)
        {
            actualizacion = disponible;
            rutaDescargada = null;
            esperandoPermisoInstalacion = false;
            ProgresoDescarga = 0;
            MensajeEstado = string.Empty;
            NotificarTodo();
        }

        private void NotificarTodo()
        {
            foreach (string propiedad in new[]
                     {
                         nameof(VersionInstalada),
                         nameof(VersionNueva),
                         nameof(NuevaVersionTitulo),
                         nameof(ResumenVersion),
                         nameof(PlataformaVisible),
                         nameof(TamanoVisible),
                         nameof(NotasVersion),
                         nameof(EsObligatoria),
                         nameof(PuedeCerrar),
                         nameof(PuedeEjecutar),
                         nameof(TextoBotonPrincipal)
                     })
            {
                Notificar(propiedad);
            }

            DescargarEInstalarCommand.ChangeCanExecute();
        }

        private void Notificar(
            [CallerMemberName] string? nombre = null)
        {
            OnPropertyChanged(nombre);
        }
    }
}
