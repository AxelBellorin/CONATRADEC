namespace CONATRADEC.Models
{
    /// <summary>
    /// Define el origen de datos para toda la sesión.
    /// El valor no cambia automáticamente por el estado de la red.
    /// </summary>
    public enum ModoSesion
    {
        EnLinea = 0,
        SinConexion = 1
    }

    public sealed class ModoSesionEventArgs : EventArgs
    {
        public ModoSesion Modo { get; }

        public ModoSesionEventArgs(ModoSesion modo)
        {
            Modo = modo;
        }
    }
}
