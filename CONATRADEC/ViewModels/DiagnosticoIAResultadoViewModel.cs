using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Media;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Detalle operativo por fotografía. Las acciones masivas conservan un
    /// resultado individual para que un error no invalide las demás fotos.
    /// </summary>
    public sealed class DiagnosticoIAResultadoViewModel :
        DiagnosticoIAViewModelBase
    {
        private int diagnosticoId;
        private readonly TipoFotografiaIAApiService tiposFotografiaApi = new();
        private string origen = DiagnosticoIARoutes.ModoMisInspecciones;
        private InspeccionFitosanitariaDetalleV2? detalle;

        public DiagnosticoIAResultadoViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && diagnosticoId > 0);
            AgregarFotografiasCommand = new Command(
                async () => await AgregarFotografiasAsync(),
                () => !IsBusy && PuedeAgregarFotografias);
            CerrarInspeccionCommand = new Command(
                async () => await CerrarInspeccionAsync(),
                () => !IsBusy && PuedeCerrarInspeccion);
            SeleccionarTodoCommand = new Command(
                SeleccionarTodo,
                () => !IsBusy && Fotografias.Count > 0);
            QuitarSeleccionCommand = new Command(
                QuitarSeleccion,
                () => !IsBusy && TieneSeleccion);
            ProcesarSeleccionCommand = new Command(
                async () => await ProcesarSeleccionAsync(),
                () => !IsBusy && PuedeProcesarSeleccion);
            EnviarAnalizadorCommand = new Command(
                async () => await EnviarAnalizadorAsync(),
                () => !IsBusy && PuedeEnviarSeleccion);
            SolicitarRevisionCommand = new Command(
                async () => await SolicitarRevisionAsync(),
                () => !IsBusy && PuedeSolicitarRevision);
            DescartarCommand = new Command(
                async () => await DescartarAsync(),
                () => !IsBusy && PuedeDescartarSeleccion);
            AnalisisHumanoCommand = new Command(
                async () => await RegistrarAnalisisHumanoAsync(),
                () => !IsBusy && PuedeAnalizarSeleccion);
            AprobarCommand = new Command(
                async () => await RegistrarAprobacionAsync(),
                () => !IsBusy && PuedeAprobarSeleccion);
            PublicarAlbumCommand = new Command(
                async () => await PublicarAlbumAsync(),
                () => !IsBusy && PuedePublicarSeleccion);
            RegresarResultadoCommand = new Command(
                async () => await RegresarResultadoAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<InspeccionFotoV2> Fotografias { get; } = [];

        public Command ActualizarCommand { get; }
        public Command AgregarFotografiasCommand { get; }
        public Command CerrarInspeccionCommand { get; }
        public Command SeleccionarTodoCommand { get; }
        public Command QuitarSeleccionCommand { get; }
        public Command ProcesarSeleccionCommand { get; }
        public Command EnviarAnalizadorCommand { get; }
        public Command SolicitarRevisionCommand { get; }
        public Command DescartarCommand { get; }
        public Command AnalisisHumanoCommand { get; }
        public Command AprobarCommand { get; }
        public Command PublicarAlbumCommand { get; }
        public Command RegresarResultadoCommand { get; }

        public InspeccionFitosanitariaDetalleV2? Detalle
        {
            get => detalle;
            private set
            {
                if (ReferenceEquals(detalle, value))
                    return;

                detalle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneDetalle));
                OnPropertyChanged(nameof(TituloResultado));
                OnPropertyChanged(nameof(SubtituloResultado));
                OnPropertyChanged(nameof(PuedeAgregarFotografias));
                OnPropertyChanged(nameof(PuedeCerrarInspeccion));
                OnPropertyChanged(nameof(MostrarCierreTecnico));
                OnPropertyChanged(nameof(MotivoNoPuedeCerrar));
            }
        }

        public bool TieneDetalle => Detalle != null;
        public string TituloResultado => Detalle?.Titulo ?? "Inspección fitosanitaria";
        public string SubtituloResultado => Detalle == null
            ? "Cargando expediente..."
            : $"{Detalle.TerrenoTexto} · Estado: {Detalle.EstadoTexto} · {Detalle.CierreTexto}";

        public bool PuedeAgregarFotografias =>
            Detalle?.PuedeGestionarSolicitud == true &&
            Fotografias.Count < 40;


        public bool PuedeCerrarInspeccion =>
            Detalle?.PuedeCerrarInspeccion == true;

        public bool MostrarCierreTecnico =>
            Detalle is
            {
                PuedeGestionarSolicitud: true,
                CerradaTecnico: false
            };

        public string MotivoNoPuedeCerrar =>
            Detalle?.MotivoNoPuedeCerrar ?? string.Empty;

        public List<InspeccionFotoV2> FotosSeleccionadas => Fotografias
            .Where(item => item.Seleccionada)
            .ToList();

        public bool TieneSeleccion => FotosSeleccionadas.Count > 0;
        public int CantidadSeleccionada => FotosSeleccionadas.Count;
        public string TextoSeleccion => CantidadSeleccionada == 1
            ? "1 fotografía seleccionada"
            : $"{CantidadSeleccionada} fotografías seleccionadas";

        public bool PuedeProcesarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.Borrador or
                InspeccionFotoEstados.PendienteIA or
                InspeccionFotoEstados.ErrorIA or
                InspeccionFotoEstados.NoConcluyente);

        public bool PuedeEnviarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            FotosSeleccionadas.All(item => item.Estado ==
                InspeccionFotoEstados.PendienteDecisionTecnico);

        public bool PuedeSolicitarRevision =>
            TieneSeleccion &&
            (Detalle?.PuedeGestionarSolicitud == true ||
             Detalle?.PuedeAnalizar == true) &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.PendienteDecisionTecnico or
                InspeccionFotoEstados.PendienteAnalizador or
                InspeccionFotoEstados.EnAnalisisHumano or
                InspeccionFotoEstados.DevueltaAnalizador or
                InspeccionFotoEstados.ErrorIA or
                InspeccionFotoEstados.NoConcluyente);

        public bool PuedeDescartarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            FotosSeleccionadas.All(item =>
                item.Estado != InspeccionFotoEstados.PublicadaAlbum);

        public bool PuedeAnalizarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeAnalizar == true &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.PendienteAnalizador or
                InspeccionFotoEstados.EnAnalisisHumano or
                InspeccionFotoEstados.DevueltaAnalizador);

        public bool PuedeAprobarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeAprobar == true &&
            FotosSeleccionadas.All(item => item.Estado ==
                InspeccionFotoEstados.PendienteAprobacion);

        public bool PuedePublicarSeleccion =>
            CantidadSeleccionada == 1 &&
            Detalle?.PuedePublicarAlbum == true &&
            (FotosSeleccionadas[0].Estado is
                InspeccionFotoEstados.Aprobada or
                InspeccionFotoEstados.AprobadaConCorreccion) &&
            FotosSeleccionadas[0].UltimaAprobacion?.AutorizaPublicacionAlbum == true;

        public string TextoRegresar => origen switch
        {
            DiagnosticoIARoutes.ModoDecisionesPendientes => "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial => "Historial",
            _ => "Mis inspecciones"
        };

        public void AplicarParametros(int id, string? origenVista)
        {
            diagnosticoId = id;
            origen = DiagnosticoIARoutes.NormalizarModo(origenVista);
            OnPropertyChanged(nameof(TextoRegresar));
        }

        public Task InicializarAsync() => ActualizarAsync();
        public void IniciarSeguimiento() { }
        public void DetenerSeguimiento() { }

        private async Task ActualizarAsync()
        {
            if (IsBusy || diagnosticoId <= 0 || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado = "Cargando expedientes individuales...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 actualizado =
                    await InspeccionApi.ObtenerDetalleAsync(diagnosticoId);

                AplicarDetalle(actualizado);
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

        private async Task AgregarFotografiasAsync()
        {
            if (!PuedeAgregarFotografias || IsBusy ||
                !ValidarEnLinea() || Shell.Current == null)
            {
                return;
            }

            int disponibles = Math.Max(0, 40 - Fotografias.Count);
            if (disponibles == 0)
            {
                await MostrarAlertaAsync(
                    "Límite alcanzado",
                    "La inspección ya contiene 40 fotografías.");
                return;
            }

            var archivos = new List<FileResult>();
            var temporales = new List<InspeccionFotoLocal>();

            try
            {
                var opciones = new List<string>
                {
                    "Seleccionar de galería"
                };

                if (MediaPicker.Default.IsCaptureSupported)
                    opciones.Add("Tomar fotografía");

                string? origenFoto = await Shell.Current.DisplayActionSheet(
                    "Agregar fotografías",
                    "Cancelar",
                    null,
                    opciones.ToArray());

                if (string.IsNullOrWhiteSpace(origenFoto) ||
                    origenFoto == "Cancelar")
                {
                    return;
                }

                if (origenFoto == "Tomar fotografía")
                {
                    FileResult? capturada =
                        await MediaPicker.Default.CapturePhotoAsync();

                    if (capturada != null)
                        archivos.Add(capturada);
                }
                else
                {
                    IEnumerable<FileResult> seleccion =
                        await FilePicker.Default.PickMultipleAsync(
                            new PickOptions
                            {
                                PickerTitle =
                                    "Seleccione fotografías para agregar",
                                FileTypes = FilePickerFileType.Images
                            }) ?? [];

                    archivos.AddRange(seleccion);
                }

                if (archivos.Count == 0)
                    return;

                if (archivos.Count > disponibles)
                {
                    archivos = archivos.Take(disponibles).ToList();

                    await MostrarAlertaAsync(
                        "Límite de fotografías",
                        $"Solo se agregarán {disponibles} fotografía(s), porque el máximo por inspección es 40.");
                }

                string? fechaTexto = await Shell.Current.DisplayPromptAsync(
                    "Fecha de identificación en campo",
                    "Ingrese la fecha que corresponde a estas fotografías en formato yyyy-MM-dd. Déjela vacía para usar la fecha de hoy.",
                    "Continuar",
                    "Cancelar",
                    "yyyy-MM-dd",
                    10,
                    Keyboard.Text);

                if (fechaTexto == null)
                    return;

                DateTime fechaCampo = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(fechaTexto) &&
                    !DateTime.TryParseExact(
                        fechaTexto.Trim(),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out fechaCampo))
                {
                    await MostrarAlertaAsync(
                        "Fecha no válida",
                        "Use el formato yyyy-MM-dd, por ejemplo 2026-08-04.");
                    return;
                }

                if (fechaCampo.Date > DateTime.Today)
                {
                    await MostrarAlertaAsync(
                        "Fecha no válida",
                        "La fecha de identificación en campo no puede estar en el futuro.");
                    return;
                }

                ApiResult<List<TipoFotografiaIAItem>> tiposResult =
                    await tiposFotografiaApi.ListarActivosAsync();

                if (!tiposResult.Success ||
                    tiposResult.Data == null ||
                    tiposResult.Data.Count == 0)
                {
                    await MostrarAlertaAsync(
                        "Catálogo requerido",
                        string.IsNullOrWhiteSpace(tiposResult.Message)
                            ? "No hay tipos de fotografía activos. Solicite al administrador que configure el catálogo."
                            : tiposResult.Message);
                    return;
                }

                List<TipoFotografiaIAItem> tiposDisponibles =
                    tiposResult.Data
                        .Where(item => item.Activo)
                        .OrderBy(item => item.Orden)
                        .ThenBy(item => item.Nombre)
                        .ToList();

                string? nombreSeleccionado =
                    await Shell.Current.DisplayActionSheet(
                        "Tipo de fotografía",
                        "Cancelar",
                        null,
                        tiposDisponibles
                            .Select(item => item.NombreMostrar)
                            .ToArray());

                if (string.IsNullOrWhiteSpace(nombreSeleccionado) ||
                    nombreSeleccionado == "Cancelar")
                {
                    return;
                }

                TipoFotografiaIAItem? tipoSeleccionado =
                    tiposDisponibles.FirstOrDefault(item =>
                        string.Equals(
                            item.NombreMostrar,
                            nombreSeleccionado,
                            StringComparison.Ordinal));

                if (tipoSeleccionado == null)
                    return;

                foreach (FileResult archivo in archivos)
                {
                    string ruta = await CopiarTemporalAsync(archivo);
                    temporales.Add(new InspeccionFotoLocal
                    {
                        RutaLocal = ruta,
                        NombreArchivo = archivo.FileName,
                        TipoContenido = archivo.ContentType ?? "image/jpeg",
                        FechaIdentificacionCampo = fechaCampo.Date,
                        TipoFotografiaSeleccionada = tipoSeleccionado
                    });
                }

                bool confirmar = await ConfirmarAsync(
                    "Agregar fotografías",
                    $"Se incorporarán {temporales.Count} fotografía(s) a la inspección #{diagnosticoId}. Quedarán pendientes de análisis IA.");

                if (!confirmar)
                    return;

                IsBusy = true;
                MensajeEstado = "Agregando fotografías a la inspección...";
                ActualizarComandos();

                InspeccionFitosanitariaDetalleV2 actualizado =
                    await InspeccionApi.AgregarFotosAsync(
                        diagnosticoId,
                        temporales);

                AplicarDetalle(actualizado);

                await MostrarAlertaAsync(
                    "Fotografías agregadas",
                    $"Se incorporaron {temporales.Count} fotografía(s). Puede seleccionarlas y ejecutar el análisis con IA.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                foreach (InspeccionFotoLocal temporal in temporales)
                    EliminarTemporalSeguro(temporal.RutaLocal);

                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private void AplicarDetalle(
            InspeccionFitosanitariaDetalleV2 actualizado)
        {
            Detalle = actualizado;

            foreach (InspeccionFotoV2 anterior in Fotografias)
                anterior.PropertyChanged -= FotoPropertyChanged;

            Fotografias.Clear();
            foreach (InspeccionFotoV2 foto in actualizado.Fotografias)
            {
                foto.PropertyChanged += FotoPropertyChanged;
                Fotografias.Add(foto);
            }

            OnPropertyChanged(nameof(PuedeAgregarFotografias));
            OnPropertyChanged(nameof(PuedeCerrarInspeccion));
            OnPropertyChanged(nameof(MostrarCierreTecnico));
            OnPropertyChanged(nameof(MotivoNoPuedeCerrar));
            OnPropertyChanged(nameof(SubtituloResultado));
            NotificarSeleccion();
        }

        private void FotoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InspeccionFotoV2.Seleccionada))
                NotificarSeleccion();
        }

        private void SeleccionarTodo()
        {
            foreach (InspeccionFotoV2 foto in Fotografias.Where(item => item.PuedeSeleccionarse))
                foto.Seleccionada = true;

            NotificarSeleccion();
        }

        private void QuitarSeleccion()
        {
            foreach (InspeccionFotoV2 foto in Fotografias)
                foto.Seleccionada = false;

            NotificarSeleccion();
        }

        private async Task CerrarInspeccionAsync()
        {
            if (!PuedeCerrarInspeccion || Detalle == null)
                return;

            bool confirmar = await ConfirmarAsync(
                "Cerrar inspección",
                "Después del cierre no podrá agregar, descartar ni volver a analizar fotografías desde la etapa técnica. Las fotografías enviadas quedarán visibles para el analizador.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Cerrando inspección y habilitando la revisión humana...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 actualizado =
                    await InspeccionApi.CerrarInspeccionAsync(diagnosticoId);

                AplicarDetalle(actualizado);

                await MostrarAlertaAsync(
                    "Inspección cerrada",
                    "La inspección fue enviada a la bandeja del analizador.");
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

        private async Task ProcesarSeleccionAsync()
        {
            if (!PuedeProcesarSeleccion)
                return;

            bool confirmar = await ConfirmarAsync(
                "Analizar fotografías",
                "La IA procesará cada fotografía por separado. Los resultados correctos se conservarán aunque otra fotografía falle.");

            if (!confirmar)
                return;

            await EjecutarOperacionAsync(
                "Analizando fotografías seleccionadas...",
                ids => ProcesarEnLotesAsync(ids),
                "Análisis completado");
        }

        private async Task<InspeccionOperacionMasivaV2> ProcesarEnLotesAsync(
            IReadOnlyCollection<int> ids)
        {
            var acumulado = new InspeccionOperacionMasivaV2
            {
                TotalSolicitadas = ids.Count
            };

            // Se envía una fotografía por petición para respetar el procesamiento
            // individual y evitar que el tiempo de una imagen agote todo el lote.
            foreach (int[] lote in ids.Chunk(1))
            {
                InspeccionOperacionMasivaV2 parcial =
                    await InspeccionApi.ProcesarFotosAsync(
                        diagnosticoId,
                        lote);

                acumulado.TotalExitosas += parcial.TotalExitosas;
                acumulado.TotalConError += parcial.TotalConError;
                acumulado.Resultados.AddRange(parcial.Resultados);
            }

            return acumulado;
        }

        private async Task EnviarAnalizadorAsync()
        {
            if (!PuedeEnviarSeleccion)
                return;

            await EjecutarOperacionAsync(
                "Preparando fotografías para la revisión humana...",
                ids => InspeccionApi.EnviarAnalizadorAsync(
                    diagnosticoId,
                    ids),
                "Fotografías preparadas");
        }

        private async Task SolicitarRevisionAsync()
        {
            if (!PuedeSolicitarRevision || Shell.Current == null)
                return;

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Nueva evaluación IA",
                "Explique qué debe revisar la IA en las fotografías seleccionadas.",
                "Continuar",
                "Cancelar",
                "Motivo obligatorio",
                2000,
                Keyboard.Default);

            if (motivo == null)
                return;

            motivo = motivo.Trim();
            if (motivo.Length < 8)
            {
                await MostrarAlertaAsync(
                    "Motivo requerido",
                    "Escriba al menos 8 caracteres.");
                return;
            }

            string? propuesta = await Shell.Current.DisplayPromptAsync(
                "Diagnóstico considerado",
                "Diagnóstico opcional que desea contrastar.",
                "Procesar",
                "Omitir",
                "Opcional",
                300,
                Keyboard.Default);

            await EjecutarOperacionAsync(
                "Reevaluando fotografías seleccionadas...",
                ids => InspeccionApi.SolicitarRevisionIAAsync(
                    diagnosticoId,
                    ids,
                    motivo,
                    propuesta),
                "Reevaluación completada");
        }

        private async Task DescartarAsync()
        {
            if (!PuedeDescartarSeleccion || Shell.Current == null)
                return;

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Descartar fotografías",
                "Indique el motivo. Las imágenes y su historial no se eliminarán.",
                "Descartar",
                "Cancelar",
                "Motivo obligatorio",
                1000,
                Keyboard.Default);

            if (motivo == null || motivo.Trim().Length < 8)
            {
                if (motivo != null)
                {
                    await MostrarAlertaAsync(
                        "Motivo requerido",
                        "Escriba al menos 8 caracteres.");
                }
                return;
            }

            await EjecutarOperacionAsync(
                "Registrando descarte lógico...",
                ids => InspeccionApi.DescartarFotosAsync(
                    diagnosticoId,
                    ids,
                    motivo.Trim()),
                "Descarte registrado");
        }

        private async Task RegistrarAnalisisHumanoAsync()
        {
            if (!PuedeAnalizarSeleccion || Shell.Current == null)
                return;

            string? diagnostico = await Shell.Current.DisplayPromptAsync(
                "Clasificación humana",
                "Escriba el diagnóstico que aplicará a las fotografías seleccionadas.",
                "Continuar",
                "Cancelar",
                "Diagnóstico obligatorio",
                300,
                Keyboard.Default);

            if (string.IsNullOrWhiteSpace(diagnostico))
                return;

            string? categoria = await Shell.Current.DisplayActionSheet(
                "Categoría principal",
                "Cancelar",
                null,
                "ENFERMEDAD",
                "PLAGA",
                "ALTERACION_NUTRICIONAL",
                "ESTRES_ABIOTICO",
                "DANO_MECANICO",
                "AFECTACION_NO_DETERMINADA",
                "NO_APLICA");

            if (string.IsNullOrWhiteSpace(categoria) || categoria == "Cancelar")
                return;

            string? severidad = await Shell.Current.DisplayActionSheet(
                "Severidad visual",
                "Cancelar",
                null,
                "LEVE",
                "MODERADA",
                "SEVERA",
                "NO_EVALUABLE",
                "NO_APLICA");

            if (string.IsNullOrWhiteSpace(severidad) || severidad == "Cancelar")
                return;

            string? certeza = await Shell.Current.DisplayActionSheet(
                "Nivel de certeza",
                "Cancelar",
                null,
                "ALTO",
                "MEDIO",
                "BAJO",
                "NO_DETERMINADO");

            if (string.IsNullOrWhiteSpace(certeza) || certeza == "Cancelar")
                return;

            string? observaciones = await Shell.Current.DisplayPromptAsync(
                "Observaciones",
                "Observaciones técnicas opcionales.",
                "Continuar",
                "Omitir",
                "Opcional",
                3000,
                Keyboard.Default);

            bool enviar = await ConfirmarAsync(
                "Enviar al aprobador",
                "¿Desea guardar y enviar inmediatamente esta clasificación al aprobador? Si cancela, quedará como borrador.");

            List<InspeccionFotoAnalisisHumanoRequestV2> items =
                FotosSeleccionadas.Select(foto =>
                {
                    InspeccionFotoResultadoIAV2? ia = foto.ResultadoIA;
                    return new InspeccionFotoAnalisisHumanoRequestV2
                    {
                        FotografiaId = foto.FotografiaId,
                        CalidadEvaluacion = ia?.CalidadEvaluacion ?? "NO_EVALUABLE",
                        EstadoGeneral = ia?.EstadoGeneral ?? "INDETERMINADA",
                        CategoriaPrincipal = categoria,
                        CategoriasSecundarias = ia?.CategoriasSecundarias ?? [],
                        Diagnostico = diagnostico.Trim(),
                        TipoDiagnostico = ia?.TipoDiagnostico ?? string.Empty,
                        Severidad = severidad,
                        NivelCerteza = certeza,
                        Observaciones = observaciones?.Trim() ?? string.Empty
                    };
                }).ToList();

            await EjecutarOperacionAsync(
                enviar
                    ? "Guardando y enviando clasificación humana..."
                    : "Guardando clasificación humana...",
                _ => InspeccionApi.GuardarAnalisisHumanoAsync(
                    diagnosticoId,
                    items,
                    enviar),
                "Clasificación humana guardada");
        }

        private async Task RegistrarAprobacionAsync()
        {
            if (!PuedeAprobarSeleccion || Shell.Current == null)
                return;

            string? decision = await Shell.Current.DisplayActionSheet(
                "Decisión del aprobador",
                "Cancelar",
                null,
                "APROBAR",
                "APROBAR_CON_CORRECCION",
                "DEVOLVER_AL_ANALIZADOR",
                "RECHAZAR",
                "NO_CONCLUYENTE");

            if (string.IsNullOrWhiteSpace(decision) || decision == "Cancelar")
                return;

            string? diagnosticoFinal = string.Empty;
            if (decision == "APROBAR_CON_CORRECCION")
            {
                diagnosticoFinal = await Shell.Current.DisplayPromptAsync(
                    "Diagnóstico final corregido",
                    "Escriba la clasificación final que reemplazará la propuesta del analizador.",
                    "Continuar",
                    "Cancelar",
                    "Diagnóstico final",
                    300,
                    Keyboard.Default);

                if (string.IsNullOrWhiteSpace(diagnosticoFinal))
                    return;
            }

            string? observaciones = await Shell.Current.DisplayPromptAsync(
                "Observaciones de aprobación",
                "Puede documentar el motivo de la decisión.",
                "Continuar",
                "Omitir",
                "Opcional",
                3000,
                Keyboard.Default);

            bool autorizaAlbum = false;
            if (decision is "APROBAR" or "APROBAR_CON_CORRECCION")
            {
                autorizaAlbum = await ConfirmarAsync(
                    "Autorizar álbum",
                    "¿Estas fotografías pueden publicarse posteriormente en el Álbum Botánico?");
            }

            List<InspeccionFotoAprobacionRequestV2> items =
                FotosSeleccionadas.Select(foto =>
                {
                    InspeccionFotoAnalisisHumanoV2 humano =
                        foto.UltimoAnalisisHumano!;

                    return new InspeccionFotoAprobacionRequestV2
                    {
                        FotografiaId = foto.FotografiaId,
                        Decision = decision,
                        CalidadEvaluacionFinal = humano.CalidadEvaluacion,
                        EstadoGeneralFinal = humano.EstadoGeneral,
                        CategoriaPrincipalFinal = humano.CategoriaPrincipal,
                        CategoriasSecundariasFinales = humano.CategoriasSecundarias,
                        DiagnosticoFinal = string.IsNullOrWhiteSpace(diagnosticoFinal)
                            ? humano.Diagnostico
                            : diagnosticoFinal.Trim(),
                        TipoDiagnosticoFinal = humano.TipoDiagnostico,
                        SeveridadFinal = humano.Severidad,
                        NivelCertezaFinal = humano.NivelCerteza,
                        Observaciones = observaciones?.Trim() ?? string.Empty,
                        AutorizaPublicacionAlbum = autorizaAlbum
                    };
                }).ToList();

            await EjecutarOperacionAsync(
                "Registrando decisiones individuales...",
                _ => InspeccionApi.RegistrarAprobacionesAsync(
                    diagnosticoId,
                    items),
                "Decisiones registradas");
        }

        private async Task PublicarAlbumAsync()
        {
            if (!PuedePublicarSeleccion || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadas[0];

            IsBusy = true;
            MensajeEstado = "Cargando catálogo activo del álbum...";
            ActualizarComandos();

            try
            {
                List<InspeccionAlbumCategoriaV2> categorias =
                    await InspeccionApi.ObtenerCatalogoAlbumAsync();

                if (categorias.Count == 0)
                {
                    await MostrarAlertaAsync(
                        "Álbum sin fichas",
                        "No existen categorías activas con fichas disponibles para publicar.");
                    return;
                }

                int? categoriaSugerida =
                    foto.ResultadoIA?.CategoriaAlbumBotanicoIdSugerida;

                categorias = categorias
                    .OrderByDescending(item =>
                        item.CategoriaAlbumBotanicoId == categoriaSugerida)
                    .ThenBy(item => item.Nombre)
                    .ToList();

                string? categoriaTexto = await Shell.Current.DisplayActionSheet(
                    "Categoría del Álbum Botánico",
                    "Cancelar",
                    null,
                    categorias
                        .Select(item => item.TextoSeleccion)
                        .ToArray());

                if (string.IsNullOrWhiteSpace(categoriaTexto) ||
                    categoriaTexto == "Cancelar")
                {
                    return;
                }

                InspeccionAlbumCategoriaV2? categoria = categorias
                    .FirstOrDefault(item =>
                        item.TextoSeleccion == categoriaTexto);

                if (categoria == null || categoria.Fichas.Count == 0)
                    return;

                int? fichaSugerida =
                    foto.ResultadoIA?.AlbumBotanicoCafeIdSugerido;

                List<InspeccionAlbumFichaV2> fichas = categoria.Fichas
                    .OrderByDescending(item =>
                        item.AlbumBotanicoCafeId == fichaSugerida)
                    .ThenBy(item => item.Titulo)
                    .ToList();

                string? fichaTexto = await Shell.Current.DisplayActionSheet(
                    "Ficha que recibirá la fotografía",
                    "Cancelar",
                    null,
                    fichas
                        .Select(item => item.TextoSeleccion)
                        .ToArray());

                if (string.IsNullOrWhiteSpace(fichaTexto) ||
                    fichaTexto == "Cancelar")
                {
                    return;
                }

                InspeccionAlbumFichaV2? ficha = fichas.FirstOrDefault(item =>
                    item.TextoSeleccion == fichaTexto);

                if (ficha == null)
                    return;

                string? descripcion = await Shell.Current.DisplayPromptAsync(
                    "Descripción",
                    "Descripción opcional para la fotografía del álbum.",
                    "Publicar",
                    "Cancelar",
                    "Opcional",
                    500,
                    Keyboard.Default);

                if (descripcion == null)
                    return;

                MensajeEstado = "Publicando referencia aprobada en el álbum...";

                await InspeccionApi.PublicarAlbumAsync(
                    diagnosticoId,
                    foto.FotografiaId,
                    categoria.CategoriaAlbumBotanicoId,
                    ficha.AlbumBotanicoCafeId,
                    descripcion,
                    false,
                    0);

                await MostrarAlertaAsync(
                    "Publicación completada",
                    "La foto fue vinculada al álbum y la evidencia original permanece en la inspección.");
                await RecargarDespuesOperacionAsync();
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

        private async Task EjecutarOperacionAsync(
            string mensaje,
            Func<IReadOnlyCollection<int>, Task<InspeccionOperacionMasivaV2>> accion,
            string tituloResultado)
        {
            List<int> ids = FotosSeleccionadas
                .Select(item => item.FotografiaId)
                .ToList();

            if (ids.Count == 0)
                return;

            IsBusy = true;
            MensajeEstado = mensaje;
            ActualizarComandos();

            try
            {
                InspeccionOperacionMasivaV2 resultado = await accion(ids);
                await MostrarAlertaAsync(tituloResultado, resultado.Resumen);
                await RecargarDespuesOperacionAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
                await RecargarDespuesOperacionAsync();
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task RecargarDespuesOperacionAsync()
        {
            InspeccionFitosanitariaDetalleV2 actualizado =
                await InspeccionApi.ObtenerDetalleAsync(diagnosticoId);

            AplicarDetalle(actualizado);
        }

        private static async Task<string> CopiarTemporalAsync(
            FileResult archivo)
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

        private async Task RegresarResultadoAsync()
        {
            string ruta = DiagnosticoIARoutes.CrearRutaSolicitud(origen);
            await GoToAsyncParameters(ruta);
        }

        private void NotificarSeleccion()
        {
            OnPropertyChanged(nameof(FotosSeleccionadas));
            OnPropertyChanged(nameof(TieneSeleccion));
            OnPropertyChanged(nameof(CantidadSeleccionada));
            OnPropertyChanged(nameof(TextoSeleccion));
            OnPropertyChanged(nameof(PuedeProcesarSeleccion));
            OnPropertyChanged(nameof(PuedeEnviarSeleccion));
            OnPropertyChanged(nameof(PuedeSolicitarRevision));
            OnPropertyChanged(nameof(PuedeDescartarSeleccion));
            OnPropertyChanged(nameof(PuedeAnalizarSeleccion));
            OnPropertyChanged(nameof(PuedeAprobarSeleccion));
            OnPropertyChanged(nameof(PuedePublicarSeleccion));
            OnPropertyChanged(nameof(PuedeAgregarFotografias));
            OnPropertyChanged(nameof(PuedeCerrarInspeccion));
            OnPropertyChanged(nameof(MostrarCierreTecnico));
            OnPropertyChanged(nameof(MotivoNoPuedeCerrar));
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            AgregarFotografiasCommand.ChangeCanExecute();
            CerrarInspeccionCommand.ChangeCanExecute();
            SeleccionarTodoCommand.ChangeCanExecute();
            QuitarSeleccionCommand.ChangeCanExecute();
            ProcesarSeleccionCommand.ChangeCanExecute();
            EnviarAnalizadorCommand.ChangeCanExecute();
            SolicitarRevisionCommand.ChangeCanExecute();
            DescartarCommand.ChangeCanExecute();
            AnalisisHumanoCommand.ChangeCanExecute();
            AprobarCommand.ChangeCanExecute();
            PublicarAlbumCommand.ChangeCanExecute();
            RegresarResultadoCommand.ChangeCanExecute();
        }
    }
}
