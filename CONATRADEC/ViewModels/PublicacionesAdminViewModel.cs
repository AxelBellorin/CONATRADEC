using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class PublicacionesAdminViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();
        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string estadoSeleccionado = "TODOS";
        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoMas;
        private bool cargado;
        private bool ultimaCargaExitosa;
        private long versionAplicada = -1;
        private bool categoriasCargadas;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;
        private CancellationTokenSource? cargaCancellationTokenSource;

        public PublicacionesAdminViewModel()
        {
            Categorias = new ObservableCollection<
                CategoriaPublicacionResponse>();

            Estados = new ObservableCollection<string>
            {
                "TODOS",
                "BORRADOR",
                "PUBLICADA",
                "PROGRAMADA",
                "VENCIDA",
                "ARCHIVADA"
            };

            Publicaciones = new ObservableCollection<
                PublicacionListadoResponse>();

            BuscarCommand = new Command(
                async () => await CargarAsync(true),
                () => !IsBusy && CanAdministrar);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && CanAdministrar);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => !IsBusy && CanAdministrar);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && !CargandoMas &&
                      PuedeCargarMas && CanAdministrar);

            NuevaCommand = new Command(
                async () => await NuevaAsync(),
                () => !IsBusy && CanAdd);

            EditarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EditarAsync(item),
                    item => !IsBusy && CanEdit && item != null);

            CambiarEstadoCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await CambiarEstadoAsync(item),
                    item => !IsBusy && CanEdit && item != null);

            CambiarDestacadaCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await CambiarDestacadaAsync(item),
                    item => !IsBusy && CanEdit && item != null);

            EliminarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EliminarAsync(item),
                    item => !IsBusy && CanDelete && item != null);

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar),
                () => !IsBusy);
        }

        public ObservableCollection<CategoriaPublicacionResponse>
            Categorias { get; }

        public ObservableCollection<string> Estados { get; }

        public ObservableCollection<PublicacionListadoResponse>
            Publicaciones { get; }

        public CategoriaPublicacionResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                string nuevo = string.IsNullOrWhiteSpace(value)
                    ? "TODOS"
                    : value;

                if (estadoSeleccionado == nuevo)
                    return;

                estadoSeleccionado = nuevo;
                OnPropertyChanged();
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

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                ActualizarComandos();
                NotificarLista();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public bool CargandoMas
        {
            get => cargandoMas;
            private set
            {
                if (cargandoMas == value)
                    return;

                cargandoMas = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public bool TienePublicaciones => Publicaciones.Count > 0;

        public bool MostrarVacio =>
            cargado && !TienePublicaciones && !IsBusy;

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool MostrarFinLista =>
            cargado &&
            TienePublicaciones &&
            !PuedeCargarMas &&
            !CargandoMas;

        public string TotalTexto =>
            totalRegistros == 1
                ? "1 publicación"
                : $"{totalRegistros} publicaciones";

        public bool CanAdministrar =>
            CanAdd || CanEdit || CanDelete;

        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }
        public Command NuevaCommand { get; }
        public Command<PublicacionListadoResponse> EditarCommand { get; }
        public Command<PublicacionListadoResponse> CambiarEstadoCommand { get; }
        public Command<PublicacionListadoResponse> CambiarDestacadaCommand { get; }
        public Command<PublicacionListadoResponse> EliminarCommand { get; }
        public Command RegresarCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(CanAdministrar));
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            if (!CanAdministrar || IsBusy)
                return;

            bool hayCambios =
                PublicacionListadoEstadoService
                    .HayCambiosDesde(versionAplicada);

            if (cargado && !hayCambios)
                return;

            if (hayCambios)
                categoriasCargadas = false;

            await CargarInicialAsync();
            if (ultimaCargaExitosa)
            {
                versionAplicada =
                    PublicacionListadoEstadoService.VersionActual;
            }
        }

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanAdministrar)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararCarga(reiniciar);

            try
            {
                if (reiniciar)
                {
                    ultimaCargaExitosa = false;
                    IsBusy = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int pagina = reiniciar
                    ? 1
                    : paginaActual + 1;

                ApiResult<PublicacionPaginadaResponse> result =
                    await apiService.GetAdministracionAsync(
                        ObtenerCategoriaId(),
                        EstadoSeleccionado,
                        TextoBusqueda,
                        pagina,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!EsMensajeCancelacion(result.Message))
                        Mensaje = result.Message;

                    return;
                }

                AplicarPagina(result.Data, reiniciar);
            }
            catch (OperationCanceledException)
            {
                // La navegación o una nueva consulta canceló la anterior.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested)
                {
                    Mensaje =
                        "No fue posible cargar las publicaciones.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las publicaciones",
                        ex);
                }
            }
            finally
            {
                if (reiniciar)
                {
                    IsBusy = false;
                    IsRefreshing = false;
                }
                else
                {
                    CargandoMas = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        public void CancelarCarga()
        {
            cargaCancellationTokenSource?.Cancel();
        }

        private async Task CargarInicialAsync()
        {
            CancellationTokenSource source =
                PrepararCarga(true);

            try
            {
                ultimaCargaExitosa = false;
                IsBusy = true;
                Mensaje = string.Empty;

                Task<ApiResult<List<CategoriaPublicacionResponse>>>
                    categoriasTask = categoriasCargadas
                        ? Task.FromResult(
                            ApiResult<List<CategoriaPublicacionResponse>>
                                .Ok(new List<CategoriaPublicacionResponse>()))
                        : apiService.GetCategoriasAsync(source.Token);

                Task<ApiResult<PublicacionPaginadaResponse>>
                    publicacionesTask =
                        apiService.GetAdministracionAsync(
                            ObtenerCategoriaId(),
                            EstadoSeleccionado,
                            TextoBusqueda,
                            1,
                            ObtenerTamanoPagina(),
                            source.Token);

                await Task.WhenAll(
                    categoriasTask,
                    publicacionesTask);

                if (source.IsCancellationRequested)
                    return;

                ApiResult<List<CategoriaPublicacionResponse>>
                    categoriasResult = await categoriasTask;

                ApiResult<PublicacionPaginadaResponse>
                    publicacionesResult = await publicacionesTask;

                if (!categoriasCargadas)
                {
                    if (!categoriasResult.Success ||
                        categoriasResult.Data == null)
                    {
                        Mensaje = categoriasResult.Message;
                        return;
                    }

                    AplicarCategorias(categoriasResult.Data);
                }

                if (!publicacionesResult.Success ||
                    publicacionesResult.Data == null)
                {
                    Mensaje = publicacionesResult.Message;
                    return;
                }

                AplicarPagina(
                    publicacionesResult.Data,
                    reiniciar: true);
            }
            catch (OperationCanceledException)
            {
                // Se canceló al salir.
            }
            finally
            {
                IsBusy = false;
                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        private void AplicarCategorias(
            IEnumerable<CategoriaPublicacionResponse> items)
        {
            Categorias.Clear();
            Categorias.Add(
                CategoriaPublicacionResponse.Todas());

            foreach (CategoriaPublicacionResponse categoria
                     in items.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada ??=
                Categorias.FirstOrDefault();

            categoriasCargadas = true;
        }

        private void AplicarPagina(
            PublicacionPaginadaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                Publicaciones.Clear();

            foreach (PublicacionListadoResponse item
                     in pagina.Items)
            {
                if (Publicaciones.Any(x =>
                        x.PublicacionId == item.PublicacionId))
                {
                    continue;
                }

                item.ImagenPortadaUrl =
                    ImagenMiniaturaUrlService.Crear(
                        item.ImagenPortadaUrl,
                        ancho: 720,
                        alto: 480,
                        calidad: 68);

                Publicaciones.Add(item);
            }

            paginaActual = pagina.Pagina;
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            totalRegistros = pagina.TotalRegistros;
            cargado = true;
            ultimaCargaExitosa = true;
            versionAplicada =
                PublicacionListadoEstadoService.VersionActual;
            NotificarLista();
        }

        private int? ObtenerCategoriaId()
        {
            int? id =
                CategoriaSeleccionada?
                    .CategoriaPublicacionId;

            return id.HasValue && id.Value > 0
                ? id
                : null;
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI
                ? 16
                : 8;

        private CancellationTokenSource PrepararCarga(
            bool cancelarAnterior)
        {
            if (cancelarAnterior)
            {
                cargaCancellationTokenSource?.Cancel();
                cargaCancellationTokenSource?.Dispose();
                cargaCancellationTokenSource = null;
            }

            var source = new CancellationTokenSource();
            cargaCancellationTokenSource = source;
            return source;
        }

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            if (!ReferenceEquals(
                    cargaCancellationTokenSource,
                    source))
            {
                source.Dispose();
                return;
            }

            cargaCancellationTokenSource.Dispose();
            cargaCancellationTokenSource = null;
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            estadoSeleccionado = "TODOS";
            categoriaSeleccionada = Categorias.FirstOrDefault();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoSeleccionado));
            OnPropertyChanged(nameof(CategoriaSeleccionada));

            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarMasAsync()
        {
            await CargarAsync(false);
        }

        private async Task NuevaAsync()
        {
            if (!CanAdd || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = 0
                });
        }

        private async Task EditarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private async Task CambiarEstadoAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || IsBusy)
                return;

            string nuevoEstado = string.Equals(
                item.EstadoPublicacion,
                "PUBLICADA",
                StringComparison.OrdinalIgnoreCase)
                ? "ARCHIVADA"
                : "PUBLICADA";

            bool confirmar = await ConfirmarAsync(
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                nuevoEstado == "PUBLICADA"
                    ? $"¿Desea publicar “{item.Titulo}”?"
                    : $"¿Desea archivar “{item.Titulo}”?",
                nuevoEstado == "PUBLICADA"
                    ? "Publicar"
                    : "Archivar",
                "Cancelar");

            if (!confirmar)
                return;

            bool actualizado = false;

            try
            {
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.CambiarEstadoAsync(
                        item.PublicacionId,
                        nuevoEstado);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(result.Message);
                PublicacionListadoEstadoService.MarcarActualizacion();
                actualizado = true;
            }
            finally
            {
                IsBusy = false;
            }

            if (actualizado)
                await CargarAsync(true);
        }

        private async Task CambiarDestacadaAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || IsBusy)
                return;

            bool actualizado = false;

            try
            {
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.CambiarDestacadaAsync(
                        item.PublicacionId,
                        !item.Destacada);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await MostrarExitoAsync(result.Message);
                PublicacionListadoEstadoService.MarcarActualizacion();
                actualizado = true;
            }
            finally
            {
                IsBusy = false;
            }

            if (actualizado)
                await CargarAsync(true);
        }

        private async Task EliminarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanDelete || item == null || IsBusy)
                return;

            bool confirmar = await ConfirmarEliminacionAsync(
                $"la publicación “{item.Titulo}”");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;

                ApiResult<bool> result =
                    await apiService.EliminarAsync(
                        item.PublicacionId);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                Publicaciones.Remove(item);
                totalRegistros = Math.Max(0, totalRegistros - 1);
                PublicacionListadoEstadoService.MarcarActualizacion();
                NotificarLista();
                await MostrarExitoAsync(result.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool EsMensajeCancelacion(
            string? message) =>
            string.Equals(
                message,
                "La operación fue cancelada.",
                StringComparison.OrdinalIgnoreCase);

        private void NotificarLista()
        {
            OnPropertyChanged(nameof(TienePublicaciones));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(TotalTexto));
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            NuevaCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            CambiarEstadoCommand.ChangeCanExecute();
            CambiarDestacadaCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
        }
    }
}
