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
        private bool estaCargando;
        private string mensajeCarga = "Cargando tipos de fotografía...";

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

        /// <summary>
        /// Estado visual para que la carga del catálogo no parezca un bloqueo.
        /// No modifica la lógica de corrección ni agrega esperas artificiales.
        /// </summary>
        public bool EstaCargando
        {
            get => estaCargando;
            private set
            {
                if (estaCargando == value)
                    return;

                estaCargando = value;
                OnPropertyChanged();
            }
        }

        public string MensajeCarga
        {
            get => mensajeCarga;
            private set
            {
                if (mensajeCarga == value)
                    return;

                mensajeCarga = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

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
            if (!PermiteCorregirMetadatos || TiposFotografia.Count > 0 || EstaCargando)
                return;

            EstaCargando = true;
            MensajeCarga = "Cargando tipos de fotografía...";

            try
            {
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
            finally
            {
                EstaCargando = false;
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (!PermiteCorregirMetadatos || EstaCargando)
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
            if (EstaCargando)
                return;

            resultado.TrySetResult(null);
            await Navigation.PopModalAsync(animated: false);
        }

        protected override bool OnBackButtonPressed()
        {
            if (EstaCargando)
                return true;

            resultado.TrySetResult(null);
            return base.OnBackButtonPressed();
        }
    }
}
