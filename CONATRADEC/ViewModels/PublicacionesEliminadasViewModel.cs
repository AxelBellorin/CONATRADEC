using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Papelera administrativa de publicaciones. Mantiene solamente la página
    /// visible y restaura cada publicación como BORRADOR para evitar que vuelva
    /// al feed público sin una revisión explícita del administrador.
    /// </summary>
    public sealed class PublicacionesEliminadasViewModel : GlobalService
    {
        private readonly PublicacionesEliminadasApiService apiService = new();

        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool pantallaCargada;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public PublicacionesEliminadasViewModel()
        {
            tamanoPaginaActual =
                ObtenerTamanoPagina();

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar publicaciones eliminadas"),
                    () => CanAdministrar && !IsBusy);

            LimpiarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltroAsync,
                        "limpiar la búsqueda de publicaciones eliminadas"),
                    () => CanAdministrar && !IsBusy);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar publicaciones eliminadas"),
                    () => CanAdministrar && !IsBusy);

            ReactivarCommand =
                new Command<PublicacionListadoResponse>(
                    async item => await EjecutarSeguroAsync(
                        () => ReactivarAsync(item),
                        "restaurar la publicación"),
                    item =>
                        item != null &&
                        CanView &&
                        CanEdit &&
                        !IsBusy);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        PuedeIrAnterior &&
                        CanAdministrar &&
                        !IsBusy);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        PuedeIrSiguiente &&
                        CanAdministrar &&
                        !IsBusy);

            CerrarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        CerrarAsync,
                        "cerrar publicaciones eliminadas"),
                    () => !IsBusy);
        }

        public ObservableCollection<PublicacionListadoResponse>
            Registros { get; } = new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<PublicacionListadoResponse> ReactivarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command CerrarCommand { get; }

        public bool CanAdministrar =>
            CanView &&
            (CanAdd || CanEdit || CanDelete);

        public string Titulo =>
            "Publicaciones eliminadas";

        public string Descripcion =>
            "Consulte publicaciones eliminadas y restáurelas como borradores conservando su identificador, portada e historial.";

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevo = value ?? string.Empty;

                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevo = value ?? string.Empty;

                if (mensaje == nuevo)
                    return;

                mensaje = nuevo;
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
                ActualizarComandos();
            }
        }

        public bool MostrandoRelay
        {
            get => mostrandoRelay;
            private set
            {
                if (mostrandoRelay == value)
                    return;

                mostrandoRelay = value;
                OnPropertyChanged();
            }
        }

        public string TituloRelay
        {
            get => tituloRelay;
            private set
            {
                string nuevo = value ?? string.Empty;

                if (tituloRelay == nuevo)
                    return;

                tituloRelay = nuevo;
                OnPropertyChanged();
            }
        }

        public string DetalleRelay
        {
            get => detalleRelay;
            private set
            {
                string nuevo = value ?? string.Empty;

                if (detalleRelay == nuevo)
                    return;

                detalleRelay = nuevo;
                OnPropertyChanged();
            }
        }

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(RangoPaginaTexto));
                OnPropertyChanged(nameof(MostrarPaginacion));
            }
        }

        public int PaginaActual =>
            Math.Max(1, paginaActual);

        public int TotalPaginas =>
            Math.Max(1, totalPaginas);

        public bool PuedeIrAnterior =>
            pantallaCargada && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanAdministrar &&
            pantallaCargada &&
            Registros.Count > 0;

        public string PaginaTexto =>
            $"Página {PaginaActual} de {TotalPaginas}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 ||
                    Registros.Count == 0)
                {
                    return "Sin registros en esta página";
                }

                int inicio =
                    ((PaginaActual - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio + Registros.Count - 1,
                        TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string Resumen =>
            TotalRegistros == 1
                ? "1 publicación eliminada"
                : $"{TotalRegistros:N0} publicaciones eliminadas";

        public bool MostrarVacio =>
            CanAdministrar &&
            pantallaCargada &&
            !IsBusy &&
            Registros.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanAdministrar;

        public async Task InicializarAsync()
        {
            LoadPagePermissions("noticiasPage");

            OnPropertyChanged(nameof(CanAdministrar));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();

            if (!CanAdministrar || pantallaCargada)
                return;

            textoBusquedaAplicado = string.Empty;
            tamanoPaginaActual = ObtenerTamanoPagina();

            await CargarPaginaAsync(
                1,
                "Cargando publicaciones eliminadas...",
                "Consultando información actual del servidor");
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            OcultarRelay();
            ActualizarComandos();
            NotificarEstado();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty)
                    .Trim();

            await CargarPaginaAsync(
                1,
                "Buscando publicaciones eliminadas...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltroAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                "Actualizando publicaciones eliminadas...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    PaginaActual,
                    "Actualizando publicaciones eliminadas...",
                    "Consultando nuevamente la página actual");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual - 1,
                "Cargando página anterior...",
                "Consultando la página anterior de publicaciones eliminadas");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                "Cargando página siguiente...",
                "Consultando la siguiente página de publicaciones eliminadas");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string titulo,
            string detalle)
        {
            if (!CanAdministrar || IsBusy)
                return;

            paginaSolicitada =
                Math.Max(1, paginaSolicitada);

            CancellationTokenSource source =
                PrepararCarga();

            try
            {
                MostrarRelay(titulo, detalle);
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<PublicacionPaginadaResponse> resultado =
                    await apiService.ListarAsync(
                        textoBusquedaAplicado,
                        paginaSolicitada,
                        ObtenerTamanoPagina(),
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsCancelacion(resultado.Message))
                    {
                        Mensaje =
                            string.IsNullOrWhiteSpace(resultado.Message)
                                ? "No fue posible cargar las publicaciones eliminadas."
                                : resultado.Message;
                    }

                    return;
                }

                AplicarPagina(resultado.Data);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar las publicaciones eliminadas.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las publicaciones eliminadas",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    IsRefreshing = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private void AplicarPagina(
            PublicacionPaginadaResponse pagina)
        {
            Registros.Clear();

            foreach (PublicacionListadoResponse item
                     in pagina.Items)
            {
                if (item.PublicacionId <= 0)
                    continue;

                item.ImagenPortadaUrl =
                    ImagenMiniaturaUrlService.Crear(
                        item.ImagenPortadaUrl,
                        ancho: 720,
                        alto: 480,
                        calidad: 68);

                Registros.Add(item);
            }

            paginaActual =
                Math.Max(1, pagina.Pagina);

            totalPaginas =
                Math.Max(1, pagina.TotalPaginas);

            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            TotalRegistros =
                Math.Max(0, pagina.TotalRegistros);

            Mensaje = string.Empty;
            NotificarEstado();
        }

        private async Task ReactivarAsync(
            PublicacionListadoResponse? item)
        {
            if (item?.PublicacionId is not > 0 || IsBusy)
                return;

            if (!CanView || !CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para restaurar publicaciones.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Restaurar publicación",
                $"¿Desea restaurar “{item.Titulo}”? La publicación volverá como BORRADOR y no aparecerá en el feed hasta que sea publicada nuevamente.",
                "Restaurar",
                "Cancelar");

            if (!confirmar)
                return;

            bool reactivada = false;
            int paginaAntesReactivar =
                PaginaActual;
            string mensajeExito =
                "Publicación restaurada como borrador.";

            CancellationTokenSource source =
                PrepararCarga();

            try
            {
                MostrarRelay(
                    "Restaurando publicación...",
                    "Reactivando el registro como borrador");

                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await apiService.ReactivarAsync(
                        item.PublicacionId,
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible restaurar la publicación."
                            : resultado.Message);
                    return;
                }

                reactivada = true;
                mensajeExito =
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? mensajeExito
                        : resultado.Message;
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }

            if (!reactivada)
                return;

            /*
             * La restauración modifica tanto la papelera como el listado
             * activo. Ambos estados vuelven a derivarse del servidor.
             */
            PublicacionListadoEstadoService.MarcarActualizacion();

            await CargarPaginaAsync(
                paginaAntesReactivar,
                "Actualizando publicaciones eliminadas...",
                "Consultando nuevamente la página después de restaurar");

            await MostrarExitoAsync(mensajeExito);
        }

        private async Task CerrarAsync()
        {
            if (IsBusy ||
                Shell.Current?.Navigation == null)
            {
                return;
            }

            try
            {
                MostrarRelay(
                    "Regresando...",
                    "Cerrando publicaciones eliminadas");

                IsBusy = true;
                ActualizarComandos();

                await Task.Yield();
                await Shell.Current.Navigation.PopModalAsync();
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task EjecutarSeguroAsync(
            Func<Task> accion,
            string descripcion)
        {
            try
            {
                await accion();
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    descripcion,
                    ex);
            }
        }

        private void MostrarRelay(
            string titulo,
            string detalle)
        {
            TituloRelay = titulo;
            DetalleRelay = detalle;
            MostrandoRelay = true;
        }

        private void OcultarRelay()
        {
            MostrandoRelay = false;
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            LimpiarCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            ReactivarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            CerrarCommand.ChangeCanExecute();
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(CanAdministrar));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(TieneMensaje));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(Resumen));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
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
                    ref cargaCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCts),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCts,
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
            }
            finally
            {
                source.Dispose();
            }
        }

        private static bool EsCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
