using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.ObjectModel;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Vista disponible dentro de Mis inspecciones. Se conserva el modo
    /// anterior de decisiones para que los enlaces históricos sigan abriendo
    /// el filtro correcto, pero ya no se presenta como un módulo separado.
    /// </summary>
    public sealed class VistaMisInspeccionesItem
    {
        public VistaMisInspeccionesItem(string modo, string nombre)
        {
            Modo = modo;
            Nombre = nombre;
        }

        public string Modo { get; }
        public string Nombre { get; }

        public override string ToString() => Nombre;
    }

    public partial class DiagnosticoIASolicitudPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIASolicitudViewModel viewModel;
        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;

        private string modoActual = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool cargandoTecnicos;
        private bool cambiandoVista;

        public DiagnosticoIASolicitudPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIASolicitudViewModel();
            BindingContext = viewModel;

            VistasInspecciones.Add(
                new VistaMisInspeccionesItem(
                    DiagnosticoIARoutes.ModoMisInspecciones,
                    "Todas mis inspecciones"));
            VistasInspecciones.Add(
                new VistaMisInspeccionesItem(
                    DiagnosticoIARoutes.ModoDecisionesPendientes,
                    "Requieren decisión técnica"));
        }

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public ObservableCollection<VistaMisInspeccionesItem>
            VistasInspecciones { get; } = [];

        /// <summary>
        /// Mis inspecciones y sus decisiones comparten el mismo encabezado.
        /// Historial conserva su denominación porque responde a otro alcance.
        /// </summary>
        public string TituloVista => string.Equals(
                modoActual,
                DiagnosticoIARoutes.ModoHistorial,
                StringComparison.OrdinalIgnoreCase)
            ? "Historial de inspecciones"
            : viewModel.EsModoNueva
                ? "Nueva inspección fitosanitaria"
                : "Mis inspecciones";

        public string SubtituloVista => viewModel.EsModoNueva
            ? "Registre la evidencia y la fecha real de identificación en campo."
            : string.Equals(
                modoActual,
                DiagnosticoIARoutes.ModoDecisionesPendientes,
                StringComparison.OrdinalIgnoreCase)
                ? "Filtro activo: inspecciones con fotografías que requieren una decisión técnica."
                : "Consulte el avance, errores, devoluciones y decisiones de sus inspecciones.";

        public bool MostrarFiltroTecnico =>
            string.Equals(
                modoActual,
                DiagnosticoIARoutes.ModoHistorial,
                StringComparison.OrdinalIgnoreCase);

        public bool MostrarSelectorMisInspecciones =>
            !viewModel.EsModoNueva && !MostrarFiltroTecnico;

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
                OnPropertyChanged(nameof(MostrarSelectorMisInspecciones));
                OnPropertyChanged(nameof(TituloVista));
                OnPropertyChanged(nameof(SubtituloVista));
                SincronizarSelectorVista();
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
            SincronizarSelectorVista();

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

        private void SincronizarSelectorVista()
        {
            if (VistaInspeccionesPicker == null ||
                VistasInspecciones.Count == 0 ||
                viewModel.EsModoNueva ||
                MostrarFiltroTecnico)
            {
                return;
            }

            cambiandoVista = true;
            try
            {
                int indice = VistasInspecciones
                    .Select((item, posicion) => new { item, posicion })
                    .Where(item => string.Equals(
                        item.item.Modo,
                        modoActual,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.posicion)
                    .DefaultIfEmpty(0)
                    .First();

                VistaInspeccionesPicker.SelectedIndex = indice;
            }
            finally
            {
                cambiandoVista = false;
            }
        }

        private async void OnVistaInspeccionesChanged(
            object sender,
            EventArgs e)
        {
            if (cambiandoVista ||
                viewModel.EsModoNueva ||
                MostrarFiltroTecnico ||
                VistaInspeccionesPicker.SelectedItem is not
                    VistaMisInspeccionesItem seleccionada ||
                string.Equals(
                    modoActual,
                    seleccionada.Modo,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            modoActual = seleccionada.Modo;
            bandejaApi.EstablecerTecnicoContextual(modoActual, null);
            viewModel.AplicarModo(modoActual);

            OnPropertyChanged(nameof(TituloVista));
            OnPropertyChanged(nameof(SubtituloVista));
            OnPropertyChanged(nameof(MostrarFiltroTecnico));
            OnPropertyChanged(nameof(MostrarSelectorMisInspecciones));

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
                        ? "No fue posible cargar los usuarios responsables."
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
