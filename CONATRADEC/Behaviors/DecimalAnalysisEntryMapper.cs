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
    /// Permite que únicamente los campos numéricos del flujo de análisis
    /// acepten tanto punto como coma como separador decimal en Android.
    ///
    /// Los campos de texto del mismo formulario, por ejemplo laboratorio e
    /// identificador del análisis, conservan el teclado alfanumérico normal.
    /// La normalización final continúa realizándose en los ViewModels, por lo
    /// que la API siempre recibe valores decimales válidos.
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
            /*
             * Antes se aplicaba la configuración numérica a TODOS los Entry
             * cuyo BindingContext pertenecía al formulario de análisis.
             * Eso convertía también Laboratorio e Identificador en campos
             * numéricos en Android.
             *
             * A partir de ahora solo se modifica el control nativo cuando el
             * propio XAML declaró explícitamente Keyboard="Numeric".
             */
            if (!EsCampoNumericoDelFlujoAnalisis(entry) ||
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

        private static bool EsCampoNumericoDelFlujoAnalisis(
            Entry entry)
        {
            bool perteneceAlFlujo =
                entry.BindingContext is NuevoAnalisisFormViewModel ||
                entry.BindingContext is ResultadoAnalisisItemViewModel;

            if (!perteneceAlFlujo)
                return false;

            /*
             * Keyboard="Numeric" se resuelve a Keyboard.Numeric.
             * Los Entry de texto mantienen Keyboard.Default/Text y no
             * deben recibir InputTypes.ClassNumber ni DigitsKeyListener.
             */
            return entry.Keyboard == Keyboard.Numeric;
        }
    }
}
