using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RolViewModel : GlobalService
    {
        private readonly RolApiService rolApiService;
        private ObservableCollection<RolResponse> list = new();
        private bool cargandoRoles;
        private bool eliminandoRol;

        public ObservableCollection<RolResponse> List
        {
            get => list;
            set
            {
                if (ReferenceEquals(list, value))
                    return;

                list = value;
                OnPropertyChanged();
            }
        }

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public RolViewModel()
            : this(new RolApiService())
        {
        }

        public RolViewModel(RolApiService rolApiService)
        {
            this.rolApiService = rolApiService
                ?? throw new ArgumentNullException(nameof(rolApiService));

            AddCommand = new Command(
                async () => await OnAddAsync());

            EditCommand = new Command<RolResponse>(
                async rol => await OnEditAsync(rol));

            DeleteCommand = new Command<RolResponse>(
                async rol => await OnDeleteAsync(rol));

            ViewCommand = new Command<RolResponse>(
                async rol => await OnViewAsync(rol));
        }

        public async Task LoadRol(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver roles.");
                return;
            }

            if (cargandoRoles)
                return;

            cargandoRoles = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await rolApiService.GetRolResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<RolResponse>(
                    (resultado.Data ?? new ObservableCollection<RolResponse>())
                    .OrderBy(x => x.NombreRol ?? string.Empty));

                if (List.Count == 0)
                    await MostrarToastAsync("No se encontraron roles.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los roles.");
            }
            finally
            {
                cargandoRoles = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private async Task OnAddAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar.");
                return;
            }

            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Rol", new RolRequest(new RolResponse()) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }

        private async Task OnEditAsync(RolResponse? rol)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (IsBusy || rol == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Rol", new RolRequest(rol) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }

        private async Task OnViewAsync(RolResponse? rol)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver.");
                return;
            }

            if (IsBusy || rol == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Rol", new RolRequest(rol) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }

        private async Task OnDeleteAsync(RolResponse? rol)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy || eliminandoRol || rol == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
                "Eliminar rol",
                $"¿Desea eliminar el rol '{rol.NombreRol}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            eliminandoRol = true;
            IsBusy = true;

            try
            {
                var resultado = await rolApiService.DeleteRolResultAsync(
                    new RolRequest(rol));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(rol);
                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Rol eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el rol.");
            }
            finally
            {
                eliminandoRol = false;
                IsBusy = false;
            }
        }
    }
}
