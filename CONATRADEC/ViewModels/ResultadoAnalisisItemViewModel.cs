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
                string valorNormalizado =
                    NormalizarEntradaDecimal(value);

                if (valor == valorNormalizado)
                    return;

                valor = valorNormalizado;
                OnPropertyChanged(nameof(Valor));
            }
        }

        public ObservableCollection<UnidadMedidaResponse>
            UnidadesMedida { get; set; } = new();

        public UnidadMedidaResponse? UnidadSeleccionada
        {
            get => unidadSeleccionada;
            set
            {
                if (unidadSeleccionada == value)
                    return;

                unidadSeleccionada = value;
                OnPropertyChanged(nameof(UnidadSeleccionada));
            }
        }

        private static string NormalizarEntradaDecimal(
            string? valorEntrada)
        {
            string texto = (valorEntrada ?? string.Empty)
                .Trim()
                .Replace(',', '.');

            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            int primerSeparador = texto.IndexOf('.');

            if (primerSeparador < 0)
                return texto;

            string parteEntera = texto[..primerSeparador];
            string parteDecimal = texto[(primerSeparador + 1)..]
                .Replace(".", string.Empty);

            return $"{parteEntera}.{parteDecimal}";
        }
    }
}
