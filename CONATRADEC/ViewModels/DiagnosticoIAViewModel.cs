using System.Runtime.CompilerServices;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;

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

        /*
         * La numeración y posición visual se calculan únicamente con las
         * opciones que el usuario realmente puede ver. Así un aprobador que no
         * tenga las opciones previas verá 01 y la tarjeta ocupará la primera
         * posición disponible, también en Windows.
         */
        public string NumeroNuevaInspeccion => NumeroDe(0);
        public string NumeroMisInspecciones => NumeroDe(1);
        public string NumeroAnalizador => NumeroDe(2);
        public string NumeroAprobador => NumeroDe(3);
        public string NumeroHistorial => NumeroDe(4);

        public int FilaNuevaInspeccion => FilaDe(0);
        public int FilaMisInspecciones => FilaDe(1);
        public int FilaAnalizador => FilaDe(2);
        public int FilaAprobador => FilaDe(3);
        public int FilaHistorial => FilaDe(4);

        public int ColumnaNuevaInspeccion => ColumnaDe(0);
        public int ColumnaMisInspecciones => ColumnaDe(1);
        public int ColumnaAnalizador => ColumnaDe(2);
        public int ColumnaAprobador => ColumnaDe(3);
        public int ColumnaHistorial => ColumnaDe(4);

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

            /*
             * La captura de campo y la cola local permanecen disponibles sin
             * conexión. Las etapas humanas posteriores necesitan el servidor
             * para respetar asignaciones, bloqueos exclusivos y auditoría.
             */
            bool enLinea = ModoSesionService.EsEnLinea;
            PuedeHistorial = puedeLeerSolicitud && enLinea;
            PuedeAnalizador = enLinea && PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazAnalizador);
            PuedeAprobador = enLinea && PermissionService.Instance.HasRead(
                DiagnosticoIARoutes.InterfazAprobador);

            if (enLinea && puedeLeerSolicitud)
            {
                FitosanitariaOfflineService.Instance
                    .SolicitarSincronizacionEnSegundoPlano();
            }

            ActualizarComandos();
            NotificarPresentacionOpciones();
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
            NotificarPresentacionOpciones();
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

        private bool OpcionVisible(int indice) => indice switch
        {
            0 => PuedeNuevaInspeccion,
            1 => PuedeMisInspecciones,
            2 => PuedeAnalizador,
            3 => PuedeAprobador,
            4 => PuedeHistorial,
            _ => false
        };

        private int PosicionVisible(int indice)
        {
            int posicion = 0;
            for (int actual = 0; actual <= indice; actual++)
            {
                if (OpcionVisible(actual))
                    posicion++;
            }

            return Math.Max(posicion - 1, 0);
        }

        private string NumeroDe(int indice) =>
            OpcionVisible(indice)
                ? (PosicionVisible(indice) + 1).ToString("00")
                : string.Empty;

        private int FilaDe(int indice)
        {
            int posicion = PosicionVisible(indice);
            return DeviceInfo.Platform == DevicePlatform.WinUI
                ? posicion / 2
                : posicion;
        }

        private int ColumnaDe(int indice)
        {
            if (DeviceInfo.Platform != DevicePlatform.WinUI)
                return 0;

            return PosicionVisible(indice) % 2;
        }

        private void NotificarPresentacionOpciones()
        {
            OnPropertyChanged(nameof(NumeroNuevaInspeccion));
            OnPropertyChanged(nameof(NumeroMisInspecciones));
            OnPropertyChanged(nameof(NumeroAnalizador));
            OnPropertyChanged(nameof(NumeroAprobador));
            OnPropertyChanged(nameof(NumeroHistorial));

            OnPropertyChanged(nameof(FilaNuevaInspeccion));
            OnPropertyChanged(nameof(FilaMisInspecciones));
            OnPropertyChanged(nameof(FilaAnalizador));
            OnPropertyChanged(nameof(FilaAprobador));
            OnPropertyChanged(nameof(FilaHistorial));

            OnPropertyChanged(nameof(ColumnaNuevaInspeccion));
            OnPropertyChanged(nameof(ColumnaMisInspecciones));
            OnPropertyChanged(nameof(ColumnaAnalizador));
            OnPropertyChanged(nameof(ColumnaAprobador));
            OnPropertyChanged(nameof(ColumnaHistorial));
        }
    }
}
