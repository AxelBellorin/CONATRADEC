using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Globalization;
using System.Text;

namespace CONATRADEC.ViewModels
{
    public sealed class ElementoQuimicoFormViewModel : GlobalService
    {
        private readonly ElementoQuimicoApiService elementoApiService;
        private CancellationTokenSource? guardadoCts;

        private ElementoQuimicoRequest elementoQuimico = new();
        private string simboloElementoQuimico = string.Empty;
        private string nombreElementoQuimico = string.Empty;
        private string pesoEquivalenteTexto = string.Empty;
        private string simboloOriginal = string.Empty;
        private string nombreOriginal = string.Empty;
        private decimal? pesoOriginal;
        private string errorSimbolo = string.Empty;
        private string errorNombre = string.Empty;
        private string errorPesoEquivalente = string.Empty;
        private FormMode.FormModeSelect mode;

        public ElementoQuimicoFormViewModel()
            : this(new ElementoQuimicoApiService())
        {
        }

        public ElementoQuimicoFormViewModel(
            ElementoQuimicoApiService elementoApiService)
        {
            this.elementoApiService = elementoApiService
                ?? throw new ArgumentNullException(
                    nameof(elementoApiService));

            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => CanSave && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public ElementoQuimicoRequest ElementoQuimico
        {
            get => elementoQuimico;
            set
            {
                elementoQuimico =
                    value ?? new ElementoQuimicoRequest();

                SimboloElementoQuimico =
                    elementoQuimico.SimboloElementoQuimico
                    ?? string.Empty;

                NombreElementoQuimico =
                    elementoQuimico.NombreElementoQuimico
                    ?? string.Empty;

                PesoEquivalenteTexto =
                    elementoQuimico
                        .PesoEquivalenteElementoQuimico
                        .HasValue
                            ? elementoQuimico
                                .PesoEquivalenteElementoQuimico
                                .Value
                                .ToString(
                                    "0.00",
                                    CultureInfo.InvariantCulture)
                            : string.Empty;

                simboloOriginal =
                    SimboloElementoQuimico.Trim();

                nombreOriginal =
                    NombreElementoQuimico.Trim();

                pesoOriginal =
                    elementoQuimico
                        .PesoEquivalenteElementoQuimico
                        .HasValue
                            ? RedondearDosDecimales(
                                elementoQuimico
                                    .PesoEquivalenteElementoQuimico
                                    .Value)
                            : null;

                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public string SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set
            {
                string nuevoValor =
                    (value ?? string.Empty)
                        .ReplaceLineEndings(" ");

                if (simboloElementoQuimico == nuevoValor)
                    return;

                simboloElementoQuimico = nuevoValor;
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
                string nuevoValor =
                    (value ?? string.Empty)
                        .ReplaceLineEndings(" ");

                if (nombreElementoQuimico == nuevoValor)
                    return;

                nombreElementoQuimico = nuevoValor;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(
                        nombreElementoQuimico))
                {
                    ErrorNombre = string.Empty;
                }
            }
        }

        /// <summary>
        /// Limita la entrada a dígitos, un separador decimal y
        /// un máximo de dos cifras decimales.
        /// </summary>
        public string PesoEquivalenteTexto
        {
            get => pesoEquivalenteTexto;
            set
            {
                string nuevoValor =
                    LimitarDosDecimales(value);

                if (pesoEquivalenteTexto == nuevoValor)
                    return;

                pesoEquivalenteTexto = nuevoValor;
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

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                    return;

                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Subtitulo));
                OnPropertyChanged(nameof(TextoBotonCancelar));
                OnPropertyChanged(nameof(MostrarBotonCancelar));
                RefrescarComandos();
            }
        }

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool CanSave =>
            Mode switch
            {
                FormMode.FormModeSelect.Create => CanAdd,
                FormMode.FormModeSelect.Edit => CanEdit,
                _ => false
            };

        public bool ShowSaveButton =>
            CanSave;

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

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Registre el símbolo, nombre y peso equivalente con dos decimales.",

                FormMode.FormModeSelect.Edit =>
                    "Actualice la información del elemento seleccionado.",

                FormMode.FormModeSelect.View =>
                    "Consulte la información registrada.",

                _ =>
                    string.Empty
            };

        public string TextoBotonCancelar =>
            IsReadOnly
                ? "Regresar"
                : "Cancelar";

        public bool MostrarBotonCancelar =>
            !IsReadOnly;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("elementoQuimicoPage");

            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(ShowSaveButton));
            RefrescarComandos();
        }

        public void CancelarOperaciones()
        {
            try
            {
                guardadoCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task SaveAsync()
        {
            if (!CanSave || IsBusy)
                return;

            if (!ValidarFormulario(
                    out decimal pesoEquivalente))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");

                return;
            }

            if (!await ValidarInternetAsync())
                return;

            if (Mode == FormMode.FormModeSelect.Edit &&
                !HayCambios(pesoEquivalente))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");

                return;
            }

            bool confirmar =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el elemento químico")
                    : await ConfirmarActualizacionAsync(
                        "el elemento químico");

            if (!confirmar)
                return;

            guardadoCts?.Cancel();
            guardadoCts?.Dispose();
            guardadoCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ElementoQuimico.SimboloElementoQuimico =
                    SimboloElementoQuimico
                        .Trim()
                        .ToUpperInvariant();

                ElementoQuimico.NombreElementoQuimico =
                    NombreElementoQuimico
                        .Trim()
                        .ToUpperInvariant();

                ElementoQuimico.PesoEquivalenteElementoQuimico =
                    RedondearDosDecimales(
                        pesoEquivalente);

                ApiResult<bool> resultado =
                    Mode == FormMode.FormModeSelect.Create
                        ? await elementoApiService
                            .CreateElementoQuimicoResultAsync(
                                ElementoQuimico,
                                guardadoCts.Token)
                        : await elementoApiService
                            .UpdateElementoQuimicoResultAsync(
                                ElementoQuimico,
                                guardadoCts.Token);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                ElementoQuimicoListadoEstadoService
                    .MarcarCambio();

                await RegresarAlListadoAsync();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Elemento químico guardado correctamente."
                        : resultado.Message);
            }
            catch (OperationCanceledException)
            {
                // La página se cerró durante el guardado.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el elemento químico",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            decimal? pesoActual =
                TryParseDecimal(
                    PesoEquivalenteTexto,
                    out decimal peso)
                        ? RedondearDosDecimales(peso)
                        : null;

            if (!IsReadOnly &&
                HayCambios(pesoActual))
            {
                bool confirmar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirmar)
                    return;
            }

            await RegresarAlListadoAsync();
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
            else if (SimboloElementoQuimico.Length > 10)
            {
                ErrorSimbolo =
                    "El símbolo no puede superar 10 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(
                    NombreElementoQuimico))
            {
                ErrorNombre =
                    "Ingrese el nombre del elemento químico.";
            }
            else if (NombreElementoQuimico.Length > 100)
            {
                ErrorNombre =
                    "El nombre no puede superar 100 caracteres.";
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
            else if (pesoEquivalente > 99999999.99m)
            {
                ErrorPesoEquivalente =
                    "El peso equivalente supera el valor permitido.";
            }
            else
            {
                pesoEquivalente =
                    RedondearDosDecimales(
                        pesoEquivalente);

                PesoEquivalenteTexto =
                    pesoEquivalente.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture);
            }

            return
                !TieneErrorSimbolo &&
                !TieneErrorNombre &&
                !TieneErrorPesoEquivalente;
        }

        private bool HayCambios(
            decimal? pesoActual)
        {
            string simboloActual =
                SimboloElementoQuimico.Trim();

            string nombreActual =
                NombreElementoQuimico.Trim();

            if (Mode == FormMode.FormModeSelect.Create)
            {
                return
                    !string.IsNullOrWhiteSpace(simboloActual) ||
                    !string.IsNullOrWhiteSpace(nombreActual) ||
                    pesoActual.HasValue;
            }

            return
                !string.Equals(
                    simboloActual,
                    simboloOriginal,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    nombreActual,
                    nombreOriginal,
                    StringComparison.OrdinalIgnoreCase) ||
                pesoActual != pesoOriginal;
        }

        private Task RegresarAlListadoAsync() =>
            GoToAsyncParameters(
                AppRoutes.ElementosQuimicos);

        private void LimpiarErrores()
        {
            ErrorSimbolo = string.Empty;
            ErrorNombre = string.Empty;
            ErrorPesoEquivalente = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }

        private static string LimitarDosDecimales(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var resultado = new StringBuilder();
            bool tieneSeparador = false;
            int decimales = 0;

            foreach (char caracter in valor.Trim())
            {
                if (char.IsDigit(caracter))
                {
                    if (!tieneSeparador ||
                        decimales < 2)
                    {
                        resultado.Append(caracter);

                        if (tieneSeparador)
                            decimales++;
                    }

                    continue;
                }

                if ((caracter == '.' ||
                     caracter == ',') &&
                    !tieneSeparador)
                {
                    if (resultado.Length == 0)
                        resultado.Append('0');

                    resultado.Append(caracter);
                    tieneSeparador = true;
                }
            }

            return resultado.ToString();
        }

        private static bool TryParseDecimal(
            string? valor,
            out decimal resultado)
        {
            resultado = 0;

            if (string.IsNullOrWhiteSpace(valor))
                return false;

            string normalizado =
                valor
                    .Trim()
                    .Replace(',', '.');

            return decimal.TryParse(
                normalizado,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out resultado);
        }

        private static decimal RedondearDosDecimales(
            decimal valor) =>
            decimal.Round(
                valor,
                2,
                MidpointRounding.AwayFromZero);
    }
}
