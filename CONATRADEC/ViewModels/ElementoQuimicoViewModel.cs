using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class ElementoQuimicoViewModel : GlobalService
    {
        private ObservableCollection<ElementoQuimicoResponse> list = new();
        private readonly ElementoQuimicoApiService elementoApiService;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public ObservableCollection<ElementoQuimicoResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public ElementoQuimicoViewModel()
        {
            elementoApiService = new ElementoQuimicoApiService();

            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<ElementoQuimicoResponse>(OnEdit);
            DeleteCommand = new Command<ElementoQuimicoResponse>(OnDelete);
            ViewCommand = new Command<ElementoQuimicoResponse>(OnView);
        }

        public async Task LoadElementoQuimico(bool isBusy)
        {
            IsBusy = isBusy;

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await elementoApiService.GetElementoQuimicoAsync();

            List.Clear();

            if (response.Count > 0)
            {
                foreach (var item in response)
                    List.Add(item);
            }
            else
            {
                _ = MostrarToastAsync("No se encontraron elementos químicos.");
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
                    { "ElementoQuimico", new ElementoQuimicoRequest(new ElementoQuimicoResponse()) }
                };

                await GoToAsyncParameters("//ElementoQuimicoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"No se pudo conectar al servidor {ex}");
            }
        }

        private async void OnEdit(ElementoQuimicoResponse elemento)
        {
            if (IsBusy || elemento == null) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "ElementoQuimico", new ElementoQuimicoRequest(elemento) }
                };

                await GoToAsyncParameters("//ElementoQuimicoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        private async void OnDelete(ElementoQuimicoResponse elemento)
        {
            if (IsBusy || elemento == null) return;

            IsBusy = true;
            try
            {
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar el elemento {elemento.NombreElementoQuimico}?",
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

                    var response = await elementoApiService.DeleteElementoQuimicoAsync(
                        new ElementoQuimicoRequest(elemento));

                    if (response)
                    {
                        _ = MostrarToastAsync("Éxito\nElemento químico eliminado correctamente");
                        await LoadElementoQuimico(IsBusy);
                    }
                    else
                    {
                        _ = MostrarToastAsync("Error\nEl elemento no se pudo eliminar, intente nuevamente");
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

        private async void OnView(ElementoQuimicoResponse elemento)
        {
            if (IsBusy || elemento == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "ElementoQuimico", new ElementoQuimicoRequest(elemento) }
            };

            await GoToAsyncParameters("//ElementoQuimicoFormPage", parameters);
        }
    }
}
