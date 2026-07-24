using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumFotosViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private readonly AlbumBotanicoCargaApiService cargaApiService = new();

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private ObservableCollection<AlbumGaleriaItemResponse>
            registros = new();

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
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

        public ObservableCollection<AlbumGaleriaItemResponse> Registros
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
        public bool HayRegistros => Registros.Count > 0;
        public bool SinRegistros => !HayRegistros;
        public bool TieneMas => tieneMas;

        public bool TodasSeleccionada =>
            CategoriaSeleccionada == null;

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

        public string TituloGaleria =>
            CategoriaSeleccionada == null
                ? "Galería completa"
                : CategoriaSeleccionada.NombreCategoria;

        public string TotalRegistrosTexto
        {
            get
            {
                if (totalRegistros == 0)
                    return "0 registros encontrados";

                if (Registros.Count >= totalRegistros)
                {
                    return totalRegistros == 1
                        ? "1 registro encontrado"
                        : $"{totalRegistros} registros encontrados";
                }

                return $"{Registros.Count} de {totalRegistros} registros";
            }
        }

        public Command CargarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Command SeleccionarTodasCommand { get; }
        public Command CargarMasCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            SeleccionarCategoriaCommand { get; }

        public Command AgregarCategoriaCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            EditarCategoriaCommand { get; }

        public Command<CategoriaAlbumBotanicoResponse>
            CambiarEstadoCategoriaCommand { get; }

        public Command AgregarRegistroCommand { get; }

        public Command<AlbumGaleriaItemResponse>
            AbrirDetalleCommand { get; }

        public Command<AlbumGaleriaItemResponse>
            EditarRegistroCommand { get; }

        public Command<AlbumGaleriaItemResponse>
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

            CargarMasCommand = new Command(
                async () => await CargarMasAsync());

            SeleccionarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await SeleccionarCategoriaAsync(item));

            AgregarCategoriaCommand = new Command(
                async () => await AgregarCategoriaAsync());

            EditarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await EditarCategoriaAsync(item));

            CambiarEstadoCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await CambiarEstadoCategoriaAsync(item));

            AgregarRegistroCommand = new Command(
                async () => await AgregarRegistroAsync());

            AbrirDetalleCommand =
                new Command<AlbumGaleriaItemResponse>(
                    async item => await AbrirDetalleAsync(item));

            EditarRegistroCommand =
                new Command<AlbumGaleriaItemResponse>(
                    async item => await EditarRegistroAsync(item));

            CambiarEstadoRegistroCommand =
                new Command<AlbumGaleriaItemResponse>(
                    async item =>
                        await CambiarEstadoRegistroAsync(item));
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("albumFotosPage");
            OnPropertyChanged(nameof(MostrarAdministracion));
            OnPropertyChanged(nameof(MostrarInactivos));

            if (!MostrarInactivos)
                IncluirInactivos = false;
        }

        public Task AsegurarCargaAsync(bool showIndicator)
        {
            bool necesitaRecargar =
                !cargadoInicialmente ||
                versionCargada !=
                    AlbumBotanicoRefreshState.VersionActual;

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
                int? selectedId =
                    CategoriaSeleccionada?
                        .CategoriaAlbumBotanicoId;

                bool cargaInicialMinima =
                    selectedId == null &&
                    !IncluirInactivos &&
                    string.IsNullOrWhiteSpace(TextoBusqueda);

                if (cargaInicialMinima)
                {
                    ApiResult<AlbumInicioResponse> result =
                        await cargaApiService.GetInicioAsync(
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

                    Task<ApiResult<AlbumGaleriaPaginaResponse>>
                        paginaTask = cargaApiService.GetPaginaAsync(
                            selectedId,
                            TextoBusqueda,
                            IncluirInactivos,
                            pagina: 1,
                            tamanoPagina: TamanoPagina,
                            cancellationToken: token);

                    await Task.WhenAll(
                        categoriasTask,
                        paginaTask);

                    if (token.IsCancellationRequested)
                        return;

                    ApiResult<List<CategoriaAlbumBotanicoResponse>>
                        categoriasResult = await categoriasTask;

                    ApiResult<AlbumGaleriaPaginaResponse>
                        paginaResult = await paginaTask;

                    if (!categoriasResult.Success)
                    {
                        await MostrarToastAsync(
                            categoriasResult.Message);
                        return;
                    }

                    if (!paginaResult.Success ||
                        paginaResult.Data == null)
                    {
                        await MostrarToastAsync(
                            paginaResult.Message);
                        return;
                    }

                    AplicarCategorias(
                        categoriasResult.Data ?? new(),
                        selectedId);

                    if (selectedId.HasValue &&
                        CategoriaSeleccionada == null)
                    {
                        paginaResult =
                            await cargaApiService.GetPaginaAsync(
                                null,
                                TextoBusqueda,
                                IncluirInactivos,
                                pagina: 1,
                                tamanoPagina: TamanoPagina,
                                cancellationToken: token);

                        if (token.IsCancellationRequested)
                            return;

                        if (!paginaResult.Success ||
                            paginaResult.Data == null)
                        {
                            await MostrarToastAsync(
                                paginaResult.Message);
                            return;
                        }
                    }

                    AplicarPagina(
                        paginaResult.Data,
                        reemplazar: true);
                }

                cargadoInicialmente = true;
                versionCargada =
                    AlbumBotanicoRefreshState.VersionActual;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        public Task BuscarAsync() =>
            CargarPrimeraPaginaAsync(true);

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

        private async Task CargarPrimeraPaginaAsync(
            bool showIndicator)
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
                ApiResult<AlbumGaleriaPaginaResponse> result =
                    await cargaApiService.GetPaginaAsync(
                        CategoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
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

                AplicarPagina(
                    result.Data,
                    reemplazar: true);
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
                ApiResult<AlbumGaleriaPaginaResponse> result =
                    await cargaApiService.GetPaginaAsync(
                        CategoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
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

                AplicarPagina(
                    result.Data,
                    reemplazar: false);
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
                ? Categorias.FirstOrDefault(x =>
                    x.CategoriaAlbumBotanicoId == selectedId.Value)
                : null;

            MarcarCategoriaSeleccionada();
        }

        private void AplicarPagina(
            AlbumGaleriaPaginaResponse pagina,
            bool reemplazar)
        {
            if (reemplazar)
            {
                Registros = new ObservableCollection<
                    AlbumGaleriaItemResponse>(pagina.Items);
            }
            else
            {
                HashSet<int> idsExistentes = Registros
                    .Select(x => x.AlbumBotanicoCafeId)
                    .ToHashSet();

                foreach (AlbumGaleriaItemResponse item in pagina.Items)
                {
                    if (idsExistentes.Add(
                            item.AlbumBotanicoCafeId))
                    {
                        Registros.Add(item);
                    }
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
            MarcarCategoriaSeleccionada();
            await CargarPrimeraPaginaAsync(true);
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

        private async Task AgregarRegistroAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para crear registros.");
                return;
            }

            List<CategoriaAlbumBotanicoResponse> categoriasActivas =
                Categorias.Where(x => x.Activo).ToList();

            if (categoriasActivas.Count == 0)
            {
                await MostrarToastAsync(
                    "Debe crear o activar una categoría antes de agregar un registro.");
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
            AlbumGaleriaItemResponse? item)
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
            AlbumGaleriaItemResponse? item)
        {
            if (item == null)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar registros.");
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
            AlbumGaleriaItemResponse? item)
        {
            if (item == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para cambiar el estado de registros.");
                return;
            }

            bool nuevoEstado = !item.Activo;
            Page? page = Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                nuevoEstado ? "Activar registro" : "Desactivar registro",
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
