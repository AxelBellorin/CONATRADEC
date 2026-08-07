using CONATRADEC.Models;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Presenta una fotografía durante la revisión guiada del analizador.
    /// La página únicamente captura la decisión del usuario; las operaciones
    /// contra la API continúan ejecutándose desde el flujo principal.
    /// </summary>
    public partial class RevisionAnalizadorFotografiaPage : ContentPage
    {
        private readonly IReadOnlyList<InspeccionFotoV2> fotografias;
        private readonly int indice;
        private readonly TaskCompletionSource<RevisionAnalizadorAccion>
            resultadoSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private bool resultadoResuelto;
        private bool cierreEnCurso;

        public RevisionAnalizadorFotografiaPage(
            IReadOnlyList<InspeccionFotoV2> fotografias,
            int indice)
        {
            ArgumentNullException.ThrowIfNull(fotografias);

            if (fotografias.Count == 0)
            {
                throw new ArgumentException(
                    "Debe existir al menos una fotografía para iniciar la revisión.",
                    nameof(fotografias));
            }

            if (indice < 0 || indice >= fotografias.Count)
                throw new ArgumentOutOfRangeException(nameof(indice));

            this.fotografias = fotografias;
            this.indice = indice;

            InitializeComponent();
            CargarFotografia();
        }

        /// <summary>
        /// Tarea esperada por el flujo principal para conocer la decisión tomada.
        /// </summary>
        public Task<RevisionAnalizadorAccion> ResultadoTask =>
            resultadoSource.Task;

        private InspeccionFotoV2 Fotografia => fotografias[indice];

        private void CargarFotografia()
        {
            InspeccionFotoV2 foto = Fotografia;
            InspeccionFotoResultadoIAV2? resultadoIa = foto.ResultadoIA;

            TituloRevisionLabel.Text =
                $"Revisión guiada · Fotografía {indice + 1} de {fotografias.Count}";
            SubtituloFotografiaLabel.Text = foto.Titulo;
            FotografiaImage.Source = CrearOrigenImagen(foto.UrlImagen);

            DiagnosticoIaLabel.Text = resultadoIa?.DiagnosticoVisible ??
                "Sin diagnóstico preliminar de IA";

            ResumenIaLabel.Text = string.IsNullOrWhiteSpace(
                resultadoIa?.ResumenImagen)
                    ? "Revise visualmente la evidencia antes de tomar una decisión."
                    : resultadoIa!.ResumenImagen;

            var detalles = new List<string>();

            if (!string.IsNullOrWhiteSpace(resultadoIa?.NivelCerteza))
                detalles.Add($"Certeza: {resultadoIa.NivelCerteza}");

            if (!string.IsNullOrWhiteSpace(resultadoIa?.SeveridadVisual))
                detalles.Add($"Severidad: {resultadoIa.SeveridadVisual}");

            if (!string.IsNullOrWhiteSpace(resultadoIa?.CategoriaPrincipal))
            {
                detalles.Add(
                    $"Categoría: {resultadoIa.CategoriaPrincipal.Replace('_', ' ')}");
            }

            DetalleIaLabel.Text = string.Join(" · ", detalles);
            DetalleIaLabel.IsVisible = detalles.Count > 0;
        }

        private async void OnAmpliarImagenTapped(
            object? sender,
            TappedEventArgs e) =>
            await AbrirVisorAsync();

        private async void OnAmpliarImagenClicked(
            object? sender,
            EventArgs e) =>
            await AbrirVisorAsync();

        private async Task AbrirVisorAsync()
        {
            var visor = new VisorFotografiaFitosanitariaPage(
                fotografias,
                indice);

            await Navigation.PushModalAsync(visor, animated: false);
        }

        private async void OnConfirmarClicked(object? sender, EventArgs e) =>
            await CerrarAsync(RevisionAnalizadorAccion.Confirmar);

        private async void OnCorregirClicked(object? sender, EventArgs e) =>
            await CerrarAsync(RevisionAnalizadorAccion.Corregir);

        private async void OnDevolverTecnicoClicked(
            object? sender,
            EventArgs e) =>
            await CerrarAsync(RevisionAnalizadorAccion.DevolverTecnico);

        private async void OnOmitirClicked(object? sender, EventArgs e) =>
            await CerrarAsync(RevisionAnalizadorAccion.Omitir);

        private async void OnCancelarClicked(object? sender, EventArgs e) =>
            await CerrarAsync(RevisionAnalizadorAccion.Cancelar);

        private static ImageSource? CrearOrigenImagen(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return null;

            string valor = ruta.Trim();

            return Uri.TryCreate(valor, UriKind.Absolute, out Uri? uri)
                ? ImageSource.FromUri(uri)
                : ImageSource.FromFile(valor);
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CerrarAsync(RevisionAnalizadorAccion.Cancelar);
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (!resultadoResuelto && !Navigation.ModalStack.Contains(this))
            {
                resultadoResuelto = true;
                resultadoSource.TrySetResult(
                    RevisionAnalizadorAccion.Cancelar);
            }
        }

        private async Task CerrarAsync(RevisionAnalizadorAccion accion)
        {
            if (cierreEnCurso || resultadoResuelto)
                return;

            cierreEnCurso = true;
            resultadoResuelto = true;

            try
            {
                IReadOnlyList<Page> modales = Navigation.ModalStack;

                if (modales.Count > 0 && ReferenceEquals(modales[^1], this))
                    await Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                resultadoSource.TrySetResult(accion);
                cierreEnCurso = false;
            }
        }
    }
}
