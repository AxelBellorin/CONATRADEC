using System.Reflection;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Limpia los cachés estáticos de formularios después de una
    /// reactivación directa. Se utiliza reflexión para no cambiar
    /// las firmas públicas de todos los servicios existentes.
    /// </summary>
    internal static class CatalogoCacheInvalidator
    {
        private const BindingFlags Flags =
            BindingFlags.Static |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        public static void Limpiar(string catalogo)
        {
            try
            {
                switch (catalogo)
                {
                    case CatalogoEliminadoCodigos.Pais:
                        LimpiarCampos(
                            typeof(PaisApiService),
                            "cacheFormulario",
                            "cacheCreadoUtc");

                        // La lista activa cambia de composición tras reactivar.
                        UbicacionVisitaService.MarcarPaisesParaRecargar();
                        break;

                    case CatalogoEliminadoCodigos.Departamento:
                        LimpiarColeccion(
                            typeof(DepartamentoApiService),
                            "CachePorPais");

                        // La reactivación cambia tanto la lista del país actual
                        // como el conteo mostrado en la tarjeta de País.
                        UbicacionVisitaService.MarcarDepartamentosParaRecargar();
                        UbicacionVisitaService.MarcarPaisesParaRecargar();
                        break;

                    case CatalogoEliminadoCodigos.Municipio:
                        LimpiarColeccion(
                            typeof(MunicipioApiService),
                            "CachePorDepartamento");

                        // También cambia el conteo de municipios del padre.
                        UbicacionVisitaService.MarcarMunicipiosParaRecargar();
                        UbicacionVisitaService.MarcarDepartamentosParaRecargar();
                        break;

                    case CatalogoEliminadoCodigos.ElementoQuimico:
                        LimpiarCampos(
                            typeof(ElementoQuimicoApiService),
                            "cacheFormulario",
                            "cacheCreadoUtc");

                        /*
                         * La reactivación desde el modal común cambia la
                         * composición del listado activo. Se incrementa la
                         * versión para que, al cerrar el modal, se renueve la
                         * página visible dentro de la misma visita.
                         */
                        ElementoQuimicoListadoEstadoService
                            .MarcarParaRecargar();
                        break;

                    case CatalogoEliminadoCodigos.TipoCultivo:
                        LimpiarCampos(
                            typeof(TipoCultivoApiService),
                            "cacheFormulario",
                            "cacheCreadoUtc");

                        AnalisisSueloApiService.LimpiarCacheTiposCultivo();

                        /*
                         * La reactivación se realiza desde el modal común de
                         * eliminados. Se marca el listado administrativo para
                         * que, al cerrar el modal, renueve la página visible.
                         */
                        TipoCultivoListadoEstadoService.MarcarCambio();
                        break;

                    case CatalogoEliminadoCodigos.TipoAnalisis:
                        LimpiarCampos(
                            typeof(TipoAnalisisSueloApiService),
                            "cacheFormulario",
                            "cacheCreadoUtc");
                        break;

                    case CatalogoEliminadoCodigos.CategoriaPublicacion:
                        PublicacionListadoEstadoService.MarcarActualizacion();
                        break;
                }
            }
            catch
            {
                /*
                 * La reactivación ya fue confirmada por la API.
                 * Un fallo al limpiar caché nunca debe marcarla como fallida;
                 * la próxima expiración o recarga volverá a consultar.
                 */
            }
        }

        private static void LimpiarCampos(
            Type tipo,
            params string[] campos)
        {
            foreach (string nombre in campos)
            {
                FieldInfo? campo = tipo.GetField(nombre, Flags);

                if (campo == null)
                    continue;

                object? valor = campo.FieldType.IsValueType
                    ? Activator.CreateInstance(campo.FieldType)
                    : null;

                campo.SetValue(null, valor);
            }
        }

        private static void LimpiarColeccion(
            Type tipo,
            string campoNombre)
        {
            object? coleccion = tipo.GetField(campoNombre, Flags)?.GetValue(null);

            coleccion?
                .GetType()
                .GetMethod(
                    "Clear",
                    BindingFlags.Instance | BindingFlags.Public)
                ?.Invoke(coleccion, null);
        }
    }
}
