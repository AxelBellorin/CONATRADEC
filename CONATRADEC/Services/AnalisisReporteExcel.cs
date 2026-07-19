using ClosedXML.Excel;
using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CONATRADEC.Services
{
    public static class AnalisisReporteExcel
    {
        private const string Verde = "#3B655B";
        private const string Cafe = "#9B552C";
        private const string VerdeSuave = "#EEF5F2";
        private const string AmarilloSuave = "#FFF8D8";

        public static byte[] Generar(AnalisisReporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            using XLWorkbook libro = new();

            CrearResumen(libro, reporte);
            CrearLaboratorio(libro, reporte);
            CrearRequerimiento(libro, reporte);

            if (reporte.Balance != null)
                CrearBalance(libro, reporte.Balance);

            if (reporte.Enmienda != null)
                CrearEnmienda(libro, reporte.Enmienda);

            if (reporte.FertilizacionMixta != null)
                CrearFertilizacionMixta(libro, reporte.FertilizacionMixta);

            foreach (IXLWorksheet hoja in libro.Worksheets)
                ConfigurarHoja(hoja);

            using MemoryStream stream = new();
            libro.SaveAs(stream);
            return stream.ToArray();
        }

        private static void CrearResumen(
            XLWorkbook libro,
            AnalisisReporte reporte)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Resumen");

            hoja.Cell("A1").Value = "CONATRACAFÉ SOIL";
            hoja.Cell("A2").Value = "Reporte integral de análisis de suelo";
            hoja.Range("A1:D1").Merge();
            hoja.Range("A2:D2").Merge();

            hoja.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
            hoja.Range("A1:D1").Style.Font.FontColor = XLColor.White;
            hoja.Range("A1:D1").Style.Font.Bold = true;
            hoja.Range("A1:D1").Style.Font.FontSize = 18;
            hoja.Range("A2:D2").Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
            hoja.Range("A2:D2").Style.Font.FontColor = XLColor.White;
            hoja.Range("A2:D2").Style.Font.Italic = true;

            int fila = 4;
            AgregarDato(hoja, ref fila, "Identificador", reporte.Identificador);
            AgregarDato(hoja, ref fila, "Fecha del análisis", reporte.FechaAnalisis);
            AgregarDato(hoja, ref fila, "Laboratorio", reporte.Laboratorio);
            AgregarDato(hoja, ref fila, "Cliente", reporte.Cliente);
            AgregarDato(hoja, ref fila, "Terreno", reporte.Terreno);
            AgregarDato(hoja, ref fila, "Cultivo", reporte.TipoCultivo);
            AgregarDato(hoja, ref fila, "Tipo de análisis", reporte.TipoAnalisis);
            AgregarDato(hoja, ref fila, "Producción (qq oro)", reporte.ProduccionQqOro);
            AgregarDato(hoja, ref fila, "Tamaño de finca (mz)", reporte.TamanoFincaMz);
            AgregarDato(hoja, ref fila, "pH", reporte.Ph);
            AgregarDato(hoja, ref fila, "Materia orgánica", reporte.MateriaOrganica);
            AgregarDato(hoja, ref fila, "Acidez total", reporte.AcidezTotal);

            fila++;
            hoja.Cell(fila, 1).Value = "Recomendación general";
            hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
            hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.White;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Range(fila, 2, fila, 4).Merge();
            hoja.Cell(fila, 2).Value = reporte.RecomendacionGeneral;
            hoja.Cell(fila, 2).Style.Alignment.WrapText = true;

            if (reporte.Observaciones.Count > 0)
            {
                fila++;
                hoja.Cell(fila, 1).Value = "Observaciones";
                hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
                hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.White;
                hoja.Cell(fila, 1).Style.Font.Bold = true;
                hoja.Range(fila, 2, fila, 4).Merge();
                hoja.Cell(fila, 2).Value = string.Join(" · ", reporte.Observaciones);
                hoja.Cell(fila, 2).Style.Alignment.WrapText = true;
            }

            hoja.Column(1).Width = 28;
            hoja.Column(2).Width = 65;
            hoja.Columns(3, 4).Width = 18;
            hoja.SheetView.FreezeRows(2);
        }

        private static void CrearLaboratorio(
            XLWorkbook libro,
            AnalisisReporte reporte)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Laboratorio");
            string[] encabezados = { "Elemento", "Cantidad", "Unidad" };
            CrearEncabezado(hoja, 1, encabezados);

            int fila = 2;
            foreach (AnalisisReporteValorLaboratorio item in reporte.ValoresLaboratorio)
            {
                hoja.Cell(fila, 1).Value = item.Elemento;
                hoja.Cell(fila, 2).Value = item.Cantidad;
                hoja.Cell(fila, 3).Value = item.Unidad;
                hoja.Cell(fila, 2).Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            FinalizarTabla(hoja, 1, fila - 1, encabezados.Length);
        }

        private static void CrearRequerimiento(
            XLWorkbook libro,
            AnalisisReporte reporte)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Requerimiento");
            string[] encabezados =
            {
                "Elemento",
                "Cantidad ingresada",
                "Convertido lb/mz",
                "Requerimiento",
                "Unidad",
                "Clasificación",
                "Observación"
            };

            CrearEncabezado(hoja, 1, encabezados);

            int fila = 2;
            foreach (AnalisisReporteRequerimiento item in reporte.Requerimientos)
            {
                hoja.Cell(fila, 1).Value = item.Elemento;
                hoja.Cell(fila, 2).Value = item.CantidadIngresada;
                AsignarNullable(hoja.Cell(fila, 3), item.CantidadConvertidaLbMz);
                AsignarNullable(hoja.Cell(fila, 4), item.RequerimientoLbMz);
                hoja.Cell(fila, 5).Value = item.UnidadResultado;
                hoja.Cell(fila, 6).Value = item.Clasificacion;
                hoja.Cell(fila, 7).Value = item.Observacion;
                hoja.Range(fila, 2, fila, 4).Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            FinalizarTabla(hoja, 1, fila - 1, encabezados.Length);
            hoja.Column(7).Width = 60;
            hoja.Column(7).Style.Alignment.WrapText = true;
        }

        private static void CrearBalance(
            XLWorkbook libro,
            AnalisisReporteBalance balance)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Balance fórmula");

            hoja.Cell("A1").Value = "BALANCE DE FÓRMULA";
            hoja.Range("A1:H1").Merge();
            EstiloTitulo(hoja.Range("A1:H1"));

            int fila = 3;
            AgregarDato(hoja, ref fila, "Nombre de fórmula", balance.NombreFormula);
            AgregarDato(hoja, ref fila, "Mezcla exacta (qq)", balance.MezclaTotalQq);
            AgregarDato(hoja, ref fila, "Total libras", balance.TotalLibras);
            AgregarDato(hoja, ref fila, "Total de plantas", balance.TotalPlantas);
            AgregarDato(hoja, ref fila, "Aplicaciones", balance.TotalAplicaciones);
            AgregarDato(hoja, ref fila, "Dosis anual (oz/planta)", balance.DosisPlantaAnualOz);
            AgregarDato(hoja, ref fila, "Dosis por aplicación (oz/planta)", balance.DosisPlantaPorAplicacionOz);
            AgregarDato(hoja, ref fila, "Precio exacto de referencia", balance.PrecioExactoReferencia, moneda: true);
            AgregarDato(hoja, ref fila, "Costo real de compra", balance.CostoRealCompra, moneda: true);
            AgregarDato(hoja, ref fila, "Costo por aplicación", balance.PrecioPorAplicacion, moneda: true);

            if (balance.FormulaComercial.Count > 0)
            {
                fila++;
                hoja.Cell(fila, 1).Value = "Fórmula comercial";
                hoja.Cell(fila, 1).Style.Font.Bold = true;
                hoja.Cell(fila, 2).Value = string.Join(
                    " - ",
                    balance.FormulaComercial.Select(x => $"{x.Key} {x.Value:N2}"));
                fila++;
            }

            fila++;
            int filaEncabezado = fila;
            string[] encabezados =
            {
                "Fuente",
                "Elemento",
                "Libras",
                "QQ exactos",
                "QQ a comprar",
                "Precio por QQ",
                "Subtotal exacto",
                "Costo real de compra"
            };
            CrearEncabezado(hoja, filaEncabezado, encabezados);

            fila++;
            foreach (AnalisisReporteBalanceDetalle item in balance.Detalles)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.Elemento;
                hoja.Cell(fila, 3).Value = item.Libras;
                hoja.Cell(fila, 4).Value = item.QuintalesExactos;
                hoja.Cell(fila, 5).Value = item.QuintalesComprar;
                hoja.Cell(fila, 6).Value = item.PrecioPorQuintal;
                hoja.Cell(fila, 7).Value = item.SubtotalExacto;
                hoja.Cell(fila, 8).Value = item.CostoCompra;
                hoja.Range(fila, 3, fila, 5).Style.NumberFormat.Format = "0.000";
                hoja.Range(fila, 6, fila, 8).Style.NumberFormat.Format = "C$ #,##0.00";
                fila++;
            }

            FinalizarTabla(hoja, filaEncabezado, fila - 1, encabezados.Length);
            hoja.SheetView.FreezeRows(filaEncabezado);
        }

        private static void CrearEnmienda(
            XLWorkbook libro,
            AnalisisReporteEnmienda enmienda)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Enmienda calcárea");

            hoja.Cell("A1").Value = "ENMIENDA CALCÁREA";
            hoja.Range("A1:D1").Merge();
            EstiloTitulo(hoja.Range("A1:D1"));

            int fila = 3;
            AgregarDato(hoja, ref fila, "Nombre del análisis", enmienda.NombreAnalisis);
            AgregarDato(hoja, ref fila, "Fuente", enmienda.Fuente);
            AgregarDato(hoja, ref fila, "Total de plantas", enmienda.TotalPlantas);
            AgregarDato(hoja, ref fila, "Aplicaciones", enmienda.TotalAplicaciones);
            AgregarDato(hoja, ref fila, "pH", enmienda.Ph);
            AgregarDato(hoja, ref fila, "Calcio", enmienda.Calcio);
            AgregarDato(hoja, ref fila, "Magnesio", enmienda.Magnesio);
            AgregarDato(hoja, ref fila, "Potasio", enmienda.Potasio);
            AgregarDato(hoja, ref fila, "Acidez total", enmienda.AcidezTotal);
            AgregarDato(hoja, ref fila, "CICE", enmienda.Cice);
            AgregarDato(hoja, ref fila, "Saturación actual (%)", enmienda.SaturacionActual);
            AgregarDato(hoja, ref fila, "Saturación deseada (%)", enmienda.SaturacionDeseada);
            AgregarDato(hoja, ref fila, "PRNT (%)", enmienda.Prnt);
            AgregarDato(hoja, ref fila, "Necesidad (ton/ha)", enmienda.NecesidadEncaladoTonHa);
            AgregarDato(hoja, ref fila, "Necesidad (kg/ha)", enmienda.NecesidadEncaladoKgHa);
            AgregarDato(hoja, ref fila, "Necesidad (lb/ha)", enmienda.NecesidadEncaladoLbHa);
            AgregarDato(hoja, ref fila, "Necesidad (lb/mz)", enmienda.NecesidadEncaladoLbMz);
            AgregarDato(hoja, ref fila, "Dosis anual (oz/planta)", enmienda.DosisPlantaAnualOz);
            AgregarDato(hoja, ref fila, "Dosis por aplicación (oz/planta)", enmienda.DosisPlantaPorAplicacionOz);

            hoja.Column(1).Width = 38;
            hoja.Column(2).Width = 24;
            hoja.SheetView.FreezeRows(1);
        }

        private static void CrearFertilizacionMixta(
            XLWorkbook libro,
            AnalisisReporteFertilizacionMixta mixta)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Mixta fuentes");

            hoja.Cell("A1").Value = "FERTILIZACIÓN MIXTA - FUENTES";
            hoja.Range("A1:B1").Merge();
            EstiloTitulo(hoja.Range("A1:B1"));

            hoja.Cell("A3").Value = "Observación";
            hoja.Cell("A3").Style.Font.Bold = true;
            hoja.Cell("B3").Value = mixta.Observacion;
            hoja.Cell("B3").Style.Alignment.WrapText = true;

            int filaFuentes = 5;
            CrearEncabezado(hoja, filaFuentes, new[] { "Fuente", "Cantidad (qq)" });
            int fila = filaFuentes + 1;

            foreach (AnalisisReporteMixtaFuente item in mixta.Fuentes)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.CantidadQq;
                hoja.Range(fila, 1, fila, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(AmarilloSuave);
                fila++;
            }

            FinalizarTabla(hoja, filaFuentes, fila - 1, 2);

            IXLWorksheet hojaResultado = libro.Worksheets.Add("Mixta resultado");
            int filaDetalle = 1;
            string[] encabezados =
            {
                "Elemento",
                "Requerimiento original",
                "Aporte orgánico",
                "Diferencia",
                "Déficit",
                "Sobrante"
            };
            CrearEncabezado(hojaResultado, filaDetalle, encabezados);
            fila = filaDetalle + 1;

            foreach (AnalisisReporteMixtaDetalle item in mixta.Detalles)
            {
                hojaResultado.Cell(fila, 1).Value = item.Elemento;
                hojaResultado.Cell(fila, 2).Value = item.RequerimientoOriginal;
                hojaResultado.Cell(fila, 3).Value = item.AporteOrganico;
                hojaResultado.Cell(fila, 4).Value = item.Diferencia;
                hojaResultado.Cell(fila, 5).Value = item.Deficit;
                hojaResultado.Cell(fila, 6).Value = item.Sobrante;
                hojaResultado.Range(fila, 2, fila, 6).Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            FinalizarTabla(
                hojaResultado,
                filaDetalle,
                fila - 1,
                encabezados.Length);
        }

        private static void CrearEncabezado(
            IXLWorksheet hoja,
            int fila,
            IReadOnlyList<string> encabezados)
        {
            for (int indice = 0; indice < encabezados.Count; indice++)
                hoja.Cell(fila, indice + 1).Value = encabezados[indice];

            IXLRange rango = hoja.Range(fila, 1, fila, encabezados.Count);
            rango.Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
            rango.Style.Font.FontColor = XLColor.White;
            rango.Style.Font.Bold = true;
            rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rango.Style.Alignment.WrapText = true;
        }

        private static void FinalizarTabla(
            IXLWorksheet hoja,
            int primeraFila,
            int ultimaFila,
            int ultimaColumna)
        {
            if (ultimaFila >= primeraFila)
            {
                IXLRange rango = hoja.Range(
                    primeraFila,
                    1,
                    ultimaFila,
                    ultimaColumna);

                rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                rango.Style.Border.OutsideBorderColor = XLColor.LightGray;
                rango.Style.Border.InsideBorderColor = XLColor.LightGray;

                if (ultimaFila > primeraFila)
                    rango.SetAutoFilter();
            }

            // Anchos fijos: además de dar uniformidad, evitan que ClosedXML
            // tenga que medir fuentes del sistema en Android.
            hoja.Column(1).Width = 30;
            for (int columna = 2; columna <= ultimaColumna; columna++)
                hoja.Column(columna).Width = 18;

            hoja.RangeUsed()!.Style.Alignment.WrapText = true;
            hoja.RowsUsed().Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            hoja.SheetView.FreezeRows(primeraFila);
        }

        private static void EstiloTitulo(IXLRange rango)
        {
            rango.Style.Fill.BackgroundColor = XLColor.FromHtml(Verde);
            rango.Style.Font.FontColor = XLColor.White;
            rango.Style.Font.Bold = true;
            rango.Style.Font.FontSize = 16;
            rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void ConfigurarHoja(IXLWorksheet hoja)
        {
            int ultimaColumna = hoja.LastColumnUsed()?.ColumnNumber() ?? 1;

            hoja.ShowGridLines = false;
            hoja.PageSetup.PaperSize = XLPaperSize.A4Paper;
            hoja.PageSetup.PageOrientation = ultimaColumna > 4
                ? XLPageOrientation.Landscape
                : XLPageOrientation.Portrait;
            hoja.PageSetup.FitToPages(1, 0);
            hoja.PageSetup.CenterHorizontally = true;
            hoja.PageSetup.Margins.Left = 0.3;
            hoja.PageSetup.Margins.Right = 0.3;
            hoja.PageSetup.Margins.Top = 0.4;
            hoja.PageSetup.Margins.Bottom = 0.4;
            hoja.PageSetup.Margins.Header = 0.2;
            hoja.PageSetup.Margins.Footer = 0.2;
        }

        private static void AgregarDato(
            IXLWorksheet hoja,
            ref int fila,
            string etiqueta,
            string? valor)
        {
            hoja.Cell(fila, 1).Value = etiqueta;
            hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = valor ?? string.Empty;
            fila++;
        }

        private static void AgregarDato(
            IXLWorksheet hoja,
            ref int fila,
            string etiqueta,
            decimal valor,
            bool moneda = false)
        {
            hoja.Cell(fila, 1).Value = etiqueta;
            hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = valor;
            hoja.Cell(fila, 2).Style.NumberFormat.Format = moneda
                ? "C$ #,##0.00"
                : "0.0000";
            fila++;
        }

        private static void AgregarDato(
            IXLWorksheet hoja,
            ref int fila,
            string etiqueta,
            int valor)
        {
            hoja.Cell(fila, 1).Value = etiqueta;
            hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = valor;
            fila++;
        }

        private static void AgregarDato(
            IXLWorksheet hoja,
            ref int fila,
            string etiqueta,
            decimal? valor)
        {
            hoja.Cell(fila, 1).Value = etiqueta;
            hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            AsignarNullable(hoja.Cell(fila, 2), valor);
            fila++;
        }

        private static void AsignarNullable(IXLCell celda, decimal? valor)
        {
            if (valor.HasValue)
            {
                celda.Value = valor.Value;
                celda.Style.NumberFormat.Format = "0.0000";
            }
            else
            {
                celda.Value = string.Empty;
            }
        }
    }
}
