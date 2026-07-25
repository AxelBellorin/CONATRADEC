using System;
namespace CONATRADEC.Models
{
    public sealed class InterfazRequest : Permiso
    {
        public int InterfazId { get; set; }

        /// <summary>
        /// Código interno utilizado para validar.
        /// </summary>
        public string NombreInterfaz { get; set; } =
            string.Empty;

        /// <summary>
        /// Nombre legible. Se envía por compatibilidad, aunque el guardado
        /// de la matriz se realiza mediante InterfazId.
        /// </summary>
        public string NombreAmigableInterfaz { get; set; } =
            string.Empty;

        public InterfazRequest()
        {
        }

        public InterfazRequest(
            InterfazResponse interfazResponse)
        {
            ArgumentNullException.ThrowIfNull(
                interfazResponse);

            InterfazId =
                interfazResponse.InterfazId;

            NombreInterfaz =
                interfazResponse.NombreInterfaz;

            NombreAmigableInterfaz =
                interfazResponse
                    .NombreAmigableInterfaz;

            Leer = interfazResponse.Leer;
            Agregar = interfazResponse.Agregar;
            Actualizar = interfazResponse.Actualizar;
            Eliminar = interfazResponse.Eliminar;
        }
    }
}
