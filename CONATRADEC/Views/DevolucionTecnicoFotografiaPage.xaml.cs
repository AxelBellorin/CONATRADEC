using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.Views
{
    public partial class DevolucionTecnicoFotografiaPage : ContentPage
    {
        private readonly TaskCompletionSource<
            DevolucionTecnicoFormularioResultado?> resultado = new();
        private MotivoDevolucionTecnicoItem? motivoSeleccionado;
        private string instrucciones = string.Empty;
        private string mensaje = "Cargando catálogo de motivos...";
        private bool estaCargando = true;

        public DevolucionTecnicoFotografiaPage(
            InspeccionFotoV2 fotografia,
            int posicion,
            int total)
        {
            Fotografia = fotografia;
            PosicionTexto = $"Fotografía {posicion} de {total}";
            InitializeComponent();
            BindingContext = this;
        }

        public InspeccionFotoV2 Fotografia { get; }
        public string PosicionTexto { get; }
        public ObservableCollection<MotivoDevolucionTecnicoItem> Motivos { get; } = [];

        public MotivoDevolucionTecnicoItem? MotivoSeleccionado
        {
            get => motivoSeleccionado;
            set
            {
                if (ReferenceEquals(motivoSeleccionado, value))
                    return;
                motivoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMotivoSeleccionado));

                if (value != null)
                    Instrucciones = value.InstruccionSugerida;
            }
        }

        public bool TieneMotivoSeleccionado => MotivoSeleccionado != null;

        public string Instrucciones
        {
            get => instrucciones;
            set
            {
                if (instrucciones == value)
                    return;
                instrucciones = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                if (mensaje == value)
                    return;
                mensaje = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

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

        public Task<DevolucionTecnicoFormularioResultado?> EsperarResultadoAsync() =>
            resultado.Task;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Motivos.Count > 0)
                return;

            var api = new MotivoDevolucionTecnicoApiService();
            ApiResult<List<MotivoDevolucionTecnicoItem>> respuesta =
                await api.ListarActivosAsync();

            EstaCargando = false;
            if (!respuesta.Success)
            {
                Mensaje = respuesta.Message;
                return;
            }

            foreach (MotivoDevolucionTecnicoItem item in respuesta.Data ?? [])
                Motivos.Add(item);

            Mensaje = Motivos.Count == 0
                ? "No existen motivos activos. Solicite al administrador que configure el catálogo."
                : string.Empty;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (MotivoSeleccionado == null)
            {
                await DisplayAlert(
                    "Motivo requerido",
                    "Seleccione el motivo de devolución.",
                    "Aceptar");
                return;
            }

            string texto = Instrucciones.Trim();
            if (texto.Length is < 8 or > 3000)
            {
                await DisplayAlert(
                    "Instrucciones requeridas",
                    "Escriba entre 8 y 3000 caracteres para indicar la corrección esperada.",
                    "Aceptar");
                return;
            }

            resultado.TrySetResult(new DevolucionTecnicoFormularioResultado
            {
                MotivoId = MotivoSeleccionado.MotivoDevolucionTecnicoId,
                Instrucciones = texto
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
