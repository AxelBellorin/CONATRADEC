using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

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
        private bool navegando;
        private bool ejecutandoOperacion;
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
                async () => await EjecutarComandoSeguroAsync(
                    () => CargarAsync(reiniciar: true),
                    "buscar publicaciones"),
                () => !IsBusy && !Navegando && CanAdministrar);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar los filtros de publicaciones"),
                () => !IsBusy && !Navegando && CanAdministrar);

            RefrescarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RefrescarAsync,
                    "actualizar las publicaciones"),
                () => !IsBusy && !Navegando && CanAdministrar);

            CargarMasCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    CargarMasAsync,
                    "cargar más publicaciones"),
                () =>
                    !IsBusy &&
                    !CargandoMas &&
                    !Navegando &&
                    PuedeCargarMas &&
                    CanAdministrar);

            /*
             * Los comandos de navegación pueden ejecutarse aunque exista una
             * carga en curso. La carga se cancela antes de cambiar de página.
             */
            NuevaCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    NuevaAsync,
                    "abrir una nueva publicación"),
                () => !Navegando && !EjecutandoOperacion && CanAdd);

            EditarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => EditarAsync(item),
                        "editar la publicación"),
                    item =>
                        item != null &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanEdit);

            CambiarEstadoCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => CambiarEstadoAsync(item),
                        "cambiar el estado de la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanEdit);

            CambiarDestacadaCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => CambiarDestacadaAsync(item),
                        "actualizar el destacado de la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanEdit);

            EliminarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarComandoSeguroAsync(
                        () => EliminarAsync(item),
                        "eliminar la publicación"),
                    item =>
                        item != null &&
                        !IsBusy &&
                        !Navegando &&
                        !EjecutandoOperacion &&
                        CanDelete);

            RegresarCommand = new Command(
                async () => await EjecutarComandoSeguroAsync(
                    RegresarAsync,
                    "regresar al centro de noticias"),
                () => !Navegando && !EjecutandoOperacion);
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
                string nuevoValor = string.IsNullOrWhiteSpace(value)
                    ? "TODOS"
                    : value;

                if (estadoSeleccionado == nuevoValor)
                    return;

                estadoSeleccionado = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
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
                string nuevoValor = value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
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
                NotificarLista();
            }
        }

        public bool Navegando
        {
            get => navegando;
            private set
            {
                if (navegando == value)
                    return;

                navegando = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        private bool EjecutandoOperacion
        {
            get => ejecutandoOperacion;
            set
            {
                if (ejecutandoOperacion == value)
                    return;

                ejecutandoOperacion = value;
                ActualizarComandos();
            }
        }

        public bool TienePublicaciones =>
            Publicaciones.Count > 0;

        public bool MostrarVacio =>
            cargado &&
            !TienePublicaciones &&
            !IsBusy;

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
            if (!CanAdministrar || IsBusy || Navegando)
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
            if (!CanAdministrar || Navegando)
                return;

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararCarga();

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

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!result.Success || result.Data == null)
                {
                    if (!EsMensajeCancelacion(result.Message))
                        Mensaje = result.Message;

                    return;
                }

                AplicarPagina(
                    result.Data,
                    reiniciar);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar la consulta.
            }
            catch (ObjectDisposedException)
            {
                // El stream se cerró porque la página cambió.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
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
                if (EsCargaActual(source))
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
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        /// <summary>
        /// Cancela la petición activa sin disponer su token inmediatamente.
        /// La tarea que creó el token será responsable de disponerlo.
        /// </summary>
        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    null);

            CancelarSeguro(source);

            if (!EjecutandoOperacion)
                IsBusy = false;

            IsRefreshing = false;
            CargandoMas = false;
        }

        private async Task CargarInicialAsync()
        {
            CancellationTokenSource source =
                PrepararCarga();

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

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                ApiResult<List<CategoriaPublicacionResponse>>
                    categoriasResult = await categoriasTask;

                ApiResult<PublicacionPaginadaResponse>
                    publicacionesResult = await publicacionesTask;

                if (!categoriasCargadas)
                {
                    if (!categoriasResult.Success ||
                        categoriasResult.Data == null)
                    {
                        if (!EsMensajeCancelacion(categoriasResult.Message))
                            Mensaje = categoriasResult.Message;

                        return;
                    }

                    AplicarCategorias(categoriasResult.Data);
                }

                if (!publicacionesResult.Success ||
                    publicacionesResult.Data == null)
                {
                    if (!EsMensajeCancelacion(publicacionesResult.Message))
                        Mensaje = publicacionesResult.Message;

                    return;
                }

                AplicarPagina(
                    publicacionesResult.Data,
                    reiniciar: true);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al salir de la página.
            }
            catch (ObjectDisposedException)
            {
                // Cierre esperado del stream durante navegación rápida.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
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
                if (EsCargaActual(source))
                    IsBusy = false;

                LiberarCarga(source);
                ActualizarComandos();
                NotificarLista();
            }
        }

        private void AplicarCategorias(
            IEnumerable<CategoriaPublicacionResponse> items)
        {
            int? seleccionAnterior =
                CategoriaSeleccionada?.CategoriaPublicacionId;

            Categorias.Clear();
            Categorias.Add(
                CategoriaPublicacionResponse.Todas());

            foreach (CategoriaPublicacionResponse categoria
                     in items.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada =
                Categorias.FirstOrDefault(x =>
                    x.CategoriaPublicacionId == seleccionAnterior)
                ?? Categorias.FirstOrDefault();

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

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCancellationTokenSource,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCancellationTokenSource),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCancellationTokenSource,
                null,
                source);

            source.Dispose();
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // La tarea propietaria terminó y dispuso el token primero.
            }
        }

        private async Task LimpiarFiltrosAsync()
        {
            textoBusqueda = string.Empty;
            estadoSeleccionado = "TODOS";
            categoriaSeleccionada =
                Categorias.FirstOrDefault();

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoSeleccionado));
            OnPropertyChanged(nameof(CategoriaSeleccionada));

            await CargarAsync(reiniciar: true);
        }

        private async Task RefrescarAsync()
        {
            try
            {
                IsRefreshing = true;
                await CargarAsync(reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task CargarMasAsync() =>
            CargarAsync(reiniciar: false);

        private async Task NuevaAsync()
        {
            if (!CanAdd || Navegando || EjecutandoOperacion)
                return;

            await NavegarSeguroAsync(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = 0
                });
        }

        private async Task EditarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || Navegando || EjecutandoOperacion)
                return;

            await NavegarSeguroAsync(
                AppRoutes.PublicacionFormulario,
                new Dictionary<string, object>
                {
                    ["PublicacionId"] = item.PublicacionId
                });
        }

        private Task RegresarAsync() =>
            NavegarSeguroAsync(AppRoutes.Regresar);

        private async Task NavegarSeguroAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();
                await Task.Yield();

                await GoToAsyncParameters(
                    ruta,
                    parametros);
            }
            finally
            {
                Navegando = false;
            }
        }

        private async Task CambiarEstadoAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || IsBusy || Navegando || EjecutandoOperacion)
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
                EjecutandoOperacion = true;
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
                EjecutandoOperacion = false;
            }

            if (actualizado)
                await CargarAsync(reiniciar: true);
        }

        private async Task CambiarDestacadaAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanEdit || item == null || IsBusy || Navegando || EjecutandoOperacion)
                return;

            bool actualizado = false;

            try
            {
                EjecutandoOperacion = true;
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
                EjecutandoOperacion = false;
            }

            if (actualizado)
                await CargarAsync(reiniciar: true);
        }

        private async Task EliminarAsync(
            PublicacionListadoResponse? item)
        {
            if (!CanDelete || item == null || IsBusy || Navegando || EjecutandoOperacion)
                return;

            bool confirmar = await ConfirmarEliminacionAsync(
                $"la publicación “{item.Titulo}”");

            if (!confirmar)
                return;

            try
            {
                EjecutandoOperacion = true;
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
                totalRegistros = Math.Max(
                    0,
                    totalRegistros - 1);

                PublicacionListadoEstadoService.MarcarActualizacion();
                NotificarLista();

                await MostrarExitoAsync(result.Message);
            }
            finally
            {
                IsBusy = false;
                EjecutandoOperacion = false;
            }
        }

        private async Task EjecutarComandoSeguroAsync(
            Func<Task> accion,
            string operacion)
        {
            try
            {
                await accion();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por navegación o nueva consulta.
            }
            catch (ObjectDisposedException)
            {
                // Cierre esperado del stream o token durante navegación rápida.
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    operacion,
                    ex);
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
