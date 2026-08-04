using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra en una sola pantalla el límite histórico de revisiones y el
    /// proveedor multimodal utilizado por las inspecciones por fotografía.
    /// </summary>
    public sealed class DiagnosticoIAConfiguracionViewModel : GlobalService
    {
        private readonly DiagnosticoIAConfiguracionApiService apiLimites = new();
        private readonly InspeccionFitosanitariaApiService apiProveedor =
            InspeccionFitosanitariaApiService.Instance;

        private bool inicializado;
        private bool revisionesIlimitadas;
        private string maximoRevisionesTexto = "2";
        private string resumenActual = string.Empty;
        private string ultimaModificacion = string.Empty;
        private string mensajeEstado = string.Empty;
        private ProveedorIAConfiguracionV2 proveedor = new();
        private string resultadoPrueba = string.Empty;

        public DiagnosticoIAConfiguracionViewModel()
        {
            Protocolos.Add("GEMINI_NATIVO");
            Protocolos.Add("OPENAI_COMPATIBLE");

            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy && CanView);

            GuardarLimitesCommand = new Command(
                async () => await GuardarLimitesAsync(),
                () => !IsBusy && CanEdit);

            GuardarProveedorCommand = new Command(
                async () => await GuardarProveedorAsync(),
                () => !IsBusy && CanEdit);

            ProbarConexionCommand = new Command(
                async () => await ProbarConexionAsync(),
                () => !IsBusy && CanEdit);

            UsarGeminiCommand = new Command(
                AplicarPresetGemini,
                () => !IsBusy && CanEdit);

            UsarOpenRouterCommand = new Command(
                AplicarPresetOpenRouter,
                () => !IsBusy && CanEdit);
        }

        public ObservableCollection<DiagnosticoIAConfiguracionHistorialItem>
            Historial { get; } = [];

        public ObservableCollection<string> Protocolos { get; } = [];

        public Command RegresarCommand { get; }
        public Command ActualizarCommand { get; }
        public Command GuardarLimitesCommand { get; }
        public Command GuardarProveedorCommand { get; }
        public Command ProbarConexionCommand { get; }
        public Command UsarGeminiCommand { get; }
        public Command UsarOpenRouterCommand { get; }

        public bool RevisionesIlimitadas
        {
            get => revisionesIlimitadas;
            set
            {
                if (revisionesIlimitadas == value)
                    return;

                revisionesIlimitadas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarCampoMaximo));
                OnPropertyChanged(nameof(AyudaLimite));
            }
        }

        public bool MostrarCampoMaximo => !RevisionesIlimitadas;

        public string MaximoRevisionesTexto
        {
            get => maximoRevisionesTexto;
            set
            {
                string nuevo = value ?? string.Empty;
                if (maximoRevisionesTexto == nuevo)
                    return;

                maximoRevisionesTexto = nuevo;
                OnPropertyChanged();
            }
        }

        public string ResumenActual
        {
            get => resumenActual;
            private set
            {
                if (resumenActual == value)
                    return;

                resumenActual = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string UltimaModificacion
        {
            get => ultimaModificacion;
            private set
            {
                if (ultimaModificacion == value)
                    return;

                ultimaModificacion = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public ProveedorIAConfiguracionV2 Proveedor
        {
            get => proveedor;
            private set
            {
                if (ReferenceEquals(proveedor, value))
                    return;

                proveedor = value ?? new ProveedorIAConfiguracionV2();
                OnPropertyChanged();
                NotificarProveedor();
            }
        }

        public string NombreProveedor
        {
            get => Proveedor.Proveedor;
            set
            {
                Proveedor.Proveedor = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Protocolo
        {
            get => Proveedor.Protocolo;
            set
            {
                string nuevo = value ?? string.Empty;
                if (Proveedor.Protocolo == nuevo)
                    return;

                Proveedor.Protocolo = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsGeminiNativo));
                OnPropertyChanged(nameof(EsOpenAICompatible));
            }
        }

        public string BaseUrl
        {
            get => Proveedor.BaseUrl;
            set
            {
                Proveedor.BaseUrl = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Endpoint
        {
            get => Proveedor.Endpoint;
            set
            {
                Proveedor.Endpoint = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ApiKey
        {
            get => Proveedor.ApiKey;
            set
            {
                Proveedor.ApiKey = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ModeloPrincipal
        {
            get => Proveedor.ModeloPrincipal;
            set
            {
                Proveedor.ModeloPrincipal = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ModeloRespaldo
        {
            get => Proveedor.ModeloRespaldo;
            set
            {
                Proveedor.ModeloRespaldo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public double TimeoutSegundosValor
        {
            get => Proveedor.TimeoutSegundos;
            set
            {
                int nuevo = (int)Math.Round(value);
                if (Proveedor.TimeoutSegundos == nuevo)
                    return;

                Proveedor.TimeoutSegundos = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeoutSegundosTexto));
            }
        }

        public string TimeoutSegundosTexto =>
            $"Configurado: {Proveedor.TimeoutSegundos} segundos";

        public bool ProveedorActivo
        {
            get => Proveedor.Activo;
            set
            {
                Proveedor.Activo = value;
                OnPropertyChanged();
            }
        }

        public bool EsGeminiNativo =>
            string.Equals(
                Protocolo,
                "GEMINI_NATIVO",
                StringComparison.OrdinalIgnoreCase);

        public bool EsOpenAICompatible =>
            string.Equals(
                Protocolo,
                "OPENAI_COMPATIBLE",
                StringComparison.OrdinalIgnoreCase);

        public string ApiKeyGuardadaTexto => Proveedor.TieneApiKey
            ? $"Clave guardada: {Proveedor.ApiKeyMascara}. Déjela vacía para conservarla."
            : "No existe una clave guardada. Debe ingresar una antes de probar o guardar.";

        public string ResultadoPrueba
        {
            get => resultadoPrueba;
            private set
            {
                if (resultadoPrueba == value)
                    return;

                resultadoPrueba = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneResultadoPrueba));
            }
        }

        public bool TieneResultadoPrueba =>
            !string.IsNullOrWhiteSpace(ResultadoPrueba);

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

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public bool SoloLectura => !CanEdit;
        public bool TieneHistorial => Historial.Count > 0;
        public bool SinHistorial => !TieneHistorial;

        public string AyudaLimite => RevisionesIlimitadas
            ? "Las revisiones adicionales de IA no tendrán límite numérico. Los errores técnicos no cuentan como una revisión completada."
            : "El análisis inicial no cuenta. Solo se contabilizan las revisiones adicionales completadas correctamente.";

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (!CanView)
                return;

            if (inicializado)
            {
                await CargarAsync();
                return;
            }

            inicializado = true;
            await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanEdit = permiso?.actualizar == true;
            CanAdd = permiso?.agregar == true;
            CanDelete = permiso?.eliminar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(SoloLectura));
            ActualizarComandos();
        }

        private async Task CargarAsync()
        {
            if (IsBusy || !CanView)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando configuración de inteligencia artificial...";
            ResultadoPrueba = string.Empty;
            ActualizarComandos();

            try
            {
                DiagnosticoIAConfiguracion limites =
                    await apiLimites.ObtenerAsync();

                AplicarLimites(limites);

                ProveedorIAConfiguracionV2 configuracionProveedor =
                    await apiProveedor.ObtenerProveedorIAAsync();

                configuracionProveedor.ApiKey = string.Empty;
                Proveedor = configuracionProveedor;
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

        private async Task GuardarLimitesAsync()
        {
            if (IsBusy || !CanEdit)
                return;

            if (!int.TryParse(MaximoRevisionesTexto, out int maximo) ||
                maximo is < 1 or > 20)
            {
                await MostrarAlertaAsync(
                    "Configuración de IA",
                    "Indique un valor entre 1 y 20 revisiones.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                "Guardar límite de revisiones",
                RevisionesIlimitadas
                    ? "Las fotografías podrán solicitar revisiones adicionales sin límite numérico."
                    : $"Cada expediente podrá solicitar hasta {maximo} revisiones adicionales.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Guardando límite de revisiones...";
            ActualizarComandos();

            try
            {
                DiagnosticoIAConfiguracion limites =
                    await apiLimites.ActualizarAsync(
                        new DiagnosticoIAConfiguracionActualizarRequest
                        {
                            MaximoRevisionesGemini = maximo,
                            RevisionesIlimitadas = RevisionesIlimitadas
                        });

                AplicarLimites(limites);
                await MostrarAlertaAsync(
                    "Configuración actualizada",
                    "El límite de revisiones quedó guardado.");
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

        private async Task GuardarProveedorAsync()
        {
            if (IsBusy || !CanEdit || !ValidarProveedor())
                return;

            bool confirmar = await ConfirmarAsync(
                "Guardar proveedor de IA",
                $"Se utilizará {NombreProveedor} con el protocolo {Protocolo} y el modelo {ModeloPrincipal}. La clave se almacenará protegida en el servidor.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Guardando proveedor de inteligencia artificial...";
            ActualizarComandos();

            try
            {
                ProveedorIAConfiguracionV2 guardada =
                    await apiProveedor.GuardarProveedorIAAsync(Proveedor);

                guardada.ApiKey = string.Empty;
                Proveedor = guardada;
                ResultadoPrueba = string.Empty;

                await MostrarAlertaAsync(
                    "Proveedor guardado",
                    "La configuración ya está vigente para los próximos análisis por fotografía.");
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

        private async Task ProbarConexionAsync()
        {
            if (IsBusy || !CanEdit || !ValidarProveedor())
                return;

            IsBusy = true;
            MensajeEstado = "Probando conexión con el proveedor...";
            ResultadoPrueba = string.Empty;
            ActualizarComandos();

            try
            {
                ProveedorIAPruebaV2 prueba =
                    await apiProveedor.ProbarProveedorIAAsync(Proveedor);

                ResultadoPrueba = prueba.Exitoso
                    ? $"Conexión correcta · {prueba.Proveedor} · {prueba.Modelo} · {prueba.Milisegundos} ms. {prueba.Mensaje}"
                    : $"La prueba no fue satisfactoria: {prueba.Mensaje}";
            }
            catch (Exception ex)
            {
                ResultadoPrueba = ex.Message;
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private bool ValidarProveedor()
        {
            if (string.IsNullOrWhiteSpace(NombreProveedor) ||
                string.IsNullOrWhiteSpace(Protocolo) ||
                string.IsNullOrWhiteSpace(BaseUrl) ||
                string.IsNullOrWhiteSpace(Endpoint) ||
                string.IsNullOrWhiteSpace(ModeloPrincipal))
            {
                _ = MostrarAlertaAsync(
                    "Datos incompletos",
                    "Proveedor, protocolo, URL, endpoint y modelo principal son obligatorios.");
                return false;
            }

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                 uri.Scheme != Uri.UriSchemeHttp))
            {
                _ = MostrarAlertaAsync(
                    "URL no válida",
                    "Ingrese una URL absoluta HTTP o HTTPS.");
                return false;
            }

            if (Proveedor.TimeoutSegundos is < 30 or > 600)
            {
                _ = MostrarAlertaAsync(
                    "Tiempo no válido",
                    "El tiempo de espera debe estar entre 30 y 600 segundos.");
                return false;
            }

            if (!Proveedor.TieneApiKey && string.IsNullOrWhiteSpace(ApiKey))
            {
                _ = MostrarAlertaAsync(
                    "Clave requerida",
                    "Ingrese la clave del proveedor antes de guardar o probar.");
                return false;
            }

            return true;
        }

        private void AplicarPresetGemini()
        {
            NombreProveedor = "GEMINI";
            Protocolo = "GEMINI_NATIVO";
            BaseUrl = "https://generativelanguage.googleapis.com/";
            Endpoint = "v1beta/models/{model}:generateContent";
            ModeloPrincipal = "gemini-3.6-flash";
            ModeloRespaldo = "gemini-3.5-flash";
            TimeoutSegundosValor = 180;
            ProveedorActivo = true;
            ResultadoPrueba = string.Empty;
        }

        private void AplicarPresetOpenRouter()
        {
            NombreProveedor = "OPENROUTER";
            Protocolo = "OPENAI_COMPATIBLE";
            BaseUrl = "https://openrouter.ai/";
            Endpoint = "api/v1/chat/completions";
            ModeloPrincipal = "google/gemma-3-27b-it:free";
            ModeloRespaldo = string.Empty;
            TimeoutSegundosValor = 180;
            ProveedorActivo = true;
            ResultadoPrueba = string.Empty;
        }

        private void AplicarLimites(DiagnosticoIAConfiguracion configuracion)
        {
            RevisionesIlimitadas = configuracion.RevisionesIlimitadas;
            MaximoRevisionesTexto = Math.Clamp(
                configuracion.MaximoRevisionesGemini,
                1,
                20).ToString();
            ResumenActual = configuracion.Resumen;
            UltimaModificacion =
                $"Última modificación: {configuracion.FechaModificacionTexto} · {configuracion.UsuarioModificacion}";

            Historial.Clear();
            foreach (DiagnosticoIAConfiguracionHistorialItem item
                     in configuracion.Historial)
            {
                Historial.Add(item);
            }

            OnPropertyChanged(nameof(TieneHistorial));
            OnPropertyChanged(nameof(SinHistorial));
            OnPropertyChanged(nameof(AyudaLimite));
        }

        private void NotificarProveedor()
        {
            OnPropertyChanged(nameof(NombreProveedor));
            OnPropertyChanged(nameof(Protocolo));
            OnPropertyChanged(nameof(BaseUrl));
            OnPropertyChanged(nameof(Endpoint));
            OnPropertyChanged(nameof(ApiKey));
            OnPropertyChanged(nameof(ModeloPrincipal));
            OnPropertyChanged(nameof(ModeloRespaldo));
            OnPropertyChanged(nameof(TimeoutSegundosValor));
            OnPropertyChanged(nameof(TimeoutSegundosTexto));
            OnPropertyChanged(nameof(ProveedorActivo));
            OnPropertyChanged(nameof(EsGeminiNativo));
            OnPropertyChanged(nameof(EsOpenAICompatible));
            OnPropertyChanged(nameof(ApiKeyGuardadaTexto));
        }

        private async Task MostrarErrorAsync(Exception ex)
        {
            if (ex is DiagnosticoIAApiException
                {
                    EsSesionInvalidada: true
                } ||
                ex is InspeccionFitosanitariaApiException
                {
                    EsSesionInvalidada: true
                })
            {
                return;
            }

            await MostrarAlertaAsync(
                "Configuración de IA",
                ex.Message);
        }

        private static Task MostrarAlertaAsync(
            string titulo,
            string mensaje)
        {
            if (Shell.Current == null)
                return Task.CompletedTask;

            return Shell.Current.DisplayAlert(titulo, mensaje, "Aceptar");
        }

        private static Task<bool> ConfirmarAsync(
            string titulo,
            string mensaje)
        {
            if (Shell.Current == null)
                return Task.FromResult(false);

            return Shell.Current.DisplayAlert(
                titulo,
                mensaje,
                "Guardar",
                "Cancelar");
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            GuardarLimitesCommand.ChangeCanExecute();
            GuardarProveedorCommand.ChangeCanExecute();
            ProbarConexionCommand.ChangeCanExecute();
            UsarGeminiCommand.ChangeCanExecute();
            UsarOpenRouterCommand.ChangeCanExecute();
        }
    }
}
