using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAnalizadorPage : ContentPage
    {
        private readonly DiagnosticoIAAnalizadorViewModel viewModel;
        private bool selectorTecnicoAbierto;
        private bool validandoPermiso;

        public DiagnosticoIAAnalizadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAnalizadorViewModel();
            viewModel.PaginaCargada += OnPaginaCargada;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActivarPagina();

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
                    DiagnosticoIARoutes.InterfazAnalizador))
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
                    "No tiene permiso para consultar la bandeja del analizador.",
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

                AnalizadorListado.ScrollTo(
                    0,
                    position: ScrollToPosition.Start,
                    animate: false);
            });
        }
    }
}
