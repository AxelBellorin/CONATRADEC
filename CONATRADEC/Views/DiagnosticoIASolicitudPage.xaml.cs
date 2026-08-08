using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Maui.Devices;

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
        /*
         * El XAML consulta propiedades como TituloVista y SubtituloVista
         * durante InitializeComponent(). Por eso el ViewModel debe existir
         * antes de que comience la construcción del árbol visual.
         */
        private readonly DiagnosticoIASolicitudViewModel viewModel = new();

        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;

        private string modoActual = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool cargandoTecnicos;
        private bool cambiandoVista;
        private bool selectorVistaAbierto;
        private Button? selectorVistaButton;
        private bool selectorTecnicoAbierto;
        private Button? selectorTecnicoButton;

        public DiagnosticoIASolicitudPage()
        {
            InitializeComponent();

            /*
             * WinUI puede dejar vacío el texto visible del Picker cuando usa
             * ItemDisplayBinding con objetos. Ambos modelos implementan
             * ToString(), por lo que se usa directamente ese texto estable.
             */
            VistaInspeccionesPicker.ItemDisplayBinding = null;
            TecnicoFiltroPicker.ItemDisplayBinding = null;

            BindingContext = viewModel;
            PrepararSelectorVistaPersonalizado();
            PrepararSelectorTecnicoPersonalizado();

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

        /// <summary>
        /// En Windows el Picker nativo puede desplegar correctamente sus
        /// opciones y, aun así, no conservar el texto seleccionado. Para esta
        /// vista se reemplaza visualmente por un botón estable que abre un
        /// selector nativo y conserva siempre la opción elegida.
        /// </summary>
        private void PrepararSelectorVistaPersonalizado()
        {
            if (VistaInspeccionesPicker?.Parent is not Layout contenedor ||
                selectorVistaButton != null)
            {
                return;
            }

            VistaInspeccionesPicker.IsVisible = false;

            selectorVistaButton = new Button
            {
                Text = "Todas mis inspecciones",
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 260 : 315,
                HeightRequest = 44,
                Padding = new Thickness(14, 8),
                HorizontalOptions = LayoutOptions.End,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#C9D4D0"),
                BorderWidth = 1,
                TextColor = Color.FromArgb("#263A35"),
                CornerRadius = 8
            };
            selectorVistaButton.Clicked += OnSelectorVistaPersonalizadoClicked;
            contenedor.Children.Add(selectorVistaButton);
        }

        private async void OnSelectorVistaPersonalizadoClicked(
            object? sender,
            EventArgs e)
        {
            if (selectorVistaAbierto || viewModel.IsBusy ||
                VistasInspecciones.Count == 0 ||
                viewModel.EsModoNueva || MostrarFiltroTecnico)
            {
                return;
            }

            selectorVistaAbierto = true;
            try
            {
                string? opcion = await DisplayActionSheet(
                    "Vista de mis inspecciones",
                    "Cancelar",
                    null,
                    VistasInspecciones.Select(item => item.Nombre).ToArray());

                if (string.IsNullOrWhiteSpace(opcion) ||
                    string.Equals(opcion, "Cancelar", StringComparison.Ordinal))
                {
                    return;
                }

                VistaMisInspeccionesItem? seleccionada =
                    VistasInspecciones.FirstOrDefault(item =>
                        string.Equals(
                            item.Nombre,
                            opcion,
                            StringComparison.Ordinal));

                if (seleccionada == null)
                    return;

                await CambiarVistaAsync(seleccionada);
            }
            finally
            {
                selectorVistaAbierto = false;
            }
        }

        /// <summary>
        /// Historial utiliza el mismo selector estable que las bandejas del
        /// analizador y aprobador. El Picker se conserva únicamente como estado
        /// interno para no alterar la lógica existente de filtros.
        /// </summary>
        private void PrepararSelectorTecnicoPersonalizado()
        {
            if (TecnicoFiltroPicker?.Parent is not Layout contenedor ||
                selectorTecnicoButton != null)
            {
                return;
            }

            TecnicoFiltroPicker.IsVisible = false;

            selectorTecnicoButton = new Button
            {
                Text = "Todos los técnicos",
                HeightRequest = 42,
                Padding = new Thickness(12, 7),
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#C9D4D0"),
                BorderWidth = 1,
                TextColor = Color.FromArgb("#263A35"),
                CornerRadius = 8
            };
            selectorTecnicoButton.Clicked +=
                OnSelectorTecnicoPersonalizadoClicked;
            contenedor.Children.Add(selectorTecnicoButton);
        }

        private async void OnSelectorTecnicoPersonalizadoClicked(
            object? sender,
            EventArgs e)
        {
            if (selectorTecnicoAbierto || cargandoTecnicos ||
                viewModel.IsBusy || !MostrarFiltroTecnico ||
                TecnicosFiltro.Count == 0)
            {
                return;
            }

            selectorTecnicoAbierto = true;
            try
            {
                string? opcion = await DisplayActionSheet(
                    "Técnico responsable",
                    "Cancelar",
                    null,
                    TecnicosFiltro.Select(item => item.TextoMostrar).ToArray());

                if (string.IsNullOrWhiteSpace(opcion) ||
                    string.Equals(opcion, "Cancelar", StringComparison.Ordinal))
                {
                    return;
                }

                TecnicoInspeccionFiltroItem? seleccionado =
                    TecnicosFiltro.FirstOrDefault(item =>
                        string.Equals(
                            item.TextoMostrar,
                            opcion,
                            StringComparison.Ordinal));

                if (seleccionado == null)
                    return;

                cargandoTecnicos = true;
                try
                {
                    TecnicoFiltroPicker.SelectedItem = seleccionado;
                    TecnicoFiltroPicker.SelectedIndex =
                        TecnicosFiltro.IndexOf(seleccionado);
                }
                finally
                {
                    cargandoTecnicos = false;
                }

                ActualizarTextoSelectorTecnico(seleccionado);
                AplicarFiltroTecnico(seleccionado);
            }
            finally
            {
                selectorTecnicoAbierto = false;
            }
        }

        private void ActualizarTextoSelectorTecnico(
            TecnicoInspeccionFiltroItem? seleccionado = null)
        {
            if (selectorTecnicoButton == null)
                return;

            seleccionado ??= TecnicoFiltroPicker.SelectedItem as
                TecnicoInspeccionFiltroItem;

            selectorTecnicoButton.Text =
                seleccionado?.TextoMostrar ?? "Todos los técnicos";
        }

        private void AplicarFiltroTecnico(
            TecnicoInspeccionFiltroItem? seleccionado)
        {
            bandejaApi.EstablecerTecnicoContextual(
                modoActual,
                seleccionado?.UsuarioTecnicoId is > 0
                    ? seleccionado.UsuarioTecnicoId
                    : null);

            if (viewModel.BuscarInspeccionesCommand.CanExecute(null))
                viewModel.BuscarInspeccionesCommand.Execute(null);
        }

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
            if (VistasInspecciones.Count == 0 ||
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
                VistaInspeccionesPicker.SelectedItem =
                    VistasInspecciones[indice];

                if (selectorVistaButton != null)
                    selectorVistaButton.Text = VistasInspecciones[indice].Nombre;
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
                VistaInspeccionesPicker.SelectedItem is not
                    VistaMisInspeccionesItem seleccionada)
            {
                return;
            }

            await CambiarVistaAsync(seleccionada);
        }

        private async Task CambiarVistaAsync(
            VistaMisInspeccionesItem seleccionada)
        {
            if (viewModel.EsModoNueva || MostrarFiltroTecnico ||
                string.Equals(
                    modoActual,
                    seleccionada.Modo,
                    StringComparison.OrdinalIgnoreCase))
            {
                SincronizarSelectorVista();
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
            SincronizarSelectorVista();
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
                TecnicoFiltroPicker.SelectedItem =
                    TecnicosFiltro.Count > indice
                        ? TecnicosFiltro[indice]
                        : null;
                ActualizarTextoSelectorTecnico(
                    TecnicoFiltroPicker.SelectedItem as
                        TecnicoInspeccionFiltroItem);
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

            ActualizarTextoSelectorTecnico(seleccionado);
            AplicarFiltroTecnico(seleccionado);
        }

        private void OnLimpiarFiltrosClicked(
            object sender,
            EventArgs e)
        {
            bandejaApi.EstablecerTecnicoContextual(modoActual, null);

            cargandoTecnicos = true;
            TecnicoFiltroPicker.SelectedIndex =
                TecnicosFiltro.Count > 0 ? 0 : -1;
            TecnicoFiltroPicker.SelectedItem =
                TecnicosFiltro.Count > 0 ? TecnicosFiltro[0] : null;
            cargandoTecnicos = false;
            ActualizarTextoSelectorTecnico(
                TecnicoFiltroPicker.SelectedItem as
                    TecnicoInspeccionFiltroItem);

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
