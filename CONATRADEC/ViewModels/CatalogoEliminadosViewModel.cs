using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class CatalogoEliminadosViewModel : GlobalService
    {
        private readonly CatalogoEliminadoConfiguracion configuracion;
        private readonly CatalogosEliminadosApiService apiService;
        private readonly List<CatalogoEliminadoItem> originales = new();

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;

        public CatalogoEliminadosViewModel(
            CatalogoEliminadoConfiguracion configuracion)
            : this(
                configuracion,
                new CatalogosEliminadosApiService())
        {
        }

        public CatalogoEliminadosViewModel(
            CatalogoEliminadoConfiguracion configuracion,
            CatalogosEliminadosApiService apiService)
        {
            this.configuracion =
                configuracion ??
                throw new ArgumentNullException(
                    nameof(configuracion));

            this.apiService =
                apiService ??
                throw new ArgumentNullException(
                    nameof(apiService));

            BuscarCommand =
                new Command(AplicarFiltro);

            LimpiarCommand =
                new Command(LimpiarFiltro);

            RefrescarCommand =
                new Command(
                    async () =>
                        await RefrescarAsync());

            ReactivarCommand =
                new Command<CatalogoEliminadoItem>(
                    async item =>
                        await ReactivarAsync(item),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy);

            CerrarCommand =
                new Command(
                    async () =>
                        await CerrarAsync());
        }

        public ObservableCollection<CatalogoEliminadoItem>
            Registros { get; } = new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<CatalogoEliminadoItem>
            ReactivarCommand { get; }
        public Command CerrarCommand { get; }

        public string Titulo =>
            configuracion.Titulo;

        public string Descripcion =>
            configuracion.Descripcion;

        public string PlaceholderBusqueda =>
            $"Buscar {configuracion.Singular} eliminado";

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor =
                    value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor =
                    value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneMensaje));
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(
                Mensaje);

        public bool MostrarAccesoDenegado =>
            !CanView;

        public bool MostrarVacio =>
            CanView &&
            !IsBusy &&
            Registros.Count == 0 &&
            !TieneMensaje;

        public string Resumen =>
            Registros.Count == 1
                ? "1 registro eliminado"
                : $"{Registros.Count} registros eliminados";

        public async Task InicializarAsync()
        {
            LoadPagePermissions(
                configuracion.Interfaz);

            OnPropertyChanged(
                nameof(MostrarAccesoDenegado));

            ReactivarCommand.ChangeCanExecute();
            NotificarEstado();

            if (CanView)
            {
                await CargarAsync();
            }
        }

        private async Task CargarAsync()
        {
            if (!CanView ||
                IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();

                ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    resultado =
                        await apiService.ListarAsync(
                            configuracion.Codigo);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    originales.Clear();
                    Registros.Clear();

                    Mensaje =
                        string.IsNullOrWhiteSpace(
                            resultado.Message)
                            ? "No fue posible cargar los registros eliminados."
                            : resultado.Message;

                    return;
                }

                originales.Clear();
                originales.AddRange(
                    resultado.Data
                        .Where(item =>
                            item.Id > 0 &&
                            !item.Activo)
                        .OrderBy(item =>
                            item.Titulo));

                AplicarFiltro();
            }
            catch (Exception ex)
            {
                Mensaje =
                    "Ocurrió un error inesperado al cargar los registros eliminados.";

                await MostrarToastAsync(
                    "Error: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();
            }
        }

        private async Task RefrescarAsync()
        {
            if (IsBusy)
                return;

            IsRefreshing = true;
            await CargarAsync();
        }

        private void AplicarFiltro()
        {
            string filtro =
                TextoBusqueda.Trim();

            IEnumerable<CatalogoEliminadoItem>
                consulta =
                    originales;

            if (!string.IsNullOrWhiteSpace(
                    filtro))
            {
                consulta =
                    consulta.Where(item =>
                        Contiene(
                            item.Titulo,
                            filtro) ||
                        Contiene(
                            item.Subtitulo,
                            filtro) ||
                        Contiene(
                            item.Detalle,
                            filtro) ||
                        Contiene(
                            item.Codigo,
                            filtro));
            }

            Registros.Clear();

            foreach (
                CatalogoEliminadoItem item
                in consulta)
            {
                Registros.Add(item);
            }

            Mensaje = string.Empty;
            NotificarEstado();
        }

        private void LimpiarFiltro()
        {
            TextoBusqueda =
                string.Empty;

            AplicarFiltro();
        }

        private async Task ReactivarAsync(
            CatalogoEliminadoItem? item)
        {
            if (item == null ||
                item.Id <= 0 ||
                IsBusy)
            {
                return;
            }

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permiso para reactivar este registro.");

                return;
            }

            Page? pagina =
                Application.Current?
                    .Windows
                    .FirstOrDefault()?
                    .Page;

            if (pagina == null)
                return;

            bool confirmar =
                await pagina.DisplayAlert(
                    "Reactivar registro",
                    $"¿Desea reactivar '{item.Titulo}' conservando su identificador e historial?",
                    "Reactivar",
                    "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();

                ApiResult<bool> resultado =
                    await apiService.ReactivarAsync(
                        configuracion.Codigo,
                        item.Id);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(
                            resultado.Message)
                            ? "No fue posible reactivar el registro."
                            : resultado.Message);

                    return;
                }

                originales.RemoveAll(
                    registro =>
                        registro.Id ==
                        item.Id);

                Registros.Remove(item);
                NotificarEstado();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "Registro reactivado correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();
            }
        }

        private static bool Contiene(
            string? valor,
            string filtro) =>
            (valor ?? string.Empty)
                .Contains(
                    filtro,
                    StringComparison.OrdinalIgnoreCase);

        private static async Task CerrarAsync()
        {
            if (Shell.Current?
                    .Navigation != null)
            {
                await Shell.Current
                    .Navigation
                    .PopModalAsync();
            }
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(
                nameof(MostrarVacio));

            OnPropertyChanged(
                nameof(Resumen));

            OnPropertyChanged(
                nameof(TieneMensaje));
        }
    }
}
