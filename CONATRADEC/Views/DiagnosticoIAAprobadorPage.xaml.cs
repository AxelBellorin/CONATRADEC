using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAprobadorPage : ContentPage
    {
        private readonly DiagnosticoIAAprobadorViewModel viewModel;
        private bool selectorTecnicoAbierto;
        private bool validandoPermiso;

        public DiagnosticoIAAprobadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAprobadorViewModel();
            viewModel.PaginaCargada += OnPaginaCargada;
            BindingContext = viewModel;
            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActivarPagina();
            AjustarResponsive(Width);

            if (!await ValidarPermisoLecturaAsync())
                return;

            await viewModel.InicializarOReanudarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            selectorTecnicoAbierto = false;
            base.OnDisappearing();
        }

        private async Task<bool> ValidarPermisoLecturaAsync()
        {
            if (PermissionService.Instance.HasRead(
                    DiagnosticoIARoutes.InterfazAprobador))
            {
                return true;
            }

            if (validandoPermiso)
                return false;

            validandoPermiso = true;
            try
            {
                await DisplayAlert(
                    "Acceso no autorizado",
                    "No tiene permiso para consultar la bandeja del aprobador.",
                    "Aceptar");

                if (Shell.Current != null)
                {
                    try
                    {
                        await Shell.Current.GoToAsync(AppRoutes.Regresar);
                    }
                    catch (InvalidOperationException)
                    {
                        await Shell.Current.GoToAsync(
                            DiagnosticoIARoutes.RutaModulo);
                    }
                }

                return false;
            }
            finally
            {
                validandoPermiso = false;
            }
        }

        private async void OnSeleccionarTecnicoClicked(
            object? sender,
            EventArgs e)
        {
            if (selectorTecnicoAbierto || viewModel.IsBusy ||
                viewModel.TecnicosFiltro.Count == 0)
            {
                return;
            }

            selectorTecnicoAbierto = true;
            try
            {
                string[] opciones = viewModel.TecnicosFiltro
                    .Select(item => item.TextoMostrar)
                    .ToArray();

                string? seleccion = await DisplayActionSheet(
                    "Técnico responsable",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    string.Equals(
                        seleccion,
                        "Cancelar",
                        StringComparison.Ordinal))
                {
                    return;
                }

                TecnicoInspeccionFiltroItem? tecnico =
                    viewModel.TecnicosFiltro.FirstOrDefault(item =>
                        string.Equals(
                            item.TextoMostrar,
                            seleccion,
                            StringComparison.Ordinal));

                if (tecnico != null)
                    viewModel.TecnicoSeleccionado = tecnico;
            }
            finally
            {
                selectorTecnicoAbierto = false;
            }
        }

        private void OnPaginaCargada(object? sender, EventArgs e)
        {
            Dispatcher.Dispatch(() =>
            {
                if (viewModel.Solicitudes.Count == 0)
                    return;

                AprobadorListado.ScrollTo(
                    0,
                    position: ScrollToPosition.Start,
                    animate: false);
            });
        }

        private void OnPageSizeChanged(object? sender, EventArgs e) =>
            AjustarResponsive(Width);

        private void AjustarResponsive(double width)
        {
            if (width <= 0)
                return;

            AprobadorListado.Margin = width switch
            {
                < 600 => new Thickness(12, 12, 12, 20),
                < 900 => new Thickness(18, 16, 18, 24),
                _ => new Thickness(26, 22, 26, 28)
            };
        }
    }
}
