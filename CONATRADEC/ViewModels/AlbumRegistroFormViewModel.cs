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
            }
        }

        public string NombreCientifico
        {
            get => nombreCientifico;
            set
            {
                nombreCientifico = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();
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

        public string TituloPagina =>
            Mode == FormMode.FormModeSelect.Create
                ? "Nuevo registro botánico"
                : "Editar registro botánico";

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }

        public AlbumRegistroFormViewModel()
        {
            GuardarCommand =
                new Command(async () => await GuardarAsync());
            CancelarCommand =
                new Command(async () => await CancelarAsync());
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

            try
            {
                var categoryResult =
                    await apiService.GetCategoriasAsync(false);

                if (!categoryResult.Success)
                {
                    await MostrarToastAsync(
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

                if (Mode == FormMode.FormModeSelect.Edit &&
                    RegistroId > 0)
                {
                    var detailResult =
                        await apiService.GetDetalleAsync(
                            RegistroId,
                            true);

                    if (!detailResult.Success ||
                        detailResult.Data == null)
                    {
                        await MostrarToastAsync(
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
                        detail.NombreCientifico ??
                        string.Empty;
                    Descripcion = detail.Descripcion;
                    Caracteristicas =
                        detail.Caracteristicas ??
                        string.Empty;
                    Sintomas =
                        detail.Sintomas ?? string.Empty;
                    Causas =
                        detail.Causas ?? string.Empty;
                    Recomendaciones =
                        detail.Recomendaciones ??
                        string.Empty;
                    Observaciones =
                        detail.Observaciones ??
                        string.Empty;
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
                IsBusy = false;
            }
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (CategoriaSeleccionada == null)
            {
                await MostrarToastAsync(
                    "Seleccione una categoría.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                await MostrarToastAsync(
                    "Ingrese el título del registro.");
                return;
            }

            if (Titulo.Trim().Length > 200)
            {
                await MostrarToastAsync(
                    "El título no puede superar los 200 caracteres.");
                return;
            }

            if (NombreCientifico.Trim().Length > 200)
            {
                await MostrarToastAsync(
                    "El nombre científico no puede superar los 200 caracteres.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                await MostrarToastAsync(
                    "Ingrese la descripción general.");
                return;
            }

            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Guardar registro",
                "¿Desea guardar el registro botánico?",
                "Guardar",
                "Cancelar");

            if (!confirm)
                return;

            var request = new AlbumRegistroRequest
            {
                AlbumBotanicoCafeId = RegistroId,
                CategoriaAlbumBotanicoId =
                    CategoriaSeleccionada
                        .CategoriaAlbumBotanicoId,
                Titulo = Titulo.Trim(),
                NombreCientifico =
                    LimpiarOpcional(NombreCientifico),
                Descripcion = Descripcion.Trim(),
                Caracteristicas =
                    LimpiarOpcional(Caracteristicas),
                Sintomas = LimpiarOpcional(Sintomas),
                Causas = LimpiarOpcional(Causas),
                Recomendaciones =
                    LimpiarOpcional(Recomendaciones),
                Observaciones =
                    LimpiarOpcional(Observaciones)
            };

            IsBusy = true;

            try
            {
                if (Mode == FormMode.FormModeSelect.Create)
                {
                    var result =
                        await apiService
                            .CrearRegistroAsync(request);

                    if (!result.Success ||
                        result.Data == null)
                    {
                        await page.DisplayAlert(
                            "No fue posible",
                            result.Message,
                            "Aceptar");
                        return;
                    }

                    RegistroId =
                        result.Data.AlbumBotanicoCafeId;

                    await MostrarToastAsync(result.Message);

                    await GoToAsyncParameters(
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
                        await page.DisplayAlert(
                            "No fue posible",
                            result.Message,
                            "Aceptar");
                        return;
                    }

                    await MostrarToastAsync(result.Message);

                    await GoToAsyncParameters(
                        AppRoutes.AlbumDetalle,
                        new Dictionary<string, object>
                        {
                            ["RegistroId"] = RegistroId
                        });
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelarAsync()
        {
            if (RegistroId > 0)
            {
                await GoToAsyncParameters(
                    AppRoutes.AlbumDetalle,
                    new Dictionary<string, object>
                    {
                        ["RegistroId"] = RegistroId
                    });
            }
            else
            {
                await GoToAsyncParameters(
                    AppRoutes.AlbumFotos);
            }
        }

        private static string? LimpiarOpcional(
            string value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
    }
}
