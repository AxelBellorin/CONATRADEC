namespace CONATRADEC.Models
{
    public class TipoCultivoResponse
    {
        public int TipoCultivoId { get; set; }

        public string? NombreTipoCultivo { get; set; }

        // Se conserva por compatibilidad con las pantallas de análisis
        // que anteriormente recibían el nombre en la propiedad "tipoCultivo".
        public string? TipoCultivo { get; set; }

        public string? DescripcionTipoCultivo { get; set; }

        public bool Activo { get; set; }

        // Propiedad utilizada por los Picker existentes del proyecto.
        // Prioriza TipoCultivo cuando la API antigua lo devuelve y, en caso
        // contrario, usa NombreTipoCultivo del nuevo CRUD de configuración.
        public string NombreMostrar =>
            !string.IsNullOrWhiteSpace(TipoCultivo)
                ? TipoCultivo.Trim()
                : NombreTipoCultivo?.Trim() ?? string.Empty;
    }
}
