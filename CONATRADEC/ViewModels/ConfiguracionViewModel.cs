using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Catálogo de configuración filtrado por permisos.
    /// </summary>
    public sealed class ConfiguracionViewModel :
        GlobalService
    {
        private readonly IReadOnlyList<
            ConfiguracionCategoria> catalogoCompleto;

        private CancellationTokenSource? filtroCts;

        private IReadOnlyList<
            ConfiguracionGrupoVisual> gruposVisibles =
                Array.Empty<ConfiguracionGrupoVisual>();

        private string textoBusqueda =
            string.Empty;

        private bool navegando;

        private int cantidadOpciones;

        public ConfiguracionViewModel()
        {
            catalogoCompleto = CrearCatalogo();

            AbrirOpcionCommand =
                new Command<ConfiguracionOpcion>(
                    async opcion =>
                        await AbrirOpcionAsync(opcion),
                    opcion =>
                        opcion != null &&
                        !Navegando);
        }

        public IReadOnlyList<
            ConfiguracionGrupoVisual> GruposVisibles
        {
            get => gruposVisibles;
            private set
            {
                if (ReferenceEquals(
                        gruposVisibles,
                        value))
                {
                    return;
                }

                gruposVisibles = value;
                OnPropertyChanged();
            }
        }

        public Command<ConfiguracionOpcion>
            AbrirOpcionCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevo =
                    value ?? string.Empty;

                if (textoBusqueda == nuevo)
                    return;

                textoBusqueda = nuevo;
                OnPropertyChanged();
                ProgramarFiltro();
            }
        }

        public bool Navegando
        {
            get => navegando;
            private set
            {
                if (navegando == value)
                    return;

                navegando = value;
                OnPropertyChanged();

                AbrirOpcionCommand
                    .ChangeCanExecute();
            }
        }

        public bool MostrarSinOpciones =>
            cantidadOpciones == 0;

        public string ResumenOpciones =>
            cantidadOpciones == 1
                ? "1 opción disponible"
                : $"{cantidadOpciones} opciones disponibles";

        public void ActualizarOpciones()
        {
            CancelarBusqueda();
            AplicarFiltro();
        }

        public void CancelarBusqueda()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref filtroCts,
                    null);

            CancelarSeguro(source);
        }

        private void ProgramarFiltro()
        {
            var source =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref filtroCts,
                    source);

            CancelarSeguro(anterior);

            _ = AplicarFiltroConEsperaAsync(
                source);
        }

        private async Task AplicarFiltroConEsperaAsync(
            CancellationTokenSource source)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(250),
                    source.Token);

                if (source.IsCancellationRequested ||
                    !ReferenceEquals(
                        Volatile.Read(ref filtroCts),
                        source))
                {
                    return;
                }

                await MainThread
                    .InvokeOnMainThreadAsync(
                        AplicarFiltro);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                Interlocked.CompareExchange(
                    ref filtroCts,
                    null,
                    source);

                source.Dispose();
            }
        }

        private void AplicarFiltro()
        {
            string filtro =
                TextoBusqueda.Trim();

            var grupos =
                new List<ConfiguracionGrupoVisual>();

            int total = 0;

            foreach (ConfiguracionCategoria categoria
                     in catalogoCompleto
                         .OrderBy(item => item.Orden))
            {
                bool coincideCategoria =
                    string.IsNullOrWhiteSpace(filtro) ||
                    categoria.TextoBusqueda.Contains(
                        filtro,
                        StringComparison.OrdinalIgnoreCase);

                List<ConfiguracionOpcion> opciones =
                    categoria.Opciones
                        .OrderBy(item => item.Orden)
                        .Where(item =>
                            PermissionService.Instance
                                .HasRead(item.Interfaz))
                        .Where(item =>
                            coincideCategoria ||
                            item.TextoBusqueda.Contains(
                                filtro,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (opciones.Count == 0)
                    continue;

                total += opciones.Count;

                grupos.Add(
                    new ConfiguracionGrupoVisual(
                        categoria.Titulo,
                        categoria.Descripcion,
                        opciones));
            }

            GruposVisibles = grupos;
            cantidadOpciones = total;

            OnPropertyChanged(
                nameof(MostrarSinOpciones));

            OnPropertyChanged(
                nameof(ResumenOpciones));
        }

        private async Task AbrirOpcionAsync(
            ConfiguracionOpcion? opcion)
        {
            if (opcion == null || Navegando)
                return;

            if (!PermissionService.Instance
                    .HasRead(opcion.Interfaz))
            {
                await MostrarAdvertenciaAsync(
                    "No tiene permiso para consultar " +
                    opcion.Titulo.ToLowerInvariant() +
                    ".");

                ActualizarOpciones();
                return;
            }

            Navegando = true;

            try
            {
                CancelarBusqueda();

                await GoToAsyncParameters(
                    opcion.Ruta);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "abrir " +
                    opcion.Titulo.ToLowerInvariant(),
                    ex);
            }
            finally
            {
                Navegando = false;
            }
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static IReadOnlyList<
            ConfiguracionCategoria> CrearCatalogo()
        {
            Color verdeSuave =
                Color.FromArgb("#EEF5F2");

            Color cafeSuave =
                Color.FromArgb("#F7F1EC");

            Color amarilloSuave =
                Color.FromArgb("#FFF7E6");

            Color azulSuave =
                Color.FromArgb("#EEF4FF");

            Color grisSuave =
                Color.FromArgb("#F3F4F6");

            return new List<ConfiguracionCategoria>
            {
                Categoria(
                    "Seguridad y usuarios",
                    "Usuarios, roles y control de accesos.",
                    1,
                    Opcion(
                        "Usuarios",
                        "Crear y administrar usuarios.",
                        "iconuser.png",
                        InterfazCodigos.Usuarios,
                        AppRoutes.Usuarios,
                        1,
                        verdeSuave),
                    Opcion(
                        "Roles",
                        "Definir perfiles de acceso.",
                        "iconrol.png",
                        InterfazCodigos.Roles,
                        AppRoutes.Roles,
                        2,
                        verdeSuave),
                    Opcion(
                        "Matriz de permisos",
                        "Asignar acciones disponibles por rol.",
                        "iconpermission.png",
                        InterfazCodigos.MatrizPermisos,
                        AppRoutes.MatrizPermisos,
                        3,
                        verdeSuave)),

                Categoria(
                    "Ubicación, propietarios y fincas",
                    "Propietarios, catálogos geográficos y terrenos.",
                    2,
                    Opcion(
                        "Propietarios",
                        "Registrar una vez a cada propietario y reutilizarlo en sus terrenos.",
                        "iconuser.png",
                        InterfazCodigos.Propietarios,
                        AppRoutes.Propietarios,
                        1,
                        cafeSuave),
                    Opcion(
                        "Países y ubicaciones",
                        "Países, departamentos y municipios.",
                        "iconcountry.png",
                        InterfazCodigos.Paises,
                        AppRoutes.Paises,
                        2,
                        cafeSuave),
                    Opcion(
                        "Terrenos",
                        "Registrar fincas vinculadas con un propietario.",
                        "iconland.png",
                        InterfazCodigos.Terrenos,
                        AppRoutes.Terrenos,
                        3,
                        cafeSuave)),

                Categoria(
                    "Catálogos agronómicos",
                    "Información base utilizada en los análisis de suelo.",
                    3,
                    Opcion(
                        "Tipos de cultivo",
                        "Cultivos disponibles para análisis.",
                        "iconcultivo.png",
                        InterfazCodigos.TiposCultivo,
                        AppRoutes.TiposCultivo,
                        1,
                        verdeSuave),
                    Opcion(
                        "Tipos de análisis",
                        "Clasificaciones de análisis de suelo.",
                        "icontipoanalisis.png",
                        InterfazCodigos.TiposAnalisisSuelo,
                        AppRoutes.TiposAnalisisSuelo,
                        2,
                        verdeSuave),
                    Opcion(
                        "Elementos químicos",
                        "Nutrientes y pesos equivalentes.",
                        "iconchemicalelement.png",
                        InterfazCodigos.ElementosQuimicos,
                        AppRoutes.ElementosQuimicos,
                        3,
                        verdeSuave),
                    Opcion(
                        "Fuentes de nutrientes",
                        "Fertilizantes y fuentes orgánicas.",
                        "iconfuentenutriente.png",
                        InterfazCodigos.FuentesNutrientes,
                        AppRoutes.FuenteNutriente,
                        4,
                        verdeSuave)),

                Categoria(
                    "Parámetros nutricionales",
                    "Valores técnicos para recomendaciones y cálculos.",
                    4,
                    Opcion(
                        "Extracción de nutrientes",
                        "Valores de extracción por quintal oro.",
                        "iconextraccion.png",
                        InterfazCodigos.ExtraccionNutrientes,
                        AppRoutes.ExtraccionNutrientes,
                        1,
                        amarilloSuave),
                    Opcion(
                        "Rangos nutricionales",
                        "Niveles mínimos y máximos por cultivo.",
                        "iconrangonutriente.png",
                        InterfazCodigos.RangosNutrientes,
                        AppRoutes.RangosNutrientes,
                        2,
                        amarilloSuave),
                    Opcion(
                        "Unidades y conversiones",
                        "Unidades permitidas y fórmulas por elemento.",
                        "iconsettings.png",
                        InterfazCodigos.ElementosQuimicos,
                        AppRoutes.ConfiguracionUnidades,
                        3,
                        amarilloSuave)),

                Categoria(
                    "Contenido y comunicación",
                    "Catálogos utilizados por el centro de noticias.",
                    5,
                    Opcion(
                        "Tipos de publicación",
                        "Noticias, ofertas, eventos y categorías.",
                        "iconnews.png",
                        InterfazCodigos.CategoriasPublicacion,
                        AppRoutes.CategoriasPublicacion,
                        1,
                        azulSuave)),

                Categoria(
                    "Inteligencia artificial",
                    "Parámetros administrativos del proveedor, sus revisiones y devoluciones técnicas.",
                    6,
                    Opcion(
                        "Configuración de Diagnóstico IA",
                        "Administrar cuántas revisiones adicionales puede solicitarse a Gemini por diagnóstico.",
                        "iconsettings.png",
                        DiagnosticoIARoutes.InterfazConfiguracion,
                        DiagnosticoIARoutes.PaginaConfiguracion,
                        1,
                        amarilloSuave),
                    Opcion(
                        "Motivos de devolución al técnico",
                        "Administrar causas, instrucciones y tipos de corrección solicitados por el analizador.",
                        "iconsettings.png",
                        DiagnosticoIARoutes.InterfazConfiguracion,
                        MotivoDevolucionTecnicoRoutes.Pagina,
                        2,
                        amarilloSuave)),

                Categoria(
                    "Auditoría y control",
                    "Seguimiento de las operaciones realizadas.",
                    7,
                    Opcion(
                        "Bitácora",
                        "Consultar cambios y acciones registradas.",
                        "iconsettings.png",
                        InterfazCodigos.Bitacora,
                        AppRoutes.Bitacora,
                        1,
                        grisSuave)),

                Categoria(
                    "Sistema y aplicación",
                    "Versiones, mantenimiento y herramientas.",
                    8,
                    Opcion(
                        "Actualizaciones",
                        "Buscar, descargar e instalar nuevas versiones.",
                        "iconappupdate.png",
                        InterfazCodigos.Actualizaciones,
                        AppRoutes.ActualizacionAplicacion,
                        1,
                        azulSuave))
            };
        }

        private static ConfiguracionCategoria Categoria(
            string titulo,
            string descripcion,
            int orden,
            params ConfiguracionOpcion[] opciones) =>
            new()
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Orden = orden,
                Opciones = opciones
                    .OrderBy(item => item.Orden)
                    .ToList()
            };

        private static ConfiguracionOpcion Opcion(
            string titulo,
            string descripcion,
            string icono,
            string interfaz,
            string ruta,
            int orden,
            Color colorFondoIcono) =>
            new()
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Icono = icono,
                Interfaz = interfaz,
                Ruta = ruta,
                Orden = orden,
                ColorFondoIcono = colorFondoIcono
            };
    }
}
