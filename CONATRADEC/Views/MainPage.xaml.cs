using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();

        private bool ordenandoAnalisis;
        private bool ordenProgramado;

        public MainPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();
            BindingContext = viewModel;

            viewModel.AnalisisGuardados.CollectionChanged +=
                AnalisisGuardados_CollectionChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");
            viewModel.PrepararPantalla();

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para ver la pantalla principal.",
                    "Aceptar");

                return;
            }

            // La consulta se ejecuta únicamente al presionar el botón.
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            /*
             * No se elimina la suscripción porque este ViewModel pertenece
             * a esta página y se reutiliza cuando se vuelve a mostrar.
             */
        }

        private async void OnListarAnalisisClicked(
            object? sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.CargarAnalisisAsync(true);

            ProgramarOrdenamiento();
        }

        private void AnalisisGuardados_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            /*
             * No se puede usar Move directamente dentro de este evento.
             * ObservableCollection lanza:
             *
             * Cannot change ObservableCollection during a
             * CollectionChanged event.
             *
             * Por eso el ordenamiento se envía a la siguiente ejecución
             * del Dispatcher, cuando el evento actual ya terminó.
             */
            ProgramarOrdenamiento();
        }

        private void ProgramarOrdenamiento()
        {
            if (ordenandoAnalisis ||
                ordenProgramado)
            {
                return;
            }

            ordenProgramado = true;

            Dispatcher.Dispatch(() =>
            {
                ordenProgramado = false;
                OrdenarAnalisis();
            });
        }

        private void OrdenarAnalisis()
        {
            if (ordenandoAnalisis ||
                viewModel.AnalisisGuardados.Count <= 1)
            {
                return;
            }

            List<AnalisisGuardadoResumen> ordenados =
                viewModel.AnalisisGuardados
                    .OrderByDescending(
                        ObtenerFechaPrincipal)
                    .ThenBy(
                        x => x.ClienteMostrar,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenByDescending(
                        x => x.FechaCalculoValor ??
                             DateTime.MinValue)
                    .ThenBy(
                        x => x.IdentificadorMostrar,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            bool yaEstaOrdenado =
                viewModel.AnalisisGuardados
                    .Select((item, indice) =>
                        ReferenceEquals(
                            item,
                            ordenados[indice]))
                    .All(x => x);

            if (yaEstaOrdenado)
                return;

            ordenandoAnalisis = true;

            try
            {
                for (int indiceDestino = 0;
                     indiceDestino < ordenados.Count;
                     indiceDestino++)
                {
                    AnalisisGuardadoResumen item =
                        ordenados[indiceDestino];

                    int indiceActual =
                        viewModel.AnalisisGuardados
                            .IndexOf(item);

                    if (indiceActual < 0 ||
                        indiceActual == indiceDestino)
                    {
                        continue;
                    }

                    viewModel.AnalisisGuardados.Move(
                        indiceActual,
                        indiceDestino);
                }
            }
            finally
            {
                ordenandoAnalisis = false;
            }
        }

        private static DateTime ObtenerFechaPrincipal(
            AnalisisGuardadoResumen analisis)
        {
            DateTime fecha =
                analisis.FechaAnalisisValor
                ??
                analisis.FechaCalculoValor
                ??
                DateTime.MinValue;

            /*
             * Se usa solamente la fecha para que los análisis hechos
             * el mismo día se ordenen después por nombre del productor.
             */
            return fecha.Date;
        }
    }
}