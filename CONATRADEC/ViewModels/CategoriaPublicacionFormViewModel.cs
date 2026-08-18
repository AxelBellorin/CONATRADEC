using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CONATRADEC.ViewModels
{
    public sealed class CategoriaPublicacionFormViewModel : GlobalService
    {
        private static readonly Regex ColorRegex = new(
            "^#[0-9A-Fa-f]{6}$",
            RegexOptions.Compiled);

        private readonly CategoriaPublicacionApiService
            apiService = new();

        private int categoriaPublicacionId;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string colorHex = "#3B655B";
        private string ordenTexto = "1";
        private ColorPublicacionOption? colorSeleccionado;
        private bool preparado;
        private bool datosCargados;
        private CancellationTokenSource?
            cargaCancellationTokenSource;

        private string nombreOriginal = string.Empty;
        private string descripcionOriginal = string.Empty;
        private string colorHexOriginal = "#3B655B";
        private string ordenOriginal = "1";

        public CategoriaPublicacionFormViewModel()
        {
            Colores =
                new ObservableCollection<
                    ColorPublicacionOption>
                {
                    new()
                    {
                        Nombre = "Verde institucional",
                        Hex = "#3B655B"
                    },
                    new()
                    {
                        Nombre = "Café",
                        Hex = "#9B552C"
                    },
                    new()
                    {
                        Nombre = "Naranja",
                        Hex = "#FF9800"
                    },
                    new()
                    {
                        Nombre = "Amarillo",
                        Hex = "#F2C94C"
                    },
                    new()
                    {
                        Nombre = "Azul",
                        Hex = "#2F80ED"
                    },
                    new()
                    {
                        Nombre = "Rojo",
                        Hex = "#D64545"
                    },
                    new()
                    {
                        Nombre = "Morado",
                        Hex = "#7B61FF"
                    },
                    new()
                    {
                        Nombre = "Gris",
                        Hex = "#6B7280"
                    }
                };

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && PuedeGuardar);

            CancelarCommand = new Command(
                async () => await CancelarAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<ColorPublicacionOption>
            Colores { get; }

        public int CategoriaPublicacionId =>
            categoriaPublicacionId;

        public bool EsEdicion =>
            CategoriaPublicacionId > 0;

        public string TituloPagina =>
            EsEdicion
                ? "Editar tipo de publicación"
                : "Nuevo tipo de publicación";

        public string TextoBoton =>
            EsEdicion
                ? "Guardar cambios"
                : "Crear tipo";

        public string Nombre
        {
            get => nombre;
            set
            {
                string nuevo =
                    value ?? string.Empty;

                if (nombre == nuevo)
                    return;

                nombre = nuevo;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                string nuevo =
                    value ?? string.Empty;

                if (descripcion == nuevo)
                    return;

                descripcion = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(CaracteresDescripcion));
            }
        }

        public string CaracteresDescripcion =>
            $"{Descripcion.Length}/250";

        public string ColorHex
        {
            get => colorHex;
            set
            {
                string nuevo =
                    value ?? string.Empty;

                if (colorHex == nuevo)
                    return;

                colorHex = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(ColorVistaPrevia));

                ColorPublicacionOption? coincidencia =
                    Colores.FirstOrDefault(
                        x => string.Equals(
                            x.Hex,
                            colorHex,
                            StringComparison.OrdinalIgnoreCase));

                if (!ReferenceEquals(
                        colorSeleccionado,
                        coincidencia))
                {
                    colorSeleccionado =
                        coincidencia;

                    OnPropertyChanged(
                        nameof(ColorSeleccionado));
                }
            }
        }

        public ColorPublicacionOption? ColorSeleccionado
        {
            get => colorSeleccionado;
            set
            {
                if (ReferenceEquals(
                        colorSeleccionado,
                        value))
                {
                    return;
                }

                colorSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                    ColorHex = value.Hex;
            }
        }

        public Color ColorVistaPrevia
        {
            get
            {
                try
                {
                    return ColorRegex.IsMatch(
                            ColorHex.Trim())
                        ? Color.FromArgb(
                            ColorHex.Trim())
                        : Color.FromArgb(
                            "#D1D5DB");
                }
                catch
                {
                    return Color.FromArgb(
                        "#D1D5DB");
                }
            }
        }

        public string OrdenTexto
        {
            get => ordenTexto;
            set
            {
                string nuevo =
                    value ?? string.Empty;

                if (ordenTexto == nuevo)
                    return;

                ordenTexto = nuevo;
                OnPropertyChanged();
            }
        }

        public bool PuedeAcceder =>
            preparado &&
            (EsEdicion
                ? CanView && CanEdit
                : CanAdd);

        public bool PuedeGuardar =>
            PuedeAcceder &&
            datosCargados;

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public void Preparar(int categoriaId)
        {
            CancelarCarga();

            preparado = true;
            datosCargados = categoriaId <= 0;

            categoriaPublicacionId =
                Math.Max(0, categoriaId);

            LimpiarCampos();
            CapturarEstadoOriginal();

            OnPropertyChanged(
                nameof(CategoriaPublicacionId));

            OnPropertyChanged(
                nameof(EsEdicion));

            OnPropertyChanged(
                nameof(TituloPagina));

            OnPropertyChanged(
                nameof(TextoBoton));

            OnPropertyChanged(
                nameof(PuedeAcceder));

            OnPropertyChanged(
                nameof(PuedeGuardar));

            GuardarCommand.ChangeCanExecute();
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                InterfazCodigos.CategoriasPublicacion);

            OnPropertyChanged(
                nameof(PuedeAcceder));

            OnPropertyChanged(
                nameof(PuedeGuardar));

            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }

        public async Task InicializarAsync()
        {
            if (!preparado ||
                !PuedeAcceder)
            {
                return;
            }

            if (!EsEdicion)
            {
                datosCargados = true;
                CapturarEstadoOriginal();
                NotificarEstadoGuardado();
                return;
            }

            if (datosCargados || IsBusy)
                return;

            CancelarCarga();

            cargaCancellationTokenSource?.Dispose();

            var source =
                new CancellationTokenSource();

            cargaCancellationTokenSource =
                source;

            try
            {
                IsBusy = true;
                NotificarEstadoGuardado();

                ApiResult<
                    CategoriaPublicacionCatalogoResponse>
                    result =
                        await apiService.ObtenerAsync(
                            CategoriaPublicacionId,
                            source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success ||
                    result.Data == null)
                {
                    if (!string.Equals(
                            result.Message,
                            "La operación fue cancelada.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        await MostrarErrorAsync(
                            result.Message);
                    }

                    /*
                     * Un 404 en edición significa que el registro ya no existe
                     * o dejó de estar activo. No se habilita un formulario con
                     * datos obsoletos.
                     */
                    if (result.StatusCode == 404)
                    {
                        await GoToAsyncParameters(
                            AppRoutes.Regresar);
                    }

                    return;
                }

                if (!result.Data.Activo)
                {
                    await MostrarAdvertenciaAsync(
                        "El tipo de publicación ya no se encuentra activo.");

                    await GoToAsyncParameters(
                        AppRoutes.Regresar);

                    return;
                }

                AplicarDatos(result.Data);
                datosCargados = true;
                CapturarEstadoOriginal();
                NotificarEstadoGuardado();
            }
            catch (OperationCanceledException)
            {
                // El formulario se cerró antes de completar la consulta.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested)
                {
                    await MostrarErrorInesperadoAsync(
                        "cargar el tipo de publicación",
                        ex);
                }
            }
            finally
            {
                IsBusy = false;

                if (ReferenceEquals(
                        cargaCancellationTokenSource,
                        source))
                {
                    cargaCancellationTokenSource.Dispose();
                    cargaCancellationTokenSource = null;
                }
                else
                {
                    source.Dispose();
                }

                NotificarEstadoGuardado();
                CancelarCommand.ChangeCanExecute();
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                cargaCancellationTokenSource;

            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void AplicarDatos(
            CategoriaPublicacionCatalogoResponse item)
        {
            Nombre =
                item.NombreCategoriaPublicacion ??
                string.Empty;

            Descripcion =
                item.DescripcionCategoriaPublicacion ??
                string.Empty;

            OrdenTexto =
                item.Orden.ToString();

            ColorHex =
                string.IsNullOrWhiteSpace(
                    item.ColorHex)
                    ? "#3B655B"
                    : item.ColorHex
                        .Trim()
                        .ToUpperInvariant();

            ColorSeleccionado =
                Colores.FirstOrDefault(
                    x => string.Equals(
                        x.Hex,
                        ColorHex,
                        StringComparison.OrdinalIgnoreCase));
        }

        private void LimpiarCampos()
        {
            Nombre = string.Empty;
            Descripcion = string.Empty;
            OrdenTexto = "1";
            ColorHex = "#3B655B";

            ColorSeleccionado =
                Colores.FirstOrDefault(
                    x => string.Equals(
                        x.Hex,
                        ColorHex,
                        StringComparison.OrdinalIgnoreCase));
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (!PuedeGuardar)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para guardar tipos de publicación o los datos aún no están disponibles.");
                return;
            }

            string? error =
                Validar();

            if (!string.IsNullOrWhiteSpace(error))
            {
                await MostrarAdvertenciaAsync(error);
                return;
            }

            if (EsEdicion &&
                !HayCambios())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            int orden =
                int.Parse(
                    OrdenTexto.Trim());

            var request =
                new CategoriaPublicacionGuardarRequest
                {
                    /*
                     * Se conserva la presentación escrita por el usuario.
                     * Backend normaliza únicamente para comparar duplicados.
                     */
                    NombreCategoriaPublicacion =
                        Nombre
                            .ReplaceLineEndings(" ")
                            .Trim(),

                    DescripcionCategoriaPublicacion =
                        Descripcion.Trim(),

                    ColorHex =
                        ColorHex
                            .Trim()
                            .ToUpperInvariant(),

                    Orden = orden
                };

            bool confirmar =
                EsEdicion
                    ? await ConfirmarActualizacionAsync(
                        $"el tipo de publicación “{request.NombreCategoriaPublicacion}”")
                    : await ConfirmarGuardadoAsync(
                        $"el tipo de publicación “{request.NombreCategoriaPublicacion}”");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                GuardarCommand.ChangeCanExecute();
                CancelarCommand.ChangeCanExecute();

                ApiResult<bool> result =
                    EsEdicion
                        ? await apiService.ActualizarAsync(
                            CategoriaPublicacionId,
                            request)
                        : await apiService.CrearAsync(
                            request);

                if (!result.Success)
                {
                    if (string.Equals(
                            result.Message,
                            "La creación fue cancelada.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    await MostrarErrorAsync(
                        result.Message);

                    return;
                }

                CapturarEstadoOriginal();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        result.Message)
                        ? "Tipo de publicación guardado correctamente."
                        : result.Message);

                await GoToAsyncParameters(
                    AppRoutes.Regresar);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el tipo de publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
                GuardarCommand.ChangeCanExecute();
                CancelarCommand.ChangeCanExecute();
            }
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            if (HayCambios())
            {
                bool confirmar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirmar)
                    return;
            }

            await GoToAsyncParameters(
                AppRoutes.Regresar);
        }

        private string? Validar()
        {
            Nombre =
                Nombre
                    .ReplaceLineEndings(" ")
                    .Trim();

            Descripcion =
                Descripcion.Trim();

            ColorHex =
                ColorHex.Trim();

            OrdenTexto =
                OrdenTexto.Trim();

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                return
                    "Debe escribir el nombre del tipo de publicación.";
            }

            if (Nombre.Length > 80)
            {
                return
                    "El nombre puede contener como máximo 80 caracteres.";
            }

            if (Descripcion.Length > 250)
            {
                return
                    "La descripción puede contener como máximo 250 caracteres.";
            }

            if (!ColorRegex.IsMatch(ColorHex))
            {
                return
                    "El color debe tener el formato hexadecimal #RRGGBB, por ejemplo #3B655B.";
            }

            if (!int.TryParse(
                    OrdenTexto,
                    out int orden) ||
                orden < 0 ||
                orden > 9999)
            {
                return
                    "El orden debe ser un número entero entre 0 y 9999.";
            }

            return null;
        }

        private bool HayCambios()
        {
            string nombreActual =
                Nombre
                    .ReplaceLineEndings(" ")
                    .Trim();

            string descripcionActual =
                Descripcion.Trim();

            string colorActual =
                ColorHex
                    .Trim()
                    .ToUpperInvariant();

            string ordenActual =
                NormalizarOrdenParaComparar(
                    OrdenTexto);

            return
                !string.Equals(
                    nombreActual,
                    nombreOriginal,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    descripcionActual,
                    descripcionOriginal,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    colorActual,
                    colorHexOriginal,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    ordenActual,
                    ordenOriginal,
                    StringComparison.Ordinal);
        }

        private void CapturarEstadoOriginal()
        {
            nombreOriginal =
                Nombre
                    .ReplaceLineEndings(" ")
                    .Trim();

            descripcionOriginal =
                Descripcion.Trim();

            colorHexOriginal =
                ColorHex
                    .Trim()
                    .ToUpperInvariant();

            ordenOriginal =
                NormalizarOrdenParaComparar(
                    OrdenTexto);
        }

        private static string NormalizarOrdenParaComparar(
            string? valor)
        {
            string texto =
                (valor ?? string.Empty)
                    .Trim();

            return int.TryParse(
                    texto,
                    out int numero)
                ? numero.ToString()
                : texto;
        }

        private void NotificarEstadoGuardado()
        {
            OnPropertyChanged(
                nameof(PuedeAcceder));

            OnPropertyChanged(
                nameof(PuedeGuardar));

            GuardarCommand.ChangeCanExecute();
        }
    }
}
