using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra las fuentes eliminadas lógicamente y permite
    /// restaurarlas conservando su información anterior.
    /// </summary>
    public sealed class FuenteNutrienteEliminadasViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService apiService;
        private readonly List<FuenteNutrienteResponse> fuentesOriginales = new();

        private string textoBusqueda = string.Empty;
        private string mensaje = string.Empty;
        private bool isRefreshing;

        public FuenteNutrienteEliminadasViewModel()
            : this(new FuenteNutrienteApiService())
        {
        }

        public FuenteNutrienteEliminadasViewModel(
            FuenteNutrienteApiService apiService)
        {
            this.apiService = apiService
                ?? throw new ArgumentNullException(nameof(apiService));

            BuscarCommand = new Command(AplicarFiltro);
            LimpiarCommand = new Command(LimpiarFiltro);
            RefrescarCommand = new Command(
                async () => await RefrescarAsync());
            ReactivarCommand = new Command<FuenteNutrienteResponse>(
                async fuente => await ReactivarAsync(fuente),
                fuente =>
                    fuente != null &&
                    CanEdit &&
                    !IsBusy);
            CerrarCommand = new Command(
                async () => await CerrarAsync());
        }

        public ObservableCollection<FuenteNutrienteResponse> Fuentes { get; } =
            new();

        public Command BuscarCommand { get; }
        public Command LimpiarCommand { get; }
        public Command RefrescarCommand { get; }
        public Command<FuenteNutrienteResponse> ReactivarCommand { get; }
        public Command CerrarCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor = value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public bool MostrarVacio =>
            CanView &&
            !IsBusy &&
            Fuentes.Count == 0 &&
            !TieneMensaje;

        public string Resumen =>
            Fuentes.Count == 1
                ? "1 fuente eliminada"
                : $"{Fuentes.Count} fuentes eliminadas";

        public bool MostrarAccesoDenegado =>
            !CanView;

        public async Task InicializarAsync()
        {
            ActualizarPermisos();
            await CargarAsync();
        }

        private void ActualizarPermisos()
        {
            LoadPagePermissions("fuenteNutrientePage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ReactivarCommand.ChangeCanExecute();
            NotificarEstado();
        }

        private async Task CargarAsync()
        {
            if (!CanView || IsBusy)
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();

                ApiResult<ObservableCollection<FuenteNutrienteResponse>> resultado =
                    await apiService.GetFuenteNutrienteInactivasResultAsync();

                if (!resultado.Success || resultado.Data == null)
                {
                    Mensaje = string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible cargar las fuentes eliminadas."
                        : resultado.Message;

                    fuentesOriginales.Clear();
                    Fuentes.Clear();
                    return;
                }

                fuentesOriginales.Clear();
                fuentesOriginales.AddRange(
                    resultado.Data
                        .Where(item =>
                            item.FuenteNutrientesId > 0 &&
                            item.Activo != true)
                        .OrderBy(item =>
                            item.NombreNutriente ?? string.Empty));

                AplicarFiltro();
            }
            catch (Exception ex)
            {
                Mensaje =
                    "Ocurrió un error inesperado al cargar las fuentes eliminadas.";

                await MostrarToastAsync(
                    "Error: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();
            }
        }

        private async Task RefrescarAsync()
        {
            if (IsBusy)
                return;

            IsRefreshing = true;
            await CargarAsync();
        }

        private void AplicarFiltro()
        {
            string filtro =
                (TextoBusqueda ?? string.Empty)
                    .Trim();

            IEnumerable<FuenteNutrienteResponse> consulta =
                fuentesOriginales;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                consulta = consulta.Where(item =>
                    (item.NombreNutriente ?? string.Empty).Contains(
                        filtro,
                        StringComparison.OrdinalIgnoreCase) ||
                    (item.DescripcionNutriente ?? string.Empty).Contains(
                        filtro,
                        StringComparison.OrdinalIgnoreCase));
            }

            Fuentes.Clear();

            foreach (FuenteNutrienteResponse fuente in consulta)
            {
                Fuentes.Add(fuente);
            }

            Mensaje = string.Empty;
            NotificarEstado();
        }

        private void LimpiarFiltro()
        {
            TextoBusqueda = string.Empty;
            AplicarFiltro();
        }

        private async Task ReactivarAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente?.FuenteNutrientesId is not > 0 ||
                IsBusy)
            {
                return;
            }

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permiso para reactivar fuentes de nutrientes.");
                return;
            }

            bool confirmar =
                await Application.Current!
                    .MainPage!
                    .DisplayAlert(
                        "Reactivar fuente",
                        $"¿Desea reactivar '{fuente.NombreNutriente}' con sus datos y clasificación anteriores?",
                        "Reactivar",
                        "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();

                ApiResult<FuenteNutrienteResponse> resultado =
                    await apiService.ReactivarFuenteNutrienteResultAsync(
                        fuente.FuenteNutrientesId.Value);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible reactivar la fuente."
                            : resultado.Message);
                    return;
                }

                fuentesOriginales.RemoveAll(item =>
                    item.FuenteNutrientesId ==
                    fuente.FuenteNutrientesId);

                Fuentes.Remove(fuente);
                NotificarEstado();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Fuente reactivada correctamente."
                        : resultado.Message);
            }
            finally
            {
                IsBusy = false;
                ReactivarCommand.ChangeCanExecute();
                NotificarEstado();
            }
        }

        private static async Task CerrarAsync()
        {
            if (Shell.Current?.Navigation != null)
            {
                await Shell.Current.Navigation.PopModalAsync();
            }
        }

        private void NotificarEstado()
        {
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(Resumen));
            OnPropertyChanged(nameof(TieneMensaje));
        }
    }
}
