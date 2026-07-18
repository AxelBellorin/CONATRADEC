using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CONATRADEC.ViewModels
{
    public sealed class AnalisisGuardadoDetalleViewModel : GlobalService, IQueryAttributable
    {
        private readonly GuardarTodoApiService guardarTodoApiService = new();
        private readonly AnalisisGuardadoCatalogoService catalogoService = new();
        private readonly FertilizacionMixtaApiService fertilizacionMixtaApiService = new();

        private AnalisisGuardadoResumen? resumen;
        private AnalisisGuardadoDetalleData? detalle;
        private FertilizacionMixtaCalculoResponse? fertilizacionMixtaRecalculada;
        private string mensaje = string.Empty;

        public AnalisisGuardadoDetalleViewModel()
        {
            VolverCommand = new Command(async () => await GoToAsyncParameters(AppRoutes.Regresar));
            EditarCommand = new Command(async () => await EditarAsync(), () => !IsBusy && Detalle != null);
        }

        public AnalisisGuardadoResumen? Resumen
        {
            get => resumen;
            private set
            {
                resumen = value;
                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(ClienteMostrar));
                OnPropertyChanged(nameof(TerrenoMostrar));
            }
        }

        public AnalisisGuardadoDetalleData? Detalle
        {
            get => detalle;
            private set
            {
                detalle = value;
                OnPropertyChanged(nameof(Detalle));
                OnPropertyChanged(nameof(TieneDetalle));
                OnPropertyChanged(nameof(TieneBalance));
                OnPropertyChanged(nameof(TieneEnmienda));
                OnPropertyChanged(nameof(TieneFertilizacionMixta));
                EditarCommand.ChangeCanExecute();
            }
        }

        public FertilizacionMixtaCalculoResponse? FertilizacionMixtaRecalculada
        {
            get => fertilizacionMixtaRecalculada;
            private set
            {
                fertilizacionMixtaRecalculada = value;
                OnPropertyChanged(nameof(FertilizacionMixtaRecalculada));
                OnPropertyChanged(nameof(TieneMixtaRecalculada));
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje = value ?? string.Empty;
                OnPropertyChanged(nameof(Mensaje));
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);
        public bool TieneDetalle => Detalle != null;
        public bool TieneBalance => Detalle?.BalanceNutricional != null;
        public bool TieneEnmienda => Detalle?.EnmiendaCalcarea != null;
        public bool TieneFertilizacionMixta => Detalle?.FertilizacionMixta != null;
        public bool TieneMixtaRecalculada => FertilizacionMixtaRecalculada?.Detalles?.Count > 0;

        public string ClienteMostrar => Resumen?.ClienteMostrar ?? "Cliente no disponible";
        public string TerrenoMostrar => Resumen?.TerrenoMostrar ?? "Terreno no disponible";

        public Command VolverCommand { get; }
        public Command EditarCommand { get; }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                EditarCommand.ChangeCanExecute();
            }
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            int id = 0;

            if (query.TryGetValue("analisisSueloCalculoId", out object? valorId))
                int.TryParse(valorId?.ToString(), out id);

            if (query.TryGetValue("resumenAnalisis", out object? valorResumen))
                Resumen = valorResumen as AnalisisGuardadoResumen;

            await CargarAsync(id);
        }

        private async Task CargarAsync(int analisisSueloCalculoId)
        {
            if (analisisSueloCalculoId <= 0 || IsBusy)
            {
                Mensaje = "No se recibió un identificador válido para cargar el análisis.";
                return;
            }

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                Detalle = null;
                FertilizacionMixtaRecalculada = null;

                AnalisisGuardadoDetalleResponse response =
                    await guardarTodoApiService.ObtenerDetalleAsync(
                        analisisSueloCalculoId);

                if (!response.Success || response.Data == null)
                {
                    Mensaje = string.IsNullOrWhiteSpace(response.Message)
                        ? "No fue posible cargar el análisis."
                        : response.Message;
                    return;
                }

                Detalle = response.Data;

                await CompletarNombresAsync(response.Data);
                await RecalcularFertilizacionMixtaAsync(response.Data);

                // Fuerza la actualización de BindableLayout después de completar nombres.
                OnPropertyChanged(nameof(Detalle));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CompletarNombresAsync(AnalisisGuardadoDetalleData data)
        {
            List<CatalogoElementoAnalisis> elementos =
                await catalogoService.ListarElementosAsync();

            List<CatalogoFuenteAnalisis> fuentes =
                await catalogoService.ListarFuentesAsync();

            Dictionary<int, CatalogoElementoAnalisis> elementosPorId = elementos
                .Where(x => x.ElementoQuimicosId.HasValue)
                .GroupBy(x => x.ElementoQuimicosId!.Value)
                .ToDictionary(x => x.Key, x => x.First());

            Dictionary<int, string> fuentesPorId = fuentes
                .Where(x => x.FuenteNutrientesId.HasValue)
                .GroupBy(x => x.FuenteNutrientesId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().NombreNutriente ?? string.Empty);

            foreach (AnalisisGuardadoElementoOriginal item in
                     data.DatosAnalisis.ElementosQuimicos)
            {
                if (!elementosPorId.TryGetValue(item.ElementoQuimicosId, out var elemento))
                    continue;

                item.NombreElemento = elemento.NombreElementoQuimico;
                item.SimboloElemento = elemento.SimboloElementoQuimico;
            }

            foreach (AnalisisGuardadoRequerimientoElemento item in
                     data.RequerimientoAnual.Elementos)
            {
                if (!elementosPorId.TryGetValue(item.ElementoQuimicosId, out var elemento))
                    continue;

                item.NombreElemento = elemento.NombreElementoQuimico;
                item.SimboloElemento = elemento.SimboloElementoQuimico;
            }

            if (data.BalanceNutricional != null)
            {
                foreach (AnalisisGuardadoFormulaDetalle item in
                         data.BalanceNutricional.Detalles)
                {
                    if (fuentesPorId.TryGetValue(item.FuenteNutrientesId, out string? fuente))
                        item.NombreFuente = fuente;

                    if (elementosPorId.TryGetValue(item.ElementoQuimicosId, out var elemento))
                    {
                        item.NombreElemento = string.IsNullOrWhiteSpace(elemento.SimboloElementoQuimico)
                            ? elemento.NombreElementoQuimico
                            : $"{elemento.NombreElementoQuimico} ({elemento.SimboloElementoQuimico})";
                    }
                }
            }

            if (data.EnmiendaCalcarea != null &&
                fuentesPorId.TryGetValue(
                    data.EnmiendaCalcarea.FuenteNutrientesId,
                    out string? nombreEnmienda))
            {
                data.EnmiendaCalcarea.NombreFuente = nombreEnmienda;
            }

            if (data.FertilizacionMixta != null)
            {
                foreach (AnalisisGuardadoMixtaFuente item in
                         data.FertilizacionMixta.Fuentes)
                {
                    if (fuentesPorId.TryGetValue(item.FuenteNutrientesId, out string? fuente))
                        item.NombreFuente = fuente;
                }

                foreach (AnalisisGuardadoMixtaDetalle item in
                         data.FertilizacionMixta.Detalles)
                {
                    if (!elementosPorId.TryGetValue(item.ElementoQuimicosId, out var elemento))
                        continue;

                    item.NombreElemento = string.IsNullOrWhiteSpace(elemento.SimboloElementoQuimico)
                        ? elemento.NombreElementoQuimico
                        : $"{elemento.NombreElementoQuimico} ({elemento.SimboloElementoQuimico})";
                }
            }
        }

        private async Task RecalcularFertilizacionMixtaAsync(
            AnalisisGuardadoDetalleData data)
        {
            AnalisisGuardadoFertilizacionMixta? mixta = data.FertilizacionMixta;

            if (mixta == null ||
                mixta.Fuentes.Count == 0 ||
                mixta.Detalles.Count == 0)
            {
                return;
            }

            FertilizacionMixtaCalcularRequest request = new()
            {
                Observacion = mixta.Mixta.Observacion,
                Elementos = mixta.Detalles
                    .Select(x => new ElementoFertilizacionMixtaRequest
                    {
                        ElementoQuimicosId = x.ElementoQuimicosId,
                        Exportable = x.RequerimientoOriginal
                    })
                    .ToList(),
                Fuentes = mixta.Fuentes
                    .Select(x => new FuenteFertilizacionMixtaRequest
                    {
                        FuenteNutrientesId = x.FuenteNutrientesId,
                        CantidadQq = x.CantidadQq
                    })
                    .ToList()
            };

            FertilizacionMixtaCalculoResponse? resultado =
                await fertilizacionMixtaApiService.CalcularAsync(request);

            if (resultado?.Success == true)
                FertilizacionMixtaRecalculada = resultado;
            else if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Message))
                Mensaje = resultado.Message;
        }

        private async Task EditarAsync()
        {
            if (Detalle == null || Resumen == null || IsBusy)
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
                    ["analisisSueloCalculoId"] =
                        Resumen.AnalisisSueloCalculoId,
                    ["resumenAnalisis"] = Resumen
                });
        }
    }
}
