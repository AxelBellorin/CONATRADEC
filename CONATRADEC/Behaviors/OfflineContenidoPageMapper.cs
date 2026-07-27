using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Handlers;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Conserva únicamente la corrección de navegación de Nuevo análisis.
    ///
    /// Se eliminan de este mapper:
    /// - indicadores visibles de sincronización;
    /// - selector de modo dentro del formulario;
    /// - comprobación HTTP antes de navegar;
    /// - verificación automática global.
    ///
    /// El origen de datos se decide una sola vez en el login.
    /// </summary>
    public static class OfflineContenidoPageMapper
    {
        private static int registrado;

        private static readonly SemaphoreSlim
            navegacionNuevoAnalisisLock = new(1, 1);

        private static readonly BindableProperty ConfiguradoProperty =
            BindableProperty.CreateAttached(
                "OfflineContenidoConfigurado",
                typeof(bool),
                typeof(OfflineContenidoPageMapper),
                false);

        public static void Register()
        {
            if (Interlocked.Exchange(ref registrado, 1) == 1)
                return;

            PageHandler.Mapper.AppendToMapping(
                nameof(OfflineContenidoPageMapper),
                static (_, view) =>
                {
                    if (view is not ContentPage page ||
                        !string.Equals(
                            page.GetType().Name,
                            "MainPage",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    page.Loaded -= OnPageLoaded;
                    page.Loaded += OnPageLoaded;

                    page.Dispatcher.Dispatch(
                        () => ConfigurarPaginaPrincipal(page));
                });
        }

        private static void OnPageLoaded(
            object? sender,
            EventArgs e)
        {
            if (sender is not ContentPage page)
                return;

            ConfigurarPaginaPrincipal(page);
        }

        private static void ConfigurarPaginaPrincipal(
            ContentPage page)
        {
            ImageButton? botonNuevo =
                ObtenerBotonNuevoAnalisis(page);

            if (botonNuevo == null)
                return;

            ConfigurarBotonNuevoAnalisis(
                page,
                botonNuevo);

            if ((bool)page.GetValue(ConfiguradoProperty))
                return;

            page.Appearing -= OnPaginaPrincipalAppearing;
            page.Appearing += OnPaginaPrincipalAppearing;

            page.SetValue(ConfiguradoProperty, true);
        }

        private static void OnPaginaPrincipalAppearing(
            object? sender,
            EventArgs e)
        {
            if (sender is not ContentPage page)
                return;

            if (page.BindingContext is MainPageViewModel viewModel)
            {
                /*
                 * Evita que Cancelar deje congelados Nuevo o Actualizar.
                 */
                viewModel.CancelarCarga();
                viewModel.IsBusy = false;
                viewModel.PrepararPantalla();
            }

            ImageButton? botonNuevo =
                ObtenerBotonNuevoAnalisis(page);

            if (botonNuevo != null)
            {
                ConfigurarBotonNuevoAnalisis(
                    page,
                    botonNuevo);
            }
        }

        private static ImageButton? ObtenerBotonNuevoAnalisis(
            ContentPage page)
        {
            CollectionView? listado =
                page.FindByName<CollectionView>(
                    "AnalisisCollectionView");

            return listado?.Header is View encabezado
                ? BuscarBotonNuevoAnalisis(encabezado)
                : null;
        }

        private static void ConfigurarBotonNuevoAnalisis(
            ContentPage page,
            ImageButton botonNuevo)
        {
            botonNuevo.Command = null;
            botonNuevo.Command = new Command(
                async () =>
                    await AbrirNuevoAnalisisAsync(page));

            botonNuevo.IsEnabled = true;
            botonNuevo.InputTransparent = false;
        }

        private static async Task AbrirNuevoAnalisisAsync(
            ContentPage page)
        {
            bool entro;

            try
            {
                entro = await navegacionNuevoAnalisisLock
                    .WaitAsync(TimeSpan.Zero);
            }
            catch
            {
                return;
            }

            if (!entro)
                return;

            try
            {
                if (page.BindingContext is not MainPageViewModel viewModel)
                    return;

                viewModel.CancelarCarga();
                viewModel.IsBusy = false;
                viewModel.PrepararPantalla();

                if (!viewModel.CanAdd)
                {
                    await page.DisplayAlert(
                        "Acceso denegado",
                        "No tiene permisos para registrar análisis.",
                        "Aceptar");
                    return;
                }

                if (ModoSesionService.EsOffline)
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
                            "Este dispositivo no tiene un motor completo. Inicie una sesión en línea y utilice Descargar todo.",
                            "Aceptar");
                        return;
                    }
                }

                await ModoTrabajoAnalisisService.Instance
                    .PrepararNuevoAnalisisAsync();

                AnalisisEdicionService.Instance.Limpiar();

                await Shell.Current.GoToAsync(
                    "//NuevoAnalisisFormPage",
                    false);
            }
            finally
            {
                navegacionNuevoAnalisisLock.Release();
            }
        }

        private static ImageButton? BuscarBotonNuevoAnalisis(
            View? view)
        {
            if (view is ImageButton imageButton &&
                imageButton.Source is FileImageSource file &&
                string.Equals(
                    file.File,
                    "iconadd.png",
                    StringComparison.OrdinalIgnoreCase))
            {
                return imageButton;
            }

            if (view is ContentView contentView)
                return BuscarBotonNuevoAnalisis(contentView.Content);

            if (view is ScrollView scrollView)
                return BuscarBotonNuevoAnalisis(scrollView.Content);

            if (view is Border border)
                return BuscarBotonNuevoAnalisis(border.Content as View);

            if (view is Layout layout)
            {
                foreach (IView child in layout.Children)
                {
                    if (child is not View childView)
                        continue;

                    ImageButton? encontrado =
                        BuscarBotonNuevoAnalisis(childView);

                    if (encontrado != null)
                        return encontrado;
                }
            }

            return null;
        }
    }
}
