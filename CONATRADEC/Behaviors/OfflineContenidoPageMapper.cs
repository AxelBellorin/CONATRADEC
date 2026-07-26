using CONATRADEC.Controls;
using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.Handlers;
using System.Threading;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Agrega los indicadores automáticos de Noticias y Álbum.
    /// También inicia la verificación global después de que el dispositivo fue
    /// preparado por primera vez para trabajar sin conexión.
    /// </summary>
    public static class OfflineContenidoPageMapper
    {
        private static int registrado;

        private static readonly BindableProperty
            ConfiguradoProperty =
                BindableProperty.CreateAttached(
                    "Configurado",
                    typeof(bool),
                    typeof(OfflineContenidoPageMapper),
                    false);

        public static void Register()
        {
            if (Interlocked.Exchange(
                    ref registrado,
                    1) == 1)
            {
                return;
            }

            PageHandler.Mapper.AppendToMapping(
                nameof(OfflineContenidoPageMapper),
                static (_, view) =>
                {
                    if (view is not ContentPage page)
                        return;

                    bool permiteTrabajoOffline =
                        DatosSinConexionPermisos.TienePermiso;

                    if (permiteTrabajoOffline)
                    {
                        SincronizacionOfflineGlobalService
                            .Instance
                            .VerificarActualizacionesEnSegundoPlano();
                    }

                    /*
                     * Sin el permiso global, Noticias y Álbum funcionan
                     * exclusivamente contra la API y Nuevo análisis no muestra
                     * el selector del motor local.
                     */
                    bool esNuevoAnalisis =
                        string.Equals(
                            page.GetType().Name,
                            "NuevoAnalisisFormPage",
                            StringComparison.OrdinalIgnoreCase);

                    bool esPaginaPrincipal =
                        string.Equals(
                            page.GetType().Name,
                            "MainPage",
                            StringComparison.OrdinalIgnoreCase);

                    if (!permiteTrabajoOffline ||
                        (!esNuevoAnalisis &&
                         !esPaginaPrincipal &&
                         page is not noticiasPage &&
                         page is not albumFotosPage))
                    {
                        return;
                    }

                    page.Loaded -=
                        OnPageLoaded;

                    page.Loaded +=
                        OnPageLoaded;

                    page.Dispatcher.Dispatch(
                        () =>
                            ConfigurarPagina(
                                page));
                });
        }

        private static void OnPageLoaded(
            object? sender,
            EventArgs e)
        {
            if (sender is not ContentPage page)
                return;

            page.Loaded -=
                OnPageLoaded;

            ConfigurarPagina(
                page);
        }

        private static void ConfigurarPagina(
            ContentPage page)
        {
            if ((bool)page.GetValue(
                    ConfiguradoProperty))
            {
                return;
            }

            bool configurado;

            if (string.Equals(
                    page.GetType().Name,
                    "MainPage",
                    StringComparison.OrdinalIgnoreCase))
            {
                configurado =
                    ConfigurarPaginaPrincipal(
                        page);
            }
            else if (string.Equals(
                    page.GetType().Name,
                    "NuevoAnalisisFormPage",
                    StringComparison.OrdinalIgnoreCase))
            {
                configurado =
                    ConfigurarNuevoAnalisis(
                        page);
            }
            else
            {
                configurado =
                    page switch
                    {
                        noticiasPage noticias =>
                            ConfigurarNoticias(
                                noticias),

                        albumFotosPage album =>
                            ConfigurarAlbum(
                                album),

                        _ =>
                            false
                    };
            }

            if (configurado)
            {
                page.SetValue(
                    ConfiguradoProperty,
                    true);
            }
        }

        private static bool ConfigurarPaginaPrincipal(
            ContentPage page)
        {
            CollectionView? listado =
                page.FindByName<CollectionView>(
                    "AnalisisCollectionView");

            if (listado?.Header is not View encabezado)
                return false;

            ImageButton? botonNuevo =
                BuscarBotonNuevoAnalisis(
                    encabezado);

            if (botonNuevo == null)
                return false;

            botonNuevo.Command =
                new Command(
                    async () =>
                        await AbrirNuevoAnalisisAsync(
                            page));

            /*
             * El listado puede estar esperando una respuesta del servidor.
             * La creación offline no debe depender de esa consulta.
             */
            botonNuevo.IsEnabled = true;

            return true;
        }

        private static async Task AbrirNuevoAnalisisAsync(
            ContentPage page)
        {
            if (page.BindingContext is not MainPageViewModel viewModel)
                return;

            if (!viewModel.CanAdd)
            {
                await page.DisplayAlert(
                    "Acceso denegado",
                    "No tiene permisos para registrar análisis.",
                    "Aceptar");
                return;
            }

            if (viewModel.IsBusy)
                return;

            bool trabajarOffline =
                SesionOfflineService.SesionActualEsOffline ||
                !EstadoConexionService.Instance.HayInternet;

            if (trabajarOffline)
            {
                if (!DatosSinConexionPermisos.TienePermiso)
                {
                    await page.DisplayAlert(
                        "Trabajo sin conexión",
                        "Su usuario no tiene habilitado el trabajo sin conexión.",
                        "Aceptar");
                    return;
                }

                bool motorDisponible =
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync();

                if (!motorDisponible)
                {
                    await page.DisplayAlert(
                        "Motor no disponible",
                        "Este dispositivo no tiene un motor de cálculo válido. Conéctese y pulse Actualizar todo.",
                        "Aceptar");
                    return;
                }

                await ModoTrabajoAnalisisService.Instance
                    .PrepararNuevoAnalisisAsync();
            }

            viewModel.CancelarCarga();
            AnalisisEdicionService.Instance.Limpiar();

            await Shell.Current.GoToAsync(
                "//NuevoAnalisisFormPage",
                false);
        }

        private static ImageButton? BuscarBotonNuevoAnalisis(
            View? view)
        {
            if (view is ImageButton imageButton)
            {
                if (imageButton.Source is FileImageSource file &&
                    string.Equals(
                        file.File,
                        "iconadd.png",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return imageButton;
                }
            }

            if (view is ContentView contentView)
            {
                return BuscarBotonNuevoAnalisis(
                    contentView.Content);
            }

            if (view is ScrollView scrollView)
            {
                return BuscarBotonNuevoAnalisis(
                    scrollView.Content);
            }

            if (view is Border border)
            {
                return BuscarBotonNuevoAnalisis(
                    border.Content as View);
            }

            if (view is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is not View childView)
                        continue;

                    ImageButton? encontrado =
                        BuscarBotonNuevoAnalisis(
                            childView);

                    if (encontrado != null)
                        return encontrado;
                }
            }

            return null;
        }

        private static bool ConfigurarNuevoAnalisis(
            ContentPage page)
        {
            VerticalStackLayout? contenedor =
                BuscarContenedorPrincipal(
                    page.Content);

            if (contenedor == null)
                return false;

            ModoTrabajoAnalisisView? existente =
                contenedor.Children
                    .OfType<
                        ModoTrabajoAnalisisView>()
                    .FirstOrDefault();

            if (existente != null)
                return true;

            var modoView =
                new ModoTrabajoAnalisisView();

            /*
             * El primer hijo es el encabezado del formulario. El selector se
             * inserta inmediatamente después y antes de los datos del usuario.
             */
            int indice =
                Math.Min(
                    1,
                    contenedor.Children.Count);

            contenedor.Children.Insert(
                indice,
                modoView);

            page.Appearing +=
                async (_, _) =>
                    await modoView.ActivarAsync();

            page.Disappearing +=
                (_, _) =>
                    modoView.Desactivar();

            _ = modoView.ActivarAsync();

            return true;
        }

        private static VerticalStackLayout?
            BuscarContenedorPrincipal(
                View? view)
        {
            if (view is VerticalStackLayout vertical &&
                vertical.Children.Count >= 2)
            {
                return vertical;
            }

            if (view is ContentView contentView)
            {
                return BuscarContenedorPrincipal(
                    contentView.Content);
            }

            if (view is ScrollView scrollView)
            {
                return BuscarContenedorPrincipal(
                    scrollView.Content);
            }

            if (view is Grid grid)
            {
                foreach (var child
                         in grid.Children)
                {
                    if (child is not View childView)
                        continue;

                    VerticalStackLayout? encontrado =
                        BuscarContenedorPrincipal(
                            childView);

                    if (encontrado != null)
                        return encontrado;
                }
            }

            if (view is Layout layout)
            {
                foreach (var child
                         in layout.Children)
                {
                    if (child is not View childView)
                        continue;

                    VerticalStackLayout? encontrado =
                        BuscarContenedorPrincipal(
                            childView);

                    if (encontrado != null)
                        return encontrado;
                }
            }

            return null;
        }

        private static bool ConfigurarNoticias(
            noticiasPage page)
        {
            Grid? contenidoPrincipal =
                page.FindByName<Grid>(
                    "ContenidoPrincipal");

            if (contenidoPrincipal == null)
                return false;

            if (contenidoPrincipal.Children
                .OfType<
                    EstadoSincronizacionContenidoView>()
                .Any())
            {
                return true;
            }

            RefreshView? refreshView =
                contenidoPrincipal.Children
                    .OfType<RefreshView>()
                    .FirstOrDefault();

            if (refreshView == null)
                return false;

            contenidoPrincipal
                .RowDefinitions
                .Clear();

            contenidoPrincipal
                .RowDefinitions
                .Add(
                    new RowDefinition(
                        GridLength.Auto));

            contenidoPrincipal
                .RowDefinitions
                .Add(
                    new RowDefinition(
                        GridLength.Auto));

            contenidoPrincipal
                .RowDefinitions
                .Add(
                    new RowDefinition(
                        GridLength.Star));

            Grid.SetRow(
                refreshView,
                2);

            foreach (
                ActivityIndicator indicator
                in contenidoPrincipal.Children
                    .OfType<ActivityIndicator>())
            {
                Grid.SetRowSpan(
                    indicator,
                    3);
            }

            var estadoView =
                new EstadoSincronizacionContenidoView
                {
                    Modulo =
                        "noticias",

                    SincronizarAsync =
                        async (
                            manual,
                            cancellationToken) =>
                        {
                            if (!DatosSinConexionPermisos
                                    .TienePermiso ||
                                page.BindingContext
                                is not NoticiasViewModel
                                    viewModel)
                            {
                                return;
                            }

                            bool disponible =
                                await EsperarDisponibleAsync(
                                    () =>
                                        viewModel.IsBusy,
                                    cancellationToken);

                            if (!disponible)
                                return;

                            NoticiasOfflineSyncResult
                                resultado =
                                    await NoticiasOfflineSyncService
                                        .Instance
                                        .SincronizarSiNecesarioAsync(
                                            forzarDescargaCompleta:
                                                manual,
                                            cancellationToken:
                                                cancellationToken);

                            if (!resultado.Success)
                            {
                                if (manual)
                                {
                                    await page.DisplayAlert(
                                        "Sincronización incompleta",
                                        resultado.Message,
                                        "Aceptar");
                                }

                                return;
                            }

                            await viewModel.CargarAsync(
                                reiniciar: true);
                        }
                };

            ConfigurarCicloPagina(
                page,
                estadoView);

            Grid.SetRow(
                estadoView,
                1);

            contenidoPrincipal.Children.Add(
                estadoView);

            return true;
        }

        private static bool ConfigurarAlbum(
            albumFotosPage page)
        {
            CollectionView? albumCollectionView =
                page.FindByName<CollectionView>(
                    "AlbumCollectionView");

            if (albumCollectionView?.Header
                is not VerticalStackLayout header)
            {
                return false;
            }

            if (header.Children
                .OfType<
                    EstadoSincronizacionContenidoView>()
                .Any())
            {
                return true;
            }

            var estadoView =
                new EstadoSincronizacionContenidoView
                {
                    Modulo =
                        "album",

                    SincronizarAsync =
                        async (
                            manual,
                            cancellationToken) =>
                        {
                            if (!DatosSinConexionPermisos
                                    .TienePermiso ||
                                page.BindingContext
                                is not AlbumFotosViewModel
                                    viewModel)
                            {
                                return;
                            }

                            bool disponible =
                                await EsperarDisponibleAsync(
                                    () =>
                                        viewModel.IsBusy,
                                    cancellationToken);

                            if (!disponible)
                                return;

                            AlbumOfflineSyncResult result =
                                await AlbumOfflineSyncService
                                    .Instance
                                    .SincronizarSiNecesarioAsync(
                                        forzarDescargaCompleta:
                                            manual,
                                        cancellationToken:
                                            cancellationToken);

                            if (!result.Success)
                            {
                                if (manual)
                                {
                                    await page.DisplayAlert(
                                        "Sincronización incompleta",
                                        result.Message,
                                        "Aceptar");
                                }

                                return;
                            }

                            await viewModel.LoadAsync(
                                true);
                        }
                };

            ConfigurarCicloPagina(
                page,
                estadoView);

            header.Children.Add(
                estadoView);

            return true;
        }

        private static void ConfigurarCicloPagina(
            ContentPage page,
            EstadoSincronizacionContenidoView
                estadoView)
        {
            page.Appearing +=
                (_, _) =>
                    estadoView.Activar();

            page.Disappearing +=
                (_, _) =>
                    estadoView.Desactivar();
        }

        private static async Task<bool>
            EsperarDisponibleAsync(
                Func<bool> estaOcupado,
                CancellationToken
                    cancellationToken)
        {
            const int maxIntentos = 40;

            for (int intento = 0;
                 intento < maxIntentos;
                 intento++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (!estaOcupado())
                    return true;

                await Task.Delay(
                    TimeSpan.FromMilliseconds(
                        250),
                    cancellationToken);
            }

            return !estaOcupado();
        }
    }
}
