using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace CONATRADEC.Behaviors;

/// <summary>
/// Registra comportamientos globales para cerrar el teclado cuando el
/// usuario finaliza un Entry desde el teclado o ejecuta una búsqueda.
///
/// El cierre al tocar fuera de un campo se controla globalmente desde
/// Platforms/Android/MainActivity.cs.
/// </summary>
public static class KeyboardDismissBehavior
{
    private const string EntryMapperKey =
        "CONATRADEC.KeyboardDismiss.Entry";

    private const string SearchBarMapperKey =
        "CONATRADEC.KeyboardDismiss.SearchBar";

    private static bool isRegistered;

    public static void Register()
    {
        if (isRegistered)
            return;

        isRegistered = true;

        EntryHandler.Mapper.AppendToMapping(
            EntryMapperKey,
            (_, virtualView) =>
            {
                if (virtualView is not Entry entry)
                    return;

                entry.Completed -= OnInputCompleted;
                entry.Completed += OnInputCompleted;
            });

        SearchBarHandler.Mapper.AppendToMapping(
            SearchBarMapperKey,
            (_, virtualView) =>
            {
                if (virtualView is not SearchBar searchBar)
                    return;

                searchBar.SearchButtonPressed -=
                    OnInputCompleted;

                searchBar.SearchButtonPressed +=
                    OnInputCompleted;
            });
    }

    private static async void OnInputCompleted(
        object? sender,
        EventArgs e)
    {
        await KeyboardService.HideAsync();
    }
}
