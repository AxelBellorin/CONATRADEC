using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class RolViewModel : GlobalService
    {
        private readonly RolApiService rolApiService = new();

        private CancellationTokenSource? cargaCts;
        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;
        private bool navegando;
        private bool pantallaCargada;
        private bool mostrandoRelay;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";
        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public RolViewModel()
        {
            tamanoPaginaActual =
                ObtenerTamanoPagina();

            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RegresarConfiguracionAsync,
                        "regresar a configuración"),
                    () => !IsBusy && !Navegando);

            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddAsync,
                        "abrir el formulario de rol"),
                    () => CanAdd && !IsBusy && !Navegando);

            EditCommand =
                new Command<RolResponse>(
                    async rol => await EjecutarSeguroAsync(
                        () => OnEditAsync(rol),
                        "editar el rol"),
                    rol =>
                        rol != null &&
                        rol.EsEditable &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<RolResponse>(
                    async rol => await EjecutarSeguroAsync(
                        () => OnDeleteAsync(rol),
                        "eliminar el rol"),
                    rol =>
                        rol != null &&
                        rol.EsEditable &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<RolResponse>(
                    async rol => await EjecutarSeguroAsync(
                        () => OnViewAsync(rol),
                        "consultar el rol"),
                    rol =>
                        rol != null &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar roles"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar la búsqueda"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar los roles"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

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

        public ObservableCollection<RolResponse>
            List { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<RolResponse> EditCommand { get; }
        public Command<RolResponse> DeleteCommand { get; }
        public Command<RolResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

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
                OnPropertyChanged(nameof(ResumenResultados));
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
            List.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 ||
                    List.Count == 0)
                {
                    return "Sin registros en esta página";
                }

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) *
                     Math.Max(1, tamanoPaginaActual)) + 1;

                int fin =
                    Math.Min(
                        inicio + List.Count - 1,
                        TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 rol encontrado"
                : $"{TotalRegistros:N0} roles encontrados";

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public bool TienePaginaCargada =>
            pantallaCargada;

        public void ActualizarPermisos()
        {
            LoadPagePermissions("rolPage");

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(MostrarAccesoDenegado));

            ActualizarComandos();
            NotificarEstado();
        }

        /// <summary>
        /// Entrada desde otra interfaz: se descarta el estado de la visita
        /// anterior y se consulta únicamente la primera página del servidor.
        /// </summary>
        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            CancelarCarga();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            List.Clear();
            NotificarEstado();

            await CargarPaginaAsync(
                1,
                true,
                "Cargando roles...",
                "Consultando información actual del servidor");
        }

        public Task InicializarAsync() =>
            pantallaCargada
                ? Task.CompletedTask
                : CargarPaginaAsync(
                    1,
                    true,
                    "Cargando roles...",
                    "Consultando información actual del servidor");

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual),
                false,
                "Actualizando roles...",
                "Aplicando los cambios realizados dentro del módulo");

        /// <summary>
        /// Devuelve true solo cuando la mutación puede cambiar la composición
        /// global de la página y se necesita un GET justificado.
        /// </summary>
        public bool AplicarMutacionPendiente(
            RolMutacionListado mutacion)
        {
            if (mutacion == null ||
                mutacion.Actual.RolId is not > 0 ||
                !pantallaCargada)
            {
                return true;
            }

            return mutacion.Tipo switch
            {
                RolMutacionListadoTipo.Actualizado =>
                    !AplicarActualizacionLocal(mutacion),

                RolMutacionListadoTipo.Creado =>
                    !AplicarCreacionLocal(mutacion.Actual),

                _ => true
            };
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
                false,
                "Buscando roles...",
                "Consultando los registros que coinciden con la búsqueda");
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;

            await CargarPaginaAsync(
                1,
                false,
                "Actualizando roles...",
                "Quitando filtros y consultando la primera página");
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    false,
                    "Actualizando roles...",
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
                "Consultando la página anterior de roles");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                false,
                "Cargando página siguiente...",
                "Consultando la siguiente página de roles");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (!CanView || Navegando)
                return;

            paginaSolicitada =
                Math.Max(1, paginaSolicitada);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                MostrarRelay(
                    tituloOperacion,
                    detalleOperacion);

                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();
                NotificarEstado();

                ApiResult<RolAdministracionPaginaResponse> resultado =
                    await rolApiService.BuscarPaginadoAsync(
                        textoBusquedaAplicado,
                        incluirInactivos: false,
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
                        Mensaje = resultado.Message;
                    }

                    return;
                }

                AplicarPagina(resultado.Data);
                pantallaCargada = true;
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o iniciar otra consulta.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras se abandonaba la pantalla.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje = "No fue posible cargar los roles.";

                    await MostrarErrorInesperadoAsync(
                        "cargar los roles",
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

        /// <summary>
        /// La colección conserva exclusivamente la página visible.
        /// </summary>
        private void AplicarPagina(
            RolAdministracionPaginaResponse pagina)
        {
            List.Clear();

            foreach (RolResponse item in pagina.Items)
            {
                if (item.RolId is > 0)
                    List.Add(item);
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

        private async Task RegresarConfiguracionAsync()
        {
            await NavegarAsync(
                AppRoutes.Configuracion,
                null,
                "Regresando...",
                "Volviendo a Configuración");
        }

        private Task OnAddAsync()
        {
            if (!CanAdd)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RolFormularioInterno,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create
                },
                "Abriendo rol...",
                "Preparando el formulario de creación");
        }

        private async Task OnEditAsync(RolResponse? rol)
        {
            if (rol == null)
                return;

            if (rol.EsAdministrador)
            {
                await MostrarAdvertenciaAsync(
                    "El rol Administrador está protegido y no puede editarse.");
                return;
            }

            await NavegarAsync(
                AppRoutes.RolFormularioInterno,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Rol"] = ClonarRol(rol)
                },
                "Abriendo rol...",
                "Preparando la información para edición");
        }

        private Task OnViewAsync(RolResponse? rol)
        {
            if (rol == null)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.RolFormularioInterno,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["Rol"] = ClonarRol(rol)
                },
                "Abriendo rol...",
                "Preparando la información para consulta");
        }

        private async Task OnDeleteAsync(RolResponse? rol)
        {
            if (rol == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para eliminar roles.");
                return;
            }

            if (rol.EsAdministrador)
            {
                await MostrarAdvertenciaAsync(
                    "El rol Administrador está protegido y no puede eliminarse.");
                return;
            }

            string dependencias =
                rol.CantidadUsuarios > 0 ||
                rol.CantidadInterfaces > 0
                    ? "\n\nEl servidor protegerá las relaciones existentes."
                    : string.Empty;

            bool confirmar =
                await ConfirmarAsync(
                    "Eliminar rol",
                    $"¿Desea eliminar el rol '{rol.NombreMostrar}'?" +
                    dependencias,
                    "Eliminar",
                    "Cancelar");

            if (!confirmar)
                return;

            bool eliminado = false;

            try
            {
                MostrarRelay(
                    "Eliminando rol...",
                    "Actualizando el estado en el servidor");

                IsBusy = true;
                ActualizarComandos();

                ApiResult<bool> resultado =
                    await rolApiService
                        .EliminarRolAdministracionResultAsync(
                            rol.RolId!.Value);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                eliminado = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Rol eliminado correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                OcultarRelay();
                ActualizarComandos();
            }

            if (!eliminado)
                return;

            bool requiereRecarga =
                paginaActual < totalPaginas;

            if (!requiereRecarga)
            {
                int paginaAntesEliminar =
                    paginaActual;

                List.Remove(rol);

                TotalRegistros =
                    Math.Max(0, TotalRegistros - 1);

                RecalcularPaginasLocales();

                if (List.Count == 0 &&
                    TotalRegistros > 0 &&
                    paginaAntesEliminar > 1)
                {
                    requiereRecarga = true;
                }
                else
                {
                    NotificarEstado();
                }
            }

            if (requiereRecarga)
            {
                int paginaDestino =
                    Math.Min(
                        Math.Max(1, paginaActual),
                        Math.Max(1, totalPaginas));

                await CargarPaginaAsync(
                    paginaDestino,
                    false,
                    "Actualizando roles...",
                    "Ajustando la página después de la eliminación");
            }
        }

        private bool AplicarActualizacionLocal(
            RolMutacionListado mutacion)
        {
            RolResponse? anterior =
                mutacion.Anterior;

            if (anterior?.RolId is not > 0 ||
                mutacion.Actual.RolId is not > 0)
            {
                return false;
            }

            int indice =
                EncontrarIndice(mutacion.Actual.RolId.Value);

            if (indice < 0)
                return false;

            bool cambioOrden =
                !Iguales(
                    anterior.NombreRol,
                    mutacion.Actual.NombreRol);

            if (cambioOrden)
                return false;

            if (!string.IsNullOrWhiteSpace(textoBusquedaAplicado) &&
                (!Iguales(
                    anterior.NombreRol,
                    mutacion.Actual.NombreRol) ||
                 !Iguales(
                    anterior.DescripcionRol,
                    mutacion.Actual.DescripcionRol)))
            {
                return false;
            }

            List[indice] =
                ClonarRol(mutacion.Actual);

            NotificarEstado();
            return true;
        }

        private bool AplicarCreacionLocal(
            RolResponse creado)
        {
            if (!CoincideBusqueda(
                    creado,
                    textoBusquedaAplicado))
            {
                return true;
            }

            bool unicaPaginaCompletaEnMemoria =
                paginaActual == 1 &&
                totalPaginas <= 1 &&
                TotalRegistros <
                    Math.Max(1, tamanoPaginaActual);

            if (!unicaPaginaCompletaEnMemoria)
                return false;

            List.Add(ClonarRol(creado));
            OrdenarPaginaLocal();

            TotalRegistros = TotalRegistros + 1;
            RecalcularPaginasLocales();
            NotificarEstado();

            return true;
        }

        private void OrdenarPaginaLocal()
        {
            List<RolResponse> ordenados =
                List
                    .OrderBy(
                        item => item.NombreMostrar,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(item => item.RolId)
                    .ToList();

            List.Clear();

            foreach (RolResponse item in ordenados)
                List.Add(item);
        }

        private void RecalcularPaginasLocales()
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

        private int EncontrarIndice(int rolId)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].RolId == rolId)
                    return i;
            }

            return -1;
        }

        private static bool CoincideBusqueda(
            RolResponse rol,
            string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
                return true;

            return Contiene(rol.NombreRol, filtro) ||
                   Contiene(rol.DescripcionRol, filtro);
        }

        private static bool Contiene(
            string? valor,
            string filtro) =>
            (valor ?? string.Empty)
                .Contains(
                    filtro,
                    StringComparison.OrdinalIgnoreCase);

        private static bool Iguales(
            string? izquierda,
            string? derecha) =>
            string.Equals(
                izquierda?.Trim() ?? string.Empty,
                derecha?.Trim() ?? string.Empty,
                StringComparison.Ordinal);

        private static RolResponse ClonarRol(
            RolResponse rol) =>
            new()
            {
                RolId = rol.RolId,
                NombreRol = rol.NombreRol,
                DescripcionRol = rol.DescripcionRol,
                CantidadUsuarios = rol.CantidadUsuarios,
                CantidadInterfaces = rol.CantidadInterfaces
            };

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros,
            string titulo,
            string detalle)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();
                MostrarRelay(titulo, detalle);
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
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
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
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararNuevaCarga()
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
