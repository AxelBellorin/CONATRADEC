using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAAnalizadorPage : ContentPage
    {
        private const double BreakpointFiltrosCompactos = 760d;

        private readonly DiagnosticoIAAnalizadorViewModel viewModel;
        private bool selectorTecnicoAbierto;
        private bool validandoPermiso;
        private bool filtrosAvanzadosConfigurados;
        private Border? filtrosAvanzadosBorder;
        private VerticalStackLayout? filtrosAvanzadosContenido;
        private Grid? filtrosAvanzadosGrid;
        private Grid? accionesFiltrosGrid;
        private Label? ayudaFiltrosLabel;
        private Button? limpiarFiltrosButton;
        private Button? buscarFiltrosButton;
        private readonly List<View> camposFiltros = [];
        private int columnasFiltrosAplicadas = -1;

        public DiagnosticoIAAnalizadorPage()
        {
            InitializeComponent();
            viewModel = new DiagnosticoIAAnalizadorViewModel();
            viewModel.PaginaCargada += OnPaginaCargada;
            BindingContext = viewModel;
            ConfigurarFiltrosAvanzados();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActivarPagina();

            if (!await ValidarPermisoLecturaAsync())
                return;

            ConfigurarFiltrosAvanzados();
            await viewModel.InicializarOReanudarAsync();
            AplicarResponsiveFiltros(Width);
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            selectorTecnicoAbierto = false;
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveFiltros(width);
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

        /// <summary>
        /// Eleva la bandeja del analizador al mismo estándar de consulta que el
        /// aprobador sin modificar el XAML histórico ni los endpoints anteriores.
        /// Los filtros se escriben localmente y únicamente consultan el servidor
        /// al pulsar Buscar.
        /// </summary>
        private void ConfigurarFiltrosAvanzados()
        {
            if (filtrosAvanzadosConfigurados ||
                AnalizadorListado?.Header is not VerticalStackLayout encabezado)
            {
                return;
            }

            filtrosAvanzadosConfigurados = true;

            // Oculta únicamente el bloque histórico de filtro por técnico. El
            // selector sigue utilizándose dentro del nuevo panel avanzado.
            if (TecnicoSelectorButton?.Parent != null)
            {
                Border? bloqueTecnico = BuscarAncestro<Border>(
                    TecnicoSelectorButton);
                if (bloqueTecnico != null)
                    bloqueTecnico.IsVisible = false;
            }

            var titulo = new Label
            {
                Text = "Buscar y filtrar expedientes",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#263A35"),
                VerticalTextAlignment = TextAlignment.Center
            };

            var resumen = new Label
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#6B7773"),
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalTextAlignment = TextAlignment.Center
            };
            resumen.SetBinding(
                Label.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.ResumenFiltrosActivos));

            var textosCabecera = new VerticalStackLayout
            {
                Spacing = 1,
                HorizontalOptions = LayoutOptions.Fill
            };
            textosCabecera.Children.Add(titulo);
            textosCabecera.Children.Add(resumen);

            var alternar = new Button
            {
                HeightRequest = 42,
                MinimumWidthRequest = 145,
                Padding = new Thickness(12, 6),
                BackgroundColor = Color.FromArgb("#E3EFEA"),
                TextColor = Color.FromArgb("#3B655B"),
                CornerRadius = 9,
                HorizontalOptions = LayoutOptions.Fill
            };
            alternar.SetBinding(
                Button.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.TextoBotonFiltros));
            alternar.SetBinding(
                Button.CommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.AlternarFiltrosCommand));

            var cabecera = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                RowSpacing = 8
            };
            cabecera.Add(textosCabecera, 0, 0);
            cabecera.Add(alternar, 1, 0);

            var buscarEntry = new Entry
            {
                Placeholder =
                    "Nombre, terreno, propietario, técnico, ubicación o archivo",
                ReturnType = ReturnType.Search,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
                HeightRequest = 44,
                BackgroundColor = Colors.Transparent
            };
            buscarEntry.SetBinding(
                Entry.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarInspeccion));
            buscarEntry.SetBinding(
                Entry.ReturnCommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarCommand));

            var buscarEntryBorder = CrearBordeEntrada(buscarEntry);

            var buscarButton = new Button
            {
                Text = "Buscar",
                HeightRequest = 44,
                MinimumWidthRequest = 110,
                Padding = new Thickness(14, 7),
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                CornerRadius = 9,
                HorizontalOptions = LayoutOptions.Fill
            };
            buscarButton.SetBinding(
                Button.CommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarCommand));

            var filaBusqueda = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 8
            };
            filaBusqueda.Add(buscarEntryBorder, 0, 0);
            filaBusqueda.Add(buscarButton, 1, 0);

            filtrosAvanzadosGrid = new Grid
            {
                RowSpacing = 10,
                ColumnSpacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };

            Button tecnicoButton = CrearSelectorTecnicoAvanzado();
            camposFiltros.Add(CrearCampoFiltro(
                "Técnico responsable",
                tecnicoButton));

            var propietario = new Entry
            {
                Placeholder = "Nombre o identificación",
                ReturnType = ReturnType.Search,
                HeightRequest = 42,
                BackgroundColor = Colors.Transparent
            };
            propietario.SetBinding(
                Entry.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.PropietarioFiltro));
            propietario.SetBinding(
                Entry.ReturnCommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarCommand));
            camposFiltros.Add(CrearCampoFiltro(
                "Propietario",
                CrearBordeEntrada(propietario, 42)));

            var departamento = new Entry
            {
                Placeholder = "Ej. Matagalpa",
                ReturnType = ReturnType.Search,
                HeightRequest = 42,
                BackgroundColor = Colors.Transparent
            };
            departamento.SetBinding(
                Entry.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.DepartamentoFiltro));
            departamento.SetBinding(
                Entry.ReturnCommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarCommand));
            camposFiltros.Add(CrearCampoFiltro(
                "Departamento",
                CrearBordeEntrada(departamento, 42)));

            var tipoFoto = new Picker
            {
                HeightRequest = 42,
                BackgroundColor = Colors.Transparent,
                ItemDisplayBinding = new Binding(nameof(FiltroCodigoOpcionV2.Nombre))
            };
            tipoFoto.SetBinding(
                Picker.ItemsSourceProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.TiposFotografiaFiltro));
            tipoFoto.SetBinding(
                Picker.SelectedItemProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.TipoFotografiaFiltroSeleccionado));
            camposFiltros.Add(CrearCampoFiltro(
                "Tipo de fotografía",
                CrearBordeEntrada(tipoFoto, 42)));

            var estado = new Picker
            {
                HeightRequest = 42,
                BackgroundColor = Colors.Transparent,
                ItemDisplayBinding = new Binding(nameof(FiltroCodigoOpcionV2.Nombre))
            };
            estado.SetBinding(
                Picker.ItemsSourceProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.EstadosFiltro));
            estado.SetBinding(
                Picker.SelectedItemProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.EstadoFiltroSeleccionado));
            camposFiltros.Add(CrearCampoFiltro(
                "Estado",
                CrearBordeEntrada(estado, 42)));

            camposFiltros.Add(CrearCampoFecha(
                "Registro desde",
                nameof(DiagnosticoIAAnalizadorViewModel.UsarFechaDesde),
                nameof(DiagnosticoIAAnalizadorViewModel.FechaDesde)));
            camposFiltros.Add(CrearCampoFecha(
                "Registro hasta",
                nameof(DiagnosticoIAAnalizadorViewModel.UsarFechaHasta),
                nameof(DiagnosticoIAAnalizadorViewModel.FechaHasta)));

            foreach (View campo in camposFiltros)
                filtrosAvanzadosGrid.Children.Add(campo);

            var ayuda = new Label
            {
                Text =
                    "Escribir o seleccionar filtros no consulta el servidor. Pulse Buscar para aplicarlos. Las fechas se interpretan con la zona horaria del dispositivo.",
                FontSize = 11,
                TextColor = Color.FromArgb("#6B7773"),
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.Fill
            };

            var limpiar = new Button
            {
                Text = "Limpiar filtros",
                HeightRequest = 40,
                MinimumWidthRequest = 130,
                Padding = new Thickness(10, 5),
                BackgroundColor = Color.FromArgb("#F5F7F6"),
                TextColor = Color.FromArgb("#5E6B67"),
                CornerRadius = 9,
                HorizontalOptions = LayoutOptions.Fill
            };
            limpiar.SetBinding(
                Button.CommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.LimpiarFiltrosCommand));

            var buscarFinal = new Button
            {
                Text = "Buscar",
                HeightRequest = 40,
                MinimumWidthRequest = 110,
                Padding = new Thickness(12, 5),
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                CornerRadius = 9,
                HorizontalOptions = LayoutOptions.Fill
            };
            buscarFinal.SetBinding(
                Button.CommandProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.BuscarCommand));

            ayudaFiltrosLabel = ayuda;
            limpiarFiltrosButton = limpiar;
            buscarFiltrosButton = buscarFinal;

            accionesFiltrosGrid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill
            };
            accionesFiltrosGrid.Children.Add(ayudaFiltrosLabel);
            accionesFiltrosGrid.Children.Add(limpiarFiltrosButton);
            accionesFiltrosGrid.Children.Add(buscarFiltrosButton);

            filtrosAvanzadosContenido = new VerticalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };
            filtrosAvanzadosContenido.SetBinding(
                VisualElement.IsVisibleProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.FiltrosExpandidos));
            filtrosAvanzadosContenido.Children.Add(filaBusqueda);
            filtrosAvanzadosContenido.Children.Add(filtrosAvanzadosGrid);
            filtrosAvanzadosContenido.Children.Add(accionesFiltrosGrid);

            var contenido = new VerticalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };
            contenido.Children.Add(cabecera);
            contenido.Children.Add(filtrosAvanzadosContenido);

            filtrosAvanzadosBorder = new Border
            {
                Padding = new Thickness(12),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(13)
                },
                HorizontalOptions = LayoutOptions.Fill,
                Content = contenido
            };

            // Se ubica antes del panel amarillo de estado y después de la
            // cabecera operativa. Así forma parte del mismo scroll de la lista.
            int indiceInsercion = Math.Max(0, encabezado.Children.Count - 1);
            encabezado.Children.Insert(indiceInsercion, filtrosAvanzadosBorder);

            AplicarResponsiveFiltros(Width);
        }

        private Button CrearSelectorTecnicoAvanzado()
        {
            var boton = new Button
            {
                HeightRequest = 42,
                Padding = new Thickness(12, 6),
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#C9D4D0"),
                BorderWidth = 1,
                TextColor = Color.FromArgb("#263A35"),
                CornerRadius = 8
            };
            boton.SetBinding(
                Button.TextProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.TecnicoFiltroTexto));
            boton.Clicked += OnSeleccionarTecnicoClicked;
            return boton;
        }

        private static View CrearCampoFiltro(string titulo, View control)
        {
            var label = new Label
            {
                Text = titulo,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#263A35")
            };

            var contenido = new VerticalStackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Fill
            };
            contenido.Children.Add(label);
            contenido.Children.Add(control);
            return contenido;
        }

        private View CrearCampoFecha(
            string titulo,
            string propiedadUsar,
            string propiedadFecha)
        {
            var check = new CheckBox
            {
                Color = Color.FromArgb("#3B655B"),
                VerticalOptions = LayoutOptions.Center
            };
            check.SetBinding(
                CheckBox.IsCheckedProperty,
                propiedadUsar);

            var label = new Label
            {
                Text = titulo,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#263A35"),
                VerticalTextAlignment = TextAlignment.Center
            };

            var cabecera = new HorizontalStackLayout
            {
                Spacing = 4
            };
            cabecera.Children.Add(check);
            cabecera.Children.Add(label);

            var fecha = new DatePicker
            {
                Format = "dd/MM/yyyy",
                HeightRequest = 42,
                BackgroundColor = Colors.Transparent
            };
            fecha.SetBinding(
                DatePicker.DateProperty,
                propiedadFecha);
            fecha.SetBinding(
                DatePicker.IsVisibleProperty,
                propiedadUsar);
            fecha.SetBinding(
                DatePicker.MinimumDateProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.FechaMinimaFiltro));
            fecha.SetBinding(
                DatePicker.MaximumDateProperty,
                nameof(DiagnosticoIAAnalizadorViewModel.FechaMaximaFiltro));

            var fechaBorder = CrearBordeEntrada(fecha, 42);
            fechaBorder.SetBinding(
                VisualElement.IsVisibleProperty,
                propiedadUsar);

            var contenedor = new VerticalStackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Fill
            };
            contenedor.Children.Add(cabecera);
            contenedor.Children.Add(fechaBorder);
            return contenedor;
        }

        private static Border CrearBordeEntrada(
            View control,
            double altura = 44)
        {
            return new Border
            {
                HeightRequest = altura,
                Padding = new Thickness(10, 0),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#C9D4D0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(8)
                },
                HorizontalOptions = LayoutOptions.Fill,
                Content = control
            };
        }

        private void AplicarResponsiveFiltros(double ancho)
        {
            if (filtrosAvanzadosGrid == null || camposFiltros.Count == 0)
                return;

            if (double.IsNaN(ancho) || ancho <= 0)
                ancho = AnalizadorListado?.Width ?? 0;

            if (double.IsNaN(ancho) || ancho <= 0)
                return;

            int columnas = ancho < BreakpointFiltrosCompactos ? 1 : 2;
            if (columnasFiltrosAplicadas == columnas)
                return;

            filtrosAvanzadosGrid.ColumnDefinitions.Clear();
            filtrosAvanzadosGrid.RowDefinitions.Clear();

            for (int columna = 0; columna < columnas; columna++)
            {
                filtrosAvanzadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            int filas = (int)Math.Ceiling(
                camposFiltros.Count / (double)columnas);
            for (int fila = 0; fila < filas; fila++)
            {
                filtrosAvanzadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            for (int indice = 0; indice < camposFiltros.Count; indice++)
            {
                View campo = camposFiltros[indice];
                Grid.SetRow(campo, indice / columnas);
                Grid.SetColumn(campo, indice % columnas);
                Grid.SetColumnSpan(campo, 1);
            }

            ReconfigurarAccionesFiltros(columnas == 1);
            columnasFiltrosAplicadas = columnas;
        }

        private void ReconfigurarAccionesFiltros(bool compacto)
        {
            if (accionesFiltrosGrid == null ||
                ayudaFiltrosLabel == null ||
                limpiarFiltrosButton == null ||
                buscarFiltrosButton == null)
            {
                return;
            }

            accionesFiltrosGrid.ColumnDefinitions.Clear();
            accionesFiltrosGrid.RowDefinitions.Clear();

            if (compacto)
            {
                accionesFiltrosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                accionesFiltrosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                accionesFiltrosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                accionesFiltrosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ayudaFiltrosLabel, 0);
                Grid.SetColumn(ayudaFiltrosLabel, 0);
                Grid.SetColumnSpan(ayudaFiltrosLabel, 1);

                Grid.SetRow(limpiarFiltrosButton, 1);
                Grid.SetColumn(limpiarFiltrosButton, 0);
                Grid.SetColumnSpan(limpiarFiltrosButton, 1);
                limpiarFiltrosButton.MinimumWidthRequest = 0;

                Grid.SetRow(buscarFiltrosButton, 2);
                Grid.SetColumn(buscarFiltrosButton, 0);
                Grid.SetColumnSpan(buscarFiltrosButton, 1);
                buscarFiltrosButton.MinimumWidthRequest = 0;
            }
            else
            {
                accionesFiltrosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                accionesFiltrosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                accionesFiltrosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                accionesFiltrosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ayudaFiltrosLabel, 0);
                Grid.SetColumn(ayudaFiltrosLabel, 0);
                Grid.SetColumnSpan(ayudaFiltrosLabel, 1);

                Grid.SetRow(limpiarFiltrosButton, 0);
                Grid.SetColumn(limpiarFiltrosButton, 1);
                Grid.SetColumnSpan(limpiarFiltrosButton, 1);
                limpiarFiltrosButton.MinimumWidthRequest = 130;

                Grid.SetRow(buscarFiltrosButton, 0);
                Grid.SetColumn(buscarFiltrosButton, 2);
                Grid.SetColumnSpan(buscarFiltrosButton, 1);
                buscarFiltrosButton.MinimumWidthRequest = 110;
            }
        }

        private static T? BuscarAncestro<T>(Element? elemento)
            where T : Element
        {
            Element? actual = elemento?.Parent;
            while (actual != null)
            {
                if (actual is T encontrado)
                    return encontrado;

                actual = actual.Parent;
            }

            return null;
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
