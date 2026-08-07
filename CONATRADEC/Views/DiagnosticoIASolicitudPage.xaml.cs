using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.ObjectModel;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIASolicitudPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIASolicitudViewModel viewModel;
        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;

        private string modoActual = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool cargandoTecnicos;

        public DiagnosticoIASolicitudPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIASolicitudViewModel();
            BindingContext = viewModel;
        }

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public bool MostrarFiltroTecnico =>
            string.Equals(
                modoActual,
                DiagnosticoIARoutes.ModoHistorial,
                StringComparison.OrdinalIgnoreCase);

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            if (query.TryGetValue("modo", out object? modo))
            {
                modoActual = DiagnosticoIARoutes.NormalizarModo(
                    modo?.ToString());
                viewModel.AplicarModo(modoActual);
                query.Remove("modo");

                OnPropertyChanged(nameof(MostrarFiltroTecnico));
            }

            if (query.TryGetValue(
                    "TerrenoSeleccionado",
                    out object? valor) &&
                valor is TerrenoBusquedaIAItem terreno)
            {
                viewModel.AplicarTerrenoSeleccionado(terreno);
                query.Remove("TerrenoSeleccionado");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (MostrarFiltroTecnico)
            {
                await CargarTecnicosAsync();
            }
            else
            {
                bandejaApi.EstablecerTecnicoContextual(modoActual, null);
            }

            await viewModel.InicializarAsync();
        }

        private async Task CargarTecnicosAsync()
        {
            if (cargandoTecnicos)
                return;

            cargandoTecnicos = true;

            try
            {
                TecnicoInspeccionFiltroRespuesta respuesta =
                    await bandejaApi.ObtenerTecnicosAsync(modoActual);

                int? seleccionadoId =
                    bandejaApi.ObtenerTecnicoContextual(modoActual);

                TecnicosFiltro.Clear();
                TecnicosFiltro.Add(TecnicoInspeccionFiltroItem.Todos());

                foreach (TecnicoInspeccionFiltroItem tecnico
                         in respuesta.Tecnicos)
                {
                    TecnicosFiltro.Add(tecnico);
                }

                int indice = 0;
                if (seleccionadoId is > 0)
                {
                    int encontrado = TecnicosFiltro
                        .Select((item, posicion) => new { item, posicion })
                        .Where(item =>
                            item.item.UsuarioTecnicoId == seleccionadoId.Value)
                        .Select(item => item.posicion)
                        .FirstOrDefault();

                    indice = encontrado > 0 ? encontrado : 0;
                }

                TecnicoFiltroPicker.SelectedIndex = indice;
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Filtro de técnicos",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "No fue posible cargar los técnicos responsables."
                        : ex.Message,
                    "Aceptar");
            }
            finally
            {
                cargandoTecnicos = false;
            }
        }

        private void OnTecnicoFiltroChanged(
            object sender,
            EventArgs e)
        {
            if (cargandoTecnicos || !MostrarFiltroTecnico)
                return;

            TecnicoInspeccionFiltroItem? seleccionado =
                TecnicoFiltroPicker.SelectedItem as
                    TecnicoInspeccionFiltroItem;

            bandejaApi.EstablecerTecnicoContextual(
                modoActual,
                seleccionado?.UsuarioTecnicoId is > 0
                    ? seleccionado.UsuarioTecnicoId
                    : null);

            if (viewModel.BuscarInspeccionesCommand.CanExecute(null))
                viewModel.BuscarInspeccionesCommand.Execute(null);
        }

        private void OnLimpiarFiltrosClicked(
            object sender,
            EventArgs e)
        {
            bandejaApi.EstablecerTecnicoContextual(modoActual, null);

            cargandoTecnicos = true;
            TecnicoFiltroPicker.SelectedIndex =
                TecnicosFiltro.Count > 0 ? 0 : -1;
            cargandoTecnicos = false;

            if (viewModel.LimpiarFiltrosCommand.CanExecute(null))
                viewModel.LimpiarFiltrosCommand.Execute(null);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.EsVisorAbierto)
            {
                viewModel.CerrarVisor();
                return true;
            }

            return base.OnBackButtonPressed();
        }
    }
}
