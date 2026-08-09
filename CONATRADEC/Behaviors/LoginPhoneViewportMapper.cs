using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Handlers;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Ajusta el login de teléfonos Android con altura útil intermedia.
    ///
    /// El diseño original ya contempla pantallas pequeñas mediante ScrollView
    /// y un modo Compact. Este mapper se enfoca en teléfonos actuales de
    /// aproximadamente 6 pulgadas, donde el formulario debe entrar completo
    /// en orientación vertical sin exigir desplazamiento cuando el teclado
    /// está cerrado.
    ///
    /// No modifica autenticación, biometría, comandos ni persistencia.
    /// Tabletas y Windows conservan sus comportamientos existentes.
    /// </summary>
    internal static class LoginPhoneViewportMapper
    {
        private const string MapperKey =
            "CONATRADEC.LoginPhoneViewport";

        private static readonly ConditionalWeakTable<
            loginPage,
            LoginPhoneViewportState> States = new();

        private static bool isRegistered;

        internal static void Register()
        {
#if ANDROID
            if (isRegistered)
                return;

            isRegistered = true;

            PageHandler.Mapper.AppendToMapping(
                MapperKey,
                static (_, view) =>
                {
                    if (view is not loginPage page)
                        return;

                    MainThread.BeginInvokeOnMainThread(
                        () => Attach(page));
                });
#endif
        }

        private static void Attach(loginPage page)
        {
            if (DeviceInfo.Platform != DevicePlatform.Android)
                return;

            LoginPhoneViewportState state =
                States.GetValue(
                    page,
                    static currentPage =>
                        new LoginPhoneViewportState(currentPage));

            state.Attach();
        }

        private sealed class LoginPhoneViewportState
        {
            private readonly loginPage page;
            private bool attached;

            public LoginPhoneViewportState(loginPage page)
            {
                this.page = page;
            }

            public void Attach()
            {
                if (attached)
                    return;

                attached = true;

                page.Loaded += Page_Loaded;
                page.Appearing += Page_Appearing;
                page.SizeChanged += Page_SizeChanged;

                Apply();
            }

            private void Page_Loaded(
                object? sender,
                EventArgs e)
            {
                Apply();
            }

            private void Page_Appearing(
                object? sender,
                EventArgs e)
            {
                /*
                 * loginPage ejecuta primero su adaptación móvil propia.
                 * Se reaplica después para que esta optimización sea el estado
                 * visual final en teléfonos con altura intermedia.
                 */
                page.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(120),
                    Apply);
            }

            private void Page_SizeChanged(
                object? sender,
                EventArgs e)
            {
                Apply();
            }

            private void Apply()
            {
                if (DeviceInfo.Platform != DevicePlatform.Android)
                    return;

                double width = page.Width;
                double height = page.Height;

                if (width <= 0 || height <= 0)
                    return;

                /*
                 * Las tabletas son administradas por LoginTabletResponsiveMapper.
                 * En horizontal se conserva el ScrollView porque el teclado y la
                 * menor altura hacen natural el desplazamiento.
                 */
                bool esTelefono =
                    Math.Min(width, height) < 600;

                bool esVertical =
                    height >= width;

                if (!esTelefono || !esVertical)
                    return;

                /*
                 * Menos de 720 unidades lógicas ya usa el modo Compact original.
                 * Más de 980 dispone de altura suficiente para el modo Tall.
                 * El rango intermedio es donde muchos teléfonos de 6 pulgadas
                 * quedaban apenas más altos que el viewport y pedían scroll.
                 */
                if (height < 720 || height >= 980)
                    return;

                if (!TryResolveVisualTree(
                        out LoginPhoneVisualTree visual))
                {
                    return;
                }

                bool alturaAjustada =
                    height < 820;

                ApplyPhoneLayout(
                    visual,
                    alturaAjustada);
            }

            private static void ApplyPhoneLayout(
                LoginPhoneVisualTree visual,
                bool alturaAjustada)
            {
                visual.LoginScrollView.HorizontalOptions =
                    LayoutOptions.Fill;

                visual.LoginScrollView.VerticalOptions =
                    LayoutOptions.Fill;

                visual.ResponsiveRootGrid.Padding =
                    alturaAjustada
                        ? new Thickness(12, 5)
                        : new Thickness(14, 7);

                visual.LoginContentStack.MaximumWidthRequest = 500;
                visual.LoginContentStack.HorizontalOptions =
                    LayoutOptions.Center;
                visual.LoginContentStack.VerticalOptions =
                    LayoutOptions.Start;
                visual.LoginContentStack.Spacing =
                    alturaAjustada ? 6 : 7;

                visual.MobileHeader.Spacing = 2;
                visual.MobileHeader.Margin =
                    new Thickness(0, 0, 0, 1);

                double logoSize =
                    alturaAjustada ? 46 : 52;

                visual.MobileLogoBorder.WidthRequest = logoSize;
                visual.MobileLogoBorder.HeightRequest = logoSize;
                visual.MobileLogoBorder.Padding =
                    new Thickness(3);

                visual.MobileAppTitle.FontSize =
                    alturaAjustada ? 19 : 20;

                /*
                 * En alturas ajustadas se conserva la marca principal y se
                 * oculta solamente la frase secundaria para recuperar espacio.
                 */
                visual.MobileTagline.FontSize = 9;
                visual.MobileTagline.IsVisible =
                    !alturaAjustada;

                visual.LoginCard.HorizontalOptions =
                    LayoutOptions.Fill;
                visual.LoginCard.Padding =
                    alturaAjustada
                        ? new Thickness(12, 10)
                        : new Thickness(14, 11);

                visual.LoginCardContent.Spacing =
                    alturaAjustada ? 8 : 9;

                visual.LoginHeaderGrid.ColumnSpacing = 4;
                visual.WelcomeTextStack.Spacing = 2;
                visual.WelcomeTitle.FontSize =
                    alturaAjustada ? 22 : 23;
                visual.WelcomeSubtitle.FontSize = 11;
                visual.WelcomeSubtitle.MaximumWidthRequest =
                    alturaAjustada ? 225 : 245;

                ApplyMascotSize(
                    visual,
                    stageWidth: alturaAjustada ? 84 : 92,
                    stageHeight: alturaAjustada ? 76 : 84,
                    glowWidth: alturaAjustada ? 62 : 68,
                    glowHeight: alturaAjustada ? 56 : 62,
                    visualSize: alturaAjustada ? 66 : 72,
                    imageSize: alturaAjustada ? 63 : 69,
                    speechWidth: alturaAjustada ? 70 : 76,
                    speechFontSize: alturaAjustada ? 7 : 7.4,
                    privacyWidth: alturaAjustada ? 46 : 50,
                    privacyHeight: 20,
                    privacyTop: alturaAjustada ? 15 : 17,
                    privacyFontSize: 6);

                visual.UserFieldStack.Spacing = 4;
                visual.PasswordFieldStack.Spacing = 4;
                visual.RememberMeGrid.ColumnSpacing = 5;

                visual.BiometricBorder.Padding =
                    alturaAjustada
                        ? new Thickness(7)
                        : new Thickness(8);

                visual.LoginButton.HeightRequest = 50;
                visual.BusyIndicator.HeightRequest = 24;

                /*
                 * El pie repite la misma identidad que ya aparece arriba.
                 * Mantenerlo oculto en teléfono evita crear scroll innecesario.
                 */
                visual.MobileFooter.IsVisible = false;
            }

            private static void ApplyMascotSize(
                LoginPhoneVisualTree visual,
                double stageWidth,
                double stageHeight,
                double glowWidth,
                double glowHeight,
                double visualSize,
                double imageSize,
                double speechWidth,
                double speechFontSize,
                double privacyWidth,
                double privacyHeight,
                double privacyTop,
                double privacyFontSize)
            {
                visual.MascotStage.WidthRequest = stageWidth;
                visual.MascotStage.HeightRequest = stageHeight;

                visual.MascotGlow.WidthRequest = glowWidth;
                visual.MascotGlow.HeightRequest = glowHeight;

                visual.MascotVisual.WidthRequest = visualSize;
                visual.MascotVisual.HeightRequest = visualSize;

                visual.MascotImage.WidthRequest = imageSize;
                visual.MascotImage.HeightRequest = imageSize;

                visual.MascotSpeechText.MaximumWidthRequest =
                    speechWidth;
                visual.MascotSpeechText.FontSize =
                    speechFontSize;

                visual.PrivacyShield.WidthRequest = privacyWidth;
                visual.PrivacyShield.HeightRequest = privacyHeight;
                visual.PrivacyShield.Margin =
                    new Thickness(0, privacyTop, 0, 0);
                visual.PrivacyShieldText.FontSize =
                    privacyFontSize;
            }

            private bool TryResolveVisualTree(
                out LoginPhoneVisualTree visual)
            {
                visual = default!;

                ScrollView? loginScrollView =
                    page.FindByName<ScrollView>(
                        "LoginScrollView");

                Grid? responsiveRootGrid =
                    page.FindByName<Grid>(
                        "ResponsiveRootGrid");

                VerticalStackLayout? loginContentStack =
                    page.FindByName<VerticalStackLayout>(
                        "LoginContentStack");

                VerticalStackLayout? mobileHeader =
                    page.FindByName<VerticalStackLayout>(
                        "MobileHeader");

                Border? mobileLogoBorder =
                    page.FindByName<Border>(
                        "MobileLogoBorder");

                Label? mobileAppTitle =
                    page.FindByName<Label>(
                        "MobileAppTitle");

                Label? mobileTagline =
                    page.FindByName<Label>(
                        "MobileTagline");

                Border? loginCard =
                    page.FindByName<Border>(
                        "LoginCard");

                VerticalStackLayout? loginCardContent =
                    page.FindByName<VerticalStackLayout>(
                        "LoginCardContent");

                Grid? loginHeaderGrid =
                    page.FindByName<Grid>(
                        "LoginHeaderGrid");

                VerticalStackLayout? welcomeTextStack =
                    page.FindByName<VerticalStackLayout>(
                        "WelcomeTextStack");

                Label? welcomeTitle =
                    page.FindByName<Label>(
                        "WelcomeTitle");

                Label? welcomeSubtitle =
                    page.FindByName<Label>(
                        "WelcomeSubtitle");

                Grid? mascotStage =
                    page.FindByName<Grid>(
                        "MascotStage");

                Border? mascotGlow =
                    page.FindByName<Border>(
                        "MascotGlow");

                Grid? mascotVisual =
                    page.FindByName<Grid>(
                        "MascotVisual");

                Image? mascotImage =
                    page.FindByName<Image>(
                        "MascotImage");

                Label? mascotSpeechText =
                    page.FindByName<Label>(
                        "MascotSpeechText");

                Border? privacyShield =
                    page.FindByName<Border>(
                        "PrivacyShield");

                Label? privacyShieldText =
                    page.FindByName<Label>(
                        "PrivacyShieldText");

                VerticalStackLayout? userFieldStack =
                    page.FindByName<VerticalStackLayout>(
                        "UserFieldStack");

                VerticalStackLayout? passwordFieldStack =
                    page.FindByName<VerticalStackLayout>(
                        "PasswordFieldStack");

                Grid? rememberMeGrid =
                    page.FindByName<Grid>(
                        "RememberMeGrid");

                Border? biometricBorder =
                    page.FindByName<Border>(
                        "BiometricBorder");

                Button? loginButton =
                    page.FindByName<Button>(
                        "LoginButton");

                ActivityIndicator? busyIndicator =
                    page.FindByName<ActivityIndicator>(
                        "BusyIndicator");

                Label? mobileFooter =
                    page.FindByName<Label>(
                        "MobileFooter");

                if (loginScrollView == null ||
                    responsiveRootGrid == null ||
                    loginContentStack == null ||
                    mobileHeader == null ||
                    mobileLogoBorder == null ||
                    mobileAppTitle == null ||
                    mobileTagline == null ||
                    loginCard == null ||
                    loginCardContent == null ||
                    loginHeaderGrid == null ||
                    welcomeTextStack == null ||
                    welcomeTitle == null ||
                    welcomeSubtitle == null ||
                    mascotStage == null ||
                    mascotGlow == null ||
                    mascotVisual == null ||
                    mascotImage == null ||
                    mascotSpeechText == null ||
                    privacyShield == null ||
                    privacyShieldText == null ||
                    userFieldStack == null ||
                    passwordFieldStack == null ||
                    rememberMeGrid == null ||
                    biometricBorder == null ||
                    loginButton == null ||
                    busyIndicator == null ||
                    mobileFooter == null)
                {
                    return false;
                }

                visual = new LoginPhoneVisualTree(
                    loginScrollView,
                    responsiveRootGrid,
                    loginContentStack,
                    mobileHeader,
                    mobileLogoBorder,
                    mobileAppTitle,
                    mobileTagline,
                    loginCard,
                    loginCardContent,
                    loginHeaderGrid,
                    welcomeTextStack,
                    welcomeTitle,
                    welcomeSubtitle,
                    mascotStage,
                    mascotGlow,
                    mascotVisual,
                    mascotImage,
                    mascotSpeechText,
                    privacyShield,
                    privacyShieldText,
                    userFieldStack,
                    passwordFieldStack,
                    rememberMeGrid,
                    biometricBorder,
                    loginButton,
                    busyIndicator,
                    mobileFooter);

                return true;
            }
        }

        private sealed record LoginPhoneVisualTree(
            ScrollView LoginScrollView,
            Grid ResponsiveRootGrid,
            VerticalStackLayout LoginContentStack,
            VerticalStackLayout MobileHeader,
            Border MobileLogoBorder,
            Label MobileAppTitle,
            Label MobileTagline,
            Border LoginCard,
            VerticalStackLayout LoginCardContent,
            Grid LoginHeaderGrid,
            VerticalStackLayout WelcomeTextStack,
            Label WelcomeTitle,
            Label WelcomeSubtitle,
            Grid MascotStage,
            Border MascotGlow,
            Grid MascotVisual,
            Image MascotImage,
            Label MascotSpeechText,
            Border PrivacyShield,
            Label PrivacyShieldText,
            VerticalStackLayout UserFieldStack,
            VerticalStackLayout PasswordFieldStack,
            Grid RememberMeGrid,
            Border BiometricBorder,
            Button LoginButton,
            ActivityIndicator BusyIndicator,
            Label MobileFooter);
    }
}
