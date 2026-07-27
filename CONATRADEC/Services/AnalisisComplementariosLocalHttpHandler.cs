using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Atiende localmente los catálogos técnicos y los tres cálculos
    /// complementarios del análisis.
    ///
    /// Los formatos JSON son los mismos que utilizan actualmente los servicios
    /// EnmiendaCalcareaApiService, BalanceNutricionalApiService y
    /// FertilizacionMixtaApiService.
    /// </summary>
    public sealed class AnalisisComplementariosLocalHttpHandler :
        DelegatingHandler
    {
        private const string RutaEnmiendas =
            "/api/fuente-nutriente/enmiendas-calcareas";

        private const string RutaFuentesMixtas =
            "/api/fuente-nutriente/listar-fertilizacion-mixta";

        private const string RutaFuentesGeneral =
            "/api/fuente-nutriente/listar";

        private const string RutaCalcularEnmienda =
            "/api/enmiendas-calcareas/calcular";

        private const string RutaCalcularBalance =
            "/api/formula-nutricional/calcular";

        private const string RutaCalcularMixta =
            "/api/fertilizacion-mixta/calcular";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = ObtenerPath(request);

            bool esRutaLocal =
                EsRutaCatalogo(path) ||
                EsRutaCalculo(path);

            if (!esRutaLocal ||
                !DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            byte[] contenido =
                request.Content == null
                    ? Array.Empty<byte>()
                    : await request.Content.ReadAsByteArrayAsync(
                        cancellationToken);

            RestaurarContenido(
                request,
                contenido);

            if (DebeTrabajarLocal())
            {
                return await CrearRespuestaLocalAsync(
                    request,
                    path,
                    contenido,
                    cancellationToken);
            }

            try
            {
                HttpResponseMessage response =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                if (!EsFalloInfraestructura(
                        response.StatusCode))
                {
                    return response;
                }

                MotorCalculoPaquete? paquete =
                    await MotorCalculoPaqueteService.Instance
                        .ObtenerPaqueteActivoAsync(
                            cancellationToken);

                if (paquete == null)
                    return response;

                response.Dispose();

                await ModoTrabajoAnalisisService.Instance
                    .CambiarAOfflinePorCaidaAsync(
                        cancellationToken);

                return await CrearRespuestaLocalAsync(
                    request,
                    path,
                    contenido,
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                return await CrearFallbackAsync(
                    request,
                    path,
                    contenido,
                    cancellationToken);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return await CrearFallbackAsync(
                    request,
                    path,
                    contenido,
                    cancellationToken);
            }
            catch (IOException)
            {
                return await CrearFallbackAsync(
                    request,
                    path,
                    contenido,
                    cancellationToken);
            }
        }

        private static bool DebeTrabajarLocal() =>
            !EstadoConexionService.Instance.HayInternet ||
            ModoTrabajoAnalisisService
                .Instance
                .EstadoActual
                .Modo ==
                ModoTrabajoAnalisis.SinConexion;

        private static async Task<HttpResponseMessage>
            CrearFallbackAsync(
                HttpRequestMessage request,
                string path,
                byte[] contenido,
                CancellationToken cancellationToken)
        {
            MotorCalculoPaquete? paquete =
                await MotorCalculoPaqueteService.Instance
                    .ObtenerPaqueteActivoAsync(
                        cancellationToken);

            if (paquete == null)
                throw new HttpRequestException(
                    "La API no responde y no existe un motor local válido.");

            EstadoConexionService.Instance
                .ReportarServidorNoDisponible();

            await ModoTrabajoAnalisisService.Instance
                .CambiarAOfflinePorCaidaAsync(
                    cancellationToken);

            return await CrearRespuestaLocalAsync(
                request,
                path,
                contenido,
                cancellationToken);
        }

        private static async Task<HttpResponseMessage>
            CrearRespuestaLocalAsync(
                HttpRequestMessage request,
                string path,
                byte[] contenido,
                CancellationToken cancellationToken)
        {
            MotorCalculoPaquete? paquete =
                await MotorCalculoPaqueteService.Instance
                    .ObtenerPaqueteActivoAsync(
                        cancellationToken);

            if (paquete == null)
            {
                return CrearError(
                    request,
                    HttpStatusCode.ServiceUnavailable,
                    "Este dispositivo no tiene un motor de cálculo válido.");
            }

            object resultado;

            try
            {
                resultado =
                    path.ToLowerInvariant() switch
                    {
                        RutaEnmiendas =>
                            CrearCatalogoEnmiendas(paquete),

                        RutaFuentesMixtas =>
                            CrearCatalogoFuentes(
                                paquete,
                                solamenteMixtas: true),

                        RutaFuentesGeneral =>
                            CrearCatalogoFuentes(
                                paquete,
                                solamenteMixtas: false),

                        RutaCalcularEnmienda =>
                            CalcularEnmienda(
                                paquete,
                                contenido),

                        RutaCalcularBalance =>
                            CalcularBalance(
                                paquete,
                                contenido),

                        RutaCalcularMixta =>
                            CalcularMixta(
                                paquete,
                                contenido),

                        _ =>
                            throw new InvalidOperationException(
                                "La ruta no está habilitada en el motor local.")
                    };
            }
            catch (Exception ex)
            {
                return CrearError(
                    request,
                    HttpStatusCode.BadRequest,
                    ex.Message);
            }

            return CrearJson(
                request,
                resultado);
        }

        private static object CrearCatalogoEnmiendas(
            MotorCalculoPaquete paquete)
        {
            return paquete.Contenido
                .ParametrosEnmiendaCalcarea
                .Where(item => item.Activo)
                .Join(
                    paquete.Contenido.FuentesNutrientes
                        .Where(item => item.Activo),
                    parametro =>
                        parametro.FuenteNutrientesId,
                    fuente =>
                        fuente.FuenteNutrientesId,
                    (parametro, fuente) => new
                    {
                        parametroEnmiendaCalcareaId =
                            parametro.ParametroEnmiendaCalcareaId,
                        fuenteNutrientesId =
                            fuente.FuenteNutrientesId,
                        nombreNutriente =
                            fuente.NombreNutriente,
                        precioNutriente =
                            fuente.PrecioNutriente,
                        prnt =
                            parametro.Prnt,
                        descripcionParametro =
                            parametro.DescripcionParametro
                    })
                .OrderBy(item =>
                    item.nombreNutriente)
                .ToList();
        }

        private static object CrearCatalogoFuentes(
            MotorCalculoPaquete paquete,
            bool solamenteMixtas)
        {
            IEnumerable<MotorFuenteNutriente> fuentes =
                paquete.Contenido.FuentesNutrientes
                    .Where(item => item.Activo);

            if (solamenteMixtas)
            {
                fuentes = fuentes.Where(item =>
                    item.HabilitadaFertilizacionMixta &&
                    paquete.Contenido
                        .FuentesFertilizacionMixtaIds
                        .Contains(item.FuenteNutrientesId));
            }

            return fuentes
                .Select(fuente => new
                {
                    fuenteNutrientesId =
                        fuente.FuenteNutrientesId,
                    nombreNutriente =
                        fuente.NombreNutriente,
                    descripcionNutriente =
                        fuente.DescripcionNutriente,
                    precioNutriente =
                        fuente.PrecioNutriente,
                    activo =
                        fuente.Activo,
                    habilitadaEnmiendaCalcarea =
                        fuente.HabilitadaEnmiendaCalcarea,
                    habilitadaFertilizacionMixta =
                        fuente.HabilitadaFertilizacionMixta,
                    prnt =
                        paquete.Contenido
                            .ParametrosEnmiendaCalcarea
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.FuenteNutrientesId ==
                                    fuente.FuenteNutrientesId)?
                            .Prnt,
                    descripcionParametro =
                        paquete.Contenido
                            .ParametrosEnmiendaCalcarea
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.FuenteNutrientesId ==
                                    fuente.FuenteNutrientesId)?
                            .DescripcionParametro,
                    elementosQuimicos =
                        paquete.Contenido.AportesFuentes
                            .Where(aporte =>
                                aporte.Activo &&
                                aporte.FuenteNutrientesId ==
                                    fuente.FuenteNutrientesId)
                            .Join(
                                paquete.Contenido.Elementos
                                    .Where(item => item.Activo),
                                aporte =>
                                    aporte.ElementoQuimicosId,
                                elemento =>
                                    elemento.ElementoQuimicosId,
                                (aporte, elemento) => new
                                {
                                    fuenteNutrienteElementoQuimicoId =
                                        aporte
                                            .FuenteNutrienteElementoQuimicoId,
                                    elementoQuimicosId =
                                        elemento.ElementoQuimicosId,
                                    nombreElementoQuimico =
                                        elemento.NombreElementoQuimico,
                                    simboloElementoQuimico =
                                        elemento.SimboloElementoQuimico,
                                    cantidadAporte =
                                        aporte.CantidadAporte
                                })
                            .OrderBy(item =>
                                OrdenElemento(
                                    item.simboloElementoQuimico))
                            .ToList()
                })
                .OrderBy(item =>
                    item.nombreNutriente)
                .ToList();
        }

        private static object CalcularEnmienda(
            MotorCalculoPaquete paquete,
            byte[] contenido)
        {
            using JsonDocument documento =
                Parsear(contenido);

            JsonElement root =
                documento.RootElement;

            int fuenteId =
                Entero(root, "fuenteNutrientesId");

            MotorFuenteNutriente fuente =
                paquete.Contenido.FuentesNutrientes
                    .FirstOrDefault(item =>
                        item.Activo &&
                        item.FuenteNutrientesId ==
                            fuenteId)
                ?? throw new InvalidOperationException(
                    "La fuente de enmienda no existe en el paquete descargado.");

            MotorParametroEnmienda parametro =
                paquete.Contenido.ParametrosEnmiendaCalcarea
                    .FirstOrDefault(item =>
                        item.Activo &&
                        item.FuenteNutrientesId ==
                            fuenteId)
                ?? throw new InvalidOperationException(
                    "La fuente seleccionada no tiene parámetros de enmienda descargados.");

            decimal ph = Decimal(root, "ph");
            decimal ca = Decimal(root, "ca");
            decimal mg = Decimal(root, "mg");
            decimal k = Decimal(root, "k");
            decimal acidez =
                Decimal(root, "acidezTotal");

            int terrenoId =
                Entero(root, "terrenoId");

            int plantas =
                Entero(root, "totalPlantas");

            int aplicaciones =
                Entero(root, "totalAplicaciones");

            if (plantas <= 0)
            {
                throw new InvalidOperationException(
                    "El total de plantas debe ser mayor a cero.");
            }

            if (aplicaciones < 1 ||
                aplicaciones > 4)
            {
                throw new InvalidOperationException(
                    "El total de aplicaciones debe estar entre 1 y 4.");
            }

            decimal sumaBases =
                ca + mg + k;

            decimal cice =
                sumaBases + acidez;

            if (cice <= 0)
            {
                throw new InvalidOperationException(
                    "La CICE calculada debe ser mayor a cero.");
            }

            if (parametro.Prnt <= 0)
            {
                throw new InvalidOperationException(
                    "El PRNT configurado debe ser mayor a cero.");
            }

            decimal saturacionActual =
                sumaBases / cice * 100m;

            decimal diferencia =
                Math.Max(
                    parametro.SaturacionBasesDeseada -
                    saturacionActual,
                    0m);

            decimal necesidadTonHa =
                diferencia *
                cice /
                parametro.Prnt;

            decimal necesidadKgHa =
                necesidadTonHa *
                parametro.FactorTonHaAKgHa;

            decimal necesidadLbHa =
                necesidadTonHa *
                parametro.FactorTonHaALbHa;

            decimal necesidadLbMz =
                necesidadLbHa *
                parametro.FactorHaAMz;

            decimal necesidadOzMz =
                necesidadLbMz *
                16m;

            decimal dosisPlantaAnual =
                necesidadOzMz /
                plantas;

            decimal dosisPorAplicacion =
                dosisPlantaAnual /
                aplicaciones;

            return new
            {
                enmiendaCalcareaId =
                    (int?)null,
                nombreAnalisis =
                    Texto(root, "nombreAnalisis"),
                fuenteNutriente =
                    fuente.NombreNutriente,
                ph = R(ph),
                ca = R(ca),
                mg = R(mg),
                k = R(k),
                acidezTotal = R(acidez),
                saturacionDeseada =
                    R(parametro.SaturacionBasesDeseada),
                prnt =
                    R(parametro.Prnt),
                sumaBases =
                    R(sumaBases),
                cice =
                    R(cice),
                saturacionActual =
                    R(saturacionActual),
                necesidadEncaladoTonHa =
                    R(necesidadTonHa),
                necesidadEncaladoKgHa =
                    R(necesidadKgHa),
                necesidadEncaladoLbHa =
                    R(necesidadLbHa),
                terrenoId,
                totalPlantas =
                    plantas,
                totalAplicaciones =
                    aplicaciones,
                necesidadEncaladoLbMz =
                    R(necesidadLbMz),
                necesidadEncaladoOzMz =
                    R(necesidadOzMz),
                dosisPlantaAnualOz =
                    R(dosisPlantaAnual),
                dosisPlantaPorAplicacionOz =
                    R(dosisPorAplicacion)
            };
        }

        private static object CalcularBalance(
            MotorCalculoPaquete paquete,
            byte[] contenido)
        {
            using JsonDocument documento =
                Parsear(contenido);

            JsonElement root =
                documento.RootElement;

            int terrenoId =
                Entero(root, "terrenoId");

            int plantas =
                Entero(root, "totalPlantas");

            int aplicaciones =
                Entero(root, "totalAplicaciones");

            if (plantas <= 0)
            {
                throw new InvalidOperationException(
                    "El total de plantas debe ser mayor a cero.");
            }

            if (aplicaciones < 1 ||
                aplicaciones > 4)
            {
                throw new InvalidOperationException(
                    "El total de aplicaciones debe estar entre 1 y 4.");
            }

            JsonElement items =
                Propiedad(root, "items");

            if (items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Debe seleccionar al menos una fuente de nutriente.");
            }

            var entradas =
                new List<BalanceEntrada>();

            foreach (JsonElement item in items.EnumerateArray())
            {
                int fuenteId =
                    Entero(item, "fuenteNutrientesId");

                int elementoId =
                    Entero(item, "elementoQuimicosId");

                decimal libras =
                    DecimalFlexible(
                        item,
                        "libras",
                        "requerimientoLibras");

                if (fuenteId <= 0 ||
                    elementoId <= 0 ||
                    libras <= 0)
                {
                    throw new InvalidOperationException(
                        "Una fuente, elemento o requerimiento del balance no es válido.");
                }

                entradas.Add(
                    new BalanceEntrada
                    {
                        FuenteId = fuenteId,
                        ElementoId = elementoId,
                        Libras = libras
                    });
            }

            decimal totalLibras =
                entradas.Sum(item => item.Libras);

            decimal mezclaTotalQq =
                totalLibras / 100m;

            var totales =
                new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase);

            var detalles =
                new List<object>();

            foreach (BalanceEntrada entrada in entradas
                         .OrderByDescending(item => item.Libras))
            {
                MotorFuenteNutriente fuente =
                    paquete.Contenido.FuentesNutrientes
                        .FirstOrDefault(item =>
                            item.Activo &&
                            item.FuenteNutrientesId ==
                                entrada.FuenteId)
                    ?? throw new InvalidOperationException(
                        $"La fuente con ID {entrada.FuenteId} no existe en el paquete.");

                MotorElemento elementoBase =
                    paquete.Contenido.Elementos
                        .FirstOrDefault(item =>
                            item.Activo &&
                            item.ElementoQuimicosId ==
                                entrada.ElementoId)
                    ?? throw new InvalidOperationException(
                        $"El elemento con ID {entrada.ElementoId} no existe en el paquete.");

                List<MotorFuenteAporte> composicion =
                    paquete.Contenido.AportesFuentes
                        .Where(item =>
                            item.Activo &&
                            item.FuenteNutrientesId ==
                                entrada.FuenteId)
                        .ToList();

                if (!composicion.Any(item =>
                        item.ElementoQuimicosId ==
                            entrada.ElementoId))
                {
                    throw new InvalidOperationException(
                        $"La fuente {fuente.NombreNutriente} no aporta el elemento {elementoBase.SimboloElementoQuimico}.");
                }

                decimal qq =
                    entrada.Libras / 100m;

                decimal onzasAnuales =
                    entrada.Libras * 16m;

                decimal librasPorAplicacion =
                    entrada.Libras / aplicaciones;

                decimal onzasPorAplicacion =
                    onzasAnuales / aplicaciones;

                decimal subtotal =
                    qq * fuente.PrecioNutriente;

                var aportes =
                    new Dictionary<string, decimal>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (MotorFuenteAporte aporte in composicion)
                {
                    MotorElemento? elemento =
                        paquete.Contenido.Elementos
                            .FirstOrDefault(item =>
                                item.Activo &&
                                item.ElementoQuimicosId ==
                                    aporte.ElementoQuimicosId);

                    if (elemento == null)
                        continue;

                    string simbolo =
                        elemento.SimboloElementoQuimico
                            .Trim()
                            .ToLowerInvariant();

                    decimal valor =
                        qq * aporte.CantidadAporte;

                    if (valor <= 0)
                        continue;

                    aportes[simbolo] =
                        R(valor);

                    if (!totales.ContainsKey(simbolo))
                        totales[simbolo] = 0;

                    totales[simbolo] += valor;
                }

                detalles.Add(new
                {
                    fuente =
                        fuente.NombreNutriente,
                    elemento =
                        elementoBase.SimboloElementoQuimico,
                    lb =
                        R(entrada.Libras),
                    qq =
                        R(qq),
                    aportes =
                        aportes
                            .OrderBy(item =>
                                OrdenElemento(item.Key))
                            .ToDictionary(
                                item => item.Key,
                                item => item.Value),
                    requerimientoLibras =
                        R(entrada.Libras),
                    librasPorAplicacion =
                        R(librasPorAplicacion),
                    onzasAnuales =
                        R(onzasAnuales),
                    onzasPorAplicacion =
                        R(onzasPorAplicacion),
                    precioPorQuintal =
                        R(fuente.PrecioNutriente),
                    subtotalFuente =
                        R(subtotal)
                });
            }

            decimal totalOnzas =
                totalLibras * 16m;

            decimal precioExacto =
                entradas.Sum(entrada =>
                {
                    MotorFuenteNutriente fuente =
                        paquete.Contenido.FuentesNutrientes
                            .First(item =>
                                item.FuenteNutrientesId ==
                                    entrada.FuenteId);

                    return
                        entrada.Libras /
                        100m *
                        fuente.PrecioNutriente;
                });

            decimal costoCompra =
                entradas.Sum(entrada =>
                {
                    MotorFuenteNutriente fuente =
                        paquete.Contenido.FuentesNutrientes
                            .First(item =>
                                item.FuenteNutrientesId ==
                                    entrada.FuenteId);

                    decimal qq =
                        entrada.Libras / 100m;

                    return
                        Math.Ceiling(qq) *
                        fuente.PrecioNutriente;
                });

            decimal dosisAnual =
                totalOnzas / plantas;

            decimal dosisAplicacion =
                dosisAnual / aplicaciones;

            /*
             * PrecioTotalFormula usa el costo real de compra en quintales
             * enteros. Los detalles mantienen subtotalFuente exacto como
             * referencia, igual que la interfaz actual.
             */
            return new
            {
                formulaNutricionalId =
                    (int?)null,
                nombreFormula =
                    Texto(root, "nombreFormula"),
                totalLibras =
                    R(totalLibras),
                mezclaTotalQq =
                    R(mezclaTotalQq),
                formulaComercial =
                    totales
                        .Where(item => item.Value > 0)
                        .Select(item => new
                        {
                            item.Key,
                            Valor =
                                R(item.Value /
                                  mezclaTotalQq)
                        })
                        .OrderBy(item =>
                            OrdenElemento(item.Key))
                        .ToDictionary(
                            item => item.Key,
                            item => item.Valor),
                totalPlantas =
                    plantas,
                totalAplicaciones =
                    aplicaciones,
                totalOnzas =
                    R(totalOnzas),
                /*
                 * La API actual devuelve aquí el subtotal exacto. El ViewModel
                 * conserva ese valor como referencia y después sustituye el
                 * precio oficial por el costo real de quintales enteros.
                 */
                precioTotalFormula =
                    R(precioExacto),
                precioExactoFormulaReferencia =
                    R(precioExacto),
                precioPorAplicacion =
                    R(precioExacto / aplicaciones),
                dosisPlantaAnualOz =
                    R(dosisAnual),
                dosisPlantaPorAplicacionOz =
                    R(dosisAplicacion),
                detalle =
                    detalles
            };
        }

        private static object CalcularMixta(
            MotorCalculoPaquete paquete,
            byte[] contenido)
        {
            using JsonDocument documento =
                Parsear(contenido);

            JsonElement root =
                documento.RootElement;

            JsonElement elementosJson =
                Propiedad(root, "elementos");

            JsonElement fuentesJson =
                Propiedad(root, "fuentes");

            if (elementosJson.ValueKind !=
                    JsonValueKind.Array ||
                elementosJson.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Debe enviar al menos un elemento calculado.");
            }

            if (fuentesJson.ValueKind !=
                    JsonValueKind.Array ||
                fuentesJson.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Debe agregar al menos una fuente orgánica.");
            }

            var fuentesEntrada =
                new List<MixtaFuenteEntrada>();

            foreach (JsonElement item in fuentesJson.EnumerateArray())
            {
                int fuenteId =
                    Entero(item, "fuenteNutrientesId");

                decimal cantidadQq =
                    Decimal(item, "cantidadQq");

                if (fuenteId <= 0 ||
                    cantidadQq <= 0)
                {
                    throw new InvalidOperationException(
                        "Una fuente o cantidad de fertilización mixta no es válida.");
                }

                if (!paquete.Contenido
                        .FuentesFertilizacionMixtaIds
                        .Contains(fuenteId))
                {
                    throw new InvalidOperationException(
                        $"La fuente con ID {fuenteId} no está habilitada para fertilización mixta.");
                }

                fuentesEntrada.Add(
                    new MixtaFuenteEntrada
                    {
                        FuenteId = fuenteId,
                        CantidadQq = cantidadQq
                    });
            }

            var fuentesRespuesta =
                fuentesEntrada
                    .Select(item =>
                    {
                        MotorFuenteNutriente fuente =
                            paquete.Contenido.FuentesNutrientes
                                .FirstOrDefault(x =>
                                    x.Activo &&
                                    x.FuenteNutrientesId ==
                                        item.FuenteId)
                            ?? throw new InvalidOperationException(
                                $"La fuente con ID {item.FuenteId} no existe en el paquete.");

                        return new
                        {
                            fuenteNutrientesId =
                                item.FuenteId,
                            nombreFuente =
                                fuente.NombreNutriente,
                            cantidadQq =
                                R(item.CantidadQq)
                        };
                    })
                    .ToList();

            var detalles =
                new List<object>();

            foreach (JsonElement item
                     in elementosJson.EnumerateArray())
            {
                int elementoId =
                    Entero(item, "elementoQuimicosId");

                decimal exportable =
                    Decimal(item, "exportable");

                if (elementoId <= 0 ||
                    exportable < 0)
                {
                    throw new InvalidOperationException(
                        "Uno de los elementos de fertilización mixta no es válido.");
                }

                MotorElemento elemento =
                    paquete.Contenido.Elementos
                        .FirstOrDefault(x =>
                            x.Activo &&
                            x.ElementoQuimicosId ==
                                elementoId)
                    ?? throw new InvalidOperationException(
                        $"El elemento con ID {elementoId} no existe en el paquete.");

                decimal aporteOrganico = 0;

                var fuentesDetalle =
                    new List<object>();

                foreach (MixtaFuenteEntrada fuenteItem
                         in fuentesEntrada)
                {
                    MotorFuenteNutriente fuente =
                        paquete.Contenido.FuentesNutrientes
                            .First(x =>
                                x.FuenteNutrientesId ==
                                    fuenteItem.FuenteId);

                    decimal aporteUnidad =
                        paquete.Contenido.AportesFuentes
                            .FirstOrDefault(x =>
                                x.Activo &&
                                x.FuenteNutrientesId ==
                                    fuenteItem.FuenteId &&
                                x.ElementoQuimicosId ==
                                    elementoId)?
                            .CantidadAporte ??
                        0m;

                    decimal aporteTotal =
                        fuenteItem.CantidadQq *
                        aporteUnidad;

                    aporteOrganico += aporteTotal;

                    fuentesDetalle.Add(new
                    {
                        fuenteNutrientesId =
                            fuenteItem.FuenteId,
                        nombreFuente =
                            fuente.NombreNutriente,
                        cantidadQq =
                            R(fuenteItem.CantidadQq),
                        aportePorUnidad =
                            R(aporteUnidad),
                        aporteTotal =
                            R(aporteTotal)
                    });
                }

                decimal diferencia =
                    exportable - aporteOrganico;

                decimal deficit =
                    diferencia > 0
                        ? diferencia
                        : 0;

                decimal sobrante =
                    diferencia < 0
                        ? Math.Abs(diferencia)
                        : 0;

                detalles.Add(new
                {
                    elementoQuimicosId =
                        elementoId,
                    elemento =
                        elemento.SimboloElementoQuimico.Trim(),
                    exportable =
                        R(exportable),
                    aporteOrganico =
                        R(aporteOrganico),
                    diferencia =
                        R(diferencia),
                    deficit =
                        R(deficit),
                    sobrante =
                        R(sobrante),
                    fuentes =
                        fuentesDetalle
                });
            }

            return new
            {
                observacion =
                    Texto(root, "observacion"),
                fuentes =
                    fuentesRespuesta,
                detalles
            };
        }

        private static JsonDocument Parsear(
            byte[] contenido)
        {
            if (contenido.Length == 0)
            {
                throw new InvalidOperationException(
                    "No se recibieron datos para calcular.");
            }

            return JsonDocument.Parse(contenido);
        }

        private static JsonElement Propiedad(
            JsonElement element,
            string nombre)
        {
            foreach (JsonProperty property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return default;
        }

        private static decimal Decimal(
            JsonElement element,
            string nombre)
        {
            JsonElement value =
                Propiedad(element, nombre);

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out decimal numero))
            {
                return numero;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    value.GetString()?
                        .Replace(",", "."),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out numero))
            {
                return numero;
            }

            return 0;
        }

        private static decimal DecimalFlexible(
            JsonElement element,
            params string[] nombres)
        {
            foreach (string nombre in nombres)
            {
                decimal value =
                    Decimal(element, nombre);

                if (value != 0)
                    return value;
            }

            return 0;
        }

        private static int Entero(
            JsonElement element,
            string nombre)
        {
            JsonElement value =
                Propiedad(element, nombre);

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int numero))
            {
                return numero;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(
                    value.GetString(),
                    out numero))
            {
                return numero;
            }

            return 0;
        }

        private static string Texto(
            JsonElement element,
            string nombre)
        {
            JsonElement value =
                Propiedad(element, nombre);

            return value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim() ??
                  string.Empty
                : string.Empty;
        }

        private static decimal R(
            decimal value) =>
            Math.Round(
                value,
                4,
                MidpointRounding.AwayFromZero);

        private static int OrdenElemento(
            string? simbolo) =>
            (simbolo ?? string.Empty)
                .Trim()
                .ToUpperInvariant() switch
            {
                "N" => 1,
                "K" => 2,
                "MG" => 3,
                "CA" => 4,
                "P" => 5,
                _ => 99
            };

        private static bool EsRutaCatalogo(
            string path) =>
            string.Equals(
                path,
                RutaEnmiendas,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                path,
                RutaFuentesMixtas,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                path,
                RutaFuentesGeneral,
                StringComparison.OrdinalIgnoreCase);

        private static bool EsRutaCalculo(
            string path) =>
            string.Equals(
                path,
                RutaCalcularEnmienda,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                path,
                RutaCalcularBalance,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                path,
                RutaCalcularMixta,
                StringComparison.OrdinalIgnoreCase);

        private static bool EsFalloInfraestructura(
            HttpStatusCode statusCode) =>
            statusCode is
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri =
                request.RequestUri;

            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath;

            string raw =
                uri.OriginalString;

            int query =
                raw.IndexOf('?');

            if (query >= 0)
                raw = raw[..query];

            return "/" +
                raw.TrimStart('/');
        }

        private static void RestaurarContenido(
            HttpRequestMessage request,
            byte[] contenido)
        {
            if (request.Content == null)
                return;

            var restored =
                new ByteArrayContent(contenido);

            foreach (var header
                     in request.Content.Headers)
            {
                restored.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            request.Content = restored;
        }

        private static HttpResponseMessage CrearJson(
            HttpRequestMessage request,
            object data)
        {
            string json =
                JsonSerializer.Serialize(
                    data,
                    JsonOptions);

            var response =
                new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Calculo-Origen",
                "LOCAL");

            return response;
        }

        private static HttpResponseMessage CrearError(
            HttpRequestMessage request,
            HttpStatusCode status,
            string message) =>
            new(status)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(
                        new
                        {
                            success = false,
                            message
                        },
                        JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

        private sealed class BalanceEntrada
        {
            public int FuenteId { get; init; }
            public int ElementoId { get; init; }
            public decimal Libras { get; init; }
        }

        private sealed class MixtaFuenteEntrada
        {
            public int FuenteId { get; init; }
            public decimal CantidadQq { get; init; }
        }
    }
}
