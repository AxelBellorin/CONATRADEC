using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class BitacoraViewModel : GlobalService
    {
        private readonly BitacoraApiService apiService = new();
        private ObservableCollection<BitacoraListadoItem> registros = new();
        private ObservableCollection<string> acciones = new();
        private ObservableCollection<string> modulos = new();
        private ObservableCollection<BitacoraUsuarioFiltro> usuarios = new();
        private DateTime fechaDesde = DateTime.Today.AddDays(-7);
        private DateTime fechaHasta = DateTime.Today;
        private string accionSeleccionada = "Todas";
        private string moduloSeleccionado = "Todos";
        private BitacoraUsuarioFiltro? usuarioSeleccionado;
        private string estadoSeleccionado = "Todos";
        private string textoBusqueda = string.Empty;
        private int pagina = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private bool catalogosCargados;

        public ObservableCollection<BitacoraListadoItem> Registros
        {
            get => registros;
            private set
            {
                registros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HayRegistros));
                OnPropertyChanged(nameof(NoHayRegistros));
            }
        }

        public ObservableCollection<string> Acciones
        {
            get => acciones;
            private set { acciones = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Modulos
        {
            get => modulos;
            private set { modulos = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BitacoraUsuarioFiltro> Usuarios
        {
            get => usuarios;
            private set { usuarios = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Estados { get; } = new()
        {
            "Todos",
            "Correctos",
            "Con error"
        };

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set { fechaDesde = value; OnPropertyChanged(); }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set { fechaHasta = value; OnPropertyChanged(); }
        }

        public string AccionSeleccionada
        {
            get => accionSeleccionada;
            set { accionSeleccionada = value; OnPropertyChanged(); }
        }

        public string ModuloSeleccionado
        {
            get => moduloSeleccionado;
            set { moduloSeleccionado = value; OnPropertyChanged(); }
        }

        public BitacoraUsuarioFiltro? UsuarioSeleccionado
        {
            get => usuarioSeleccionado;
            set { usuarioSeleccionado = value; OnPropertyChanged(); }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set { estadoSeleccionado = value; OnPropertyChanged(); }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set { textoBusqueda = value; OnPropertyChanged(); }
        }

        public int Pagina
        {
            get => pagina;
            private set
            {
                pagina = value;
                OnPropertyChanged();
                ActualizarPaginacion();
            }
        }

        public int TotalPaginas
        {
            get => totalPaginas;
            private set
            {
                totalPaginas = Math.Max(1, value);
                OnPropertyChanged();
                ActualizarPaginacion();
            }
        }

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public bool HayRegistros => Registros.Count > 0;
        public bool NoHayRegistros => !HayRegistros && !IsBusy;
        public bool PuedeAnterior => Pagina > 1 && !IsBusy;
        public bool PuedeSiguiente => Pagina < TotalPaginas && !IsBusy;
        public string ResumenPagina => $"Página {Pagina} de {TotalPaginas}";
        public string ResumenResultados =>
            $"{TotalRegistros:N0} registro(s) encontrado(s)";

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command AnteriorCommand { get; }
        public Command SiguienteCommand { get; }
        public Command<BitacoraListadoItem> VerDetalleCommand { get; }

        public BitacoraViewModel()
        {
            LoadPagePermissions("bitacoraPage");

            BuscarCommand = new Command(async () =>
                await CargarAsync(true));
            LimpiarCommand = new Command(async () =>
                await LimpiarFiltrosAsync());
            AnteriorCommand = new Command(async () =>
                await CambiarPaginaAsync(-1));
            SiguienteCommand = new Command(async () =>
                await CambiarPaginaAsync(1));
            VerDetalleCommand = new Command<BitacoraListadoItem>(
                async item => await VerDetalleAsync(item));
        }

        public async Task InicializarAsync()
        {
            LoadPagePermissions("bitacoraPage");

            if (!CanView)
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para consultar la bitácora.");
                await GoToAsyncParameters(AppRoutes.Regresar);
                return;
            }

            if (!catalogosCargados)
                await CargarCatalogosAsync();

            await CargarAsync(true);
        }

        public async Task CargarAsync(bool reiniciarPagina)
        {
            if (IsBusy || !CanView)
                return;

            if (FechaHasta.Date < FechaDesde.Date)
            {
                await MostrarAdvertenciaAsync(
                    "La fecha final no puede ser menor que la fecha inicial.");
                return;
            }

            if (!await ValidarInternetAsync())
                return;

            if (reiniciarPagina)
                Pagina = 1;

            IsBusy = true;
            OnPropertyChanged(nameof(NoHayRegistros));

            try
            {
                DateTime desdeUtc = DateTime.SpecifyKind(
                        FechaDesde.Date,
                        DateTimeKind.Local)
                    .ToUniversalTime();

                DateTime hastaUtc = DateTime.SpecifyKind(
                        FechaHasta.Date.AddDays(1).AddTicks(-1),
                        DateTimeKind.Local)
                    .ToUniversalTime();

                bool? exitoso = EstadoSeleccionado switch
                {
                    "Correctos" => true,
                    "Con error" => false,
                    _ => null
                };

                ApiResult<BitacoraPaginadaResponse> resultado =
                    await apiService.ListarAsync(
                        desdeUtc,
                        hastaUtc,
                        UsuarioSeleccionado?.UsuarioId,
                        AccionSeleccionada == "Todas"
                            ? null
                            : AccionSeleccionada,
                        ModuloSeleccionado == "Todos"
                            ? null
                            : ModuloSeleccionado,
                        exitoso,
                        TextoBusqueda,
                        Pagina,
                        25);

                if (!resultado.Success || resultado.Data == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    Registros = new();
                    TotalRegistros = 0;
                    TotalPaginas = 1;
                    return;
                }

                Registros = new ObservableCollection<BitacoraListadoItem>(
                    resultado.Data.Items);
                Pagina = resultado.Data.Pagina;
                TotalPaginas = resultado.Data.TotalPaginas;
                TotalRegistros = resultado.Data.TotalRegistros;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(NoHayRegistros));
                ActualizarPaginacion();
            }
        }

        private async Task CargarCatalogosAsync()
        {
            ApiResult<BitacoraCatalogosResponse> resultado =
                await apiService.CatalogosAsync();

            if (!resultado.Success || resultado.Data == null)
            {
                await MostrarErrorAsync(resultado.Message);
                return;
            }

            Acciones = new ObservableCollection<string>(
                new[] { "Todas" }.Concat(resultado.Data.Acciones));
            Modulos = new ObservableCollection<string>(
                new[] { "Todos" }.Concat(resultado.Data.Modulos));

            var todos = new BitacoraUsuarioFiltro
            {
                UsuarioId = null,
                Nombre = "Todos"
            };

            Usuarios = new ObservableCollection<BitacoraUsuarioFiltro>(
                new[] { todos }.Concat(resultado.Data.Usuarios));

            AccionSeleccionada = Acciones.First();
            ModuloSeleccionado = Modulos.First();
            UsuarioSeleccionado = Usuarios.First();
            catalogosCargados = true;
        }

        private async Task LimpiarFiltrosAsync()
        {
            FechaDesde = DateTime.Today.AddDays(-7);
            FechaHasta = DateTime.Today;
            AccionSeleccionada = Acciones.FirstOrDefault() ?? "Todas";
            ModuloSeleccionado = Modulos.FirstOrDefault() ?? "Todos";
            UsuarioSeleccionado = Usuarios.FirstOrDefault();
            EstadoSeleccionado = "Todos";
            TextoBusqueda = string.Empty;
            await CargarAsync(true);
        }

        private async Task CambiarPaginaAsync(int incremento)
        {
            int nuevaPagina = Pagina + incremento;
            if (nuevaPagina < 1 || nuevaPagina > TotalPaginas || IsBusy)
                return;

            Pagina = nuevaPagina;
            await CargarAsync(false);
        }

        private async Task VerDetalleAsync(BitacoraListadoItem? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.BitacoraDetalle,
                new Dictionary<string, object>
                {
                    ["BitacoraId"] = item.BitacoraId
                });
        }

        private void ActualizarPaginacion()
        {
            OnPropertyChanged(nameof(PuedeAnterior));
            OnPropertyChanged(nameof(PuedeSiguiente));
            OnPropertyChanged(nameof(ResumenPagina));
            AnteriorCommand?.ChangeCanExecute();
            SiguienteCommand?.ChangeCanExecute();
        }
    }
}
