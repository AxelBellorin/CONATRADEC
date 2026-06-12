using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class ResultadoAnalisisItemViewModel : GlobalService
    {
        private string valor = string.Empty;
        private UnidadMedidaResponse? unidadSeleccionada;

        public int? ElementoQuimicoId { get; set; }

        public string CodigoParametro { get; set; } = string.Empty;

        public string NombreParametro { get; set; } = string.Empty;

        public string PlaceholderValor { get; set; } = string.Empty;

        public bool EsConstante { get; set; }

        public bool EsElementoQuimico { get; set; }

        public bool PuedeEliminar { get; set; }

        public string Valor
        {
            get => valor;
            set
            {
                valor = value;
                OnPropertyChanged(nameof(Valor));
            }
        }

        public ObservableCollection<UnidadMedidaResponse> UnidadesMedida { get; set; } = new ObservableCollection<UnidadMedidaResponse>();

        public UnidadMedidaResponse? UnidadSeleccionada
        {
            get => unidadSeleccionada;
            set
            {
                unidadSeleccionada = value;
                OnPropertyChanged(nameof(UnidadSeleccionada));
            }
        }
    }
}