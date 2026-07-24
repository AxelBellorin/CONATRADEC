namespace CONATRADEC.Services
{
    /// <summary>
    /// Construye una URL hacia el endpoint de miniaturas ya disponible en la
    /// API. Las tarjetas descargan una imagen pequeña y el detalle conserva la
    /// imagen completa.
    /// </summary>
    public static class ImagenMiniaturaUrlService
    {
        public static string Crear(
            string? rutaOUrl,
            int ancho,
            int alto,
            int calidad)
        {
            if (string.IsNullOrWhiteSpace(rutaOUrl))
                return string.Empty;

            string valor = rutaOUrl.Trim();

            if (valor.Contains(
                    "/imagenes/miniatura",
                    StringComparison.OrdinalIgnoreCase))
            {
                return valor;
            }

            string ruta = valor;

            if (Uri.TryCreate(
                    valor,
                    UriKind.Absolute,
                    out Uri? uriAbsoluta))
            {
                ruta = uriAbsoluta.AbsolutePath;
            }

            if (!ruta.StartsWith('/'))
                ruta = "/" + ruta;

            string baseUrl =
                new UrlApiService().BaseUrlApi.TrimEnd('/');

            return
                $"{baseUrl}/imagenes/miniatura" +
                $"?ruta={Uri.EscapeDataString(ruta)}" +
                $"&ancho={Math.Clamp(ancho, 120, 1200)}" +
                $"&alto={Math.Clamp(alto, 120, 1200)}" +
                $"&calidad={Math.Clamp(calidad, 45, 85)}";
        }
    }
}
