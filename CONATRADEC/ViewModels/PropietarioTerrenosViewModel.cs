using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(
        nameof(Propietario),
        "Propietario")]
    public sealed class PropietarioTerrenosViewModel : GlobalService
    {
        private readonly PropietarioTerrenosApiService terrenosService =
            new();

        private readonly PropietarioApiService propietarioApiService =
            new();

        private CancellationTokenSource? propietariosCts;
        private PropietarioResponse? propietario;
        private PropietarioTerrenoResumenResponse? terrenoSeleccionado;
        private string textoBusquedaDestino = string.Empty;
        private string textoBusquedaDestinoAplicado = string.Empty;
        private bool mostrarSelectorPropietario;
        private bool isRefreshing;
        private int paginaPropietarios = 1;
        private int totalPaginasPropietarios = 1;
        private int totalPropietarios;
        private int tamanoPaginaPropietarios;

        public PropietarioTerrenosViewModel()
        {
            tamanoPaginaPropietarios =
                ObtenerTamanoPaginaPropietarios();

            RegresarCommand =
                new Command(
                    async () =>
                        await RegresarAsync(),
                    () =>
                        !IsBusy);

            ActualizarCommand =
                new Command(
                    async () =>
                        await RefrescarAsync(),
                    () =>
                        !IsBusy);

            CambiarPropietarioCommand =
                new Command<
                    PropietarioTerrenoResumenResponse>(
                    async terreno =>
                        await AbrirSelectorAsync(
                            terreno),
                    terreno =>
                        terreno != null &&
                        terreno.Activo &&
                        CanEdit &&
                        !IsBusy);

            BuscarPropietariosCommand =
                new Command(
                    async () =>
                        await AplicarBusquedaPropietariosAsync(),
                    () =>
                        MostrarSelectorPropietario &&
                        !IsBusy);

            PaginaAnteriorPropietariosCommand =
                new Command(
                    async () =>
                        await IrPaginaAnteriorPropietariosAsync(),
                    () =>
                        MostrarSelectorPropietario &&
                        PuedeIrAnteriorPropietarios &&
                        !IsBusy);

            PaginaSiguientePropietariosCommand =
                new Command(
                    async () =>
                        await IrPaginaSiguientePropietariosAsync(),
                    () =>
                        MostrarSelectorPropietario &&
                        PuedeIrSiguientePropietarios &&
                        !IsBusy);

            SeleccionarPropietarioCommand =
                new Command<PropietarioResponse>(
                    async propietarioDestino =>
                        await ReasignarAsync(
                            propietarioDestino),
                    propietarioDestino =>
                        propietarioDestino != null &&
                        propietarioDestino.Activo &&
                        propietarioDestino.PropietarioId > 0 &&
                        propietarioDestino.PropietarioId !=
                            Propietario?.PropietarioId &&
                        TerrenoSeleccionado != null &&
                        CanEdit &&
                        !IsBusy);

            CancelarCambioCommand =
                new Command(
                    CerrarSelector,
                    () =>
                        MostrarSelectorPropietario &&
                        !IsBusy);

            Terrenos.CollectionChanged +=
                (_, _) =>
                {
                    OnPropertyChanged(
                        nameof(HayTerrenos));

                    OnPropertyChanged(
                        nameof(NoHayTerrenos));

                    OnPropertyChanged(
                        nameof(TextoResumenTerrenos));
                };

            PropietariosDestino.CollectionChanged +=
                (_, _) =>
                {
                    OnPropertyChanged(
                        nameof(HayPropietariosDestino));

                    OnPropertyChanged(
                        nameof(NoHayPropietariosDestino));
                };
        }

        public ObservableCollection<
            PropietarioTerrenoResumenResponse>
            Terrenos { get; } =
                new();

        public ObservableCollection<PropietarioResponse>
            PropietariosDestino { get; } =
                new();

        public Command RegresarCommand { get; }
        public Command ActualizarCommand { get; }

        public Command<PropietarioTerrenoResumenResponse>
            CambiarPropietarioCommand { get; }

        public Command BuscarPropietariosCommand { get; }
        public Command PaginaAnteriorPropietariosCommand { get; }
        public Command PaginaSiguientePropietariosCommand { get; }

        public Command<PropietarioResponse>
            SeleccionarPropietarioCommand { get; }

        public Command CancelarCambioCommand { get; }

        public PropietarioResponse? Propietario
        {
            get => propietario;
            set
            {
                if (ReferenceEquals(
                        propietario,
                        value))
                {
                    return;
                }

                propietario = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Titulo));
                OnPropertyChanged(nameof(TextoPropietario));
                OnPropertyChanged(nameof(TextoIdentificacion));
                OnPropertyChanged(nameof(TextoContacto));
                OnPropertyChanged(nameof(TextoEstadoPropietario));
                OnPropertyChanged(nameof(TextoResumenTerrenos));

                ActualizarComandos();
            }
        }

        public PropietarioTerrenoResumenResponse?
            TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            private set
            {
                if (ReferenceEquals(
                        terrenoSeleccionado,
                        value))
                {
                    return;
                }

                terrenoSeleccionado = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(
                        TextoTerrenoSeleccionado));

                ActualizarComandos();
            }
        }

        public string TextoBusquedaDestino
        {
            get => textoBusquedaDestino;
            set
            {
                string nuevo =
                    value ??
                    string.Empty;

                if (textoBusquedaDestino == nuevo)
                    return;

                textoBusquedaDestino = nuevo;
                OnPropertyChanged();
            }
        }

        public bool MostrarSelectorPropietario
        {
            get => mostrarSelectorPropietario;
            private set
            {
                if (mostrarSelectorPropietario ==
                    value)
                {
                    return;
                }

                mostrarSelectorPropietario = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

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

        public string Titulo =>
            "Terrenos del propietario";

        public string TextoPropietario =>
            Propietario?.TextoPrincipal ??
            "Propietario";

        public string TextoIdentificacion =>
            Propietario?.TextoIdentificacion ??
            "Sin identificación";

        public string TextoContacto =>
            Propietario?.TextoContacto ??
            "Sin contacto registrado";

        public string TextoEstadoPropietario =>
            Propietario?.TextoEstado ??
            string.Empty;

        public string TextoResumenTerrenos =>
            Terrenos.Count == 1
                ? "1 terreno vinculado"
                : $"{Terrenos.Count} terrenos vinculados";

        public string TextoTerrenoSeleccionado =>
            TerrenoSeleccionado == null
                ? string.Empty
                : "Cambiar propietario de " +
                  TerrenoSeleccionado.TextoCodigo;

        public string TextoResumenPropietarios =>
            totalPropietarios == 1
                ? "1 propietario disponible"
                : $"{totalPropietarios:N0} propietarios disponibles";

        public bool HayTerrenos =>
            Terrenos.Count > 0;

        public bool NoHayTerrenos =>
            Terrenos.Count == 0;

        public bool HayPropietariosDestino =>
            PropietariosDestino.Count > 0;

        public bool NoHayPropietariosDestino =>
            PropietariosDestino.Count == 0 &&
            !IsBusy;

        public int PaginaActualPropietarios =>
            paginaPropietarios;

        public int TotalPaginasPropietarios =>
            totalPaginasPropietarios;

        public bool PuedeIrAnteriorPropietarios =>
            paginaPropietarios > 1;

        public bool PuedeIrSiguientePropietarios =>
            paginaPropietarios <
            totalPaginasPropietarios;

        public bool MostrarPaginacionPropietarios =>
            MostrarSelectorPropietario &&
            PropietariosDestino.Count > 0;

        public string PaginaPropietariosTexto =>
            $"Página {Math.Max(1, paginaPropietarios)} de {Math.Max(1, totalPaginasPropietarios)}";

        public string RangoPropietariosTexto
        {
            get
            {
                if (totalPropietarios <= 0 ||
                    PropietariosDestino.Count == 0)
                {
                    return
                        "Sin registros en esta página";
                }

                int inicio =
                    ((Math.Max(
                        1,
                        paginaPropietarios) - 1) *
                     Math.Max(
                         1,
                         tamanoPaginaPropietarios)) + 1;

                int fin =
                    Math.Min(
                        inicio +
                        PropietariosDestino.Count - 1,
                        totalPropietarios);

                return
                    $"Mostrando {inicio}-{fin} de {totalPropietarios}";
            }
        }

        public new bool CanView =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance.HasUpdate(
                InterfazCodigos.Propietarios);

        public Task InicializarAsync() =>
            CargarDetalleAsync();

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref propietariosCts,
                    null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;

            ActualizarComandos();
        }

        private async Task RefrescarAsync()
        {
            if (IsBusy)
                return;

            IsRefreshing = true;

            try
            {
                await CargarDetalleAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task CargarDetalleAsync()
        {
            if (IsBusy)
                return;

            int propietarioId =
                Propietario?
                    .PropietarioId ??
                0;

            if (propietarioId <= 0)
            {
                await MostrarErrorAsync(
                    "No se recibió el propietario que desea consultar.");
                return;
            }

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para visualizar propietarios.");
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await MostrarAdvertenciaAsync(
                    "La administración de propietarios requiere conexión a internet.");
                return;
            }

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<PropietarioDetalleResponse> resultado =
                    await terrenosService.ObtenerDetalleAsync(
                        propietarioId);

                if (!resultado.Success ||
                    resultado.Data?.Propietario == null)
                {
                    await MostrarErrorAsync(
                        resultado.Message);

                    return;
                }

                PropietarioResponse propietarioActual =
                    resultado.Data.Propietario;

                propietarioActual.TotalTerrenos =
                    resultado.Data
                        .Terrenos
                        .Count;

                Propietario =
                    propietarioActual;

                Terrenos.Clear();

                foreach (
                    PropietarioTerrenoResumenResponse terreno
                    in resultado.Data.Terrenos)
                {
                    Terrenos.Add(
                        terreno);
                }

                OnPropertyChanged(
                    nameof(
                        TextoResumenTerrenos));
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
        }

        private async Task AbrirSelectorAsync(
            PropietarioTerrenoResumenResponse? terreno)
        {
            if (terreno == null)
                return;

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para cambiar el propietario de un terreno.");
                return;
            }

            if (!terreno.Activo)
            {
                await MostrarAdvertenciaAsync(
                    "No se puede reasignar un terreno inactivo.");
                return;
            }

            TerrenoSeleccionado =
                terreno;

            TextoBusquedaDestino =
                string.Empty;

            textoBusquedaDestinoAplicado =
                string.Empty;

            MostrarSelectorPropietario =
                true;

            await CargarPropietariosDestinoAsync(
                1);
        }

        private async Task AplicarBusquedaPropietariosAsync()
        {
            textoBusquedaDestinoAplicado =
                (TextoBusquedaDestino ??
                 string.Empty)
                    .Trim();

            await CargarPropietariosDestinoAsync(
                1);
        }

        private Task IrPaginaAnteriorPropietariosAsync()
        {
            if (!PuedeIrAnteriorPropietarios)
                return Task.CompletedTask;

            return CargarPropietariosDestinoAsync(
                paginaPropietarios - 1);
        }

        private Task IrPaginaSiguientePropietariosAsync()
        {
            if (!PuedeIrSiguientePropietarios)
                return Task.CompletedTask;

            return CargarPropietariosDestinoAsync(
                paginaPropietarios + 1);
        }

        /// <summary>
        /// El selector mantiene únicamente la página visible y excluye al
        /// propietario actual directamente en el servidor.
        /// </summary>
        private async Task CargarPropietariosDestinoAsync(
            int paginaSolicitada)
        {
            if (IsBusy ||
                !MostrarSelectorPropietario)
            {
                return;
            }

            paginaSolicitada =
                Math.Max(
                    1,
                    paginaSolicitada);

            CancellationTokenSource source =
                PrepararCargaPropietarios();

            try
            {
                CambiarEstadoOcupado(true);

                ApiResult<PropietarioPaginaResponse> resultado =
                    await propietarioApiService.BuscarPaginadoAsync(
                        textoBusquedaDestinoAplicado,
                        incluirInactivos: false,
                        paraSeleccionTerreno: true,
                        pagina: paginaSolicitada,
                        tamanoPagina:
                            ObtenerTamanoPaginaPropietarios(),
                        cancellationToken:
                            source.Token,
                        excluirPropietarioId:
                            Propietario?.PropietarioId);

                if (source.IsCancellationRequested ||
                    !EsCargaPropietariosActual(
                        source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsCancelacion(
                            resultado.Message))
                    {
                        await MostrarErrorAsync(
                            resultado.Message);
                    }

                    return;
                }

                PropietarioPaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(
                        1,
                        pagina.TotalPaginas);

                if (paginaSolicitada >
                        paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    resultado =
                        await propietarioApiService
                            .BuscarPaginadoAsync(
                                textoBusquedaDestinoAplicado,
                                incluirInactivos: false,
                                paraSeleccionTerreno: true,
                                pagina:
                                    paginasServidor,
                                tamanoPagina:
                                    ObtenerTamanoPaginaPropietarios(),
                                cancellationToken:
                                    source.Token,
                                excluirPropietarioId:
                                    Propietario?.PropietarioId);

                    if (source.IsCancellationRequested ||
                        !EsCargaPropietariosActual(
                            source))
                    {
                        return;
                    }

                    if (!resultado.Success ||
                        resultado.Data == null)
                    {
                        if (!EsCancelacion(
                                resultado.Message))
                        {
                            await MostrarErrorAsync(
                                resultado.Message);
                        }

                        return;
                    }

                    pagina =
                        resultado.Data;
                }

                AplicarPaginaPropietarios(
                    pagina);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (EsCargaPropietariosActual(
                        source))
                {
                    CambiarEstadoOcupado(false);
                }

                LiberarCargaPropietarios(
                    source);
            }
        }

        private void AplicarPaginaPropietarios(
            PropietarioPaginaResponse pagina)
        {
            PropietariosDestino.Clear();

            foreach (PropietarioResponse item
                     in pagina.Items)
            {
                if (!item.Activo ||
                    item.PropietarioId <= 0)
                {
                    continue;
                }

                PropietariosDestino.Add(
                    item);
            }

            paginaPropietarios =
                Math.Max(
                    1,
                    pagina.Pagina);

            totalPaginasPropietarios =
                Math.Max(
                    1,
                    pagina.TotalPaginas);

            tamanoPaginaPropietarios =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPaginaPropietarios();

            totalPropietarios =
                Math.Max(
                    0,
                    pagina.TotalRegistros);

            NotificarEstadoSelector();
            ActualizarComandos();
        }

        private async Task ReasignarAsync(
            PropietarioResponse? propietarioDestino)
        {
            if (propietarioDestino == null ||
                TerrenoSeleccionado == null)
            {
                return;
            }

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para cambiar propietarios.");
                return;
            }

            PropietarioTerrenoResumenResponse terreno =
                TerrenoSeleccionado;

            bool confirmar =
                await ConfirmarAsync(
                    "Cambiar propietario",
                    $"¿Desea asignar {terreno.TextoCodigo} a " +
                    $"{propietarioDestino.TextoPrincipal}? " +
                    $"Se retirará de {TextoPropietario}.",
                    "Cambiar propietario",
                    "Cancelar");

            if (!confirmar)
                return;

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<bool> resultado =
                    await terrenosService
                        .ReasignarTerrenoAsync(
                            propietarioDestino
                                .PropietarioId,
                            terreno
                                .TerrenoId);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);

                    return;
                }

                Terrenos.Remove(
                    terreno);

                if (Propietario != null)
                {
                    Propietario.TotalTerrenos =
                        Terrenos.Count;

                    OnPropertyChanged(
                        nameof(
                            TextoResumenTerrenos));
                }

                /*
                 * El contador de terrenos mostrado en la lista principal de
                 * Propietarios cambió; se renueva al volver a ella.
                 */
                PropietarioVisitaService
                    .MarcarAdministracionParaRecargar();

                CerrarSelector();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "Terreno reasignado correctamente."
                        : resultado.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
        }

        private async Task RegresarAsync()
        {
            if (MostrarSelectorPropietario)
            {
                CerrarSelector();
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.Regresar);
        }

        private void CerrarSelector()
        {
            CancelarCarga();

            MostrarSelectorPropietario =
                false;

            TerrenoSeleccionado =
                null;

            TextoBusquedaDestino =
                string.Empty;

            textoBusquedaDestinoAplicado =
                string.Empty;

            PropietariosDestino.Clear();

            paginaPropietarios = 1;
            totalPaginasPropietarios = 1;
            totalPropietarios = 0;
            tamanoPaginaPropietarios =
                ObtenerTamanoPaginaPropietarios();

            NotificarEstadoSelector();
        }

        private void CambiarEstadoOcupado(
            bool valor)
        {
            IsBusy = valor;
            ActualizarComandos();

            OnPropertyChanged(
                nameof(
                    NoHayPropietariosDestino));
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            CambiarPropietarioCommand.ChangeCanExecute();
            BuscarPropietariosCommand.ChangeCanExecute();
            PaginaAnteriorPropietariosCommand.ChangeCanExecute();
            PaginaSiguientePropietariosCommand.ChangeCanExecute();
            SeleccionarPropietarioCommand.ChangeCanExecute();
            CancelarCambioCommand.ChangeCanExecute();
        }

        private void NotificarEstadoSelector()
        {
            OnPropertyChanged(
                nameof(
                    PuedeIrAnteriorPropietarios));

            OnPropertyChanged(
                nameof(
                    PuedeIrSiguientePropietarios));

            OnPropertyChanged(
                nameof(
                    MostrarPaginacionPropietarios));

            OnPropertyChanged(
                nameof(
                    PaginaActualPropietarios));

            OnPropertyChanged(
                nameof(
                    TotalPaginasPropietarios));

            OnPropertyChanged(
                nameof(
                    PaginaPropietariosTexto));

            OnPropertyChanged(
                nameof(
                    RangoPropietariosTexto));

            OnPropertyChanged(
                nameof(
                    TextoResumenPropietarios));

            OnPropertyChanged(
                nameof(
                    NoHayPropietariosDestino));
        }

        private static int ObtenerTamanoPaginaPropietarios() =>
            DeviceInfo.Current.Platform ==
            DevicePlatform.WinUI
                ? 32
                : 14;

        private CancellationTokenSource
            PrepararCargaPropietarios()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref propietariosCts,
                    source);

            CancelarSeguro(
                anterior);

            return source;
        }

        private bool EsCargaPropietariosActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(
                    ref propietariosCts),
                source);

        private void LiberarCargaPropietarios(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref propietariosCts,
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

        private static bool EsCancelacion(
            string? mensaje) =>
            !string.IsNullOrWhiteSpace(
                mensaje) &&
            mensaje.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
