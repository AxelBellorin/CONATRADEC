using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

#if ANDROID
using Android.Content;
using Android.Views.InputMethods;
#endif

namespace CONATRADEC.Services;

/// <summary>
/// Servicio centralizado para cerrar el teclado virtual.
///
/// En Android:
/// - Libera el foco del control activo.
/// - Solicita cerrar el teclado.
/// - Espera brevemente cuando una alerta o un Snackbar necesita que
///   la pantalla termine de reajustarse.
///
/// En Windows no realiza ninguna acción.
/// </summary>
public static class KeyboardService
{
    private const int KeyboardAnimationWaitMilliseconds = 140;

    private static readonly SemaphoreSlim HideLock =
        new(1, 1);

    private static long lastHideRequestMilliseconds =
        long.MinValue;

    /// <summary>
    /// Cierra el teclado inmediatamente desde una interacción táctil.
    /// No bloquea el hilo de la interfaz.
    /// </summary>
    public static void HideImmediately()
    {
#if ANDROID
        if (MainThread.IsMainThread)
        {
            HideInternal();
            return;
        }

        MainThread.BeginInvokeOnMainThread(
            () => HideInternal());
#endif
    }

    /// <summary>
    /// Cierra el teclado y espera el tiempo mínimo necesario para que
    /// Android reajuste el área visible antes de mostrar mensajes,
    /// confirmaciones o realizar navegación.
    /// </summary>
    public static async Task HideAsync()
    {
#if ANDROID
        await HideLock.WaitAsync();

        try
        {
            bool hideRequested =
                await MainThread.InvokeOnMainThreadAsync(
                    HideInternal);

            long elapsedMilliseconds =
                Environment.TickCount64 -
                Volatile.Read(
                    ref lastHideRequestMilliseconds);

            if (!hideRequested &&
                elapsedMilliseconds >=
                    KeyboardAnimationWaitMilliseconds)
            {
                return;
            }

            int remainingMilliseconds =
                KeyboardAnimationWaitMilliseconds -
                (int)Math.Max(0, elapsedMilliseconds);

            if (remainingMilliseconds > 0)
            {
                await Task.Delay(
                    remainingMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"No fue posible ocultar el teclado virtual: {ex}");
        }
        finally
        {
            HideLock.Release();
        }
#else
        await Task.CompletedTask;
#endif
    }

#if ANDROID
    private static bool HideInternal()
    {
        var activity =
            Platform.CurrentActivity;

        var focusedView =
            activity?.CurrentFocus;

        if (activity == null ||
            focusedView == null)
        {
            return false;
        }

        var windowToken =
            focusedView.WindowToken;

        if (windowToken == null)
            return false;

        var inputMethodManager =
            activity.GetSystemService(
                Context.InputMethodService)
            as InputMethodManager;

        inputMethodManager?
            .HideSoftInputFromWindow(
                windowToken,
                HideSoftInputFlags.None);

        focusedView.ClearFocus();

        Volatile.Write(
            ref lastHideRequestMilliseconds,
            Environment.TickCount64);

        return true;
    }
#endif
}
