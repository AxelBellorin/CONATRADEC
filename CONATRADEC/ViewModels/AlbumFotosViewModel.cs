using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Galería jerárquica del Álbum Botánico.
    ///
    /// Conserva la paginación y virtualización existentes, pero incorpora el
    /// nivel Subcategoría sin cargar fichas individuales ni fotografías extra.
    /// </summary>
    public sealed class AlbumFotosViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private readonly AlbumJerarquiaApiService jerarquiaApiService = new();

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private ObservableCollection<SubcategoriaAlbumBotanicoResponse>
            subcategorias = new();

        private ObservableCollection<AlbumGaleriaJerarquiaItemResponse>
            registros = new();

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private SubcategoriaAlbumBotanicoResponse? subcategoriaSeleccionada;
        private CancellationTokenSource? consultaCts;
        private string textoBusqueda = string.Empty;
        private bool incluirInactivos;
        private bool isRefreshing;
        private bool cargando;
        private bool cargandoMas;
        private bool cargadoInicialmente;
        private int paginaActual;
        private int totalRegistros;
        private bool tieneMas;
        private long versionCargada = -1;

        private int TamanoPagina =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 12
                : 6;

        public ObservableCollection<CategoriaAlbumBotanicoResponse>
            Categorias
        {
            get => categorias;
            private set
            {
                categorias = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HayCategorias));
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
                OnPropertyChanged(nameof(MostrarFiltroSubcategorias));
            }
        }

        public ObservableCollection<AlbumGaleriaJerarquiaItemResponse>
            Registros
        {
            get => registros;
            private set
            {
                registros = value;
                OnPropertyChanged();
                NotificarEstadoGaleria();
            }
        }

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            private set
            {
                categoriaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloGaleria));
                OnPropertyChanged(nameof(TodasSeleccionada));
                OnPropertyChanged(nameof(FondoTodas));
                OnPropertyChanged(nameof(TextoTodas));
                OnPropertyChanged(nameof(BordeTodas));
                OnPropertyChanged(nameof(MostrarFiltroSubcategorias));
                OnPropertyChanged(nameof(MostrarSeccionSubcategorias));
                OnPropertyChanged(nameof(PuedeAdministrarSubcategorias));
            }
        }

        public SubcategoriaAlbumBotanicoResponse? SubcategoriaSeleccionada
        {
            get => subcategoriaSeleccionada;
            private set
            {
                subcategoriaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloGaleria));
                OnPropertyChanged(nameof(TodasSubcategoriasSeleccionada));
                OnPropertyChanged(nameof(TieneSubcategoriaSeleccionada));
                OnPropertyChanged(nameof(TextoCambiarEstadoSubcategoria));
                OnPropertyChanged(nameof(FondoTodasSubcategorias));
                OnPropertyChanged(nameof(TextoTodasSubcategorias));
                OnPropertyChanged(nameof(BordeTodasSubcategorias));
                OnPropertyChanged(nameof(PuedeEditarSubcategoria));
            }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IncluirInactivos
        {
            get => incluirInactivos;
            set
            {
                incluirInactivos = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoadingMore
        {
            get => cargandoMas;
            private set
            {
                cargandoMas = value;
                OnPropertyChanged();
            }
        }

        public bool HayCategorias => Categorias.Count > 0;
        public bool HaySubcategorias => Subcategorias.Count > 0;
        public bool HayRegistros => Registros.Count > 0;
        public bool SinRegistros => !HayRegistros;
        public bool TieneMas => tieneMas;

        public bool TodasSeleccionada => CategoriaSeleccionada == null;
        public bool TodasSubcategoriasSeleccionada =>
            SubcategoriaSeleccionada == null;

        public bool MostrarFiltroSubcategorias =>
            CategoriaSeleccionada != null && HaySubcategorias;

        public bool MostrarSeccionSubcategorias =>
            CategoriaSeleccionada != null;

        public bool TieneSubcategoriaSeleccionada =>
            SubcategoriaSeleccionada != null;

        public string TextoCambiarEstadoSubcategoria =>
            SubcategoriaSeleccionada?.AccionEstadoTexto ?? "Cambiar estado";

        public string FondoTodasSubcategorias =>
            TodasSubcategoriasSeleccionada ? "#3B655B" : "#FFFFFF";

        public string TextoTodasSubcategorias =>
            TodasSubcategoriasSeleccionada ? "#FFFFFF" : "#3B655B";

        public Brush BordeTodasSubcategorias =>
            TodasSubcategoriasSeleccionada
                ? BordeTodasSeleccionado
                : BordeTodasNormal;

        public bool PuedeAdministrarSubcategorias =>
            CategoriaSeleccionada != null && MostrarAdministracion;

        public bool PuedeEditarSubcategoria =>
            SubcategoriaSeleccionada != null && MostrarAdministracion;

        public string FondoTodas =>
            TodasSeleccionada ? "#3B655B" : "#FFFFFF";

        public string TextoTodas =>
            TodasSeleccionada ? "#FFFFFF" : "#3B655B";

        private static readonly Brush BordeTodasSeleccionado =
            new SolidColorBrush(Color.FromArgb("#3B655B"));

        private static readonly Brush BordeTodasNormal =
            new SolidColorBrush(Color.FromArgb("#DDE7E3"));

        public Brush BordeTodas =>
            TodasSeleccionada
                ? BordeTodasSeleccionado
                : BordeTodasNormal;

        public bool MostrarAdministracion =>
            CanAdd || CanEdit || CanDelete;

        public bool MostrarInactivos =>
            CanEdit || CanDelete;

        public string TituloGaleria
        {
            get
            {
                if (CategoriaSeleccionada == null)
                    return "Galería completa";

                if (SubcategoriaSeleccionada == null)
                    return CategoriaSeleccionada.NombreCategoria;

                return
                    $"{CategoriaSeleccionada.NombreCategoria} → " +
                    SubcategoriaSeleccionada.NombreSubcategoria;
            }
        }

        public string TotalRegistrosTexto
        {
            get
            {
                if (totalRegistros == 0)
                    return "0 fichas encontradas";

                if (Registros.Count >= totalRegistros)
                {
                    return totalRegistros == 1
                        ? "1 ficha encontrada"
                        : $"{totalRegistros} fichas encontradas";
                }

                return $"{Registros.Count} de {totalRegistros} fichas";
            }
        }

        public Command CargarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Command SeleccionarTodasCommand { get; }
        public Command SeleccionarTodasSubcategoriasCommand { get; }
        public Command CargarMasCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            SeleccionarCategoriaCommand { get; }

        public Command<SubcategoriaAlbumBotanicoResponse>
            SeleccionarSubcategoriaCommand { get; }

        public Command AgregarCategoriaCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            EditarCategoriaCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            CambiarEstadoCategoriaCommand { get; }

        public Command CrearSubcategoriaCommand { get; }
        public Command EditarSubcategoriaCommand { get; }
        public Command CambiarEstadoSubcategoriaCommand { get; }
        public Command AgregarRegistroCommand { get; }

        public Command<AlbumGaleriaJerarquiaItemResponse>
            AbrirDetalleCommand { get; }

        public Command<AlbumGaleriaJerarquiaItemResponse>
            EditarRegistroCommand { get; }

        public Command<AlbumGaleriaJerarquiaItemResponse>
            CambiarEstadoRegistroCommand { get; }

        public AlbumFotosViewModel()
        {
            CargarCommand = new Command(
                async () => await LoadAsync(true));

            RefrescarCommand = new Command(
                async () => await RefreshAsync());

            BuscarCommand = new Command(
                async () => await BuscarAsync());

            LimpiarBusquedaCommand = new Command(
                async () => await LimpiarBusquedaAsync());

            SeleccionarTodasCommand = new Command(
                async () => await SeleccionarCategoriaAsync(null));

            SeleccionarTodasSubcategoriasCommand = new Command(
                async () => await SeleccionarSubcategoriaAsync(null));

            CargarMasCommand = new Command(
                async () => await CargarMasAsync());

            SeleccionarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item => await SeleccionarCategoriaAsync(item));

            SeleccionarSubcategoriaCommand =
                new Command<SubcategoriaAlbumBotanicoResponse>(
                    async item => await SeleccionarSubcategoriaAsync(item));

            AgregarCategoriaCommand = new Command(
                async () => await AgregarCategoriaAsync());

            EditarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item => await EditarCategoriaAsync(item));

            CambiarEstadoCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item => await CambiarEstadoCategoriaAsync(item));

            CrearSubcategoriaCommand = new Command(
                async () => await CrearSubcategoriaAsync());

            EditarSubcategoriaCommand = new Command(
                async () => await EditarSubcategoriaAsync());

            CambiarEstadoSubcategoriaCommand = new Command(
                async () => await CambiarEstadoSubcategoriaAsync());

            AgregarRegistroCommand = new Command(
                async () => await AgregarRegistroAsync());

            AbrirDetalleCommand =
                new Command<AlbumGaleriaJerarquiaItemResponse>(
                    async item => await AbrirDetalleAsync(item));

            EditarRegistroCommand =
                new Command<AlbumGaleriaJerarquiaItemResponse>(
                    async item => await EditarRegistroAsync(item));

            CambiarEstadoRegistroCommand =
                new Command<AlbumGaleriaJerarquiaItemResponse>(
                    async item => await CambiarEstadoRegistroAsync(item));
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(MostrarAdministracion));
            OnPropertyChanged(nameof(MostrarInactivos));
            OnPropertyChanged(nameof(PuedeAdministrarSubcategorias));
            OnPropertyChanged(nameof(PuedeEditarSubcategoria));

            if (!MostrarInactivos)
                IncluirInactivos = false;
        }

        public Task AsegurarCargaAsync(bool showIndicator)
        {
            bool necesitaRecargar =
                !cargadoInicialmente ||
                versionCargada != AlbumBotanicoRefreshState.VersionActual;

            return necesitaRecargar
                ? LoadAsync(showIndicator)
                : Task.CompletedTask;
        }

        public void CancelarConsultas()
        {
            CancellationTokenSource? anterior = consultaCts;
            consultaCts = null;

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

        public async Task LoadAsync(bool showIndicator)
        {
            if (!CanView || cargando)
                return;

            cargando = true;

            if (showIndicator)
                IsBusy = true;

            CancellationTokenSource cts = RenovarConsulta();
            CancellationToken token = cts.Token;

            try
            {
                int? categoriaId =
                    CategoriaSeleccionada?.CategoriaAlbumBotanicoId;
                int? subcategoriaId =
                    SubcategoriaSeleccionada?.SubcategoriaAlbumBotanicoId;

                bool cargaInicialMinima =
                    categoriaId == null &&
                    subcategoriaId == null &&
                    !IncluirInactivos &&
                    string.IsNullOrWhiteSpace(TextoBusqueda);

                if (cargaInicialMinima)
                {
                    ApiResult<AlbumInicioJerarquiaResponse> result =
                        await jerarquiaApiService.GetInicioAsync(
                            TamanoPagina,
                            token);

                    if (token.IsCancellationRequested)
                        return;

                    if (!result.Success || result.Data == null)
                    {
                        await MostrarToastAsync(result.Message);
                        return;
                    }

                    AplicarCategorias(
                        result.Data.Categorias,
                        selectedId: null);

                    AplicarSubcategorias(
                        result.Data.Subcategorias,
                        selectedId: null,
                        categoriaId: null);

                    AplicarPagina(
                        result.Data.Galeria,
                        reemplazar: true);
                }
                else
                {
                    Task<ApiResult<List<CategoriaAlbumBotanicoResponse>>>
                        categoriasTask = apiService.GetCategoriasAsync(
                            IncluirInactivos,
                            token);

                    Task<ApiResult<List<SubcategoriaAlbumBotanicoResponse>>>
                        subcategoriasTask =
                            jerarquiaApiService.GetSubcategoriasAsync(
                                categoriaId,
                                IncluirInactivos,
                                token);

                    Task<ApiResult<AlbumGaleriaJerarquiaPaginaResponse>>
                        paginaTask = jerarquiaApiService.GetPaginaAsync(
                            categoriaId,
                            subcategoriaId,
                            TextoBusqueda,
                            IncluirInactivos,
                            pagina: 1,
                            tamanoPagina: TamanoPagina,
                            cancellationToken: token);

                    await Task.WhenAll(
                        categoriasTask,
                        subcategoriasTask,
                        paginaTask);

                    if (token.IsCancellationRequested)
                        return;

                    ApiResult<List<CategoriaAlbumBotanicoResponse>>
                        categoriasResult = await categoriasTask;
                    ApiResult<List<SubcategoriaAlbumBotanicoResponse>>
                        subcategoriasResult = await subcategoriasTask;
                    ApiResult<AlbumGaleriaJerarquiaPaginaResponse>
                        paginaResult = await paginaTask;

                    if (!categoriasResult.Success)
                    {
                        await MostrarToastAsync(categoriasResult.Message);
                        return;
                    }

                    if (!subcategoriasResult.Success)
                    {
                        await MostrarToastAsync(subcategoriasResult.Message);
                        return;
                    }

                    if (!paginaResult.Success || paginaResult.Data == null)
                    {
                        await MostrarToastAsync(paginaResult.Message);
                        return;
                    }

                    AplicarCategorias(
                        categoriasResult.Data ?? [],
                        categoriaId);

                    if (categoriaId.HasValue &&
                        CategoriaSeleccionada == null)
                    {
                        categoriaId = null;
                        subcategoriaId = null;
                    }

                    AplicarSubcategorias(
                        subcategoriasResult.Data ?? [],
                        subcategoriaId,
                        categoriaId);

                    if (subcategoriaId.HasValue &&
                        SubcategoriaSeleccionada == null)
                    {
                        paginaResult =
                            await jerarquiaApiService.GetPaginaAsync(
                                categoriaId,
                                null,
                                TextoBusqueda,
                                IncluirInactivos,
                                pagina: 1,
                                tamanoPagina: TamanoPagina,
                                cancellationToken: token);

                        if (!paginaResult.Success ||
                            paginaResult.Data == null)
                        {
                            await MostrarToastAsync(paginaResult.Message);
                            return;
                        }
                    }

                    AplicarPagina(paginaResult.Data, reemplazar: true);
                }

                cargadoInicialmente = true;
                versionCargada = AlbumBotanicoRefreshState.VersionActual;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        public Task BuscarAsync() => CargarPrimeraPaginaAsync(true);

        public async Task LimpiarBusquedaAsync()
        {
            TextoBusqueda = string.Empty;
            await CargarPrimeraPaginaAsync(true);
        }

        public async Task AplicarInactivosAsync()
        {
            if (!MostrarInactivos)
            {
                IncluirInactivos = false;
                return;
            }

            CategoriaSeleccionada = null;
            SubcategoriaSeleccionada = null;
            await LoadAsync(true);
        }

        private async Task RefreshAsync()
        {
            if (cargando)
                return;

            IsRefreshing = true;

            try
            {
                await LoadAsync(false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarPrimeraPaginaAsync(bool showIndicator)
        {
            if (!CanView || cargando)
                return;

            cargando = true;

            if (showIndicator)
                IsBusy = true;

            CancellationTokenSource cts = RenovarConsulta();
            CancellationToken token = cts.Token;

            try
            {
                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await jerarquiaApiService.GetPaginaAsync(
                        CategoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
                        SubcategoriaSeleccionada?
                            .SubcategoriaAlbumBotanicoId,
                        TextoBusqueda,
                        IncluirInactivos,
                        pagina: 1,
                        tamanoPagina: TamanoPagina,
                        cancellationToken: token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                AplicarPagina(result.Data, reemplazar: true);
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        private async Task CargarMasAsync()
        {
            if (!CanView ||
                IsBusy ||
                cargando ||
                IsLoadingMore ||
                !TieneMas)
            {
                return;
            }

            IsLoadingMore = true;

            CancellationTokenSource cts =
                consultaCts ??= new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await jerarquiaApiService.GetPaginaAsync(
                        CategoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
                        SubcategoriaSeleccionada?
                            .SubcategoriaAlbumBotanicoId,
                        TextoBusqueda,
                        IncluirInactivos,
                        pagina: paginaActual + 1,
                        tamanoPagina: TamanoPagina,
                        cancellationToken: token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                AplicarPagina(result.Data, reemplazar: false);
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private void AplicarCategorias(
            IEnumerable<CategoriaAlbumBotanicoResponse> items,
            int? selectedId)
        {
            Categorias = new ObservableCollection<
                CategoriaAlbumBotanicoResponse>(items);

            CategoriaSeleccionada = selectedId.HasValue
                ? Categorias.FirstOrDefault(item =>
                    item.CategoriaAlbumBotanicoId == selectedId.Value)
                : null;

            MarcarCategoriaSeleccionada();
        }

        private void AplicarSubcategorias(
            IEnumerable<SubcategoriaAlbumBotanicoResponse> items,
            int? selectedId,
            int? categoriaId)
        {
            IEnumerable<SubcategoriaAlbumBotanicoResponse> filtradas =
                categoriaId.HasValue
                    ? items.Where(item =>
                        item.CategoriaAlbumBotanicoId == categoriaId.Value)
                    : [];

            Subcategorias = new ObservableCollection<
                SubcategoriaAlbumBotanicoResponse>(filtradas);

            SubcategoriaSeleccionada = selectedId.HasValue
                ? Subcategorias.FirstOrDefault(item =>
                    item.SubcategoriaAlbumBotanicoId == selectedId.Value)
                : null;

            MarcarSubcategoriaSeleccionada();
        }

        private void AplicarPagina(
            AlbumGaleriaJerarquiaPaginaResponse pagina,
            bool reemplazar)
        {
            if (reemplazar)
            {
                Registros = new ObservableCollection<
                    AlbumGaleriaJerarquiaItemResponse>(pagina.Items);
            }
            else
            {
                HashSet<int> idsExistentes = Registros
                    .Select(item => item.AlbumBotanicoCafeId)
                    .ToHashSet();

                foreach (AlbumGaleriaJerarquiaItemResponse item in pagina.Items)
                {
                    if (idsExistentes.Add(item.AlbumBotanicoCafeId))
                        Registros.Add(item);
                }
            }

            paginaActual = pagina.PaginaActual;
            totalRegistros = pagina.TotalRegistros;
            tieneMas = pagina.TieneMas;
            NotificarEstadoGaleria();
        }

        private void NotificarEstadoGaleria()
        {
            OnPropertyChanged(nameof(HayRegistros));
            OnPropertyChanged(nameof(SinRegistros));
            OnPropertyChanged(nameof(TieneMas));
            OnPropertyChanged(nameof(TotalRegistrosTexto));
        }

        private CancellationTokenSource RenovarConsulta()
        {
            CancelarConsultas();
            consultaCts = new CancellationTokenSource();
            return consultaCts;
        }

        private async Task SeleccionarCategoriaAsync(
            CategoriaAlbumBotanicoResponse? item)
        {
            if (IsBusy || cargando)
                return;

            CategoriaSeleccionada = item;
            SubcategoriaSeleccionada = null;
            MarcarCategoriaSeleccionada();

            ApiResult<List<SubcategoriaAlbumBotanicoResponse>> resultado =
                await jerarquiaApiService.GetSubcategoriasAsync(
                    item?.CategoriaAlbumBotanicoId,
                    IncluirInactivos);

            if (!resultado.Success)
            {
                await MostrarToastAsync(resultado.Message);
                return;
            }

            AplicarSubcategorias(
                resultado.Data ?? [],
                selectedId: null,
                item?.CategoriaAlbumBotanicoId);

            await CargarPrimeraPaginaAsync(true);
        }

        private async Task SeleccionarSubcategoriaAsync(
            SubcategoriaAlbumBotanicoResponse? item)
        {
            if (IsBusy || cargando)
                return;

            SubcategoriaSeleccionada = item;
            MarcarSubcategoriaSeleccionada();
            await CargarPrimeraPaginaAsync(true);
        }

        private void MarcarSubcategoriaSeleccionada()
        {
            foreach (SubcategoriaAlbumBotanicoResponse subcategoria in
                Subcategorias)
            {
                subcategoria.IsSelected =
                    SubcategoriaSeleccionada != null &&
                    subcategoria.SubcategoriaAlbumBotanicoId ==
                        SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId;
            }

            OnPropertyChanged(nameof(TodasSubcategoriasSeleccionada));
            OnPropertyChanged(nameof(FondoTodasSubcategorias));
            OnPropertyChanged(nameof(TextoTodasSubcategorias));
            OnPropertyChanged(nameof(BordeTodasSubcategorias));
        }

        private void MarcarCategoriaSeleccionada()
        {
            foreach (CategoriaAlbumBotanicoResponse categoria in Categorias)
            {
                categoria.IsSelected =
                    CategoriaSeleccionada != null &&
                    categoria.CategoriaAlbumBotanicoId ==
                        CategoriaSeleccionada.CategoriaAlbumBotanicoId;
            }

            OnPropertyChanged(nameof(TodasSeleccionada));
            OnPropertyChanged(nameof(FondoTodas));
            OnPropertyChanged(nameof(TextoTodas));
            OnPropertyChanged(nameof(BordeTodas));
        }

        private async Task AgregarCategoriaAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para crear categorías.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.CategoriaAlbumFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new CategoriaAlbumBotanicoRequest()
                });
        }

        private async Task EditarCategoriaAsync(
            CategoriaAlbumBotanicoResponse? item)
        {
            if (item == null)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar categorías.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.CategoriaAlbumFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Item"] = new CategoriaAlbumBotanicoRequest(item)
                });
        }

        private async Task CambiarEstadoCategoriaAsync(
            CategoriaAlbumBotanicoResponse? item)
        {
            if (item == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para cambiar el estado de categorías.");
                return;
            }

            bool nuevoEstado = !item.Activo;
            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                nuevoEstado ? "Activar categoría" : "Desactivar categoría",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{item.NombreCategoria}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                ApiResult<bool> result =
                    await apiService.CambiarEstadoCategoriaAsync(
                        item.CategoriaAlbumBotanicoId,
                        nuevoEstado);

                if (!result.Success)
                {
                    await page.DisplayAlert(
                        "No fue posible",
                        result.Message,
                        "Aceptar");
                    return;
                }

                AlbumBotanicoRefreshState.MarcarCambio();
                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CrearSubcategoriaAsync()
        {
            if (!CanAdd || CategoriaSeleccionada == null ||
                Application.Current?.MainPage is not Page page)
            {
                return;
            }

            string? nombre = await page.DisplayPromptAsync(
                "Nueva subcategoría",
                $"Se creará dentro de {CategoriaSeleccionada.NombreCategoria}.",
                "Crear",
                "Cancelar",
                "Ejemplo: Insectos",
                120,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
                return;

            string? descripcion = await page.DisplayPromptAsync(
                "Descripción",
                "Descripción opcional de la subcategoría.",
                "Continuar",
                "Omitir",
                "Opcional",
                600,
                Keyboard.Text);

            ApiResult<SubcategoriaAlbumBotanicoResponse> result =
                await jerarquiaApiService.CrearSubcategoriaAsync(
                    new GuardarSubcategoriaAlbumRequest
                    {
                        CategoriaAlbumBotanicoId =
                            CategoriaSeleccionada.CategoriaAlbumBotanicoId,
                        NombreSubcategoria = nombre.Trim(),
                        Descripcion = descripcion?.Trim()
                    });

            if (!result.Success)
            {
                await page.DisplayAlert(
                    "No fue posible",
                    result.Message,
                    "Aceptar");
                return;
            }

            AlbumBotanicoRefreshState.MarcarCambio();
            await MostrarToastAsync(result.Message);
            await SeleccionarCategoriaAsync(CategoriaSeleccionada);
        }

        private async Task EditarSubcategoriaAsync()
        {
            if (!CanEdit || SubcategoriaSeleccionada == null ||
                Application.Current?.MainPage is not Page page)
            {
                return;
            }

            string? nombre = await page.DisplayPromptAsync(
                "Editar subcategoría",
                "Actualice el nombre.",
                "Guardar",
                "Cancelar",
                initialValue: SubcategoriaSeleccionada.NombreSubcategoria,
                maxLength: 120,
                keyboard: Keyboard.Text);

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
                return;

            int id = SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId;

            ApiResult<bool> result =
                await jerarquiaApiService.ActualizarSubcategoriaAsync(
                    id,
                    new GuardarSubcategoriaAlbumRequest
                    {
                        CategoriaAlbumBotanicoId =
                            SubcategoriaSeleccionada.CategoriaAlbumBotanicoId,
                        NombreSubcategoria = nombre.Trim(),
                        Descripcion = SubcategoriaSeleccionada.Descripcion
                    });

            if (!result.Success)
            {
                await page.DisplayAlert(
                    "No fue posible",
                    result.Message,
                    "Aceptar");
                return;
            }

            AlbumBotanicoRefreshState.MarcarCambio();
            await MostrarToastAsync(result.Message);
            await SeleccionarCategoriaAsync(CategoriaSeleccionada);
        }

        private async Task CambiarEstadoSubcategoriaAsync()
        {
            if (!CanDelete || SubcategoriaSeleccionada == null ||
                Application.Current?.MainPage is not Page page)
            {
                return;
            }

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

            ApiResult<bool> result =
                await jerarquiaApiService.CambiarEstadoSubcategoriaAsync(
                    SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId,
                    nuevoEstado);

            if (!result.Success)
            {
                await page.DisplayAlert(
                    "No fue posible",
                    result.Message,
                    "Aceptar");
                return;
            }

            AlbumBotanicoRefreshState.MarcarCambio();
            await MostrarToastAsync(result.Message);
            await SeleccionarCategoriaAsync(CategoriaSeleccionada);
        }

        private async Task AgregarRegistroAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para crear registros.");
                return;
            }

            List<CategoriaAlbumBotanicoResponse> categoriasActivas =
                Categorias.Where(item => item.Activo).ToList();

            if (categoriasActivas.Count == 0)
            {
                await MostrarToastAsync(
                    "Debe crear o activar una categoría antes de agregar una ficha.");
                return;
            }

            int categoriaId = CategoriaSeleccionada?.Activo == true
                ? CategoriaSeleccionada.CategoriaAlbumBotanicoId
                : categoriasActivas[0].CategoriaAlbumBotanicoId;

            await GoToAsyncParameters(
                AppRoutes.AlbumRegistroFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["RegistroId"] = 0,
                    ["CategoriaId"] = categoriaId
                });
        }

        private async Task AbrirDetalleAsync(
            AlbumGaleriaJerarquiaItemResponse? item)
        {
            if (item == null)
                return;

            await GoToAsyncParameters(
                AppRoutes.AlbumDetalle,
                new Dictionary<string, object>
                {
                    ["RegistroId"] = item.AlbumBotanicoCafeId
                });
        }

        private async Task EditarRegistroAsync(
            AlbumGaleriaJerarquiaItemResponse? item)
        {
            if (item == null)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar fichas.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AlbumRegistroFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["RegistroId"] = item.AlbumBotanicoCafeId,
                    ["CategoriaId"] = item.CategoriaAlbumBotanicoId
                });
        }

        private async Task CambiarEstadoRegistroAsync(
            AlbumGaleriaJerarquiaItemResponse? item)
        {
            if (item == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para cambiar el estado de fichas.");
                return;
            }

            bool nuevoEstado = !item.Activo;
            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                nuevoEstado ? "Activar ficha" : "Desactivar ficha",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{item.Titulo}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                ApiResult<bool> result =
                    await apiService.CambiarEstadoRegistroAsync(
                        item.AlbumBotanicoCafeId,
                        nuevoEstado);

                if (!result.Success)
                {
                    await page.DisplayAlert(
                        "No fue posible",
                        result.Message,
                        "Aceptar");
                    return;
                }

                AlbumBotanicoRefreshState.MarcarCambio();
                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
