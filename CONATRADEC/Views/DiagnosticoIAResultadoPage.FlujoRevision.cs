using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Layouts;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAResultadoPage
    {
        private readonly InspeccionRevisionApiService revisionApi = new();
        private readonly Dictionary<int, DevolucionTecnicoFotografiaV2>
            devolucionesPorFoto = [];
        private readonly Dictionary<int, AccionesTarjetaRevision>
            accionesPorFoto = [];

        private ContextoRevisionAnalizadorV2? contextoRevision;
        private bool flujoRevisionConectado;
        private bool filtrandoFotosRevision;
        private bool operacionRevisionActiva;
        private bool errorContextoMostrado;
        private CancellationTokenSource? refrescoContextoCts;
        private int idRevision;
        private string modoRevision = DiagnosticoIARoutes.ModoMisInspecciones;

        private bool EsVistaAnalizadorRevision =>
            string.Equals(
                modoRevision,
                DiagnosticoIARoutes.ModoAnalizador,
                StringComparison.OrdinalIgnoreCase);

        private bool EsVistaAprobadorRevision =>
            string.Equals(
                modoRevision,
                DiagnosticoIARoutes.ModoAprobador,
                StringComparison.OrdinalIgnoreCase);

        private bool EsVistaTecnicoRevision =>
            modoRevision is
                DiagnosticoIARoutes.ModoMisInspecciones or
                DiagnosticoIARoutes.ModoDecisionesPendientes;

        private void ConfigurarFlujoRevision(int id, string modo)
        {
            idRevision = id;
            modoRevision = DiagnosticoIARoutes.NormalizarModo(modo);
        }

        /// <summary>
        /// Registra las rutas auxiliares. Las acciones ya no se colocan en un
        /// panel flotante global: cada tarjeta recibe sus propios controles.
        /// </summary>
        private void InicializarCapaRevision()
        {
            MotivoDevolucionTecnicoRoutes.AsegurarRegistro();
        }

        private async Task AplicarFlujoRevisionAsync()
        {
            if (idRevision <= 0)
                return;

            ConectarFlujoRevision();

            try
            {
                contextoRevision = await revisionApi.ObtenerContextoAsync(
                    idRevision);

                errorContextoMostrado = false;
                ActualizarMapaDevoluciones(contextoRevision.Devoluciones);
                NormalizarPresentacionFotografias();
                FiltrarFotografiasPorRol();
                OcultarAccionesGlobalesAnteriores();
                ProgramarIntegracionAccionesTarjetas();
            }
            catch (Exception ex)
            {
                contextoRevision = null;
                accionesPorFoto.Clear();

                if (!errorContextoMostrado &&
                    (EsVistaAnalizadorRevision || EsVistaTecnicoRevision))
                {
                    errorContextoMostrado = true;
                    await DisplayAlert(
                        "Flujo de revisión",
                        string.IsNullOrWhiteSpace(ex.Message)
                            ? "No fue posible cargar el flujo de revisión."
                            : ex.Message,
                        "Aceptar");
                }
            }
        }

        private void ConectarFlujoRevision()
        {
            if (flujoRevisionConectado)
                return;

            viewModel.Fotografias.CollectionChanged +=
                OnFotografiasRevisionCollectionChanged;
            flujoRevisionConectado = true;
            ActualizarSuscripcionesFotografias();
        }

        private void DesconectarFlujoRevision()
        {
            if (!flujoRevisionConectado)
                return;

            viewModel.Fotografias.CollectionChanged -=
                OnFotografiasRevisionCollectionChanged;

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
                foto.PropertyChanged -= OnFotoRevisionPropertyChanged;

            refrescoContextoCts?.Cancel();
            refrescoContextoCts?.Dispose();
            refrescoContextoCts = null;
            accionesPorFoto.Clear();
            flujoRevisionConectado = false;
        }

        private void OnFotografiasRevisionCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (filtrandoFotosRevision)
                return;

            ActualizarSuscripcionesFotografias();
            Dispatcher.Dispatch(() =>
            {
                NormalizarPresentacionFotografias();
                FiltrarFotografiasPorRol();
                OcultarAccionesGlobalesAnteriores();
                ProgramarIntegracionAccionesTarjetas();
                ProgramarRefrescoContexto();
            });
        }

        private void ProgramarRefrescoContexto()
        {
            if (idRevision <= 0 || operacionRevisionActiva)
                return;

            refrescoContextoCts?.Cancel();
            refrescoContextoCts?.Dispose();
            refrescoContextoCts = new CancellationTokenSource();
            CancellationToken token = refrescoContextoCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(350, token);
                    if (token.IsCancellationRequested)
                        return;

                    ContextoRevisionAnalizadorV2 actualizado =
                        await revisionApi.ObtenerContextoAsync(
                            idRevision,
                            token);

                    Dispatcher.Dispatch(() =>
                    {
                        contextoRevision = actualizado;
                        ActualizarMapaDevoluciones(actualizado.Devoluciones);
                        ProgramarIntegracionAccionesTarjetas();
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch
                {
                    // El botón Actualizar permite reintentar sin bloquear la UI.
                }
            }, token);
        }

        private void ActualizarMapaDevoluciones(
            IEnumerable<DevolucionTecnicoFotografiaV2>? devoluciones)
        {
            devolucionesPorFoto.Clear();

            foreach (DevolucionTecnicoFotografiaV2 devolucion in
                     devoluciones?.Where(item => item.EstaPendiente) ?? [])
            {
                devolucionesPorFoto[devolucion.FotografiaId] = devolucion;
            }
        }

        private void ActualizarSuscripcionesFotografias()
        {
            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                foto.PropertyChanged -= OnFotoRevisionPropertyChanged;
                foto.PropertyChanged += OnFotoRevisionPropertyChanged;
            }
        }

        private void OnFotoRevisionPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is
                nameof(InspeccionFotoV2.Seleccionada) or
                nameof(InspeccionFotoV2.JerarquiaAlbum))
            {
                ProgramarIntegracionAccionesTarjetas();
            }
        }

        private void NormalizarPresentacionFotografias()
        {
            for (int indice = 0; indice < viewModel.Fotografias.Count; indice++)
            {
                InspeccionFotoV2 foto = viewModel.Fotografias[indice];

                if ((foto.Descartada || string.Equals(
                        foto.Estado,
                        InspeccionFotoEstados.Descartada,
                        StringComparison.OrdinalIgnoreCase)) &&
                    foto.ResultadoIA == null)
                {
                    foto.ResultadoIA = new InspeccionFotoResultadoIAV2
                    {
                        DiagnosticoProbable =
                            "Fotografía descartada por el técnico",
                        ResumenImagen = string.IsNullOrWhiteSpace(
                            foto.MotivoDescarte)
                            ? "Esta evidencia quedó fuera del flujo operativo."
                            : $"Motivo: {foto.MotivoDescarte}"
                    };

                    viewModel.Fotografias[indice] = foto;
                }
            }
        }

        private void FiltrarFotografiasPorRol()
        {
            if (!EsVistaAnalizadorRevision && !EsVistaAprobadorRevision)
                return;

            filtrandoFotosRevision = true;
            try
            {
                List<InspeccionFotoV2> quitar = viewModel.Fotografias
                    .Where(foto =>
                        foto.Descartada ||
                        string.Equals(
                            foto.Estado,
                            InspeccionFotoEstados.Descartada,
                            StringComparison.OrdinalIgnoreCase) ||
                        (EsVistaAnalizadorRevision &&
                         !EsEstadoVisibleAnalizador(foto.Estado)))
                    .ToList();

                foreach (InspeccionFotoV2 foto in quitar)
                {
                    foto.PropertyChanged -= OnFotoRevisionPropertyChanged;
                    viewModel.Fotografias.Remove(foto);
                    accionesPorFoto.Remove(foto.FotografiaId);
                }
            }
            finally
            {
                filtrandoFotosRevision = false;
            }
        }

        private static bool EsEstadoVisibleAnalizador(string? estado) =>
            estado is
                InspeccionFotoEstados.PendienteAnalizador or
                InspeccionFotoEstados.EnAnalisisHumano or
                InspeccionFotoEstados.DevueltaAnalizador;

        /// <summary>
        /// El panel superior histórico dependía de seleccionar fotografías y
        /// duplicaba acciones. En la vista del analizador se oculta por completo
        /// para que cada expediente fotográfico opere desde su propia tarjeta.
        /// </summary>
        private void OcultarAccionesGlobalesAnteriores()
        {
            try
            {
                IReadOnlyList<IVisualTreeElement> elementos =
                    this.GetVisualTreeDescendants().ToList();

                if (EsVistaAnalizadorRevision)
                {
                    Label? titulo = elementos
                        .OfType<Label>()
                        .FirstOrDefault(item => string.Equals(
                            item.Text,
                            "Acciones por fotografía",
                            StringComparison.OrdinalIgnoreCase));

                    Border? panel = titulo == null
                        ? null
                        : BuscarAncestro<Border>(titulo);

                    if (panel != null)
                        panel.IsVisible = false;

                    foreach (CheckBox check in elementos.OfType<CheckBox>()
                                 .Where(item =>
                                     item.BindingContext is InspeccionFotoV2))
                    {
                        check.IsVisible = false;
                        check.IsChecked = false;
                    }

                    foreach (Button boton in elementos.OfType<Button>())
                    {
                        if (string.Equals(
                                boton.Text,
                                "Clasificación humana",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            boton.IsVisible = false;
                        }
                    }
                }

                if (EsVistaTecnicoRevision)
                {
                    foreach (Button boton in elementos.OfType<Button>())
                    {
                        if (string.Equals(
                                boton.Text,
                                "Finalizar etapa técnica",
                                StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(
                                boton.Text,
                                "Finalizar y enviar inspección",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            boton.IsEnabled = viewModel.PuedeCerrarInspeccion;
                        }
                    }
                }
            }
            catch
            {
                // Las validaciones del backend permanecen activas.
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

        private void ProgramarIntegracionAccionesTarjetas()
        {
            Dispatcher.Dispatch(IntegrarAccionesEnTarjetas);

            _ = Task.Run(async () =>
            {
                await Task.Delay(140);
                Dispatcher.Dispatch(IntegrarAccionesEnTarjetas);

                await Task.Delay(260);
                Dispatcher.Dispatch(IntegrarAccionesEnTarjetas);
            });
        }

        private void IntegrarAccionesEnTarjetas()
        {
            if (contextoRevision == null)
                return;

            OcultarAccionesGlobalesAnteriores();

            IReadOnlyList<IVisualTreeElement> elementos =
                this.GetVisualTreeDescendants().ToList();

            HashSet<IVisualTreeElement> visibles = elementos.ToHashSet();
            foreach (int id in accionesPorFoto
                         .Where(item => !visibles.Contains(item.Value.Panel))
                         .Select(item => item.Key)
                         .ToList())
            {
                accionesPorFoto.Remove(id);
            }

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                if (EsVistaAnalizadorRevision &&
                    EsEstadoVisibleAnalizador(foto.Estado))
                {
                    AccionesTarjetaRevision? acciones =
                        ObtenerOCrearAccionesAnalizador(foto, elementos);
                    if (acciones != null)
                        ActualizarAccionesAnalizador(acciones, foto);
                    continue;
                }

                if (EsVistaTecnicoRevision &&
                    devolucionesPorFoto.TryGetValue(
                        foto.FotografiaId,
                        out DevolucionTecnicoFotografiaV2? devolucion) &&
                    devolucion.EstaPendiente)
                {
                    AccionesTarjetaRevision? acciones =
                        ObtenerOCrearAccionesTecnico(
                            foto,
                            devolucion,
                            elementos);
                    if (acciones != null)
                        ActualizarAccionesTecnico(acciones, foto, devolucion);
                    continue;
                }

                if (accionesPorFoto.TryGetValue(
                        foto.FotografiaId,
                        out AccionesTarjetaRevision? existente))
                {
                    existente.Panel.IsVisible = false;
                }
            }
        }

        private AccionesTarjetaRevision? ObtenerOCrearAccionesAnalizador(
            InspeccionFotoV2 foto,
            IReadOnlyList<IVisualTreeElement> elementos)
        {
            if (accionesPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out AccionesTarjetaRevision? existente) &&
                existente.EsAnalizador &&
                elementos.Contains(existente.Panel))
            {
                return existente;
            }

            Border? tarjeta = EncontrarTarjetaFotografia(foto, elementos);
            if (tarjeta == null)
                return null;

            Label estado = CrearEtiquetaAyuda();
            Label bloqueo = CrearEtiquetaBloqueo();

            Button clasificar = CrearBotonTarjeta(
                "Confirmar o corregir clasificación",
                "#9B552C",
                Colors.White);
            clasificar.CommandParameter = foto;
            clasificar.Clicked += OnClasificarTarjetaClicked;

            Button devolver = CrearBotonTarjeta(
                "Solicitar corrección al técnico",
                "#FFF4EA",
                Color.FromArgb("#9B552C"));
            devolver.CommandParameter = foto;
            devolver.Clicked += OnDevolverTarjetaClicked;

            Button finalizar = CrearBotonTarjeta(
                "Finalizar revisión y enviar al aprobador",
                "#263A35",
                Colors.White);
            finalizar.CommandParameter = foto;
            finalizar.Clicked += OnFinalizarRevisionTarjetaClicked;

            Microsoft.Maui.Controls.Layout botones = CrearContenedorBotones(
                clasificar,
                devolver,
                finalizar);

            var contenido = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    new Label
                    {
                        Text = "Acciones del analizador",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#263A35")
                    },
                    estado,
                    botones,
                    bloqueo
                }
            };

            Border panel = CrearPanelTarjeta(contenido, "#F8FBFA", "#C8DED6");
            IntegrarPanelEnTarjeta(tarjeta, panel);

            var acciones = new AccionesTarjetaRevision(
                tarjeta,
                panel,
                true,
                estado,
                bloqueo,
                clasificar,
                devolver,
                finalizar,
                null);

            accionesPorFoto[foto.FotografiaId] = acciones;
            return acciones;
        }

        private AccionesTarjetaRevision? ObtenerOCrearAccionesTecnico(
            InspeccionFotoV2 foto,
            DevolucionTecnicoFotografiaV2 devolucion,
            IReadOnlyList<IVisualTreeElement> elementos)
        {
            if (accionesPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out AccionesTarjetaRevision? existente) &&
                !existente.EsAnalizador &&
                elementos.Contains(existente.Panel))
            {
                return existente;
            }

            Border? tarjeta = EncontrarTarjetaFotografia(foto, elementos);
            if (tarjeta == null)
                return null;

            Label estado = CrearEtiquetaAyuda();
            Label bloqueo = CrearEtiquetaBloqueo();
            Button atender = CrearBotonTarjeta(
                "Atender corrección solicitada",
                "#F2C94C",
                Color.FromArgb("#263A35"));
            atender.CommandParameter = foto;
            atender.Clicked += OnAtenderDevolucionTarjetaClicked;

            var contenido = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    new Label
                    {
                        Text = "Corrección solicitada por el analizador",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#7A4B1F")
                    },
                    estado,
                    bloqueo,
                    atender
                }
            };

            Border panel = CrearPanelTarjeta(contenido, "#FFF9E8", "#E2B93B");
            IntegrarPanelEnTarjeta(tarjeta, panel);

            var acciones = new AccionesTarjetaRevision(
                tarjeta,
                panel,
                false,
                estado,
                bloqueo,
                null,
                null,
                null,
                atender);

            accionesPorFoto[foto.FotografiaId] = acciones;
            return acciones;
        }

        private static Border? EncontrarTarjetaFotografia(
            InspeccionFotoV2 foto,
            IReadOnlyList<IVisualTreeElement> elementos)
        {
            return elementos
                .OfType<Border>()
                .Where(item => ReferenceEquals(item.BindingContext, foto))
                .Select(item => new
                {
                    Borde = item,
                    Puntaje = CalcularPuntajeTarjeta(item)
                })
                .Where(item => item.Puntaje > 0)
                .OrderByDescending(item => item.Puntaje)
                .Select(item => item.Borde)
                .FirstOrDefault();
        }

        private static int CalcularPuntajeTarjeta(Border borde)
        {
            if (borde.Content is not Grid grid)
                return 0;

            int puntaje = grid.RowDefinitions.Count * 100;
            puntaje += grid.ColumnDefinitions.Count * 20;
            puntaje += grid.Children.Count;

            if (grid.RowDefinitions.Count >= 3)
                puntaje += 500;

            return puntaje;
        }

        private static void IntegrarPanelEnTarjeta(
            Border tarjeta,
            Border panel)
        {
            if (tarjeta.Content is not View contenidoOriginal)
                return;

            tarjeta.Content = null;
            var contenedor = new VerticalStackLayout
            {
                Spacing = 12
            };
            contenedor.Children.Add(contenidoOriginal);
            contenedor.Children.Add(panel);
            tarjeta.Content = contenedor;
        }

        private static Border CrearPanelTarjeta(
            View contenido,
            string fondo,
            string borde) =>
            new()
            {
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 0),
                BackgroundColor = Color.FromArgb(fondo),
                Stroke = Color.FromArgb(borde),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                Content = contenido
            };

        private static Label CrearEtiquetaAyuda() =>
            new()
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#4F5D59"),
                LineBreakMode = LineBreakMode.WordWrap
            };

        private static Label CrearEtiquetaBloqueo() =>
            new()
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#6B5710"),
                LineBreakMode = LineBreakMode.WordWrap
            };

        private static Button CrearBotonTarjeta(
            string texto,
            string fondo,
            Color textoColor) =>
            new()
            {
                Text = texto,
                BackgroundColor = Color.FromArgb(fondo),
                TextColor = textoColor,
                CornerRadius = 10,
                HeightRequest = 44,
                Padding = new Thickness(12, 7),
                Margin = new Thickness(0, 0, 8, 8),
                FontSize = 12,
                HorizontalOptions = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? LayoutOptions.Fill
                    : LayoutOptions.Start
            };

        private static Microsoft.Maui.Controls.Layout CrearContenedorBotones(params Button[] botones)
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                var vertical = new VerticalStackLayout
                {
                    Spacing = 2
                };

                foreach (Button boton in botones)
                {
                    boton.HorizontalOptions = LayoutOptions.Fill;
                    vertical.Children.Add(boton);
                }

                return vertical;
            }

            var flex = new FlexLayout
            {
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.Wrap,
                JustifyContent = FlexJustify.Start,
                AlignItems = FlexAlignItems.Center
            };

            foreach (Button boton in botones)
            {
                boton.MinimumWidthRequest = 210;
                flex.Children.Add(boton);
            }

            return flex;
        }

        private void ActualizarAccionesAnalizador(
            AccionesTarjetaRevision acciones,
            InspeccionFotoV2 foto)
        {
            ResumenRevisionAnalizadorV2 resumen = contextoRevision!.Resumen;
            acciones.Panel.IsVisible = true;
            acciones.Estado.Text = foto.TieneAnalisisHumano
                ? "La clasificación humana está guardada como borrador y puede corregirse antes del cierre."
                : "Esta fotografía todavía necesita una clasificación humana.";

            if (acciones.Clasificar != null)
            {
                acciones.Clasificar.Text = foto.TieneAnalisisHumano
                    ? "Revisar o corregir clasificación"
                    : "Confirmar o corregir clasificación";
                acciones.Clasificar.IsEnabled =
                    !operacionRevisionActiva &&
                    EsEstadoVisibleAnalizador(foto.Estado);
            }

            if (acciones.Devolver != null)
            {
                acciones.Devolver.IsEnabled =
                    !operacionRevisionActiva &&
                    EsEstadoVisibleAnalizador(foto.Estado);
            }

            if (acciones.Finalizar != null)
            {
                acciones.Finalizar.IsEnabled =
                    !operacionRevisionActiva &&
                    resumen.PuedeFinalizarRevision &&
                    !resumen.EtapaAnalizadorFinalizada;
            }

            acciones.Bloqueo.Text = resumen.PuedeFinalizarRevision
                ? "Todas las fotografías están listas. Este botón finaliza la revisión completa de la inspección."
                : resumen.MotivoNoPuedeFinalizarRevision;
        }

        private static void ActualizarAccionesTecnico(
            AccionesTarjetaRevision acciones,
            InspeccionFotoV2 foto,
            DevolucionTecnicoFotografiaV2 devolucion)
        {
            acciones.Panel.IsVisible = true;
            acciones.Estado.Text =
                $"Motivo: {devolucion.MotivoNombre}. " +
                (devolucion.RequiereNuevaFotografia
                    ? "Se requiere una nueva fotografía."
                    : "Puede corregirse la evidencia actual.");
            acciones.Bloqueo.Text = devolucion.InstruccionCompleta;

            if (acciones.Atender != null)
            {
                acciones.Atender.CommandParameter = foto;
                acciones.Atender.IsEnabled = true;
            }
        }

        private async void OnClasificarTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoVisibleAnalizador(foto.Estado))
            {
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                bool guardada = await ClasificarFotografiaAsync(foto);
                if (!guardada)
                    return;

                await DisplayAlert(
                    "Clasificación humana",
                    "La clasificación de esta fotografía fue guardada como borrador.",
                    "Aceptar");

                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Clasificación humana",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async Task<bool> ClasificarFotografiaAsync(
            InspeccionFotoV2 foto)
        {
            string? diagnostico = await DisplayPromptAsync(
                "Diagnóstico humano",
                $"Confirme o corrija el diagnóstico de {foto.Titulo}.",
                "Continuar",
                "Cancelar",
                "Diagnóstico obligatorio",
                300,
                Keyboard.Default,
                foto.UltimoAnalisisHumano?.Diagnostico ??
                foto.ResultadoIA?.DiagnosticoProbable ?? string.Empty);

            if (string.IsNullOrWhiteSpace(diagnostico))
                return false;

            string? categoria = await DisplayActionSheet(
                "Categoría principal",
                "Cancelar",
                null,
                "ENFERMEDAD",
                "PLAGA",
                "ALTERACION_NUTRICIONAL",
                "ESTRES_ABIOTICO",
                "DANO_MECANICO",
                "AFECTACION_NO_DETERMINADA",
                "NO_APLICA");

            if (string.IsNullOrWhiteSpace(categoria) || categoria == "Cancelar")
                return false;

            string? severidad = await DisplayActionSheet(
                "Severidad visual",
                "Cancelar",
                null,
                "LEVE",
                "MODERADA",
                "SEVERA",
                "NO_EVALUABLE",
                "NO_APLICA");

            if (string.IsNullOrWhiteSpace(severidad) || severidad == "Cancelar")
                return false;

            string? certeza = await DisplayActionSheet(
                "Nivel de certeza",
                "Cancelar",
                null,
                "ALTO",
                "MEDIO",
                "BAJO",
                "NO_DETERMINADO");

            if (string.IsNullOrWhiteSpace(certeza) || certeza == "Cancelar")
                return false;

            string? observaciones = await DisplayPromptAsync(
                "Observaciones",
                "Documente la confirmación o las diferencias respecto al resultado de la IA.",
                "Guardar borrador",
                "Cancelar",
                "Opcional",
                3000,
                Keyboard.Default,
                foto.UltimoAnalisisHumano?.Observaciones ?? string.Empty);

            if (observaciones == null)
                return false;

            if (!foto.TieneClasificacionAlbumCompleta)
            {
                var paginaJerarquia = new JerarquiaAlbumFotografiaPage(
                    idRevision,
                    foto,
                    "ANALIZADOR");
                await Navigation.PushModalAsync(paginaJerarquia);
                bool guardado = await paginaJerarquia.ResultadoTask;
                if (!guardado)
                    return false;
            }

            InspeccionFotoResultadoIAV2? ia = foto.ResultadoIA;
            var item = new InspeccionFotoAnalisisHumanoRequestV2
            {
                FotografiaId = foto.FotografiaId,
                CalidadEvaluacion = ia?.CalidadEvaluacion ?? "NO_EVALUABLE",
                EstadoGeneral = ia?.EstadoGeneral ?? "INDETERMINADA",
                CategoriaPrincipal = categoria,
                CategoriasSecundarias = ia?.CategoriasSecundarias ?? [],
                Diagnostico = diagnostico.Trim(),
                TipoDiagnostico = ia?.TipoDiagnostico ?? string.Empty,
                Severidad = severidad,
                NivelCerteza = certeza,
                Observaciones = observaciones.Trim()
            };

            await InspeccionFitosanitariaApiService.Instance
                .GuardarAnalisisHumanoAsync(
                    idRevision,
                    [item],
                    enviarAprobacion: false);

            return true;
        }

        private async void OnDevolverTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoVisibleAnalizador(foto.Estado))
            {
                return;
            }

            var formulario = new DevolucionTecnicoFotografiaPage(foto, 1, 1);
            Task<DevolucionTecnicoFormularioResultado?> espera =
                formulario.EsperarResultadoAsync();
            await Navigation.PushModalAsync(formulario, animated: false);
            DevolucionTecnicoFormularioResultado? resultado = await espera;

            if (resultado == null)
                return;

            bool confirmar = await DisplayAlert(
                "Solicitar corrección",
                "La fotografía saldrá temporalmente de la bandeja del analizador y la etapa técnica se reabrirá para que el técnico atienda la solicitud. ¿Desea continuar?",
                "Devolver",
                "Cancelar");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                await revisionApi.DevolverTecnicoAsync(
                    idRevision,
                    foto.FotografiaId,
                    resultado.MotivoId,
                    resultado.Instrucciones);

                await DisplayAlert(
                    "Corrección solicitada",
                    "La fotografía fue devuelta al técnico. Los demás borradores humanos se conservaron.",
                    "Aceptar");

                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Devolver al técnico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnAtenderDevolucionTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !devolucionesPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out DevolucionTecnicoFotografiaV2? devolucion))
            {
                return;
            }

            var formulario = new CorreccionTecnicoFotografiaPage(
                foto,
                devolucion);
            Task<CorreccionTecnicoFormularioResultado?> espera =
                formulario.EsperarResultadoAsync();
            await Navigation.PushModalAsync(formulario, animated: false);
            CorreccionTecnicoFormularioResultado? resultado = await espera;

            if (resultado == null)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                await revisionApi.ResolverDevolucionAsync(
                    idRevision,
                    foto.FotografiaId,
                    resultado.TipoFotografia,
                    resultado.FechaIdentificacionCampo,
                    resultado.RespuestaTecnico);

                await DisplayAlert(
                    "Corrección registrada",
                    "La fotografía quedó pendiente de un nuevo análisis con IA. Selecciónela y use «Analizar con IA».",
                    "Aceptar");

                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Corrección técnica",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnFinalizarRevisionTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                contextoRevision?.Resumen.PuedeFinalizarRevision != true)
            {
                return;
            }

            bool confirmar = await DisplayAlert(
                "Finalizar revisión humana",
                "Esta acción pertenece a la inspección completa. Todas las últimas clasificaciones humanas se enviarán juntas al aprobador. ¿Desea continuar?",
                "Finalizar y enviar",
                "Cancelar");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                contextoRevision = await revisionApi.FinalizarAnalizadorAsync(
                    idRevision);

                await DisplayAlert(
                    "Revisión finalizada",
                    "Todas las fotografías evaluables quedaron pendientes de aprobación.",
                    "Aceptar");

                if (viewModel.RegresarResultadoCommand.CanExecute(null))
                    viewModel.RegresarResultadoCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Finalizar revisión",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async Task RecargarDespuesOperacionRevisionAsync()
        {
            if (viewModel.ActualizarCommand.CanExecute(null))
                viewModel.ActualizarCommand.Execute(null);

            await EsperarActualizacionViewModelAsync();
            await AplicarFlujoRevisionAsync();
        }

        private void ActualizarEstadoBotonesTarjetas()
        {
            foreach (KeyValuePair<int, AccionesTarjetaRevision> par in
                     accionesPorFoto)
            {
                int id = par.Key;
                AccionesTarjetaRevision acciones = par.Value;
                InspeccionFotoV2? foto = viewModel.Fotografias
                    .FirstOrDefault(item => item.FotografiaId == id);

                if (foto == null)
                    continue;

                if (acciones.EsAnalizador && contextoRevision != null)
                {
                    ActualizarAccionesAnalizador(acciones, foto);
                }
                else if (!acciones.EsAnalizador &&
                         devolucionesPorFoto.TryGetValue(
                             id,
                             out DevolucionTecnicoFotografiaV2? devolucion))
                {
                    ActualizarAccionesTecnico(acciones, foto, devolucion);
                    if (acciones.Atender != null)
                        acciones.Atender.IsEnabled = !operacionRevisionActiva;
                }
            }
        }

        private async Task EsperarActualizacionViewModelAsync()
        {
            for (int intento = 0; intento < 40; intento++)
            {
                await Task.Delay(100);
                if (!viewModel.IsBusy)
                    return;
            }
        }

        private sealed class AccionesTarjetaRevision
        {
            public AccionesTarjetaRevision(
                Border tarjeta,
                Border panel,
                bool esAnalizador,
                Label estado,
                Label bloqueo,
                Button? clasificar,
                Button? devolver,
                Button? finalizar,
                Button? atender)
            {
                Tarjeta = tarjeta;
                Panel = panel;
                EsAnalizador = esAnalizador;
                Estado = estado;
                Bloqueo = bloqueo;
                Clasificar = clasificar;
                Devolver = devolver;
                Finalizar = finalizar;
                Atender = atender;
            }

            public Border Tarjeta { get; }
            public Border Panel { get; }
            public bool EsAnalizador { get; }
            public Label Estado { get; }
            public Label Bloqueo { get; }
            public Button? Clasificar { get; }
            public Button? Devolver { get; }
            public Button? Finalizar { get; }
            public Button? Atender { get; }
        }
    }
}
