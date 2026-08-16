using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Linq;
using static CONATRADEC.Models.FormMode;

namespace CONATRADEC.Views
{
    public partial class userFormPage : ContentPage, IQueryAttributable
    {
        private const string MarcaEncabezadoPropio =
            "CONATRADEC_FORM_BACK_WRAPPER";

        /*
         * Los formularios se reorganizan según el ancho útil real de la tarjeta,
         * no únicamente según el tipo de dispositivo. Esto cubre Windows
         * redimensionado, tablet vertical y ventanas divididas.
         */
        private const double AnchoMinimoDosColumnas = 760;
        private const double AnchoMinimoTresColumnasUbicacion = 960;
        private const double AnchoMinimoDosColumnasUbicacion = 640;

        private UserFormViewModel viewModel = new();
        private readonly SemaphoreSlim inicializacionLock = new(1, 1);

        private FormModeSelect modeActual;
        private UserRequest userActual = new();
        private long versionParametros;
        private long versionInicializada;
        private bool parametrosNavegacionValidos;
        private bool paginaVisible;

        /*
         * Evita volver a limpiar la fecha si OnAppearing se ejecuta
         * nuevamente mientras el usuario permanece en el formulario.
         */
        private bool fechaNacimientoPreparada;
        private bool ignorarCambioFechaNacimiento;

        /// <summary>
        /// Recibe Mode y User de forma atómica. No se usan QueryProperty
        /// separados porque la página de Shell puede reutilizarse y no debe
        /// inicializarse hasta contar con ambos valores de la navegación.
        /// </summary>
        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            bool tieneModo =
                query.TryGetValue("Mode", out object? modeValue) &&
                modeValue is FormModeSelect;

            bool tieneUsuario =
                query.TryGetValue("User", out object? userValue) &&
                userValue is UserRequest;

            parametrosNavegacionValidos =
                tieneModo && tieneUsuario;

            if (tieneModo)
            {
                modeActual =
                    (FormModeSelect)modeValue!;
            }

            if (tieneUsuario)
            {
                userActual =
                    (UserRequest)userValue!;
            }

            fechaNacimientoPreparada = false;
            Interlocked.Increment(ref versionParametros);

            /*
             * En algunas secuencias de Shell los atributos pueden aplicarse
             * cuando la página ya comenzó a mostrarse. Si eso ocurre, se
             * inicializa inmediatamente con la nueva versión de parámetros.
             */
            if (paginaVisible)
            {
                Dispatcher.Dispatch(
                    () =>
                        _ = InicializarParametrosPendientesAsync());
            }
        }

        public userFormPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            ConfigurarBotonRegresarSuperior();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;
            AjustarDiseno(Width);

            await InicializarParametrosPendientesAsync();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            base.OnDisappearing();
        }

        /// <summary>
        /// Inicializa el formulario únicamente después de haber recibido de
        /// forma conjunta Mode y User. De esta manera una navegación Editar o
        /// Ver nunca puede caer temporalmente en el valor predeterminado Crear.
        /// </summary>
        private async Task InicializarParametrosPendientesAsync()
        {
            await inicializacionLock.WaitAsync();

            try
            {
                long versionActual =
                    Volatile.Read(ref versionParametros);

                if (versionActual <= 0 ||
                    versionInicializada == versionActual)
                {
                    return;
                }

                if (!parametrosNavegacionValidos)
                {
                    versionInicializada = versionActual;

                    await DisplayAlert(
                        "No fue posible abrir el usuario",
                        "No se recibieron correctamente los datos de navegación del formulario.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Usuarios);
                    return;
                }

                FormModeSelect modo = modeActual;
                UserRequest usuario = userActual;

                if ((modo == FormModeSelect.Edit ||
                     modo == FormModeSelect.View) &&
                    usuario.UsuarioId is not > 0)
                {
                    versionInicializada = versionActual;

                    await DisplayAlert(
                        "Usuario no disponible",
                        "No se recibió un usuario válido para esta operación.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Usuarios);
                    return;
                }

                /*
                 * Normalmente la visita ya fue creada por userPage. Esta
                 * llamada cubre también una navegación directa al formulario
                 * sin invalidar la caché de la visita actual.
                 */
                UsuarioVisitaService.AsegurarVisita();

                var nuevoViewModel =
                    new UserFormViewModel();

                /*
                 * Mode y User se asignan desde la misma captura de parámetros.
                 * No se permite que OnAppearing construya un formulario con
                 * valores predeterminados mientras Shell termina la navegación.
                 */
                nuevoViewModel.Mode = modo;
                nuevoViewModel.User = usuario;

                viewModel = nuevoViewModel;
                BindingContext = viewModel;
                fechaNacimientoPreparada = false;

                viewModel.LoadPagePermissions("userPage");

                bool denegado =
                    !viewModel.CanView ||
                    (viewModel.Mode == FormModeSelect.Create &&
                     !viewModel.CanAdd) ||
                    (viewModel.Mode == FormModeSelect.Edit &&
                     !viewModel.CanEdit);

                if (denegado)
                {
                    versionInicializada = versionActual;

                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para realizar esta operación.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Usuarios);
                    return;
                }

                await viewModel.InicializarAsync();

                /*
                 * Si mientras se esperaba la API Shell entregó otra navegación,
                 * la siguiente ejecución tomará la versión más reciente.
                 */
                versionInicializada = versionActual;

                if (versionActual ==
                    Volatile.Read(ref versionParametros))
                {
                    PrepararFechaNacimiento();
                }
            }
            finally
            {
                inicializacionLock.Release();
            }
        }

        /// <summary>
        /// En modo Crear deja el campo visualmente vacío y mantiene
        /// FechaNacimientoUsuario en null hasta que el usuario seleccione
        /// una fecha. En Editar y Ver muestra la fecha guardada.
        /// </summary>
        private void PrepararFechaNacimiento()
        {
            if (fechaNacimientoPreparada ||
                FechaNacimientoPicker == null ||
                FechaNacimientoTexto == null)
            {
                return;
            }

            /*
             * El binding del DatePicker puede disparar DateSelected durante
             * la construcción de la página. Esa notificación no representa
             * una selección realizada por el usuario.
             */
            ignorarCambioFechaNacimiento = true;

            try
            {
                if (viewModel.Mode == FormModeSelect.Create)
                {
                    /*
                     * El DatePicker necesita una fecha interna, pero el valor
                     * enviado a la API se mantiene nulo hasta una selección.
                     */
                    viewModel.FechaNacimientoUsuario = null;
                    FechaNacimientoTexto.Text =
                        "Seleccione una fecha";
                    FechaNacimientoTexto.TextColor =
                        Color.FromArgb("#6B7280");
                }
                else if (viewModel.FechaNacimientoUsuario.HasValue)
                {
                    DateTime fecha =
                        viewModel.FechaNacimientoUsuario.Value
                            .ToDateTime(TimeOnly.MinValue);

                    FechaNacimientoPicker.Date = fecha;
                    FechaNacimientoTexto.Text =
                        fecha.ToString("dd/MM/yyyy");
                    FechaNacimientoTexto.TextColor =
                        Color.FromArgb("#1F1F1F");
                }
                else
                {
                    FechaNacimientoTexto.Text =
                        "Seleccione una fecha";
                    FechaNacimientoTexto.TextColor =
                        Color.FromArgb("#6B7280");
                }

                fechaNacimientoPreparada = true;
                AjustarAreaInteractivaFecha();
            }
            finally
            {
                ignorarCambioFechaNacimiento = false;
            }
        }

        /// <summary>
        /// Marca la fecha como seleccionada únicamente después de una
        /// interacción explícita con el DatePicker.
        /// </summary>
        private void FechaNacimientoPicker_DateSelected(
            object? sender,
            DateChangedEventArgs e)
        {
            if (ignorarCambioFechaNacimiento ||
                !fechaNacimientoPreparada ||
                viewModel.IsReadOnly)
            {
                return;
            }

            viewModel.FechaNacimientoUsuario =
                DateOnly.FromDateTime(e.NewDate);

            FechaNacimientoTexto.Text =
                e.NewDate.ToString("dd/MM/yyyy");

            FechaNacimientoTexto.TextColor =
                Color.FromArgb("#1F1F1F");
        }

        /// <summary>
        /// En WinUI el control nativo puede conservar su ancho natural.
        /// Se iguala su área de interacción al campo visible, evitando
        /// modificar el tamaño cuando el valor no cambió.
        /// </summary>
        private void FechaNacimientoField_SizeChanged(
            object? sender,
            EventArgs e)
        {
            AjustarAreaInteractivaFecha();
        }

        private void AjustarAreaInteractivaFecha()
        {
            if (FechaNacimientoField == null ||
                FechaNacimientoPicker == null ||
                FechaNacimientoField.Width <= 0)
            {
                return;
            }

            double ancho = FechaNacimientoField.Width;

            if (Math.Abs(
                    FechaNacimientoPicker.WidthRequest -
                    ancho) > 0.5)
            {
                FechaNacimientoPicker.WidthRequest =
                    ancho;
            }
        }

        /// <summary>
        /// Configura una flecha propia para el formulario de usuarios.
        ///
        /// Se enlaza directamente con CancelCommand para conservar:
        /// - la confirmación de cambios sin guardar;
        /// - la limpieza del formulario;
        /// - el regreso correcto al listado de usuarios.
        ///
        /// También se marca el contenido para impedir que el servicio global
        /// agregue una segunda flecha sobre esta página.
        /// </summary>
        private void ConfigurarBotonRegresarSuperior()
        {
            if (Content != null)
            {
                Content.StyleId =
                    MarcaEncabezadoPropio;
            }

            if (FormularioContainer == null)
                return;

            bool yaExiste =
                FormularioContainer
                    .Children
                    .OfType<View>()
                    .Any(view =>
                        string.Equals(
                            view.AutomationId,
                            "EncabezadoRegresarUsuario",
                            StringComparison.Ordinal));

            if (yaExiste)
                return;

            var botonRegresar =
                new Button
                {
                    Text = "←",
                    WidthRequest = 48,
                    HeightRequest = 48,
                    MinimumWidthRequest = 48,
                    MinimumHeightRequest = 48,
                    Padding = 0,
                    CornerRadius = 14,
                    FontSize = 23,
                    FontAttributes =
                        FontAttributes.Bold,
                    BackgroundColor =
                        Color.FromArgb("#F3F5F4"),
                    TextColor =
                        Color.FromArgb("#263238"),
                    HorizontalOptions =
                        LayoutOptions.Start,
                    VerticalOptions =
                        LayoutOptions.Center,
                    AutomationId =
                        "BotonRegresarUsuario"
                };

            botonRegresar.SetBinding(
                Button.CommandProperty,
                nameof(
                    UserFormViewModel
                        .CancelCommand));

            SemanticProperties.SetDescription(
                botonRegresar,
                "Regresar al listado de usuarios");

            var encabezado =
                new Grid
                {
                    AutomationId =
                        "EncabezadoRegresarUsuario",
                    Padding =
                        new Thickness(0, 0, 0, 2),
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    VerticalOptions =
                        LayoutOptions.Start
                };

            encabezado.Children.Add(
                botonRegresar);

            FormularioContainer
                .Children
                .Insert(
                    0,
                    encabezado);
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double ancho)
        {
            if (ancho <= 0 ||
                FormularioContainer == null)
            {
                return;
            }

            double margen =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 72
                    : 32;

            double anchoFormulario =
                Math.Min(
                    Math.Max(280, ancho - margen),
                    1100);

            FormularioContainer.WidthRequest =
                anchoFormulario;

            int columnasAccesoContacto =
                anchoFormulario >= AnchoMinimoDosColumnas
                    ? 2
                    : 1;

            int columnasUbicacion =
                anchoFormulario >= AnchoMinimoTresColumnasUbicacion
                    ? 3
                    : anchoFormulario >= AnchoMinimoDosColumnasUbicacion
                        ? 2
                        : 1;

            AjustarGrid(
                AccesoGrid,
                new[]
                {
                    UsuarioSection,
                    ClaveSection,
                    NombreSection,
                    IdentificacionSection
                },
                columnasAccesoContacto);

            AjustarGrid(
                ContactoGrid,
                new[]
                {
                    CorreoSection,
                    TelefonoSection,
                    FechaSection,
                    RolSection
                },
                columnasAccesoContacto);

            AjustarGrid(
                UbicacionGrid,
                new[]
                {
                    PaisSection,
                    DepartamentoSection,
                    MunicipioSection
                },
                columnasUbicacion);
        }

        private static void AjustarGrid(
            Grid grid,
            IReadOnlyList<View> secciones,
            int columnas)
        {
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            columnas =
                Math.Clamp(
                    columnas,
                    1,
                    Math.Max(1, secciones.Count));

            int filas =
                (int)Math.Ceiling(
                    secciones.Count / (double)columnas);

            for (int i = 0; i < columnas; i++)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            for (int i = 0; i < filas; i++)
            {
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            for (int i = 0; i < secciones.Count; i++)
            {
                Grid.SetRow(secciones[i], i / columnas);
                Grid.SetColumn(secciones[i], i % columnas);
            }
        }
    }
}
