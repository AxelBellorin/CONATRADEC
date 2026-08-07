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
        private readonly InspeccionAlbumAprobadorApiService albumAprobadorApi = new();
        private readonly Dictionary<int, DevolucionTecnicoFotografiaV2>
            devolucionesPorFoto = [];
        private readonly Dictionary<int, AccionesTarjetaRevision>
            accionesPorFoto = [];
        private readonly Dictionary<int, AccionesTarjetaAprobador>
            accionesAprobadorPorFoto = [];
        private readonly Dictionary<int, EstadoAlbumAprobador>
            estadosAlbumAprobadorPorFoto = [];

        private Border? panelGlobalAprobador;
        private Label? estadoGlobalAprobadorLabel;
        private Label? ayudaGlobalAprobadorLabel;
        private Button? aprobarSeleccionAprobadorButton;
        private Button? devolverSeleccionAprobadorButton;
        private Button? rechazarSeleccionAprobadorButton;

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

                if (EsVistaAprobadorRevision)
                    await CargarEstadosAlbumAprobadorAsync();

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
            accionesAprobadorPorFoto.Clear();
            estadosAlbumAprobadorPorFoto.Clear();
            panelGlobalAprobador = null;
            estadoGlobalAprobadorLabel = null;
            ayudaGlobalAprobadorLabel = null;
            aprobarSeleccionAprobadorButton = null;
            devolverSeleccionAprobadorButton = null;
            rechazarSeleccionAprobadorButton = null;
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

        /// <summary>
        /// Consulta el estado real de la publicación de cada fotografía
        /// aprobada. El backend reconcilia también desactivaciones realizadas
        /// directamente desde la administración del Álbum Botánico.
        /// </summary>
        private async Task CargarEstadosAlbumAprobadorAsync()
        {
            estadosAlbumAprobadorPorFoto.Clear();

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                bool requiereEstado = foto.TieneAprobacion ||
                    foto.Estado is
                        InspeccionFotoEstados.Aprobada or
                        InspeccionFotoEstados.AprobadaConCorreccion or
                        InspeccionFotoEstados.PublicadaAlbum ||
                    foto.PublicadaAlbum;

                if (!requiereEstado)
                    continue;

                try
                {
                    EstadoAlbumAprobador estado =
                        await albumAprobadorApi.ObtenerEstadoAsync(
                            idRevision,
                            foto.FotografiaId);
                    estadosAlbumAprobadorPorFoto[foto.FotografiaId] = estado;
                }
                catch
                {
                    /*
                     * Si el endpoint de estado no responde, la pantalla conserva
                     * el último estado incluido en el detalle de la inspección.
                     */
                    estadosAlbumAprobadorPorFoto[foto.FotografiaId] =
                        new EstadoAlbumAprobador
                        {
                            FotografiaId = foto.FotografiaId,
                            Aprobada = foto.Estado is
                                InspeccionFotoEstados.Aprobada or
                                InspeccionFotoEstados.AprobadaConCorreccion or
                                InspeccionFotoEstados.PublicadaAlbum,
                            Autorizada =
                                foto.UltimaAprobacion?.AutorizaPublicacionAlbum == true,
                            PublicadaActiva = foto.PublicadaAlbum,
                            TuvoPublicacion = foto.PublicadaAlbum,
                            EstadoEvidencia = foto.Estado
                        };
                }
            }
        }

        private EstadoAlbumAprobador ObtenerEstadoAlbumAprobador(
            InspeccionFotoV2 foto)
        {
            if (estadosAlbumAprobadorPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out EstadoAlbumAprobador? estado))
            {
                return estado;
            }

            return new EstadoAlbumAprobador
            {
                FotografiaId = foto.FotografiaId,
                Aprobada = foto.Estado is
                    InspeccionFotoEstados.Aprobada or
                    InspeccionFotoEstados.AprobadaConCorreccion or
                    InspeccionFotoEstados.PublicadaAlbum,
                Autorizada =
                    foto.UltimaAprobacion?.AutorizaPublicacionAlbum == true,
                PublicadaActiva = foto.PublicadaAlbum,
                TuvoPublicacion = foto.PublicadaAlbum,
                EstadoEvidencia = foto.Estado
            };
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
                         !EsEstadoVisibleAnalizador(foto.Estado)) ||
                        (EsVistaAprobadorRevision &&
                         !EsEstadoVisibleAprobador(foto.Estado)))
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
                InspeccionFotoEstados.DevueltaAnalizador or
                InspeccionFotoEstados.PendienteAprobacion;

        private static bool EsEstadoPendienteRevisionAnalizador(string? estado) =>
            estado is
                InspeccionFotoEstados.PendienteAnalizador or
                InspeccionFotoEstados.DevueltaAnalizador;

        private static bool EsEstadoRevisadoSinEnviar(string? estado) =>
            string.Equals(
                estado,
                InspeccionFotoEstados.EnAnalisisHumano,
                StringComparison.OrdinalIgnoreCase);

        private static bool EsEstadoEnviadoAprobador(string? estado) =>
            string.Equals(
                estado,
                InspeccionFotoEstados.PendienteAprobacion,
                StringComparison.OrdinalIgnoreCase);

        private static bool EsEstadoPendienteAprobador(string? estado) =>
            string.Equals(
                estado,
                InspeccionFotoEstados.PendienteAprobacion,
                StringComparison.OrdinalIgnoreCase);

        private static bool EsEstadoVisibleAprobador(string? estado) =>
            estado is
                InspeccionFotoEstados.PendienteAprobacion or
                InspeccionFotoEstados.Aprobada or
                InspeccionFotoEstados.AprobadaConCorreccion or
                InspeccionFotoEstados.DevueltaAnalizador or
                InspeccionFotoEstados.Rechazada or
                InspeccionFotoEstados.NoConcluyente or
                InspeccionFotoEstados.PublicadaAlbum;

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
                    /*
                     * La selección múltiple vuelve a estar visible en la vista
                     * del analizador. El panel superior funciona como centro de
                     * revisión guiada y las decisiones de cada fotografía se
                     * mantienen separadas dentro de su tarjeta.
                     */
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

                    ActualizarPanelGlobalAnalizador();
                }

                if (EsVistaAprobadorRevision)
                {
                    foreach (Button boton in elementos.OfType<Button>())
                    {
                        if (string.Equals(
                                boton.Text,
                                "Registrar decisión",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            boton.IsVisible = false;
                        }

                        if (string.Equals(
                                boton.Text,
                                "Seleccionar todas",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            boton.IsVisible = true;
                        }
                    }

                    AsegurarPanelGlobalAprobador(elementos);
                    ActualizarPanelGlobalAprobador();
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

            foreach (int id in accionesAprobadorPorFoto
                         .Where(item => !visibles.Contains(item.Value.Panel))
                         .Select(item => item.Key)
                         .ToList())
            {
                accionesAprobadorPorFoto.Remove(id);
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

                if (EsVistaAprobadorRevision &&
                    EsEstadoVisibleAprobador(foto.Estado))
                {
                    AccionesTarjetaAprobador? acciones =
                        ObtenerOCrearAccionesAprobador(foto, elementos);
                    if (acciones != null)
                        ActualizarAccionesAprobador(acciones, foto);
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

            Button confirmar = CrearBotonTarjeta(
                "✓ Confirmar diagnóstico IA",
                "#3B655B",
                Colors.White);
            confirmar.CommandParameter = foto;
            confirmar.Clicked += OnConfirmarTarjetaClicked;

            Button corregir = CrearBotonTarjeta(
                "✎ Corregir diagnóstico",
                "#9B552C",
                Colors.White);
            corregir.CommandParameter = foto;
            corregir.Clicked += OnCorregirTarjetaClicked;

            Button devolver = CrearBotonTarjeta(
                "↩ Devolver al técnico",
                "#FFF4EA",
                Color.FromArgb("#9B552C"));
            devolver.CommandParameter = foto;
            devolver.Clicked += OnDevolverTarjetaClicked;

            Button enviarAprobador = CrearBotonTarjeta(
                "Enviar al aprobador",
                "#263A35",
                Colors.White);
            enviarAprobador.CommandParameter = foto;
            enviarAprobador.Clicked += OnEnviarAprobadorTarjetaClicked;

            Microsoft.Maui.Controls.Layout botones = CrearContenedorBotones(
                confirmar,
                corregir,
                devolver,
                enviarAprobador);

            var contenido = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    new Label
                    {
                        Text = "Acciones de esta fotografía",
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
                confirmar,
                corregir,
                devolver,
                enviarAprobador,
                null);

            accionesPorFoto[foto.FotografiaId] = acciones;
            return acciones;
        }

        private AccionesTarjetaAprobador? ObtenerOCrearAccionesAprobador(
            InspeccionFotoV2 foto,
            IReadOnlyList<IVisualTreeElement> elementos)
        {
            if (accionesAprobadorPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out AccionesTarjetaAprobador? existente) &&
                elementos.Contains(existente.Panel))
            {
                return existente;
            }

            Border? tarjeta = EncontrarTarjetaFotografia(foto, elementos);
            if (tarjeta == null)
                return null;

            Label estado = CrearEtiquetaAyuda();
            Label ayuda = CrearEtiquetaBloqueo();

            Button aprobar = CrearBotonTarjeta(
                "✓ Aprobar",
                "#3B655B",
                Colors.White);
            aprobar.CommandParameter = foto;
            aprobar.Clicked += OnAprobarTarjetaAprobadorClicked;

            Button corregir = CrearBotonTarjeta(
                "✎ Aprobar con corrección",
                "#9B552C",
                Colors.White);
            corregir.CommandParameter = foto;
            corregir.Clicked += OnAprobarCorreccionTarjetaAprobadorClicked;

            Button devolver = CrearBotonTarjeta(
                "↩ Devolver al analizador",
                "#FFF4EA",
                Color.FromArgb("#9B552C"));
            devolver.CommandParameter = foto;
            devolver.Clicked += OnDevolverAnalizadorTarjetaAprobadorClicked;

            Button rechazar = CrearBotonTarjeta(
                "✕ Rechazar",
                "#FDECEC",
                Color.FromArgb("#B42318"));
            rechazar.CommandParameter = foto;
            rechazar.Clicked += OnRechazarTarjetaAprobadorClicked;

            Button noConcluyente = CrearBotonTarjeta(
                "? No concluyente",
                "#EEF2F0",
                Color.FromArgb("#52625D"));
            noConcluyente.CommandParameter = foto;
            noConcluyente.Clicked += OnNoConcluyenteTarjetaAprobadorClicked;

            Button autorizarAlbum = CrearBotonTarjeta(
                "Autorizar para Álbum",
                "#E3EFEA",
                Color.FromArgb("#315E52"));
            autorizarAlbum.CommandParameter = foto;
            autorizarAlbum.Clicked +=
                OnAutorizarAlbumTarjetaAprobadorClicked;

            Button publicar = CrearBotonTarjeta(
                "Publicar en Álbum Botánico",
                "#F2C94C",
                Color.FromArgb("#263A35"));
            publicar.CommandParameter = foto;
            publicar.Clicked += OnPublicarAlbumTarjetaAprobadorClicked;

            Button cancelarAlbum = CrearBotonTarjeta(
                "Cancelar autorización",
                "#FDECEC",
                Color.FromArgb("#B42318"));
            cancelarAlbum.CommandParameter = foto;
            cancelarAlbum.Clicked +=
                OnCancelarAutorizacionAlbumTarjetaAprobadorClicked;

            Microsoft.Maui.Controls.Layout botones = CrearContenedorBotones(
                aprobar,
                corregir,
                devolver,
                rechazar,
                noConcluyente,
                autorizarAlbum,
                publicar,
                cancelarAlbum);

            var contenido = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    new Label
                    {
                        Text = "Decisión del aprobador",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#263A35")
                    },
                    estado,
                    botones,
                    ayuda
                }
            };

            Border panel = CrearPanelTarjeta(contenido, "#F8FBFA", "#C8DED6");
            IntegrarPanelEnTarjeta(tarjeta, panel);

            var acciones = new AccionesTarjetaAprobador(
                tarjeta,
                panel,
                estado,
                ayuda,
                aprobar,
                corregir,
                devolver,
                rechazar,
                noConcluyente,
                autorizarAlbum,
                publicar,
                cancelarAlbum);

            accionesAprobadorPorFoto[foto.FotografiaId] = acciones;
            return acciones;
        }

        private void ActualizarAccionesAprobador(
            AccionesTarjetaAprobador acciones,
            InspeccionFotoV2 foto)
        {
            acciones.Panel.IsVisible = true;

            bool pendiente = EsEstadoPendienteAprobador(foto.Estado);
            bool aprobada = foto.Estado is
                InspeccionFotoEstados.Aprobada or
                InspeccionFotoEstados.AprobadaConCorreccion or
                InspeccionFotoEstados.PublicadaAlbum;
            bool devuelta = string.Equals(
                foto.Estado,
                InspeccionFotoEstados.DevueltaAnalizador,
                StringComparison.OrdinalIgnoreCase);
            bool rechazada = foto.Estado is
                InspeccionFotoEstados.Rechazada or
                InspeccionFotoEstados.NoConcluyente;

            EstadoAlbumAprobador estadoAlbum =
                ObtenerEstadoAlbumAprobador(foto);
            bool autorizada = aprobada && estadoAlbum.Autorizada;
            bool publicada = aprobada && estadoAlbum.PublicadaActiva;

            if (pendiente)
            {
                acciones.Estado.Text =
                    "Revise la clasificación humana y registre la decisión técnica de esta fotografía.";
                acciones.Ayuda.Text =
                    "La decisión sobre el Álbum Botánico se toma después de aprobar. Aprobar una evidencia no la publica ni la autoriza automáticamente.";
            }
            else if (publicada)
            {
                acciones.Estado.Text =
                    "✓ Fotografía aprobada y publicada activamente en el Álbum Botánico.";
                acciones.Ayuda.Text =
                    "Puede retirar la copia del álbum y cancelar su autorización sin alterar la evidencia original ni la aprobación técnica.";
            }
            else if (aprobada && autorizada)
            {
                acciones.Estado.Text = estadoAlbum.TuvoPublicacion
                    ? "✓ Fotografía aprobada y autorizada. La copia anterior del Álbum Botánico ya no está activa."
                    : "✓ Fotografía aprobada y autorizada para el Álbum Botánico.";
                acciones.Ayuda.Text = estadoAlbum.TuvoPublicacion
                    ? "La administración del álbum pudo haber desactivado la copia anterior. Puede publicarla nuevamente o cancelar la autorización."
                    : "Puede publicarla cuando lo considere conveniente o cancelar la autorización antes de hacerlo.";
            }
            else if (aprobada)
            {
                acciones.Estado.Text =
                    "✓ Fotografía aprobada. Aún no está autorizada para el Álbum Botánico.";
                acciones.Ayuda.Text =
                    "La aprobación técnica ya terminó. Si más adelante decide incorporarla al álbum, autorícela desde este mismo expediente.";
            }
            else if (devuelta)
            {
                acciones.Estado.Text =
                    "↩ Fotografía devuelta al analizador para corrección.";
                acciones.Ayuda.Text =
                    "Volverá a estar pendiente de aprobación cuando el analizador la envíe nuevamente.";
            }
            else if (rechazada)
            {
                acciones.Estado.Text = string.Equals(
                        foto.Estado,
                        InspeccionFotoEstados.NoConcluyente,
                        StringComparison.OrdinalIgnoreCase)
                    ? "? La fotografía fue cerrada como no concluyente."
                    : "✕ La fotografía fue rechazada.";
                acciones.Ayuda.Text =
                    "La decisión queda registrada en la trazabilidad del expediente.";
            }

            bool habilitada =
                !operacionRevisionActiva &&
                pendiente &&
                foto.TieneAnalisisHumano;

            foreach (CheckBox selector in acciones.Tarjeta
                         .GetVisualTreeDescendants()
                         .OfType<CheckBox>())
            {
                selector.IsEnabled = habilitada;
                if (!pendiente)
                    selector.IsChecked = false;
            }

            acciones.Aprobar.IsVisible = pendiente;
            acciones.Corregir.IsVisible = pendiente;
            acciones.Devolver.IsVisible = pendiente;
            acciones.Rechazar.IsVisible = pendiente;
            acciones.NoConcluyente.IsVisible = pendiente;

            acciones.Aprobar.IsEnabled = habilitada;
            acciones.Corregir.IsEnabled = habilitada;
            acciones.Devolver.IsEnabled = habilitada;
            acciones.Rechazar.IsEnabled = habilitada;
            acciones.NoConcluyente.IsEnabled = habilitada;

            acciones.AutorizarAlbum.IsVisible =
                aprobada && !autorizada && !publicada;
            acciones.AutorizarAlbum.IsEnabled =
                acciones.AutorizarAlbum.IsVisible && !operacionRevisionActiva;

            acciones.Publicar.IsVisible =
                aprobada && autorizada && !publicada;
            acciones.Publicar.Text = estadoAlbum.TuvoPublicacion
                ? "Publicar nuevamente en Álbum Botánico"
                : "Publicar en Álbum Botánico";
            acciones.Publicar.IsEnabled =
                acciones.Publicar.IsVisible && !operacionRevisionActiva;

            acciones.CancelarAlbum.IsVisible =
                aprobada && autorizada;
            acciones.CancelarAlbum.Text = publicada
                ? "Retirar del Álbum y cancelar autorización"
                : "Cancelar autorización";
            acciones.CancelarAlbum.IsEnabled =
                acciones.CancelarAlbum.IsVisible && !operacionRevisionActiva;
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

            bool pendienteRevision =
                EsEstadoPendienteRevisionAnalizador(foto.Estado);
            bool revisadaSinEnviar =
                EsEstadoRevisadoSinEnviar(foto.Estado) &&
                foto.TieneAnalisisHumano;
            bool enviadaAprobador =
                EsEstadoEnviadoAprobador(foto.Estado);

            foreach (CheckBox selector in acciones.Tarjeta
                         .GetVisualTreeDescendants()
                         .OfType<CheckBox>())
            {
                selector.IsEnabled = !enviadaAprobador;
                if (enviadaAprobador)
                    selector.IsChecked = false;
            }

            if (enviadaAprobador)
            {
                acciones.Estado.Text =
                    "✓ Esta fotografía fue enviada al aprobador y quedó bloqueada para el analizador.";
                acciones.Bloqueo.Text =
                    "El aprobador ya puede revisarla. Si la devuelve, se habilitará nuevamente solo esta fotografía.";
            }
            else if (revisadaSinEnviar)
            {
                acciones.Estado.Text =
                    "✓ Revisión humana completada. La fotografía está lista para enviarse al aprobador.";
                acciones.Bloqueo.Text = resumen.EtapaTecnicaFinalizada
                    ? "Puede enviarla individualmente o seleccionarla junto con otras fotografías revisadas."
                    : "El envío al aprobador se habilitará cuando el técnico finalice su etapa.";
            }
            else
            {
                acciones.Estado.Text =
                    "Revise el resultado de la IA y decida si lo confirma, lo corrige o devuelve la evidencia al técnico.";
                acciones.Bloqueo.Text =
                    "Las acciones afectan únicamente esta fotografía.";
            }

            bool puedeRevisar =
                !operacionRevisionActiva &&
                pendienteRevision;

            if (acciones.Confirmar != null)
            {
                acciones.Confirmar.IsVisible = pendienteRevision;
                acciones.Confirmar.IsEnabled = puedeRevisar;
            }

            if (acciones.Corregir != null)
            {
                acciones.Corregir.IsVisible = pendienteRevision;
                acciones.Corregir.IsEnabled = puedeRevisar;
            }

            if (acciones.Devolver != null)
            {
                acciones.Devolver.IsVisible = pendienteRevision;
                acciones.Devolver.IsEnabled = puedeRevisar;
            }

            if (acciones.EnviarAprobador != null)
            {
                acciones.EnviarAprobador.IsVisible = revisadaSinEnviar;
                acciones.EnviarAprobador.IsEnabled =
                    !operacionRevisionActiva &&
                    resumen.EtapaTecnicaFinalizada &&
                    !resumen.EtapaAnalizadorFinalizada;
            }

            ActualizarPanelGlobalAnalizador();
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

        private void OnSeleccionarTodoAnalizadorClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva)
                return;

            if (EsVistaAprobadorRevision)
            {
                foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
                {
                    foto.Seleccionada =
                        foto.PuedeSeleccionarse &&
                        EsEstadoPendienteAprobador(foto.Estado);
                }

                ActualizarPanelGlobalAprobador();
                return;
            }

            if (!EsVistaAnalizadorRevision)
                return;

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                foto.Seleccionada =
                    foto.PuedeSeleccionarse &&
                    (EsEstadoPendienteRevisionAnalizador(foto.Estado) ||
                     EsEstadoRevisadoSinEnviar(foto.Estado));
            }

            ActualizarPanelGlobalAnalizador();
        }

        private void AsegurarPanelGlobalAprobador(
            IReadOnlyList<IVisualTreeElement> elementos)
        {
            if (!EsVistaAprobadorRevision)
                return;

            if (panelGlobalAprobador?.Parent != null)
                return;

            Label? titulo = elementos
                .OfType<Label>()
                .FirstOrDefault(item => string.Equals(
                    item.Text,
                    "Acciones por fotografía",
                    StringComparison.OrdinalIgnoreCase));

            Border? contenedorPrincipal = BuscarAncestro<Border>(titulo);
            if (contenedorPrincipal?.Content is not VerticalStackLayout stack)
                return;

            estadoGlobalAprobadorLabel = CrearEtiquetaAyuda();
            estadoGlobalAprobadorLabel.FontSize = 13;
            estadoGlobalAprobadorLabel.FontAttributes = FontAttributes.Bold;
            estadoGlobalAprobadorLabel.TextColor = Color.FromArgb("#315E52");

            ayudaGlobalAprobadorLabel = CrearEtiquetaBloqueo();

            aprobarSeleccionAprobadorButton = CrearBotonTarjeta(
                "✓ Aprobar seleccionadas",
                "#3B655B",
                Colors.White);
            aprobarSeleccionAprobadorButton.Clicked +=
                OnAprobarSeleccionAprobadorClicked;

            devolverSeleccionAprobadorButton = CrearBotonTarjeta(
                "↩ Devolver seleccionadas",
                "#FFF4EA",
                Color.FromArgb("#9B552C"));
            devolverSeleccionAprobadorButton.Clicked +=
                OnDevolverSeleccionAprobadorClicked;

            rechazarSeleccionAprobadorButton = CrearBotonTarjeta(
                "✕ Rechazar seleccionadas",
                "#FDECEC",
                Color.FromArgb("#B42318"));
            rechazarSeleccionAprobadorButton.Clicked +=
                OnRechazarSeleccionAprobadorClicked;

            Microsoft.Maui.Controls.Layout botones = CrearContenedorBotones(
                aprobarSeleccionAprobadorButton,
                devolverSeleccionAprobadorButton,
                rechazarSeleccionAprobadorButton);

            panelGlobalAprobador = CrearPanelTarjeta(
                new VerticalStackLayout
                {
                    Spacing = 7,
                    Children =
                    {
                        new Label
                        {
                            Text = "Decisión para fotografías seleccionadas",
                            FontSize = 16,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#263A35")
                        },
                        estadoGlobalAprobadorLabel,
                        botones,
                        ayudaGlobalAprobadorLabel
                    }
                },
                "#F8FBFA",
                "#C8DED6");

            int indice = Math.Min(2, stack.Children.Count);
            stack.Children.Insert(indice, panelGlobalAprobador);
        }

        private void ActualizarPanelGlobalAprobador()
        {
            if (!EsVistaAprobadorRevision || panelGlobalAprobador == null)
                return;

            int pendientes = viewModel.Fotografias.Count(item =>
                EsEstadoPendienteAprobador(item.Estado));
            int aprobadas = viewModel.Fotografias.Count(item =>
                item.Estado is
                    InspeccionFotoEstados.Aprobada or
                    InspeccionFotoEstados.AprobadaConCorreccion or
                    InspeccionFotoEstados.PublicadaAlbum);
            int devueltas = viewModel.Fotografias.Count(item =>
                string.Equals(
                    item.Estado,
                    InspeccionFotoEstados.DevueltaAnalizador,
                    StringComparison.OrdinalIgnoreCase));
            int rechazadas = viewModel.Fotografias.Count(item =>
                item.Estado is
                    InspeccionFotoEstados.Rechazada or
                    InspeccionFotoEstados.NoConcluyente);
            int seleccionadas = viewModel.Fotografias.Count(item =>
                item.Seleccionada && EsEstadoPendienteAprobador(item.Estado));
            int autorizadasAlbum = estadosAlbumAprobadorPorFoto.Values.Count(
                item => item.Autorizada);
            int publicadasAlbum = estadosAlbumAprobadorPorFoto.Values.Count(
                item => item.PublicadaActiva);

            if (estadoGlobalAprobadorLabel != null)
            {
                estadoGlobalAprobadorLabel.Text =
                    $"Pendientes: {pendientes} · Aprobadas: {aprobadas} · Devueltas: {devueltas} · Rechazadas/no concluyentes: {rechazadas} · Álbum autorizadas: {autorizadasAlbum} · Publicadas: {publicadasAlbum} · Seleccionadas: {seleccionadas}.";
            }

            if (ayudaGlobalAprobadorLabel != null)
            {
                ayudaGlobalAprobadorLabel.Text = seleccionadas == 0
                    ? "Seleccione una o varias fotografías pendientes. Aprobar no autoriza ni publica en el álbum: esa decisión se administra después, por fotografía."
                    : "Las acciones masivas resuelven únicamente la decisión técnica. Aprobar con corrección y la gestión del Álbum Botánico se mantienen como acciones individuales.";
            }

            bool habilitar = !operacionRevisionActiva && seleccionadas > 0;
            if (aprobarSeleccionAprobadorButton != null)
            {
                aprobarSeleccionAprobadorButton.Text = seleccionadas > 0
                    ? $"✓ Aprobar seleccionadas ({seleccionadas})"
                    : "✓ Aprobar seleccionadas";
                aprobarSeleccionAprobadorButton.IsEnabled = habilitar;
            }

            if (devolverSeleccionAprobadorButton != null)
                devolverSeleccionAprobadorButton.IsEnabled = habilitar;
            if (rechazarSeleccionAprobadorButton != null)
                rechazarSeleccionAprobadorButton.IsEnabled = habilitar;
        }

        private async void OnAprobarTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is InspeccionFotoV2 foto)
            {
                await EjecutarDecisionAprobadorIndividualAsync(foto, "APROBAR");
            }
        }

        private async void OnAprobarCorreccionTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is InspeccionFotoV2 foto)
            {
                await EjecutarDecisionAprobadorIndividualAsync(
                    foto,
                    "APROBAR_CON_CORRECCION");
            }
        }

        private async void OnDevolverAnalizadorTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is InspeccionFotoV2 foto)
            {
                await EjecutarDecisionAprobadorIndividualAsync(
                    foto,
                    "DEVOLVER_AL_ANALIZADOR");
            }
        }

        private async void OnRechazarTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is InspeccionFotoV2 foto)
            {
                await EjecutarDecisionAprobadorIndividualAsync(foto, "RECHAZAR");
            }
        }

        private async void OnNoConcluyenteTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is InspeccionFotoV2 foto)
            {
                await EjecutarDecisionAprobadorIndividualAsync(
                    foto,
                    "NO_CONCLUYENTE");
            }
        }

        private async void OnAutorizarAlbumTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto)
            {
                return;
            }

            EstadoAlbumAprobador estado = ObtenerEstadoAlbumAprobador(foto);
            if (!estado.Aprobada || estado.Autorizada)
                return;

            bool confirmar = await DisplayAlert(
                "Autorizar para Álbum Botánico",
                "La aprobación técnica no cambiará. Esta autorización permitirá publicar la fotografía ahora o más adelante. ¿Desea autorizarla?",
                "Autorizar",
                "Cancelar");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();
            try
            {
                EstadoAlbumAprobador actualizado =
                    await albumAprobadorApi.CambiarAutorizacionAsync(
                        idRevision,
                        foto.FotografiaId,
                        autorizar: true);
                estadosAlbumAprobadorPorFoto[foto.FotografiaId] = actualizado;
                await RecargarDespuesOperacionRevisionAsync();

                await DisplayAlert(
                    "Autorización registrada",
                    "La fotografía quedó autorizada para el Álbum Botánico. Puede publicarla ahora o hacerlo posteriormente.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Autorizar para Álbum Botánico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnCancelarAutorizacionAlbumTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto)
            {
                return;
            }

            EstadoAlbumAprobador estado = ObtenerEstadoAlbumAprobador(foto);
            if (!estado.Autorizada)
                return;

            string mensaje = estado.PublicadaActiva
                ? "La copia activa será retirada lógicamente del Álbum Botánico y la autorización quedará cancelada. La aprobación técnica y la evidencia original no se modificarán."
                : "La autorización para incorporar esta fotografía al Álbum Botánico quedará cancelada. La aprobación técnica no se modificará.";

            bool confirmar = await DisplayAlert(
                estado.PublicadaActiva
                    ? "Retirar del Álbum Botánico"
                    : "Cancelar autorización",
                mensaje,
                estado.PublicadaActiva ? "Retirar" : "Cancelar autorización",
                "Volver");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();
            try
            {
                EstadoAlbumAprobador actualizado =
                    await albumAprobadorApi.CambiarAutorizacionAsync(
                        idRevision,
                        foto.FotografiaId,
                        autorizar: false);
                estadosAlbumAprobadorPorFoto[foto.FotografiaId] = actualizado;
                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Álbum Botánico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnPublicarAlbumTarjetaAprobadorClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto)
            {
                return;
            }

            EstadoAlbumAprobador estado = ObtenerEstadoAlbumAprobador(foto);
            if (!estado.Aprobada || !estado.Autorizada || estado.PublicadaActiva)
            {
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();
            try
            {
                await PublicarFotografiaAprobadaAsync(foto);
                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Publicar en Álbum Botánico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnAprobarSeleccionAprobadorClicked(
            object? sender,
            EventArgs e) =>
            await EjecutarDecisionMasivaAprobadorAsync("APROBAR");

        private async void OnDevolverSeleccionAprobadorClicked(
            object? sender,
            EventArgs e) =>
            await EjecutarDecisionMasivaAprobadorAsync(
                "DEVOLVER_AL_ANALIZADOR");

        private async void OnRechazarSeleccionAprobadorClicked(
            object? sender,
            EventArgs e) =>
            await EjecutarDecisionMasivaAprobadorAsync("RECHAZAR");

        private async Task EjecutarDecisionAprobadorIndividualAsync(
            InspeccionFotoV2 foto,
            string decision)
        {
            if (operacionRevisionActiva ||
                !EsVistaAprobadorRevision ||
                !EsEstadoPendienteAprobador(foto.Estado) ||
                foto.UltimoAnalisisHumano == null)
            {
                return;
            }

            bool positiva = decision is "APROBAR" or "APROBAR_CON_CORRECCION";

            InspeccionFotoAnalisisHumanoV2 humano = foto.UltimoAnalisisHumano;
            string diagnosticoFinal = humano.Diagnostico;

            if (decision == "APROBAR_CON_CORRECCION")
            {
                string? correccion = await DisplayPromptAsync(
                    "Diagnóstico final corregido",
                    "Indique el diagnóstico final que quedará en el expediente.",
                    "Continuar",
                    "Cancelar",
                    humano.Diagnostico,
                    300,
                    Keyboard.Text);

                if (string.IsNullOrWhiteSpace(correccion))
                    return;

                diagnosticoFinal = correccion.Trim();
            }

            bool requiereMotivo = decision is
                "DEVOLVER_AL_ANALIZADOR" or "RECHAZAR" or "NO_CONCLUYENTE";

            string? observaciones = await DisplayPromptAsync(
                "Observaciones del aprobador",
                requiereMotivo
                    ? "Explique el motivo de la decisión."
                    : "Observación opcional para la aprobación.",
                "Continuar",
                "Cancelar",
                string.Empty,
                2000,
                Keyboard.Text);

            if (observaciones == null)
                return;

            if (requiereMotivo && observaciones.Trim().Length < 8)
            {
                await DisplayAlert(
                    "Motivo requerido",
                    "Indique un motivo de al menos 8 caracteres.",
                    "Aceptar");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar decisión",
                $"Se registrará la decisión {decision.Replace('_', ' ')} para {foto.Titulo}. ¿Desea continuar?",
                "Confirmar",
                "Cancelar");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                await InspeccionFitosanitariaApiService.Instance
                    .RegistrarAprobacionesAsync(
                        idRevision,
                        [CrearSolicitudAprobacion(
                            foto,
                            humano,
                            decision,
                            diagnosticoFinal,
                            observaciones.Trim(),
                            autorizaAlbum: false)]);

                await RecargarDespuesOperacionRevisionAsync();

                if (positiva)
                {
                    await DisplayAlert(
                        "Fotografía aprobada",
                        "La decisión técnica quedó registrada. El Álbum Botánico se administra por separado: puede autorizar esta fotografía ahora o en cualquier momento posterior.",
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Decisión del aprobador",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async Task EjecutarDecisionMasivaAprobadorAsync(
            string decision)
        {
            if (operacionRevisionActiva || !EsVistaAprobadorRevision)
                return;

            List<InspeccionFotoV2> seleccionadas = viewModel.Fotografias
                .Where(item =>
                    item.Seleccionada &&
                    EsEstadoPendienteAprobador(item.Estado))
                .OrderBy(item => item.Orden)
                .ToList();

            if (seleccionadas.Count == 0)
            {
                await DisplayAlert(
                    "Seleccione fotografías",
                    "Seleccione una o varias fotografías pendientes de aprobación.",
                    "Aceptar");
                return;
            }

            if (seleccionadas.Any(item => item.UltimoAnalisisHumano == null))
            {
                await DisplayAlert(
                    "Clasificación humana requerida",
                    "Todas las fotografías seleccionadas deben tener una clasificación humana enviada.",
                    "Aceptar");
                return;
            }

            bool requiereMotivo = decision is
                "DEVOLVER_AL_ANALIZADOR" or "RECHAZAR";

            string? observaciones = await DisplayPromptAsync(
                "Observación para las seleccionadas",
                requiereMotivo
                    ? "Indique el motivo que se registrará en todas las fotografías seleccionadas."
                    : "Observación opcional para la aprobación masiva.",
                "Continuar",
                "Cancelar",
                string.Empty,
                2000,
                Keyboard.Text);

            if (observaciones == null)
                return;

            if (requiereMotivo && observaciones.Trim().Length < 8)
            {
                await DisplayAlert(
                    "Motivo requerido",
                    "Indique un motivo de al menos 8 caracteres.",
                    "Aceptar");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar decisión masiva",
                $"Se aplicará {decision.Replace('_', ' ')} a {seleccionadas.Count} fotografía(s). ¿Desea continuar?",
                "Aplicar",
                "Cancelar");

            if (!confirmar)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            int exitosas = 0;
            var errores = new List<string>();

            try
            {
                foreach (InspeccionFotoV2 foto in seleccionadas)
                {
                    try
                    {
                        InspeccionFotoAnalisisHumanoV2 humano =
                            foto.UltimoAnalisisHumano!;

                        await InspeccionFitosanitariaApiService.Instance
                            .RegistrarAprobacionesAsync(
                                idRevision,
                                [CrearSolicitudAprobacion(
                                    foto,
                                    humano,
                                    decision,
                                    humano.Diagnostico,
                                    observaciones.Trim(),
                                    autorizaAlbum: false)]);
                        exitosas++;
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"{foto.Titulo}: {ex.Message}");
                    }
                }

                await RecargarDespuesOperacionRevisionAsync();

                string resumen =
                    $"Se procesaron correctamente {exitosas} de {seleccionadas.Count} fotografía(s).";
                if (decision == "APROBAR" && exitosas > 0)
                {
                    resumen +=
                        " Las fotografías quedaron aprobadas técnicamente. La autorización y publicación en el Álbum Botánico se deciden posteriormente por fotografía.";
                }

                if (errores.Count > 0)
                    resumen += $" Hubo {errores.Count} error(es).";

                await DisplayAlert(
                    "Decisión masiva",
                    resumen,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private static InspeccionFotoAprobacionRequestV2
            CrearSolicitudAprobacion(
                InspeccionFotoV2 foto,
                InspeccionFotoAnalisisHumanoV2 humano,
                string decision,
                string diagnosticoFinal,
                string observaciones,
                bool autorizaAlbum) =>
            new()
            {
                FotografiaId = foto.FotografiaId,
                Decision = decision,
                CalidadEvaluacionFinal = humano.CalidadEvaluacion,
                EstadoGeneralFinal = humano.EstadoGeneral,
                CategoriaPrincipalFinal = humano.CategoriaPrincipal,
                CategoriasSecundariasFinales = humano.CategoriasSecundarias,
                DiagnosticoFinal = diagnosticoFinal,
                TipoDiagnosticoFinal = humano.TipoDiagnostico,
                SeveridadFinal = humano.Severidad,
                NivelCertezaFinal = humano.NivelCerteza,
                Observaciones = observaciones,
                AutorizaPublicacionAlbum = autorizaAlbum
            };

        private async Task PublicarFotografiaAprobadaAsync(
            InspeccionFotoV2 foto)
        {
            JerarquiaDiagnosticoFotoResponse? jerarquia =
                foto.JerarquiaAlbum ??
                await ObtenerJerarquiaActualizadaAsync(foto.FotografiaId);

            if (jerarquia?.CategoriaAlbumBotanicoId is not > 0 ||
                jerarquia.AlbumBotanicoCafeId is not > 0 ||
                jerarquia.CategoriaEsPropuesta ||
                jerarquia.FichaEsPropuesta)
            {
                var paginaJerarquia = new JerarquiaAlbumFotografiaPage(
                    idRevision,
                    foto,
                    "APROBADOR");

                await Navigation.PushModalAsync(paginaJerarquia);
                bool guardada = await paginaJerarquia.ResultadoTask;
                if (!guardada)
                    return;

                jerarquia = await ObtenerJerarquiaActualizadaAsync(
                    foto.FotografiaId);
                foto.JerarquiaAlbum = jerarquia;

                if (jerarquia?.CategoriaAlbumBotanicoId is not > 0 ||
                    jerarquia.AlbumBotanicoCafeId is not > 0 ||
                    jerarquia.CategoriaEsPropuesta ||
                    jerarquia.FichaEsPropuesta)
                {
                    await DisplayAlert(
                        "Clasificación pendiente",
                        "Para publicar debe quedar vinculada a una categoría y subcategoría oficiales del Álbum Botánico.",
                        "Aceptar");
                    return;
                }
            }

            string? descripcion = await DisplayPromptAsync(
                "Publicar en Álbum Botánico",
                $"Se copiará en {jerarquia.Categoria} → {jerarquia.Ficha}. Puede agregar una descripción opcional.",
                "Publicar",
                "Cancelar",
                string.Empty,
                500,
                Keyboard.Text);

            if (descripcion == null)
                return;

            await InspeccionFitosanitariaApiService.Instance.PublicarAlbumAsync(
                idRevision,
                foto.FotografiaId,
                jerarquia.CategoriaAlbumBotanicoId.Value,
                jerarquia.AlbumBotanicoCafeId.Value,
                descripcion,
                esPortada: false,
                orden: 0);
        }

        private async void OnRevisarSeleccionAnalizadorClicked(
            object? sender,
            EventArgs e)
        {
            if (!EsVistaAnalizadorRevision || operacionRevisionActiva)
                return;

            List<InspeccionFotoV2> seleccionadas = viewModel.Fotografias
                .Where(item =>
                    item.Seleccionada &&
                    EsEstadoPendienteRevisionAnalizador(item.Estado))
                .OrderBy(item => item.Orden)
                .ToList();

            if (seleccionadas.Count == 0)
            {
                await DisplayAlert(
                    "Seleccione fotografías",
                    "Seleccione una o varias fotografías antes de iniciar la revisión guiada.",
                    "Aceptar");
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            bool cancelarSecuencia = false;
            int completadas = 0;

            try
            {
                for (int indice = 0; indice < seleccionadas.Count; indice++)
                {
                    InspeccionFotoV2 foto = seleccionadas[indice];
                    var pagina = new RevisionAnalizadorFotografiaPage(
                        seleccionadas,
                        indice);

                    await Navigation.PushModalAsync(pagina, animated: false);
                    RevisionAnalizadorAccion accion =
                        await pagina.ResultadoTask;

                    if (accion == RevisionAnalizadorAccion.Cancelar)
                    {
                        cancelarSecuencia = true;
                        break;
                    }

                    if (accion == RevisionAnalizadorAccion.Omitir)
                        continue;

                    try
                    {
                        bool realizada = accion switch
                        {
                            RevisionAnalizadorAccion.Confirmar =>
                                await ConfirmarDiagnosticoIaAsync(foto),
                            RevisionAnalizadorAccion.Corregir =>
                                await CorregirDiagnosticoAsync(foto),
                            RevisionAnalizadorAccion.DevolverTecnico =>
                                await SolicitarDevolucionTecnicoAsync(foto),
                            _ => false
                        };

                        if (realizada)
                        {
                            foto.Seleccionada = false;
                            completadas++;
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert(
                            $"{foto.Titulo}",
                            ex.Message,
                            "Aceptar");
                    }
                }

                if (completadas > 0)
                    await RecargarDespuesOperacionRevisionAsync();

                if (!cancelarSecuencia)
                {
                    await DisplayAlert(
                        "Revisión guiada",
                        $"Se atendieron {completadas} de {seleccionadas.Count} fotografía(s) seleccionadas. Las omitidas permanecen pendientes.",
                        "Aceptar");
                }
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnFotografiaResultadoTapped(
            object? sender,
            TappedEventArgs e)
        {
            InspeccionFotoV2? foto = e.Parameter as InspeccionFotoV2;
            if (foto == null)
                return;

            List<InspeccionFotoV2> fotos = viewModel.Fotografias
                .Where(item => !string.IsNullOrWhiteSpace(item.UrlImagen))
                .OrderBy(item => item.Orden)
                .ToList();

            int indice = fotos.FindIndex(item =>
                item.FotografiaId == foto.FotografiaId);

            if (indice < 0)
                return;

            var visor = new VisorFotografiaFitosanitariaPage(fotos, indice);
            await Navigation.PushModalAsync(visor, animated: false);
        }

        private void ActualizarPanelGlobalAnalizador()
        {
            if (!EsVistaAnalizadorRevision)
                return;

            try
            {
                int seleccionadasRevision = viewModel.Fotografias.Count(item =>
                    item.Seleccionada &&
                    EsEstadoPendienteRevisionAnalizador(item.Estado));
                int seleccionadasEnvio = viewModel.Fotografias.Count(item =>
                    item.Seleccionada &&
                    EsEstadoRevisadoSinEnviar(item.Estado) &&
                    item.TieneAnalisisHumano);
                int revisadasSinEnviar = viewModel.Fotografias.Count(item =>
                    EsEstadoRevisadoSinEnviar(item.Estado) &&
                    item.TieneAnalisisHumano);
                int enviadasAprobador = viewModel.Fotografias.Count(item =>
                    EsEstadoEnviadoAprobador(item.Estado));
                int pendientesRevision = viewModel.Fotografias.Count(item =>
                    EsEstadoPendienteRevisionAnalizador(item.Estado));

                ResumenRevisionAnalizadorV2? resumen = contextoRevision?.Resumen;
                int recibidas = resumen?.TotalRecibidasAnalizador ??
                    viewModel.Fotografias.Count;

                if (EstadoRevisionAnalizadorLabel != null)
                {
                    EstadoRevisionAnalizadorLabel.Text =
                        $"Recibidas: {recibidas} · Revisadas sin enviar: {revisadasSinEnviar} · " +
                        $"Enviadas al aprobador: {enviadasAprobador} · Pendientes de revisar: {pendientesRevision}.";
                }

                if (AyudaRevisionAnalizadorLabel != null)
                {
                    if (resumen == null)
                    {
                        AyudaRevisionAnalizadorLabel.Text = string.Empty;
                    }
                    else if (!resumen.EtapaTecnicaFinalizada)
                    {
                        AyudaRevisionAnalizadorLabel.Text =
                            "Puede revisar fotografías recibidas, pero el envío al aprobador se habilitará cuando el técnico finalice su etapa.";
                    }
                    else if (pendientesRevision > 0)
                    {
                        AyudaRevisionAnalizadorLabel.Text = pendientesRevision == 1
                            ? "Falta revisar 1 fotografía."
                            : $"Faltan revisar {pendientesRevision} fotografías.";
                    }
                    else if (revisadasSinEnviar > 0)
                    {
                        AyudaRevisionAnalizadorLabel.Text = revisadasSinEnviar == 1
                            ? "Hay 1 fotografía revisada pendiente de envío al aprobador."
                            : $"Hay {revisadasSinEnviar} fotografías revisadas pendientes de envío al aprobador.";
                    }
                    else if (enviadasAprobador > 0)
                    {
                        AyudaRevisionAnalizadorLabel.Text =
                            "Todas las fotografías disponibles fueron enviadas al aprobador.";
                    }
                    else
                    {
                        AyudaRevisionAnalizadorLabel.Text =
                            resumen.MotivoNoPuedeFinalizarRevision;
                    }
                }

                if (RevisarSeleccionAnalizadorButton != null)
                {
                    RevisarSeleccionAnalizadorButton.Text = seleccionadasRevision > 0
                        ? $"Revisar seleccionadas ({seleccionadasRevision})"
                        : "Revisar seleccionadas";
                    RevisarSeleccionAnalizadorButton.IsEnabled =
                        !operacionRevisionActiva && seleccionadasRevision > 0;
                }

                if (FinalizarRevisionAnalizadorButton != null)
                {
                    FinalizarRevisionAnalizadorButton.Text = seleccionadasEnvio > 0
                        ? $"Enviar seleccionadas al aprobador ({seleccionadasEnvio})"
                        : "Enviar seleccionadas al aprobador";
                    bool puedeEnviarSeleccion =
                        !operacionRevisionActiva &&
                        resumen?.EtapaTecnicaFinalizada == true &&
                        resumen.EtapaAnalizadorFinalizada == false &&
                        seleccionadasEnvio > 0;

                    FinalizarRevisionAnalizadorButton.IsEnabled =
                        puedeEnviarSeleccion;
                    FinalizarRevisionAnalizadorButton.BackgroundColor =
                        puedeEnviarSeleccion
                            ? Color.FromArgb("#263A35")
                            : Color.FromArgb("#B0B0B0");
                }
            }
            catch
            {
                // La capa visual no sustituye las validaciones del backend.
            }
        }

        private async void OnConfirmarTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoPendienteRevisionAnalizador(foto.Estado))
            {
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                if (await ConfirmarDiagnosticoIaAsync(foto))
                {
                    await DisplayAlert(
                        "Diagnóstico confirmado",
                        "La clasificación de la IA quedó registrada como clasificación humana de esta fotografía.",
                        "Aceptar");

                    await RecargarDespuesOperacionRevisionAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Confirmar diagnóstico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async void OnCorregirTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoPendienteRevisionAnalizador(foto.Estado))
            {
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                if (await CorregirDiagnosticoAsync(foto))
                {
                    await DisplayAlert(
                        "Diagnóstico corregido",
                        "La clasificación humana quedó guardada con la categoría y subcategoría seleccionadas del catálogo.",
                        "Aceptar");

                    await RecargarDespuesOperacionRevisionAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Corregir diagnóstico",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
            }
        }

        private async Task<bool> ConfirmarDiagnosticoIaAsync(
            InspeccionFotoV2 foto)
        {
            InspeccionFotoResultadoIAV2? ia = foto.ResultadoIA;
            if (ia == null || string.IsNullOrWhiteSpace(ia.DiagnosticoProbable))
            {
                await DisplayAlert(
                    "Resultado IA no disponible",
                    "Esta fotografía no tiene un diagnóstico de IA que pueda confirmarse.",
                    "Aceptar");
                return false;
            }

            /*
             * Si la IA propuso una subcategoría que aún no existe, primero se
             * obliga a revisar el catálogo. La categoría nunca se escribe
             * libremente desde la inspección.
             */
            if (!foto.TieneClasificacionAlbumCompleta)
            {
                var paginaJerarquia = new JerarquiaAlbumFotografiaPage(
                    idRevision,
                    foto,
                    "ANALIZADOR");
                await Navigation.PushModalAsync(paginaJerarquia);
                bool guardada = await paginaJerarquia.ResultadoTask;
                if (!guardada)
                    return false;
            }

            await GuardarClasificacionHumanaAsync(
                foto,
                ia.DiagnosticoProbable,
                ia.CategoriaPrincipal,
                "Diagnóstico de IA confirmado por el analizador sin cambios.");

            return true;
        }

        private async Task<bool> CorregirDiagnosticoAsync(
            InspeccionFotoV2 foto)
        {
            var paginaJerarquia = new JerarquiaAlbumFotografiaPage(
                idRevision,
                foto,
                "ANALIZADOR");
            await Navigation.PushModalAsync(paginaJerarquia);
            bool guardada = await paginaJerarquia.ResultadoTask;
            if (!guardada)
                return false;

            JerarquiaDiagnosticoFotoResponse? jerarquia =
                await ObtenerJerarquiaActualizadaAsync(foto.FotografiaId);

            string diagnostico =
                !string.IsNullOrWhiteSpace(jerarquia?.Ficha)
                    ? jerarquia.Ficha.Trim()
                    : foto.ResultadoIA?.DiagnosticoProbable?.Trim() ??
                      string.Empty;

            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                await DisplayAlert(
                    "Clasificación incompleta",
                    "No fue posible determinar la subcategoría corregida.",
                    "Aceptar");
                return false;
            }

            string categoriaPrincipal = MapearCategoriaPrincipalHumana(
                jerarquia?.Categoria,
                foto.ResultadoIA?.CategoriaPrincipal);

            string diagnosticoIa =
                foto.ResultadoIA?.DiagnosticoProbable?.Trim() ??
                "Sin diagnóstico IA";

            await GuardarClasificacionHumanaAsync(
                foto,
                diagnostico,
                categoriaPrincipal,
                $"Clasificación corregida por el analizador. IA sugirió: {diagnosticoIa}. Clasificación humana: {diagnostico}.");

            return true;
        }

        private async Task GuardarClasificacionHumanaAsync(
            InspeccionFotoV2 foto,
            string diagnostico,
            string? categoriaPrincipal,
            string observacion)
        {
            InspeccionFotoResultadoIAV2? ia = foto.ResultadoIA;
            InspeccionFotoAnalisisHumanoV2? anterior =
                foto.UltimoAnalisisHumano;

            var item = new InspeccionFotoAnalisisHumanoRequestV2
            {
                FotografiaId = foto.FotografiaId,
                CalidadEvaluacion = PrimerValor(
                    ia?.CalidadEvaluacion,
                    anterior?.CalidadEvaluacion,
                    "NO_EVALUABLE"),
                EstadoGeneral = PrimerValor(
                    ia?.EstadoGeneral,
                    anterior?.EstadoGeneral,
                    "INDETERMINADA"),
                CategoriaPrincipal = PrimerValor(
                    categoriaPrincipal,
                    anterior?.CategoriaPrincipal,
                    "NO_APLICA"),
                CategoriasSecundarias = ia?.CategoriasSecundarias ??
                    anterior?.CategoriasSecundarias ?? [],
                Diagnostico = diagnostico.Trim(),
                TipoDiagnostico = PrimerValor(
                    ia?.TipoDiagnostico,
                    anterior?.TipoDiagnostico,
                    string.Empty),
                Severidad = PrimerValor(
                    ia?.SeveridadVisual,
                    anterior?.Severidad,
                    "NO_EVALUABLE"),
                NivelCerteza = PrimerValor(
                    ia?.NivelCerteza,
                    anterior?.NivelCerteza,
                    "NO_DETERMINADO"),
                Observaciones = observacion
            };

            await InspeccionFitosanitariaApiService.Instance
                .GuardarAnalisisHumanoAsync(
                    idRevision,
                    [item],
                    enviarAprobacion: false);
        }

        private async Task<JerarquiaDiagnosticoFotoResponse?>
            ObtenerJerarquiaActualizadaAsync(int fotografiaId)
        {
            var servicio = new AlbumJerarquiaApiService();
            ApiResult<List<JerarquiaDiagnosticoFotoResponse>> resultado =
                await servicio.GetJerarquiaDiagnosticoAsync(idRevision);

            if (!resultado.Success)
                throw new InvalidOperationException(resultado.Message);

            return resultado.Data?.FirstOrDefault(item =>
                item.FotografiaId == fotografiaId);
        }

        private static string MapearCategoriaPrincipalHumana(
            string? categoriaAlbum,
            string? categoriaIa)
        {
            string normalizada = (categoriaAlbum ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (normalizada.Contains("ENFERMED"))
                return "ENFERMEDAD";
            if (normalizada.Contains("PLAGA"))
                return "PLAGA";
            if (normalizada.Contains("NUTRIC"))
                return "ALTERACION_NUTRICIONAL";
            if (normalizada.Contains("ESTRÉS") ||
                normalizada.Contains("ESTRES"))
            {
                return "ESTRES_ABIOTICO";
            }
            if (normalizada.Contains("MECÁNIC") ||
                normalizada.Contains("MECANIC"))
            {
                return "DANO_MECANICO";
            }
            if (normalizada.Contains("SANA"))
                return "NO_APLICA";

            return PrimerValor(
                categoriaIa,
                "AFECTACION_NO_DETERMINADA");
        }

        private static string PrimerValor(params string?[] valores) =>
            valores.FirstOrDefault(valor =>
                !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;

        private async void OnDevolverTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoPendienteRevisionAnalizador(foto.Estado))
            {
                return;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                if (await SolicitarDevolucionTecnicoAsync(foto))
                {
                    await DisplayAlert(
                        "Corrección solicitada",
                        "La fotografía fue devuelta al técnico. Los demás borradores humanos se conservaron.",
                        "Aceptar");

                    await RecargarDespuesOperacionRevisionAsync();
                }
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

        private async Task<bool> SolicitarDevolucionTecnicoAsync(
            InspeccionFotoV2 foto)
        {
            var formulario = new DevolucionTecnicoFotografiaPage(foto, 1, 1);
            Task<DevolucionTecnicoFormularioResultado?> espera =
                formulario.EsperarResultadoAsync();
            await Navigation.PushModalAsync(formulario, animated: false);
            DevolucionTecnicoFormularioResultado? resultado = await espera;

            if (resultado == null)
                return false;

            bool confirmar = await DisplayAlert(
                "Devolver al técnico",
                "La fotografía saldrá temporalmente de la bandeja del analizador y la etapa técnica se reabrirá para que el usuario creador atienda la solicitud. ¿Desea continuar?",
                "Devolver",
                "Cancelar");

            if (!confirmar)
                return false;

            await revisionApi.DevolverTecnicoAsync(
                idRevision,
                foto.FotografiaId,
                resultado.MotivoId,
                resultado.Instrucciones);

            return true;
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

        private async void OnEnviarAprobadorTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !EsEstadoRevisadoSinEnviar(foto.Estado) ||
                !foto.TieneAnalisisHumano)
            {
                return;
            }

            bool confirmar = await DisplayAlert(
                "Enviar al aprobador",
                "La fotografía quedará bloqueada para el analizador y estará disponible inmediatamente en la bandeja del aprobador. ¿Desea continuar?",
                "Enviar",
                "Cancelar");

            if (!confirmar)
                return;

            await EnviarFotografiasAprobadorAsync(
                [foto.FotografiaId],
                mostrarResumen: false);
        }

        private async void OnFinalizarRevisionAnalizadorClicked(
            object? sender,
            EventArgs e)
        {
            await EnviarSeleccionadasAprobadorAsync();
        }

        /* Compatibilidad con versiones anteriores que todavía referencien el
         * manejador desde controles generados dinámicamente. */
        private async void OnFinalizarRevisionTarjetaClicked(
            object? sender,
            EventArgs e)
        {
            await EnviarSeleccionadasAprobadorAsync();
        }

        private async Task EnviarSeleccionadasAprobadorAsync()
        {
            if (operacionRevisionActiva)
                return;

            List<int> fotografiaIds = viewModel.Fotografias
                .Where(item =>
                    item.Seleccionada &&
                    EsEstadoRevisadoSinEnviar(item.Estado) &&
                    item.TieneAnalisisHumano)
                .OrderBy(item => item.Orden)
                .Select(item => item.FotografiaId)
                .Distinct()
                .ToList();

            if (fotografiaIds.Count == 0)
            {
                await DisplayAlert(
                    "Seleccione fotografías revisadas",
                    "Seleccione una o varias fotografías que ya tengan revisión humana antes de enviarlas al aprobador.",
                    "Aceptar");
                return;
            }

            string detalle = fotografiaIds.Count == 1
                ? "Se enviará 1 fotografía al aprobador y quedará bloqueada para el analizador."
                : $"Se enviarán {fotografiaIds.Count} fotografías al aprobador y quedarán bloqueadas para el analizador.";

            bool confirmar = await DisplayAlert(
                "Enviar seleccionadas al aprobador",
                detalle + " ¿Desea continuar?",
                "Enviar",
                "Cancelar");

            if (!confirmar)
                return;

            await EnviarFotografiasAprobadorAsync(
                fotografiaIds,
                mostrarResumen: true);
        }

        private async Task EnviarFotografiasAprobadorAsync(
            IReadOnlyCollection<int> fotografiaIds,
            bool mostrarResumen)
        {
            if (fotografiaIds.Count == 0 || operacionRevisionActiva)
                return;

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();

            try
            {
                contextoRevision = await revisionApi.EnviarAprobadorAsync(
                    idRevision,
                    fotografiaIds);

                foreach (InspeccionFotoV2 foto in viewModel.Fotografias
                             .Where(item => fotografiaIds.Contains(item.FotografiaId)))
                {
                    foto.Seleccionada = false;
                }

                await DisplayAlert(
                    "Envío realizado",
                    mostrarResumen
                        ? fotografiaIds.Count == 1
                            ? "La fotografía seleccionada quedó disponible para el aprobador."
                            : $"Las {fotografiaIds.Count} fotografías seleccionadas quedaron disponibles para el aprobador."
                        : "La fotografía quedó disponible para el aprobador.",
                    "Aceptar");

                await RecargarDespuesOperacionRevisionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Enviar al aprobador",
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

            foreach (KeyValuePair<int, AccionesTarjetaAprobador> par in
                     accionesAprobadorPorFoto)
            {
                InspeccionFotoV2? foto = viewModel.Fotografias
                    .FirstOrDefault(item => item.FotografiaId == par.Key);
                if (foto != null)
                    ActualizarAccionesAprobador(par.Value, foto);
            }

            ActualizarPanelGlobalAnalizador();
            ActualizarPanelGlobalAprobador();
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

        private sealed class AccionesTarjetaAprobador
        {
            public AccionesTarjetaAprobador(
                Border tarjeta,
                Border panel,
                Label estado,
                Label ayuda,
                Button aprobar,
                Button corregir,
                Button devolver,
                Button rechazar,
                Button noConcluyente,
                Button autorizarAlbum,
                Button publicar,
                Button cancelarAlbum)
            {
                Tarjeta = tarjeta;
                Panel = panel;
                Estado = estado;
                Ayuda = ayuda;
                Aprobar = aprobar;
                Corregir = corregir;
                Devolver = devolver;
                Rechazar = rechazar;
                NoConcluyente = noConcluyente;
                AutorizarAlbum = autorizarAlbum;
                Publicar = publicar;
                CancelarAlbum = cancelarAlbum;
            }

            public Border Tarjeta { get; }
            public Border Panel { get; }
            public Label Estado { get; }
            public Label Ayuda { get; }
            public Button Aprobar { get; }
            public Button Corregir { get; }
            public Button Devolver { get; }
            public Button Rechazar { get; }
            public Button NoConcluyente { get; }
            public Button AutorizarAlbum { get; }
            public Button Publicar { get; }
            public Button CancelarAlbum { get; }
        }

        private sealed class AccionesTarjetaRevision
        {
            public AccionesTarjetaRevision(
                Border tarjeta,
                Border panel,
                bool esAnalizador,
                Label estado,
                Label bloqueo,
                Button? confirmar,
                Button? corregir,
                Button? devolver,
                Button? enviarAprobador,
                Button? atender)
            {
                Tarjeta = tarjeta;
                Panel = panel;
                EsAnalizador = esAnalizador;
                Estado = estado;
                Bloqueo = bloqueo;
                Confirmar = confirmar;
                Corregir = corregir;
                Devolver = devolver;
                EnviarAprobador = enviarAprobador;
                Atender = atender;
            }

            public Border Tarjeta { get; }
            public Border Panel { get; }
            public bool EsAnalizador { get; }
            public Label Estado { get; }
            public Label Bloqueo { get; }
            public Button? Confirmar { get; }
            public Button? Corregir { get; }
            public Button? Devolver { get; }
            public Button? EnviarAprobador { get; }
            public Button? Atender { get; }
        }
    }
}
