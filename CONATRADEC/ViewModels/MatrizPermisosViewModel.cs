using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
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

        private RolResponse? rolSeleccionado;
        private string filtro = string.Empty;
        private string estado =
            "Seleccione un rol para consultar sus permisos.";
        private bool usuarioPuedeEditar;
        private int versionFiltro;

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
                    async () => await GoToAsyncParameters(
                        AppRoutes.Configuracion),
                    () => !IsBusy);

            RefrescarCommand =
                new Command(
                    async () => await CargarRolesAsync(),
                    () => !IsBusy);

            GuardarCommand =
                new Command(
                    async () => await GuardarAsync(),
                    () => PuedeGuardar);

            RevertirCambiosCommand =
                new Command(
                    RevertirCambios,
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
            set
            {
                if (rolSeleccionado?.RolId == value?.RolId)
                    return;

                rolSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsAdministrador));
                OnPropertyChanged(nameof(PuedeEditar));

                CancelarPermisos();
                LimpiarPermisos();

                if (value?.RolId is > 0)
                {
                    Estado =
                        $"Cargando permisos para {value.NombreMostrar}...";

                    _ = CargarPermisosAsync(value);
                }
                else
                {
                    Estado =
                        "Seleccione un rol para consultar sus permisos.";
                }

                ActualizarComandos();
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

        public bool EsAdministrador
        {
            get
            {
                string nombre =
                    RolSeleccionado?.NombreRol?
                        .Trim()
                        .ToUpperInvariant() ??
                    string.Empty;

                return nombre is
                    "ADMINISTRADOR" or
                    "ADMIN";
            }
        }

        public bool PuedeEditar =>
            UsuarioPuedeEditar &&
            !EsAdministrador &&
            !IsBusy &&
            RolSeleccionado?.RolId is > 0;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public bool TienePermisos =>
            PermisosVisibles.Count > 0;

        public bool MostrarVacio =>
            RolSeleccionado != null &&
            !IsBusy &&
            PermisosVisibles.Count == 0;

        public bool PuedeGuardar =>
            PuedeEditar &&
            Permisos.Any(item => item.IsDirty);

        public bool PuedeRevertir =>
            !IsBusy &&
            Permisos.Any(item => item.IsDirty);

        public void ActualizarPermisosPagina()
        {
            LoadPagePermissions("matrizPermisosPage");
            UsuarioPuedeEditar = CanEdit;

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();
        }

        public async Task InicializarAsync()
        {
            if (Roles.Count == 0)
                await CargarRolesAsync();
        }

        public void CancelarOperaciones()
        {
            CancelarYDescartar(ref cargaRolesCts);
            CancelarYDescartar(ref cargaPermisosCts);
            CancelarYDescartar(ref guardarCts);
        }

        private async Task CargarRolesAsync()
        {
            CancelarYDescartar(ref cargaRolesCts);
            cargaRolesCts = new CancellationTokenSource();
            CancellationToken token = cargaRolesCts.Token;

            try
            {
                IsBusy = true;
                Estado = "Cargando roles...";
                ActualizarComandos();

                ApiResult<RolAdministracionPaginaResponse> resultado =
                    await consultaApiService.BuscarRolesAsync(
                        null,
                        1,
                        100,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    Estado = resultado.Message;
                    return;
                }

                var rolesCargados =
                    new List<RolResponse>(
                        resultado.Data.Items);

                for (int pagina = 2;
                     pagina <= resultado.Data.TotalPaginas;
                     pagina++)
                {
                    ApiResult<RolAdministracionPaginaResponse>
                        paginaResultado =
                            await consultaApiService
                                .BuscarRolesAsync(
                                    null,
                                    pagina,
                                    100,
                                    token);

                    if (token.IsCancellationRequested)
                        return;

                    if (!paginaResultado.Success ||
                        paginaResultado.Data == null)
                    {
                        Estado = paginaResultado.Message;
                        return;
                    }

                    rolesCargados.AddRange(
                        paginaResultado.Data.Items);
                }

                int? rolAnterior = RolSeleccionado?.RolId;

                /*
                 * Se limpia la selección antes de reemplazar los objetos
                 * para que un refresco recargue también los permisos.
                 */
                RolSeleccionado = null;
                Roles.Clear();

                foreach (RolResponse rol in rolesCargados)
                    Roles.Add(rol);

                RolSeleccionado =
                    rolAnterior is > 0
                        ? Roles.FirstOrDefault(item =>
                            item.RolId == rolAnterior)
                        : null;

                Estado =
                    Roles.Count == 0
                        ? "No existen roles activos."
                        : "Seleccione un rol para consultar sus permisos.";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Estado =
                    "No fue posible cargar los roles.";

                await MostrarErrorInesperadoAsync(
                    "cargar los roles de la matriz",
                    ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task CargarPermisosAsync(
            RolResponse rolSolicitado)
        {
            if (rolSolicitado.RolId is not > 0)
                return;

            CancelarYDescartar(ref cargaPermisosCts);
            cargaPermisosCts = new CancellationTokenSource();
            CancellationToken token = cargaPermisosCts.Token;
            int rolId = rolSolicitado.RolId.Value;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<MatrizPermisosResponse> resultado =
                    await matrizApiService
                        .GetMatrizByRolIdResultAsync(
                            rolId,
                            token);

                if (token.IsCancellationRequested ||
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

                LimpiarPermisos();

                foreach (InterfazResponse item
                         in resultado.Data.Interfaz
                             .OrderBy(item =>
                                 item.NombreMostrar)
                             .ThenBy(item =>
                                 item.NombreInterfaz))
                {
                    /*
                     * Se conserva el comportamiento anterior del rol
                     * Administrador: lectura y actualización siempre
                     * visibles y la fila permanece protegida.
                     */
                    if (EsAdministrador)
                    {
                        item.Leer = true;
                        item.Actualizar = true;
                    }

                    item.CanEdit =
                        UsuarioPuedeEditar &&
                        !EsAdministrador;

                    item.AcceptChanges();
                    item.PropertyChanged += Item_PropertyChanged;

                    Permisos.Add(item);

                    snapshot[item.InterfazId] =
                        PermisoSnapshot.From(item);
                }

                AplicarFiltro();
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
                IsBusy = false;
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar ||
                RolSeleccionado == null)
            {
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Guardar permisos",
                        $"¿Desea actualizar los permisos de " +
                        $"'{RolSeleccionado.NombreMostrar}'?",
                        "Guardar",
                        "Cancelar");

            if (!confirmar)
                return;

            CancelarYDescartar(ref guardarCts);
            guardarCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                Estado = "Guardando permisos...";
                ActualizarComandos();

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
                        guardarCts.Token);

                if (!resultado.Success)
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
                Estado =
                    "No fue posible guardar los permisos.";

                await MostrarErrorInesperadoAsync(
                    "guardar la matriz de permisos",
                    ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private void MarcarColumna(string? columna)
        {
            if (!PuedeEditar)
                return;

            string codigo =
                (columna ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            foreach (InterfazResponse item in Permisos)
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
            Estado = "Hay cambios pendientes por guardar.";
            ActualizarComandos();
        }

        private void RevertirCambios()
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

            Estado = "Cambios revertidos.";
            ActualizarComandos();
        }

        private async Task AplicarFiltroConRetrasoAsync(
            int version)
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

        private void AplicarFiltro()
        {
            string texto =
                Filtro.Trim();

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

        private void ActualizarEdicionItems()
        {
            foreach (InterfazResponse item in Permisos)
            {
                item.CanEdit =
                    UsuarioPuedeEditar &&
                    !EsAdministrador;
            }
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
                    Permisos.Any(item => item.IsDirty)
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

        private void CancelarPermisos()
        {
            CancelarYDescartar(ref cargaPermisosCts);
        }

        private static void CancelarYDescartar(
            ref CancellationTokenSource? source)
        {
            CancellationTokenSource? actual =
                Interlocked.Exchange(ref source, null);

            if (actual == null)
                return;

            try
            {
                actual.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            actual.Dispose();
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
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(TienePermisos));
            OnPropertyChanged(nameof(MostrarVacio));
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
    }
}
