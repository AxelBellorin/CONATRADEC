using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class UserViewModel : GlobalService
    {
        private readonly AdministracionConsultaApiService
            consultaApiService = new();

        private readonly UserApiService
            userApiService = new();

        private CancellationTokenSource? cargaCts;

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoMas;
        private bool navegando;
        private bool pantallaCargada;
        private int paginaActual;
        private int totalPaginas = 1;
        private int totalRegistros;

        public ObservableCollection<UserResponse>
            UsersList { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddUserCommand { get; }
        public Command<UserResponse> EditUserCommand { get; }
        public Command<UserResponse> DeleteUserCommand { get; }
        public Command<UserResponse> ViewUserCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command CargarMasCommand { get; }

        public UserViewModel()
        {
            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => NavegarAsync(AppRoutes.Configuracion),
                        "regresar a configuración"),
                    () => !IsBusy && !Navegando);

            AddUserCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddUserAsync,
                        "abrir el formulario de usuario"),
                    () => CanAdd && !IsBusy && !Navegando);

            EditUserCommand =
                new Command<UserResponse>(
                    async user => await EjecutarSeguroAsync(
                        () => OnEditUserAsync(user),
                        "editar el usuario"),
                    user =>
                        user != null &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            DeleteUserCommand =
                new Command<UserResponse>(
                    async user => await EjecutarSeguroAsync(
                        () => OnDeleteUserAsync(user),
                        "eliminar el usuario"),
                    user =>
                        user != null &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            ViewUserCommand =
                new Command<UserResponse>(
                    async user => await EjecutarSeguroAsync(
                        () => OnViewUserAsync(user),
                        "consultar el usuario"),
                    user =>
                        user != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(true),
                        "buscar usuarios"),
                    () => CanView && !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () => CanView && !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los usuarios"),
                    () => CanView && !Navegando);

            CargarMasCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => CargarAsync(false),
                        "cargar más usuarios"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !CargandoMas &&
                        !Navegando &&
                        PuedeCargarMas);
        }

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
                NotificarEstado();
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

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 usuario encontrado"
                : $"{TotalRegistros} usuarios encontrados";

        public bool PuedeCargarMas =>
            paginaActual < totalPaginas;

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            !CargandoMas &&
            UsersList.Count == 0 &&
            !TieneMensaje;

        public bool MostrarFinLista =>
            CanView &&
            pantallaCargada &&
            UsersList.Count > 0 &&
            !PuedeCargarMas &&
            !IsBusy &&
            !CargandoMas;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("userPage");
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();
        }

        public Task InicializarAsync() =>
            CargarAsync(true);

        // Compatibilidad con el code-behind anterior.
        public Task LoadUsers(bool mostrarIndicadorCarga) =>
            CargarAsync(true);

        public async Task CargarAsync(bool reiniciar)
        {
            if (!CanView || Navegando)
                return;

            if (!reiniciar &&
                (CargandoMas || !PuedeCargarMas))
            {
                return;
            }

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                if (reiniciar)
                {
                    IsBusy = true;
                    Mensaje = string.Empty;
                }
                else
                {
                    CargandoMas = true;
                }

                int paginaSolicitada =
                    reiniciar
                        ? 1
                        : paginaActual + 1;

                ApiResult<UsuarioAdministracionPaginaResponse> resultado =
                    await consultaApiService.BuscarUsuariosAsync(
                        TextoBusqueda,
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
                        Mensaje = resultado.Message;

                    return;
                }

                AplicarPagina(resultado.Data, reiniciar);
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
                    Mensaje = "No fue posible cargar los usuarios.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los usuarios",
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
                NotificarEstado();
            }
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref cargaCts, null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoMas = false;
        }

        private void AplicarPagina(
            UsuarioAdministracionPaginaResponse pagina,
            bool reiniciar)
        {
            if (reiniciar)
                UsersList.Clear();

            HashSet<int> ids =
                UsersList
                    .Where(item => item.UsuarioId is > 0)
                    .Select(item => item.UsuarioId!.Value)
                    .ToHashSet();

            foreach (UserResponse item in pagina.Items)
            {
                if (item.UsuarioId is not > 0)
                    continue;

                if (ids.Add(item.UsuarioId.Value))
                    UsersList.Add(item);
            }

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            TotalRegistros = Math.Max(0, pagina.TotalRegistros);
            Mensaje = string.Empty;

            OnPropertyChanged(nameof(PuedeCargarMas));
            NotificarEstado();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            await CargarAsync(true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarAsync(true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task OnAddUserAsync() =>
            NavegarAsync(
                "//UserFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["User"] = new UserRequest(new UserResponse())
                });

        private Task OnEditUserAsync(UserResponse? user)
        {
            if (user == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//UserFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["User"] = new UserRequest(user)
                });
        }

        private Task OnViewUserAsync(UserResponse? user)
        {
            if (user == null)
                return Task.CompletedTask;

            return NavegarAsync(
                "//UserFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["User"] = new UserRequest(user)
                });
        }

        private async Task OnDeleteUserAsync(UserResponse? user)
        {
            if (user == null || IsBusy)
                return;

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar usuario",
                        $"¿Desea desactivar a '{user.NombreMostrar}'?",
                        "Eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await userApiService.DeleteUserResultAsync(
                        new UserRequest(user));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                UsersList.Remove(user);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Usuario desactivado correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();

                if (parametros == null)
                    await GoToAsyncParameters(ruta);
                else
                    await GoToAsyncParameters(ruta, parametros);
            }
            finally
            {
                Navegando = false;
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
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(descripcion, ex);
            }
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddUserCommand.ChangeCanExecute();
            EditUserCommand.ChangeCanExecute();
            DeleteUserCommand.ChangeCanExecute();
            ViewUserCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarFinLista));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararNuevaCarga()
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
        }

        private static bool EsCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
