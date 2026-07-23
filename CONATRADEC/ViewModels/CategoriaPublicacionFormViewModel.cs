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

        private readonly CategoriaPublicacionApiService apiService = new();

        private int categoriaPublicacionId;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string colorHex = "#3B655B";
        private string ordenTexto = "1";
        private ColorPublicacionOption? colorSeleccionado;
        private bool preparado;

        public CategoriaPublicacionFormViewModel()
        {
            Colores = new ObservableCollection<ColorPublicacionOption>
            {
                new() { Nombre = "Verde institucional", Hex = "#3B655B" },
                new() { Nombre = "Café", Hex = "#9B552C" },
                new() { Nombre = "Naranja", Hex = "#FF9800" },
                new() { Nombre = "Amarillo", Hex = "#F2C94C" },
                new() { Nombre = "Azul", Hex = "#2F80ED" },
                new() { Nombre = "Rojo", Hex = "#D64545" },
                new() { Nombre = "Morado", Hex = "#7B61FF" },
                new() { Nombre = "Gris", Hex = "#6B7280" }
            };

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && PuedeGuardar);

            CancelarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<ColorPublicacionOption> Colores { get; }

        public int CategoriaPublicacionId => categoriaPublicacionId;
        public bool EsEdicion => CategoriaPublicacionId > 0;

        public string TituloPagina =>
            EsEdicion
                ? "Editar tipo de publicación"
                : "Nuevo tipo de publicación";

        public string TextoBoton =>
            EsEdicion ? "Guardar cambios" : "Crear tipo";

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CaracteresDescripcion));
            }
        }

        public string CaracteresDescripcion =>
            $"{Descripcion.Length}/250";

        public string ColorHex
        {
            get => colorHex;
            set
            {
                string nuevo = value ?? string.Empty;

                if (colorHex == nuevo)
                    return;

                colorHex = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorVistaPrevia));

                ColorPublicacionOption? coincidencia = Colores
                    .FirstOrDefault(x => string.Equals(
                        x.Hex,
                        colorHex,
                        StringComparison.OrdinalIgnoreCase));

                if (!ReferenceEquals(colorSeleccionado, coincidencia))
                {
                    colorSeleccionado = coincidencia;
                    OnPropertyChanged(nameof(ColorSeleccionado));
                }
            }
        }

        public ColorPublicacionOption? ColorSeleccionado
        {
            get => colorSeleccionado;
            set
            {
                if (ReferenceEquals(colorSeleccionado, value))
                    return;

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
                    return ColorRegex.IsMatch(ColorHex)
                        ? Color.FromArgb(ColorHex)
                        : Color.FromArgb("#D1D5DB");
                }
                catch
                {
                    return Color.FromArgb("#D1D5DB");
                }
            }
        }

        public string OrdenTexto
        {
            get => ordenTexto;
            set
            {
                ordenTexto = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool PuedeGuardar =>
            EsEdicion ? CanEdit : CanAdd;

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public void Preparar(CategoriaPublicacionCatalogoResponse? item)
        {
            if (preparado)
                return;

            preparado = true;
            categoriaPublicacionId = item?.CategoriaPublicacionId ?? 0;
            Nombre = item?.NombreCategoriaPublicacion ?? string.Empty;
            Descripcion = item?.DescripcionCategoriaPublicacion ?? string.Empty;
            OrdenTexto = (item?.Orden ?? 1).ToString();
            ColorHex = string.IsNullOrWhiteSpace(item?.ColorHex)
                ? "#3B655B"
                : item.ColorHex.ToUpperInvariant();

            ColorSeleccionado = Colores.FirstOrDefault(x =>
                string.Equals(
                    x.Hex,
                    ColorHex,
                    StringComparison.OrdinalIgnoreCase));

            OnPropertyChanged(nameof(CategoriaPublicacionId));
            OnPropertyChanged(nameof(EsEdicion));
            OnPropertyChanged(nameof(TituloPagina));
            OnPropertyChanged(nameof(TextoBoton));
            OnPropertyChanged(nameof(PuedeGuardar));
            GuardarCommand.ChangeCanExecute();
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("categoriaPublicacionPage");
            OnPropertyChanged(nameof(PuedeGuardar));
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (!PuedeGuardar)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para guardar tipos de publicación.");
                return;
            }

            string? error = Validar();

            if (!string.IsNullOrWhiteSpace(error))
            {
                await MostrarAdvertenciaAsync(error);
                return;
            }

            int orden = int.Parse(OrdenTexto.Trim());

            var request = new CategoriaPublicacionGuardarRequest
            {
                NombreCategoriaPublicacion = Nombre.Trim(),
                DescripcionCategoriaPublicacion = Descripcion.Trim(),
                ColorHex = ColorHex.Trim().ToUpperInvariant(),
                Orden = orden
            };

            bool confirmar = EsEdicion
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

                ApiResult<bool> result = EsEdicion
                    ? await apiService.ActualizarAsync(
                        CategoriaPublicacionId,
                        request)
                    : await apiService.CrearAsync(request);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(result.Message);
                await GoToAsyncParameters(AppRoutes.Regresar);
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

        private string? Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return "Debe escribir el nombre del tipo de publicación.";

            if (Nombre.Trim().Length > 80)
                return "El nombre puede contener como máximo 80 caracteres.";

            if (Descripcion.Length > 250)
                return "La descripción puede contener como máximo 250 caracteres.";

            if (!ColorRegex.IsMatch(ColorHex.Trim()))
                return "El color debe tener el formato hexadecimal #RRGGBB, por ejemplo #3B655B.";

            if (!int.TryParse(OrdenTexto.Trim(), out int orden) ||
                orden < 0 ||
                orden > 9999)
            {
                return "El orden debe ser un número entero entre 0 y 9999.";
            }

            return null;
        }
    }
}
