using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAnalizadorViewModel :
        DiagnosticoIAViewModelBase
    {
        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await CargarAsync(),
                () => !IsBusy);

            AbrirCommand = new Command<InspeccionFitosanitariaListaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null && !IsBusy);
        }

        public ObservableCollection<InspeccionFitosanitariaListaItemV2>
            Solicitudes { get; } = [];

        public Command ActualizarCommand { get; }
        public Command<InspeccionFitosanitariaListaItemV2> AbrirCommand { get; }

        public bool SinSolicitudes =>
            !IsBusy && Solicitudes.Count == 0;

        public Task InicializarAsync() => CargarAsync();

        private async Task CargarAsync()
        {
            if (IsBusy || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado =
                "Cargando fotografías pendientes del analizador...";
            ActualizarCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();

            try
            {
                List<InspeccionFitosanitariaListaItemV2> items =
                    await InspeccionApi.ObtenerBandejaAsync(
                        DiagnosticoIARoutes.ModoAnalizador);

                Solicitudes.Clear();
                foreach (InspeccionFitosanitariaListaItemV2 item in items)
                    Solicitudes.Add(item);

                OnPropertyChanged(nameof(SinSolicitudes));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinSolicitudes));
                ActualizarCommand.ChangeCanExecute();
                AbrirCommand.ChangeCanExecute();
            }
        }

        private async Task AbrirAsync(
            InspeccionFitosanitariaListaItemV2? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    DiagnosticoIARoutes.ModoAnalizador));
        }
    }
}
