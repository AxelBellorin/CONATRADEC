using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Linq;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace CONATRADEC.Services
{
    /// <summary>
    /// Recupera visualmente la ventana principal en Windows.
    ///
    /// Window.Activate por sí solo no vuelve a mostrar una ventana que quedó
    /// oculta o minimizada durante una reconstrucción del Shell. Este servicio
    /// restaura el HWND, lo lleva al frente y después activa WinUI.
    /// En Android y las demás plataformas no realiza ninguna operación.
    /// </summary>
    public static class WindowsWindowRecoveryService
    {
#if WINDOWS
        private const int SwShow = 5;
        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(
            IntPtr hWnd,
            int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(
            IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(
            IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(
            IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(
            IntPtr hWnd);
#endif

        public static void RestaurarYActivar()
        {
#if WINDOWS
            void Restaurar()
            {
                try
                {
                    Window? ventanaMaui =
                        Application.Current?
                            .Windows
                            .FirstOrDefault();

                    if (ventanaMaui?
                            .Handler?
                            .PlatformView
                        is not Microsoft.UI.Xaml.Window ventanaNativa)
                    {
                        return;
                    }

                    IntPtr handle =
                        WinRT.Interop.WindowNative
                            .GetWindowHandle(
                                ventanaNativa);

                    if (handle != IntPtr.Zero)
                    {
                        if (IsIconic(handle))
                        {
                            ShowWindow(
                                handle,
                                SwRestore);
                        }
                        else if (!IsWindowVisible(handle))
                        {
                            ShowWindow(
                                handle,
                                SwShow);
                        }

                        BringWindowToTop(handle);
                        SetForegroundWindow(handle);
                    }

                    ventanaNativa.Activate();
                }
                catch
                {
                    /*
                     * La recuperación visual no debe provocar un segundo
                     * error durante el cierre o la reautenticación.
                     */
                }
            }

            if (MainThread.IsMainThread)
            {
                Restaurar();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(
                    Restaurar);
            }
#endif
        }
    }
}
