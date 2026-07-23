using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CONATRADEC.ViewModels
{
    public class FuenteNutrienteViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService
            fuenteNutrienteApiService;

        private ObservableCollection<FuenteNutrienteResponse>
            list = new();

        private ObservableCollection<FuenteNutrienteResponse>
            listaCompleta = new();

        private ObservableCollection<string>
            elementosTabla = new();

        private ObservableCollection<
            FuenteNutrienteTablaDinamicaRow>
            tablaComposicion = new();

        private bool mostrarTablaComposicion;
        private bool cargandoFuentes;

        private FuenteNutrienteCategoriaOption?
            filtroCategoriaSeleccionada;

        public ObservableCollection<FuenteNutrienteResponse>
            List
        {
            get => list;
            set
            {
                if (ReferenceEquals(list, value))
                    return;

                list = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FuenteNutrienteResponse>
            ListaCompleta
        {
            get => listaCompleta;
            set
            {
                if (ReferenceEquals(listaCompleta, value))
                    return;

                listaCompleta = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<
            FuenteNutrienteCategoriaOption> FiltrosCategoria
        {
            get;
        }

        public FuenteNutrienteCategoriaOption?
            FiltroCategoriaSeleccionada
        {
            get => filtroCategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(
                        filtroCategoriaSeleccionada,
                        value))
                {
                    return;
                }

                filtroCategoriaSeleccionada = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(MostrarSeccionTablaComposicion));

                if (!MostrarSeccionTablaComposicion)
                    MostrarTablaComposicion = false;

                AplicarFiltroCategoria();
            }
        }

        public ObservableCollection<string> ElementosTabla
        {
            get => elementosTabla;
            set
            {
                if (ReferenceEquals(elementosTabla, value))
                    return;

                elementosTabla = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneElementosTabla));
                OnPropertyChanged(nameof(NoTieneElementosTabla));
                OnPropertyChanged(nameof(MostrarTablaConDatos));
                OnPropertyChanged(
                    nameof(MostrarMensajeTablaVacia));
            }
        }

        public ObservableCollection<
            FuenteNutrienteTablaDinamicaRow>
            TablaComposicion
        {
            get => tablaComposicion;
            set
            {
                if (ReferenceEquals(
                        tablaComposicion,
                        value))
                {
                    return;
                }

                tablaComposicion = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarTablaComposicion
        {
            get => mostrarTablaComposicion;
            set
            {
                if (mostrarTablaComposicion == value)
                    return;

                mostrarTablaComposicion = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TextoBotonTablaComposicion));
                OnPropertyChanged(
                    nameof(MostrarTablaConDatos));
                OnPropertyChanged(
                    nameof(MostrarMensajeTablaVacia));
            }
        }

        public string TextoBotonTablaComposicion =>
            MostrarTablaComposicion
                ? "Ocultar tabla"
                : "Ver tabla";

        public bool MostrarSeccionTablaComposicion =>
            FiltroCategoriaSeleccionada?.Codigo !=
            FuenteNutrienteCategoriaOption
                .CodigoEnmiendaCalcarea;

        public bool TieneElementosTabla =>
            ElementosTabla.Count > 0;

        public bool NoTieneElementosTabla =>
            !TieneElementosTabla;

        public bool MostrarTablaConDatos =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            TieneElementosTabla;

        public bool MostrarMensajeTablaVacia =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            NoTieneElementosTabla;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public Command ToggleTablaComposicionCommand
        {
            get;
        }

        public FuenteNutrienteViewModel()
            : this(new FuenteNutrienteApiService())
        {
        }

        public FuenteNutrienteViewModel(
            FuenteNutrienteApiService fuenteNutrienteApiService)
        {
            this.fuenteNutrienteApiService =
                fuenteNutrienteApiService ??
                throw new ArgumentNullException(
                    nameof(fuenteNutrienteApiService));

            FiltrosCategoria =
                new ObservableCollection<
                    FuenteNutrienteCategoriaOption>();

            CargarFiltrosCategoria();

            AddCommand =
                new Command(
                    async () => await OnAddAsync());

            EditCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await OnEditAsync(fuente));

            DeleteCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await OnDeleteAsync(fuente));

            ViewCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await OnViewAsync(fuente));

            ToggleTablaComposicionCommand =
                new Command(() =>
                {
                    if (!MostrarSeccionTablaComposicion)
                        return;

                    MostrarTablaComposicion =
                        !MostrarTablaComposicion;
                });
        }

        private void CargarFiltrosCategoria()
        {
            FiltrosCategoria.Clear();

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoTodas,

                    Nombre =
                        "Todas"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoBalanceNutricional,

                    Nombre =
                        "Balance nutricional"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoEnmiendaCalcarea,

                    Nombre =
                        "Enmienda calcárea"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption
                            .CodigoFertilizacionMixta,

                    Nombre =
                        "Fertilización mixta"
                });

            filtroCategoriaSeleccionada =
                FiltrosCategoria.FirstOrDefault();

            OnPropertyChanged(
                nameof(FiltroCategoriaSeleccionada));

            OnPropertyChanged(
                nameof(MostrarSeccionTablaComposicion));
        }

        public async Task LoadFuenteNutriente(
            bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver fuentes de nutrientes.");

                return;
            }

            if (cargandoFuentes)
                return;

            cargandoFuentes = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado =
                    await fuenteNutrienteApiService
                        .GetFuenteNutrienteResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        resultado.Message);

                    return;
                }

                ListaCompleta =
                    new ObservableCollection<
                        FuenteNutrienteResponse>(
                        (resultado.Data ??
                         new ObservableCollection<
                             FuenteNutrienteResponse>())
                        .OrderBy(x =>
                            x.NombreNutriente ??
                            string.Empty));

                AplicarFiltroCategoria();

                if (ListaCompleta.Count == 0)
                {
                    await MostrarToastAsync(
                        "No se encontraron fuentes de nutrientes.");
                }
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar las fuentes de nutrientes.");
            }
            finally
            {
                cargandoFuentes = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private void AplicarFiltroCategoria()
        {
            string codigoFiltro =
                FiltroCategoriaSeleccionada?.Codigo ??
                FuenteNutrienteCategoriaOption
                    .CodigoTodas;

            IEnumerable<FuenteNutrienteResponse>
                fuentesFiltradas = ListaCompleta;

            if (codigoFiltro ==
                FuenteNutrienteCategoriaOption
                    .CodigoBalanceNutricional)
            {
                fuentesFiltradas =
                    fuentesFiltradas.Where(x =>
                        x.EsBalanceNutricional);
            }
            else if (codigoFiltro ==
                     FuenteNutrienteCategoriaOption
                         .CodigoEnmiendaCalcarea)
            {
                fuentesFiltradas =
                    fuentesFiltradas.Where(x =>
                        x.EsEnmiendaCalcarea);
            }
            else if (codigoFiltro ==
                     FuenteNutrienteCategoriaOption
                         .CodigoFertilizacionMixta)
            {
                fuentesFiltradas =
                    fuentesFiltradas.Where(x =>
                        x.EsFertilizacionMixta);
            }

            var listaFiltrada =
                new ObservableCollection<
                    FuenteNutrienteResponse>(
                    fuentesFiltradas.OrderBy(x =>
                        x.NombreNutriente ??
                        string.Empty));

            List =
                listaFiltrada;

            ConstruirTablaComposicion(
                listaFiltrada);
        }

        private void ConstruirTablaComposicion(
            IEnumerable<FuenteNutrienteResponse> fuentes)
        {
            if (!MostrarSeccionTablaComposicion)
            {
                ElementosTabla =
                    new ObservableCollection<string>();

                TablaComposicion =
                    new ObservableCollection<
                        FuenteNutrienteTablaDinamicaRow>();

                return;
            }

            var fuentesConAporte =
                fuentes
                    .Where(
                        FuenteTieneAporteElementoQuimico)
                    .OrderBy(x =>
                        x.NombreNutriente ??
                        string.Empty)
                    .ToList();

            var simbolos =
                fuentesConAporte
                    .SelectMany(f =>
                        f.ElementosQuimicos ??
                        new List<
                            FuenteNutrienteElementoQuimicoResponse>())
                    .Where(e =>
                        !string.IsNullOrWhiteSpace(
                            e.SimboloElementoQuimico) &&
                        (e.CantidadAporte ?? 0) > 0)
                    .Select(e =>
                        e.SimboloElementoQuimico!
                            .Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        ObtenerOrdenElemento)
                    .ThenBy(x => x)
                    .ToList();

            ElementosTabla =
                new ObservableCollection<string>(
                    simbolos);

            var filas =
                new ObservableCollection<
                    FuenteNutrienteTablaDinamicaRow>();

            foreach (var fuente
                     in fuentesConAporte)
            {
                var fila =
                    new FuenteNutrienteTablaDinamicaRow
                    {
                        FuenteNutrientesId =
                            fuente.FuenteNutrientesId,

                        Fuente =
                            fuente.NombreNutriente ??
                            "Fuente sin nombre"
                    };

                foreach (var simbolo
                         in simbolos)
                {
                    var aporte =
                        fuente.ElementosQuimicos?
                            .FirstOrDefault(e =>
                                (e.SimboloElementoQuimico ??
                                 string.Empty)
                                .Trim()
                                .Equals(
                                    simbolo,
                                    StringComparison
                                        .OrdinalIgnoreCase));

                    fila.Celdas.Add(
                        new FuenteNutrienteTablaDinamicaCell
                        {
                            SimboloElemento =
                                simbolo,

                            Valor =
                                aporte?.CantidadAporte ??
                                0
                        });
                }

                filas.Add(fila);
            }

            TablaComposicion =
                filas;
        }

        private static bool
            FuenteTieneAporteElementoQuimico(
                FuenteNutrienteResponse fuente)
        {
            return
                fuente.ElementosQuimicos != null &&
                fuente.ElementosQuimicos.Any(x =>
                    !string.IsNullOrWhiteSpace(
                        x.SimboloElementoQuimico) &&
                    (x.CantidadAporte ?? 0) > 0);
        }

        private static int ObtenerOrdenElemento(
            string simbolo)
        {
            return simbolo
                .Trim()
                .ToUpperInvariant() switch
            {
                "N" => 1,
                "P" => 2,
                "K" => 3,
                "CA" => 4,
                "MG" => 5,
                "ZN" => 6,
                "S" => 7,
                "B" => 8,
                _ => 100
            };
        }

        private async Task OnAddAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar.");

                return;
            }

            if (IsBusy)
                return;

            var parameters =
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Create
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest()
                    }
                };

            await GoToAsyncParameters(
                AppRoutes.FuenteNutrienteFormulario,
                parameters);
        }

        private async Task OnEditAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar.");

                return;
            }

            if (IsBusy || fuente == null)
                return;

            var parameters =
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.Edit
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest(
                            fuente)
                    }
                };

            await GoToAsyncParameters(
                AppRoutes.FuenteNutrienteFormulario,
                parameters);
        }

        private async Task OnViewAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver.");

                return;
            }

            if (IsBusy || fuente == null)
                return;

            var parameters =
                new Dictionary<string, object>
                {
                    {
                        "Mode",
                        FormMode.FormModeSelect.View
                    },
                    {
                        "Fuente",
                        new FuenteNutrienteRequest(
                            fuente)
                    }
                };

            await GoToAsyncParameters(
                AppRoutes.FuenteNutrienteFormulario,
                parameters);
        }

        private async Task OnDeleteAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar.");

                return;
            }

            if (IsBusy || fuente == null)
                return;

            bool confirmar =
                await Shell.Current.DisplayAlert(
                    "Eliminar",
                    $"¿Desea eliminar la fuente '{fuente.NombreNutriente}'?",
                    "Sí",
                    "No");

            if (!confirmar)
                return;

            IsBusy = true;

            try
            {
                var resultado =
                    await fuenteNutrienteApiService
                        .DeleteFuenteNutrienteResultAsync(
                            new FuenteNutrienteRequest(
                                fuente));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        resultado.Message);

                    return;
                }

                ListaCompleta.Remove(fuente);
                AplicarFiltroCategoria();

                await MostrarToastAsync(
                    "Fuente de nutriente eliminada correctamente.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar la fuente de nutriente.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
