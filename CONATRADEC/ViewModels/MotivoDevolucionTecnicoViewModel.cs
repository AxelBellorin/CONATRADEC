using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class MotivoDevolucionTecnicoViewModel : GlobalService
    {
        private readonly MotivoDevolucionTecnicoApiService api = new();
        private string buscar = string.Empty;
        private bool incluirInactivos;
        private string mensajeEstado = string.Empty;
        private bool inicializado;

        public MotivoDevolucionTecnicoViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);
            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);
            BuscarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);
            NuevoCommand = new Command(
                async () => await AbrirFormularioAsync(null),
                () => !IsBusy && CanAdd);
            EditarCommand = new Command<MotivoDevolucionTecnicoItem>(
                async item => await AbrirFormularioAsync(item),
                item => item?.Activo == true && !IsBusy && CanEdit);
            EliminarCommand = new Command<MotivoDevolucionTecnicoItem>(
                async item => await CambiarEstadoAsync(item, false),
                item => item?.Activo == true && !IsBusy && CanDelete);
            ActivarCommand = new Command<MotivoDevolucionTecnicoItem>(
                async item => await CambiarEstadoAsync(item, true),
                item => item?.Activo == false && !IsBusy && CanEdit);
        }

        public ObservableCollection<MotivoDevolucionTecnicoItem> Items { get; } = [];
        public Command RegresarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command NuevoCommand { get; }
        public Command<MotivoDevolucionTecnicoItem> EditarCommand { get; }
        public Command<MotivoDevolucionTecnicoItem> EliminarCommand { get; }
        public Command<MotivoDevolucionTecnicoItem> ActivarCommand { get; }

        public string Buscar
        {
            get => buscar;
            set
            {
                if (buscar == value)
                    return;
                buscar = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IncluirInactivos
        {
            get => incluirInactivos;
            set
            {
                if (incluirInactivos == value)
                    return;

                incluirInactivos = value;
                OnPropertyChanged();

                /*
                 * Al marcar o desmarcar el filtro se recarga inmediatamente el
                 * catálogo. Así la opción Activar aparece sin exigir que el
                 * usuario presione también el botón Actualizar.
                 */
                if (inicializado && !IsBusy && CanView)
                    _ = CargarAsync();
            }
        }

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                if (mensajeEstado == value)
                    return;
                mensajeEstado = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado => !string.IsNullOrWhiteSpace(MensajeEstado);
        public bool SinItems => !IsBusy && Items.Count == 0;

        public async Task InicializarAsync()
        {
            MotivoDevolucionTecnicoRoutes.AsegurarRegistro();
            ActualizarPermisos();
            inicializado = true;

            if (CanView)
                await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazConfiguracion);
            CanView = permiso?.leer == true;
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            CanDelete = permiso?.eliminar == true;
            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            ActualizarComandos();
        }

        private async Task CargarAsync()
        {
            if (IsBusy || !CanView)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando motivos de devolución...";
            ActualizarComandos();

            try
            {
                ApiResult<List<MotivoDevolucionTecnicoItem>> resultado =
                    await api.ListarAdministracionAsync(
                        IncluirInactivos,
                        Buscar);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                Items.Clear();
                foreach (MotivoDevolucionTecnicoItem item in resultado.Data ?? [])
                    Items.Add(item);

                OnPropertyChanged(nameof(SinItems));
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync("Motivos de devolución", ex.Message);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinItems));
                ActualizarComandos();
            }
        }

        private async Task AbrirFormularioAsync(
            MotivoDevolucionTecnicoItem? item)
        {
            if (IsBusy)
                return;

            MotivoDevolucionTecnicoRoutes.AsegurarRegistro();
            await GoToAsyncParameters(
                MotivoDevolucionTecnicoRoutes.CrearRutaFormulario(
                    item?.MotivoDevolucionTecnicoId));
        }

        private async Task CambiarEstadoAsync(
            MotivoDevolucionTecnicoItem? item,
            bool recuperar)
        {
            if (item == null || IsBusy)
                return;

            bool confirmar = await ConfirmarAsync(
                recuperar ? "Activar motivo" : "Desactivar motivo",
                recuperar
                    ? $"¿Desea activar «{item.NombreMostrar}»?"
                    : $"¿Desea desactivar «{item.NombreMostrar}»? Las devoluciones históricas conservarán el código, nombre e instrucciones asociados.",
                recuperar ? "Activar" : "Desactivar");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = recuperar
                ? "Activando motivo..."
                : "Desactivando motivo...";
            ActualizarComandos();

            try
            {
                ApiResult<bool> resultado = recuperar
                    ? await api.RecuperarAsync(item.MotivoDevolucionTecnicoId)
                    : await api.EliminarAsync(item.MotivoDevolucionTecnicoId);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                await CargarDespuesOperacionAsync();
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync("Motivos de devolución", ex.Message);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task CargarDespuesOperacionAsync()
        {
            ApiResult<List<MotivoDevolucionTecnicoItem>> resultado =
                await api.ListarAdministracionAsync(IncluirInactivos, Buscar);

            if (!resultado.Success)
                throw new InvalidOperationException(resultado.Message);

            Items.Clear();
            foreach (MotivoDevolucionTecnicoItem item in resultado.Data ?? [])
                Items.Add(item);
            OnPropertyChanged(nameof(SinItems));
        }

        private static Task MostrarAlertaAsync(string titulo, string mensaje) =>
            Shell.Current?.DisplayAlert(titulo, mensaje, "Aceptar") ??
            Task.CompletedTask;

        private static Task<bool> ConfirmarAsync(
            string titulo,
            string mensaje,
            string aceptar) =>
            Shell.Current?.DisplayAlert(titulo, mensaje, aceptar, "Cancelar") ??
            Task.FromResult(false);

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            NuevoCommand.ChangeCanExecute();
            EditarCommand.ChangeCanExecute();
            EliminarCommand.ChangeCanExecute();
            ActivarCommand.ChangeCanExecute();
        }
    }
}
