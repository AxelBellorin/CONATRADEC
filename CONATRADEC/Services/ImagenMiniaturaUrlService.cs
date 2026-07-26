namespace CONATRADEC.Services
{
    /// <summary>
    /// Construye una URL hacia el endpoint de miniaturas ya disponible en la
    /// API. Si la miniatura ya fue guardada en AppDataDirectory, devuelve la
    /// ruta local y evita una nueva solicitud de imagen.
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

            if (!valor.StartsWith(
                    "http",
                    StringComparison.OrdinalIgnoreCase))
            {
                return valor;
            }

            string miniaturaUrl;

            if (valor.Contains(
                    "/imagenes/miniatura",
                    StringComparison.OrdinalIgnoreCase))
            {
                miniaturaUrl = valor;
            }
            else
            {
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
                    new UrlApiService()
                        .BaseUrlApi
                        .TrimEnd('/');

                miniaturaUrl =
                    $"{baseUrl}/imagenes/miniatura" +
                    $"?ruta={Uri.EscapeDataString(ruta)}" +
                    $"&ancho={Math.Clamp(ancho, 120, 1200)}" +
                    $"&alto={Math.Clamp(alto, 120, 1200)}" +
                    $"&calidad={Math.Clamp(calidad, 45, 85)}";
            }

            return ImagenLocalCacheService
                .ResolverMiniatura(
                    miniaturaUrl);
        }
    }
}
