using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(Fotos), "Fotos")]
    [QueryProperty(nameof(FotoInicial), "FotoInicial")]
    public partial class FotosTerrenoGaleriaPage : ContentPage
    {
        private FotoTerrenoItem? fotoInicial;
        private int fotoSeleccionadaIndex;

        private double startScale = 1;
        private double panStartX = 0;
        private double panStartY = 0;

        private Image? imagenConZoom;

        private const double MinScale = 1;
        private const double MaxScale = 5;
        private const double DoubleTapScale = 2.5;

        public ObservableCollection<FotoTerrenoItem> FotosGaleria { get; } = new();

        public int FotoSeleccionadaIndex
        {
            get => fotoSeleccionadaIndex;
            set
            {
                if (fotoSeleccionadaIndex != value)
                {
                    fotoSeleccionadaIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Titulo));
                }
            }
        }

        public string Titulo
        {
            get
            {
                if (FotosGaleria.Count == 0)
                    return "Fotos del terreno";

                return $"Foto {FotoSeleccionadaIndex + 1} de {FotosGaleria.Count}";
            }
        }

        public List<FotoTerrenoItem>? Fotos
        {
            get => FotosGaleria.ToList();
            set
            {
                FotosGaleria.Clear();

                if (value != null)
                {
                    foreach (var foto in value)
                    {
                        FotosGaleria.Add(foto);
                    }
                }

                OnPropertyChanged(nameof(FotosGaleria));
                OnPropertyChanged(nameof(Titulo));

                AplicarFotoInicial();
            }
        }

        public FotoTerrenoItem? FotoInicial
        {
            get => fotoInicial;
            set
            {
                fotoInicial = value;
                AplicarFotoInicial();
            }
        }

        public FotosTerrenoGaleriaPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private void AplicarFotoInicial()
        {
            if (fotoInicial == null || FotosGaleria.Count == 0)
                return;

            int index = FotosGaleria.IndexOf(fotoInicial);

            if (index < 0)
            {
                index = FotosGaleria
                    .ToList()
                    .FindIndex(f =>
                        (fotoInicial.FotoTerrenoId != null &&
                         f.FotoTerrenoId == fotoInicial.FotoTerrenoId)
                        ||
                        (!string.IsNullOrWhiteSpace(fotoInicial.LocalPath) &&
                         f.LocalPath == fotoInicial.LocalPath)
                        ||
                        (!string.IsNullOrWhiteSpace(fotoInicial.UrlFotoTerreno) &&
                         f.UrlFotoTerreno == fotoInicial.UrlFotoTerreno));
            }

            if (index >= 0)
            {
                FotoSeleccionadaIndex = index;
            }
        }

        private void Imagen_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (sender is not Image image)
                return;

            imagenConZoom = image;

            if (e.Status == GestureStatus.Started)
            {
                startScale = image.Scale;

                image.AnchorX = e.ScaleOrigin.X;
                image.AnchorY = e.ScaleOrigin.Y;
            }
            else if (e.Status == GestureStatus.Running)
            {
                double newScale = Clamp(startScale * e.Scale, MinScale, MaxScale);

                image.Scale = newScale;

                if (newScale <= MinScale)
                {
                    ResetImage(image);
                    CarouselFotos.IsSwipeEnabled = true;
                }
                else
                {
                    CarouselFotos.IsSwipeEnabled = false;
                    AjustarTraslacion(image);
                }
            }
            else if (e.Status == GestureStatus.Completed ||
                     e.Status == GestureStatus.Canceled)
            {
                if (image.Scale <= MinScale)
                {
                    ResetImage(image);
                    CarouselFotos.IsSwipeEnabled = true;
                }
                else
                {
                    CarouselFotos.IsSwipeEnabled = false;
                    AjustarTraslacion(image);
                }
            }
        }

        private void Imagen_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (sender is not Image image)
                return;

            if (image.Scale <= MinScale)
            {
                CarouselFotos.IsSwipeEnabled = true;
                return;
            }

            imagenConZoom = image;
            CarouselFotos.IsSwipeEnabled = false;

            if (e.StatusType == GestureStatus.Started)
            {
                panStartX = image.TranslationX;
                panStartY = image.TranslationY;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                double maxX = ObtenerMaxTranslationX(image);
                double maxY = ObtenerMaxTranslationY(image);

                image.TranslationX = Clamp(panStartX + e.TotalX, -maxX, maxX);
                image.TranslationY = Clamp(panStartY + e.TotalY, -maxY, maxY);
            }
            else if (e.StatusType == GestureStatus.Completed ||
                     e.StatusType == GestureStatus.Canceled)
            {
                AjustarTraslacion(image);
            }
        }

        private void Imagen_DoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Image image)
                return;

            imagenConZoom = image;

            if (image.Scale > MinScale)
            {
                ResetImage(image);
                CarouselFotos.IsSwipeEnabled = true;
            }
            else
            {
                image.AnchorX = 0.5;
                image.AnchorY = 0.5;
                image.Scale = DoubleTapScale;
                image.TranslationX = 0;
                image.TranslationY = 0;

                CarouselFotos.IsSwipeEnabled = false;
            }
        }

        private void CarouselFotos_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (imagenConZoom != null)
            {
                ResetImage(imagenConZoom);
                imagenConZoom = null;
            }

            CarouselFotos.IsSwipeEnabled = true;
        }

        private void ResetImage(Image image)
        {
            image.Scale = MinScale;
            image.TranslationX = 0;
            image.TranslationY = 0;
            image.AnchorX = 0.5;
            image.AnchorY = 0.5;
        }

        private void AjustarTraslacion(Image image)
        {
            double maxX = ObtenerMaxTranslationX(image);
            double maxY = ObtenerMaxTranslationY(image);

            image.TranslationX = Clamp(image.TranslationX, -maxX, maxX);
            image.TranslationY = Clamp(image.TranslationY, -maxY, maxY);
        }

        private double ObtenerMaxTranslationX(Image image)
        {
            if (image.Width <= 0)
                return 0;

            return Math.Max(0, (image.Width * (image.Scale - 1)) / 2);
        }

        private double ObtenerMaxTranslationY(Image image)
        {
            if (image.Height <= 0)
                return 0;

            return Math.Max(0, (image.Height * (image.Scale - 1)) / 2);
        }

        private double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private async void BtnCerrar_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}