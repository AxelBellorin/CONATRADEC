using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(
        nameof(ModoSeleccionTexto),
        "ModoSeleccion")]
    public sealed class PropietariosViewModel :
        GlobalService
    {
        private readonly PropietarioApiService service =
            new();

        private readonly PropietarioCrudApiService crudService =
            new();

        private string textoBusqueda =
            string.Empty;

        private string? modoSeleccionTexto;

        private bool mostrarEliminados;

        private bool inicializado;

        public PropietariosViewModel()
        {
            BuscarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy);

            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy);

            RegresarCommand = new Command(
                async () => await RegresarAsync(),
                () => !IsBusy);

            NuevoCommand = new Command(
                async () => await NuevoAsync(),
                () => CanAdd && !IsBusy);

            AbrirCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await AbrirAsync(propietario),
                    propietario =>
                        propietario != null &&
                        !IsBusy);

            VerCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await VerAsync(propietario),
                    propietario =>
                        propietario != null &&
                        CanView &&
                        !EsModoSeleccion &&
                        !IsBusy);

            EditarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EditarAsync(propietario),
                    propietario =>
                        propietario != null &&
                        propietario.Activo &&
                        CanEdit &&
                        !EsModoSeleccion &&
                        !IsBusy);

            EliminarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EliminarAsync(propietario),
                    propietario =>
                        propietario != null &&
                        propietario.Activo &&
                        CanDelete &&
                        !EsModoSeleccion &&
                        !IsBusy);

            RecuperarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await RecuperarAsync(propietario),
                    propietario =>
                        propietario != null &&
                        !propietario.Activo &&
                        CanEdit &&
                        !EsModoSeleccion &&
                        !IsBusy);

            VerTerrenosCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await VerTerrenosAsync(propietario),
                    propietario =>
                        propietario != null &&
                        propietario.TotalTerrenos > 0 &&
                        CanView &&
                        !EsModoSeleccion &&
                        !IsBusy);
        }

        public ObservableCollection<
            PropietarioResponse> Propietarios
        {
            get;
        } = new();

        public Command BuscarCommand { get; }

        public Command ActualizarCommand { get; }

        public Command RegresarCommand { get; }

        public Command NuevoCommand { get; }

        public Command<PropietarioResponse>
            AbrirCommand { get; }

        public Command<PropietarioResponse>
            VerCommand { get; }

        public Command<PropietarioResponse>
            EditarCommand { get; }

        public Command<PropietarioResponse>
            EliminarCommand { get; }

        public Command<PropietarioResponse>
            RecuperarCommand { get; }

        public Command<PropietarioResponse>
            VerTerrenosCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                if (textoBusqueda == value)
                    return;

                textoBusqueda =
                    value ?? string.Empty;

                OnPropertyChanged();
            }
        }

        public bool MostrarEliminados
        {
            get => mostrarEliminados;
            set
            {
                if (mostrarEliminados == value)
                    return;

                mostrarEliminados =
                    value && !EsModoSeleccion;

                OnPropertyChanged();

                if (inicializado &&
                    !IsBusy &&
                    !EsModoSeleccion)
                {
                    _ = CargarAsync();
                }
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            set
            {
                modoSeleccionTexto = value;

                if (EsModoSeleccion &&
                    mostrarEliminados)
                {
                    mostrarEliminados = false;
                    OnPropertyChanged(
                        nameof(MostrarEliminados));
                }

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(EsModoSeleccion));
                OnPropertyChanged(
                    nameof(MostrarAccionesAdministracion));
                OnPropertyChanged(
                    nameof(MostrarFiltroEliminados));
                OnPropertyChanged(
                    nameof(Titulo));
                OnPropertyChanged(
                    nameof(TextoRegresar));
            }
        }

        public bool EsModoSeleccion =>
            bool.TryParse(
                ModoSeleccionTexto,
                out bool valor) &&
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
            PermissionService.Instance
                .HasRead(
                    InterfazCodigos.Propietarios);

        public new bool CanAdd =>
            PermissionService.Instance
                .HasAdd(
                    InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance
                .HasUpdate(
                    InterfazCodigos.Propietarios);

        public new bool CanDelete =>
            PermissionService.Instance
                .HasDelete(
                    InterfazCodigos.Propietarios);

        public bool MostrarListaVacia =>
            inicializado &&
            !IsBusy &&
            Propietarios.Count == 0;

        public async Task InicializarAsync()
        {
            if (inicializado)
            {
                await CargarAsync();
                return;
            }

            inicializado = true;
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (IsBusy)
                return;

            if (!ModoSesionService.EsEnLinea)
            {
                await MostrarAdvertenciaAsync(
                    "La administración de propietarios " +
                    "requiere conexión a internet.");

                Propietarios.Clear();
                ActualizarEstadoLista();
                return;
            }

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<ObservableCollection<
                    PropietarioResponse>> result =
                    await service
                        .GetPropietariosResultAsync(
                            TextoBusqueda,
                            incluirInactivos:
                                MostrarEliminados &&
                                !EsModoSeleccion,
                            paraSeleccionTerreno:
                                EsModoSeleccion);

                if (!result.Success ||
                    result.Data == null)
                {
                    await MostrarErrorAsync(
                        result.Message);
                    return;
                }

                Propietarios.Clear();

                foreach (PropietarioResponse item
                         in result.Data)
                {
                    Propietarios.Add(item);
                }

                ActualizarEstadoLista();
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
        }

        private async Task RegresarAsync()
        {
            if (IsBusy)
                return;

            if (EsModoSeleccion)
            {
                /*
                 * La selección se abre desde el formulario de terreno. En
                 * este modo se vuelve a la pantalla que ya existe en la pila.
                 */
                await Shell.Current.GoToAsync("..");
                return;
            }

            /*
             * En administración se regresa mediante una ruta absoluta. Esto
             * limpia las copias históricas de propietarios que pudieron quedar
             * apiladas y evita el ciclo infinito del botón Atrás.
             */
            await GoToAsyncParameters(
                AppRoutes.Configuracion);
        }

        private async Task NuevoAsync()
        {
            if (!CanAdd)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para crear propietarios.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,

                    ["ModoSeleccion"] =
                        EsModoSeleccion.ToString()
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

                PropietarioSeleccionService
                    .Seleccionar(propietario);

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

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.View,

                    ["Propietario"] =
                        propietario,

                    ["ModoSeleccion"] =
                        EsModoSeleccion.ToString()
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

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,

                    ["Propietario"] =
                        propietario,

                    ["ModoSeleccion"] =
                        EsModoSeleccion.ToString()
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

            await GoToAsyncParameters(
                AppRoutes.PropietarioTerrenos,
                new Dictionary<string, object>
                {
                    ["Propietario"] =
                        propietario
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
                    "No se puede eliminar el propietario porque tiene " +
                    "terrenos vinculados. Utilice Ver terrenos para " +
                    "reasignarlos antes de continuar.");
                return;
            }

            bool confirmar =
                await ConfirmarAsync(
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
                    await crudService
                        .EliminarPropietarioResultAsync(
                            propietario.PropietarioId);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                recargar = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "Propietario eliminado correctamente."
                        : resultado.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }

            if (recargar)
                await CargarAsync();
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

            bool confirmar =
                await ConfirmarAsync(
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
                    await crudService
                        .RecuperarPropietarioResultAsync(
                            propietario.PropietarioId);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                recargar = true;

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "Propietario recuperado correctamente."
                        : resultado.Message);
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }

            if (recargar)
                await CargarAsync();
        }

        private void CambiarEstadoOcupado(
            bool valor)
        {
            IsBusy = valor;

            OnPropertyChanged(
                nameof(MostrarListaVacia));

            BuscarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
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
            OnPropertyChanged(
                nameof(MostrarListaVacia));
        }
    }
}
