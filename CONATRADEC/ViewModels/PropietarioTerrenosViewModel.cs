using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    [QueryProperty(
        nameof(Propietario),
        "Propietario")]
    public sealed class PropietarioTerrenosViewModel :
        GlobalService
    {
        private readonly PropietarioTerrenosApiService
            terrenosService =
                new();

        private readonly PropietarioApiService
            propietarioApiService =
                new();

        private PropietarioResponse? propietario;

        private PropietarioTerrenoResumenResponse?
            terrenoSeleccionado;

        private string textoBusquedaDestino =
            string.Empty;

        private bool mostrarSelectorPropietario;

        public PropietarioTerrenosViewModel()
        {
            RegresarCommand =
                new Command(
                    async () =>
                        await RegresarAsync(),
                    () => !IsBusy);

            ActualizarCommand =
                new Command(
                    async () =>
                        await CargarDetalleAsync(),
                    () => !IsBusy);

            CambiarPropietarioCommand =
                new Command<
                    PropietarioTerrenoResumenResponse>(
                    async terreno =>
                        await AbrirSelectorAsync(
                            terreno),
                    terreno =>
                        terreno != null &&
                        terreno.Activo &&
                        CanEdit &&
                        !IsBusy);

            BuscarPropietariosCommand =
                new Command(
                    async () =>
                        await CargarPropietariosDestinoAsync(),
                    () =>
                        MostrarSelectorPropietario &&
                        !IsBusy);

            SeleccionarPropietarioCommand =
                new Command<PropietarioResponse>(
                    async propietarioDestino =>
                        await ReasignarAsync(
                            propietarioDestino),
                    propietarioDestino =>
                        propietarioDestino != null &&
                        propietarioDestino.Activo &&
                        propietarioDestino.PropietarioId > 0 &&
                        propietarioDestino.PropietarioId !=
                            Propietario?.PropietarioId &&
                        TerrenoSeleccionado != null &&
                        CanEdit &&
                        !IsBusy);

            CancelarCambioCommand =
                new Command(
                    CerrarSelector,
                    () =>
                        MostrarSelectorPropietario &&
                        !IsBusy);

            Terrenos.CollectionChanged +=
                (_, _) =>
                {
                    OnPropertyChanged(
                        nameof(HayTerrenos));
                    OnPropertyChanged(
                        nameof(NoHayTerrenos));
                    OnPropertyChanged(
                        nameof(TextoResumenTerrenos));
                };

            PropietariosDestino.CollectionChanged +=
                (_, _) =>
                {
                    OnPropertyChanged(
                        nameof(HayPropietariosDestino));
                    OnPropertyChanged(
                        nameof(NoHayPropietariosDestino));
                };
        }

        public ObservableCollection<
            PropietarioTerrenoResumenResponse>
            Terrenos { get; } = new();

        public ObservableCollection<
            PropietarioResponse>
            PropietariosDestino { get; } = new();

        public Command RegresarCommand { get; }

        public Command ActualizarCommand { get; }

        public Command<
            PropietarioTerrenoResumenResponse>
            CambiarPropietarioCommand { get; }

        public Command BuscarPropietariosCommand { get; }

        public Command<PropietarioResponse>
            SeleccionarPropietarioCommand { get; }

        public Command CancelarCambioCommand { get; }

        public PropietarioResponse? Propietario
        {
            get => propietario;
            set
            {
                if (ReferenceEquals(
                        propietario,
                        value))
                {
                    return;
                }

                propietario = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(Titulo));
                OnPropertyChanged(
                    nameof(TextoPropietario));
                OnPropertyChanged(
                    nameof(TextoIdentificacion));
                OnPropertyChanged(
                    nameof(TextoContacto));
                OnPropertyChanged(
                    nameof(TextoEstadoPropietario));
                OnPropertyChanged(
                    nameof(TextoResumenTerrenos));

                ActualizarComandos();
            }
        }

        public PropietarioTerrenoResumenResponse?
            TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            private set
            {
                if (ReferenceEquals(
                        terrenoSeleccionado,
                        value))
                {
                    return;
                }

                terrenoSeleccionado = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TextoTerrenoSeleccionado));

                ActualizarComandos();
            }
        }

        public string TextoBusquedaDestino
        {
            get => textoBusquedaDestino;
            set
            {
                string nuevo =
                    value ?? string.Empty;

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

        public string Titulo =>
            "Terrenos del propietario";

        public string TextoPropietario =>
            Propietario?.TextoPrincipal ??
            "Propietario";

        public string TextoIdentificacion =>
            Propietario?.TextoIdentificacion ??
            "Sin identificación";

        public string TextoContacto =>
            Propietario?.TextoContacto ??
            "Sin contacto registrado";

        public string TextoEstadoPropietario =>
            Propietario?.TextoEstado ??
            string.Empty;

        public string TextoResumenTerrenos =>
            Terrenos.Count == 1
                ? "1 terreno vinculado"
                : $"{Terrenos.Count} terrenos vinculados";

        public string TextoTerrenoSeleccionado =>
            TerrenoSeleccionado == null
                ? string.Empty
                : "Cambiar propietario de " +
                  TerrenoSeleccionado.TextoCodigo;

        public bool HayTerrenos =>
            Terrenos.Count > 0;

        public bool NoHayTerrenos =>
            Terrenos.Count == 0;

        public bool HayPropietariosDestino =>
            PropietariosDestino.Count > 0;

        public bool NoHayPropietariosDestino =>
            PropietariosDestino.Count == 0;

        public new bool CanView =>
            PermissionService.Instance
                .HasRead(
                    InterfazCodigos.Propietarios);

        public new bool CanEdit =>
            PermissionService.Instance
                .HasUpdate(
                    InterfazCodigos.Propietarios);

        public async Task InicializarAsync()
        {
            await CargarDetalleAsync();
        }

        private async Task CargarDetalleAsync()
        {
            if (IsBusy)
                return;

            int propietarioId =
                Propietario?.PropietarioId ??
                0;

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
                    "La administración de propietarios requiere " +
                    "conexión a internet.");
                return;
            }

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<PropietarioDetalleResponse>
                    resultado =
                        await terrenosService
                            .ObtenerDetalleAsync(
                                propietarioId);

                if (!resultado.Success ||
                    resultado.Data?.Propietario == null)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                PropietarioResponse propietarioActual =
                    resultado.Data.Propietario;

                propietarioActual.TotalTerrenos =
                    resultado.Data.Terrenos.Count;

                Propietario =
                    propietarioActual;

                Terrenos.Clear();

                foreach (
                    PropietarioTerrenoResumenResponse terreno
                    in resultado.Data.Terrenos)
                {
                    Terrenos.Add(terreno);
                }

                OnPropertyChanged(
                    nameof(TextoResumenTerrenos));
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
                    "No tiene permiso para cambiar el propietario " +
                    "de un terreno.");
                return;
            }

            if (!terreno.Activo)
            {
                await MostrarAdvertenciaAsync(
                    "No se puede reasignar un terreno inactivo.");
                return;
            }

            TerrenoSeleccionado =
                terreno;

            TextoBusquedaDestino =
                string.Empty;

            MostrarSelectorPropietario =
                true;

            await CargarPropietariosDestinoAsync();
        }

        private async Task
            CargarPropietariosDestinoAsync()
        {
            if (IsBusy ||
                !MostrarSelectorPropietario)
            {
                return;
            }

            CambiarEstadoOcupado(true);

            try
            {
                ApiResult<ObservableCollection<
                    PropietarioResponse>> resultado =
                        await propietarioApiService
                            .GetPropietariosResultAsync(
                                TextoBusquedaDestino,
                                incluirInactivos: false,
                                paraSeleccionTerreno: true);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                int propietarioActualId =
                    Propietario?.PropietarioId ??
                    0;

                PropietariosDestino.Clear();

                foreach (PropietarioResponse item
                         in resultado.Data
                             .Where(item =>
                                 item.Activo &&
                                 item.PropietarioId !=
                                     propietarioActualId)
                             .OrderBy(item =>
                                 item.NombreCompleto))
                {
                    PropietariosDestino.Add(item);
                }
            }
            finally
            {
                CambiarEstadoOcupado(false);
            }
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

            bool confirmar =
                await ConfirmarAsync(
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
                    await terrenosService
                        .ReasignarTerrenoAsync(
                            propietarioDestino.PropietarioId,
                            terreno.TerrenoId);

                if (!resultado.Success ||
                    resultado.Data != true)
                {
                    await MostrarErrorAsync(
                        resultado.Message);
                    return;
                }

                Terrenos.Remove(terreno);

                if (Propietario != null)
                {
                    Propietario.TotalTerrenos =
                        Terrenos.Count;

                    OnPropertyChanged(
                        nameof(TextoResumenTerrenos));
                }

                CerrarSelector();

                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
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

            await GoToAsyncParameters(
                AppRoutes.Regresar);
        }

        private void CerrarSelector()
        {
            MostrarSelectorPropietario =
                false;

            TerrenoSeleccionado =
                null;

            TextoBusquedaDestino =
                string.Empty;

            PropietariosDestino.Clear();
        }

        private void CambiarEstadoOcupado(
            bool valor)
        {
            IsBusy = valor;
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            CambiarPropietarioCommand.ChangeCanExecute();
            BuscarPropietariosCommand.ChangeCanExecute();
            SeleccionarPropietarioCommand.ChangeCanExecute();
            CancelarCambioCommand.ChangeCanExecute();
        }
    }
}
