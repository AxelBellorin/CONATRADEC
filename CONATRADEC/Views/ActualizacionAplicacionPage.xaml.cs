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
        private CancellationTokenSource? visitaCts;

        private bool accesoSistema;
        private bool primeraAparicion = true;
        private bool inicializando;

        public ActualizacionAplicacionPage()
        {
            InitializeComponent();
            BindingContext = estado;
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
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

            if (query.TryGetValue(
                    "AccesoSistema",
                    out object? acceso) &&
                acceso is bool permitido)
            {
                accesoSistema = permitido;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            AjustarDiseno(Width);

            bool puedeAbrirAdministrativamente =
                PermissionService.Instance.HasRead(
                    InterfazCodigos.Actualizaciones);

            bool autorizado =
                accesoSistema ||
                puedeAbrirAdministrativamente;

            ContenidoAutorizado.IsVisible = autorizado;
            SinPermisoPanel.IsVisible = !autorizado;

            if (!autorizado)
                return;

            /*
             * La primera aparición representa una nueva visita real. Regresar
             * desde el permiso de instalación/instalador nativo mantiene la
             * misma instancia y no provoca otra consulta HTTP.
             */
            if (!primeraAparicion || inicializando)
                return;

            primeraAparicion = false;
            inicializando = true;

            visitaCts?.Cancel();
            visitaCts?.Dispose();
            visitaCts = new CancellationTokenSource();

            try
            {
                await estado.InicializarAsync();

                if (actualizacionRecibida is not null)
                {
                    ActualizacionDisponible disponible =
                        actualizacionRecibida;

                    actualizacionRecibida = null;

                    /*
                     * AppShell acaba de obtener este objeto del servidor; no se
                     * duplica el GET al abrir la página automáticamente.
                     */
                    await estado.EstablecerActualizacionAsync(
                        disponible);
                }
                else
                {
                    /*
                     * Entrada manual desde Configuración: siempre un GET fresco,
                     * aunque exista una actualización persistida de otra visita.
                     */
                    await estado.ComprobarAsync(
                        visitaCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // La página dejó de estar activa durante la comprobación.
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

        protected override void OnDisappearing()
        {
            /*
             * Solo se cancela la comprobación perteneciente a esta visita. La
             * descarga global dispone de su propio CancellationTokenSource y no
             * se interrumpe al navegar a otra página.
             */
            visitaCts?.Cancel();
            visitaCts?.Dispose();
            visitaCts = null;

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (!estado.PuedeCerrar)
                return true;

            return base.OnBackButtonPressed();
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0 ||
                ContenidoPrincipal == null)
            {
                return;
            }

            ContenidoPrincipal.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);

            bool compacto = width < 650;

            AjustarEncabezado(compacto);
            AjustarVersionInstalada(compacto);
            AjustarDatosNuevaVersion(compacto);
        }

        private void AjustarEncabezado(bool compacto)
        {
            if (HeaderActionsGrid == null)
                return;

            HeaderActionsGrid.ColumnDefinitions.Clear();
            HeaderActionsGrid.RowDefinitions.Clear();

            if (compacto)
            {
                HeaderActionsGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                HeaderActionsGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });
                HeaderActionsGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(BuscarHeaderButton, 0);
                Grid.SetRow(BuscarHeaderButton, 0);
                Grid.SetColumn(CerrarHeaderButton, 0);
                Grid.SetRow(CerrarHeaderButton, 1);
            }
            else
            {
                HeaderActionsGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                HeaderActionsGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                HeaderActionsGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(BuscarHeaderButton, 0);
                Grid.SetRow(BuscarHeaderButton, 0);
                Grid.SetColumn(CerrarHeaderButton, 1);
                Grid.SetRow(CerrarHeaderButton, 0);
            }
        }

        private void AjustarVersionInstalada(bool compacto)
        {
            if (VersionInfoGrid == null)
                return;

            VersionInfoGrid.ColumnDefinitions.Clear();
            VersionInfoGrid.RowDefinitions.Clear();

            if (compacto)
            {
                VersionInfoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                VersionInfoGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });
                VersionInfoGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(PlataformaInstaladaCard, 0);
                Grid.SetRow(PlataformaInstaladaCard, 1);
            }
            else
            {
                VersionInfoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                VersionInfoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                VersionInfoGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(PlataformaInstaladaCard, 1);
                Grid.SetRow(PlataformaInstaladaCard, 0);
            }
        }

        private void AjustarDatosNuevaVersion(bool compacto)
        {
            if (NuevaVersionDatosGrid == null)
                return;

            NuevaVersionDatosGrid.ColumnDefinitions.Clear();
            NuevaVersionDatosGrid.RowDefinitions.Clear();

            if (compacto)
            {
                NuevaVersionDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });

                for (int i = 0; i < 4; i++)
                {
                    NuevaVersionDatosGrid.RowDefinitions.Add(
                        new RowDefinition { Height = GridLength.Auto });
                }

                Grid.SetColumn(NuevaVersionCard, 0);
                Grid.SetRow(NuevaVersionCard, 1);
                Grid.SetColumn(NuevaPlataformaCard, 0);
                Grid.SetRow(NuevaPlataformaCard, 2);
                Grid.SetColumn(TamanoVersionCard, 0);
                Grid.SetRow(TamanoVersionCard, 3);
            }
            else
            {
                NuevaVersionDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                NuevaVersionDatosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });
                NuevaVersionDatosGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });
                NuevaVersionDatosGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(NuevaVersionCard, 1);
                Grid.SetRow(NuevaVersionCard, 0);
                Grid.SetColumn(NuevaPlataformaCard, 0);
                Grid.SetRow(NuevaPlataformaCard, 1);
                Grid.SetColumn(TamanoVersionCard, 1);
                Grid.SetRow(TamanoVersionCard, 1);
            }
        }

        private async void BuscarActualizaciones_Clicked(
            object sender,
            EventArgs e)
        {
            try
            {
                using var cts =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(30));

                await estado.ComprobarAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // El estado ya refleja la cancelación.
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

            await Shell.Current.GoToAsync("..");
        }

        private async void VolverSinPermiso_Clicked(
            object sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
