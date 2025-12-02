using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class DepartamentoFormViewModel : GlobalService
    {
        private DepartamentoRequest departamento;
        private PaisRequest paisRequest;

        private string nombreDepartamento = string.Empty;

        private FormMode.FormModeSelect mode;

        private readonly DepartamentoApiService departamentoApiService = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public DepartamentoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        // OBJETO EDITADO
        public DepartamentoRequest Departamento
        {
            get => departamento;
            set
            {
                departamento = value;
                NombreDepartamento = value?.NombreDepartamento ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set { paisRequest = value; OnPropertyChanged(); }
        }

        // CAMPOS
        public string NombreDepartamento
        {
            get => nombreDepartamento;
            set { nombreDepartamento = value; OnPropertyChanged(); }
        }

        // MODO DEL FORMULARIO
        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEntryReadOnly));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(Title));
            }
        }

        // UI BINDINGS
        public bool IsEntryReadOnly => Mode == FormMode.FormModeSelect.View;

        public bool CanSave => Mode != FormMode.FormModeSelect.View;

        public string Title =>
            Mode == FormMode.FormModeSelect.Create ? "Crear Departamento" :
            Mode == FormMode.FormModeSelect.Edit ? "Editar Departamento" :
            "Detalles del Departamento";

        // LÓGICA PRINCIPAL
        private async Task SaveAsync()
        {
            if (!CanSave)
            {
                await MostrarToastAsync("No se puede guardar en modo vista.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NombreDepartamento))
            {
                await App.Current.MainPage.DisplayAlert("Validación", "Ingrese un nombre.", "Aceptar");
                return;
            }

            if (Mode == FormMode.FormModeSelect.Create)
                await CreateAsync();
            else if (Mode == FormMode.FormModeSelect.Edit)
                await UpdateAsync();
        }

        private async Task CreateAsync()
        {
            Departamento.NombreDepartamento = NombreDepartamento;
            Departamento.PaisId = PaisRequest.PaisId;

            var ok = await departamentoApiService.CreateDepartamentoAsync(Departamento);

            if (ok)
            {
                await MostrarToastAsync("Departamento creado.");
                await ReturnToList();
            }
            else
                await MostrarToastAsync("No se pudo guardar.");
        }

        private async Task UpdateAsync()
        {
            Departamento.NombreDepartamento = NombreDepartamento;
            Departamento.PaisId = PaisRequest.PaisId;

            var ok = await departamentoApiService.UpdateDepartamentoAsync(Departamento);

            if (ok)
            {
                await MostrarToastAsync("Departamento actualizado.");
                await ReturnToList();
            }
            else
                await MostrarToastAsync("No se pudo actualizar.");
        }

        private async Task CancelAsync()
        {
            await ReturnToList();
        }

        private Task ReturnToList()
        {
            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                { "TitlePage", $"Departamento de {PaisRequest.NombrePais}" }
            };

            return GoToAsyncParameters("//DepartamentoPage", parameters);
        }
    }
}
