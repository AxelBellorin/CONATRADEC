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
        public ICommand UserPageButtonCommand { get; }
        public MainPageViewModel()
        {
            MainPageButtonCommand = new Command(async ()=> await GoToMainPageButtonCommand());
            UserPageButtonCommand = new Command(async ()=> await GoToUserPageButtonCommand());
        }      

        private async Task GoToMainPageButtonCommand()
        {
            await  Shell.Current.GoToAsync("//LoginPage");
        }

        private async Task GoToUserPageButtonCommand()
        {
            await Shell.Current.GoToAsync("//UserPage");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
