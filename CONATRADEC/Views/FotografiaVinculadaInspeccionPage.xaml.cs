using System.Text.RegularExpressions;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Modal compacto utilizado cuando una fotografía del Álbum Botánico
    /// pertenece a una inspección fitosanitaria. La información secundaria se
    /// mantiene plegada para priorizar la acción que el usuario debe realizar.
    /// </summary>
    public partial class FotografiaVinculadaInspeccionPage : ContentPage
    {
        private readonly TaskCompletionSource<bool> resultado =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool detallesVisibles;
        private bool cerrando;

        public Task<bool> ResultadoTask => resultado.Task;

        public FotografiaVinculadaInspeccionPage(
            int inspeccionId,
            string? mensajeBackend)
        {
            InitializeComponent();
            CargarInformacion(inspeccionId, mensajeBackend);
        }

        private void CargarInformacion(
            int inspeccionId,
            string? mensajeBackend)
        {
            string mensaje = mensajeBackend?.Trim() ?? string.Empty;
            DatosVinculo datos = ParsearDatos(inspeccionId, mensaje);

            InspeccionLabel.Text = datos.NombreInspeccion.Length > 0
                ? $"Inspección #{datos.InspeccionId} · {datos.NombreInspeccion}"
                : $"Inspección #{datos.InspeccionId}";

            var resumen = new List<string>();
            if (datos.Terreno.Length > 0)
                resumen.Add($"Terreno {datos.Terreno}");
            if (datos.Fotografia.Length > 0)
                resumen.Add(datos.Fotografia);

            TerrenoFotoLabel.Text = resumen.Count > 0
                ? string.Join(" · ", resumen)
                : "Evidencia publicada desde la inspección";

            FechaIdentificacionLabel.Text = datos.FechaInspeccion.Length > 0
                ? $"Identificación: {datos.FechaInspeccion}"
                : "La evidencia conserva su trazabilidad original.";

            TecnicoLabel.Text = TextoOAlternativa(
                datos.Tecnico,
                "No especificado");
            PublicadaPorLabel.Text = TextoOAlternativa(
                datos.PublicadaPor,
                "No especificado");
            FechaPublicacionLabel.Text = TextoOAlternativa(
                datos.FechaPublicacion,
                "No especificada");

            DetalleFallbackLabel.IsVisible = datos.MostrarMensajeOriginal;
            DetalleFallbackLabel.Text = datos.MostrarMensajeOriginal
                ? mensaje
                : string.Empty;
        }

        private static DatosVinculo ParsearDatos(
            int inspeccionId,
            string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return new DatosVinculo
                {
                    InspeccionId = inspeccionId,
                    MostrarMensajeOriginal = false
                };
            }

            const string patron =
                @"Inspección:\s*#(?<id>\d+)\s*·\s*(?<nombre>.*?)\s+" +
                @"Terreno:\s*(?<terreno>.*?)\s+" +
                @"Fecha de inspección:\s*(?<fechaInspeccion>.*?)\s+" +
                @"Fotografía:\s*(?<fotografia>.*?)\s+" +
                @"Técnico:\s*(?<tecnico>.*?)\s+" +
                @"Publicada por:\s*(?<publicadaPor>.*?)\s+" +
                @"Fecha de publicación:\s*(?<fechaPublicacion>.*?)\s+" +
                @"Para retirarla";

            Match match = Regex.Match(
                mensaje,
                patron,
                RegexOptions.CultureInvariant |
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline);

            if (!match.Success)
            {
                return new DatosVinculo
                {
                    InspeccionId = inspeccionId,
                    MostrarMensajeOriginal = true
                };
            }

            int idDetectado = inspeccionId;
            if (int.TryParse(match.Groups["id"].Value, out int idParseado) &&
                idParseado > 0)
            {
                idDetectado = idParseado;
            }

            return new DatosVinculo
            {
                InspeccionId = idDetectado,
                NombreInspeccion = Limpiar(match.Groups["nombre"].Value),
                Terreno = Limpiar(match.Groups["terreno"].Value),
                FechaInspeccion = Limpiar(
                    match.Groups["fechaInspeccion"].Value),
                Fotografia = NormalizarFotografia(
                    match.Groups["fotografia"].Value),
                Tecnico = Limpiar(match.Groups["tecnico"].Value),
                PublicadaPor = Limpiar(match.Groups["publicadaPor"].Value),
                FechaPublicacion = Limpiar(
                    match.Groups["fechaPublicacion"].Value),
                MostrarMensajeOriginal = false
            };
        }

        private static string NormalizarFotografia(string valor)
        {
            string limpio = Limpiar(valor);
            if (limpio.Length == 0)
                return string.Empty;

            return limpio.StartsWith("Fotografía", StringComparison.OrdinalIgnoreCase)
                ? limpio
                : $"Fotografía {limpio}";
        }

        private static string Limpiar(string? valor) =>
            Regex.Replace(
                    valor?.Trim() ?? string.Empty,
                    @"\s+",
                    " ",
                    RegexOptions.CultureInvariant)
                .Trim();

        private static string TextoOAlternativa(
            string valor,
            string alternativa) =>
            string.IsNullOrWhiteSpace(valor)
                ? alternativa
                : valor;

        private void OnVerDetallesClicked(object? sender, EventArgs e)
        {
            detallesVisibles = !detallesVisibles;
            DetallesContainer.IsVisible = detallesVisibles;
            VerDetallesButton.Text = detallesVisibles
                ? "Ocultar detalles ▴"
                : "Ver detalles ▾";
        }

        private async void OnIrInspeccionClicked(
            object? sender,
            EventArgs e) =>
            await CerrarAsync(true);

        private async void OnCerrarClicked(
            object? sender,
            EventArgs e) =>
            await CerrarAsync(false);

        private async Task CerrarAsync(bool irInspeccion)
        {
            if (cerrando)
                return;

            cerrando = true;

            try
            {
                if (Navigation.ModalStack.Contains(this))
                    await Navigation.PopModalAsync();
            }
            finally
            {
                resultado.TrySetResult(irInspeccion);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CerrarAsync(false);
            return true;
        }

        private sealed class DatosVinculo
        {
            public int InspeccionId { get; init; }
            public string NombreInspeccion { get; init; } = string.Empty;
            public string Terreno { get; init; } = string.Empty;
            public string FechaInspeccion { get; init; } = string.Empty;
            public string Fotografia { get; init; } = string.Empty;
            public string Tecnico { get; init; } = string.Empty;
            public string PublicadaPor { get; init; } = string.Empty;
            public string FechaPublicacion { get; init; } = string.Empty;
            public bool MostrarMensajeOriginal { get; init; }
        }
    }
}
