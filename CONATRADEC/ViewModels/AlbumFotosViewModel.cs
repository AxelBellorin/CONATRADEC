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
    /// Mantiene una sola página en memoria, conserva el estado durante la
    /// visita y separa el texto que el usuario está escribiendo del filtro que
    /// realmente corresponde a los registros visibles.
    /// </summary>
    public sealed class AlbumFotosViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private readonly AlbumAdministracionApiService administracionApi = new();

        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();

        private ObservableCollection<SubcategoriaAlbumBotanicoResponse>
            subcategorias = new();

        private ObservableCollection<AlbumGaleriaJerarquiaItemResponse>
            registros = new();

        private List<SubcategoriaAlbumBotanicoResponse>
            subcategoriasDisponibles = [];

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private SubcategoriaAlbumBotanicoResponse? subcategoriaSeleccionada;
        private CancellationTokenSource? consultaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private bool isRefreshing;
        private bool cargando;
        private bool cargadoInicialmente;
        private int paginaActual = 1;
        private int totalPaginas;
        private int totalRegistros;
        private long versionCargada = -1;

        private int TamanoPagina =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 12
                : 8;

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

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public int PaginaActual
        {
            get => paginaActual;
            private set
            {
                if (paginaActual == value)
                    return;

                paginaActual = value;
                OnPropertyChanged();
                NotificarPaginacion();
            }
        }

        public int TotalPaginas => Math.Max(1, totalPaginas);
        public int TotalRegistros => totalRegistros;
        public bool PuedeIrAnterior => totalRegistros > 0 && PaginaActual > 1;
        public bool PuedeIrSiguiente =>
            totalRegistros > 0 && PaginaActual < totalPaginas;
        public bool MostrarPaginacion => totalRegistros > 0;
        public string PaginaTexto =>
            $"Página {PaginaActual} de {TotalPaginas}";

        public string RangoPaginaTexto
        {
            get
            {
                if (totalRegistros == 0)
                    return "0 registros";

                int inicio = ((PaginaActual - 1) * TamanoPagina) + 1;
                int fin = Math.Min(
                    inicio + Registros.Count - 1,
                    totalRegistros);

                return $"{inicio}-{fin} de {totalRegistros}";
            }
        }

        public bool HayCategorias => Categorias.Count > 0;
        public bool HaySubcategorias => Subcategorias.Count > 0;
        public bool HayRegistros => Registros.Count > 0;
        public bool SinRegistros => !HayRegistros;

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
            SubcategoriaSeleccionada?.AccionEstadoTexto ?? "Desactivar";

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
            SubcategoriaSeleccionada != null &&
            CanView &&
            (CanEdit || CanDelete);

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
            CanView && (CanAdd || CanEdit || CanDelete);

        public bool MostrarEliminados =>
            CanView && (CanEdit || CanDelete);

        public bool SeHaListado => cargadoInicialmente;

        public bool RequiereRecargaPorCambios =>
            cargadoInicialmente &&
            versionCargada != AlbumBotanicoRefreshState.VersionActual;

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

        public string TotalRegistrosTexto =>
            totalRegistros == 1
                ? "1 ficha encontrada"
                : $"{totalRegistros} fichas encontradas";

        public Command CargarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Command SeleccionarTodasCommand { get; }
        public Command SeleccionarTodasSubcategoriasCommand { get; }

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
                async () => await InicializarAsync());

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
                async () => await AgregarRegistroAsync());

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
            OnPropertyChanged(nameof(MostrarEliminados));
            OnPropertyChanged(nameof(PuedeAdministrarSubcategorias));
            OnPropertyChanged(nameof(PuedeEditarSubcategoria));
        }

        public async Task IniciarNuevaVisitaAsync()
        {
            CancelarConsultas();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            CategoriaSeleccionada = null;
            SubcategoriaSeleccionada = null;
            Categorias = [];
            Subcategorias = [];
            subcategoriasDisponibles = [];
            Registros = [];
            PaginaActual = 1;
            totalPaginas = 0;
            totalRegistros = 0;
            cargadoInicialmente = false;
            versionCargada = -1;
            NotificarPaginacion();

            await CargarContextoAsync(
                pagina: 1,
                showIndicator: true);
        }

        public Task InicializarAsync() =>
            cargadoInicialmente
                ? Task.CompletedTask
                : CargarContextoAsync(
                    pagina: 1,
                    showIndicator: true);

        public Task RecargarContextoActualAsync() =>
            CargarContextoAsync(
                pagina: Math.Max(1, PaginaActual),
                showIndicator: true);

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

        public async Task BuscarAsync()
        {
            textoBusquedaAplicado = TextoBusqueda.Trim();
            await CargarPaginaAsync(1, true);
        }

        public async Task LimpiarBusquedaAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            await CargarPaginaAsync(1, true);
        }

        public async Task<bool> IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return false;

            int anterior = PaginaActual;
            await CargarPaginaAsync(PaginaActual - 1, true);
            return PaginaActual != anterior;
        }

        public async Task<bool> IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return false;

            int anterior = PaginaActual;
            await CargarPaginaAsync(PaginaActual + 1, true);
            return PaginaActual != anterior;
        }

        private async Task RefreshAsync()
        {
            if (cargando)
                return;

            IsRefreshing = true;

            try
            {
                await CargarContextoAsync(
                    Math.Max(1, PaginaActual),
                    showIndicator: false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarContextoAsync(
            int pagina,
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
                int? categoriaId =
                    CategoriaSeleccionada?.CategoriaAlbumBotanicoId;
                int? subcategoriaId =
                    SubcategoriaSeleccionada?.SubcategoriaAlbumBotanicoId;

                ApiResult<AlbumInicioJerarquiaResponse> result =
                    await administracionApi.GetContextoAsync(
                        categoriaId,
                        subcategoriaId,
                        textoBusquedaAplicado,
                        pagina,
                        TamanoPagina,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                AplicarContexto(
                    result.Data,
                    categoriaId,
                    subcategoriaId);

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

        private async Task CargarPaginaAsync(
            int pagina,
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
                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await administracionApi.GetPaginaAsync(
                        CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                        SubcategoriaSeleccionada?.SubcategoriaAlbumBotanicoId,
                        textoBusquedaAplicado,
                        pagina,
                        TamanoPagina,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                AplicarPagina(result.Data);
                cargadoInicialmente = true;
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        private void AplicarContexto(
            AlbumInicioJerarquiaResponse contexto,
            int? categoriaSeleccionadaId,
            int? subcategoriaSeleccionadaId)
        {
            Categorias = new ObservableCollection<
                CategoriaAlbumBotanicoResponse>(contexto.Categorias);

            subcategoriasDisponibles = contexto.Subcategorias.ToList();

            CategoriaSeleccionada = categoriaSeleccionadaId.HasValue
                ? Categorias.FirstOrDefault(item =>
                    item.CategoriaAlbumBotanicoId ==
                        categoriaSeleccionadaId.Value)
                : null;

            int? categoriaAplicadaId =
                CategoriaSeleccionada?.CategoriaAlbumBotanicoId;

            AplicarSubcategorias(
                subcategoriaSeleccionadaId,
                categoriaAplicadaId);

            MarcarCategoriaSeleccionada();
            AplicarPagina(contexto.Galeria);

            AlbumBotanicoVisitaService.GuardarCatalogos(
                Categorias,
                subcategoriasDisponibles);
        }

        private void AplicarSubcategorias(
            int? selectedId,
            int? categoriaId)
        {
            IEnumerable<SubcategoriaAlbumBotanicoResponse> filtradas =
                categoriaId.HasValue
                    ? subcategoriasDisponibles.Where(item =>
                        item.CategoriaAlbumBotanicoId == categoriaId.Value &&
                        item.Activo)
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
            AlbumGaleriaJerarquiaPaginaResponse pagina)
        {
            Registros = new ObservableCollection<
                AlbumGaleriaJerarquiaItemResponse>(pagina.Items);

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(0, pagina.TotalPaginas);
            totalRegistros = Math.Max(0, pagina.TotalRegistros);

            OnPropertyChanged(nameof(PaginaActual));
            NotificarEstadoGaleria();
            NotificarPaginacion();
        }

        private void NotificarEstadoGaleria()
        {
            OnPropertyChanged(nameof(HayRegistros));
            OnPropertyChanged(nameof(SinRegistros));
            OnPropertyChanged(nameof(TotalRegistros));
            OnPropertyChanged(nameof(TotalRegistrosTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
        }

        private void NotificarPaginacion()
        {
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
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

            int? nuevaId = item?.CategoriaAlbumBotanicoId;
            int? actualId = CategoriaSeleccionada?.CategoriaAlbumBotanicoId;

            if (nuevaId == actualId &&
                SubcategoriaSeleccionada == null &&
                PaginaActual == 1)
            {
                return;
            }

            CategoriaSeleccionada = item;
            SubcategoriaSeleccionada = null;
            MarcarCategoriaSeleccionada();

            AplicarSubcategorias(
                selectedId: null,
                item?.CategoriaAlbumBotanicoId);

            await CargarPaginaAsync(1, true);
        }

        private async Task SeleccionarSubcategoriaAsync(
            SubcategoriaAlbumBotanicoResponse? item)
        {
            if (IsBusy || cargando)
                return;

            int? nuevaId = item?.SubcategoriaAlbumBotanicoId;
            int? actualId =
                SubcategoriaSeleccionada?.SubcategoriaAlbumBotanicoId;

            if (nuevaId == actualId && PaginaActual == 1)
                return;

            SubcategoriaSeleccionada = item;
            MarcarSubcategoriaSeleccionada();
            await CargarPaginaAsync(1, true);
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
            if (!CanView || !CanAdd)
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

            if (!CanView || !CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar categorías.");
                return;
            }

            if (IsBusy || cargando)
                return;

            IsBusy = true;
            CancellationTokenSource cts = RenovarConsulta();
            CancellationToken token = cts.Token;

            try
            {
                ApiResult<CategoriaAlbumBotanicoResponse> result =
                    await apiService.GetCategoriaAsync(
                        item.CategoriaAlbumBotanicoId,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                await GoToAsyncParameters(
                    AppRoutes.CategoriaAlbumFormulario,
                    new Dictionary<string, object>
                    {
                        ["Mode"] = FormMode.FormModeSelect.Edit,
                        ["Item"] =
                            new CategoriaAlbumBotanicoRequest(result.Data)
                    });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CambiarEstadoCategoriaAsync(
            CategoriaAlbumBotanicoResponse? item)
        {
            if (item == null || IsBusy)
                return;

            if (!CanView || !CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para desactivar categorías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Desactivar categoría",
                $"¿Desea desactivar '{item.NombreCategoria}'? " +
                "La categoría dejará de mostrarse en el listado activo.",
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
                        false);

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
                await CargarContextoAsync(PaginaActual, false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EditarSubcategoriaAsync()
        {
            if (SubcategoriaSeleccionada == null)
                return;

            if (!CanView || !CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar subcategorías.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AlbumRegistroFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["RegistroId"] =
                        SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId,
                    ["CategoriaId"] =
                        SubcategoriaSeleccionada.CategoriaAlbumBotanicoId
                });
        }

        private async Task CambiarEstadoSubcategoriaAsync()
        {
            if (SubcategoriaSeleccionada == null || IsBusy)
                return;

            var item = new AlbumGaleriaJerarquiaItemResponse
            {
                AlbumBotanicoCafeId =
                    SubcategoriaSeleccionada.SubcategoriaAlbumBotanicoId,
                CategoriaAlbumBotanicoId =
                    SubcategoriaSeleccionada.CategoriaAlbumBotanicoId,
                Categoria = SubcategoriaSeleccionada.Categoria,
                Titulo = SubcategoriaSeleccionada.NombreSubcategoria,
                Activo = true
            };

            await CambiarEstadoRegistroAsync(item);
        }

        private async Task AgregarRegistroAsync()
        {
            if (!CanView || !CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para crear subcategorías.");
                return;
            }

            List<CategoriaAlbumBotanicoResponse> categoriasActivas =
                Categorias.Where(item => item.Activo).ToList();

            if (categoriasActivas.Count == 0)
            {
                await MostrarToastAsync(
                    "Debe crear o activar una categoría antes de agregar una subcategoría.");
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
            if (item == null || !CanView)
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

            if (!CanView || !CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar subcategorías.");
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

            if (!CanView || !CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para desactivar subcategorías.");
                return;
            }

            Page? page = Application.Current?.MainPage;
            if (page == null)
                return;

            bool confirm = await page.DisplayAlert(
                "Desactivar subcategoría",
                $"¿Desea desactivar '{item.Titulo}'? " +
                "Podrá reactivarla desde Elementos eliminados.",
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
                        false);

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
                await CargarContextoAsync(PaginaActual, false);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
