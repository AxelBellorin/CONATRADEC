using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(nameof(Modo), "Modo")]
    [QueryProperty(nameof(Item), "Item")]
    public sealed class TipoFotografiaIAFormViewModel : GlobalService
    {
        private readonly TipoFotografiaIAApiService api = new();

        private string modo = "Crear";
        private TipoFotografiaIAItem? item;
        private string codigo = string.Empty;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string instruccionIA = string.Empty;
        private string ordenTexto = "1";

        public TipoFotografiaIAFormViewModel()
        {
            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => PuedeGuardar);

            CancelarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
        }

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public string Modo
        {
            get => modo;
            set
            {
                string nuevo = string.IsNullOrWhiteSpace(value)
                    ? "Crear"
                    : value.Trim();

                if (modo == nuevo)
                    return;

                modo = nuevo;
                OnPropertyChanged();
                NotificarModo();
            }
        }

        public TipoFotografiaIAItem? Item
        {
            get => item;
            set
            {
                if (ReferenceEquals(item, value))
                    return;

                item = value;
                OnPropertyChanged();
                AplicarItem();
            }
        }

        public string Codigo
        {
            get => codigo;
            set
            {
                string nuevo = NormalizarCodigo(value);
                if (codigo == nuevo)
                    return;

                codigo = nuevo;
                OnPropertyChanged();
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                string nuevo = value ?? string.Empty;
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
                string nuevo = value ?? string.Empty;
                if (descripcion == nuevo)
                    return;

                descripcion = nuevo;
                OnPropertyChanged();
            }
        }

        public string InstruccionIA
        {
            get => instruccionIA;
            set
            {
                string nuevo = value ?? string.Empty;
                if (instruccionIA == nuevo)
                    return;

                instruccionIA = nuevo;
                OnPropertyChanged();
            }
        }

        public string OrdenTexto
        {
            get => ordenTexto;
            set
            {
                string nuevo = value ?? string.Empty;
                if (ordenTexto == nuevo)
                    return;

                ordenTexto = nuevo;
                OnPropertyChanged();
            }
        }

        public bool EsCrear =>
            string.Equals(Modo, "Crear", StringComparison.OrdinalIgnoreCase);

        public bool EsEditar =>
            string.Equals(Modo, "Editar", StringComparison.OrdinalIgnoreCase);

        public bool EsVer => !EsCrear && !EsEditar;

        public bool EsSoloLectura => EsVer;
        public bool CodigoSoloLectura => !EsCrear || EsSoloLectura;
        public bool MostrarGuardar => !EsVer;

        public string TituloPagina => EsCrear
            ? "Nuevo tipo de fotografía"
            : EsEditar
                ? "Editar tipo de fotografía"
                : "Detalle del tipo de fotografía";

        public string SubtituloPagina =>
            "La instrucción orienta a Gemini sobre qué debe observar con mayor atención en cada imagen.";

        private bool PuedeGuardar =>
            !IsBusy &&
            MostrarGuardar &&
            (EsCrear
                ? PermissionService.Instance.HasAdd(
                    TipoFotografiaIARoutes.InterfazConfiguracion)
                : PermissionService.Instance.HasUpdate(
                    TipoFotografiaIARoutes.InterfazConfiguracion));

        private void AplicarItem()
        {
            if (Item == null)
                return;

            Codigo = Item.Codigo;
            Nombre = Item.Nombre;
            Descripcion = Item.Descripcion;
            InstruccionIA = Item.InstruccionIA;
            OrdenTexto = Item.Orden.ToString();
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar)
                return;

            string codigoNormalizado = NormalizarCodigo(Codigo);
            string nombreNormalizado = (Nombre ?? string.Empty).Trim();
            string descripcionNormalizada =
                (Descripcion ?? string.Empty).Trim();
            string instruccionNormalizada =
                (InstruccionIA ?? string.Empty).Trim();

            if (codigoNormalizado.Length is < 2 or > 40)
            {
                await MostrarAdvertenciaAsync(
                    "El código debe tener entre 2 y 40 caracteres.");
                return;
            }

            if (nombreNormalizado.Length is < 2 or > 100)
            {
                await MostrarAdvertenciaAsync(
                    "El nombre debe tener entre 2 y 100 caracteres.");
                return;
            }

            if (instruccionNormalizada.Length is < 20 or > 2000)
            {
                await MostrarAdvertenciaAsync(
                    "La instrucción para la IA debe tener entre 20 y 2000 caracteres.");
                return;
            }

            if (!int.TryParse(OrdenTexto, out int orden) ||
                orden is < 1 or > 999)
            {
                await MostrarAdvertenciaAsync(
                    "El orden debe ser un número entre 1 y 999.");
                return;
            }

            bool confirmar = EsCrear
                ? await ConfirmarGuardadoAsync("tipo de fotografía")
                : await ConfirmarActualizacionAsync("tipo de fotografía");

            if (!confirmar)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                var request = new TipoFotografiaIARequest
                {
                    Codigo = codigoNormalizado,
                    Nombre = nombreNormalizado,
                    Descripcion = descripcionNormalizada,
                    InstruccionIA = instruccionNormalizada,
                    Orden = orden
                };

                ApiResult<TipoFotografiaIAItem> result = EsCrear
                    ? await api.CrearAsync(request)
                    : await api.ActualizarAsync(
                        Item?.TipoFotografiaIAId ?? 0,
                        request);

                if (!result.Success)
                {
                    await GlobalService.MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Tipo de fotografía guardado correctamente."
                        : result.Message);

                await GoToAsyncParameters(AppRoutes.Regresar);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private static string NormalizarCodigo(string? valor)
        {
            string texto = (valor ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(' ', '_');

            while (texto.Contains("__", StringComparison.Ordinal))
                texto = texto.Replace("__", "_", StringComparison.Ordinal);

            return texto.Length <= 40 ? texto : texto[..40];
        }

        private void NotificarModo()
        {
            OnPropertyChanged(nameof(EsCrear));
            OnPropertyChanged(nameof(EsEditar));
            OnPropertyChanged(nameof(EsVer));
            OnPropertyChanged(nameof(EsSoloLectura));
            OnPropertyChanged(nameof(CodigoSoloLectura));
            OnPropertyChanged(nameof(MostrarGuardar));
            OnPropertyChanged(nameof(TituloPagina));
            OnPropertyChanged(nameof(SubtituloPagina));
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }
    }
}
