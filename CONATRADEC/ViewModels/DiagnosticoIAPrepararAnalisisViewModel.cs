using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Prepara y ejecuta el análisis IA inicial de una selección de fotografías.
    /// La selección es un lote visual, pero cada fotografía conserva su propio
    /// contexto, llamada HTTP, estado y resultado.
    /// </summary>
    public sealed class DiagnosticoIAPrepararAnalisisViewModel :
        DiagnosticoIAViewModelBase
    {
        private readonly InspeccionFitosanitariaApiService inspeccionApi =
            InspeccionFitosanitariaApiService.Instance;

        private int diagnosticoId;
        private int[] fotografiaIds = [];
        private bool parametrosAplicados;
        private bool inicializado;

        public DiagnosticoIAPrepararAnalisisViewModel()
        {
            ProcesarCommand = new Command(
                async () => await ProcesarAsync(),
                () => !IsBusy && PuedeProcesar);
        }

        public ObservableCollection<InspeccionFotoContextoIAItem>
            Fotografias { get; } = [];

        public Command ProcesarCommand { get; }

        public string Titulo => Fotografias.Count == 1
            ? "Preparar análisis IA"
            : $"Preparar análisis IA · {Fotografias.Count} fotografías";

        public string Subtitulo => Fotografias.Count == 1
            ? "Revise la imagen y escriba qué desea que la IA observe específicamente."
            : "Revise cada imagen y escriba qué desea que la IA observe. " +
              "El lote se procesa fotografía por fotografía y un error no detiene las demás.";

        public int CantidadFotografias => Fotografias.Count;
        public int CantidadCompletadas => Fotografias.Count(item => item.Completada);
        public int CantidadConError => Fotografias.Count(item => item.ConError);
        public int CantidadPendientes => Fotografias.Count(item => !item.Completada);

        public string TextoProgreso
        {
            get
            {
                if (Fotografias.Count == 0)
                    return "No hay fotografías pendientes de análisis.";

                var partes = new List<string>
                {
                    CantidadFotografias == 1
                        ? "1 fotografía seleccionada"
                        : $"{CantidadFotografias} fotografías en el lote"
                };

                if (CantidadCompletadas > 0)
                {
                    partes.Add(CantidadCompletadas == 1
                        ? "1 completada"
                        : $"{CantidadCompletadas} completadas");
                }

                if (CantidadConError > 0)
                {
                    partes.Add(CantidadConError == 1
                        ? "1 con error"
                        : $"{CantidadConError} con error");
                }

                return string.Join(" · ", partes);
            }
        }

        public string TextoBotonProcesar
        {
            get
            {
                int pendientes = CantidadPendientes;
                return pendientes switch
                {
                    <= 0 => "Análisis completado",
                    1 => "Procesar fotografía con IA",
                    _ => $"Procesar {pendientes} fotografías con IA"
                };
            }
        }

        public string TextoAyudaProcesamiento => Fotografias.Count == 1
            ? "Revise la imagen y su contexto antes de iniciar el análisis."
            : "Revise todas las imágenes y sus contextos antes de iniciar. " +
              "Las fotografías se procesarán individualmente.";

        public bool PuedeProcesar =>
            diagnosticoId > 0 &&
            Fotografias.Count > 0 &&
            Fotografias.Any(item => !item.Completada) &&
            Fotografias
                .Where(item => !item.Completada)
                .All(item => item.ContextoValido);

        public void AplicarParametros(
            int id,
            IEnumerable<int>? ids)
        {
            if (parametrosAplicados)
                return;

            diagnosticoId = id;
            fotografiaIds = (ids ?? [])
                .Where(item => item > 0)
                .Distinct()
                .ToArray();
            parametrosAplicados = true;
        }

        public async Task InicializarAsync()
        {
            if (inicializado || IsBusy)
                return;

            inicializado = true;

            if (diagnosticoId <= 0 || fotografiaIds.Length == 0)
            {
                await MostrarAlertaAsync(
                    "Selección no disponible",
                    "No se recibieron fotografías válidas para preparar el análisis IA.");
                return;
            }

            IsBusy = true;
            MensajeEstado = "Cargando fotografías seleccionadas...";
            ActualizarComandos();

            try
            {
                InspeccionFitosanitariaDetalleV2 detalle =
                    await inspeccionApi.ObtenerDetalleAsync(diagnosticoId);

                Dictionary<int, InspeccionFotoV2> porId = detalle.Fotografias
                    .Where(item => fotografiaIds.Contains(item.FotografiaId))
                    .ToDictionary(item => item.FotografiaId);

                Fotografias.Clear();

                foreach (int fotografiaId in fotografiaIds)
                {
                    if (!porId.TryGetValue(
                            fotografiaId,
                            out InspeccionFotoV2? fotografia) ||
                        !EsProcesablePorIAInicial(fotografia))
                    {
                        continue;
                    }

                    var item = new InspeccionFotoContextoIAItem
                    {
                        Fotografia = fotografia,
                        EstadoOperacion = fotografia.ResultadoIA != null &&
                                          fotografia.Estado ==
                                              InspeccionFotoEstados.ErrorIA
                            ? "Existe un resultado IA válido guardado. Se recuperará sin volver a consumir el modelo."
                            : "Pendiente de contexto y procesamiento"
                    };

                    item.PropertyChanged += OnFotografiaPropertyChanged;
                    Fotografias.Add(item);
                }

                if (Fotografias.Count == 0)
                {
                    await MostrarAlertaAsync(
                        "No hay fotografías pendientes",
                        "Las fotografías seleccionadas ya no requieren un análisis IA inicial. Actualice el expediente para ver su estado actual.");
                }
            }
            catch (Exception ex)
            {
                inicializado = false;
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarEstado();
                ActualizarComandos();
            }
        }

        private async Task ProcesarAsync()
        {
            if (IsBusy || !PuedeProcesar || !ValidarEnLinea())
                return;

            List<InspeccionFotoContextoIAItem> pendientes = Fotografias
                .Where(item => !item.Completada)
                .OrderBy(item => item.Orden)
                .ToList();

            if (pendientes.Count == 0)
                return;

            List<InspeccionFotoContextoIAItem> contextosInvalidos = pendientes
                .Where(item => !item.ContextoValido)
                .ToList();

            if (contextosInvalidos.Count > 0)
            {
                await MostrarAlertaAsync(
                    "Contexto incompleto",
                    pendientes.Count == 1
                        ? "La fotografía debe tener entre 8 y 500 caracteres de contexto antes de iniciar el análisis."
                        : "Cada fotografía debe tener entre 8 y 500 caracteres de contexto antes de procesar el lote.");
                return;
            }

            bool confirmar = await ConfirmarAsync(
                pendientes.Count == 1
                    ? "Procesar fotografía con IA"
                    : "Procesar lote con IA",
                pendientes.Count == 1
                    ? "Se analizará esta fotografía con el contexto mostrado."
                    : $"Se analizarán {pendientes.Count} fotografías una por una. Cada imagen usará únicamente su propio contexto y un error no detendrá las demás.");

            if (!confirmar)
                return;

            IsBusy = true;
            MensajeEstado = "Iniciando análisis IA por fotografía...";
            ActualizarComandos();

            int exitosas = 0;
            int conError = 0;

            foreach (InspeccionFotoContextoIAItem item in pendientes)
            {
                item.Procesando = true;
                item.ConError = false;
                item.EstadoOperacion =
                    $"Procesando fotografía {item.Orden} con IA...";
                MensajeEstado =
                    $"Analizando fotografía {item.Orden} · {exitosas + conError + 1} de {pendientes.Count}...";

                try
                {
                    string contexto = item.RecuperaResultadoExistente
                        ? "Recuperar resultado IA válido ya almacenado para esta fotografía."
                        : item.Contexto.Trim();

                    InspeccionOperacionMasivaV2 resultado =
                        await inspeccionApi.ProcesarFotosConContextoAsync(
                            diagnosticoId,
                            [item.FotografiaId],
                            new Dictionary<int, string>
                            {
                                [item.FotografiaId] = contexto
                            });

                    InspeccionOperacionItemV2? detalle = resultado.Resultados
                        .FirstOrDefault(resultadoItem =>
                            resultadoItem.FotografiaId == item.FotografiaId);

                    bool exitoso = detalle?.Exitoso == true ||
                                   (detalle == null &&
                                    resultado.TotalExitosas > 0 &&
                                    resultado.TotalConError == 0);

                    if (!exitoso)
                    {
                        throw new InvalidOperationException(
                            string.IsNullOrWhiteSpace(detalle?.Mensaje)
                                ? "El backend no pudo completar el análisis de esta fotografía."
                                : detalle!.Mensaje);
                    }

                    item.Completada = true;
                    item.ConError = false;
                    item.EstadoOperacion = string.IsNullOrWhiteSpace(detalle?.Mensaje)
                        ? "Análisis IA completado correctamente."
                        : $"Completada · {detalle!.Mensaje}";
                    exitosas++;
                }
                catch (Exception ex)
                {
                    item.ConError = true;
                    item.EstadoOperacion = string.IsNullOrWhiteSpace(ex.Message)
                        ? "No fue posible completar el análisis de esta fotografía."
                        : ex.Message;
                    conError++;
                }
                finally
                {
                    item.Procesando = false;
                    NotificarEstado();
                }
            }

            MensajeEstado = string.Empty;
            IsBusy = false;
            NotificarEstado();
            ActualizarComandos();

            if (conError == 0)
            {
                await MostrarAlertaAsync(
                    "Análisis IA completado",
                    exitosas == 1
                        ? "La fotografía fue procesada correctamente. Regresará al expediente para continuar con la decisión técnica."
                        : $"Las {exitosas} fotografías fueron procesadas correctamente. Regresará al expediente para continuar con la decisión técnica.");

                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("..");

                return;
            }

            await MostrarAlertaAsync(
                exitosas > 0
                    ? "Análisis IA parcialmente completado"
                    : "Análisis IA no completado",
                exitosas > 0
                    ? $"{exitosas} fotografía(s) terminaron correctamente y {conError} quedaron con error. Revise el mensaje de cada tarjeta y reintente solamente las pendientes."
                    : $"Las {conError} fotografía(s) quedaron con error. Revise el detalle visible en cada tarjeta antes de reintentar.");
        }

        private static bool EsProcesablePorIAInicial(
            InspeccionFotoV2 foto)
        {
            if (foto.Descartada)
                return false;

            if (foto.Estado is
                InspeccionFotoEstados.Borrador or
                InspeccionFotoEstados.PendienteIA or
                InspeccionFotoEstados.ErrorIA)
            {
                return true;
            }

            return foto.ResultadoIA == null &&
                   foto.Estado == InspeccionFotoEstados.NoConcluyente;
        }

        private void OnFotografiaPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is
                nameof(InspeccionFotoContextoIAItem.Contexto) or
                nameof(InspeccionFotoContextoIAItem.ContextoValido) or
                nameof(InspeccionFotoContextoIAItem.Completada) or
                nameof(InspeccionFotoContextoIAItem.ConError) or
                nameof(InspeccionFotoContextoIAItem.Procesando))
            {
                NotificarEstado();
                ActualizarComandos();
            }
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(Titulo));
            OnPropertyChanged(nameof(Subtitulo));
            OnPropertyChanged(nameof(CantidadFotografias));
            OnPropertyChanged(nameof(CantidadCompletadas));
            OnPropertyChanged(nameof(CantidadConError));
            OnPropertyChanged(nameof(CantidadPendientes));
            OnPropertyChanged(nameof(TextoProgreso));
            OnPropertyChanged(nameof(TextoBotonProcesar));
            OnPropertyChanged(nameof(TextoAyudaProcesamiento));
            OnPropertyChanged(nameof(PuedeProcesar));
        }

        private void ActualizarComandos()
        {
            ProcesarCommand.ChangeCanExecute();
        }
    }
}
