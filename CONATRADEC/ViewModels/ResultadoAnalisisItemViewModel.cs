using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    public class ResultadoAnalisisItemViewModel : INotifyPropertyChanged
    {
        private int? elementoQuimicoId;
        private string codigoParametro = string.Empty;
        private string nombreParametro = string.Empty;
        private string valor = string.Empty;
        private string unidadSeleccionada = string.Empty;
        private string placeholderValor = "Valor";
        private bool esConstante;
        private bool esElementoQuimico;
        private bool puedeEliminar;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int? ElementoQuimicoId
        {
            get => elementoQuimicoId;
            set => SetProperty(ref elementoQuimicoId, value);
        }

        public string CodigoParametro
        {
            get => codigoParametro;
            set => SetProperty(ref codigoParametro, value);
        }

        public string NombreParametro
        {
            get => nombreParametro;
            set => SetProperty(ref nombreParametro, value);
        }

        public string Valor
        {
            get => valor;
            set => SetProperty(ref valor, value);
        }

        public string UnidadSeleccionada
        {
            get => unidadSeleccionada;
            set => SetProperty(ref unidadSeleccionada, value);
        }

        public string PlaceholderValor
        {
            get => placeholderValor;
            set => SetProperty(ref placeholderValor, value);
        }

        public bool EsConstante
        {
            get => esConstante;
            set => SetProperty(ref esConstante, value);
        }

        public bool EsElementoQuimico
        {
            get => esElementoQuimico;
            set => SetProperty(ref esElementoQuimico, value);
        }

        public bool PuedeEliminar
        {
            get => puedeEliminar;
            set => SetProperty(ref puedeEliminar, value);
        }

        public ObservableCollection<string> UnidadesMedida { get; set; } = new();

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}