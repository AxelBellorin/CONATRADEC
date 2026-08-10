using Microsoft.Maui.Controls;

namespace CONATRADEC.Views;

/// <summary>
/// Refuerzo de ciclo de vida para la pantalla de inicio de sesión.
///
/// El login contiene eventos Unfocused asíncronos que esperan unos milisegundos
/// antes de restaurar bordes y animaciones. En Windows, al cerrar la aplicación
/// durante depuración, WinUI puede comenzar a destruir los controles nativos
/// durante esa espera. Si el callback continúa después, cualquier cambio visual
/// puede producir una COMException porque el control ya está desconectado.
///
/// Este partial no modifica la lógica del login. Únicamente marca la interfaz
/// como en proceso de cierre antes de que desaparezcan los handlers nativos.
/// </summary>
public partial class loginPage
{
    private Window? _loginLifecycleWindow;
    private bool _loginLifecycleEventsAttached;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (!_loginLifecycleEventsAttached)
        {
            Loaded += LoginPage_ShutdownSafetyLoaded;
            Unloaded += LoginPage_ShutdownSafetyUnloaded;
            _loginLifecycleEventsAttached = true;
        }

        SuscribirCierreVentana();
    }

    protected override void OnHandlerChanging(
        HandlerChangingEventArgs args)
    {
        /*
         * Cuando NewHandler es null, MAUI está a punto de retirar el control
         * nativo. La documentación de MAUI recomienda usar HandlerChanging
         * precisamente para limpiar referencias antes de que desaparezca.
         */
        if (args.OldHandler is not null &&
            args.NewHandler is null)
        {
            MarcarArbolVisualEnCierre();
            DesuscribirCierreVentana();
        }

        base.OnHandlerChanging(args);
    }

    private void LoginPage_ShutdownSafetyLoaded(
        object? sender,
        EventArgs e)
    {
        /*
         * La misma instancia puede volver a cargarse después de una navegación.
         * Se restablece el indicador que InputEntry_Unfocused ya utiliza para
         * evitar cambios visuales mientras la contraseña cambia de modo.
         */
        _isTogglingPasswordVisibility = false;
        SuscribirCierreVentana();
    }

    private void LoginPage_ShutdownSafetyUnloaded(
        object? sender,
        EventArgs e)
    {
        MarcarArbolVisualEnCierre();
        DesuscribirCierreVentana();
    }

    private void SuscribirCierreVentana()
    {
        Window? ventanaActual = Window;

        if (ReferenceEquals(
                _loginLifecycleWindow,
                ventanaActual))
        {
            return;
        }

        DesuscribirCierreVentana();

        _loginLifecycleWindow = ventanaActual;

        if (_loginLifecycleWindow is not null)
        {
            _loginLifecycleWindow.Destroying +=
                LoginWindow_Destroying;
        }
    }

    private void DesuscribirCierreVentana()
    {
        if (_loginLifecycleWindow is null)
            return;

        _loginLifecycleWindow.Destroying -=
            LoginWindow_Destroying;

        _loginLifecycleWindow = null;
    }

    private void LoginWindow_Destroying(
        object? sender,
        EventArgs e)
    {
        MarcarArbolVisualEnCierre();
        DesuscribirCierreVentana();
    }

    private void MarcarArbolVisualEnCierre()
    {
        /*
         * InputEntry_Unfocused ya consulta _isTogglingPasswordVisibility
         * después de su Task.Delay(100). Se reutiliza esa barrera para que
         * el callback salga antes de tocar UserBorder/PasswordBorder cuando
         * WinUI se está cerrando.
         */
        _pageIsVisible = false;
        _isTogglingPasswordVisibility = true;

        /*
         * Solo se cancelan tareas administradas. No se tocan controles
         * visuales aquí porque precisamente pueden estar desconectándose.
         */
        StopIdleAnimation();
    }
}
