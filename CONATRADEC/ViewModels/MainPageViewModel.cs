using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    
    class MainPageViewModel: INotifyPropertyChanged
    {
        public ICommand MainPageButtonCommand { get; }
        public MainPageViewModel()
        {
            MainPageButtonCommand = new Command(async ()=> await GoToMainPageButtonCommand());
        }      

        private async Task GoToMainPageButtonCommand()
        {
            await  Shell.Current.GoToAsync("//LoginPage");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
