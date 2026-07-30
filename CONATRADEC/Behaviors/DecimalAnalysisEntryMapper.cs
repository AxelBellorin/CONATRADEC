using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System;

#if ANDROID
using Android.Text;
using Android.Text.Method;
#endif

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Permite que los campos numéricos del flujo de análisis acepten
    /// tanto punto como coma como separador decimal en Android.
    ///
    /// La normalización final continúa realizándose en los ViewModels,
    /// por lo que la API siempre recibe valores decimales válidos.
    /// </summary>
    public static class DecimalAnalysisEntryMapper
    {
        private const string MappingName =
            "CONATRADEC.DecimalAnalysisEntry";

        private static bool registrado;

        public static void Register()
        {
            if (registrado)
                return;

            registrado = true;

            EntryHandler.Mapper.AppendToMapping(
                MappingName,
                (_, virtualView) =>
                {
                    if (virtualView is not Entry entry)
                        return;

                    /*
                     * Algunos Entry reciben el BindingContext después de
                     * crear el control nativo, especialmente los generados
                     * dentro de BindableLayout. Se vuelve a aplicar cuando
                     * cambie el contexto para cubrir ambos casos.
                     */
                    entry.BindingContextChanged -=
                        OnBindingContextChanged;

                    entry.BindingContextChanged +=
                        OnBindingContextChanged;

                    AplicarConfiguracion(entry);
                });
        }

        private static void OnBindingContextChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is Entry entry)
                AplicarConfiguracion(entry);
        }

        private static void AplicarConfiguracion(
            Entry entry)
        {
#if ANDROID
            if (!EsCampoDelFlujoAnalisis(entry) ||
                entry.Handler is not EntryHandler handler)
            {
                return;
            }

            Android.Widget.EditText control =
                handler.PlatformView;

            control.InputType =
                InputTypes.ClassNumber |
                InputTypes.NumberFlagDecimal;

            /*
             * Android utiliza normalmente un único separador según
             * el idioma del dispositivo. Este KeyListener permite
             * capturar ambos caracteres sin cambiar la cultura del
             * teléfono ni la lógica de los demás formularios.
             */
            control.KeyListener =
                DigitsKeyListener.GetInstance(
                    "0123456789.,");
#endif
        }

        private static bool EsCampoDelFlujoAnalisis(
            Entry entry) =>
            entry.BindingContext is NuevoAnalisisFormViewModel ||
            entry.BindingContext is ResultadoAnalisisItemViewModel;
    }
}
