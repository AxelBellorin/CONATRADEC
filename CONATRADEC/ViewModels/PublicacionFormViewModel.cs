using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class PublicacionFormViewModel : GlobalService
    {
        private readonly PublicacionApiService apiService = new();

        private int publicacionId;
        private CategoriaPublicacionResponse? categoriaSeleccionada;
        private string titulo = string.Empty;
        private string resumen = string.Empty;
        private string contenido = string.Empty;
        private string enlaceExterno = string.Empty;
        private string textoEnlace = string.Empty;
        private string ubicacion = string.Empty;
        private string estadoSeleccionado = "BORRADOR";
        private bool destacada;
        private bool tieneEvento;
        private bool tieneFechaFinEvento;
        private bool tieneFechaFinPublicacion;
        private DateTime fechaEventoInicio = DateTime.Today;
        private TimeSpan horaEventoInicio = new(8, 0, 0);
        private DateTime fechaEventoFin = DateTime.Today;
        private TimeSpan horaEventoFin = new(10, 0, 0);
        private DateTime fechaInicioPublicacion = DateTime.Today;
        private TimeSpan horaInicioPublicacion = DateTime.Now.TimeOfDay;
        private DateTime fechaFinPublicacion = DateTime.Today.AddDays(30);
        private TimeSpan horaFinPublicacion = new(23, 59, 0);
        private ImageSource? portadaPreview;
        private FileResult? portadaSeleccionada;
        private string rutaPortadaActual = string.Empty;
        private string mensaje = string.Empty;
        private bool inicializado;

        public PublicacionFormViewModel()
        {
            Categorias = new ObservableCollection<
                CategoriaPublicacionResponse>();

            Estados = new ObservableCollection<string>
            {
                "BORRADOR",
                "PUBLICADA",
                "ARCHIVADA"
            };

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy && PuedeGuardar);

            CancelarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            EliminarPortadaCommand = new Command(
                async () => await EliminarPortadaAsync(),
                () => !IsBusy && TienePortada && PuedeGuardar);
        }

        public ObservableCollection<CategoriaPublicacionResponse>
            Categorias { get; }

        public ObservableCollection<string> Estados { get; }

        public int PublicacionId
        {
            get => publicacionId;
            private set
            {
                publicacionId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsEdicion));
                OnPropertyChanged(nameof(TituloPagina));
                OnPropertyChanged(nameof(TextoBotonGuardar));
                OnPropertyChanged(nameof(PuedeGuardar));
            }
        }

        public bool EsEdicion => PublicacionId > 0;

        public string TituloPagina =>
            EsEdicion ? "Editar publicación" : "Nueva publicación";

        public string TextoBotonGuardar =>
            EsEdicion ? "Guardar cambios" : "Crear publicación";

        public bool PuedeGuardar =>
            EsEdicion ? CanEdit : CanAdd;

        public CategoriaPublicacionResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public string Titulo
        {
            get => titulo;
            set
            {
                titulo = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CaracteresTitulo));
            }
        }

        public string CaracteresTitulo =>
            $"{Titulo.Length}/180";

        public string Resumen
        {
            get => resumen;
            set
            {
                resumen = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CaracteresResumen));
            }
        }

        public string CaracteresResumen =>
            $"{Resumen.Length}/500";

        public string Contenido
        {
            get => contenido;
            set
            {
                contenido = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string EnlaceExterno
        {
            get => enlaceExterno;
            set
            {
                enlaceExterno = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string TextoEnlace
        {
            get => textoEnlace;
            set
            {
                textoEnlace = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Ubicacion
        {
            get => ubicacion;
            set
            {
                ubicacion = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string EstadoSeleccionado
        {
            get => estadoSeleccionado;
            set
            {
                estadoSeleccionado = string.IsNullOrWhiteSpace(value)
                    ? "BORRADOR"
                    : value;

                OnPropertyChanged();
            }
        }

        public bool Destacada
        {
            get => destacada;
            set
            {
                destacada = value;
                OnPropertyChanged();
            }
        }

        public bool TieneEvento
        {
            get => tieneEvento;
            set
            {
                tieneEvento = value;
                OnPropertyChanged();
            }
        }

        public bool TieneFechaFinEvento
        {
            get => tieneFechaFinEvento;
            set
            {
                tieneFechaFinEvento = value;
                OnPropertyChanged();
            }
        }

        public bool TieneFechaFinPublicacion
        {
            get => tieneFechaFinPublicacion;
            set
            {
                tieneFechaFinPublicacion = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaEventoInicio
        {
            get => fechaEventoInicio;
            set
            {
                fechaEventoInicio = value.Date;
                OnPropertyChanged();
            }
        }

        public TimeSpan HoraEventoInicio
        {
            get => horaEventoInicio;
            set
            {
                horaEventoInicio = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaEventoFin
        {
            get => fechaEventoFin;
            set
            {
                fechaEventoFin = value.Date;
                OnPropertyChanged();
            }
        }

        public TimeSpan HoraEventoFin
        {
            get => horaEventoFin;
            set
            {
                horaEventoFin = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaInicioPublicacion
        {
            get => fechaInicioPublicacion;
            set
            {
                fechaInicioPublicacion = value.Date;
                OnPropertyChanged();
            }
        }

        public TimeSpan HoraInicioPublicacion
        {
            get => horaInicioPublicacion;
            set
            {
                horaInicioPublicacion = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaFinPublicacion
        {
            get => fechaFinPublicacion;
            set
            {
                fechaFinPublicacion = value.Date;
                OnPropertyChanged();
            }
        }

        public TimeSpan HoraFinPublicacion
        {
            get => horaFinPublicacion;
            set
            {
                horaFinPublicacion = value;
                OnPropertyChanged();
            }
        }

        public ImageSource? PortadaPreview
        {
            get => portadaPreview;
            private set
            {
                portadaPreview = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TienePortada));
                EliminarPortadaCommand.ChangeCanExecute();
            }
        }

        public bool TienePortada => PortadaPreview != null;

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Command EliminarPortadaCommand { get; }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("noticiasPage");
            OnPropertyChanged(nameof(PuedeGuardar));
            GuardarCommand.ChangeCanExecute();
            EliminarPortadaCommand.ChangeCanExecute();
        }

        public async Task InicializarAsync(int id)
        {
            if (IsBusy)
                return;

            if (inicializado && PublicacionId == id)
                return;

            PublicacionId = Math.Max(0, id);
            inicializado = true;

            if (!PuedeGuardar)
            {
                Mensaje =
                    "No tiene permiso para crear o editar publicaciones.";
                return;
            }

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                await CargarCategoriasAsync();

                if (EsEdicion)
                    await CargarPublicacionAsync();
                else
                    PrepararNuevaPublicacion();
            }
            catch (Exception ex)
            {
                inicializado = false;
                Mensaje = "No fue posible preparar el formulario.";
                await MostrarErrorInesperadoAsync(
                    "preparar el formulario de publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        public async Task SeleccionarPortadaAsync(FileResult archivo)
        {
            if (archivo == null)
                return;

            string extension = Path.GetExtension(archivo.FileName)
                .ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg" and
                not ".png" and not ".webp")
            {
                await MostrarAdvertenciaAsync(
                    "Seleccione una imagen JPG, JPEG, PNG o WEBP.");
                return;
            }

            try
            {
                string cachePath = Path.Combine(
                    FileSystem.CacheDirectory,
                    $"noticia_portada_{Guid.NewGuid():N}{extension}");

                await using Stream origen = await archivo.OpenReadAsync();
                await using FileStream destino = File.Create(cachePath);
                await origen.CopyToAsync(destino);

                portadaSeleccionada = archivo;
                PortadaPreview = ImageSource.FromFile(cachePath);
            }
            catch
            {
                await MostrarErrorAsync(
                    "No fue posible preparar la imagen seleccionada.");
            }
        }

        private async Task CargarCategoriasAsync()
        {
            ApiResult<List<CategoriaPublicacionResponse>> result =
                await apiService.GetCategoriasAsync();

            if (!result.Success || result.Data == null)
                throw new InvalidOperationException(result.Message);

            Categorias.Clear();

            foreach (CategoriaPublicacionResponse categoria
                     in result.Data.OrderBy(x => x.Orden))
            {
                Categorias.Add(categoria);
            }

            CategoriaSeleccionada ??= Categorias.FirstOrDefault();
        }

        private async Task CargarPublicacionAsync()
        {
            ApiResult<PublicacionDetalleResponse> result =
                await apiService.GetParaAdministrarAsync(PublicacionId);

            if (!result.Success || result.Data == null)
                throw new InvalidOperationException(result.Message);

            PublicacionDetalleResponse item = result.Data;

            CategoriaSeleccionada = Categorias.FirstOrDefault(x =>
                x.CategoriaPublicacionId ==
                item.CategoriaPublicacionId);

            Titulo = item.Titulo;
            Resumen = item.Resumen;
            Contenido = item.Contenido;
            EnlaceExterno = item.EnlaceExterno;
            TextoEnlace = item.TextoEnlace;
            Ubicacion = item.Ubicacion;
            EstadoSeleccionado = item.EstadoPublicacion;
            Destacada = item.Destacada;

            DateTime inicioPublicacion =
                AFechaLocal(item.FechaInicioPublicacionUtc);

            FechaInicioPublicacion = inicioPublicacion.Date;
            HoraInicioPublicacion = inicioPublicacion.TimeOfDay;

            TieneFechaFinPublicacion =
                item.FechaFinPublicacionUtc.HasValue;

            if (item.FechaFinPublicacionUtc.HasValue)
            {
                DateTime finPublicacion =
                    AFechaLocal(item.FechaFinPublicacionUtc.Value);

                FechaFinPublicacion = finPublicacion.Date;
                HoraFinPublicacion = finPublicacion.TimeOfDay;
            }

            TieneEvento = item.FechaEventoInicioUtc.HasValue;
            TieneFechaFinEvento = item.FechaEventoFinUtc.HasValue;

            if (item.FechaEventoInicioUtc.HasValue)
            {
                DateTime inicioEvento =
                    AFechaLocal(item.FechaEventoInicioUtc.Value);

                FechaEventoInicio = inicioEvento.Date;
                HoraEventoInicio = inicioEvento.TimeOfDay;
            }

            if (item.FechaEventoFinUtc.HasValue)
            {
                DateTime finEvento =
                    AFechaLocal(item.FechaEventoFinUtc.Value);

                FechaEventoFin = finEvento.Date;
                HoraEventoFin = finEvento.TimeOfDay;
            }

            rutaPortadaActual = item.RutaImagenPortada;

            if (!string.IsNullOrWhiteSpace(item.ImagenPortadaUrl))
                PortadaPreview = ImageSource.FromUri(
                    new Uri(item.ImagenPortadaUrl));
        }

        private void PrepararNuevaPublicacion()
        {
            DateTime ahora = DateTime.Now;
            FechaInicioPublicacion = ahora.Date;
            HoraInicioPublicacion = ahora.TimeOfDay;
            FechaFinPublicacion = ahora.Date.AddDays(30);
            HoraFinPublicacion = new TimeSpan(23, 59, 0);
            FechaEventoInicio = ahora.Date;
            HoraEventoInicio = new TimeSpan(8, 0, 0);
            FechaEventoFin = ahora.Date;
            HoraEventoFin = new TimeSpan(10, 0, 0);
            EstadoSeleccionado = "BORRADOR";
            CategoriaSeleccionada = Categorias.FirstOrDefault();
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar || IsBusy)
                return;

            string? error = ValidarFormulario();

            if (!string.IsNullOrWhiteSpace(error))
            {
                await MostrarAdvertenciaAsync(error);
                return;
            }

            bool confirmar = EsEdicion
                ? await ConfirmarActualizacionAsync("la publicación")
                : await ConfirmarGuardadoAsync("la publicación");

            if (!confirmar)
                return;

            if (!await ValidarInternetAsync())
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                PublicacionGuardarRequest request = ConstruirRequest();
                string mensajeExito;

                if (EsEdicion)
                {
                    ApiResult<bool> result =
                        await apiService.ActualizarAsync(request);

                    if (!result.Success)
                    {
                        Mensaje = result.Message;
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    mensajeExito = result.Message;
                }
                else
                {
                    ApiResult<PublicacionCreadaResponse> result =
                        await apiService.CrearAsync(request);

                    if (!result.Success || result.Data == null)
                    {
                        Mensaje = result.Message;
                        await MostrarErrorAsync(result.Message);
                        return;
                    }

                    PublicacionId = result.Data.PublicacionId;
                    mensajeExito = result.Message;
                }

                if (portadaSeleccionada != null)
                {
                    ApiResult<PortadaPublicacionResponse> portadaResult =
                        await apiService.SubirPortadaAsync(
                            PublicacionId,
                            portadaSeleccionada);

                    if (!portadaResult.Success)
                    {
                        PublicacionListadoEstadoService
                            .MarcarActualizacion();

                        Mensaje =
                            "La publicación fue guardada, pero la portada no pudo cargarse. " +
                            portadaResult.Message;

                        await MostrarAdvertenciaAsync(Mensaje);
                        await GoToAsyncParameters(AppRoutes.Regresar);
                        return;
                    }
                }

                PublicacionListadoEstadoService.MarcarActualizacion();
                await MostrarExitoAsync(
                    string.IsNullOrWhiteSpace(mensajeExito)
                        ? "Publicación guardada correctamente."
                        : mensajeExito);

                await GoToAsyncParameters(AppRoutes.Regresar);
            }
            catch (Exception ex)
            {
                Mensaje = "No fue posible guardar la publicación.";
                await MostrarErrorInesperadoAsync(
                    "guardar la publicación",
                    ex);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task EliminarPortadaAsync()
        {
            if (!TienePortada || !PuedeGuardar)
                return;

            bool confirmar = await ConfirmarAsync(
                "Eliminar portada",
                "¿Desea quitar la imagen de portada de esta publicación?",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            if (!EsEdicion || string.IsNullOrWhiteSpace(rutaPortadaActual))
            {
                portadaSeleccionada = null;
                PortadaPreview = null;
                return;
            }

            ApiResult<bool> result =
                await apiService.EliminarPortadaAsync(PublicacionId);

            if (!result.Success)
            {
                await MostrarErrorAsync(result.Message);
                return;
            }

            rutaPortadaActual = string.Empty;
            portadaSeleccionada = null;
            PortadaPreview = null;
            PublicacionListadoEstadoService.MarcarActualizacion();
            await MostrarExitoAsync(result.Message);
        }

        private PublicacionGuardarRequest ConstruirRequest()
        {
            DateTime inicioPublicacion =
                FechaInicioPublicacion.Date + HoraInicioPublicacion;

            DateTimeOffset inicioOffset =
                CrearDateTimeOffsetLocal(inicioPublicacion);

            DateTimeOffset? finPublicacion = null;

            if (TieneFechaFinPublicacion)
            {
                finPublicacion = CrearDateTimeOffsetLocal(
                    FechaFinPublicacion.Date + HoraFinPublicacion);
            }

            DateTimeOffset? inicioEvento = null;
            DateTimeOffset? finEvento = null;

            if (TieneEvento)
            {
                inicioEvento = CrearDateTimeOffsetLocal(
                    FechaEventoInicio.Date + HoraEventoInicio);

                if (TieneFechaFinEvento)
                {
                    finEvento = CrearDateTimeOffsetLocal(
                        FechaEventoFin.Date + HoraEventoFin);
                }
            }

            return new PublicacionGuardarRequest
            {
                PublicacionId = PublicacionId,
                CategoriaPublicacionId =
                    CategoriaSeleccionada!.CategoriaPublicacionId,
                Titulo = Titulo.Trim(),
                Resumen = Resumen.Trim(),
                Contenido = Contenido.Trim(),
                EnlaceExterno = EnlaceExterno.Trim(),
                TextoEnlace = TextoEnlace.Trim(),
                Ubicacion = Ubicacion.Trim(),
                FechaEventoInicio = inicioEvento,
                FechaEventoFin = finEvento,
                FechaInicioPublicacion = inicioOffset,
                FechaFinPublicacion = finPublicacion,
                EstadoPublicacion = EstadoSeleccionado,
                Destacada = Destacada
            };
        }

        private string? ValidarFormulario()
        {
            if (CategoriaSeleccionada == null)
                return "Seleccione una categoría.";

            if (string.IsNullOrWhiteSpace(Titulo))
                return "Ingrese el título de la publicación.";

            if (Titulo.Trim().Length > 180)
                return "El título no puede superar los 180 caracteres.";

            if (string.IsNullOrWhiteSpace(Resumen))
                return "Ingrese un resumen para la publicación.";

            if (Resumen.Trim().Length > 500)
                return "El resumen no puede superar los 500 caracteres.";

            if (string.IsNullOrWhiteSpace(Contenido))
                return "Ingrese el contenido completo de la publicación.";

            DateTime inicioPublicacion =
                FechaInicioPublicacion.Date + HoraInicioPublicacion;

            if (TieneFechaFinPublicacion)
            {
                DateTime finPublicacion =
                    FechaFinPublicacion.Date + HoraFinPublicacion;

                if (finPublicacion < inicioPublicacion)
                {
                    return "La fecha final de publicación no puede ser anterior a la fecha inicial.";
                }
            }

            if (TieneEvento && TieneFechaFinEvento)
            {
                DateTime inicioEvento =
                    FechaEventoInicio.Date + HoraEventoInicio;

                DateTime finEvento =
                    FechaEventoFin.Date + HoraEventoFin;

                if (finEvento < inicioEvento)
                {
                    return "La fecha final del evento no puede ser anterior a la fecha inicial.";
                }
            }

            if (!string.IsNullOrWhiteSpace(EnlaceExterno) &&
                (!Uri.TryCreate(
                    EnlaceExterno.Trim(),
                    UriKind.Absolute,
                    out Uri? enlace) ||
                 (enlace.Scheme != Uri.UriSchemeHttp &&
                  enlace.Scheme != Uri.UriSchemeHttps)))
            {
                return "Ingrese un enlace HTTP o HTTPS válido.";
            }

            return null;
        }

        private static DateTimeOffset CrearDateTimeOffsetLocal(
            DateTime local)
        {
            DateTime valor = DateTime.SpecifyKind(
                local,
                DateTimeKind.Unspecified);

            TimeSpan offset =
                TimeZoneInfo.Local.GetUtcOffset(valor);

            return new DateTimeOffset(valor, offset);
        }

        private static DateTime AFechaLocal(DateTime value)
        {
            DateTime utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return utc.ToLocalTime();
        }

        private void ActualizarComandos()
        {
            GuardarCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
            EliminarPortadaCommand.ChangeCanExecute();
        }
    }
}
