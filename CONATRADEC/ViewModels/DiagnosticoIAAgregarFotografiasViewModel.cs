using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Prepara individualmente las fotografías antes de incorporarlas a una
    /// inspección fitosanitaria. Cada imagen conserva fecha y tipo propios.
    /// </summary>
    public sealed class DiagnosticoIAAgregarFotografiasViewModel :
        DiagnosticoIAViewModelBase
    {
        private readonly TipoFotografiaIAApiService tiposApi = new();
        private int inspeccionId;
        private bool inicializado;
        private bool guardado;
        private bool limpiezaRealizada;

        public DiagnosticoIAAgregarFotografiasViewModel()
        {
            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && PuedeGuardar);

            EliminarFotoCommand = new Command<InspeccionFotoPreparacionLocal>(
                EliminarFoto,
                _ => !IsBusy);
        }

        public ObservableCollection<InspeccionFotoPreparacionLocal>
            Fotografias { get; } = [];

        public ObservableCollection<TipoFotografiaIAItem>
            TiposFotografia { get; } = [];

        public Command GuardarCommand { get; }
        public Command<InspeccionFotoPreparacionLocal> EliminarFotoCommand { get; }

        public DateTime FechaMaxima => DateTime.Today;
        public int CantidadFotografias => Fotografias.Count;
        public string Titulo => CantidadFotografias == 1
            ? "Preparar fotografía"
            : $"Preparar {CantidadFotografias} fotografías";

        public string Subtitulo =>
            "Revise cada imagen y asigne su fecha de identificación y tipo de fotografía. " +
            "Los valores son independientes por evidencia; el contexto específico para la IA se solicitará al momento de procesar el lote.";

        public string TextoGuardar => CantidadFotografias == 1
            ? "Agregar fotografía"
            : $"Agregar {CantidadFotografias} fotografías";

        public bool PuedeGuardar =>
            inspeccionId > 0 &&
            Fotografias.Count > 0 &&
            Fotografias.All(item => item.EsValida);

        public void AplicarParametros(
            int id,
            IEnumerable<InspeccionFotoPreparacionLocal>? fotografias)
        {
            if (inicializado)
                return;

            inspeccionId = id;
            Fotografias.Clear();

            foreach (InspeccionFotoPreparacionLocal foto in
                     fotografias ?? [])
            {
                foto.PropertyChanged += OnFotoPropertyChanged;
                Fotografias.Add(foto);
            }

            inicializado = true;
            NotificarEstado();
        }

        public async Task InicializarAsync()
        {
            if (IsBusy || TiposFotografia.Count > 0)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando tipos de fotografía...";
            ActualizarComandos();

            try
            {
                ApiResult<List<TipoFotografiaIAItem>> resultado =
                    await tiposApi.ListarActivosAsync();

                List<TipoFotografiaIAItem> tipos = resultado.Data?
                    .Where(item => item.Activo)
                    .OrderBy(item => item.Orden)
                    .ThenBy(item => item.Nombre)
                    .ToList() ?? [];

                if (!resultado.Success || tipos.Count == 0)
                {
                    await MostrarAlertaAsync(
                        "Catálogo requerido",
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No hay tipos de fotografía activos."
                            : resultado.Message);
                    return;
                }

                TiposFotografia.Clear();
                foreach (TipoFotografiaIAItem tipo in tipos)
                    TiposFotografia.Add(tipo);

                /*
                 * Si solo existe un tipo activo, se selecciona automáticamente.
                 * Con varios tipos el técnico debe decidir por cada fotografía.
                 */
                if (TiposFotografia.Count == 1)
                {
                    foreach (InspeccionFotoPreparacionLocal foto in Fotografias)
                    {
                        foto.TipoFotografiaSeleccionada =
                            TiposFotografia[0];
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarEstado();
                ActualizarComandos();
            }
        }

        public void LiberarTemporalesSiCorresponde()
        {
            if (guardado || limpiezaRealizada)
                return;

            limpiezaRealizada = true;

            foreach (InspeccionFotoPreparacionLocal foto in Fotografias)
                foto.EliminarArchivoTemporal();
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar || IsBusy || !ValidarEnLinea())
                return;

            List<InspeccionFotoPreparacionLocal> invalidas = Fotografias
                .Where(item => !item.EsValida)
                .ToList();

            if (invalidas.Count > 0)
            {
                await MostrarAlertaAsync(
                    "Datos incompletos",
                    "Revise la fecha y el tipo de cada fotografía antes de continuar.");
                return;
            }

            IsBusy = true;
            MensajeEstado = "Agregando fotografías a la inspección...";
            ActualizarComandos();

            try
            {
                List<InspeccionFotoLocal> fotos = Fotografias
                    .Select(item => item.CrearFotoLocal())
                    .ToList();

                await InspeccionApi.AgregarFotosAsync(
                    inspeccionId,
                    fotos);

                guardado = true;
                limpiezaRealizada = true;

                foreach (InspeccionFotoPreparacionLocal foto in Fotografias)
                    foto.EliminarArchivoTemporal();

                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarEstado();
                ActualizarComandos();
            }
        }

        private void EliminarFoto(InspeccionFotoPreparacionLocal? foto)
        {
            if (foto == null || IsBusy)
                return;

            foto.PropertyChanged -= OnFotoPropertyChanged;
            foto.EliminarArchivoTemporal();
            Fotografias.Remove(foto);
            Reordenar();
            NotificarEstado();
            ActualizarComandos();
        }

        private void Reordenar()
        {
            /*
             * OrdenTemporal es inmutable para preservar la referencia original
             * mostrada al técnico. No es necesario renumerar al eliminar.
             */
        }

        private void OnFotoPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(InspeccionFotoPreparacionLocal.EsValida)
                or nameof(InspeccionFotoPreparacionLocal.FechaIdentificacionCampo)
                or nameof(InspeccionFotoPreparacionLocal.TipoFotografiaSeleccionada))
            {
                NotificarEstado();
                ActualizarComandos();
            }
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(CantidadFotografias));
            OnPropertyChanged(nameof(Titulo));
            OnPropertyChanged(nameof(Subtitulo));
            OnPropertyChanged(nameof(TextoGuardar));
            OnPropertyChanged(nameof(PuedeGuardar));
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            EliminarFotoCommand.ChangeCanExecute();
        }
    }
}
