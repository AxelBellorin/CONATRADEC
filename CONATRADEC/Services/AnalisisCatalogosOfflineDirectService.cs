using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Entrega directamente desde el motor descargado los catálogos necesarios
    /// para crear o editar un análisis. Evita pasar por varias capas HTTP y
    /// SQLite durante una sesión offline, reduciendo bloqueos y listas vacías.
    /// </summary>
    internal static class AnalisisCatalogosOfflineDirectService
    {
        public static async Task<
            ObservableCollection<TipoCultivoResponse>>
            ObtenerTiposCultivoAsync(
                CancellationToken cancellationToken = default)
        {
            MotorCalculoPaquete? paquete =
                await ObtenerPaqueteAsync(cancellationToken);

            if (paquete == null)
                return new ObservableCollection<TipoCultivoResponse>();

            HashSet<int> cultivosConRangos =
                paquete.Contenido.RangosCultivo
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoCultivoId > 0 &&
                        x.ElementoQuimicosId > 0)
                    .Select(x => x.TipoCultivoId)
                    .ToHashSet();

            List<TipoCultivoResponse> tipos =
                paquete.Contenido.TiposCultivo
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoCultivoId > 0 &&
                        cultivosConRangos.Contains(
                            x.TipoCultivoId))
                    .OrderBy(x => x.NombreTipoCultivo)
                    .Select(x => new TipoCultivoResponse
                    {
                        TipoCultivoId = x.TipoCultivoId,
                        NombreTipoCultivo =
                            x.NombreTipoCultivo?.Trim(),
                        TipoCultivo =
                            x.NombreTipoCultivo?.Trim(),
                        DescripcionTipoCultivo = string.Empty,
                        Activo = true,
                        CantidadRangosActivos =
                            paquete.Contenido.RangosCultivo.Count(rango =>
                                rango.Activo &&
                                rango.TipoCultivoId ==
                                    x.TipoCultivoId),
                        CantidadAnalisis = 0
                    })
                    .ToList();

            return new ObservableCollection<TipoCultivoResponse>(
                tipos);
        }

        public static async Task<
            ObservableCollection<UnidadMedidaResponse>>
            ObtenerUnidadesAsync(
                CancellationToken cancellationToken = default)
        {
            MotorCalculoPaquete? paquete =
                await ObtenerPaqueteAsync(cancellationToken);

            if (paquete == null)
                return new ObservableCollection<UnidadMedidaResponse>();

            HashSet<int> unidadesUsadas =
                paquete.Contenido.ConversionesElementos
                    .Where(x => x != null && x.Activo)
                    .Select(x => x.UnidadMedidaId)
                    .Concat(
                        paquete.Contenido
                            .ConversionesMateriaOrganica
                            .Where(x => x != null && x.Activo)
                            .Select(x => x.UnidadMedidaId))
                    .Where(x => x > 0)
                    .ToHashSet();

            List<UnidadMedidaResponse> unidades =
                paquete.Contenido.Unidades
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.UnidadMedidaId > 0 &&
                        unidadesUsadas.Contains(
                            x.UnidadMedidaId))
                    .OrderBy(x => x.NombreUnidadMedida)
                    .Select(x =>
                    {
                        string nombre =
                            (x.NombreUnidadMedida ?? string.Empty)
                                .Trim();

                        return new UnidadMedidaResponse
                        {
                            UnidadMedidaId = x.UnidadMedidaId,
                            NombreUnidadMedida = nombre,
                            DescripcionUnidadMedida = string.Empty,
                            SimboloUnidadMedida = nombre,
                            AbreviaturaUnidadMedida = nombre,
                            Activo = true
                        };
                    })
                    .ToList();

            return new ObservableCollection<UnidadMedidaResponse>(
                unidades);
        }

        public static async Task<
            ObservableCollection<ElementoQuimicoResponse>>
            ObtenerElementosAsync(
                CancellationToken cancellationToken = default)
        {
            MotorCalculoPaquete? paquete =
                await ObtenerPaqueteAsync(cancellationToken);

            if (paquete == null)
                return new ObservableCollection<ElementoQuimicoResponse>();

            HashSet<int> elementosConUnidad =
                paquete.Contenido.ConversionesElementos
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId > 0 &&
                        x.UnidadMedidaId > 0)
                    .Select(x => x.ElementoQuimicosId)
                    .ToHashSet();

            List<ElementoQuimicoResponse> elementos =
                paquete.Contenido.Elementos
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId > 0 &&
                        elementosConUnidad.Contains(
                            x.ElementoQuimicosId))
                    .OrderBy(x => x.NombreElementoQuimico)
                    .Select(x => new ElementoQuimicoResponse
                    {
                        ElementoQuimicosId =
                            x.ElementoQuimicosId,
                        SimboloElementoQuimico =
                            (x.SimboloElementoQuimico ??
                             string.Empty).Trim(),
                        NombreElementoQuimico =
                            (x.NombreElementoQuimico ??
                             string.Empty).Trim(),
                        PesoEquivalenteElementoQuimico =
                            x.PesoEquivalenteElementoQuimico,
                        Activo = true
                    })
                    .ToList();

            return new ObservableCollection<ElementoQuimicoResponse>(
                elementos);
        }

        private static async Task<MotorCalculoPaquete?>
            ObtenerPaqueteAsync(
                CancellationToken cancellationToken)
        {
            if (!ModoSesionService.EsOffline ||
                !DatosSinConexionPermisos.TienePermiso)
            {
                return null;
            }

            /*
             * Permite que WinUI y Android dibujen el indicador de carga antes
             * de leer o deserializar el archivo local.
             */
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            return await MotorCalculoPaqueteService.Instance
                .ObtenerPaqueteActivoAsync(
                    cancellationToken);
        }
    }
}
