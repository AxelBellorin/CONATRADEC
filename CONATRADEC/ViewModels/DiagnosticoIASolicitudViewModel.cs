using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Opción visible del tipo de fotografía. El nombre se presenta al usuario
    /// y el código se envía sin cambios al backend.
    /// </summary>
    public sealed class TipoFotografiaOpcion
    {
        public TipoFotografiaOpcion(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        public string Codigo { get; }
        public string Nombre { get; }
    }

    /// <summary>
    /// Registra inspecciones y muestra las bandejas. La IA se ejecuta después
    /// desde el detalle, permitiendo seleccionar una, varias o todas las fotos.
    ///
    /// Para mejorar el rendimiento, la pantalla de captura mantiene una sola
    /// fotografía dentro del árbol visual. Las demás permanecen como archivos
    /// temporales y se muestran únicamente cuando el usuario navega hacia ellas.
    /// </summary>
    public sealed class DiagnosticoIASolicitudViewModel :
        DiagnosticoIAViewModelBase
    {
        private const double ZoomMinimo = 1d;
        private const double ZoomMaximo = 3d;
        private const double PasoZoom = 0.25d;

        private bool inicializado;
        private string modoVista = DiagnosticoIARoutes.ModoMisInspecciones;
        private string codigoTerreno = string.Empty;
        private string observacion = string.Empty;
        private TerrenoBusquedaIAItem? terrenoSeleccionado;
        private int indiceFotoActual = -1;
        private bool esVisorAbierto;
        private double escalaVisor = ZoomMinimo;

        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly SemaphoreSlim cargaBandejaLock = new(1, 1);

        private bool cargaInicialCompletada;
        private bool estaCargandoMas;
        private bool hayMas;
        private DateTime? siguienteFechaUtc;
        private int? siguienteId;

        private string buscarInspeccion = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string departamentoFiltro = string.Empty;
        private bool usarFechaDesde;
        private bool usarFechaHasta;
        private bool filtrosExpandidos;
        private DateTime fechaDesde = FechaDesdePredeterminada;
        private DateTime fechaHasta = DateTime.Today;
        private FiltroCodigoOpcionV2? tipoFotografiaFiltroSeleccionado;
        private FiltroCodigoOpcionV2? estadoFiltroSeleccionado;

        public DiagnosticoIASolicitudViewModel()
        {
            Fotos.CollectionChanged += AlCambiarColeccionFotos;

            tipoFotografiaFiltroSeleccionado =
                TiposFotografiaFiltro[0];
            estadoFiltroSeleccionado =
                EstadosInspeccionFiltro[0];

            AgregarFotoCommand = new Command(
                async () => await AgregarFotosAsync(),
                () => !IsBusy && EsModoNueva);

            TomarFotoCommand = new Command(
                async () => await TomarFotoAsync(),
                () => !IsBusy &&
                      EsModoNueva &&
                      MediaPicker.Default.IsCaptureSupported);

            QuitarFotoCommand = new Command<InspeccionFotoLocal>(
                QuitarFoto,
                item => item != null && !IsBusy);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && EsModoNueva && TieneFotos);

            ActualizarCommand = new Command(
                async () => await BuscarInspeccionesAsync(),
                () => !IsBusy && !EstaCargandoMas && EsModoListado);

            BuscarInspeccionesCommand = new Command(
                async () => await BuscarInspeccionesAsync(),
                () => !IsBusy && !EstaCargandoMas && EsModoListado);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && !EstaCargandoMas && EsModoListado);

            AlternarFiltrosCommand = new Command(
                AlternarFiltros,
                () => !IsBusy && EsModoListado);

            CargarMasCommand = new Command(
                async () => await CargarBandejaAsync(reiniciar: false),
                () => PuedeCargarMas);

            AbrirResultadoCommand =
                new Command<InspeccionFitosanitariaBandejaItemV2>(
                    async item => await AbrirResultadoAsync(item),
                    item => item != null && !IsBusy && !EstaCargandoMas);

            BuscarTerrenoCommand = new Command(
                async () => await GoToAsyncParameters(
                    DiagnosticoIARoutes.PaginaBusquedaTerreno),
                () => !IsBusy && EsModoNueva);

            QuitarTerrenoCommand = new Command(
                QuitarTerreno,
                () => !IsBusy && TerrenoSeleccionado != null);

            FotoAnteriorCommand = new Command(
                IrFotoAnterior,
                () => !IsBusy && TieneFotoAnterior);

            FotoSiguienteCommand = new Command(
                IrFotoSiguiente,
                () => !IsBusy && TieneFotoSiguiente);

            AbrirVisorCommand = new Command(
                AbrirVisor,
                () => !IsBusy && TieneFotos);

            CerrarVisorCommand = new Command(
                CerrarVisor,
                () => EsVisorAbierto);

            AumentarZoomCommand = new Command(
                AumentarZoom,
                () => EsVisorAbierto && EscalaVisor < ZoomMaximo);

            ReducirZoomCommand = new Command(
                ReducirZoom,
                () => EsVisorAbierto && EscalaVisor > ZoomMinimo);

            RestablecerZoomCommand = new Command(
                RestablecerZoom,
                () => EsVisorAbierto && EscalaVisor > ZoomMinimo);
        }

        public ObservableCollection<InspeccionFotoLocal> Fotos { get; } = [];
        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public IReadOnlyList<TipoFotografiaOpcion> TiposFotografia { get; } =
        [
            new("EVIDENCIA", "Evidencia general"),
            new("HOJA", "Hoja"),
            new("FRUTO", "Fruto"),
            new("TALLO", "Tallo"),
            new("RAMA", "Rama"),
            new("PLANTA_COMPLETA", "Planta completa"),
            new("RAIZ", "Raíz"),
            new("OTRA", "Otra")
        ];


        public IReadOnlyList<FiltroCodigoOpcionV2>
            TiposFotografiaFiltro { get; } =
        [
            new(string.Empty, "Todos los tipos"),
            new("EVIDENCIA", "Evidencia general"),
            new("HOJA", "Hoja"),
            new("FRUTO", "Fruto"),
            new("TALLO", "Tallo"),
            new("RAMA", "Rama"),
            new("PLANTA_COMPLETA", "Planta completa"),
            new("RAIZ", "Raíz"),
            new("OTRA", "Otra")
        ];

        public IReadOnlyList<FiltroCodigoOpcionV2>
            EstadosInspeccionFiltro { get; } =
        [
            new(string.Empty, "Todos los estados"),
            new("BORRADOR", "Borrador"),
            new("EN_PROCESO", "En proceso"),
            new("EN_PROCESO_CON_ERRORES", "En proceso con errores"),
            new("PENDIENTE_REVISION", "Pendiente de revisión"),
            new("PENDIENTE_APROBACION", "Pendiente de aprobación"),
            new("FINALIZADA", "Finalizada"),
            new("FINALIZADA_PARCIALMENTE", "Finalizada parcialmente")
        ];

        public Command AgregarFotoCommand { get; }
        public Command TomarFotoCommand { get; }
        public Command<InspeccionFotoLocal> QuitarFotoCommand { get; }
        public Command GuardarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command BuscarInspeccionesCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command CargarMasCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2>
            AbrirResultadoCommand { get; }
        public Command BuscarTerrenoCommand { get; }
        public Command QuitarTerrenoCommand { get; }
        public Command FotoAnteriorCommand { get; }
        public Command FotoSiguienteCommand { get; }
        public Command AbrirVisorCommand { get; }
        public Command CerrarVisorCommand { get; }
        public Command AumentarZoomCommand { get; }
        public Command ReducirZoomCommand { get; }
        public Command RestablecerZoomCommand { get; }

        private static readonly DateTime FechaMinimaPermitida =
            new(2000, 1, 1);

        private static DateTime FechaMaximaPermitida =>
            DateTime.Today;

        private static DateTime FechaDesdePredeterminada
        {
            get
            {
                DateTime candidata = DateTime.Today.AddDays(-30);
                return candidata < FechaMinimaPermitida
                    ? FechaMinimaPermitida
                    : candidata;
            }
        }

        public DateTime FechaMinimaFiltro =>
            FechaMinimaPermitida;

        public DateTime FechaMaximaFiltro =>
            FechaMaximaPermitida;

        public string CodigoTerreno
        {
            get => codigoTerreno;
            set
            {
                string nuevo = value ?? string.Empty;
                if (codigoTerreno == nuevo)
                    return;

                codigoTerreno = nuevo;
                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => observacion;
            set
            {
                string nuevo = value ?? string.Empty;
                if (observacion == nuevo)
                    return;

                observacion = nuevo;
                OnPropertyChanged();
            }
        }

        public TerrenoBusquedaIAItem? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            private set
            {
                if (ReferenceEquals(terrenoSeleccionado, value))
                    return;

                terrenoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));
                OnPropertyChanged(nameof(TerrenoSeleccionadoTexto));
                QuitarTerrenoCommand.ChangeCanExecute();
            }
        }

        public bool TieneTerrenoSeleccionado =>
            TerrenoSeleccionado != null;

        public string TerrenoSeleccionadoTexto =>
            TerrenoSeleccionado == null
                ? "La inspección puede guardarse sin terreno vinculado."
                : TerrenoSeleccionado.ResumenSeleccion;

        public bool EsModoNueva =>
            modoVista == DiagnosticoIARoutes.ModoNuevaInspeccion;

        public bool EsModoListado => !EsModoNueva;

        public string TituloPagina => modoVista switch
        {
            DiagnosticoIARoutes.ModoNuevaInspeccion =>
                "Nueva inspección fitosanitaria",
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial =>
                "Historial de inspecciones",
            _ => "Mis inspecciones"
        };

        public string SubtituloPagina => EsModoNueva
            ? "Registre la evidencia y la fecha real de identificación en campo. El análisis se ejecutará por fotografía."
            : "Cada tarjeta resume el avance individual de las fotografías.";

        public bool TieneFotos => Fotos.Count > 0;

        public bool TieneVariasFotos => Fotos.Count > 1;

        public bool SinFotos => !TieneFotos;

        public string ResumenFotos => Fotos.Count == 1
            ? "1 fotografía preparada"
            : $"{Fotos.Count} fotografías preparadas";

        public InspeccionFotoLocal? FotoActual =>
            indiceFotoActual >= 0 &&
            indiceFotoActual < Fotos.Count
                ? Fotos[indiceFotoActual]
                : null;

        /// <summary>
        /// Solo una de estas dos fuentes contiene imagen a la vez. Al abrir el
        /// visor se libera la fuente de la tarjeta y viceversa, evitando que la
        /// misma fotografía quede decodificada en dos controles simultáneos.
        /// </summary>
        public ImageSource? ImagenTarjetaActual =>
            VisorCerrado ? FotoActual?.Miniatura : null;

        public ImageSource? ImagenVisorActual =>
            EsVisorAbierto ? FotoActual?.Miniatura : null;

        public string ContadorFotoActual => TieneFotos
            ? $"Fotografía {indiceFotoActual + 1} de {Fotos.Count}"
            : "Sin fotografías";

        public bool TieneFotoAnterior =>
            TieneFotos && indiceFotoActual > 0;

        public bool TieneFotoSiguiente =>
            TieneFotos && indiceFotoActual < Fotos.Count - 1;

        /// <summary>
        /// El Picker muestra nombres legibles, pero la fotografía conserva el
        /// código requerido por la API (HOJA, FRUTO, PLANTA_COMPLETA, etc.).
        /// </summary>
        public TipoFotografiaOpcion? TipoFotografiaSeleccionada
        {
            get
            {
                string? codigo = FotoActual?.TipoFotografia;

                return string.IsNullOrWhiteSpace(codigo)
                    ? null
                    : TiposFotografia.FirstOrDefault(
                        opcion => opcion.Codigo == codigo);
            }
            set
            {
                if (FotoActual == null ||
                    value == null ||
                    FotoActual.TipoFotografia == value.Codigo)
                {
                    return;
                }

                FotoActual.TipoFotografia = value.Codigo;
            }
        }

        public string TipoFotografiaActualTexto =>
            TipoFotografiaSeleccionada?.Nombre ?? "Sin tipo";

        public string EstadoNavegacionFoto
        {
            get
            {
                if (!TieneFotos)
                    return string.Empty;

                if (Fotos.Count == 1)
                    return "Única fotografía agregada";

                if (!TieneFotoAnterior)
                    return "Primera fotografía · avance con Siguiente";

                if (!TieneFotoSiguiente)
                    return "Última fotografía · regrese con Anterior";

                return "Puede avanzar o regresar";
            }
        }

        public string EnfoqueFotoActual =>
            FotoActual?.TipoFotografia switch
            {
                "HOJA" =>
                    "Prioriza el haz y el envés de la hoja. Observa manchas, clorosis, necrosis, perforaciones, galerías, pústulas, micelio, esporas, insectos, huevos, deformaciones, bordes y patrón de distribución de los síntomas.",
                "FRUTO" =>
                    "Procura mostrar varios frutos y un acercamiento del daño. Observa perforaciones, pudriciones, manchas, deformaciones, insectos, residuos, maduración irregular y distribución de los síntomas.",
                "TALLO" =>
                    "Incluye el área afectada y tejido sano alrededor. Observa lesiones, grietas, exudados, perforaciones, galerías, hongos, necrosis y cambios de coloración.",
                "RAMA" =>
                    "Muestra la rama completa y un acercamiento. Observa secamiento, pérdida de hojas, lesiones, perforaciones, insectos, deformaciones y distribución del daño.",
                "PLANTA_COMPLETA" =>
                    "Fotografía la planta completa con buena iluminación. Observa vigor, marchitez, defoliación, crecimiento desigual, coloración general y distribución de síntomas.",
                "RAIZ" =>
                    "Limpia suavemente el exceso de suelo sin dañar la raíz. Observa pudrición, coloración, deformaciones, nódulos, lesiones, insectos y pérdida de raíces finas.",
                "OTRA" =>
                    "Incluye una vista general y un acercamiento del hallazgo, procurando buena iluminación, enfoque y una referencia clara del tamaño.",
                _ =>
                    "Incluye una vista general y otra cercana del hallazgo. Mantén buena iluminación, enfoque y suficiente contexto para interpretar la evidencia."
            };

        public bool EsVisorAbierto
        {
            get => esVisorAbierto;
            private set
            {
                if (esVisorAbierto == value)
                    return;

                esVisorAbierto = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VisorCerrado));
                OnPropertyChanged(nameof(ImagenTarjetaActual));
                OnPropertyChanged(nameof(ImagenVisorActual));
                ActualizarComandosVisor();
            }
        }

        public bool VisorCerrado => !EsVisorAbierto;

        public double EscalaVisor
        {
            get => escalaVisor;
            private set
            {
                double nuevaEscala = Math.Clamp(
                    value,
                    ZoomMinimo,
                    ZoomMaximo);

                if (Math.Abs(escalaVisor - nuevaEscala) < 0.001d)
                    return;

                escalaVisor = nuevaEscala;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PorcentajeZoom));
                ActualizarComandosVisor();
            }
        }

        public string PorcentajeZoom =>
            $"{Math.Round(EscalaVisor * 100d):0}%";

        public string BuscarInspeccion
        {
            get => buscarInspeccion;
            set
            {
                string nuevo = value ?? string.Empty;
                if (buscarInspeccion == nuevo)
                    return;

                buscarInspeccion = nuevo;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public string PropietarioFiltro
        {
            get => propietarioFiltro;
            set
            {
                string nuevo = value ?? string.Empty;
                if (propietarioFiltro == nuevo)
                    return;

                propietarioFiltro = nuevo;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public string DepartamentoFiltro
        {
            get => departamentoFiltro;
            set
            {
                string nuevo = value ?? string.Empty;
                if (departamentoFiltro == nuevo)
                    return;

                departamentoFiltro = nuevo;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public bool UsarFechaDesde
        {
            get => usarFechaDesde;
            set
            {
                if (usarFechaDesde == value)
                    return;

                usarFechaDesde = value;

                if (usarFechaDesde &&
                    (FechaDesde < FechaMinimaFiltro ||
                     FechaDesde > FechaMaximaFiltro))
                {
                    FechaDesde = FechaDesdePredeterminada;
                }

                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public bool UsarFechaHasta
        {
            get => usarFechaHasta;
            set
            {
                if (usarFechaHasta == value)
                    return;

                usarFechaHasta = value;

                if (usarFechaHasta &&
                    (FechaHasta < FechaMinimaFiltro ||
                     FechaHasta > FechaMaximaFiltro))
                {
                    FechaHasta = FechaMaximaFiltro;
                }

                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                DateTime nueva = LimitarFechaFiltro(value);
                if (fechaDesde == nueva)
                    return;

                fechaDesde = nueva;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                DateTime nueva = LimitarFechaFiltro(value);
                if (fechaHasta == nueva)
                    return;

                fechaHasta = nueva;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public bool FiltrosExpandidos
        {
            get => filtrosExpandidos;
            private set
            {
                if (filtrosExpandidos == value)
                    return;

                filtrosExpandidos = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonFiltros));
            }
        }

        public int CantidadFiltrosActivos
        {
            get
            {
                int cantidad = 0;

                if (!string.IsNullOrWhiteSpace(BuscarInspeccion))
                    cantidad++;
                if (!string.IsNullOrWhiteSpace(PropietarioFiltro))
                    cantidad++;
                if (!string.IsNullOrWhiteSpace(DepartamentoFiltro))
                    cantidad++;
                if (!string.IsNullOrWhiteSpace(
                        TipoFotografiaFiltroSeleccionado.Codigo))
                    cantidad++;
                if (!string.IsNullOrWhiteSpace(
                        EstadoFiltroSeleccionado.Codigo))
                    cantidad++;
                if (UsarFechaDesde)
                    cantidad++;
                if (UsarFechaHasta)
                    cantidad++;

                return cantidad;
            }
        }

        public bool TieneFiltrosActivos =>
            CantidadFiltrosActivos > 0;

        public string TextoBotonFiltros =>
            FiltrosExpandidos
                ? "Ocultar ▲"
                : CantidadFiltrosActivos == 0
                    ? "Buscar y filtrar ▼"
                    : $"Buscar y filtrar ({CantidadFiltrosActivos}) ▼";

        public string ResumenFiltrosActivos =>
            CantidadFiltrosActivos == 0
                ? "Sin filtros adicionales"
                : CantidadFiltrosActivos == 1
                    ? "1 filtro activo"
                    : $"{CantidadFiltrosActivos} filtros activos";

        public FiltroCodigoOpcionV2 TipoFotografiaFiltroSeleccionado
        {
            get => tipoFotografiaFiltroSeleccionado ??
                TiposFotografiaFiltro[0];
            set
            {
                if (ReferenceEquals(tipoFotografiaFiltroSeleccionado, value))
                    return;

                tipoFotografiaFiltroSeleccionado = value;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public FiltroCodigoOpcionV2 EstadoFiltroSeleccionado
        {
            get => estadoFiltroSeleccionado ??
                EstadosInspeccionFiltro[0];
            set
            {
                if (ReferenceEquals(estadoFiltroSeleccionado, value))
                    return;

                estadoFiltroSeleccionado = value;
                OnPropertyChanged();
                NotificarEstadoFiltros();
            }
        }

        public bool EstaCargandoMas
        {
            get => estaCargandoMas;
            private set
            {
                if (estaCargandoMas == value)
                    return;

                estaCargandoMas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeCargarMas));
                OnPropertyChanged(nameof(MostrarCargarMas));
                ActualizarComandosListado();
            }
        }

        public bool HayMas
        {
            get => hayMas;
            private set
            {
                if (hayMas == value)
                    return;

                hayMas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeCargarMas));
                OnPropertyChanged(nameof(MostrarCargarMas));
                OnPropertyChanged(nameof(TextoResultadoListado));
                ActualizarComandosListado();
            }
        }

        public bool PuedeCargarMas =>
            EsModoListado &&
            HayMas &&
            !IsBusy &&
            !EstaCargandoMas;

        public bool MostrarCargarMas =>
            TieneSolicitudes &&
            (HayMas || EstaCargandoMas);

        public bool TieneSolicitudes => Solicitudes.Count > 0;

        public string TextoResultadoListado
        {
            get
            {
                string cantidad = Solicitudes.Count == 1
                    ? "1 inspección cargada"
                    : $"{Solicitudes.Count} inspecciones cargadas";

                return HayMas
                    ? cantidad + " · hay más resultados"
                    : cantidad;
            }
        }

        public bool SinSolicitudes =>
            EsModoListado &&
            cargaInicialCompletada &&
            !IsBusy &&
            !EstaCargandoMas &&
            Solicitudes.Count == 0;

        public void AplicarModo(string? modo)
        {
            modoVista = DiagnosticoIARoutes.NormalizarModo(modo);
            CerrarVisor();

            cargaInicialCompletada = false;
            siguienteFechaUtc = null;
            siguienteId = null;
            HayMas = false;

            if (EsModoListado)
                Solicitudes.Clear();

            OnPropertyChanged(nameof(EsModoNueva));
            OnPropertyChanged(nameof(EsModoListado));
            OnPropertyChanged(nameof(TituloPagina));
            OnPropertyChanged(nameof(SubtituloPagina));
            OnPropertyChanged(nameof(TieneSolicitudes));
            OnPropertyChanged(nameof(TextoResultadoListado));
            OnPropertyChanged(nameof(SinSolicitudes));
            OnPropertyChanged(nameof(PuedeCargarMas));
            OnPropertyChanged(nameof(MostrarCargarMas));
            ActualizarComandos();
        }

        public void AplicarTerrenoSeleccionado(
            TerrenoBusquedaIAItem terreno)
        {
            TerrenoSeleccionado = terreno;
            CodigoTerreno = terreno.CodigoTerreno;
        }

        public async Task InicializarAsync()
        {
            if (!ValidarEnLinea())
                return;

            if (inicializado && EsModoNueva)
                return;

            inicializado = true;

            if (EsModoListado)
                await CargarBandejaAsync(reiniciar: true);
        }

        public void CerrarVisor()
        {
            EsVisorAbierto = false;
            RestablecerZoom();
        }

        private async Task AgregarFotosAsync()
        {
            if (IsBusy || !ValidarEnLinea())
                return;

            try
            {
                IEnumerable<FileResult> seleccion =
                    await FilePicker.Default.PickMultipleAsync(
                        new PickOptions
                        {
                            PickerTitle =
                                "Seleccione fotografías de la inspección",
                            FileTypes = FilePickerFileType.Images
                        }) ?? [];

                int primerIndiceAgregado = -1;

                foreach (FileResult archivo in seleccion)
                {
                    if (Fotos.Count >= 40)
                    {
                        await MostrarAlertaAsync(
                            "Límite alcanzado",
                            "Puede registrar hasta 40 fotografías por inspección.");
                        break;
                    }

                    string ruta = await CopiarTemporalAsync(archivo);

                    if (Fotos.Any(item => item.RutaLocal == ruta))
                    {
                        EliminarTemporalSeguro(ruta);
                        continue;
                    }

                    Fotos.Add(new InspeccionFotoLocal
                    {
                        RutaLocal = ruta,
                        NombreArchivo = archivo.FileName,
                        TipoContenido =
                            archivo.ContentType ?? "image/jpeg",
                        FechaIdentificacionCampo = DateTime.Today,
                        TipoFotografia = "EVIDENCIA"
                    });

                    if (primerIndiceAgregado < 0)
                        primerIndiceAgregado = Fotos.Count - 1;
                }

                if (primerIndiceAgregado >= 0)
                    EstablecerIndiceFotoActual(primerIndiceAgregado);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task TomarFotoAsync()
        {
            if (IsBusy ||
                !ValidarEnLinea() ||
                !MediaPicker.Default.IsCaptureSupported)
            {
                return;
            }

            if (Fotos.Count >= 40)
            {
                await MostrarAlertaAsync(
                    "Límite alcanzado",
                    "Puede registrar hasta 40 fotografías por inspección.");
                return;
            }

            try
            {
                FileResult? archivo =
                    await MediaPicker.Default.CapturePhotoAsync();

                if (archivo == null)
                    return;

                string ruta = await CopiarTemporalAsync(archivo);

                Fotos.Add(new InspeccionFotoLocal
                {
                    RutaLocal = ruta,
                    NombreArchivo = archivo.FileName,
                    TipoContenido =
                        archivo.ContentType ?? "image/jpeg",
                    FechaIdentificacionCampo = DateTime.Today,
                    TipoFotografia = "EVIDENCIA"
                });

                EstablecerIndiceFotoActual(Fotos.Count - 1);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private void QuitarFoto(InspeccionFotoLocal? foto)
        {
            if (foto == null || IsBusy)
                return;

            int indiceEliminado = Fotos.IndexOf(foto);
            if (indiceEliminado < 0)
                return;

            int nuevoIndice = indiceFotoActual;

            if (indiceEliminado < indiceFotoActual)
            {
                nuevoIndice--;
            }
            else if (indiceEliminado == indiceFotoActual &&
                     indiceFotoActual == Fotos.Count - 1)
            {
                nuevoIndice--;
            }

            Fotos.RemoveAt(indiceEliminado);
            EliminarTemporalSeguro(foto.RutaLocal);

            if (!TieneFotos)
                CerrarVisor();

            EstablecerIndiceFotoActual(nuevoIndice, true);
        }

        private void IrFotoAnterior()
        {
            if (!TieneFotoAnterior)
                return;

            EstablecerIndiceFotoActual(indiceFotoActual - 1);
        }

        private void IrFotoSiguiente()
        {
            if (!TieneFotoSiguiente)
                return;

            EstablecerIndiceFotoActual(indiceFotoActual + 1);
        }

        private void AbrirVisor()
        {
            if (!TieneFotos || IsBusy)
                return;

            RestablecerZoom();
            EsVisorAbierto = true;
        }

        private void AumentarZoom()
        {
            EscalaVisor += PasoZoom;
        }

        private void ReducirZoom()
        {
            EscalaVisor -= PasoZoom;
        }

        private void RestablecerZoom()
        {
            EscalaVisor = ZoomMinimo;
        }

        private void EstablecerIndiceFotoActual(
            int nuevoIndice,
            bool forzarNotificacion = false)
        {
            int indiceNormalizado = Fotos.Count == 0
                ? -1
                : Math.Clamp(nuevoIndice, 0, Fotos.Count - 1);

            if (!forzarNotificacion &&
                indiceFotoActual == indiceNormalizado)
            {
                return;
            }

            indiceFotoActual = indiceNormalizado;
            RestablecerZoom();

            OnPropertyChanged(nameof(FotoActual));
            OnPropertyChanged(nameof(ImagenTarjetaActual));
            OnPropertyChanged(nameof(ImagenVisorActual));
            OnPropertyChanged(nameof(ContadorFotoActual));
            OnPropertyChanged(nameof(TieneFotoAnterior));
            OnPropertyChanged(nameof(TieneFotoSiguiente));
            OnPropertyChanged(nameof(TipoFotografiaSeleccionada));
            OnPropertyChanged(nameof(TipoFotografiaActualTexto));
            OnPropertyChanged(nameof(EstadoNavegacionFoto));
            OnPropertyChanged(nameof(EnfoqueFotoActual));

            ActualizarComandos();
        }

        private void AlCambiarColeccionFotos(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (InspeccionFotoLocal foto in e.OldItems)
                    foto.PropertyChanged -= AlCambiarFotoActual;
            }

            if (e.NewItems != null)
            {
                foreach (InspeccionFotoLocal foto in e.NewItems)
                    foto.PropertyChanged += AlCambiarFotoActual;
            }

            int indiceSugerido = Fotos.Count == 0
                ? -1
                : indiceFotoActual < 0
                    ? 0
                    : Math.Min(indiceFotoActual, Fotos.Count - 1);

            OnPropertyChanged(nameof(TieneFotos));
            OnPropertyChanged(nameof(TieneVariasFotos));
            OnPropertyChanged(nameof(SinFotos));
            OnPropertyChanged(nameof(ResumenFotos));

            if (!TieneFotos)
                EsVisorAbierto = false;

            EstablecerIndiceFotoActual(
                indiceSugerido,
                forzarNotificacion: true);
        }

        private void AlCambiarFotoActual(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, FotoActual))
                return;

            if (e.PropertyName ==
                nameof(InspeccionFotoLocal.TipoFotografia))
            {
                OnPropertyChanged(nameof(TipoFotografiaSeleccionada));
                OnPropertyChanged(nameof(TipoFotografiaActualTexto));
                OnPropertyChanged(nameof(EnfoqueFotoActual));
            }
        }

        private async Task GuardarAsync()
        {
            if (IsBusy || !TieneFotos || !ValidarEnLinea())
                return;

            bool confirmar = await ConfirmarAsync(
                "Guardar inspección",
                "Las fotografías se conservarán como evidencia. Después podrá seleccionar cuáles analizar, enviar o descartar lógicamente.");

            if (!confirmar)
                return;

            CerrarVisor();
            IsBusy = true;
            MensajeEstado =
                "Guardando fotografías y fechas de campo...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 detalle =
                    await InspeccionApi.CrearAsync(
                        Fotos.ToList(),
                        CodigoTerreno,
                        Observacion);

                foreach (InspeccionFotoLocal foto in Fotos)
                {
                    foto.PropertyChanged -= AlCambiarFotoActual;
                    EliminarTemporalSeguro(foto.RutaLocal);
                }

                Fotos.Clear();
                Observacion = string.Empty;

                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaResultado(
                        detalle.InspeccionId,
                        DiagnosticoIARoutes.ModoMisInspecciones));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task BuscarInspeccionesAsync()
        {
            if (!await ValidarFechasFiltroAsync())
                return;

            await CargarBandejaAsync(reiniciar: true);

            // Después de aplicar la búsqueda se libera el espacio vertical
            // para que la lista vuelva a ser el contenido principal.
            FiltrosExpandidos = false;
        }

        private async Task LimpiarFiltrosAsync()
        {
            BuscarInspeccion = string.Empty;
            PropietarioFiltro = string.Empty;
            DepartamentoFiltro = string.Empty;
            UsarFechaDesde = false;
            UsarFechaHasta = false;
            FechaDesde = FechaDesdePredeterminada;
            FechaHasta = FechaMaximaFiltro;
            TipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            EstadoFiltroSeleccionado = EstadosInspeccionFiltro[0];
            FiltrosExpandidos = false;

            await CargarBandejaAsync(reiniciar: true);
        }

        private void AlternarFiltros()
        {
            if (IsBusy || !EsModoListado)
                return;

            FiltrosExpandidos = !FiltrosExpandidos;
        }

        private async Task<bool> ValidarFechasFiltroAsync()
        {
            DateTime hoy = FechaMaximaPermitida;

            if (UsarFechaDesde && FechaDesde > hoy)
            {
                await MostrarAlertaAsync(
                    "Fecha inicial no válida",
                    "La fecha inicial no puede estar en el futuro.");
                return false;
            }

            if (UsarFechaHasta && FechaHasta > hoy)
            {
                await MostrarAlertaAsync(
                    "Fecha final no válida",
                    "La fecha final no puede estar en el futuro.");
                return false;
            }

            if (UsarFechaDesde &&
                UsarFechaHasta &&
                FechaDesde > FechaHasta)
            {
                await MostrarAlertaAsync(
                    "Rango de fechas no válido",
                    "La fecha inicial debe ser anterior o igual a la fecha final.");
                return false;
            }

            return true;
        }

        private static DateTime LimitarFechaFiltro(DateTime valor)
        {
            DateTime fecha = valor.Date;

            if (fecha < FechaMinimaPermitida)
                return FechaMinimaPermitida;

            if (fecha > FechaMaximaPermitida)
                return FechaMaximaPermitida;

            return fecha;
        }

        private void NotificarEstadoFiltros()
        {
            OnPropertyChanged(nameof(CantidadFiltrosActivos));
            OnPropertyChanged(nameof(TieneFiltrosActivos));
            OnPropertyChanged(nameof(TextoBotonFiltros));
            OnPropertyChanged(nameof(ResumenFiltrosActivos));
        }

        private async Task CargarBandejaAsync(bool reiniciar)
        {
            if (!EsModoListado || !ValidarEnLinea(false))
                return;

            if (!reiniciar && !HayMas)
                return;

            if (!await cargaBandejaLock.WaitAsync(0))
                return;

            bool cargaInicial = reiniciar;

            try
            {
                if (cargaInicial)
                {
                    IsBusy = true;
                    MensajeEstado = "Buscando inspecciones...";
                    cargaInicialCompletada = false;
                    siguienteFechaUtc = null;
                    siguienteId = null;
                    HayMas = false;
                }
                else
                {
                    EstaCargandoMas = true;
                }

                ActualizarComandos();

                var filtro = new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = modoVista,
                    Buscar = BuscarInspeccion.Trim(),
                    Propietario = PropietarioFiltro.Trim(),
                    Departamento = DepartamentoFiltro.Trim(),
                    TipoFotografia =
                        TipoFotografiaFiltroSeleccionado.Codigo,
                    Estado = EstadoFiltroSeleccionado.Codigo,
                    FechaDesde = UsarFechaDesde
                        ? FechaDesde.Date
                        : null,
                    FechaHasta = UsarFechaHasta
                        ? FechaHasta.Date
                        : null,
                    DesfaseHorarioMinutos =
                        (int)DateTimeOffset.Now.Offset.TotalMinutes,
                    UltimaFechaUtc = cargaInicial
                        ? null
                        : siguienteFechaUtc,
                    UltimoId = cargaInicial
                        ? null
                        : siguienteId,
                    TamanoPagina = 20
                };

                InspeccionFitosanitariaBandejaPaginaV2 pagina =
                    await bandejaApi.ObtenerAsync(filtro);

                if (cargaInicial)
                    Solicitudes.Clear();

                HashSet<int> existentes = Solicitudes
                    .Select(item => item.InspeccionId)
                    .ToHashSet();

                foreach (
                    InspeccionFitosanitariaBandejaItemV2 item in
                        pagina.Items ?? [])
                {
                    if (existentes.Add(item.InspeccionId))
                        Solicitudes.Add(item);
                }

                siguienteFechaUtc = pagina.SiguienteFechaUtc;
                siguienteId = pagina.SiguienteId;
                HayMas = pagina.HayMas;
                cargaInicialCompletada = true;

                OnPropertyChanged(nameof(TieneSolicitudes));
                OnPropertyChanged(nameof(TextoResultadoListado));
                OnPropertyChanged(nameof(SinSolicitudes));
                OnPropertyChanged(nameof(MostrarCargarMas));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                if (cargaInicial)
                {
                    MensajeEstado = string.Empty;
                    IsBusy = false;
                }
                else
                {
                    EstaCargandoMas = false;
                }

                OnPropertyChanged(nameof(SinSolicitudes));
                OnPropertyChanged(nameof(PuedeCargarMas));
                OnPropertyChanged(nameof(MostrarCargarMas));
                ActualizarComandos();
                cargaBandejaLock.Release();
            }
        }

        private async Task AbrirResultadoAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || IsBusy || EstaCargandoMas)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    modoVista));
        }

        private void QuitarTerreno()
        {
            TerrenoSeleccionado = null;
            CodigoTerreno = string.Empty;
        }

        private static async Task<string> CopiarTemporalAsync(
            FileResult archivo)
        {
            string extension = Path.GetExtension(archivo.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string carpeta = Path.Combine(
                FileSystem.CacheDirectory,
                "inspecciones-fitosanitarias");

            Directory.CreateDirectory(carpeta);

            string destino = Path.Combine(
                carpeta,
                $"{Guid.NewGuid():N}{extension}");

            await using Stream origen =
                await archivo.OpenReadAsync();

            await using FileStream salida = new(
                destino,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await origen.CopyToAsync(salida);
            return destino;
        }

        private static void EliminarTemporalSeguro(string? ruta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ruta) &&
                    File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch
            {
            }
        }

        private void ActualizarComandos()
        {
            AgregarFotoCommand.ChangeCanExecute();
            TomarFotoCommand.ChangeCanExecute();
            QuitarFotoCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
            ActualizarComandosListado();
            BuscarTerrenoCommand.ChangeCanExecute();
            QuitarTerrenoCommand.ChangeCanExecute();
            FotoAnteriorCommand.ChangeCanExecute();
            FotoSiguienteCommand.ChangeCanExecute();
            AbrirVisorCommand.ChangeCanExecute();
            ActualizarComandosVisor();
        }

        private void ActualizarComandosListado()
        {
            ActualizarCommand.ChangeCanExecute();
            BuscarInspeccionesCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirResultadoCommand.ChangeCanExecute();
        }

        private void ActualizarComandosVisor()
        {
            CerrarVisorCommand.ChangeCanExecute();
            AumentarZoomCommand.ChangeCanExecute();
            ReducirZoomCommand.ChangeCanExecute();
            RestablecerZoomCommand.ChangeCanExecute();
        }
    }
}
