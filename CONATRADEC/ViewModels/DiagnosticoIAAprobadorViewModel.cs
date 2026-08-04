using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAprobadorViewModel :
        DiagnosticoIAViewModelBase
    {
        private bool inicializado;
        private bool puedeLeerAlbum;
        private bool puedeAgregarAlbum;
        private DiagnosticoIADetalle? seleccionado;
        private DiagnosticoIACatalogos catalogos = new();
        private DiagnosticoIAAlbumCatalogo catalogoAlbum = new();
        private string decision = "APROBAR_SIN_CAMBIOS";
        private string calidadFinal = "PARCIALMENTE_EVALUABLE";
        private string estadoGeneralFinal = "INDETERMINADA";
        private string categoriaFinal = "AFECTACION_NO_DETERMINADA";
        private string categoriasSecundariasTexto = string.Empty;
        private string diagnosticoFinal = string.Empty;
        private string tipoDiagnosticoFinal = string.Empty;
        private string severidadFinal = "NO_EVALUABLE";
        private string certezaFinal = "NO_DETERMINADO";
        private string observaciones = string.Empty;
        private bool autorizaAlbum;
        private DiagnosticoIAAlbumCategoria? categoriaAlbumSeleccionada;
        private DiagnosticoIAAlbumRegistro? registroAlbumSeleccionado;

        public DiagnosticoIAAprobadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && CanView);

            SeleccionarCommand = new Command<DiagnosticoIAListaItem>(
                async item => await SeleccionarAsync(item),
                item => item != null && !IsBusy);

            ResolverCommand = new Command(
                async () => await ResolverAsync(),
                () => !IsBusy && CanEdit && PuedeResolverCaso);

            CargarAlbumCommand = new Command(
                async () => await CargarCatalogoAlbumAsync(),
                () =>
                    !IsBusy &&
                    puedeAgregarAlbum &&
                    Seleccionado?.PuedePublicarAlbum == true);

            SeleccionarFichaCommand =
                new Command<DiagnosticoIAImagenItem>(
                    async imagen =>
                        await SeleccionarFichaAsync(imagen),
                    imagen =>
                        imagen?.ResultadoIA != null &&
                        !IsBusy &&
                        CanEdit &&
                        PuedeResolverCaso &&
                        puedeLeerAlbum);

            CrearFichaPropuestaCommand =
                new Command<DiagnosticoIAImagenItem>(
                    async imagen =>
                        await CrearFichaPropuestaAsync(imagen),
                    imagen =>
                        imagen?.ResultadoIA?.ClasificacionAlbumPropuesta == true &&
                        !IsBusy &&
                        CanEdit &&
                        PuedeResolverCaso &&
                        puedeAgregarAlbum);

            PublicarAlbumCommand = new Command(
                async () => await PublicarAlbumAsync(),
                () =>
                    !IsBusy &&
                    puedeAgregarAlbum &&
                    Seleccionado?.PuedePublicarAlbum == true &&
                    CategoriaAlbumSeleccionada != null &&
                    RegistroAlbumSeleccionado != null &&
                    Seleccionado.Imagenes.Any(item =>
                        item.SeleccionadaParaAlbum &&
                        item.AptaParaAlbum &&
                        !item.Publicada));
        }

        public ObservableCollection<DiagnosticoIAListaItem>
            Pendientes { get; } = [];

        public ObservableCollection<DiagnosticoIAAlbumCategoria>
            CategoriasAlbum { get; } = [];

        public ObservableCollection<DiagnosticoIAAlbumRegistro>
            RegistrosAlbum { get; } = [];

        public Command ActualizarCommand { get; }
        public Command<DiagnosticoIAListaItem> SeleccionarCommand { get; }
        public Command ResolverCommand { get; }
        public Command CargarAlbumCommand { get; }
        public Command PublicarAlbumCommand { get; }
        public Command<DiagnosticoIAImagenItem>
            SeleccionarFichaCommand { get; }
        public Command<DiagnosticoIAImagenItem>
            CrearFichaPropuestaCommand { get; }

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
                OnPropertyChanged(nameof(PuedeResolverCaso));
                OnPropertyChanged(nameof(PuedeMostrarAlbum));
                OnPropertyChanged(nameof(AdvertenciaMismoUsuario));
                OnPropertyChanged(nameof(TieneAdvertenciaMismoUsuario));
                OnPropertyChanged(nameof(TieneClasificacionesPendientesAprobador));
                CargarFormulario(value);
                ActualizarComandos();
            }
        }

        public DiagnosticoIACatalogos Catalogos
        {
            get => catalogos;
            private set
            {
                catalogos = value ?? new DiagnosticoIACatalogos();
                OnPropertyChanged();
                OnPropertyChanged(nameof(Decisiones));
                OnPropertyChanged(nameof(CalidadesEvaluacion));
                OnPropertyChanged(nameof(EstadosGenerales));
                OnPropertyChanged(nameof(Categorias));
                OnPropertyChanged(nameof(Severidades));
                OnPropertyChanged(nameof(NivelesCerteza));
                OnPropertyChanged(nameof(CalidadesImagen));
            }
        }

        public IReadOnlyList<string> Decisiones =>
            Catalogos.DecisionesAprobacion;
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
        public IReadOnlyList<string> CalidadesImagen =>
            Catalogos.CalidadesImagen;

        public bool TienePendientes => Pendientes.Count > 0;
        public bool SinPendientes => !TienePendientes;
        public bool TieneSeleccionado => Seleccionado != null;

        public bool PuedeResolverCaso =>
            Seleccionado?.Estado ==
                DiagnosticoIAEstados.PendienteAprobacion;

        public bool PuedeMostrarAlbum =>
            puedeAgregarAlbum && Seleccionado?.PuedePublicarAlbum == true;

        public bool TieneClasificacionesPendientesAprobador =>
            Seleccionado?.Imagenes.Any(item =>
                item.ResultadoIA?.ClasificacionAlbumPendiente == true ||
                item.ResultadoIA?.ClasificacionAlbumPropuesta == true) == true;

        public string AdvertenciaMismoUsuario
        {
            get
            {
                if (Seleccionado?.AnalisisHumanoActual == null)
                    return string.Empty;

                string actual = Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty);

                return int.TryParse(actual, out int usuarioId) &&
                       usuarioId ==
                           Seleccionado.AnalisisHumanoActual.UsuarioAnalizadorId
                    ? "Usted realizó el análisis. Sus permisos también le permiten aprobarlo; ambas acciones quedarán registradas por separado."
                    : string.Empty;
            }
        }

        public bool TieneAdvertenciaMismoUsuario =>
            !string.IsNullOrWhiteSpace(AdvertenciaMismoUsuario);

        public string Decision
        {
            get => decision;
            set
            {
                SetString(ref decision, value);
                OnPropertyChanged(nameof(EsAprobacion));
                OnPropertyChanged(nameof(EsCorreccion));
                OnPropertyChanged(nameof(PuedeAutorizarAlbum));

                if (!EsAprobacion)
                    AutorizaAlbum = false;
            }
        }

        public bool EsAprobacion =>
            Decision is "APROBAR_SIN_CAMBIOS" or
                "APROBAR_CON_CORRECCION";

        public bool EsCorreccion =>
            Decision == "APROBAR_CON_CORRECCION";

        public bool PuedeAutorizarAlbum => EsAprobacion;

        public string CalidadFinal
        {
            get => calidadFinal;
            set => SetString(ref calidadFinal, value);
        }

        public string EstadoGeneralFinal
        {
            get => estadoGeneralFinal;
            set => SetString(ref estadoGeneralFinal, value);
        }

        public string CategoriaFinal
        {
            get => categoriaFinal;
            set => SetString(ref categoriaFinal, value);
        }

        public string CategoriasSecundariasTexto
        {
            get => categoriasSecundariasTexto;
            set => SetString(ref categoriasSecundariasTexto, value);
        }

        public string DiagnosticoFinal
        {
            get => diagnosticoFinal;
            set => SetString(ref diagnosticoFinal, value);
        }

        public string TipoDiagnosticoFinal
        {
            get => tipoDiagnosticoFinal;
            set => SetString(ref tipoDiagnosticoFinal, value);
        }

        public string SeveridadFinal
        {
            get => severidadFinal;
            set => SetString(ref severidadFinal, value);
        }

        public string CertezaFinal
        {
            get => certezaFinal;
            set => SetString(ref certezaFinal, value);
        }

        public string Observaciones
        {
            get => observaciones;
            set => SetString(ref observaciones, value);
        }

        public bool AutorizaAlbum
        {
            get => autorizaAlbum;
            set
            {
                if (autorizaAlbum == value)
                    return;

                autorizaAlbum = value && EsAprobacion;
                OnPropertyChanged();
            }
        }

        public DiagnosticoIAAlbumCategoria? CategoriaAlbumSeleccionada
        {
            get => categoriaAlbumSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaAlbumSeleccionada, value))
                    return;

                categoriaAlbumSeleccionada = value;
                OnPropertyChanged();
                FiltrarRegistrosAlbum();
                ActualizarComandos();
            }
        }

        public DiagnosticoIAAlbumRegistro? RegistroAlbumSeleccionado
        {
            get => registroAlbumSeleccionado;
            set
            {
                if (ReferenceEquals(registroAlbumSeleccionado, value))
                    return;

                registroAlbumSeleccionado = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
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
            MensajeEstado = "Preparando bandeja del aprobador...";
            ActualizarComandos();

            try
            {
                Catalogos = await Api.ObtenerCatalogosAsync();

                if (puedeLeerAlbum)
                {
                    catalogoAlbum =
                        await Api.ObtenerCatalogoAlbumAsync(null);
                }

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
                DiagnosticoIARoutes.InterfazAprobador);
            var permisoAlbum = PermissionService.Instance.Get(
                "albumFotosPage");

            CanView = permiso?.leer == true;
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            CanDelete = permiso?.eliminar == true;
            // Consultar el catálogo activo forma parte del permiso del
            // aprobador. Crear o publicar fichas sí requiere permiso de
            // agregado en el Álbum Botánico.
            puedeLeerAlbum = CanView;
            puedeAgregarAlbum = permisoAlbum?.agregar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(PuedeMostrarAlbum));
            ActualizarComandos();
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || !CanView || !ValidarEnLinea())
                return;

            IsBusy = true;
            MensajeEstado = "Actualizando cola del aprobador...";
            ActualizarComandos();

            try
            {
                if (Catalogos.DecisionesAprobacion.Count == 0)
                    Catalogos = await Api.ObtenerCatalogosAsync();

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

        private async Task ActualizarColaInternaAsync()
        {
            List<DiagnosticoIAListaItem> items =
                await Api.ObtenerColaAprobadorAsync();

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
            MensajeEstado = "Cargando análisis y evidencia...";
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

        private async Task ResolverAsync()
        {
            if (IsBusy || !CanEdit || !PuedeResolverCaso || Seleccionado == null)
                return;

            if (EsAprobacion && TieneClasificacionesPendientesAprobador)
            {
                await MostrarAlertaAsync(
                    "Clasificaciones pendientes",
                    "Antes de aprobar debe resolver cada propuesta del analizador. Puede usar una ficha existente o autorizar la creación de una nueva.");
                return;
            }

            if (Decision == "APROBAR_CON_CORRECCION" &&
                string.IsNullOrWhiteSpace(DiagnosticoFinal))
            {
                await MostrarAlertaAsync(
                    "Clasificación final",
                    "Complete el diagnóstico final antes de aprobar con corrección.");
                return;
            }

            if (Decision == "DEVOLVER_AL_ANALIZADOR" &&
                string.IsNullOrWhiteSpace(Observaciones))
            {
                await MostrarAlertaAsync(
                    "Motivo requerido",
                    "Explique qué debe revisar o corregir el analizador.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Registrar decisión",
                $"Se registrará la decisión {Decision.Replace('_', ' ')}. La acción quedará en el historial.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Registrando decisión del aprobador...";
            ActualizarComandos();

            try
            {
                var request = new DiagnosticoIAAprobacionRequest
                {
                    Decision = Decision,
                    CalidadEvaluacionFinal = CalidadFinal,
                    EstadoGeneralFinal = EstadoGeneralFinal,
                    CategoriaPrincipalFinal = CategoriaFinal,
                    CategoriasSecundariasFinales =
                        SepararLista(CategoriasSecundariasTexto),
                    DiagnosticoFinal = DiagnosticoFinal.Trim(),
                    TipoDiagnosticoFinal =
                        TipoDiagnosticoFinal.Trim(),
                    SeveridadFinal = SeveridadFinal,
                    NivelCertezaFinal = CertezaFinal,
                    Observaciones = Observaciones.Trim(),
                    AutorizaPublicacionAlbum = AutorizaAlbum,
                    EvaluacionesImagen = Seleccionado.Imagenes
                        .Select(item =>
                            new DiagnosticoIAImagenEvaluacionRequest
                            {
                                DiagnosticoIAImagenId =
                                    item.DiagnosticoIAImagenId,
                                CalidadTecnica = item.CalidadTecnica,
                                EsEvidenciaValida =
                                    item.EsEvidenciaValida,
                                AptaParaAlbum =
                                    AutorizaAlbum &&
                                    item.EsEvidenciaValida &&
                                    item.AptaParaAlbum,
                                Observacion =
                                    item.ObservacionAprobador.Trim()
                            })
                        .ToList()
                };

                Seleccionado = await Api.RegistrarAprobacionAsync(
                    Seleccionado.DiagnosticoIAId,
                    request);

                await ActualizarColaInternaAsync();

                await MostrarAlertaAsync(
                    "Decisión registrada",
                    DiagnosticoIAEstados.Mostrar(Seleccionado.Estado));
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

        private async Task SeleccionarFichaAsync(
            DiagnosticoIAImagenItem? imagen)
        {
            if (imagen?.ResultadoIA == null ||
                Seleccionado == null ||
                IsBusy ||
                !CanEdit ||
                !PuedeResolverCaso ||
                !puedeLeerAlbum)
            {
                return;
            }

            if (!await AsegurarCatalogoClasificacionAsync())
                return;

            DiagnosticoIAAlbumCategoria? categoria =
                await SeleccionarCategoriaClasificacionAsync();

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
                    "La categoría seleccionada no contiene fichas activas.");
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
            MensajeEstado = "Aplicando clasificación oficial...";
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

        private async Task CrearFichaPropuestaAsync(
            DiagnosticoIAImagenItem? imagen)
        {
            DiagnosticoIAImagenResultadoItem? resultado =
                imagen?.ResultadoIA;

            if (resultado == null ||
                !resultado.ClasificacionAlbumPropuesta ||
                Seleccionado == null ||
                IsBusy ||
                !CanEdit ||
                !PuedeResolverCaso ||
                !puedeAgregarAlbum)
            {
                return;
            }

            if (!await AsegurarCatalogoClasificacionAsync())
                return;

            DiagnosticoIAAlbumCategoria? categoria = catalogoAlbum
                .Categorias
                .FirstOrDefault(item =>
                    item.CategoriaAlbumBotanicoId ==
                        resultado.CategoriaAlbumBotanicoIdSugerida);

            categoria ??= await SeleccionarCategoriaClasificacionAsync();

            if (categoria == null)
                return;

            string? titulo = await Shell.Current!.DisplayPromptAsync(
                "Autorizar nueva ficha",
                "Revise el título propuesto por el analizador.",
                "Continuar",
                "Cancelar",
                "Título",
                200,
                Keyboard.Text,
                resultado.ClasificacionAlbumSugerida);

            if (string.IsNullOrWhiteSpace(titulo))
                return;

            string? nombreCientifico = await Shell.Current.DisplayPromptAsync(
                "Nombre científico",
                "Dato opcional.",
                "Continuar",
                "Omitir",
                "Nombre científico",
                200,
                Keyboard.Text,
                resultado.NombreCientificoSugerido);

            string? descripcion = await Shell.Current.DisplayPromptAsync(
                "Descripción de la ficha",
                "Registre una descripción inicial para el Álbum Botánico.",
                "Crear ficha",
                "Cancelar",
                "Descripción obligatoria",
                1000,
                Keyboard.Text,
                resultado.ResumenImagen);

            if (string.IsNullOrWhiteSpace(descripcion) ||
                descripcion.Trim().Length < 8)
            {
                await MostrarAlertaAsync(
                    "Descripción requerida",
                    "La ficha necesita una descripción de al menos 8 caracteres.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Crear ficha oficial",
                $"Se creará {categoria.NombreCategoria} → {titulo.Trim()} en el Álbum Botánico y quedará vinculada con la fotografía.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Creando ficha autorizada...";
            ActualizarComandos();

            try
            {
                await Api.CrearClasificacionAlbumAsync(
                    Seleccionado.DiagnosticoIAId,
                    imagen!.DiagnosticoIAImagenId,
                    categoria.CategoriaAlbumBotanicoId,
                    titulo,
                    nombreCientifico,
                    descripcion,
                    string.Join(", ", resultado.SintomasVisibles));

                catalogoAlbum =
                    await Api.ObtenerCatalogoAlbumAsync(null);

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

        private async Task<bool> AsegurarCatalogoClasificacionAsync()
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

            return catalogoAlbum.Categorias.Count > 0;
        }

        private async Task<DiagnosticoIAAlbumCategoria?>
            SeleccionarCategoriaClasificacionAsync()
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

        private async Task CargarCatalogoAlbumAsync()
        {
            if (IsBusy ||
                !puedeAgregarAlbum ||
                Seleccionado?.PuedePublicarAlbum != true)
            {
                return;
            }

            IsBusy = true;
            MensajeEstado = "Cargando categorías y registros del álbum...";
            ActualizarComandos();

            try
            {
                catalogoAlbum = await Api.ObtenerCatalogoAlbumAsync(null);

                CategoriasAlbum.Clear();
                foreach (var item in catalogoAlbum.Categorias)
                    CategoriasAlbum.Add(item);

                CategoriaAlbumSeleccionada =
                    SugerirCategoriaAlbum();
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

        private DiagnosticoIAAlbumCategoria? SugerirCategoriaAlbum()
        {
            if (Seleccionado?.UltimaAprobacion == null)
                return CategoriasAlbum.FirstOrDefault();

            string categoriaFinal =
                Seleccionado.UltimaAprobacion.CategoriaPrincipalFinal;

            string palabra = categoriaFinal switch
            {
                "ENFERMEDAD" => "enfermed",
                "PLAGA" => "plaga",
                "ALTERACION_NUTRICIONAL" => "nutri",
                "ESTRES_ABIOTICO" => "estrés",
                "DANO_MECANICO" => "daño",
                "NO_APLICA" => "sana",
                _ => string.Empty
            };

            return CategoriasAlbum.FirstOrDefault(item =>
                       !string.IsNullOrWhiteSpace(palabra) &&
                       item.NombreCategoria.Contains(
                           palabra,
                           StringComparison.OrdinalIgnoreCase)) ??
                   CategoriasAlbum.FirstOrDefault();
        }

        private void FiltrarRegistrosAlbum()
        {
            RegistrosAlbum.Clear();
            RegistroAlbumSeleccionado = null;

            if (CategoriaAlbumSeleccionada == null)
                return;

            foreach (var item in catalogoAlbum.Registros
                         .Where(item =>
                             item.CategoriaAlbumBotanicoId ==
                             CategoriaAlbumSeleccionada.CategoriaAlbumBotanicoId)
                         .OrderBy(item => item.Titulo))
            {
                RegistrosAlbum.Add(item);
            }

            string diagnostico =
                Seleccionado?.UltimaAprobacion?.DiagnosticoFinal ??
                string.Empty;

            RegistroAlbumSeleccionado = RegistrosAlbum.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(diagnostico) &&
                (item.Titulo.Contains(
                     diagnostico,
                     StringComparison.OrdinalIgnoreCase) ||
                 diagnostico.Contains(
                     item.Titulo,
                     StringComparison.OrdinalIgnoreCase))) ??
                RegistrosAlbum.FirstOrDefault();
        }

        private async Task PublicarAlbumAsync()
        {
            if (IsBusy ||
                Seleccionado?.PuedePublicarAlbum != true ||
                CategoriaAlbumSeleccionada == null ||
                RegistroAlbumSeleccionado == null)
            {
                return;
            }

            List<DiagnosticoIAImagenItem> seleccionadas =
                Seleccionado.Imagenes
                    .Where(item =>
                        item.SeleccionadaParaAlbum &&
                        item.AptaParaAlbum &&
                        !item.Publicada)
                    .ToList();

            if (seleccionadas.Count == 0)
            {
                await MostrarAlertaAsync(
                    "Fotografías",
                    "Seleccione al menos una fotografía autorizada para publicar.");
                return;
            }

            if (seleccionadas.Count(item => item.EsPortada) > 1)
            {
                await MostrarAlertaAsync(
                    "Portada",
                    "Solo una fotografía puede seleccionarse como portada.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Publicar en el álbum botánico",
                $"Se copiarán {seleccionadas.Count} fotografías en {CategoriaAlbumSeleccionada.NombreCategoria} → {RegistroAlbumSeleccionado.Titulo}. El diagnóstico conservará sus originales.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Copiando fotografías aprobadas al álbum...";
            ActualizarComandos();

            try
            {
                var request = new DiagnosticoIAPublicarAlbumRequest
                {
                    CategoriaAlbumBotanicoId =
                        CategoriaAlbumSeleccionada.CategoriaAlbumBotanicoId,
                    AlbumBotanicoCafeId =
                        RegistroAlbumSeleccionado.AlbumBotanicoCafeId,
                    Imagenes = seleccionadas
                        .Select(item =>
                            new DiagnosticoIAPublicarAlbumImagenRequest
                            {
                                DiagnosticoIAImagenId =
                                    item.DiagnosticoIAImagenId,
                                Descripcion = item.DescripcionAlbum,
                                EsPortada = item.EsPortada,
                                Orden = item.OrdenAlbum
                            })
                        .ToList()
                };

                DiagnosticoIAPublicacionResultado resultado =
                    await Api.PublicarAlbumAsync(
                        Seleccionado.DiagnosticoIAId,
                        request);

                Seleccionado = await Api.ObtenerDetalleAsync(
                    Seleccionado.DiagnosticoIAId);

                await MostrarAlertaAsync(
                    "Álbum actualizado",
                    $"Se publicaron {resultado.TotalPublicadas} fotografías avaladas.");
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

        private void CargarFormulario(DiagnosticoIADetalle? detalle)
        {
            if (detalle?.AnalisisHumanoActual == null)
                return;

            DiagnosticoIAAnalisisHumanoItem humano =
                detalle.AnalisisHumanoActual;

            Decision = "APROBAR_SIN_CAMBIOS";
            CalidadFinal = humano.CalidadEvaluacion;
            EstadoGeneralFinal = humano.EstadoGeneral;
            CategoriaFinal = humano.CategoriaPrincipal;
            CategoriasSecundariasTexto = UnirLista(
                humano.CategoriasSecundarias);
            DiagnosticoFinal = humano.DiagnosticoPropuesto;
            TipoDiagnosticoFinal = humano.TipoDiagnostico;
            SeveridadFinal = humano.SeveridadPropuesta;
            CertezaFinal = humano.NivelCerteza;
            Observaciones = string.Empty;
            AutorizaAlbum = false;

            foreach (DiagnosticoIAImagenItem imagen in detalle.Imagenes)
            {
                imagen.AplicarEvaluacionExistente();

                if (imagen.UltimaEvaluacion == null)
                {
                    imagen.CalidadTecnica = "MEDIA";
                    imagen.EsEvidenciaValida = true;
                    imagen.AptaParaAlbum = false;
                    imagen.OrdenAlbum = imagen.Orden;
                }

                imagen.PropertyChanged += (_, _) =>
                    ActualizarComandos();
            }

            if (detalle.PuedePublicarAlbum)
            {
                foreach (var imagen in detalle.Imagenes.Where(item =>
                             item.AptaParaAlbum && !item.Publicada))
                {
                    imagen.SeleccionadaParaAlbum = true;
                }
            }

            CategoriasAlbum.Clear();
            RegistrosAlbum.Clear();
            CategoriaAlbumSeleccionada = null;
            RegistroAlbumSeleccionado = null;
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
            ResolverCommand.ChangeCanExecute();
            CargarAlbumCommand.ChangeCanExecute();
            PublicarAlbumCommand.ChangeCanExecute();
            SeleccionarFichaCommand.ChangeCanExecute();
            CrearFichaPropuestaCommand.ChangeCanExecute();
        }
    }
}
