using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ConfiguracionViewModel : GlobalService
    {
        private readonly IReadOnlyList<ConfiguracionCategoria>
            catalogoCompleto;

        private CancellationTokenSource? filtroCts;

        private IReadOnlyList<ConfiguracionGrupoVisual>
            gruposVisibles =
                Array.Empty<ConfiguracionGrupoVisual>();

        private string textoBusqueda = string.Empty;
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

        /// <summary>
        /// El CollectionView recibe la colección completa mediante una
        /// única notificación. No se ejecutan Clear/Add por cada tarjeta.
        /// </summary>
        public IReadOnlyList<ConfiguracionGrupoVisual>
            GruposVisibles
        {
            get => gruposVisibles;
            private set
            {
                if (ReferenceEquals(gruposVisibles, value))
                    return;

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
                string nuevoValor =
                    value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
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

                AbrirOpcionCommand.ChangeCanExecute();
            }
        }

        public bool MostrarSinOpciones =>
            cantidadOpciones == 0;

        public string ResumenOpciones =>
            cantidadOpciones == 1
                ? "1 opción disponible"
                : $"{cantidadOpciones} opciones disponibles";

        /// <summary>
        /// Recarga permisos y búsqueda una sola vez al mostrar la página.
        /// La cantidad de columnas se controla directamente en el
        /// GridItemsLayout y ya no reconstruye los datos.
        /// </summary>
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

            _ = AplicarFiltroConEsperaAsync(source);
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
                    !EsFiltroActual(source))
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(
                    AplicarFiltro);
            }
            catch (OperationCanceledException)
            {
                // Una nueva tecla sustituyó esta búsqueda.
            }
            catch (ObjectDisposedException)
            {
                // La pantalla se cerró antes de finalizar.
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
                new List<ConfiguracionGrupoVisual>(
                    catalogoCompleto.Count);

            int totalOpciones = 0;

            foreach (ConfiguracionCategoria categoria
                     in catalogoCompleto)
            {
                bool coincideCategoria =
                    string.IsNullOrWhiteSpace(filtro) ||
                    categoria.TextoBusqueda.Contains(
                        filtro,
                        StringComparison.OrdinalIgnoreCase);

                var opciones =
                    new List<ConfiguracionOpcion>(
                        categoria.Opciones.Count);

                foreach (ConfiguracionOpcion opcion
                         in categoria.Opciones)
                {
                    if (!PermissionService.Instance.HasRead(
                            opcion.Interfaz))
                    {
                        continue;
                    }

                    bool coincide =
                        coincideCategoria ||
                        opcion.TextoBusqueda.Contains(
                            filtro,
                            StringComparison.OrdinalIgnoreCase);

                    if (coincide)
                        opciones.Add(opcion);
                }

                if (opciones.Count == 0)
                    continue;

                totalOpciones += opciones.Count;

                grupos.Add(
                    new ConfiguracionGrupoVisual(
                        categoria.Titulo,
                        categoria.Descripcion,
                        opciones));
            }

            /*
             * Una sola asignación evita decenas de eventos
             * CollectionChanged y mediciones repetidas.
             */
            GruposVisibles = grupos;
            cantidadOpciones = totalOpciones;

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

            if (!PermissionService.Instance.HasRead(
                    opcion.Interfaz))
            {
                await MostrarAdvertenciaAsync(
                    $"No tiene permiso para consultar " +
                    $"{opcion.Titulo.ToLowerInvariant()}.");

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
                    $"abrir {opcion.Titulo.ToLowerInvariant()}",
                    ex);
            }
            finally
            {
                Navegando = false;
            }
        }

        private bool EsFiltroActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref filtroCts),
                source);

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
                // La búsqueda ya había finalizado.
            }
        }

        private static IReadOnlyList<ConfiguracionCategoria>
            CrearCatalogo()
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
                        "userPage",
                        AppRoutes.Usuarios,
                        1,
                        verdeSuave),

                    Opcion(
                        "Roles",
                        "Definir perfiles de acceso.",
                        "iconrol.png",
                        "rolPage",
                        AppRoutes.Roles,
                        2,
                        verdeSuave),

                    Opcion(
                        "Matriz de permisos",
                        "Asignar acciones disponibles por rol.",
                        "iconpermission.png",
                        "matrizPermisosPage",
                        AppRoutes.MatrizPermisos,
                        3,
                        verdeSuave)),

                Categoria(
                    "Ubicación y fincas",
                    "Catálogos geográficos y terrenos.",
                    2,
                    Opcion(
                        "Países y ubicaciones",
                        "Países, departamentos y municipios.",
                        "iconcountry.png",
                        "paisPage",
                        AppRoutes.Paises,
                        1,
                        cafeSuave),

                    Opcion(
                        "Terrenos",
                        "Registrar y administrar fincas.",
                        "iconland.png",
                        "terrenoPage",
                        AppRoutes.Terrenos,
                        2,
                        cafeSuave)),

                Categoria(
                    "Catálogos agronómicos",
                    "Información base utilizada en los análisis de suelo.",
                    3,
                    Opcion(
                        "Tipos de cultivo",
                        "Cultivos disponibles para análisis.",
                        "iconcultivo.png",
                        "tipoCultivoPage",
                        AppRoutes.TiposCultivo,
                        1,
                        verdeSuave),

                    Opcion(
                        "Tipos de análisis",
                        "Clasificaciones de análisis de suelo.",
                        "icontipoanalisis.png",
                        "tipoAnalisisSueloPage",
                        AppRoutes.TiposAnalisisSuelo,
                        2,
                        verdeSuave),

                    Opcion(
                        "Elementos químicos",
                        "Nutrientes y pesos equivalentes.",
                        "iconchemicalelement.png",
                        "elementoQuimicoPage",
                        AppRoutes.ElementosQuimicos,
                        3,
                        verdeSuave),

                    Opcion(
                        "Fuentes de nutrientes",
                        "Fertilizantes y fuentes orgánicas.",
                        "iconfuentenutriente.png",
                        "fuenteNutrientePage",
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
                        "extraccionNutrientePage",
                        AppRoutes.ExtraccionNutrientes,
                        1,
                        amarilloSuave),

                    Opcion(
                        "Rangos nutricionales",
                        "Niveles mínimos y máximos por cultivo.",
                        "iconrangonutriente.png",
                        "rangoNutrientePage",
                        AppRoutes.RangosNutrientes,
                        2,
                        amarilloSuave)),

                Categoria(
                    "Contenido y comunicación",
                    "Catálogos utilizados por el centro de noticias.",
                    5,
                    Opcion(
                        "Tipos de publicación",
                        "Noticias, ofertas, eventos y categorías.",
                        "iconnews.png",
                        "categoriaPublicacionPage",
                        AppRoutes.CategoriasPublicacion,
                        1,
                        azulSuave)),

                Categoria(
                    "Auditoría y control",
                    "Seguimiento de las operaciones realizadas.",
                    6,
                    Opcion(
                        "Bitácora",
                        "Consultar cambios y acciones registradas.",
                        "iconsettings.png",
                        "bitacoraPage",
                        AppRoutes.Bitacora,
                        1,
                        grisSuave))
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
                Opciones =
                    opciones
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
