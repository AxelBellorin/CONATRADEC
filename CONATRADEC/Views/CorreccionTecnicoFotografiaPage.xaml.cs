using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.Views
{
    public partial class CorreccionTecnicoFotografiaPage : ContentPage
    {
        private readonly TaskCompletionSource<
            CorreccionTecnicoFormularioResultado?> resultado = new();
        private TipoFotografiaIAItem? tipoSeleccionado;
        private DateTime fechaCampo;
        private string respuestaTecnico = string.Empty;

        public CorreccionTecnicoFotografiaPage(
            InspeccionFotoV2 fotografia,
            DevolucionTecnicoFotografiaV2 devolucion)
        {
            Fotografia = fotografia;
            Devolucion = devolucion;
            fechaCampo = fotografia.FechaIdentificacionCampo?.Date ?? DateTime.Today;
            InitializeComponent();
            BindingContext = this;
        }

        public InspeccionFotoV2 Fotografia { get; }
        public DevolucionTecnicoFotografiaV2 Devolucion { get; }
        public ObservableCollection<TipoFotografiaIAItem> TiposFotografia { get; } = [];
        public bool RequiereNuevaFotografia => Devolucion.RequiereNuevaFotografia;
        public bool PermiteCorregirMetadatos =>
            Devolucion.PermiteCorregirMetadatos && !RequiereNuevaFotografia;
        public DateTime FechaMaxima => DateTime.Today;

        public TipoFotografiaIAItem? TipoSeleccionado
        {
            get => tipoSeleccionado;
            set
            {
                if (ReferenceEquals(tipoSeleccionado, value))
                    return;
                tipoSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaCampo
        {
            get => fechaCampo;
            set
            {
                DateTime nueva = value.Date;
                if (fechaCampo == nueva)
                    return;
                fechaCampo = nueva;
                OnPropertyChanged();
            }
        }

        public string RespuestaTecnico
        {
            get => respuestaTecnico;
            set
            {
                if (respuestaTecnico == value)
                    return;
                respuestaTecnico = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public Task<CorreccionTecnicoFormularioResultado?> EsperarResultadoAsync() =>
            resultado.Task;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!PermiteCorregirMetadatos || TiposFotografia.Count > 0)
                return;

            var api = new TipoFotografiaIAApiService();
            ApiResult<List<TipoFotografiaIAItem>> respuesta =
                await api.ListarActivosAsync();

            if (!respuesta.Success)
            {
                await DisplayAlert(
                    "Tipos de fotografía",
                    respuesta.Message,
                    "Aceptar");
                return;
            }

            foreach (TipoFotografiaIAItem item in respuesta.Data ?? [])
                TiposFotografia.Add(item);

            TipoSeleccionado = TiposFotografia.FirstOrDefault(item =>
                string.Equals(
                    item.Codigo,
                    Fotografia.TipoFotografia,
                    StringComparison.OrdinalIgnoreCase)) ??
                TiposFotografia.FirstOrDefault();
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (!PermiteCorregirMetadatos)
                return;

            if (TipoSeleccionado == null)
            {
                await DisplayAlert(
                    "Tipo requerido",
                    "Seleccione el tipo de fotografía corregido.",
                    "Aceptar");
                return;
            }

            string respuesta = RespuestaTecnico.Trim();
            if (respuesta.Length is < 8 or > 2000)
            {
                await DisplayAlert(
                    "Respuesta requerida",
                    "Escriba entre 8 y 2000 caracteres explicando la corrección realizada.",
                    "Aceptar");
                return;
            }

            resultado.TrySetResult(new CorreccionTecnicoFormularioResultado
            {
                TipoFotografia = TipoSeleccionado.Codigo,
                FechaIdentificacionCampo = FechaCampo,
                RespuestaTecnico = respuesta
            });

            await Navigation.PopModalAsync(animated: false);
        }

        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            resultado.TrySetResult(null);
            await Navigation.PopModalAsync(animated: false);
        }

        protected override bool OnBackButtonPressed()
        {
            resultado.TrySetResult(null);
            return base.OnBackButtonPressed();
        }
    }
}
