using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls.Shapes;

#if ANDROID
using Android.Views;
#endif

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Visor ampliable para fotografías fitosanitarias publicadas en el Álbum.
    /// Imagen y señalización comparten el mismo lienzo para que zoom y arrastre
    /// mantengan las cajas exactamente sobre la evidencia original.
    /// </summary>
    public sealed class ZoomableAlbumFitosanitarioImage : ContentView
    {
        private const double EscalaMinima = 1d;
        private const double EscalaMaxima = 5d;
        private const double EscalaDobleToque = 2.5d;
        private const double ToleranciaEscalaNormal = 0.02d;

        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create(
                nameof(Source),
                typeof(ImageSource),
                typeof(ZoomableAlbumFitosanitarioImage),
                default(ImageSource),
                propertyChanged: OnSourceChanged);

        public static readonly BindableProperty AlbumFotoIdProperty =
            BindableProperty.Create(
                nameof(AlbumFotoId),
                typeof(int),
                typeof(ZoomableAlbumFitosanitarioImage),
                0,
                propertyChanged: OnAlbumFotoIdChanged);

        private readonly Grid raiz;
        private readonly Grid lienzo;
        private readonly Image imagen;
        private readonly AbsoluteLayout capa;
        private readonly Border insignia;
        private readonly Label insigniaTexto;
        private readonly PanGestureRecognizer panGesture;

        private double escalaActual = EscalaMinima;
        private double escalaInicio = EscalaMinima;
        private double desplazamientoX;
        private double desplazamientoY;
        private double panInicioX;
        private double panInicioY;
        private bool panAgregado;
        private bool estaAmpliada;
        private bool cacheConectado;

#if ANDROID
        private Android.Views.View? vistaAndroid;
        private ScaleGestureDetector? detectorEscalaAndroid;
        private GestureDetector? detectorToquesAndroid;
        private float ultimoXAndroid;
        private float ultimoYAndroid;
        private bool arrastrandoAndroid;
        private bool pellizcandoAndroid;
#endif

        public ZoomableAlbumFitosanitarioImage()
        {
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;

            imagen = new Image
            {
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                AnchorX = 0.5,
                AnchorY = 0.5
            };

            capa = new AbsoluteLayout
            {
                InputTransparent = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            lienzo = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                AnchorX = 0.5,
                AnchorY = 0.5,
                IsClippedToBounds = true
            };
            lienzo.Children.Add(imagen);
            lienzo.Children.Add(capa);

            insigniaTexto = new Label
            {
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            insignia = new Border
            {
                IsVisible = false,
                Margin = new Thickness(12),
                Padding = new Thickness(9, 5),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                BackgroundColor = Color.FromArgb("#D9315E52"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(10)
                },
                Content = insigniaTexto
            };

            raiz = new Grid
            {
                IsClippedToBounds = true
            };
            raiz.Children.Add(lienzo);
            raiz.Children.Add(insignia);
            Content = raiz;

            panGesture = new PanGestureRecognizer
            {
                TouchPoints = 1
            };
            panGesture.PanUpdated += OnPanUpdated;

#if !ANDROID
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);

            var dobleToque = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2
            };
            dobleToque.Tapped += OnDobleToque;
            GestureRecognizers.Add(dobleToque);
#endif

            imagen.HandlerChanged += OnImagenHandlerChanged;
            SizeChanged += (_, _) => RenderizarSenalizacion();
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
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

        public bool IsZoomed => estaAmpliada;

        public event EventHandler<AlbumImageZoomChangedEventArgs>? ZoomChanged;

        private static void OnSourceChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is not ZoomableAlbumFitosanitarioImage control)
                return;

            control.imagen.Source = newValue as ImageSource;
            control.RestablecerZoom();
            control.RenderizarSenalizacion();
        }

        private static void OnAlbumFotoIdChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is ZoomableAlbumFitosanitarioImage control)
            {
                control.RestablecerZoom();
                control.RenderizarSenalizacion();
            }
        }

        private void OnImagenHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            ConectarGestosAndroid();
#endif
        }

        private void OnControlLoaded(object? sender, EventArgs e)
        {
            if (!cacheConectado)
            {
                AlbumFitosanitarioSenalizacionCache.Actualizado +=
                    OnCacheActualizado;
                cacheConectado = true;
            }

#if ANDROID
            ConectarGestosAndroid();
#endif
            RenderizarSenalizacion();
        }

        private void OnControlUnloaded(object? sender, EventArgs e)
        {
            if (cacheConectado)
            {
                AlbumFitosanitarioSenalizacionCache.Actualizado -=
                    OnCacheActualizado;
                cacheConectado = false;
            }

#if ANDROID
            DesconectarGestosAndroid();
#endif
        }

        private void OnCacheActualizado(object? sender, int albumId) =>
            Dispatcher.Dispatch(RenderizarSenalizacion);

#if !ANDROID
        private void OnPinchUpdated(
            object? sender,
            PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                escalaInicio = lienzo.Scale;
                escalaActual = lienzo.Scale;
                desplazamientoX = lienzo.TranslationX;
                desplazamientoY = lienzo.TranslationY;
                return;
            }

            if (e.Status == GestureStatus.Running)
            {
                escalaActual += (e.Scale - 1d) * escalaInicio;
                escalaActual = Limitar(
                    escalaActual,
                    EscalaMinima,
                    EscalaMaxima);

                lienzo.Scale = escalaActual;

                if (EsEscalaNormal(escalaActual))
                {
                    lienzo.TranslationX = 0;
                    lienzo.TranslationY = 0;
                }
                else
                {
                    double ancho = Math.Max(Width, 1d);
                    double alto = Math.Max(Height, 1d);
                    double origenX = e.ScaleOrigin.X - 0.5d;
                    double origenY = e.ScaleOrigin.Y - 0.5d;
                    double incremento = escalaActual - escalaInicio;

                    lienzo.TranslationX =
                        desplazamientoX - (origenX * ancho * incremento);
                    lienzo.TranslationY =
                        desplazamientoY - (origenY * alto * incremento);

                    LimitarTraslacionActual();
                }

                ActualizarEstadoZoom(!EsEscalaNormal(escalaActual));
                return;
            }

            if (e.Status is GestureStatus.Completed or GestureStatus.Canceled)
            {
                if (EsEscalaNormal(lienzo.Scale))
                {
                    RestablecerZoom();
                }
                else
                {
                    escalaActual = lienzo.Scale;
                    desplazamientoX = lienzo.TranslationX;
                    desplazamientoY = lienzo.TranslationY;
                    LimitarTraslacionActual();
                    ActualizarEstadoZoom(true);
                    HabilitarPanSiCorresponde();
                }
            }
        }
#endif

        private void OnPanUpdated(
            object? sender,
            PanUpdatedEventArgs e)
        {
            if (!estaAmpliada || EsEscalaNormal(lienzo.Scale))
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    panInicioX = lienzo.TranslationX;
                    panInicioY = lienzo.TranslationY;
                    break;

                case GestureStatus.Running:
                    lienzo.TranslationX = panInicioX + e.TotalX;
                    lienzo.TranslationY = panInicioY + e.TotalY;
                    LimitarTraslacionActual();
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    LimitarTraslacionActual();
                    desplazamientoX = lienzo.TranslationX;
                    desplazamientoY = lienzo.TranslationY;
                    break;
            }
        }

        private void OnDobleToque(
            object? sender,
            TappedEventArgs e) =>
            AlternarDobleToque();

        private void AlternarDobleToque()
        {
            if (estaAmpliada || !EsEscalaNormal(lienzo.Scale))
            {
                RestablecerZoom();
                return;
            }

            escalaActual = EscalaDobleToque;
            escalaInicio = EscalaDobleToque;
            desplazamientoX = 0;
            desplazamientoY = 0;

            lienzo.Scale = EscalaDobleToque;
            lienzo.TranslationX = 0;
            lienzo.TranslationY = 0;

            ActualizarEstadoZoom(true);
            HabilitarPanSiCorresponde();
        }

        public void RestablecerZoom()
        {
            escalaActual = EscalaMinima;
            escalaInicio = EscalaMinima;
            desplazamientoX = 0;
            desplazamientoY = 0;
            panInicioX = 0;
            panInicioY = 0;

            lienzo.Scale = EscalaMinima;
            lienzo.TranslationX = 0;
            lienzo.TranslationY = 0;
            lienzo.AnchorX = 0.5;
            lienzo.AnchorY = 0.5;

            DeshabilitarPanSiCorresponde();
            ActualizarEstadoZoom(false);

#if ANDROID
            arrastrandoAndroid = false;
            pellizcandoAndroid = false;
            PermitirIntercepcionPadreAndroid();
#endif
        }

        private void HabilitarPanSiCorresponde()
        {
#if !ANDROID
            if (panAgregado || !estaAmpliada)
                return;

            GestureRecognizers.Add(panGesture);
            panAgregado = true;
#endif
        }

        private void DeshabilitarPanSiCorresponde()
        {
#if !ANDROID
            if (!panAgregado)
                return;

            GestureRecognizers.Remove(panGesture);
            panAgregado = false;
#endif
        }

        private void LimitarTraslacionActual()
        {
            if (EsEscalaNormal(lienzo.Scale))
            {
                lienzo.TranslationX = 0;
                lienzo.TranslationY = 0;
                return;
            }

            double ancho = lienzo.Width > 0 ? lienzo.Width : Width;
            double alto = lienzo.Height > 0 ? lienzo.Height : Height;

            if (ancho <= 0 || alto <= 0)
                return;

            double limiteX = Math.Max(
                0,
                ancho * (lienzo.Scale - EscalaMinima) / 2d);
            double limiteY = Math.Max(
                0,
                alto * (lienzo.Scale - EscalaMinima) / 2d);

            lienzo.TranslationX = Limitar(
                lienzo.TranslationX,
                -limiteX,
                limiteX);
            lienzo.TranslationY = Limitar(
                lienzo.TranslationY,
                -limiteY,
                limiteY);
        }

        private void ActualizarEstadoZoom(bool ampliada)
        {
            if (estaAmpliada == ampliada)
                return;

            estaAmpliada = ampliada;

            if (estaAmpliada)
                HabilitarPanSiCorresponde();
            else
                DeshabilitarPanSiCorresponde();

            ZoomChanged?.Invoke(
                this,
                new AlbumImageZoomChangedEventArgs(estaAmpliada));
        }

        private static bool EsEscalaNormal(double escala) =>
            escala <= EscalaMinima + ToleranciaEscalaNormal;

        private static double Limitar(
            double valor,
            double minimo,
            double maximo) =>
            Math.Max(minimo, Math.Min(maximo, valor));

        private void RenderizarSenalizacion()
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

            // AspectFit: se conservan márgenes cuando la relación no coincide.
            double escala = Math.Min(
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
                        StrokeThickness = 2.6,
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

#if ANDROID
        private void ConectarGestosAndroid()
        {
            if (imagen.Handler?.PlatformView is not Android.Views.View nuevaVista)
                return;

            if (ReferenceEquals(vistaAndroid, nuevaVista) &&
                detectorEscalaAndroid != null)
            {
                return;
            }

            DesconectarGestosAndroid();
            vistaAndroid = nuevaVista;

            detectorEscalaAndroid = new ScaleGestureDetector(
                nuevaVista.Context,
                new AlbumScaleListener(this));
            detectorToquesAndroid = new GestureDetector(
                nuevaVista.Context,
                new AlbumTapListener(this));

            nuevaVista.Touch += OnAndroidTouch;
            nuevaVista.Clickable = true;
        }

        private void DesconectarGestosAndroid()
        {
            if (vistaAndroid != null)
                vistaAndroid.Touch -= OnAndroidTouch;

            detectorEscalaAndroid?.Dispose();
            detectorToquesAndroid?.Dispose();
            detectorEscalaAndroid = null;
            detectorToquesAndroid = null;
            vistaAndroid = null;
        }

        private void OnAndroidTouch(
            object? sender,
            Android.Views.View.TouchEventArgs e)
        {
            MotionEvent? evento = e.Event;
            if (evento == null)
            {
                e.Handled = false;
                return;
            }

            detectorEscalaAndroid?.OnTouchEvent(evento);
            detectorToquesAndroid?.OnTouchEvent(evento);

            int cantidadDedos = evento.PointerCount;
            MotionEventActions accion = evento.ActionMasked;

            if (accion == MotionEventActions.PointerUp && cantidadDedos <= 2)
            {
                pellizcandoAndroid = false;

                if (estaAmpliada || !EsEscalaNormal(lienzo.Scale))
                {
                    PrepararArrastreConDedoRestanteAndroid(evento);
                    BloquearIntercepcionPadreAndroid();
                    e.Handled = true;
                    return;
                }
            }

            if (accion == MotionEventActions.Down && cantidadDedos == 1)
            {
                pellizcandoAndroid = false;
                arrastrandoAndroid = false;
            }

            if (cantidadDedos >= 2)
            {
                pellizcandoAndroid = true;
                BloquearIntercepcionPadreAndroid();
                e.Handled = true;
                return;
            }

            if (estaAmpliada || !EsEscalaNormal(lienzo.Scale))
            {
                BloquearIntercepcionPadreAndroid();
                ProcesarArrastreAndroid(evento, accion);

                if (accion is MotionEventActions.Up or MotionEventActions.Cancel)
                    pellizcandoAndroid = false;

                e.Handled = true;
                return;
            }

            if (accion is MotionEventActions.Up or MotionEventActions.Cancel)
            {
                pellizcandoAndroid = false;
                arrastrandoAndroid = false;
                PermitirIntercepcionPadreAndroid();
            }

            e.Handled = true;
        }

        private void PrepararArrastreConDedoRestanteAndroid(
            MotionEvent evento)
        {
            if (evento.PointerCount < 2)
            {
                arrastrandoAndroid = false;
                return;
            }

            int indiceLevantado = evento.ActionIndex;
            int indiceRestante = indiceLevantado == 0 ? 1 : 0;

            ultimoXAndroid = evento.GetX(indiceRestante);
            ultimoYAndroid = evento.GetY(indiceRestante);
            arrastrandoAndroid = true;
        }

        private void ProcesarArrastreAndroid(
            MotionEvent evento,
            MotionEventActions accion)
        {
            double densidad =
                Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo.Density;
            if (densidad <= 0)
                densidad = 1d;

            float x = evento.GetX();
            float y = evento.GetY();

            switch (accion)
            {
                case MotionEventActions.Down:
                    ultimoXAndroid = x;
                    ultimoYAndroid = y;
                    arrastrandoAndroid = true;
                    break;

                case MotionEventActions.Move:
                    if (!arrastrandoAndroid || pellizcandoAndroid)
                    {
                        ultimoXAndroid = x;
                        ultimoYAndroid = y;
                        arrastrandoAndroid = true;
                        return;
                    }

                    double deltaX = (x - ultimoXAndroid) / densidad;
                    double deltaY = (y - ultimoYAndroid) / densidad;
                    ultimoXAndroid = x;
                    ultimoYAndroid = y;

                    lienzo.TranslationX += deltaX;
                    lienzo.TranslationY += deltaY;
                    LimitarTraslacionActual();
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    arrastrandoAndroid = false;
                    desplazamientoX = lienzo.TranslationX;
                    desplazamientoY = lienzo.TranslationY;
                    LimitarTraslacionActual();
                    break;
            }
        }

        private void AplicarEscalaAndroid(float factorEscala)
        {
            if (factorEscala <= 0 || float.IsNaN(factorEscala))
                return;

            double nuevaEscala = Limitar(
                lienzo.Scale * factorEscala,
                EscalaMinima,
                EscalaMaxima);

            lienzo.Scale = nuevaEscala;
            escalaActual = nuevaEscala;
            escalaInicio = nuevaEscala;

            if (EsEscalaNormal(nuevaEscala))
            {
                lienzo.TranslationX = 0;
                lienzo.TranslationY = 0;
                desplazamientoX = 0;
                desplazamientoY = 0;
                ActualizarEstadoZoom(false);
            }
            else
            {
                LimitarTraslacionActual();
                desplazamientoX = lienzo.TranslationX;
                desplazamientoY = lienzo.TranslationY;
                ActualizarEstadoZoom(true);
            }
        }

        private void FinalizarEscalaAndroid()
        {
            pellizcandoAndroid = false;

            if (EsEscalaNormal(lienzo.Scale))
            {
                RestablecerZoom();
                return;
            }

            LimitarTraslacionActual();
            desplazamientoX = lienzo.TranslationX;
            desplazamientoY = lienzo.TranslationY;
            ActualizarEstadoZoom(true);
            BloquearIntercepcionPadreAndroid();
        }

        private void BloquearIntercepcionPadreAndroid() =>
            vistaAndroid?.Parent?.RequestDisallowInterceptTouchEvent(true);

        private void PermitirIntercepcionPadreAndroid() =>
            vistaAndroid?.Parent?.RequestDisallowInterceptTouchEvent(false);

        private sealed class AlbumScaleListener :
            ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly ZoomableAlbumFitosanitarioImage owner;

            public AlbumScaleListener(ZoomableAlbumFitosanitarioImage owner)
            {
                this.owner = owner;
            }

            public override bool OnScaleBegin(ScaleGestureDetector detector)
            {
                owner.pellizcandoAndroid = true;
                owner.BloquearIntercepcionPadreAndroid();
                return true;
            }

            public override bool OnScale(ScaleGestureDetector detector)
            {
                owner.AplicarEscalaAndroid(detector.ScaleFactor);
                return true;
            }

            public override void OnScaleEnd(ScaleGestureDetector detector) =>
                owner.FinalizarEscalaAndroid();
        }

        private sealed class AlbumTapListener :
            GestureDetector.SimpleOnGestureListener
        {
            private readonly ZoomableAlbumFitosanitarioImage owner;

            public AlbumTapListener(ZoomableAlbumFitosanitarioImage owner)
            {
                this.owner = owner;
            }

            public override bool OnDown(MotionEvent e) => true;

            public override bool OnDoubleTap(MotionEvent e)
            {
                owner.AlternarDobleToque();

                if (owner.estaAmpliada)
                    owner.BloquearIntercepcionPadreAndroid();
                else
                    owner.PermitirIntercepcionPadreAndroid();

                return true;
            }
        }
#endif
    }
}
