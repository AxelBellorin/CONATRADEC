using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace CONATRADEC.Views
{
    public partial class NuevoAnalisisFormPage :
        ContentPage
    {
        private readonly
            NuevoAnalisisFormEdicionViewModel
                viewModel = new();

        private readonly List<ElementoQuimicoResponse>
            catalogoElementos = new();

        private readonly ObservableCollection<
            ElementoDisponibleItem>
                elementosDisponibles = new();

        private Picker? pickerElementoAgregar;
        private Button? botonAgregarElemento;
        private Label? mensajeElementosDisponibles;
        private Border? selectorElementosBorder;

        public NuevoAnalisisFormPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();
            BindingContext = viewModel;

            viewModel.ElementosQuimicosAnalisis
                .CollectionChanged +=
                    ElementosQuimicos_CollectionChanged;

            CrearSelectorParaAgregarElementos();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.EsModoEdicion)
            {
                viewModel.LoadPagePermissions("MainPage");

                if (!viewModel.CanView ||
                    !viewModel.CanEdit)
                {
                    await DisplayAlert(
                        "Acceso denegado",
                        "No tiene permisos para editar análisis de suelo.",
                        "Aceptar");

                    AnalisisEdicionService.Instance.Limpiar();

                    await Shell.Current
                        .GoToAsync("//MainPage");

                    return;
                }
            }
            else
            {
                if (!PermissionService.Instance
                        .HasRead(
                            "NuevoAnalisisFormPage"))
                {
                    await DisplayAlert(
                        "Acceso denegado",
                        "No tiene permisos para ver el formulario de análisis de suelo.",
                        "Aceptar");

                    await Shell.Current
                        .GoToAsync("//MainPage");

                    return;
                }

                viewModel.LoadPagePermissions(
                    "NuevoAnalisisFormPage");
            }

            await viewModel
                .InicializarPaginaAsync(false);

            CargarCatalogoElementos();

            Button? botonEnviar =
                BuscarBotonEnviar(this);

            if (botonEnviar != null)
            {
                botonEnviar.Text =
                    viewModel
                        .TextoAccionFormulario;
            }
        }

        private void CrearSelectorParaAgregarElementos()
        {
            Label? tituloElementos =
                BuscarLabelPorTexto(
                    this,
                    "Elementos químicos");

            if (tituloElementos?.Parent
                    is not VerticalStackLayout encabezado ||
                encabezado.Parent
                    is not VerticalStackLayout contenedor)
            {
                return;
            }

            pickerElementoAgregar = new Picker
            {
                Title =
                    "Seleccione un elemento eliminado",
                ItemsSource = elementosDisponibles,
                ItemDisplayBinding =
                    new Binding(
                        nameof(
                            ElementoDisponibleItem
                                .NombreMostrar)),
                HorizontalOptions =
                    LayoutOptions.Fill,
                BackgroundColor =
                    Colors.Transparent,
                TextColor =
                    Color.FromArgb("#111827")
            };

            pickerElementoAgregar
                .SelectedIndexChanged +=
                    PickerElementoAgregar_SelectedIndexChanged;

            Border pickerBorder = new()
            {
                BackgroundColor =
                    Color.FromArgb("#F9FAFB"),
                Stroke =
                    Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1,
                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(11)
                    },
                Padding = new Thickness(10, 2),
                Content = pickerElementoAgregar
            };

            botonAgregarElemento = new Button
            {
                Text = "Agregar",
                BackgroundColor =
                    Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                FontAttributes =
                    FontAttributes.Bold,
                CornerRadius = 11,
                Padding = new Thickness(16, 10),
                IsEnabled = false
            };

            botonAgregarElemento.Clicked +=
                AgregarElemento_Clicked;

            Grid filaSelector = new()
            {
                ColumnSpacing = 10,
                ColumnDefinitions =
                {
                    new ColumnDefinition(
                        GridLength.Star),

                    new ColumnDefinition(
                        GridLength.Auto)
                }
            };

            Grid.SetColumn(pickerBorder, 0);
            Grid.SetColumn(botonAgregarElemento, 1);

            filaSelector.Children.Add(pickerBorder);
            filaSelector.Children.Add(
                botonAgregarElemento);

            mensajeElementosDisponibles = new Label
            {
                FontSize = 12,
                TextColor =
                    Color.FromArgb("#6B7280")
            };

            selectorElementosBorder = new Border
            {
                BackgroundColor =
                    Color.FromArgb("#EEF5F2"),
                Stroke =
                    Color.FromArgb("#C8DED6"),
                StrokeThickness = 1,
                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(13)
                    },
                Padding = 12,
                Margin =
                    new Thickness(0, 0, 0, 4),
                Content =
                    new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Label
                            {
                                Text =
                                    "Agregar elemento químico",
                                FontAttributes =
                                    FontAttributes.Bold,
                                FontSize = 14,
                                TextColor =
                                    Color.FromArgb("#3B655B")
                            },

                            new Label
                            {
                                Text =
                                    "Puede recuperar cualquier elemento que haya quitado del análisis.",
                                FontSize = 12,
                                TextColor =
                                    Color.FromArgb("#4B5563")
                            },

                            filaSelector,
                            mensajeElementosDisponibles
                        }
                    }
            };

            /*
             * La tarjeta contiene:
             * 0 = encabezado
             * 1 = lista de elementos
             *
             * El selector se coloca entre ambos.
             */
            contenedor.Children.Insert(
                1,
                selectorElementosBorder);

            ActualizarEstadoSelector();
        }

        private void CargarCatalogoElementos()
        {
            catalogoElementos.Clear();

            foreach (
                ElementoQuimicoResponse elemento
                in viewModel.CatalogoElementosQuimicos.Where(x =>
                    x.ElementoQuimicosId.HasValue &&
                    x.ElementoQuimicosId.Value > 0))
            {
                if (catalogoElementos.Any(x =>
                        x.ElementoQuimicosId ==
                        elemento.ElementoQuimicosId))
                {
                    continue;
                }

                catalogoElementos.Add(elemento);
            }

            ActualizarElementosDisponibles();
        }

        private void
            ElementosQuimicos_CollectionChanged(
                object? sender,
                NotifyCollectionChangedEventArgs e)
        {
            ActualizarElementosDisponibles();
        }

        private void ActualizarElementosDisponibles()
        {
            int? seleccionadoId =
                (pickerElementoAgregar?
                    .SelectedItem
                 as ElementoDisponibleItem)?
                    .Elemento
                    .ElementoQuimicosId;

            HashSet<int> elementosActivos =
                viewModel
                    .ElementosQuimicosAnalisis
                    .Where(x =>
                        x.ElementoQuimicoId.HasValue)
                    .Select(x =>
                        x.ElementoQuimicoId!.Value)
                    .ToHashSet();

            elementosDisponibles.Clear();

            foreach (
                ElementoQuimicoResponse elemento
                in catalogoElementos)
            {
                if (!elemento
                        .ElementoQuimicosId
                        .HasValue ||
                    elementosActivos.Contains(
                        elemento
                            .ElementoQuimicosId
                            .Value))
                {
                    continue;
                }

                elementosDisponibles.Add(
                    new ElementoDisponibleItem(
                        elemento));
            }

            if (pickerElementoAgregar != null)
            {
                pickerElementoAgregar.SelectedItem =
                    seleccionadoId.HasValue
                        ? elementosDisponibles
                            .FirstOrDefault(x =>
                                x.Elemento
                                    .ElementoQuimicosId ==
                                seleccionadoId)
                        : null;
            }

            ActualizarEstadoSelector();
        }

        private void
            PickerElementoAgregar_SelectedIndexChanged(
                object? sender,
                EventArgs e)
        {
            ActualizarEstadoSelector();
        }

        private void ActualizarEstadoSelector()
        {
            bool tieneDisponibles =
                elementosDisponibles.Count > 0;

            bool tieneSeleccion =
                pickerElementoAgregar?
                    .SelectedItem
                    is ElementoDisponibleItem;

            if (pickerElementoAgregar != null)
            {
                pickerElementoAgregar.IsEnabled =
                    tieneDisponibles;
            }

            if (botonAgregarElemento != null)
            {
                botonAgregarElemento.IsEnabled =
                    tieneDisponibles &&
                    tieneSeleccion;
            }

            if (mensajeElementosDisponibles != null)
            {
                mensajeElementosDisponibles.Text =
                    tieneDisponibles
                        ? $"{elementosDisponibles.Count} elemento(s) disponible(s) para agregar."
                        : "Todos los elementos del catálogo están agregados.";
            }
        }

        private void AgregarElemento_Clicked(
            object? sender,
            EventArgs e)
        {
            if (pickerElementoAgregar?
                    .SelectedItem
                is not ElementoDisponibleItem
                    seleccionado)
            {
                return;
            }

            ElementoQuimicoResponse elemento =
                seleccionado.Elemento;

            if (!elemento
                    .ElementoQuimicosId
                    .HasValue)
            {
                return;
            }

            int elementoId =
                elemento.ElementoQuimicosId.Value;

            if (viewModel
                    .ElementosQuimicosAnalisis
                    .Any(x =>
                        x.ElementoQuimicoId ==
                        elementoId))
            {
                ActualizarElementosDisponibles();
                return;
            }

            string simbolo =
                (elemento
                    .SimboloElementoQuimico ??
                 string.Empty)
                    .Trim();

            string nombre =
                (elemento
                    .NombreElementoQuimico ??
                 string.Empty)
                    .Trim();

            ObservableCollection<UnidadMedidaResponse>
                unidades =
                    new(
                        viewModel
                            .UnidadesMedidaCatalogo);

            ResultadoAnalisisItemViewModel
                nuevoElemento =
                    new()
                    {
                        ElementoQuimicoId =
                            elementoId,

                        CodigoParametro =
                            simbolo,

                        NombreParametro =
                            string.IsNullOrWhiteSpace(
                                simbolo)
                                ? nombre
                                : $"{nombre} ({simbolo})",

                        PlaceholderValor =
                            "Valor reportado",

                        EsConstante = false,
                        EsElementoQuimico = true,
                        PuedeEliminar = true,
                        Valor = string.Empty,

                        UnidadesMedida = unidades,

                        UnidadSeleccionada =
                            ObtenerUnidadPredeterminada(
                                unidades,
                                simbolo)
                    };

            int indice =
                ObtenerIndiceInsercion(elementoId);

            viewModel
                .ElementosQuimicosAnalisis
                .Insert(
                    indice,
                    nuevoElemento);

            pickerElementoAgregar.SelectedItem =
                null;

            ActualizarElementosDisponibles();
        }

        private int ObtenerIndiceInsercion(
            int elementoId)
        {
            int ordenNuevo =
                catalogoElementos.FindIndex(x =>
                    x.ElementoQuimicosId ==
                    elementoId);

            if (ordenNuevo < 0)
            {
                return viewModel
                    .ElementosQuimicosAnalisis
                    .Count;
            }

            for (
                int indice = 0;
                indice <
                    viewModel
                        .ElementosQuimicosAnalisis
                        .Count;
                indice++)
            {
                int? idActual =
                    viewModel
                        .ElementosQuimicosAnalisis[
                            indice]
                        .ElementoQuimicoId;

                if (!idActual.HasValue)
                    continue;

                int ordenActual =
                    catalogoElementos.FindIndex(x =>
                        x.ElementoQuimicosId ==
                        idActual.Value);

                if (ordenActual > ordenNuevo)
                    return indice;
            }

            return viewModel
                .ElementosQuimicosAnalisis
                .Count;
        }

        private static UnidadMedidaResponse?
            ObtenerUnidadPredeterminada(
                ObservableCollection<
                    UnidadMedidaResponse> unidades,
                string? simbolo)
        {
            string simboloNormalizado =
                (simbolo ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

            if (simboloNormalizado == "N")
            {
                return BuscarUnidad(
                    unidades,
                    "%",
                    "PORCENTAJE",
                    "PPM",
                    "MG/KG");
            }

            if (simboloNormalizado == "P")
            {
                return BuscarUnidad(
                    unidades,
                    "PPM",
                    "CMOL/KG",
                    "MEQ/100G",
                    "MG/KG");
            }

            if (simboloNormalizado == "K" ||
                simboloNormalizado == "CA" ||
                simboloNormalizado == "MG")
            {
                return BuscarUnidad(
                    unidades,
                    "MEQ/100G",
                    "PPM",
                    "CMOL/KG",
                    "MG/KG");
            }

            return BuscarUnidad(
                unidades,
                "MG/KG",
                "PPM",
                "G/KG",
                "%");
        }

        private static UnidadMedidaResponse?
            BuscarUnidad(
                IEnumerable<UnidadMedidaResponse>
                    unidades,
                params string[] posiblesValores)
        {
            foreach (
                string valor
                in posiblesValores)
            {
                string normalizado =
                    NormalizarUnidad(valor);

                UnidadMedidaResponse? unidad =
                    unidades.FirstOrDefault(x =>
                        NormalizarUnidad(
                            x.TextoBusqueda)
                            .Contains(
                                normalizado));

                if (unidad != null)
                    return unidad;
            }

            return unidades.FirstOrDefault();
        }

        private static string NormalizarUnidad(
            string? texto)
        {
            return
                (texto ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant()
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty);
        }

        private static Label?
            BuscarLabelPorTexto(
                IVisualTreeElement elemento,
                string texto)
        {
            if (elemento is Label label &&
                string.Equals(
                    label.Text?.Trim(),
                    texto,
                    StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }

            foreach (
                IVisualTreeElement hijo
                in elemento.GetVisualChildren())
            {
                Label? encontrado =
                    BuscarLabelPorTexto(
                        hijo,
                        texto);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }

        private static Button?
            BuscarBotonEnviar(
                IVisualTreeElement elemento)
        {
            if (elemento is Button boton &&
                (
                    string.Equals(
                        boton.Text,
                        "Enviar Análisis",
                        StringComparison.OrdinalIgnoreCase) ||

                    string.Equals(
                        boton.Text,
                        "Continuar actualización",
                        StringComparison.OrdinalIgnoreCase)
                ))
            {
                return boton;
            }

            foreach (
                IVisualTreeElement hijo
                in elemento.GetVisualChildren())
            {
                Button? encontrado =
                    BuscarBotonEnviar(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }

        private sealed class
            ElementoDisponibleItem
        {
            public ElementoDisponibleItem(
                ElementoQuimicoResponse elemento)
            {
                Elemento = elemento;
            }

            public ElementoQuimicoResponse
                Elemento { get; }

            public string NombreMostrar
            {
                get
                {
                    string nombre =
                        (Elemento
                            .NombreElementoQuimico ??
                         "Elemento químico")
                            .Trim();

                    string simbolo =
                        (Elemento
                            .SimboloElementoQuimico ??
                         string.Empty)
                            .Trim();

                    return
                        string.IsNullOrWhiteSpace(
                            simbolo)
                            ? nombre
                            : $"{nombre} ({simbolo})";
                }
            }
        }
    }
}
