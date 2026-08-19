using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra una subcategoría específica del Álbum Botánico.
    /// La estructura oficial es Categoría -> Subcategoría -> Fotografías.
    /// AlbumBotanicoCafe representa directamente la subcategoría y conserva
    /// toda su información técnica.
    /// </summary>
    public sealed class AlbumRegistroFormViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private CancellationTokenSource? cargaCts;

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private FormMode.FormModeSelect mode;
        private int registroId;
        private int categoriaInicialId;
        private bool inicializado;
        private string titulo = string.Empty;
        private string nombreCientifico = string.Empty;
        private string descripcion = string.Empty;
        private string caracteristicas = string.Empty;
        private string sintomas = string.Empty;
        private string causas = string.Empty;
        private string recomendaciones = string.Empty;
        private string observaciones = string.Empty;

        private string errorCategoria = string.Empty;
        private string errorTitulo = string.Empty;
        private string errorNombreCientifico = string.Empty;
        private string errorDescripcion = string.Empty;

        public ObservableCollection<CategoriaAlbumBotanicoResponse> Categorias
        {
            get => categorias;
            private set
            {
                categorias = value;
                OnPropertyChanged();
            }
        }

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();

                if (categoriaSeleccionada != null)
                    ErrorCategoria = string.Empty;
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloPagina));
                OnPropertyChanged(nameof(PuedeGuardar));
                RefrescarComandos();
            }
        }

        public int RegistroId
        {
            get => registroId;
            set
            {
                registroId = value;
                OnPropertyChanged();
            }
        }

        public int CategoriaInicialId
        {
            get => categoriaInicialId;
            set
            {
                categoriaInicialId = value;
                OnPropertyChanged();
            }
        }

        public string Titulo
        {
            get => titulo;
            set
            {
                titulo = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(titulo) &&
                    titulo.Trim().Length <= 200)
                {
                    ErrorTitulo = string.Empty;
                }
            }
        }

        public string NombreCientifico
        {
            get => nombreCientifico;
            set
            {
                nombreCientifico = value ?? string.Empty;
                OnPropertyChanged();

                if (nombreCientifico.Trim().Length <= 200)
                    ErrorNombreCientifico = string.Empty;
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcion))
                    ErrorDescripcion = string.Empty;
            }
        }

        public string Caracteristicas
        {
            get => caracteristicas;
            set
            {
                caracteristicas = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Sintomas
        {
            get => sintomas;
            set
            {
                sintomas = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Causas
        {
            get => causas;
            set
            {
                causas = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Recomendaciones
        {
            get => recomendaciones;
            set
            {
                recomendaciones = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Observaciones
        {
            get => observaciones;
            set
            {
                observaciones = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorCategoria
        {
            get => errorCategoria;
            private set
            {
                if (errorCategoria == value)
                    return;

                errorCategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorCategoria));
            }
        }

        public bool TieneErrorCategoria =>
            !string.IsNullOrWhiteSpace(ErrorCategoria);

        public string ErrorTitulo
        {
            get => errorTitulo;
            private set
            {
                if (errorTitulo == value)
                    return;

                errorTitulo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorTitulo));
            }
        }

        public bool TieneErrorTitulo =>
            !string.IsNullOrWhiteSpace(ErrorTitulo);

        public string ErrorNombreCientifico
        {
            get => errorNombreCientifico;
            private set
            {
                if (errorNombreCientifico == value)
                    return;

                errorNombreCientifico = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombreCientifico));
            }
        }

        public bool TieneErrorNombreCientifico =>
            !string.IsNullOrWhiteSpace(ErrorNombreCientifico);

        public string ErrorDescripcion
        {
            get => errorDescripcion;
            private set
            {
                if (errorDescripcion == value)
                    return;

                errorDescripcion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(ErrorDescripcion);

        public string TituloPagina =>
            Mode == FormMode.FormModeSelect.Create
                ? "Nueva subcategoría"
                : "Editar subcategoría";

        public bool PuedeGuardar =>
            CanView &&
            (Mode == FormMode.FormModeSelect.Create
                ? CanAdd
                : Mode == FormMode.FormModeSelect.Edit && CanEdit);

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public AlbumRegistroFormViewModel()
        {
            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && PuedeGuardar);

            CancelarCommand = new Command(
                async () => await CancelarAsync(),
                () => !IsBusy);
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(PuedeGuardar));
            RefrescarComandos();
        }

        public async Task InicializarAsync()
        {
            if (inicializado || IsBusy)
                return;

            inicializado = true;
            IsBusy = true;
            RefrescarComandos();

            CancellationTokenSource cts = RenovarCarga();
            CancellationToken token = cts.Token;

            try
            {
                List<CategoriaAlbumBotanicoResponse> categoriasActivas;

                if (AlbumBotanicoVisitaService.IntentarObtenerCategorias(
                        out List<CategoriaAlbumBotanicoResponse> cache))
                {
                    categoriasActivas = cache
                        .Where(item => item.Activo)
                        .ToList();
                }
                else
                {
                    ApiResult<List<CategoriaAlbumBotanicoResponse>> resultado =
                        await apiService.GetCategoriasAsync(false, token);

                    if (token.IsCancellationRequested)
                        return;

                    if (!resultado.Success)
                    {
                        await MostrarErrorAsync(resultado.Message);
                        inicializado = false;
                        return;
                    }

                    categoriasActivas = resultado.Data ?? [];
                }

                Categorias = new ObservableCollection<
                    CategoriaAlbumBotanicoResponse>(categoriasActivas);

                if (Mode == FormMode.FormModeSelect.Edit && RegistroId > 0)
                {
                    /*
                     * La edición siempre obtiene la ficha actual por ID.
                     * El catálogo puede reutilizarse dentro de la visita, pero
                     * los datos editables nunca provienen del snapshot del listado.
                     */
                    ApiResult<AlbumDetalleResponse> detalleResultado =
                        await apiService.GetDetalleAsync(
                            RegistroId,
                            incluirInactivos: true,
                            cancellationToken: token);

                    if (token.IsCancellationRequested)
                        return;

                    if (!detalleResultado.Success ||
                        detalleResultado.Data == null)
                    {
                        await MostrarErrorAsync(detalleResultado.Message);
                        inicializado = false;
                        return;
                    }

                    AlbumDetalleResponse detalle = detalleResultado.Data;

                    CategoriaSeleccionada = Categorias.FirstOrDefault(item =>
                        item.CategoriaAlbumBotanicoId ==
                            detalle.CategoriaAlbumBotanicoId);

                    Titulo = detalle.Titulo;
                    NombreCientifico = detalle.NombreCientifico ?? string.Empty;
                    Descripcion = detalle.Descripcion;
                    Caracteristicas = detalle.Caracteristicas ?? string.Empty;
                    Sintomas = detalle.Sintomas ?? string.Empty;
                    Causas = detalle.Causas ?? string.Empty;
                    Recomendaciones = detalle.Recomendaciones ?? string.Empty;
                    Observaciones = detalle.Observaciones ?? string.Empty;
                }
                else
                {
                    CategoriaSeleccionada = Categorias.FirstOrDefault(item =>
                        item.CategoriaAlbumBotanicoId == CategoriaInicialId)
                        ?? Categorias.FirstOrDefault();
                }

                LimpiarErrores();
            }
            catch (OperationCanceledException)
            {
                inicializado = false;
            }
            catch (Exception ex)
            {
                inicializado = false;
                await MostrarErrorInesperadoAsync(
                    "cargar la subcategoría del álbum",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? anterior = cargaCts;
            cargaCts = null;

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        private CancellationTokenSource RenovarCarga()
        {
            CancelarCarga();
            cargaCts = new CancellationTokenSource();
            return cargaCts;
        }

        private async Task GuardarAsync()
        {
            if (IsBusy || !PuedeGuardar)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            bool esCreacion = Mode == FormMode.FormModeSelect.Create;
            bool confirmar = esCreacion
                ? await ConfirmarGuardadoAsync("la subcategoría")
                : await ConfirmarActualizacionAsync("la subcategoría");

            if (!confirmar)
                return;

            var request = new AlbumRegistroRequest
            {
                AlbumBotanicoCafeId = RegistroId,
                CategoriaAlbumBotanicoId =
                    CategoriaSeleccionada!.CategoriaAlbumBotanicoId,
                Titulo = Titulo.Trim(),
                NombreCientifico = LimpiarOpcional(NombreCientifico),
                Descripcion = Descripcion.Trim(),
                Caracteristicas = LimpiarOpcional(Caracteristicas),
                Sintomas = LimpiarOpcional(Sintomas),
                Causas = LimpiarOpcional(Causas),
                Recomendaciones = LimpiarOpcional(Recomendaciones),
                Observaciones = LimpiarOpcional(Observaciones)
            };

            IsBusy = true;
            RefrescarComandos();
            CancellationTokenSource cts = RenovarCarga();

            try
            {
                string mensaje;

                if (esCreacion)
                {
                    ApiResult<RegistroAlbumCreadoData> resultado =
                        await apiService.CrearRegistroAsync(request, cts.Token);

                    if (cts.Token.IsCancellationRequested)
                        return;

                    if (!resultado.Success || resultado.Data == null)
                    {
                        await MostrarErrorAsync(resultado.Message);
                        return;
                    }

                    RegistroId = resultado.Data.AlbumBotanicoCafeId;
                    Mode = FormMode.FormModeSelect.Edit;
                    mensaje = resultado.Message;
                }
                else
                {
                    ApiResult<bool> resultado =
                        await apiService.ActualizarRegistroAsync(
                            request,
                            cts.Token);

                    if (cts.Token.IsCancellationRequested)
                        return;

                    if (!resultado.Success)
                    {
                        await MostrarErrorAsync(resultado.Message);
                        return;
                    }

                    mensaje = resultado.Message;
                }

                AlbumBotanicoRefreshState.MarcarCambio();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(mensaje)
                        ? "Subcategoría guardada correctamente."
                        : mensaje);

                if (esCreacion)
                {
                    await GoToAsyncParameters(
                        $"{AppRoutes.Regresar}/" +
                        AppRoutes.AlbumFotosAdministrar,
                        new Dictionary<string, object>
                        {
                            ["RegistroId"] = RegistroId
                        });
                }
                else
                {
                    await GoToAsyncParameters(AppRoutes.Regresar);
                }
            }
            catch (OperationCanceledException)
            {
                // La navegación canceló el guardado en curso.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar la subcategoría del álbum",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            CancelarCarga();
            await GoToAsyncParameters(AppRoutes.Regresar);
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            Titulo = Titulo.Trim();
            NombreCientifico = NombreCientifico.Trim();
            Descripcion = Descripcion.Trim();

            if (CategoriaSeleccionada == null)
                ErrorCategoria = "Seleccione una categoría.";

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ErrorTitulo = "Ingrese el nombre de la subcategoría.";
            }
            else if (Titulo.Length > 200)
            {
                ErrorTitulo =
                    "El nombre no puede superar los 200 caracteres.";
            }

            if (NombreCientifico.Length > 200)
            {
                ErrorNombreCientifico =
                    "El nombre científico no puede superar los 200 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(Descripcion))
                ErrorDescripcion = "Ingrese la descripción general.";

            return
                !TieneErrorCategoria &&
                !TieneErrorTitulo &&
                !TieneErrorNombreCientifico &&
                !TieneErrorDescripcion;
        }

        private void LimpiarErrores()
        {
            ErrorCategoria = string.Empty;
            ErrorTitulo = string.Empty;
            ErrorNombreCientifico = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private static string? LimpiarOpcional(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();

        private void RefrescarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }
    }
}
