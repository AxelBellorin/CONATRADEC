using CONATRADEC.Services;
using Microsoft.Maui.Handlers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Refuerza en toda la interfaz la regla de trabajo offline:
    ///
    /// - conserva visibles los botones que el permiso del usuario habilita;
    /// - al presionarlos muestra por qué la operación está limitada;
    /// - evita que un formulario de creación o edición permanezca abierto;
    /// - no afecta consultas ni el módulo de análisis de suelo.
    ///
    /// La validación se aplica sobre los comandos ya existentes. No reemplaza
    /// PermissionService ni modifica CanAdd, CanEdit o CanDelete.
    /// </summary>
    public static class OfflineCrudPageMapper
    {
        private static int registrado;

        private static readonly ConditionalWeakTable<
            ContentPage,
            EstadoPagina> EstadosPagina = new();

        private static readonly ConditionalWeakTable<
            BindableObject,
            EstadoComando> EstadosComando = new();

        public static void Register()
        {
            if (Interlocked.Exchange(
                    ref registrado,
                    1) == 1)
            {
                return;
            }

            PageHandler.Mapper.AppendToMapping(
                nameof(OfflineCrudPageMapper),
                static (_, view) =>
                {
                    if (view is not ContentPage pagina)
                        return;

                    pagina.Dispatcher.Dispatch(
                        () => Adjuntar(pagina));
                });
        }

        private static void Adjuntar(
            ContentPage pagina)
        {
            EstadoPagina estado =
                EstadosPagina.GetValue(
                    pagina,
                    static paginaActual =>
                        new EstadoPagina(paginaActual));

            estado.Adjuntar();
        }

        private sealed class EstadoPagina
        {
            private readonly ContentPage pagina;

            private bool adjuntado;
            private bool bloqueandoFormulario;

            public EstadoPagina(
                ContentPage pagina)
            {
                this.pagina = pagina;
            }

            public void Adjuntar()
            {
                if (adjuntado)
                    return;

                adjuntado = true;

                pagina.Loaded += Pagina_Loaded;
                pagina.Appearing += Pagina_Appearing;
                pagina.BindingContextChanged +=
                    Pagina_BindingContextChanged;

                ProgramarProteccionComandos();
            }

            private void Pagina_Loaded(
                object? sender,
                EventArgs e)
            {
                ProgramarProteccionComandos();
            }

            private async void Pagina_Appearing(
                object? sender,
                EventArgs e)
            {
                ProgramarProteccionComandos();
                await ValidarFormularioAsync();
            }

            private void Pagina_BindingContextChanged(
                object? sender,
                EventArgs e)
            {
                ProgramarProteccionComandos();
            }

            /// <summary>
            /// Se realizan varias pasadas porque CollectionView y algunos
            /// DataTemplate crean sus botones después de Loaded/Appearing.
            /// </summary>
            private void ProgramarProteccionComandos()
            {
                pagina.Dispatcher.Dispatch(
                    ProtegerComandosVisibles);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(150),
                    ProtegerComandosVisibles);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(600),
                    ProtegerComandosVisibles);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(1500),
                    ProtegerComandosVisibles);

                pagina.Dispatcher.DispatchDelayed(
                    TimeSpan.FromMilliseconds(3000),
                    ProtegerComandosVisibles);
            }

            private void ProtegerComandosVisibles()
            {
                if (!OfflineWriteAccessService
                        .EscrituraRestringida ||
                    OfflineWriteAccessService
                        .EsPaginaAnalisis(pagina))
                {
                    return;
                }

                Dictionary<ICommand, string> comandos =
                    ObtenerComandosEscritura(
                        pagina.BindingContext);

                if (comandos.Count == 0)
                    return;

                foreach (ToolbarItem item
                         in pagina.ToolbarItems)
                {
                    ProtegerMenuItem(
                        item,
                        comandos);
                }

                ProtegerArbolVisual(
                    pagina,
                    comandos);
            }

            private async Task ValidarFormularioAsync()
            {
                if (bloqueandoFormulario ||
                    !OfflineWriteAccessService
                        .EscrituraRestringida ||
                    OfflineWriteAccessService
                        .EsPaginaAnalisis(pagina) ||
                    !EsPaginaFormulario(pagina) ||
                    EsFormularioSoloLectura(
                        pagina.BindingContext))
                {
                    return;
                }

                bloqueandoFormulario = true;

                try
                {
                    await OfflineWriteAccessService
                        .MostrarRestriccionAsync(pagina);

                    Shell? shell = Shell.Current;

                    if (shell == null)
                        return;

                    try
                    {
                        await shell.GoToAsync(
                            AppRoutes.Regresar,
                            false);
                    }
                    catch
                    {
                        /*
                         * Algunas rutas absolutas no admiten "..".
                         * En ese caso se vuelve a la primera sección que el
                         * usuario pueda consultar.
                         */
                        await shell.GoToAsync(
                            NavigationPermissionService
                                .ObtenerRutaInicialPermitida(),
                            false);
                    }
                }
                finally
                {
                    bloqueandoFormulario = false;
                }
            }
        }

        private static Dictionary<ICommand, string>
            ObtenerComandosEscritura(
                object? bindingContext)
        {
            var resultado =
                new Dictionary<ICommand, string>(
                    ReferenciaComandoComparer.Instance);

            if (bindingContext == null)
                return resultado;

            PropertyInfo[] propiedades =
                bindingContext
                    .GetType()
                    .GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public);

            foreach (PropertyInfo propiedad
                     in propiedades)
            {
                if (propiedad.GetIndexParameters().Length > 0 ||
                    !typeof(ICommand).IsAssignableFrom(
                        propiedad.PropertyType) ||
                    !OfflineWriteAccessService
                        .EsNombreComandoEscritura(
                            propiedad.Name))
                {
                    continue;
                }

                try
                {
                    if (propiedad.GetValue(
                            bindingContext) is ICommand comando)
                    {
                        resultado[comando] =
                            propiedad.Name;
                    }
                }
                catch
                {
                    /*
                     * Una propiedad calculada no debe impedir la protección de
                     * los demás comandos de la página.
                     */
                }
            }

            return resultado;
        }

        private static void ProtegerArbolVisual(
            IVisualTreeElement elemento,
            IReadOnlyDictionary<ICommand, string> comandos)
        {
            if (elemento is Button boton)
            {
                ProtegerComando(
                    boton,
                    boton.Command,
                    comando => boton.Command = comando,
                    comandos);
            }
            else if (elemento is ImageButton imageButton)
            {
                ProtegerComando(
                    imageButton,
                    imageButton.Command,
                    comando => imageButton.Command = comando,
                    comandos);
            }
            else if (elemento is SwipeView swipeView)
            {
                ProtegerSwipeItems(
                    swipeView.LeftItems,
                    comandos);

                ProtegerSwipeItems(
                    swipeView.RightItems,
                    comandos);

                ProtegerSwipeItems(
                    swipeView.TopItems,
                    comandos);

                ProtegerSwipeItems(
                    swipeView.BottomItems,
                    comandos);
            }

            if (elemento is View vista)
            {
                foreach (IGestureRecognizer recognizer
                         in vista.GestureRecognizers)
                {
                    if (recognizer
                        is TapGestureRecognizer tap)
                    {
                        ProtegerComando(
                            tap,
                            tap.Command,
                            comando => tap.Command = comando,
                            comandos);
                    }
                }
            }

            foreach (IVisualTreeElement hijo
                     in elemento.GetVisualChildren())
            {
                ProtegerArbolVisual(
                    hijo,
                    comandos);
            }
        }

        private static void ProtegerSwipeItems(
            SwipeItems? items,
            IReadOnlyDictionary<ICommand, string> comandos)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item is MenuItem menuItem)
                {
                    ProtegerMenuItem(
                        menuItem,
                        comandos);
                }
            }
        }

        private static void ProtegerMenuItem(
            MenuItem menuItem,
            IReadOnlyDictionary<ICommand, string> comandos)
        {
            ProtegerComando(
                menuItem,
                menuItem.Command,
                comando => menuItem.Command = comando,
                comandos);
        }

        private static void ProtegerComando(
            BindableObject propietario,
            ICommand? comandoActual,
            Action<ICommand> asignarComando,
            IReadOnlyDictionary<ICommand, string> comandos)
        {
            if (comandoActual == null)
                return;

            if (EstadosComando.TryGetValue(
                    propietario,
                    out EstadoComando? estadoExistente))
            {
                if (ReferenceEquals(
                        comandoActual,
                        estadoExistente.ComandoProtegido))
                {
                    return;
                }

                estadoExistente.Desconectar();
                EstadosComando.Remove(propietario);
            }

            if (!comandos.ContainsKey(comandoActual))
                return;

            Command<object?>? comandoProtegido = null;

            comandoProtegido =
                new Command<object?>(
                    async parametro =>
                    {
                        if (OfflineWriteAccessService
                                .EscrituraRestringida)
                        {
                            await OfflineWriteAccessService
                                .MostrarRestriccionAsync();
                            return;
                        }

                        if (comandoActual.CanExecute(
                                parametro))
                        {
                            comandoActual.Execute(
                                parametro);
                        }
                    },
                    parametro =>
                        comandoActual.CanExecute(
                            parametro));

            EventHandler canExecuteChanged =
                (_, _) =>
                    comandoProtegido
                        .ChangeCanExecute();

            comandoActual.CanExecuteChanged +=
                canExecuteChanged;

            var nuevoEstado =
                new EstadoComando(
                    comandoActual,
                    comandoProtegido,
                    canExecuteChanged);

            EstadosComando.Add(
                propietario,
                nuevoEstado);

            asignarComando(
                comandoProtegido);
        }

        private static bool EsPaginaFormulario(
            ContentPage pagina)
        {
            string nombre =
                pagina.GetType().Name;

            return
                nombre.EndsWith(
                    "FormPage",
                    StringComparison.OrdinalIgnoreCase) ||
                nombre.Contains(
                    "Formulario",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsFormularioSoloLectura(
            object? bindingContext)
        {
            if (bindingContext == null)
                return false;

            Type tipo =
                bindingContext.GetType();

            foreach (string nombrePropiedad
                     in new[]
                     {
                         "Mode",
                         "Modo",
                         "FormMode",
                         "ModoFormulario"
                     })
            {
                PropertyInfo? propiedad =
                    tipo.GetProperty(
                        nombrePropiedad,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.IgnoreCase);

                if (propiedad == null ||
                    propiedad.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                try
                {
                    string valor =
                        propiedad.GetValue(bindingContext)?
                            .ToString() ??
                        string.Empty;

                    if (valor.Equals(
                            "View",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "ReadOnly",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "Lectura",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "Visualizar",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (valor.Equals(
                            "Create",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "Edit",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "Crear",
                            StringComparison.OrdinalIgnoreCase) ||
                        valor.Equals(
                            "Editar",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                catch
                {
                    // Se continúa con las propiedades booleanas de respaldo.
                }
            }

            foreach (string nombrePropiedad
                     in new[]
                     {
                         "IsReadOnly",
                         "SoloLectura"
                     })
            {
                PropertyInfo? propiedad =
                    tipo.GetProperty(
                        nombrePropiedad,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.IgnoreCase);

                if (propiedad?.PropertyType != typeof(bool))
                    continue;

                try
                {
                    if (propiedad.GetValue(
                            bindingContext) is bool soloLectura)
                    {
                        return soloLectura;
                    }
                }
                catch
                {
                }
            }

            /*
             * Un formulario sin modo explícito se considera de escritura.
             * Así se cubren formularios antiguos que siempre se utilizaban
             * únicamente para agregar o editar.
             */
            return false;
        }

        private sealed class EstadoComando
        {
            private readonly ICommand comandoOriginal;
            private readonly EventHandler canExecuteChanged;

            public EstadoComando(
                ICommand comandoOriginal,
                ICommand comandoProtegido,
                EventHandler canExecuteChanged)
            {
                this.comandoOriginal =
                    comandoOriginal;

                ComandoProtegido =
                    comandoProtegido;

                this.canExecuteChanged =
                    canExecuteChanged;
            }

            public ICommand ComandoProtegido { get; }

            public void Desconectar()
            {
                comandoOriginal.CanExecuteChanged -=
                    canExecuteChanged;
            }
        }

        private sealed class ReferenciaComandoComparer :
            IEqualityComparer<ICommand>
        {
            public static ReferenciaComandoComparer Instance { get; } =
                new();

            public bool Equals(
                ICommand? x,
                ICommand? y) =>
                ReferenceEquals(x, y);

            public int GetHashCode(
                ICommand obj) =>
                RuntimeHelpers.GetHashCode(obj);
        }
    }
}
