using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Registra nuevas inspecciones y presenta listados resumidos. El detalle
    /// completo se abre en DiagnosticoIAResultadoPage para evitar una pantalla
    /// excesivamente extensa.
    /// </summary>
    public sealed class DiagnosticoIASolicitudViewModel :
        DiagnosticoIAViewModelBase
    {
        private bool inicializado;
        private string codigoTerreno = string.Empty;
        private string observacion = string.Empty;
        private TerrenoBusquedaIAItem? terrenoSeleccionado;
        private readonly List<DiagnosticoIAListaItem> todasSolicitudes = [];
        private int maximoFotos = 40;
        private string modoVista = DiagnosticoIARoutes.ModoMisInspecciones;

        public DiagnosticoIASolicitudViewModel()
        {
            Fotos.CollectionChanged += (_, _) =>
            {
                NotificarFotos();
                ActualizarComandos();
            };

            AgregarFotoCommand = new Command(
                async () => await AgregarFotosGaleriaAsync(),
                () => !IsBusy && CanAdd && PuedeAgregarFotos);

            TomarFotoCommand = new Command(
                async () => await TomarFotoAsync(),
                () => !IsBusy && CanAdd && PuedeAgregarFotos);

            QuitarFotoCommand = new Command<FotoDiagnosticoSeleccionada>(
                QuitarFoto,
                item => item != null && !IsBusy);

            AnalizarCommand = new Command(
                async () => await AnalizarAsync(),
                () => !IsBusy && CanAdd && TieneFotos);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && CanView);

            AbrirResultadoCommand = new Command<DiagnosticoIAListaItem>(
                async item => await AbrirResultadoAsync(item),
                item => item != null && !IsBusy);

            VerResultadosCommand = new Command(
                async () => await AbrirMisResultadosAsync(),
                () => !IsBusy && CanView);

            BuscarTerrenoCommand = new Command(
                async () => await GoToAsyncParameters(
                    DiagnosticoIARoutes.PaginaBusquedaTerreno),
                () => !IsBusy && CanView);

            QuitarTerrenoCommand = new Command(
                QuitarTerreno,
                () => !IsBusy && TieneTerrenoSeleccionado);
        }

        public ObservableCollection<FotoDiagnosticoSeleccionada> Fotos { get; } = [];
        public ObservableCollection<DiagnosticoIAListaItem> MisSolicitudes { get; } = [];
        public ObservableCollection<string> TiposFotografia { get; } = [];

        public Command AgregarFotoCommand { get; }
        public Command TomarFotoCommand { get; }
        public Command<FotoDiagnosticoSeleccionada> QuitarFotoCommand { get; }
        public Command AnalizarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<DiagnosticoIAListaItem> AbrirResultadoCommand { get; }
        public Command VerResultadosCommand { get; }
        public Command BuscarTerrenoCommand { get; }
        public Command QuitarTerrenoCommand { get; }

        public string CodigoTerreno
        {
            get => codigoTerreno;
            private set
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
                CodigoTerreno = value?.CodigoTerreno ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));
                OnPropertyChanged(nameof(SinTerrenoSeleccionado));
                QuitarTerrenoCommand.ChangeCanExecute();
            }
        }

        public string ModoVista
        {
            get => modoVista;
            private set
            {
                if (modoVista == value)
                    return;

                modoVista = value;
                OnPropertyChanged();
                NotificarModo();
            }
        }

        public bool EsModoNueva =>
            ModoVista == DiagnosticoIARoutes.ModoNuevaInspeccion;

        public bool EsModoDecisiones =>
            ModoVista == DiagnosticoIARoutes.ModoDecisionesPendientes;

        public bool EsModoHistorial =>
            ModoVista == DiagnosticoIARoutes.ModoHistorial;

        public bool MostrarFormularioNueva => EsModoNueva;
        public bool MostrarListado => !EsModoNueva;

        public string TituloPantalla => ModoVista switch
        {
            DiagnosticoIARoutes.ModoNuevaInspeccion =>
                "Nueva inspección fitosanitaria",
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial =>
                "Historial de inspecciones",
            _ => "Mis inspecciones fitosanitarias"
        };

        public string SubtituloPantalla => ModoVista switch
        {
            DiagnosticoIARoutes.ModoNuevaInspeccion =>
                "Cada fotografía se procesa como una evidencia independiente.",
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Revise el resultado preliminar antes de continuar.",
            DiagnosticoIARoutes.ModoHistorial =>
                "Inspecciones finalizadas, canceladas o rechazadas.",
            _ => "Seguimiento resumido de solicitudes y resultados."
        };

        public string TextoRegresar => "Inspección fitosanitaria";

        public string TituloListado => ModoVista switch
        {
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Pendientes de mi decisión",
            DiagnosticoIARoutes.ModoHistorial => "Historial",
            _ => "Mis inspecciones"
        };

        public string MensajeListaVacia => ModoVista switch
        {
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "No tiene decisiones pendientes.",
            DiagnosticoIARoutes.ModoHistorial =>
                "Todavía no existen inspecciones en el historial.",
            _ => "Todavía no hay inspecciones registradas."
        };

        public bool TieneFotos => Fotos.Count > 0;
        public bool PuedeAgregarFotos => Fotos.Count < maximoFotos;
        public bool TieneSolicitudes => MisSolicitudes.Count > 0;
        public bool SinSolicitudes => !TieneSolicitudes;
        public bool TieneTerrenoSeleccionado => TerrenoSeleccionado != null;
        public bool SinTerrenoSeleccionado => !TieneTerrenoSeleccionado;

        public string ResumenFotos =>
            $"{Fotos.Count} de {maximoFotos} fotografías seleccionadas";

        public string ResumenProcesamiento => Fotos.Count == 0
            ? "Cada fotografía tendrá un resultado independiente y puede pertenecer a una planta diferente."
            : $"Se procesarán {Fotos.Count} fotografía(s) de forma independiente. Puede cerrar la aplicación después de registrar la solicitud; el servidor conservará el trabajo.";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (!inicializado)
            {
                inicializado = true;
                DiagnosticoIARoutes.AsegurarRegistro();

                if (CanView && ValidarEnLinea(false))
                {
                    try
                    {
                        DiagnosticoIACatalogos catalogos =
                            await Api.ObtenerCatalogosAsync();

                        maximoFotos = Math.Clamp(
                            catalogos.MaximoFotografiasPorInspeccion,
                            1,
                            100);

                        TiposFotografia.Clear();
                        foreach (string tipo in catalogos.PartesPlantaSugeridas)
                            TiposFotografia.Add(tipo);

                        if (!TiposFotografia.Contains("EVIDENCIA"))
                            TiposFotografia.Insert(0, "EVIDENCIA");

                        NotificarFotos();
                    }
                    catch (Exception ex)
                    {
                        await MostrarErrorAsync(ex);
                    }
                }
            }

            if (MostrarListado)
                await ActualizarAsync();
        }

        public void AplicarModo(string? modo)
        {
            ModoVista = DiagnosticoIARoutes.NormalizarModo(modo);
            AplicarFiltroModo();
        }

        public void AplicarTerrenoSeleccionado(TerrenoBusquedaIAItem? terreno)
        {
            if (terreno != null)
                TerrenoSeleccionado = terreno;
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazSolicitud);

            CanView = permiso.leer;
            CanAdd = permiso.agregar;
            CanEdit = permiso.actualizar;
            CanDelete = permiso.eliminar;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            ActualizarComandos();
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || !CanView || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado = "Cargando inspecciones...";
            ActualizarComandos();

            try
            {
                List<DiagnosticoIAListaItem> items =
                    await Api.ObtenerMisSolicitudesAsync();

                todasSolicitudes.Clear();
                todasSolicitudes.AddRange(items);
                AplicarFiltroModo();
                MensajeEstado = string.Empty;
            }
            catch (Exception ex)
            {
                MensajeEstado = string.Empty;
                await MostrarErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task AgregarFotosGaleriaAsync()
        {
            if (IsBusy || !PuedeAgregarFotos)
                return;

            try
            {
                IEnumerable<FileResult>? resultados =
                    await FilePicker.Default.PickMultipleAsync(
                        new PickOptions
                        {
                            PickerTitle = "Seleccionar fotografías",
                            FileTypes = FilePickerFileType.Images
                        });

                if (resultados == null)
                    return;

                foreach (FileResult resultado in resultados)
                {
                    if (!PuedeAgregarFotos)
                        break;

                    Fotos.Add(await CrearFotoTemporalAsync(resultado));
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task TomarFotoAsync()
        {
            if (IsBusy || !PuedeAgregarFotos)
                return;

            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await MostrarAlertaAsync(
                        "Cámara",
                        "La captura de fotografías no está disponible en este dispositivo.");
                    return;
                }

                FileResult? resultado =
                    await MediaPicker.Default.CapturePhotoAsync();

                if (resultado != null)
                    Fotos.Add(await CrearFotoTemporalAsync(resultado));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task AnalizarAsync()
        {
            if (IsBusy || !CanAdd || !TieneFotos || !ValidarEnLinea())
                return;

            bool confirmar = await ConfirmarAsync(
                "Registrar inspección",
                $"Se guardarán {Fotos.Count} fotografías. Cada una se analizará por separado y el técnico decidirá después si el caso pasa al analizador humano.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Guardando fotografías e iniciando el procesamiento...";
            ActualizarComandos();

            try
            {
                var progreso = new Progress<DiagnosticoIAProcesamientoEstado>(
                    ActualizarProgreso);

                DiagnosticoIADetalle detalle = await Api.AnalizarAsync(
                    Fotos.ToList(),
                    CodigoTerreno,
                    Observacion,
                    progreso);

                int diagnosticoId = detalle.DiagnosticoIAId;
                LimpiarFormulario();

                await MostrarAlertaAsync(
                    "Inspección registrada",
                    "La evidencia quedó guardada. Se abrirá la pantalla de resultado de esta inspección.");

                await AbrirResultadoAsync(
                    diagnosticoId,
                    DiagnosticoIARoutes.ModoMisInspecciones);
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

        private async Task AbrirResultadoAsync(
            DiagnosticoIAListaItem? item)
        {
            if (item == null)
                return;

            await AbrirResultadoAsync(item.DiagnosticoIAId, ModoVista);
        }

        private async Task AbrirResultadoAsync(
            int diagnosticoId,
            string origen)
        {
            if (Shell.Current == null || diagnosticoId <= 0)
                return;

            DiagnosticoIARoutes.AsegurarRegistro();
            await Shell.Current.GoToAsync(
                DiagnosticoIARoutes.CrearRutaResultado(
                    diagnosticoId,
                    origen),
                false);
        }

        private async Task AbrirMisResultadosAsync()
        {
            if (Shell.Current == null)
                return;

            await Shell.Current.GoToAsync(
                DiagnosticoIARoutes.CrearRutaSolicitud(
                    DiagnosticoIARoutes.ModoMisInspecciones),
                false);
        }

        private void ActualizarProgreso(
            DiagnosticoIAProcesamientoEstado estado)
        {
            if (estado == null)
                return;

            string mensaje = string.IsNullOrWhiteSpace(estado.Mensaje)
                ? "Procesando fotografías..."
                : estado.Mensaje.Trim();

            MensajeEstado = estado.TotalFotografias > 0
                ? $"{mensaje} {estado.FotografiasProcesadas} de {estado.TotalFotografias} ({estado.Porcentaje}%)."
                : mensaje;
        }

        private void AplicarFiltroModo()
        {
            IEnumerable<DiagnosticoIAListaItem> filtrados = todasSolicitudes;

            if (EsModoDecisiones)
            {
                filtrados = filtrados.Where(item =>
                    item.Estado ==
                        DiagnosticoIAEstados.PendienteDecisionTecnico);
            }
            else if (EsModoHistorial)
            {
                filtrados = filtrados.Where(item =>
                    EsEstadoHistorial(item.Estado));
            }

            MisSolicitudes.Clear();
            foreach (DiagnosticoIAListaItem item in filtrados)
                MisSolicitudes.Add(item);

            OnPropertyChanged(nameof(TieneSolicitudes));
            OnPropertyChanged(nameof(SinSolicitudes));
        }

        private static bool EsEstadoHistorial(string? estado) =>
            estado is
                DiagnosticoIAEstados.CanceladoPorTecnico or
                DiagnosticoIAEstados.Aprobado or
                DiagnosticoIAEstados.AprobadoConCorreccion or
                DiagnosticoIAEstados.Rechazado or
                DiagnosticoIAEstados.NoConcluyente or
                DiagnosticoIAEstados.PublicadoAlbum or
                DiagnosticoIAEstados.Anulado;

        private void NotificarModo()
        {
            OnPropertyChanged(nameof(EsModoNueva));
            OnPropertyChanged(nameof(EsModoDecisiones));
            OnPropertyChanged(nameof(EsModoHistorial));
            OnPropertyChanged(nameof(MostrarFormularioNueva));
            OnPropertyChanged(nameof(MostrarListado));
            OnPropertyChanged(nameof(TituloPantalla));
            OnPropertyChanged(nameof(SubtituloPantalla));
            OnPropertyChanged(nameof(TituloListado));
            OnPropertyChanged(nameof(MensajeListaVacia));
        }

        private void NotificarFotos()
        {
            OnPropertyChanged(nameof(TieneFotos));
            OnPropertyChanged(nameof(PuedeAgregarFotos));
            OnPropertyChanged(nameof(ResumenFotos));
            OnPropertyChanged(nameof(ResumenProcesamiento));
        }

        private void QuitarFoto(FotoDiagnosticoSeleccionada? foto)
        {
            if (foto == null)
                return;

            Fotos.Remove(foto);

            try
            {
                if (File.Exists(foto.RutaLocal))
                    File.Delete(foto.RutaLocal);
            }
            catch
            {
            }
        }

        private void QuitarTerreno()
        {
            TerrenoSeleccionado = null;
        }

        private void LimpiarFormulario()
        {
            foreach (FotoDiagnosticoSeleccionada foto in Fotos.ToList())
                QuitarFoto(foto);

            TerrenoSeleccionado = null;
            Observacion = string.Empty;
        }

        private static async Task<FotoDiagnosticoSeleccionada>
            CrearFotoTemporalAsync(FileResult resultado)
        {
            string extension = Path.GetExtension(resultado.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string ruta = Path.Combine(
                FileSystem.CacheDirectory,
                $"diagnostico-{Guid.NewGuid():N}{extension}");

            await using Stream origen = await resultado.OpenReadAsync();
            await using FileStream destino = new(
                ruta,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

            await origen.CopyToAsync(destino);

            return new FotoDiagnosticoSeleccionada
            {
                RutaLocal = ruta,
                NombreArchivo = resultado.FileName,
                TipoContenido = string.IsNullOrWhiteSpace(resultado.ContentType)
                    ? "image/jpeg"
                    : resultado.ContentType,
                TipoFotografia = "EVIDENCIA"
            };
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            AgregarFotoCommand.ChangeCanExecute();
            TomarFotoCommand.ChangeCanExecute();
            QuitarFotoCommand.ChangeCanExecute();
            AnalizarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            AbrirResultadoCommand.ChangeCanExecute();
            VerResultadosCommand.ChangeCanExecute();
            BuscarTerrenoCommand.ChangeCanExecute();
            QuitarTerrenoCommand.ChangeCanExecute();
        }
    }
}
