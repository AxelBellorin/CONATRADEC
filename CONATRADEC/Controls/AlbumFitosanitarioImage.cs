using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Imagen del Álbum Botánico que superpone, únicamente cuando la fotografía
    /// proviene de una publicación fitosanitaria, las regiones del diagnóstico
    /// publicado. El archivo original no se modifica ni se duplica.
    /// </summary>
    public sealed class AlbumFitosanitarioImage : ContentView
    {
        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create(
                nameof(Source),
                typeof(ImageSource),
                typeof(AlbumFitosanitarioImage),
                default(ImageSource),
                propertyChanged: OnSourceChanged);

        public static readonly BindableProperty AlbumFotoIdProperty =
            BindableProperty.Create(
                nameof(AlbumFotoId),
                typeof(int),
                typeof(AlbumFitosanitarioImage),
                0,
                propertyChanged: OnAlbumFotoIdChanged);

        private readonly Grid contenedor;
        private readonly Image imagen;
        private readonly AbsoluteLayout capa;
        private readonly Border insignia;
        private readonly Label insigniaTexto;
        private bool conectado;

        public AlbumFitosanitarioImage()
        {
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;

            imagen = new Image
            {
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            capa = new AbsoluteLayout
            {
                InputTransparent = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            insigniaTexto = new Label
            {
                FontSize = 9.5,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            insignia = new Border
            {
                IsVisible = false,
                Margin = new Thickness(9),
                Padding = new Thickness(7, 4),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                BackgroundColor = Color.FromArgb("#D9315E52"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(9)
                },
                Content = insigniaTexto
            };

            contenedor = new Grid
            {
                IsClippedToBounds = true
            };
            contenedor.Children.Add(imagen);
            contenedor.Children.Add(capa);
            contenedor.Children.Add(insignia);

            Content = contenedor;

            SizeChanged += (_, _) => Renderizar();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public int AlbumFotoId
        {
            get => (int)GetValue(AlbumFotoIdProperty);
            set => SetValue(AlbumFotoIdProperty, value);
        }

        private static void OnSourceChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AlbumFitosanitarioImage control)
            {
                control.imagen.Source = newValue as ImageSource;
                control.Renderizar();
            }
        }

        private static void OnAlbumFotoIdChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is AlbumFitosanitarioImage control)
                control.Renderizar();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (!conectado)
            {
                AlbumFitosanitarioSenalizacionCache.Actualizado +=
                    OnCacheActualizado;
                conectado = true;
            }

            Renderizar();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            if (!conectado)
                return;

            AlbumFitosanitarioSenalizacionCache.Actualizado -=
                OnCacheActualizado;
            conectado = false;
        }

        private void OnCacheActualizado(object? sender, int albumId) =>
            Dispatcher.Dispatch(Renderizar);

        private void Renderizar()
        {
            capa.Children.Clear();
            insignia.IsVisible = false;

            if (AlbumFotoId <= 0 || Width <= 0 || Height <= 0)
                return;

            if (!AlbumFitosanitarioSenalizacionCache.TryObtener(
                    AlbumFotoId,
                    out AlbumFotoSenalizacionResponse? datos) ||
                datos == null ||
                !datos.EsFitosanitaria)
            {
                return;
            }

            insigniaTexto.Text = datos.TieneSenalizacion
                ? "Con señalización"
                : "Sin señalización";
            insignia.BackgroundColor = Color.FromArgb(
                datos.TieneSenalizacion
                    ? "#D9315E52"
                    : "#D95F6B67");
            insignia.IsVisible = true;

            if (!datos.TieneSenalizacion ||
                datos.AnchoImagen is not > 0 ||
                datos.AltoImagen is not > 0)
            {
                return;
            }

            double anchoOriginal = datos.AnchoImagen.Value;
            double altoOriginal = datos.AltoImagen.Value;

            // AspectFill: la fotografía puede recortarse en los bordes.
            double escala = Math.Max(
                Width / anchoOriginal,
                Height / altoOriginal);
            double anchoRender = anchoOriginal * escala;
            double altoRender = altoOriginal * escala;
            double offsetX = (Width - anchoRender) / 2d;
            double offsetY = (Height - altoRender) / 2d;

            foreach (AlbumFitosanitarioCapa diagnostico in datos.Capas ?? [])
            {
                Color color = ResolverColor(diagnostico.ColorMarcador);

                foreach (AlbumFitosanitarioLesion lesion in
                         diagnostico.Lesiones ?? [])
                {
                    if (!TryCrearRectangulo(
                            lesion.Box2d,
                            anchoOriginal,
                            altoOriginal,
                            escala,
                            offsetX,
                            offsetY,
                            out Rect bounds))
                    {
                        continue;
                    }

                    var marca = new Border
                    {
                        InputTransparent = true,
                        BackgroundColor = Colors.Transparent,
                        Stroke = color,
                        StrokeThickness = 2.4,
                        StrokeShape = new RoundRectangle
                        {
                            CornerRadius = new CornerRadius(3)
                        }
                    };

                    AbsoluteLayout.SetLayoutBounds(marca, bounds);
                    AbsoluteLayout.SetLayoutFlags(
                        marca,
                        Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
                    capa.Children.Add(marca);
                }
            }
        }

        private static bool TryCrearRectangulo(
            IReadOnlyList<int>? box,
            double anchoOriginal,
            double altoOriginal,
            double escala,
            double offsetX,
            double offsetY,
            out Rect rect)
        {
            rect = Rect.Zero;

            if (box == null || box.Count != 4 ||
                anchoOriginal <= 0 || altoOriginal <= 0)
            {
                return false;
            }

            int ymin = Math.Clamp(box[0], 0, 1000);
            int xmin = Math.Clamp(box[1], 0, 1000);
            int ymax = Math.Clamp(box[2], 0, 1000);
            int xmax = Math.Clamp(box[3], 0, 1000);

            if (ymin >= ymax || xmin >= xmax)
                return false;

            double x = offsetX + xmin / 1000d * anchoOriginal * escala;
            double y = offsetY + ymin / 1000d * altoOriginal * escala;
            double ancho = (xmax - xmin) / 1000d * anchoOriginal * escala;
            double alto = (ymax - ymin) / 1000d * altoOriginal * escala;

            if (ancho < 2 || alto < 2)
                return false;

            rect = new Rect(x, y, ancho, alto);
            return true;
        }

        private static Color ResolverColor(string? hexadecimal)
        {
            try
            {
                return Color.FromArgb(
                    string.IsNullOrWhiteSpace(hexadecimal)
                        ? "#E53935"
                        : hexadecimal);
            }
            catch
            {
                return Color.FromArgb("#E53935");
            }
        }
    }
}
