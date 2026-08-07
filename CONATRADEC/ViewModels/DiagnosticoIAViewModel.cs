using System.Runtime.CompilerServices;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Panel principal del módulo operativo de inspección fitosanitaria.
    /// Las decisiones técnicas forman parte de Mis inspecciones y ya no se
    /// exponen como un módulo independiente.
    /// </summary>
    public sealed class DiagnosticoIAViewModel : GlobalService
    {
        private bool puedeNuevaInspeccion;
        private bool puedeMisInspecciones;
        private bool puedeAnalizador;
        private bool puedeAprobador;
        private bool puedeHistorial;

        public DiagnosticoIAViewModel()
        {
            RegresarCommand = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Principal),
                () => !IsBusy);

            NuevaInspeccionCommand = CrearComandoModo(
                DiagnosticoIARoutes.ModoNuevaInspeccion,
                () => PuedeNuevaInspeccion);

            MisInspeccionesCommand = CrearComandoModo(
                DiagnosticoIARoutes.ModoMisInspecciones,
                () => PuedeMisInspecciones);

            HistorialCommand = CrearComandoModo(
                DiagnosticoIARoutes.ModoHistorial,
                () => PuedeHistorial);

            AbrirAnalizadorCommand = new Command(
                async () => await NavegarAsync(
                    AppRoutes.DiagnosticoIAAnalizador),
                () => PuedeAnalizador && !IsBusy);

            AbrirAprobadorCommand = new Command(
                async () => await NavegarAsync(
                    AppRoutes.DiagnosticoIAAprobador),
                () => PuedeAprobador && !IsBusy);
        }

        public Command RegresarCommand { get; }
        public Command NuevaInspeccionCommand { get; }
        public Command MisInspeccionesCommand { get; }
        public Command AbrirAnalizadorCommand { get; }
        public Command AbrirAprobadorCommand { get; }
        public Command HistorialCommand { get; }

        public bool PuedeNuevaInspeccion
        {
            get => puedeNuevaInspeccion;
            private set => Asignar(ref puedeNuevaInspeccion, value);
        }

        public bool PuedeMisInspecciones
        {
            get => puedeMisInspecciones;
            private set => Asignar(ref puedeMisInspecciones, value);
        }

        public bool PuedeAnalizador
        {
            get => puedeAnalizador;
            private set => Asignar(ref puedeAnalizador, value);
        }

        public bool PuedeAprobador
        {
            get => puedeAprobador;
            private set => Asignar(ref puedeAprobador, value);
        }

        public bool PuedeHistorial
        {
            get => puedeHistorial;
            private set => Asignar(ref puedeHistorial, value);
        }

        public bool TieneAlgunaOpcion =>
            PuedeNuevaInspeccion ||
            PuedeMisInspecciones ||
            PuedeAnalizador ||
            PuedeAprobador ||
            PuedeHistorial;

        public bool SinOpciones => !TieneAlgunaOpcion;

        public Task InicializarAsync()
        {
            DiagnosticoIARoutes.AsegurarRegistro();

            bool puedeLeerSolicitud = PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazSolicitud);

            PuedeNuevaInspeccion =
                puedeLeerSolicitud &&
                PermissionService.Instance.HasAdd(
                    DiagnosticoIARoutes.InterfazSolicitud);

            PuedeMisInspecciones = puedeLeerSolicitud;
            PuedeHistorial = puedeLeerSolicitud;
            PuedeAnalizador = PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazAnalizador);
            PuedeAprobador = PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazAprobador);

            ActualizarComandos();
            return Task.CompletedTask;
        }

        private Command CrearComandoModo(
            string modo,
            Func<bool> puedeEjecutar) =>
            new(
                async () => await NavegarAsync(
                    DiagnosticoIARoutes.CrearRutaSolicitud(modo)),
                () => puedeEjecutar() && !IsBusy);

        private void Asignar(
            ref bool campo,
            bool valor,
            [CallerMemberName] string? nombrePropiedad = null)
        {
            if (campo == valor)
                return;

            campo = valor;
            OnPropertyChanged(nombrePropiedad);
            OnPropertyChanged(nameof(TieneAlgunaOpcion));
            OnPropertyChanged(nameof(SinOpciones));
        }

        private async Task NavegarAsync(string ruta)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ActualizarComandos();

            try
            {
                DiagnosticoIARoutes.AsegurarRegistro();
                await GoToAsyncParameters(ruta);
            }
            finally
            {
                IsBusy = false;
                ActualizarComandos();
            }
        }

        private void ActualizarComandos()
        {
            RegresarCommand.ChangeCanExecute();
            NuevaInspeccionCommand.ChangeCanExecute();
            MisInspeccionesCommand.ChangeCanExecute();
            AbrirAnalizadorCommand.ChangeCanExecute();
            AbrirAprobadorCommand.ChangeCanExecute();
            HistorialCommand.ChangeCanExecute();
        }
    }
}
