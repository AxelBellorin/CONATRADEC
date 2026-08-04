using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAnalizadorViewModel :
        DiagnosticoIAViewModelBase
    {
        private bool inicializado;
        private DiagnosticoIADetalle? seleccionado;
        private DiagnosticoIACatalogos catalogos = new();
        private DiagnosticoIAAlbumCatalogo catalogoAlbum = new();
        private string calidadEvaluacion = "PARCIALMENTE_EVALUABLE";
        private string estadoGeneral = "INDETERMINADA";
        private string categoriaPrincipal = "AFECTACION_NO_DETERMINADA";
        private string categoriasSecundariasTexto = string.Empty;
        private string diagnosticoPropuesto = string.Empty;
        private string tipoDiagnostico = string.Empty;
        private string severidadPropuesta = "NO_EVALUABLE";
        private string nivelCerteza = "NO_DETERMINADO";
        private string partesAfectadasTexto = string.Empty;
        private string evidenciasObservadasTexto = string.Empty;
        private string observaciones = string.Empty;
        private string retroalimentacionGemini = string.Empty;
        private string diagnosticoPropuestoGemini = string.Empty;

        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && CanView);

            SeleccionarCommand = new Command<DiagnosticoIAListaItem>(
                async item => await SeleccionarAsync(item),
                item => item != null && !IsBusy);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && CanEdit && PuedeEditarAnalisis);

            EnviarCommand = new Command(
                async () => await EnviarAsync(),
                () =>
                    !IsBusy &&
                    CanEdit &&
                    PuedeEnviarAprobacion);

            ClasificarImagenCommand =
                new Command<DiagnosticoIAImagenItem>(
                    async imagen =>
                        await ClasificarImagenAsync(imagen),
                    imagen =>
                        imagen?.ResultadoIA != null &&
                        !IsBusy &&
                        CanEdit &&
                        PuedeEditarAnalisis);

            ProponerClasificacionCommand =
                new Command<DiagnosticoIAImagenItem>(
                    async imagen =>
                        await ProponerClasificacionAsync(imagen),
                    imagen =>
                        imagen?.ResultadoIA != null &&
                        !IsBusy &&
                        CanEdit &&
                        PuedeEditarAnalisis);

            SegundaRevisionCommand = new Command(
                async () => await SegundaRevisionAsync(),
                () =>
                    !IsBusy &&
                    CanEdit &&
                    PuedeEditarAnalisis &&
                    RetroalimentacionGemini.Trim().Length >= 8 &&
                    PuedeSolicitarRevisionGemini);

            ReintentarIACommand = new Command(
                async () => await ReintentarIAAsync(),
                () => !IsBusy && CanEdit && TieneErrorIA);
        }

        public ObservableCollection<DiagnosticoIAListaItem>
            Pendientes { get; } = [];

        public Command ActualizarCommand { get; }
        public Command<DiagnosticoIAListaItem> SeleccionarCommand { get; }
        public Command GuardarCommand { get; }
        public Command EnviarCommand { get; }
        public Command SegundaRevisionCommand { get; }
        public Command ReintentarIACommand { get; }
        public Command<DiagnosticoIAImagenItem>
            ClasificarImagenCommand { get; }
        public Command<DiagnosticoIAImagenItem>
            ProponerClasificacionCommand { get; }

        public DiagnosticoIACatalogos Catalogos
        {
            get => catalogos;
            private set
            {
                catalogos = value ?? new DiagnosticoIACatalogos();
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalidadesEvaluacion));
                OnPropertyChanged(nameof(EstadosGenerales));
                OnPropertyChanged(nameof(Categorias));
                OnPropertyChanged(nameof(Severidades));
                OnPropertyChanged(nameof(NivelesCerteza));
            }
        }

        public IReadOnlyList<string> CalidadesEvaluacion =>
            Catalogos.CalidadEvaluacion;
        public IReadOnlyList<string> EstadosGenerales =>
            Catalogos.EstadosGenerales;
        public IReadOnlyList<string> Categorias =>
            Catalogos.Categorias;
        public IReadOnlyList<string> Severidades =>
            Catalogos.Severidades;
        public IReadOnlyList<string> NivelesCerteza =>
            Catalogos.NivelesCerteza;

        public DiagnosticoIADetalle? Seleccionado
        {
            get => seleccionado;
            private set
            {
                if (ReferenceEquals(seleccionado, value))
                    return;

                seleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneSeleccionado));
                OnPropertyChanged(nameof(TieneRevision));
                OnPropertyChanged(nameof(TieneErrorIA));
                OnPropertyChanged(nameof(PuedeEditarAnalisis));
                OnPropertyChanged(nameof(PuedeSolicitarRevisionGemini));
                OnPropertyChanged(nameof(ResumenLimiteRevisiones));
                OnPropertyChanged(nameof(TieneClasificacionesSinRevisar));
                OnPropertyChanged(nameof(PuedeEnviarAprobacion));
                CargarFormulario(value);
                ActualizarComandos();
            }
        }

        public bool TienePendientes => Pendientes.Count > 0;
        public bool SinPendientes => !TienePendientes;
        public bool TieneSeleccionado => Seleccionado != null;
        public bool TieneRevision => Seleccionado?.UltimaRevisionIA != null;

        public bool PuedeSolicitarRevisionGemini =>
            Seleccionado?.PuedeSolicitarRevisionGemini == true;

        public string ResumenLimiteRevisiones =>
            Seleccionado?.ResumenLimiteRevisiones ??
            "Seleccione un diagnóstico para consultar el límite de revisiones.";

        public bool TieneErrorIA =>
            Seleccionado?.Estado == DiagnosticoIAEstados.ErrorAnalisis;

        public bool PuedeEditarAnalisis =>
            Seleccionado?.Estado is
                DiagnosticoIAEstados.PendienteAnalizador or
                DiagnosticoIAEstados.EnAnalisisHumano or
                DiagnosticoIAEstados.DevueltoCorreccion;

        public bool TieneClasificacionesSinRevisar =>
            Seleccionado?.Imagenes.Any(item =>
                item.ResultadoIA?.ClasificacionAlbumPendiente == true) == true;

        public bool PuedeEnviarAprobacion =>
            Seleccionado?.AnalisisHumanoActual?.EstadoRegistro ==
                "BORRADOR" &&
            !TieneClasificacionesSinRevisar;

        public string CalidadEvaluacion
        {
            get => calidadEvaluacion;
            set => SetString(ref calidadEvaluacion, value);
        }

        public string EstadoGeneral
        {
            get => estadoGeneral;
            set => SetString(ref estadoGeneral, value);
        }

        public string CategoriaPrincipal
        {
            get => categoriaPrincipal;
            set => SetString(ref categoriaPrincipal, value);
        }

        public string CategoriasSecundariasTexto
        {
            get => categoriasSecundariasTexto;
            set => SetString(ref categoriasSecundariasTexto, value);
        }

        public string DiagnosticoPropuesto
        {
            get => diagnosticoPropuesto;
            set => SetString(ref diagnosticoPropuesto, value);
        }

        public string TipoDiagnostico
        {
            get => tipoDiagnostico;
            set => SetString(ref tipoDiagnostico, value);
        }

        public string SeveridadPropuesta
        {
            get => severidadPropuesta;
            set => SetString(ref severidadPropuesta, value);
        }

        public string NivelCerteza
        {
            get => nivelCerteza;
            set => SetString(ref nivelCerteza, value);
        }

        public string PartesAfectadasTexto
        {
            get => partesAfectadasTexto;
            set => SetString(ref partesAfectadasTexto, value);
        }

        public string EvidenciasObservadasTexto
        {
            get => evidenciasObservadasTexto;
            set => SetString(ref evidenciasObservadasTexto, value);
        }

        public string Observaciones
        {
            get => observaciones;
            set => SetString(ref observaciones, value);
        }

        public string RetroalimentacionGemini
        {
            get => retroalimentacionGemini;
            set
            {
                SetString(ref retroalimentacionGemini, value);
                SegundaRevisionCommand.ChangeCanExecute();
            }
        }

        public string DiagnosticoPropuestoGemini
        {
            get => diagnosticoPropuestoGemini;
            set => SetString(ref diagnosticoPropuestoGemini, value);
        }

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (inicializado)
            {
                if (CanView && ValidarEnLinea(false))
                    await ActualizarAsync();

                return;
            }

            inicializado = true;

            if (!CanView || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado = "Preparando bandeja del analizador...";
            ActualizarComandos();

            try
            {
                Catalogos = await Api.ObtenerCatalogosAsync();
                catalogoAlbum =
                    await Api.ObtenerCatalogoAlbumAsync(null);
                await ActualizarColaInternaAsync();
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

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazAnalizador);

            CanView = permiso?.leer == true;
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            CanDelete = permiso?.eliminar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            ActualizarComandos();
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || !CanView || !ValidarEnLinea())
                return;

            IsBusy = true;
            MensajeEstado = "Actualizando cola del analizador...";
            ActualizarComandos();

            try
            {
                if (Catalogos.Categorias.Count == 0)
                    Catalogos = await Api.ObtenerCatalogosAsync();

                if (catalogoAlbum.Categorias.Count == 0)
                {
                    catalogoAlbum =
                        await Api.ObtenerCatalogoAlbumAsync(null);
                }

                await ActualizarColaInternaAsync();

                if (Seleccionado != null)
                {
                    Seleccionado = await Api.ObtenerDetalleAsync(
                        Seleccionado.DiagnosticoIAId);
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
                ActualizarComandos();
            }
        }

        private async Task ActualizarColaInternaAsync()
        {
            List<DiagnosticoIAListaItem> items =
                await Api.ObtenerColaAnalizadorAsync();

            Pendientes.Clear();
            foreach (var item in items)
                Pendientes.Add(item);

            OnPropertyChanged(nameof(TienePendientes));
            OnPropertyChanged(nameof(SinPendientes));
        }

        private async Task SeleccionarAsync(
            DiagnosticoIAListaItem? item)
        {
            if (item == null || IsBusy)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando evidencia y resultados...";
            ActualizarComandos();

            try
            {
                Seleccionado = await Api.ObtenerDetalleAsync(
                    item.DiagnosticoIAId);
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

        private async Task GuardarAsync()
        {
            if (IsBusy || !CanEdit || !PuedeEditarAnalisis || Seleccionado == null)
                return;

            if (string.IsNullOrWhiteSpace(DiagnosticoPropuesto) &&
                EstadoGeneral == "CON_AFECTACION")
            {
                await MostrarAlertaAsync(
                    "Clasificación incompleta",
                    "Indique el diagnóstico propuesto o describa la afectación no determinada.");
                return;
            }

            IsBusy = true;
            MensajeEstado = "Guardando análisis humano...";
            ActualizarComandos();

            try
            {
                var request = new DiagnosticoIAAnalisisHumanoRequest
                {
                    CalidadEvaluacion = CalidadEvaluacion,
                    EstadoGeneral = EstadoGeneral,
                    CategoriaPrincipal = CategoriaPrincipal,
                    CategoriasSecundarias =
                        SepararLista(CategoriasSecundariasTexto),
                    DiagnosticoPropuesto = DiagnosticoPropuesto.Trim(),
                    TipoDiagnostico = TipoDiagnostico.Trim(),
                    SeveridadPropuesta = SeveridadPropuesta,
                    NivelCerteza = NivelCerteza,
                    PartesAfectadas = SepararLista(
                        PartesAfectadasTexto),
                    EvidenciasObservadas = SepararLista(
                        EvidenciasObservadasTexto),
                    Observaciones = Observaciones.Trim()
                };

                Seleccionado = await Api.GuardarAnalisisHumanoAsync(
                    Seleccionado.DiagnosticoIAId,
                    request);

                await ActualizarColaInternaAsync();
                await MostrarAlertaAsync(
                    "Análisis guardado",
                    "La clasificación quedó como borrador. Puede seguir editándola o enviarla al aprobador.");
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

        private async Task EnviarAsync()
        {
            if (IsBusy ||
                !CanEdit ||
                !PuedeEnviarAprobacion)
            {
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Enviar para aprobación",
                "La versión actual dejará de ser un borrador y pasará a la bandeja del aprobador.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Enviando análisis para aprobación...";
            ActualizarComandos();

            try
            {
                Seleccionado = await Api.EnviarAnalisisHumanoAsync(
                    Seleccionado.DiagnosticoIAId);

                await ActualizarColaInternaAsync();
                await MostrarAlertaAsync(
                    "Enviado",
                    "El diagnóstico quedó pendiente de aprobación.");
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

        private async Task ReintentarIAAsync()
        {
            if (IsBusy || !CanEdit || !TieneErrorIA || Seleccionado == null)
                return;

            bool confirmar = await ConfirmarAsync(
                "Reintentar análisis de Gemini",
                "Se volverán a evaluar las fotografías ya guardadas. No se duplicarán las imágenes ni el diagnóstico.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Gemini está analizando nuevamente la evidencia...";
            ActualizarComandos();

            int diagnosticoId = Seleccionado.DiagnosticoIAId;

            try
            {
                await Api.ReintentarIAAsync(diagnosticoId);
                Seleccionado = await Api.ObtenerDetalleAsync(diagnosticoId);
                await ActualizarColaInternaAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);

                if (!EsSesionInvalidada(ex))
                {
                    try
                    {
                        Seleccionado = await Api.ObtenerDetalleAsync(
                            diagnosticoId);
                        await ActualizarColaInternaAsync();
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task SegundaRevisionAsync()
        {
            if (IsBusy ||
                !CanEdit ||
                !PuedeEditarAnalisis ||
                Seleccionado == null ||
                !PuedeSolicitarRevisionGemini ||
                RetroalimentacionGemini.Trim().Length < 8)
            {
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Nueva revisión de Gemini",
                "Gemini volverá a examinar las fotografías comparando los resultados anteriores con su observación técnica. La decisión humana seguirá siendo la principal.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Gemini está realizando una revisión independiente...";
            ActualizarComandos();

            try
            {
                Seleccionado = await Api.SolicitarSegundaRevisionAsync(
                    Seleccionado.DiagnosticoIAId,
                    RetroalimentacionGemini.Trim(),
                    DiagnosticoPropuestoGemini.Trim());

                RetroalimentacionGemini = string.Empty;
                DiagnosticoPropuestoGemini = string.Empty;
                OnPropertyChanged(nameof(TieneRevision));
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

        private async Task ClasificarImagenAsync(
            DiagnosticoIAImagenItem? imagen)
        {
            if (imagen?.ResultadoIA == null ||
                Seleccionado == null ||
                IsBusy ||
                !CanEdit ||
                !PuedeEditarAnalisis)
            {
                return;
            }

            if (!await AsegurarCatalogoAlbumAsync())
                return;

            DiagnosticoIAAlbumCategoria? categoria =
                await SeleccionarCategoriaAsync();

            if (categoria == null)
                return;

            List<DiagnosticoIAAlbumRegistro> registros = catalogoAlbum
                .Registros
                .Where(item =>
                    item.CategoriaAlbumBotanicoId ==
                        categoria.CategoriaAlbumBotanicoId)
                .OrderBy(item => item.Titulo)
                .ToList();

            if (registros.Count == 0)
            {
                await MostrarAlertaAsync(
                    "Catálogo del álbum",
                    "La categoría seleccionada todavía no contiene fichas activas. Registre una propuesta para que el aprobador la revise.");
                return;
            }

            string cancelar = "Cancelar";
            string? seleccion = await Shell.Current!.DisplayActionSheet(
                "Seleccione la ficha oficial",
                cancelar,
                null,
                registros.Select(item => item.Titulo).ToArray());

            if (string.IsNullOrWhiteSpace(seleccion) ||
                seleccion == cancelar)
            {
                return;
            }

            DiagnosticoIAAlbumRegistro? registro = registros.FirstOrDefault(
                item => string.Equals(
                    item.Titulo,
                    seleccion,
                    StringComparison.Ordinal));

            if (registro == null)
                return;

            IsBusy = true;
            MensajeEstado = "Vinculando la fotografía con el Álbum Botánico...";
            ActualizarComandos();

            try
            {
                await Api.ResolverClasificacionExistenteAsync(
                    Seleccionado.DiagnosticoIAId,
                    imagen.DiagnosticoIAImagenId,
                    registro.AlbumBotanicoCafeId);

                Seleccionado = await Api.ObtenerDetalleAsync(
                    Seleccionado.DiagnosticoIAId);
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

        private async Task ProponerClasificacionAsync(
            DiagnosticoIAImagenItem? imagen)
        {
            if (imagen?.ResultadoIA == null ||
                Seleccionado == null ||
                IsBusy ||
                !CanEdit ||
                !PuedeEditarAnalisis)
            {
                return;
            }

            if (!await AsegurarCatalogoAlbumAsync())
                return;

            DiagnosticoIAAlbumCategoria? categoria =
                await SeleccionarCategoriaAsync();

            if (categoria == null)
                return;

            string sugerencia =
                imagen.ResultadoIA.ClasificacionAlbumSugerida;

            if (string.IsNullOrWhiteSpace(sugerencia))
                sugerencia = imagen.ResultadoIA.DiagnosticoProbable;

            string? titulo = await Shell.Current!.DisplayPromptAsync(
                "Proponer nueva ficha",
                "Indique el nombre que debería tener la ficha. El aprobador decidirá si se crea.",
                "Continuar",
                "Cancelar",
                "Nombre de la ficha",
                200,
                Keyboard.Text,
                sugerencia);

            if (string.IsNullOrWhiteSpace(titulo))
                return;

            string? nombreCientifico = await Shell.Current.DisplayPromptAsync(
                "Nombre científico",
                "Este dato es opcional y podrá ser corregido por el aprobador.",
                "Continuar",
                "Omitir",
                "Nombre científico",
                200,
                Keyboard.Text,
                imagen.ResultadoIA.NombreCientificoSugerido);

            string? motivo = await Shell.Current.DisplayPromptAsync(
                "Justificación técnica",
                "Explique por qué las fichas activas del álbum no representan esta evidencia.",
                "Guardar propuesta",
                "Cancelar",
                "Motivo obligatorio",
                1000,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(motivo) ||
                motivo.Trim().Length < 8)
            {
                await MostrarAlertaAsync(
                    "Justificación requerida",
                    "La propuesta necesita una explicación de al menos 8 caracteres.");
                return;
            }

            IsBusy = true;
            MensajeEstado = "Guardando propuesta para el aprobador...";
            ActualizarComandos();

            try
            {
                await Api.ProponerClasificacionAlbumAsync(
                    Seleccionado.DiagnosticoIAId,
                    imagen.DiagnosticoIAImagenId,
                    categoria.CategoriaAlbumBotanicoId,
                    titulo,
                    nombreCientifico,
                    motivo);

                Seleccionado = await Api.ObtenerDetalleAsync(
                    Seleccionado.DiagnosticoIAId);
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

        private async Task<bool> AsegurarCatalogoAlbumAsync()
        {
            if (catalogoAlbum.Categorias.Count > 0)
                return true;

            try
            {
                catalogoAlbum =
                    await Api.ObtenerCatalogoAlbumAsync(null);
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
                return false;
            }

            if (catalogoAlbum.Categorias.Count > 0)
                return true;

            await MostrarAlertaAsync(
                "Catálogo vacío",
                "No existen categorías activas en el Álbum Botánico.");
            return false;
        }

        private async Task<DiagnosticoIAAlbumCategoria?>
            SeleccionarCategoriaAsync()
        {
            string cancelar = "Cancelar";
            string? seleccion = await Shell.Current!.DisplayActionSheet(
                "Seleccione una categoría activa",
                cancelar,
                null,
                catalogoAlbum.Categorias
                    .OrderBy(item => item.NombreCategoria)
                    .Select(item => item.NombreCategoria)
                    .ToArray());

            if (string.IsNullOrWhiteSpace(seleccion) ||
                seleccion == cancelar)
            {
                return null;
            }

            return catalogoAlbum.Categorias.FirstOrDefault(item =>
                string.Equals(
                    item.NombreCategoria,
                    seleccion,
                    StringComparison.Ordinal));
        }

        private void CargarFormulario(DiagnosticoIADetalle? detalle)
        {
            if (detalle == null)
                return;

            DiagnosticoIAAnalisisHumanoItem? humano =
                detalle.AnalisisHumanoActual;

            DiagnosticoIARevisionItem? revision =
                detalle.UltimaRevisionIA?.Estado == "COMPLETADA"
                    ? detalle.UltimaRevisionIA
                    : null;

            CalidadEvaluacion = humano?.CalidadEvaluacion ??
                revision?.CalidadEvaluacion ??
                detalle.CalidadEvaluacionIA;
            EstadoGeneral = humano?.EstadoGeneral ??
                revision?.EstadoGeneral ??
                detalle.EstadoGeneralIA;
            CategoriaPrincipal = humano?.CategoriaPrincipal ??
                revision?.CategoriaPrincipal ??
                detalle.CategoriaPrincipalIA;
            CategoriasSecundariasTexto = UnirLista(
                humano?.CategoriasSecundarias ??
                revision?.CategoriasSecundarias ??
                detalle.CategoriasSecundariasIA);
            DiagnosticoPropuesto = humano?.DiagnosticoPropuesto ??
                revision?.DiagnosticoRevisado ??
                detalle.DiagnosticoSugerido;
            TipoDiagnostico = humano?.TipoDiagnostico ??
                revision?.TipoDiagnostico ??
                detalle.TipoDiagnosticoIA;
            SeveridadPropuesta = humano?.SeveridadPropuesta ??
                revision?.SeveridadVisual ??
                detalle.SeveridadVisualIA;
            NivelCerteza = humano?.NivelCerteza ??
                revision?.NivelCoincidencia ??
                detalle.NivelCoincidencia;
            PartesAfectadasTexto = UnirLista(
                humano?.PartesAfectadas ??
                revision?.PartesAfectadas ??
                detalle.PartesAfectadas);
            EvidenciasObservadasTexto = UnirLista(
                humano?.EvidenciasObservadas ??
                revision?.EvidenciasApoyo ??
                detalle.SintomasVisibles);
            Observaciones = humano?.Observaciones ?? string.Empty;
            DiagnosticoPropuestoGemini = DiagnosticoPropuesto;
        }

        private void SetString(ref string campo, string? valor)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged();
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            SeleccionarCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            EnviarCommand.ChangeCanExecute();
            SegundaRevisionCommand.ChangeCanExecute();
            ReintentarIACommand.ChangeCanExecute();
            ClasificarImagenCommand.ChangeCanExecute();
            ProponerClasificacionCommand.ChangeCanExecute();
        }
    }
}
