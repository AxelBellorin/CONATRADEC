using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class MainPageViewModel : GlobalService
    {
        private const string AlcanceTodos = "Todos los análisis";
        private const string AlcancePropios = "Solo mis análisis";

        private readonly GuardarTodoApiService guardarTodoApiService = new();
        private readonly AnalisisUsuarioApiService analisisUsuarioApiService = new();
        private readonly UserApiService userApiService = new();

        private readonly List<AnalisisGuardadoResumen> todosAnalisis = new();

        private ObservableCollection<AnalisisGuardadoResumen>
            analisisGuardados = new();

        private bool isRefreshing;
        private string mensaje = string.Empty;
        private string textoBusqueda = string.Empty;

        private bool usarFiltroRangoFecha;

        // Rango predeterminado: últimos 7 días, incluyendo hoy.
        private DateTime fechaDesde =
            DateTime.Today.AddDays(-6);

        private DateTime fechaHasta =
            DateTime.Today;

        private string errorRangoFecha = string.Empty;

        private bool esAdministrador;
        private bool seHaListado;

        private string alcanceListadoSeleccionado = AlcanceTodos;
        private UsuarioFiltroAnalisis? usuarioFiltroSeleccionado;

        public MainPageViewModel()
        {
            UsuariosFiltro =
                new ObservableCollection<UsuarioFiltroAnalisis>();

            OpcionesAlcanceListado =
                new ObservableCollection<string>
                {
                    AlcanceTodos,
                    AlcancePropios
                };

            ListarCommand = new Command(
                async () => await ListarManualmenteAsync(),
                () => !IsBusy);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && SeHaListado);

            VisualizarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async analisis => await VisualizarAsync(analisis),
                    analisis => !IsBusy && analisis != null);

            EditarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async analisis => await EditarAsync(analisis),
                    analisis => !IsBusy && analisis != null);

            EliminarCommand =
                new Command<AnalisisGuardadoResumen>(
                    async analisis => await EliminarAsync(analisis),
                    analisis => !IsBusy && analisis != null);

            LimpiarFiltrosCommand = new Command(LimpiarFiltros);

            NuevoAnalisisCommand = new Command(
                async () => await NuevoAnalisisAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<AnalisisGuardadoResumen>
            AnalisisGuardados
        {
            get => analisisGuardados;
            private set
            {
                if (ReferenceEquals(analisisGuardados, value))
                    return;

                analisisGuardados =
                    value ??
                    new ObservableCollection<
                        AnalisisGuardadoResumen>();

                OnPropertyChanged(nameof(AnalisisGuardados));
                NotificarResumenLista();
            }
        }

        public ObservableCollection<UsuarioFiltroAnalisis>
            UsuariosFiltro
        { get; }

        public ObservableCollection<string>
            OpcionesAlcanceListado
        { get; }

        public Command ListarCommand { get; }

        public Command ActualizarCommand { get; }

        public Command<AnalisisGuardadoResumen>
            VisualizarCommand
        { get; }

        public Command<AnalisisGuardadoResumen>
            EditarCommand
        { get; }

        public Command<AnalisisGuardadoResumen>
            EliminarCommand
        { get; }

        public Command LimpiarFiltrosCommand { get; }

        public Command NuevoAnalisisCommand { get; }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;

                ListarCommand.ChangeCanExecute();
                ActualizarCommand.ChangeCanExecute();
                VisualizarCommand.ChangeCanExecute();
                EditarCommand.ChangeCanExecute();
                EliminarCommand.ChangeCanExecute();
                NuevoAnalisisCommand.ChangeCanExecute();
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
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged(nameof(TextoBusqueda));
                AplicarFiltros();
            }
        }

        public bool UsarFiltroRangoFecha
        {
            get => usarFiltroRangoFecha;
            set
            {
                if (usarFiltroRangoFecha == value)
                    return;

                usarFiltroRangoFecha = value;
                OnPropertyChanged(nameof(UsarFiltroRangoFecha));
                AplicarFiltros();
            }
        }

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                fechaDesde = value.Date;
                OnPropertyChanged(nameof(FechaDesde));
                AplicarFiltros();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                fechaHasta = value.Date;
                OnPropertyChanged(nameof(FechaHasta));
                AplicarFiltros();
            }
        }

        public string ErrorRangoFecha
        {
            get => errorRangoFecha;
            private set
            {
                errorRangoFecha = value ?? string.Empty;
                OnPropertyChanged(nameof(ErrorRangoFecha));
                OnPropertyChanged(nameof(TieneErrorRangoFecha));
            }
        }

        public bool TieneErrorRangoFecha =>
            !string.IsNullOrWhiteSpace(ErrorRangoFecha);

        public bool EsAdministrador
        {
            get => esAdministrador;
            private set
            {
                if (esAdministrador == value)
                    return;

                esAdministrador = value;
                OnPropertyChanged(nameof(EsAdministrador));
                OnPropertyChanged(nameof(MostrarSelectorAlcance));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));
            }
        }

        public bool MostrarSelectorAlcance => EsAdministrador;

        public string AlcanceListadoSeleccionado
        {
            get => alcanceListadoSeleccionado;
            set
            {
                string nuevoValor =
                    string.IsNullOrWhiteSpace(value)
                        ? AlcanceTodos
                        : value;

                if (string.Equals(
                        alcanceListadoSeleccionado,
                        nuevoValor,
                        StringComparison.Ordinal))
                {
                    return;
                }

                alcanceListadoSeleccionado = nuevoValor;

                OnPropertyChanged(nameof(AlcanceListadoSeleccionado));
                OnPropertyChanged(nameof(ListarSoloPropios));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));

                ReiniciarListadoPorCambioAlcance();
            }
        }

        public bool ListarSoloPropios =>
            !EsAdministrador ||
            string.Equals(
                AlcanceListadoSeleccionado,
                AlcancePropios,
                StringComparison.OrdinalIgnoreCase);

        public bool PuedeFiltrarPorUsuario =>
            EsAdministrador &&
            !ListarSoloPropios &&
            SeHaListado;

        public bool SeHaListado
        {
            get => seHaListado;
            private set
            {
                if (seHaListado == value)
                    return;

                seHaListado = value;

                OnPropertyChanged(nameof(SeHaListado));
                OnPropertyChanged(nameof(NoSeHaListado));
                OnPropertyChanged(nameof(TextoBotonListar));
                OnPropertyChanged(nameof(MensajeListaVacia));
                OnPropertyChanged(nameof(SubtituloListaVacia));
                OnPropertyChanged(nameof(TotalMostradoTexto));
                OnPropertyChanged(nameof(PuedeFiltrarPorUsuario));

                ActualizarCommand.ChangeCanExecute();
            }
        }

        public bool NoSeHaListado => !SeHaListado;

        public string TextoBotonListar =>
            SeHaListado ? "Actualizar lista" : "Listar análisis";

        public string MensajeListaVacia =>
            SeHaListado
                ? "No hay análisis para mostrar"
                : "Los análisis no se cargan automáticamente";

        public string SubtituloListaVacia =>
            SeHaListado
                ? "Cambie los filtros o cree un nuevo análisis."
                : "Seleccione el alcance y presione Listar análisis.";

        public UsuarioFiltroAnalisis?
            UsuarioFiltroSeleccionado
        {
            get => usuarioFiltroSeleccionado;
            set
            {
                if (ReferenceEquals(usuarioFiltroSeleccionado, value))
                    return;

                usuarioFiltroSeleccionado = value;
                OnPropertyChanged(nameof(UsuarioFiltroSeleccionado));
                AplicarFiltros();
            }
        }

        public bool TieneAnalisis =>
            AnalisisGuardados.Count > 0;

        public string TotalMostradoTexto =>
            !SeHaListado
                ? "Listado bajo demanda"
                : AnalisisGuardados.Count == 1
                    ? "1 análisis encontrado"
                    : $"{AnalisisGuardados.Count} análisis encontrados";

        public void PrepararPantalla()
        {
            string rolNombre =
                Preferences.Get(
                    SessionKeys.KeyRolNombre,
                    string.Empty);

            if (string.IsNullOrWhiteSpace(rolNombre))
                return;

            bool administrador =
                EsRolAdministrador(rolNombre);

            EsAdministrador = administrador;

            if (!administrador &&
                !string.Equals(
                    alcanceListadoSeleccionado,
                    AlcancePropios,
                    StringComparison.Ordinal))
            {
                alcanceListadoSeleccionado = AlcancePropios;

                OnPropertyChanged(
                    nameof(AlcanceListadoSeleccionado));

                OnPropertyChanged(nameof(ListarSoloPropios));

                OnPropertyChanged(
                    nameof(PuedeFiltrarPorUsuario));
            }
        }

        public async Task CargarAnalisisAsync(
            bool mostrarIndicador = true)
        {
            if (IsBusy)
                return;

            try
            {
                AnalisisEdicionService.Instance.Limpiar();

                if (mostrarIndicador)
                    IsBusy = true;

                Mensaje = string.Empty;
                ErrorRangoFecha = string.Empty;

                int usuarioActualId =
                    ObtenerUsuarioActualId();

                if (usuarioActualId <= 0)
                {
                    Mensaje =
                        "No se encontró el usuario autenticado. " +
                        "Cierre sesión e ingrese nuevamente.";

                    return;
                }

                string rolNombre =
                    Preferences.Get(
                        SessionKeys.KeyRolNombre,
                        string.Empty);

                List<UserResponse> usuarios = new();

                if (string.IsNullOrWhiteSpace(rolNombre))
                {
                    ApiResult<ObservableCollection<UserResponse>>
                        resultadoUsuarios =
                            await userApiService
                                .GetUsersResultAsync();

                    usuarios =
                        resultadoUsuarios.Data?.ToList() ??
                        new List<UserResponse>();

                    UserResponse? actual =
                        usuarios.FirstOrDefault(x =>
                            x.UsuarioId == usuarioActualId);

                    rolNombre =
                        actual?.RolNombre ?? string.Empty;
                }

                bool administrador =
                    EsRolAdministrador(rolNombre);

                EsAdministrador = administrador;

                if (!administrador)
                {
                    alcanceListadoSeleccionado =
                        AlcancePropios;

                    OnPropertyChanged(
                        nameof(AlcanceListadoSeleccionado));

                    OnPropertyChanged(nameof(ListarSoloPropios));
                }

                bool listarSoloUsuarioActual =
                    !administrador ||
                    string.Equals(
                        AlcanceListadoSeleccionado,
                        AlcancePropios,
                        StringComparison.OrdinalIgnoreCase);

                if (administrador &&
                    !listarSoloUsuarioActual &&
                    usuarios.Count == 0)
                {
                    ApiResult<ObservableCollection<UserResponse>>
                        resultadoUsuarios =
                            await userApiService
                                .GetUsersResultAsync();

                    usuarios =
                        resultadoUsuarios.Data?.ToList() ??
                        new List<UserResponse>();
                }

                int? filtroServidor =
                    listarSoloUsuarioActual
                        ? usuarioActualId
                        : null;

                /*
                 * Ambas consultas son independientes y se ejecutan juntas.
                 *
                 * El listado por usuario conserva exactamente el alcance
                 * actual. El listado resumido aporta los tres indicadores
                 * de cálculos complementarios en una sola petición.
                 */
                Task<AnalisisGuardadoUsuarioListaResponse>
                    tareaListado =
                        analisisUsuarioApiService
                            .ListarAsync(filtroServidor);

                Task<AnalisisGuardadoListaResponse>
                    tareaComplementos =
                        guardarTodoApiService.ListarAsync();

                await Task.WhenAll(
                    tareaListado,
                    tareaComplementos);

                AnalisisGuardadoUsuarioListaResponse
                    respuestaListado =
                        await tareaListado;

                if (!respuestaListado.Success)
                {
                    Mensaje =
                        string.IsNullOrWhiteSpace(
                            respuestaListado.Message)
                            ? "No fue posible cargar los análisis."
                            : respuestaListado.Message;

                    await MostrarToastAsync(Mensaje);
                    return;
                }

                AnalisisGuardadoListaResponse
                    respuestaComplementos =
                        await tareaComplementos;

                Dictionary<int, AnalisisGuardadoResumen>
                    complementos =
                        (respuestaComplementos.Data ??
                         new List<AnalisisGuardadoResumen>())
                            .Where(x =>
                                x.AnalisisSueloCalculoId > 0)
                            .GroupBy(x =>
                                x.AnalisisSueloCalculoId)
                            .ToDictionary(
                                x => x.Key,
                                x => x.First());

                Dictionary<int, string> nombresUsuario =
                    usuarios
                        .Where(x => x.UsuarioId.HasValue)
                        .GroupBy(x => x.UsuarioId!.Value)
                        .ToDictionary(
                            x => x.Key,
                            x =>
                                x.First()
                                    .NombreCompletoUsuario ??
                                x.First()
                                    .NombreUsuario ??
                                $"Usuario #{x.Key}");

                todosAnalisis.Clear();

                foreach (
                    AnalisisGuardadoUsuarioItem item
                    in respuestaListado.Data)
                {
                    if (item.Calculo == null ||
                        item.Calculo
                            .AnalisisSueloCalculoId <= 0)
                    {
                        continue;
                    }

                    complementos.TryGetValue(
                        item.Calculo
                            .AnalisisSueloCalculoId,
                        out AnalisisGuardadoResumen?
                            complemento);

                    int? usuarioId =
                        item.Calculo.UsuarioId;

                    string nombreUsuario =
                        ObtenerNombreUsuario(
                            usuarioId,
                            usuarioActualId,
                            nombresUsuario);

                    todosAnalisis.Add(
                        new AnalisisGuardadoResumen
                        {
                            AnalisisSueloId =
                                item.AnalisisSueloId,

                            AnalisisSueloCalculoId =
                                item.Calculo
                                    .AnalisisSueloCalculoId,

                            IdentificadorAnalisisSuelo =
                                item.IdentificadorAnalisisSuelo,

                            LaboratorioAnalasisSuelo =
                                item.LaboratorioAnalasisSuelo,

                            FechaAnalisisSuelo =
                                item.FechaAnalisisSuelo,

                            FechaCalculo =
                                item.Calculo.FechaCalculo,

                            TerrenoId =
                                item.Terreno?.TerrenoId ?? 0,

                            CodigoTerreno =
                                item.Terreno?
                                    .CodigoTerreno ??
                                string.Empty,

                            NombreCliente =
                                item.Terreno?
                                    .NombrePropietarioTerreno ??
                                string.Empty,

                            NombreTerreno =
                                item.Terreno?
                                    .CodigoTerreno ??
                                string.Empty,

                            TipoCultivoId =
                                item.TipoCultivo?
                                    .TipoCultivoId ?? 0,

                            TipoAnalisisSueloId =
                                item.TipoAnalisisSuelo?
                                    .TipoAnalisisSueloId ?? 0,

                            CantidadQuintalesOro =
                                item.Calculo
                                    .CantidadQuintalesOro,

                            TamanoFinca =
                                item.Calculo.TamanoFinca,

                            PhAnalisisSuelo =
                                item.Calculo
                                    .PhAnalisisSuelo,

                            UsuarioId = usuarioId,
                            NombreUsuario = nombreUsuario,

                            TieneFormulaNutricional =
                                complemento?
                                    .TieneFormulaNutricional ??
                                false,

                            TieneEnmiendaCalcarea =
                                complemento?
                                    .TieneEnmiendaCalcarea ??
                                false,

                            TieneFertilizacionMixta =
                                complemento?
                                    .TieneFertilizacionMixta ??
                                false
                        });
                }

                ConfigurarFiltroUsuarios(
                    usuarios,
                    listarSoloUsuarioActual);

                AplicarFiltros();
                SeHaListado = true;
            }
            catch (Exception ex)
            {
                Mensaje =
                    $"No fue posible cargar los análisis: {ex.Message}";

                await MostrarToastAsync(Mensaje);
            }
            finally
            {
                if (mostrarIndicador)
                    IsBusy = false;
            }
        }

        private void ConfigurarFiltroUsuarios(
            IEnumerable<UserResponse> usuarios,
            bool listarSoloUsuarioActual)
        {
            int? seleccionAnterior =
                UsuarioFiltroSeleccionado?.UsuarioId;

            UsuariosFiltro.Clear();

            if (!EsAdministrador ||
                listarSoloUsuarioActual)
            {
                usuarioFiltroSeleccionado = null;

                OnPropertyChanged(
                    nameof(UsuarioFiltroSeleccionado));

                OnPropertyChanged(
                    nameof(PuedeFiltrarPorUsuario));

                return;
            }

            UsuariosFiltro.Add(
                new UsuarioFiltroAnalisis
                {
                    UsuarioId = null,
                    NombreCompleto =
                        "Todos los usuarios"
                });

            foreach (
                UserResponse usuario
                in usuarios
                    .Where(x =>
                        x.UsuarioId.HasValue)
                    .OrderBy(x =>
                        x.NombreCompletoUsuario ??
                        x.NombreUsuario))
            {
                UsuariosFiltro.Add(
                    new UsuarioFiltroAnalisis
                    {
                        UsuarioId = usuario.UsuarioId,

                        NombreCompleto =
                            usuario.NombreCompletoUsuario ??
                            usuario.NombreUsuario ??
                            $"Usuario #{usuario.UsuarioId}"
                    });
            }

            foreach (
                int usuarioId
                in todosAnalisis
                    .Where(x =>
                        x.UsuarioId.HasValue)
                    .Select(x =>
                        x.UsuarioId!.Value)
                    .Distinct()
                    .OrderBy(x => x))
            {
                if (UsuariosFiltro.Any(x =>
                        x.UsuarioId == usuarioId))
                {
                    continue;
                }

                string nombre =
                    todosAnalisis.First(x =>
                        x.UsuarioId ==
                        usuarioId).UsuarioMostrar;

                UsuariosFiltro.Add(
                    new UsuarioFiltroAnalisis
                    {
                        UsuarioId = usuarioId,
                        NombreCompleto = nombre
                    });
            }

            usuarioFiltroSeleccionado =
                UsuariosFiltro.FirstOrDefault(x =>
                    x.UsuarioId == seleccionAnterior)
                ?? UsuariosFiltro.FirstOrDefault();

            OnPropertyChanged(
                nameof(UsuarioFiltroSeleccionado));

            OnPropertyChanged(
                nameof(PuedeFiltrarPorUsuario));
        }

        private void AplicarFiltros()
        {
            ErrorRangoFecha = string.Empty;

            IEnumerable<AnalisisGuardadoResumen>
                consulta = todosAnalisis;

            string texto =
                (TextoBusqueda ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.TextoBusqueda.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (UsarFiltroRangoFecha)
            {
                if (FechaDesde.Date > FechaHasta.Date)
                {
                    ErrorRangoFecha =
                        "La fecha Desde no puede ser mayor " +
                        "que la fecha Hasta.";

                    AnalisisGuardados =
                        new ObservableCollection<
                            AnalisisGuardadoResumen>();

                    return;
                }

                consulta = consulta.Where(x =>
                    x.FechaAnalisisValor.HasValue &&
                    x.FechaAnalisisValor.Value.Date >=
                        FechaDesde.Date &&
                    x.FechaAnalisisValor.Value.Date <=
                        FechaHasta.Date);
            }

            if (PuedeFiltrarPorUsuario &&
                UsuarioFiltroSeleccionado?
                    .UsuarioId is int usuarioId)
            {
                consulta = consulta.Where(x =>
                    x.UsuarioId == usuarioId);
            }

            List<AnalisisGuardadoResumen> filtrados =
                consulta
                    .OrderByDescending(
                        ObtenerFechaPrincipal)
                    .ThenBy(
                        x => x.ClienteMostrar,
                        StringComparer
                            .CurrentCultureIgnoreCase)
                    .ThenByDescending(
                        x => x.FechaCalculoValor ??
                             DateTime.MinValue)
                    .ThenBy(
                        x => x.IdentificadorMostrar,
                        StringComparer
                            .CurrentCultureIgnoreCase)
                    .ToList();

            /*
             * Android usa RecyclerView para CollectionView. Reemplazar la
             * colección completa genera una sola actualización y evita
             * ejecutar Clear/Add mientras RecyclerView calcula su layout.
             */
            AnalisisGuardados =
                new ObservableCollection<
                    AnalisisGuardadoResumen>(filtrados);
        }

        private static DateTime ObtenerFechaPrincipal(
            AnalisisGuardadoResumen analisis)
        {
            DateTime fecha =
                analisis.FechaAnalisisValor
                ?? analisis.FechaCalculoValor
                ?? DateTime.MinValue;

            return fecha.Date;
        }

        private void LimpiarFiltros()
        {
            textoBusqueda = string.Empty;
            usarFiltroRangoFecha = false;

            // Al limpiar, se restablecen los últimos 7 días.
            fechaDesde =
                DateTime.Today.AddDays(-6);

            fechaHasta =
                DateTime.Today;

            ErrorRangoFecha = string.Empty;

            if (PuedeFiltrarPorUsuario)
            {
                usuarioFiltroSeleccionado =
                    UsuariosFiltro.FirstOrDefault();
            }

            OnPropertyChanged(nameof(TextoBusqueda));

            OnPropertyChanged(
                nameof(UsarFiltroRangoFecha));

            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));

            OnPropertyChanged(
                nameof(UsuarioFiltroSeleccionado));

            AplicarFiltros();
        }

        private void ReiniciarListadoPorCambioAlcance()
        {
            todosAnalisis.Clear();
            AnalisisGuardados =
                new ObservableCollection<
                    AnalisisGuardadoResumen>();

            UsuariosFiltro.Clear();

            usuarioFiltroSeleccionado = null;
            Mensaje = string.Empty;
            ErrorRangoFecha = string.Empty;
            SeHaListado = false;

            OnPropertyChanged(
                nameof(UsuarioFiltroSeleccionado));

        }

        private void NotificarResumenLista()
        {
            OnPropertyChanged(nameof(TieneAnalisis));

            OnPropertyChanged(
                nameof(TotalMostradoTexto));
        }

        private async Task ListarManualmenteAsync()
        {
            if (IsBusy)
                return;

            await CargarAnalisisAsync(true);
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy || !SeHaListado)
                return;

            try
            {
                IsRefreshing = true;
                await CargarAnalisisAsync(false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task NuevoAnalisisAsync()
        {
            if (IsBusy || !CanAdd)
                return;

            AnalisisEdicionService.Instance.Limpiar();

            await GoToAsyncParameters(
                "//NuevoAnalisisFormPage");
        }

        private async Task VisualizarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null ||
                IsBusy ||
                !CanView)
            {
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.AnalisisGuardadoDetalle,
                new Dictionary<string, object>
                {
                    ["analisisSueloCalculoId"] =
                        analisis.AnalisisSueloCalculoId,

                    ["resumenAnalisis"] = analisis
                });
        }

        private async Task EditarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar análisis.");

                return;
            }

            try
            {
                IsBusy = true;

                Mensaje =
                    "Cargando el análisis para edición...";

                (bool success, string message) =
                    await AnalisisEdicionService.Instance
                        .PrepararAsync(
                            analisis
                                .AnalisisSueloCalculoId,
                            analisis);

                if (!success)
                {
                    Mensaje = message;

                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "No se pudo abrir",
                            message,
                            "Aceptar");

                    return;
                }

                Mensaje = string.Empty;

                await GoToAsyncParameters(
                    "//NuevoAnalisisFormPage");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EliminarAsync(
            AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar análisis.");

                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Eliminar análisis",
                        $"¿Desea eliminar el análisis " +
                        $"{analisis.IdentificadorMostrar}? " +
                        "Esta acción también desactivará " +
                        "sus cálculos relacionados.",
                        "Sí, eliminar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                EliminarAnalisisResponse respuesta =
                    await guardarTodoApiService
                        .EliminarAsync(
                            analisis.AnalisisSueloId);

                if (!respuesta.Success)
                {
                    Mensaje =
                        string.IsNullOrWhiteSpace(
                            respuesta.Message)
                            ? "La API no pudo eliminar el análisis."
                            : respuesta.Message;

                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "No se pudo eliminar",
                            Mensaje,
                            "Aceptar");

                    return;
                }

                todosAnalisis.RemoveAll(x =>
                    x.AnalisisSueloId ==
                    analisis.AnalisisSueloId);

                AplicarFiltros();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(
                        respuesta.Message)
                        ? "Análisis eliminado correctamente."
                        : respuesta.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string ObtenerNombreUsuario(
            int? usuarioId,
            int usuarioActualId,
            IReadOnlyDictionary<int, string>
                nombresUsuario)
        {
            if (usuarioId.HasValue &&
                nombresUsuario.TryGetValue(
                    usuarioId.Value,
                    out string? nombre))
            {
                return nombre;
            }

            if (usuarioId == usuarioActualId)
            {
                return Preferences.Get(
                    SessionKeys.KeyNombreCompletoUsuario,
                    $"Usuario #{usuarioId}");
            }

            return $"Usuario #{usuarioId}";
        }

        private static bool EsRolAdministrador(
            string? rolNombre)
        {
            return
                !string.IsNullOrWhiteSpace(rolNombre) &&
                rolNombre.Contains(
                    "ADMIN",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static int ObtenerUsuarioActualId()
        {
            string valor =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    "0");

            return int.TryParse(
                valor,
                out int id)
                    ? id
                    : 0;
        }
    }
}
