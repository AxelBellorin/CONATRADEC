using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Formulario para crear, editar y visualizar roles.
    /// Los parámetros de navegación se consumen como una unidad para impedir
    /// que Edit/View caigan accidentalmente en Create.
    /// </summary>
    public class RolFormViewModel :
        GlobalService,
        IQueryAttributable
    {
        private readonly RolApiService rolApiService = new();

        private CancellationTokenSource? operacionCts;
        private RolRequest rol = new();
        private RolResponse? rolOriginal;

        private string nombreRol = string.Empty;
        private string descripcionRol = string.Empty;
        private string errorNombreRol = string.Empty;
        private string errorDescripcionRol = string.Empty;

        private FormMode.FormModeSelect mode =
            FormMode.FormModeSelect.View;

        private bool parametrosRecibidos;
        private bool parametrosValidos;
        private bool validacionNavegacionEjecutada;

        public RolFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () =>
                    ParametrosValidos &&
                    ShowSaveButton &&
                    !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public bool ParametrosValidos =>
            parametrosValidos;

        public RolRequest Rol
        {
            get => rol;
            private set
            {
                rol = value ?? new RolRequest();

                OnPropertyChanged();

                NombreRol =
                    rol.NombreRol ?? string.Empty;

                DescripcionRol =
                    rol.DescripcionRol ?? string.Empty;

                LimpiarErrores();
                NotificarModo();
            }
        }

        public string NombreRol
        {
            get => nombreRol;
            set
            {
                nombreRol = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombreRol))
                    ErrorNombreRol = string.Empty;
            }
        }

        public string DescripcionRol
        {
            get => descripcionRol;
            set
            {
                descripcionRol = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcionRol))
                    ErrorDescripcionRol = string.Empty;
            }
        }

        public string ErrorNombreRol
        {
            get => errorNombreRol;
            private set
            {
                if (errorNombreRol == value)
                    return;

                errorNombreRol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorNombreRol));
            }
        }

        public bool TieneErrorNombreRol =>
            !string.IsNullOrWhiteSpace(ErrorNombreRol);

        public string ErrorDescripcionRol
        {
            get => errorDescripcionRol;
            private set
            {
                if (errorDescripcionRol == value)
                    return;

                errorDescripcionRol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorDescripcionRol));
            }
        }

        public bool TieneErrorDescripcionRol =>
            !string.IsNullOrWhiteSpace(ErrorDescripcionRol);

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            private set
            {
                if (mode == value)
                    return;

                mode = value;
                OnPropertyChanged();
                NotificarModo();
            }
        }

        public bool EsRolProtegido =>
            string.Equals(
                rolOriginal?.NombreMostrar ??
                Rol.NombreRol?.Trim(),
                "ADMINISTRADOR",
                StringComparison.OrdinalIgnoreCase);

        public bool IsReadOnly =>
            !ParametrosValidos ||
            Mode == FormMode.FormModeSelect.View ||
            (Mode == FormMode.FormModeSelect.Edit &&
             EsRolProtegido);

        public bool ShowSaveButton =>
            ParametrosValidos &&
            !EsRolProtegido &&
            Mode switch
            {
                FormMode.FormModeSelect.Create => CanAdd,
                FormMode.FormModeSelect.Edit => CanEdit,
                _ => false
            };

        public string Title
        {
            get
            {
                if (!ParametrosValidos)
                    return "Rol";

                return Mode switch
                {
                    FormMode.FormModeSelect.Create =>
                        "Crear rol",

                    FormMode.FormModeSelect.Edit =>
                        "Editar rol",

                    FormMode.FormModeSelect.View =>
                        "Detalles del rol",

                    _ =>
                        "Rol"
                };
            }
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("rolPage");

            OnPropertyChanged(nameof(CanView));
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));

            NotificarModo();
        }

        /// <summary>
        /// Shell entrega el diccionario completo en una sola llamada. Create
        /// debe venir explícitamente; View/Edit requieren además un rol válido.
        /// </summary>
        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            parametrosRecibidos = true;
            validacionNavegacionEjecutada = false;

            bool modoValido =
                TryObtenerModo(
                    query,
                    out FormMode.FormModeSelect modoSolicitado);

            RolResponse? recibido =
                TryObtenerRol(
                    query);

            bool requiereRol =
                modoValido &&
                (modoSolicitado == FormMode.FormModeSelect.View ||
                 modoSolicitado == FormMode.FormModeSelect.Edit);

            parametrosValidos =
                modoValido &&
                (!requiereRol ||
                 recibido?.RolId is > 0);

            if (!parametrosValidos)
            {
                rolOriginal = null;
                Mode = FormMode.FormModeSelect.View;
                Rol = new RolRequest();
                NotificarModo();
                return;
            }

            if (modoSolicitado == FormMode.FormModeSelect.Create)
            {
                rolOriginal = null;
                Rol = new RolRequest();
            }
            else
            {
                rolOriginal =
                    ClonarRol(recibido!);

                Rol =
                    new RolRequest(rolOriginal);
            }

            Mode = modoSolicitado;
            NotificarModo();
        }

        public async Task<bool> ValidarNavegacionAsync()
        {
            if (ParametrosValidos)
                return true;

            if (validacionNavegacionEjecutada)
                return false;

            validacionNavegacionEjecutada = true;

            await Task.Yield();

            if (ParametrosValidos)
                return true;

            await MostrarErrorAsync(
                parametrosRecibidos
                    ? "No se recibieron todos los datos necesarios para abrir el rol."
                    : "No fue posible recibir los parámetros de navegación del rol.");

            await RegresarAsync();
            return false;
        }

        public void CancelarOperaciones()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref operacionCts,
                    null);

            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                source.Dispose();
            }
        }

        private async Task SaveAsync()
        {
            if (IsBusy ||
                !ParametrosValidos ||
                IsReadOnly ||
                !ShowSaveButton)
            {
                return;
            }

            if (!ValidarCampos())
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (Mode == FormMode.FormModeSelect.Create)
            {
                await CrearRolAsync();
                return;
            }

            if (Mode == FormMode.FormModeSelect.Edit)
                await ActualizarRolAsync();
        }

        private async Task CrearRolAsync()
        {
            bool confirm =
                await ConfirmarGuardadoAsync("el rol");

            if (!confirm)
                return;

            CancellationTokenSource source =
                PrepararOperacion();

            RolMutacionListado? mutacion = null;
            string mensajeExito = string.Empty;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                ApiResult<RolResponse> result =
                    await rolApiService
                        .CrearRolAdministracionResultAsync(
                            Rol,
                            source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success ||
                    result.Data?.RolId is not > 0)
                {
                    if (!EsCancelacion(result.Message))
                    {
                        await MostrarErrorAsync(
                            string.IsNullOrWhiteSpace(result.Message)
                                ? "No fue posible guardar el rol. Intente nuevamente."
                                : result.Message);
                    }

                    return;
                }

                RolResponse creado =
                    ClonarRol(result.Data);

                mutacion =
                    new RolMutacionListado(
                        RolMutacionListadoTipo.Creado,
                        creado);

                mensajeExito =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Rol guardado correctamente."
                        : result.Message;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "guardar el rol",
                    ex);
                return;
            }
            finally
            {
                LiberarOperacion(source);
                IsBusy = false;
                RefrescarComandos();
            }

            if (mutacion == null)
                return;

            RolVisitaService.RegistrarMutacion(mutacion);

            await MostrarExitoAsync(mensajeExito);
            await RegresarAsync();
        }

        private async Task ActualizarRolAsync()
        {
            if (EsRolProtegido)
            {
                await MostrarAdvertenciaAsync(
                    "El rol Administrador está protegido y no puede editarse.");
                return;
            }

            if (!HayCambios())
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            if (Rol.RolId is not > 0 ||
                rolOriginal?.RolId is not > 0)
            {
                await MostrarErrorAsync(
                    "No se encontró el rol que se desea actualizar.");
                return;
            }

            bool confirm =
                await ConfirmarActualizacionAsync("el rol");

            if (!confirm)
                return;

            CancellationTokenSource source =
                PrepararOperacion();

            RolMutacionListado? mutacion = null;
            string mensajeExito = string.Empty;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                SincronizarModelo();

                ApiResult<RolResponse> result =
                    await rolApiService
                        .ActualizarRolAdministracionResultAsync(
                            Rol,
                            source.Token);

                if (source.IsCancellationRequested)
                    return;

                if (!result.Success ||
                    result.Data?.RolId is not > 0)
                {
                    await MostrarErrorAsync(
                        string.IsNullOrWhiteSpace(result.Message)
                            ? "No fue posible actualizar el rol. Intente nuevamente."
                            : result.Message);
                    return;
                }

                RolResponse actualizado =
                    ClonarRol(result.Data);

                mutacion =
                    new RolMutacionListado(
                        RolMutacionListadoTipo.Actualizado,
                        actualizado,
                        ClonarRol(rolOriginal));

                rolOriginal =
                    ClonarRol(actualizado);

                Rol =
                    new RolRequest(actualizado);

                mensajeExito =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Rol actualizado correctamente."
                        : result.Message;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "actualizar el rol",
                    ex);
                return;
            }
            finally
            {
                LiberarOperacion(source);
                IsBusy = false;
                RefrescarComandos();
            }

            if (mutacion == null)
                return;

            RolVisitaService.RegistrarMutacion(mutacion);

            await MostrarExitoAsync(mensajeExito);
            await RegresarAsync();
        }

        private async Task CancelAsync()
        {
            if (IsBusy)
                return;

            try
            {
                if (ParametrosValidos &&
                    Mode != FormMode.FormModeSelect.View &&
                    HayCambios())
                {
                    bool confirm =
                        await ConfirmarSalidaSinGuardarAsync();

                    if (!confirm)
                        return;
                }

                await RegresarAsync();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "salir del formulario de rol",
                    ex);
            }
        }

        private async Task RegresarAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                await GoToAsyncParameters(AppRoutes.Roles);
            }
        }

        private bool ValidarCampos()
        {
            LimpiarErrores();

            NombreRol = NombreRol.Trim();
            DescripcionRol = DescripcionRol.Trim();

            if (string.IsNullOrWhiteSpace(NombreRol))
            {
                ErrorNombreRol =
                    "Ingrese el nombre del rol.";
            }

            if (NombreRol.Length > 50)
            {
                ErrorNombreRol =
                    "El nombre del rol no puede superar 50 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(DescripcionRol))
            {
                ErrorDescripcionRol =
                    "Ingrese la descripción del rol.";
            }

            if (DescripcionRol.Length > 500)
            {
                ErrorDescripcionRol =
                    "La descripción no puede superar 500 caracteres.";
            }

            return
                !TieneErrorNombreRol &&
                !TieneErrorDescripcionRol;
        }

        private bool HayCambios()
        {
            string nombreActual =
                NombreRol.Trim();

            string descripcionActual =
                DescripcionRol.Trim();

            string nombreOriginal =
                Rol.NombreRol?.Trim() ?? string.Empty;

            string descripcionOriginal =
                Rol.DescripcionRol?.Trim() ?? string.Empty;

            return
                nombreActual != nombreOriginal ||
                descripcionActual != descripcionOriginal;
        }

        private void SincronizarModelo()
        {
            Rol.NombreRol = NombreRol.Trim();
            Rol.DescripcionRol = DescripcionRol.Trim();
        }

        private void LimpiarErrores()
        {
            ErrorNombreRol = string.Empty;
            ErrorDescripcionRol = string.Empty;
        }

        private void NotificarModo()
        {
            OnPropertyChanged(nameof(ParametrosValidos));
            OnPropertyChanged(nameof(IsReadOnly));
            OnPropertyChanged(nameof(ShowSaveButton));
            OnPropertyChanged(nameof(EsRolProtegido));
            OnPropertyChanged(nameof(Title));
            RefrescarComandos();
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }

        private CancellationTokenSource PrepararOperacion()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref operacionCts,
                    source);

            if (anterior != null)
            {
                try
                {
                    anterior.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    anterior.Dispose();
                }
            }

            return source;
        }

        private void LiberarOperacion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref operacionCts,
                null,
                source);

            source.Dispose();
        }

        private static bool TryObtenerModo(
            IDictionary<string, object> query,
            out FormMode.FormModeSelect modo)
        {
            modo = FormMode.FormModeSelect.View;

            if (!query.TryGetValue("Mode", out object? valor) ||
                valor == null)
            {
                return false;
            }

            if (valor is FormMode.FormModeSelect tipado)
            {
                modo = tipado;
                return Enum.IsDefined(modo);
            }

            return Enum.TryParse(
                       valor.ToString(),
                       true,
                       out modo) &&
                   Enum.IsDefined(modo);
        }

        private static RolResponse? TryObtenerRol(
            IDictionary<string, object> query)
        {
            if (!query.TryGetValue("Rol", out object? valor) ||
                valor == null)
            {
                return null;
            }

            if (valor is RolResponse response)
                return ClonarRol(response);

            if (valor is RolRequest request &&
                request.RolId is > 0)
            {
                return new RolResponse
                {
                    RolId = request.RolId,
                    NombreRol = request.NombreRol,
                    DescripcionRol = request.DescripcionRol
                };
            }

            return null;
        }

        private static RolResponse ClonarRol(
            RolResponse rol) =>
            new()
            {
                RolId = rol.RolId,
                NombreRol = rol.NombreRol,
                DescripcionRol = rol.DescripcionRol,
                CantidadUsuarios = rol.CantidadUsuarios,
                CantidadInterfaces = rol.CantidadInterfaces
            };

        private static bool EsCancelacion(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
