using CONATRADEC;
using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

public class DepartamentoViewModel : GlobalService
{
    private PaisRequest paisRequest;
    private string titlePage;

    private ObservableCollection<DepartamentoResponse> list = new();
    public ObservableCollection<DepartamentoResponse> List
    {
        get => list;
        set { list = value; OnPropertyChanged(); }
    }

    private readonly DepartamentoApiService departamentoApiService;

    public Command ReturnCommand { get; }
    public Command AddCommand { get; }
    public Command EditCommand { get; }
    public Command DeleteCommand { get; }
    public Command ViewCommand { get; }

    public PaisRequest PaisRequest
    {
        get => paisRequest;
        set { paisRequest = value; OnPropertyChanged(); }
    }

    public string TitlePage
    {
        get => titlePage;
        set { titlePage = value; OnPropertyChanged(); }
    }

    public DepartamentoViewModel()
    {
        departamentoApiService = new DepartamentoApiService();

        ReturnCommand = new Command(async () => await GoToAsyncParameters("//PaisPage"));
        AddCommand = new Command(async () => await OnAdd());
        EditCommand = new Command<DepartamentoResponse>(OnEdit);
        DeleteCommand = new Command<DepartamentoResponse>(OnDelete);
        ViewCommand = new Command<DepartamentoResponse>(OnView);

        //ApplyPermissions();
    }

    public async Task LoadDepartamento(bool isBusy)
    {
        if (!CanView)
        {
            await MostrarToastAsync("No tiene permisos para ver departamentos.");
            return;
        }

        IsBusy = isBusy;
        try
        {
            List.Clear();

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await departamentoApiService.GetDepartamentosAsync(PaisRequest.PaisId);

            if (response.Any())
                List = response;
            else
                _ = MostrarToastAsync("No se encontraron departamentos.");
        }
        catch (Exception ex)
        {
            _ = MostrarToastAsync("Error " + ex.Message);
        }
        finally { IsBusy = false; }
    }

    private async Task OnAdd()
    {
        if (!CanAdd)
        {
            await MostrarToastAsync("No tiene permisos para agregar.");
            return;
        }

        if (IsBusy) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Pais", PaisRequest},
                { "Departamento", new DepartamentoRequest(new DepartamentoResponse()) }
            };
            await GoToAsyncParameters("//DepartamentoFormPage", parameters);
        }
        catch (Exception ex)
        {
            _ = MostrarToastAsync("Error " + ex.Message);
        }
    }

    private async void OnEdit(DepartamentoResponse departamento)
    {
        if (!CanEdit)
        {
            await MostrarToastAsync("No tiene permisos para editar.");
            return;
        }

        if (IsBusy || departamento == null) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest},
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Departamento", new DepartamentoRequest(departamento) }
            };
            await GoToAsyncParameters("//DepartamentoFormPage", parameters);
        }
        catch (Exception ex)
        {
            _ = MostrarToastAsync("Error " + ex.Message);
        }
    }

    private async void OnDelete(DepartamentoResponse dpto)
    {
        if (!CanDelete)
        {
            await MostrarToastAsync("No tiene permisos para eliminar.");
            return;
        }

        if (IsBusy || dpto == null) return;

        IsBusy = true;
        try
        {
            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar", $"¿Deseas eliminar el departamento '{dpto.NombreDepartamento}'?", "Sí", "No");

            if (!confirm) return;

            bool tieneInternet = await TieneInternetAsync();
            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                return;
            }

            var response = await departamentoApiService.DeleteDepartamentoAsync(new DepartamentoRequest(dpto));

            if (response)
            {
                _ = MostrarToastAsync("Departamento eliminado.");
                await LoadDepartamento(true);
            }
            else
            {
                _ = MostrarToastAsync("No se pudo eliminar el departamento.");
            }
        }
        catch (Exception ex)
        {
            _ = MostrarToastAsync("Error " + ex.Message);
        }
        finally { IsBusy = false; }
    }

    private async void OnView(DepartamentoResponse departamento)
    {
        if (!CanView)
        {
            await MostrarToastAsync("No tiene permisos para ver detalles.");
            return;
        }

        if (IsBusy || departamento == null) return;

        var parameters = new Dictionary<string, object>
        {
            { "Pais", PaisRequest},
            { "Departamento", new DepartamentoRequest(departamento) },
            { "TitlePage", $"Municipios de {departamento.NombreDepartamento.ToString()} - {PaisRequest.NombrePais.ToString()}"}
        };
        await GoToAsyncParameters("//MunicipioPage", parameters);
    }
}
