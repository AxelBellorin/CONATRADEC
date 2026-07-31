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

        private string textoBusqueda =
            string.Empty;

        private string? modoSeleccionTexto;

        private bool inicializado;

        public PropietariosViewModel()
        {
            BuscarCommand = new Command(
                async () => await CargarAsync());

            ActualizarCommand = new Command(
                async () => await CargarAsync());

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

            EditarCommand =
                new Command<PropietarioResponse>(
                    async propietario =>
                        await EditarAsync(propietario),
                    propietario =>
                        propietario != null &&
                        CanEdit &&
                        !IsBusy);
        }

        public ObservableCollection<
            PropietarioResponse> Propietarios
        {
            get;
        } = new();

        public Command BuscarCommand { get; }

        public Command ActualizarCommand { get; }

        public Command NuevoCommand { get; }

        public Command<PropietarioResponse>
            AbrirCommand { get; }

        public Command<PropietarioResponse>
            EditarCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                if (textoBusqueda == value)
                    return;

                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string? ModoSeleccionTexto
        {
            get => modoSeleccionTexto;
            set
            {
                modoSeleccionTexto = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(EsModoSeleccion));

                OnPropertyChanged(
                    nameof(Titulo));
            }
        }

        public bool EsModoSeleccion =>
            bool.TryParse(
                ModoSeleccionTexto,
                out bool valor) &&
            valor;

        public string Titulo =>
            EsModoSeleccion
                ? "Seleccionar propietario"
                : "Propietarios";

        public new bool CanAdd =>
            PermissionService.Instance
                .HasAdd(
                    InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance
                .HasUpdate(
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
                            incluirInactivos: false,
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
                        "No puede asignar un propietario inactivo.");
                    return;
                }

                PropietarioSeleccionService
                    .Seleccionar(propietario);

                await Shell.Current.GoToAsync("..");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.View,

                    ["Propietario"] =
                        propietario
                });
        }

        private async Task EditarAsync(
            PropietarioResponse? propietario)
        {
            if (propietario == null)
                return;

            await GoToAsyncParameters(
                AppRoutes.PropietarioFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,

                    ["Propietario"] =
                        propietario
                });
        }

        private void CambiarEstadoOcupado(bool valor)
        {
            IsBusy = valor;

            OnPropertyChanged(
                nameof(MostrarListaVacia));

            NuevoCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
        }

        private void ActualizarEstadoLista()
        {
            OnPropertyChanged(
                nameof(MostrarListaVacia));
        }
    }
}
