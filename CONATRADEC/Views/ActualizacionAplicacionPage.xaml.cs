using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.Views
{
    public partial class ActualizacionAplicacionPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly ActualizacionEstadoService estado =
            ActualizacionEstadoService.Instance;

        private ActualizacionDisponible? actualizacionRecibida;
        private bool inicializando;

        public ActualizacionAplicacionPage()
        {
            InitializeComponent();
            BindingContext = estado;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue(
                    "Actualizacion",
                    out object? valor) &&
                valor is ActualizacionDisponible disponible)
            {
                actualizacionRecibida = disponible;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (inicializando)
                return;

            inicializando = true;

            try
            {
                await estado.InicializarAsync();

                if (actualizacionRecibida is not null)
                {
                    ActualizacionDisponible disponible =
                        actualizacionRecibida;

                    actualizacionRecibida = null;

                    await estado.EstablecerActualizacionAsync(
                        disponible);
                }
                else if (estado.DebeComprobarAlAbrir)
                {
                    await estado.ComprobarAsync();
                }
            }
            catch (Exception ex)
            {
                await GlobalService.MostrarErrorAsync(
                    "No fue posible abrir el centro de actualizaciones. " +
                    ex.Message);
            }
            finally
            {
                inicializando = false;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (!estado.PuedeCerrar)
                return true;

            return base.OnBackButtonPressed();
        }

        private async void BuscarActualizaciones_Clicked(
            object sender,
            EventArgs e)
        {
            try
            {
                await estado.ComprobarAsync();
            }
            catch (OperationCanceledException)
            {
                // La cancelación ya queda reflejada en la interfaz.
            }
        }

        private async void AccionPrincipal_Clicked(
            object sender,
            EventArgs e)
        {
            await estado.EjecutarAccionPrincipalAsync(
                instalarAutomaticamente: true);
        }

        private async void CancelarDescarga_Clicked(
            object sender,
            EventArgs e)
        {
            bool confirmar = await DisplayAlert(
                "Cancelar descarga",
                "¿Desea cancelar la descarga de la actualización?",
                "Sí, cancelar",
                "Continuar descargando");

            if (confirmar)
                estado.CancelarDescarga();
        }

        private async void Volver_Clicked(
            object sender,
            EventArgs e)
        {
            if (!estado.PuedeCerrar)
                return;

            await Shell.Current.GoToAsync(
                "..");
        }
    }
}
