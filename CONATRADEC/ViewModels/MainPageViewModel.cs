using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public class MainPageViewModel : GlobalService
    {
        private readonly GuardarTodoApiService guardarTodoApiService = new();
        private readonly TerrenoApiService terrenoApiService = new();

        private readonly List<AnalisisGuardadoResumen> todosAnalisis = new();

        private bool isRefreshing;
        private string mensaje = string.Empty;
        private string textoBusqueda = string.Empty;
        private bool usarFiltroFecha;
        private DateTime fechaFiltro = DateTime.Today;

        public MainPageViewModel()
        {
            AnalisisGuardados = new ObservableCollection<AnalisisGuardadoResumen>();

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy);

            VisualizarCommand = new Command<AnalisisGuardadoResumen>(
                async analisis => await VisualizarAsync(analisis),
                analisis => !IsBusy && analisis != null);

            EditarCommand = new Command<AnalisisGuardadoResumen>(
                async analisis => await EditarAsync(analisis),
                analisis => !IsBusy && analisis != null);

            EliminarCommand = new Command<AnalisisGuardadoResumen>(
                async analisis => await EliminarAsync(analisis),
                analisis => !IsBusy && analisis != null);

            LimpiarFiltrosCommand = new Command(LimpiarFiltros);

            NuevoAnalisisCommand = new Command(
                async () => await NuevoAnalisisAsync(),
                () => !IsBusy);
        }

        public ObservableCollection<AnalisisGuardadoResumen> AnalisisGuardados { get; }

        public Command ActualizarCommand { get; }
        public Command<AnalisisGuardadoResumen> VisualizarCommand { get; }
        public Command<AnalisisGuardadoResumen> EditarCommand { get; }
        public Command<AnalisisGuardadoResumen> EliminarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command NuevoAnalisisCommand { get; }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;

                ActualizarCommand.ChangeCanExecute();
                VisualizarCommand.ChangeCanExecute();
                EditarCommand.ChangeCanExecute();
                EliminarCommand.ChangeCanExecute();
                NuevoAnalisisCommand.ChangeCanExecute();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public string Mensaje
        {
            get => mensaje;
            set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                textoBusqueda = value ?? string.Empty;
                OnPropertyChanged(nameof(TextoBusqueda));
                AplicarFiltros();
            }
        }

        public bool UsarFiltroFecha
        {
            get => usarFiltroFecha;
            set
            {
                if (usarFiltroFecha == value)
                    return;

                usarFiltroFecha = value;
                OnPropertyChanged(nameof(UsarFiltroFecha));
                AplicarFiltros();
            }
        }

        public DateTime FechaFiltro
        {
            get => fechaFiltro;
            set
            {
                fechaFiltro = value.Date;
                OnPropertyChanged(nameof(FechaFiltro));
                AplicarFiltros();
            }
        }

        public bool TieneAnalisis => AnalisisGuardados.Count > 0;

        public string TotalMostradoTexto =>
            AnalisisGuardados.Count == 1
                ? "1 análisis encontrado"
                : $"{AnalisisGuardados.Count} análisis encontrados";

        public async Task CargarAnalisisAsync(bool mostrarIndicador = true)
        {
            if (IsBusy)
                return;

            try
            {
                if (mostrarIndicador)
                    IsBusy = true;

                Mensaje = string.Empty;

                Task<AnalisisGuardadoListaResponse> tareaAnalisis =
                    guardarTodoApiService.ListarAsync();

                Task<ObservableCollection<TerrenoResponse>> tareaTerrenos =
                    terrenoApiService.GetTerrenosAsync();

                await Task.WhenAll(tareaAnalisis, tareaTerrenos);

                AnalisisGuardadoListaResponse respuestaAnalisis =
                    await tareaAnalisis;

                ObservableCollection<TerrenoResponse> terrenos =
                    await tareaTerrenos;

                if (!respuestaAnalisis.Success)
                {
                    Mensaje = string.IsNullOrWhiteSpace(respuestaAnalisis.Message)
                        ? "No fue posible cargar los análisis."
                        : respuestaAnalisis.Message;

                    await MostrarToastAsync(Mensaje);
                    return;
                }

                Dictionary<int, TerrenoResponse> terrenosPorId = terrenos
                    .Where(x => x.TerrenoId.HasValue && x.TerrenoId.Value > 0)
                    .GroupBy(x => x.TerrenoId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

                todosAnalisis.Clear();

                foreach (AnalisisGuardadoResumen analisis in
                         respuestaAnalisis.Data.OrderByDescending(x => x.FechaCalculoValor))
                {
                    if (terrenosPorId.TryGetValue(analisis.TerrenoId, out TerrenoResponse? terreno))
                    {
                        analisis.NombreCliente = terreno.NombreCliente ?? string.Empty;
                        analisis.CodigoTerreno = terreno.CodigoTerreno ?? string.Empty;
                        analisis.NombreTerreno = terreno.NombreTerreno ?? string.Empty;
                    }

                    todosAnalisis.Add(analisis);
                }

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                Mensaje = $"No fue posible cargar los análisis: {ex.Message}";
                await MostrarToastAsync(Mensaje);
            }
            finally
            {
                if (mostrarIndicador)
                    IsBusy = false;
            }
        }

        private void AplicarFiltros()
        {
            IEnumerable<AnalisisGuardadoResumen> consulta = todosAnalisis;

            string texto = (TextoBusqueda ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.TextoBusqueda.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (UsarFiltroFecha)
            {
                consulta = consulta.Where(x =>
                    x.FechaAnalisisValor?.Date == FechaFiltro.Date);
            }

            List<AnalisisGuardadoResumen> filtrados = consulta
                .OrderByDescending(x => x.FechaCalculoValor)
                .ToList();

            AnalisisGuardados.Clear();

            foreach (AnalisisGuardadoResumen analisis in filtrados)
                AnalisisGuardados.Add(analisis);

            OnPropertyChanged(nameof(TieneAnalisis));
            OnPropertyChanged(nameof(TotalMostradoTexto));
        }

        private void LimpiarFiltros()
        {
            textoBusqueda = string.Empty;
            usarFiltroFecha = false;
            fechaFiltro = DateTime.Today;

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(UsarFiltroFecha));
            OnPropertyChanged(nameof(FechaFiltro));

            AplicarFiltros();
        }

        private async Task ActualizarAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsRefreshing = true;
                await CargarAnalisisAsync(false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task NuevoAnalisisAsync()
        {
            if (IsBusy || !CanAdd)
                return;

            await GoToAsyncParameters("//NuevoAnalisisFormPage");
        }

        private async Task VisualizarAsync(AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || IsBusy || !CanView)
                return;

            await GoToAsyncParameters(
                AppRoutes.AnalisisGuardadoDetalle,
                new Dictionary<string, object>
                {
                    ["analisisSueloCalculoId"] = analisis.AnalisisSueloCalculoId,
                    ["resumenAnalisis"] = analisis
                });
        }

        private async Task EditarAsync(AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar análisis.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.EditarAnalisisGuardado,
                new Dictionary<string, object>
                {
                    ["analisisSueloCalculoId"] = analisis.AnalisisSueloCalculoId,
                    ["resumenAnalisis"] = analisis
                });
        }

        private async Task EliminarAsync(AnalisisGuardadoResumen? analisis)
        {
            if (analisis == null || IsBusy)
                return;

            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar análisis.");
                return;
            }

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Eliminar análisis",
                $"¿Desea eliminar el análisis {analisis.IdentificadorMostrar}? Esta acción también desactivará sus cálculos relacionados.",
                "Sí, eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;

                EliminarAnalisisResponse respuesta =
                    await guardarTodoApiService.EliminarAsync(
                        analisis.AnalisisSueloId);

                if (!respuesta.Success)
                {
                    Mensaje = string.IsNullOrWhiteSpace(respuesta.Message)
                        ? "La API no pudo eliminar el análisis."
                        : respuesta.Message;

                    await Application.Current.MainPage.DisplayAlert(
                        "No se pudo eliminar",
                        Mensaje,
                        "Aceptar");
                    return;
                }

                todosAnalisis.RemoveAll(x =>
                    x.AnalisisSueloId == analisis.AnalisisSueloId);

                AplicarFiltros();

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(respuesta.Message)
                        ? "Análisis eliminado correctamente."
                        : respuesta.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
