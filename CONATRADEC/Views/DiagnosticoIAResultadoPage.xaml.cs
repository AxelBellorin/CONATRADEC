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
        private readonly Border tecnicoResponsableBanner;
        private readonly Label asignacionResponsableLabel;
        private readonly Label asignacionAyudaLabel;
        private readonly Border asignacionResponsableBanner;
        private readonly Button tomarInspeccionButton;
        private readonly Button administrarAsignacionButton;

        private int diagnosticoIdActual;
        private string origenActual = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool avisoLocalMostrado;
        private bool procesandoAsignacion;
        private InspeccionRevisionAsignacion? asignacionRevision;

        public DiagnosticoIAResultadoPage()
        {
            InitializeComponent();

            (tecnicoResponsableBanner, tecnicoResponsableLabel) =
                CrearBannerTecnicoResponsable();

            (
                asignacionResponsableBanner,
                asignacionResponsableLabel,
                asignacionAyudaLabel,
                tomarInspeccionButton,
                administrarAsignacionButton
            ) = CrearBannerAsignacionResponsable();

            IntegrarBannersEncabezado();

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
                asignacionResponsableBanner.IsVisible = false;
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
                {
                    asignacionAyudaLabel.Text =
                        "Tu permiso para esta etapa es de consulta. No se adquirirá un bloqueo de edición.";
                    return false;
                }

                if (asignacionRevision.AsignadaAlUsuarioActual)
                    return await PrepararBloqueoRevisionAsync();

                return false;
            }
            catch (Exception ex)
            {
                asignacionRevision = null;
                asignacionResponsableBanner.IsVisible = true;
                asignacionResponsableLabel.Text =
                    "No fue posible consultar el responsable de la etapa.";
                asignacionAyudaLabel.Text = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Actualice el expediente para volver a intentarlo."
                    : ex.Message;
                tomarInspeccionButton.IsVisible = false;
                administrarAsignacionButton.IsVisible =
                    PuedeAdministrarAsignacion;
                return false;
            }
        }

        private void ActualizarBannerAsignacion()
        {
            if (!EsVistaOperativaAsignable || asignacionRevision == null)
            {
                asignacionResponsableBanner.IsVisible = false;
                return;
            }

            string etapa = ModoAsignacionActual == "aprobador"
                ? "Aprobador responsable"
                : "Analizador responsable";

            asignacionResponsableBanner.IsVisible = true;
            asignacionResponsableLabel.Text =
                $"{etapa}: {asignacionRevision.ResponsableTexto}";

            if (asignacionRevision.DisponibleParaTomar)
            {
                asignacionAyudaLabel.Text = PuedeEditarEtapaActual
                    ? "La inspección está disponible. Tomarla te convierte en responsable persistente de esta etapa; después se abrirá el bloqueo temporal de edición."
                    : "La inspección está sin asignar, pero tu usuario no posee permiso de actualización para tomarla.";
            }
            else if (asignacionRevision.AsignadaAlUsuarioActual)
            {
                asignacionAyudaLabel.Text =
                    "Esta etapa está asignada a ti. El bloqueo temporal se adquiere únicamente mientras mantienes abierta la ficha de trabajo.";
            }
            else
            {
                asignacionAyudaLabel.Text =
                    "Puedes consultar el expediente, pero solo el responsable asignado puede modificar esta etapa. Un supervisor autorizado puede reasignarla.";
            }

            tomarInspeccionButton.IsVisible =
                asignacionRevision.DisponibleParaTomar &&
                PuedeEditarEtapaActual;
            tomarInspeccionButton.IsEnabled = !procesandoAsignacion;

            administrarAsignacionButton.IsVisible =
                PuedeAdministrarAsignacion;
            administrarAsignacionButton.Text =
                asignacionRevision.DisponibleParaTomar
                    ? "Asignar responsable"
                    : "Reasignar responsable";
            administrarAsignacionButton.IsEnabled = !procesandoAsignacion;
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

        private static (Border Banner, Label Texto)
            CrearBannerTecnicoResponsable()
        {
            var etiqueta = new Label
            {
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#315E52"),
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalTextAlignment = TextAlignment.Center
            };

            var titulo = new Label
            {
                Text = "Registrado por",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7773"),
                VerticalTextAlignment = TextAlignment.Center
            };

            View contenido;

            if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            {
                contenido = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        titulo,
                        etiqueta
                    }
                };
            }
            else
            {
                var fila = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        },
                        new ColumnDefinition
                        {
                            Width = GridLength.Star
                        }
                    },
                    ColumnSpacing = 12
                };

                fila.Add(titulo, 0, 0);
                fila.Add(etiqueta, 1, 0);
                contenido = fila;
            }

            var banner = new Border
            {
                IsVisible = false,
                Padding = new Thickness(11, 7),
                Margin = new Thickness(0, 6, 0, 0),
                BackgroundColor = Color.FromArgb("#EAF3EF"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 10
                },
                MaximumWidthRequest = 520,
                HorizontalOptions = LayoutOptions.Start,
                Content = contenido
            };

            return (banner, etiqueta);
        }

        private (Border Banner, Label Responsable, Label Ayuda, Button Tomar, Button Administrar)
            CrearBannerAsignacionResponsable()
        {
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

            var tomar = new Button
            {
                Text = "Tomar inspección",
                HeightRequest = 42,
                Padding = new Thickness(14, 7),
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
                HeightRequest = 42,
                Padding = new Thickness(14, 7),
                CornerRadius = 9,
                BackgroundColor = Color.FromArgb("#EEF2F0"),
                TextColor = Color.FromArgb("#315E52"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                IsVisible = false
            };
            administrar.Clicked += OnAdministrarAsignacionClicked;

            View acciones;
            if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            {
                tomar.HorizontalOptions = LayoutOptions.Fill;
                administrar.HorizontalOptions = LayoutOptions.Fill;
                acciones = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        tomar,
                        administrar
                    }
                };
            }
            else
            {
                var fila = new HorizontalStackLayout
                {
                    Spacing = 8
                };
                fila.Children.Add(tomar);
                fila.Children.Add(administrar);
                acciones = fila;
            }

            var contenido = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    new Label
                    {
                        Text = "Responsabilidad del flujo",
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#6B7773")
                    },
                    responsable,
                    ayuda,
                    acciones
                }
            };

            var banner = new Border
            {
                IsVisible = false,
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 6, 0, 0),
                BackgroundColor = Color.FromArgb("#F8FBFA"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 11
                },
                MaximumWidthRequest = 720,
                HorizontalOptions = LayoutOptions.Fill,
                Content = contenido
            };

            return (banner, responsable, ayuda, tomar, administrar);
        }

        private void IntegrarBannersEncabezado()
        {
            /*
             * Los datos del creador y del responsable pertenecen al encabezado
             * del expediente. Se integran debajo del título para no crear una
             * franja independiente sobre el menú lateral de Windows.
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

            VerticalStackLayout? bloqueTitulo =
                encabezado.Children
                    .OfType<VerticalStackLayout>()
                    .FirstOrDefault();

            if (bloqueTitulo == null)
                return;

            bloqueTitulo.Children.Add(tecnicoResponsableBanner);
            bloqueTitulo.Children.Add(asignacionResponsableBanner);
        }

        private async Task CargarTecnicoResponsableAsync()
        {
            if (diagnosticoIdActual <= 0)
            {
                tecnicoResponsableBanner.IsVisible = false;
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
                tecnicoResponsableBanner.IsVisible = true;
            }
            catch
            {
                /*
                 * El dato es informativo. Un problema al cargar el nombre del
                 * usuario creador no debe bloquear el expediente ni sus decisiones.
                 */
                tecnicoResponsableBanner.IsVisible = false;
            }
        }
    }
}
