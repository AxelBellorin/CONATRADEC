using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    public sealed class MotivoDevolucionTecnicoFormViewModel : GlobalService
    {
        private readonly MotivoDevolucionTecnicoApiService api = new();
        private int id;
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
                () => !IsBusy && (EsNuevo ? CanAdd : CanEdit));
        }

        public Command RegresarCommand { get; }
        public Command GuardarCommand { get; }

        public bool EsNuevo => id <= 0;
        public bool CodigoEditable => EsNuevo;
        public string Titulo => EsNuevo
            ? "Nuevo motivo de devolución"
            : "Editar motivo de devolución";
        public string Subtitulo =>
            "Define la causa, la instrucción sugerida y el tipo de corrección que deberá realizar el técnico.";

        public string Codigo
        {
            get => codigo;
            set => Asignar(ref codigo, value?.ToUpperInvariant().Replace(' ', '_') ?? string.Empty);
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
                if (mensajeEstado == value)
                    return;
                mensajeEstado = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeEstado));
            }
        }

        public bool TieneMensajeEstado => !string.IsNullOrWhiteSpace(MensajeEstado);

        public string AyudaCorreccion => RequiereNuevaFotografia
            ? "El técnico deberá agregar otra evidencia y descartar la fotografía sustituida."
            : PermiteCorregirMetadatos
                ? "El técnico podrá corregir tipo de fotografía y fecha, y luego reenviar la evidencia a la IA."
                : "Seleccione una forma de resolución para que la devolución no quede sin una acción disponible.";

        public void AplicarId(int valor)
        {
            id = Math.Max(0, valor);
            OnPropertyChanged(nameof(EsNuevo));
            OnPropertyChanged(nameof(CodigoEditable));
            OnPropertyChanged(nameof(Titulo));
        }

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            if (!EsNuevo)
                await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            var permiso = PermissionService.Instance.Get(
                DiagnosticoIARoutes.InterfazConfiguracion);
            CanAdd = permiso?.agregar == true;
            CanEdit = permiso?.actualizar == true;
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            ActualizarComandos();
        }

        private async Task CargarAsync()
        {
            IsBusy = true;
            MensajeEstado = "Cargando motivo...";
            ActualizarComandos();

            try
            {
                ApiResult<List<MotivoDevolucionTecnicoItem>> resultado =
                    await api.ListarAdministracionAsync(true, null);
                MotivoDevolucionTecnicoItem? item = resultado.Data?
                    .FirstOrDefault(value =>
                        value.MotivoDevolucionTecnicoId == id);

                if (!resultado.Success || item == null)
                    throw new InvalidOperationException(
                        resultado.Message.Length > 0
                            ? resultado.Message
                            : "No se encontró el motivo indicado.");

                Codigo = item.Codigo;
                Nombre = item.Nombre;
                Descripcion = item.Descripcion;
                InstruccionSugerida = item.InstruccionSugerida;
                RequiereNuevaFotografia = item.RequiereNuevaFotografia;
                PermiteCorregirMetadatos = item.PermiteCorregirMetadatos;
                OrdenTexto = item.Orden.ToString();
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(ex.Message);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private async Task GuardarAsync()
        {
            if (!ValidarFormulario(out string mensaje))
            {
                await MostrarAlertaAsync(mensaje);
                return;
            }

            if (!int.TryParse(OrdenTexto, out int orden))
                return;

            var request = new MotivoDevolucionTecnicoRequest
            {
                Codigo = Codigo.Trim(),
                Nombre = Nombre.Trim(),
                Descripcion = Descripcion.Trim(),
                InstruccionSugerida = InstruccionSugerida.Trim(),
                RequiereNuevaFotografia = RequiereNuevaFotografia,
                PermiteCorregirMetadatos = PermiteCorregirMetadatos,
                Orden = orden
            };

            IsBusy = true;
            MensajeEstado = "Guardando motivo...";
            ActualizarComandos();

            try
            {
                ApiResult<MotivoDevolucionTecnicoItem> resultado = EsNuevo
                    ? await api.CrearAsync(request)
                    : await api.ActualizarAsync(id, request);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                await Shell.Current!.DisplayAlert(
                    "Motivo de devolución",
                    resultado.Message.Length > 0
                        ? resultado.Message
                        : "Motivo guardado correctamente.",
                    "Aceptar");
                await GoToAsyncParameters(AppRoutes.Regresar);
            }
            catch (Exception ex)
            {
                await MostrarAlertaAsync(ex.Message);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private bool ValidarFormulario(out string mensaje)
        {
            if (Codigo.Trim().Length is < 3 or > 60)
            {
                mensaje = "El código debe contener entre 3 y 60 caracteres.";
                return false;
            }

            if (Nombre.Trim().Length is < 3 or > 140)
            {
                mensaje = "El nombre debe contener entre 3 y 140 caracteres.";
                return false;
            }

            if (InstruccionSugerida.Trim().Length is < 8 or > 2000)
            {
                mensaje = "La instrucción sugerida debe contener entre 8 y 2000 caracteres.";
                return false;
            }

            if (!int.TryParse(OrdenTexto, out int orden) || orden is < 1 or > 999)
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

        private static Task MostrarAlertaAsync(string mensaje) =>
            Shell.Current?.DisplayAlert(
                "Motivo de devolución",
                mensaje,
                "Aceptar") ?? Task.CompletedTask;

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            GuardarCommand.ChangeCanExecute();
        }
    }
}
