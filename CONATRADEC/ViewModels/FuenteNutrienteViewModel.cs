using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class FuenteNutrienteViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService;

        private ObservableCollection<FuenteNutrienteResponse> list = new();
        private ObservableCollection<FuenteNutrienteResponse> listaCompleta = new();

        private ObservableCollection<string> elementosTabla = new();
        private ObservableCollection<FuenteNutrienteTablaDinamicaRow> tablaComposicion = new();

        private bool mostrarTablaComposicion = false;
        private FuenteNutrienteCategoriaOption? filtroCategoriaSeleccionada;

        public ObservableCollection<FuenteNutrienteResponse> List
        {
            get => list;
            set
            {
                list = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FuenteNutrienteResponse> ListaCompleta
        {
            get => listaCompleta;
            set
            {
                listaCompleta = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FuenteNutrienteCategoriaOption> FiltrosCategoria { get; }

        public FuenteNutrienteCategoriaOption? FiltroCategoriaSeleccionada
        {
            get => filtroCategoriaSeleccionada;
            set
            {
                filtroCategoriaSeleccionada = value;
                OnPropertyChanged();

                AplicarFiltroCategoria();
            }
        }

        public ObservableCollection<string> ElementosTabla
        {
            get => elementosTabla;
            set
            {
                elementosTabla = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneElementosTabla));
                OnPropertyChanged(nameof(NoTieneElementosTabla));
                OnPropertyChanged(nameof(MostrarTablaConDatos));
                OnPropertyChanged(nameof(MostrarMensajeTablaVacia));
            }
        }

        public ObservableCollection<FuenteNutrienteTablaDinamicaRow> TablaComposicion
        {
            get => tablaComposicion;
            set
            {
                tablaComposicion = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarTablaComposicion
        {
            get => mostrarTablaComposicion;
            set
            {
                mostrarTablaComposicion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonTablaComposicion));
                OnPropertyChanged(nameof(MostrarTablaConDatos));
                OnPropertyChanged(nameof(MostrarMensajeTablaVacia));
            }
        }

        public string TextoBotonTablaComposicion =>
            MostrarTablaComposicion ? "Ocultar tabla" : "Ver tabla";

        public bool TieneElementosTabla =>
            ElementosTabla != null && ElementosTabla.Any();

        public bool NoTieneElementosTabla =>
            !TieneElementosTabla;

        public bool MostrarTablaConDatos =>
            MostrarTablaComposicion && TieneElementosTabla;

        public bool MostrarMensajeTablaVacia =>
            MostrarTablaComposicion && NoTieneElementosTabla;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }
        public Command ToggleTablaComposicionCommand { get; }

        public FuenteNutrienteViewModel()
        {
            fuenteNutrienteApiService = new FuenteNutrienteApiService();

            FiltrosCategoria = new ObservableCollection<FuenteNutrienteCategoriaOption>();
            CargarFiltrosCategoria();

            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<FuenteNutrienteResponse>(OnEdit);
            DeleteCommand = new Command<FuenteNutrienteResponse>(OnDelete);
            ViewCommand = new Command<FuenteNutrienteResponse>(OnView);

            ToggleTablaComposicionCommand = new Command(() =>
            {
                MostrarTablaComposicion = !MostrarTablaComposicion;
            });
        }

        private void CargarFiltrosCategoria()
        {
            FiltrosCategoria.Clear();

            FiltrosCategoria.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoTodas,
                Nombre = "Todas"
            });

            FiltrosCategoria.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoBalanceNutricional,
                Nombre = "Balance nutricional"
            });

            FiltrosCategoria.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea,
                Nombre = "Enmienda calcárea"
            });

            FiltrosCategoria.Add(new FuenteNutrienteCategoriaOption
            {
                Codigo = FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta,
                Nombre = "Fertilización mixta"
            });

            filtroCategoriaSeleccionada = FiltrosCategoria.FirstOrDefault();
            OnPropertyChanged(nameof(FiltroCategoriaSeleccionada));
        }

        public async Task LoadFuenteNutriente(bool isBusy)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver fuentes de nutrientes.");
                return;
            }

            IsBusy = isBusy;

            List.Clear();
            ListaCompleta.Clear();
            ElementosTabla.Clear();
            TablaComposicion.Clear();

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await fuenteNutrienteApiService.GetFuenteNutrienteAsync();

            var fuentesOrdenadas = new ObservableCollection<FuenteNutrienteResponse>(
                response.OrderBy(x => x.NombreNutriente ?? string.Empty)
            );

            if (fuentesOrdenadas.Any())
            {
                ListaCompleta = fuentesOrdenadas;
                AplicarFiltroCategoria();
            }
            else
            {
                await MostrarToastAsync("No se encontraron fuentes de nutrientes.");
                ListaCompleta = new ObservableCollection<FuenteNutrienteResponse>();
                List = new ObservableCollection<FuenteNutrienteResponse>();
                ElementosTabla = new ObservableCollection<string>();
                TablaComposicion = new ObservableCollection<FuenteNutrienteTablaDinamicaRow>();
            }

            IsBusy = false;
        }

        private void AplicarFiltroCategoria()
        {
            if (ListaCompleta == null)
                return;

            string codigoFiltro = FiltroCategoriaSeleccionada?.Codigo
                ?? FuenteNutrienteCategoriaOption.CodigoTodas;

            IEnumerable<FuenteNutrienteResponse> fuentesFiltradas = ListaCompleta;

            if (codigoFiltro == FuenteNutrienteCategoriaOption.CodigoBalanceNutricional)
            {
                fuentesFiltradas = fuentesFiltradas.Where(x => x.EsBalanceNutricional);
            }
            else if (codigoFiltro == FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea)
            {
                fuentesFiltradas = fuentesFiltradas.Where(x => x.EsEnmiendaCalcarea);
            }
            else if (codigoFiltro == FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta)
            {
                fuentesFiltradas = fuentesFiltradas.Where(x => x.EsFertilizacionMixta);
            }

            var listaFiltrada = new ObservableCollection<FuenteNutrienteResponse>(
                fuentesFiltradas.OrderBy(x => x.NombreNutriente ?? string.Empty)
            );

            List = listaFiltrada;

            ConstruirTablaComposicion(listaFiltrada);
        }

        private void ConstruirTablaComposicion(IEnumerable<FuenteNutrienteResponse> fuentes)
        {
            var fuentesConAporte = fuentes
                .Where(FuenteTieneAporteElementoQuimico)
                .OrderBy(x => x.NombreNutriente ?? string.Empty)
                .ToList();

            var simbolos = fuentesConAporte
                .SelectMany(f => f.ElementosQuimicos ?? new List<FuenteNutrienteElementoQuimicoResponse>())
                .Where(e =>
                    !string.IsNullOrWhiteSpace(e.SimboloElementoQuimico) &&
                    (e.CantidadAporte ?? 0) > 0)
                .Select(e => e.SimboloElementoQuimico!.Trim())
                .Distinct()
                .OrderBy(ObtenerOrdenElemento)
                .ThenBy(x => x)
                .ToList();

            ElementosTabla = new ObservableCollection<string>(simbolos);

            var filas = new ObservableCollection<FuenteNutrienteTablaDinamicaRow>();

            foreach (var fuente in fuentesConAporte)
            {
                var fila = new FuenteNutrienteTablaDinamicaRow
                {
                    FuenteNutrientesId = fuente.FuenteNutrientesId,
                    Fuente = fuente.NombreNutriente ?? "Fuente sin nombre"
                };

                foreach (var simbolo in simbolos)
                {
                    var aporte = fuente.ElementosQuimicos?
                        .FirstOrDefault(e =>
                            (e.SimboloElementoQuimico ?? string.Empty)
                                .Trim()
                                .Equals(simbolo, System.StringComparison.OrdinalIgnoreCase));

                    fila.Celdas.Add(new FuenteNutrienteTablaDinamicaCell
                    {
                        SimboloElemento = simbolo,
                        Valor = aporte?.CantidadAporte ?? 0
                    });
                }

                filas.Add(fila);
            }

            TablaComposicion = filas;
        }

        private bool FuenteTieneAporteElementoQuimico(FuenteNutrienteResponse fuente)
        {
            return fuente?.ElementosQuimicos != null &&
                   fuente.ElementosQuimicos.Any(x =>
                       !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico) &&
                       (x.CantidadAporte ?? 0) > 0);
        }

        private int ObtenerOrdenElemento(string simbolo)
        {
            return simbolo.Trim().ToUpper() switch
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

        private async Task OnAdd()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Fuente", new FuenteNutrienteRequest() }
            };

            await GoToAsyncParameters("//FuenteNutrienteFormPage", parameters);
        }

        private async void OnEdit(FuenteNutrienteResponse fuente)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (fuente == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Fuente", new FuenteNutrienteRequest(fuente) }
            };

            await GoToAsyncParameters("//FuenteNutrienteFormPage", parameters);
        }

        private async void OnView(FuenteNutrienteResponse fuente)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver.");
                return;
            }

            if (fuente == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Fuente", new FuenteNutrienteRequest(fuente) }
            };

            await GoToAsyncParameters("//FuenteNutrienteFormPage", parameters);
        }

        private async void OnDelete(FuenteNutrienteResponse fuente)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (fuente == null)
                return;

            bool confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar",
                $"¿Deseas eliminar la fuente '{fuente.NombreNutriente}'?",
                "Sí",
                "No");

            if (!confirm)
                return;

            if (!await TieneInternetAsync())
            {
                await MostrarToastAsync("Sin conexión a internet.");
                return;
            }

            var result = await fuenteNutrienteApiService.DeleteFuenteNutrienteAsync(
                new FuenteNutrienteRequest(fuente));

            if (result)
            {
                await MostrarToastAsync("Fuente de nutriente eliminada.");
                await LoadFuenteNutriente(true);
            }
            else
            {
                await MostrarToastAsync("No se pudo eliminar la fuente de nutriente.");
            }
        }
    }
}