using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;
using System.Linq;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    public partial class albumDetallePage : ContentPage
    {
        private readonly AlbumDetalleViewModel viewModel = new();
        private readonly AlbumFitosanitarioSenalizacionApiService
            senalizacionApi = new();
        private CollectionView? fotosCollection;
        private int spanFotos = -1;

        public int RegistroId
        {
            set => viewModel.Id = value;
        }

        public albumDetallePage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            Loaded += (_, _) => AplicarColumnasFotografias();
            SizeChanged += (_, _) => AplicarColumnasFotografias();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                /*
                 * Una entrada o regreso visible al detalle parte de datos de
                 * señalización limpios. Al abrir el visor no se borra la caché,
                 * de modo que el subflujo puede reutilizar las mismas capas.
                 */
                if (viewModel.Id > 0)
                {
                    AlbumFitosanitarioSenalizacionCache.LimpiarAlbum(
                        viewModel.Id);
                }

                viewModel.ActualizarPermisos();

                if (!viewModel.CanView)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para consultar el álbum botánico.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.AlbumFotos);
                    return;
                }

                AplicarColumnasFotografias();
                await viewModel.LoadAsync(true);
                await CargarSenalizacionesFitosanitariasAsync();
                AplicarColumnasFotografias();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir el detalle del álbum: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            /*
             * No se limpia la señalización aquí: abrir el visor fotográfico es
             * un subflujo del mismo módulo y necesita conservar estas capas.
             * La siguiente aparición del detalle invalida y vuelve a consultar.
             */
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        /// <summary>
        /// Carga todas las señales de la ficha en una sola petición. La caché
        /// vive únicamente durante esta visita al detalle del álbum.
        /// </summary>
        private async Task CargarSenalizacionesFitosanitariasAsync()
        {
            int albumId = viewModel.Detalle?.AlbumBotanicoCafeId ?? 0;
            if (albumId <= 0)
                return;

            try
            {
                var datos = await senalizacionApi.ObtenerAsync(albumId);
                AlbumFitosanitarioSenalizacionCache.ReemplazarAlbum(
                    albumId,
                    datos);
            }
            catch (Exception ex)
            {
                // La señalización es complementaria: la galería normal continúa
                // disponible aun si esta consulta falla temporalmente.
                Debug.WriteLine(
                    $"No fue posible cargar señalización fitosanitaria: {ex}");
                AlbumFitosanitarioSenalizacionCache.ReemplazarAlbum(
                    albumId,
                    []);
            }
        }

        private void AplicarColumnasFotografias()
        {
            fotosCollection ??= BuscarCollectionView();

            if (fotosCollection?.ItemsLayout is not GridItemsLayout layout)
                return;

            double ancho = fotosCollection.Width > 0
                ? fotosCollection.Width
                : Width;

            if (ancho <= 0)
                return;

            int span = ancho < 520
                ? 1
                : ancho < 850
                    ? 2
                    : ancho < 1150
                        ? 3
                        : 4;

            if (spanFotos == span && layout.Span == span)
                return;

            spanFotos = span;
            layout.Span = span;
        }

        private CollectionView? BuscarCollectionView()
        {
            if (Content is Grid grid)
            {
                return grid.Children
                    .OfType<CollectionView>()
                    .FirstOrDefault();
            }

            return null;
        }
    }
}
