using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Media;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

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
        private readonly AlbumBotanicoApiService albumBotanicoApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
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
                () => !IsBusy && EsInspeccionAbierta && Fotografias.Count > 0);
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
                OnPropertyChanged(nameof(EsInspeccionCerrada));
                OnPropertyChanged(nameof(EsInspeccionAbierta));
            }
        }

        public bool TieneDetalle => Detalle != null;
        public bool EsInspeccionCerrada => Detalle?.CerradaTecnico == true;
        public bool EsInspeccionAbierta => !EsInspeccionCerrada;

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
            .Where(item => item.Seleccionada && item.PuedeSeleccionarse)
            .OrderBy(item => item.Orden)
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
            Detalle?.PuedeGestionarSolicitud == true &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.PendienteDecisionTecnico or
                InspeccionFotoEstados.ErrorIA);

        public bool PuedeDescartarSeleccion =>
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            FotosSeleccionadas.All(item =>
                !item.EsEstadoFinal &&
                !item.EstaProcesando);

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
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial => "Historial",
            DiagnosticoIARoutes.ModoAnalizador =>
                "Bandeja del analizador",
            DiagnosticoIARoutes.ModoAprobador =>
                "Bandeja del aprobador",
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

                /*
                 * Los resultados generados con versiones anteriores podían
                 * marcar una planta sana como NO_APLICA también para el Álbum
                 * Botánico. La normalización se ejecuta una sola vez cuando el
                 * detalle todavía necesita esa corrección y luego se recarga.
                 */
                if (RequiereNormalizarPlantasSanas(actualizado))
                {
                    try
                    {
                        await InspeccionApi.NormalizarPlantasSanasAsync(
                            diagnosticoId);

                        actualizado =
                            await InspeccionApi.ObtenerDetalleAsync(
                                diagnosticoId);
                    }
                    catch (InspeccionFitosanitariaApiException)
                    {
                        /*
                         * El modelo visual conserva una propuesta de respaldo
                         * para no ocultar la clasificación mientras el backend
                         * nuevo todavía no haya sido publicado.
                         */
                    }
                }

                await AplicarDetalleAsync(actualizado);
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

                await AplicarDetalleAsync(actualizado);

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

        private static bool RequiereNormalizarPlantasSanas(
            InspeccionFitosanitariaDetalleV2 detalle) =>
            detalle.Fotografias.Any(foto =>
                foto.ResultadoIA?.EsAparentementeSana == true &&
                foto.ResultadoIA.TieneFichaAlbumCoincidente == false &&
                foto.ResultadoIA.RequiereDecisionClasificacion == false);

        private async Task AplicarDetalleAsync(
            InspeccionFitosanitariaDetalleV2 actualizado)
        {
            Detalle = actualizado;

            foreach (InspeccionFotoV2 anterior in Fotografias)
                anterior.PropertyChanged -= FotoPropertyChanged;

            Fotografias.Clear();
            foreach (InspeccionFotoV2 foto in actualizado.Fotografias)
            {
                if (!foto.PuedeSeleccionarse)
                    foto.Seleccionada = false;

                foto.PropertyChanged += FotoPropertyChanged;
                Fotografias.Add(foto);
            }

            await CargarJerarquiaAlbumAsync();

            OnPropertyChanged(nameof(PuedeAgregarFotografias));
            OnPropertyChanged(nameof(PuedeCerrarInspeccion));
            OnPropertyChanged(nameof(MostrarCierreTecnico));
            OnPropertyChanged(nameof(MotivoNoPuedeCerrar));
            OnPropertyChanged(nameof(SubtituloResultado));
            NotificarSeleccion();
        }

        private async Task CargarJerarquiaAlbumAsync()
        {
            if (diagnosticoId <= 0 || Fotografias.Count == 0)
                return;

            ApiResult<List<JerarquiaDiagnosticoFotoResponse>> resultado =
                await jerarquiaApi.GetJerarquiaDiagnosticoAsync(diagnosticoId);

            if (!resultado.Success)
                return;

            List<JerarquiaDiagnosticoFotoResponse> items =
                resultado.Data ?? [];

            JerarquiaAlbumCacheService.Establecer(
                diagnosticoId,
                items);

            Dictionary<int, JerarquiaDiagnosticoFotoResponse> mapa = items
                .Where(item => item.FotografiaId > 0)
                .GroupBy(item => item.FotografiaId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Last());

            foreach (InspeccionFotoV2 foto in Fotografias)
            {
                foto.JerarquiaAlbum = mapa.TryGetValue(
                    foto.FotografiaId,
                    out JerarquiaDiagnosticoFotoResponse? jerarquia)
                    ? jerarquia
                    : null;
            }
        }

        private void FotoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InspeccionFotoV2.Seleccionada))
                NotificarSeleccion();
        }

        private void SeleccionarTodo()
        {
            foreach (InspeccionFotoV2 foto in Fotografias.Where(item =>
                         item.PuedeSeleccionarse &&
                         !item.EsEstadoFinal))
            {
                foto.Seleccionada = true;
            }

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
                "El cierre es definitivo y solo está disponible cuando todas las fotografías finalizaron. Después de continuar no podrá modificar, procesar, descartar, aprobar ni publicar ninguna evidencia. Solo podrá consultarse.");
            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Cerrando definitivamente la inspección...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 actualizado =
                    await InspeccionApi.CerrarInspeccionAsync(diagnosticoId);

                await AplicarDetalleAsync(actualizado);

                await MostrarAlertaAsync(
                    "Inspección cerrada",
                    "La inspección quedó cerrada definitivamente y en modo de solo lectura.");
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
                "Cada fotografía se enviará en una petición independiente. Un error no detendrá ni revertirá las demás.");

            if (!confirmar)
                return;

            await EjecutarOperacionAsync(
                "Analizando fotografías seleccionadas...",
                ids => EjecutarPorFotografiaAsync(
                    ids,
                    fotografiaId => InspeccionApi.ProcesarFotosAsync(
                        diagnosticoId,
                        [fotografiaId])),
                "Análisis completado");
        }

        private async Task<InspeccionOperacionMasivaV2>
            EjecutarPorFotografiaAsync(
                IReadOnlyCollection<int> ids,
                Func<int, Task<InspeccionOperacionMasivaV2>> accion)
        {
            var acumulado = new InspeccionOperacionMasivaV2
            {
                TotalSolicitadas = ids.Count
            };

            foreach (int fotografiaId in ids.Distinct())
            {
                try
                {
                    InspeccionOperacionMasivaV2 parcial =
                        await accion(fotografiaId);

                    AcumularResultado(acumulado, parcial);
                }
                catch (Exception ex)
                {
                    acumulado.TotalConError++;
                    acumulado.Resultados.Add(
                        new InspeccionOperacionItemV2
                        {
                            FotografiaId = fotografiaId,
                            Exitoso = false,
                            Mensaje = ex.Message
                        });
                }
            }

            return acumulado;
        }

        private async Task EnviarAnalizadorAsync()
        {
            if (!PuedeEnviarSeleccion)
                return;

            await EjecutarOperacionAsync(
                "Preparando fotografías para la revisión humana...",
                ids => EjecutarPorFotografiaAsync(
                    ids,
                    fotografiaId => InspeccionApi.EnviarAnalizadorAsync(
                        diagnosticoId,
                        [fotografiaId])),
                "Fotografías preparadas");
        }

        private async Task SolicitarRevisionAsync()
        {
            if (!PuedeSolicitarRevision || Shell.Current == null)
                return;

            await EjecutarSecuenciaIndividualAsync(
                "Reevaluando con IA",
                "Reevaluación completada",
                async (foto, indice, total) =>
                {
                    /*
                     * La fotografía, el motivo y el diagnóstico opcional se
                     * presentan en una sola interfaz. Cada formulario pertenece
                     * exclusivamente a la fotografía que se está procesando.
                     */
                    var formulario = new RevisionIAFotografiaPage(
                        foto,
                        indice,
                        total);

                    Task<RevisionIAFormularioResultado?> esperaResultado =
                        formulario.EsperarResultadoAsync();

                    await Shell.Current.Navigation.PushModalAsync(
                        formulario,
                        animated: false);

                    RevisionIAFormularioResultado? resultado =
                        await esperaResultado;

                    if (resultado == null)
                        return null;

                    return await InspeccionApi.SolicitarRevisionIAAsync(
                        diagnosticoId,
                        [foto.FotografiaId],
                        resultado.Motivo,
                        resultado.DiagnosticoPropuesto);
                });
        }

        private async Task DescartarAsync()
        {
            if (!PuedeDescartarSeleccion || Shell.Current == null)
                return;

            await EjecutarSecuenciaIndividualAsync(
                "Registrando descarte",
                "Descarte registrado",
                async (foto, indice, total) =>
                {
                    string? motivo = await Shell.Current.DisplayPromptAsync(
                        $"Descartar fotografía · {indice} de {total}",
                        $"{foto.Titulo}. Indique el motivo exclusivo de esta evidencia. La imagen y su historial no se eliminarán.",
                        "Descartar",
                        "Cancelar",
                        "Motivo obligatorio",
                        1000,
                        Keyboard.Default);

                    if (motivo == null)
                        return null;

                    motivo = motivo.Trim();
                    if (motivo.Length < 8)
                    {
                        await MostrarAlertaAsync(
                            "Motivo requerido",
                            $"El descarte de {foto.Titulo} necesita al menos 8 caracteres.");
                        return null;
                    }

                    return await InspeccionApi.DescartarFotosAsync(
                        diagnosticoId,
                        [foto.FotografiaId],
                        motivo);
                });
        }

        private async Task RegistrarAnalisisHumanoAsync()
        {
            if (!PuedeAnalizarSeleccion || Shell.Current == null)
                return;

            await EjecutarSecuenciaIndividualAsync(
                "Guardando clasificación humana",
                "Clasificaciones humanas",
                async (foto, indice, total) =>
                {
                    string? diagnostico = await Shell.Current.DisplayPromptAsync(
                        $"Clasificación humana · {indice} de {total}",
                        $"{foto.Titulo}. Escriba el diagnóstico exclusivo de esta fotografía.",
                        "Continuar",
                        "Cancelar",
                        "Diagnóstico obligatorio",
                        300,
                        Keyboard.Default);

                    if (string.IsNullOrWhiteSpace(diagnostico))
                        return null;

                    string? categoria = await Shell.Current.DisplayActionSheet(
                        $"Categoría principal · {indice} de {total}",
                        "Cancelar",
                        null,
                        "ENFERMEDAD",
                        "PLAGA",
                        "ALTERACION_NUTRICIONAL",
                        "ESTRES_ABIOTICO",
                        "DANO_MECANICO",
                        "AFECTACION_NO_DETERMINADA",
                        "NO_APLICA");

                    if (string.IsNullOrWhiteSpace(categoria) ||
                        categoria == "Cancelar")
                    {
                        return null;
                    }

                    string? severidad = await Shell.Current.DisplayActionSheet(
                        $"Severidad visual · {indice} de {total}",
                        "Cancelar",
                        null,
                        "LEVE",
                        "MODERADA",
                        "SEVERA",
                        "NO_EVALUABLE",
                        "NO_APLICA");

                    if (string.IsNullOrWhiteSpace(severidad) ||
                        severidad == "Cancelar")
                    {
                        return null;
                    }

                    string? certeza = await Shell.Current.DisplayActionSheet(
                        $"Nivel de certeza · {indice} de {total}",
                        "Cancelar",
                        null,
                        "ALTO",
                        "MEDIO",
                        "BAJO",
                        "NO_DETERMINADO");

                    if (string.IsNullOrWhiteSpace(certeza) ||
                        certeza == "Cancelar")
                    {
                        return null;
                    }

                    string? observaciones =
                        await Shell.Current.DisplayPromptAsync(
                            $"Observaciones · {indice} de {total}",
                            $"Observaciones técnicas opcionales para {foto.Titulo}.",
                            "Continuar",
                            "Omitir",
                            "Opcional",
                            3000,
                            Keyboard.Default);

                    bool enviar = await ConfirmarAsync(
                        $"Enviar al aprobador · {indice} de {total}",
                        $"¿Desea enviar ahora la clasificación de {foto.Titulo}? Si cancela, únicamente esta fotografía quedará como borrador humano.");

                    InspeccionFotoResultadoIAV2? ia = foto.ResultadoIA;

                    if (!foto.TieneClasificacionAlbumCompleta)
                    {
                        bool clasificacionLista =
                            await GestionarClasificacionAlbumAnalizadorAsync(
                                foto,
                                indice,
                                total);

                        if (!clasificacionLista)
                            return null;
                    }

                    var item = new InspeccionFotoAnalisisHumanoRequestV2
                    {
                        FotografiaId = foto.FotografiaId,
                        CalidadEvaluacion =
                            ia?.CalidadEvaluacion ?? "NO_EVALUABLE",
                        EstadoGeneral =
                            ia?.EstadoGeneral ?? "INDETERMINADA",
                        CategoriaPrincipal = categoria,
                        CategoriasSecundarias =
                            ia?.CategoriasSecundarias ?? [],
                        Diagnostico = diagnostico.Trim(),
                        TipoDiagnostico =
                            ia?.TipoDiagnostico ?? string.Empty,
                        Severidad = severidad,
                        NivelCerteza = certeza,
                        Observaciones =
                            observaciones?.Trim() ?? string.Empty
                    };

                    return await InspeccionApi.GuardarAnalisisHumanoAsync(
                        diagnosticoId,
                        [item],
                        enviar);
                });
        }

        private async Task RegistrarAprobacionAsync()
        {
            if (!PuedeAprobarSeleccion || Shell.Current == null)
                return;

            await EjecutarSecuenciaIndividualAsync(
                "Registrando aprobación",
                "Decisiones del aprobador",
                async (foto, indice, total) =>
                {
                    InspeccionFotoAnalisisHumanoV2? humano =
                        foto.UltimoAnalisisHumano;

                    if (humano == null)
                    {
                        throw new InvalidOperationException(
                            $"{foto.Titulo} no tiene una clasificación humana enviada.");
                    }

                    string? decision = await Shell.Current.DisplayActionSheet(
                        $"Decisión del aprobador · {indice} de {total}",
                        "Cancelar",
                        null,
                        "APROBAR",
                        "APROBAR_CON_CORRECCION",
                        "DEVOLVER_AL_ANALIZADOR",
                        "RECHAZAR",
                        "NO_CONCLUYENTE");

                    if (string.IsNullOrWhiteSpace(decision) ||
                        decision == "Cancelar")
                    {
                        return null;
                    }

                    string diagnosticoFinal = humano.Diagnostico;
                    if (decision == "APROBAR_CON_CORRECCION")
                    {
                        string? correccion =
                            await Shell.Current.DisplayPromptAsync(
                                $"Diagnóstico final · {indice} de {total}",
                                $"Escriba la corrección exclusiva de {foto.Titulo}.",
                                "Continuar",
                                "Cancelar",
                                "Diagnóstico final",
                                300,
                                Keyboard.Default);

                        if (string.IsNullOrWhiteSpace(correccion))
                            return null;

                        diagnosticoFinal = correccion.Trim();
                    }

                    string? observaciones =
                        await Shell.Current.DisplayPromptAsync(
                            $"Observaciones de aprobación · {indice} de {total}",
                            $"Documente opcionalmente la decisión de {foto.Titulo}.",
                            "Continuar",
                            "Omitir",
                            "Opcional",
                            3000,
                            Keyboard.Default);

                    bool autorizaAlbum = false;
                    if (decision is "APROBAR" or
                        "APROBAR_CON_CORRECCION")
                    {
                        if (!foto.TieneClasificacionAlbumCompleta)
                        {
                            bool clasificacionResuelta =
                                await GestionarClasificacionAlbumAprobadorAsync(
                                    foto,
                                    indice,
                                    total);

                            if (!clasificacionResuelta)
                                return null;
                        }

                        autorizaAlbum = await ConfirmarAsync(
                            $"Autorizar álbum · {indice} de {total}",
                            $"¿La fotografía {foto.Orden} podrá utilizarse posteriormente en el Álbum Botánico?");
                    }

                    var item = new InspeccionFotoAprobacionRequestV2
                    {
                        FotografiaId = foto.FotografiaId,
                        Decision = decision,
                        CalidadEvaluacionFinal =
                            humano.CalidadEvaluacion,
                        EstadoGeneralFinal = humano.EstadoGeneral,
                        CategoriaPrincipalFinal =
                            humano.CategoriaPrincipal,
                        CategoriasSecundariasFinales =
                            humano.CategoriasSecundarias,
                        DiagnosticoFinal = diagnosticoFinal,
                        TipoDiagnosticoFinal =
                            humano.TipoDiagnostico,
                        SeveridadFinal = humano.Severidad,
                        NivelCertezaFinal = humano.NivelCerteza,
                        Observaciones =
                            observaciones?.Trim() ?? string.Empty,
                        AutorizaPublicacionAlbum = autorizaAlbum
                    };

                    return await InspeccionApi.RegistrarAprobacionesAsync(
                        diagnosticoId,
                        [item]);
                });
        }

        private async Task<bool> GestionarClasificacionAlbumAnalizadorAsync(
            InspeccionFotoV2 foto,
            int indice,
            int total)
        {
            if (Shell.Current == null || foto.ResultadoIA == null)
                return false;

            var pagina = new JerarquiaAlbumFotografiaPage(
                diagnosticoId,
                foto,
                "ANALIZADOR");

            await Shell.Current.Navigation.PushModalAsync(pagina);
            bool guardado = await pagina.ResultadoTask;

            if (guardado)
                await CargarJerarquiaAlbumAsync();

            return guardado;
        }

        private async Task<bool> GestionarClasificacionAlbumAprobadorAsync(
            InspeccionFotoV2 foto,
            int indice,
            int total)
        {
            if (Shell.Current == null || foto.ResultadoIA == null)
                return false;

            var pagina = new JerarquiaAlbumFotografiaPage(
                diagnosticoId,
                foto,
                "APROBADOR");

            await Shell.Current.Navigation.PushModalAsync(pagina);
            bool guardado = await pagina.ResultadoTask;

            if (guardado)
                await CargarJerarquiaAlbumAsync();

            return guardado;
        }

        private async Task<(
            CategoriaAlbumBotanicoResponse? Categoria,
            List<InspeccionAlbumFichaV2> Fichas)>
            CargarContextoClasificacionAlbumAsync(
                InspeccionFotoResultadoIAV2 resultado)
        {
            ApiResult<List<CategoriaAlbumBotanicoResponse>> categoriasResult =
                await albumBotanicoApi.GetCategoriasAsync(false);

            if (!categoriasResult.Success || categoriasResult.Data == null)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(categoriasResult.Message)
                        ? "No fue posible cargar los capítulos activos del Álbum Botánico."
                        : categoriasResult.Message);
            }

            List<CategoriaAlbumBotanicoResponse> categorias =
                categoriasResult.Data
                    .Where(item => item.Activo)
                    .ToList();

            CategoriaAlbumBotanicoResponse? categoria = categorias
                .FirstOrDefault(item =>
                    item.CategoriaAlbumBotanicoId ==
                    resultado.CategoriaAlbumBotanicoIdSugerida);

            if (categoria == null)
            {
                string nombreSugerido = NormalizarComparacion(
                    resultado.CategoriaAlbumPropuesta);

                categoria = categorias.FirstOrDefault(item =>
                    NormalizarComparacion(item.NombreCategoria) ==
                    nombreSugerido);
            }

            if (categoria == null && resultado.EsAparentementeSana)
            {
                categoria = categorias.FirstOrDefault(item =>
                    NormalizarComparacion(item.NombreCategoria)
                        .Contains("PLANTASSANA", StringComparison.Ordinal));
            }

            var fichas = new List<InspeccionAlbumFichaV2>();

            if (categoria != null)
            {
                List<InspeccionAlbumCategoriaV2> catalogo =
                    await InspeccionApi.ObtenerCatalogoAlbumAsync();

                fichas = catalogo
                    .FirstOrDefault(item =>
                        item.CategoriaAlbumBotanicoId ==
                        categoria.CategoriaAlbumBotanicoId)
                    ?.Fichas
                    .Where(item => item.AlbumBotanicoCafeId > 0)
                    .OrderBy(item => item.Titulo)
                    .ToList() ?? [];
            }

            return (categoria, fichas);
        }

        private async Task<InspeccionAlbumFichaV2?>
            SeleccionarFichaExistenteAsync(
                CategoriaAlbumBotanicoResponse categoria,
                IReadOnlyCollection<InspeccionAlbumFichaV2> fichas,
                int? fichaSugeridaId)
        {
            if (Shell.Current == null || fichas.Count == 0)
                return null;

            List<InspeccionAlbumFichaV2> ordenadas = fichas
                .OrderByDescending(item =>
                    item.AlbumBotanicoCafeId == fichaSugeridaId)
                .ThenBy(item => item.Titulo)
                .ToList();

            string? seleccion = await Shell.Current.DisplayActionSheet(
                $"Ficha de {categoria.NombreCategoria}",
                "Cancelar",
                null,
                ordenadas.Select(item => item.TextoSeleccion).ToArray());

            if (string.IsNullOrWhiteSpace(seleccion) || seleccion == "Cancelar")
                return null;

            return ordenadas.FirstOrDefault(item =>
                item.TextoSeleccion == seleccion);
        }

        private static string NormalizarComparacion(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string descompuesto = valor
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(descompuesto.Length);

            foreach (char caracter in descompuesto)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter))
                    builder.Append(caracter);
            }

            return builder.ToString();
        }

        private async Task PublicarAlbumAsync()
        {
            if (!PuedePublicarSeleccion || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadas[0];
            JerarquiaDiagnosticoFotoResponse? jerarquia =
                foto.JerarquiaAlbum;

            if (jerarquia?.CategoriaAlbumBotanicoId is not > 0 ||
                jerarquia.SubcategoriaAlbumBotanicoId is not > 0 ||
                jerarquia.AlbumBotanicoCafeId is not > 0 ||
                jerarquia.CategoriaEsPropuesta ||
                jerarquia.SubcategoriaEsPropuesta ||
                jerarquia.FichaEsPropuesta)
            {
                await MostrarAlertaAsync(
                    "Clasificación jerárquica pendiente",
                    "Antes de publicar, el aprobador debe dejar una categoría, una subcategoría y una ficha oficiales para esta fotografía.");
                return;
            }

            string ruta =
                $"{jerarquia.Categoria} → " +
                $"{jerarquia.Subcategoria} → " +
                jerarquia.Ficha;

            bool confirmar = await ConfirmarAsync(
                "Publicar en el Álbum Botánico",
                $"La fotografía se publicará en:\n\n{ruta}\n\n" +
                "La evidencia original permanecerá en la inspección.");

            if (!confirmar)
                return;

            string? descripcion = await Shell.Current.DisplayPromptAsync(
                "Descripción de la fotografía",
                "Descripción opcional para la imagen publicada.",
                "Publicar",
                "Cancelar",
                "Opcional",
                500,
                Keyboard.Default);

            if (descripcion == null)
                return;

            IsBusy = true;
            MensajeEstado = "Publicando referencia aprobada en el álbum...";
            ActualizarComandos();

            try
            {
                await InspeccionApi.PublicarAlbumAsync(
                    diagnosticoId,
                    foto.FotografiaId,
                    jerarquia.CategoriaAlbumBotanicoId.Value,
                    jerarquia.AlbumBotanicoCafeId.Value,
                    descripcion,
                    false,
                    0);

                await MostrarAlertaAsync(
                    "Publicación completada",
                    $"La fotografía fue publicada en {ruta}.");

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

        private async Task EjecutarSecuenciaIndividualAsync(
            string mensajeBase,
            string tituloResultado,
            Func<
                InspeccionFotoV2,
                int,
                int,
                Task<InspeccionOperacionMasivaV2?>> accion)
        {
            List<InspeccionFotoV2> fotos = FotosSeleccionadas.ToList();
            if (fotos.Count == 0)
                return;

            var acumulado = new InspeccionOperacionMasivaV2
            {
                TotalSolicitadas = fotos.Count
            };

            int atendidas = 0;
            bool cancelada = false;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                for (int indice = 0; indice < fotos.Count; indice++)
                {
                    InspeccionFotoV2 foto = fotos[indice];
                    int posicion = indice + 1;

                    MensajeEstado =
                        $"{mensajeBase}: fotografía {posicion} de {fotos.Count}...";

                    try
                    {
                        InspeccionOperacionMasivaV2? parcial =
                            await accion(foto, posicion, fotos.Count);

                        if (parcial == null)
                        {
                            cancelada = true;
                            break;
                        }

                        AcumularResultado(acumulado, parcial);
                    }
                    catch (Exception ex)
                    {
                        acumulado.TotalConError++;
                        acumulado.Resultados.Add(
                            new InspeccionOperacionItemV2
                            {
                                FotografiaId = foto.FotografiaId,
                                Exitoso = false,
                                Mensaje = ex.Message
                            });
                    }

                    atendidas++;
                }

                await RecargarDespuesOperacionAsync();

                int pendientes = Math.Max(0, fotos.Count - atendidas);
                string resumen =
                    $"{acumulado.TotalExitosas} fotografía(s) completadas";

                if (acumulado.TotalConError > 0)
                {
                    resumen +=
                        $" y {acumulado.TotalConError} con error";
                }

                resumen += ".";

                if (cancelada && pendientes > 0)
                {
                    resumen +=
                        $" El proceso se detuvo antes de atender {pendientes} fotografía(s).";
                }

                await MostrarAlertaAsync(tituloResultado, resumen);
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

        private static void AcumularResultado(
            InspeccionOperacionMasivaV2 destino,
            InspeccionOperacionMasivaV2 origen)
        {
            destino.TotalExitosas += origen.TotalExitosas;
            destino.TotalConError += origen.TotalConError;
            destino.Resultados.AddRange(origen.Resultados);
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

            if (RequiereNormalizarPlantasSanas(actualizado))
            {
                try
                {
                    await InspeccionApi.NormalizarPlantasSanasAsync(
                        diagnosticoId);

                    actualizado =
                        await InspeccionApi.ObtenerDetalleAsync(
                            diagnosticoId);
                }
                catch (InspeccionFitosanitariaApiException)
                {
                    /* La propuesta visual de respaldo permanece disponible. */
                }
            }

            await AplicarDetalleAsync(actualizado);
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
            Shell? shell = Shell.Current;
            if (shell == null)
                return;

            string rutaAnterior =
                shell.CurrentState?.Location?.OriginalString ?? string.Empty;

            /*
             * El origen normal es la pantalla que abrió este detalle. Si Shell
             * no conserva esa entrada (por ejemplo, después de una entrada
             * directa), se reconstruye solamente el listado correspondiente.
             */
            try
            {
                await GoToAsyncParameters(AppRoutes.Regresar);
                await Task.Delay(100);
            }
            catch (InvalidOperationException)
            {
                // La entrada directa puede no conservar una página anterior.
            }

            string rutaActual =
                shell.CurrentState?.Location?.OriginalString ?? string.Empty;

            if (string.Equals(
                    rutaAnterior,
                    rutaActual,
                    StringComparison.OrdinalIgnoreCase))
            {
                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaRegresoResultado(origen));
            }
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
