using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    public sealed class MotivoDevolucionTecnicoFormViewModel : GlobalService
    {
        private readonly MotivoDevolucionTecnicoApiService api = new();
        private int id;
        private bool inicializado;
        private string rowVersion = string.Empty;
        private string codigo = string.Empty;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;
        private string instruccionSugerida = string.Empty;
        private bool requiereNuevaFotografia;
        private bool permiteCorregirMetadatos = true;
        private string ordenTexto = "1";
        private string mensajeEstado = string.Empty;

        public MotivoDevolucionTecnicoFormViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => PuedeGuardar);
        }

        public Command RegresarCommand { get; }
        public Command GuardarCommand { get; }

        public bool EsNuevo => id <= 0;
        public bool CodigoEditable => EsNuevo && PuedeEditarFormulario;

        public string Titulo => EsNuevo
            ? "Nuevo motivo de devolución"
            : "Editar motivo de devolución";

        public string Subtitulo =>
            "Define la causa, la instrucción sugerida y la corrección que deberá realizar el técnico.";

        public bool SinPermisoOperacion => EsNuevo
            ? !CanAdd
            : !CanView;

        public string MensajeSinPermiso => EsNuevo
            ? "No tiene permiso para crear motivos de devolución."
            : "No tiene permiso para consultar este motivo de devolución.";

        public bool MostrarGuardar => EsNuevo ? CanAdd : CanEdit;

        public bool PuedeEditarFormulario =>
            !IsBusy && MostrarGuardar;

        public bool PuedeMostrarFormulario =>
            !SinPermisoOperacion;

        private bool PuedeGuardar =>
            !IsBusy && MostrarGuardar;

        public string Codigo
        {
            get => codigo;
            set => Asignar(
                ref codigo,
                NormalizarCodigo(value));
        }

        public string Nombre
        {
            get => nombre;
            set => Asignar(ref nombre, value ?? string.Empty);
        }

        public string Descripcion
        {
            get => descripcion;
            set => Asignar(ref descripcion, value ?? string.Empty);
        }

        public string InstruccionSugerida
        {
            get => instruccionSugerida;
            set => Asignar(ref instruccionSugerida, value ?? string.Empty);
        }

        public bool RequiereNuevaFotografia
        {
            get => requiereNuevaFotografia;
            set
            {
                if (requiereNuevaFotografia == value)
                    return;

                requiereNuevaFotografia = value;
                if (value)
                    permiteCorregirMetadatos = false;

                OnPropertyChanged();
                OnPropertyChanged(nameof(PermiteCorregirMetadatos));
                OnPropertyChanged(nameof(AyudaCorreccion));
            }
        }

        public bool PermiteCorregirMetadatos
        {
            get => permiteCorregirMetadatos;
            set
            {
                bool nuevo = RequiereNuevaFotografia ? false : value;
                if (permiteCorregirMetadatos == nuevo)
                    return;

                permiteCorregirMetadatos = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AyudaCorreccion));
            }
        }

        public string OrdenTexto
        {
            get => ordenTexto;
            set => Asignar(ref ordenTexto, value ?? string.Empty);
        }

        public string MensajeEstado
        {
            get => mensajeEstado;
            private set
            {
                string nuevo = value ?? string.Empty;
                if (mensajeEstado == nuevo)
                    return;

                mensajeEstado = nuevo;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado =>
            !string.IsNullOrWhiteSpace(MensajeEstado);

        public string AyudaCorreccion => RequiereNuevaFotografia
            ? "El técnico deberá agregar otra evidencia y descartar la fotografía sustituida."
            : PermiteCorregirMetadatos
                ? "El técnico podrá corregir tipo de fotografía y fecha, y luego reenviar la evidencia a la IA."
                : "Seleccione una forma de resolución para que la devolución no quede sin una acción disponible.";

        public void AplicarId(int valor)
        {
            id = Math.Max(0, valor);
            inicializado = false;
            rowVersion = string.Empty;
            OnPropertyChanged(nameof(EsNuevo));
            OnPropertyChanged(nameof(CodigoEditable));
            OnPropertyChanged(nameof(Titulo));
            OnPropertyChanged(nameof(SinPermisoOperacion));
            OnPropertyChanged(nameof(MensajeSinPermiso));
            OnPropertyChanged(nameof(MostrarGuardar));
            OnPropertyChanged(nameof(PuedeEditarFormulario));
            OnPropertyChanged(nameof(PuedeMostrarFormulario));
            ActualizarComandos();
        }

        public async Task InicializarAsync()
        {
            ActualizarPermisos();

            if (inicializado)
                return;

            inicializado = true;

            if (EsNuevo)
                return;

            if (!CanView)
                return;

            await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                MotivoDevolucionTecnicoRoutes.InterfazConfiguracion);

            CanView = permiso?.leer == true;
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            CanDelete = permiso?.eliminar == true;

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(SinPermisoOperacion));
            OnPropertyChanged(nameof(MensajeSinPermiso));
            OnPropertyChanged(nameof(MostrarGuardar));
            OnPropertyChanged(nameof(PuedeEditarFormulario));
            OnPropertyChanged(nameof(PuedeMostrarFormulario));
            OnPropertyChanged(nameof(CodigoEditable));
            ActualizarComandos();
        }

        private async Task CargarAsync()
        {
            if (id <= 0 || IsBusy || !CanView)
                return;

            IsBusy = true;
            MensajeEstado = "Cargando motivo actualizado...";
            ActualizarComandos();
            NotificarInteraccion();

            try
            {
                ApiResult<MotivoDevolucionTecnicoItem> resultado =
                    await api.ObtenerV2Async(id);

                if (!resultado.Success || resultado.Data == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                MotivoDevolucionTecnicoItem item = resultado.Data;

                if (!item.Activo)
                {
                    await MostrarAdvertenciaAsync(
                        "El motivo fue desactivado por otro usuario. Regrese al listado activo o recupérelo desde Eliminados.");
                    await GoToAsyncParameters(AppRoutes.Regresar);
                    return;
                }

                rowVersion = item.RowVersion;
                Codigo = item.Codigo;
                Nombre = item.Nombre;
                Descripcion = item.Descripcion;
                InstruccionSugerida = item.InstruccionSugerida;
                RequiereNuevaFotografia = item.RequiereNuevaFotografia;
                PermiteCorregirMetadatos = item.PermiteCorregirMetadatos;
                OrdenTexto = item.Orden.ToString();
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarInteraccion();
                ActualizarComandos();
            }
        }

        private async Task GuardarAsync()
        {
            if (!PuedeGuardar)
                return;

            if (!ValidarFormulario(out string mensaje, out int orden))
            {
                await MostrarAdvertenciaAsync(mensaje);
                return;
            }

            if (!EsNuevo && string.IsNullOrWhiteSpace(rowVersion))
            {
                await MostrarAdvertenciaAsync(
                    "El registro no tiene una versión válida. Regrese al listado y ábralo nuevamente.");
                return;
            }

            bool confirmar = EsNuevo
                ? await ConfirmarGuardadoAsync("motivo de devolución")
                : await ConfirmarActualizacionAsync("motivo de devolución");

            if (!confirmar)
                return;

            var request = new MotivoDevolucionTecnicoRequest
            {
                Codigo = Codigo.Trim(),
                Nombre = Nombre.Trim(),
                Descripcion = Descripcion.Trim(),
                InstruccionSugerida = InstruccionSugerida.Trim(),
                RequiereNuevaFotografia = RequiereNuevaFotografia,
                PermiteCorregirMetadatos = PermiteCorregirMetadatos,
                Orden = orden,
                RowVersion = rowVersion
            };

            bool recargarPorConflicto = false;
            IsBusy = true;
            MensajeEstado = "Guardando motivo...";
            ActualizarComandos();
            NotificarInteraccion();

            try
            {
                ApiResult<MotivoDevolucionTecnicoItem> resultado = EsNuevo
                    ? await api.CrearV2Async(request)
                    : await api.ActualizarV2Async(id, request);

                if (!resultado.Success)
                {
                    if (!EsNuevo && resultado.StatusCode == 409)
                    {
                        recargarPorConflicto = true;
                        await MostrarAdvertenciaAsync(
                            string.IsNullOrWhiteSpace(resultado.Message)
                                ? "El motivo fue modificado por otro usuario. Se cargarán los datos actuales."
                                : resultado.Message);
                    }
                    else
                    {
                        await MostrarErrorAsync(resultado.Message);
                    }

                }
                else
                {
                    await MostrarExitoAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Motivo guardado correctamente."
                            : resultado.Message);

                    await GoToAsyncParameters(AppRoutes.Regresar);
                }
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                NotificarInteraccion();
                ActualizarComandos();
            }

            if (recargarPorConflicto)
                await CargarAsync();
        }

        private bool ValidarFormulario(
            out string mensaje,
            out int orden)
        {
            string codigoNormalizado = Codigo.Trim();

            if (codigoNormalizado.Length is < 3 or > 60 ||
                codigoNormalizado.Any(caracter =>
                    !((caracter >= 'A' && caracter <= 'Z') ||
                      char.IsDigit(caracter) ||
                      caracter == '_')))
            {
                mensaje =
                    "El código debe contener entre 3 y 60 caracteres: letras mayúsculas, números o guion bajo.";
                orden = 0;
                return false;
            }

            if (Nombre.Trim().Length is < 3 or > 140)
            {
                mensaje = "El nombre debe contener entre 3 y 140 caracteres.";
                orden = 0;
                return false;
            }

            if (Descripcion.Trim().Length > 700)
            {
                mensaje = "La descripción no puede superar 700 caracteres.";
                orden = 0;
                return false;
            }

            if (InstruccionSugerida.Trim().Length is < 8 or > 2000)
            {
                mensaje =
                    "La instrucción sugerida debe contener entre 8 y 2000 caracteres.";
                orden = 0;
                return false;
            }

            if (!int.TryParse(OrdenTexto, out orden) ||
                orden is < 1 or > 999)
            {
                mensaje = "El orden debe estar entre 1 y 999.";
                return false;
            }

            if (RequiereNuevaFotografia == PermiteCorregirMetadatos)
            {
                mensaje =
                    "Seleccione exactamente una forma de resolución: nueva fotografía o corrección de metadatos.";
                return false;
            }

            mensaje = string.Empty;
            return true;
        }

        private static string NormalizarCodigo(string? valor)
        {
            string texto = (valor ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(' ', '_');

            while (texto.Contains("__", StringComparison.Ordinal))
                texto = texto.Replace("__", "_", StringComparison.Ordinal);

            return texto;
        }

        private void Asignar(
            ref string campo,
            string valor,
            [CallerMemberName] string? propiedad = null)
        {
            if (campo == valor)
                return;

            campo = valor;
            OnPropertyChanged(propiedad);
        }

        private void NotificarInteraccion()
        {
            OnPropertyChanged(nameof(MostrarGuardar));
            OnPropertyChanged(nameof(PuedeEditarFormulario));
            OnPropertyChanged(nameof(CodigoEditable));
            ActualizarComandos();
        }

        private static Task MostrarErrorAsync(string mensaje) =>
            GlobalService.MostrarErrorAsync(
                string.IsNullOrWhiteSpace(mensaje)
                    ? "No fue posible completar la operación."
                    : mensaje);

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
        }
    }
}
