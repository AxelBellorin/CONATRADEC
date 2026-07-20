using CONATRADEC.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CONATRADEC.Converters
{
    internal static class ConversionVisualizacionHelper
    {
        public static decimal ObtenerDecimal(object? valor)
        {
            if (valor == null)
                return 0m;

            if (valor is decimal decimalValor)
                return decimalValor;

            if (valor is double doubleValor)
                return Convert.ToDecimal(doubleValor);

            if (valor is float floatValor)
                return Convert.ToDecimal(floatValor);

            if (valor is int intValor)
                return intValor;

            if (valor is long longValor)
                return longValor;

            return decimal.TryParse(
                valor.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal resultado)
                    ? resultado
                    : 0m;
        }

        public static IEnumerable<AnalisisGuardadoFormulaDetalle>
            ObtenerDetalles(object? valor)
        {
            if (valor is IEnumerable<AnalisisGuardadoFormulaDetalle>
                detallesTipados)
            {
                return detallesTipados;
            }

            if (valor is IEnumerable enumerable)
            {
                return enumerable
                    .Cast<object>()
                    .OfType<AnalisisGuardadoFormulaDetalle>();
            }

            return Enumerable.Empty<AnalisisGuardadoFormulaDetalle>();
        }
    }

    public sealed class QuintalesCompraConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            decimal quintales =
                ConversionVisualizacionHelper.ObtenerDecimal(value);

            return Math.Ceiling(Math.Max(0m, quintales));
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class CostoCompraFuenteConverter :
        IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (values.Length < 2)
                return 0m;

            decimal quintales =
                ConversionVisualizacionHelper.ObtenerDecimal(values[0]);

            decimal precio =
                ConversionVisualizacionHelper.ObtenerDecimal(values[1]);

            return Math.Ceiling(Math.Max(0m, quintales)) *
                   Math.Max(0m, precio);
        }

        public object[] ConvertBack(
            object? value,
            Type[] targetTypes,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class BalanceResumenTotalConverter :
        IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            List<AnalisisGuardadoFormulaDetalle> detalles =
                ConversionVisualizacionHelper
                    .ObtenerDetalles(value)
                    .ToList();

            string operacion =
                parameter?.ToString()?.Trim().ToUpperInvariant()
                ?? string.Empty;

            return operacion switch
            {
                "LIBRAS" =>
                    detalles.Sum(x => x.Libras),

                "QQEXACTOS" =>
                    detalles.Sum(x => x.Qq),

                "QQCOMPRA" =>
                    detalles.Sum(x =>
                        Math.Ceiling(Math.Max(0m, x.Qq))),

                "SUBTOTALEXACTO" =>
                    detalles.Sum(x => x.SubtotalFuente),

                "COSTOCOMPRA" =>
                    detalles.Sum(x =>
                        Math.Ceiling(Math.Max(0m, x.Qq)) *
                        Math.Max(0m, x.PrecioPorQuintal)),

                "ONZAS" =>
                    detalles.Sum(x => x.OnzasAnuales),

                _ => 0m
            };
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class ModoBalanceConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value is true
                ? "Complementado con fertilización mixta"
                : "Balance de fórmula independiente";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class ModoMixtaConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value is true
                ? "Fertilización mixta como complemento del balance"
                : "Fertilización mixta independiente";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class EnmiendaInterpretacionConverter :
        IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            decimal necesidad =
                ConversionVisualizacionHelper.ObtenerDecimal(value);

            if (necesidad <= 0m)
            {
                return "Con los datos guardados no se determinó una " +
                       "necesidad positiva de encalado.";
            }

            if (necesidad < 0.5m)
            {
                return "La necesidad de encalado calculada es baja. " +
                       "Debe aplicarse según la fuente y la dosis indicadas.";
            }

            if (necesidad < 1.5m)
            {
                return "La necesidad de encalado calculada es moderada. " +
                       "Se recomienda respetar las aplicaciones y la dosis " +
                       "por planta mostradas en este resultado.";
            }

            return "La necesidad de encalado calculada es alta. " +
                   "Conviene distribuir la aplicación según el número de " +
                   "aplicaciones indicado y dar seguimiento al pH del suelo.";
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
