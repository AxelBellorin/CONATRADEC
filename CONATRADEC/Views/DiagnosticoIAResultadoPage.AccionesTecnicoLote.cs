using CONATRADEC.Controls;
using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Globalization;
using System.Text;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Presentación de las acciones masivas del técnico.
    ///
    /// La selección es global, pero cada botón trabaja únicamente con el
    /// subconjunto de fotografías que cumple las reglas de su acción. De esta
    /// forma una fotografía pendiente de IA y otra lista para enviar pueden
    /// permanecer seleccionadas al mismo tiempo sin bloquearse entre sí.
    ///
    /// En esta misma capa se presenta, para la vista del técnico, una
    /// clasificación independiente por cada diagnóstico. La fuente principal
    /// es la persistencia fitosanitaria del backend y el catálogo local queda
    /// como respaldo compatible para instalaciones históricas.
    /// </summary>
    public partial class DiagnosticoIAResultadoPage
    {
        private const string ClassIdClasificacionesMultiplesAlbum =
            "ClasificacionesMultiplesAlbum";

        private readonly InspeccionClasificacionDiagnosticoApiService
            clasificacionesDiagnosticoApi = new();

        private readonly AlbumJerarquiaApiService
            clasificacionesMultiplesAlbumApi = new();

        private readonly Dictionary<int,
            List<InspeccionClasificacionDiagnosticoV2>>
            clasificacionesPersistidasPorFoto = [];

        private readonly List<AlbumRegistroJerarquiaResponse>
            catalogoClasificacionesMultiples = [];

        private Button? procesarSeleccionIAButton;
        private Button? enviarSeleccionAnalizadorButton;
        private bool accionesTecnicoLoteIntegradas;

        private bool cargandoCatalogoClasificacionesMultiples;
        private bool catalogoClasificacionesMultiplesCargado;
        private bool catalogoClasificacionesMultiplesDisponible;
        private bool clasificacionesPersistidasCargadas;
        private string firmaClasificacionesMultiples = string.Empty;

        private void IntegrarAccionesTecnicoLote()
        {
            if (accionesTecnicoLoteIntegradas)
                return;

            procesarSeleccionIAButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    item =>
                        ReferenceEquals(
                            item.Command,
                            viewModel.ProcesarSeleccionCommand) ||
                        string.Equals(
                            item.Text,
                            "Analizar con IA",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            item.Text,
                            "Procesar selección con IA",
                            StringComparison.Ordinal));

            enviarSeleccionAnalizadorButton ??=
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    item =>
                        ReferenceEquals(
                            item.Command,
                            viewModel.EnviarAnalizadorCommand) ||
                        string.Equals(
                            item.Text,
                            "Enviar al analizador",
                            StringComparison.Ordinal));

            if (procesarSeleccionIAButton == null ||
                enviarSeleccionAnalizadorButton == null)
            {
                return;
            }

            /*
             * El XAML conserva los MultiBinding históricos para compatibilidad
             * visual con versiones anteriores. En esta pantalla actual la
             * autoridad para las acciones del técnico es el ViewModel, porque
             * una selección puede contener estados diferentes.
             */
            procesarSeleccionIAButton.RemoveBinding(
                VisualElement.IsVisibleProperty);
            enviarSeleccionAnalizadorButton.RemoveBinding(
                VisualElement.IsVisibleProperty);

            accionesTecnicoLoteIntegradas = true;
        }

        private void ActualizarAccionesTecnicoLote()
        {
            IntegrarAccionesTecnicoLote();

            bool vistaTecnico = EsVistaTecnicoClasificacionesMultiples();

            if (procesarSeleccionIAButton != null)
            {
                procesarSeleccionIAButton.Text =
                    viewModel.TextoBotonProcesarIA;
                procesarSeleccionIAButton.IsVisible =
                    vistaTecnico &&
                    viewModel.PuedeProcesarSeleccion;
                procesarSeleccionIAButton.IsEnabled =
                    procesarSeleccionIAButton.IsVisible &&
                    !viewModel.IsBusy;
            }

            if (enviarSeleccionAnalizadorButton != null)
            {
                enviarSeleccionAnalizadorButton.Text =
                    viewModel.TextoBotonEnviarAnalizador;
                enviarSeleccionAnalizadorButton.IsVisible =
                    vistaTecnico &&
                    viewModel.PuedeEnviarSeleccion;
                enviarSeleccionAnalizadorButton.IsEnabled =
                    enviarSeleccionAnalizadorButton.IsVisible &&
                    !viewModel.IsBusy;
            }

            ActualizarTextosSeguimientoIA();
            ProgramarClasificacionesMultiplesAlbum();
        }

        /// <summary>
        /// Evita que "0 reevaluaciones" parezca significar que el análisis
        /// inicial nunca se ejecutó. Cuando ya existe ResultadoIA se informa de
        /// forma explícita que el análisis inicial está completado y el contador
        /// se identifica como reevaluaciones adicionales.
        /// </summary>
        private void ActualizarTextosSeguimientoIA()
        {
            foreach (Label label in
                     ResponsiveLayoutUtility.FindDescendants<Label>(this))
            {
                if (label.BindingContext is not InspeccionFotoV2 foto ||
                    foto.ResultadoIA == null)
                {
                    continue;
                }

                string textoActual = label.Text?.Trim() ?? string.Empty;

                if (!textoActual.StartsWith(
                        "Reevaluaciones IA",
                        StringComparison.OrdinalIgnoreCase) &&
                    !textoActual.StartsWith(
                        "Análisis inicial completado",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                label.RemoveBinding(Label.TextProperty);
                label.Text = CrearTextoSeguimientoIA(foto);
            }
        }

        private static string CrearTextoSeguimientoIA(
            InspeccionFotoV2 foto)
        {
            int reevaluaciones = Math.Max(
                0,
                foto.RevisionesIACompletadas);

            if (foto.RevisionesIAIlimitadas)
            {
                return
                    $"Análisis inicial completado · " +
                    $"Reevaluaciones adicionales: {reevaluaciones} · sin límite";
            }

            int maximo = Math.Max(1, foto.MaximoRevisionesIA);

            return
                $"Análisis inicial completado · " +
                $"Reevaluaciones adicionales: {reevaluaciones} de {maximo}";
        }

        /// <summary>
        /// Las clasificaciones múltiples de esta tarjeta pertenecen a la vista
        /// del técnico. El backend conserva la relación por diagnóstico para que
        /// las etapas posteriores del mismo módulo puedan reutilizarla.
        /// </summary>
        private bool EsVistaTecnicoClasificacionesMultiples() =>
            string.Equals(
                viewModel.TextoRegresar,
                "Mis inspecciones",
                StringComparison.OrdinalIgnoreCase);

        private void ProgramarClasificacionesMultiplesAlbum()
        {
            /*
             * La sincronización se ejecuta en cualquier etapa del propio
             * módulo fitosanitario. La presentación múltiple de esta tarjeta
             * sigue limitada a la vista del técnico, pero el backend debe
             * enterarse también de correcciones del analizador y del aprobador.
             */
            bool existenDiagnosticos = viewModel.Fotografias.Any(item =>
                (item.UltimaAprobacion?.DiagnosticosFinales?.Count ?? 0) > 0 ||
                (item.UltimoAnalisisHumano?.Diagnosticos?.Count ?? 0) > 0 ||
                (item.ResultadoIA?.Diagnosticos?.Count ?? 0) > 0);

            if (!existenDiagnosticos)
                return;

            string firmaActual = CrearFirmaClasificacionesMultiples();
            if (!string.Equals(
                    firmaActual,
                    firmaClasificacionesMultiples,
                    StringComparison.Ordinal))
            {
                firmaClasificacionesMultiples = firmaActual;
                clasificacionesPersistidasCargadas = false;
                catalogoClasificacionesMultiplesCargado = false;
                clasificacionesPersistidasPorFoto.Clear();
            }

            if (catalogoClasificacionesMultiplesCargado)
            {
                Dispatcher.Dispatch(RenderizarClasificacionesMultiplesAlbum);
                _ = ReintentarRenderClasificacionesMultiplesAlbumAsync();
                return;
            }

            if (cargandoCatalogoClasificacionesMultiples)
                return;

            _ = CargarCatalogoClasificacionesMultiplesAsync();
        }

        private string CrearFirmaClasificacionesMultiples()
        {
            return string.Join(
                "|",
                viewModel.Fotografias
                    .Where(item =>
                        (item.UltimaAprobacion?.DiagnosticosFinales?.Count ?? 0) > 0 ||
                        (item.UltimoAnalisisHumano?.Diagnosticos?.Count ?? 0) > 0 ||
                        (item.ResultadoIA?.Diagnosticos?.Count ?? 0) > 0)
                    .OrderBy(item => item.FotografiaId)
                    .Select(item =>
                    {
                        IEnumerable<InspeccionDiagnosticoVisualV2> vigentes =
                            item.UltimaAprobacion?.DiagnosticosFinales?.Count > 0
                                ? item.UltimaAprobacion.DiagnosticosFinales
                                : item.UltimoAnalisisHumano?.Diagnosticos?.Count > 0
                                    ? item.UltimoAnalisisHumano.Diagnosticos
                                    : item.ResultadoIA?.Diagnosticos ?? [];

                        string diagnosticos = string.Join(
                            ",",
                            vigentes.Select(diag =>
                                $"{diag.IdOrigenIA}:{diag.Id}:{diag.Diagnostico}:" +
                                $"{diag.EsPrincipal}:{diag.AccionHumana}"));

                        return
                            $"{item.FotografiaId}:" +
                            $"{item.ResultadoIA?.VersionVisual ?? 0}:" +
                            $"{item.UltimoAnalisisHumano?.Version ?? 0}:" +
                            $"{item.UltimaAprobacion?.AprobacionId ?? 0}:" +
                            diagnosticos;
                    }));
        }

        private async Task CargarCatalogoClasificacionesMultiplesAsync()
        {
            if (cargandoCatalogoClasificacionesMultiples)
                return;

            cargandoCatalogoClasificacionesMultiples = true;

            try
            {
                int inspeccionId = viewModel.Detalle?.InspeccionId ?? 0;

                /*
                 * Fuente principal: persistencia del propio módulo
                 * fitosanitario. El backend sincroniza IA -> analizador ->
                 * aprobador y conserva una fila independiente por diagnóstico.
                 */
                if (inspeccionId > 0)
                {
                    ApiResult<List<InspeccionClasificacionDiagnosticoV2>>
                        persistidas = await clasificacionesDiagnosticoApi
                            .ObtenerAsync(inspeccionId);

                    clasificacionesPersistidasPorFoto.Clear();

                    if (persistidas.Success && persistidas.Data != null)
                    {
                        foreach (IGrouping<int,
                                     InspeccionClasificacionDiagnosticoV2>
                                 grupo in persistidas.Data
                                     .Where(item => item.Activo)
                                     .GroupBy(item => item.FotografiaId))
                        {
                            clasificacionesPersistidasPorFoto[grupo.Key] =
                                grupo
                                    .OrderByDescending(item => item.EsPrincipal)
                                    .ThenBy(item => item.OrdenDiagnostico)
                                    .ToList();
                        }

                        clasificacionesPersistidasCargadas = true;
                    }
                }

                /*
                 * Respaldo compatible: si el backend aún no tiene la ampliación
                 * o alguna fotografía histórica no pudo sincronizarse, se usa
                 * el catálogo activo para construir solo la presentación local.
                 */
                ApiResult<List<AlbumRegistroJerarquiaResponse>> resultado =
                    await clasificacionesMultiplesAlbumApi
                        .GetJerarquiaRegistrosAsync(
                            incluirInactivos: false);

                catalogoClasificacionesMultiples.Clear();

                if (resultado.Success && resultado.Data != null)
                {
                    catalogoClasificacionesMultiples.AddRange(
                        resultado.Data
                            .Where(item => item.Activo)
                            .OrderBy(item => item.Categoria)
                            .ThenBy(item => item.Titulo));

                    catalogoClasificacionesMultiplesDisponible = true;
                    catalogoClasificacionesMultiplesCargado = true;
                }
                else
                {
                    catalogoClasificacionesMultiplesDisponible = false;
                    catalogoClasificacionesMultiplesCargado =
                        clasificacionesPersistidasCargadas;
                }
            }
            catch
            {
                catalogoClasificacionesMultiplesDisponible = false;
                catalogoClasificacionesMultiplesCargado =
                    clasificacionesPersistidasCargadas;
            }
            finally
            {
                cargandoCatalogoClasificacionesMultiples = false;
                Dispatcher.Dispatch(RenderizarClasificacionesMultiplesAlbum);
                _ = ReintentarRenderClasificacionesMultiplesAlbumAsync();
            }
        }

        private async Task ReintentarRenderClasificacionesMultiplesAlbumAsync()
        {
            /*
             * BindableLayout crea las tarjetas después de actualizar la
             * colección. Este reintento breve garantiza que la presentación
             * múltiple se aplique aunque el primer render ocurra antes de que
             * la tarjeta visual haya sido materializada.
             */
            await Task.Delay(140);
            Dispatcher.Dispatch(RenderizarClasificacionesMultiplesAlbum);
        }

        private void RenderizarClasificacionesMultiplesAlbum()
        {
            if (!EsVistaTecnicoClasificacionesMultiples())
                return;

            foreach (InspeccionFotoV2 foto in viewModel.Fotografias)
            {
                if (foto.ResultadoIA?.Diagnosticos?.Count is not > 1)
                    continue;

                VerticalStackLayout? contenedor =
                    BuscarContenedorClasificacionAlbum(foto);

                if (contenedor == null)
                    continue;

                List<ClasificacionDiagnosticoAlbumPresentacion> propuestas =
                    ConstruirClasificacionesMultiples(foto);

                if (propuestas.Count <= 1)
                    continue;

                contenedor.Children.Clear();
                contenedor.Children.Add(
                    CrearPanelClasificacionesMultiples(propuestas));
            }
        }

        /// <summary>
        /// Encuentra únicamente el contenedor de clasificación del Álbum dentro
        /// de la tarjeta de una fotografía. No toca otros bloques del expediente.
        /// </summary>
        private VerticalStackLayout? BuscarContenedorClasificacionAlbum(
            InspeccionFotoV2 foto)
        {
            foreach (VerticalStackLayout stack in
                     ResponsiveLayoutUtility
                         .FindDescendants<VerticalStackLayout>(this))
            {
                if (!ReferenceEquals(stack.BindingContext, foto))
                    continue;

                foreach (Border borde in stack.Children.OfType<Border>())
                {
                    if (string.Equals(
                            borde.ClassId,
                            ClassIdClasificacionesMultiplesAlbum,
                            StringComparison.Ordinal))
                    {
                        return stack;
                    }

                    if (EsPanelHistoricoAlbum(borde))
                        return stack;
                }
            }

            return null;
        }

        private static bool EsPanelHistoricoAlbum(Border borde)
        {
            foreach (Label label in
                     ResponsiveLayoutUtility.FindDescendants<Label>(borde))
            {
                string texto = label.Text?.Trim() ?? string.Empty;

                if (texto.StartsWith(
                        "Clasificación asignada al Álbum Botánico",
                        StringComparison.OrdinalIgnoreCase) ||
                    texto.StartsWith(
                        "Clasificación propuesta para el Álbum Botánico",
                        StringComparison.OrdinalIgnoreCase) ||
                    texto.StartsWith(
                        "Clasificaciones propuestas para el Álbum Botánico",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private List<ClasificacionDiagnosticoAlbumPresentacion>
            ConstruirClasificacionesMultiples(InspeccionFotoV2 foto)
        {
            if (clasificacionesPersistidasPorFoto.TryGetValue(
                    foto.FotografiaId,
                    out List<InspeccionClasificacionDiagnosticoV2>?
                        persistidas) &&
                persistidas.Count > 0)
            {
                return persistidas
                    .Where(item => !item.EstaDescartada)
                    .OrderByDescending(item => item.EsPrincipal)
                    .ThenBy(item => item.OrdenDiagnostico)
                    .Select(MapearClasificacionPersistida)
                    .ToList();
            }

            InspeccionFotoResultadoIAV2? resultado = foto.ResultadoIA;
            if (resultado == null)
                return [];

            List<(InspeccionDiagnosticoVisualV2 Diagnostico, int Indice)>
                diagnosticos = (resultado.Diagnosticos ?? [])
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(item.Diagnostico))
                    .Select((diagnostico, indice) =>
                        (Diagnostico: diagnostico, Indice: indice))
                    .OrderByDescending(item =>
                        item.Diagnostico.EsPrincipal)
                    .ThenBy(item => item.Indice)
                    .ToList();

            if (diagnosticos.Count == 0)
                return [];

            bool existePrincipal = diagnosticos.Any(item =>
                item.Diagnostico.EsPrincipal);

            var propuestas =
                new List<ClasificacionDiagnosticoAlbumPresentacion>();

            for (int i = 0; i < diagnosticos.Count; i++)
            {
                InspeccionDiagnosticoVisualV2 diagnostico =
                    diagnosticos[i].Diagnostico;

                bool esPrincipal =
                    diagnostico.EsPrincipal ||
                    (!existePrincipal && i == 0);

                propuestas.Add(
                    ConstruirClasificacionDiagnostico(
                        foto,
                        diagnostico,
                        esPrincipal,
                        i + 1));
            }

            return propuestas;
        }

        private static ClasificacionDiagnosticoAlbumPresentacion
            MapearClasificacionPersistida(
                InspeccionClasificacionDiagnosticoV2 item)
        {
            bool categoriaExiste =
                item.CategoriaAlbumBotanicoIdSeleccionada is > 0 ||
                item.CategoriaAlbumBotanicoIdSugerida is > 0;

            bool subcategoriaExiste =
                item.AlbumBotanicoCafeIdSeleccionado is > 0 ||
                item.AlbumBotanicoCafeIdSugerido is > 0;

            string motivo = item.FuenteVigente switch
            {
                "APROBACION" =>
                    "Clasificación sincronizada con la decisión vigente del aprobador.",
                "ANALIZADOR" =>
                    "Clasificación sincronizada con el diagnóstico humano vigente.",
                _ when item.CoincideCatalogo =>
                    "Coincidencia persistida con una subcategoría activa del catálogo.",
                _ =>
                    "La afectación está persistida como propuesta y requiere revisión posterior."
            };

            return new ClasificacionDiagnosticoAlbumPresentacion
            {
                Orden = Math.Max(1, item.OrdenDiagnostico),
                EsPrincipal = item.EsPrincipal,
                Rol = item.Rol,
                Diagnostico = item.Diagnostico,
                Categoria = string.IsNullOrWhiteSpace(item.CategoriaMostrar)
                    ? "Clasificación pendiente"
                    : item.CategoriaMostrar,
                Subcategoria = string.IsNullOrWhiteSpace(item.SubcategoriaMostrar)
                    ? "Subcategoría por definir"
                    : item.SubcategoriaMostrar,
                NombreCientifico = item.NombreCientificoSugerido,
                CategoriaExiste = categoriaExiste,
                SubcategoriaExiste = subcategoriaExiste,
                CatalogoDisponible = true,
                Motivo = motivo
            };
        }

        private ClasificacionDiagnosticoAlbumPresentacion
            ConstruirClasificacionDiagnostico(
                InspeccionFotoV2 foto,
                InspeccionDiagnosticoVisualV2 diagnostico,
                bool esPrincipal,
                int orden)
        {
            InspeccionFotoResultadoIAV2 resultado = foto.ResultadoIA!;
            JerarquiaDiagnosticoFotoResponse? jerarquia =
                esPrincipal
                    ? foto.JerarquiaAlbum
                    : null;

            string nombreDiagnostico =
                LimpiarNombreDiagnostico(diagnostico.Diagnostico);

            /*
             * La jerarquía histórica de la fotografía continúa representando
             * el diagnóstico principal. Para ese diagnóstico se conserva su
             * coincidencia exacta y su estado actual.
             */
            if (jerarquia?.TieneClasificacion == true)
            {
                string categoriaJerarquia = PrimerTexto(
                    jerarquia.Categoria,
                    MapearCategoriaDiagnostico(diagnostico, resultado));

                string subcategoriaJerarquia = PrimerTexto(
                    jerarquia.Ficha,
                    jerarquia.Subcategoria,
                    nombreDiagnostico);

                return new ClasificacionDiagnosticoAlbumPresentacion
                {
                    Orden = orden,
                    EsPrincipal = true,
                    Rol = "Diagnóstico principal",
                    Diagnostico = diagnostico.Diagnostico.Trim(),
                    Categoria = categoriaJerarquia,
                    Subcategoria = subcategoriaJerarquia,
                    NombreCientifico = PrimerTexto(
                        jerarquia.NombreCientifico,
                        resultado.NombreCientificoSugerido),
                    CategoriaExiste = !jerarquia.CategoriaEsPropuesta,
                    SubcategoriaExiste = !jerarquia.FichaEsPropuesta,
                    CatalogoDisponible = true,
                    Motivo = PrimerTexto(
                        jerarquia.Motivo,
                        resultado.MotivoAlbumPropuesta,
                        "Clasificación principal asociada con esta fotografía.")
                };
            }

            string categoria =
                MapearCategoriaDiagnostico(diagnostico, resultado);

            AlbumRegistroJerarquiaResponse? existente =
                BuscarSubcategoriaExistente(
                    categoria,
                    nombreDiagnostico);

            if (existente != null)
            {
                return new ClasificacionDiagnosticoAlbumPresentacion
                {
                    Orden = orden,
                    EsPrincipal = esPrincipal,
                    Rol = esPrincipal
                        ? "Diagnóstico principal"
                        : "Diagnóstico adicional",
                    Diagnostico = diagnostico.Diagnostico.Trim(),
                    Categoria = existente.Categoria,
                    Subcategoria = PrimerTexto(
                        existente.Titulo,
                        existente.Subcategoria,
                        nombreDiagnostico),
                    NombreCientifico = existente.NombreCientifico ?? string.Empty,
                    CategoriaExiste = true,
                    SubcategoriaExiste = true,
                    CatalogoDisponible =
                        catalogoClasificacionesMultiplesDisponible,
                    Motivo =
                        "Coincidencia por nombre con una subcategoría activa del catálogo."
                };
            }

            bool categoriaExiste =
                catalogoClasificacionesMultiplesDisponible &&
                catalogoClasificacionesMultiples.Any(item =>
                    NormalizarClave(item.Categoria) ==
                    NormalizarClave(categoria));

            string nombreCientifico =
                esPrincipal
                    ? resultado.NombreCientificoSugerido
                    : string.Empty;

            string motivo;

            if (!catalogoClasificacionesMultiplesDisponible)
            {
                motivo =
                    "No fue posible validar temporalmente esta clasificación contra el catálogo activo.";
            }
            else if (categoriaExiste)
            {
                motivo =
                    "La categoría existe, pero no se encontró una subcategoría activa con este diagnóstico. Se muestra como propuesta para revisión posterior.";
            }
            else
            {
                motivo =
                    "No se encontró una categoría y subcategoría activas compatibles. La clasificación queda como propuesta visual para revisión posterior.";
            }

            return new ClasificacionDiagnosticoAlbumPresentacion
            {
                Orden = orden,
                EsPrincipal = esPrincipal,
                Rol = esPrincipal
                    ? "Diagnóstico principal"
                    : "Diagnóstico adicional",
                Diagnostico = diagnostico.Diagnostico.Trim(),
                Categoria = string.IsNullOrWhiteSpace(categoria)
                    ? "Clasificación pendiente"
                    : categoria,
                Subcategoria = string.IsNullOrWhiteSpace(nombreDiagnostico)
                    ? "Subcategoría por definir"
                    : nombreDiagnostico,
                NombreCientifico = nombreCientifico,
                CategoriaExiste = categoriaExiste,
                SubcategoriaExiste = false,
                CatalogoDisponible =
                    catalogoClasificacionesMultiplesDisponible,
                Motivo = motivo
            };
        }

        private AlbumRegistroJerarquiaResponse? BuscarSubcategoriaExistente(
            string categoria,
            string diagnostico)
        {
            if (!catalogoClasificacionesMultiplesDisponible ||
                string.IsNullOrWhiteSpace(diagnostico))
            {
                return null;
            }

            string categoriaClave = NormalizarClave(categoria);
            string diagnosticoClave =
                NormalizarClave(LimpiarNombreDiagnostico(diagnostico));

            if (string.IsNullOrWhiteSpace(diagnosticoClave))
                return null;

            /*
             * Primero exige coincidencia de categoría. Como respaldo se acepta
             * una coincidencia exacta única por nombre si el proveedor no
             * entregó una categoría normalizable.
             */
            AlbumRegistroJerarquiaResponse? coincidencia =
                catalogoClasificacionesMultiples.FirstOrDefault(item =>
                    NormalizarClave(item.Categoria) == categoriaClave &&
                    CoincideNombreSubcategoria(item, diagnosticoClave));

            if (coincidencia != null)
                return coincidencia;

            List<AlbumRegistroJerarquiaResponse> porNombre =
                catalogoClasificacionesMultiples
                    .Where(item =>
                        CoincideNombreSubcategoria(
                            item,
                            diagnosticoClave))
                    .Take(2)
                    .ToList();

            return porNombre.Count == 1
                ? porNombre[0]
                : null;
        }

        private static bool CoincideNombreSubcategoria(
            AlbumRegistroJerarquiaResponse item,
            string diagnosticoClave)
        {
            string titulo = NormalizarClave(
                LimpiarNombreDiagnostico(item.Titulo));

            string subcategoria = NormalizarClave(
                LimpiarNombreDiagnostico(item.Subcategoria));

            return titulo == diagnosticoClave ||
                   subcategoria == diagnosticoClave;
        }

        private static string MapearCategoriaDiagnostico(
            InspeccionDiagnosticoVisualV2 diagnostico,
            InspeccionFotoResultadoIAV2 resultado)
        {
            string fuente = NormalizarClave(
                $"{diagnostico.Categoria} {diagnostico.TipoDiagnostico}");

            if (fuente.Contains("PLAGA", StringComparison.Ordinal) ||
                fuente.Contains("INSECT", StringComparison.Ordinal) ||
                fuente.Contains("ACARO", StringComparison.Ordinal))
            {
                return "Plagas";
            }

            if (fuente.Contains("ENFERMED", StringComparison.Ordinal) ||
                fuente.Contains("HONGO", StringComparison.Ordinal) ||
                fuente.Contains("FUNG", StringComparison.Ordinal) ||
                fuente.Contains("BACTER", StringComparison.Ordinal) ||
                fuente.Contains("VIRUS", StringComparison.Ordinal))
            {
                return "Enfermedades";
            }

            if (fuente.Contains("NUTRIC", StringComparison.Ordinal) ||
                fuente.Contains("DEFICI", StringComparison.Ordinal) ||
                fuente.Contains("ALTERACION", StringComparison.Ordinal))
            {
                return "Alteraciones nutricionales";
            }

            if (fuente.Contains("ESTRES", StringComparison.Ordinal) ||
                fuente.Contains("ABIOT", StringComparison.Ordinal))
            {
                return "Estrés abiótico";
            }

            if (fuente.Contains("MECAN", StringComparison.Ordinal))
                return "Daños mecánicos";

            if (resultado.EsAparentementeSana)
                return "Plantas sanas";

            if (diagnostico.EsPrincipal &&
                !string.IsNullOrWhiteSpace(
                    resultado.CategoriaAlbumPropuesta))
            {
                return resultado.CategoriaAlbumPropuesta.Trim();
            }

            if (!string.IsNullOrWhiteSpace(diagnostico.Categoria))
            {
                return FormatearCodigo(diagnostico.Categoria);
            }

            return "Clasificación pendiente";
        }

        private Border CrearPanelClasificacionesMultiples(
            IReadOnlyCollection<ClasificacionDiagnosticoAlbumPresentacion>
                propuestas)
        {
            var titulo = new Label
            {
                Text = "Clasificaciones propuestas para el Álbum Botánico",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#31584D"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var subtitulo = new Label
            {
                Text =
                    "Cada diagnóstico detectado por IA conserva su propia propuesta de categoría y subcategoría. La decisión oficial se resolverá en las etapas posteriores.",
                FontSize = 11,
                TextColor = Color.FromArgb("#5E6B67"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var encabezadoTextos = new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    titulo,
                    subtitulo
                }
            };

            var badge = new Border
            {
                Padding = new Thickness(9, 5),
                BackgroundColor = Color.FromArgb("#E5F2ED"),
                Stroke = Color.FromArgb("#C4DED4"),
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(10)
                    },
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = propuestas.Count == 1
                        ? "1 clasificación"
                        : $"{propuestas.Count} clasificaciones",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#31584D")
                }
            };

            var encabezado = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 10
            };
            encabezado.Add(encabezadoTextos, 0, 0);
            encabezado.Add(badge, 1, 0);

            var tarjetas = new VerticalStackLayout
            {
                Spacing = 10
            };

            foreach (ClasificacionDiagnosticoAlbumPresentacion propuesta in
                     propuestas)
            {
                tarjetas.Children.Add(
                    CrearTarjetaClasificacionDiagnostico(propuesta));
            }

            var contenido = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    encabezado,
                    tarjetas
                }
            };

            return new Border
            {
                ClassId = ClassIdClasificacionesMultiplesAlbum,
                Padding = 12,
                BackgroundColor = Color.FromArgb("#F7FAF8"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(12)
                    },
                Content = contenido
            };
        }

        private static Border CrearTarjetaClasificacionDiagnostico(
            ClasificacionDiagnosticoAlbumPresentacion propuesta)
        {
            var numero = new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                Padding = 0,
                BackgroundColor = Color.FromArgb(
                    propuesta.EsPrincipal
                        ? "#3B655B"
                        : "#52786D"),
                StrokeThickness = 0,
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(15)
                    },
                VerticalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = propuesta.Orden.ToString(
                        CultureInfo.InvariantCulture),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                }
            };

            var rol = new Label
            {
                Text = propuesta.Rol.ToUpperInvariant(),
                FontSize = 9,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(
                    propuesta.EsPrincipal
                        ? "#31584D"
                        : "#667A73")
            };

            var diagnostico = new Label
            {
                Text = propuesta.Diagnostico,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#263A35"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var ruta = new Label
            {
                Text =
                    $"{propuesta.Categoria}  →  {propuesta.Subcategoria}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#31584D"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var textos = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    rol,
                    diagnostico,
                    ruta
                }
            };

            if (!string.IsNullOrWhiteSpace(
                    propuesta.NombreCientifico))
            {
                textos.Children.Add(new Label
                {
                    Text = propuesta.NombreCientifico,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = Color.FromArgb("#5E6B67"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            var encabezado = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10
            };
            encabezado.Add(numero, 0, 0);
            encabezado.Add(textos, 1, 0);

            var estados = new VerticalStackLayout
            {
                Spacing = 5
            };

            estados.Children.Add(
                CrearEstadoClasificacion(
                    propuesta.CatalogoDisponible
                        ? propuesta.CategoriaExiste
                            ? "Categoría existente"
                            : "Categoría propuesta"
                        : "Categoría pendiente de validar",
                    propuesta.CatalogoDisponible &&
                    propuesta.CategoriaExiste));

            estados.Children.Add(
                CrearEstadoClasificacion(
                    propuesta.CatalogoDisponible
                        ? propuesta.SubcategoriaExiste
                            ? "Subcategoría existente"
                            : "Subcategoría propuesta"
                        : "Subcategoría pendiente de validar",
                    propuesta.CatalogoDisponible &&
                    propuesta.SubcategoriaExiste));

            var motivo = new Label
            {
                Text = propuesta.Motivo,
                FontSize = 11,
                TextColor = Color.FromArgb("#5E6B67"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var contenido = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    encabezado,
                    estados,
                    motivo
                }
            };

            return new Border
            {
                Padding = 11,
                BackgroundColor = Color.FromArgb(
                    propuesta.EsPrincipal
                        ? "#FFFFFF"
                        : "#FBFCFB"),
                Stroke = Color.FromArgb(
                    propuesta.EsPrincipal
                        ? "#BFD9CF"
                        : "#D4E5DE"),
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(10)
                    },
                Content = contenido
            };
        }

        private static Border CrearEstadoClasificacion(
            string texto,
            bool existente)
        {
            return new Border
            {
                Padding = new Thickness(8, 4),
                HorizontalOptions = LayoutOptions.Start,
                BackgroundColor = Color.FromArgb(
                    existente
                        ? "#E8F3EE"
                        : "#FFF7ED"),
                Stroke = Color.FromArgb(
                    existente
                        ? "#C7E0D6"
                        : "#E5C7A8"),
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(9)
                    },
                Content = new Label
                {
                    Text = texto,
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb(
                        existente
                            ? "#31584D"
                            : "#87512E")
                }
            };
        }

        private static string LimpiarNombreDiagnostico(string? valor)
        {
            string texto = (valor ?? string.Empty).Trim();

            int parentesis = texto.IndexOf('(');
            if (parentesis > 0)
                texto = texto[..parentesis].Trim();

            int separador = texto.IndexOf(
                " - ",
                StringComparison.Ordinal);

            if (separador > 0)
                texto = texto[..separador].Trim();

            return texto;
        }

        private static string FormatearCodigo(string? valor)
        {
            string texto = (valor ?? string.Empty)
                .Trim()
                .Replace('_', ' ');

            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto.ToLowerInvariant();

            return CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(texto);
        }

        private static string NormalizarClave(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto = valor
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(texto.Length);
            bool espacioPendiente = false;

            foreach (char caracter in texto)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter))
                {
                    if (espacioPendiente && builder.Length > 0)
                        builder.Append(' ');

                    builder.Append(caracter);
                    espacioPendiente = false;
                }
                else if (builder.Length > 0)
                {
                    espacioPendiente = true;
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Trim();
        }

        private static string PrimerTexto(params string?[] valores) =>
            valores.FirstOrDefault(valor =>
                !string.IsNullOrWhiteSpace(valor))?.Trim()
            ?? string.Empty;

        private sealed class ClasificacionDiagnosticoAlbumPresentacion
        {
            public int Orden { get; init; }
            public bool EsPrincipal { get; init; }
            public string Rol { get; init; } = string.Empty;
            public string Diagnostico { get; init; } = string.Empty;
            public string Categoria { get; init; } = string.Empty;
            public string Subcategoria { get; init; } = string.Empty;
            public string NombreCientifico { get; init; } = string.Empty;
            public bool CategoriaExiste { get; init; }
            public bool SubcategoriaExiste { get; init; }
            public bool CatalogoDisponible { get; init; }
            public string Motivo { get; init; } = string.Empty;
        }
    }
}
