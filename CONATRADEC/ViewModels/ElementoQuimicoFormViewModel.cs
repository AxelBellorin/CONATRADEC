using CONATRADEC.Services;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.ViewModels
{
    public class ElementoQuimicoFormViewModel : GlobalService
    {
        private ElementoQuimicoRequest elementoQuimico;
        private bool isCancel;

        private string simboloElementoQuimico;
        private string nombreElementoQuimico;
        private decimal? pesoEquivalentEelementoQuimico;

        private FormMode.FormModeSelect mode = new FormMode.FormModeSelect();

        private readonly ElementoQuimicoApiService elementoApiService = new ElementoQuimicoApiService();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public ElementoQuimicoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public string SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set { simboloElementoQuimico = value; OnPropertyChanged(); }
        }

        public string NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set { nombreElementoQuimico = value; OnPropertyChanged(); }
        }

        public decimal? PesoEquivalentEelementoQuimico
        {
            get => pesoEquivalentEelementoQuimico;
            set { pesoEquivalentEelementoQuimico = value; OnPropertyChanged(); }
        }

        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        public ElementoQuimicoRequest ElementoQuimico
        {
            get => elementoQuimico;
            set
            {
                elementoQuimico = value;
                OnPropertyChanged();

                SimboloElementoQuimico = value.SimboloElementoQuimico;
                NombreElementoQuimico = value.NombreElementoQuimico;
                PesoEquivalentEelementoQuimico = value.PesoEquivalentEelementoQuimico;
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ShowSaveButton));
                // ((Command)SaveCommand).ChangeCanExecute(); // si lo querés recalcular
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;

        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Elemento Químico",
            FormMode.FormModeSelect.Edit => "Editar Elemento Químico",
            FormMode.FormModeSelect.View => "Detalles del Elemento Químico",
            _ => "",
        };

        private async Task CancelAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "Desea no guardar los cambios",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToAsyncParameters("//ElementoQuimicoPage");
                    }
                }
                else
                {
                    await GoToAsyncParameters("//ElementoQuimicoPage");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool ValidateFieldsAsync()
        {
            if (ElementoQuimico == null) return false;

            if (SimboloElementoQuimico != ElementoQuimico.SimboloElementoQuimico) return true;
            if (NombreElementoQuimico != ElementoQuimico.NombreElementoQuimico) return true;
            if (PesoEquivalentEelementoQuimico != ElementoQuimico.PesoEquivalentEelementoQuimico) return true;

            return false;
        }

        private async Task SaveAsync()
        {
            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateElementoQuimicoAsync();
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateElementoQuimicoAsync();
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        private async Task CreateElementoQuimicoAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea guardar los datos del elemento químico?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        ElementoQuimico.SimboloElementoQuimico = SimboloElementoQuimico;
                        ElementoQuimico.NombreElementoQuimico = NombreElementoQuimico;
                        ElementoQuimico.PesoEquivalentEelementoQuimico = PesoEquivalentEelementoQuimico;

                        bool tieneInternet = await TieneInternetAsync();
                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        var response = await elementoApiService.CreateElementoQuimicoAsync(ElementoQuimico);

                        if (response)
                        {
                            await GoToElementoQuimicoPage();
                            _ = MostrarToastAsync("Éxito \nElemento químico guardado correctamente");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error \nEl elemento no se pudo guardar, intente nuevamente");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private async Task UpdateElementoQuimicoAsync()
        {
            try
            {
                IsCancel = ValidateFieldsAsync();

                if (IsCancel)
                {
                    bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        "¿Desea actualizar?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        ElementoQuimico.SimboloElementoQuimico = SimboloElementoQuimico;
                        ElementoQuimico.NombreElementoQuimico = NombreElementoQuimico;
                        ElementoQuimico.PesoEquivalentEelementoQuimico = PesoEquivalentEelementoQuimico;

                        bool tieneInternet = await TieneInternetAsync();
                        if (!tieneInternet)
                        {
                            _ = MostrarToastAsync("Sin conexión a internet.");
                            IsBusy = false;
                            return;
                        }

                        var response = await elementoApiService.UpdateElementoQuimicoAsync(ElementoQuimico);

                        if (response)
                        {
                            await GoToElementoQuimicoPage();
                            _ = MostrarToastAsync("Éxito \nElemento químico actualizado correctamente");
                        }
                        else
                        {
                            _ = MostrarToastAsync("Error \nEl elemento no se pudo actualizar, intente nuevamente");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private Task GoToElementoQuimicoPage()
        {
            return GoToAsyncParameters("//ElementoQuimicoPage");
        }
    }
}
