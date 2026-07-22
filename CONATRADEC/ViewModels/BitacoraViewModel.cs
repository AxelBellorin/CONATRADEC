using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class BitacoraViewModel : GlobalService
    {
        private readonly BitacoraApiService apiService = new();
        private readonly List<BitacoraUsuarioFiltro> opcionesUsuarios = new();

        private ObservableCollection<BitacoraListadoItem> registros = new();
        private ObservableCollection<string> acciones = new();
        private ObservableCollection<string> modulos = new();
        private ObservableCollection<string> usuarios = new();

        private DateTime fechaDesde = DateTime.Today.AddDays(-7);
        private DateTime fechaHasta = DateTime.Today;
        private string accionSeleccionada = "Todas";
        private string moduloSeleccionado = "Todos";
        private int usuarioSeleccionadoIndex;
        private string estadoSeleccionado = "Todos";
        private string textoBusqueda = string.Empty;
        private int pagina = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private bool catalogosCargados;
        private bool consultaRealizada;

        public ObservableCollection<BitacoraListadoItem> Registros
        {
            get => registros;
            private set
            {
                registros = value ?? new ObservableCollection<BitacoraListadoItem>();

                OnPropertyChanged();
                ActualizarEstadoResultados();
            }
        }

        public ObservableCollection<string> Acciones
        {
            get => acciones;
            private set
            {
                acciones = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Modulos
        {
            get => modulos;
            private set
            {
                modulos = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// El Picker recibe únicamente textos. El identificador real se
        /// conserva en opcionesUsuarios utilizando el mismo índice.
        /// </summary>
        public ObservableCollection<string> Usuarios
        {
            get => usuarios;
            private set
            {
                usuarios = value ?? new ObservableCollection<string>();
                OnPropertyChanged();
            }
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
            set
            {
                fechaDesde = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                fechaHasta = value;
                OnPropertyChanged();
            }
        }

        public string AccionSeleccionada
        {
            get => accionSeleccionada;
            set
            {
                accionSeleccionada = value ?? "Todas";
                OnPropertyChanged();
            }
        }

        public string ModuloSeleccionado
        {
            get => moduloSeleccionado;
            set
            {
                moduloSeleccionado = value ?? "Todos";
                OnPropertyChanged();
            }
        }

        public int UsuarioSeleccionadoIndex
        {
            get => usuarioSeleccionadoIndex;
            set
            {
                int indice = value;

                if (indice < 0 && Usuarios.Count > 0)
                    indice = 0;

                if (usuarioSeleccionadoIndex == indice)
                    return;

                usuarioSeleccionadoIndex = indice;
                OnPropertyChanged();
            }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                estadoSeleccionado = value ?? "Todos";
                OnPropertyChanged();
            }
        }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public int Pagina
        {
            get => pagina;
            private set
            {
                pagina = Math.Max(1, value);
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
                totalRegistros = Math.Max(0, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
            }
        }

        public bool HayRegistros => Registros.Count > 0;

        /// <summary>
        /// Se muestra únicamente después de terminar correctamente una
        /// consulta que no devolvió coincidencias.
        /// </summary>
        public bool MostrarSinResultados =>
            consultaRealizada &&
            !HayRegistros &&
            !IsBusy;

        public bool MostrarAyudaDetalle =>
            HayRegistros &&
            !IsBusy;

        /// <summary>
        /// La paginación no se renderiza cuando la colección está vacía.
        /// Esto evita el problema de medición de CollectionView en WinUI.
        /// </summary>
        public bool MostrarPaginacion =>
            HayRegistros &&
            !IsBusy;

        public bool PuedeAnterior =>
            MostrarPaginacion &&
            Pagina > 1;

        public bool PuedeSiguiente =>
            MostrarPaginacion &&
            Pagina < TotalPaginas;

        public string ResumenPagina =>
            $"Página {Pagina} de {TotalPaginas}";

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 registro encontrado"
                : $"{TotalRegistros:N0} registros encontrados";

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command AnteriorCommand { get; }
        public Command SiguienteCommand { get; }
        public Command<BitacoraListadoItem> VerDetalleCommand { get; }

        public BitacoraViewModel()
        {
            LoadPagePermissions("bitacoraPage");

            BuscarCommand = new Command(
                async () => await CargarAsync(true));

            LimpiarCommand = new Command(
                async () => await LimpiarFiltrosAsync());

            AnteriorCommand = new Command(
                async () => await CambiarPaginaAsync(-1));

            SiguienteCommand = new Command(
                async () => await CambiarPaginaAsync(1));

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
            ActualizarEstadoResultados();

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
                        ObtenerUsuarioSeleccionadoId(),
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
                    consultaRealizada = false;
                    Registros = new ObservableCollection<BitacoraListadoItem>();
                    TotalRegistros = 0;
                    Pagina = 1;
                    TotalPaginas = 1;

                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible consultar la bitácora."
                            : resultado.Message);

                    return;
                }

                List<BitacoraListadoItem> items =
                    resultado.Data.Items ??
                    new List<BitacoraListadoItem>();

                Registros = new ObservableCollection<BitacoraListadoItem>(
                    items);

                Pagina = resultado.Data.Pagina;
                TotalPaginas = resultado.Data.TotalPaginas;
                TotalRegistros = resultado.Data.TotalRegistros;
                consultaRealizada = true;
            }
            catch (Exception ex)
            {
                consultaRealizada = false;
                Registros = new ObservableCollection<BitacoraListadoItem>();
                TotalRegistros = 0;
                Pagina = 1;
                TotalPaginas = 1;

                await MostrarErrorInesperadoAsync(
                    "consultar la bitácora",
                    ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarEstadoResultados();
                ActualizarPaginacion();
            }
        }

        private async Task CargarCatalogosAsync()
        {
            ApiResult<BitacoraCatalogosResponse> resultado =
                await apiService.CatalogosAsync();

            if (!resultado.Success || resultado.Data == null)
            {
                await MostrarErrorAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible cargar los filtros de la bitácora."
                        : resultado.Message);

                return;
            }

            Acciones = new ObservableCollection<string>(
                new[] { "Todas" }
                    .Concat(resultado.Data.Acciones ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            Modulos = new ObservableCollection<string>(
                new[] { "Todos" }
                    .Concat(resultado.Data.Modulos ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            ConstruirCatalogoUsuarios(resultado.Data.Usuarios);

            AccionSeleccionada =
                Acciones.FirstOrDefault() ?? "Todas";

            ModuloSeleccionado =
                Modulos.FirstOrDefault() ?? "Todos";

            SeleccionarPrimerUsuario();

            EstadoSeleccionado =
                Estados.FirstOrDefault() ?? "Todos";

            catalogosCargados = true;
        }

        private void ConstruirCatalogoUsuarios(
            IEnumerable<BitacoraUsuarioFiltro>? usuariosApi)
        {
            opcionesUsuarios.Clear();

            opcionesUsuarios.Add(new BitacoraUsuarioFiltro
            {
                UsuarioId = null,
                Nombre = "Todos"
            });

            IEnumerable<BitacoraUsuarioFiltro> usuariosValidos =
                (usuariosApi ??
                 Enumerable.Empty<BitacoraUsuarioFiltro>())
                .Where(x =>
                    x.UsuarioId.HasValue &&
                    x.UsuarioId.Value > 0 &&
                    !string.IsNullOrWhiteSpace(x.Nombre))
                .GroupBy(x => x.UsuarioId)
                .Select(x => x.First())
                .OrderBy(x => x.Nombre);

            opcionesUsuarios.AddRange(usuariosValidos);

            HashSet<string> nombresRepetidos = opcionesUsuarios
                .Where(x => x.UsuarioId.HasValue)
                .GroupBy(
                    x => x.Nombre.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Usuarios = new ObservableCollection<string>(
                opcionesUsuarios.Select(x =>
                {
                    string nombre = x.Nombre.Trim();

                    return x.UsuarioId.HasValue &&
                           nombresRepetidos.Contains(nombre)
                        ? $"{nombre} (ID {x.UsuarioId.Value})"
                        : nombre;
                }));

            SeleccionarPrimerUsuario();
        }

        private void SeleccionarPrimerUsuario()
        {
            usuarioSeleccionadoIndex =
                Usuarios.Count > 0
                    ? 0
                    : -1;

            OnPropertyChanged(
                nameof(UsuarioSeleccionadoIndex));
        }

        private int? ObtenerUsuarioSeleccionadoId()
        {
            int indice = UsuarioSeleccionadoIndex;

            return indice >= 0 &&
                   indice < opcionesUsuarios.Count
                ? opcionesUsuarios[indice].UsuarioId
                : null;
        }

        private async Task LimpiarFiltrosAsync()
        {
            if (IsBusy)
                return;

            FechaDesde = DateTime.Today.AddDays(-7);
            FechaHasta = DateTime.Today;

            AccionSeleccionada =
                Acciones.FirstOrDefault() ?? "Todas";

            ModuloSeleccionado =
                Modulos.FirstOrDefault() ?? "Todos";

            SeleccionarPrimerUsuario();

            EstadoSeleccionado =
                Estados.FirstOrDefault() ?? "Todos";

            TextoBusqueda = string.Empty;

            await CargarAsync(true);
        }

        private async Task CambiarPaginaAsync(
            int incremento)
        {
            int nuevaPagina = Pagina + incremento;

            if (nuevaPagina < 1 ||
                nuevaPagina > TotalPaginas ||
                IsBusy)
            {
                return;
            }

            Pagina = nuevaPagina;
            await CargarAsync(false);
        }

        private async Task VerDetalleAsync(
            BitacoraListadoItem? item)
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

        private void ActualizarEstadoResultados()
        {
            OnPropertyChanged(nameof(HayRegistros));
            OnPropertyChanged(nameof(MostrarSinResultados));
            OnPropertyChanged(nameof(MostrarAyudaDetalle));
            OnPropertyChanged(nameof(MostrarPaginacion));
        }

        private void ActualizarPaginacion()
        {
            OnPropertyChanged(nameof(PuedeAnterior));
            OnPropertyChanged(nameof(PuedeSiguiente));
            OnPropertyChanged(nameof(ResumenPagina));
            OnPropertyChanged(nameof(MostrarPaginacion));

            AnteriorCommand?.ChangeCanExecute();
            SiguienteCommand?.ChangeCanExecute();
        }
    }
}
