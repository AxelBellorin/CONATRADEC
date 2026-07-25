using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class ConfiguracionViewModel : GlobalService
    {
        private readonly List<ConfiguracionCategoria> catalogoCompleto;
        private CancellationTokenSource? filtroCts;

        private string textoBusqueda = string.Empty;
        private bool navegando;
        private int cantidadColumnas = 1;
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
        /// Colección visual plana. Contiene encabezados y filas,
        /// pero nunca listas visuales anidadas.
        /// </summary>
        public ObservableCollection<ConfiguracionElementoVisual>
            ElementosVisibles { get; } = new();

        public Command<ConfiguracionOpcion>
            AbrirOpcionCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor = value ?? string.Empty;

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
        /// Ajusta la distribución según el ancho real de la ventana.
        /// </summary>
        public void ConfigurarColumnas(int columnas)
        {
            int nuevoValor = Math.Clamp(columnas, 1, 3);

            if (cantidadColumnas == nuevoValor)
                return;

            cantidadColumnas = nuevoValor;
            AplicarFiltro();
        }

        /// <summary>
        /// Recarga las opciones y vuelve a evaluar los permisos.
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
            var source = new CancellationTokenSource();

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
                    TimeSpan.FromMilliseconds(300),
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
                // Una nueva tecla sustituyó este filtro.
            }
            catch (ObjectDisposedException)
            {
                // La página se cerró antes de terminar la espera.
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
                TextoBusqueda.Trim().ToUpperInvariant();

            var nuevosElementos =
                new List<ConfiguracionElementoVisual>();

            int totalOpciones = 0;

            foreach (ConfiguracionCategoria categoria in
                     catalogoCompleto.OrderBy(x => x.Orden))
            {
                List<ConfiguracionOpcion> opciones =
                    categoria.Opciones
                        .Where(opcion =>
                            PermissionService.Instance.HasRead(
                                opcion.Interfaz))
                        .Where(opcion =>
                            string.IsNullOrWhiteSpace(filtro) ||
                            opcion.TextoBusqueda.Contains(filtro) ||
                            categoria.Titulo
                                .ToUpperInvariant()
                                .Contains(filtro) ||
                            categoria.Descripcion
                                .ToUpperInvariant()
                                .Contains(filtro))
                        .OrderBy(opcion => opcion.Orden)
                        .ToList();

                if (opciones.Count == 0)
                    continue;

                totalOpciones += opciones.Count;

                nuevosElementos.Add(
                    ConfiguracionElementoVisual
                        .CrearEncabezado(categoria));

                for (int indice = 0;
                     indice < opciones.Count;
                     indice += cantidadColumnas)
                {
                    ConfiguracionOpcion opcion1 =
                        opciones[indice];

                    ConfiguracionOpcion? opcion2 =
                        indice + 1 < opciones.Count
                            ? opciones[indice + 1]
                            : null;

                    ConfiguracionOpcion? opcion3 =
                        indice + 2 < opciones.Count
                            ? opciones[indice + 2]
                            : null;

                    nuevosElementos.Add(
                        ConfiguracionElementoVisual.CrearFila(
                            cantidadColumnas,
                            opcion1,
                            cantidadColumnas >= 2
                                ? opcion2
                                : null,
                            cantidadColumnas >= 3
                                ? opcion3
                                : null));
                }
            }

            ElementosVisibles.Clear();

            foreach (ConfiguracionElementoVisual elemento in
                     nuevosElementos)
            {
                ElementosVisibles.Add(elemento);
            }

            cantidadOpciones = totalOpciones;

            OnPropertyChanged(nameof(ElementosVisibles));
            OnPropertyChanged(nameof(MostrarSinOpciones));
            OnPropertyChanged(nameof(ResumenOpciones));
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
                await GoToAsyncParameters(opcion.Ruta);
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
                // El filtro ya había terminado.
            }
        }

        private static List<ConfiguracionCategoria>
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
                Opciones = opciones.ToList()
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
