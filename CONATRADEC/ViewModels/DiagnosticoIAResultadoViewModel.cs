using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Detalle operativo de una inspección. El técnico trabaja por fotografía;
    /// las acciones del analizador y aprobador se integran en la página mediante
    /// el flujo de revisión especializado.
    /// </summary>
    public sealed class DiagnosticoIAResultadoViewModel :
        DiagnosticoIAViewModelBase
    {
        private int diagnosticoId;
        private readonly TipoFotografiaIAApiService tiposFotografiaApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly InspeccionFitosanitariaApiService inspeccionApi =
            InspeccionFitosanitariaApiService.Instance;
        private string origen = DiagnosticoIARoutes.ModoMisInspecciones;
        private InspeccionFitosanitariaDetalleV2? detalle;
        private bool soloConsultaAsignacion;
        private string etapaConsultaAsignacion = string.Empty;

        public DiagnosticoIAResultadoViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && diagnosticoId > 0);
            AgregarFotografiasCommand = new Command(
                async () => await AgregarFotografiasAsync(),
                () => !IsBusy && PuedeAgregarFotografias);
            SeleccionarTodoCommand = new Command(
                SeleccionarTodo,
                () => !IsBusy && !SoloConsultaAsignacion &&
                      EsEtapaTecnicaAbierta &&
                      Fotografias.Any(item => item.PuedeSeleccionarse));
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
            AprobarCommand = new Command(
                async () => await RegistrarAprobacionAsync(),
                () => !IsBusy && PuedeAprobarSeleccion);
            PublicarAlbumCommand = new Command(
                async () => await PublicarAlbumAsync(),
                () => !IsBusy && PuedePublicarSeleccion);
            CerrarDefinitivoCommand = new Command(
                async () => await CerrarDefinitivamenteAsync(),
                () => !IsBusy && PuedeCerrarDefinitivamente);
            RegresarResultadoCommand = new Command(
                async () => await RegresarResultadoAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<InspeccionFotoV2> Fotografias { get; } = [];

        public Command ActualizarCommand { get; }
        public Command AgregarFotografiasCommand { get; }
        public Command SeleccionarTodoCommand { get; }
        public Command QuitarSeleccionCommand { get; }
        public Command ProcesarSeleccionCommand { get; }
        public Command EnviarAnalizadorCommand { get; }
        public Command SolicitarRevisionCommand { get; }
        public Command DescartarCommand { get; }
        public Command AprobarCommand { get; }
        public Command PublicarAlbumCommand { get; }
        public Command CerrarDefinitivoCommand { get; }
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
                OnPropertyChanged(nameof(EsEtapaTecnicaAbierta));
                OnPropertyChanged(nameof(PuedeAprobarSeleccion));
                OnPropertyChanged(nameof(PuedePublicarSeleccion));
                OnPropertyChanged(nameof(PuedeCerrarDefinitivamente));
            }
        }

        public bool TieneDetalle => Detalle != null;
        public bool EsInspeccionCerrada => Detalle?.CerradaDefinitiva == true;
        public bool EsEtapaTecnicaAbierta =>
            Detalle is
            {
                EtapaTecnicaFinalizada: false,
                CerradaDefinitiva: false
            };

        public bool SoloConsultaAsignacion => soloConsultaAsignacion;

        public string TituloResultado =>
            Detalle?.Titulo ?? "Inspección fitosanitaria";

        public string SubtituloResultado
        {
            get
            {
                if (Detalle == null)
                    return "Cargando expediente...";

                string baseTexto =
                    $"{Detalle.TerrenoTexto} · Estado: {Detalle.EstadoTexto} · " +
                    Detalle.CierreTexto;

                if (!SoloConsultaAsignacion)
                    return baseTexto;

                string etapa = string.IsNullOrWhiteSpace(etapaConsultaAsignacion)
                    ? "esta etapa"
                    : $"la etapa de {etapaConsultaAsignacion}";

                return $"Solo consulta · Asignada a otro responsable para {etapa} · {baseTexto}";
            }
        }

        public bool PuedeAgregarFotografias =>
            !SoloConsultaAsignacion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta &&
            Fotografias.Count < 40;

        public bool PuedeCerrarInspeccion =>
            !SoloConsultaAsignacion &&
            Detalle?.PuedeCerrarInspeccion == true &&
            EsEtapaTecnicaAbierta;

        public bool MostrarCierreTecnico =>
            !SoloConsultaAsignacion &&
            Detalle is
            {
                PuedeGestionarSolicitud: true,
                EtapaTecnicaFinalizada: false,
                CerradaDefinitiva: false
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
            !SoloConsultaAsignacion &&
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.Borrador or
                InspeccionFotoEstados.PendienteIA or
                InspeccionFotoEstados.ErrorIA or
                InspeccionFotoEstados.NoConcluyente);

        public bool PuedeEnviarSeleccion =>
            !SoloConsultaAsignacion &&
            TieneSeleccion &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta &&
            FotosSeleccionadas.All(item => item.Estado ==
                InspeccionFotoEstados.PendienteDecisionTecnico);

        public bool PuedeSolicitarRevision =>
            !SoloConsultaAsignacion &&
            CantidadSeleccionada == 1 &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta &&
            FotosSeleccionadas.All(item => item.PuedeSolicitarRevisionIA) &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.PendienteDecisionTecnico or
                InspeccionFotoEstados.ErrorIA);

        public bool PuedeDescartarSeleccion =>
            !SoloConsultaAsignacion &&
            CantidadSeleccionada == 1 &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta &&
            FotosSeleccionadas.All(item => item.Estado is
                InspeccionFotoEstados.Borrador or
                InspeccionFotoEstados.PendienteIA or
                InspeccionFotoEstados.ErrorIA or
                InspeccionFotoEstados.PendienteDecisionTecnico or
                InspeccionFotoEstados.DevueltaTecnico);

        public bool PuedeAprobarSeleccion =>
            !SoloConsultaAsignacion &&
            CantidadSeleccionada == 1 &&
            Detalle?.PuedeAprobar == true &&
            !EsInspeccionCerrada &&
            FotosSeleccionadas[0].Estado ==
                InspeccionFotoEstados.PendienteAprobacion;

        public bool PuedePublicarSeleccion =>
            !SoloConsultaAsignacion &&
            CantidadSeleccionada == 1 &&
            Detalle?.PuedePublicarAlbum == true &&
            FotosSeleccionadas[0].PuedePublicarseEnAlbum &&
            FotosSeleccionadas[0].TieneClasificacionAlbumCompleta;

        public bool PuedeCerrarDefinitivamente =>
            !SoloConsultaAsignacion &&
            string.Equals(
                origen,
                DiagnosticoIARoutes.ModoAprobador,
                StringComparison.OrdinalIgnoreCase) &&
            Detalle?.PuedeAprobar == true &&
            Detalle.EtapaTecnicaFinalizada &&
            !Detalle.CerradaDefinitiva &&
            Fotografias.Count > 0 &&
            Fotografias.All(item => item.EsEstadoFinal);

        public string TextoRegresar => SoloConsultaAsignacion
            ? "Volver a la bandeja"
            : origen switch
            {
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
            soloConsultaAsignacion = false;
            etapaConsultaAsignacion = string.Empty;
            OnPropertyChanged(nameof(SoloConsultaAsignacion));
            OnPropertyChanged(nameof(TextoRegresar));
            OnPropertyChanged(nameof(SubtituloResultado));
            OnPropertyChanged(nameof(PuedeCerrarDefinitivamente));
            ActualizarComandos();
        }

        /// <summary>
        /// Convierte el expediente en una vista de consulta cuando la etapa ya
        /// pertenece a otro responsable. El backend sigue siendo la autoridad
        /// final; esta bandera evita presentar acciones de escritura en móvil y
        /// Windows mientras se conserva el acceso de lectura.
        /// </summary>
        public void ConfigurarSoloConsultaAsignacion(
            bool soloConsulta,
            string? etapa = null)
        {
            string etapaNormalizada = (etapa ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            if (soloConsultaAsignacion == soloConsulta &&
                etapaConsultaAsignacion == etapaNormalizada)
            {
                return;
            }

            soloConsultaAsignacion = soloConsulta;
            etapaConsultaAsignacion = soloConsulta
                ? etapaNormalizada
                : string.Empty;

            if (soloConsulta)
            {
                foreach (InspeccionFotoV2 foto in Fotografias)
                    foto.Seleccionada = false;
            }

            OnPropertyChanged(nameof(SoloConsultaAsignacion));
            OnPropertyChanged(nameof(TextoRegresar));
            OnPropertyChanged(nameof(SubtituloResultado));
            OnPropertyChanged(nameof(PuedeAgregarFotografias));
            OnPropertyChanged(nameof(PuedeCerrarInspeccion));
            OnPropertyChanged(nameof(MostrarCierreTecnico));
            OnPropertyChanged(nameof(PuedeProcesarSeleccion));
            OnPropertyChanged(nameof(PuedeEnviarSeleccion));
            OnPropertyChanged(nameof(PuedeSolicitarRevision));
            OnPropertyChanged(nameof(PuedeDescartarSeleccion));
            OnPropertyChanged(nameof(PuedeAprobarSeleccion));
            OnPropertyChanged(nameof(PuedePublicarSeleccion));
            OnPropertyChanged(nameof(PuedeCerrarDefinitivamente));
            NotificarSeleccion();
            ActualizarComandos();
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
                    await inspeccionApi.ObtenerDetalleAsync(diagnosticoId);

                if (RequiereNormalizarPlantasSanas(actualizado))
                {
                    try
                    {
                        await inspeccionApi.NormalizarPlantasSanasAsync(
                            diagnosticoId);
                        actualizado = await inspeccionApi.ObtenerDetalleAsync(
                            diagnosticoId);
                    }
                    catch (InspeccionFitosanitariaApiException)
                    {
                        // La propuesta visible permanece disponible como respaldo.
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

        private static bool RequiereNormalizarPlantasSanas(
            InspeccionFitosanitariaDetalleV2 detalle) =>
            !detalle.CerradaDefinitiva &&
            detalle.Fotografias.Any(foto =>
                foto.ResultadoIA?.EsAparentementeSana == true &&
                foto.ResultadoIA.TieneFichaAlbumCoincidente == false &&
                foto.ResultadoIA.RequiereDecisionClasificacion == false);

        private async Task AplicarDetalleAsync(
            InspeccionFitosanitariaDetalleV2 actualizado)
        {
            foreach (InspeccionFotoV2 anterior in Fotografias)
                anterior.PropertyChanged -= OnFotoPropertyChanged;

            Detalle = actualizado;
            Fotografias.Clear();

            foreach (InspeccionFotoV2 foto in actualizado.Fotografias
                         .OrderBy(item => item.Orden))
            {
                foto.Seleccionada = false;
                foto.PropertyChanged += OnFotoPropertyChanged;
                Fotografias.Add(foto);
            }

            await CargarJerarquiaAsync(actualizado.InspeccionId);
            NotificarSeleccion();
            ActualizarComandos();
        }

        private async Task CargarJerarquiaAsync(int inspeccionId)
        {
            try
            {
                ApiResult<List<JerarquiaDiagnosticoFotoResponse>> resultado =
                    await jerarquiaApi.GetJerarquiaDiagnosticoAsync(
                        inspeccionId);

                if (!resultado.Success || resultado.Data == null)
                    return;

                Dictionary<int, JerarquiaDiagnosticoFotoResponse> porFoto =
                    resultado.Data
                        .GroupBy(item => item.FotografiaId)
                        .ToDictionary(group => group.Key, group => group.Last());

                foreach (InspeccionFotoV2 foto in Fotografias)
                {
                    foto.JerarquiaAlbum = porFoto.GetValueOrDefault(
                        foto.FotografiaId);
                }
            }
            catch
            {
                // La clasificación IA permanece visible aunque el catálogo no
                // pueda cargarse temporalmente.
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

            archivos = archivos.Take(disponibles).ToList();
            if (archivos.Count == 0)
                return;

            string? fechaTexto = await Shell.Current.DisplayPromptAsync(
                "Fecha de identificación en campo",
                "Ingrese la fecha en formato yyyy-MM-dd.",
                "Continuar",
                "Cancelar",
                DateTime.Today.ToString("yyyy-MM-dd"),
                10,
                Keyboard.Text);

            if (fechaTexto == null)
                return;

            if (!DateTime.TryParseExact(
                    fechaTexto.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fechaCampo) ||
                fechaCampo.Date > DateTime.Today)
            {
                await MostrarAlertaAsync(
                    "Fecha no válida",
                    "Use yyyy-MM-dd y no indique una fecha futura.");
                return;
            }

            ApiResult<List<TipoFotografiaIAItem>> tiposResult =
                await tiposFotografiaApi.ListarActivosAsync();

            List<TipoFotografiaIAItem> tipos = tiposResult.Data?
                .Where(item => item.Activo)
                .OrderBy(item => item.Orden)
                .ThenBy(item => item.Nombre)
                .ToList() ?? [];

            if (!tiposResult.Success || tipos.Count == 0)
            {
                await MostrarAlertaAsync(
                    "Catálogo requerido",
                    string.IsNullOrWhiteSpace(tiposResult.Message)
                        ? "No hay tipos de fotografía activos."
                        : tiposResult.Message);
                return;
            }

            string? seleccionTipo = await Shell.Current.DisplayActionSheet(
                "Tipo de fotografía",
                "Cancelar",
                null,
                tipos.Select(item => item.NombreMostrar).ToArray());

            TipoFotografiaIAItem? tipo = tipos.FirstOrDefault(item =>
                string.Equals(
                    item.NombreMostrar,
                    seleccionTipo,
                    StringComparison.Ordinal));

            if (tipo == null)
                return;

            var temporales = new List<InspeccionFotoLocal>();

            try
            {
                foreach (FileResult archivo in archivos)
                {
                    string ruta = await CopiarTemporalAsync(archivo);
                    temporales.Add(new InspeccionFotoLocal
                    {
                        RutaLocal = ruta,
                        NombreArchivo = archivo.FileName,
                        TipoContenido = archivo.ContentType ?? "image/jpeg",
                        FechaIdentificacionCampo = fechaCampo.Date,
                        TipoFotografiaSeleccionada = tipo
                    });
                }

                bool confirmar = await Shell.Current.DisplayAlert(
                    "Agregar fotografías",
                    $"Se incorporarán {temporales.Count} fotografía(s) a la inspección. Quedarán pendientes de análisis IA.",
                    "Agregar",
                    "Cancelar");

                if (!confirmar)
                    return;

                IsBusy = true;
                MensajeEstado = "Agregando fotografías...";
                ActualizarComandos();

                await inspeccionApi.AgregarFotosAsync(
                    diagnosticoId,
                    temporales);
                await ActualizarAsyncForzado();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                foreach (InspeccionFotoLocal temporal in temporales)
                {
                    try
                    {
                        if (File.Exists(temporal.RutaLocal))
                            File.Delete(temporal.RutaLocal);
                    }
                    catch
                    {
                    }
                }

                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task ProcesarSeleccionAsync()
        {
            await EjecutarOperacionAsync(
                "Analizando fotografías con IA...",
                () => inspeccionApi.ProcesarFotosAsync(
                    diagnosticoId,
                    FotosSeleccionadas.Select(item => item.FotografiaId)
                        .ToList()));
        }

        private async Task EnviarAnalizadorAsync()
        {
            await EjecutarOperacionAsync(
                "Enviando fotografías al analizador...",
                () => inspeccionApi.EnviarAnalizadorAsync(
                    diagnosticoId,
                    FotosSeleccionadas.Select(item => item.FotografiaId)
                        .ToList()));
        }

        private async Task SolicitarRevisionAsync()
        {
            if (!PuedeSolicitarRevision || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadas[0];
            if (!foto.PuedeSolicitarRevisionIA)
            {
                await MostrarAlertaAsync(
                    "Límite de reevaluaciones alcanzado",
                    foto.RevisionesIATexto +
                    ". Puede enviar la fotografía al analizador humano o continuar con otra decisión técnica.");
                return;
            }

            string? retroalimentacion = await Shell.Current.DisplayPromptAsync(
                "Solicitar nueva evaluación IA",
                "Explique qué debe revisar nuevamente la IA.",
                "Solicitar",
                "Cancelar",
                string.Empty,
                2000,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(retroalimentacion) ||
                retroalimentacion.Trim().Length < 8)
            {
                if (retroalimentacion != null)
                {
                    await MostrarAlertaAsync(
                        "Información insuficiente",
                        "La retroalimentación debe contener al menos 8 caracteres.");
                }
                return;
            }

            await EjecutarOperacionAsync(
                "Solicitando nueva evaluación IA...",
                () => inspeccionApi.SolicitarRevisionIAAsync(
                    diagnosticoId,
                    [foto.FotografiaId],
                    retroalimentacion,
                    foto.ResultadoIA?.DiagnosticoProbable));
        }

        private async Task DescartarAsync()
        {
            if (!PuedeDescartarSeleccion || Shell.Current == null)
                return;

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Descartar fotografía",
                "Indique el motivo del descarte. La evidencia seguirá disponible en auditoría.",
                "Descartar",
                "Cancelar",
                string.Empty,
                1000,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(motivo) ||
                motivo.Trim().Length < 8)
            {
                if (motivo != null)
                {
                    await MostrarAlertaAsync(
                        "Motivo requerido",
                        "El motivo debe contener al menos 8 caracteres.");
                }
                return;
            }

            InspeccionFotoV2 foto = FotosSeleccionadas[0];
            await EjecutarOperacionAsync(
                "Descartando fotografía...",
                () => inspeccionApi.DescartarFotosAsync(
                    diagnosticoId,
                    [foto.FotografiaId],
                    motivo));
        }

        private async Task RegistrarAprobacionAsync()
        {
            if (!PuedeAprobarSeleccion || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadas[0];
            InspeccionFotoAnalisisHumanoV2? humano =
                foto.UltimoAnalisisHumano;

            if (humano == null)
            {
                await MostrarAlertaAsync(
                    "Clasificación requerida",
                    "La fotografía no tiene una clasificación humana enviada.");
                return;
            }

            string? decision = await Shell.Current.DisplayActionSheet(
                "Decisión del aprobador",
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
                return;
            }

            string diagnosticoFinal = humano.Diagnostico;
            if (decision == "APROBAR_CON_CORRECCION")
            {
                string? correccion = await Shell.Current.DisplayPromptAsync(
                    "Diagnóstico final corregido",
                    "Indique el diagnóstico final que quedará en el expediente.",
                    "Continuar",
                    "Cancelar",
                    humano.Diagnostico,
                    300,
                    Keyboard.Text);

                if (string.IsNullOrWhiteSpace(correccion))
                    return;

                diagnosticoFinal = correccion.Trim();
            }

            string? observaciones = await Shell.Current.DisplayPromptAsync(
                "Observaciones de aprobación",
                "Documente la decisión. Este campo puede quedar vacío.",
                "Continuar",
                "Cancelar",
                string.Empty,
                3000,
                Keyboard.Text);

            if (observaciones == null)
                return;

            bool decisionPositiva = decision is
                "APROBAR" or "APROBAR_CON_CORRECCION";

            if (decisionPositiva && !foto.TieneClasificacionAlbumCompleta)
            {
                var pagina = new JerarquiaAlbumFotografiaPage(
                    diagnosticoId,
                    foto,
                    "APROBADOR");

                await Shell.Current.Navigation.PushModalAsync(pagina);
                bool guardada = await pagina.ResultadoTask;
                if (!guardada)
                    return;

                await CargarJerarquiaAsync(diagnosticoId);
            }

            bool autorizaAlbum = decisionPositiva &&
                await Shell.Current.DisplayAlert(
                    "Autorizar uso en el Álbum Botánico",
                    "¿Autoriza que esta fotografía pueda copiarse posteriormente al Álbum Botánico?",
                    "Autorizar",
                    "No autorizar");

            var item = new InspeccionFotoAprobacionRequestV2
            {
                FotografiaId = foto.FotografiaId,
                Decision = decision,
                CalidadEvaluacionFinal = humano.CalidadEvaluacion,
                EstadoGeneralFinal = humano.EstadoGeneral,
                CategoriaPrincipalFinal = humano.CategoriaPrincipal,
                CategoriasSecundariasFinales = humano.CategoriasSecundarias,
                DiagnosticoFinal = diagnosticoFinal,
                TipoDiagnosticoFinal = humano.TipoDiagnostico,
                SeveridadFinal = humano.Severidad,
                NivelCertezaFinal = humano.NivelCerteza,
                Observaciones = observaciones.Trim(),
                AutorizaPublicacionAlbum = autorizaAlbum
            };

            await EjecutarOperacionAsync(
                "Registrando decisión del aprobador...",
                () => inspeccionApi.RegistrarAprobacionesAsync(
                    diagnosticoId,
                    [item]));
        }

        private async Task PublicarAlbumAsync()
        {
            if (!PuedePublicarSeleccion || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadas[0];
            JerarquiaDiagnosticoFotoResponse? jerarquia =
                foto.JerarquiaAlbum;

            if (jerarquia?.CategoriaAlbumBotanicoId is not > 0 ||
                jerarquia.AlbumBotanicoCafeId is not > 0)
            {
                await MostrarAlertaAsync(
                    "Clasificación pendiente",
                    "La fotografía debe tener una categoría y una subcategoría específica oficiales.");
                return;
            }

            bool confirmar = await Shell.Current.DisplayAlert(
                "Copiar al Álbum Botánico",
                $"La fotografía se copiará en {jerarquia.Categoria} → {jerarquia.Ficha}. El expediente original no será modificado.",
                "Copiar",
                "Cancelar");

            if (!confirmar)
                return;

            string? descripcion = await Shell.Current.DisplayPromptAsync(
                "Descripción para el álbum",
                "Descripción opcional de la fotografía.",
                "Publicar",
                "Cancelar",
                string.Empty,
                500,
                Keyboard.Text);

            if (descripcion == null)
                return;

            IsBusy = true;
            MensajeEstado = "Copiando fotografía autorizada al álbum...";
            ActualizarComandos();

            try
            {
                await inspeccionApi.PublicarAlbumAsync(
                    diagnosticoId,
                    foto.FotografiaId,
                    jerarquia.CategoriaAlbumBotanicoId.Value,
                    jerarquia.AlbumBotanicoCafeId.Value,
                    descripcion,
                    esPortada: false,
                    orden: 0);

                await ActualizarAsyncForzado();

                await MostrarAlertaAsync(
                    "Publicación completada",
                    "La fotografía fue copiada al Álbum Botánico sin alterar el expediente.");
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

        private async Task CerrarDefinitivamenteAsync()
        {
            if (!PuedeCerrarDefinitivamente || Shell.Current == null)
                return;

            bool confirmar = await Shell.Current.DisplayAlert(
                "Cerrar definitivamente",
                "El expediente quedará en modo de solo lectura. Las fotografías autorizadas podrán seguir copiándose al Álbum Botánico sin modificar la inspección. ¿Desea continuar?",
                "Cerrar expediente",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Cerrando definitivamente la inspección...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 actualizado =
                    await inspeccionApi.CerrarInspeccionAsync(diagnosticoId);

                await AplicarDetalleAsync(actualizado);

                await MostrarAlertaAsync(
                    "Expediente cerrado",
                    "La inspección quedó cerrada definitivamente y disponible solo para consulta.");
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
            Func<Task<InspeccionOperacionMasivaV2>> operacion)
        {
            if (IsBusy || !ValidarEnLinea())
                return;

            IsBusy = true;
            MensajeEstado = mensaje;
            ActualizarComandos();

            try
            {
                InspeccionOperacionMasivaV2 resultado = await operacion();
                if (resultado.TotalExitosas == 0)
                {
                    string detalle = resultado.Resultados
                        .FirstOrDefault()?.Mensaje ??
                        "La operación no pudo completarse.";
                    await MostrarAlertaAsync("Operación no completada", detalle);
                }

                await ActualizarAsyncForzado();
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

        private async Task ActualizarAsyncForzado()
        {
            InspeccionFitosanitariaDetalleV2 actualizado =
                await inspeccionApi.ObtenerDetalleAsync(diagnosticoId);

            if (RequiereNormalizarPlantasSanas(actualizado))
            {
                try
                {
                    await inspeccionApi.NormalizarPlantasSanasAsync(
                        diagnosticoId);
                    actualizado = await inspeccionApi.ObtenerDetalleAsync(
                        diagnosticoId);
                }
                catch (InspeccionFitosanitariaApiException)
                {
                    // Se conserva el resultado visible como respaldo.
                }
            }

            await AplicarDetalleAsync(actualizado);
        }

        private void SeleccionarTodo()
        {
            if (SoloConsultaAsignacion)
                return;

            foreach (InspeccionFotoV2 foto in Fotografias)
            {
                foto.Seleccionada = foto.PuedeSeleccionarse &&
                    foto.Estado is
                        InspeccionFotoEstados.Borrador or
                        InspeccionFotoEstados.PendienteIA or
                        InspeccionFotoEstados.ErrorIA or
                        InspeccionFotoEstados.PendienteDecisionTecnico or
                        InspeccionFotoEstados.DevueltaTecnico;
            }

            NotificarSeleccion();
        }

        private void QuitarSeleccion()
        {
            foreach (InspeccionFotoV2 foto in Fotografias)
                foto.Seleccionada = false;

            NotificarSeleccion();
        }

        private void OnFotoPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InspeccionFotoV2.Seleccionada))
                NotificarSeleccion();
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
            OnPropertyChanged(nameof(PuedeAprobarSeleccion));
            OnPropertyChanged(nameof(PuedePublicarSeleccion));
            OnPropertyChanged(nameof(PuedeCerrarDefinitivamente));
            ActualizarComandos();
        }

        private async Task RegresarResultadoAsync()
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("..");
        }

        private static async Task<string> CopiarTemporalAsync(
            FileResult archivo)
        {
            string extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string ruta = Path.Combine(
                FileSystem.CacheDirectory,
                $"fitosanitaria_{Guid.NewGuid():N}{extension}");

            await using Stream entrada = await archivo.OpenReadAsync();
            await using FileStream salida = File.Create(ruta);
            await entrada.CopyToAsync(salida);
            return ruta;
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            AgregarFotografiasCommand.ChangeCanExecute();
            SeleccionarTodoCommand.ChangeCanExecute();
            QuitarSeleccionCommand.ChangeCanExecute();
            ProcesarSeleccionCommand.ChangeCanExecute();
            EnviarAnalizadorCommand.ChangeCanExecute();
            SolicitarRevisionCommand.ChangeCanExecute();
            DescartarCommand.ChangeCanExecute();
            AprobarCommand.ChangeCanExecute();
            PublicarAlbumCommand.ChangeCanExecute();
            CerrarDefinitivoCommand.ChangeCanExecute();
            RegresarResultadoCommand.ChangeCanExecute();
        }
    }
}
