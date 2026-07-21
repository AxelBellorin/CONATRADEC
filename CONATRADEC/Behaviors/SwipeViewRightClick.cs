using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace CONATRADEC.Behaviors;

/// <summary>
/// Agrega un menú contextual nativo de Windows a todos los SwipeView.
///
/// En Windows, el clic derecho muestra las mismas acciones declaradas en
/// RightItems, LeftItems, BottomItems o TopItems, incluyendo texto, icono,
/// Command y CommandParameter.
///
/// En Android y las demás plataformas no modifica el swipe táctil.
/// </summary>
public static class SwipeViewRightClick
{
    private const string MapperKey = "CONATRADEC.SwipeViewRightClick";

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            propertyName: "IsEnabled",
            returnType: typeof(bool),
            declaringType: typeof(SwipeViewRightClick),
            defaultValue: true);

    public static bool GetIsEnabled(BindableObject bindable)
    {
        return (bool)bindable.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(BindableObject bindable, bool value)
    {
        bindable.SetValue(IsEnabledProperty, value);
    }

#if WINDOWS
    private static readonly ConditionalWeakTable<UIElement, object>
        RegisteredNativeViews = new();

    private static bool _isRegistered;
#endif

    /// <summary>
    /// Registra una sola vez la personalización global de SwipeView.
    /// Se llama desde MauiProgram.CreateMauiApp antes de builder.Build().
    /// </summary>
    public static void Register()
    {
#if WINDOWS
        if (_isRegistered)
            return;

        _isRegistered = true;

        SwipeViewHandler.Mapper.AppendToMapping(
            MapperKey,
            (handler, virtualView) =>
            {
                if (handler.PlatformView is not UIElement nativeView)
                    return;

                if (virtualView is not Microsoft.Maui.Controls.SwipeView swipeView)
                    return;

                if (RegisteredNativeViews.TryGetValue(nativeView, out _))
                    return;

                RegisteredNativeViews.Add(nativeView, new object());

                nativeView.IsRightTapEnabled = true;

                RightTappedEventHandler rightTappedHandler = (_, args) =>
                {
                    ShowContextMenu(nativeView, swipeView, args);
                };

                nativeView.AddHandler(
                    UIElement.RightTappedEvent,
                    rightTappedHandler,
                    handledEventsToo: true);
            });
#endif
    }

#if WINDOWS
    private static void ShowContextMenu(
        UIElement nativeView,
        Microsoft.Maui.Controls.SwipeView swipeView,
        RightTappedRoutedEventArgs args)
    {
        if (!GetIsEnabled(swipeView) || !swipeView.IsEnabled)
            return;

        Microsoft.Maui.Controls.SwipeItems? sourceItems =
            GetAvailableItems(swipeView);

        if (sourceItems is null || sourceItems.Count == 0)
            return;

        var menuFlyout =
            new Microsoft.UI.Xaml.Controls.MenuFlyout();

        foreach (Microsoft.Maui.Controls.SwipeItem swipeItem
                 in sourceItems.OfType<Microsoft.Maui.Controls.SwipeItem>())
        {
            if (!swipeItem.IsVisible)
                continue;

            var command = swipeItem.Command;
            var commandParameter = swipeItem.CommandParameter;

            var menuItem =
                new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = string.IsNullOrWhiteSpace(swipeItem.Text)
                        ? "Acción"
                        : swipeItem.Text,

                    IsEnabled = command is null ||
                                command.CanExecute(commandParameter)
                };

            menuItem.Icon = CreateNativeIcon(
                swipeItem.IconImageSource);

            menuItem.Click += (_, _) =>
            {
                if (command is null)
                    return;

                if (command.CanExecute(commandParameter))
                    command.Execute(commandParameter);
            };

            menuFlyout.Items.Add(menuItem);
        }

        if (menuFlyout.Items.Count == 0)
            return;

        args.Handled = true;

        menuFlyout.ShowAt(
            nativeView,
            args.GetPosition(nativeView));
    }

    /// <summary>
    /// Convierte un FileImageSource de MAUI en un ImageIcon nativo de WinUI.
    /// </summary>
    private static Microsoft.UI.Xaml.Controls.IconElement?
        CreateNativeIcon(
            Microsoft.Maui.Controls.ImageSource? imageSource)
    {
        if (imageSource is not FileImageSource fileImageSource)
            return null;

        if (string.IsNullOrWhiteSpace(fileImageSource.File))
            return null;

        string imagePath = fileImageSource.File
            .Replace('\\', '/')
            .TrimStart('/');

        try
        {
            var bitmapImage = new BitmapImage(
                new Uri(
                    $"ms-appx:///{imagePath}",
                    UriKind.Absolute));

            return new Microsoft.UI.Xaml.Controls.ImageIcon
            {
                Source = bitmapImage,
                Width = 18,
                Height = 18
            };
        }
        catch
        {
            return null;
        }
    }

    private static Microsoft.Maui.Controls.SwipeItems?
        GetAvailableItems(
            Microsoft.Maui.Controls.SwipeView swipeView)
    {
        if (swipeView.RightItems is { Count: > 0 })
            return swipeView.RightItems;

        if (swipeView.LeftItems is { Count: > 0 })
            return swipeView.LeftItems;

        if (swipeView.BottomItems is { Count: > 0 })
            return swipeView.BottomItems;

        if (swipeView.TopItems is { Count: > 0 })
            return swipeView.TopItems;

        return null;
    }
#endif
}
