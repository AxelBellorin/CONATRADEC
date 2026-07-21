using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    public class PaisFormViewModel : GlobalService
    {
        private PaisRequest pais = new();
        private bool isCancel;

        private string nombrePais = string.Empty;
        private string codigoISOPais = string.Empty;

        private string errorNombrePais = string.Empty;
        private string errorCodigoISOPais = string.Empty;

        private FormMode.FormModeSelect mode =
            new FormMode.FormModeSelect();

        private readonly PaisApiService paisApiService =
            new PaisApiService();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public PaisFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public string NombrePais
        {
            get => nombrePais;
            set
            {
                nombrePais = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombrePais))
                    ErrorNombrePais = string.Empty;
            }
        }

        public string CodigoISOPais
        {
            get => codigoISOPais;
            set
            {
                codigoISOPais =
                    (value ?? string.Empty)
                    .ToUpperInvariant();

                OnPropertyChanged();

                if (Regex.IsMatch(
                    codigoISOPais.Trim(),
                    "^[A-Z]{3}$"))
                {
                    ErrorCodigoISOPais = string.Empty;
                }
            }
        }

        public string ErrorNombrePais
        {
            get => errorNombrePais;
            set
            {
                if (errorNombrePais == value)
                    return;

                errorNombrePais = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombrePais));
            }
        }

        public bool TieneErrorNombrePais =>
            !string.IsNullOrWhiteSpace(ErrorNombrePais);

        public string ErrorCodigoISOPais
        {
            get => errorCodigoISOPais;
            set
            {
                if (errorCodigoISOPais == value)
                    return;

                errorCodigoISOPais = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorCodigoISOPais));
            }
        }

        public bool TieneErrorCodigoISOPais =>
            !string.IsNullOrWhiteSpace(ErrorCodigoISOPais);

        public bool IsCancel
        {
            get => isCancel;
            set => isCancel = value;
        }

        public PaisRequest Pais
        {
            get => pais;
            set
            {
                pais = value ?? new PaisRequest();

                OnPropertyChanged();

                NombrePais = pais.NombrePais ?? string.Empty;
                CodigoISOPais =
                    pais.CodigoISOPais ?? string.Empty;

                LimpiarErrores();
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

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool ShowSaveButton =>
            Mode != FormMode.FormModeSelect.View;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear país",

                FormMode.FormModeSelect.Edit =>
                    "Editar país",

                FormMode.FormModeSelect.View =>
                    "Detalles del país",

                _ =>
                    "País"
            };

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsCancel = HayCambios();

                if (IsCancel)
                {
                    bool confirm =
                        await ConfirmarSalidaSinGuardarAsync();

                    if (!confirm)
                        return;
                }

                await GoToAsyncParameters("//PaisPage");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "salir del formulario de país",
                    ex);
            }
            finally
            {
                IsCancel = false;
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy || IsReadOnly)
                return;

            try
            {
                if (!ValidarCampos())
                {
                    await MostrarAdvertenciaAsync(
                        "Revise los campos marcados antes de continuar.");

                    return;
                }

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    await CreatePaisAsync();
                }
                else if (Mode == FormMode.FormModeSelect.Edit)
                {
                    await UpdatePaisAsync();
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el país",
                    ex);
            }
        }

        private async Task CreatePaisAsync()
        {
            bool confirm =
                await ConfirmarGuardadoAsync("el país");

            if (!confirm)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                bool response =
                    await paisApiService.CreatePaisAsync(Pais);

                if (!response)
                {
                    await MostrarErrorAsync(
                        "No fue posible guardar el país. Intente nuevamente.");

                    return;
                }

                await GoToAsyncParameters("//PaisPage");

                await MostrarExitoAsync(
                    "País guardado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el país",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task UpdatePaisAsync()
        {
            if (!HayCambios())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");

                return;
            }

            bool confirm =
                await ConfirmarActualizacionAsync("el país");

            if (!confirm)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                bool response =
                    await paisApiService.UpdatePaisAsync(Pais);

                if (!response)
                {
                    await MostrarErrorAsync(
                        "No fue posible actualizar el país. Intente nuevamente.");

                    return;
                }

                await GoToAsyncParameters("//PaisPage");

                await MostrarExitoAsync(
                    "País actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "actualizar el país",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void SincronizarModelo()
        {
            Pais.NombrePais = NombrePais.Trim();
            Pais.CodigoISOPais =
                CodigoISOPais.Trim().ToUpperInvariant();
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombrePais.Trim();

            string codigoActual =
                CodigoISOPais.Trim().ToUpperInvariant();

            string nombreOriginal =
                Pais.NombrePais?.Trim() ?? string.Empty;

            string codigoOriginal =
                Pais.CodigoISOPais?
                    .Trim()
                    .ToUpperInvariant() ??
                string.Empty;

            return
                nombreActual != nombreOriginal ||
                codigoActual != codigoOriginal;
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            NombrePais = NombrePais.Trim();
            CodigoISOPais =
                CodigoISOPais.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(NombrePais))
            {
                ErrorNombrePais =
                    "Ingrese el nombre del país.";
            }

            if (string.IsNullOrWhiteSpace(CodigoISOPais))
            {
                ErrorCodigoISOPais =
                    "Ingrese el código ISO del país.";
            }
            else if (!Regex.IsMatch(
                         CodigoISOPais,
                         "^[A-Z]{3}$"))
            {
                ErrorCodigoISOPais =
                    "El código ISO debe contener exactamente 3 letras.";
            }

            return
                !TieneErrorNombrePais &&
                !TieneErrorCodigoISOPais;
        }

        private void LimpiarErrores()
        {
            ErrorNombrePais = string.Empty;
            ErrorCodigoISOPais = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
