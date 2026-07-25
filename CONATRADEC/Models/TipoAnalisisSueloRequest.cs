using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class TipoAnalisisSueloRequest
    {
        [JsonIgnore]
        public int TipoAnalisisSueloId { get; set; }

        /*
         * El código se conserva únicamente para mostrarlo.
         * Nunca se envía en crear o editar.
         */
        [JsonIgnore]
        public string CodigoTipoAnalisisSuelo { get; set; } =
            string.Empty;

        [JsonIgnore]
        public bool EsTipoSistema { get; set; }

        public string NombreTipoAnalisisSuelo { get; set; } =
            string.Empty;

        public string DescripcionTipoAnalisisSuelo { get; set; } =
            string.Empty;

        public TipoAnalisisSueloRequest()
        {
        }

        public TipoAnalisisSueloRequest(
            TipoAnalisisSueloResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            TipoAnalisisSueloId =
                response.TipoAnalisisSueloId;

            CodigoTipoAnalisisSuelo =
                response.CodigoTipoAnalisisSuelo ??
                string.Empty;

            EsTipoSistema =
                response.EsTipoSistema;

            NombreTipoAnalisisSuelo =
                response.NombreTipoAnalisisSuelo ??
                string.Empty;

            DescripcionTipoAnalisisSuelo =
                response.DescripcionTipoAnalisisSuelo ??
                string.Empty;
        }
    }
}
