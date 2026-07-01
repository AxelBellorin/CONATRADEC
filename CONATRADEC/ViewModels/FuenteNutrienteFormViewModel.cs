using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class FuenteNutrienteFormViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService = new();
        private readonly ElementoQuimicoApiService elementoQuimicoApiService = new();

        private FuenteNutrienteRequest fuente = new();
        private FormMode.FormModeSelect mode = new();

        private string estadoInicial = string.Empty;
        private string categoriaOriginalCodigo = FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;

        private string nombreNutriente = string.Empty;
        private string descripcionNutriente = string.Empty;
        private string precioNutrienteTexto = string.Empty;
        private string prntEnmiendaCalcareaTexto = string.Empty;
        private string descripcionParametroEnmiendaCalcarea = string.Empty;

        private string errorNombre = string.Empty;
        private string errorPrecio = string.Empty;
        private string errorAportes = string.Empty;
        private string errorCategoria = string.Empty;
        private string errorPrntEnmiendaCalcarea = string.Empty;
        private string errorDescripcionParametroEnmiendaCalcarea = string.Empty;

        private bool tieneErrorNombre;
        private bool tieneErrorPrecio;
        private bool tieneErrorAportes;
        private bool tieneErrorCategoria;
        private bool tieneErrorPrntEnmiendaCalcarea;
        private bool tieneErrorDescripcionParametroEnmiendaCalcarea;

        private FuenteNutrienteCategoriaOption? categoriaSeleccionada;

        private ObservableCollection<ElementoQuimicoResponse> elementosQuimicos = new();
        private ObservableCollection<FuenteNutrienteAporteFormItem> aportes = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command AddAporteCommand { get; }
        public Command RemoveAporteCommand { get; }

        public FuenteNutrienteFormViewModel()
        {
            CategoriasFuente = new ObservableCollection<FuenteNutrienteCategoriaOption>();
            CargarCategoriasFuente();

            SaveCommand = new Command(async () => await SaveAsync(), () => !IsReadOnly);
            CancelCommand = new Command(async () => await CancelAsync());

            AddAporteCommand = new Command(AddAporte, () => !IsReadOnly && MostrarAportesElementosQuimicos);
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
                OnPropertyChanged(nameof(MostrarDatosEnmiendaCalcarea));
                OnPropertyChanged(nameof(MostrarAportesElementosQuimicos));
                OnPropertyChanged(nameof(MostrarBotonAgregarAporte));

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

        public ObservableCollection<FuenteNutrienteCategoriaOption> CategoriasFuente { get; }

        public FuenteNutrienteCategoriaOption? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                categoriaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarDatosEnmiendaCalcarea));
                OnPropertyChanged(nameof(MostrarAportesElementosQuimicos));
                OnPropertyChanged(nameof(MostrarBotonAgregarAporte));

                if (!IsReadOnly &&
                    MostrarAportesElementosQuimicos &&
                    Aportes.Count == 0)
                {
                    AddAporte();
                }

                AddAporteCommand.ChangeCanExecute();
            }
        }

        public string PrntEnmiendaCalcareaTexto
        {
            get => prntEnmiendaCalcareaTexto;
            set
            {
                prntEnmiendaCalcareaTexto = value;
                OnPropertyChanged();
            }
        }

        public string DescripcionParametroEnmiendaCalcarea
        {
            get => descripcionParametroEnmiendaCalcarea;
            set
            {
                descripcionParametroEnmiendaCalcarea = value;
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

        public string ErrorCategoria
        {
            get => errorCategoria;
            set
            {
                errorCategoria = value;
                OnPropertyChanged();
            }
        }

        public string ErrorPrntEnmiendaCalcarea
        {
            get => errorPrntEnmiendaCalcarea;
            set
            {
                errorPrntEnmiendaCalcarea = value;
                OnPropertyChanged();
            }
        }

        public string ErrorDescripcionParametroEnmiendaCalcarea
        {
            get => errorDescripcionParametroEnmiendaCalcarea;
            set
            {
                errorDescripcionParametroEnmiendaCalcarea = value;
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

        public bool TieneErrorCategoria
        {
            get => tieneErrorCategoria;
            set
            {
                tieneErrorCategoria = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorPrntEnmiendaCalcarea
        {
            get => tieneErrorPrntEnmiendaCalcarea;
            set
            {
                tieneErrorPrntEnmiendaCalcarea = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorDescripcionParametroEnmiendaCalcarea
        {
            get => tieneErrorDescripcionParametroEnmiendaCalcarea;
            set
            {
                tieneErrorDescripcionParametroEnmiendaCalcarea = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarDatosEnmiendaCalcarea =>
            CategoriaSeleccionada?.Codigo == FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;

        public bool MostrarAportesElementosQuimicos =>
            CategoriaSeleccionada?.Codigo == FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;

        public bool MostrarBotonAgregarAporte =>
            ShowSaveButton && MostrarAportesElementosQuimicos;

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

                if (CategoriasFuente.Count == 0)
                    CargarCategoriasFuente();

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

        private void CargarCategoriasFuente()
        {
            CategoriasFuente.Clear();

            CategoriasFuente.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoBalanceNutricional,
                Nombre = "Balance nutricional"
            });

            CategoriasFuente.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea,
                Nombre = "Enmienda calcárea"
            });

            CategoriasFuente.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta,
                Nombre = "Fertilización mixta"
            });
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

                PrntEnmiendaCalcareaTexto = string.Empty;
                DescripcionParametroEnmiendaCalcarea = string.Empty;

                categoriaOriginalCodigo = FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;
                CategoriaSeleccionada = BuscarCategoriaPorCodigo(categoriaOriginalCodigo);

                if (MostrarAportesElementosQuimicos)
                    AddAporte();

                return;
            }

            NombreNutriente = Fuente.NombreNutriente ?? string.Empty;
            DescripcionNutriente = Fuente.DescripcionNutriente ?? string.Empty;
            PrecioNutrienteTexto = Fuente.PrecioNutriente > 0
                ? Fuente.PrecioNutriente.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;

            PrntEnmiendaCalcareaTexto = Fuente.PrntEnmiendaCalcarea.HasValue
                ? Fuente.PrntEnmiendaCalcarea.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;

            DescripcionParametroEnmiendaCalcarea =
                Fuente.DescripcionParametroEnmiendaCalcarea ?? string.Empty;

            categoriaOriginalCodigo = ObtenerCodigoCategoriaDesdeFuente();
            CategoriaSeleccionada = BuscarCategoriaPorCodigo(categoriaOriginalCodigo);

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

            if (!IsReadOnly && MostrarAportesElementosQuimicos && Aportes.Count == 0)
                AddAporte();
        }

        private FuenteNutrienteCategoriaOption? BuscarCategoriaPorCodigo(string codigo)
        {
            return CategoriasFuente.FirstOrDefault(x =>
                string.Equals(x.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        }

        private string ObtenerCodigoCategoriaDesdeFuente()
        {
            if (Fuente.HabilitadaEnmiendaCalcarea)
                return FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;

            if (Fuente.HabilitadaFertilizacionMixta)
                return FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta;

            return FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;
        }

        private string ObtenerCodigoCategoriaSeleccionada()
        {
            return CategoriaSeleccionada?.Codigo ?? FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;
        }

        private void AddAporte()
        {
            if (IsReadOnly || !MostrarAportesElementosQuimicos)
                return;

            Aportes.Add(new FuenteNutrienteAporteFormItem());
        }

        private void RemoveAporte(FuenteNutrienteAporteFormItem item)
        {
            if (IsReadOnly || !MostrarAportesElementosQuimicos)
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

                bool guardadoCorrectamente = false;
                int? fuenteNutrientesId = request.FuenteNutrientesId;

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    FuenteNutrienteResponse? fuenteCreada =
                        await fuenteNutrienteApiService.CreateFuenteNutrienteConRespuestaAsync(request);

                    if (fuenteCreada?.FuenteNutrientesId == null || fuenteCreada.FuenteNutrientesId <= 0)
                    {
                        await MostrarToastAsync("No se pudo obtener el ID de la fuente creada.");
                        return;
                    }

                    fuenteNutrientesId = fuenteCreada.FuenteNutrientesId;
                    Fuente.FuenteNutrientesId = fuenteNutrientesId;
                    guardadoCorrectamente = true;
                }
                else if (Mode == FormMode.FormModeSelect.Edit)
                {
                    guardadoCorrectamente = await fuenteNutrienteApiService.UpdateFuenteNutrienteAsync(request);
                }

                if (!guardadoCorrectamente)
                {
                    await MostrarToastAsync("No se pudo guardar la fuente de nutriente.");
                    return;
                }

                if (fuenteNutrientesId == null || fuenteNutrientesId <= 0)
                {
                    await MostrarToastAsync("No se encontró el ID de la fuente para aplicar la clasificación.");
                    return;
                }

                bool categoriaAplicada =
                    await AplicarCategoriaFuenteAsync(fuenteNutrientesId.Value);

                if (!categoriaAplicada)
                {
                    if (Mode == FormMode.FormModeSelect.Create)
                    {
                        Mode = FormMode.FormModeSelect.Edit;
                        categoriaOriginalCodigo = FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;
                    }

                    await MostrarToastAsync("La fuente se guardó, pero no se pudo aplicar la clasificación seleccionada.");
                    return;
                }

                await GoToAsyncParameters("//FuenteNutrientePage");

                string success = Mode == FormMode.FormModeSelect.Create
                    ? "Fuente de nutriente guardada correctamente."
                    : "Fuente de nutriente actualizada correctamente.";

                await MostrarToastAsync(success);
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

        private async Task<bool> AplicarCategoriaFuenteAsync(int fuenteNutrientesId)
        {
            string categoriaActual = ObtenerCodigoCategoriaSeleccionada();
            string categoriaOriginal = categoriaOriginalCodigo;

            if (categoriaActual == categoriaOriginal)
                return true;

            if (categoriaOriginal == FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea &&
                categoriaActual != FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea)
            {
                bool deshabilitada =
                    await fuenteNutrienteApiService.DeshabilitarEnmiendaCalcareaAsync(fuenteNutrientesId);

                if (!deshabilitada)
                    return false;
            }

            if (categoriaOriginal == FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta &&
                categoriaActual != FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta)
            {
                bool deshabilitada =
                    await fuenteNutrienteApiService.DeshabilitarFertilizacionMixtaAsync(fuenteNutrientesId);

                if (!deshabilitada)
                    return false;
            }

            if (categoriaActual == FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea &&
                categoriaOriginal != FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea)
            {
                bool habilitada =
                    await fuenteNutrienteApiService.HabilitarEnmiendaCalcareaAsync(
                        fuenteNutrientesId,
                        new HabilitarEnmiendaCalcareaRequest
                        {
                            Prnt = ParseDecimal(PrntEnmiendaCalcareaTexto),
                            DescripcionParametro = DescripcionParametroEnmiendaCalcarea?.Trim() ?? string.Empty
                        });

                if (!habilitada)
                    return false;
            }

            if (categoriaActual == FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta &&
                categoriaOriginal != FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta)
            {
                bool habilitada =
                    await fuenteNutrienteApiService.HabilitarFertilizacionMixtaAsync(fuenteNutrientesId);

                if (!habilitada)
                    return false;
            }

            categoriaOriginalCodigo = categoriaActual;

            return true;
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

            if (!MostrarAportesElementosQuimicos)
                return request;

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

            if (CategoriaSeleccionada == null ||
                string.IsNullOrWhiteSpace(CategoriaSeleccionada.Codigo))
            {
                ErrorCategoria = "Debe seleccionar la clasificación de la fuente.";
                TieneErrorCategoria = true;
                valido = false;
            }

            if (DebeEnviarHabilitarEnmiendaCalcarea())
            {
                if (!TryParseDecimal(PrntEnmiendaCalcareaTexto, out decimal prnt) || prnt < 0)
                {
                    ErrorPrntEnmiendaCalcarea = "Ingrese un PRNT válido. Puede ser 0 o mayor.";
                    TieneErrorPrntEnmiendaCalcarea = true;
                    valido = false;
                }

                if (string.IsNullOrWhiteSpace(DescripcionParametroEnmiendaCalcarea))
                {
                    ErrorDescripcionParametroEnmiendaCalcarea = "Debe ingresar la descripción del parámetro.";
                    TieneErrorDescripcionParametroEnmiendaCalcarea = true;
                    valido = false;
                }
            }

            if (MostrarAportesElementosQuimicos)
            {
                var aportesCompletos = Aportes
                    .Where(x => x.ElementoQuimicosId.HasValue || !string.IsNullOrWhiteSpace(x.CantidadAporteTexto))
                    .ToList();

                if (aportesCompletos.Count == 0)
                {
                    ErrorAportes = "Debe agregar al menos un aporte de elemento químico para balance nutricional.";
                    TieneErrorAportes = true;
                    valido = false;
                }

                decimal totalAporte = 0;

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

                    totalAporte += cantidad;
                }

                if (valido && totalAporte > 100)
                {
                    ErrorAportes = $"La suma total de los aportes no puede superar el 100%. Total actual: {totalAporte:N2}%.";
                    TieneErrorAportes = true;
                    valido = false;
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
            }

            return valido;
        }

        private bool DebeEnviarHabilitarEnmiendaCalcarea()
        {
            string categoriaActual = ObtenerCodigoCategoriaSeleccionada();

            return categoriaActual == FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea &&
                   categoriaOriginalCodigo != FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;
        }

        private void LimpiarErrores()
        {
            ErrorNombre = string.Empty;
            ErrorPrecio = string.Empty;
            ErrorAportes = string.Empty;
            ErrorCategoria = string.Empty;
            ErrorPrntEnmiendaCalcarea = string.Empty;
            ErrorDescripcionParametroEnmiendaCalcarea = string.Empty;

            TieneErrorNombre = false;
            TieneErrorPrecio = false;
            TieneErrorAportes = false;
            TieneErrorCategoria = false;
            TieneErrorPrntEnmiendaCalcarea = false;
            TieneErrorDescripcionParametroEnmiendaCalcarea = false;
        }

        private string ObtenerEstadoActual()
        {
            string aportesTexto = string.Join("|",
                Aportes.Select(x =>
                    $"{x.ElementoQuimicosId}-{x.CantidadAporteTexto?.Trim()}"));

            return $"{NombreNutriente?.Trim()}|" +
                   $"{DescripcionNutriente?.Trim()}|" +
                   $"{PrecioNutrienteTexto?.Trim()}|" +
                   $"{ObtenerCodigoCategoriaSeleccionada()}|" +
                   $"{PrntEnmiendaCalcareaTexto?.Trim()}|" +
                   $"{DescripcionParametroEnmiendaCalcarea?.Trim()}|" +
                   $"{aportesTexto}";
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