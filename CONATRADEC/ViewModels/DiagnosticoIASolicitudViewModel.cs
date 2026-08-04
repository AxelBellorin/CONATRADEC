using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Registra inspecciones y muestra las bandejas. La IA se ejecuta después
    /// desde el detalle, permitiendo seleccionar una, varias o todas las fotos.
    /// </summary>
    public sealed class DiagnosticoIASolicitudViewModel :
        DiagnosticoIAViewModelBase
    {
        private bool inicializado;
        private string modoVista = DiagnosticoIARoutes.ModoMisInspecciones;
        private string codigoTerreno = string.Empty;
        private string observacion = string.Empty;
        private TerrenoBusquedaIAItem? terrenoSeleccionado;

        public DiagnosticoIASolicitudViewModel()
        {
            Fotos.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(TieneFotos));
                OnPropertyChanged(nameof(ResumenFotos));
                ActualizarComandos();
            };

            AgregarFotoCommand = new Command(
                async () => await AgregarFotosAsync(),
                () => !IsBusy && EsModoNueva);

            TomarFotoCommand = new Command(
                async () => await TomarFotoAsync(),
                () => !IsBusy && EsModoNueva && MediaPicker.Default.IsCaptureSupported);

            QuitarFotoCommand = new Command<InspeccionFotoLocal>(
                QuitarFoto,
                item => item != null && !IsBusy);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && EsModoNueva && TieneFotos);

            ActualizarCommand = new Command(
                async () => await CargarBandejaAsync(),
                () => !IsBusy && !EsModoNueva);

            AbrirResultadoCommand = new Command<InspeccionFitosanitariaListaItemV2>(
                async item => await AbrirResultadoAsync(item),
                item => item != null && !IsBusy);

            BuscarTerrenoCommand = new Command(
                async () => await GoToAsyncParameters(
                    DiagnosticoIARoutes.PaginaBusquedaTerreno),
                () => !IsBusy && EsModoNueva);

            QuitarTerrenoCommand = new Command(
                QuitarTerreno,
                () => !IsBusy && TerrenoSeleccionado != null);
        }

        public ObservableCollection<InspeccionFotoLocal> Fotos { get; } = [];
        public ObservableCollection<InspeccionFitosanitariaListaItemV2>
            Solicitudes { get; } = [];

        public IReadOnlyList<string> TiposFotografia { get; } =
        [
            "EVIDENCIA",
            "HOJA",
            "FRUTO",
            "TALLO",
            "RAMA",
            "PLANTA_COMPLETA",
            "RAIZ",
            "OTRA"
        ];

        public Command AgregarFotoCommand { get; }
        public Command TomarFotoCommand { get; }
        public Command<InspeccionFotoLocal> QuitarFotoCommand { get; }
        public Command GuardarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<InspeccionFitosanitariaListaItemV2> AbrirResultadoCommand { get; }
        public Command BuscarTerrenoCommand { get; }
        public Command QuitarTerrenoCommand { get; }

        public string CodigoTerreno
        {
            get => codigoTerreno;
            set
            {
                string nuevo = value ?? string.Empty;
                if (codigoTerreno == nuevo)
                    return;

                codigoTerreno = nuevo;
                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => observacion;
            set
            {
                string nuevo = value ?? string.Empty;
                if (observacion == nuevo)
                    return;

                observacion = nuevo;
                OnPropertyChanged();
            }
        }

        public TerrenoBusquedaIAItem? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            private set
            {
                if (ReferenceEquals(terrenoSeleccionado, value))
                    return;

                terrenoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));
                OnPropertyChanged(nameof(TerrenoSeleccionadoTexto));
                QuitarTerrenoCommand.ChangeCanExecute();
            }
        }

        public bool TieneTerrenoSeleccionado => TerrenoSeleccionado != null;

        public string TerrenoSeleccionadoTexto => TerrenoSeleccionado == null
            ? "La inspección puede guardarse sin terreno vinculado."
            : TerrenoSeleccionado.ResumenSeleccion;

        public bool EsModoNueva =>
            modoVista == DiagnosticoIARoutes.ModoNuevaInspeccion;

        public bool EsModoListado => !EsModoNueva;

        public string TituloPagina => modoVista switch
        {
            DiagnosticoIARoutes.ModoNuevaInspeccion => "Nueva inspección fitosanitaria",
            DiagnosticoIARoutes.ModoDecisionesPendientes => "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial => "Historial de inspecciones",
            _ => "Mis inspecciones"
        };

        public string SubtituloPagina => EsModoNueva
            ? "Registre la evidencia y la fecha real de identificación en campo. El análisis se ejecutará por fotografía."
            : "Cada tarjeta resume el avance individual de las fotografías.";

        public bool TieneFotos => Fotos.Count > 0;
        public string ResumenFotos => Fotos.Count == 1
            ? "1 fotografía preparada"
            : $"{Fotos.Count} fotografías preparadas";

        public bool TieneSolicitudes => Solicitudes.Count > 0;
        public bool SinSolicitudes => EsModoListado && !IsBusy && Solicitudes.Count == 0;

        public void AplicarModo(string? modo)
        {
            modoVista = DiagnosticoIARoutes.NormalizarModo(modo);
            OnPropertyChanged(nameof(EsModoNueva));
            OnPropertyChanged(nameof(EsModoListado));
            OnPropertyChanged(nameof(TituloPagina));
            OnPropertyChanged(nameof(SubtituloPagina));
            OnPropertyChanged(nameof(SinSolicitudes));
            ActualizarComandos();
        }

        public void AplicarTerrenoSeleccionado(TerrenoBusquedaIAItem terreno)
        {
            TerrenoSeleccionado = terreno;
            CodigoTerreno = terreno.CodigoTerreno;
        }

        public async Task InicializarAsync()
        {
            if (!ValidarEnLinea())
                return;

            if (inicializado && EsModoNueva)
                return;

            inicializado = true;

            if (EsModoListado)
                await CargarBandejaAsync();
        }

        private async Task AgregarFotosAsync()
        {
            if (IsBusy || !ValidarEnLinea())
                return;

            try
            {
                IEnumerable<FileResult> seleccion =
                    await FilePicker.Default.PickMultipleAsync(
                        new PickOptions
                        {
                            PickerTitle = "Seleccione fotografías de la inspección",
                            FileTypes = FilePickerFileType.Images
                        }) ?? [];

                foreach (FileResult archivo in seleccion)
                {
                    if (Fotos.Count >= 40)
                    {
                        await MostrarAlertaAsync(
                            "Límite alcanzado",
                            "Puede registrar hasta 40 fotografías por inspección.");
                        break;
                    }

                    string ruta = await CopiarTemporalAsync(archivo);

                    if (Fotos.Any(item => item.RutaLocal == ruta))
                        continue;

                    Fotos.Add(new InspeccionFotoLocal
                    {
                        RutaLocal = ruta,
                        NombreArchivo = archivo.FileName,
                        TipoContenido = archivo.ContentType ?? "image/jpeg",
                        FechaIdentificacionCampo = DateTime.Today,
                        TipoFotografia = "EVIDENCIA"
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task TomarFotoAsync()
        {
            if (IsBusy || !ValidarEnLinea() ||
                !MediaPicker.Default.IsCaptureSupported)
            {
                return;
            }

            if (Fotos.Count >= 40)
            {
                await MostrarAlertaAsync(
                    "Límite alcanzado",
                    "Puede registrar hasta 40 fotografías por inspección.");
                return;
            }

            try
            {
                FileResult? archivo = await MediaPicker.Default.CapturePhotoAsync();
                if (archivo == null)
                    return;

                string ruta = await CopiarTemporalAsync(archivo);
                Fotos.Add(new InspeccionFotoLocal
                {
                    RutaLocal = ruta,
                    NombreArchivo = archivo.FileName,
                    TipoContenido = archivo.ContentType ?? "image/jpeg",
                    FechaIdentificacionCampo = DateTime.Today,
                    TipoFotografia = "EVIDENCIA"
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private void QuitarFoto(InspeccionFotoLocal? foto)
        {
            if (foto == null || IsBusy)
                return;

            Fotos.Remove(foto);
        }

        private async Task GuardarAsync()
        {
            if (IsBusy || !TieneFotos || !ValidarEnLinea())
                return;

            bool confirmar = await ConfirmarAsync(
                "Guardar inspección",
                "Las fotografías se conservarán como evidencia. Después podrá seleccionar cuáles analizar, enviar o descartar lógicamente.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Guardando fotografías y fechas de campo...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 detalle =
                    await InspeccionApi.CrearAsync(
                        Fotos.ToList(),
                        CodigoTerreno,
                        Observacion);

                foreach (InspeccionFotoLocal foto in Fotos)
                    EliminarTemporalSeguro(foto.RutaLocal);

                Fotos.Clear();
                Observacion = string.Empty;

                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaResultado(
                        detalle.InspeccionId,
                        DiagnosticoIARoutes.ModoMisInspecciones));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task CargarBandejaAsync()
        {
            if (IsBusy || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado = "Cargando inspecciones...";
            ActualizarComandos();

            try
            {
                string modoApi = modoVista switch
                {
                    DiagnosticoIARoutes.ModoHistorial => "historial",
                    _ => "mis"
                };

                List<InspeccionFitosanitariaListaItemV2> items =
                    await InspeccionApi.ObtenerBandejaAsync(modoApi);

                if (modoVista == DiagnosticoIARoutes.ModoDecisionesPendientes)
                {
                    items = items
                        .Where(item => item.Estado is
                            "EN_PROCESO" or
                            "EN_PROCESO_CON_ERRORES")
                        .ToList();
                }

                Solicitudes.Clear();
                foreach (InspeccionFitosanitariaListaItemV2 item in items)
                    Solicitudes.Add(item);

                OnPropertyChanged(nameof(TieneSolicitudes));
                OnPropertyChanged(nameof(SinSolicitudes));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinSolicitudes));
                ActualizarComandos();
            }
        }

        private async Task AbrirResultadoAsync(
            InspeccionFitosanitariaListaItemV2? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    modoVista));
        }

        private void QuitarTerreno()
        {
            TerrenoSeleccionado = null;
            CodigoTerreno = string.Empty;
        }

        private static async Task<string> CopiarTemporalAsync(FileResult archivo)
        {
            string extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string carpeta = Path.Combine(
                FileSystem.CacheDirectory,
                "inspecciones-fitosanitarias");
            Directory.CreateDirectory(carpeta);

            string destino = Path.Combine(
                carpeta,
                $"{Guid.NewGuid():N}{extension}");

            await using Stream origen = await archivo.OpenReadAsync();
            await using FileStream salida = File.Create(destino);
            await origen.CopyToAsync(salida);
            return destino;
        }

        private static void EliminarTemporalSeguro(string? ruta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch
            {
            }
        }

        private void ActualizarComandos()
        {
            AgregarFotoCommand.ChangeCanExecute();
            TomarFotoCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            BuscarTerrenoCommand.ChangeCanExecute();
            QuitarTerrenoCommand.ChangeCanExecute();
        }
    }
}
