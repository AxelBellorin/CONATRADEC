using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumFotosViewModel : GlobalService
    {
        private readonly AlbumBotanicoApiService apiService = new();
        private ObservableCollection<CategoriaAlbumBotanicoResponse>
            categorias = new();
        private ObservableCollection<AlbumGaleriaItemResponse>
            registros = new();
        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private string textoBusqueda = string.Empty;
        private bool incluirInactivos;
        private bool isRefreshing;
        private bool cargando;

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

        public ObservableCollection<AlbumGaleriaItemResponse>
            Registros
        {
            get => registros;
            private set
            {
                registros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HayRegistros));
                OnPropertyChanged(nameof(SinRegistros));
                OnPropertyChanged(nameof(TotalRegistrosTexto));
            }
        }

        public CategoriaAlbumBotanicoResponse?
            CategoriaSeleccionada
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

        public bool HayCategorias => Categorias.Count > 0;
        public bool HayRegistros => Registros.Count > 0;
        public bool SinRegistros => !HayRegistros;
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

        public string TotalRegistrosTexto =>
            Registros.Count == 1
                ? "1 registro encontrado"
                : $"{Registros.Count} registros encontrados";

        public Command CargarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Command SeleccionarTodasCommand { get; }
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
            CargarCommand =
                new Command(async () => await LoadAsync(true));

            RefrescarCommand =
                new Command(async () => await RefreshAsync());

            BuscarCommand =
                new Command(async () =>
                    await BuscarAsync());

            LimpiarBusquedaCommand =
                new Command(async () =>
                    await LimpiarBusquedaAsync());

            SeleccionarTodasCommand =
                new Command(async () =>
                    await SeleccionarCategoriaAsync(null));

            SeleccionarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await SeleccionarCategoriaAsync(item));

            AgregarCategoriaCommand =
                new Command(async () =>
                    await AgregarCategoriaAsync());

            EditarCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await EditarCategoriaAsync(item));

            CambiarEstadoCategoriaCommand =
                new Command<CategoriaAlbumBotanicoResponse>(
                    async item =>
                        await CambiarEstadoCategoriaAsync(item));

            AgregarRegistroCommand =
                new Command(async () =>
                    await AgregarRegistroAsync());

            AbrirDetalleCommand =
                new Command<AlbumGaleriaItemResponse>(
                    async item =>
                        await AbrirDetalleAsync(item));

            EditarRegistroCommand =
                new Command<AlbumGaleriaItemResponse>(
                    async item =>
                        await EditarRegistroAsync(item));

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

        public async Task LoadAsync(bool showIndicator)
        {
            if (!CanView || cargando)
                return;

            cargando = true;

            if (showIndicator)
                IsBusy = true;

            try
            {
                int? selectedId =
                    CategoriaSeleccionada?
                        .CategoriaAlbumBotanicoId;

                var categoryResult =
                    await apiService.GetCategoriasAsync(
                        IncluirInactivos);

                if (!categoryResult.Success)
                {
                    await MostrarToastAsync(
                        categoryResult.Message);
                    return;
                }

                Categorias =
                    new ObservableCollection<
                        CategoriaAlbumBotanicoResponse>(
                        categoryResult.Data ??
                        new List<
                            CategoriaAlbumBotanicoResponse>());

                CategoriaSeleccionada =
                    selectedId.HasValue
                        ? Categorias.FirstOrDefault(x =>
                            x.CategoriaAlbumBotanicoId ==
                            selectedId.Value)
                        : null;

                MarcarCategoriaSeleccionada();

                await CargarGaleriaAsync(false);
            }
            finally
            {
                cargando = false;

                if (showIndicator)
                    IsBusy = false;
            }
        }

        public Task BuscarAsync() =>
            CargarGaleriaAsync(true);

        public async Task LimpiarBusquedaAsync()
        {
            TextoBusqueda = string.Empty;
            await CargarGaleriaAsync(true);
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

        private async Task CargarGaleriaAsync(
            bool showIndicator)
        {
            if (!CanView)
                return;

            if (showIndicator)
                IsBusy = true;

            try
            {
                var result =
                    await apiService.GetGaleriaAsync(
                        CategoriaSeleccionada?
                            .CategoriaAlbumBotanicoId,
                        TextoBusqueda,
                        IncluirInactivos);

                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                Registros =
                    new ObservableCollection<
                        AlbumGaleriaItemResponse>(
                        result.Data ??
                        new List<
                            AlbumGaleriaItemResponse>());
            }
            finally
            {
                if (showIndicator)
                    IsBusy = false;
            }
        }

        private async Task SeleccionarCategoriaAsync(
            CategoriaAlbumBotanicoResponse? item)
        {
            if (IsBusy)
                return;

            CategoriaSeleccionada = item;
            MarcarCategoriaSeleccionada();
            await CargarGaleriaAsync(true);
        }

        private void MarcarCategoriaSeleccionada()
        {
            foreach (var categoria in Categorias)
            {
                categoria.IsSelected =
                    CategoriaSeleccionada != null &&
                    categoria.CategoriaAlbumBotanicoId ==
                    CategoriaSeleccionada
                        .CategoriaAlbumBotanicoId;
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
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,
                    ["Item"] =
                        new CategoriaAlbumBotanicoRequest()
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
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,
                    ["Item"] =
                        new CategoriaAlbumBotanicoRequest(item)
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
                nuevoEstado
                    ? "Activar categoría"
                    : "Desactivar categoría",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{item.NombreCategoria}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var result =
                    await apiService
                        .CambiarEstadoCategoriaAsync(
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

            var categoriasActivas =
                Categorias.Where(x => x.Activo).ToList();

            if (categoriasActivas.Count == 0)
            {
                await MostrarToastAsync(
                    "Debe crear o activar una categoría antes de agregar un registro.");
                return;
            }

            int categoriaId =
                CategoriaSeleccionada?.Activo == true
                    ? CategoriaSeleccionada
                        .CategoriaAlbumBotanicoId
                    : categoriasActivas[0]
                        .CategoriaAlbumBotanicoId;

            await GoToAsyncParameters(
                AppRoutes.AlbumRegistroFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,
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
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,
                    ["RegistroId"] = item.AlbumBotanicoCafeId,
                    ["CategoriaId"] =
                        item.CategoriaAlbumBotanicoId
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
                nuevoEstado
                    ? "Activar registro"
                    : "Desactivar registro",
                $"¿Desea {(nuevoEstado ? "activar" : "desactivar")} " +
                $"'{item.Titulo}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                var result =
                    await apiService
                        .CambiarEstadoRegistroAsync(
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

                await MostrarToastAsync(result.Message);
                await CargarGaleriaAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
