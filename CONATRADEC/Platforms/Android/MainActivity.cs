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
        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

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
    }
}