using Microsoft.Maui.Controls;

#if ANDROID
using Android.Views;
#endif

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Imagen reutilizable del Álbum Botánico con zoom táctil.
    ///
    /// Android utiliza sus detectores multitáctiles nativos para que el
    /// pellizco pueda convivir con el CarouselView. El carrusel continúa
    /// recibiendo el swipe normal cuando la fotografía está en 1x y solo se
    /// bloquea mientras existen dos dedos sobre la imagen o hay zoom activo.
    ///
    /// En las demás plataformas se conserva la implementación de gestos MAUI.
    /// </summary>
    public sealed class ZoomableAlbumImage : ContentView
    {
        private const double EscalaMinima = 1d;
        private const double EscalaMaxima = 5d;
        private const double EscalaDobleToque = 2.5d;
        private const double ToleranciaEscalaNormal = 0.02d;

        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create(
                nameof(Source),
                typeof(ImageSource),
                typeof(ZoomableAlbumImage),
                default(ImageSource),
                propertyChanged: OnSourceChanged);

        private readonly Image imagen;
        private readonly PanGestureRecognizer panGesture;

        // Estado usado por la implementación MAUI de iOS/Mac/Windows.
        private double escalaActual = EscalaMinima;
        private double escalaInicio = EscalaMinima;
        private double desplazamientoX;
        private double desplazamientoY;
        private double panInicioX;
        private double panInicioY;
        private bool panAgregado;
        private bool estaAmpliada;

#if ANDROID
        private Android.Views.View? vistaAndroid;
        private ScaleGestureDetector? detectorEscalaAndroid;
        private GestureDetector? detectorToquesAndroid;
        private float ultimoXAndroid;
        private float ultimoYAndroid;
        private bool arrastrandoAndroid;
        private bool pellizcandoAndroid;
#endif

        public ZoomableAlbumImage()
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

            Content = imagen;

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
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
        }

        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Indica si la fotografía está actualmente ampliada.
        /// </summary>
        public bool IsZoomed => estaAmpliada;

        /// <summary>
        /// Notifica los cambios de estado del zoom. Se conserva como evento
        /// público por si otro visor necesita reaccionar a este estado.
        /// </summary>
        public event EventHandler<AlbumImageZoomChangedEventArgs>? ZoomChanged;

        private static void OnSourceChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is not ZoomableAlbumImage control)
                return;

            control.imagen.Source = newValue as ImageSource;
            control.RestablecerZoom();
        }

        private void OnImagenHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            ConectarGestosAndroid();
#endif
        }

        private void OnControlLoaded(object? sender, EventArgs e)
        {
#if ANDROID
            ConectarGestosAndroid();
#endif
        }

        private void OnControlUnloaded(object? sender, EventArgs e)
        {
#if ANDROID
            DesconectarGestosAndroid();
#endif
        }

#if !ANDROID
        /// <summary>
        /// Implementación multiplataforma basada en el algoritmo recomendado
        /// por .NET MAUI. e.Scale es incremental, por eso se acumula en
        /// escalaActual en lugar de multiplicarlo siempre por la escala inicial.
        /// </summary>
        private void OnPinchUpdated(
            object? sender,
            PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                escalaInicio = imagen.Scale;
                escalaActual = imagen.Scale;
                desplazamientoX = imagen.TranslationX;
                desplazamientoY = imagen.TranslationY;
                return;
            }

            if (e.Status == GestureStatus.Running)
            {
                escalaActual += (e.Scale - 1d) * escalaInicio;
                escalaActual = Limitar(
                    escalaActual,
                    EscalaMinima,
                    EscalaMaxima);

                imagen.Scale = escalaActual;

                if (EsEscalaNormal(escalaActual))
                {
                    imagen.TranslationX = 0;
                    imagen.TranslationY = 0;
                }
                else
                {
                    double ancho = Math.Max(Width, 1d);
                    double alto = Math.Max(Height, 1d);

                    double origenX = e.ScaleOrigin.X - 0.5d;
                    double origenY = e.ScaleOrigin.Y - 0.5d;
                    double incremento = escalaActual - escalaInicio;

                    imagen.TranslationX =
                        desplazamientoX - (origenX * ancho * incremento);
                    imagen.TranslationY =
                        desplazamientoY - (origenY * alto * incremento);

                    LimitarTraslacionActual();
                }

                ActualizarEstadoZoom(!EsEscalaNormal(escalaActual));
                return;
            }

            if (e.Status is GestureStatus.Completed or GestureStatus.Canceled)
            {
                if (EsEscalaNormal(imagen.Scale))
                {
                    RestablecerZoom();
                }
                else
                {
                    escalaActual = imagen.Scale;
                    desplazamientoX = imagen.TranslationX;
                    desplazamientoY = imagen.TranslationY;
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
            if (!estaAmpliada || EsEscalaNormal(imagen.Scale))
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    panInicioX = imagen.TranslationX;
                    panInicioY = imagen.TranslationY;
                    break;

                case GestureStatus.Running:
                    imagen.TranslationX = panInicioX + e.TotalX;
                    imagen.TranslationY = panInicioY + e.TotalY;
                    LimitarTraslacionActual();
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    LimitarTraslacionActual();
                    desplazamientoX = imagen.TranslationX;
                    desplazamientoY = imagen.TranslationY;
                    break;
            }
        }

        private void OnDobleToque(
            object? sender,
            TappedEventArgs e)
        {
            AlternarDobleToque();
        }

        private void AlternarDobleToque()
        {
            if (estaAmpliada || !EsEscalaNormal(imagen.Scale))
            {
                RestablecerZoom();
                return;
            }

            escalaActual = EscalaDobleToque;
            escalaInicio = EscalaDobleToque;
            desplazamientoX = 0;
            desplazamientoY = 0;

            imagen.Scale = EscalaDobleToque;
            imagen.TranslationX = 0;
            imagen.TranslationY = 0;

            ActualizarEstadoZoom(true);
            HabilitarPanSiCorresponde();
        }

        /// <summary>
        /// Devuelve la fotografía a su tamaño original y al centro.
        /// </summary>
        public void RestablecerZoom()
        {
            escalaActual = EscalaMinima;
            escalaInicio = EscalaMinima;
            desplazamientoX = 0;
            desplazamientoY = 0;
            panInicioX = 0;
            panInicioY = 0;

            imagen.Scale = EscalaMinima;
            imagen.TranslationX = 0;
            imagen.TranslationY = 0;
            imagen.AnchorX = 0.5;
            imagen.AnchorY = 0.5;

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
            if (EsEscalaNormal(imagen.Scale))
            {
                imagen.TranslationX = 0;
                imagen.TranslationY = 0;
                return;
            }

            double ancho = imagen.Width > 0 ? imagen.Width : Width;
            double alto = imagen.Height > 0 ? imagen.Height : Height;

            if (ancho <= 0 || alto <= 0)
                return;

            double limiteX =
                Math.Max(0, ancho * (imagen.Scale - EscalaMinima) / 2d);
            double limiteY =
                Math.Max(0, alto * (imagen.Scale - EscalaMinima) / 2d);

            imagen.TranslationX = Limitar(
                imagen.TranslationX,
                -limiteX,
                limiteX);

            imagen.TranslationY = Limitar(
                imagen.TranslationY,
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

#if ANDROID
        /// <summary>
        /// Android: el detector nativo distingue correctamente el segundo dedo
        /// y evita que el RecyclerView interno del CarouselView intercepte el
        /// gesto mientras se está pellizcando o desplazando una imagen ampliada.
        /// </summary>
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

            var listenerEscala =
                new AlbumScaleListener(this);

            detectorEscalaAndroid =
                new ScaleGestureDetector(
                    nuevaVista.Context,
                    listenerEscala);

            var listenerToques =
                new AlbumTapListener(this);

            detectorToquesAndroid =
                new GestureDetector(
                    nuevaVista.Context,
                    listenerToques);

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

            /*
             * ACTION_POINTER_UP todavía informa los dos punteros en
             * PointerCount, incluido el que acaba de levantarse. Si aquí se
             * volviera a marcar pellizcandoAndroid=true, el siguiente gesto
             * de un dedo nunca llegaría a mover la fotografía.
             *
             * Cuando queda un dedo sobre la imagen ampliada se toma su
             * posición como nuevo origen del pan. Esto permite pasar de
             * pellizcar a arrastrar sin tener que levantar ambos dedos.
             */
            if (accion == MotionEventActions.PointerUp &&
                cantidadDedos <= 2)
            {
                pellizcandoAndroid = false;

                if (estaAmpliada || !EsEscalaNormal(imagen.Scale))
                {
                    PrepararArrastreConDedoRestanteAndroid(evento);
                    BloquearIntercepcionPadreAndroid();
                    e.Handled = true;
                    return;
                }
            }

            /*
             * Un ACTION_DOWN inicia una secuencia táctil completamente nueva.
             * Se limpia cualquier estado residual del pellizco anterior antes
             * de comenzar el desplazamiento de una fotografía ampliada.
             */
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

            if (estaAmpliada || !EsEscalaNormal(imagen.Scale))
            {
                BloquearIntercepcionPadreAndroid();
                ProcesarArrastreAndroid(evento, accion);

                if (accion is MotionEventActions.Up or
                    MotionEventActions.Cancel)
                {
                    pellizcandoAndroid = false;
                }

                e.Handled = true;
                return;
            }

            if (accion is MotionEventActions.Up or
                MotionEventActions.Cancel)
            {
                pellizcandoAndroid = false;
                arrastrandoAndroid = false;
                PermitirIntercepcionPadreAndroid();
            }

            /*
             * El evento se marca como manejado en DOWN para que esta vista
             * siga recibiendo ACTION_POINTER_DOWN. El CarouselView padre puede
             * interceptar posteriormente el MOVE de un solo dedo porque no se
             * ha solicitado bloquear su intercepción.
             */
            e.Handled = true;
        }

        /// <summary>
        /// Conserva el dedo que permanece en pantalla al terminar un pellizco
        /// como punto inicial del desplazamiento de la imagen ampliada.
        /// </summary>
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

                    imagen.TranslationX += deltaX;
                    imagen.TranslationY += deltaY;
                    LimitarTraslacionActual();
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    arrastrandoAndroid = false;
                    desplazamientoX = imagen.TranslationX;
                    desplazamientoY = imagen.TranslationY;
                    LimitarTraslacionActual();
                    break;
            }
        }

        private void AplicarEscalaAndroid(float factorEscala)
        {
            if (factorEscala <= 0 || float.IsNaN(factorEscala))
                return;

            double nuevaEscala = Limitar(
                imagen.Scale * factorEscala,
                EscalaMinima,
                EscalaMaxima);

            imagen.Scale = nuevaEscala;
            escalaActual = nuevaEscala;
            escalaInicio = nuevaEscala;

            if (EsEscalaNormal(nuevaEscala))
            {
                imagen.TranslationX = 0;
                imagen.TranslationY = 0;
                desplazamientoX = 0;
                desplazamientoY = 0;
                ActualizarEstadoZoom(false);
            }
            else
            {
                LimitarTraslacionActual();
                desplazamientoX = imagen.TranslationX;
                desplazamientoY = imagen.TranslationY;
                ActualizarEstadoZoom(true);
            }
        }

        private void FinalizarEscalaAndroid()
        {
            pellizcandoAndroid = false;

            if (EsEscalaNormal(imagen.Scale))
            {
                RestablecerZoom();
                return;
            }

            LimitarTraslacionActual();
            desplazamientoX = imagen.TranslationX;
            desplazamientoY = imagen.TranslationY;
            ActualizarEstadoZoom(true);
            BloquearIntercepcionPadreAndroid();
        }

        private void BloquearIntercepcionPadreAndroid()
        {
            vistaAndroid?.Parent?.RequestDisallowInterceptTouchEvent(true);
        }

        private void PermitirIntercepcionPadreAndroid()
        {
            vistaAndroid?.Parent?.RequestDisallowInterceptTouchEvent(false);
        }

        private sealed class AlbumScaleListener :
            ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly ZoomableAlbumImage owner;

            public AlbumScaleListener(ZoomableAlbumImage owner)
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

            public override void OnScaleEnd(ScaleGestureDetector detector)
            {
                owner.FinalizarEscalaAndroid();
            }
        }

        private sealed class AlbumTapListener :
            GestureDetector.SimpleOnGestureListener
        {
            private readonly ZoomableAlbumImage owner;

            public AlbumTapListener(ZoomableAlbumImage owner)
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

    public sealed class AlbumImageZoomChangedEventArgs : EventArgs
    {
        public AlbumImageZoomChangedEventArgs(bool isZoomed)
        {
            IsZoomed = isZoomed;
        }

        public bool IsZoomed { get; }
    }
}
