using CONATRADEC.Models;
using CONATRADEC.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.ViewModels
{
    public sealed class NuevoAnalisisFormEdicionViewModel :
        NuevoAnalisisFormViewModel
    {
        private readonly AnalisisSueloApiService
            analisisSueloApiService = new();

        private bool inicializandoEdicion;

        public NuevoAnalisisFormEdicionViewModel()
        {
            EnviarAnalisisCommand = new Command(
                async () => await EnviarSegunModoAsync(),
                () => PuedeEnviar);

            CancelarCommand = new Command(
                async () => await CancelarSegunModoAsync(),
                () => !IsBusy);
        }

        public new Command EnviarAnalisisCommand { get; }

        public new Command CancelarCommand { get; }

        public bool EsModoEdicion =>
            AnalisisEdicionService.Instance.EsModoEdicion;

        public new bool PuedeEnviar =>
            !IsBusy &&
            (EsModoEdicion ? CanEdit : CanAdd);

        public string TextoAccionFormulario =>
            EsModoEdicion
                ? "Continuar actualización"
                : "Enviar Análisis";

        public async Task InicializarPaginaAsync(
            bool forceReload = false)
        {
            await base.InicializarAsync(forceReload);

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService.Instance.ContextoActual;

            if (contexto != null)
                AplicarContexto(contexto);

            NotificarModo();
        }

        private void AplicarContexto(
            AnalisisEdicionContexto contexto)
        {
            inicializandoEdicion = true;

            try
            {
                AnalisisSueloGuardarCalculoRequest origen =
                    contexto.RequestActual;

                TerrenoResponse? terreno =
                    contexto.Terrenos.FirstOrDefault(x =>
                        x.TerrenoId == origen.TerrenoId);

                if (terreno != null)
                {
                    TerrenoSeleccionado = terreno;

                    TerrenosFiltrados.Clear();
                    TerrenosFiltrados.Add(terreno);

                    TextoBusquedaTerreno =
                        $"{terreno.CodigoTerreno} - " +
                        $"{terreno.NombreTerreno}";
                }

                TipoCultivoResponse? cultivo =
                    TiposCultivo.FirstOrDefault(x =>
                        x.TipoCultivoId ==
                            origen.TipoCultivoId);

                if (cultivo == null)
                {
                    cultivo =
                        contexto.TiposCultivo.FirstOrDefault(x =>
                            x.TipoCultivoId ==
                                origen.TipoCultivoId);

                    if (cultivo != null &&
                        !TiposCultivo.Any(x =>
                            x.TipoCultivoId ==
                                cultivo.TipoCultivoId))
                    {
                        TiposCultivo.Add(cultivo);
                    }
                }

                TipoCultivoSeleccionado = cultivo;

                FechaAnalisisLaboratorio =
                    DateTime.TryParse(
                        origen.FechaAnalisisSuelo,
                        out DateTime fecha)
                        ? fecha
                        : DateTime.Today;

                Laboratorio =
                    origen.LaboratorioAnalasisSuelo ??
                    string.Empty;

                IdentificadorAnalisisSuelo =
                    origen.IdentificadorAnalisisSuelo ??
                    string.Empty;

                CantidadQuintalesOro =
                    Formatear(
                        origen.CantidadQuintalesOro);

                TamanoFinca =
                    Formatear(
                        origen.TamanoFinca);

                CantidadPlantas =
                    contexto.CantidadPlantas > 0
                        ? contexto.CantidadPlantas.ToString(
                            CultureInfo.InvariantCulture)
                        : string.Empty;

                /*
                 * Los primeros análisis guardados podían tener pH y AT
                 * en cero dentro del requerimiento anual, aunque la
                 * enmienda sí conservara los valores correctos.
                 *
                 * Por eso se usa la enmienda como respaldo únicamente
                 * cuando el valor principal está vacío o es igual a 0.
                 */
                decimal? phRestaurado =
                    ObtenerValorPrincipalORespaldo(
                        origen.Ph,
                        contexto.Detalle
                            .EnmiendaCalcarea?
                            .Ph);

                decimal? acidezRestaurada =
                    ObtenerValorPrincipalORespaldo(
                        origen.AcidezTotal,
                        contexto.Detalle
                            .EnmiendaCalcarea?
                            .AcidezTotal);

                Ph =
                    Formatear(
                        phRestaurado);

                AcidezTotal =
                    Formatear(
                        acidezRestaurada);

                CalcioCice =
                    FormatearOpcional(
                        origen.CalcioCice);

                MagnesioCice =
                    FormatearOpcional(
                        origen.MagnesioCice);

                PotasioCice =
                    FormatearOpcional(
                        origen.PotasioCice);

                AplicarMateriaOrganica(
                    contexto,
                    origen);

                AplicarElementos(
                    contexto,
                    origen);
            }
            finally
            {
                inicializandoEdicion = false;
            }
        }

        private void AplicarMateriaOrganica(
            AnalisisEdicionContexto contexto,
            AnalisisSueloGuardarCalculoRequest origen)
        {
            ResultadoAnalisisItemViewModel? materia =
                ParametrosConstantesAnalisis.FirstOrDefault(x =>
                    string.Equals(
                        x.CodigoParametro,
                        "MATERIA_ORGANICA",
                        StringComparison.OrdinalIgnoreCase));

            if (materia == null)
                return;

            materia.Valor =
                Formatear(
                    origen.MateriaOrganica);

            materia.UnidadSeleccionada =
                materia.UnidadesMedida.FirstOrDefault(x =>
                    x.UnidadMedidaId ==
                        origen.UnidadMedidaMateriaOrganicaId)
                ??
                contexto.UnidadesMedida.FirstOrDefault(x =>
                    x.UnidadMedidaId ==
                        origen.UnidadMedidaMateriaOrganicaId)
                ??
                materia.UnidadesMedida.FirstOrDefault();
        }

        private void AplicarElementos(
            AnalisisEdicionContexto contexto,
            AnalisisSueloGuardarCalculoRequest origen)
        {
            Dictionary<int, ResultadoAnalisisItemViewModel>
                plantillas =
                    ElementosQuimicosAnalisis
                        .Where(x =>
                            x.ElementoQuimicoId.HasValue)
                        .GroupBy(x =>
                            x.ElementoQuimicoId!.Value)
                        .ToDictionary(
                            x => x.Key,
                            x => x.First());

            ElementosQuimicosAnalisis.Clear();

            foreach (
                ElementoQuimicoAnalisisRequest elemento
                in origen.ElementosQuimicos)
            {
                if (!elemento
                        .ElementoQuimicosId
                        .HasValue)
                {
                    continue;
                }

                int id =
                    elemento
                        .ElementoQuimicosId
                        .Value;

                plantillas.TryGetValue(
                    id,
                    out ResultadoAnalisisItemViewModel?
                        plantilla);

                ElementoQuimicoResponse? catalogo =
                    contexto.ElementosCatalogo.FirstOrDefault(
                        x =>
                            x.ElementoQuimicosId ==
                                id);

                ObservableCollection<UnidadMedidaResponse>
                    unidades =
                        new(
                            contexto.UnidadesMedida);

                ElementosQuimicosAnalisis.Add(
                    new ResultadoAnalisisItemViewModel
                    {
                        ElementoQuimicoId =
                            id,

                        CodigoParametro =
                            plantilla?.CodigoParametro
                            ??
                            catalogo?
                                .SimboloElementoQuimico
                            ??
                            string.Empty,

                        NombreParametro =
                            plantilla?.NombreParametro
                            ??
                            ConstruirNombreElemento(
                                catalogo,
                                id),

                        PlaceholderValor =
                            "Valor reportado",

                        EsConstante =
                            false,

                        EsElementoQuimico =
                            true,

                        PuedeEliminar =
                            true,

                        Valor =
                            Formatear(
                                elemento
                                    .CantidadElemento),

                        UnidadesMedida =
                            unidades,

                        UnidadSeleccionada =
                            unidades.FirstOrDefault(x =>
                                x.UnidadMedidaId ==
                                    elemento.UnidadMedidaId)
                            ??
                            unidades.FirstOrDefault()
                    });
            }
        }

        private async Task EnviarSegunModoAsync()
        {
            if (!EsModoEdicion)
            {
                if (base.EnviarAnalisisCommand
                        .CanExecute(null))
                {
                    base.EnviarAnalisisCommand
                        .Execute(null);
                }

                return;
            }

            if (IsBusy ||
                inicializandoEdicion)
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
            {
                await MostrarAsync(
                    "Edición",
                    "No se encontró el análisis preparado para editar.");

                return;
            }

            if (!ValidarEdicion(
                    out string error))
            {
                await MostrarAsync(
                    "Validación",
                    error);

                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandosEdicion();

                AnalisisSueloGuardarCalculoRequest
                    request =
                        ConstruirRequestActual(
                            contexto);

                int plantas =
                    int.TryParse(
                        CantidadPlantas,
                        out int cantidad)
                        ? cantidad
                        : contexto.CantidadPlantas;

                bool cambioRequerimiento =
                    AnalisisEdicionService
                        .Instance
                        .CambioRequerimiento(
                            request);

                AnalisisSueloCalculoDataResponse
                    resultado;

                if (cambioRequerimiento)
                {
                    AnalisisSueloCalculoResponse?
                        respuesta =
                            await analisisSueloApiService
                                .CalcularAsync(
                                    CrearRequestCalcular(
                                        request));

                    if (respuesta?.Success != true ||
                        respuesta.Data == null)
                    {
                        await MostrarAsync(
                            "No se pudo actualizar",
                            respuesta?.Message
                            ??
                            "No fue posible actualizar el requerimiento anual.");

                        return;
                    }

                    resultado =
                        respuesta.Data;
                }
                else
                {
                    resultado =
                        contexto.ResultadoOriginal;
                }

                AnalisisEdicionService
                    .Instance
                    .GuardarFormularioActual(
                        request,
                        plantas);

                await AnalisisEdicionService
                    .Instance
                    .RestaurarTemporalAsync(
                        resultado,
                        request,
                        plantas,
                        cambioRequerimiento);

                Dictionary<string, object>
                    parametros =
                        new()
                        {
                            ["resultadoCalculo"] =
                                resultado,

                            ["requestGuardarAnalisis"] =
                                request,

                            ["cantidadPlantas"] =
                                plantas,

                            ["esModoEdicion"] =
                                true,

                            ["analisisSueloCalculoIdEdicion"] =
                                contexto
                                    .AnalisisSueloCalculoId,

                            ["calcularBalanceFormula"] =
                                contexto.TieneBalance,

                            ["calcularEnmiendaCalcarea"] =
                                contexto.TieneEnmienda,

                            ["calcularFertilizacionMixta"] =
                                contexto.TieneMixta,

                            ["calculosRestaurados"] =
                                true
                        };

                await GoToAsyncParameters(
                    "//ResultadoAnalisisSueloPage",
                    parametros);
            }
            catch (Exception ex)
            {
                await MostrarAsync(
                    "Error",
                    "No fue posible continuar la edición: " +
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandosEdicion();
            }
        }

        private AnalisisSueloGuardarCalculoRequest
            ConstruirRequestActual(
                AnalisisEdicionContexto contexto)
        {
            ResultadoAnalisisItemViewModel? materia =
                ParametrosConstantesAnalisis.FirstOrDefault(x =>
                    string.Equals(
                        x.CodigoParametro,
                        "MATERIA_ORGANICA",
                        StringComparison.OrdinalIgnoreCase));

            return new AnalisisSueloGuardarCalculoRequest
            {
                TerrenoId =
                    TerrenoSeleccionado?
                        .TerrenoId,

                TipoCultivoId =
                    TipoCultivoSeleccionado?
                        .TipoCultivoId,

                TipoAnalisisSueloId =
                    contexto.RequestOriginal
                        .TipoAnalisisSueloId,

                UsuarioId =
                    contexto.RequestOriginal
                        .UsuarioId
                    ??
                    UsuarioId,

                CantidadQuintalesOro =
                    Decimal(
                        CantidadQuintalesOro),

                TamanoFinca =
                    Decimal(
                        TamanoFinca),

                Ph =
                    Decimal(
                        Ph),

                MateriaOrganica =
                    DecimalOpcional(
                        materia?.Valor),

                UnidadMedidaMateriaOrganicaId =
                    materia?
                        .UnidadSeleccionada?
                        .UnidadMedidaId,

                AcidezTotal =
                    DecimalOpcional(
                        AcidezTotal),

                CalcioCice =
                    DecimalOpcional(
                        CalcioCice),

                MagnesioCice =
                    DecimalOpcional(
                        MagnesioCice),

                PotasioCice =
                    DecimalOpcional(
                        PotasioCice),

                ElementosQuimicos =
                    ElementosQuimicosAnalisis
                        .Select(x =>
                            new ElementoQuimicoAnalisisRequest
                            {
                                ElementoQuimicosId =
                                    x.ElementoQuimicoId,

                                UnidadMedidaId =
                                    x.UnidadSeleccionada?
                                        .UnidadMedidaId,

                                CantidadElemento =
                                    Decimal(
                                        x.Valor)
                            })
                        .ToList(),

                FuentesOrganicas =
                    new List<
                        FuenteOrganicaAnalisisRequest>(),

                FechaAnalisisSuelo =
                    FechaAnalisisLaboratorio.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                LaboratorioAnalasisSuelo =
                    Laboratorio.Trim(),

                IdentificadorAnalisisSuelo =
                    IdentificadorAnalisisSuelo.Trim()
            };
        }

        private static AnalisisSueloCalcularRequest
            CrearRequestCalcular(
                AnalisisSueloGuardarCalculoRequest request)
        {
            return new AnalisisSueloCalcularRequest
            {
                TerrenoId =
                    request.TerrenoId,

                TipoCultivoId =
                    request.TipoCultivoId,

                TipoAnalisisSueloId =
                    request.TipoAnalisisSueloId,

                UsuarioId =
                    request.UsuarioId,

                CantidadQuintalesOro =
                    request.CantidadQuintalesOro,

                TamanoFinca =
                    request.TamanoFinca,

                Ph =
                    request.Ph,

                MateriaOrganica =
                    request.MateriaOrganica,

                UnidadMedidaMateriaOrganicaId =
                    request
                        .UnidadMedidaMateriaOrganicaId,

                AcidezTotal =
                    request.AcidezTotal,

                CalcioCice =
                    request.CalcioCice,

                MagnesioCice =
                    request.MagnesioCice,

                PotasioCice =
                    request.PotasioCice,

                ElementosQuimicos =
                    request.ElementosQuimicos,

                FuentesOrganicas =
                    request.FuentesOrganicas
            };
        }

        private bool ValidarEdicion(
            out string error)
        {
            error = string.Empty;

            if (TerrenoSeleccionado?.TerrenoId
                is null or <= 0)
            {
                error =
                    "Debe seleccionar el cliente y terreno.";
            }
            else if (
                TipoCultivoSeleccionado?.TipoCultivoId
                is null or <= 0)
            {
                error =
                    "Debe seleccionar el tipo de cultivo.";
            }
            else if (
                FechaAnalisisLaboratorio.Date >
                DateTime.Today)
            {
                error =
                    "La fecha del análisis no puede ser futura.";
            }
            else if (
                string.IsNullOrWhiteSpace(
                    Laboratorio))
            {
                error =
                    "Debe ingresar el laboratorio.";
            }
            else if (
                string.IsNullOrWhiteSpace(
                    IdentificadorAnalisisSuelo))
            {
                error =
                    "Debe ingresar el identificador del análisis.";
            }
            else if (
                !Positivo(
                    CantidadQuintalesOro))
            {
                error =
                    "Los quintales oro deben ser mayores que cero.";
            }
            else if (
                !Positivo(
                    TamanoFinca))
            {
                error =
                    "El tamaño de finca debe ser mayor que cero.";
            }
            else if (
                !EnteroPositivo(
                    CantidadPlantas))
            {
                error =
                    "La cantidad de plantas debe ser mayor que cero.";
            }
            else if (
                !Rango(
                    Ph,
                    0,
                    14))
            {
                error =
                    "El pH debe estar entre 0 y 14.";
            }
            else if (
                ElementosQuimicosAnalisis.Count == 0)
            {
                error =
                    "Debe conservar al menos un elemento químico.";
            }
            else if (
                ElementosQuimicosAnalisis.Any(x =>
                    x.ElementoQuimicoId
                        is null or <= 0
                    ||
                    x.UnidadSeleccionada?
                        .UnidadMedidaId
                        is null or <= 0
                    ||
                    !NoNegativo(
                        x.Valor)))
            {
                error =
                    "Revise el valor y la unidad de cada " +
                    "elemento químico.";
            }

            return string.IsNullOrWhiteSpace(
                error);
        }

        private async Task CancelarSegunModoAsync()
        {
            if (!EsModoEdicion)
            {
                if (base.CancelarCommand
                        .CanExecute(null))
                {
                    base.CancelarCommand
                        .Execute(null);
                }

                return;
            }

            AnalisisEdicionService
                .Instance
                .Limpiar();

            await GoToAsyncParameters(
                AppRoutes.Principal);
        }

        private void NotificarModo()
        {
            OnPropertyChanged(
                nameof(EsModoEdicion));

            OnPropertyChanged(
                nameof(PuedeEnviar));

            OnPropertyChanged(
                nameof(TextoAccionFormulario));

            RefrescarComandosEdicion();
        }

        private void RefrescarComandosEdicion()
        {
            EnviarAnalisisCommand
                .ChangeCanExecute();

            CancelarCommand
                .ChangeCanExecute();

            OnPropertyChanged(
                nameof(PuedeEnviar));
        }

        private static string ConstruirNombreElemento(
            ElementoQuimicoResponse? elemento,
            int id)
        {
            if (elemento == null)
                return $"Elemento #{id}";

            string nombre =
                elemento.NombreElementoQuimico
                ??
                $"Elemento #{id}";

            string simbolo =
                elemento.SimboloElementoQuimico
                ??
                string.Empty;

            return string.IsNullOrWhiteSpace(
                    simbolo)
                ? nombre
                : $"{nombre} ({simbolo})";
        }

        private static decimal?
            ObtenerValorPrincipalORespaldo(
                decimal? valorPrincipal,
                decimal? valorRespaldo)
        {
            if (valorPrincipal.HasValue &&
                valorPrincipal.Value > 0)
            {
                return valorPrincipal;
            }

            if (valorRespaldo.HasValue &&
                valorRespaldo.Value > 0)
            {
                return valorRespaldo;
            }

            return
                valorPrincipal
                ??
                valorRespaldo
                ??
                0;
        }

        private static decimal Decimal(
            string? valor) =>
                decimal.TryParse(
                    (valor ?? string.Empty)
                        .Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal resultado)
                    ? resultado
                    : 0;

        private static decimal? DecimalOpcional(
            string? valor) =>
                string.IsNullOrWhiteSpace(
                    valor)
                    ? 0
                    : Decimal(valor);

        private static bool Positivo(
            string? valor) =>
                Decimal(valor) > 0;

        private static bool NoNegativo(
            string? valor) =>
                Decimal(valor) >= 0;

        private static bool EnteroPositivo(
            string? valor) =>
                int.TryParse(
                    valor,
                    out int resultado)
                &&
                resultado > 0;

        private static bool Rango(
            string? valor,
            decimal minimo,
            decimal maximo)
        {
            decimal numero =
                Decimal(valor);

            return
                numero >= minimo &&
                numero <= maximo;
        }

        private static string Formatear(
            decimal? valor) =>
                (valor ?? 0).ToString(
                    "0.####",
                    CultureInfo.InvariantCulture);

        private static string FormatearOpcional(
            decimal? valor) =>
                valor.HasValue &&
                valor.Value != 0
                    ? Formatear(valor)
                    : string.Empty;

        private static async Task MostrarAsync(
            string titulo,
            string mensaje)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage
                    .DisplayAlert(
                        titulo,
                        mensaje,
                        "Aceptar");
            }
        }
    }
}
