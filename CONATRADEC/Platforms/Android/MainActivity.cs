using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using CONATRADEC.Services;
using Microsoft.Maui;
using Plugin.Fingerprint;

// Alias explícitos para evitar ambigüedad con Microsoft.Maui.Graphics.
using AndroidColor = Android.Graphics.Color;
using AndroidRect = Android.Graphics.Rect;
using AndroidBackCallback =
    AndroidX.Activity.OnBackPressedCallback;

namespace CONATRADEC
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private AndroidBackCallback?
            bloquearRetrocesoCallback;

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            /*
             * Android moderno puede procesar el botón y el gesto de retroceso
             * directamente mediante OnBackPressedDispatcher, sin pasar siempre
             * por Shell.OnBackButtonPressed.
             *
             * Este callback consume globalmente el retroceso para que la
             * navegación se realice únicamente mediante los botones de la app.
             */
            bloquearRetrocesoCallback =
                new BloquearRetrocesoCallback();

            OnBackPressedDispatcher.AddCallback(
                bloquearRetrocesoCallback);

            // Color de la barra de estado.
            Window?.SetStatusBarColor(
                AndroidColor.ParseColor("#3B655B"));

            if (Build.VERSION.SdkInt >=
                    BuildVersionCodes.M &&
                Window != null)
            {
                var insets =
                    WindowCompat.GetInsetsController(
                        Window,
                        Window.DecorView);

                if (insets is not null)
                {
                    // False = iconos claros.
                    insets.AppearanceLightStatusBars =
                        false;
                }
            }

            // Mantiene funcionando la autenticación biométrica.
            CrossFingerprint
                .SetCurrentActivityResolver(
                    () => this);
        }

        protected override void OnResume()
        {
            base.OnResume();

            CrossFingerprint
                .SetCurrentActivityResolver(
                    () => this);
        }

        protected override void OnDestroy()
        {
            bloquearRetrocesoCallback?.Remove();
            bloquearRetrocesoCallback?.Dispose();
            bloquearRetrocesoCallback = null;

            base.OnDestroy();
        }

        /// <summary>
        /// Detecta globalmente cuando el usuario toca fuera del campo
        /// de texto, campo numérico, campo decimal o Editor activo.
        ///
        /// Todos esos controles de MAUI utilizan internamente un
        /// EditText en Android, por lo que no es necesario modificar
        /// cada página XAML.
        /// </summary>
        public override bool DispatchTouchEvent(
            MotionEvent? motionEvent)
        {
            if (motionEvent?.Action ==
                    MotionEventActions.Down &&
                CurrentFocus is EditText focusedInput)
            {
                var inputBounds =
                    new AndroidRect();

                focusedInput.GetGlobalVisibleRect(
                    inputBounds);

                bool touchedOutsideInput =
                    !inputBounds.Contains(
                        (int)motionEvent.RawX,
                        (int)motionEvent.RawY);

                if (touchedOutsideInput)
                {
                    KeyboardService.HideImmediately();
                }
            }

            return base.DispatchTouchEvent(
                motionEvent);
        }

        /// <summary>
        /// Consume el botón físico, el botón de la barra de navegación y el
        /// gesto predictivo de retroceso de Android.
        /// </summary>
        private sealed class BloquearRetrocesoCallback
            : AndroidBackCallback
        {
            public BloquearRetrocesoCallback()
                : base(true)
            {
            }

            public override void HandleOnBackPressed()
            {
                /*
                 * Intencionalmente vacío.
                 * No se llama al comportamiento base porque eso permitiría
                 * regresar, cerrar la página o salir de la aplicación.
                 */
            }
        }
    }
}
