using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Extensión aditiva de la pantalla de resultados. Sustituye visualmente
    /// los tres controles históricos del Álbum (clasificar/publicar/retirar por
    /// fotografía) por una sola entrada a la administración por diagnóstico.
    /// La lógica técnica, bloqueos y decisiones del expediente permanecen en
    /// DiagnosticoIAResultadoPage.FlujoRevision.cs sin ser reconstruidos.
    /// </summary>
    public partial class DiagnosticoIAResultadoPage
    {
        private readonly Dictionary<int, Button>
            botonesAlbumDiagnosticosV2 = [];
        private bool integracionAlbumDiagnosticosProgramada;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0 || height <= 0)
                return;

            IntegrarBotonesAlbumPorDiagnostico();
            ProgramarIntegracionAlbumPorDiagnostico();
        }

        private void ProgramarIntegracionAlbumPorDiagnostico()
        {
            if (integracionAlbumDiagnosticosProgramada ||
                !EsVistaAprobadorAdministracion)
            {
                return;
            }

            integracionAlbumDiagnosticosProgramada = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    for (int intento = 0; intento < 18; intento++)
                    {
                        await Task.Delay(120);

                        if (!paginaVisualActiva)
                            break;

                        Dispatcher.Dispatch(() =>
                        {
                            if (paginaVisualActiva)
                                IntegrarBotonesAlbumPorDiagnostico();
                        });
                    }
                }
                finally
                {
                    integracionAlbumDiagnosticosProgramada = false;
                }
            });
        }

        private void IntegrarBotonesAlbumPorDiagnostico()
        {
            if (!paginaVisualActiva || !EsVistaAprobadorAdministracion)
                return;

            foreach (KeyValuePair<int, AccionesTarjetaAprobador> par in
                     accionesAprobadorPorFoto.ToList())
            {
                int fotoId = par.Key;
                AccionesTarjetaAprobador acciones = par.Value;

                InspeccionFotoV2? foto = viewModel.Fotografias
                    .FirstOrDefault(item => item.FotografiaId == fotoId);

                if (foto == null)
                    continue;

                if (botonesAlbumDiagnosticosV2.TryGetValue(
                        fotoId,
                        out Button? existente) &&
                    existente.Parent != null)
                {
                    ActualizarBotonAlbumDiagnosticos(existente, foto);
                    continue;
                }

                Microsoft.Maui.Controls.Layout? contenedor =
                    acciones.ClasificarAlbum.Parent as Microsoft.Maui.Controls.Layout ??
                    acciones.Publicar.Parent as Microsoft.Maui.Controls.Layout ??
                    acciones.RetirarAlbum.Parent as Microsoft.Maui.Controls.Layout;

                if (contenedor == null)
                    continue;

                int indice = ObtenerPrimerIndice(
                    contenedor,
                    acciones.ClasificarAlbum,
                    acciones.Publicar,
                    acciones.RetirarAlbum);

                RemoverSiPertenece(contenedor, acciones.ClasificarAlbum);
                RemoverSiPertenece(contenedor, acciones.Publicar);
                RemoverSiPertenece(contenedor, acciones.RetirarAlbum);

                var boton = new Button
                {
                    Text = "Administrar clasificaciones y publicaciones",
                    CommandParameter = foto,
                    BackgroundColor = Color.FromArgb("#F2C94C"),
                    TextColor = Color.FromArgb("#263A35"),
                    CornerRadius = 10,
                    HeightRequest = 44,
                    Padding = new Thickness(12, 7),
                    Margin = new Thickness(0, 0, 8, 8),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions =
                        DeviceInfo.Current.Idiom == DeviceIdiom.Phone
                            ? LayoutOptions.Fill
                            : LayoutOptions.Start
                };

                boton.Clicked +=
                    OnAdministrarAlbumDiagnosticosClicked;

                indice = Math.Clamp(
                    indice,
                    0,
                    contenedor.Children.Count);
                contenedor.Children.Insert(indice, boton);

                botonesAlbumDiagnosticosV2[fotoId] = boton;
                ActualizarBotonAlbumDiagnosticos(boton, foto);
            }
        }

        private void ActualizarBotonAlbumDiagnosticos(
            Button boton,
            InspeccionFotoV2 foto)
        {
            bool puedeAdministrar =
                foto.EstaAprobadaTecnicamente &&
                PermissionService.Instance.HasUpdate(
                    DiagnosticoIARoutes.InterfazAprobador);

            boton.IsVisible = puedeAdministrar;
            boton.IsEnabled = puedeAdministrar && !operacionRevisionActiva;
        }

        private static int ObtenerPrimerIndice(
            Microsoft.Maui.Controls.Layout contenedor,
            params Button[] botones)
        {
            int indice = int.MaxValue;

            foreach (Button boton in botones)
            {
                if (!ReferenceEquals(boton.Parent, contenedor))
                    continue;

                int actual = contenedor.Children.IndexOf(boton);
                if (actual >= 0)
                    indice = Math.Min(indice, actual);
            }

            return indice == int.MaxValue
                ? contenedor.Children.Count
                : indice;
        }

        private static void RemoverSiPertenece(
            Microsoft.Maui.Controls.Layout contenedor,
            Button boton)
        {
            if (ReferenceEquals(boton.Parent, contenedor))
                contenedor.Children.Remove(boton);
        }

        private async void OnAdministrarAlbumDiagnosticosClicked(
            object? sender,
            EventArgs e)
        {
            if (operacionRevisionActiva ||
                !EsVistaAprobadorAdministracion ||
                sender is not Button boton ||
                boton.CommandParameter is not InspeccionFotoV2 foto ||
                !foto.EstaAprobadaTecnicamente)
            {
                return;
            }

            if (!PermissionService.Instance.HasUpdate(
                    DiagnosticoIARoutes.InterfazAprobador))
            {
                await DisplayAlert(
                    "Álbum Botánico",
                    "No tiene permiso para administrar las clasificaciones de esta etapa.",
                    "Aceptar");
                return;
            }

            bool bloqueoTemporalRevisados = false;

            if (EsVistaAprobadorRevisadas &&
                (bloqueoRevision == null ||
                 !bloqueoRevision.Adquirido ||
                 string.IsNullOrWhiteSpace(bloqueoRevision.Token)))
            {
                if (!await PrepararBloqueoRevisionAsync())
                    return;

                bloqueoTemporalRevisados = true;
            }

            operacionRevisionActiva = true;
            ActualizarEstadoBotonesTarjetas();
            ActualizarBotonAlbumDiagnosticos(boton, foto);

            try
            {
                var pagina = new JerarquiaAlbumFotografiaPage(
                    idRevision,
                    foto,
                    "APROBADOR");

                await MostrarModalManteniendoBloqueoAsync(pagina);
                bool huboCambios = await pagina.ResultadoTask;

                if (huboCambios)
                {
                    AlbumBotanicoRefreshState.MarcarCambio();
                    await RecargarDespuesOperacionRevisionAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Álbum Botánico",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "No fue posible administrar las clasificaciones de la fotografía."
                        : ex.Message,
                    "Aceptar");
            }
            finally
            {
                if (bloqueoTemporalRevisados)
                    await LiberarBloqueoRevisionAsync();

                operacionRevisionActiva = false;
                ActualizarEstadoBotonesTarjetas();
                IntegrarBotonesAlbumPorDiagnostico();
                ProgramarIntegracionAlbumPorDiagnostico();
            }
        }
    }
}
