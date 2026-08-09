using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Devices;
using System.Linq;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAResultadoPage :
        ContentPage,
        IQueryAttributable
    {
        private const string InterfazControlFitosanitario =
            "diagnosticoIAControlPage";

        private readonly DiagnosticoIAResultadoViewModel viewModel;
        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly Label tecnicoResponsableLabel;
        private readonly Label asignacionTituloLabel;
        private readonly Label asignacionResponsableLabel;
        private readonly Label asignacionAyudaLabel;
        private readonly VerticalStackLayout asignacionInformacionLayout;
        private readonly Border resumenResponsabilidadBanner;
        private readonly Button tomarInspeccionButton;
        private readonly Button administrarAsignacionButton;

        private int diagnosticoIdActual;
        private string origenActual = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool avisoLocalMostrado;
        private bool procesandoAsignacion;
        private bool tecnicoResponsableDisponible;
        private InspeccionRevisionAsignacion? asignacionRevision;

        public DiagnosticoIAResultadoPage()
        {
            InitializeComponent();

            (
                resumenResponsabilidadBanner,
                tecnicoResponsableLabel,
                asignacionInformacionLayout,
                asignacionTituloLabel,
                asignacionResponsableLabel,
                asignacionAyudaLabel,
                tomarInspeccionButton,
                administrarAsignacionButton
            ) = CrearResumenResponsabilidad();

            IntegrarResumenEncabezado();

            viewModel = new DiagnosticoIAResultadoViewModel();
            BindingContext = viewModel;
            InicializarCapaRevision();
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int id = 0;
            string? origen = null;

            if (query.TryGetValue("diagnosticoId", out object? valor))
                int.TryParse(valor?.ToString(), out id);

            if (query.TryGetValue("origen", out object? origenValor))
                origen = origenValor?.ToString();

            diagnosticoIdActual = id;
            origenActual = DiagnosticoIARoutes.NormalizarModo(origen);
            ConfigurarFlujoRevision(diagnosticoIdActual, origenActual);
            viewModel.AplicarParametros(id, origen);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            /*
             * Los identificadores negativos pertenecen exclusivamente a la cola
             * fitosanitaria local. Aún no existe un expediente central que pueda
             * analizarse; la tarjeta se conserva en Mis inspecciones hasta que
             * una sesión en línea complete la sincronización.
             */
            if (diagnosticoIdActual < 0 && ModoSesionService.EsOffline)
            {
                if (!avisoLocalMostrado)
                {
                    avisoLocalMostrado = true;
                    await DisplayAlert(
                        "Inspección pendiente de sincronización",
                        "Esta inspección fue guardada en el dispositivo. Sus fotografías se enviarán al servidor cuando vuelva a iniciar una sesión en línea; después podrá continuar con el análisis IA y el resto del flujo.",
                        "Aceptar");
                }

                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("..");
                return;
            }

            /*
             * Primero se carga el expediente para que abrir una ficha nunca la
             * asigne silenciosamente. Analizador y aprobador toman la etapa de
             * manera explícita desde el panel de responsables.
             */
            await viewModel.InicializarAsync();
            await CargarTecnicoResponsableAsync();

            bool edicionDisponible =
                await PrepararAsignacionYBloqueoAsync();

            await AplicarFlujoRevisionAsync();

            if (EsVistaOperativaAsignable && !edicionDisponible)
                ProgramarModoSoloConsultaAsignacion();
        }

        protected override async void OnDisappearing()
        {
            /*
             * Las ventanas modales que forman parte de la misma revisión no
             * liberan el bloqueo. Al abandonar realmente el expediente sí se
             * libera de inmediato; si la aplicación termina abruptamente, el
             * backend lo libera automáticamente al vencer el lease.
             */
            if (!DebeMantenerBloqueoRevisionAlOcultarse)
                await LiberarBloqueoRevisionAsync();

            DesconectarFlujoRevision();
            viewModel.DetenerSeguimiento();
            base.OnDisappearing();
        }

        private bool EsVistaOperativaAsignable =>
            origenActual is
                DiagnosticoIARoutes.ModoAnalizador or
                DiagnosticoIARoutes.ModoAprobador;

        private string ModoAsignacionActual =>
            origenActual == DiagnosticoIARoutes.ModoAprobador
                ? "aprobador"
                : "analizador";

        private string InterfazEtapaActual =>
            ModoAsignacionActual == "aprobador"
                ? DiagnosticoIARoutes.InterfazAprobador
                : DiagnosticoIARoutes.InterfazAnalizador;

        private bool PuedeEditarEtapaActual =>
            PermissionService.Instance.HasUpdate(InterfazEtapaActual);

        private bool PuedeAdministrarAsignacion =>
            PermissionService.Instance.HasRead(
                InterfazControlFitosanitario) &&
            PermissionService.Instance.HasUpdate(
                InterfazControlFitosanitario);

        private async Task<bool> PrepararAsignacionYBloqueoAsync()
        {
            if (!EsVistaOperativaAsignable || diagnosticoIdActual <= 0)
            {
                asignacionRevision = null;
                asignacionInformacionLayout.IsVisible = false;
                tomarInspeccionButton.IsVisible = false;
                administrarAsignacionButton.IsVisible = false;
                ActualizarVisibilidadResumen();
                return true;
            }

            try
            {
                asignacionRevision =
                    await bloqueoRevisionApi.ObtenerAsignacionAsync(
                        diagnosticoIdActual,
                        ModoAsignacionActual);

                ActualizarBannerAsignacion();

                if (!PuedeEditarEtapaActual)
                    return false;

                if (asignacionRevision.AsignadaAlUsuarioActual)
                    return await PrepararBloqueoRevisionAsync();

                return false;
            }
            catch (Exception ex)
            {
                asignacionRevision = null;
                asignacionInformacionLayout.IsVisible = true;
                asignacionTituloLabel.Text = ModoAsignacionActual == "aprobador"
                    ? "Responsable de aprobación"
                    : "Responsable de análisis";
                asignacionResponsableLabel.Text = "No disponible";
                asignacionAyudaLabel.Text = string.IsNullOrWhiteSpace(ex.Message)
                    ? "No fue posible consultar la asignación. Actualiza el expediente para reintentar."
                    : ex.Message;
                tomarInspeccionButton.IsVisible = false;
                administrarAsignacionButton.IsVisible =
                    PuedeAdministrarAsignacion;
                administrarAsignacionButton.Text = "Administrar asignación";
                administrarAsignacionButton.IsEnabled = !procesandoAsignacion;
                ActualizarVisibilidadResumen();
                return false;
            }
        }

        private void ActualizarBannerAsignacion()
        {
            if (!EsVistaOperativaAsignable || asignacionRevision == null)
            {
                asignacionInformacionLayout.IsVisible = false;
                tomarInspeccionButton.IsVisible = false;
                administrarAsignacionButton.IsVisible = false;
                ActualizarVisibilidadResumen();
                return;
            }

            asignacionInformacionLayout.IsVisible = true;
            asignacionTituloLabel.Text = ModoAsignacionActual == "aprobador"
                ? "Responsable de aprobación"
                : "Responsable de análisis";

            if (asignacionRevision.DisponibleParaTomar)
            {
                asignacionResponsableLabel.Text = "Sin asignar";
                asignacionAyudaLabel.Text = PuedeEditarEtapaActual
                    ? "Disponible para tomar"
                    : "Solo consulta · no tienes permiso de actualización para esta etapa";
            }
            else if (asignacionRevision.AsignadaAlUsuarioActual)
            {
                asignacionResponsableLabel.Text =
                    NombreResponsableAsignado();
                asignacionAyudaLabel.Text =
                    "Asignado a ti · edición protegida por bloqueo temporal";
            }
            else
            {
                asignacionResponsableLabel.Text =
                    NombreResponsableAsignado();
                asignacionAyudaLabel.Text =
                    "Asignado a otro usuario · expediente en modo consulta";
            }

            tomarInspeccionButton.IsVisible =
                asignacionRevision.DisponibleParaTomar &&
                PuedeEditarEtapaActual;
            tomarInspeccionButton.IsEnabled = !procesandoAsignacion;

            /*
             * La reasignación es exclusivamente administrativa. No depende del
             * nombre del rol: requiere los permisos reales Leer + Actualizar de
             * diagnosticoIAControlPage.
             */
            administrarAsignacionButton.IsVisible =
                PuedeAdministrarAsignacion;
            administrarAsignacionButton.Text =
                asignacionRevision.DisponibleParaTomar
                    ? "Asignar responsable"
                    : "Reasignar responsable";
            administrarAsignacionButton.IsEnabled = !procesandoAsignacion;

            ActualizarVisibilidadResumen();
        }

        private string NombreResponsableAsignado()
        {
            if (asignacionRevision == null)
                return "Sin asignar";

            if (!string.IsNullOrWhiteSpace(
                    asignacionRevision.UsuarioAsignadoNombre))
            {
                return asignacionRevision.UsuarioAsignadoNombre.Trim();
            }

            return asignacionRevision.UsuarioAsignadoId is > 0
                ? $"Usuario #{asignacionRevision.UsuarioAsignadoId}"
                : "Sin asignar";
        }

        private void ActualizarVisibilidadResumen()
        {
            resumenResponsabilidadBanner.IsVisible =
                tecnicoResponsableDisponible ||
                (EsVistaOperativaAsignable &&
                 asignacionInformacionLayout.IsVisible);
        }

        private async void OnTomarInspeccionClicked(
            object? sender,
            EventArgs e)
        {
            if (procesandoAsignacion ||
                asignacionRevision?.DisponibleParaTomar != true ||
                !PuedeEditarEtapaActual)
            {
                return;
            }

            bool confirmar = await DisplayAlert(
                "Tomar inspección",
                "Quedarás como responsable de esta etapa hasta que un supervisor la reasigne o el flujo termine. ¿Deseas continuar?",
                "Tomar inspección",
                "Cancelar");

            if (!confirmar)
                return;

            procesandoAsignacion = true;
            ActualizarBannerAsignacion();

            try
            {
                asignacionRevision = await bloqueoRevisionApi.TomarAsync(
                    diagnosticoIdActual,
                    ModoAsignacionActual);

                await DisplayAlert(
                    "Inspección asignada",
                    "La etapa quedó asignada a tu usuario. Se volverá a abrir el expediente para iniciar la sesión exclusiva de edición.",
                    "Continuar");

                await RecargarExpedienteActualAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No se pudo tomar la inspección",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "La asignación cambió o no tienes permiso para realizar esta acción."
                        : ex.Message,
                    "Aceptar");

                await PrepararAsignacionYBloqueoAsync();
            }
            finally
            {
                procesandoAsignacion = false;
                ActualizarBannerAsignacion();
            }
        }

        private async void OnAdministrarAsignacionClicked(
            object? sender,
            EventArgs e)
        {
            if (procesandoAsignacion ||
                !EsVistaOperativaAsignable ||
                !PuedeAdministrarAsignacion)
            {
                return;
            }

            procesandoAsignacion = true;
            ActualizarBannerAsignacion();

            try
            {
                List<InspeccionRevisionUsuarioAsignable> usuarios =
                    await bloqueoRevisionApi.ObtenerUsuariosAsignablesAsync(
                        ModoAsignacionActual);

                if (usuarios.Count == 0)
                {
                    await DisplayAlert(
                        "Sin usuarios disponibles",
                        "No hay usuarios activos con permiso de actualización para esta etapa.",
                        "Aceptar");
                    return;
                }

                string[] opciones = usuarios
                    .Select(item => item.TextoMostrar)
                    .ToArray();

                string? seleccion = await DisplayActionSheet(
                    "Seleccione el nuevo responsable",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    seleccion == "Cancelar")
                {
                    return;
                }

                InspeccionRevisionUsuarioAsignable? usuario =
                    usuarios.FirstOrDefault(item =>
                        string.Equals(
                            item.TextoMostrar,
                            seleccion,
                            StringComparison.Ordinal));

                if (usuario == null)
                    return;

                string? motivo = await DisplayPromptAsync(
                    "Motivo de asignación",
                    "Explique el motivo administrativo. Mínimo 8 caracteres; quedará registrado en auditoría.",
                    "Guardar",
                    "Cancelar",
                    string.Empty,
                    1000,
                    Keyboard.Text);

                if (motivo == null)
                    return;

                motivo = motivo.Trim();
                if (motivo.Length < 8)
                {
                    await DisplayAlert(
                        "Motivo requerido",
                        "El motivo debe contener al menos 8 caracteres.",
                        "Aceptar");
                    return;
                }

                await bloqueoRevisionApi.ReasignarAsync(
                    diagnosticoIdActual,
                    ModoAsignacionActual,
                    usuario.UsuarioId,
                    motivo);

                await DisplayAlert(
                    "Asignación actualizada",
                    $"La etapa quedó asignada a {usuario.TextoMostrar}. La acción fue registrada en auditoría.",
                    "Continuar");

                await RecargarExpedienteActualAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No se pudo actualizar la asignación",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "La operación administrativa no pudo completarse."
                        : ex.Message,
                    "Aceptar");
            }
            finally
            {
                procesandoAsignacion = false;
                ActualizarBannerAsignacion();
            }
        }

        private async Task RecargarExpedienteActualAsync()
        {
            if (Shell.Current == null || diagnosticoIdActual <= 0)
                return;

            string ruta = DiagnosticoIARoutes.CrearRutaResultado(
                diagnosticoIdActual,
                origenActual);

            await Shell.Current.GoToAsync("..", false);
            await Shell.Current.GoToAsync(ruta, false);
        }

        /// <summary>
        /// Evita que una ficha sin responsable o asignada a otra persona deje
        /// botones operativos visibles. Se aplica varias veces porque las
        /// acciones por fotografía se crean de forma diferida al materializar
        /// las tarjetas del CollectionView.
        /// </summary>
        private void ProgramarModoSoloConsultaAsignacion()
        {
            AplicarModoSoloConsultaAsignacion();

            _ = Task.Run(async () =>
            {
                await Task.Delay(180);
                Dispatcher.Dispatch(AplicarModoSoloConsultaAsignacion);

                await Task.Delay(320);
                Dispatcher.Dispatch(AplicarModoSoloConsultaAsignacion);
            });
        }

        private void AplicarModoSoloConsultaAsignacion()
        {
            if (!EsVistaOperativaAsignable ||
                asignacionRevision?.AsignadaAlUsuarioActual == true)
            {
                return;
            }

            IReadOnlyList<IVisualTreeElement> elementos =
                this.GetVisualTreeDescendants().ToList();

            foreach (CheckBox selector in elementos.OfType<CheckBox>())
            {
                selector.IsChecked = false;
                selector.IsEnabled = false;
            }

            foreach (Button boton in elementos.OfType<Button>())
            {
                if (ReferenceEquals(boton, tomarInspeccionButton) ||
                    ReferenceEquals(boton, administrarAsignacionButton) ||
                    EsBotonNavegacionConsulta(boton.Text))
                {
                    continue;
                }

                boton.IsEnabled = false;
            }
        }

        private static bool EsBotonNavegacionConsulta(string? texto)
        {
            string valor = (texto ?? string.Empty).Trim();

            return valor.Contains(
                       "Mis inspecciones",
                       StringComparison.OrdinalIgnoreCase) ||
                   valor.Contains(
                       "Bandeja del analizador",
                       StringComparison.OrdinalIgnoreCase) ||
                   valor.Contains(
                       "Bandeja del aprobador",
                       StringComparison.OrdinalIgnoreCase) ||
                   valor.Contains(
                       "Historial",
                       StringComparison.OrdinalIgnoreCase) ||
                   valor.Contains(
                       "Volver",
                       StringComparison.OrdinalIgnoreCase) ||
                   valor.Equals(
                       "Actualizar",
                       StringComparison.OrdinalIgnoreCase);
        }

        private (
            Border Banner,
            Label Tecnico,
            VerticalStackLayout AsignacionLayout,
            Label AsignacionTitulo,
            Label Responsable,
            Label Ayuda,
            Button Tomar,
            Button Administrar) CrearResumenResponsabilidad()
        {
            var tecnicoTitulo = new Label
            {
                Text = "Registrado por",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7773")
            };

            var tecnico = new Label
            {
                Text = "Cargando...",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#315E52"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var tecnicoLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    tecnicoTitulo,
                    tecnico
                }
            };

            var asignacionTitulo = new Label
            {
                Text = "Responsable del flujo",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7773")
            };

            var responsable = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#263A35"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var ayuda = new Label
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#52625D"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var asignacionLayout = new VerticalStackLayout
            {
                IsVisible = false,
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    asignacionTitulo,
                    responsable,
                    ayuda
                }
            };

            var tomar = new Button
            {
                Text = "Tomar inspección",
                HeightRequest = 40,
                Padding = new Thickness(14, 6),
                CornerRadius = 9,
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                IsVisible = false
            };
            tomar.Clicked += OnTomarInspeccionClicked;

            var administrar = new Button
            {
                Text = "Asignar responsable",
                HeightRequest = 40,
                Padding = new Thickness(14, 6),
                CornerRadius = 9,
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                BorderColor = Color.FromArgb("#BFD5CD"),
                BorderWidth = 1,
                TextColor = Color.FromArgb("#315E52"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                IsVisible = false
            };
            administrar.Clicked += OnAdministrarAsignacionClicked;

            View contenido;

            if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            {
                tomar.HorizontalOptions = LayoutOptions.Fill;
                administrar.HorizontalOptions = LayoutOptions.Fill;

                var acciones = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        tomar,
                        administrar
                    }
                };

                contenido = new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        tecnicoLayout,
                        asignacionLayout,
                        acciones
                    }
                };
            }
            else
            {
                var acciones = new HorizontalStackLayout
                {
                    Spacing = 8,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center
                };
                acciones.Children.Add(tomar);
                acciones.Children.Add(administrar);

                var fila = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition
                        {
                            Width = new GridLength(0.85, GridUnitType.Star)
                        },
                        new ColumnDefinition
                        {
                            Width = new GridLength(1.55, GridUnitType.Star)
                        },
                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        }
                    },
                    ColumnSpacing = 18,
                    VerticalOptions = LayoutOptions.Center
                };

                fila.Add(tecnicoLayout, 0, 0);
                fila.Add(asignacionLayout, 1, 0);
                fila.Add(acciones, 2, 0);
                contenido = fila;
            }

            var banner = new Border
            {
                IsVisible = false,
                Padding = new Thickness(14, 11),
                Margin = new Thickness(0, 2, 0, 0),
                BackgroundColor = Color.FromArgb("#F4F8F6"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 11
                },
                HorizontalOptions = LayoutOptions.Fill,
                Content = contenido
            };

            return (
                banner,
                tecnico,
                asignacionLayout,
                asignacionTitulo,
                responsable,
                ayuda,
                tomar,
                administrar);
        }

        private void IntegrarResumenEncabezado()
        {
            /*
             * La cabecera se presenta como una sola tarjeta: navegación, título,
             * creador y responsable pertenecen al mismo contexto visual. En
             * teléfono el resumen baja a una tercera fila; en escritorio ocupa
             * todo el ancho debajo del título sin dejar bloques flotantes.
             */
            if (Content is not ContentView contentView ||
                contentView.Content is not Grid contenedorPrincipal)
            {
                return;
            }

            Grid? encabezado =
                contenedorPrincipal.Children
                    .OfType<Grid>()
                    .FirstOrDefault(x => Grid.GetRow(x) == 0);

            if (encabezado == null)
                return;

            bool esTelefono =
                DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

            int filaResumen = esTelefono ? 2 : 1;
            while (encabezado.RowDefinitions.Count <= filaResumen)
            {
                encabezado.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = GridLength.Auto
                    });
            }

            Grid.SetRow(resumenResponsabilidadBanner, filaResumen);
            Grid.SetColumn(resumenResponsabilidadBanner, 0);
            Grid.SetColumnSpan(
                resumenResponsabilidadBanner,
                Math.Max(1, encabezado.ColumnDefinitions.Count));
            encabezado.Children.Add(resumenResponsabilidadBanner);

            List<Button> botonesEncabezado =
                encabezado.Children
                    .OfType<Button>()
                    .Where(boton =>
                        !ReferenceEquals(boton, tomarInspeccionButton) &&
                        !ReferenceEquals(boton, administrarAsignacionButton))
                    .ToList();

            if (botonesEncabezado.Count > 0)
            {
                Button regresar = botonesEncabezado[0];
                regresar.HeightRequest = 42;
                regresar.WidthRequest = esTelefono ? 150 : 190;
                regresar.Padding = new Thickness(12, 7);
                regresar.BackgroundColor = Color.FromArgb("#F8FBFA");
                regresar.BorderColor = Color.FromArgb("#C8DED6");
                regresar.BorderWidth = 1;
                regresar.FontAttributes = FontAttributes.Bold;
            }

            /*
             * Se envuelve el Grid existente sin reconstruir sus bindings ni
             * comandos. Esto conserva íntegra la lógica de navegación y hace que
             * la cabecera se perciba como una sola unidad en Windows y Android.
             */
            if (!contenedorPrincipal.Children.Remove(encabezado))
                return;

            var tarjetaEncabezado = new Border
            {
                Padding = esTelefono
                    ? new Thickness(13, 12)
                    : new Thickness(16, 14),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#D7E5E0"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 14
                },
                MaximumWidthRequest = 1250,
                HorizontalOptions = LayoutOptions.Fill,
                Content = encabezado
            };

            Grid.SetRow(tarjetaEncabezado, 0);
            contenedorPrincipal.Children.Add(tarjetaEncabezado);
        }

        private async Task CargarTecnicoResponsableAsync()
        {
            tecnicoResponsableDisponible = false;

            if (diagnosticoIdActual <= 0)
            {
                ActualizarVisibilidadResumen();
                return;
            }

            try
            {
                TecnicoInspeccionFiltroItem tecnico =
                    await bandejaApi.ObtenerTecnicoResponsableAsync(
                        diagnosticoIdActual);

                tecnicoResponsableLabel.Text =
                    !string.IsNullOrWhiteSpace(tecnico.NombreCompleto)
                        ? tecnico.NombreCompleto.Trim()
                        : !string.IsNullOrWhiteSpace(tecnico.NombreUsuario)
                            ? tecnico.NombreUsuario.Trim()
                            : tecnico.UsuarioTecnicoId > 0
                                ? $"Usuario #{tecnico.UsuarioTecnicoId}"
                                : "Usuario no disponible";

                tecnicoResponsableDisponible = true;
            }
            catch
            {
                /*
                 * El creador es un dato informativo. Si no puede cargarse, el
                 * flujo operativo continúa sin bloquear el expediente.
                 */
                tecnicoResponsableLabel.Text = "Usuario no disponible";
                tecnicoResponsableDisponible = false;
            }

            ActualizarVisibilidadResumen();
        }

    }
}
