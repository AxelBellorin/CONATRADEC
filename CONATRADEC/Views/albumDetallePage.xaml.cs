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
                AplicarColumnasFotografias();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir el detalle del álbum: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
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
