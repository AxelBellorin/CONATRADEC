using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Representa una unidad editable dentro de la pantalla administrativa.
    /// Los valores numéricos se mantienen como texto para respetar la coma
    /// o el punto decimal utilizado por Windows y Android.
    /// </summary>
    public sealed class ConfiguracionUnidadItemViewModel :
        GlobalService
    {
        private bool activo;
        private bool visibleEnFormulario;
        private bool unidadPredeterminada;
        private FormulaConversionDisponibleResponse?
            formulaSeleccionada;
        private string factorPrincipal = "1";
        private string factorSecundario = "1";
        private string factorTerciario = "1";
        private string divisor = "1";
        private string desplazamiento = "0";
        private string orden = "0";
        private string observacion = string.Empty;

        public event EventHandler?
            PredeterminadaActivada;

        public int ConfiguracionId { get; set; }

        public int UnidadMedidaId { get; set; }

        public string NombreUnidadMedida { get; set; } =
            string.Empty;

        public IReadOnlyList<
            FormulaConversionDisponibleResponse>
            FormulasDisponibles { get; set; } =
                Array.Empty<
                    FormulaConversionDisponibleResponse>();

        public bool Activo
        {
            get => activo;
            set
            {
                if (activo == value)
                    return;

                activo = value;
                OnPropertyChanged();

                if (!activo &&
                    UnidadPredeterminada)
                {
                    UnidadPredeterminada =
                        false;
                }

                OnPropertyChanged(
                    nameof(PuedeSerPredeterminada));
                OnPropertyChanged(
                    nameof(EstadoTexto));
            }
        }

        public bool VisibleEnFormulario
        {
            get => visibleEnFormulario;
            set
            {
                if (visibleEnFormulario == value)
                    return;

                visibleEnFormulario = value;
                OnPropertyChanged();

                if (!visibleEnFormulario &&
                    UnidadPredeterminada)
                {
                    UnidadPredeterminada =
                        false;
                }

                OnPropertyChanged(
                    nameof(PuedeSerPredeterminada));
            }
        }

        public bool UnidadPredeterminada
        {
            get => unidadPredeterminada;
            set
            {
                bool nuevoValor =
                    value &&
                    PuedeSerPredeterminada;

                if (unidadPredeterminada ==
                    nuevoValor)
                {
                    return;
                }

                unidadPredeterminada =
                    nuevoValor;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(EtiquetaPredeterminada));

                if (unidadPredeterminada)
                {
                    PredeterminadaActivada?.Invoke(
                            this,
                            EventArgs.Empty);
                }
            }
        }

        public FormulaConversionDisponibleResponse?
            FormulaSeleccionada
        {
            get => formulaSeleccionada;
            set
            {
                if (ReferenceEquals(
                        formulaSeleccionada,
                        value))
                {
                    return;
                }

                formulaSeleccionada =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(DescripcionFormula));
                OnPropertyChanged(
                    nameof(RequiereMateriaOrganica));
            }
        }

        public string FactorPrincipal
        {
            get => factorPrincipal;
            set
            {
                factorPrincipal =
                    NormalizarNumero(value);

                OnPropertyChanged();
            }
        }

        public string FactorSecundario
        {
            get => factorSecundario;
            set
            {
                factorSecundario =
                    NormalizarNumero(value);

                OnPropertyChanged();
            }
        }

        public string FactorTerciario
        {
            get => factorTerciario;
            set
            {
                factorTerciario =
                    NormalizarNumero(value);

                OnPropertyChanged();
            }
        }

        public string Divisor
        {
            get => divisor;
            set
            {
                divisor =
                    NormalizarNumero(value);

                OnPropertyChanged();
            }
        }

        public string Desplazamiento
        {
            get => desplazamiento;
            set
            {
                desplazamiento =
                    NormalizarNumero(value);

                OnPropertyChanged();
            }
        }

        public string Orden
        {
            get => orden;
            set
            {
                orden =
                    NormalizarEntero(value);

                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => observacion;
            set
            {
                observacion =
                    value ??
                    string.Empty;

                OnPropertyChanged();
            }
        }

        public bool EsUnidadInternaKgHa =>
            string.Equals(
                NombreUnidadMedida.Trim(),
                "kg/ha",
                StringComparison.OrdinalIgnoreCase);

        public bool PuedeQuitar =>
            !EsUnidadInternaKgHa;

        public bool PuedeSerPredeterminada =>
            Activo &&
            VisibleEnFormulario;

        public string EstadoTexto =>
            Activo
                ? "Activa"
                : "Inactiva";

        public string EtiquetaPredeterminada =>
            UnidadPredeterminada
                ? "Predeterminada"
                : "Opcional";

        public string DescripcionFormula =>
            FormulaSeleccionada?.Descripcion ??
            string.Empty;

        public bool RequiereMateriaOrganica =>
            FormulaSeleccionada?.RequiereMateriaOrganica ==
            true;

        public string NombreMostrar =>
            NombreUnidadMedida.Trim();

        public static
            ConfiguracionUnidadItemViewModel
            DesdeRespuesta(
                UnidadConversionConfiguradaResponse
                    origen,
                IReadOnlyList<
                    FormulaConversionDisponibleResponse>
                        formulas)
        {
            ConfiguracionUnidadItemViewModel item =
                new()
                {
                    ConfiguracionId =
                        origen.ConfiguracionId,
                    UnidadMedidaId =
                        origen.UnidadMedidaId,
                    NombreUnidadMedida =
                        origen.NombreUnidadMedida,
                    FormulasDisponibles =
                        formulas,
                    activo =
                        origen.Activo,
                    visibleEnFormulario =
                        origen.VisibleEnFormulario,
                    unidadPredeterminada =
                        origen.UnidadPredeterminada,
                    factorPrincipal =
                        FormatearDecimal(
                            origen.FactorPrincipal),
                    factorSecundario =
                        FormatearDecimal(
                            origen.FactorSecundario),
                    factorTerciario =
                        FormatearDecimal(
                            origen.FactorTerciario),
                    divisor =
                        FormatearDecimal(
                            origen.Divisor),
                    desplazamiento =
                        FormatearDecimal(
                            origen.Desplazamiento),
                    orden =
                        origen.Orden.ToString(
                            CultureInfo.InvariantCulture),
                    observacion =
                        origen.Observacion ??
                        string.Empty
                };

            item.formulaSeleccionada =
                formulas.FirstOrDefault(x =>
                    string.Equals(
                        x.Codigo,
                        origen
                            .CodigoFormulaConversion,
                        StringComparison
                            .OrdinalIgnoreCase))
                ??
                formulas.FirstOrDefault();

            return item;
        }

        public static
            ConfiguracionUnidadItemViewModel
            Nueva(
                UnidadMedidaCatalogoConfiguracionResponse
                    unidad,
                IReadOnlyList<
                    FormulaConversionDisponibleResponse>
                        formulas,
                int ordenSugerido,
                bool predeterminada)
        {
            FormulaConversionDisponibleResponse?
                formulaLineal =
                    formulas.FirstOrDefault(x =>
                        string.Equals(
                            x.Codigo,
                            "LINEAL",
                            StringComparison
                                .OrdinalIgnoreCase))
                    ??
                    formulas.FirstOrDefault();

            return new ConfiguracionUnidadItemViewModel
            {
                UnidadMedidaId =
                    unidad.UnidadMedidaId,
                NombreUnidadMedida =
                    unidad.NombreUnidadMedida,
                FormulasDisponibles =
                    formulas,
                activo = true,
                visibleEnFormulario = true,
                unidadPredeterminada =
                    predeterminada,
                formulaSeleccionada =
                    formulaLineal,
                factorPrincipal = "1",
                factorSecundario = "1",
                factorTerciario = "1",
                divisor = "1",
                desplazamiento = "0",
                orden =
                    ordenSugerido.ToString(
                        CultureInfo.InvariantCulture)
            };
        }

        public bool TryCrearRequest(
            out GuardarUnidadConversionRequest
                request,
            out string error)
        {
            request =
                new GuardarUnidadConversionRequest();

            error = string.Empty;

            if (UnidadMedidaId <= 0)
            {
                error =
                    "La unidad de medida no es válida.";

                return false;
            }

            string formula =
                FormulaSeleccionada?.Codigo?.Trim()
                ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                    formula))
            {
                error =
                    $"Seleccione la fórmula de {NombreMostrar}.";

                return false;
            }

            if (!TryParseDecimal(
                    FactorPrincipal,
                    out decimal factor1))
            {
                error =
                    $"El factor principal de {NombreMostrar} no es válido.";

                return false;
            }

            if (!TryParseDecimal(
                    FactorSecundario,
                    out decimal factor2))
            {
                error =
                    $"El factor secundario de {NombreMostrar} no es válido.";

                return false;
            }

            if (!TryParseDecimal(
                    FactorTerciario,
                    out decimal factor3))
            {
                error =
                    $"El factor terciario de {NombreMostrar} no es válido.";

                return false;
            }

            if (!TryParseDecimal(
                    Divisor,
                    out decimal divisorValor) ||
                divisorValor == 0)
            {
                error =
                    $"El divisor de {NombreMostrar} debe ser distinto de cero.";

                return false;
            }

            if (!TryParseDecimal(
                    Desplazamiento,
                    out decimal desplazamientoValor))
            {
                error =
                    $"El desplazamiento de {NombreMostrar} no es válido.";

                return false;
            }

            if (!int.TryParse(
                    Orden,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int ordenValor) &&
                !int.TryParse(
                    Orden,
                    NumberStyles.Integer,
                    CultureInfo.CurrentCulture,
                    out ordenValor))
            {
                error =
                    $"El orden de {NombreMostrar} no es válido.";

                return false;
            }

            request =
                new GuardarUnidadConversionRequest
                {
                    UnidadMedidaId =
                        UnidadMedidaId,
                    CodigoFormulaConversion =
                        formula,
                    FactorPrincipal =
                        factor1,
                    FactorSecundario =
                        factor2,
                    FactorTerciario =
                        factor3,
                    Divisor =
                        divisorValor,
                    Desplazamiento =
                        desplazamientoValor,
                    UnidadPredeterminada =
                        UnidadPredeterminada,
                    VisibleEnFormulario =
                        VisibleEnFormulario,
                    Orden =
                        ordenValor,
                    Observacion =
                        string.IsNullOrWhiteSpace(
                            Observacion)
                            ? null
                            : Observacion.Trim(),
                    Activo =
                        Activo
                };

            return true;
        }

        private static bool TryParseDecimal(
            string? texto,
            out decimal valor)
        {
            string limpio =
                (texto ?? string.Empty)
                    .Trim();

            string normalizado =
                limpio.Replace(
                    ',',
                    '.');

            if (decimal.TryParse(
                    normalizado,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out valor))
            {
                return true;
            }

            return decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out valor);
        }

        private static string FormatearDecimal(
            decimal valor) =>
                valor.ToString(
                    "0.########",
                    CultureInfo.InvariantCulture);

        private static string NormalizarNumero(
            string? texto)
        {
            string valor =
                (texto ?? string.Empty)
                    .Trim();

            return valor.Length <= 30
                ? valor
                : valor[..30];
        }

        private static string NormalizarEntero(
            string? texto)
        {
            string valor =
                new(
                    (texto ?? string.Empty)
                        .Where(char.IsDigit)
                        .Take(6)
                        .ToArray());

            return valor;
        }

        public override string ToString() =>
            NombreMostrar;
    }
}
