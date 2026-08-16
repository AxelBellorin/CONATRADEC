using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Ventana especializada de Roles eliminados. Mantiene únicamente la página
    /// visible y realiza búsqueda/paginación directamente en el servidor.
    /// </summary>
    public sealed class RolEliminadosViewModel : GlobalService
    {
        private readonly RolApiService rolApiService = new();

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

        public RolEliminadosViewModel()
        {
            tamanoPaginaActual =
                ObtenerTamanoPagina();

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar roles eliminados"),
                    () => CanView && !IsBusy);

            LimpiarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltroAsync,
                        "limpiar la búsqueda"),
                    () => CanView && !IsBusy);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar roles eliminados"),
                    () => CanView && !IsBusy);

            ReactivarCommand =
                new Command<RolResponse>(
                    async rol => await EjecutarSeguroAsync(
                        () => ReactivarAsync(rol),
                        "reactivar el rol"),
                    rol =>
                        rol != null &&
                        CanEdit &&
                        !IsBusy);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        PuedeIrAnterior &&
                        CanView &&
                        !IsBusy);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        PuedeIrSiguiente &&
                        CanView &&
                        !IsBusy);

            CerrarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        CerrarAsync,
                        "cerrar roles eliminados"),
                    () => !IsBusy);
        }

        public ObservableCollection<RolResponse>
            Registros { get; } = new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<RolResponse> ReactivarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command CerrarCommand { get; }

        public string Titulo =>
            "Roles eliminados";

        public string Descripcion =>
            "Reactive roles conservando su identificador y su historial de permisos.";

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
            paginaActual;

        public int TotalPaginas =>
            totalPaginas;

        public bool PuedeIrAnterior =>
            pantallaCargada &&
            paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada &&
            paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView &&
            pantallaCargada &&
            Registros.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

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
                    ((Math.Max(1, paginaActual) - 1) *
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
                ? "1 rol eliminado"
                : $"{TotalRegistros:N0} roles eliminados";

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            Registros.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public async Task InicializarAsync()
        {
            LoadPagePermissions("rolPage");

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();

            if (!CanView || pantallaCargada)
                return;

            textoBusquedaAplicado = string.Empty;
            tamanoPaginaActual = ObtenerTamanoPagina();

            await CargarPaginaAsync(
                1,
                "Cargando roles eliminados...",
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
                "Buscando roles eliminados...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltroAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                "Actualizando roles eliminados...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    "Actualizando roles eliminados...",
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
                "Consultando la página anterior de roles eliminados");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                "Cargando página siguiente...",
                "Consultando la siguiente página de roles eliminados");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string titulo,
            string detalle)
        {
            if (!CanView || IsBusy)
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

                ApiResult<RolAdministracionPaginaResponse> resultado =
                    await rolApiService.BuscarPaginadoAsync(
                        textoBusquedaAplicado,
                        incluirInactivos: true,
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
                                ? "No fue posible cargar los roles eliminados."
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
                        "No fue posible cargar los roles eliminados.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los roles eliminados",
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
            RolAdministracionPaginaResponse pagina)
        {
            Registros.Clear();

            foreach (RolResponse item in pagina.Items)
            {
                if (item.RolId is > 0)
                    Registros.Add(item);
            }

            paginaActual =
                Math.Max(1, pagina.PaginaActual);

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
            RolResponse? rol)
        {
            if (rol?.RolId is not > 0 || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para reactivar roles.");
                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Reactivar rol",
                    $"¿Desea reactivar '{rol.NombreMostrar}' conservando su identificador e historial de permisos?",
                    "Reactivar",
                    "Cancelar");

            if (!confirmar)
                return;

            bool reactivado = false;
            bool recargarPagina = false;
            int paginaAntesReactivar =
                paginaActual;
            string mensajeExito =
                "Rol reactivado correctamente.";

            CancellationTokenSource source =
                PrepararCarga();

            try
            {
                MostrarRelay(
                    "Reactivando rol...",
                    "Restaurando el registro en el servidor");

                IsBusy = true;
                ActualizarComandos();

                ApiResult<RolResponse> resultado =
                    await rolApiService
                        .ReactivarRolAdministracionResultAsync(
                            new RolRequest(rol),
                            source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data?.RolId is not > 0)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible reactivar el rol."
                            : resultado.Message);
                    return;
                }

                reactivado = true;
                mensajeExito =
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? mensajeExito
                        : resultado.Message;

                Registros.Remove(rol);
                TotalRegistros =
                    Math.Max(0, TotalRegistros - 1);

                RecalcularPaginas();

                if (Registros.Count == 0 &&
                    TotalRegistros > 0 &&
                    paginaAntesReactivar > 1)
                {
                    recargarPagina = true;
                }
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

            if (!reactivado)
                return;

            /*
             * La lista activa se considera stale solo después de una
             * reactivación real. Cerrar el modal sin cambios no provoca GET.
             */
            RolVisitaService.MarcarListadoParaRecargar();

            if (recargarPagina)
            {
                int paginaDestino =
                    Math.Min(
                        Math.Max(1, paginaActual),
                        Math.Max(1, totalPaginas));

                await CargarPaginaAsync(
                    paginaDestino,
                    "Actualizando roles eliminados...",
                    "Ajustando la página después de la reactivación");
            }

            await MostrarExitoAsync(mensajeExito);
        }

        private void RecalcularPaginas()
        {
            int tamano =
                Math.Max(1, tamanoPaginaActual);

            totalPaginas =
                TotalRegistros == 0
                    ? 1
                    : (int)Math.Ceiling(
                        TotalRegistros /
                        (double)tamano);

            paginaActual =
                Math.Min(
                    Math.Max(1, paginaActual),
                    Math.Max(1, totalPaginas));

            NotificarEstado();
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
                    "Cerrando roles eliminados y volviendo al listado");

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
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

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
