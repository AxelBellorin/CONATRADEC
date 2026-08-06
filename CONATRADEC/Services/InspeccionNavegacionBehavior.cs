using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Desactiva la navegación visual nativa de Shell en todo el módulo de
    /// inspección fitosanitaria. Las páginas conservan únicamente los botones
    /// propios definidos en sus encabezados y comandos de cada flujo.
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
        /// Se ejecuta en dos ciclos del hilo principal. El segundo ciclo ocurre
        /// después de los servicios heredados que agregan encabezados a algunos
        /// formularios, permitiendo retirar cualquier flecha duplicada.
        /// </summary>
        private void ProgramarAplicacion()
        {
            MainThread.BeginInvokeOnMainThread(
                () => MainThread.BeginInvokeOnMainThread(
                    AplicarEnPaginaActual));
        }

        private void AplicarEnPaginaActual()
        {
            /*
             * Shell.CurrentPage devuelve Page. Antes de utilizar propiedades
             * exclusivas de ContentPage se comprueba explícitamente el tipo.
             */
            if (shell?.CurrentPage is not ContentPage pagina ||
                !EsPaginaInspeccion(pagina))
            {
                return;
            }

            Shell.SetNavBarIsVisible(pagina, false);
            NavigationPage.SetHasNavigationBar(pagina, false);
            NavigationPage.SetHasBackButton(pagina, false);

            BackButtonBehavior comportamiento =
                Shell.GetBackButtonBehavior(pagina) ??
                new BackButtonBehavior();

            comportamiento.IsVisible = false;
            comportamiento.IsEnabled = false;
            comportamiento.Command = null;

            Shell.SetBackButtonBehavior(
                pagina,
                comportamiento);

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
        /// Algunos formularios antiguos reciben una flecha dinámica mediante
        /// FormNavigationHeaderService. Si una pantalla del módulo ya posee su
        /// encabezado propio, se restaura su contenido original.
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
