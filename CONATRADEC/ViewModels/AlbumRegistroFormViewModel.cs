using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumRegistroFormViewModel :
        GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private CategoriaAlbumBotanicoResponse?
            categoriaSeleccionada;

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

        public CategoriaAlbumBotanicoResponse?
            CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
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
                nombreCientifico =
                    value ?? string.Empty;
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
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
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
                var categoryResult =
                    await apiService
                        .GetCategoriasAsync(false);

                if (!categoryResult.Success)
                {
                    await MostrarErrorAsync(
                        categoryResult.Message);
                    inicializado = false;
                    return;
                }

                Categorias =
                    new ObservableCollection<
                        CategoriaAlbumBotanicoResponse>(
                        categoryResult.Data ??
                        new List<
                            CategoriaAlbumBotanicoResponse>());

                if (Mode ==
                        FormMode.FormModeSelect.Edit &&
                    RegistroId > 0)
                {
                    var detailResult =
                        await apiService.GetDetalleAsync(
                            RegistroId,
                            true);

                    if (!detailResult.Success ||
                        detailResult.Data == null)
                    {
                        await MostrarErrorAsync(
                            detailResult.Message);
                        inicializado = false;
                        return;
                    }

                    AlbumDetalleResponse detail =
                        detailResult.Data;

                    CategoriaSeleccionada =
                        Categorias.FirstOrDefault(x =>
                            x.CategoriaAlbumBotanicoId ==
                            detail.CategoriaAlbumBotanicoId);

                    Titulo = detail.Titulo;

                    NombreCientifico =
                        detail.NombreCientifico
                        ?? string.Empty;

                    Descripcion = detail.Descripcion;

                    Caracteristicas =
                        detail.Caracteristicas
                        ?? string.Empty;

                    Sintomas =
                        detail.Sintomas ?? string.Empty;

                    Causas =
                        detail.Causas ?? string.Empty;

                    Recomendaciones =
                        detail.Recomendaciones
                        ?? string.Empty;

                    Observaciones =
                        detail.Observaciones
                        ?? string.Empty;
                }
                else
                {
                    CategoriaSeleccionada =
                        Categorias.FirstOrDefault(x =>
                            x.CategoriaAlbumBotanicoId ==
                            CategoriaInicialId) ??
                        Categorias.FirstOrDefault();
                }

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

            bool confirm =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el registro botánico")
                    : await ConfirmarActualizacionAsync(
                        "el registro botánico");

            if (!confirm)
                return;

            var request = new AlbumRegistroRequest
            {
                AlbumBotanicoCafeId = RegistroId,

                CategoriaAlbumBotanicoId =
                    CategoriaSeleccionada!
                        .CategoriaAlbumBotanicoId,

                Titulo = Titulo.Trim(),

                NombreCientifico =
                    LimpiarOpcional(NombreCientifico),

                Descripcion = Descripcion.Trim(),

                Caracteristicas =
                    LimpiarOpcional(Caracteristicas),

                Sintomas =
                    LimpiarOpcional(Sintomas),

                Causas =
                    LimpiarOpcional(Causas),

                Recomendaciones =
                    LimpiarOpcional(Recomendaciones),

                Observaciones =
                    LimpiarOpcional(Observaciones)
            };

            IsBusy = true;
            RefrescarComandos();

            try
            {
                if (Mode ==
                    FormMode.FormModeSelect.Create)
                {
                    var result =
                        await apiService
                            .CrearRegistroAsync(request);

                    if (!result.Success ||
                        result.Data == null)
                    {
                        await MostrarErrorAsync(
                            result.Message);
                        return;
                    }

                    RegistroId =
                        result.Data.AlbumBotanicoCafeId;

                    /*
                     * El registro ya fue insertado. El modo se cambia antes
                     * de navegar para impedir otra inserción si la navegación
                     * llegara a fallar.
                     */
                    Mode = FormMode.FormModeSelect.Edit;

                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(
                            result.Message)
                            ? "Registro botánico guardado correctamente."
                            : result.Message);

                    /*
                     * Retira el formulario de creación de la pila y abre la
                     * administración de fotografías. De esta forma, al usar
                     * Regresar desde fotografías se vuelve directamente al
                     * panel principal del álbum.
                     */
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
                    var result =
                        await apiService
                            .ActualizarRegistroAsync(request);

                    if (!result.Success)
                    {
                        await MostrarErrorAsync(
                            result.Message);
                        return;
                    }

                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(
                            result.Message)
                            ? "Registro botánico actualizado correctamente."
                            : result.Message);

                    /*
                     * Retroceso real. Si la edición se abrió desde la galería,
                     * vuelve a la galería. Si se abrió desde el detalle,
                     * vuelve al detalle. No crea una nueva página.
                     */
                    await GoToAsyncParameters(
                        AppRoutes.Regresar);
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el registro botánico"
                        : "actualizar el registro botánico",
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

            /*
             * El formulario siempre fue abierto desde otra pantalla del álbum.
             * Solo se elimina la página actual de la pila para evitar ciclos.
             */
            await GoToAsyncParameters(
                AppRoutes.Regresar);
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            Titulo = Titulo.Trim();
            NombreCientifico =
                NombreCientifico.Trim();
            Descripcion = Descripcion.Trim();

            if (CategoriaSeleccionada == null)
            {
                ErrorCategoria =
                    "Seleccione una categoría.";
            }

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ErrorTitulo =
                    "Ingrese el título del registro.";
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
            {
                ErrorDescripcion =
                    "Ingrese la descripción general.";
            }

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

        private static string? LimpiarOpcional(
            string value) =>
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
