using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAViewModel : GlobalService
    {
        private const int MaximoFotos = 4;

        private readonly DiagnosticoIAApiService api =
            DiagnosticoIAApiService.Instance;

        private bool inicializado;
        private string codigoTerreno = string.Empty;
        private string observacionUsuario = string.Empty;
        private DiagnosticoIAItem? resultadoReciente;
        private DiagnosticoIAItem? pendienteSeleccionado;
        private string decisionSeleccionada = "CONFIRMAR";
        private string diagnosticoFinal = string.Empty;
        private string observacionesClasificador = string.Empty;
        private string retroalimentacionParaIA = string.Empty;
        private string diagnosticoPropuestoRevision = string.Empty;

        public DiagnosticoIAViewModel()
        {
            Fotos.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(TieneFotos));
                OnPropertyChanged(nameof(PuedeAgregarMasFotos));
                OnPropertyChanged(nameof(ResumenFotos));
                ActualizarComandos();
            };

            AgregarFotoCommand = new Command(
                async () => await AgregarFotoGaleriaAsync(),
                () => !IsBusy && PuedeAgregarMasFotos);

            TomarFotoCommand = new Command(
                async () => await TomarFotoAsync(),
                () => !IsBusy && PuedeAgregarMasFotos);

            QuitarFotoCommand =
                new Command<FotoDiagnosticoSeleccionada>(
                    QuitarFoto,
                    foto => foto != null && !IsBusy);

            AnalizarCommand = new Command(
                async () => await AnalizarAsync(),
                () =>
                    !IsBusy &&
                    CanAdd &&
                    TieneFotos);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy);

            SeleccionarPendienteCommand =
                new Command<DiagnosticoIAItem>(
                    SeleccionarPendiente,
                    item => item != null && !IsBusy);

            SolicitarSegundaRevisionCommand = new Command(
                async () => await SolicitarSegundaRevisionAsync(),
                () =>
                    !IsBusy &&
                    CanClassify &&
                    PendienteSeleccionado?.PuedeSolicitarOtraRevision == true &&
                    RetroalimentacionParaIA.Trim().Length >= 8);

            ClasificarCommand = new Command(
                async () => await ClasificarAsync(),
                () =>
                    !IsBusy &&
                    CanClassify &&
                    PendienteSeleccionado != null);
        }

        public ObservableCollection<FotoDiagnosticoSeleccionada>
            Fotos { get; } = [];

        public ObservableCollection<DiagnosticoIAItem>
            MisDiagnosticos { get; } = [];

        public ObservableCollection<DiagnosticoIAItem>
            Pendientes { get; } = [];

        public IReadOnlyList<string> Decisiones { get; } =
        [
            "CONFIRMAR",
            "CORREGIR",
            "NO_CONCLUYENTE",
            "IMAGEN_RECHAZADA"
        ];

        public Command AgregarFotoCommand { get; }
        public Command TomarFotoCommand { get; }
        public Command<FotoDiagnosticoSeleccionada> QuitarFotoCommand { get; }
        public Command AnalizarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command<DiagnosticoIAItem> SeleccionarPendienteCommand { get; }
        public Command SolicitarSegundaRevisionCommand { get; }
        public Command ClasificarCommand { get; }

        public bool CanClassify { get; private set; }

        public bool TieneFotos => Fotos.Count > 0;

        public bool PuedeAgregarMasFotos =>
            Fotos.Count < MaximoFotos;

        public string ResumenFotos =>
            $"{Fotos.Count} de {MaximoFotos} fotografías seleccionadas";

        public bool TieneResultadoReciente =>
            ResultadoReciente != null;

        public bool TieneMisDiagnosticos =>
            MisDiagnosticos.Count > 0;

        public bool MostrarSinMisDiagnosticos =>
            !TieneMisDiagnosticos;

        public bool TienePendientes =>
            Pendientes.Count > 0;

        public bool MostrarSinPendientes =>
            !TienePendientes;

        public bool TienePendienteSeleccionado =>
            PendienteSeleccionado != null;

        public bool TieneRevisionSeleccionada =>
            PendienteSeleccionado?.TieneRevisionIA == true;

        public bool TieneRevisionCompletadaSeleccionada =>
            PendienteSeleccionado?.TieneRevisionCompletada == true;

        public bool PuedeSolicitarSegundaRevision =>
            PendienteSeleccionado?.PuedeSolicitarOtraRevision == true;

        public string ResumenRevisionesSeleccionado =>
            PendienteSeleccionado?.ResumenRevisiones ??
            "0 segundas revisiones realizadas";

        public bool RequiereDiagnosticoCorregido =>
            DecisionSeleccionada == "CORREGIR";

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

        public string ObservacionUsuario
        {
            get => observacionUsuario;
            set
            {
                string nuevo = value ?? string.Empty;
                if (observacionUsuario == nuevo)
                    return;

                observacionUsuario = nuevo;
                OnPropertyChanged();
            }
        }

        public DiagnosticoIAItem? ResultadoReciente
        {
            get => resultadoReciente;
            private set
            {
                if (ReferenceEquals(resultadoReciente, value))
                    return;

                resultadoReciente = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneResultadoReciente));
            }
        }

        public DiagnosticoIAItem? PendienteSeleccionado
        {
            get => pendienteSeleccionado;
            private set
            {
                if (ReferenceEquals(pendienteSeleccionado, value))
                    return;

                pendienteSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TienePendienteSeleccionado));
                OnPropertyChanged(nameof(TieneRevisionSeleccionada));
                OnPropertyChanged(nameof(TieneRevisionCompletadaSeleccionada));
                OnPropertyChanged(nameof(PuedeSolicitarSegundaRevision));
                OnPropertyChanged(nameof(ResumenRevisionesSeleccionado));
                SolicitarSegundaRevisionCommand.ChangeCanExecute();
                ClasificarCommand.ChangeCanExecute();
            }
        }

        public string DecisionSeleccionada
        {
            get => decisionSeleccionada;
            set
            {
                string nueva = value ?? "CONFIRMAR";
                if (decisionSeleccionada == nueva)
                    return;

                decisionSeleccionada = nueva;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RequiereDiagnosticoCorregido));

                if (!RequiereDiagnosticoCorregido)
                    DiagnosticoFinal = string.Empty;
            }
        }

        public string DiagnosticoFinal
        {
            get => diagnosticoFinal;
            set
            {
                string nuevo = value ?? string.Empty;
                if (diagnosticoFinal == nuevo)
                    return;

                diagnosticoFinal = nuevo;
                OnPropertyChanged();
            }
        }

        public string ObservacionesClasificador
        {
            get => observacionesClasificador;
            set
            {
                string nuevo = value ?? string.Empty;
                if (observacionesClasificador == nuevo)
                    return;

                observacionesClasificador = nuevo;
                OnPropertyChanged();
            }
        }

        public string RetroalimentacionParaIA
        {
            get => retroalimentacionParaIA;
            set
            {
                string nuevo = value ?? string.Empty;
                if (retroalimentacionParaIA == nuevo)
                    return;

                retroalimentacionParaIA = nuevo;
                OnPropertyChanged();
                SolicitarSegundaRevisionCommand.ChangeCanExecute();
            }
        }

        public string DiagnosticoPropuestoRevision
        {
            get => diagnosticoPropuestoRevision;
            set
            {
                string nuevo = value ?? string.Empty;
                if (diagnosticoPropuestoRevision == nuevo)
                    return;

                diagnosticoPropuestoRevision = nuevo;
                OnPropertyChanged();
            }
        }

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (!inicializado)
                inicializado = true;

            await ActualizarAsync();
        }

        private void ActualizarPermisos()
        {
            UserPermissionDTO permiso =
                PermissionService.Instance.Get(
                    DiagnosticoIARoutes.Interfaz);

            CanView = permiso.leer;
            CanAdd = permiso.agregar;
            CanEdit = permiso.actualizar;
            CanDelete = permiso.eliminar;
            CanClassify = permiso.actualizar;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanClassify));
            ActualizarComandos();
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarEnLinea(mostrarMensaje: false))
                return;

            IsBusy = true;

            try
            {
                ActualizarPermisos();

                if (CanView)
                {
                    DiagnosticoIAPaginaRespuesta propios =
                        await api.ObtenerMisDiagnosticosAsync();

                    ReemplazarColeccion(
                        MisDiagnosticos,
                        propios.Data);

                    OnPropertyChanged(nameof(TieneMisDiagnosticos));
                    OnPropertyChanged(nameof(MostrarSinMisDiagnosticos));
                }

                if (CanClassify)
                {
                    DiagnosticoIAPaginaRespuesta pendientes =
                        await api.ObtenerPendientesAsync();

                    ReemplazarColeccion(
                        Pendientes,
                        pendientes.Data);

                    OnPropertyChanged(nameof(TienePendientes));
                    OnPropertyChanged(nameof(MostrarSinPendientes));

                    if (PendienteSeleccionado != null)
                    {
                        PendienteSeleccionado = Pendientes
                            .FirstOrDefault(item =>
                                item.DiagnosticoIAId ==
                                PendienteSeleccionado.DiagnosticoIAId);
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible actualizar",
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task AgregarFotoGaleriaAsync()
        {
            if (!ValidarEnLinea())
                return;

            try
            {
                FileResult? resultado =
                    await MediaPicker.Default.PickPhotoAsync(
                        new MediaPickerOptions
                        {
                            Title = "Seleccione una fotografía del cafeto"
                        });

                if (resultado != null)
                    await AgregarResultadoAsync(resultado);
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible seleccionar la fotografía",
                    ex.Message);
            }
        }

        private async Task TomarFotoAsync()
        {
            if (!ValidarEnLinea())
                return;

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await MostrarAlertaAsync(
                    "Cámara no disponible",
                    "Este dispositivo no permite tomar fotografías desde la aplicación.");
                return;
            }

            try
            {
                FileResult? resultado =
                    await MediaPicker.Default.CapturePhotoAsync(
                        new MediaPickerOptions
                        {
                            Title = "Fotografía para diagnóstico"
                        });

                if (resultado != null)
                    await AgregarResultadoAsync(resultado);
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible tomar la fotografía",
                    ex.Message);
            }
        }

        private async Task AgregarResultadoAsync(
            FileResult resultado)
        {
            if (!PuedeAgregarMasFotos)
                return;

            string extension = Path.GetExtension(resultado.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string rutaDestino = Path.Combine(
                FileSystem.CacheDirectory,
                $"diagnostico-ia-{Guid.NewGuid():N}{extension}");

            await using Stream origen =
                await resultado.OpenReadAsync();

            await using var destino = new FileStream(
                rutaDestino,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await origen.CopyToAsync(destino);

            Fotos.Add(
                new FotoDiagnosticoSeleccionada
                {
                    RutaLocal = rutaDestino,
                    NombreArchivo =
                        string.IsNullOrWhiteSpace(resultado.FileName)
                            ? Path.GetFileName(rutaDestino)
                            : resultado.FileName,
                    TipoContenido =
                        string.IsNullOrWhiteSpace(resultado.ContentType)
                            ? "image/jpeg"
                            : resultado.ContentType
                });
        }

        private void QuitarFoto(
            FotoDiagnosticoSeleccionada? foto)
        {
            if (foto == null || !Fotos.Remove(foto))
                return;

            EliminarArchivoSeguro(foto.RutaLocal);
        }

        private async Task AnalizarAsync()
        {
            if (IsBusy || !CanAdd || !TieneFotos)
                return;

            if (!ValidarEnLinea())
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                DiagnosticoIAItem resultado =
                    await api.AnalizarAsync(
                        Fotos.ToList(),
                        CodigoTerreno,
                        ObservacionUsuario);

                ResultadoReciente = resultado;
                MisDiagnosticos.Insert(0, resultado);
                OnPropertyChanged(nameof(TieneMisDiagnosticos));
                OnPropertyChanged(nameof(MostrarSinMisDiagnosticos));

                LimpiarFormulario();

                await MostrarAlertaAsync(
                    "Análisis preliminar completado",
                    "Gemini emitió un veredicto preliminar. El caso quedó pendiente de confirmación por una persona autorizada.");

                if (CanClassify)
                    await CargarPendientesSinBloqueoAsync();
            }
            catch (DiagnosticoIAApiException ex)
                when (ex.StatusCode == 429)
            {
                await MostrarAlertaAsync(
                    "Límite gratuito alcanzado",
                    "Gemini no puede recibir otra solicitud en este momento. Las fotografías ya quedaron guardadas en el servidor.");
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible completar el análisis",
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private void SeleccionarPendiente(
            DiagnosticoIAItem? item)
        {
            if (item == null)
                return;

            PendienteSeleccionado = item;
            DecisionSeleccionada = "CONFIRMAR";
            DiagnosticoFinal = string.Empty;
            ObservacionesClasificador = string.Empty;
            RetroalimentacionParaIA = string.Empty;
            DiagnosticoPropuestoRevision = string.Empty;
        }

        private async Task SolicitarSegundaRevisionAsync()
        {
            if (IsBusy ||
                !CanClassify ||
                PendienteSeleccionado == null ||
                !PendienteSeleccionado.PuedeSolicitarOtraRevision)
            {
                return;
            }

            if (!ValidarEnLinea())
                return;

            if (RetroalimentacionParaIA.Trim().Length < 8)
            {
                await MostrarAlertaAsync(
                    "Retroalimentación requerida",
                    "Explique qué observó o por qué duda del veredicto antes de pedir otra revisión a Gemini.");
                return;
            }

            bool confirmar = await Shell.Current.DisplayAlert(
                "Solicitar segunda revisión",
                "Gemini volverá a examinar las mismas fotografías y comparará su primer veredicto con su observación. La respuesta seguirá siendo preliminar y no guardará una clasificación final.",
                "Revisar nuevamente",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                int id = PendienteSeleccionado.DiagnosticoIAId;

                DiagnosticoIAItem resultado =
                    await api.SolicitarSegundaRevisionAsync(
                        id,
                        RetroalimentacionParaIA,
                        DiagnosticoPropuestoRevision);

                ReemplazarDiagnosticoEnColecciones(
                    resultado);

                PendienteSeleccionado = resultado;
                RetroalimentacionParaIA = string.Empty;
                DiagnosticoPropuestoRevision = string.Empty;

                await MostrarAlertaAsync(
                    "Segunda revisión completada",
                    "Gemini revisó nuevamente las fotografías y contrastó su veredicto con el criterio indicado. La decisión humana continúa pendiente.");
            }
            catch (DiagnosticoIAApiException ex)
                when (ex.StatusCode == 429)
            {
                await MostrarAlertaAsync(
                    "Límite gratuito alcanzado",
                    "Gemini no puede realizar otra revisión en este momento. El diagnóstico original y la retroalimentación permanecen guardados.");
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible completar la segunda revisión",
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task ClasificarAsync()
        {
            if (IsBusy ||
                !CanClassify ||
                PendienteSeleccionado == null)
            {
                return;
            }

            if (!ValidarEnLinea())
                return;

            if (RequiereDiagnosticoCorregido &&
                string.IsNullOrWhiteSpace(DiagnosticoFinal))
            {
                await MostrarAlertaAsync(
                    "Diagnóstico requerido",
                    "Indique cuál es el diagnóstico correcto antes de guardar la corrección.");
                return;
            }

            bool confirmar = await Shell.Current.DisplayAlert(
                "Guardar clasificación",
                "¿Desea registrar esta decisión como validación humana?",
                "Guardar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                int id = PendienteSeleccionado.DiagnosticoIAId;

                DiagnosticoIAItem resultado =
                    await api.ClasificarAsync(
                        id,
                        DecisionSeleccionada,
                        DiagnosticoFinal,
                        ObservacionesClasificador);

                DiagnosticoIAItem? pendiente = Pendientes
                    .FirstOrDefault(item =>
                        item.DiagnosticoIAId == id);

                if (pendiente != null)
                    Pendientes.Remove(pendiente);

                DiagnosticoIAItem? propio = MisDiagnosticos
                    .FirstOrDefault(item =>
                        item.DiagnosticoIAId == id);

                if (propio != null)
                {
                    int indice = MisDiagnosticos.IndexOf(propio);
                    MisDiagnosticos[indice] = resultado;
                }

                PendienteSeleccionado = null;
                OnPropertyChanged(nameof(TienePendientes));
                OnPropertyChanged(nameof(MostrarSinPendientes));

                await MostrarAlertaAsync(
                    "Clasificación registrada",
                    "La validación humana fue guardada correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(
                    "No fue posible guardar la clasificación",
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private void ReemplazarDiagnosticoEnColecciones(
            DiagnosticoIAItem resultado)
        {
            DiagnosticoIAItem? pendiente = Pendientes
                .FirstOrDefault(item =>
                    item.DiagnosticoIAId ==
                    resultado.DiagnosticoIAId);

            if (pendiente != null)
            {
                int indice = Pendientes.IndexOf(pendiente);
                Pendientes[indice] = resultado;
            }

            DiagnosticoIAItem? propio = MisDiagnosticos
                .FirstOrDefault(item =>
                    item.DiagnosticoIAId ==
                    resultado.DiagnosticoIAId);

            if (propio != null)
            {
                int indice = MisDiagnosticos.IndexOf(propio);
                MisDiagnosticos[indice] = resultado;
            }

            OnPropertyChanged(nameof(TienePendientes));
            OnPropertyChanged(nameof(MostrarSinPendientes));
            OnPropertyChanged(nameof(TieneMisDiagnosticos));
            OnPropertyChanged(nameof(MostrarSinMisDiagnosticos));
        }

        private async Task CargarPendientesSinBloqueoAsync()
        {
            DiagnosticoIAPaginaRespuesta pendientes =
                await api.ObtenerPendientesAsync();

            ReemplazarColeccion(
                Pendientes,
                pendientes.Data);

            OnPropertyChanged(nameof(TienePendientes));
            OnPropertyChanged(nameof(MostrarSinPendientes));
        }

        private void LimpiarFormulario()
        {
            foreach (FotoDiagnosticoSeleccionada foto in Fotos.ToList())
                EliminarArchivoSeguro(foto.RutaLocal);

            Fotos.Clear();
            CodigoTerreno = string.Empty;
            ObservacionUsuario = string.Empty;
        }

        private bool ValidarEnLinea(
            bool mostrarMensaje = true)
        {
            NetworkAccess accesoRed =
                Connectivity.Current.NetworkAccess;

#if WINDOWS
            bool redDisponible =
                accesoRed != NetworkAccess.None;
#else
            bool redDisponible =
                accesoRed == NetworkAccess.Internet;
#endif

            bool enLinea =
                ModoSesionService.EsEnLinea &&
                redDisponible;

            if (!enLinea && mostrarMensaje)
            {
                _ = MostrarAlertaAsync(
                    "Conexión requerida",
                    "El diagnóstico con Gemini está disponible únicamente en modo En línea.");
            }

            return enLinea;
        }

        private void ActualizarComandos()
        {
            AgregarFotoCommand.ChangeCanExecute();
            TomarFotoCommand.ChangeCanExecute();
            QuitarFotoCommand.ChangeCanExecute();
            AnalizarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            SeleccionarPendienteCommand.ChangeCanExecute();
            SolicitarSegundaRevisionCommand.ChangeCanExecute();
            ClasificarCommand.ChangeCanExecute();
        }

        private static void ReemplazarColeccion<T>(
            ObservableCollection<T> destino,
            IEnumerable<T> elementos)
        {
            destino.Clear();

            foreach (T elemento in elementos)
                destino.Add(elemento);
        }

        private static void EliminarArchivoSeguro(
            string? ruta)
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

        private static Task MostrarAlertaAsync(
            string titulo,
            string mensaje) =>
            Shell.Current.DisplayAlert(
                titulo,
                mensaje,
                "Aceptar");
    }
}
