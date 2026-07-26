using CONATRADEC.Models;
using System.Globalization;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Réplica local del cálculo de requerimiento anual actualmente ejecutado
    /// por AnalisisSueloCalculoService en la API.
    ///
    /// No consulta catálogos ni precios del servidor durante el cálculo. Todas
    /// las reglas se toman de una sola versión del paquete descargado.
    /// </summary>
    public sealed class MotorCalculoLocalService
    {
        public const string FormulaLineal =
            "LINEAL";

        public const string FormulaMeqPesoEquivalente =
            "MEQ_PESO_EQUIVALENTE";

        public const string FormulaNitrogenoMateriaOrganicaLegado =
            "NITROGENO_MO_LEGADO";

        public const string FormulaNitrogenoMateriaOrganicaEstandar =
            "NITROGENO_MO_ESTANDAR";

        private static readonly Lazy<MotorCalculoLocalService> lazy =
            new(() => new MotorCalculoLocalService());

        public static MotorCalculoLocalService Instance =>
            lazy.Value;

        private MotorCalculoLocalService()
        {
        }

        public async Task<AnalisisSueloCalculoResponse>
            CalcularRequerimientoAnualAsync(
                AnalisisSueloCalcularRequest request,
                CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarEntrada(request);

                MotorCalculoPaquete? paquete =
                    await MotorCalculoPaqueteService.Instance
                        .ObtenerPaqueteActivoAsync(
                            cancellationToken);

                if (paquete == null)
                {
                    return Error(
                        "Este dispositivo no tiene un motor de cálculo válido. Descargue todos los datos con conexión.");
                }

                if (!paquete.Modulos.RequerimientoAnual)
                {
                    return Error(
                        "La versión descargada no permite calcular el requerimiento anual.");
                }

                MotorCalculoContenido contenido =
                    paquete.Contenido;

                decimal materiaOrganicaPorcentaje =
                    ConvertirMateriaOrganica(
                        request.MateriaOrganica!.Value,
                        request
                            .UnidadMedidaMateriaOrganicaId!
                            .Value,
                        contenido);

                MotorTipoCultivo? tipoCultivo =
                    contenido.TiposCultivo
                        .FirstOrDefault(item =>
                            item.Activo &&
                            item.TipoCultivoId ==
                                request.TipoCultivoId);

                if (tipoCultivo == null)
                {
                    return Error(
                        "El tipo de cultivo no existe en el paquete descargado.");
                }

                MotorTipoAnalisis? tipoAnalisis =
                    contenido.TiposAnalisis
                        .FirstOrDefault(item =>
                            item.Activo &&
                            item.TipoAnalisisSueloId ==
                                request.TipoAnalisisSueloId);

                if (tipoAnalisis == null)
                {
                    return Error(
                        "El tipo de análisis no existe en el paquete descargado.");
                }

                if (!string.Equals(
                        NormalizarCodigo(
                            tipoAnalisis
                                .NombreTipoAnalisisSuelo),
                        "REQUERIMIENTO_ANUAL",
                        StringComparison.Ordinal))
                {
                    return Error(
                        $"El tipo de análisis {tipoAnalisis.NombreTipoAnalisisSuelo} todavía no está habilitado en el motor local.");
                }

                var data =
                    new AnalisisSueloCalculoDataResponse
                    {
                        TerrenoId =
                            request.TerrenoId,
                        TipoCultivoId =
                            request.TipoCultivoId,
                        TipoCultivo =
                            tipoCultivo.NombreTipoCultivo,
                        TipoAnalisisSueloId =
                            request.TipoAnalisisSueloId,
                        TipoAnalisisSuelo =
                            tipoAnalisis
                                .NombreTipoAnalisisSuelo,
                        CantidadQuintalesOro =
                            request.CantidadQuintalesOro,
                        TamanoFinca =
                            request.TamanoFinca,
                        Ph =
                            request.Ph,
                        AcidezTotal =
                            request.AcidezTotal,
                        RecomendacionGeneral =
                            "Cálculo local de requerimiento anual generado con la versión " +
                            $"{paquete.VersionPaquete} del motor y sus parámetros descargados."
                    };

                foreach (
                    ElementoQuimicoAnalisisRequest entrada
                    in request.ElementosQuimicos)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    int elementoId =
                        entrada.ElementoQuimicosId ??
                        0;

                    int unidadId =
                        entrada.UnidadMedidaId ??
                        0;

                    decimal cantidad =
                        entrada.CantidadElemento ??
                        0;

                    MotorElemento? elemento =
                        contenido.Elementos
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.ElementoQuimicosId ==
                                    elementoId);

                    if (elemento == null)
                    {
                        data.Observaciones.Add(
                            $"No se encontró el elemento químico con ID {elementoId} en el paquete local.");
                        continue;
                    }

                    MotorExtraccion? parametroExtraccion =
                        contenido.ParametrosExtraccion
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.ElementoQuimicosId ==
                                    elementoId);

                    MotorRangoCultivo? rango =
                        contenido.RangosCultivo
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.TipoCultivoId ==
                                    request.TipoCultivoId &&
                                item.ElementoQuimicosId ==
                                    elementoId);

                    decimal cantidadConvertidaLbMz =
                        ConvertirElemento(
                            elemento,
                            unidadId,
                            cantidad,
                            materiaOrganicaPorcentaje,
                            contenido);

                    decimal? extraccionPorQQOro =
                        parametroExtraccion?
                            .CantidadExtraidaPorQQOro;

                    decimal? extraccionPorProduccion =
                        extraccionPorQQOro.HasValue
                            ? Math.Round(
                                request
                                    .CantidadQuintalesOro!
                                    .Value *
                                extraccionPorQQOro.Value,
                                4)
                            : null;

                    decimal? rangoMinimoLbMz =
                        rango == null
                            ? null
                            : ConvertirElemento(
                                elemento,
                                contenido
                                    .UnidadRangoKgHaId,
                                rango.ValorMinimo,
                                materiaOrganicaPorcentaje,
                                contenido);

                    decimal? rangoMaximoLbMz =
                        rango == null
                            ? null
                            : ConvertirElemento(
                                elemento,
                                contenido
                                    .UnidadRangoKgHaId,
                                rango.ValorMaximo,
                                materiaOrganicaPorcentaje,
                                contenido);

                    decimal? requerimiento =
                        rango != null &&
                        extraccionPorProduccion.HasValue
                            ? Math.Round(
                                (rangoMaximoLbMz ?? 0) +
                                extraccionPorProduccion
                                    .Value,
                                4)
                            : null;

                    string clasificacion =
                        ClasificarElemento(
                            cantidadConvertidaLbMz,
                            rangoMinimoLbMz,
                            rangoMaximoLbMz);

                    string simbolo =
                        elemento
                            .SimboloElementoQuimico
                            .Trim();

                    data.Elementos.Add(
                        new ElementoResultadoCalculoResponse
                        {
                            ElementoQuimicosId =
                                elementoId,
                            SimboloElementoQuimico =
                                simbolo,
                            NombreElementoQuimico =
                                elemento
                                    .NombreElementoQuimico
                                    .Trim(),
                            CantidadIngresada =
                                cantidad,
                            CantidadConvertidaLbMz =
                                Math.Round(
                                    cantidadConvertidaLbMz,
                                    4),
                            ExtraccionPorQQOro =
                                extraccionPorQQOro,
                            ExtraccionPorProduccion =
                                extraccionPorProduccion,
                            RangoMinimo =
                                rango?.ValorMinimo,
                            RangoMaximo =
                                rango?.ValorMaximo,
                            RangoMinimoLbMz =
                                rangoMinimoLbMz,
                            RangoMaximoLbMz =
                                rangoMaximoLbMz,
                            RequerimientoCalculado =
                                requerimiento,
                            UnidadBase =
                                rango?.UnidadBase,
                            UnidadMedidaResultadoId =
                                contenido
                                    .UnidadResultadoId,
                            UnidadResultado =
                                contenido
                                    .UnidadResultado,
                            Clasificacion =
                                clasificacion,
                            IncluirEnCalculosComplementarios =
                                !string.Equals(
                                    clasificacion,
                                    "EXCESIVO",
                                    StringComparison
                                        .OrdinalIgnoreCase),
                            Observacion =
                                CrearObservacion(
                                    simbolo,
                                    parametroExtraccion,
                                    rango,
                                    cantidadConvertidaLbMz,
                                    rangoMinimoLbMz,
                                    rangoMaximoLbMz,
                                    requerimiento,
                                    clasificacion)
                        });
                }

                if (data.Elementos.Count == 0)
                {
                    data.Observaciones.Add(
                        "No se calcularon elementos químicos válidos.");
                }

                decimal ph =
                    request.Ph ??
                    0;

                if (ph > 0)
                {
                    data.Observaciones.Add(
                        InterpretarPhCafe(ph));
                }

                data.Observaciones.Add(
                    $"Origen del cálculo: dispositivo. Motor {paquete.VersionPaquete}.");

                return new AnalisisSueloCalculoResponse
                {
                    Success = true,
                    Message =
                        "Cálculo local completado correctamente.",
                    Data = data
                };
            }
            catch (OperationCanceledException)
            {
                return Error(
                    "El cálculo local fue cancelado.");
            }
            catch (Exception ex)
            {
                return Error(
                    ex.Message);
            }
        }

        private static decimal ConvertirMateriaOrganica(
            decimal valor,
            int unidadMedidaId,
            MotorCalculoContenido contenido)
        {
            if (valor <= 0)
            {
                throw new InvalidOperationException(
                    "La materia orgánica debe ser mayor que cero.");
            }

            MotorConversionMateriaOrganica? configuracion =
                contenido
                    .ConversionesMateriaOrganica
                    .FirstOrDefault(item =>
                        item.Activo &&
                        item.UnidadMedidaId ==
                            unidadMedidaId);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "La unidad de materia orgánica no existe en el motor descargado.");
            }

            decimal convertido =
                AplicarFormulaLineal(
                    valor,
                    configuracion.FactorPrincipal,
                    configuracion.FactorSecundario,
                    configuracion.FactorTerciario,
                    configuracion.Divisor,
                    configuracion.Desplazamiento);

            if (convertido <= 0 ||
                convertido > 20)
            {
                throw new InvalidOperationException(
                    "La materia orgánica convertida debe estar entre 0 y 20%.");
            }

            return Math.Round(
                convertido,
                4);
        }

        private static decimal ConvertirElemento(
            MotorElemento elemento,
            int unidadMedidaId,
            decimal valor,
            decimal materiaOrganicaPorcentaje,
            MotorCalculoContenido contenido)
        {
            if (valor < 0)
            {
                throw new InvalidOperationException(
                    "El valor de un elemento no puede ser negativo.");
            }

            MotorConversionElemento? configuracion =
                contenido
                    .ConversionesElementos
                    .FirstOrDefault(item =>
                        item.Activo &&
                        item.ElementoQuimicosId ==
                            elemento.ElementoQuimicosId &&
                        item.UnidadMedidaId ==
                            unidadMedidaId);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    $"La unidad seleccionada no está configurada para el elemento {elemento.SimboloElementoQuimico}.");
            }

            string formula =
                NormalizarCodigo(
                    configuracion
                        .CodigoFormulaConversion);

            decimal resultado =
                formula switch
                {
                    FormulaLineal =>
                        AplicarFormulaLineal(
                            valor,
                            configuracion
                                .FactorPrincipal,
                            configuracion
                                .FactorSecundario,
                            configuracion
                                .FactorTerciario,
                            configuracion.Divisor,
                            configuracion
                                .Desplazamiento),

                    FormulaMeqPesoEquivalente =>
                        AplicarFormulaMeq(
                            valor,
                            elemento
                                .PesoEquivalenteElementoQuimico,
                            configuracion
                                .FactorPrincipal,
                            configuracion
                                .FactorSecundario,
                            configuracion
                                .FactorTerciario,
                            configuracion.Divisor,
                            configuracion
                                .Desplazamiento),

                    FormulaNitrogenoMateriaOrganicaLegado =>
                        AplicarFormulaNitrogenoLegado(
                            valor,
                            materiaOrganicaPorcentaje,
                            configuracion
                                .FactorPrincipal,
                            configuracion
                                .FactorSecundario,
                            configuracion
                                .FactorTerciario,
                            configuracion.Divisor,
                            configuracion
                                .Desplazamiento),

                    FormulaNitrogenoMateriaOrganicaEstandar =>
                        AplicarFormulaNitrogenoEstandar(
                            valor,
                            materiaOrganicaPorcentaje,
                            configuracion
                                .FactorPrincipal,
                            configuracion
                                .FactorSecundario,
                            configuracion
                                .FactorTerciario,
                            configuracion.Divisor,
                            configuracion
                                .Desplazamiento),

                    _ => throw new InvalidOperationException(
                        $"La fórmula '{configuracion.CodigoFormulaConversion}' no está soportada localmente.")
                };

            if (resultado < 0)
            {
                throw new InvalidOperationException(
                    "Una conversión produjo un valor negativo.");
            }

            return Math.Round(
                resultado,
                4);
        }

        private static decimal AplicarFormulaLineal(
            decimal valor,
            decimal factorPrincipal,
            decimal factorSecundario,
            decimal factorTerciario,
            decimal divisor,
            decimal desplazamiento) =>
            (
                valor *
                factorPrincipal *
                factorSecundario *
                factorTerciario
            ) /
            ValidarDivisor(divisor) +
            desplazamiento;

        private static decimal AplicarFormulaMeq(
            decimal valor,
            decimal pesoEquivalente,
            decimal factorPrincipal,
            decimal factorSecundario,
            decimal factorTerciario,
            decimal divisor,
            decimal desplazamiento)
        {
            if (pesoEquivalente <= 0)
            {
                throw new InvalidOperationException(
                    "El elemento no tiene un peso equivalente válido.");
            }

            return
                (
                    valor *
                    pesoEquivalente *
                    factorPrincipal *
                    factorSecundario *
                    factorTerciario
                ) /
                ValidarDivisor(divisor) +
                desplazamiento;
        }

        private static decimal AplicarFormulaNitrogenoLegado(
            decimal nitrogenoPorcentaje,
            decimal materiaOrganicaPorcentaje,
            decimal factorPrincipal,
            decimal factorSecundario,
            decimal factorTerciario,
            decimal divisor,
            decimal desplazamiento)
        {
            ValidarPorcentajesNitrogeno(
                nitrogenoPorcentaje,
                materiaOrganicaPorcentaje);

            return
                (
                    nitrogenoPorcentaje *
                    materiaOrganicaPorcentaje *
                    materiaOrganicaPorcentaje *
                    factorPrincipal *
                    factorSecundario *
                    factorTerciario
                ) /
                ValidarDivisor(divisor) +
                desplazamiento;
        }

        private static decimal AplicarFormulaNitrogenoEstandar(
            decimal nitrogenoPorcentaje,
            decimal materiaOrganicaPorcentaje,
            decimal masaSueloKgHa,
            decimal factorMineralizacion,
            decimal factorKgHaALbMz,
            decimal divisorPorcentajes,
            decimal desplazamiento)
        {
            ValidarPorcentajesNitrogeno(
                nitrogenoPorcentaje,
                materiaOrganicaPorcentaje);

            return
                (
                    nitrogenoPorcentaje *
                    materiaOrganicaPorcentaje *
                    masaSueloKgHa *
                    factorMineralizacion *
                    factorKgHaALbMz
                ) /
                ValidarDivisor(divisorPorcentajes) +
                desplazamiento;
        }

        private static void ValidarPorcentajesNitrogeno(
            decimal nitrogenoPorcentaje,
            decimal materiaOrganicaPorcentaje)
        {
            if (nitrogenoPorcentaje < 0 ||
                nitrogenoPorcentaje > 100)
            {
                throw new InvalidOperationException(
                    "El nitrógeno en porcentaje debe estar entre 0 y 100.");
            }

            if (materiaOrganicaPorcentaje <= 0 ||
                materiaOrganicaPorcentaje > 20)
            {
                throw new InvalidOperationException(
                    "La materia orgánica debe estar entre 0 y 20% para calcular nitrógeno.");
            }
        }

        private static decimal ValidarDivisor(
            decimal divisor)
        {
            if (divisor == 0)
            {
                throw new InvalidOperationException(
                    "El divisor de una conversión no puede ser cero.");
            }

            return divisor;
        }

        private static string ClasificarElemento(
            decimal? cantidadConvertidaLbMz,
            decimal? rangoMinimoLbMz,
            decimal? rangoMaximoLbMz)
        {
            if (!cantidadConvertidaLbMz.HasValue ||
                !rangoMinimoLbMz.HasValue ||
                !rangoMaximoLbMz.HasValue ||
                rangoMinimoLbMz.Value <= 0 ||
                rangoMaximoLbMz.Value <= 0)
            {
                return "SIN_CLASIFICACION";
            }

            decimal valor =
                cantidadConvertidaLbMz.Value;

            decimal minimo =
                rangoMinimoLbMz.Value;

            decimal maximo =
                rangoMaximoLbMz.Value;

            decimal limiteMuyBajo =
                minimo *
                0.50m;

            decimal limiteBajo =
                minimo *
                0.75m;

            decimal limiteAlto =
                maximo *
                1.50m;

            if (valor < limiteMuyBajo)
                return "MUY_BAJO";

            if (valor < limiteBajo)
                return "BAJO";

            if (valor < minimo)
                return "MEDIO_BAJO";

            if (valor <= maximo)
                return "ADECUADO";

            if (valor <= limiteAlto)
                return "ALTO";

            return "EXCESIVO";
        }

        private static string CrearObservacion(
            string simbolo,
            MotorExtraccion? parametroExtraccion,
            MotorRangoCultivo? rango,
            decimal? cantidadConvertidaLbMz,
            decimal? rangoMinimoLbMz,
            decimal? rangoMaximoLbMz,
            decimal? requerimientoCalculado,
            string? clasificacion)
        {
            if (parametroExtraccion == null)
            {
                return
                    $"El elemento {simbolo} no tiene parámetro de extracción por QQ oro configurado.";
            }

            if (rango == null)
            {
                return
                    $"El elemento {simbolo} no tiene rango nutricional configurado para el cultivo.";
            }

            if (!cantidadConvertidaLbMz.HasValue)
            {
                return
                    $"No fue posible convertir el elemento {simbolo} a lb/Mz.";
            }

            if (!requerimientoCalculado.HasValue)
            {
                return
                    $"No fue posible calcular el requerimiento anual para {simbolo}.";
            }

            return
                $"Elemento {simbolo}: clasificación {clasificacion}. " +
                $"Cantidad convertida: {cantidadConvertidaLbMz.Value:0.####} lb/Mz. " +
                $"Rango de referencia: {rangoMinimoLbMz:0.####} - " +
                $"{rangoMaximoLbMz:0.####} lb/Mz. " +
                $"Requerimiento anual calculado: " +
                $"{requerimientoCalculado.Value:0.####} lb/Mz.";
        }

        private static string InterpretarPhCafe(
            decimal ph)
        {
            if (ph < 4.5m)
            {
                return
                    "pH muy ácido. El suelo presenta acidez severa; se recomienda evaluar enmienda calcárea.";
            }

            if (ph < 5.5m)
            {
                return
                    "pH ácido. Puede limitar la disponibilidad de nutrientes; se recomienda evaluar enmienda calcárea.";
            }

            if (ph <= 6.5m)
            {
                return
                    "pH adecuado para café. Se encuentra dentro del rango recomendado para el cultivo.";
            }

            if (ph <= 7.3m)
            {
                return
                    "pH cercano a neutro. Revisar la disponibilidad de nutrientes antes de recomendar fertilización.";
            }

            if (ph <= 8.4m)
            {
                return
                    "pH alcalino. Puede afectar la disponibilidad de micronutrientes.";
            }

            return
                "pH fuertemente alcalino. Se recomienda revisión técnica especializada antes de aplicar fertilización.";
        }

        private static void ValidarEntrada(
            AnalisisSueloCalcularRequest request)
        {
            if (request.TerrenoId is not > 0)
                throw new InvalidOperationException(
                    "Debe seleccionar un terreno válido.");

            if (request.TipoCultivoId is not > 0)
                throw new InvalidOperationException(
                    "Debe seleccionar un tipo de cultivo válido.");

            if (request.TipoAnalisisSueloId is not > 0)
                throw new InvalidOperationException(
                    "Debe seleccionar un tipo de análisis válido.");

            if (request.CantidadQuintalesOro is not > 0)
                throw new InvalidOperationException(
                    "La cantidad de quintales oro debe ser mayor que cero.");

            if (request.TamanoFinca is not > 0)
                throw new InvalidOperationException(
                    "El tamaño de la finca debe ser mayor que cero.");

            if (request.MateriaOrganica is not > 0)
                throw new InvalidOperationException(
                    "La materia orgánica debe ser mayor que cero.");

            if (request.UnidadMedidaMateriaOrganicaId is not > 0)
                throw new InvalidOperationException(
                    "Debe seleccionar la unidad de materia orgánica.");

            decimal ph = request.Ph ?? 0;

            if (ph < 0 ||
                ph > 14)
            {
                throw new InvalidOperationException(
                    "El pH debe estar entre 0 y 14.");
            }

            if (request.ElementosQuimicos == null ||
                request.ElementosQuimicos.Count == 0)
            {
                throw new InvalidOperationException(
                    "Debe ingresar al menos un elemento químico.");
            }
        }

        private static string NormalizarCodigo(
            string? valor) =>
            (valor ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

        private static AnalisisSueloCalculoResponse Error(
            string mensaje) =>
            new()
            {
                Success = false,
                Message = mensaje
            };
    }
}
