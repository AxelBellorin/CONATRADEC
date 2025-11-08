using CONATRADEC.Services;                  // Servicios de acceso a API (matriz de permisos, roles).
using CONATRADEC.Models;                    // Modelos: RolResponse, InterfazResponse, etc.
using System.Collections.ObjectModel;       // Colecciones observables para data binding.
using System.ComponentModel;                // INotifyPropertyChanged (usado por items).
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Extensions; // Extensiones (ToObservableCollection).

namespace CONATRADEC.ViewModels;

public class MatrizPermisosViewModel : GlobalService
{
    // ====================================================
    // COLECCIONES Y PROPIEDADES PRINCIPALES
    // ====================================================

    private ObservableCollection<RolResponse> roles = new();         // Lista de roles disponibles para seleccionar.
    private ObservableCollection<InterfazResponse> _permisos = new(); // Permisos del rol seleccionado (colección principal).
    private readonly List<InterfazResponse> _hooked = new();         // Ítems suscritos a PropertyChanged (para detectar cambios).
    private ObservableCollection<InterfazResponse> permisosFiltrados; // (No se usa directamente; expuesto por propiedad calculada).
    private List<PermisoSnapshot> _snapshot = new();                 // Copia del estado actual (para Revertir).
    private RolResponse? _rolSeleccionado;                           // Rol actualmente seleccionado.
    private string _filtro = string.Empty;                            // Texto para filtrar por NombrePermiso.
    private string _estado = string.Empty;                            // Mensaje de estado para la UI.

    // ====================================================
    // Servicios API
    // ====================================================

    private readonly MatrizPermisosApiService matrizPermisosApiService; // API: obtener/guardar matriz de permisos.
    private readonly RolApiService rolApiService;                        // API: obtener roles.

    // ====================================================
    // COMANDOS
    // ====================================================

    public Command RefrescarCommand { get; }         // Recarga la lista de roles.
    public Command GuardarCommand { get; }           // Guarda los permisos del rol actual.
    public Command RevertirCambiosCommand { get; }   // Revierte cambios al último snapshot.
    public Command MarcarColumnaCommand { get; }     // Marca/Desmarca una columna completa.
    public Command LimpiarTodoCommand { get; }       // Limpia todas las columnas (equivalente a "ninguno").
    public Command MarcarFilaCommand { get; }        // Marca todos los flags de una fila.
    public Command LimpiarFilaCommand { get; }       // Desmarca todos los flags de una fila.

    // ====================================================
    // PROPIEDADES BINDABLE
    // ====================================================

    // Colección principal de permisos; reengancha eventos al asignar una nueva instancia.
    public ObservableCollection<InterfazResponse> Permisos
    {
        get => _permisos;
        set
        {
            if (_permisos == value) return;

            // Desuscribir cambios de la colección anterior (si existía).
            if (_permisos is not null)
                _permisos.CollectionChanged -= Permisos_CollectionChanged;

            // Asignar nueva colección (o vacía si viene null).
            _permisos = value ?? new ObservableCollection<InterfazResponse>();
            _permisos.CollectionChanged += Permisos_CollectionChanged;

            // Reenganchar PropertyChanged de cada ítem.
            RehookItems();

            // Notificar a la UI que cambió la colección y su versión filtrada.
            OnPropertyChanged(nameof(Permisos));
            OnPropertyChanged(nameof(PermisosFiltrados));
        }
    }

    // Vista filtrada (derivada) de la colección principal según el texto del filtro.
    public ObservableCollection<InterfazResponse> PermisosFiltrados => string.IsNullOrWhiteSpace(_filtro)
        ? Permisos
        : Permisos
            .Where(p => p.NombreInterfaz.Contains(_filtro, StringComparison.OrdinalIgnoreCase))
            .ToObservableCollection();

    // Texto de filtro; notifica recalculado del filtrado al modificarse.
    public string Filtro
    {
        get => _filtro;
        set
        {
            if (_filtro != value)
            {
                _filtro = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PermisosFiltrados));
            }
        }
    }

    // Mensaje de estado (para barra de estado / label informativo).
    public string Estado
    {
        get => _estado;
        set
        {
            if (_estado != value)
            {
                _estado = value;
                OnPropertyChanged();
            }
        }
    }

    // Lista de roles (para un Picker/ComboBox).
    public ObservableCollection<RolResponse> Roles
    {
        get => roles;
        set { roles = value; OnPropertyChanged(); }
    }

    // Rol seleccionado; al cambiar, dispara la carga de permisos de ese rol.
    public RolResponse? RolSeleccionado
    {
        get => _rolSeleccionado;
        set
        {
            if (_rolSeleccionado != value)
            {
                _rolSeleccionado = value;
                _ = CargarPermisosDelRolAsync(value); // Fire-and-forget; evita bloquear el setter.
            }
        }
    }

    // Propiedades calculadas según el estado actual (no almacenan valor).
    public bool PuedeGuardar => RolSeleccionado != null && Permisos.Any(p => p.IsDirty);
    public bool PuedeRevertir => Permisos.Any(p => p.IsDirty);

    // ====================================================
    // CONSTRUCTOR
    // ====================================================

    public MatrizPermisosViewModel()
    {
        // Instanciación de servicios (a futuro: inyección de dependencias).
        matrizPermisosApiService = new MatrizPermisosApiService();
        rolApiService = new RolApiService();

        // Inicialización de comandos y sus CanExecute.
        RefrescarCommand = new Command(async () => await CargarRolesAsync());
        GuardarCommand = new Command(async () => await GuardarAsync(), () => PuedeGuardar);
        RevertirCambiosCommand = new Command(RevertirCambios, () => PuedeRevertir);

        MarcarColumnaCommand = new Command<string>(MarcarColumna);
        LimpiarTodoCommand = new Command(() => MarcarColumna("ninguno"));

        // Operaciones por fila: marcan todo o limpian todo y actualizan habilitados.
        MarcarFilaCommand = new Command<InterfazResponse>(fila => { fila.SetAll(true); ActualizarHabilitados(); });
        LimpiarFilaCommand = new Command<InterfazResponse>(fila => { fila.SetAll(false); ActualizarHabilitados(); });

        // Carga inicial de roles al construir el ViewModel.
        _ = CargarRolesAsync();
    }

    // ====================================================
    // MÉTODOS AUXILIARES (ENGANCHE DE ITEMS Y HABILITADOS)
    // ====================================================

    // Reengancha manejadores de PropertyChanged para todos los ítems actuales.
    private void RehookItems()
    {
        // Desuscribe ítems previamente enganchados.
        foreach (var it in _hooked)
            it.PropertyChanged -= Item_PropertyChanged;
        _hooked.Clear();

        // Suscribe ítems actuales.
        foreach (var p in Permisos)
        {
            p.PropertyChanged += Item_PropertyChanged;
            _hooked.Add(p);
        }
    }

    // Recalcula CanExecute de Guardar/Revertir y notifica propiedades calculadas.
    private void ActualizarHabilitados()
    {
        (GuardarCommand as Command).ChangeCanExecute();
        (RevertirCambiosCommand as Command).ChangeCanExecute();
        OnPropertyChanged(nameof(PuedeGuardar));
        OnPropertyChanged(nameof(PuedeRevertir));
    }

    // ====================================================
    // CARGA DE DATOS
    // ====================================================

    // Carga la lista de Roles desde la API.
    private async Task CargarRolesAsync()
    {
        try
        {
            IsBusy = true;

            if (Roles is not null)
                Roles.Clear();

            if (Permisos is not null)
                Permisos.Clear();

            // Llama al servicio que retorna los roles.
            var response = await rolApiService.GetRolAsync();

            if (response is null)
                return;

            Roles = response;
            Estado = $"Roles cargados: {Roles.Count}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Carga los permisos para el rol seleccionado.
    private async Task CargarPermisosDelRolAsync(RolResponse rolResponse)
    {
        if (RolSeleccionado is null)
            return;

        try
        {
            IsBusy = true;

            // Limpia colección previa solo si existe y tiene elementos.
            if (Permisos is not null && Permisos.Count > 0)
                Permisos.Clear();

            // Pide al servicio la(s) matriz/matrices para el rol.
            var response = await matrizPermisosApiService.GetMatrizByRolAsync(new RolRequest(rolResponse));

            if (response is null || response.Count < 1)
            {
                Estado = "No se pudieron cargar los permisos.";
                return;
            }

            // Toma la primera matriz devuelta (según contrato actual de tu API).
            var matriz = response.FirstOrDefault();

            // Si por alguna razón viene null, sale con estado informativo (evita NRE).
            if (matriz?.Interfaz is null)
            {
                Estado = "No se encontraron permisos para el rol seleccionado.";
                return;
            }

            // Reemplaza la colección de permisos con la devuelta por la API.
            Permisos = matriz.Interfaz;

            // Marca como "limpios" los items recién cargados (no hay cambios pendientes).
            foreach (var p in Permisos)
                p.AcceptChanges();

            // Actualiza el snapshot para poder revertir los cambios luego.
            _snapshot = Permisos.Select(PermisoSnapshot.From).ToList();

            Estado = $"Permisos cargados para {RolSeleccionado.NombreRol}";
            ActualizarHabilitados();
        }
        catch (Exception ex)
        {
            // Muestra una única alerta con el error (antes había dos seguidas con el mismo mensaje).
            await App.Current.MainPage.DisplayAlert("Error", $"No se pudieron cargar los permisos: {ex.Message}", "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ====================================================
    // COMPORTAMIENTO DE LOS COMANDOS
    // ====================================================

    // Marca o desmarca una columna completa según 'columna'.
    private void MarcarColumna(string? columna)
    {
        // Determina el valor a asignar según la columna; "ninguno" desmarca todo.
        bool? valor = columna switch
        {
            "leer" => true,
            "agregar" => true,
            "actualizar" => true,
            "eliminar" => true,
            "ninguno" => false,
            _ => null
        };
        if (valor is null) return;

        // Aplica a cada fila según la columna solicitada.
        foreach (var p in Permisos)
        {
            switch (columna)
            {
                case "leer": p.Leer = valor.Value; break;
                case "agregar": p.Agregar = valor.Value; break;
                case "actualizar": p.Actualizar = valor.Value; break;
                case "eliminar": p.Eliminar = valor.Value; break;
                case "ninguno": p.SetAll(false); break;
            }
        }

        // Notificación simple para refrescos dependientes (si aplica en la vista).
        OnPropertyChanged(nameof(Filtro));

        // Recalcular habilitados (Guardar/Revertir).
        ActualizarHabilitados();
    }

    // Restaura los valores desde el snapshot para cada permiso.
    private void RevertirCambios()
    {
        foreach (var s in _snapshot)
        {
            var p = Permisos.FirstOrDefault(x => x.InterfazId == s.PermisoId);
            if (p is null) continue;

            p.Leer = s.Leer;
            p.Agregar = s.Agregar;
            p.Actualizar = s.Actualizar;
            p.Eliminar = s.Eliminar;

            // Tras revertir, el item ya no está "sucio".
            p.AcceptChanges();
        }

        Estado = "Cambios revertidos";
        ActualizarHabilitados();
    }

    // Se dispara cuando cambia la colección (agregar/quitar elementos).
    private void Permisos_CollectionChanged(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RehookItems();                            // Reengancha eventos a los nuevos ítems.
        OnPropertyChanged(nameof(PermisosFiltrados)); // Refresca la vista filtrada.
    }

    // Se dispara cuando cambia alguna propiedad de un ítem (leer/agregar/actualizar/eliminar/nombre).
    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Si cambió un flag de permiso, marca estado como "hay cambios".
        if (e.PropertyName is nameof(Permiso.Leer)
            or nameof(Permiso.Agregar)
            or nameof(Permiso.Actualizar)
            or nameof(Permiso.Eliminar))
        {
            Estado = "Hay cambios sin guardar";
            // Si el filtrado dependiera de estos flags, podrías notificar aquí:
            // OnPropertyChanged(nameof(PermisosFiltrados));
        }

        // Si cambió el nombre, refresca la vista filtrada.
        if (e.PropertyName == nameof(InterfazResponse.NombreInterfaz))
            OnPropertyChanged(nameof(PermisosFiltrados));

        // Actualiza CanExecute de los botones Guardar/Revertir.
        ActualizarHabilitados();
    }

    // Guarda la matriz completa (contrato actual): no solo los IsDirty.
    private async Task GuardarAsync()
    {
        if (RolSeleccionado is null)
            return;

        try
        {
            IsBusy = true;

            // Evita llamadas innecesarias si no hay cambios.
            var hayCambios = Permisos.Any(p => p.IsDirty);
            if (!hayCambios)
            {
                Estado = "No hay cambios para guardar.";
                return;
            }

            // Envía SIEMPRE la lista completa de permisos según el contrato actual del endpoint.
            var permisosParaEnviar = Permisos;

            // Construye el request para la API.
            var matriz = new MatrizPermisosRequest
            {
                Rol = new RolRequest
                {
                    RolId = RolSeleccionado.RolId,
                    NombreRol = RolSeleccionado.NombreRol
                },
                Permisos = permisosParaEnviar.Select(p => new InterfazRequest
                {
                    InterfazId = p.InterfazId,
                    NombreInterfaz = p.NombreInterfaz,
                    Leer = p.Leer,
                    Agregar = p.Agregar,
                    Actualizar = p.Actualizar,
                    Eliminar = p.Eliminar
                }).ToList()
            };

            var payload = new List<MatrizPermisosRequest> { matriz };

            // Llama a la API para guardar.
            var ok = await matrizPermisosApiService.GuardarMatrizAsync(payload);
            if (ok)
            {
                // Opcional: marca limpios solo los que estaban sucios (mantiene semántica actual).
                foreach (var p in Permisos.Where(x => x.IsDirty))
                    p.AcceptChanges();

                // Actualiza snapshot al nuevo estado persistido.
                _snapshot = Permisos.Select(PermisoSnapshot.From).ToList();

                Estado = "Permisos guardados correctamente";
                await App.Current.MainPage.DisplayAlert("Datos Guardados", "Se ha guardado correctamente", "OK");

                ActualizarHabilitados();
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Error", "No se pudieron guardar los permisos. Intente nuevamente.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            // Manejo genérico (puedes especializar por HttpRequestException/TaskCanceledException si lo deseas).
            Estado = "Error al guardar permisos";
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

// ====================================================
// MODELOS AUXILIARES
// ====================================================

// Snapshots inmutables del estado de una fila de permisos.
// Permiten revertir al último estado cargado/guardado.
public record PermisoSnapshot(int PermisoId, bool Leer, bool Agregar, bool Actualizar, bool Eliminar)
{
    public static PermisoSnapshot From(InterfazResponse p)
        => new(p.InterfazId, p.Leer, p.Agregar, p.Actualizar, p.Eliminar);
}
