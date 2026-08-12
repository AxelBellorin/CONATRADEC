using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.ComponentModel;

namespace CONATRADEC.Views
{
    public partial class configuracionPage :
        ContentPage
    {
        private static bool rutasRegistradas;

        private readonly ConfiguracionViewModel
            viewModel = new();

        /*
         * Este indicador separa exclusivamente el diseño de Android Tablet.
         * Teléfono y Windows continúan utilizando el CollectionView original.
         */
        private readonly bool esTabletAndroid;

        private int cantidadColumnasActual;
        private bool paginaVisible;

        public configuracionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;

            esTabletAndroid =
                DeviceInfo.Current.Platform ==
                    DevicePlatform.Android &&
                DeviceInfo.Current.Idiom ==
                    DeviceIdiom.Tablet;

            /*
             * No se modifica el catálogo actual para teléfono ni Windows.
             * Únicamente Android Tablet utiliza el listado de filas de dos
             * tarjetas que evita que los encabezados queden intercalados.
             */
            OpcionesCollection.IsVisible =
                !esTabletAndroid;

            OpcionesTabletCollection.IsVisible =
                esTabletAndroid;

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;

            RegistrarRutas();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;

            AjustarCantidadColumnas(
                Width);

            ActualizarAccionesSistema();
            viewModel.ActualizarOpciones();

            if (esTabletAndroid)
            {
                ActualizarGruposTablet();
            }
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;

            viewModel.CancelarBusqueda();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(
                width,
                height);

            if (paginaVisible)
            {
                AjustarCantidadColumnas(
                    width);
            }
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            /*
             * Android Tablet no utiliza OpcionesGridLayout.
             * Sus filas ya están construidas explícitamente con dos columnas.
             */
            if (esTabletAndroid)
                return;

            if (width <= 0 ||
                OpcionesGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1180
                    ? 3
                    : width >= 680
                        ? 2
                        : 1;

            if (cantidadColumnasActual ==
                nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual =
                nuevasColumnas;

            OpcionesGridLayout.Span =
                nuevasColumnas;
        }

        /// <summary>
        /// Cuando el filtro o los permisos reconstruyen los grupos visibles,
        /// se reconstruyen también las filas exclusivas de Android Tablet.
        /// </summary>
        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (!esTabletAndroid ||
                e.PropertyName !=
                    nameof(
                        ConfiguracionViewModel
                            .GruposVisibles))
            {
                return;
            }

            ActualizarGruposTablet();
        }

        /// <summary>
        /// Convierte cada grupo visual en filas de dos tarjetas.
        /// Si la categoría contiene una cantidad impar de opciones,
        /// la última fila conserva una sola opción y esa tarjeta ocupa
        /// automáticamente el ancho de las dos columnas.
        /// </summary>
        private void ActualizarGruposTablet()
        {
            if (!esTabletAndroid ||
                OpcionesTabletCollection == null)
            {
                return;
            }

            var gruposTablet =
                new List<ConfiguracionGrupoTablet>();

            foreach (
                ConfiguracionGrupoVisual grupo
                in viewModel.GruposVisibles)
            {
                var filas =
                    new List<ConfiguracionFilaTablet>();

                for (
                    int indice = 0;
                    indice < grupo.Count;
                    indice += 2)
                {
                    ConfiguracionOpcion primera =
                        grupo[indice];

                    ConfiguracionOpcion? segunda =
                        indice + 1 < grupo.Count
                            ? grupo[indice + 1]
                            : null;

                    filas.Add(
                        new ConfiguracionFilaTablet(
                            primera,
                            segunda));
                }

                gruposTablet.Add(
                    new ConfiguracionGrupoTablet(
                        grupo.Titulo,
                        grupo.Descripcion,
                        filas));
            }

            OpcionesTabletCollection.ItemsSource =
                gruposTablet;
        }

        /// <summary>
        /// Datos sin conexión continúa respetando su permiso original.
        /// Cerrar sesión permanece siempre disponible dentro de Configuración.
        /// Si el usuario no puede trabajar sin conexión, la tarjeta de salida
        /// ocupa el ancho completo para no dejar un espacio vacío.
        /// </summary>
        private void ActualizarAccionesSistema()
        {
            bool mostrarSinConexion =
                DatosSinConexionPermisos.TienePermiso;

            ActualizarAccionesSistema(
                DatosSinConexionCard,
                CerrarSesionCard,
                mostrarSinConexion);

            ActualizarAccionesSistema(
                DatosSinConexionCardTablet,
                CerrarSesionCardTablet,
                mostrarSinConexion);
        }

        private static void ActualizarAccionesSistema(
            Border? datosSinConexion,
            Border? cerrarSesion,
            bool mostrarSinConexion)
        {
            if (datosSinConexion == null ||
                cerrarSesion == null)
            {
                return;
            }

            datosSinConexion.IsVisible =
                mostrarSinConexion;

            if (mostrarSinConexion)
            {
                Grid.SetColumn(
                    cerrarSesion,
                    1);

                Grid.SetColumnSpan(
                    cerrarSesion,
                    1);
            }
            else
            {
                Grid.SetColumn(
                    cerrarSesion,
                    0);

                Grid.SetColumnSpan(
                    cerrarSesion,
                    2);
            }
        }

        private async void DatosSinConexionCard_Tapped(
            object? sender,
            TappedEventArgs e)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                DatosSinConexionCard.IsVisible =
                    false;

                DatosSinConexionCardTablet.IsVisible =
                    false;

                ActualizarAccionesSistema();
                return;
            }

            await viewModel.GoToAsyncParameters(
                "//DatosSinConexionPage");
        }

        private static void RegistrarRutas()
        {
            if (rutasRegistradas)
                return;

            Routing.RegisterRoute(
                AppRoutes.Bitacora,
                typeof(bitacoraPage));

            Routing.RegisterRoute(
                AppRoutes.BitacoraDetalle,
                typeof(bitacoraDetallePage));

            Routing.RegisterRoute(
                AppRoutes.ConfiguracionUnidades,
                typeof(configuracionUnidadesPage));

            /*
             * El catálogo de motivos se muestra como una opción normal dentro
             * del grupo Inteligencia artificial. La ruta se registra una sola
             * vez antes de que el usuario pueda abrir la tarjeta.
             */
            MotivoDevolucionTecnicoRoutes.AsegurarRegistro();

            rutasRegistradas = true;
        }

        /// <summary>
        /// Fila visual utilizada únicamente por Android Tablet.
        /// </summary>
        public sealed class ConfiguracionFilaTablet
        {
            public ConfiguracionFilaTablet(
                ConfiguracionOpcion opcion1,
                ConfiguracionOpcion? opcion2)
            {
                Opcion1 = opcion1;
                Opcion2 = opcion2;
            }

            public ConfiguracionOpcion Opcion1
            {
                get;
            }

            public ConfiguracionOpcion? Opcion2
            {
                get;
            }

            public bool TieneSegundaOpcion =>
                Opcion2 != null;

            public int ColumnSpanPrimera =>
                TieneSegundaOpcion
                    ? 1
                    : 2;
        }

        /// <summary>
        /// Grupo compatible con CollectionView.IsGrouped para Android Tablet.
        /// </summary>
        public sealed class ConfiguracionGrupoTablet :
            List<ConfiguracionFilaTablet>
        {
            public ConfiguracionGrupoTablet(
                string titulo,
                string descripcion,
                IEnumerable<
                    ConfiguracionFilaTablet> filas)
                : base(filas)
            {
                Titulo = titulo;
                Descripcion = descripcion;
            }

            public string Titulo
            {
                get;
            }

            public string Descripcion
            {
                get;
            }
        }
    }
}
