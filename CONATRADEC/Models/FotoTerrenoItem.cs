using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Models
{
    public class FotoTerrenoItem :
        INotifyPropertyChanged
    {
        private ImageSource? imagen;

        public int? FotoTerrenoId { get; set; }

        public int? TerrenoId { get; set; }

        public string? UrlFotoTerreno { get; set; }

        public string? LocalPath { get; set; }

        public string? NombreArchivo { get; set; }

        public bool EsNueva { get; set; }

        /// <summary>
        /// Notifica los cambios para que la interfaz pueda desconectar
        /// inmediatamente una imagen antes de eliminar su archivo temporal.
        /// </summary>
        public ImageSource? Imagen
        {
            get => imagen;
            set
            {
                if (ReferenceEquals(
                        imagen,
                        value))
                {
                    return;
                }

                imagen = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}
