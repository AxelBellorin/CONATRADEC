using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(nameof(Propietario), "Propietario")]
    public sealed class PropietarioTerrenosViewModel : GlobalService
    {
        private readonly PropietarioTerrenosApiService terrenosService = new();
        private readonly PropietarioApiService propietarioApiService = new();

        private CancellationTokenSource? propietariosCts;
        private PropietarioResponse? propietario;
        private PropietarioTerrenoResumenResponse? terrenoSeleccionado;
        private string textoBusquedaDestino = string.Empty;
        private bool mostrarSelectorPropietario;
        private bool cargandoMasPropietarios;
        private int paginaPropietarios;
        private int totalPaginasPropietarios;
        private int totalPropietarios;

        public PropietarioTerrenosViewModel()
        {
            RegresarCommand = new Command(
                async () => await RegresarAsync(),
                () => !IsBusy && !CargandoMasPropietarios);

            ActualizarCommand = new Command(
                async () => await CargarDetalleAsync(),
                () => !IsBusy && !CargandoMasPropietarios);

            CambiarPropietarioCommand =
                new Command<PropietarioTerrenoResumenResponse>(
                    async terreno => await AbrirSelectorAsync(terreno),
                    terreno =>
                        terreno != null &&
                        terreno.Activo &&
                        CanEdit &&
                        !IsBusy &&
                        !CargandoMasPropietarios);

            BuscarPropietariosCommand = new Command(
                async () => await CargarPropietariosDestinoAsync(true),
                () => MostrarSelectorPropietario && !IsBusy);

            CargarMasPropietariosCommand = new Command(
                async () => await CargarPropietariosDestinoAsync(false),
                () =>
                    MostrarSelectorPropietario &&
                    !IsBusy &&
                    !CargandoMasPropietarios &&
                    PuedeCargarMasPropietarios);

            SeleccionarPropietarioCommand =
                new Command<PropietarioResponse>(
                    async propietarioDestino =>
                        await ReasignarAsync(propietarioDestino),
                    propietarioDestino =>
                        propietarioDestino != null &&
                        propietarioDestino.Activo &&
                        propietarioDestino.PropietarioId > 0 &&
                        propietarioDestino.PropietarioId !=
                            Propietario?.PropietarioId &&
                        TerrenoSeleccionado != null &&
                        CanEdit &&
                        !IsBusy &&
                        !CargandoMasPropietarios);

            CancelarCambioCommand = new Command(
                CerrarSelector,
                () => MostrarSelectorPropietario && !IsBusy);

            Terrenos.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HayTerrenos));
                OnPropertyChanged(nameof(NoHayTerrenos));
                OnPropertyChanged(nameof(TextoResumenTerrenos));
            };

            PropietariosDestino.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HayPropietariosDestino));
                OnPropertyChanged(nameof(NoHayPropietariosDestino));
            };
        }

        public ObservableCollection<PropietarioTerrenoResumenResponse>
            Terrenos { get; } = new();

        public ObservableCollection<PropietarioResponse>
            PropietariosDestino { get; } = new();

        public Command RegresarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<PropietarioTerrenoResumenResponse>
            CambiarPropietarioCommand { get; }
        public Command BuscarPropietariosCommand { get; }
        public Command CargarMasPropietariosCommand { get; }
        public Command<PropietarioResponse>
            SeleccionarPropietarioCommand { get; }
        public Command CancelarCambioCommand { get; }

        public PropietarioResponse? Propietario
        {
            get => propietario;
            set
            {
                if (ReferenceEquals(propietario, value))
                    return;

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

        public PropietarioTerrenoResumenResponse? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            private set
            {
                if (ReferenceEquals(terrenoSeleccionado, value))
                    return;

                terrenoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoTerrenoSeleccionado));
                ActualizarComandos();
            }
        }

        public string TextoBusquedaDestino
        {
            get => textoBusquedaDestino;
            set
            {
                string nuevo = value ?? string.Empty;

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
                if (mostrarSelectorPropietario == value)
                    return;

                mostrarSelectorPropietario = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public bool CargandoMasPropietarios
        {
            get => cargandoMasPropietarios;
            private set
            {
                if (cargandoMasPropietarios == value)
                    return;

                cargandoMasPropietarios = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public string Titulo => "Terrenos del propietario";
        public string TextoPropietario =>
            Propietario?.TextoPrincipal ?? "Propietario";
        public string TextoIdentificacion =>
            Propietario?.TextoIdentificacion ?? "Sin identificación";
        public string TextoContacto =>
            Propietario?.TextoContacto ?? "Sin contacto registrado";
        public string TextoEstadoPropietario =>
            Propietario?.TextoEstado ?? string.Empty;
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

        public bool HayTerrenos => Terrenos.Count > 0;
        public bool NoHayTerrenos => Terrenos.Count == 0;
        public bool HayPropietariosDestino =>
            PropietariosDestino.Count > 0;
        public bool NoHayPropietariosDestino =>
            PropietariosDestino.Count == 0 &&
            !IsBusy &&
            !CargandoMasPropietarios;
        public bool PuedeCargarMasPropietarios =>
            paginaPropietarios < totalPaginasPropietarios;
        public bool MostrarFinPropietarios =>
            PropietariosDestino.Count > 0 &&
            !PuedeCargarMasPropietarios &&
            !CargandoMasPropietarios;

        public new bool CanView =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance.HasUpdate(
                InterfazCodigos.Propietarios);

        public async Task InicializarAsync() =>
            await CargarDetalleAsync();

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref propietariosCts, null);

            CancelarSeguro(source);
            IsBusy = false;
            CargandoMasPropietarios = false;
            ActualizarComandos();
        }

        private async Task CargarDetalleAsync()
        {
            if (IsBusy)
                return;

            int propietarioId = Propietario?.PropietarioId ?? 0;

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
                    await terrenosService.ObtenerDetalleAsync(propietarioId);

                if (!resultado.Success ||
                    resultado.Data?.Propietario == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                PropietarioResponse propietarioActual =
                    resultado.Data.Propietario;

                propietarioActual.TotalTerrenos =
                    resultado.Data.Terrenos.Count;

                Propietario = propietarioActual;
                Terrenos.Clear();

                foreach (PropietarioTerrenoResumenResponse terreno
                         in resultado.Data.Terrenos)
                {
                    Terrenos.Add(terreno);
                }

                OnPropertyChanged(nameof(TextoResumenTerrenos));
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

            TerrenoSeleccionado = terreno;
            TextoBusquedaDestino = string.Empty;
            MostrarSelectorPropietario = true;
            await CargarPropietariosDestinoAsync(true);
        }

        private async Task CargarPropietariosDestinoAsync(bool reiniciar)
        {
            if (IsBusy || !MostrarSelectorPropietario)
                return;

            if (!reiniciar &&
                (CargandoMasPropietarios ||
                 !PuedeCargarMasPropietarios))
            {
                return;
            }

            CancellationTokenSource source = PrepararCargaPropietarios();

            try
            {
                if (reiniciar)
                {
                    CambiarEstadoOcupado(true);
                }
                else
                {
                    CargandoMasPropietarios = true;
                }

                int pagina = reiniciar
                    ? 1
                    : paginaPropietarios + 1;

                ApiResult<PropietarioPaginaResponse> resultado =
                    await propietarioApiService.BuscarPaginadoAsync(
                        TextoBusquedaDestino,
                        incluirInactivos: false,
                        paraSeleccionTerreno: true,
                        pagina: pagina,
                        tamanoPagina: ObtenerTamanoPaginaPropietarios(),
                        cancellationToken: source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaPropietariosActual(source))
                {
                    return;
                }

                if (!resultado.Success || resultado.Data == null)
                {
                    if (!EsCancelacion(resultado.Message))
                        await MostrarErrorAsync(resultado.Message);

                    return;
                }

                AplicarPaginaPropietarios(
                    resultado.Data,
                    reiniciar);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (EsCargaPropietariosActual(source))
                {
                    if (reiniciar)
                        CambiarEstadoOcupado(false);
                    else
                        CargandoMasPropietarios = false;
                }

                LiberarCargaPropietarios(source);
            }
        }

        private void AplicarPaginaPropietarios(
            PropietarioPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                PropietariosDestino.Clear();

            int propietarioActualId =
                Propietario?.PropietarioId ?? 0;

            HashSet<int> ids = PropietariosDestino
                .Select(item => item.PropietarioId)
                .ToHashSet();

            foreach (PropietarioResponse item in pagina.Items)
            {
                if (!item.Activo ||
                    item.PropietarioId == propietarioActualId ||
                    !ids.Add(item.PropietarioId))
                {
                    continue;
                }

                PropietariosDestino.Add(item);
            }

            paginaPropietarios = Math.Max(1, pagina.Pagina);
            totalPaginasPropietarios =
                Math.Max(0, pagina.TotalPaginas);
            totalPropietarios = Math.Max(
                0,
                pagina.TotalRegistros -
                (pagina.Items.Any(x =>
                    x.PropietarioId == propietarioActualId)
                    ? 1
                    : 0));

            OnPropertyChanged(nameof(PuedeCargarMasPropietarios));
            OnPropertyChanged(nameof(MostrarFinPropietarios));
            OnPropertyChanged(nameof(TextoResumenPropietarios));
            OnPropertyChanged(nameof(NoHayPropietariosDestino));
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

            bool confirmar = await ConfirmarAsync(
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
                    await terrenosService.ReasignarTerrenoAsync(
                        propietarioDestino.PropietarioId,
                        terreno.TerrenoId);

                if (!resultado.Success || resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                Terrenos.Remove(terreno);

                if (Propietario != null)
                {
                    Propietario.TotalTerrenos = Terrenos.Count;
                    OnPropertyChanged(nameof(TextoResumenTerrenos));
                }

                CerrarSelector();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
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

            await GoToAsyncParameters(AppRoutes.Regresar);
        }

        private void CerrarSelector()
        {
            CancelarCarga();
            MostrarSelectorPropietario = false;
            TerrenoSeleccionado = null;
            TextoBusquedaDestino = string.Empty;
            PropietariosDestino.Clear();
            paginaPropietarios = 0;
            totalPaginasPropietarios = 0;
            totalPropietarios = 0;
            OnPropertyChanged(nameof(TextoResumenPropietarios));
            OnPropertyChanged(nameof(MostrarFinPropietarios));
        }

        private void CambiarEstadoOcupado(bool valor)
        {
            IsBusy = valor;
            ActualizarComandos();
            OnPropertyChanged(nameof(NoHayPropietariosDestino));
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            CambiarPropietarioCommand.ChangeCanExecute();
            BuscarPropietariosCommand.ChangeCanExecute();
            CargarMasPropietariosCommand.ChangeCanExecute();
            SeleccionarPropietarioCommand.ChangeCanExecute();
            CancelarCambioCommand.ChangeCanExecute();
        }

        private static int ObtenerTamanoPaginaPropietarios() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 32
                : 14;

        private CancellationTokenSource PrepararCargaPropietarios()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref propietariosCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaPropietariosActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref propietariosCts),
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

        private static bool EsCancelacion(string? mensaje) =>
            !string.IsNullOrWhiteSpace(mensaje) &&
            mensaje.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
