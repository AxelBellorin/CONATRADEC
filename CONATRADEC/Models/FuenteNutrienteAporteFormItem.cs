using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteAporteFormItem : INotifyPropertyChanged
    {
        private ElementoQuimicoResponse? elementoSeleccionado;
        private string cantidadAporteTexto = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int? ElementoQuimicosId { get; set; }

        public ElementoQuimicoResponse? ElementoSeleccionado
        {
            get => elementoSeleccionado;
            set
            {
                elementoSeleccionado = value;
                ElementoQuimicosId = value?.ElementoQuimicosId;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreElementoMostrar));
            }
        }

        public string CantidadAporteTexto
        {
            get => cantidadAporteTexto;
            set
            {
                cantidadAporteTexto = value;
                OnPropertyChanged();
            }
        }

        public string NombreElementoMostrar
        {
            get
            {
                if (ElementoSeleccionado == null)
                    return "Elemento no seleccionado";

                string simbolo = ElementoSeleccionado.SimboloElementoQuimico?.Trim() ?? string.Empty;
                string nombre = ElementoSeleccionado.NombreElementoQuimico ?? string.Empty;

                if (string.IsNullOrWhiteSpace(simbolo))
                    return nombre;

                return $"{nombre} ({simbolo})";
            }
        }

        public FuenteNutrienteAporteFormItem()
        {
        }

        public FuenteNutrienteAporteFormItem(FuenteNutrienteElementoQuimicoRequest request)
        {
            ElementoQuimicosId = request.ElementoQuimicosId;
            CantidadAporteTexto = request.CantidadAporte.ToString("N2");
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}