using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Fotografía local pendiente de incorporarse a una inspección.
    /// Cada elemento conserva su propia fecha y tipo de evidencia para evitar
    /// aplicar por accidente los mismos metadatos a un lote completo.
    /// </summary>
    public sealed class InspeccionFotoPreparacionLocal : INotifyPropertyChanged
    {
        private DateTime fechaIdentificacionCampo = DateTime.Today;
        private TipoFotografiaIAItem? tipoFotografiaSeleccionada;

        public int OrdenTemporal { get; init; }
        public string RutaLocal { get; init; } = string.Empty;
        public string NombreArchivo { get; init; } = string.Empty;
        public string TipoContenido { get; init; } = "image/jpeg";

        public DateTime FechaIdentificacionCampo
        {
            get => fechaIdentificacionCampo;
            set
            {
                DateTime nuevaFecha = value.Date;
                if (fechaIdentificacionCampo == nuevaFecha)
                    return;

                fechaIdentificacionCampo = nuevaFecha;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsValida));
            }
        }

        public TipoFotografiaIAItem? TipoFotografiaSeleccionada
        {
            get => tipoFotografiaSeleccionada;
            set
            {
                if (ReferenceEquals(tipoFotografiaSeleccionada, value))
                    return;

                tipoFotografiaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TipoFotografiaTexto));
                OnPropertyChanged(nameof(InstruccionTipoFotografia));
                OnPropertyChanged(nameof(EsValida));
            }
        }

        public ImageSource? Miniatura => string.IsNullOrWhiteSpace(RutaLocal)
            ? null
            : ImageSource.FromFile(RutaLocal);

        public string Titulo => OrdenTemporal > 0
            ? $"Fotografía {OrdenTemporal}"
            : "Fotografía";

        public string ArchivoTexto => string.IsNullOrWhiteSpace(NombreArchivo)
            ? "Archivo sin nombre"
            : NombreArchivo;

        public string TipoFotografiaTexto =>
            TipoFotografiaSeleccionada?.NombreMostrar ??
            "Seleccione el tipo de fotografía";

        public string InstruccionTipoFotografia =>
            string.IsNullOrWhiteSpace(
                TipoFotografiaSeleccionada?.InstruccionIA)
                ? "Seleccione el tipo para conocer qué detalles priorizará la IA."
                : TipoFotografiaSeleccionada!.InstruccionIA;

        public bool EsValida =>
            !string.IsNullOrWhiteSpace(RutaLocal) &&
            File.Exists(RutaLocal) &&
            FechaIdentificacionCampo.Date <= DateTime.Today &&
            TipoFotografiaSeleccionada?.Activo == true &&
            !string.IsNullOrWhiteSpace(TipoFotografiaSeleccionada.Codigo);

        public InspeccionFotoLocal CrearFotoLocal() => new()
        {
            RutaLocal = RutaLocal,
            NombreArchivo = NombreArchivo,
            TipoContenido = string.IsNullOrWhiteSpace(TipoContenido)
                ? "image/jpeg"
                : TipoContenido,
            FechaIdentificacionCampo = FechaIdentificacionCampo.Date,
            TipoFotografiaSeleccionada = TipoFotografiaSeleccionada
        };

        public void EliminarArchivoTemporal()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(RutaLocal) &&
                    File.Exists(RutaLocal))
                {
                    File.Delete(RutaLocal);
                }
            }
            catch
            {
                // La limpieza del temporal no debe interrumpir la navegación.
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Elemento de trabajo para preparar un análisis IA inicial. El contexto
    /// pertenece exclusivamente a la fotografía mostrada en la tarjeta.
    /// </summary>
    public sealed class InspeccionFotoContextoIAItem : INotifyPropertyChanged
    {
        private string contexto = string.Empty;
        private string estadoOperacion = "Pendiente de procesar";
        private bool procesando;
        private bool completada;
        private bool conError;

        public required InspeccionFotoV2 Fotografia { get; init; }

        public int FotografiaId => Fotografia.FotografiaId;
        public int Orden => Fotografia.Orden;
        public string TipoFotografia => Fotografia.TipoFotografia;
        public string TipoFotografiaTexto => string.IsNullOrWhiteSpace(TipoFotografia)
            ? "EVIDENCIA"
            : TipoFotografia.Replace('_', ' ');
        public string UrlImagen => Fotografia.UrlImagen;
        public string FechaCampoTexto => Fotografia.FechaCampoTexto;
        public bool RecuperaResultadoExistente =>
            Fotografia.Estado == InspeccionFotoEstados.ErrorIA &&
            Fotografia.ResultadoIA != null;

        public string Contexto
        {
            get => contexto;
            set
            {
                string nuevo = value ?? string.Empty;
                if (contexto == nuevo)
                    return;

                contexto = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContextoCaracteresTexto));
                OnPropertyChanged(nameof(ContextoValido));
            }
        }

        public string ContextoCaracteresTexto =>
            $"{Contexto.Length} / 500 caracteres";

        public bool ContextoValido => RecuperaResultadoExistente ||
                                      (Contexto.Trim().Length >= 8 &&
                                       Contexto.Trim().Length <= 500);

        public string EstadoOperacion
        {
            get => estadoOperacion;
            set
            {
                string nuevo = value ?? string.Empty;
                if (estadoOperacion == nuevo)
                    return;

                estadoOperacion = nuevo;
                OnPropertyChanged();
            }
        }

        public bool Procesando
        {
            get => procesando;
            set
            {
                if (procesando == value)
                    return;

                procesando = value;
                OnPropertyChanged();
            }
        }

        public bool Completada
        {
            get => completada;
            set
            {
                if (completada == value)
                    return;

                completada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeProcesarse));
            }
        }

        public bool ConError
        {
            get => conError;
            set
            {
                if (conError == value)
                    return;

                conError = value;
                OnPropertyChanged();
            }
        }

        public bool PuedeProcesarse => !Completada;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
    }
}
