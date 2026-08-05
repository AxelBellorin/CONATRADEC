using CONATRADEC.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Formulario visual de reevaluación para una sola fotografía.
    /// Une en una misma pantalla la evidencia, el resultado anterior,
    /// el motivo obligatorio y el diagnóstico opcional.
    /// </summary>
    public partial class RevisionIAFotografiaPage : ContentPage
    {
        private readonly TaskCompletionSource<RevisionIAFormularioResultado?>
            resultadoSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private bool resultadoResuelto;
        private bool cierreEnCurso;

        public RevisionIAFotografiaPage(
            InspeccionFotoV2 fotografia,
            int posicion,
            int total)
        {
            ArgumentNullException.ThrowIfNull(fotografia);

            InitializeComponent();
            BindingContext = new RevisionIAFormularioContext(
                fotografia,
                posicion,
                total);
        }

        private RevisionIAFormularioContext Formulario =>
            (RevisionIAFormularioContext)BindingContext;

        public Task<RevisionIAFormularioResultado?>
            EsperarResultadoAsync() => resultadoSource.Task;

        private void OnAmpliarImagenTapped(object? sender, TappedEventArgs e) =>
            Formulario.ImagenAmpliadaVisible = true;

        private void OnAmpliarImagenClicked(object? sender, EventArgs e) =>
            Formulario.ImagenAmpliadaVisible = true;

        private void OnCerrarImagenClicked(object? sender, EventArgs e) =>
            Formulario.ImagenAmpliadaVisible = false;

        private async void OnCancelarClicked(object? sender, EventArgs e) =>
            await CerrarAsync(null);

        private async void OnConfirmarClicked(object? sender, EventArgs e)
        {
            Formulario.LimpiarError();

            string motivo = Formulario.Motivo.Trim();
            if (motivo.Length < 8)
            {
                Formulario.MostrarMensajeError(
                    "Explique qué debe revisar la IA con al menos 8 caracteres.");
                return;
            }

            string? diagnostico = null;
            if (Formulario.TieneDiagnosticoPropuesto)
            {
                diagnostico = Formulario.DiagnosticoPropuesto.Trim();
                if (diagnostico.Length < 2)
                {
                    Formulario.MostrarMensajeError(
                        "Escriba el diagnóstico que desea confirmar o desmentir, o desactive esa opción.");
                    return;
                }
            }

            await CerrarAsync(
                new RevisionIAFormularioResultado(
                    motivo,
                    string.IsNullOrWhiteSpace(diagnostico)
                        ? null
                        : diagnostico));
        }

        protected override bool OnBackButtonPressed()
        {
            if (Formulario.ImagenAmpliadaVisible)
            {
                Formulario.ImagenAmpliadaVisible = false;
                return true;
            }

            _ = CerrarAsync(null);
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (!resultadoResuelto)
            {
                resultadoResuelto = true;
                resultadoSource.TrySetResult(null);
            }
        }

        private async Task CerrarAsync(
            RevisionIAFormularioResultado? resultado)
        {
            if (cierreEnCurso)
                return;

            cierreEnCurso = true;

            resultadoResuelto = true;

            try
            {
                IReadOnlyList<Page> paginasModales = Navigation.ModalStack;
                if (paginasModales.Count > 0 &&
                    ReferenceEquals(
                        paginasModales[paginasModales.Count - 1],
                        this))
                {
                    await Navigation.PopModalAsync(animated: false);
                }
            }
            finally
            {
                resultadoSource.TrySetResult(resultado);
                cierreEnCurso = false;
            }
        }
    }

    public sealed class RevisionIAFormularioContext :
        INotifyPropertyChanged
    {
        private string motivo = string.Empty;
        private string diagnosticoPropuesto = string.Empty;
        private bool tieneDiagnosticoPropuesto;
        private bool imagenAmpliadaVisible;
        private string mensajeError = string.Empty;

        public RevisionIAFormularioContext(
            InspeccionFotoV2 fotografia,
            int posicion,
            int total)
        {
            TituloFormulario =
                $"Nueva evaluación IA · Fotografía {posicion} de {total}";
            SubtituloFotografia = fotografia.Titulo;
            UrlImagen = fotografia.UrlImagen;

            InspeccionFotoResultadoIAV2? anterior =
                fotografia.ResultadoIA;

            TieneResultadoAnterior = anterior != null;
            SinResultadoAnterior = !TieneResultadoAnterior;
            DiagnosticoAnterior = anterior?.DiagnosticoVisible ??
                "Sin diagnóstico anterior";
            ResumenAnterior = string.IsNullOrWhiteSpace(
                anterior?.ResumenImagen)
                    ? "La IA no proporcionó un resumen visible para esta fotografía."
                    : anterior!.ResumenImagen;

            var detalles = new List<string>();

            if (!string.IsNullOrWhiteSpace(anterior?.NivelCerteza))
                detalles.Add($"Certeza: {anterior!.NivelCerteza}");

            if (!string.IsNullOrWhiteSpace(anterior?.SeveridadVisual))
                detalles.Add($"Severidad: {anterior!.SeveridadVisual}");

            if (anterior?.SintomasVisibles?.Count > 0)
            {
                detalles.Add(
                    "Síntomas: " +
                    string.Join(", ", anterior.SintomasVisibles.Take(5)));
            }

            DetalleAnterior = detalles.Count == 0
                ? "Revise la imagen y describa el detalle que debe reconsiderarse."
                : string.Join(" · ", detalles);
        }

        public string TituloFormulario { get; }
        public string SubtituloFotografia { get; }
        public string UrlImagen { get; }
        public bool TieneResultadoAnterior { get; }
        public bool SinResultadoAnterior { get; }
        public string DiagnosticoAnterior { get; }
        public string ResumenAnterior { get; }
        public string DetalleAnterior { get; }

        public string Motivo
        {
            get => motivo;
            set
            {
                string nuevo = value ?? string.Empty;
                if (motivo == nuevo)
                    return;

                motivo = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoLongitudMotivo));
                LimpiarError();
            }
        }

        public string TextoLongitudMotivo =>
            $"{Motivo.Length}/2000";

        public bool TieneDiagnosticoPropuesto
        {
            get => tieneDiagnosticoPropuesto;
            set
            {
                if (tieneDiagnosticoPropuesto == value)
                    return;

                tieneDiagnosticoPropuesto = value;

                if (!value)
                    DiagnosticoPropuesto = string.Empty;

                OnPropertyChanged();
                LimpiarError();
            }
        }

        public string DiagnosticoPropuesto
        {
            get => diagnosticoPropuesto;
            set
            {
                string nuevo = value ?? string.Empty;
                if (diagnosticoPropuesto == nuevo)
                    return;

                diagnosticoPropuesto = nuevo;
                OnPropertyChanged();
                LimpiarError();
            }
        }

        public bool ImagenAmpliadaVisible
        {
            get => imagenAmpliadaVisible;
            set
            {
                if (imagenAmpliadaVisible == value)
                    return;

                imagenAmpliadaVisible = value;
                OnPropertyChanged();
            }
        }

        public string MensajeError
        {
            get => mensajeError;
            private set
            {
                if (mensajeError == value)
                    return;

                mensajeError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarError));
            }
        }

        public bool MostrarError =>
            !string.IsNullOrWhiteSpace(MensajeError);

        public void MostrarMensajeError(string mensaje) =>
            MensajeError = mensaje?.Trim() ?? string.Empty;

        public void LimpiarError()
        {
            if (MostrarError)
                MensajeError = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }

    public sealed record RevisionIAFormularioResultado(
        string Motivo,
        string? DiagnosticoPropuesto);
}
