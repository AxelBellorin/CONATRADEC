using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumRegistroFormViewModel :
        GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private readonly AlbumJerarquiaApiService jerarquiaApiService = new();

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private ObservableCollection<SubcategoriaAlbumBotanicoResponse>
            subcategorias = new();

        private CategoriaAlbumBotanicoResponse?
            categoriaSeleccionada;

        private SubcategoriaAlbumBotanicoResponse?
            subcategoriaSeleccionada;

        private FormMode.FormModeSelect mode;
        private int registroId;
        private int categoriaInicialId;
        private bool inicializado;
        private bool suspendiendoCargaCategoria;
        private int versionCargaSubcategorias;
        private string titulo = string.Empty;
        private string nombreCientifico = string.Empty;
        private string descripcion = string.Empty;
        private string caracteristicas = string.Empty;
        private string sintomas = string.Empty;
        private string causas = string.Empty;
        private string recomendaciones = string.Empty;
        private string observaciones = string.Empty;

        private string errorCategoria = string.Empty;
        private string errorSubcategoria = string.Empty;
        private string errorTitulo = string.Empty;
        private string errorNombreCientifico = string.Empty;
        private string errorDescripcion = string.Empty;

        public ObservableCollection<CategoriaAlbumBotanicoResponse>
            Categorias
        {
            get => categorias;
            private set
            {
                categorias = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SubcategoriaAlbumBotanicoResponse>
            Subcategorias
        {
            get => subcategorias;
            private set
            {
                subcategorias = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HaySubcategorias));
                OnPropertyChanged(nameof(SinSubcategorias));
            }
        }

        public CategoriaAlbumBotanicoResponse?
            CategoriaSeleccionada
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

                if (!suspendiendoCargaCategoria)
                {
                    _ = CargarSubcategoriasAsync(
                        categoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
                        subcategoriaPreferidaId: null,
                        mostrarError: true);
                }
            }
        }

        public SubcategoriaAlbumBotanicoResponse?
            SubcategoriaSeleccionada
        {
            get => subcategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(subcategoriaSeleccionada, value))
                    return;

                subcategoriaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeEditarSubcategoria));

                if (subcategoriaSeleccionada != null)
                    ErrorSubcategoria = string.Empty;
            }
        }

        public bool HaySubcategorias => Subcategorias.Count > 0;
        public bool SinSubcategorias => !HaySubcategorias;
        public bool PuedeEditarSubcategoria =>
            SubcategoriaSeleccionada != null;

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloPagina));
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

        public string ErrorSubcategoria
        {
            get => errorSubcategoria;
            private set
            {
                if (errorSubcategoria == value)
                    return;

                errorSubcategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorSubcategoria));
            }
        }

        public bool TieneErrorSubcategoria =>
            !string.IsNullOrWhiteSpace(ErrorSubcategoria);

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
                OnPropertyChanged(
                    nameof(TieneErrorNombreCientifico));
            }
        }

        public bool TieneErrorNombreCientifico =>
            !string.IsNullOrWhiteSpace(
                ErrorNombreCientifico);

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
                ? "Nuevo registro botánico"
                : "Editar registro botánico";

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Command CrearSubcategoriaCommand { get; }
        public Command EditarSubcategoriaCommand { get; }
        public Command CambiarEstadoSubcategoriaCommand { get; }

        public AlbumRegistroFormViewModel()
        {
            GuardarCommand =
                new Command(
                    async () => await GuardarAsync(),
                    () => !IsBusy);

            CancelarCommand =
                new Command(
                    async () => await CancelarAsync(),
                    () => !IsBusy);

            CrearSubcategoriaCommand =
                new Command(
                    async () => await CrearSubcategoriaAsync(),
                    () => !IsBusy && CategoriaSeleccionada != null);

            EditarSubcategoriaCommand =
                new Command(
                    async () => await EditarSubcategoriaAsync(),
                    () => !IsBusy && PuedeEditarSubcategoria);

            CambiarEstadoSubcategoriaCommand =
                new Command(
                    async () => await CambiarEstadoSubcategoriaAsync(),
                    () => !IsBusy && PuedeEditarSubcategoria);
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            RefrescarComandos();
        }

        public async Task InicializarAsync()
        {
            if (inicializado || IsBusy)
                return;

            inicializado = true;
            IsBusy = true;
            RefrescarComandos();

            try
            {
                ApiResult<List<CategoriaAlbumBotanicoResponse>>
                    categoryResult =
                        await apiService.GetCategoriasAsync(false);

                if (!categoryResult.Success)
                {
                    await MostrarErrorAsync(categoryResult.Message);
                    inicializado = false;
                    return;
                }

                Categorias = new ObservableCollection<
                    CategoriaAlbumBotanicoResponse>(
                        categoryResult.Data ?? []);

                int? subcategoriaInicialId = null;

                suspendiendoCargaCategoria = true;

                try
                {
                    if (Mode == FormMode.FormModeSelect.Edit &&
                        RegistroId > 0)
                    {
                        ApiResult<AlbumDetalleResponse> detailResult =
                            await apiService.GetDetalleAsync(
                                RegistroId,
                                true);

                        if (!detailResult.Success ||
                            detailResult.Data == null)
                        {
                            await MostrarErrorAsync(detailResult.Message);
                            inicializado = false;
                            return;
                        }

                        AlbumDetalleResponse detail = detailResult.Data;

                        CategoriaSeleccionada = Categorias.FirstOrDefault(x =>
                            x.CategoriaAlbumBotanicoId ==
                                detail.CategoriaAlbumBotanicoId);

                        Titulo = detail.Titulo;
                        NombreCientifico = detail.NombreCientifico ?? string.Empty;
                        Descripcion = detail.Descripcion;
                        Caracteristicas = detail.Caracteristicas ?? string.Empty;
                        Sintomas = detail.Sintomas ?? string.Empty;
                        Causas = detail.Causas ?? string.Empty;
                        Recomendaciones = detail.Recomendaciones ?? string.Empty;
                        Observaciones = detail.Observaciones ?? string.Empty;

                        ApiResult<List<AlbumRegistroJerarquiaResponse>>
                            jerarquiaResult =
                                await jerarquiaApiService
                                    .GetJerarquiaRegistrosAsync(
                                        [RegistroId],
                                        incluirInactivos: true);

                        if (jerarquiaResult.Success)
                        {
                            subcategoriaInicialId = jerarquiaResult.Data?
                                .FirstOrDefault()?
                                .SubcategoriaAlbumBotanicoId;
                        }
                    }
                    else
                    {
                        CategoriaSeleccionada =
                            Categorias.FirstOrDefault(x =>
                                x.CategoriaAlbumBotanicoId ==
                                    CategoriaInicialId) ??
                            Categorias.FirstOrDefault();
                    }
                }
                finally
                {
                    suspendiendoCargaCategoria = false;
                }

                await CargarSubcategoriasAsync(
                    CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    subcategoriaInicialId,
                    mostrarError: true);

                LimpiarErrores();
            }
            catch (Exception ex)
            {
                inicializado = false;

                await MostrarErrorInesperadoAsync(
                    "cargar el registro botánico",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CargarSubcategoriasAsync(
            int? categoriaId,
            int? subcategoriaPreferidaId,
            bool mostrarError)
        {
            int version = Interlocked.Increment(
                ref versionCargaSubcategorias);

            if (!categoriaId.HasValue || categoriaId.Value <= 0)
            {
                Subcategorias = [];
                SubcategoriaSeleccionada = null;
                RefrescarComandos();
                return;
            }

            ApiResult<List<SubcategoriaAlbumBotanicoResponse>> result =
                await jerarquiaApiService.GetSubcategoriasAsync(
                    categoriaId,
                    incluirInactivas: false);

            if (version != versionCargaSubcategorias)
                return;

            if (!result.Success)
            {
                Subcategorias = [];
                SubcategoriaSeleccionada = null;

                if (mostrarError)
                    await MostrarToastAsync(result.Message);

                RefrescarComandos();
                return;
            }

            Subcategorias = new ObservableCollection<
                SubcategoriaAlbumBotanicoResponse>(
                    result.Data ?? []);

            SubcategoriaSeleccionada =
                subcategoriaPreferidaId.HasValue
                    ? Subcategorias.FirstOrDefault(item =>
                        item.SubcategoriaAlbumBotanicoId ==
                            subcategoriaPreferidaId.Value)
                    : Subcategorias.FirstOrDefault();

            RefrescarComandos();
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            bool confirm = Mode == FormMode.FormModeSelect.Create
                ? await ConfirmarGuardadoAsync("el registro botánico")
                : await ConfirmarActualizacionAsync("el registro botánico");

            if (!confirm)
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

            try
            {
                string mensajeGuardado;

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    ApiResult<RegistroAlbumCreadoData> result =
                        await apiService.CrearRegistroAsync(request);

                    if (!result.Success || result.Data == null)
                    {
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    RegistroId = result.Data.AlbumBotanicoCafeId;
                    Mode = FormMode.FormModeSelect.Edit;
                    mensajeGuardado = result.Message;
                }
                else
                {
                    ApiResult<bool> result =
                        await apiService.ActualizarRegistroAsync(request);

                    if (!result.Success)
                    {
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    mensajeGuardado = result.Message;
                }

                ApiResult<bool> asignacion =
                    await jerarquiaApiService
                        .AsignarSubcategoriaRegistroAsync(
                            RegistroId,
                            SubcategoriaSeleccionada!
                                .SubcategoriaAlbumBotanicoId);

                if (!asignacion.Success)
                {
                    await MostrarErrorAsync(
                        "El registro fue guardado, pero no fue posible " +
                        "asignar su subcategoría. " +
                        asignacion.Message);
                    return;
                }

                AlbumBotanicoRefreshState.MarcarCambio();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(mensajeGuardado)
                        ? "Registro botánico guardado correctamente."
                        : mensajeGuardado);

                if (Mode == FormMode.FormModeSelect.Edit &&
                    request.AlbumBotanicoCafeId == 0)
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
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el registro botánico",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CrearSubcategoriaAsync()
        {
            if (IsBusy || CategoriaSeleccionada == null)
                return;

            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permiso para crear subcategorías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            string? nombre = await page.DisplayPromptAsync(
                "Nueva subcategoría",
                $"Categoría: {CategoriaSeleccionada.NombreCategoria}\n" +
                "Ingrese el nombre del nivel intermedio.",
                "Crear",
                "Cancelar",
                "Ejemplo: Insectos",
                maxLength: 120);

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            string? descripcionNueva = await page.DisplayPromptAsync(
                "Descripción",
                "Describa brevemente qué fichas pertenecen a esta subcategoría.",
                "Continuar",
                "Omitir",
                maxLength: 600);

            IsBusy = true;
            RefrescarComandos();

            try
            {
                ApiResult<SubcategoriaAlbumBotanicoResponse> result =
                    await jerarquiaApiService.CrearSubcategoriaAsync(
                        new GuardarSubcategoriaAlbumRequest
                        {
                            CategoriaAlbumBotanicoId =
                                CategoriaSeleccionada
                                    .CategoriaAlbumBotanicoId,
                            NombreSubcategoria = nombre.Trim(),
                            Descripcion =
                                string.IsNullOrWhiteSpace(descripcionNueva)
                                    ? null
                                    : descripcionNueva.Trim()
                        });

                if (!result.Success || result.Data == null)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await CargarSubcategoriasAsync(
                    CategoriaSeleccionada.CategoriaAlbumBotanicoId,
                    result.Data.SubcategoriaAlbumBotanicoId,
                    mostrarError: true);

                AlbumBotanicoRefreshState.MarcarCambio();
                await MostrarToastAsync(result.Message);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task EditarSubcategoriaAsync()
        {
            if (IsBusy ||
                CategoriaSeleccionada == null ||
                SubcategoriaSeleccionada == null)
            {
                return;
            }

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permiso para editar subcategorías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            string? nombre = await page.DisplayPromptAsync(
                "Editar subcategoría",
                "Actualice el nombre.",
                "Guardar",
                "Cancelar",
                initialValue: SubcategoriaSeleccionada.NombreSubcategoria,
                maxLength: 120);

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            int id = SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId;

            IsBusy = true;
            RefrescarComandos();

            try
            {
                ApiResult<bool> result =
                    await jerarquiaApiService.ActualizarSubcategoriaAsync(
                        id,
                        new GuardarSubcategoriaAlbumRequest
                        {
                            CategoriaAlbumBotanicoId =
                                CategoriaSeleccionada
                                    .CategoriaAlbumBotanicoId,
                            NombreSubcategoria = nombre.Trim(),
                            Descripcion = SubcategoriaSeleccionada.Descripcion
                        });

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await CargarSubcategoriasAsync(
                    CategoriaSeleccionada.CategoriaAlbumBotanicoId,
                    id,
                    mostrarError: true);

                AlbumBotanicoRefreshState.MarcarCambio();
                await MostrarToastAsync(result.Message);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CambiarEstadoSubcategoriaAsync()
        {
            if (IsBusy || SubcategoriaSeleccionada == null)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permiso para cambiar el estado de subcategorías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            bool nuevoEstado = !SubcategoriaSeleccionada.Activo;

            bool confirmar = await page.DisplayAlert(
                nuevoEstado
                    ? "Activar subcategoría"
                    : "Desactivar subcategoría",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{SubcategoriaSeleccionada.NombreSubcategoria}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            int id = SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId;
            int categoriaId =
                SubcategoriaSeleccionada.CategoriaAlbumBotanicoId;

            IsBusy = true;
            RefrescarComandos();

            try
            {
                ApiResult<bool> result =
                    await jerarquiaApiService
                        .CambiarEstadoSubcategoriaAsync(id, nuevoEstado);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await CargarSubcategoriasAsync(
                    categoriaId,
                    subcategoriaPreferidaId: null,
                    mostrarError: true);

                AlbumBotanicoRefreshState.MarcarCambio();
                await MostrarToastAsync(result.Message);
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

            if (SubcategoriaSeleccionada == null)
            {
                ErrorSubcategoria = HaySubcategorias
                    ? "Seleccione una subcategoría."
                    : "Cree una subcategoría para continuar.";
            }

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ErrorTitulo = "Ingrese el título del registro.";
            }
            else if (Titulo.Length > 200)
            {
                ErrorTitulo =
                    "El título no puede superar los 200 caracteres.";
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
                !TieneErrorSubcategoria &&
                !TieneErrorTitulo &&
                !TieneErrorNombreCientifico &&
                !TieneErrorDescripcion;
        }

        private void LimpiarErrores()
        {
            ErrorCategoria = string.Empty;
            ErrorSubcategoria = string.Empty;
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
            CrearSubcategoriaCommand.ChangeCanExecute();
            EditarSubcategoriaCommand.ChangeCanExecute();
            CambiarEstadoSubcategoriaCommand.ChangeCanExecute();
        }
    }
}
