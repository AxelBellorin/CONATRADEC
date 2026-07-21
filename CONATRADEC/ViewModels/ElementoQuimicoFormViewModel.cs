using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    public class ElementoQuimicoFormViewModel : GlobalService
    {
        private ElementoQuimicoRequest elementoQuimico = new();
        private string simboloElementoQuimico = string.Empty;
        private string nombreElementoQuimico = string.Empty;
        private decimal? pesoEquivalenteElementoQuimico;
        private string pesoEquivalenteTexto = string.Empty;
        private string errorSimbolo = string.Empty;
        private string errorNombre = string.Empty;
        private string errorPesoEquivalente = string.Empty;
        private FormMode.FormModeSelect mode =
            new FormMode.FormModeSelect();

        private readonly ElementoQuimicoApiService
            elementoApiService = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public ElementoQuimicoFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public string SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set
            {
                simboloElementoQuimico =
                    value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(
                        simboloElementoQuimico))
                {
                    ErrorSimbolo = string.Empty;
                }
            }
        }

        public string NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set
            {
                nombreElementoQuimico =
                    value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(
                        nombreElementoQuimico))
                {
                    ErrorNombre = string.Empty;
                }
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
                pesoEquivalenteTexto =
                    value ?? string.Empty;
                OnPropertyChanged();

                if (TryParseDecimal(
                        pesoEquivalenteTexto,
                        out decimal peso) &&
                    peso > 0)
                {
                    ErrorPesoEquivalente = string.Empty;
                }
            }
        }

        public string ErrorSimbolo
        {
            get => errorSimbolo;
            private set
            {
                if (errorSimbolo == value)
                    return;

                errorSimbolo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorSimbolo));
            }
        }

        public bool TieneErrorSimbolo =>
            !string.IsNullOrWhiteSpace(ErrorSimbolo);

        public string ErrorNombre
        {
            get => errorNombre;
            private set
            {
                if (errorNombre == value)
                    return;

                errorNombre = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombre));
            }
        }

        public bool TieneErrorNombre =>
            !string.IsNullOrWhiteSpace(ErrorNombre);

        public string ErrorPesoEquivalente
        {
            get => errorPesoEquivalente;
            private set
            {
                if (errorPesoEquivalente == value)
                    return;

                errorPesoEquivalente = value;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorPesoEquivalente));
            }
        }

        public bool TieneErrorPesoEquivalente =>
            !string.IsNullOrWhiteSpace(
                ErrorPesoEquivalente);

        public ElementoQuimicoRequest ElementoQuimico
        {
            get => elementoQuimico;
            set
            {
                elementoQuimico =
                    value ?? new ElementoQuimicoRequest();

                SimboloElementoQuimico =
                    elementoQuimico
                        .SimboloElementoQuimico
                    ?? string.Empty;

                NombreElementoQuimico =
                    elementoQuimico
                        .NombreElementoQuimico
                    ?? string.Empty;

                PesoEquivalentEelementoQuimico =
                    elementoQuimico
                        .PesoEquivalenteElementoQuimico;

                PesoEquivalenteTexto =
                    elementoQuimico
                        .PesoEquivalenteElementoQuimico
                        .HasValue
                        ? elementoQuimico
                            .PesoEquivalenteElementoQuimico
                            .Value
                            .ToString(
                                "0.##",
                                CultureInfo.InvariantCulture)
                        : string.Empty;

                LimpiarErrores();
                OnPropertyChanged();
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
                RefrescarComandos();
            }
        }

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool ShowSaveButton =>
            Mode != FormMode.FormModeSelect.View;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear elemento químico",
                FormMode.FormModeSelect.Edit =>
                    "Editar elemento químico",
                FormMode.FormModeSelect.View =>
                    "Detalles del elemento químico",
                _ =>
                    "Elemento químico"
            };

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            try
            {
                decimal? pesoActual = null;

                if (TryParseDecimal(
                        PesoEquivalenteTexto,
                        out decimal pesoParseado))
                {
                    pesoActual = pesoParseado;
                }

                bool hayCambios =
                    ValidateFieldsAsync(pesoActual);

                if (hayCambios && !IsReadOnly)
                {
                    bool confirm =
                        await ConfirmarSalidaSinGuardarAsync();

                    if (!confirm)
                        return;
                }

                await GoToElementoQuimicoPage();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "salir del formulario de elemento químico",
                    ex);
            }
        }

        private bool ValidateFieldsAsync(
            decimal? pesoActual)
        {
            if (ElementoQuimico == null)
                return false;

            if (!string.Equals(
                    SimboloElementoQuimico.Trim(),
                    ElementoQuimico
                        .SimboloElementoQuimico?
                        .Trim() ??
                    string.Empty,
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(
                    NombreElementoQuimico.Trim(),
                    ElementoQuimico
                        .NombreElementoQuimico?
                        .Trim() ??
                    string.Empty,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return pesoActual !=
                   ElementoQuimico
                       .PesoEquivalenteElementoQuimico;
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!ValidarFormulario(
                    out decimal pesoEquivalente))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (!ValidateFieldsAsync(pesoEquivalente))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            bool confirm =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el elemento químico")
                    : await ConfirmarActualizacionAsync(
                        "el elemento químico");

            if (!confirm)
                return;

            ElementoQuimico.SimboloElementoQuimico =
                SimboloElementoQuimico.Trim();

            ElementoQuimico.NombreElementoQuimico =
                NombreElementoQuimico.Trim();

            ElementoQuimico.PesoEquivalenteElementoQuimico =
                pesoEquivalente;

            PesoEquivalentEelementoQuimico =
                pesoEquivalente;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                bool response =
                    Mode == FormMode.FormModeSelect.Create
                        ? await elementoApiService
                            .CreateElementoQuimicoAsync(
                                ElementoQuimico)
                        : await elementoApiService
                            .UpdateElementoQuimicoAsync(
                                ElementoQuimico);

                if (!response)
                {
                    await MostrarErrorAsync(
                        Mode == FormMode.FormModeSelect.Create
                            ? "No fue posible guardar el elemento químico. Intente nuevamente."
                            : "No fue posible actualizar el elemento químico. Intente nuevamente.");
                    return;
                }

                await GoToElementoQuimicoPage();

                await MostrarExitoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "Elemento químico guardado correctamente."
                        : "Elemento químico actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el elemento químico"
                        : "actualizar el elemento químico",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private bool ValidarFormulario(
            out decimal pesoEquivalente)
        {
            LimpiarErrores();
            pesoEquivalente = 0;

            SimboloElementoQuimico =
                SimboloElementoQuimico.Trim();

            NombreElementoQuimico =
                NombreElementoQuimico.Trim();

            if (string.IsNullOrWhiteSpace(
                    SimboloElementoQuimico))
            {
                ErrorSimbolo =
                    "Ingrese el símbolo del elemento químico.";
            }

            if (string.IsNullOrWhiteSpace(
                    NombreElementoQuimico))
            {
                ErrorNombre =
                    "Ingrese el nombre del elemento químico.";
            }

            if (!TryParseDecimal(
                    PesoEquivalenteTexto,
                    out pesoEquivalente))
            {
                ErrorPesoEquivalente =
                    "Ingrese un peso equivalente válido.";
            }
            else if (pesoEquivalente <= 0)
            {
                ErrorPesoEquivalente =
                    "El peso equivalente debe ser mayor que cero.";
            }

            return
                !TieneErrorSimbolo &&
                !TieneErrorNombre &&
                !TieneErrorPesoEquivalente;
        }

        private void LimpiarErrores()
        {
            ErrorSimbolo = string.Empty;
            ErrorNombre = string.Empty;
            ErrorPesoEquivalente = string.Empty;
        }

        private static bool TryParseDecimal(
            string value,
            out decimal result)
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
            {
                return true;
            }

            if (decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return true;
            }

            value = value.Replace(",", ".");

            return decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out result);
        }

        private Task GoToElementoQuimicoPage()
        {
            return GoToAsyncParameters(
                "//ElementoQuimicoPage");
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
