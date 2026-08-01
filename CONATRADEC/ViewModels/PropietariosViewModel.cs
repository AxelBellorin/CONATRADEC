using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(
        nameof(ModoSeleccionTexto),
        "ModoSeleccion")]
    public sealed class PropietariosViewModel : GlobalService
    {
        private readonly PropietarioApiService service = new();
        private readonly PropietarioCrudApiService crudService = new();

        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string? modoSeleccionTexto;
        private bool mostrarEliminados;
        private bool inicializado;
        private bool isRefreshing;
        private bool cargandoMas;
        private int paginaActual;
        private int totalPaginas;
        private int totalRegistros;

        public PropietariosViewModel()
        {
            BuscarCommand = new Command(
                async () => await CargarAsync(reiniciar: true),
                () => PuedeEjecutarCargaInicial);

            ActualizarCommand = new Command(
                async () => await RefrescarAsync(),
                () => PuedeEjecutarCargaInicial);

            CargarMasCommand = new Command(
                async () => await CargarAsync(reiniciar: false),
                () =>
                    !IsBusy &&
                    !CargandoMas &&
                    PuedeCargarMas);

            RegresarCommand = new Command(
                async () => await RegresarAsync(),
                () => !IsBusy && !CargandoMas);

            NuevoCommand = new Command(
                async () => await NuevoAsync(),
                () => CanAdd && !IsBusy && !CargandoMas);

            AbrirCommand = new Command<PropietarioResponse>(
                async propietario => await AbrirAsync(propietario),
                propietario =>
                    propietario != null &&
                    !IsBusy &&
                    !CargandoMas);

            VerCommand = new Command<PropietarioResponse>(
                async propietario => await VerAsync(propietario),
                propietario =>
                    propietario != null &&
                    CanView &&
                    !EsModoSeleccion &&
                    !IsBusy &&
                    !CargandoMas);

            EditarCommand = new Command<PropietarioResponse>(
                async propietario => await EditarAsync(propietario),
                propietario =>
                    propietario != null &&
                    propietario.Activo &&
                    CanEdit &&
                    !EsModoSeleccion &&
                    !IsBusy &&
                    !CargandoMas);

            EliminarCommand = new Command<PropietarioResponse>(
                async propietario => await EliminarAsync(propietario),
                propietario =>
                    propietario != null &&
                    propietario.Activo &&
                    CanDelete &&
                    !EsModoSeleccion &&
                    !IsBusy &&
                    !CargandoMas);

            RecuperarCommand = new Command<PropietarioResponse>(
                async propietario => await RecuperarAsync(propietario),
                propietario =>
                    propietario != null &&
                    !propietario.Activo &&
                    CanEdit &&
                    !EsModoSeleccion &&
                    !IsBusy &&
                    !CargandoMas);

            VerTerrenosCommand = new Command<PropietarioResponse>(
                async propietario => await VerTerrenosAsync(propietario),
                propietario =>
                    propietario != null &&
                    propietario.TotalTerrenos > 0 &&
                    CanView &&
                    !EsModoSeleccion &&
                    !IsBusy &&
                    !CargandoMas);
        }

        public ObservableCollection<PropietarioResponse>
            Propietarios { get; } = new();

        public Command BuscarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command CargarMasCommand { get; }
        public Command RegresarCommand { get; }
        public Command NuevoCommand { get; }
        public Command<PropietarioResponse> AbrirCommand { get; }
        public Command<PropietarioResponse> VerCommand { get; }
        public Command<PropietarioResponse> EditarCommand { get; }
        public Command<PropietarioResponse> EliminarCommand { get; }
        public Command<PropietarioResponse> RecuperarCommand { get; }
        public Command<PropietarioResponse> VerTerrenosCommand { get; }

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

        public bool MostrarEliminados
        {
            get => mostrarEliminados;
            set
            {
                bool nuevo = value && !EsModoSeleccion;

                if (mostrarEliminados == nuevo)
                    return;

                mostrarEliminados = nuevo;
                OnPropertyChanged();

                if (inicializado &&
                    !IsBusy &&
                    !EsModoSeleccion)
                {
                    _ = CargarAsync(reiniciar: true);
                }
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            set
            {
                if (modoSeleccionTexto == value)
                    return;

                modoSeleccionTexto = value;

                if (EsModoSeleccion && mostrarEliminados)
                {
                    mostrarEliminados = false;
                    OnPropertyChanged(nameof(MostrarEliminados));
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(EsModoSeleccion));
                OnPropertyChanged(nameof(MostrarAccionesAdministracion));
                OnPropertyChanged(nameof(MostrarFiltroEliminados));
                OnPropertyChanged(nameof(Titulo));
                OnPropertyChanged(nameof(TextoRegresar));

                if (inicializado && !IsBusy)
                    _ = CargarAsync(reiniciar: true);
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
                ActualizarEstadoLista();
            }
        }

        public bool EsModoSeleccion =>
            bool.TryParse(ModoSeleccionTexto, out bool valor) &&
            valor;

        public bool MostrarAccionesAdministracion =>
            !EsModoSeleccion;

        public bool MostrarFiltroEliminados =>
            !EsModoSeleccion;

        public string Titulo =>
            EsModoSeleccion
                ? "Seleccionar propietario"
                : "Propietarios";

        public string TextoRegresar =>
            EsModoSeleccion
                ? "Cancelar selección"
                : "Configuración";

        public new bool CanView =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Propietarios);

        public new bool CanAdd =>
            PermissionService.Instance.HasAdd(
                InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance.HasUpdate(
                InterfazCodigos.Propietarios);

        public new bool CanDelete =>
            PermissionService.Instance.HasDelete(
                InterfazCodigos.Propietarios);

        public bool MostrarListaVacia =>
            inicializado &&
            !IsBusy &&
            !CargandoMas &&
            Propietarios.Count == 0;

        public bool PuedeCargarMas =>
            paginaActual > 0 &&
            paginaActual < totalPaginas;

        public bool MostrarFinLista =>
            inicializado &&
            Propietarios.Count > 0 &&
            !PuedeCargarMas &&
            !IsBusy &&
            !CargandoMas;

        public string ResumenResultados =>
            totalRegistros == 1
                ? "1 propietario encontrado"
                : $"{totalRegistros:N0} propietarios encontrados";

        private bool PuedeEjecutarCargaInicial =>
            !IsBusy &&
            !CargandoMas &&
            !IsRefreshing;

        public async Task InicializarAsync()
        {
            inicializado = true;
            await CargarAsync(reiniciar: true);
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref cargaCts, null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
            ActualizarComandos();
        }

        private async Task CargarAsync(bool reiniciar)
        {
            if (!ModoSesionService.EsEnLinea)
            {
                CancelarCarga();
                Propietarios.Clear();
                paginaActual = 0;
                totalPaginas = 0;
                totalRegistros = 0;
                ActualizarEstadoLista();

                await MostrarAdvertenciaAsync(
                    "La administración de propietarios requiere conexión a internet.");
                return;
            }

            if (reiniciar && IsBusy)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source = PrepararCarga();

            try
            {
                if (reiniciar)
                    CambiarEstadoOcupado(true);
                else
                    CargandoMas = true;

                int paginaSolicitada = reiniciar
                    ? 1
                    : paginaActual + 1;

                ApiResult<PropietarioPaginaResponse> result =
                    await service.BuscarPaginadoAsync(
                        TextoBusqueda,
                        incluirInactivos:
                            MostrarEliminados &&
                            !EsModoSeleccion,
                        paraSeleccionTerreno:
                            EsModoSeleccion,
                        pagina: paginaSolicitada,
                        tamanoPagina: ObtenerTamanoPagina(),
                        cancellationToken: source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!result.Success || result.Data == null)
                {
                    if (!EsCancelacion(result.Message))
                        await MostrarErrorAsync(result.Message);

                    return;
                }

                AplicarPagina(result.Data, reiniciar);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al cambiar búsqueda o salir de la pantalla.
            }
            catch (ObjectDisposedException)
            {
                // La navegación puede cerrar la operación anterior en Android.
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    if (reiniciar)
                    {
                        CambiarEstadoOcupado(false);
                        IsRefreshing = false;
                    }
                    else
                    {
                        CargandoMas = false;
                    }
                }

                LiberarCarga(source);
                ActualizarComandos();
                ActualizarEstadoLista();
            }
        }

        private void AplicarPagina(
            PropietarioPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                Propietarios.Clear();

            HashSet<int> ids =
                Propietarios
                    .Select(item => item.PropietarioId)
                    .ToHashSet();

            foreach (PropietarioResponse item in pagina.Items)
            {
                if (item.PropietarioId <= 0)
                    continue;

                if (ids.Add(item.PropietarioId))
                    Propietarios.Add(item);
            }

            paginaActual = Math.Max(1, pagina.Pagina);
            totalPaginas = Math.Max(0, pagina.TotalPaginas);
            totalRegistros = Math.Max(0, pagina.TotalRegistros);

            ActualizarEstadoLista();
        }

        private async Task RefrescarAsync()
        {
            if (!PuedeEjecutarCargaInicial)
                return;

            IsRefreshing = true;

            try
            {
                await CargarAsync(reiniciar: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RegresarAsync()
        {
            if (IsBusy || CargandoMas)
                return;

            CancelarCarga();

            if (EsModoSeleccion)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            await GoToAsyncParameters(AppRoutes.Configuracion);
        }

        private async Task NuevoAsync()
        {
            if (!CanAdd)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para crear propietarios.");
                return;
            }

            CancelarCarga();

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["ModoSeleccion"] = EsModoSeleccion.ToString()
                });
        }

        private async Task AbrirAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (EsModoSeleccion)
            {
                if (!propietario.Activo)
                {
                    await MostrarAdvertenciaAsync(
                        "No puede asignar un propietario eliminado.");
                    return;
                }

                PropietarioSeleccionService.Seleccionar(propietario);
                CancelarCarga();
                await Shell.Current.GoToAsync("..");
                return;
            }

            await VerAsync(propietario);
        }

        private async Task VerAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para visualizar propietarios.");
                return;
            }

            CancelarCarga();

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["Propietario"] = propietario,
                    ["ModoSeleccion"] = EsModoSeleccion.ToString()
                });
        }

        private async Task EditarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!propietario.Activo)
            {
                await MostrarAdvertenciaAsync(
                    "Recupere el propietario antes de editarlo.");
                return;
            }

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para editar propietarios.");
                return;
            }

            CancelarCarga();

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Propietario"] = propietario,
                    ["ModoSeleccion"] = EsModoSeleccion.ToString()
                });
        }

        private async Task VerTerrenosAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para visualizar propietarios.");
                return;
            }

            if (propietario.TotalTerrenos <= 0)
            {
                await MostrarInformacionAsync(
                    "El propietario no tiene terrenos vinculados.");
                return;
            }

            CancelarCarga();

            await GoToAsyncParameters(
                AppRoutes.PropietarioTerrenos,
                new Dictionary<string, object>
                {
                    ["Propietario"] = propietario
                });
        }

        private async Task EliminarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (!CanDelete)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para eliminar propietarios.");
                return;
            }

            if (propietario.TotalTerrenos > 0)
            {
                await MostrarAdvertenciaAsync(
                    "No se puede eliminar el propietario porque tiene terrenos vinculados. " +
                    "Utilice Ver terrenos para reasignarlos antes de continuar.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Eliminar propietario",
                $"¿Desea eliminar a {propietario.TextoPrincipal}? " +
                "El registro quedará disponible en Mostrar eliminados.",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            bool recargar = false;
            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<bool> resultado =
                    await crudService.EliminarPropietarioResultAsync(
                        propietario.PropietarioId);

                if (!resultado.Success || resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                recargar = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Propietario eliminado correctamente."
                        : resultado.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }

            if (recargar)
                await CargarAsync(reiniciar: true);
        }

        private async Task RecuperarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            if (propietario.Activo)
            {
                await MostrarInformacionAsync(
                    "El propietario ya se encuentra activo.");
                return;
            }

            if (!CanEdit)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para recuperar propietarios.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Recuperar propietario",
                $"¿Desea recuperar a {propietario.TextoPrincipal}? " +
                "Volverá a estar disponible para crear o reasignar terrenos.",
                "Recuperar",
                "Cancelar");

            if (!confirmar)
                return;

            bool recargar = false;
            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<bool> resultado =
                    await crudService.RecuperarPropietarioResultAsync(
                        propietario.PropietarioId);

                if (!resultado.Success || resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                recargar = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Propietario recuperado correctamente."
                        : resultado.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }

            if (recargar)
                await CargarAsync(reiniciar: true);
        }

        private void CambiarEstadoOcupado(bool valor)
        {
            IsBusy = valor;
            OnPropertyChanged(nameof(MostrarListaVacia));
            ActualizarComandos();
            ActualizarEstadoLista();
        }

        private void ActualizarComandos()
        {
            BuscarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            RegresarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            VerCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            RecuperarCommand.ChangeCanExecute();
            VerTerrenosCommand.ChangeCanExecute();
        }

        private void ActualizarEstadoLista()
        {
            OnPropertyChanged(nameof(MostrarListaVacia));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 36
                : 16;

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);

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

        private static bool EsCancelacion(string? mensaje) =>
            !string.IsNullOrWhiteSpace(mensaje) &&
            mensaje.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
