using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool cargandoInicial;
        private bool cargandoListado;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private bool navegando;
        private bool pantallaCargada;
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

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
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

        public UserViewModel()
        {
            tamanoPaginaActual = ObtenerTamanoPagina();

            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        SalirAConfiguracionAsync,
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
                        AplicarBusquedaAsync,
                        "buscar usuarios"),
                    () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () => CanView && !IsBusy && !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los usuarios"),
                    () => CanView && !IsBusy && !Navegando);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        CanView &&
                        PuedeIrAnterior &&
                        !IsBusy &&
                        !Navegando);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        CanView &&
                        PuedeIrSiguiente &&
                        !IsBusy &&
                        !Navegando);
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

        public bool CargandoInicial
        {
            get => cargandoInicial;
            private set
            {
                if (cargandoInicial == value)
                    return;

                cargandoInicial = value;
                OnPropertyChanged();
            }
        }

        public bool CargandoListado
        {
            get => cargandoListado;
            private set
            {
                if (cargandoListado == value)
                    return;

                cargandoListado = value;
                OnPropertyChanged();
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
                OnPropertyChanged(nameof(RangoPaginaTexto));
                OnPropertyChanged(nameof(MostrarPaginacion));
            }
        }

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;

        public bool PuedeIrAnterior =>
            pantallaCargada && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView &&
            pantallaCargada &&
            UsersList.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 || UsersList.Count == 0)
                    return "Sin registros en esta página";

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin = inicio + UsersList.Count - 1;
                fin = Math.Min(fin, TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 usuario encontrado"
                : $"{TotalRegistros} usuarios encontrados";

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            UsersList.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public bool TienePaginaCargada =>
            pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("userPage");
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstado();
        }

        /// <summary>
        /// Se ejecuta al entrar a Usuarios desde otra interfaz. Descarta filtros,
        /// página y datos de la visita anterior y consulta únicamente la primera
        /// página al servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            CancelarCarga();

            textoBusquedaAplicado = string.Empty;
            TextoBusqueda = string.Empty;
            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            UsersList.Clear();
            NotificarEstado();

            await CargarPaginaAsync(
                1,
                true,
                "Cargando usuarios...",
                "Consultando información actual del servidor");
        }

        /// <summary>
        /// Compatibilidad con llamadas existentes. Una página ya cargada durante
        /// la misma visita no vuelve a consultar automáticamente.
        /// </summary>
        public Task InicializarAsync() =>
            pantallaCargada
                ? Task.CompletedTask
                : CargarPaginaAsync(
                    1,
                    true,
                    "Cargando usuarios...",
                    "Consultando información actual del servidor");

        public Task LoadUsers(bool mostrarIndicadorCarga) =>
            CargarPaginaAsync(1, mostrarIndicadorCarga);

        /// <summary>
        /// Recarga únicamente la página actualmente visible. Se usa cuando una
        /// operación realizada en Usuarios inactivos modifica el conjunto activo
        /// y no es posible reconstruir con exactitud la página solo en memoria.
        /// </summary>
        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual),
                false,
                "Actualizando usuarios...",
                "Aplicando los cambios realizados en usuarios inactivos");

        /// <summary>
        /// Aplica el resultado confirmado de Crear/Editar sobre la página que ya
        /// está en memoria. No ejecuta una consulta HTTP.
        /// </summary>
        public void AplicarCambiosPendientes()
        {
            UsuarioVisitaCambio? cambio =
                UsuarioVisitaService.ConsumirCambio();

            if (cambio == null ||
                cambio.Usuario.UsuarioId is not > 0)
            {
                return;
            }

            switch (cambio.Tipo)
            {
                case UsuarioVisitaCambioTipo.Actualizado:
                    AplicarActualizacionLocal(cambio.Usuario);
                    break;

                case UsuarioVisitaCambioTipo.Creado:
                    AplicarCreacionLocal(cambio.Usuario);
                    break;
            }

            NotificarEstado();
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(ref cargaCts, null);

            CancelarSeguro(source);

            IsBusy = false;
            IsRefreshing = false;
            CargandoInicial = false;
            CargandoListado = false;
            OcultarRelay();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty).Trim();

            await CargarPaginaAsync(
                1,
                false,
                "Buscando usuarios...",
                "Consultando los usuarios que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            await CargarPaginaAsync(
                1,
                false,
                "Actualizando usuarios...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                // La actualización manual renueva también los catálogos de la
                // visita. No se consultan en este momento; se cargarán bajo
                // demanda la próxima vez que se abra Crear/Editar.
                UsuarioVisitaService.InvalidarCatalogos();
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    false,
                    "Actualizando usuarios...",
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
                false,
                "Cargando página anterior...",
                "Consultando la página anterior de usuarios");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                false,
                "Cargando página siguiente...",
                "Consultando la siguiente página de usuarios");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false,
            string? tituloOperacion = null,
            string? detalleOperacion = null)
        {
            if (!CanView || Navegando)
                return;

            paginaSolicitada = Math.Max(1, paginaSolicitada);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                MostrarRelay(
                    tituloOperacion ??
                        (cargaInicial
                            ? "Cargando usuarios..."
                            : "Actualizando usuarios..."),
                    detalleOperacion ??
                        "Consultando información actual del servidor");

                CargandoInicial = cargaInicial;
                CargandoListado = !cargaInicial;
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                ApiResult<UsuarioAdministracionPaginaResponse> resultado =
                    await consultaApiService.BuscarUsuariosAsync(
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
                        Mensaje = resultado.Message;

                    return;
                }

                UsuarioAdministracionPaginaResponse pagina = resultado.Data;
                int paginasServidor = Math.Max(1, pagina.TotalPaginas);

                // Si una eliminación realizada por otro cliente redujo el total
                // mientras estábamos en una página que ya no existe, se corrige
                // con una única consulta adicional a la última página válida.
                if (paginaSolicitada > paginasServidor)
                {
                    ApiResult<UsuarioAdministracionPaginaResponse> correccion =
                        await consultaApiService.BuscarUsuariosAsync(
                            textoBusquedaAplicado,
                            paginasServidor,
                            ObtenerTamanoPagina(),
                            source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!correccion.Success || correccion.Data == null)
                    {
                        if (!EsCancelacion(correccion.Message))
                            Mensaje = correccion.Message;

                        return;
                    }

                    pagina = correccion.Data;
                }

                AplicarPagina(pagina);
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
                    IsBusy = false;
                    IsRefreshing = false;
                    CargandoInicial = false;
                    CargandoListado = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstado();
            }
        }

        /// <summary>
        /// Reemplaza la colección completa con la página recibida. Nunca acumula
        /// páginas anteriores, manteniendo acotado el uso de memoria RAM.
        /// </summary>
        private void AplicarPagina(
            UsuarioAdministracionPaginaResponse pagina)
        {
            UsersList.Clear();

            foreach (UserResponse item in pagina.Items)
            {
                if (item.UsuarioId is > 0)
                    UsersList.Add(item);
            }

            paginaActual = Math.Max(1, pagina.PaginaActual);
            totalPaginas = Math.Max(1, pagina.TotalPaginas);
            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            TotalRegistros = Math.Max(0, pagina.TotalRegistros);
            Mensaje = string.Empty;
            NotificarEstado();
        }

        private Task OnAddUserAsync() =>
            NavegarAsync(
                "//UserFormPage",
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["User"] = new UserRequest(new UserResponse())
                },
                "Abriendo nuevo usuario...",
                "Preparando el formulario de creación");

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
                },
                "Abriendo usuario...",
                "Preparando la información para edición");
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
                },
                "Abriendo usuario...",
                "Preparando la información para consulta");
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
                MostrarRelay(
                    "Eliminando usuario...",
                    "Actualizando el estado del usuario en el servidor");

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
                RecalcularPaginasLocales();

                // Única excepción al "DELETE sin GET": si se eliminó el último
                // elemento de una página posterior, necesitamos mostrar la página
                // anterior para no dejar al usuario frente a una página vacía.
                if (UsersList.Count == 0 && paginaActual > 1)
                {
                    int destino = Math.Min(
                        paginaActual - 1,
                        Math.Max(1, totalPaginas));

                    await CargarPaginaAsync(destino);
                }

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Usuario desactivado correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
                NotificarEstado();
            }
        }

        private void AplicarActualizacionLocal(UserResponse usuario)
        {
            int indice = -1;

            for (int i = 0; i < UsersList.Count; i++)
            {
                if (UsersList[i].UsuarioId == usuario.UsuarioId)
                {
                    indice = i;
                    break;
                }
            }

            if (indice < 0)
                return;

            if (!CoincideBusquedaAplicada(usuario))
            {
                UsersList.RemoveAt(indice);
                TotalRegistros = Math.Max(0, TotalRegistros - 1);
                RecalcularPaginasLocales();
                return;
            }

            UsersList[indice] = usuario;
            OrdenarPaginaActual();
        }

        private void AplicarCreacionLocal(UserResponse usuario)
        {
            if (!CoincideBusquedaAplicada(usuario))
                return;

            TotalRegistros++;

            /*
             * Solo se inserta localmente cuando la consulta completa cabe en una
             * única página. En un listado de varias páginas no podemos conocer
             * con certeza la posición global del nuevo usuario sin otro GET.
             */
            if (totalPaginas <= 1 &&
                UsersList.Count < Math.Max(1, tamanoPaginaActual))
            {
                UsersList.Add(usuario);
                OrdenarPaginaActual();
            }

            RecalcularPaginasLocales();
        }

        private bool CoincideBusquedaAplicada(UserResponse usuario)
        {
            if (string.IsNullOrWhiteSpace(textoBusquedaAplicado))
                return true;

            string texto = textoBusquedaAplicado.Trim();

            return Contiene(usuario.NombreUsuario, texto) ||
                   Contiene(usuario.NombreCompletoUsuario, texto) ||
                   Contiene(usuario.CorreoUsuario, texto) ||
                   Contiene(usuario.IdentificacionUsuario, texto) ||
                   Contiene(usuario.TelefonoUsuario, texto);
        }

        private static bool Contiene(
            string? valor,
            string texto) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                texto,
                StringComparison.OrdinalIgnoreCase);

        private void OrdenarPaginaActual()
        {
            List<UserResponse> ordenados =
                UsersList
                    .OrderBy(
                        item => item.NombreMostrar,
                        ComparadorNaturalTexto.Instancia)
                    .ThenBy(item => item.UsuarioId)
                    .ToList();

            UsersList.Clear();

            foreach (UserResponse item in ordenados)
                UsersList.Add(item);
        }

        /// <summary>
        /// Mantiene en memoria el mismo criterio humano del servidor cuando un
        /// usuario se crea o edita durante la visita actual. Los bloques
        /// numéricos se comparan como números sin convertir el listado completo.
        /// </summary>
        private sealed class ComparadorNaturalTexto : IComparer<string>
        {
            public static ComparadorNaturalTexto Instancia { get; } = new();

            private static readonly CompareInfo ComparadorCultura =
                CultureInfo.CurrentCulture.CompareInfo;

            public int Compare(string? izquierda, string? derecha)
            {
                string a = (izquierda ?? string.Empty).Trim();
                string b = (derecha ?? string.Empty).Trim();

                int indiceA = 0;
                int indiceB = 0;

                while (indiceA < a.Length && indiceB < b.Length)
                {
                    bool numeroA = char.IsDigit(a[indiceA]);
                    bool numeroB = char.IsDigit(b[indiceB]);

                    if (numeroA && numeroB)
                    {
                        int finA = indiceA;
                        int finB = indiceB;

                        while (finA < a.Length && char.IsDigit(a[finA]))
                            finA++;

                        while (finB < b.Length && char.IsDigit(b[finB]))
                            finB++;

                        int significativoA = indiceA;
                        int significativoB = indiceB;

                        while (significativoA < finA &&
                               a[significativoA] == '0')
                        {
                            significativoA++;
                        }

                        while (significativoB < finB &&
                               b[significativoB] == '0')
                        {
                            significativoB++;
                        }

                        int longitudA = finA - significativoA;
                        int longitudB = finB - significativoB;

                        if (longitudA != longitudB)
                            return longitudA.CompareTo(longitudB);

                        for (int i = 0; i < longitudA; i++)
                        {
                            int comparacion =
                                a[significativoA + i]
                                    .CompareTo(b[significativoB + i]);

                            if (comparacion != 0)
                                return comparacion;
                        }

                        int longitudBloqueA = finA - indiceA;
                        int longitudBloqueB = finB - indiceB;

                        if (longitudBloqueA != longitudBloqueB)
                        {
                            return longitudBloqueA
                                .CompareTo(longitudBloqueB);
                        }

                        indiceA = finA;
                        indiceB = finB;
                        continue;
                    }

                    int textoFinA = indiceA;
                    int textoFinB = indiceB;

                    while (textoFinA < a.Length &&
                           !char.IsDigit(a[textoFinA]))
                    {
                        textoFinA++;
                    }

                    while (textoFinB < b.Length &&
                           !char.IsDigit(b[textoFinB]))
                    {
                        textoFinB++;
                    }

                    int comparacionTexto = ComparadorCultura.Compare(
                        a,
                        indiceA,
                        textoFinA - indiceA,
                        b,
                        indiceB,
                        textoFinB - indiceB,
                        CompareOptions.IgnoreCase |
                        CompareOptions.IgnoreNonSpace);

                    if (comparacionTexto != 0)
                        return comparacionTexto;

                    indiceA = textoFinA;
                    indiceB = textoFinB;
                }

                if (indiceA < a.Length)
                    return 1;

                if (indiceB < b.Length)
                    return -1;

                return ComparadorCultura.Compare(
                    a,
                    b,
                    CompareOptions.IgnoreCase |
                    CompareOptions.IgnoreNonSpace);
            }
        }

        private void RecalcularPaginasLocales()
        {
            int tamano = Math.Max(1, tamanoPaginaActual);

            totalPaginas =
                TotalRegistros == 0
                    ? 1
                    : (int)Math.Ceiling(
                        TotalRegistros / (double)tamano);

            paginaActual = Math.Min(
                Math.Max(1, paginaActual),
                Math.Max(1, totalPaginas));

            NotificarEstado();
        }

        private async Task SalirAConfiguracionAsync()
        {
            UsuarioVisitaService.FinalizarVisita();
            await NavegarAsync(
                AppRoutes.Configuracion,
                null,
                "Regresando a configuración...",
                "Cerrando la administración de usuarios");
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null,
            string tituloOperacion = "Cargando...",
            string detalleOperacion = "Preparando la siguiente interfaz")
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();
                MostrarRelay(tituloOperacion, detalleOperacion);

                // La Page ya está visible; se cede un ciclo al UI para que el
                // usuario vea el relay antes de iniciar la navegación.
                await Task.Yield();

                if (parametros == null)
                    await GoToAsyncParameters(ruta);
                else
                    await GoToAsyncParameters(ruta, parametros);
            }
            finally
            {
                OcultarRelay();
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
                OcultarRelay();
                await MostrarErrorInesperadoAsync(descripcion, ex);
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
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddUserCommand.ChangeCanExecute();
            EditUserCommand.ChangeCanExecute();
            DeleteUserCommand.ChangeCanExecute();
            ViewUserCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(ResumenResultados));
            ActualizarComandos();
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
