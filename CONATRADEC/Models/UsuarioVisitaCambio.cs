namespace CONATRADEC.Models
{
    public enum UsuarioVisitaCambioTipo
    {
        Creado,
        Actualizado
    }

    /// <summary>
    /// Cambio confirmado por el servidor que debe reflejarse en la página
    /// actualmente conservada en memoria sin volver a consultar el listado.
    /// </summary>
    public sealed class UsuarioVisitaCambio
    {
        public UsuarioVisitaCambioTipo Tipo { get; init; }
        public UserResponse Usuario { get; init; } = new();
    }
}
