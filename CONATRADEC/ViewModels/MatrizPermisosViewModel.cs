using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CONATRADEC.ViewModels
{
    public class MatrizPermisosViewModel : GlobalService
    {
        private const string NombreInterfazPermisos = "matrizPermisosPage";

        private readonly MatrizPermisosApiService matrizPermisosApiService;
        private readonly RolApiService rolApiService;
        private readonly List<InterfazResponse> itemsSuscritos = new();

        private ObservableCollection<RolResponse> roles = new();
        private ObservableCollection<InterfazResponse> permisos = new();
        private List<PermisoSnapshot> snapshot = new();

        private CancellationTokenSource? cargaRolesCancellation;
        private CancellationTokenSource? cargaPermisosCancellation;
        private CancellationTokenSource? guardarCancellation;

        private RolResponse? rolSeleccionado;
        private string filtro = string.Empty;
        private string estado = string.Empty;
        private bool usuarioPuedeEditar;
        private bool puedeEditarColumnasBtn;
        private bool inicializado;
        private int operacionesActivas;

        public MatrizPermisosViewModel()
            : this(
                new MatrizPermisosApiService(),
                new RolApiService())
        {
        }

        public MatrizPermisosViewModel(
            MatrizPermisosApiService matrizPermisosApiService,
            RolApiService rolApiService)
        {
            this.matrizPermisosApiService = matrizPermisosApiService
                ?? throw new ArgumentNullException(nameof(matrizPermisosApiService));

            this.rolApiService = rolApiService
                ?? throw new ArgumentNullException(nameof(rolApiService));

            permisos.CollectionChanged += Permisos_CollectionChanged;

            // Asignamos directamente el campo durante la construcción.
            // Usar la propiedad aquí ejecutaría ActualizarHabilitados() antes
            // de que los comandos hayan sido creados.
            usuarioPuedeEditar =
                PermissionService.Instance.HasUpdate(NombreInterfazPermisos);

            RefrescarCommand = new Command(
                async () => await RefrescarAsync(),
                () => !IsBusy);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => PuedeGuardar);

            RevertirCambiosCommand = new Command(
                RevertirCambios,
                () => PuedeRevertir);

            MarcarColumnaCommand = new Command<string>(MarcarColumna);
            LimpiarTodoCommand = new Command(
                () => MarcarColumna("ninguno"));

            MarcarFilaCommand = new Command<InterfazResponse>(MarcarFila);
            LimpiarFilaCommand = new Command<InterfazResponse>(LimpiarFila);

            // Ahora que todos los comandos existen, actualizamos el estado inicial.
            ActualizarEstadoEdicion();
            ActualizarHabilitados();
        }

        public Command RefrescarCommand { get; }
        public Command GuardarCommand { get; }
        public Command RevertirCambiosCommand { get; }
        public Command MarcarColumnaCommand { get; }
        public Command LimpiarTodoCommand { get; }
        public Command MarcarFilaCommand { get; }
        public Command LimpiarFilaCommand { get; }

        public ObservableCollection<RolResponse> Roles
        {
            get => roles;
            private set
            {
                if (ReferenceEquals(roles, value))
                    return;

                roles = value ?? new ObservableCollection<RolResponse>();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<InterfazResponse> Permisos
        {
            get => permisos;
            private set
            {
                if (ReferenceEquals(permisos, value))
                    return;

                permisos.CollectionChanged -= Permisos_CollectionChanged;

                DesuscribirItems();

                permisos = value
                    ?? new ObservableCollection<InterfazResponse>();

                permisos.CollectionChanged += Permisos_CollectionChanged;

                SuscribirItems();

                OnPropertyChanged();
                OnPropertyChanged(nameof(PermisosFiltrados));
            }
        }

        public ObservableCollection<InterfazResponse> PermisosFiltrados
        {
            get
            {
                IEnumerable<InterfazResponse> consulta = Permisos;

                if (!string.IsNullOrWhiteSpace(Filtro))
                {
                    consulta = consulta.Where(
                        permiso =>
                            (permiso.NombreInterfaz ?? string.Empty)
                            .Contains(
                                Filtro.Trim(),
                                StringComparison.OrdinalIgnoreCase));
                }

                return new ObservableCollection<InterfazResponse>(consulta);
            }
        }

        public RolResponse? RolSeleccionado
        {
            get => rolSeleccionado;
            set
            {
                if (EsMismoRol(rolSeleccionado, value))
                    return;

                rolSeleccionado = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(EsAdministrador));
                OnPropertyChanged(nameof(PuedeEditarColumnas));

                CancelarCargaPermisos();
                LimpiarPermisos();
                ActualizarEstadoEdicion();
                ActualizarHabilitados();

                if (value is null)
                {
                    Estado = "Seleccione un rol para consultar sus permisos.";
                    return;
                }

                Estado = $"Cargando permisos para {value.NombreRol}...";

                // El setter no puede ser async. El método llamado captura y maneja
                // internamente todas las excepciones y cancela respuestas anteriores.
                _ = CargarPermisosSeleccionadosAsync(value);
            }
        }

        public string Filtro
        {
            get => filtro;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (filtro == nuevoValor)
                    return;

                filtro = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PermisosFiltrados));
            }
        }

        public string Estado
        {
            get => estado;
            private set
            {
                if (estado == value)
                    return;

                estado = value;
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

                ActualizarEstadoEdicion();
                ActualizarHabilitados();
            }
        }

        public bool PuedeEditarColumnasBtn
        {
            get => puedeEditarColumnasBtn;
            private set
            {
                if (puedeEditarColumnasBtn == value)
                    return;

                puedeEditarColumnasBtn = value;
                OnPropertyChanged();
            }
        }

        public bool EsAdministrador =>
            EsRolAdministrador(RolSeleccionado);

        public bool PuedeEditarColumnas =>
            UsuarioPuedeEditar && !EsAdministrador && !IsBusy;

        public bool PuedeGuardar =>
            !IsBusy &&
            UsuarioPuedeEditar &&
            !EsAdministrador &&
            RolSeleccionado is not null &&
            Permisos.Any(permiso => permiso.IsDirty);

        public bool PuedeRevertir =>
            !IsBusy &&
            !EsAdministrador &&
            Permisos.Any(permiso => permiso.IsDirty);

        /// <summary>
        /// Carga inicial de la página. Se ejecuta desde OnAppearing para evitar
        /// iniciar tareas no esperadas dentro del constructor del ViewModel.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (!inicializado)
            {
                await CargarRolesAsync();
                return;
            }

            if (RolSeleccionado is not null && Permisos.Count == 0)
            {
                await CargarPermisosSeleccionadosAsync(RolSeleccionado);
            }
        }

        public void CancelarOperaciones()
        {
            CancelarYDescartar(ref cargaRolesCancellation);
            CancelarYDescartar(ref cargaPermisosCancellation);
            CancelarYDescartar(ref guardarCancellation);
        }

        private async Task RefrescarAsync()
        {
            if (IsBusy)
                return;

            await CargarRolesAsync();
        }

        private async Task CargarRolesAsync()
        {
            CancelarYDescartar(ref cargaRolesCancellation);
            cargaRolesCancellation = new CancellationTokenSource();
            CancellationToken token = cargaRolesCancellation.Token;

            IniciarOperacion();

            try
            {
                Estado = "Cargando roles...";

                var resultado = await rolApiService.GetRolResultAsync(token);

                if (token.IsCancellationRequested)
                    return;

                if (!resultado.Success)
                {
                    Estado = resultado.Message;
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                RolSeleccionado = null;
                Roles = resultado.Data
                    ?? new ObservableCollection<RolResponse>();

                inicializado = true;

                Estado = Roles.Count == 0
                    ? "No existen roles registrados."
                    : $"Roles cargados: {Roles.Count}";
            }
            catch (OperationCanceledException)
            {
                // La operación fue reemplazada o la página dejó de mostrarse.
            }
            catch (Exception)
            {
                Estado = "Ocurrió un error inesperado al cargar los roles.";
                await MostrarToastAsync(Estado);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private async Task CargarPermisosSeleccionadosAsync(
            RolResponse rolSolicitado)
        {
            CancelarYDescartar(ref cargaPermisosCancellation);
            cargaPermisosCancellation = new CancellationTokenSource();
            CancellationToken token = cargaPermisosCancellation.Token;

            int? rolIdSolicitado = rolSolicitado.RolId;
            string nombreRolSolicitado =
                rolSolicitado.NombreRol?.Trim() ?? string.Empty;

            IniciarOperacion();

            try
            {
                var request = new RolRequest(rolSolicitado);

                var resultado =
                    await matrizPermisosApiService.GetMatrizByRolResultAsync(
                        request,
                        token);

                if (token.IsCancellationRequested ||
                    !EsRolSeleccionadoActual(
                        rolIdSolicitado,
                        nombreRolSolicitado))
                {
                    return;
                }

                if (!resultado.Success)
                {
                    Estado = resultado.Message;
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                var matriz = resultado.Data?.FirstOrDefault();

                if (matriz?.Interfaz is null ||
                    matriz.Interfaz.Count == 0)
                {
                    Estado =
                        "No se encontraron permisos para el rol seleccionado.";
                    LimpiarPermisos();
                    return;
                }

                bool esAdministrador =
                    EsRolAdministrador(rolSolicitado);

                var permisosCargados = matriz.Interfaz;

                foreach (var permiso in permisosCargados)
                {
                    if (esAdministrador)
                    {
                        permiso.Leer = true;
                        permiso.Actualizar = true;
                        permiso.CanEdit = false;
                    }
                    else
                    {
                        permiso.CanEdit = UsuarioPuedeEditar;
                    }

                    permiso.AcceptChanges();
                }

                Permisos = permisosCargados;

                snapshot = Permisos
                    .Select(PermisoSnapshot.From)
                    .ToList();

                ActualizarEstadoEdicion();
                ActualizarHabilitados();

                Estado =
                    $"Permisos cargados para {rolSolicitado.NombreRol}";
            }
            catch (OperationCanceledException)
            {
                // Se seleccionó otro rol o se salió de la página.
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested &&
                    EsRolSeleccionadoActual(
                        rolIdSolicitado,
                        nombreRolSolicitado))
                {
                    Estado =
                        "Ocurrió un error inesperado al cargar los permisos.";
                    await MostrarToastAsync(Estado);
                }
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar || RolSeleccionado is null)
                return;

            CancelarYDescartar(ref guardarCancellation);
            guardarCancellation = new CancellationTokenSource();
            CancellationToken token = guardarCancellation.Token;

            RolResponse rolGuardado = RolSeleccionado;
            int? rolIdGuardado = rolGuardado.RolId;
            string nombreRolGuardado =
                rolGuardado.NombreRol?.Trim() ?? string.Empty;

            var matriz = new MatrizPermisosRequest
            {
                Rol = new RolRequest
                {
                    RolId = rolGuardado.RolId,
                    NombreRol = rolGuardado.NombreRol
                },
                Interfaz = Permisos
                    .Select(permiso => new InterfazRequest
                    {
                        InterfazId = permiso.InterfazId,
                        NombreInterfaz = permiso.NombreInterfaz,
                        Leer = permiso.Leer,
                        Agregar = permiso.Agregar,
                        Actualizar = permiso.Actualizar,
                        Eliminar = permiso.Eliminar
                    })
                    .ToList()
            };

            IniciarOperacion();

            try
            {
                Estado =
                    $"Guardando permisos de {rolGuardado.NombreRol}...";

                var resultado =
                    await matrizPermisosApiService.GuardarMatrizResultAsync(
                        matriz,
                        token);

                if (token.IsCancellationRequested)
                    return;

                if (!resultado.Success)
                {
                    Estado = resultado.Message;
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                if (EsRolSeleccionadoActual(
                    rolIdGuardado,
                    nombreRolGuardado))
                {
                    foreach (var permiso in Permisos)
                        permiso.AcceptChanges();

                    snapshot = Permisos
                        .Select(PermisoSnapshot.From)
                        .ToList();

                    Estado = resultado.Message;
                    ActualizarHabilitados();
                }

                await MostrarToastAsync(resultado.Message);
            }
            catch (OperationCanceledException)
            {
                // La página dejó de mostrarse.
            }
            catch (Exception)
            {
                Estado =
                    "Ocurrió un error inesperado al guardar los permisos.";
                await MostrarToastAsync(Estado);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private void MarcarColumna(string? columna)
        {
            if (!PuedeEditarColumnasBtn ||
                string.IsNullOrWhiteSpace(columna))
            {
                return;
            }

            foreach (var permiso in Permisos.Where(
                permiso => permiso.CanEdit))
            {
                switch (columna.Trim().ToLowerInvariant())
                {
                    case "leer":
                        permiso.Leer = true;
                        break;

                    case "agregar":
                        permiso.Agregar = true;
                        break;

                    case "actualizar":
                        permiso.Actualizar = true;
                        break;

                    case "eliminar":
                        permiso.Eliminar = true;
                        break;

                    case "ninguno":
                        permiso.SetAll(false);
                        break;

                    default:
                        return;
                }
            }

            ActualizarHabilitados();
        }

        private void MarcarFila(InterfazResponse? fila)
        {
            if (fila is null ||
                !fila.CanEdit ||
                IsBusy ||
                EsAdministrador)
            {
                return;
            }

            fila.SetAll(true);
            ActualizarHabilitados();
        }

        private void LimpiarFila(InterfazResponse? fila)
        {
            if (fila is null ||
                !fila.CanEdit ||
                IsBusy ||
                EsAdministrador)
            {
                return;
            }

            fila.SetAll(false);
            ActualizarHabilitados();
        }

        private void RevertirCambios()
        {
            if (!PuedeRevertir)
                return;

            foreach (var estadoAnterior in snapshot)
            {
                var permiso = Permisos.FirstOrDefault(
                    item => item.InterfazId == estadoAnterior.PermisoId);

                if (permiso is null)
                    continue;

                permiso.Leer = estadoAnterior.Leer;
                permiso.Agregar = estadoAnterior.Agregar;
                permiso.Actualizar = estadoAnterior.Actualizar;
                permiso.Eliminar = estadoAnterior.Eliminar;
                permiso.AcceptChanges();
            }

            Estado = "Cambios revertidos.";
            ActualizarHabilitados();
        }

        private void LimpiarPermisos()
        {
            Permisos = new ObservableCollection<InterfazResponse>();
            snapshot = new List<PermisoSnapshot>();
            Filtro = string.Empty;
            ActualizarHabilitados();
        }

        private void ActualizarEstadoEdicion()
        {
            bool puedeEditar =
                UsuarioPuedeEditar &&
                !EsAdministrador &&
                !IsBusy;

            foreach (var permiso in Permisos)
                permiso.CanEdit = puedeEditar;

            PuedeEditarColumnasBtn = puedeEditar;

            OnPropertyChanged(nameof(PuedeEditarColumnas));
        }

        private void ActualizarHabilitados()
        {
            // Protección adicional durante la construcción o destrucción
            // del ViewModel. Normalmente ya estarán inicializados.
            GuardarCommand?.ChangeCanExecute();
            RevertirCambiosCommand?.ChangeCanExecute();
            RefrescarCommand?.ChangeCanExecute();

            OnPropertyChanged(nameof(PuedeGuardar));
            OnPropertyChanged(nameof(PuedeRevertir));
            OnPropertyChanged(nameof(PuedeEditarColumnas));
        }

        private void Permisos_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            SuscribirItems();
            OnPropertyChanged(nameof(PermisosFiltrados));
            ActualizarHabilitados();
        }

        private void SuscribirItems()
        {
            DesuscribirItems();

            foreach (var permiso in Permisos)
            {
                permiso.PropertyChanged += Item_PropertyChanged;
                itemsSuscritos.Add(permiso);
            }
        }

        private void DesuscribirItems()
        {
            foreach (var permiso in itemsSuscritos)
                permiso.PropertyChanged -= Item_PropertyChanged;

            itemsSuscritos.Clear();
        }

        private void Item_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is
                nameof(Permiso.Leer) or
                nameof(Permiso.Agregar) or
                nameof(Permiso.Actualizar) or
                nameof(Permiso.Eliminar))
            {
                Estado = "Hay cambios sin guardar.";
            }

            if (e.PropertyName == nameof(InterfazResponse.NombreInterfaz))
                OnPropertyChanged(nameof(PermisosFiltrados));

            ActualizarHabilitados();
        }

        private void IniciarOperacion()
        {
            if (Interlocked.Increment(ref operacionesActivas) == 1)
                IsBusy = true;

            ActualizarEstadoEdicion();
            ActualizarHabilitados();
        }

        private void FinalizarOperacion()
        {
            int operacionesRestantes =
                Interlocked.Decrement(ref operacionesActivas);

            if (operacionesRestantes <= 0)
            {
                Interlocked.Exchange(ref operacionesActivas, 0);
                IsBusy = false;
            }

            ActualizarEstadoEdicion();
            ActualizarHabilitados();
        }

        private void CancelarCargaPermisos()
        {
            CancelarYDescartar(ref cargaPermisosCancellation);
        }

        private static void CancelarYDescartar(
            ref CancellationTokenSource? cancellationTokenSource)
        {
            if (cancellationTokenSource is null)
                return;

            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ya había sido descartado.
            }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        private bool EsRolSeleccionadoActual(
            int? rolId,
            string nombreRol)
        {
            if (RolSeleccionado is null)
                return false;

            if (rolId.HasValue && RolSeleccionado.RolId.HasValue)
                return rolId.Value == RolSeleccionado.RolId.Value;

            return string.Equals(
                nombreRol,
                RolSeleccionado.NombreRol?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsMismoRol(
            RolResponse? primero,
            RolResponse? segundo)
        {
            if (ReferenceEquals(primero, segundo))
                return true;

            if (primero is null || segundo is null)
                return false;

            if (primero.RolId.HasValue && segundo.RolId.HasValue)
                return primero.RolId.Value == segundo.RolId.Value;

            return string.Equals(
                primero.NombreRol?.Trim(),
                segundo.NombreRol?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsRolAdministrador(RolResponse? rol)
        {
            return rol?.NombreRol?.Equals(
                "ADMINISTRADOR",
                StringComparison.OrdinalIgnoreCase) == true;
        }
    }

    public record PermisoSnapshot(
        int PermisoId,
        bool Leer,
        bool Agregar,
        bool Actualizar,
        bool Eliminar)
    {
        public static PermisoSnapshot From(InterfazResponse permiso)
        {
            return new PermisoSnapshot(
                permiso.InterfazId,
                permiso.Leer,
                permiso.Agregar,
                permiso.Actualizar,
                permiso.Eliminar);
        }
    }
}
