using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    public class FuenteNutrienteFormViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService = new();
        private readonly ElementoQuimicoApiService elementoQuimicoApiService = new();

        private FuenteNutrienteRequest fuente = new();
        private FormMode.FormModeSelect mode = new();

        private string estadoInicial = string.Empty;

        private string nombreNutriente = string.Empty;
        private string descripcionNutriente = string.Empty;
        private string precioNutrienteTexto = string.Empty;

        private string errorNombre = string.Empty;
        private string errorPrecio = string.Empty;
        private string errorAportes = string.Empty;

        private bool tieneErrorNombre;
        private bool tieneErrorPrecio;
        private bool tieneErrorAportes;

        private ObservableCollection<ElementoQuimicoResponse> elementosQuimicos = new();
        private ObservableCollection<FuenteNutrienteAporteFormItem> aportes = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command AddAporteCommand { get; }
        public Command RemoveAporteCommand { get; }

        public FuenteNutrienteFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());

            AddAporteCommand = new Command(AddAporte, () => !IsReadOnly);
            RemoveAporteCommand = new Command<FuenteNutrienteAporteFormItem>(RemoveAporte);
        }

        public FuenteNutrienteRequest Fuente
        {
            get => fuente;
            set
            {
                fuente = value ?? new FuenteNutrienteRequest();
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
                OnPropertyChanged(nameof(IsFormEnabled));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));

                SaveCommand.ChangeCanExecute();
                AddAporteCommand.ChangeCanExecute();
            }
        }

        public string NombreNutriente
        {
            get => nombreNutriente;
            set
            {
                nombreNutriente = value;
                OnPropertyChanged();
            }
        }

        public string DescripcionNutriente
        {
            get => descripcionNutriente;
            set
            {
                descripcionNutriente = value;
                OnPropertyChanged();
            }
        }

        public string PrecioNutrienteTexto
        {
            get => precioNutrienteTexto;
            set
            {
                precioNutrienteTexto = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ElementoQuimicoResponse> ElementosQuimicos
        {
            get => elementosQuimicos;
            set
            {
                elementosQuimicos = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FuenteNutrienteAporteFormItem> Aportes
        {
            get => aportes;
            set
            {
                aportes = value;
                OnPropertyChanged();
            }
        }

        public string ErrorNombre
        {
            get => errorNombre;
            set
            {
                errorNombre = value;
                OnPropertyChanged();
            }
        }

        public string ErrorPrecio
        {
            get => errorPrecio;
            set
            {
                errorPrecio = value;
                OnPropertyChanged();
            }
        }

        public string ErrorAportes
        {
            get => errorAportes;
            set
            {
                errorAportes = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorNombre
        {
            get => tieneErrorNombre;
            set
            {
                tieneErrorNombre = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorPrecio
        {
            get => tieneErrorPrecio;
            set
            {
                tieneErrorPrecio = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorAportes
        {
            get => tieneErrorAportes;
            set
            {
                tieneErrorAportes = value;
                OnPropertyChanged();
            }
        }

        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;

        public bool IsFormEnabled => !IsReadOnly;

        public bool ShowSaveButton => Mode != FormMode.FormModeSelect.View;

        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear Fuente de Nutriente",
            FormMode.FormModeSelect.Edit => "Editar Fuente de Nutriente",
            FormMode.FormModeSelect.View => "Detalles de Fuente de Nutriente",
            _ => "Fuente de Nutriente"
        };

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;

                LimpiarErrores();

                await CargarElementosQuimicosAsync();

                CargarDatosIniciales();

                estadoInicial = ObtenerEstadoActual();
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarElementosQuimicosAsync()
        {
            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                ElementosQuimicos = new ObservableCollection<ElementoQuimicoResponse>();
                return;
            }

            var response = await elementoQuimicoApiService.GetElementoQuimicoAsync();

            ElementosQuimicos = new ObservableCollection<ElementoQuimicoResponse>(
                response.OrderBy(x => x.NombreElementoQuimico ?? string.Empty)
            );
        }

        private void CargarDatosIniciales()
        {
            Aportes.Clear();

            if (Mode == FormMode.FormModeSelect.Create)
            {
                Fuente = new FuenteNutrienteRequest();

                NombreNutriente = string.Empty;
                DescripcionNutriente = string.Empty;
                PrecioNutrienteTexto = string.Empty;

                AddAporte();
                return;
            }

            NombreNutriente = Fuente.NombreNutriente ?? string.Empty;
            DescripcionNutriente = Fuente.DescripcionNutriente ?? string.Empty;
            PrecioNutrienteTexto = Fuente.PrecioNutriente > 0
                ? Fuente.PrecioNutriente.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;

            if (Fuente.ElementosQuimicos != null && Fuente.ElementosQuimicos.Count > 0)
            {
                foreach (var item in Fuente.ElementosQuimicos)
                {
                    var elemento = ElementosQuimicos
                        .FirstOrDefault(x => x.ElementoQuimicosId == item.ElementoQuimicosId);

                    var aporte = new FuenteNutrienteAporteFormItem
                    {
                        ElementoQuimicosId = item.ElementoQuimicosId,
                        ElementoSeleccionado = elemento,
                        CantidadAporteTexto = item.CantidadAporte.ToString("0.##", CultureInfo.InvariantCulture)
                    };

                    Aportes.Add(aporte);
                }
            }

            if (!IsReadOnly && Aportes.Count == 0)
                AddAporte();
        }

        private void AddAporte()
        {
            if (IsReadOnly)
                return;

            Aportes.Add(new FuenteNutrienteAporteFormItem());
        }

        private void RemoveAporte(FuenteNutrienteAporteFormItem item)
        {
            if (IsReadOnly)
                return;

            if (item == null)
                return;

            Aportes.Remove(item);
        }

        private async Task CancelAsync()
        {
            try
            {
                bool hayCambios = ObtenerEstadoActual() != estadoInicial;

                if (hayCambios && !IsReadOnly)
                {
                    bool confirm = await App.Current.MainPage.DisplayAlert(
                        "Cancelar",
                        "¿Desea salir sin guardar los cambios?",
                        "Aceptar",
                        "Cancelar");

                    if (!confirm)
                        return;
                }

                await GoToAsyncParameters("//FuenteNutrientePage");
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error: " + ex.Message);
            }
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly)
                return;

            try
            {
                if (!ValidarFormulario())
                    return;

                string mensaje = Mode == FormMode.FormModeSelect.Create
                    ? "¿Desea guardar la fuente de nutriente?"
                    : "¿Desea actualizar la fuente de nutriente?";

                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Confirmar",
                    mensaje,
                    "Aceptar",
                    "Cancelar");

                if (!confirm)
                    return;

                if (!await TieneInternetAsync())
                {
                    await MostrarToastAsync("Sin conexión a internet.");
                    return;
                }

                IsBusy = true;

                FuenteNutrienteRequest request = ConstruirRequest();

                bool response = false;

                if (Mode == FormMode.FormModeSelect.Create)
                    response = await fuenteNutrienteApiService.CreateFuenteNutrienteAsync(request);
                else if (Mode == FormMode.FormModeSelect.Edit)
                    response = await fuenteNutrienteApiService.UpdateFuenteNutrienteAsync(request);

                if (response)
                {
                    await GoToAsyncParameters("//FuenteNutrientePage");

                    string success = Mode == FormMode.FormModeSelect.Create
                        ? "Fuente de nutriente guardada correctamente."
                        : "Fuente de nutriente actualizada correctamente.";

                    await MostrarToastAsync(success);
                }
                else
                {
                    await MostrarToastAsync("No se pudo guardar la fuente de nutriente.");
                }
            }
            catch (Exception ex)
            {
                await MostrarToastAsync("Error: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private FuenteNutrienteRequest ConstruirRequest()
        {
            decimal precio = ParseDecimal(PrecioNutrienteTexto);

            var request = new FuenteNutrienteRequest
            {
                FuenteNutrientesId = Fuente.FuenteNutrientesId,
                NombreNutriente = NombreNutriente?.Trim() ?? string.Empty,
                DescripcionNutriente = DescripcionNutriente?.Trim() ?? string.Empty,
                PrecioNutriente = precio,
                ElementosQuimicos = new List<FuenteNutrienteElementoQuimicoRequest>()
            };

            foreach (var item in Aportes)
            {
                if (!item.ElementoQuimicosId.HasValue)
                    continue;

                decimal cantidad = ParseDecimal(item.CantidadAporteTexto);

                if (cantidad <= 0)
                    continue;

                request.ElementosQuimicos.Add(new FuenteNutrienteElementoQuimicoRequest
                {
                    ElementoQuimicosId = item.ElementoQuimicosId.Value,
                    CantidadAporte = cantidad
                });
            }

            return request;
        }

        private bool ValidarFormulario()
        {
            LimpiarErrores();

            bool valido = true;

            if (string.IsNullOrWhiteSpace(NombreNutriente))
            {
                ErrorNombre = "El nombre de la fuente es obligatorio.";
                TieneErrorNombre = true;
                valido = false;
            }

            if (!TryParseDecimal(PrecioNutrienteTexto, out decimal precio) || precio < 0)
            {
                ErrorPrecio = "Ingrese un precio válido. Puede ser 0 o mayor.";
                TieneErrorPrecio = true;
                valido = false;
            }

            var aportesCompletos = Aportes
                .Where(x => x.ElementoQuimicosId.HasValue || !string.IsNullOrWhiteSpace(x.CantidadAporteTexto))
                .ToList();

            foreach (var aporte in aportesCompletos)
            {
                if (!aporte.ElementoQuimicosId.HasValue)
                {
                    ErrorAportes = "Hay un aporte sin elemento químico seleccionado.";
                    TieneErrorAportes = true;
                    valido = false;
                    break;
                }

                if (!TryParseDecimal(aporte.CantidadAporteTexto, out decimal cantidad) || cantidad <= 0)
                {
                    ErrorAportes = "Hay un aporte con porcentaje inválido.";
                    TieneErrorAportes = true;
                    valido = false;
                    break;
                }

                if (cantidad > 100)
                {
                    ErrorAportes = "El porcentaje de aporte no puede ser mayor a 100.";
                    TieneErrorAportes = true;
                    valido = false;
                    break;
                }
            }

            var duplicados = aportesCompletos
                .Where(x => x.ElementoQuimicosId.HasValue)
                .GroupBy(x => x.ElementoQuimicosId.Value)
                .Any(g => g.Count() > 1);

            if (duplicados)
            {
                ErrorAportes = "No puede repetir el mismo elemento químico en la fuente.";
                TieneErrorAportes = true;
                valido = false;
            }

            return valido;
        }

        private void LimpiarErrores()
        {
            ErrorNombre = string.Empty;
            ErrorPrecio = string.Empty;
            ErrorAportes = string.Empty;

            TieneErrorNombre = false;
            TieneErrorPrecio = false;
            TieneErrorAportes = false;
        }

        private string ObtenerEstadoActual()
        {
            string aportesTexto = string.Join("|",
                Aportes.Select(x =>
                    $"{x.ElementoQuimicosId}-{x.CantidadAporteTexto?.Trim()}"));

            return $"{NombreNutriente?.Trim()}|{DescripcionNutriente?.Trim()}|{PrecioNutrienteTexto?.Trim()}|{aportesTexto}";
        }

        private decimal ParseDecimal(string value)
        {
            if (TryParseDecimal(value, out decimal result))
                return result;

            return 0;
        }

        private bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
                return true;

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                return true;

            value = value.Replace(",", ".");

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}