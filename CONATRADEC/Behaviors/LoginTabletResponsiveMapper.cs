using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Handlers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Adapta automáticamente loginPage para tabletas Android.
    ///
    /// No modifica LoginViewModel ni la lógica de autenticación.
    /// Tampoco reemplaza loginPage.xaml o loginPage.xaml.cs.
    ///
    /// Teléfono:
    ///     conserva el diseño responsive existente.
    ///
    /// Tableta:
    ///     muestra el panel institucional a la izquierda y el formulario
    ///     a la derecha, aprovechando todo el ancho disponible.
    ///
    /// Windows:
    ///     conserva exactamente el diseño existente.
    /// </summary>
    internal static class LoginTabletResponsiveMapper
    {
        private const string MapperKey =
            "CONATRADEC.LoginTabletResponsive";

        private static readonly ConditionalWeakTable<
            loginPage,
            LoginResponsiveState> States = new();

        private static bool isRegistered;

        /// <summary>
        /// Registra una sola vez la adaptación del login.
        ///
        /// Se llama explícitamente desde MauiProgram.CreateMauiApp(),
        /// cuando MAUI ya ha inicializado su infraestructura.
        ///
        /// El código del mapper solo se compila y ejecuta en Android,
        /// porque esta adaptación está destinada a tabletas Android.
        /// </summary>
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
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
                return;

            LoginResponsiveState state =
                States.GetValue(
                    page,
                    static currentPage =>
                        new LoginResponsiveState(currentPage));

            state.Attach();
        }

        private sealed class LoginResponsiveState
        {
            private readonly loginPage page;

            private bool attached;
            private bool tabletLayoutWasApplied;
            private TabletOrientation? currentOrientation;

            public LoginResponsiveState(loginPage page)
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
                 * loginPage ejecuta su adaptación móvil en OnAppearing.
                 * Se reaplica el modo tableta justo después para que el
                 * diseño final sea el de dos paneles.
                 */
                page.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(80),
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
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    return;

                double width = page.Width;
                double height = page.Height;

                if (width <= 0 ||
                    height <= 0)
                {
                    return;
                }

                bool useTabletLayout =
                    Math.Min(width, height) >= 600;

                if (!useTabletLayout)
                {
                    RestorePhoneStructure();
                    return;
                }

                TabletOrientation orientation =
                    width > height
                        ? TabletOrientation.Landscape
                        : TabletOrientation.Portrait;

                /*
                 * Aunque la orientación no cambie, se vuelven a aplicar
                 * los valores. Esto evita que el OnAppearing original del
                 * login deje nuevamente tamaños de teléfono.
                 */
                currentOrientation = orientation;
                tabletLayoutWasApplied = true;

                ApplyTabletLayout(orientation);
            }

            private void ApplyTabletLayout(
                TabletOrientation orientation)
            {
                if (!TryResolveVisualTree(
                        out LoginVisualTree visual))
                {
                    return;
                }

                bool landscape =
                    orientation ==
                    TabletOrientation.Landscape;

                visual.RootGrid.ColumnDefinitions[0].Width =
                    new GridLength(
                        landscape ? 0.46 : 0.40,
                        GridUnitType.Star);

                visual.RootGrid.ColumnDefinitions[1].Width =
                    new GridLength(
                        landscape ? 0.54 : 0.60,
                        GridUnitType.Star);

                visual.InstitutionalPanel.IsVisible = true;
                visual.MobileHeader.IsVisible = false;
                visual.MobileFooter.IsVisible = false;

                ApplyInstitutionalPanel(
                    visual,
                    landscape);

                ApplyTabletForm(
                    visual,
                    landscape);
            }

            private static void ApplyInstitutionalPanel(
                LoginVisualTree visual,
                bool landscape)
            {
                visual.InstitutionalPanel.Margin =
                    landscape
                        ? new Thickness(20)
                        : new Thickness(14);

                visual.InstitutionalPanel.Padding =
                    landscape
                        ? new Thickness(34)
                        : new Thickness(22);

                visual.InstitutionalContent.MaximumWidthRequest =
                    landscape ? 470 : 330;

                visual.InstitutionalContent.Spacing =
                    landscape ? 22 : 15;

                if (visual.InstitutionalLogo != null)
                {
                    double logoSize =
                        landscape ? 116 : 92;

                    visual.InstitutionalLogo.WidthRequest =
                        logoSize;

                    visual.InstitutionalLogo.HeightRequest =
                        logoSize;

                    visual.InstitutionalLogo.Padding =
                        landscape
                            ? new Thickness(7)
                            : new Thickness(5);
                }

                if (visual.InstitutionalTitle != null)
                {
                    visual.InstitutionalTitle.FontSize =
                        landscape ? 39 : 30;

                    visual.InstitutionalTitle.LineBreakMode =
                        LineBreakMode.WordWrap;
                }

                if (visual.InstitutionalSubtitle != null)
                {
                    visual.InstitutionalSubtitle.FontSize =
                        landscape ? 16 : 12.5;

                    visual.InstitutionalSubtitle.MaximumWidthRequest =
                        landscape ? 440 : 300;

                    visual.InstitutionalSubtitle.LineHeight =
                        1.18;
                }

                if (visual.InstitutionalFeatures != null)
                {
                    visual.InstitutionalFeatures.Spacing =
                        landscape ? 13 : 9;

                    foreach (Grid featureRow
                             in visual.InstitutionalFeatures
                                 .Children
                                 .OfType<Grid>())
                    {
                        Label? featureLabel =
                            featureRow.Children
                                .OfType<Label>()
                                .FirstOrDefault();

                        if (featureLabel != null)
                        {
                            featureLabel.FontSize =
                                landscape ? 14 : 11.5;

                            featureLabel.LineBreakMode =
                                LineBreakMode.WordWrap;
                        }

                        Border? icon =
                            featureRow.Children
                                .OfType<Border>()
                                .FirstOrDefault();

                        if (icon != null)
                        {
                            double iconSize =
                                landscape ? 36 : 31;

                            icon.WidthRequest = iconSize;
                            icon.HeightRequest = iconSize;
                        }
                    }
                }

                if (visual.InstitutionalFooter != null)
                {
                    visual.InstitutionalFooter.FontSize =
                        landscape ? 11 : 9.5;

                    visual.InstitutionalFooter.HorizontalTextAlignment =
                        TextAlignment.Center;
                }
            }

            private static void ApplyTabletForm(
                LoginVisualTree visual,
                bool landscape)
            {
                visual.LoginScrollView.HorizontalOptions =
                    LayoutOptions.Fill;

                visual.LoginScrollView.VerticalOptions =
                    LayoutOptions.Fill;

                visual.ResponsiveRootGrid.Padding =
                    landscape
                        ? new Thickness(28, 24)
                        : new Thickness(18, 24);

                visual.LoginContentStack.MaximumWidthRequest =
                    landscape ? 520 : 470;

                visual.LoginContentStack.HorizontalOptions =
                    LayoutOptions.Fill;

                visual.LoginContentStack.VerticalOptions =
                    LayoutOptions.Center;

                visual.LoginContentStack.Spacing =
                    landscape ? 16 : 13;

                visual.LoginCard.HorizontalOptions =
                    LayoutOptions.Fill;

                visual.LoginCard.Padding =
                    landscape
                        ? new Thickness(28, 25)
                        : new Thickness(21, 20);

                visual.LoginCardContent.Spacing =
                    landscape ? 15 : 13;

                visual.LoginHeaderGrid.ColumnSpacing =
                    landscape ? 10 : 7;

                visual.WelcomeTextStack.Spacing =
                    landscape ? 4 : 3;

                visual.WelcomeTitle.FontSize =
                    landscape ? 30 : 27;

                visual.WelcomeSubtitle.FontSize =
                    landscape ? 13 : 12;

                visual.WelcomeSubtitle.MaximumWidthRequest =
                    landscape ? 300 : 255;

                ApplyMascotSize(
                    visual,
                    stageWidth: landscape ? 132 : 112,
                    stageHeight: landscape ? 122 : 104,
                    glowWidth: landscape ? 100 : 84,
                    glowHeight: landscape ? 92 : 77,
                    visualSize: landscape ? 104 : 88,
                    imageSize: landscape ? 100 : 84,
                    speechWidth: landscape ? 108 : 93,
                    speechFontSize: landscape ? 9.2 : 8.4,
                    privacyWidth: landscape ? 70 : 59,
                    privacyHeight: landscape ? 26 : 22,
                    privacyTop: landscape ? 25 : 20,
                    privacyFontSize: landscape ? 7 : 6.4);

                visual.UserFieldStack.Spacing =
                    landscape ? 6 : 5;

                visual.PasswordFieldStack.Spacing =
                    landscape ? 6 : 5;

                visual.RememberMeGrid.ColumnSpacing =
                    landscape ? 8 : 6;

                visual.BiometricBorder.Padding =
                    landscape
                        ? new Thickness(12)
                        : new Thickness(10);

                visual.LoginButton.HeightRequest =
                    landscape ? 56 : 54;

                visual.BusyIndicator.HeightRequest =
                    landscape ? 30 : 27;
            }

            private void RestorePhoneStructure()
            {
                if (!tabletLayoutWasApplied)
                    return;

                if (!TryResolveVisualTree(
                        out LoginVisualTree visual))
                {
                    return;
                }

                visual.RootGrid.ColumnDefinitions[0].Width =
                    new GridLength(0);

                visual.RootGrid.ColumnDefinitions[1].Width =
                    GridLength.Star;

                visual.InstitutionalPanel.IsVisible = false;
                visual.MobileHeader.IsVisible = true;

                visual.LoginContentStack.MaximumWidthRequest = 500;
                visual.LoginContentStack.HorizontalOptions =
                    LayoutOptions.Center;

                /*
                 * El manejador responsive original de loginPage conserva
                 * el resto de tamaños para teléfonos.
                 */
                tabletLayoutWasApplied = false;
                currentOrientation = null;
            }

            private bool TryResolveVisualTree(
                out LoginVisualTree visual)
            {
                visual = default!;

                if (page.Content is not Grid rootGrid ||
                    rootGrid.ColumnDefinitions.Count < 2)
                {
                    return false;
                }

                Border? institutionalPanel =
                    rootGrid.Children
                        .OfType<Border>()
                        .FirstOrDefault(item =>
                            Grid.GetColumn(item) == 0);

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

                if (institutionalPanel == null ||
                    loginScrollView == null ||
                    responsiveRootGrid == null ||
                    loginContentStack == null ||
                    mobileHeader == null ||
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

                if (institutionalPanel.Content
                    is not Grid institutionalGrid)
                {
                    return false;
                }

                VerticalStackLayout? institutionalContent =
                    institutionalGrid.Children
                        .OfType<VerticalStackLayout>()
                        .FirstOrDefault(item =>
                            Grid.GetRow(item) == 0);

                Label? institutionalFooter =
                    institutionalGrid.Children
                        .OfType<Label>()
                        .FirstOrDefault(item =>
                            Grid.GetRow(item) == 1);

                if (institutionalContent == null)
                    return false;

                Grid? logoHost =
                    institutionalContent.Children
                        .OfType<Grid>()
                        .FirstOrDefault();

                Border? institutionalLogo =
                    logoHost?.Children
                        .OfType<Border>()
                        .FirstOrDefault();

                List<VerticalStackLayout> directStacks =
                    institutionalContent.Children
                        .OfType<VerticalStackLayout>()
                        .ToList();

                VerticalStackLayout? titleStack =
                    directStacks.FirstOrDefault(
                        stack =>
                            stack.Children
                                .OfType<Label>()
                                .Any(label =>
                                    string.Equals(
                                        label.Text,
                                        "ConatraCafé Soil",
                                        StringComparison.Ordinal)));

                VerticalStackLayout? featureStack =
                    directStacks.FirstOrDefault(
                        stack =>
                            stack.Children
                                .OfType<Grid>()
                                .Count() >= 2);

                Label? institutionalTitle =
                    titleStack?.Children
                        .OfType<Label>()
                        .FirstOrDefault();

                Label? institutionalSubtitle =
                    titleStack?.Children
                        .OfType<Label>()
                        .Skip(1)
                        .FirstOrDefault();

                visual =
                    new LoginVisualTree(
                        rootGrid,
                        institutionalPanel,
                        institutionalContent,
                        institutionalLogo,
                        institutionalTitle,
                        institutionalSubtitle,
                        featureStack,
                        institutionalFooter,
                        loginScrollView,
                        responsiveRootGrid,
                        loginContentStack,
                        mobileHeader,
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

            private static void ApplyMascotSize(
                LoginVisualTree visual,
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

                visual.PrivacyShield.WidthRequest =
                    privacyWidth;

                visual.PrivacyShield.HeightRequest =
                    privacyHeight;

                visual.PrivacyShield.Margin =
                    new Thickness(
                        0,
                        privacyTop,
                        0,
                        0);

                visual.PrivacyShieldText.FontSize =
                    privacyFontSize;
            }
        }

        private enum TabletOrientation
        {
            Portrait,
            Landscape
        }

        private sealed record LoginVisualTree(
            Grid RootGrid,
            Border InstitutionalPanel,
            VerticalStackLayout InstitutionalContent,
            Border? InstitutionalLogo,
            Label? InstitutionalTitle,
            Label? InstitutionalSubtitle,
            VerticalStackLayout? InstitutionalFeatures,
            Label? InstitutionalFooter,
            ScrollView LoginScrollView,
            Grid ResponsiveRootGrid,
            VerticalStackLayout LoginContentStack,
            VerticalStackLayout MobileHeader,
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