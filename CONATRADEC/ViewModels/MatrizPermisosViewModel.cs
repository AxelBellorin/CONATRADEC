using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administración de la matriz de permisos.
    ///
    /// Esta pantalla es un caso especial: roles e interfaces forman catálogos
    /// pequeños que deben trabajarse como una unidad, por lo que no se fuerza
    /// paginación. Las matrices consultadas se reutilizan únicamente durante la
    /// visita actual y se liberan al abandonar el módulo.
    /// </summary>
    public sealed class MatrizPermisosViewModel : GlobalService
    {
        private readonly AdministracionConsultaApiService
            consultaApiService = new();

        private readonly MatrizPermisosApiService
            matrizApiService = new();

        private CancellationTokenSource? cargaRolesCts;
        private CancellationTokenSource? cargaPermisosCts;
        private CancellationTokenSource? guardarCts;

        private readonly Dictionary<int, PermisoSnapshot>
            snapshot = new();

        private readonly Dictionary<int, List<InterfazCacheItem>>
            matricesVisita = new();

        private RolResponse? rolSeleccionado;
        private string filtro = string.Empty;
        private string estado =
            "Seleccione un rol para consultar sus permisos.";
        private bool usuarioPuedeEditar;
        private bool visitaActiva;
        private int versionFiltro;
        private int operacionesActivas;

        public ObservableCollection<RolResponse>
            Roles { get; } = new();

        public ObservableCollection<InterfazResponse>
            Permisos { get; } = new();

        public ObservableCollection<InterfazResponse>
            PermisosVisibles { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command RefrescarCommand { get; }
        public Command GuardarCommand { get; }
        public Command RevertirCambiosCommand { get; }
        public Command<string> MarcarColumnaCommand { get; }
        public Command LimpiarTodoCommand { get; }
        public Command<InterfazResponse> MarcarFilaCommand { get; }
        public Command<InterfazResponse> LimpiarFilaCommand { get; }

        public MatrizPermisosViewModel()
        {
            RegresarConfiguracionCommand =
                new Command(
                    async () =>
                        await IntentarRegresarConfiguracionAsync(),
                    () => !IsBusy);

            RefrescarCommand =
                new Command(
                    async () => await RefrescarAsync(),
                    () => !IsBusy);

            GuardarCommand =
                new Command(
                    async () => await GuardarAsync(),
                    () => PuedeGuardar);

            RevertirCambiosCommand =
                new Command(
                    () => RevertirCambios(mostrarEstado: true),
                    () => PuedeRevertir);

            MarcarColumnaCommand =
                new Command<string>(
                    MarcarColumna,
                    _ => PuedeEditar);

            LimpiarTodoCommand =
                new Command(
                    () => MarcarColumna("ninguno"),
                    () => PuedeEditar);

            MarcarFilaCommand =
                new Command<InterfazResponse>(
                    item => MarcarFila(item, true),
                    item =>
                        item != null &&
                        item.CanEdit &&
                        PuedeEditar);

            LimpiarFilaCommand =
                new Command<InterfazResponse>(
                    item => MarcarFila(item, false),
                    item =>
                        item != null &&
                        item.CanEdit &&
                        PuedeEditar);
        }

        public RolResponse? RolSeleccionado
        {
            get => rolSeleccionado;
            private set
            {
                if (rolSeleccionado?.RolId == value?.RolId)
                    return;

                rolSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsAdministrador));
                OnPropertyChanged(nameof(TieneRolSeleccionado));
                OnPropertyChanged(nameof(PuedeEditar));

                ActualizarEdicionItems();
                ActualizarComandos();
                NotificarEstado();
            }
        }

        public string Filtro
        {
            get => filtro;
            set
            {
                string nuevo = value ?? string.Empty;

                if (filtro == nuevo)
                    return;

                filtro = nuevo;
                OnPropertyChanged();

                int version =
                    Interlocked.Increment(ref versionFiltro);

                _ = AplicarFiltroConRetrasoAsync(version);
            }
        }

        public string Estado
        {
            get => estado;
            private set
            {
                string nuevo = value ?? string.Empty;

                if (estado == nuevo)
                    return;

                estado = nuevo;
                OnPropertyChanged();
            }
        }

        public bool UsuarioPuedeEditar
        {
            get => usuarioPuedeEditar;
            set
            {
                if (usuarioPuedeEditar == value)
                    return;

                usuarioPuedeEditar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeEditar));

                ActualizarEdicionItems();
                ActualizarComandos();
            }
        }

        public bool EsAdministrador =>
            EsRolAdministrador(RolSeleccionado);

        public bool TieneRolSeleccionado =>
            RolSeleccionado?.RolId is > 0;

        public bool PuedeEditar =>
            UsuarioPuedeEditar &&
            !EsAdministrador &&
            !IsBusy &&
            TieneRolSeleccionado;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public bool TienePermisos =>
            PermisosVisibles.Count > 0;

        public bool MostrarVacio =>
            TieneRolSeleccionado &&
            !IsBusy &&
            PermisosVisibles.Count == 0;

        public bool TieneCambiosPendientes =>
            Permisos.Any(item => item.IsDirty);

        public bool PuedeGuardar =>
            PuedeEditar &&
            TieneCambiosPendientes;

        public bool PuedeRevertir =>
            !IsBusy &&
            TieneCambiosPendientes;

        public void ActualizarPermisosPagina()
        {
            LoadPagePermissions(InterfazCodigos.MatrizPermisos);
            UsuarioPuedeEditar = CanEdit;

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();
        }

        /// <summary>
        /// Comienza una visita fresca. Como la página es un ShellContent y su
        /// instancia puede sobrevivir a la navegación, se limpian expresamente
        /// roles, filtros y matrices retenidas de una visita anterior.
        /// </summary>
        public async Task IniciarVisitaAsync()
        {
            if (visitaActiva)
                return;

            visitaActiva = true;
            CancelarOperaciones();
            matricesVisita.Clear();
            LimpiarPermisos();
            Roles.Clear();
            RolSeleccionado = null;
            EstablecerFiltroSinRetraso(string.Empty);

            Estado =
                "Seleccione un rol para consultar sus permisos.";

            if (CanView)
                await CargarRolesAsync(rolASeleccionarId: null);
        }

        /// <summary>
        /// Finaliza la visita y libera todas las referencias que solo tienen
        /// sentido dentro del módulo actual.
        /// </summary>
        public void FinalizarVisita()
        {
            visitaActiva = false;
            CancelarOperaciones();
            Interlocked.Increment(ref versionFiltro);

            matricesVisita.Clear();
            LimpiarPermisos();
            Roles.Clear();
            RolSeleccionado = null;
            EstablecerFiltroSinRetraso(string.Empty);

            Estado =
                "Seleccione un rol para consultar sus permisos.";
        }

        public void CancelarOperaciones()
        {
            CancelarActual(ref cargaRolesCts);
            CancelarActual(ref cargaPermisosCts);
            CancelarActual(ref guardarCts);
        }

        /// <summary>
        /// Cambio de rol solicitado por el Picker. Si existen cambios sin
        /// guardar, el usuario debe decidir explícitamente si desea descartarlos.
        /// </summary>
        public async Task<bool> CambiarRolAsync(
            RolResponse? nuevoRol)
        {
            int? actualId = RolSeleccionado?.RolId;
            int? nuevoId = nuevoRol?.RolId;

            if (actualId == nuevoId)
                return true;

            if (TieneCambiosPendientes)
            {
                bool descartar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!descartar)
                    return false;
            }

            CancelarActual(ref cargaPermisosCts);
            LimpiarPermisos();
            RolSeleccionado = nuevoRol;

            if (nuevoRol?.RolId is not > 0)
            {
                Estado =
                    "Seleccione un rol para consultar sus permisos.";
                return true;
            }

            int rolId = nuevoRol.RolId.Value;

            if (matricesVisita.TryGetValue(
                    rolId,
                    out List<InterfazCacheItem>? cache))
            {
                MostrarPermisosDesdeCache(cache);
                Estado =
                    $"Permisos cargados para {nuevoRol.NombreMostrar}.";
                return true;
            }

            Estado =
                $"Cargando permisos para {nuevoRol.NombreMostrar}...";

            await CargarPermisosAsync(nuevoRol);
            return RolSeleccionado?.RolId == rolId;
        }

        public async Task<bool> IntentarRegresarConfiguracionAsync()
        {
            if (IsBusy)
                return false;

            if (TieneCambiosPendientes)
            {
                bool descartar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!descartar)
                    return false;

                // El usuario confirmó que no conservará estos cambios.
                RevertirCambios(mostrarEstado: false);
            }

            await GoToAsyncParameters(AppRoutes.Configuracion);
            return true;
        }

        /// <summary>
        /// Protege navegaciones iniciadas desde el menú global del FooterTemplate.
        /// Las navegaciones de seguridad hacia Login se excluyen en el code-behind.
        /// </summary>
        public async Task<bool> ConfirmarDescarteParaNavegacionExternaAsync()
        {
            if (!TieneCambiosPendientes)
                return true;

            bool descartar =
                await ConfirmarSalidaSinGuardarAsync();

            if (!descartar)
                return false;

            RevertirCambios(mostrarEstado: false);
            return true;
        }

        private async Task RefrescarAsync()
        {
            if (IsBusy)
                return;

            if (TieneCambiosPendientes)
            {
                bool descartar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!descartar)
                    return;
            }

            int? rolAnteriorId = RolSeleccionado?.RolId;

            CancelarActual(ref cargaPermisosCts);
            matricesVisita.Clear();
            LimpiarPermisos();
            RolSeleccionado = null;

            await CargarRolesAsync(rolAnteriorId);
        }

        private async Task CargarRolesAsync(
            int? rolASeleccionarId)
        {
            CancellationTokenSource source =
                CrearOperacion(ref cargaRolesCts);

            CancellationToken token = source.Token;
            IniciarOperacionOcupada();

            try
            {
                Estado = "Cargando roles...";

                ApiResult<List<RolResponse>> resultado =
                    await consultaApiService
                        .ObtenerRolesMatrizAsync(token);

                if (token.IsCancellationRequested ||
                    !visitaActiva)
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    Estado = resultado.Message;
                    return;
                }

                Roles.Clear();

                foreach (RolResponse rol in resultado.Data
                             .Where(item => item.RolId is > 0)
                             .OrderBy(item => item.NombreMostrar)
                             .ThenBy(item => item.RolId))
                {
                    Roles.Add(rol);
                }

                if (Roles.Count == 0)
                {
                    RolSeleccionado = null;
                    Estado = "No existen roles activos.";
                    return;
                }

                if (rolASeleccionarId is > 0)
                {
                    RolResponse? anterior =
                        Roles.FirstOrDefault(item =>
                            item.RolId == rolASeleccionarId);

                    if (anterior != null)
                    {
                        RolSeleccionado = anterior;
                        Estado =
                            $"Cargando permisos para {anterior.NombreMostrar}...";

                        await CargarPermisosAsync(anterior);
                        return;
                    }
                }

                Estado =
                    "Seleccione un rol para consultar sus permisos.";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Estado = "No fue posible cargar los roles.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los roles de la matriz",
                        ex);
                }
            }
            finally
            {
                LiberarOperacion(
                    ref cargaRolesCts,
                    source);

                FinalizarOperacionOcupada();
            }
        }

        private async Task CargarPermisosAsync(
            RolResponse rolSolicitado)
        {
            if (!visitaActiva ||
                rolSolicitado.RolId is not > 0)
            {
                return;
            }

            int rolId = rolSolicitado.RolId.Value;

            if (matricesVisita.TryGetValue(
                    rolId,
                    out List<InterfazCacheItem>? cache))
            {
                if (RolSeleccionado?.RolId == rolId)
                {
                    MostrarPermisosDesdeCache(cache);
                    Estado =
                        $"Permisos cargados para {rolSolicitado.NombreMostrar}.";
                }

                return;
            }

            CancellationTokenSource source =
                CrearOperacion(ref cargaPermisosCts);

            CancellationToken token = source.Token;
            IniciarOperacionOcupada();

            try
            {
                ApiResult<MatrizPermisosResponse> resultado =
                    await matrizApiService
                        .GetMatrizByRolIdResultAsync(
                            rolId,
                            token);

                if (token.IsCancellationRequested ||
                    !visitaActiva ||
                    RolSeleccionado?.RolId != rolId)
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data?.Interfaz == null)
                {
                    Estado = resultado.Message;
                    return;
                }

                List<InterfazCacheItem> cacheNueva =
                    ConsolidarRespuesta(
                        resultado.Data.Interfaz);

                matricesVisita[rolId] = cacheNueva;
                MostrarPermisosDesdeCache(cacheNueva);

                Estado =
                    $"Permisos cargados para {rolSolicitado.NombreMostrar}.";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested &&
                    RolSeleccionado?.RolId == rolId)
                {
                    Estado =
                        "No fue posible cargar los permisos.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los permisos del rol",
                        ex);
                }
            }
            finally
            {
                LiberarOperacion(
                    ref cargaPermisosCts,
                    source);

                FinalizarOperacionOcupada();
            }
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar ||
                RolSeleccionado?.RolId is not > 0)
            {
                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Guardar permisos",
                    $"¿Desea actualizar los permisos de " +
                    $"'{RolSeleccionado.NombreMostrar}'?",
                    "Guardar",
                    "Cancelar");

            if (!confirmar)
                return;

            CancellationTokenSource source =
                CrearOperacion(ref guardarCts);

            CancellationToken token = source.Token;
            int rolId = RolSeleccionado.RolId.Value;
            IniciarOperacionOcupada();

            try
            {
                Estado = "Guardando permisos...";

                var request =
                    new MatrizPermisosRequest
                    {
                        Rol = new RolRequest(RolSeleccionado),
                        Interfaz =
                            Permisos
                                .Select(item =>
                                    new InterfazRequest(item))
                                .ToList()
                    };

                ApiResult<bool> resultado =
                    await matrizApiService.GuardarMatrizResultAsync(
                        request,
                        token);

                if (token.IsCancellationRequested ||
                    RolSeleccionado?.RolId != rolId)
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    Estado = resultado.Message;
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                snapshot.Clear();

                foreach (InterfazResponse item in Permisos)
                {
                    item.AcceptChanges();
                    snapshot[item.InterfazId] =
                        PermisoSnapshot.From(item);
                }

                matricesVisita[rolId] =
                    CrearCacheDesdePermisos(Permisos);

                Estado =
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Permisos guardados correctamente."
                        : resultado.Message;

                await MostrarToastAsync(Estado);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Estado =
                        "No fue posible guardar los permisos.";

                    await MostrarErrorInesperadoAsync(
                        "guardar la matriz de permisos",
                        ex);
                }
            }
            finally
            {
                LiberarOperacion(
                    ref guardarCts,
                    source);

                FinalizarOperacionOcupada();
            }
        }

        /// <summary>
        /// Las acciones masivas se aplican únicamente sobre las interfaces que
        /// el usuario está viendo. De esta forma un filtro nunca modifica filas
        /// ocultas de manera sorpresiva.
        /// </summary>
        private void MarcarColumna(string? columna)
        {
            if (!PuedeEditar)
                return;

            string codigo =
                (columna ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            foreach (InterfazResponse item
                     in PermisosVisibles.ToList())
            {
                if (!item.CanEdit)
                    continue;

                switch (codigo)
                {
                    case "leer":
                        item.Leer = true;
                        break;
                    case "agregar":
                        item.Agregar = true;
                        break;
                    case "actualizar":
                        item.Actualizar = true;
                        break;
                    case "eliminar":
                        item.Eliminar = true;
                        break;
                    case "ninguno":
                        item.SetAll(false);
                        break;
                }
            }

            if (TieneCambiosPendientes)
                Estado = "Hay cambios pendientes por guardar.";

            ActualizarComandos();
        }

        private void MarcarFila(
            InterfazResponse? item,
            bool valor)
        {
            if (item == null ||
                !item.CanEdit ||
                !PuedeEditar)
            {
                return;
            }

            item.SetAll(valor);

            if (TieneCambiosPendientes)
                Estado = "Hay cambios pendientes por guardar.";

            ActualizarComandos();
        }

        private void RevertirCambios(bool mostrarEstado)
        {
            foreach (InterfazResponse item in Permisos)
            {
                if (!snapshot.TryGetValue(
                        item.InterfazId,
                        out PermisoSnapshot valor))
                {
                    continue;
                }

                item.Leer = valor.Leer;
                item.Agregar = valor.Agregar;
                item.Actualizar = valor.Actualizar;
                item.Eliminar = valor.Eliminar;
                item.AcceptChanges();
            }

            if (mostrarEstado)
                Estado = "Cambios revertidos.";

            ActualizarComandos();
        }

        private async Task AplicarFiltroConRetrasoAsync(
            int version)
        {
            try
            {
                await Task.Delay(250);

                if (version !=
                    Volatile.Read(ref versionFiltro))
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(
                    AplicarFiltro);
            }
            catch
            {
                // El filtro es local y nunca debe interrumpir la pantalla.
            }
        }

        private void AplicarFiltro()
        {
            string texto = Filtro.Trim();

            PermisosVisibles.Clear();

            IEnumerable<InterfazResponse> consulta =
                string.IsNullOrWhiteSpace(texto)
                    ? Permisos
                    : Permisos.Where(item =>
                        item.NombreMostrar.Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase) ||
                        item.NombreInterfaz.Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase));

            foreach (InterfazResponse item in consulta)
                PermisosVisibles.Add(item);

            NotificarEstado();
        }

        private void MostrarPermisosDesdeCache(
            IEnumerable<InterfazCacheItem> cache)
        {
            LimpiarPermisos();

            bool administrador = EsAdministrador;

            foreach (InterfazCacheItem dato in cache
                         .OrderBy(item => item.NombreMostrar)
                         .ThenBy(item => item.NombreInterfaz))
            {
                var item = new InterfazResponse(
                    dato.InterfazId,
                    dato.NombreInterfaz,
                    dato.NombreAmigableInterfaz,
                    administrador ? true : dato.Leer,
                    dato.Agregar,
                    administrador ? true : dato.Actualizar,
                    dato.Eliminar)
                {
                    CanEdit =
                        UsuarioPuedeEditar &&
                        !administrador
                };

                item.AcceptChanges();
                item.PropertyChanged += Item_PropertyChanged;

                Permisos.Add(item);
                snapshot[item.InterfazId] =
                    PermisoSnapshot.From(item);
            }

            AplicarFiltro();
        }

        private static List<InterfazCacheItem>
            ConsolidarRespuesta(
                IEnumerable<InterfazResponse> interfaces)
        {
            return interfaces
                .Where(item => item.InterfazId > 0)
                .GroupBy(item => item.InterfazId)
                .Select(grupo =>
                {
                    InterfazResponse primero = grupo.First();

                    return new InterfazCacheItem(
                        primero.InterfazId,
                        primero.NombreInterfaz ?? string.Empty,
                        primero.NombreAmigableInterfaz ?? string.Empty,
                        grupo.Any(item => item.Leer),
                        grupo.Any(item => item.Agregar),
                        grupo.Any(item => item.Actualizar),
                        grupo.Any(item => item.Eliminar));
                })
                .OrderBy(item => item.NombreMostrar)
                .ThenBy(item => item.NombreInterfaz)
                .ToList();
        }

        private static List<InterfazCacheItem>
            CrearCacheDesdePermisos(
                IEnumerable<InterfazResponse> permisos) =>
            permisos
                .Select(item =>
                    new InterfazCacheItem(
                        item.InterfazId,
                        item.NombreInterfaz,
                        item.NombreAmigableInterfaz,
                        item.Leer,
                        item.Agregar,
                        item.Actualizar,
                        item.Eliminar))
                .ToList();

        private void ActualizarEdicionItems()
        {
            bool puedeEditar =
                UsuarioPuedeEditar &&
                !EsAdministrador &&
                !IsBusy;

            foreach (InterfazResponse item in Permisos)
                item.CanEdit = puedeEditar;
        }

        private void Item_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is
                nameof(Permiso.Leer) or
                nameof(Permiso.Agregar) or
                nameof(Permiso.Actualizar) or
                nameof(Permiso.Eliminar) or
                nameof(Permiso.IsDirty))
            {
                Estado =
                    TieneCambiosPendientes
                        ? "Hay cambios pendientes por guardar."
                        : "Permisos sin cambios.";

                ActualizarComandos();
            }
        }

        private void LimpiarPermisos()
        {
            foreach (InterfazResponse item in Permisos)
                item.PropertyChanged -= Item_PropertyChanged;

            Permisos.Clear();
            PermisosVisibles.Clear();
            snapshot.Clear();

            NotificarEstado();
        }

        private void EstablecerFiltroSinRetraso(string valor)
        {
            string nuevo = valor ?? string.Empty;

            if (filtro == nuevo)
                return;

            filtro = nuevo;
            Interlocked.Increment(ref versionFiltro);
            OnPropertyChanged(nameof(Filtro));
            AplicarFiltro();
        }

        private void IniciarOperacionOcupada()
        {
            if (Interlocked.Increment(
                    ref operacionesActivas) == 1)
            {
                IsBusy = true;
            }

            ActualizarEdicionItems();
            ActualizarComandos();
            NotificarEstado();
        }

        private void FinalizarOperacionOcupada()
        {
            int restantes =
                Interlocked.Decrement(ref operacionesActivas);

            if (restantes <= 0)
            {
                Interlocked.Exchange(
                    ref operacionesActivas,
                    0);

                IsBusy = false;
            }

            ActualizarEdicionItems();
            ActualizarComandos();
            NotificarEstado();
        }

        private static CancellationTokenSource CrearOperacion(
            ref CancellationTokenSource? destino)
        {
            var nueva = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref destino, nueva);

            CancelarSeguro(anterior);
            return nueva;
        }

        private static void CancelarActual(
            ref CancellationTokenSource? source)
        {
            CancellationTokenSource? actual =
                Interlocked.Exchange(ref source, null);

            CancelarSeguro(actual);
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
        }

        private static void LiberarOperacion(
            ref CancellationTokenSource? destino,
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref destino,
                null,
                source);

            try
            {
                source.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static bool EsRolAdministrador(
            RolResponse? rol)
        {
            string nombre = rol?.NombreMostrar ?? string.Empty;

            return string.Equals(
                       nombre,
                       "ADMINISTRADOR",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       nombre,
                       "ADMIN",
                       StringComparison.OrdinalIgnoreCase);
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            RevertirCambiosCommand.ChangeCanExecute();
            MarcarColumnaCommand.ChangeCanExecute();
            LimpiarTodoCommand.ChangeCanExecute();
            MarcarFilaCommand.ChangeCanExecute();
            LimpiarFilaCommand.ChangeCanExecute();

            OnPropertyChanged(nameof(PuedeEditar));
            OnPropertyChanged(nameof(PuedeGuardar));
            OnPropertyChanged(nameof(PuedeRevertir));
            OnPropertyChanged(nameof(TieneCambiosPendientes));
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(TienePermisos));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(TieneRolSeleccionado));
        }

        private readonly record struct PermisoSnapshot(
            bool Leer,
            bool Agregar,
            bool Actualizar,
            bool Eliminar)
        {
            public static PermisoSnapshot From(
                InterfazResponse item) =>
                new(
                    item.Leer,
                    item.Agregar,
                    item.Actualizar,
                    item.Eliminar);
        }

        private readonly record struct InterfazCacheItem(
            int InterfazId,
            string NombreInterfaz,
            string NombreAmigableInterfaz,
            bool Leer,
            bool Agregar,
            bool Actualizar,
            bool Eliminar)
        {
            public string NombreMostrar =>
                InterfazCodigos.ObtenerNombreAmigable(
                    NombreInterfaz,
                    NombreAmigableInterfaz);
        }
    }
}
