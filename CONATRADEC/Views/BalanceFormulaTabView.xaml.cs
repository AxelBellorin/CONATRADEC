using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Vista del Balance de fórmula.
    ///
    /// El XAML histórico utiliza SelectedItem. Durante una edición, WinUI puede
    /// conservar un objeto asociado al catálogo anterior y mostrar el Picker
    /// vacío, aunque el ViewModel tenga el ID correcto.
    ///
    /// Al crear cada Picker se elimina únicamente ese binding histórico y se
    /// instala un binding TwoWay por SelectedIndex. La fuente lógica continúa
    /// almacenada por ID en BalanceFormulaElementoViewModel.
    /// </summary>
    public partial class BalanceFormulaTabView : ContentView
    {
        private readonly HashSet<Picker>
            pickersConfigurados = new();

        private readonly Dictionary<
            Picker,
            BalanceFormulaElementoViewModel>
            contextosPicker = new();

        private CancellationTokenSource?
            sincronizacionCts;

        public BalanceFormulaTabView()
        {
            InitializeComponent();

            Loaded +=
                BalanceFormulaTabView_Loaded;

            Unloaded +=
                BalanceFormulaTabView_Unloaded;

            BindingContextChanged +=
                BalanceFormulaTabView_BindingContextChanged;
        }

        private void BalanceFormulaTabView_Loaded(
            object? sender,
            EventArgs e)
        {
            ProgramarSincronizacion();
        }

        private void BalanceFormulaTabView_Unloaded(
            object? sender,
            EventArgs e)
        {
            CancelarSincronizacion();
            DesvincularPickers();
        }

        private void
            BalanceFormulaTabView_BindingContextChanged(
                object? sender,
                EventArgs e)
        {
            ProgramarSincronizacion();
        }

        /// <summary>
        /// BindableLayout crea los controles después de recibir la colección.
        /// Se realizan varios pases breves para cubrir la carga inicial y las
        /// reconstrucciones tardías de una edición online u offline.
        /// </summary>
        private void ProgramarSincronizacion()
        {
            CancelarSincronizacion();

            var nuevaCts =
                new CancellationTokenSource();

            sincronizacionCts =
                nuevaCts;

            _ = SincronizarConReintentosAsync(
                nuevaCts.Token);
        }

        private async Task
            SincronizarConReintentosAsync(
                CancellationToken cancellationToken)
        {
            int[] esperas =
            [
                0,
                60,
                140,
                280,
                520,
                900,
                1500
            ];

            try
            {
                foreach (int espera in esperas)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (espera > 0)
                    {
                        await Task.Delay(
                            espera,
                            cancellationToken);
                    }

                    await MainThread
                        .InvokeOnMainThreadAsync(
                            ConfigurarPickersActuales);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ConfigurarPickersActuales()
        {
            if (Content == null)
                return;

            HashSet<Picker> pickersActuales =
                ObtenerVistas(Content)
                    .OfType<Picker>()
                    .ToHashSet();

            foreach (Picker picker in
                     pickersConfigurados
                         .Where(x =>
                             !pickersActuales.Contains(x))
                         .ToList())
            {
                DesvincularPicker(
                    picker);
            }

            foreach (Picker picker
                     in pickersActuales)
            {
                ConfigurarPicker(
                    picker);
            }
        }

        private void ConfigurarPicker(
            Picker picker)
        {
            if (pickersConfigurados.Add(
                    picker))
            {
                picker.BindingContextChanged +=
                    Picker_BindingContextChanged;
            }

            VincularContextoPicker(
                picker);

            AsegurarBindingPorIndice(
                picker);

            SincronizarPickerDesdeViewModel(
                picker);
        }

        private void Picker_BindingContextChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not Picker picker)
                return;

            VincularContextoPicker(
                picker);

            /*
             * El DataTemplate puede reutilizar el control con otro elemento.
             * Se reconstruye el binding para que tome el nuevo contexto.
             */
            AsegurarBindingPorIndice(
                picker,
                forzar: true);

            SincronizarPickerDesdeViewModel(
                picker);
        }

        private static void AsegurarBindingPorIndice(
            Picker picker,
            bool forzar = false)
        {
            /*
             * Elimina únicamente el binding SelectedItem declarado en el XAML.
             * ItemsSource, ItemDisplayBinding, estilos y demás propiedades se
             * conservan sin cambios.
             */
            picker.RemoveBinding(
                Picker.SelectedItemProperty);

            if (forzar)
            {
                picker.RemoveBinding(
                    Picker.SelectedIndexProperty);
            }

            picker.SetBinding(
                Picker.SelectedIndexProperty,
                new Binding(
                    nameof(
                        BalanceFormulaElementoViewModel
                            .FuenteSeleccionadaIndex),
                    BindingMode.TwoWay));
        }

        private void VincularContextoPicker(
            Picker picker)
        {
            if (contextosPicker.TryGetValue(
                    picker,
                    out BalanceFormulaElementoViewModel?
                        contextoAnterior))
            {
                if (ReferenceEquals(
                        contextoAnterior,
                        picker.BindingContext))
                {
                    return;
                }

                contextoAnterior.PropertyChanged -=
                    Elemento_PropertyChanged;

                contextosPicker.Remove(
                    picker);
            }

            if (picker.BindingContext is not
                BalanceFormulaElementoViewModel
                contextoNuevo)
            {
                return;
            }

            contextoNuevo.PropertyChanged +=
                Elemento_PropertyChanged;

            contextosPicker[picker] =
                contextoNuevo;
        }

        private void Elemento_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (sender is not
                BalanceFormulaElementoViewModel
                elemento)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(
                    e.PropertyName) &&
                e.PropertyName !=
                    nameof(
                        BalanceFormulaElementoViewModel
                            .FuenteSeleccionada) &&
                e.PropertyName !=
                    nameof(
                        BalanceFormulaElementoViewModel
                            .FuenteSeleccionadaIndex) &&
                e.PropertyName !=
                    nameof(
                        BalanceFormulaElementoViewModel
                            .FuentesDisponibles))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    foreach (
                        KeyValuePair<
                            Picker,
                            BalanceFormulaElementoViewModel>
                        item in contextosPicker
                            .ToList())
                    {
                        if (ReferenceEquals(
                                item.Value,
                                elemento))
                        {
                            SincronizarPickerDesdeViewModel(
                                item.Key);
                        }
                    }
                });
        }

        private void SincronizarPickerDesdeViewModel(
            Picker picker)
        {
            if (picker.BindingContext is not
                BalanceFormulaElementoViewModel
                elemento)
            {
                return;
            }

            int indiceEsperado =
                elemento.FuenteSeleccionadaIndex;

            if (elemento.FuenteSeleccionada != null)
            {
                int fuenteId =
                    elemento
                        .FuenteSeleccionada
                        .FuenteNutrientesId ??
                    0;

                int indicePorId =
                    ObtenerIndicePorId(
                        elemento,
                        fuenteId);

                if (indicePorId >= 0 &&
                    indicePorId != indiceEsperado)
                {
                    elemento.RestaurarFuentePorId(
                        fuenteId,
                        elemento.FuenteSeleccionada);

                    indiceEsperado =
                        elemento
                            .FuenteSeleccionadaIndex;
                }
            }

            if (EsSeleccionVisualCorrecta(
                    picker,
                    elemento,
                    indiceEsperado))
            {
                return;
            }

            /*
             * Se desconecta momentáneamente SelectedIndex antes de limpiar el
             * control. De esta forma el -1 temporal no llega al ViewModel, no
             * borra la fuente y no dispara un recálculo.
             */
            picker.RemoveBinding(
                Picker.SelectedIndexProperty);

            picker.SelectedIndex =
                -1;

            AsegurarBindingPorIndice(
                picker);

            /*
             * El nuevo binding toma FuenteSeleccionadaIndex. Este respaldo
             * cubre manejadores WinUI que terminan de actualizarse un ciclo
             * después de reconstruir el ItemsSource.
             */
            if (picker.SelectedIndex !=
                indiceEsperado)
            {
                picker.SelectedIndex =
                    indiceEsperado;
            }
        }

        private static bool
            EsSeleccionVisualCorrecta(
                Picker picker,
                BalanceFormulaElementoViewModel
                    elemento,
                int indiceEsperado)
        {
            if (picker.SelectedIndex !=
                indiceEsperado)
            {
                return false;
            }

            if (indiceEsperado < 0)
            {
                return elemento
                    .FuenteSeleccionada ==
                    null;
            }

            if (picker.SelectedItem is not
                FuenteNutrienteResponse
                fuenteVisual)
            {
                return false;
            }

            return fuenteVisual
                       .FuenteNutrientesId ==
                   elemento
                       .FuenteSeleccionada?
                       .FuenteNutrientesId;
        }

        private static int ObtenerIndicePorId(
            BalanceFormulaElementoViewModel
                elemento,
            int fuenteNutrientesId)
        {
            if (fuenteNutrientesId <= 0)
                return -1;

            for (int indice = 0;
                 indice <
                    elemento
                        .FuentesDisponibles
                        .Count;
                 indice++)
            {
                if (elemento
                        .FuentesDisponibles[
                            indice]
                        .FuenteNutrientesId ==
                    fuenteNutrientesId)
                {
                    return indice;
                }
            }

            return -1;
        }

        private static IEnumerable<View>
            ObtenerVistas(
                View vista)
        {
            yield return vista;

            if (vista is ContentView
                    contentView &&
                contentView.Content is View
                    contenidoContentView)
            {
                foreach (View descendiente
                         in ObtenerVistas(
                             contenidoContentView))
                {
                    yield return descendiente;
                }

                yield break;
            }

            if (vista is Border border &&
                border.Content is View
                    contenidoBorder)
            {
                foreach (View descendiente
                         in ObtenerVistas(
                             contenidoBorder))
                {
                    yield return descendiente;
                }

                yield break;
            }

            if (vista is ScrollView
                    scrollView &&
                scrollView.Content is View
                    contenidoScroll)
            {
                foreach (View descendiente
                         in ObtenerVistas(
                             contenidoScroll))
                {
                    yield return descendiente;
                }

                yield break;
            }

            if (vista is Layout layout)
            {
                foreach (Microsoft.Maui.IView
                         hijo in layout.Children)
                {
                    if (hijo is not View
                        vistaHija)
                    {
                        continue;
                    }

                    foreach (View descendiente
                             in ObtenerVistas(
                                 vistaHija))
                    {
                        yield return descendiente;
                    }
                }
            }
        }

        private void DesvincularPicker(
            Picker picker)
        {
            picker.BindingContextChanged -=
                Picker_BindingContextChanged;

            if (contextosPicker.TryGetValue(
                    picker,
                    out BalanceFormulaElementoViewModel?
                        contexto))
            {
                contexto.PropertyChanged -=
                    Elemento_PropertyChanged;

                contextosPicker.Remove(
                    picker);
            }

            pickersConfigurados.Remove(
                picker);
        }

        private void DesvincularPickers()
        {
            foreach (Picker picker
                     in pickersConfigurados
                         .ToList())
            {
                DesvincularPicker(
                    picker);
            }

            contextosPicker.Clear();
        }

        private void CancelarSincronizacion()
        {
            CancellationTokenSource? actual =
                Interlocked.Exchange(
                    ref sincronizacionCts,
                    null);

            if (actual == null)
                return;

            try
            {
                actual.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                actual.Dispose();
            }
        }
    }
}
