using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RolViewModel : GlobalService
    {
        private readonly RolApiService rolApiService;
        private ObservableCollection<RolResponse> list = new();

        public ObservableCollection<RolResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public RolViewModel()
        {
            rolApiService = new RolApiService();

            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<RolResponse>(OnEdit);
            DeleteCommand = new Command<RolResponse>(OnDelete);
            ViewCommand = new Command<RolResponse>(OnView);
        }

        public async Task LoadRol(bool isBusy)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver roles.");
                return;
            }

            IsBusy = isBusy;

            List.Clear();

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await rolApiService.GetRolAsync();

            if (response.Any())
                List = response;
            else
                await MostrarToastAsync("No se encontraron roles.");

            IsBusy = false;
        }

        private async Task OnAdd()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Rol", new RolRequest(new RolResponse()) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }

        private async void OnEdit(RolResponse rol)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (rol == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Rol", new RolRequest(rol) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }

        private async void OnDelete(RolResponse rol)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (rol == null) return;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar",
                $"¿Deseas eliminar el rol '{rol.NombreRol}'?",
                "Sí", "No");

            if (!confirm) return;

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                return;
            }

            var result = await rolApiService.DeleteRolAsync(new RolRequest(rol));

            if (result)
            {
                await MostrarToastAsync("Rol eliminado.");
                await LoadRol(true);
            }
            else
            {
                await MostrarToastAsync("No se pudo eliminar el rol.");
            }
        }

        private async void OnView(RolResponse rol)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Rol", new RolRequest(rol) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }
    }
}
