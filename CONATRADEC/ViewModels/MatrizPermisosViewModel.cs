using CONATRADEC.Services;
using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Extensions;

namespace CONATRADEC.ViewModels;

public class MatrizPermisosViewModel : GlobalService
{

    // ====================================================
    // COLECCIONES Y PROPIEDADES PRINCIPALES
    // ====================================================

    private ObservableCollection<RolResponse> roles = new();
    private ObservableCollection<InterfazResponse> _permisos = new();
    private readonly List<InterfazResponse> _hooked = new();
    private ObservableCollection<InterfazResponse> permisosFiltrados;
    private List<PermisoSnapshot> _snapshot = new();
    private RolResponse? _rolSeleccionado;
    private string _filtro = string.Empty;
    private string _estado = string.Empty;

    // ====================================================
    // Servicio API
    // ====================================================

    private readonly MatrizPermisosApiService matrizPermisosApiService;
    private readonly RolApiService rolApiService;

    // ====================================================
    // COMANDOS
    // ====================================================

    public Command RefrescarCommand { get; }
    public Command GuardarCommand { get; }
    public Command RevertirCambiosCommand { get; }
    public Command MarcarColumnaCommand { get; }
    public Command LimpiarTodoCommand { get; }
    public Command MarcarFilaCommand { get; }
    public Command LimpiarFilaCommand { get; }

    public ObservableCollection<InterfazResponse> Permisos
    {
        get => _permisos;
        set
        {
            if (_permisos == value) return;

            if (_permisos != null)
                _permisos.CollectionChanged -= Permisos_CollectionChanged;

            _permisos = value ?? new ObservableCollection<InterfazResponse>();
            _permisos.CollectionChanged += Permisos_CollectionChanged;

            RehookItems();
            OnPropertyChanged(nameof(Permisos));
            OnPropertyChanged(nameof(PermisosFiltrados));
            OnPropertyChanged(nameof(PuedeGuardar));
            OnPropertyChanged(nameof(PuedeRevertir));
        }
    }

    public IEnumerable<InterfazResponse> PermisosFiltrados => string.IsNullOrWhiteSpace(_filtro)
                    ? Permisos
                    : Permisos.Where(p => p.NombrePermiso.Contains(_filtro, StringComparison.OrdinalIgnoreCase)).ToObservableCollection();

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

    public ObservableCollection<RolResponse> Roles
    {
        get => roles;
        set
        {
            roles = value;
            OnPropertyChanged();
        }
    }

    public RolResponse? RolSeleccionado
    {
        get => _rolSeleccionado;
        set
        {
            if (_rolSeleccionado != value)
            {
                _rolSeleccionado = value;
                _ = CargarPermisosDelRolAsync(value);
                OnPropertyChanged();
                ActualizarHabilitados();
            }
        }
    }

    // Propiedades calculadas (no almacenan valor)
    public bool PuedeGuardar => RolSeleccionado != null && Permisos.Any(p => p.IsDirty);
    public bool PuedeRevertir => Permisos.Any(p => p.IsDirty);


    // ====================================================
    // CONSTRUCTOR
    // ====================================================

    public MatrizPermisosViewModel()
    {
        matrizPermisosApiService = new MatrizPermisosApiService();
        rolApiService = new RolApiService();
        RefrescarCommand = new Command(async () => await CargarRolesAsync());
        //GuardarCommand = new Command(async () => await GuardarAsync(), () => PuedeGuardar);
        RevertirCambiosCommand = new Command(RevertirCambios, () => PuedeRevertir);
        //RevertirCambiosCommand = new Command(RevertirCambios);
        MarcarColumnaCommand = new Command<string>(MarcarColumna);
        LimpiarTodoCommand = new Command(() => MarcarColumna("ninguno"));
        MarcarFilaCommand = new Command<InterfazResponse>(fila => { fila.SetAll(true); ActualizarHabilitados(); });
        LimpiarFilaCommand = new Command<InterfazResponse>(fila => { fila.SetAll(false); ActualizarHabilitados(); });

        //Permisos.CollectionChanged += (_, __) => HookItems();   
        _ = CargarRolesAsync();
    }

    // ====================================================
    // MÉTODOS AUXILIARES
    // ====================================================

    private void RehookItems()
    {
        // Limpia suscripciones antiguas
        foreach (var it in _hooked)
            it.PropertyChanged -= Item_PropertyChanged;
        _hooked.Clear();

        // Engancha todos los ítems actuales
        foreach (var p in Permisos)
        {
            p.PropertyChanged += Item_PropertyChanged;
            _hooked.Add(p);
        }
    }
    //private void HookItems()
    //{
    //    foreach (var p in Permisos)
    //    {
    //        p.PropertyChanged += (_, __) =>
    //        {
    //            OnPropertyChanged(nameof(PuedeGuardar));
    //            OnPropertyChanged(nameof(PuedeRevertir));
    //            Estado = "Hay cambios sin guardar";
    //        };
    //    }

    //    OnPropertyChanged(nameof(PermisosFiltrados));
    //}

    private void ActualizarHabilitados()
    {
        //(GuardarCommand as Command).ChangeCanExecute();
        (RevertirCambiosCommand as Command).ChangeCanExecute();
        //OnPropertyChanged(nameof(PuedeGuardar));
        //OnPropertyChanged(nameof(PuedeRevertir));
    }

    // ====================================================
    // CARGA DE DATOS
    // ====================================================

    private async Task CargarRolesAsync()
    {
        try
        {
            IsBusy = true;

            if (Roles is not null)
                Roles.Clear();

            // Servicio HTTP para traer los roles de la base de datos
            var response = await rolApiService.GetRolAsync();

            if (response == null)
                return;
            else
            {
                Roles = response;
                Estado = $"Roles cargados: {Roles.Count}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarPermisosDelRolAsync(RolResponse rolResponse)
    {
        if (RolSeleccionado is null)
            return;

        try
        {
            IsBusy = true;
            if (Permisos.Count > 0 || Permisos is not null)
                Permisos.Clear();

            // Servicio de API para mandar a llamar los permisos del rol seleccionado
            var response = await matrizPermisosApiService.GetMatrizByRolAsync(new RolRequest(rolResponse));

            if (response.Count < 1)
            {
                Estado = "No se pudieron cargar los permisos.";
                return;
            }
            else
            {
                // Asignacion de los permisos de rol
                if (Permisos is not null)
                {
                    Permisos.Clear();

                    Permisos = response.FirstOrDefault().Permisos;

                    _snapshot = Permisos.Select(PermisoSnapshot.From).ToList();
                    Estado = $"Permisos cargados para {RolSeleccionado.NombreRol}";
                }
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", $"No se pudieron cargar los permisos: {ex.Message}", "Aceptar");
            await App.Current.MainPage.DisplayAlert("Error", $"{ex.Message}", "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ====================================================
    // COMPORTAMIENTO DE LOS COMANDOS
    // ====================================================

    private void MarcarColumna(string? columna)
    {
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
        OnPropertyChanged(nameof(Filtro));
        ActualizarHabilitados();
    }

    private void RevertirCambios()
    {
        foreach (var s in _snapshot)
        {
            var p = Permisos.FirstOrDefault(x => x.PermisoId == s.PermisoId);
            if (p is null) continue;

            p.Leer = s.Leer;
            p.Agregar = s.Agregar;
            p.Actualizar = s.Actualizar;
            p.Eliminar = s.Eliminar;
            p.AcceptChanges();
        }

        Estado = "Cambios revertidos";
        ActualizarHabilitados();
    }

    private void Permisos_CollectionChanged(object? s, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RehookItems();
        OnPropertyChanged(nameof(PermisosFiltrados));
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Permiso.Leer)
            or nameof(Permiso.Agregar)
            or nameof(Permiso.Actualizar)
            or nameof(Permiso.Eliminar))
        {
            OnPropertyChanged(nameof(PuedeGuardar));
            OnPropertyChanged(nameof(PuedeRevertir));

            Estado = "Hay cambios sin guardar";
            // Si tu filtro dependiera de estos flags, también:
            // OnPropertyChanged(nameof(PermisosFiltrados));
        }

        if (e.PropertyName == nameof(InterfazResponse.NombrePermiso))
            OnPropertyChanged(nameof(PermisosFiltrados));

        ActualizarHabilitados();
    }


    private async Task GuardarAsync()
    {
        if (RolSeleccionado is null)
            return;

        try
        {
            IsBusy = true;

            // 1️⃣ Toma solo los ítems sucios
            var cambiados = Permisos.Where(p => p.IsDirty).ToList();
            if (cambiados.Count == 0)
            {
                Estado = "No hay cambios para guardar.";
                return;
            }

            // 2️⃣ Arma el objeto que representa la matriz del rol
            var matriz = new MatrizPermisosRequest
            {
                Rol = new RolRequest
                {
                    RolId = RolSeleccionado.RolId,
                    NombreRol = RolSeleccionado.NombreRol
                },
                Permisos = cambiados.Select(p => new InterfazRequest
                {
                    PermisoId = p.PermisoId,
                    NombrePermiso = p.NombrePermiso,
                    Leer = p.Leer,
                    Agregar = p.Agregar,
                    Actualizar = p.Actualizar,
                    Eliminar = p.Eliminar
                }).ToList()
            };

            // 3️⃣ Como la API espera una lista, lo envolvemos
            var payload = new List<MatrizPermisosRequest> { matriz };

            // 4️⃣ Llamada real al API (PUT o POST según corresponda)
            var response = await matrizPermisosApiService.GuardarMatrizAsync(payload);

            if (response)
            {
                // 5️⃣ Confirmar cambios solo en los ítems que guardaste
                foreach (var p in cambiados)
                    p.AcceptChanges();

                // 6️⃣ Actualizar snapshot completo para Revertir
                _snapshot = Permisos.Select(PermisoSnapshot.From).ToList();

                Estado = "Permisos guardados correctamente";

                // 7️⃣ Refrescar comandos
                OnPropertyChanged(nameof(PuedeGuardar));
                OnPropertyChanged(nameof(PuedeRevertir));
                ActualizarHabilitados();
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Error", "No se pudieron guardar los permisos. Intente nuevamente.", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            Estado = "Error al guardar permisos";
            await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }


    //    private async Task GuardarAsync()
    //    {
    //        if (RolSeleccionado is null)
    //            return;

    //        try
    //        {
    //            IsBusy = true;

    //            var payload = new MatrizPermisosRequestDTO
    //            {
    //                Rol = new RolDTO { RolId = RolSeleccionado.RolId, NombreRol = RolSeleccionado.NombreRol },
    //                Permisos = Permisos.Select(p => new InterfazPermisoDTO
    //                {
    //                    PermisoId = p.PermisoId,
    //                    NombrePermiso = p.NombrePermiso,
    //                    Leer = p.Leer,
    //                    Agregar = p.Agregar,
    //                    Actualizar = p.Actualizar,
    //                    Eliminar = p.Eliminar
    //                }).ToList()
    //            };

    //            // Aquí iría tu llamada real al API:
    //            // await _api.GuardarMatriz(payload);
    //            await Task.Delay(300); // Simula espera de red

    //            foreach (var p in Permisos)
    //                p.AcceptChanges();

    //            _snapshot = Permisos.Select(PermisoSnapshot.From).ToList();

    //            Estado = "Permisos guardados correctamente";
    //            ActualizarHabilitados();
    //        }
    //        finally
    //        {
    //            IsBusy = false;
    //        }
    //    }
    //}
}
// ====================================================
// MODELOS AUXILIARES
// ====================================================


public record PermisoSnapshot(int PermisoId, bool Leer, bool Agregar, bool Actualizar, bool Eliminar)
{
    public static PermisoSnapshot From(InterfazResponse p) => new(p.PermisoId, p.Leer, p.Agregar, p.Actualizar, p.Eliminar);
}

