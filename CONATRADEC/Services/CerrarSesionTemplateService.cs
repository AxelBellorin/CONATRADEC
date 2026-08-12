using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Puente estable utilizado únicamente por el FooterTemplate para ejecutar
    /// el cierre de sesión sin depender del BindingContext del ControlTemplate.
    ///
    /// La lógica real de cierre continúa perteneciendo a GlobalService.
    /// Este servicio solamente localiza el ViewModel actualmente visible y
    /// ejecuta su CerrarSesionCommand.
    /// </summary>
    public static class CerrarSesionTemplateService
    {
        public static Command CerrarSesionCommand { get; } =
            new(EjecutarCerrarSesion);

        private static void EjecutarCerrarSesion()
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    GlobalService? servicio =
                        Shell.Current?
                            .CurrentPage?
                            .BindingContext as GlobalService;

                    /*
                     * Algunas páginas pueden encontrarse temporalmente sin un
                     * BindingContext derivado de GlobalService durante una
                     * transición. El fallback conserva exactamente la misma
                     * lógica existente sin almacenar una instancia global.
                     */
                    servicio ??= new GlobalService();

                    if (servicio.CerrarSesionCommand.CanExecute(null))
                    {
                        servicio.CerrarSesionCommand.Execute(null);
                    }
                });
        }
    }
}
