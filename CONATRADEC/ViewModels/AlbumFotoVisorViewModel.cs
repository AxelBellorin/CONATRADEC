using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class AlbumFotoVisorViewModel :
        GlobalService
    {
        private ObservableCollection<AlbumFotoResponse> fotos = new();
        private AlbumFotoResponse? fotoSeleccionada;
        private int fotoSeleccionadaId;
        private string tituloAlbum = "Fotografía";

        public ObservableCollection<AlbumFotoResponse> Fotos
        {
            get => fotos;
            private set
            {
                fotos = value;
                OnPropertyChanged();
                SeleccionarFotoPendiente();
                OnPropertyChanged(nameof(ContadorTexto));
            }
        }

        public AlbumFotoResponse? FotoSeleccionada
        {
            get => fotoSeleccionada;
            set
            {
                if (fotoSeleccionada == value)
                    return;

                fotoSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContadorTexto));
                OnPropertyChanged(nameof(DescripcionTexto));
            }
        }

        public int FotoSeleccionadaId
        {
            get => fotoSeleccionadaId;
            set
            {
                fotoSeleccionadaId = value;
                OnPropertyChanged();
                SeleccionarFotoPendiente();
            }
        }

        public string TituloAlbum
        {
            get => tituloAlbum;
            set
            {
                tituloAlbum = string.IsNullOrWhiteSpace(value)
                    ? "Fotografía"
                    : value;
                OnPropertyChanged();
            }
        }

        public string ContadorTexto
        {
            get
            {
                if (FotoSeleccionada == null || Fotos.Count == 0)
                    return "0 de 0";

                int index = Fotos.IndexOf(FotoSeleccionada);
                return $"{index + 1} de {Fotos.Count}";
            }
        }

        public string DescripcionTexto =>
            string.IsNullOrWhiteSpace(
                FotoSeleccionada?.DescripcionFoto)
                ? "Sin descripción"
                : FotoSeleccionada.DescripcionFoto!;

        public Command CerrarCommand { get; }

        public AlbumFotoVisorViewModel()
        {
            CerrarCommand = new Command(
                async () => await GoToAsyncParameters(
                    AppRoutes.Regresar));
        }

        public void EstablecerFotos(
            IEnumerable<AlbumFotoResponse>? items)
        {
            Fotos = new ObservableCollection<AlbumFotoResponse>(
                items ?? Enumerable.Empty<AlbumFotoResponse>());
        }

        private void SeleccionarFotoPendiente()
        {
            if (Fotos.Count == 0)
            {
                FotoSeleccionada = null;
                return;
            }

            FotoSeleccionada =
                Fotos.FirstOrDefault(x =>
                    x.AlbumBotanicoCafeFotoId ==
                    FotoSeleccionadaId)
                ?? Fotos[0];
        }
    }
}
