using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Desactiva la navegación visual nativa de retroceso de Shell en toda
    /// la aplicación. Las páginas conservan únicamente sus botones y comandos
    /// propios de navegación.
    ///
    /// El tratamiento adicional del encabezado heredado se mantiene limitado
    /// al módulo de inspección fitosanitaria, igual que antes.
    /// </summary>
    public sealed class InspeccionNavegacionBehavior : Behavior<Shell>
    {
        private const string MarcaEncabezadoFormularioAntiguo =
            "CONATRADEC_FORM_BACK_WRAPPER";

        private Shell? shell;

        protected override void OnAttachedTo(Shell bindable)
        {
            base.OnAttachedTo(bindable);

            shell = bindable;
            bindable.Navigated += Shell_Navigated;

            ProgramarAplicacion();
        }

        protected override void OnDetachingFrom(Shell bindable)
        {
            bindable.Navigated -= Shell_Navigated;
            shell = null;

            base.OnDetachingFrom(bindable);
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            ProgramarAplicacion();
        }

        /// <summary>
        /// Se ejecuta en dos ciclos del hilo principal para aplicarse después
        /// de que Shell y los servicios de encabezado terminen de preparar la
        /// página visible.
        /// </summary>
        private void ProgramarAplicacion()
        {
            MainThread.BeginInvokeOnMainThread(
                () => MainThread.BeginInvokeOnMainThread(
                    AplicarEnPaginaActual));
        }

        private void AplicarEnPaginaActual()
        {
            if (shell?.CurrentPage is not ContentPage pagina)
                return;

            /*
             * Regla global: CONATRADEC no utiliza la flecha nativa de Shell o
             * NavigationPage. La navegación se realiza con los controles que
             * ya existen dentro de cada pantalla.
             */
            NavigationPage.SetHasBackButton(
                pagina,
                false);

            BackButtonBehavior comportamiento =
                Shell.GetBackButtonBehavior(pagina) ??
                new BackButtonBehavior();

            comportamiento.IsVisible = false;
            comportamiento.IsEnabled = false;
            comportamiento.Command = null;

            Shell.SetBackButtonBehavior(
                pagina,
                comportamiento);

            if (!EsPaginaInspeccion(pagina))
                return;

            /*
             * El módulo fitosanitario ya trabajaba sin barra de navegación
             * nativa. Se conserva ese comportamiento específico.
             */
            Shell.SetNavBarIsVisible(
                pagina,
                false);

            NavigationPage.SetHasNavigationBar(
                pagina,
                false);

            QuitarFlechaHeredadaSiExiste(pagina);
        }

        private static bool EsPaginaInspeccion(ContentPage pagina)
        {
            string nombre = pagina.GetType().Name;

            return
                nombre.StartsWith(
                    "DiagnosticoIA",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.StartsWith(
                    "TipoFotografiaIA",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    nombre,
                    "TerrenoBusquedaIAPage",
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Algunos formularios antiguos pueden recibir un encabezado dinámico.
        /// Si una pantalla del módulo ya posee su encabezado propio, se restaura
        /// su contenido original.
        /// </summary>
        private static void QuitarFlechaHeredadaSiExiste(
            ContentPage pagina)
        {
            if (pagina.Content is not Grid contenedor ||
                !string.Equals(
                    contenedor.StyleId,
                    MarcaEncabezadoFormularioAntiguo,
                    StringComparison.Ordinal))
            {
                return;
            }

            View? contenidoOriginal =
                contenedor.Children
                    .OfType<View>()
                    .FirstOrDefault(
                        vista => Grid.GetRow(vista) == 1);

            if (contenidoOriginal == null)
                return;

            contenedor.Children.Remove(contenidoOriginal);
            pagina.Content = contenidoOriginal;
        }
    }
}
