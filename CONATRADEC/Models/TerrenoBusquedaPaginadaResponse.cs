using System.Collections.Generic;

namespace CONATRADEC.Models
{
    public class TerrenoBusquedaPaginadaResponse
    {
        public int Total { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public List<TerrenoResponse> Data { get; set; } = new();
    }
}