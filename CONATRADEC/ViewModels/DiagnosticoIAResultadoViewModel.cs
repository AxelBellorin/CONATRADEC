using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
        public int CantidadProcesablesIA => FotosSeleccionadasProcesablesIA.Count;
        public int CantidadListasParaEnviar => FotosSeleccionadasParaEnviar.Count;

        public string TextoBotonProcesarIA => CantidadProcesablesIA switch
        {
            <= 0 => "Procesar selección con IA",
            1 => "Procesar 1 con IA",
            _ => $"Procesar {CantidadProcesablesIA} con IA"
        };

        public string TextoBotonEnviarAnalizador => CantidadListasParaEnviar switch
        {
            <= 0 => "Enviar al analizador",
            1 => "Enviar 1 al analizador",
            _ => $"Enviar {CantidadListasParaEnviar} al analizador"
        };

        public string TextoSeleccion
        {
            get
            {
                int total = CantidadSeleccionada;
                if (total == 0)
                    return "Ninguna fotografía seleccionada";

                int requierenIa = CantidadProcesablesIA;
                int listasParaEnviar = CantidadListasParaEnviar;

                var partes = new List<string>
                {
                    total == 1
                        ? "1 fotografía seleccionada"
                        : $"{total} fotografías seleccionadas"
                };

                if (requierenIa > 0)
                {
                    partes.Add(
                        requierenIa == 1
                            ? "1 requiere IA"
                            : $"{requierenIa} requieren IA");
                }

                if (listasParaEnviar > 0)
                {
                    partes.Add(
                        listasParaEnviar == 1
                            ? "1 lista para enviar"
                            : $"{listasParaEnviar} listas para enviar");
                }

                return string.Join(" · ", partes);
            }
        }

        /// <summary>
        /// La selección visual es global, pero cada acción trabaja únicamente
        /// con las fotografías que cumplen sus propias reglas de estado. Una
        /// evidencia ya analizada nunca bloquea el análisis inicial de otra.
        /// </summary>
        public List<InspeccionFotoV2> FotosSeleccionadasProcesablesIA =>
            FotosSeleccionadas
                .Where(EsProcesablePorIAInicial)
                .ToList();

        public List<InspeccionFotoV2> FotosSeleccionadasParaEnviar =>
            FotosSeleccionadas
                .Where(item =>
                    item.Estado ==
                        InspeccionFotoEstados.PendienteDecisionTecnico)
                .ToList();

        public List<InspeccionFotoV2> FotosSeleccionadasReevaluables =>
            FotosSeleccionadas
                .Where(item =>
                    item.ResultadoIA != null &&
                    item.PuedeSolicitarRevisionIA &&
                    item.Estado ==
                        InspeccionFotoEstados.PendienteDecisionTecnico)
                .ToList();

        public bool PuedeProcesarSeleccion =>
            !SoloConsultaAsignacion &&
            FotosSeleccionadasProcesablesIA.Count > 0 &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta;

        public bool PuedeEnviarSeleccion =>
            !SoloConsultaAsignacion &&
            CantidadListasParaEnviar > 0 &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta;

        public bool PuedeSolicitarRevision =>
            !SoloConsultaAsignacion &&
            FotosSeleccionadasReevaluables.Count == 1 &&
            Detalle?.PuedeGestionarSolicitud == true &&
            EsEtapaTecnicaAbierta;

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

        private static bool EsProcesablePorIAInicial(InspeccionFotoV2 foto)
        {
            if (foto.Descartada)
                return false;

            /*
             * El estado del expediente individual es la autoridad del flujo.
             * ERROR_IA se mantiene procesable aunque exista un resultado
             * persistido, porque el backend puede recuperar de forma idempotente
             * un análisis que terminó bien pero cuyo estado final no se alcanzó
             * a registrar correctamente.
             */
            if (foto.Estado is
                InspeccionFotoEstados.Borrador or
                InspeccionFotoEstados.PendienteIA or
                InspeccionFotoEstados.ErrorIA)
            {
                return true;
            }

            return foto.ResultadoIA == null &&
                   foto.Estado == InspeccionFotoEstados.NoConcluyente;
        }

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
            MensajeEstado = "Actualizando estado de la inspección...";
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

            var temporales = new List<InspeccionFotoPreparacionLocal>();
            bool propiedadTransferida = false;

            try
            {
                int orden = 1;

                foreach (FileResult archivo in archivos)
                {
                    string ruta = await CopiarTemporalAsync(archivo);

                    temporales.Add(new InspeccionFotoPreparacionLocal
                    {
                        OrdenTemporal = orden++,
                        RutaLocal = ruta,
                        NombreArchivo = archivo.FileName,
                        TipoContenido = archivo.ContentType ?? "image/jpeg",
                        FechaIdentificacionCampo = DateTime.Today
                    });
                }

                await Shell.Current.GoToAsync(
                    DiagnosticoIARoutes.PaginaAgregarFotografias,
                    true,
                    new Dictionary<string, object>
                    {
                        ["inspeccionId"] = diagnosticoId,
                        ["fotografias"] = temporales
                    });

                /*
                 * La página de preparación pasa a ser propietaria de los
                 * archivos temporales y los elimina al guardar o cancelar.
                 */
                propiedadTransferida = true;
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                if (!propiedadTransferida)
                {
                    foreach (InspeccionFotoPreparacionLocal temporal in temporales)
                        temporal.EliminarArchivoTemporal();
                }
            }
        }

        private async Task ProcesarSeleccionAsync()
        {
            if (!PuedeProcesarSeleccion ||
                Shell.Current == null ||
                IsBusy ||
                !ValidarEnLinea())
            {
                return;
            }

            int[] fotografiaIds = FotosSeleccionadasProcesablesIA
                .Select(item => item.FotografiaId)
                .Distinct()
                .ToArray();

            if (fotografiaIds.Length == 0)
                return;

            /*
             * El análisis inicial se prepara en una página dedicada. Allí el
             * técnico ve cada imagen, escribe un contexto independiente y
             * confirma el lote antes de consumir el proveedor de IA.
             */
            await Shell.Current.GoToAsync(
                DiagnosticoIARoutes.PaginaPrepararAnalisisIA,
                true,
                new Dictionary<string, object>
                {
                    ["diagnosticoId"] = diagnosticoId,
                    ["fotografiaIds"] = fotografiaIds
                });
        }

        private async Task EnviarAnalizadorAsync()
        {
            List<InspeccionFotoV2> paraEnviar =
                FotosSeleccionadasParaEnviar;

            if (paraEnviar.Count == 0)
                return;

            string mensaje = paraEnviar.Count == CantidadSeleccionada
                ? $"Enviando {paraEnviar.Count} fotografía(s) al analizador..."
                : $"Enviando {paraEnviar.Count} de {CantidadSeleccionada} fotografía(s) seleccionadas que ya tienen decisión técnica pendiente...";

            await EjecutarOperacionAsync(
                mensaje,
                () => inspeccionApi.EnviarAnalizadorAsync(
                    diagnosticoId,
                    paraEnviar
                        .Select(item => item.FotografiaId)
                        .ToList()));
        }

        private async Task SolicitarRevisionAsync()
        {
            if (!PuedeSolicitarRevision || Shell.Current == null)
                return;

            InspeccionFotoV2 foto = FotosSeleccionadasReevaluables[0];
            if (!foto.PuedeSolicitarRevisionIA)
            {
                await MostrarAlertaAsync(
                    "Límite de reevaluaciones alcanzado",
                    foto.RevisionesIATexto +
                    ". Puede enviar la fotografía al analizador humano o continuar con otra decisión técnica.");
                return;
            }

            string? retroalimentacion =
                await TextoMultilineaDialogService.SolicitarAsync(
                    "Solicitar nueva evaluación IA",
                    "Explique qué debe revisar nuevamente la IA. Puede describir lesiones, zonas de la imagen, síntomas o cualquier detalle de campo que deba reconsiderarse.",
                    "Solicitar",
                    "Cancelar",
                    valorInicial: string.Empty,
                    maximoCaracteres: 1600,
                    minimoCaracteres: 8);

            if (retroalimentacion == null)
                return;

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

                /*
                 * Si el cliente perdió la respuesta o agotó el tiempo de espera,
                 * el backend pudo haber alcanzado a persistir ANALIZANDO_IA,
                 * ERROR_IA o incluso el resultado final. Se fuerza una lectura
                 * fresca para que la pantalla no quede mostrando PENDIENTE_IA.
                 */
                try
                {
                    await ActualizarAsyncForzado();
                }
                catch
                {
                    // Se conserva el error original como mensaje principal.
                }
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
            OnPropertyChanged(nameof(CantidadProcesablesIA));
            OnPropertyChanged(nameof(CantidadListasParaEnviar));
            OnPropertyChanged(nameof(TextoBotonProcesarIA));
            OnPropertyChanged(nameof(TextoBotonEnviarAnalizador));
            OnPropertyChanged(nameof(TextoSeleccion));
            OnPropertyChanged(nameof(FotosSeleccionadasProcesablesIA));
            OnPropertyChanged(nameof(FotosSeleccionadasParaEnviar));
            OnPropertyChanged(nameof(FotosSeleccionadasReevaluables));
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
