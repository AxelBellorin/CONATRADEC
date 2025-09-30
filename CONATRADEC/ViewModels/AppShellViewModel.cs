using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    class AppShellViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? imageUser;

        public string? ImageUser { get => imageUser; set { imageUser = value; OnPropertyChanged(); } }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
