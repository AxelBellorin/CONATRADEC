using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CONATRADEC.Models;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Comprueba que el dispositivo posea todos los datos indispensables para
    /// crear o editar análisis durante una sesión sin conexión.
    ///
    /// La validación se ejecuta antes de navegar al formulario. De esta manera
    /// nunca se abre una pantalla parcialmente cargada con cultivos, elementos
    /// o unidades vacías.
    /// </summary>
    public sealed class AnalisisOfflineFormularioValidacionService
    {
        private static readonly Lazy<
            AnalisisOfflineFormularioValidacionService> lazy =
                new(() =>
                    new AnalisisOfflineFormularioValidacionService());

        private readonly SemaphoreSlim validationLock = new(1, 1);

        private readonly ConfiguracionUnidadesApiService
            configuracionUnidadesApiService = new();

        private AnalisisOfflineFormularioValidacionResultado?
            ultimoResultado;

        private string ultimaClaveValidada = string.Empty;

        private DateTime ultimaValidacionUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromSeconds(30);

        public static AnalisisOfflineFormularioValidacionService Instance =>
            lazy.Value;

        private AnalisisOfflineFormularioValidacionService()
        {
        }

        public async Task<AnalisisOfflineFormularioValidacionResultado>
            ValidarAsync(
                bool forzar = false,
                CancellationToken cancellationToken = default)
        {
            if (!ModoSesionService.EsOffline)
            {
                return AnalisisOfflineFormularioValidacionResultado.Ok(
                    "La sesión está trabajando en línea.");
            }

            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return AnalisisOfflineFormularioValidacionResultado.Fail(
                    "No existe una sesión válida para consultar los datos offline.");
            }

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return AnalisisOfflineFormularioValidacionResultado.Fail(
                    "Su usuario no tiene habilitado el trabajo sin conexión.");
            }

            if (!SincronizacionOfflineGlobalService
                    .EstaPreparadoParaUsuario(usuarioId))
            {
                return AnalisisOfflineFormularioValidacionResultado.Fail(
                    MensajeDescargaRequerida(
                        "Este dispositivo no ha completado la descarga offline."));
            }

            await validationLock.WaitAsync(cancellationToken);

            try
            {
                MotorCalculoPaquete? paquete =
                    await MotorCalculoPaqueteService.Instance
                        .ObtenerPaqueteActivoAsync(cancellationToken);

                if (paquete == null)
                {
                    return AnalisisOfflineFormularioValidacionResultado.Fail(
                        MensajeDescargaRequerida(
                            "El motor de cálculo local no existe, está incompleto o pertenece a otra versión de la aplicación."));
                }

                string claveActual =
                    $"{usuarioId}|" +
                    $"{paquete.VersionEsquema}|" +
                    $"{paquete.VersionPaquete}";

                if (!forzar &&
                    ultimoResultado?.Success == true &&
                    string.Equals(
                        ultimaClaveValidada,
                        claveActual,
                        StringComparison.Ordinal) &&
                    DateTime.UtcNow - ultimaValidacionUtc <
                        DuracionCache)
                {
                    return ultimoResultado;
                }

                AnalisisOfflineFormularioValidacionResultado
                    validacionMotor =
                        ValidarMotor(paquete);

                if (!validacionMotor.Success)
                {
                    GuardarResultado(
                        claveActual,
                        validacionMotor);

                    return validacionMotor;
                }

                /*
                 * Se invalida la caché porque una configuración obtenida en una
                 * sesión en línea anterior no debe mezclarse con el paquete
                 * local del usuario actual.
                 */
                ConfiguracionUnidadesApiService.InvalidarCache();

                using var timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                timeout.CancelAfter(TimeSpan.FromSeconds(12));

                ConfiguracionUnidadesApiResult<
                    ConfiguracionFormularioAnalisisResponse>
                    resultadoConfiguracion =
                        await configuracionUnidadesApiService
                            .ObtenerConfiguracionFormularioAsync(
                                forzarRecarga: true,
                                cancellationToken:
                                    timeout.Token);

                if (!resultadoConfiguracion.Success ||
                    resultadoConfiguracion.Data == null)
                {
                    string detalle =
                        string.IsNullOrWhiteSpace(
                            resultadoConfiguracion.Message)
                            ? "No se encontró la configuración local de unidades."
                            : resultadoConfiguracion.Message;

                    AnalisisOfflineFormularioValidacionResultado error =
                        AnalisisOfflineFormularioValidacionResultado.Fail(
                            MensajeDescargaRequerida(detalle));

                    GuardarResultado(claveActual, error);
                    return error;
                }

                AnalisisOfflineFormularioValidacionResultado
                    validacionUnidades =
                        ValidarConfiguracionUnidades(
                            paquete,
                            resultadoConfiguracion.Data);

                GuardarResultado(
                    claveActual,
                    validacionUnidades);

                return validacionUnidades;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return AnalisisOfflineFormularioValidacionResultado.Fail(
                    MensajeDescargaRequerida(
                        "La comprobación de los datos locales tardó demasiado."));
            }
            catch (Exception ex)
            {
                return AnalisisOfflineFormularioValidacionResultado.Fail(
                    MensajeDescargaRequerida(
                        "No fue posible verificar los datos locales: " +
                        ex.Message));
            }
            finally
            {
                validationLock.Release();
            }
        }

        public void Invalidar()
        {
            ultimoResultado = null;
            ultimaClaveValidada = string.Empty;
            ultimaValidacionUtc = default;
        }

        private static AnalisisOfflineFormularioValidacionResultado
            ValidarMotor(
                MotorCalculoPaquete paquete)
        {
            MotorCalculoContenido? contenido =
                paquete.Contenido;

            if (contenido == null)
            {
                return ErrorMotor(
                    "El paquete local no contiene el motor de cálculo.");
            }

            List<MotorTipoCultivo> cultivos =
                contenido.TiposCultivo
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoCultivoId > 0)
                    .ToList();

            List<MotorTipoAnalisis> tiposAnalisis =
                contenido.TiposAnalisis
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoAnalisisSueloId > 0)
                    .ToList();

            List<MotorElemento> elementos =
                contenido.Elementos
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId > 0)
                    .ToList();

            List<MotorUnidad> unidades =
                contenido.Unidades
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.UnidadMedidaId > 0)
                    .ToList();

            if (cultivos.Count == 0)
            {
                return ErrorMotor(
                    "No hay tipos de cultivo disponibles.");
            }

            if (tiposAnalisis.Count == 0)
            {
                return ErrorMotor(
                    "No hay tipos de análisis de suelo disponibles.");
            }

            if (elementos.Count == 0)
            {
                return ErrorMotor(
                    "No hay elementos químicos disponibles.");
            }

            if (unidades.Count == 0)
            {
                return ErrorMotor(
                    "No hay unidades de medida disponibles.");
            }

            HashSet<int> unidadesIds =
                unidades.Select(x => x.UnidadMedidaId).ToHashSet();

            HashSet<int> elementosIds =
                elementos
                    .Select(x => x.ElementoQuimicosId)
                    .ToHashSet();

            foreach (MotorElemento elemento in elementos)
            {
                bool tieneConversion =
                    contenido.ConversionesElementos.Any(x =>
                        x != null &&
                        x.Activo &&
                        x.ElementoQuimicosId ==
                            elemento.ElementoQuimicosId &&
                        unidadesIds.Contains(x.UnidadMedidaId));

                if (!tieneConversion)
                {
                    return ErrorMotor(
                        $"El elemento {NombreElemento(elemento)} no tiene una unidad de conversión válida.");
                }
            }

            bool materiaOrganicaValida =
                contenido.ConversionesMateriaOrganica.Any(x =>
                    x != null &&
                    x.Activo &&
                    unidadesIds.Contains(x.UnidadMedidaId));

            if (!materiaOrganicaValida)
            {
                return ErrorMotor(
                    "Materia orgánica no tiene una unidad de conversión válida.");
            }

            HashSet<int> cultivosConRangos =
                contenido.RangosCultivo
                    .Where(x =>
                        x != null &&
                        x.Activo &&
                        x.TipoCultivoId > 0 &&
                        elementosIds.Contains(
                            x.ElementoQuimicosId))
                    .Select(x => x.TipoCultivoId)
                    .ToHashSet();

            if (cultivosConRangos.Count == 0)
            {
                return ErrorMotor(
                    "No existen rangos nutricionales válidos para los cultivos descargados.");
            }

            return AnalisisOfflineFormularioValidacionResultado.Ok(
                "El motor local contiene los catálogos y conversiones requeridos.");
        }

        private static AnalisisOfflineFormularioValidacionResultado
            ValidarConfiguracionUnidades(
                MotorCalculoPaquete paquete,
                ConfiguracionFormularioAnalisisResponse configuracion)
        {
            List<UnidadConversionConfiguradaResponse>
                unidadesMateriaOrganica =
                    configuracion.UnidadesMateriaOrganica
                        .Where(EsUnidadVisibleValida)
                        .ToList();

            if (unidadesMateriaOrganica.Count == 0)
            {
                return ErrorMotor(
                    "Materia orgánica no tiene unidades visibles configuradas.");
            }

            HashSet<int> conversionesMateriaIds =
                paquete.Contenido
                    .ConversionesMateriaOrganica
                    .Where(x => x != null && x.Activo)
                    .Select(x => x.UnidadMedidaId)
                    .ToHashSet();

            if (!unidadesMateriaOrganica.Any(x =>
                    conversionesMateriaIds.Contains(
                        x.UnidadMedidaId)))
            {
                return ErrorMotor(
                    "Las unidades de materia orgánica no coinciden con el motor descargado.");
            }

            foreach (MotorElemento elemento in
                     paquete.Contenido.Elementos.Where(x =>
                         x != null &&
                         x.Activo &&
                         x.ElementoQuimicosId > 0))
            {
                ElementoConfiguracionUnidadesResponse?
                    configuracionElemento =
                        configuracion.Elementos.FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                                elemento.ElementoQuimicosId);

                if (configuracionElemento == null)
                {
                    return ErrorMotor(
                        $"No existe configuración de unidades para {NombreElemento(elemento)}.");
                }

                List<UnidadConversionConfiguradaResponse>
                    unidadesElemento =
                        configuracionElemento.Unidades
                            .Where(EsUnidadVisibleValida)
                            .ToList();

                if (unidadesElemento.Count == 0)
                {
                    return ErrorMotor(
                        $"{NombreElemento(elemento)} no tiene unidades visibles configuradas.");
                }

                HashSet<int> conversionesElementoIds =
                    paquete.Contenido.ConversionesElementos
                        .Where(x =>
                            x != null &&
                            x.Activo &&
                            x.ElementoQuimicosId ==
                                elemento.ElementoQuimicosId)
                        .Select(x => x.UnidadMedidaId)
                        .ToHashSet();

                if (!unidadesElemento.Any(x =>
                        conversionesElementoIds.Contains(
                            x.UnidadMedidaId)))
                {
                    return ErrorMotor(
                        $"Las unidades configuradas para {NombreElemento(elemento)} no coinciden con el motor descargado.");
                }
            }

            return AnalisisOfflineFormularioValidacionResultado.Ok(
                "Los datos offline del formulario de análisis están completos.");
        }

        private static bool EsUnidadVisibleValida(
            UnidadConversionConfiguradaResponse unidad) =>
            unidad != null &&
            unidad.Activo &&
            unidad.VisibleEnFormulario &&
            unidad.UnidadMedidaId > 0 &&
            !string.IsNullOrWhiteSpace(
                unidad.NombreUnidadMedida);

        private static AnalisisOfflineFormularioValidacionResultado
            ErrorMotor(
                string detalle) =>
            AnalisisOfflineFormularioValidacionResultado.Fail(
                MensajeDescargaRequerida(detalle));

        private static string NombreElemento(
            MotorElemento elemento)
        {
            string nombre =
                elemento.NombreElementoQuimico?.Trim() ??
                string.Empty;

            string simbolo =
                elemento.SimboloElementoQuimico?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = $"Elemento #{elemento.ElementoQuimicosId}";

            return string.IsNullOrWhiteSpace(simbolo)
                ? nombre
                : $"{nombre} ({simbolo})";
        }

        private static string MensajeDescargaRequerida(
            string detalle) =>
            (detalle ?? string.Empty).TrimEnd('.', ' ') +
            ". Inicie sesión en línea y ejecute Descargar todo antes de trabajar sin conexión.";

        private void GuardarResultado(
            string clave,
            AnalisisOfflineFormularioValidacionResultado resultado)
        {
            ultimoResultado = resultado;
            ultimaClaveValidada = clave;
            ultimaValidacionUtc = DateTime.UtcNow;
        }
    }

    public sealed class AnalisisOfflineFormularioValidacionResultado
    {
        private AnalisisOfflineFormularioValidacionResultado(
            bool success,
            string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }

        public string Message { get; }

        public static AnalisisOfflineFormularioValidacionResultado Ok(
            string message) =>
            new(true, message);

        public static AnalisisOfflineFormularioValidacionResultado Fail(
            string message) =>
            new(false, message);
    }
}
