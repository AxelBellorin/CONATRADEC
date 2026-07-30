using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using CONATRADEC.Services;
using Microsoft.Maui;
using Plugin.Fingerprint;

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

            bloquearRetrocesoCallback =
                new BloquearRetrocesoCallback();

            OnBackPressedDispatcher.AddCallback(
                bloquearRetrocesoCallback);

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
                    insets.AppearanceLightStatusBars =
                        false;
                }
            }

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
        /// Registra toques reales y conserva el comportamiento global que oculta
        /// el teclado al tocar fuera de un Entry o Editor.
        /// </summary>
        public override bool DispatchTouchEvent(
            MotionEvent? motionEvent)
        {
            if (motionEvent?.Action ==
                MotionEventActions.Down)
            {
                SesionInactividadService.Instance
                    .RegistrarActividad();
            }

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

        public override bool DispatchKeyEvent(
            KeyEvent? keyEvent)
        {
            if (keyEvent?.Action ==
                KeyEventActions.Down)
            {
                SesionInactividadService.Instance
                    .RegistrarActividad();
            }

            return base.DispatchKeyEvent(
                keyEvent);
        }

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
                 * La navegación se realiza mediante los botones de la app.
                 */
            }
        }
    }
}
