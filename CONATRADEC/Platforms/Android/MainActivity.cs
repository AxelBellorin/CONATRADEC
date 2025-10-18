using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace CONATRADEC
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Color de la barra de estado
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#3B655B"));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                // Iconos oscuros (útil si el fondo es claro). 
                var insets = WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (insets is not null)
                    insets.AppearanceLightStatusBars = false; // false = iconos claros
            }
        }
    }
}
