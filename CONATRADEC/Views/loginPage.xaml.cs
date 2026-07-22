using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using System.ComponentModel;
using System.Linq;

namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
    public static string appName = "ConatraCafé Soil";

    private readonly LoginViewModel _viewModel;

    private CancellationTokenSource? _idleAnimationCts;

    private bool _isInteracting;
    private bool _isCelebrating;
    private bool _shellEventAttached;
    private bool _pageIsVisible;
    private bool _isTogglingPasswordVisibility;

    private int _tapPhraseIndex;
    private DateTime _lastTypingAnimation = DateTime.MinValue;

    private MobileLayoutMode? _currentMobileLayoutMode;

    private readonly string[] _tapPhrases =
    {
        "¡Hola!",
        "¡Aquí estoy!",
        "¡Vamos al cafetal!",
        "¡Tu aliado cafetero!",
        "¡Listos para trabajar!"
    };

    public loginPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

        InitializeComponent();

        _viewModel = new LoginViewModel();
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void UserNameEntryCompleted(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private async void PasswordEntryCompleted(object sender, EventArgs e)
    {
        if (!ValidateVisualFields())
            return;

        await PlayVerifyingReactionAsync();

        if (_viewModel.LoginCommand.CanExecute(null))
            _viewModel.LoginCommand.Execute(null);
    }

    /// <summary>
    /// Cambia la visibilidad de la contraseña y sincroniza la reacción
    /// de la mascota. Cuando la contraseña está oculta, la mascota usa
    /// el antifaz; cuando está visible, lo retira.
    /// </summary>
    private async void PasswordToggleButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_viewModel.IsBusy || _isTogglingPasswordVisibility)
            return;

        _isTogglingPasswordVisibility = true;

        try
        {
            _viewModel.OnTogglePassword();

            // Espera brevemente a que el Entry nativo actualice
            // correctamente la propiedad IsPassword.
            await Task.Delay(80);

            PasswordEntry.Focus();
            _isInteracting = true;

            if (_viewModel.IsPasswordHidden)
            {
                await ShowSpeechAsync(
                    "Tu contraseña está oculta",
                    SpeechMood.Private);

                await ShowPrivacyShieldAsync();
            }
            else
            {
                await HidePrivacyShieldAsync();

                await ShowSpeechAsync(
                    "Ahora puedes revisar tu contraseña",
                    SpeechMood.Normal);

                CancelMascotAnimations();

                await Task.WhenAll(
                    MascotVisual.TranslateTo(
                        -4,
                        0,
                        160,
                        Easing.CubicOut),

                    MascotVisual.RotateTo(
                        -4,
                        160,
                        Easing.CubicOut),

                    MascotVisual.ScaleTo(
                        1.03,
                        160,
                        Easing.CubicOut)
                );
            }
        }
        finally
        {
            await Task.Delay(160);
            _isTogglingPasswordVisibility = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _pageIsVisible = true;
        AttachShellNavigationEvent();

        await Task.Delay(100);
        await _viewModel.LoadSavedAsync();

        ApplyResponsiveMobileLayout(Width, Height);
        StartIdleAnimation();
    }

    protected override void OnDisappearing()
    {
        _pageIsVisible = false;

        DetachShellNavigationEvent();
        StopIdleAnimation();
        CancelMascotAnimations();

        base.OnDisappearing();
    }

    // =============================================================
    // DISEÑO RESPONSIVE EXCLUSIVO PARA ANDROID
    // =============================================================

    private enum MobileLayoutMode
    {
        Compact,
        Standard,
        Tall
    }

    private void LoginPage_SizeChanged(
        object? sender,
        EventArgs e)
    {
        ApplyResponsiveMobileLayout(Width, Height);
    }

    /// <summary>
    /// Ajusta únicamente la presentación móvil según la altura lógica
    /// disponible. Windows conserva exactamente su diseño original.
    /// El ScrollView se mantiene como respaldo para pantallas pequeñas,
    /// teclado abierto y tamaños de fuente ampliados.
    /// </summary>
    private void ApplyResponsiveMobileLayout(
        double width,
        double height)
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI ||
            width <= 0 ||
            height <= 0)
        {
            return;
        }

        MobileLayoutMode mode = height switch
        {
            < 720 => MobileLayoutMode.Compact,
            < 900 => MobileLayoutMode.Standard,
            _ => MobileLayoutMode.Tall
        };

        if (_currentMobileLayoutMode == mode)
            return;

        _currentMobileLayoutMode = mode;
        LoginContentStack.VerticalOptions = LayoutOptions.Start;

        switch (mode)
        {
            case MobileLayoutMode.Compact:
                ApplyCompactMobileLayout();
                break;

            case MobileLayoutMode.Standard:
                ApplyStandardMobileLayout();
                break;

            default:
                ApplyTallMobileLayout();
                break;
        }
    }

    /// <summary>
    /// Para teléfonos con poca altura útil. No reduce las zonas táctiles
    /// esenciales; compacta logo, mascota, márgenes y espacios.
    /// </summary>
    private void ApplyCompactMobileLayout()
    {
        ResponsiveRootGrid.Padding = new Thickness(12, 6);
        LoginContentStack.Spacing = 8;

        MobileHeader.Spacing = 2;
        MobileHeader.Margin = new Thickness(0, 0, 0, 1);
        MobileLogoBorder.WidthRequest = 50;
        MobileLogoBorder.HeightRequest = 50;
        MobileLogoBorder.Padding = new Thickness(3);
        MobileAppTitle.FontSize = 20;
        MobileTagline.FontSize = 9;
        MobileTagline.IsVisible = true;

        LoginCard.Padding = new Thickness(14, 12);
        LoginCardContent.Spacing = 10;
        LoginHeaderGrid.ColumnSpacing = 4;
        WelcomeTextStack.Spacing = 2;
        WelcomeTitle.FontSize = 23;
        WelcomeSubtitle.FontSize = 11;
        WelcomeSubtitle.MaximumWidthRequest = 245;

        ApplyMascotSize(
            stageWidth: 94,
            stageHeight: 86,
            glowWidth: 70,
            glowHeight: 64,
            visualSize: 74,
            imageSize: 70,
            speechWidth: 78,
            speechFontSize: 7.5,
            privacyWidth: 50,
            privacyHeight: 20,
            privacyTop: 17,
            privacyFontSize: 6);

        UserFieldStack.Spacing = 4;
        PasswordFieldStack.Spacing = 4;
        RememberMeGrid.ColumnSpacing = 5;
        BiometricBorder.Padding = new Thickness(9);

        LoginButton.HeightRequest = 50;
        BusyIndicator.HeightRequest = 24;
        MobileFooter.IsVisible = false;
    }

    /// <summary>
    /// Presentación principal para la mayoría de teléfonos actuales en
    /// orientación vertical.
    /// </summary>
    private void ApplyStandardMobileLayout()
    {
        ResponsiveRootGrid.Padding = new Thickness(16, 10);
        LoginContentStack.Spacing = 10;

        MobileHeader.Spacing = 4;
        MobileHeader.Margin = new Thickness(0, 0, 0, 2);
        MobileLogoBorder.WidthRequest = 64;
        MobileLogoBorder.HeightRequest = 64;
        MobileLogoBorder.Padding = new Thickness(4);
        MobileAppTitle.FontSize = 22;
        MobileTagline.FontSize = 10;
        MobileTagline.IsVisible = true;

        LoginCard.Padding = new Thickness(18, 16);
        LoginCardContent.Spacing = 12;
        LoginHeaderGrid.ColumnSpacing = 6;
        WelcomeTextStack.Spacing = 3;
        WelcomeTitle.FontSize = 25;
        WelcomeSubtitle.FontSize = 12;
        WelcomeSubtitle.MaximumWidthRequest = 270;

        ApplyMascotSize(
            stageWidth: 110,
            stageHeight: 100,
            glowWidth: 82,
            glowHeight: 75,
            visualSize: 86,
            imageSize: 82,
            speechWidth: 92,
            speechFontSize: 8.5,
            privacyWidth: 58,
            privacyHeight: 22,
            privacyTop: 20,
            privacyFontSize: 6.5);

        UserFieldStack.Spacing = 5;
        PasswordFieldStack.Spacing = 5;
        RememberMeGrid.ColumnSpacing = 6;
        BiometricBorder.Padding = new Thickness(10);

        LoginButton.HeightRequest = 50;
        BusyIndicator.HeightRequest = 26;
        MobileFooter.IsVisible = false;
    }

    /// <summary>
    /// Aprovecha el espacio extra en teléfonos muy altos y tabletas sin
    /// agrandar excesivamente los campos.
    /// </summary>
    private void ApplyTallMobileLayout()
    {
        ResponsiveRootGrid.Padding = new Thickness(18, 18);
        LoginContentStack.Spacing = 16;

        MobileHeader.Spacing = 6;
        MobileHeader.Margin = new Thickness(0, 2, 0, 4);
        MobileLogoBorder.WidthRequest = 76;
        MobileLogoBorder.HeightRequest = 76;
        MobileLogoBorder.Padding = new Thickness(5);
        MobileAppTitle.FontSize = 25;
        MobileTagline.FontSize = 11;
        MobileTagline.IsVisible = true;

        LoginCard.Padding = new Thickness(22, 22);
        LoginCardContent.Spacing = 16;
        LoginHeaderGrid.ColumnSpacing = 8;
        WelcomeTextStack.Spacing = 4;
        WelcomeTitle.FontSize = 27;
        WelcomeSubtitle.FontSize = 14;
        WelcomeSubtitle.MaximumWidthRequest = 285;

        ApplyMascotSize(
            stageWidth: 126,
            stageHeight: 118,
            glowWidth: 96,
            glowHeight: 88,
            visualSize: 100,
            imageSize: 96,
            speechWidth: 104,
            speechFontSize: 9,
            privacyWidth: 68,
            privacyHeight: 25,
            privacyTop: 24,
            privacyFontSize: 7);

        UserFieldStack.Spacing = 7;
        PasswordFieldStack.Spacing = 7;
        RememberMeGrid.ColumnSpacing = 8;
        BiometricBorder.Padding = new Thickness(13);

        LoginButton.HeightRequest = 54;
        BusyIndicator.HeightRequest = 30;
        MobileFooter.IsVisible = true;
    }

    private void ApplyMascotSize(
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
        MascotStage.WidthRequest = stageWidth;
        MascotStage.HeightRequest = stageHeight;

        MascotGlow.WidthRequest = glowWidth;
        MascotGlow.HeightRequest = glowHeight;

        MascotVisual.WidthRequest = visualSize;
        MascotVisual.HeightRequest = visualSize;

        MascotImage.WidthRequest = imageSize;
        MascotImage.HeightRequest = imageSize;

        MascotSpeechText.MaximumWidthRequest = speechWidth;
        MascotSpeechText.FontSize = speechFontSize;

        PrivacyShield.WidthRequest = privacyWidth;
        PrivacyShield.HeightRequest = privacyHeight;
        PrivacyShield.Margin = new Thickness(0, privacyTop, 0, 0);
        PrivacyShieldText.FontSize = privacyFontSize;
    }

    private void AttachShellNavigationEvent()
    {
        if (_shellEventAttached)
            return;

        Shell.Current.Navigating += Shell_Navigating;
        _shellEventAttached = true;
    }

    private void DetachShellNavigationEvent()
    {
        if (!_shellEventAttached)
            return;

        Shell.Current.Navigating -= Shell_Navigating;
        _shellEventAttached = false;
    }

    /// <summary>
    /// Retrasa únicamente la navegación exitosa a MainPage para permitir
    /// que la mascota celebre. No cambia la lógica del LoginViewModel.
    /// </summary>
    private async void Shell_Navigating(
        object? sender,
        ShellNavigatingEventArgs e)
    {
        if (!_pageIsVisible ||
            _isCelebrating ||
            !IsMainPageNavigation(e))
        {
            return;
        }

        ShellNavigatingDeferral? deferral = e.GetDeferral();

        if (deferral is null)
            return;

        _isCelebrating = true;
        _isInteracting = true;

        try
        {
            string displayName =
                _viewModel.User?.NombreCompletoUsuario?.Trim() ??
                _viewModel.Username?.Trim() ??
                string.Empty;

            await PlaySuccessCelebrationAsync(displayName);
        }
        finally
        {
            _isCelebrating = false;
            deferral.Complete();
        }
    }

    private static bool IsMainPageNavigation(
        ShellNavigatingEventArgs e)
    {
        string target =
            e.Target?.Location?.OriginalString ??
            string.Empty;

        return target.Contains(
            "MainPage",
            StringComparison.OrdinalIgnoreCase);
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (!_pageIsVisible)
            return;

        if (e.PropertyName == nameof(LoginViewModel.Message))
        {
            string message = _viewModel.Message?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message) ||
                message.StartsWith(
                    "Bienvenido",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    await PlayErrorReactionAsync(
                        GetFriendlyErrorPhrase(message));
                });
        }
    }

    private static string GetFriendlyErrorPhrase(string message)
    {
        if (message.Contains(
                "incorrect",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Revisa tu usuario y contraseña";
        }

        if (message.Contains(
                "conex",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "servidor",
                StringComparison.OrdinalIgnoreCase))
        {
            return "No pude conectar con el servidor";
        }

        if (message.Contains(
                "tard",
                StringComparison.OrdinalIgnoreCase))
        {
            return "La conexión está tardando mucho";
        }

        return "Algo salió mal. Inténtalo otra vez";
    }

    // =============================================================
    // INTERACCIÓN CON LOS CAMPOS
    // =============================================================

    private async void UserNameEntry_Focused(
        object sender,
        FocusEventArgs e)
    {
        _isInteracting = true;

        UserBorder.Stroke = new SolidColorBrush(Color.FromArgb("#3B655B"));
        UserBorder.StrokeThickness = 1.5;

        await HidePrivacyShieldAsync();
        await ShowSpeechAsync(
            "Te ayudo con tu usuario",
            SpeechMood.Normal);

        CancelMascotAnimations();

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                -7,
                0,
                180,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                -5,
                180,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1.04,
                180,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1.07,
                180,
                Easing.CubicOut)
        );
    }

    private async void PasswordEntry_Focused(
        object sender,
        FocusEventArgs e)
    {
        _isInteracting = true;

        PasswordBorder.Stroke =
            new SolidColorBrush(Color.FromArgb("#3B655B"));

        PasswordBorder.StrokeThickness = 1.5;

        CancelMascotAnimations();

        if (_viewModel.IsPasswordHidden)
        {
            await ShowSpeechAsync(
                "No miraré tu contraseña",
                SpeechMood.Private);

            await Task.WhenAll(
                MascotVisual.TranslateTo(
                    5,
                    0,
                    180,
                    Easing.CubicOut),

                MascotVisual.RotateTo(
                    7,
                    180,
                    Easing.CubicOut),

                MascotVisual.ScaleTo(
                    1.02,
                    180,
                    Easing.CubicOut),

                ShowPrivacyShieldAsync()
            );
        }
        else
        {
            await HidePrivacyShieldAsync();

            await ShowSpeechAsync(
                "La contraseña está visible",
                SpeechMood.Normal);

            await Task.WhenAll(
                MascotVisual.TranslateTo(
                    -4,
                    0,
                    180,
                    Easing.CubicOut),

                MascotVisual.RotateTo(
                    -4,
                    180,
                    Easing.CubicOut),

                MascotVisual.ScaleTo(
                    1.03,
                    180,
                    Easing.CubicOut)
            );
        }
    }

    private async void InputEntry_Unfocused(
        object sender,
        FocusEventArgs e)
    {
        await Task.Delay(100);

        if (_isTogglingPasswordVisibility ||
            UserNameEntry.IsFocused ||
            PasswordEntry.IsFocused)
        {
            return;
        }

        UserBorder.Stroke = new SolidColorBrush(Color.FromArgb("#D6E1DC"));
        UserBorder.StrokeThickness = 1;

        PasswordBorder.Stroke = new SolidColorBrush(Color.FromArgb("#D6E1DC"));
        PasswordBorder.StrokeThickness = 1;

        _isInteracting = false;

        await HidePrivacyShieldAsync();
        await RestoreMascotPoseAsync();
        await ShowSpeechAsync(
            "¡Bienvenido!",
            SpeechMood.Normal);
    }

    private void UserNameEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (!UserNameEntry.IsFocused)
            return;

        PlayTypingTick(isPassword: false);
    }

    private void PasswordEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (!PasswordEntry.IsFocused)
            return;

        PlayTypingTick(isPassword: true);
    }

    private void PlayTypingTick(bool isPassword)
    {
        DateTime now = DateTime.UtcNow;

        if ((now - _lastTypingAnimation).TotalMilliseconds < 90)
            return;

        _lastTypingAnimation = now;

        MainThread.BeginInvokeOnMainThread(
            async () =>
            {
                if (isPassword)
                {
                    if (_viewModel.IsPasswordHidden)
                    {
                        PrivacyShield.CancelAnimations();

                        await PrivacyShield.ScaleTo(
                            1.06,
                            70,
                            Easing.CubicOut);

                        await PrivacyShield.ScaleTo(
                            1,
                            90,
                            Easing.CubicIn);
                    }
                    else
                    {
                        MascotVisual.CancelAnimations();

                        await MascotVisual.ScaleTo(
                            1.04,
                            70,
                            Easing.CubicOut);

                        await MascotVisual.ScaleTo(
                            1.03,
                            90,
                            Easing.CubicIn);
                    }
                }
                else
                {
                    MascotVisual.CancelAnimations();

                    await MascotVisual.RotateTo(
                        -7,
                        65,
                        Easing.CubicOut);

                    await MascotVisual.RotateTo(
                        -5,
                        85,
                        Easing.CubicIn);
                }
            });
    }

    // =============================================================
    // BOTONES Y VALIDACIONES VISUALES
    // =============================================================

    private async void LoginButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (!ValidateVisualFields())
            return;

        await PlayVerifyingReactionAsync();
    }

    private async void BiometricButton_Clicked(
        object sender,
        EventArgs e)
    {
        _isInteracting = true;

        await HidePrivacyShieldAsync();

        await ShowSpeechAsync(
            "Confirma tu identidad",
            SpeechMood.Normal);

        await Task.WhenAll(
            MascotVisual.ScaleTo(
                1.08,
                140,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1.12,
                140,
                Easing.CubicOut)
        );

        await Task.WhenAll(
            MascotVisual.ScaleTo(
                1,
                180,
                Easing.BounceOut),

            MascotGlow.ScaleTo(
                1,
                180,
                Easing.CubicOut)
        );

        _isInteracting = false;
    }

    private bool ValidateVisualFields()
    {
        if (string.IsNullOrWhiteSpace(UserNameEntry.Text))
        {
            _ = PlayErrorReactionAsync(
                "Primero escribe tu usuario");

            UserNameEntry.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            _ = PlayErrorReactionAsync(
                "Ahora escribe tu contraseña");

            PasswordEntry.Focus();
            return false;
        }

        if (PasswordEntry.Text.Length < 4)
        {
            _ = PlayErrorReactionAsync(
                "La contraseña es muy corta");

            PasswordEntry.Focus();
            return false;
        }

        return true;
    }

    private async Task PlayVerifyingReactionAsync()
    {
        _isInteracting = true;

        await HidePrivacyShieldAsync();

        await ShowSpeechAsync(
            "Verificando tus datos...",
            SpeechMood.Normal);

        CancelMascotAnimations();

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                -9,
                130,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1.09,
                130,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                4,
                130,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1.12,
                130,
                Easing.CubicOut),

            LoginButton.ScaleTo(
                0.98,
                100,
                Easing.CubicOut)
        );

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                0,
                220,
                Easing.BounceOut),

            MascotVisual.ScaleTo(
                1,
                220,
                Easing.BounceOut),

            MascotVisual.RotateTo(
                0,
                220,
                Easing.BounceOut),

            MascotGlow.ScaleTo(
                1,
                220,
                Easing.CubicOut),

            LoginButton.ScaleTo(
                1,
                160,
                Easing.CubicOut)
        );

        _isInteracting = false;
    }

    // =============================================================
    // PRIVACIDAD DE LA CONTRASEÑA
    // =============================================================

    private async Task ShowPrivacyShieldAsync()
    {
        PrivacyShield.IsVisible = true;
        PrivacyShield.CancelAnimations();

        PrivacyShield.Opacity = 0;
        PrivacyShield.Scale = 0.65;
        PrivacyShield.TranslationX = 35;

        await Task.WhenAll(
            PrivacyShield.FadeTo(
                1,
                150,
                Easing.CubicOut),

            PrivacyShield.ScaleTo(
                1,
                190,
                Easing.SpringOut),

            PrivacyShield.TranslateTo(
                0,
                0,
                180,
                Easing.CubicOut)
        );
    }

    private async Task HidePrivacyShieldAsync()
    {
        if (PrivacyShield.Opacity <= 0)
            return;

        PrivacyShield.CancelAnimations();

        await Task.WhenAll(
            PrivacyShield.FadeTo(
                0,
                120,
                Easing.CubicIn),

            PrivacyShield.ScaleTo(
                0.70,
                120,
                Easing.CubicIn),

            PrivacyShield.TranslateTo(
                28,
                0,
                120,
                Easing.CubicIn)
        );
    }

    // =============================================================
    // RESPUESTAS DE LA MASCOTA
    // =============================================================

    private async void MascotTapped(
        object sender,
        TappedEventArgs e)
    {
        if (_isCelebrating)
            return;

        _tapPhraseIndex++;

        if (_tapPhraseIndex >= _tapPhrases.Length)
            _tapPhraseIndex = 0;

        _isInteracting = true;

        await HidePrivacyShieldAsync();

        await ShowSpeechAsync(
            _tapPhrases[_tapPhraseIndex],
            SpeechMood.Normal);

        CancelMascotAnimations();

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                -11,
                120,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1.10,
                120,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                9,
                120,
                Easing.CubicOut)
        );

        await MascotVisual.RotateTo(
            -9,
            120,
            Easing.CubicInOut);

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                0,
                220,
                Easing.BounceOut),

            MascotVisual.ScaleTo(
                1,
                220,
                Easing.BounceOut),

            MascotVisual.RotateTo(
                0,
                220,
                Easing.BounceOut)
        );

        _isInteracting =
            UserNameEntry.IsFocused ||
            PasswordEntry.IsFocused;
    }

    private async Task PlayErrorReactionAsync(string phrase)
    {
        if (_isCelebrating || !_pageIsVisible)
            return;

        _isInteracting = true;

        await ShowSpeechAsync(
            phrase,
            SpeechMood.Error);

        CancelMascotAnimations();

        await MascotStage.TranslateTo(
            -8,
            0,
            60,
            Easing.Linear);

        await MascotStage.TranslateTo(
            8,
            0,
            70,
            Easing.Linear);

        await MascotStage.TranslateTo(
            -6,
            0,
            70,
            Easing.Linear);

        await MascotStage.TranslateTo(
            6,
            0,
            70,
            Easing.Linear);

        await MascotStage.TranslateTo(
            0,
            0,
            90,
            Easing.CubicOut);

        await Task.WhenAll(
            MascotVisual.ScaleTo(
                0.96,
                100,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                -4,
                100,
                Easing.CubicOut)
        );

        await Task.WhenAll(
            MascotVisual.ScaleTo(
                1,
                180,
                Easing.BounceOut),

            MascotVisual.RotateTo(
                0,
                180,
                Easing.BounceOut)
        );

        _isInteracting =
            UserNameEntry.IsFocused ||
            PasswordEntry.IsFocused;
    }

    private async Task PlaySuccessCelebrationAsync(
        string displayName)
    {
        StopIdleAnimation();
        CancelMascotAnimations();

        await HidePrivacyShieldAsync();

        string firstName =
            GetFirstName(displayName);

        string phrase = string.IsNullOrWhiteSpace(firstName)
            ? "¡Inicio de sesión exitoso!"
            : $"¡Bienvenido, {firstName}!";

        await ShowSpeechAsync(
            phrase,
            SpeechMood.Success);

        PrepareConfetti();

        Task confettiTask = PlayConfettiAsync();

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                -18,
                150,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1.16,
                150,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                10,
                150,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1.20,
                150,
                Easing.CubicOut),

            MascotGlow.FadeTo(
                1,
                150,
                Easing.CubicOut),

            LoginButton.ScaleTo(
                1.03,
                150,
                Easing.CubicOut)
        );

        await MascotVisual.RotateTo(
            -10,
            120,
            Easing.CubicInOut);

        await MascotVisual.RotateTo(
            8,
            110,
            Easing.CubicInOut);

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                0,
                260,
                Easing.BounceOut),

            MascotVisual.ScaleTo(
                1,
                260,
                Easing.BounceOut),

            MascotVisual.RotateTo(
                0,
                260,
                Easing.BounceOut),

            MascotGlow.ScaleTo(
                1,
                260,
                Easing.CubicOut),

            LoginButton.ScaleTo(
                1,
                220,
                Easing.CubicOut),

            confettiTask
        );

        await Task.Delay(220);
    }

    private static string GetFirstName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return string.Empty;

        return displayName
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ??
            string.Empty;
    }

    // =============================================================
    // CONFETI
    // =============================================================

    private void PrepareConfetti()
    {
        foreach (VisualElement particle in GetConfettiParticles())
        {
            particle.CancelAnimations();
            particle.Opacity = 0;
            particle.Scale = 0.45;
            particle.Rotation = 0;
            particle.TranslationX = 0;
            particle.TranslationY = 0;
        }
    }

    private async Task PlayConfettiAsync()
    {
        VisualElement[] particles =
            GetConfettiParticles();

        double[] x =
        {
            -42,
            42,
            -50,
            50,
            -34,
            34
        };

        double[] y =
        {
            -48,
            -45,
            -10,
            -8,
            35,
            34
        };

        double[] rotation =
        {
            -160,
            170,
            -120,
            140,
            -190,
            200
        };

        Task[] tasks = new Task[particles.Length];

        for (int i = 0; i < particles.Length; i++)
        {
            tasks[i] = AnimateConfettiParticleAsync(
                particles[i],
                x[i],
                y[i],
                rotation[i],
                i * 35);
        }

        await Task.WhenAll(tasks);
    }

    private static async Task AnimateConfettiParticleAsync(
        VisualElement particle,
        double x,
        double y,
        double rotation,
        int delay)
    {
        if (delay > 0)
            await Task.Delay(delay);

        await Task.WhenAll(
            particle.FadeTo(
                1,
                80,
                Easing.CubicOut),

            particle.ScaleTo(
                1,
                100,
                Easing.CubicOut)
        );

        await Task.WhenAll(
            particle.TranslateTo(
                x,
                y,
                430,
                Easing.CubicOut),

            particle.RotateTo(
                rotation,
                430,
                Easing.CubicOut)
        );

        await particle.FadeTo(
            0,
            170,
            Easing.CubicIn);
    }

    private VisualElement[] GetConfettiParticles() =>
        new VisualElement[]
        {
            Confetti1,
            Confetti2,
            Confetti3,
            Confetti4,
            Confetti5,
            Confetti6
        };

    // =============================================================
    // GLOBO DE TEXTO
    // =============================================================

    private enum SpeechMood
    {
        Normal,
        Private,
        Error,
        Success
    }

    private async Task ShowSpeechAsync(
        string text,
        SpeechMood mood)
    {
        MascotSpeechBubble.CancelAnimations();

        await MascotSpeechBubble.FadeTo(
            0.15,
            80,
            Easing.CubicIn);

        MascotSpeechText.Text = text;
        ApplySpeechMood(mood);

        await Task.WhenAll(
            MascotSpeechBubble.FadeTo(
                1,
                140,
                Easing.CubicOut),

            MascotSpeechBubble.ScaleTo(
                1.05,
                140,
                Easing.CubicOut)
        );

        await MascotSpeechBubble.ScaleTo(
            1,
            120,
            Easing.BounceOut);
    }

    private void ApplySpeechMood(SpeechMood mood)
    {
        switch (mood)
        {
            case SpeechMood.Private:
                MascotSpeechBubble.BackgroundColor =
                    Color.FromArgb("#EAF3EF");

                MascotSpeechBubble.Stroke =
                    new SolidColorBrush(
                        Color.FromArgb("#3B655B"));

                MascotSpeechText.TextColor =
                    Color.FromArgb("#24483F");
                break;

            case SpeechMood.Error:
                MascotSpeechBubble.BackgroundColor =
                    Color.FromArgb("#FFF0EE");

                MascotSpeechBubble.Stroke =
                    new SolidColorBrush(
                        Color.FromArgb("#D65B4A"));

                MascotSpeechText.TextColor =
                    Color.FromArgb("#9A3025");
                break;

            case SpeechMood.Success:
                MascotSpeechBubble.BackgroundColor =
                    Color.FromArgb("#F3F9E9");

                MascotSpeechBubble.Stroke =
                    new SolidColorBrush(
                        Color.FromArgb("#7AB648"));

                MascotSpeechText.TextColor =
                    Color.FromArgb("#2F5A3A");
                break;

            default:
                MascotSpeechBubble.BackgroundColor =
                    Color.FromArgb("#FFF8D9");

                MascotSpeechBubble.Stroke =
                    new SolidColorBrush(
                        Color.FromArgb("#F2C94C"));

                MascotSpeechText.TextColor =
                    Color.FromArgb("#3B655B");
                break;
        }
    }

    // =============================================================
    // ANIMACIÓN DE REPOSO
    // =============================================================

    private void StartIdleAnimation()
    {
        StopIdleAnimation();

        _idleAnimationCts =
            new CancellationTokenSource();

        PrepareEntranceState();

        _ = RunIdleAnimationAsync(
            _idleAnimationCts.Token);
    }

    private void StopIdleAnimation()
    {
        if (_idleAnimationCts is null)
            return;

        _idleAnimationCts.Cancel();
        _idleAnimationCts.Dispose();
        _idleAnimationCts = null;
    }

    private void PrepareEntranceState()
    {
        CancelMascotAnimations();

        MascotStage.Opacity = 1;
        MascotVisual.Opacity = 0;
        MascotVisual.Scale = 0.82;
        MascotVisual.TranslationY = 18;
        MascotVisual.Rotation = -5;

        MascotGlow.Opacity = 0;
        MascotGlow.Scale = 0.88;

        MascotSpeechBubble.Opacity = 0;
        MascotSpeechBubble.Scale = 0.86;
        MascotSpeechBubble.TranslationY = 5;

        ApplySpeechMood(SpeechMood.Normal);
        MascotSpeechText.Text = "¡Bienvenido!";
    }

    private async Task RunIdleAnimationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await PlayEntranceAnimationAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isInteracting ||
                    _isCelebrating ||
                    _viewModel.IsBusy)
                {
                    await Task.Delay(
                        100,
                        cancellationToken);

                    continue;
                }

                await Task.WhenAll(
                    MascotVisual.TranslateTo(
                        0,
                        -5,
                        1250,
                        Easing.SinInOut),

                    MascotVisual.RotateTo(
                        2,
                        1250,
                        Easing.SinInOut),

                    MascotGlow.ScaleTo(
                        1.05,
                        1250,
                        Easing.SinInOut),

                    MascotGlow.FadeTo(
                        0.96,
                        1250,
                        Easing.SinInOut)
                );

                if (cancellationToken.IsCancellationRequested)
                    return;

                await Task.WhenAll(
                    MascotVisual.TranslateTo(
                        0,
                        4,
                        1250,
                        Easing.SinInOut),

                    MascotVisual.RotateTo(
                        -2,
                        1250,
                        Easing.SinInOut),

                    MascotGlow.ScaleTo(
                        0.98,
                        1250,
                        Easing.SinInOut),

                    MascotGlow.FadeTo(
                        0.82,
                        1250,
                        Easing.SinInOut)
                );
            }
        }
        catch (OperationCanceledException)
        {
            // La animación se detuvo al salir de la página.
        }
        catch (ObjectDisposedException)
        {
            // La página ya fue liberada.
        }
    }

    private async Task PlayEntranceAnimationAsync()
    {
        await Task.WhenAll(
            MascotVisual.FadeTo(
                1,
                320,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1,
                390,
                Easing.SpringOut),

            MascotVisual.TranslateTo(
                0,
                0,
                320,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                0,
                320,
                Easing.CubicOut),

            MascotGlow.FadeTo(
                0.88,
                280,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1,
                280,
                Easing.CubicOut),

            MascotSpeechBubble.FadeTo(
                1,
                250,
                Easing.CubicOut),

            MascotSpeechBubble.ScaleTo(
                1,
                300,
                Easing.SpringOut),

            MascotSpeechBubble.TranslateTo(
                0,
                0,
                250,
                Easing.CubicOut)
        );
    }

    private async Task RestoreMascotPoseAsync()
    {
        CancelMascotAnimations();

        await Task.WhenAll(
            MascotVisual.TranslateTo(
                0,
                0,
                190,
                Easing.CubicOut),

            MascotVisual.RotateTo(
                0,
                190,
                Easing.CubicOut),

            MascotVisual.ScaleTo(
                1,
                190,
                Easing.CubicOut),

            MascotGlow.ScaleTo(
                1,
                190,
                Easing.CubicOut),

            MascotGlow.FadeTo(
                0.88,
                190,
                Easing.CubicOut)
        );
    }

    private void CancelMascotAnimations()
    {
        MascotStage.CancelAnimations();
        MascotVisual.CancelAnimations();
        MascotGlow.CancelAnimations();
        MascotSpeechBubble.CancelAnimations();
        PrivacyShield.CancelAnimations();
    }
}