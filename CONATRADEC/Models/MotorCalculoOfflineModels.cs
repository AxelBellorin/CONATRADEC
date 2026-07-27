namespace CONATRADEC.Models
{
    public enum ModoTrabajoAnalisis
    {
        EnLinea = 0,
        SinConexion = 1
    }

    public sealed class MotorCalculoPaquete
    {
        public int VersionEsquema { get; set; }
        public string VersionMotorBase { get; set; } = string.Empty;
        public string VersionPaquete { get; set; } = string.Empty;
        public string HashSha256 { get; set; } = string.Empty;
        public DateTime FechaGeneracionUtc { get; set; }
        public string VersionMinimaAplicacion { get; set; } = string.Empty;
        public MotorCalculoModulos Modulos { get; set; } = new();
        public MotorCalculoContenido Contenido { get; set; } = new();
    }

    public sealed class MotorCalculoEstado
    {
        public int VersionEsquema { get; set; }
        public string VersionMotorBase { get; set; } = string.Empty;
        public string VersionPaquete { get; set; } = string.Empty;
        public string HashSha256 { get; set; } = string.Empty;
        public string VersionMinimaAplicacion { get; set; } = string.Empty;
        public DateTime FechaGeneracionUtc { get; set; }
        public MotorCalculoModulos Modulos { get; set; } = new();
    }

    public sealed class MotorCalculoModulos
    {
        public bool RequerimientoAnual { get; set; }
        public bool EnmiendaCalcarea { get; set; }
        public bool BalanceFormula { get; set; }
        public bool FertilizacionMixta { get; set; }
        public bool GuardadoLocal { get; set; }
        public bool Sincronizacion { get; set; }
        public bool ReportePdfLocal { get; set; }
    }

    public sealed class MotorCalculoContenido
    {
        public int UnidadResultadoId { get; set; }
        public string UnidadResultado { get; set; } = "lb/Mz";
        public int UnidadRangoKgHaId { get; set; }

        public List<MotorTipoCultivo> TiposCultivo { get; set; } = new();
        public List<MotorTipoAnalisis> TiposAnalisis { get; set; } = new();
        public List<MotorElemento> Elementos { get; set; } = new();
        public List<MotorUnidad> Unidades { get; set; } = new();

        public List<MotorConversionElemento>
            ConversionesElementos { get; set; } = new();

        public List<MotorConversionMateriaOrganica>
            ConversionesMateriaOrganica { get; set; } = new();

        public List<MotorExtraccion>
            ParametrosExtraccion { get; set; } = new();

        public List<MotorRangoCultivo>
            RangosCultivo { get; set; } = new();

        public List<MotorFuenteNutriente>
            FuentesNutrientes { get; set; } = new();

        public List<MotorFuenteAporte>
            AportesFuentes { get; set; } = new();

        public List<MotorParametroEnmienda>
            ParametrosEnmiendaCalcarea { get; set; } = new();

        public List<int>
            FuentesFertilizacionMixtaIds { get; set; } = new();
    }

    public sealed class MotorTipoCultivo
    {
        public int TipoCultivoId { get; set; }
        public string NombreTipoCultivo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public sealed class MotorTipoAnalisis
    {
        public int TipoAnalisisSueloId { get; set; }
        public string NombreTipoAnalisisSuelo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public sealed class MotorElemento
    {
        public int ElementoQuimicosId { get; set; }
        public string SimboloElementoQuimico { get; set; } = string.Empty;
        public string NombreElementoQuimico { get; set; } = string.Empty;
        public decimal PesoEquivalenteElementoQuimico { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class MotorUnidad
    {
        public int UnidadMedidaId { get; set; }
        public string NombreUnidadMedida { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public abstract class MotorConversionBase
    {
        public int UnidadMedidaId { get; set; }
        public string CodigoFormulaConversion { get; set; } = "LINEAL";
        public decimal FactorPrincipal { get; set; } = 1m;
        public decimal FactorSecundario { get; set; } = 1m;
        public decimal FactorTerciario { get; set; } = 1m;
        public decimal Divisor { get; set; } = 1m;
        public decimal Desplazamiento { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class MotorConversionElemento :
        MotorConversionBase
    {
        public int ElementoQuimicosId { get; set; }
    }

    public sealed class MotorConversionMateriaOrganica :
        MotorConversionBase
    {
    }

    public sealed class MotorExtraccion
    {
        public int ElementoQuimicosId { get; set; }
        public decimal CantidadExtraidaPorQQOro { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class MotorRangoCultivo
    {
        public int TipoCultivoId { get; set; }
        public int ElementoQuimicosId { get; set; }
        public decimal ValorMinimo { get; set; }
        public decimal ValorMaximo { get; set; }
        public string UnidadBase { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public sealed class MotorFuenteNutriente
    {
        public int FuenteNutrientesId { get; set; }
        public string NombreNutriente { get; set; } = string.Empty;
        public string DescripcionNutriente { get; set; } = string.Empty;
        public decimal PrecioNutriente { get; set; }
        public bool HabilitadaEnmiendaCalcarea { get; set; }
        public bool HabilitadaFertilizacionMixta { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class MotorFuenteAporte
    {
        public int FuenteNutrienteElementoQuimicoId { get; set; }
        public int FuenteNutrientesId { get; set; }
        public int ElementoQuimicosId { get; set; }
        public decimal CantidadAporte { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class MotorParametroEnmienda
    {
        public int ParametroEnmiendaCalcareaId { get; set; }
        public int FuenteNutrientesId { get; set; }
        public decimal SaturacionBasesDeseada { get; set; }
        public decimal Prnt { get; set; }
        public decimal FactorTonHaALbHa { get; set; }
        public decimal FactorHaAMz { get; set; }
        public decimal FactorTonHaAKgHa { get; set; }
        public string DescripcionParametro { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public sealed class ResultadoDescargaMotor
    {
        public bool Success { get; init; }
        public bool Actualizado { get; init; }
        public string Message { get; init; } = string.Empty;
        public string VersionPaquete { get; init; } = string.Empty;
        public int TotalRegistros { get; init; }

        public static ResultadoDescargaMotor Ok(
            string message,
            string version,
            int total,
            bool actualizado) =>
            new()
            {
                Success = true,
                Actualizado = actualizado,
                Message = message,
                VersionPaquete = version,
                TotalRegistros = total
            };

        public static ResultadoDescargaMotor Fail(
            string message) =>
            new()
            {
                Success = false,
                Message = message
            };
    }

    public sealed class ApiEnvelopeMotor<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public sealed class ModoTrabajoAnalisisEstado
    {
        public ModoTrabajoAnalisis Modo { get; init; }
        public bool InternetDisponible { get; init; }
        public bool PaqueteLocalDisponible { get; init; }
        public string VersionPaquete { get; init; } = string.Empty;
        public string Mensaje { get; init; } = string.Empty;
        public bool CambioAutomatico { get; init; }
    }

    public sealed class ModoTrabajoAnalisisEventArgs : EventArgs
    {
        public ModoTrabajoAnalisisEstado Estado { get; }

        public ModoTrabajoAnalisisEventArgs(
            ModoTrabajoAnalisisEstado estado)
        {
            Estado = estado;
        }
    }
}
