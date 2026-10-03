using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Administra las clasificaciones del Álbum Botánico por diagnóstico
    /// individual dentro de una sola fotografía. La fotografía mantiene una
    /// identidad única; categoría, subcategoría y publicación pertenecen a cada
    /// afectación confirmada.
    /// </summary>
    public partial class JerarquiaAlbumFotografiaPage : ContentPage
    {
        private readonly int diagnosticoId;
        private readonly string etapa;
        private readonly AlbumBotanicoApiService albumApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly InspeccionAlbumDiagnosticoApiService diagnosticoApi =
            new();
        private readonly TaskCompletionSource<bool> resultadoTcs = new();
        private readonly List<AlbumRegistroJerarquiaResponse>
            subcategoriasCatalogo = [];

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private AlbumRegistroJerarquiaResponse? subcategoriaSeleccionada;
        private ClasificacionDiagnosticoAlbumItem? diagnosticoSeleccionado;
        private bool proponerSubcategoria;
        private bool isBusy;
        private bool inicializada;
        private bool cambiosRealizados;
        private bool cargandoFormulario;
        private int versionCarga;

        private string buscarSubcategoria = string.Empty;
        private string subcategoriaPropuesta = string.Empty;
        private string nombreCientifico = string.Empty;
        private string descripcion = string.Empty;
        private string sintomas = string.Empty;
        private string motivo = string.Empty;

        public JerarquiaAlbumFotografiaPage(
            int diagnosticoId,
            InspeccionFotoV2 fotografia,
            string etapa)
        {
            InitializeComponent();

            this.diagnosticoId = diagnosticoId;
            Fotografia = fotografia ??
                throw new ArgumentNullException(nameof(fotografia));
            this.etapa = NormalizarEtapa(etapa);
            JerarquiaActual = fotografia.JerarquiaAlbum;

            GuardarCommand = new Command(
                async () => await GuardarClasificacionAsync(),
                () => !IsBusy && PuedeEditarClasificacion);
            CancelarCommand = new Command(
                async () => await CerrarAsync(false),
                () => !IsBusy);
            AlternarPropuestaCommand = new Command(
                AlternarPropuesta,
                () => !IsBusy && PuedeEditarClasificacion);
            LimpiarBusquedaCommand = new Command(
                () => BuscarSubcategoria = string.Empty,
                () => !IsBusy);
            PublicarTodosCommand = new Command(
                SeleccionarTodasPublicaciones,
                () => !IsBusy && MostrarPanelPublicaciones);
            NingunaPublicacionCommand = new Command(
                QuitarTodasPublicaciones,
                () => !IsBusy && MostrarPanelPublicaciones);
            GuardarPublicacionesCommand = new Command(
                async () => await GuardarPublicacionesAsync(),
                () => !IsBusy && MostrarPanelPublicaciones);

            BindingContext = this;
        }

        public InspeccionFotoV2 Fotografia { get; }

        /// <summary>
        /// Referencia histórica de clasificación principal. Se conserva para
        /// compatibilidad con pantallas/versiones anteriores.
        /// </summary>
        public JerarquiaDiagnosticoFotoResponse? JerarquiaActual { get; private set; }

        public ObservableCollection<CategoriaAlbumBotanicoResponse>
            Categorias { get; } = [];

        public ObservableCollection<AlbumRegistroJerarquiaResponse>
            SubcategoriasEspecificas { get; } = [];

        public ObservableCollection<ClasificacionDiagnosticoAlbumItem>
            Diagnosticos { get; } = [];

        public ObservableCollection<ClasificacionDiagnosticoAlbumItem>
            DiagnosticosPublicables { get; } = [];

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Command AlternarPropuestaCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Command PublicarTodosCommand { get; }
        public Command NingunaPublicacionCommand { get; }
        public Command GuardarPublicacionesCommand { get; }

        public Task<bool> ResultadoTask => resultadoTcs.Task;

        public bool EsAprobador => etapa == "APROBADOR";

        public string EtapaTexto => EsAprobador
            ? "Aprobador · confirme cada diagnóstico y decida cuáles se publican"
            : "Analizador · confirme cada diagnóstico o proponga la subcategoría que falte";

        public string AyudaEtapa => EsAprobador
            ? "Cada diagnóstico se resuelve por separado. Al publicar, el Álbum reutiliza la fotografía original y muestra automáticamente las regiones de señalización asociadas a esa afectación."
            : "La categoría debe existir en el Álbum. Si falta la subcategoría específica, puede dejar una propuesta para que el aprobador la confirme sin afectar los demás diagnósticos.";

        public string ResumenDiagnosticos => Diagnosticos.Count switch
        {
            0 => "Sin diagnósticos clasificables.",
            1 => "1 diagnóstico registrado para esta fotografía.",
            _ => $"{Diagnosticos.Count} diagnósticos independientes registrados para esta fotografía."
        };

        public new bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (isBusy == value)
                    return;

                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsBusy));
                ActualizarComandos();
            }
        }

        public bool NotIsBusy => !IsBusy;

        public ClasificacionDiagnosticoAlbumItem? DiagnosticoSeleccionado
        {
            get => diagnosticoSeleccionado;
            private set
            {
                if (ReferenceEquals(diagnosticoSeleccionado, value))
                    return;

                if (diagnosticoSeleccionado != null)
                    diagnosticoSeleccionado.IsSelected = false;

                diagnosticoSeleccionado = value;

                if (diagnosticoSeleccionado != null)
                    diagnosticoSeleccionado.IsSelected = true;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneDiagnosticoSeleccionado));
                OnPropertyChanged(nameof(ClasificacionCerrada));
                OnPropertyChanged(nameof(PuedeEditarClasificacion));
                OnPropertyChanged(nameof(TextoGuardar));
                ActualizarComandos();
            }
        }

        public bool TieneDiagnosticoSeleccionado =>
            DiagnosticoSeleccionado != null;

        public bool ClasificacionCerrada =>
            DiagnosticoSeleccionado?.EsResueltaAprobador == true ||
            DiagnosticoSeleccionado?.EsDescartada == true;

        public bool PuedeEditarClasificacion =>
            DiagnosticoSeleccionado != null &&
            !DiagnosticoSeleccionado.EsDescartada &&
            !DiagnosticoSeleccionado.EsResueltaAprobador;

        public bool MostrarPanelPublicaciones =>
            EsAprobador && DiagnosticosPublicables.Count > 0;

        public string TextoGuardar => EsAprobador
            ? ProponerSubcategoria
                ? "Confirmar y crear subcategoría"
                : "Confirmar clasificación"
            : ProponerSubcategoria
                ? "Guardar propuesta"
                : "Guardar clasificación";

        public string TextoAlternarPropuesta => ProponerSubcategoria
            ? "← Volver al catálogo"
            : "+ Proponer nueva subcategoría";

        public bool UsarSubcategoriaExistente => !ProponerSubcategoria;

        public bool RequiereDescripcionCreacion =>
            EsAprobador && ProponerSubcategoria;

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();

                if (cargandoFormulario)
                    return;

                BuscarSubcategoria = string.Empty;
                _ = CargarSubcategoriasEspecificasAsync(
                    value?.CategoriaAlbumBotanicoId);
            }
        }

        public AlbumRegistroJerarquiaResponse? SubcategoriaSeleccionada
        {
            get => subcategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(subcategoriaSeleccionada, value))
                    return;

                subcategoriaSeleccionada = value;
                OnPropertyChanged();

                if (value != null)
                {
                    ProponerSubcategoria = false;
                    NombreCientifico = value.NombreCientifico ?? string.Empty;
                }
            }
        }

        public bool ProponerSubcategoria
        {
            get => proponerSubcategoria;
            private set
            {
                if (proponerSubcategoria == value)
                    return;

                proponerSubcategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsarSubcategoriaExistente));
                OnPropertyChanged(nameof(RequiereDescripcionCreacion));
                OnPropertyChanged(nameof(TextoGuardar));

                if (value && !cargandoFormulario)
                {
                    subcategoriaSeleccionada = null;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));

                    if (string.IsNullOrWhiteSpace(SubcategoriaPropuesta))
                    {
                        SubcategoriaPropuesta =
                            DiagnosticoSeleccionado?.SubcategoriaMostrar ??
                            DiagnosticoSeleccionado?.Diagnostico ??
                            string.Empty;
                    }
                }
            }
        }

        public string BuscarSubcategoria
        {
            get => buscarSubcategoria;
            set
            {
                if (!Set(ref buscarSubcategoria, value))
                    return;

                AplicarFiltroSubcategorias();
            }
        }

        public string SubcategoriaPropuesta
        {
            get => subcategoriaPropuesta;
            set => Set(ref subcategoriaPropuesta, value);
        }

        public string NombreCientifico
        {
            get => nombreCientifico;
            set => Set(ref nombreCientifico, value);
        }

        public string Descripcion
        {
            get => descripcion;
            set => Set(ref descripcion, value);
        }

        public string Sintomas
        {
            get => sintomas;
            set => Set(ref sintomas, value);
        }

        public string Motivo
        {
            get => motivo;
            set => Set(ref motivo, value);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!inicializada)
                await InicializarAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (IsBusy)
                return true;

            _ = CerrarAsync(false);
            return true;
        }

        private async Task InicializarAsync()
        {
            inicializada = true;
            IsBusy = true;

            try
            {
                ApiResult<List<CategoriaAlbumBotanicoResponse>> resultado =
                    await albumApi.GetCategoriasAsync(false);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                Categorias.Clear();
                foreach (CategoriaAlbumBotanicoResponse item in
                         resultado.Data ?? [])
                {
                    Categorias.Add(item);
                }

                await RefrescarDiagnosticosAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible cargar las clasificaciones",
                    ex.Message,
                    "Aceptar");
                await CerrarAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefrescarDiagnosticosAsync(string? claveSeleccionar)
        {
            List<ClasificacionDiagnosticoAlbumItem> datos =
                await diagnosticoApi.ObtenerEstadoAsync(
                    diagnosticoId,
                    Fotografia.FotografiaId);

            string? claveAnterior = claveSeleccionar ??
                DiagnosticoSeleccionado?.DiagnosticoClave;

            DiagnosticoSeleccionado = null;
            Diagnosticos.Clear();
            DiagnosticosPublicables.Clear();

            foreach (ClasificacionDiagnosticoAlbumItem item in datos
                         .Where(item => item.Activo)
                         .OrderByDescending(item => item.EsPrincipal)
                         .ThenBy(item => item.OrdenDiagnostico))
            {
                item.PublicarEnAlbum = item.PublicadaActiva;
                Diagnosticos.Add(item);

                if (item.PuedePublicarse || item.PublicadaActiva)
                    DiagnosticosPublicables.Add(item);
            }

            OnPropertyChanged(nameof(ResumenDiagnosticos));
            OnPropertyChanged(nameof(MostrarPanelPublicaciones));
            ActualizarComandos();

            ClasificacionDiagnosticoAlbumItem? seleccionar =
                !string.IsNullOrWhiteSpace(claveAnterior)
                    ? Diagnosticos.FirstOrDefault(item =>
                        string.Equals(
                            item.DiagnosticoClave,
                            claveAnterior,
                            StringComparison.Ordinal))
                    : null;

            seleccionar ??= Diagnosticos.FirstOrDefault(item =>
                !item.EsDescartada && !item.EsResueltaAprobador);
            seleccionar ??= Diagnosticos.FirstOrDefault(item => item.EsPrincipal);
            seleccionar ??= Diagnosticos.FirstOrDefault();

            if (seleccionar != null)
                await SeleccionarDiagnosticoAsync(seleccionar);
        }

        private async void OnSeleccionarDiagnosticoClicked(
            object? sender,
            EventArgs e)
        {
            if (IsBusy ||
                sender is not Button boton ||
                boton.CommandParameter is not ClasificacionDiagnosticoAlbumItem item)
            {
                return;
            }

            await SeleccionarDiagnosticoAsync(item);
        }

        private async Task SeleccionarDiagnosticoAsync(
            ClasificacionDiagnosticoAlbumItem item)
        {
            DiagnosticoSeleccionado = item;
            await PrecargarFormularioAsync(item);
        }

        private async Task PrecargarFormularioAsync(
            ClasificacionDiagnosticoAlbumItem item)
        {
            cargandoFormulario = true;
            try
            {
                buscarSubcategoria = string.Empty;
                OnPropertyChanged(nameof(BuscarSubcategoria));

                subcategoriaPropuesta = LimpiarNombreDiagnostico(
                    PrimerTexto(
                        item.SubcategoriaSeleccionada,
                        item.SubcategoriaSugerida,
                        item.Diagnostico));
                OnPropertyChanged(nameof(SubcategoriaPropuesta));

                nombreCientifico = item.NombreCientificoSugerido ?? string.Empty;
                OnPropertyChanged(nameof(NombreCientifico));

                descripcion = Fotografia.ResultadoIA?.ResumenImagen ?? string.Empty;
                OnPropertyChanged(nameof(Descripcion));

                sintomas = string.Join(
                    Environment.NewLine,
                    Fotografia.ResultadoIA?.SintomasVisibles ?? []);
                OnPropertyChanged(nameof(Sintomas));

                motivo = PrimerTexto(
                    item.EstadoClasificacionTexto,
                    "Clasificación revisada por el analizador.");
                OnPropertyChanged(nameof(Motivo));

                int? categoriaId =
                    item.CategoriaAlbumBotanicoIdSeleccionada ??
                    item.CategoriaAlbumBotanicoIdSugerida;

                CategoriaAlbumBotanicoResponse? categoria = Categorias
                    .FirstOrDefault(catalogo =>
                        catalogo.CategoriaAlbumBotanicoId == categoriaId);

                if (categoria == null)
                {
                    string claveCategoria = NormalizarClave(
                        PrimerTexto(
                            item.CategoriaSeleccionada,
                            item.CategoriaSugerida));

                    categoria = Categorias.FirstOrDefault(catalogo =>
                        NormalizarClave(catalogo.NombreCategoria) ==
                        claveCategoria);
                }

                categoriaSeleccionada = categoria;
                OnPropertyChanged(nameof(CategoriaSeleccionada));

                int? subcategoriaId =
                    item.AlbumBotanicoCafeIdSeleccionado ??
                    item.AlbumBotanicoCafeIdSugerido;

                bool propuesta =
                    item.EsPropuestaAnalizador ||
                    (subcategoriaId is not > 0 && item.RequiereDecision);

                proponerSubcategoria = propuesta;
                OnPropertyChanged(nameof(ProponerSubcategoria));
                OnPropertyChanged(nameof(UsarSubcategoriaExistente));
                OnPropertyChanged(nameof(RequiereDescripcionCreacion));
                OnPropertyChanged(nameof(TextoGuardar));

                await CargarSubcategoriasEspecificasAsync(
                    categoria?.CategoriaAlbumBotanicoId,
                    subcategoriaId,
                    item.SubcategoriaMostrar);
            }
            finally
            {
                cargandoFormulario = false;
                OnPropertyChanged(nameof(ClasificacionCerrada));
                OnPropertyChanged(nameof(PuedeEditarClasificacion));
                ActualizarComandos();
            }
        }

        private async Task CargarSubcategoriasEspecificasAsync(
            int? categoriaId,
            int? idPreferido = null,
            string? nombrePreferido = null)
        {
            int version = Interlocked.Increment(ref versionCarga);

            subcategoriasCatalogo.Clear();
            SubcategoriasEspecificas.Clear();
            subcategoriaSeleccionada = null;
            OnPropertyChanged(nameof(SubcategoriaSeleccionada));

            if (categoriaId is not > 0)
                return;

            try
            {
                ApiResult<List<AlbumRegistroJerarquiaResponse>> resultado =
                    await jerarquiaApi.GetJerarquiaRegistrosAsync(
                        categoriaId: categoriaId,
                        subcategoriaId: null,
                        incluirInactivos: false);

                if (version != versionCarga)
                    return;

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                subcategoriasCatalogo.AddRange(
                    (resultado.Data ?? [])
                        .Where(item => item.Activo)
                        .OrderBy(item => item.Titulo));

                AplicarFiltroSubcategorias();

                AlbumRegistroJerarquiaResponse? seleccion = null;

                if (idPreferido is > 0)
                {
                    seleccion = subcategoriasCatalogo.FirstOrDefault(item =>
                        item.AlbumBotanicoCafeId == idPreferido.Value);
                }

                if (seleccion == null &&
                    !string.IsNullOrWhiteSpace(nombrePreferido))
                {
                    string clave = NormalizarClave(
                        LimpiarNombreDiagnostico(nombrePreferido));

                    seleccion = subcategoriasCatalogo.FirstOrDefault(item =>
                        NormalizarClave(
                            LimpiarNombreDiagnostico(item.Titulo)) == clave);
                }

                if (seleccion != null && !proponerSubcategoria)
                {
                    subcategoriaSeleccionada = seleccion;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));
                    nombreCientifico = seleccion.NombreCientifico ?? string.Empty;
                    OnPropertyChanged(nameof(NombreCientifico));
                }
                else if (subcategoriasCatalogo.Count == 0 &&
                    DiagnosticoSeleccionado?.EsResueltaAprobador != true)
                {
                    proponerSubcategoria = true;
                    OnPropertyChanged(nameof(ProponerSubcategoria));
                    OnPropertyChanged(nameof(UsarSubcategoriaExistente));
                    OnPropertyChanged(nameof(RequiereDescripcionCreacion));
                    OnPropertyChanged(nameof(TextoGuardar));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Subcategorías",
                    ex.Message,
                    "Aceptar");
            }
        }

        private void AplicarFiltroSubcategorias()
        {
            string filtro = NormalizarClave(BuscarSubcategoria);
            IEnumerable<AlbumRegistroJerarquiaResponse> items =
                subcategoriasCatalogo;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                items = items.Where(item =>
                    NormalizarClave(item.Titulo).Contains(
                        filtro,
                        StringComparison.Ordinal) ||
                    NormalizarClave(item.NombreCientifico).Contains(
                        filtro,
                        StringComparison.Ordinal));
            }

            SubcategoriasEspecificas.Clear();
            foreach (AlbumRegistroJerarquiaResponse item in items)
                SubcategoriasEspecificas.Add(item);
        }

        private void AlternarPropuesta()
        {
            if (IsBusy || !PuedeEditarClasificacion)
                return;

            ProponerSubcategoria = !ProponerSubcategoria;

            if (!ProponerSubcategoria)
            {
                BuscarSubcategoria = string.Empty;
                AplicarFiltroSubcategorias();
            }
        }

        private async Task GuardarClasificacionAsync()
        {
            if (IsBusy ||
                DiagnosticoSeleccionado == null ||
                !PuedeEditarClasificacion)
            {
                return;
            }

            if (!ValidarFormulario(out string error))
            {
                await DisplayAlert(
                    "Revise la clasificación",
                    error,
                    "Aceptar");
                return;
            }

            if (ProponerSubcategoria &&
                !await ValidarPropuestaContraCatalogoAsync())
            {
                return;
            }

            ClasificacionDiagnosticoAlbumItem actual =
                DiagnosticoSeleccionado;
            bool eraPropuesta = ProponerSubcategoria;

            bool confirmar = await DisplayAlert(
                EsAprobador
                    ? "Confirmar clasificación"
                    : ProponerSubcategoria
                        ? "Guardar propuesta"
                        : "Guardar clasificación",
                CrearMensajeConfirmacion(actual),
                "Continuar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            try
            {
                int? subcategoriaId = ProponerSubcategoria
                    ? null
                    : SubcategoriaSeleccionada?.AlbumBotanicoCafeId;

                var request = new ResolverClasificacionDiagnosticoAlbumRequest
                {
                    DiagnosticoClave = actual.DiagnosticoClave,
                    Etapa = etapa,
                    Accion = string.Equals(
                            actual.AccionHumana,
                            "CORREGIR",
                            StringComparison.OrdinalIgnoreCase)
                        ? "CORREGIR"
                        : "CONFIRMAR",
                    CategoriaAlbumBotanicoId =
                        CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    AlbumBotanicoCafeId = subcategoriaId,
                    ProponerSubcategoria = ProponerSubcategoria,
                    Categoria =
                        CategoriaSeleccionada?.NombreCategoria ?? string.Empty,
                    Subcategoria = ProponerSubcategoria
                        ? SubcategoriaPropuesta.Trim()
                        : SubcategoriaSeleccionada?.Titulo ?? string.Empty,
                    NombreCientifico = NombreCientifico.Trim(),
                    Descripcion = Descripcion.Trim(),
                    Sintomas = Sintomas.Trim(),
                    Motivo = Motivo.Trim()
                };

                await diagnosticoApi.ResolverClasificacionAsync(
                    diagnosticoId,
                    Fotografia.FotografiaId,
                    request);

                cambiosRealizados = true;
                string clave = actual.DiagnosticoClave;

                await RefrescarDiagnosticosAsync(clave);

                ClasificacionDiagnosticoAlbumItem? actualizado =
                    Diagnosticos.FirstOrDefault(item =>
                        string.Equals(
                            item.DiagnosticoClave,
                            clave,
                            StringComparison.Ordinal));

                if (actualizado?.EsPrincipal == true)
                    await SincronizarClasificacionPrincipalLegadaAsync(actualizado);

                ClasificacionDiagnosticoAlbumItem? siguiente =
                    Diagnosticos.FirstOrDefault(item =>
                        !item.EsDescartada && !item.EsResueltaAprobador &&
                        !string.Equals(
                            item.DiagnosticoClave,
                            clave,
                            StringComparison.Ordinal));

                if (siguiente != null)
                    await SeleccionarDiagnosticoAsync(siguiente);

                await DisplayAlert(
                    "Clasificación guardada",
                    EsAprobador
                        ? "La clasificación de este diagnóstico quedó confirmada sin modificar los demás diagnósticos de la fotografía."
                        : eraPropuesta
                            ? "La propuesta quedó registrada únicamente para este diagnóstico."
                            : "La clasificación quedó registrada únicamente para este diagnóstico.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible guardar",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Mantiene actualizada la clasificación histórica de una sola foto con
        /// el diagnóstico principal. Los diagnósticos adicionales nunca escriben
        /// sobre esta relación; su fuente oficial es la tabla V2 por diagnóstico.
        /// </summary>
        private async Task SincronizarClasificacionPrincipalLegadaAsync(
            ClasificacionDiagnosticoAlbumItem item)
        {
            int? categoriaId =
                item.CategoriaAlbumBotanicoIdSeleccionada ??
                item.CategoriaAlbumBotanicoIdSugerida;

            bool propuesta =
                item.EsPropuestaAnalizador &&
                item.AlbumBotanicoCafeIdSeleccionado is not > 0;

            int? subcategoriaId = propuesta
                ? null
                : item.AlbumBotanicoCafeIdSeleccionado ??
                  item.AlbumBotanicoCafeIdSugerido;

            if (categoriaId is not > 0)
                return;

            var request = new ResolverJerarquiaAlbumRequest
            {
                Etapa = etapa,
                CategoriaAlbumBotanicoId = categoriaId,
                SubcategoriaAlbumBotanicoId = subcategoriaId,
                AlbumBotanicoCafeId = subcategoriaId,
                ProponerCategoria = false,
                ProponerSubcategoria = propuesta,
                ProponerFicha = false,
                CategoriaPropuesta = string.Empty,
                SubcategoriaPropuesta = item.SubcategoriaMostrar,
                FichaPropuesta = item.SubcategoriaMostrar,
                NombreCientifico = item.NombreCientificoSugerido,
                Descripcion = string.IsNullOrWhiteSpace(Descripcion)
                    ? Fotografia.ResultadoIA?.ResumenImagen ?? string.Empty
                    : Descripcion,
                Sintomas = Sintomas,
                Motivo = string.IsNullOrWhiteSpace(Motivo)
                    ? "Clasificación principal sincronizada con el flujo por diagnóstico."
                    : Motivo
            };

            ApiResult<bool> resultado = await jerarquiaApi.ResolverJerarquiaAsync(
                diagnosticoId,
                Fotografia.FotografiaId,
                request);

            if (!resultado.Success)
                throw new InvalidOperationException(resultado.Message);

            JerarquiaActual = new JerarquiaDiagnosticoFotoResponse
            {
                FotografiaId = Fotografia.FotografiaId,
                TieneClasificacion = true,
                CategoriaAlbumBotanicoId = categoriaId,
                AlbumBotanicoCafeId = subcategoriaId,
                Categoria = item.CategoriaMostrar,
                Ficha = item.SubcategoriaMostrar,
                NombreCientifico = item.NombreCientificoSugerido,
                Estado = item.Estado,
                SubcategoriaEsPropuesta = propuesta,
                FichaEsPropuesta = propuesta
            };

            Fotografia.JerarquiaAlbum = JerarquiaActual;
            OnPropertyChanged(nameof(JerarquiaActual));
        }

        private async Task GuardarPublicacionesAsync()
        {
            if (!EsAprobador || IsBusy || !MostrarPanelPublicaciones)
                return;

            List<ClasificacionDiagnosticoAlbumItem> administrables =
                DiagnosticosPublicables
                    .Where(item => item.PuedePublicarse || item.PublicadaActiva)
                    .ToList();

            if (administrables.Count == 0)
                return;

            int publicar = administrables.Count(item =>
                item.PuedePublicarse && item.PublicarEnAlbum);
            int retirar = administrables.Count(item =>
                item.PublicadaActiva && !item.PublicarEnAlbum);

            bool confirmar = await DisplayAlert(
                "Guardar publicaciones",
                $"Se mantendrán/publicarán {publicar} diagnóstico(s) y se retirarán {retirar} publicación(es) activas. La fotografía original no se duplicará. ¿Desea continuar?",
                "Guardar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            try
            {
                var request = new SincronizarPublicacionesDiagnosticosAlbumRequest
                {
                    Diagnosticos = administrables
                        .Select(item =>
                            new PublicacionDiagnosticoSeleccionAlbumRequest
                            {
                                DiagnosticoClave = item.DiagnosticoClave,
                                Publicar =
                                    item.PuedePublicarse &&
                                    item.PublicarEnAlbum,
                                Descripcion =
                                    $"{item.Rol}: {item.Diagnostico}. " +
                                    item.RutaClasificacion
                            })
                        .ToList()
                };

                await diagnosticoApi.SincronizarPublicacionesAsync(
                    diagnosticoId,
                    Fotografia.FotografiaId,
                    request);

                cambiosRealizados = true;
                AlbumBotanicoRefreshState.MarcarCambio();

                string? clave = DiagnosticoSeleccionado?.DiagnosticoClave;
                await RefrescarDiagnosticosAsync(clave);

                int activas = Diagnosticos.Count(item => item.PublicadaActiva);
                await DisplayAlert(
                    "Publicaciones actualizadas",
                    activas == 0
                        ? "La fotografía no tiene diagnósticos publicados actualmente en el Álbum Botánico."
                        : activas == 1
                            ? "1 diagnóstico de esta fotografía está publicado actualmente en el Álbum Botánico."
                            : $"{activas} diagnósticos de esta fotografía están publicados actualmente en el Álbum Botánico.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible actualizar las publicaciones",
                    ex.Message,
                    "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SeleccionarTodasPublicaciones()
        {
            foreach (ClasificacionDiagnosticoAlbumItem item in
                     DiagnosticosPublicables)
            {
                if (item.PuedePublicarse)
                    item.PublicarEnAlbum = true;
            }
        }

        private void QuitarTodasPublicaciones()
        {
            foreach (ClasificacionDiagnosticoAlbumItem item in
                     DiagnosticosPublicables)
            {
                item.PublicarEnAlbum = false;
            }
        }

        private bool ValidarFormulario(out string error)
        {
            if (DiagnosticoSeleccionado == null)
            {
                error = "Seleccione un diagnóstico para continuar.";
                return false;
            }

            if (CategoriaSeleccionada == null)
            {
                error =
                    "Seleccione una categoría existente. Las categorías nuevas se administran desde el Álbum Botánico.";
                return false;
            }

            if (ProponerSubcategoria)
            {
                if (SubcategoriaPropuesta.Trim().Length < 3)
                {
                    error = "Ingrese el nombre de la subcategoría propuesta.";
                    return false;
                }

                if (EsAprobador && Descripcion.Trim().Length < 8)
                {
                    error =
                        "Ingrese una descripción de al menos 8 caracteres para crear la subcategoría oficial.";
                    return false;
                }
            }
            else if (SubcategoriaSeleccionada == null)
            {
                error =
                    "Seleccione una subcategoría del catálogo o use «Proponer nueva subcategoría».";
                return false;
            }

            if (Motivo.Trim().Length < 8)
            {
                error =
                    "Explique la clasificación con al menos 8 caracteres.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private async Task<bool> ValidarPropuestaContraCatalogoAsync()
        {
            string propuesta = SubcategoriaPropuesta.Trim();
            string clave = NormalizarClave(propuesta);

            AlbumRegistroJerarquiaResponse? exacta =
                subcategoriasCatalogo.FirstOrDefault(item =>
                    NormalizarClave(item.Titulo) == clave ||
                    (!string.IsNullOrWhiteSpace(NombreCientifico) &&
                     NormalizarClave(item.NombreCientifico) ==
                        NormalizarClave(NombreCientifico)));

            if (exacta != null)
            {
                SubcategoriaSeleccionada = exacta;
                ProponerSubcategoria = false;
                BuscarSubcategoria = exacta.Titulo;

                await DisplayAlert(
                    "Subcategoría ya existente",
                    $"Ya existe «{exacta.Titulo}» dentro de esta categoría. Se utilizará el registro existente para evitar una duplicidad.",
                    "Aceptar");
                return true;
            }

            AlbumRegistroJerarquiaResponse? similar =
                subcategoriasCatalogo
                    .Select(item => new
                    {
                        Item = item,
                        Similitud = CalcularSimilitud(
                            clave,
                            NormalizarClave(item.Titulo))
                    })
                    .Where(item => item.Similitud >= 0.84d)
                    .OrderByDescending(item => item.Similitud)
                    .Select(item => item.Item)
                    .FirstOrDefault();

            if (similar == null)
                return true;

            bool usarExistente = await DisplayAlert(
                "Posible duplicado",
                $"Existe una subcategoría muy similar: «{similar.Titulo}». ¿Desea utilizar la existente en lugar de crear «{propuesta}»?",
                "Usar existente",
                "Mantener propuesta");

            if (!usarExistente)
                return true;

            SubcategoriaSeleccionada = similar;
            ProponerSubcategoria = false;
            BuscarSubcategoria = similar.Titulo;
            return true;
        }

        private string CrearMensajeConfirmacion(
            ClasificacionDiagnosticoAlbumItem item)
        {
            string destino = ProponerSubcategoria
                ? $"{CategoriaSeleccionada?.NombreCategoria} → {SubcategoriaPropuesta.Trim()}"
                : $"{CategoriaSeleccionada?.NombreCategoria} → {SubcategoriaSeleccionada?.Titulo}";

            if (EsAprobador)
            {
                return ProponerSubcategoria
                    ? $"Se creará/confirmará «{SubcategoriaPropuesta.Trim()}» y se asignará únicamente a {item.Diagnostico}. La clasificación quedará cerrada para este diagnóstico."
                    : $"Se confirmará {destino} únicamente para {item.Diagnostico}. La clasificación quedará cerrada para este diagnóstico.";
            }

            return ProponerSubcategoria
                ? $"Se registrará {destino} como propuesta únicamente para {item.Diagnostico}."
                : $"Se registrará {destino} únicamente para {item.Diagnostico}.";
        }

        private async void OnImagenTapped(object? sender, TappedEventArgs e)
        {
            var visor = new VisorFotografiaFitosanitariaPage(
                [Fotografia],
                0);
            await Navigation.PushModalAsync(visor, animated: false);
        }

        private async Task CerrarAsync(bool resultado)
        {
            if (!resultadoTcs.Task.IsCompleted)
            {
                resultadoTcs.TrySetResult(
                    resultado || cambiosRealizados);
            }

            if (Navigation.ModalStack.Contains(this))
                await Navigation.PopModalAsync();
            else if (Navigation.NavigationStack.Contains(this))
                await Navigation.PopAsync();
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
            AlternarPropuestaCommand.ChangeCanExecute();
            LimpiarBusquedaCommand.ChangeCanExecute();
            PublicarTodosCommand.ChangeCanExecute();
            NingunaPublicacionCommand.ChangeCanExecute();
            GuardarPublicacionesCommand.ChangeCanExecute();
        }

        private static string NormalizarEtapa(string? valor) =>
            string.Equals(
                valor,
                "APROBADOR",
                StringComparison.OrdinalIgnoreCase)
                ? "APROBADOR"
                : "ANALIZADOR";

        private bool Set(
            ref string campo,
            string? valor,
            [CallerMemberName] string? nombre = null)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return false;

            campo = nuevo;
            OnPropertyChanged(nombre);
            return true;
        }

        private static string PrimerTexto(params string?[] valores) =>
            valores.FirstOrDefault(valor =>
                !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;

        private static string LimpiarNombreDiagnostico(string? valor)
        {
            string texto = (valor ?? string.Empty).Trim();
            int parentesis = texto.IndexOf('(');
            if (parentesis > 0)
                texto = texto[..parentesis].Trim();

            int separador = texto.IndexOf(" - ", StringComparison.Ordinal);
            if (separador > 0)
                texto = texto[..separador].Trim();

            return texto;
        }

        private static string NormalizarClave(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto = valor
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(texto.Length);
            bool espacioPendiente = false;

            foreach (char caracter in texto)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter))
                {
                    if (espacioPendiente && builder.Length > 0)
                        builder.Append(' ');

                    builder.Append(caracter);
                    espacioPendiente = false;
                }
                else if (builder.Length > 0)
                {
                    espacioPendiente = true;
                }
            }

            return builder.ToString().Trim();
        }

        private static double CalcularSimilitud(
            string izquierda,
            string derecha)
        {
            string a = izquierda.Replace(" ", string.Empty);
            string b = derecha.Replace(" ", string.Empty);

            if (a.Length == 0 || b.Length == 0)
                return 0d;

            if (string.Equals(a, b, StringComparison.Ordinal))
                return 1d;

            int distancia = DistanciaLevenshtein(a, b);
            return 1d - distancia / (double)Math.Max(a.Length, b.Length);
        }

        private static int DistanciaLevenshtein(string a, string b)
        {
            int[] anterior = Enumerable.Range(0, b.Length + 1).ToArray();
            int[] actual = new int[b.Length + 1];

            for (int i = 1; i <= a.Length; i++)
            {
                actual[0] = i;

                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    actual[j] = Math.Min(
                        Math.Min(actual[j - 1] + 1, anterior[j] + 1),
                        anterior[j - 1] + costo);
                }

                (anterior, actual) = (actual, anterior);
            }

            return anterior[b.Length];
        }
    }
}
