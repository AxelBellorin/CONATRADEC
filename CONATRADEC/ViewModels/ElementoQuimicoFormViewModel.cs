using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class ElementoQuimicoFormViewModel : GlobalService
    {
        private ElementoQuimicoRequest elementoQuimico = new();
        private bool isCancel;

        private string simboloElementoQuimico = string.Empty;
        private string nombreElementoQuimico = string.Empty;

        // Se mantiene por compatibilidad interna, pero ya NO debe usarse directo en Entry.Text.
        private decimal? pesoEquivalenteElementoQuimico;

        // Esta es la propiedad correcta para enlazar con Entry.Text.
        private string pesoEquivalenteTexto = string.Empty;

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
            set
            {
                simboloElementoQuimico = value;
                OnPropertyChanged();
            }
        }

        public string NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set
            {
                nombreElementoQuimico = value;
                OnPropertyChanged();
            }
        }

        public decimal? PesoEquivalentEelementoQuimico
        {
            get => pesoEquivalenteElementoQuimico;
            set
            {
                pesoEquivalenteElementoQuimico = value;
                OnPropertyChanged();
            }
        }

        public string PesoEquivalenteTexto
        {
            get => pesoEquivalenteTexto;
            set
            {
                pesoEquivalenteTexto = value;
                OnPropertyChanged();
            }
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
                elementoQuimico = value ?? new ElementoQuimicoRequest();
                OnPropertyChanged();

                SimboloElementoQuimico = elementoQuimico.SimboloElementoQuimico ?? string.Empty;
                NombreElementoQuimico = elementoQuimico.NombreElementoQuimico ?? string.Empty;

                PesoEquivalentEelementoQuimico = elementoQuimico.PesoEquivalenteElementoQuimico;

                PesoEquivalenteTexto = elementoQuimico.PesoEquivalenteElementoQuimico.HasValue
                    ? elementoQuimico.PesoEquivalenteElementoQuimico.Value.ToString("0.##", CultureInfo.InvariantCulture)
                    : string.Empty;
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

                SaveCommand.ChangeCanExecute();
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
                decimal? pesoActual = null;

                if (TryParseDecimal(PesoEquivalenteTexto, out decimal pesoParseado))
                    pesoActual = pesoParseado;

                IsCancel = ValidateFieldsAsync(pesoActual);

                if (IsCancel && !IsReadOnly)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "¿Desea salir sin guardar los cambios?",
                        "Aceptar",
                        "Cancelar");

                    if (confirm)
                    {
                        await GoToElementoQuimicoPage();
                    }
                }
                else
                {
                    await GoToElementoQuimicoPage();
                }
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error " + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool ValidateFieldsAsync(decimal? pesoActual)
        {
            if (ElementoQuimico == null)
                return false;

            if ((SimboloElementoQuimico ?? string.Empty).Trim() != (ElementoQuimico.SimboloElementoQuimico ?? string.Empty).Trim())
                return true;

            if ((NombreElementoQuimico ?? string.Empty).Trim() != (ElementoQuimico.NombreElementoQuimico ?? string.Empty).Trim())
                return true;

            if (pesoActual != ElementoQuimico.PesoEquivalenteElementoQuimico)
                return true;

            return false;
        }

        private async Task SaveAsync()
        {
            try
            {
                if (IsReadOnly)
                    return;

                if (!ValidarFormulario(out decimal pesoEquivalente))
                    return;

                IsCancel = ValidateFieldsAsync(pesoEquivalente);

                if (!IsCancel)
                {
                    await MostrarToastAsync("No hay cambios para guardar.");
                    return;
                }

                if (Mode == FormMode.FormModeSelect.Create)
                    await CreateElementoQuimicoAsync(pesoEquivalente);
                else if (Mode == FormMode.FormModeSelect.Edit)
                    await UpdateElementoQuimicoAsync(pesoEquivalente);
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error " + ex.Message);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private bool ValidarFormulario(out decimal pesoEquivalente)
        {
            pesoEquivalente = 0;

            if (string.IsNullOrWhiteSpace(SimboloElementoQuimico))
            {
                _ = MostrarToastAsync("Ingrese el símbolo del elemento químico.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NombreElementoQuimico))
            {
                _ = MostrarToastAsync("Ingrese el nombre del elemento químico.");
                return false;
            }

            if (!TryParseDecimal(PesoEquivalenteTexto, out pesoEquivalente))
            {
                _ = MostrarToastAsync("Ingrese un peso equivalente válido.");
                return false;
            }

            if (pesoEquivalente <= 0)
            {
                _ = MostrarToastAsync("El peso equivalente debe ser mayor a cero.");
                return false;
            }

            return true;
        }

        private async Task CreateElementoQuimicoAsync(decimal pesoEquivalente)
        {
            try
            {
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Confirmar",
                    "¿Desea guardar los datos del elemento químico?",
                    "Aceptar",
                    "Cancelar");

                if (!confirm)
                    return;

                ElementoQuimico.SimboloElementoQuimico = SimboloElementoQuimico.Trim();
                ElementoQuimico.NombreElementoQuimico = NombreElementoQuimico.Trim();
                ElementoQuimico.PesoEquivalenteElementoQuimico = pesoEquivalente;

                PesoEquivalentEelementoQuimico = pesoEquivalente;

                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    await MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                IsBusy = true;

                var response = await elementoApiService.CreateElementoQuimicoAsync(ElementoQuimico);

                if (response)
                {
                    await GoToElementoQuimicoPage();
                    await MostrarToastAsync("Éxito \nElemento químico guardado correctamente");
                }
                else
                {
                    await MostrarToastAsync("Error \nEl elemento no se pudo guardar, intente nuevamente");
                }
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateElementoQuimicoAsync(decimal pesoEquivalente)
        {
            try
            {
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Confirmar",
                    "¿Desea actualizar?",
                    "Aceptar",
                    "Cancelar");

                if (!confirm)
                    return;

                ElementoQuimico.SimboloElementoQuimico = SimboloElementoQuimico.Trim();
                ElementoQuimico.NombreElementoQuimico = NombreElementoQuimico.Trim();
                ElementoQuimico.PesoEquivalenteElementoQuimico = pesoEquivalente;

                PesoEquivalentEelementoQuimico = pesoEquivalente;

                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    await MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                IsBusy = true;

                var response = await elementoApiService.UpdateElementoQuimicoAsync(ElementoQuimico);

                if (response)
                {
                    await GoToElementoQuimicoPage();
                    await MostrarToastAsync("Éxito \nElemento químico actualizado correctamente");
                }
                else
                {
                    await MostrarToastAsync("Error \nEl elemento no se pudo actualizar, intente nuevamente");
                }
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out result))
                return true;

            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
                return true;

            value = value.Replace(",", ".");

            return decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out result);
        }

        private Task GoToElementoQuimicoPage()
        {
            return GoToAsyncParameters("//ElementoQuimicoPage");
        }
    }
}