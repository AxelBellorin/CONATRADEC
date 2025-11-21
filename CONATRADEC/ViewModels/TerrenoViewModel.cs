using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class TerrenoViewModel : GlobalService
    {
        private ObservableCollection<TerrenoResponse> list = new();
        private readonly TerrenoApiService terrenoApiService = new();

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public ObservableCollection<TerrenoResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public TerrenoViewModel()
        {
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<TerrenoResponse>(OnEdit);
            DeleteCommand = new Command<TerrenoResponse>(OnDelete);
            ViewCommand = new Command<TerrenoResponse>(OnView);
        }

        public async Task LoadTerrenosAsync(bool isBusy)
        {
            IsBusy = isBusy;
            List.Clear();

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await terrenoApiService.GetTerrenosAsync();

            if (response.Count != 0)
            {
                List = response;
            }
            else
            {
                _ = MostrarToastAsync("Información" + "No se encontraron terrenos.");
            }

            IsBusy = false;
        }

        private async Task OnAdd()
        {
            if (IsBusy) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },
                    { "Terreno", new TerrenoRequest(new TerrenoResponse()) }
                };

                await GoToAsyncParameters("//TerrenoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"No se pudo conectar al servidor {ex}");
            }
        }

        private async void OnEdit(TerrenoResponse terreno)
        {
            if (IsBusy || terreno == null) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Terreno", new TerrenoRequest(terreno) }
                };

                await GoToAsyncParameters("//TerrenoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        private async void OnDelete(TerrenoResponse terreno)
        {
            if (IsBusy || terreno == null) return;

            IsBusy = true;
            try
            {
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar el terreno {terreno.CodigoTerreno}?",
                    "Sí",
                    "No");

                if (confirm)
                {
                    bool tieneInternet = await TieneInternetAsync();
                    if (!tieneInternet)
                    {
                        _ = MostrarToastAsync("Sin conexión a internet.");
                        IsBusy = false;
                        return;
                    }

                    var response = await terrenoApiService.DeleteTerrenoAsync(
                        new TerrenoRequest(terreno));

                    if (response)
                    {
                        _ = MostrarToastAsync("Éxito\nTerreno eliminado correctamente");
                        await LoadTerrenosAsync(IsBusy);
                    }
                    else
                    {
                        _ = MostrarToastAsync("Error\nEl terreno no se pudo eliminar, intente nuevamente");
                    }
                }
                else
                {
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        private async void OnView(TerrenoResponse terreno)
        {
            if (IsBusy || terreno == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Terreno", new TerrenoRequest(terreno) }
            };

            await GoToAsyncParameters("//TerrenoFormPage", parameters);
        }
    }
}
