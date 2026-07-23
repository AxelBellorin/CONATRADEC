namespace CONATRADEC.Services
{
    public static class PublicacionListadoEstadoService
    {
        public static bool HayActualizacionPendiente { get; private set; }

        public static void MarcarActualizacion()
        {
            HayActualizacionPendiente = true;
        }

        public static void ConfirmarActualizacion()
        {
            HayActualizacionPendiente = false;
        }
    }
}
