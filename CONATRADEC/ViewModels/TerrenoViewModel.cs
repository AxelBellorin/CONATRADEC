using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class TerrenoViewModel : GlobalService
    {
        private ObservableCollection<TerrenoResponse> list = new();
        private readonly TerrenoApiService terrenoApiService = new();

        public ObservableCollection<TerrenoResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public TerrenoViewModel()
        {
            AddCommand = new Command(async () =>
            {
                if (!CanAdd)
                {
                    await MostrarToastAsync("No tiene permisos para agregar.");
                    return;
                }
                await OnAdd();
            });

            EditCommand = new Command<TerrenoResponse>(async (item) =>
            {
                if (!CanEdit)
                {
                    await MostrarToastAsync("No tiene permisos para editar.");
                    return;
                }
                OnEdit(item);
            });

            DeleteCommand = new Command<TerrenoResponse>(async (item) =>
            {
                if (!CanDelete)
                {
                    await MostrarToastAsync("No tiene permisos para eliminar.");
                    return;
                }
                OnDelete(item);
            });

            ViewCommand = new Command<TerrenoResponse>(async (item) =>
            {
                if (!CanView)
                {
                    await MostrarToastAsync("No tiene permisos para ver detalles.");
                    return;
                }
                OnView(item);
            });
        }

        public async Task LoadTerrenosAsync(bool isBusy)
        {
            if (!CanView)
                return;

            IsBusy = isBusy;
            List.Clear();

            if (!await TieneInternetAsync())
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await terrenoApiService.GetTerrenosAsync();

            List = response.Count != 0 ? response : new ObservableCollection<TerrenoResponse>();

            if (response.Count == 0)
                _ = MostrarToastAsync("No se encontraron terrenos.");

            IsBusy = false;
        }

        private async Task OnAdd()
        {
            if (IsBusy) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Terreno", new TerrenoRequest(new TerrenoResponse()) }
            };

            await GoToAsyncParameters("//TerrenoFormPage", parameters);
        }

        private async void OnEdit(TerrenoResponse item)
        {
            if (IsBusy || item == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Terreno", new TerrenoRequest(item) }
            };

            await GoToAsyncParameters("//TerrenoFormPage", parameters);
        }

        private async void OnDelete(TerrenoResponse item)
        {
            if (IsBusy || item == null) return;

            IsBusy = true;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar",
                $"¿Desea eliminar el terreno {item.CodigoTerreno}?",
                "Sí", "No");

            if (!confirm)
            {
                IsBusy = false;
                return;
            }

            if (!await TieneInternetAsync())
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var result = await terrenoApiService.DeleteTerrenoAsync(new TerrenoRequest(item));

            if (result)
            {
                _ = MostrarToastAsync("Terreno eliminado.");
                await LoadTerrenosAsync(true);
            }
            else
            {
                _ = MostrarToastAsync("No se pudo eliminar el terreno.");
            }

            IsBusy = false;
        }

        private async void OnView(TerrenoResponse item)
        {
            if (IsBusy || item == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Terreno", new TerrenoRequest(item) }
            };

            await GoToAsyncParameters("//TerrenoFormPage", parameters);
        }
    }
}
