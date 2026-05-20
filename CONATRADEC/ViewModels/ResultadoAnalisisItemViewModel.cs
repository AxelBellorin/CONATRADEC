using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    // ===============================================================
    // Clase: ResultadoAnalisisItemViewModel
    // Descripción:
    //   Representa cada fila visible en el formulario de análisis.
    //   Cada fila contiene:
    //     - Parámetro
    //     - Valor digitado
    //     - Unidad seleccionada
    // ===============================================================
    public class ResultadoAnalisisItemViewModel : GlobalService
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        private string codigoParametro = string.Empty;
        private string nombreParametro = string.Empty;
        private string valor = string.Empty;
        private string unidadSeleccionada = string.Empty;
        private string placeholderValor = "Valor";


        // ===========================================================
        // ================= EVENTO PROPERTYCHANGED ==================
        // ===========================================================

        public event PropertyChangedEventHandler? PropertyChanged;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

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

        public ObservableCollection<string> UnidadesMedida { get; set; } = new();


        // ===========================================================
        // ===================== MÉTODOS AUXILIARES ==================
        // ===========================================================

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