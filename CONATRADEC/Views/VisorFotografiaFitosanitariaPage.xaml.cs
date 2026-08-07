using CONATRADEC.Models;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Visor modal reutilizable para fotografías fitosanitarias.
    /// Permite avanzar y retroceder dentro de la colección recibida.
    /// </summary>
    public partial class VisorFotografiaFitosanitariaPage : ContentPage
    {
        private readonly IReadOnlyList<InspeccionFotoV2> fotografias;
        private int indiceActual;
        private bool cierreEnCurso;

        public VisorFotografiaFitosanitariaPage(
            IReadOnlyList<InspeccionFotoV2> fotografias,
            int indiceInicial)
        {
            ArgumentNullException.ThrowIfNull(fotografias);

            if (fotografias.Count == 0)
            {
                throw new ArgumentException(
                    "El visor necesita al menos una fotografía.",
                    nameof(fotografias));
            }

            if (indiceInicial < 0 || indiceInicial >= fotografias.Count)
                throw new ArgumentOutOfRangeException(nameof(indiceInicial));

            this.fotografias = fotografias;
            indiceActual = indiceInicial;

            InitializeComponent();
            MostrarFotografiaActual();
        }

        private void OnAnteriorClicked(object? sender, EventArgs e) =>
            CambiarIndice(-1);

        private void OnSiguienteClicked(object? sender, EventArgs e) =>
            CambiarIndice(1);

        private async void OnCerrarClicked(object? sender, EventArgs e) =>
            await CerrarAsync();

        private void CambiarIndice(int desplazamiento)
        {
            int nuevoIndice = indiceActual + desplazamiento;

            if (nuevoIndice < 0 || nuevoIndice >= fotografias.Count)
                return;

            indiceActual = nuevoIndice;
            MostrarFotografiaActual();
        }

        private void MostrarFotografiaActual()
        {
            InspeccionFotoV2 foto = fotografias[indiceActual];

            TituloFotografiaLabel.Text = foto.Titulo;
            ContadorFotografiaLabel.Text =
                $"{indiceActual + 1}/{fotografias.Count}";
            DiagnosticoFotografiaLabel.Text =
                foto.ResultadoIA?.DiagnosticoVisible ??
                "Sin diagnóstico preliminar de IA";
            FotografiaImage.Source = CrearOrigenImagen(foto.UrlImagen);

            AnteriorButton.IsEnabled = indiceActual > 0;
            SiguienteButton.IsEnabled =
                indiceActual < fotografias.Count - 1;

            AnteriorButton.Opacity = AnteriorButton.IsEnabled ? 1 : 0.45;
            SiguienteButton.Opacity = SiguienteButton.IsEnabled ? 1 : 0.45;
        }

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
            _ = CerrarAsync();
            return true;
        }

        private async Task CerrarAsync()
        {
            if (cierreEnCurso)
                return;

            cierreEnCurso = true;

            try
            {
                IReadOnlyList<Page> modales = Navigation.ModalStack;

                if (modales.Count > 0 && ReferenceEquals(modales[^1], this))
                    await Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                cierreEnCurso = false;
            }
        }
    }
}
