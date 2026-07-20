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
            AgregarDato(hoja, ref fila, "Responsable", reporte.Responsable);
            AgregarDato(hoja, ref fila, "Producción (qq oro)", reporte.ProduccionQqOro);
            AgregarDato(hoja, ref fila, "Tamaño de finca (mz)", reporte.TamanoFincaMz);
            AgregarDato(hoja, ref fila, "pH", reporte.Ph);
            string unidadMateriaOrganica = string.IsNullOrWhiteSpace(
                reporte.UnidadMateriaOrganica)
                ? string.Empty
                : $" {reporte.UnidadMateriaOrganica}";
            AgregarDato(
                hoja,
                ref fila,
                $"Materia orgánica{unidadMateriaOrganica}",
                reporte.MateriaOrganica);
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
            hoja.Range("A1:L1").Merge();
            EstiloTitulo(hoja.Range("A1:L1"));

            int fila = 3;
            AgregarDato(hoja, ref fila, "Nombre de fórmula", balance.NombreFormula);
            AgregarDato(hoja, ref fila, "Mezcla exacta (qq)", balance.MezclaTotalQq);
            AgregarDato(hoja, ref fila, "Total libras", balance.TotalLibras);
            AgregarDato(hoja, ref fila, "Total onzas", balance.TotalOnzas);
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
                hoja.Range(fila, 2, fila, 12).Merge();
                hoja.Cell(fila, 2).Value = string.Join(
                    " - ",
                    balance.FormulaComercial
                        .OrderBy(x => OrdenElemento(x.Key))
                        .Select(x => $"{x.Key} {x.Value:N2}"));
                ResaltarFormulaComercial(hoja, fila, 12);
                fila++;
            }

            fila++;
            int filaEncabezado = fila;
            string[] encabezados =
            {
                "Fuente",
                "Elemento",
                "Requerimiento (lb)",
                "Libras anuales",
                "Lb por aplicación",
                "Onzas anuales",
                "Oz por aplicación",
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
                hoja.Cell(fila, 3).Value = item.RequerimientoLibras;
                hoja.Cell(fila, 4).Value = item.Libras;
                hoja.Cell(fila, 5).Value = item.LibrasPorAplicacion;
                hoja.Cell(fila, 6).Value = item.OnzasAnuales;
                hoja.Cell(fila, 7).Value = item.OnzasPorAplicacion;
                hoja.Cell(fila, 8).Value = item.QuintalesExactos;
                hoja.Cell(fila, 9).Value = item.QuintalesComprar;
                hoja.Cell(fila, 10).Value = item.PrecioPorQuintal;
                hoja.Cell(fila, 11).Value = item.SubtotalExacto;
                hoja.Cell(fila, 12).Value = item.CostoCompra;
                hoja.Range(fila, 3, fila, 9).Style.NumberFormat.Format = "0.0000";
                hoja.Range(fila, 10, fila, 12).Style.NumberFormat.Format = "C$ #,##0.00";
                fila++;
            }

            FinalizarTabla(hoja, filaEncabezado, fila - 1, encabezados.Length);

            AgregarAportesBalance(
                hoja,
                balance,
                ref fila);

            hoja.SheetView.FreezeRows(filaEncabezado);
        }

        private static void AgregarAportesBalance(
            IXLWorksheet hoja,
            AnalisisReporteBalance balance,
            ref int fila)
        {
            List<string> elementos = balance.Detalles
                .SelectMany(x => x.Aportes.Keys)
                .Concat(balance.FormulaComercial.Keys)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(OrdenElemento)
                .ToList();

            if (elementos.Count == 0)
                return;

            string[] encabezados = new[] { "Fuente", "Libras", "QQ" }
                .Concat(elementos.Select(x => $"{x} aportado"))
                .ToArray();

            AgregarTituloBloque(
                hoja,
                ref fila,
                "APORTES NUTRICIONALES POR FUENTE",
                Math.Max(12, encabezados.Length));

            int filaEncabezado = fila;
            CrearEncabezado(hoja, filaEncabezado, encabezados);
            fila++;

            foreach (AnalisisReporteBalanceDetalle item in balance.Detalles)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.Libras;
                hoja.Cell(fila, 3).Value = item.QuintalesExactos;

                for (int indice = 0; indice < elementos.Count; indice++)
                {
                    hoja.Cell(fila, indice + 4).Value =
                        ObtenerAporte(item.Aportes, elementos[indice]);
                }

                hoja.Range(fila, 2, fila, encabezados.Length)
                    .Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            hoja.Cell(fila, 1).Value = "Fórmula comercial";
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = balance.TotalLibras;
            hoja.Cell(fila, 3).Value = balance.MezclaTotalQq;

            for (int indice = 0; indice < elementos.Count; indice++)
            {
                hoja.Cell(fila, indice + 4).Value =
                    ObtenerAporte(
                        balance.FormulaComercial,
                        elementos[indice]);
            }

            hoja.Range(fila, 1, fila, encabezados.Length)
                .Style.Fill.BackgroundColor = XLColor.FromHtml(AmarilloSuave);
            hoja.Range(fila, 1, fila, encabezados.Length)
                .Style.Font.Bold = true;
            hoja.Range(fila, 2, fila, encabezados.Length)
                .Style.NumberFormat.Format = "0.0000";

            FinalizarTabla(
                hoja,
                filaEncabezado,
                fila,
                encabezados.Length,
                aplicarFiltro: false);

            fila++;
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

            hoja.Cell(fila, 1).Value = "Interpretación del resultado";
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 2)
                .Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            hoja.Cell(fila, 2).Value = InterpretarEnmienda(enmienda);
            hoja.Cell(fila, 2).Style.Alignment.WrapText = true;
            fila++;

            hoja.Column(1).Width = 38;
            hoja.Column(2).Width = 70;
            hoja.SheetView.FreezeRows(1);
        }

        private static void CrearFertilizacionMixta(
            XLWorkbook libro,
            AnalisisReporteFertilizacionMixta mixta)
        {
            IXLWorksheet hoja = libro.Worksheets.Add("Fertilización mixta");

            hoja.Cell("A1").Value = "FERTILIZACIÓN MIXTA";
            hoja.Range("A1:L1").Merge();
            EstiloTitulo(hoja.Range("A1:L1"));

            hoja.Cell("A3").Value = "Observación";
            hoja.Cell("A3").Style.Font.Bold = true;
            hoja.Cell("B3").Value = mixta.Observacion;
            hoja.Cell("B3").Style.Alignment.WrapText = true;

            int filaFuentes = 5;
            CrearEncabezado(
                hoja,
                filaFuentes,
                new[] { "Fuente", "Cantidad (qq)", "Precio por QQ", "Costo" });
            int fila = filaFuentes + 1;

            foreach (AnalisisReporteMixtaFuente item in mixta.Fuentes)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.CantidadQq;
                hoja.Cell(fila, 3).Value = item.PrecioPorQq;
                hoja.Cell(fila, 4).Value = item.Costo;
                hoja.Cell(fila, 2).Style.NumberFormat.Format = "0.0000";
                hoja.Range(fila, 3, fila, 4)
                    .Style.NumberFormat.Format = "C$ #,##0.00";
                hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml(AmarilloSuave);
                fila++;
            }

            hoja.Cell(fila, 1).Value = "Total";
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = mixta.Fuentes.Sum(x => x.CantidadQq);
            hoja.Cell(fila, 4).Value = mixta.Fuentes.Sum(x => x.Costo);
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "C$ #,##0.00";
            hoja.Range(fila, 1, fila, 4)
                .Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);

            FinalizarTabla(hoja, filaFuentes, fila, 4);

            AgregarTituloBloque(
                hoja,
                ref fila,
                "RESULTADO DE FERTILIZACIÓN MIXTA",
                12);

            int filaDetalle = fila;
            string[] encabezados =
            {
                "Elemento",
                "Requerimiento original",
                "Aporte orgánico",
                "Diferencia",
                "Déficit",
                "Sobrante"
            };
            CrearEncabezado(hoja, filaDetalle, encabezados);
            fila = filaDetalle + 1;

            foreach (AnalisisReporteMixtaDetalle item in mixta.Detalles)
            {
                hoja.Cell(fila, 1).Value = item.Elemento;
                hoja.Cell(fila, 2).Value = item.RequerimientoOriginal;
                hoja.Cell(fila, 3).Value = item.AporteOrganico;
                hoja.Cell(fila, 4).Value = item.Diferencia;
                hoja.Cell(fila, 5).Value = item.Deficit;
                hoja.Cell(fila, 6).Value = item.Sobrante;
                hoja.Range(fila, 2, fila, 6).Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            FinalizarTabla(
                hoja,
                filaDetalle,
                fila - 1,
                encabezados.Length,
                aplicarFiltro: false);

            AgregarAportesMixta(hoja, mixta, ref fila);

            if (mixta.BalanceAjustado != null)
                AgregarBalanceAjustado(
                    hoja,
                    mixta.BalanceAjustado,
                    ref fila);

            if (mixta.ResumenEconomico != null)
                AgregarResumenEconomico(
                    hoja,
                    mixta.ResumenEconomico,
                    ref fila);

            hoja.SheetView.FreezeRows(filaFuentes);
        }

        private static void AgregarAportesMixta(
            IXLWorksheet hoja,
            AnalisisReporteFertilizacionMixta mixta,
            ref int fila)
        {
            if (mixta.AportesPorFuente.Count == 0)
                return;

            string[] encabezados =
            {
                "Fuente",
                "Elemento",
                "Cantidad (qq)",
                "Aporte por QQ",
                "Aporte total (lb)"
            };

            AgregarTituloBloque(
                hoja,
                ref fila,
                "APORTES POR FUENTE",
                12);

            int filaEncabezado = fila;
            CrearEncabezado(hoja, filaEncabezado, encabezados);
            fila++;

            foreach (AnalisisReporteMixtaAporteFuente item in
                     mixta.AportesPorFuente)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.Elemento;
                hoja.Cell(fila, 3).Value = item.CantidadQq;
                hoja.Cell(fila, 4).Value = item.AportePorQq;
                hoja.Cell(fila, 5).Value = item.AporteTotal;
                hoja.Range(fila, 3, fila, 5)
                    .Style.NumberFormat.Format = "0.0000";
                fila++;
            }

            FinalizarTabla(
                hoja,
                filaEncabezado,
                fila - 1,
                encabezados.Length,
                aplicarFiltro: false);
        }

        private static void AgregarBalanceAjustado(
            IXLWorksheet hoja,
            AnalisisReporteBalanceAjustado balance,
            ref int fila)
        {
            AgregarTituloBloque(
                hoja,
                ref fila,
                "BALANCE COMERCIAL AJUSTADO",
                12);

            AgregarDato(hoja, ref fila, "Nombre de fórmula", balance.NombreFormula);
            AgregarDato(hoja, ref fila, "Total libras", balance.TotalLibras);
            AgregarDato(hoja, ref fila, "Mezcla exacta (qq)", balance.MezclaTotalQq);
            AgregarDato(hoja, ref fila, "Total onzas", balance.TotalOnzas);
            AgregarDato(hoja, ref fila, "Dosis anual (oz/planta)", balance.DosisPlantaAnualOz);
            AgregarDato(hoja, ref fila, "Dosis por aplicación (oz/planta)", balance.DosisPlantaPorAplicacionOz);
            AgregarDato(hoja, ref fila, "Precio exacto de referencia", balance.PrecioExactoReferencia, moneda: true);
            AgregarDato(hoja, ref fila, "Costo comercial ajustado", balance.CostoRealCompra, moneda: true);
            AgregarDato(hoja, ref fila, "Costo por aplicación", balance.PrecioPorAplicacion, moneda: true);

            if (balance.FormulaComercial.Count > 0)
            {
                fila++;
                hoja.Cell(fila, 1).Value = "Fórmula comercial ajustada";
                hoja.Range(fila, 2, fila, 12).Merge();
                hoja.Cell(fila, 2).Value = string.Join(
                    " - ",
                    balance.FormulaComercial
                        .OrderBy(x => OrdenElemento(x.Key))
                        .Select(x => $"{x.Key} {x.Value:N2}"));
                ResaltarFormulaComercial(hoja, fila, 12);
                fila++;
            }

            fila++;
            int filaEncabezado = fila;
            string[] encabezados =
            {
                "Fuente",
                "Elemento",
                "Requerimiento original (lb)",
                "Aporte orgánico (lb)",
                "Requerimiento ajustado (lb)",
                "QQ originales",
                "QQ ajustados",
                "Reducción QQ",
                "Precio por QQ",
                "QQ a comprar",
                "Subtotal exacto",
                "Costo compra"
            };

            CrearEncabezado(hoja, filaEncabezado, encabezados);
            fila++;

            foreach (AnalisisReporteCompraAjustada item in balance.Detalles)
            {
                hoja.Cell(fila, 1).Value = item.Fuente;
                hoja.Cell(fila, 2).Value = item.Elemento;
                hoja.Cell(fila, 3).Value = item.RequerimientoOriginalLb;
                hoja.Cell(fila, 4).Value = item.AporteOrganicoLb;
                hoja.Cell(fila, 5).Value = item.RequerimientoAjustadoLb;
                hoja.Cell(fila, 6).Value = item.QuintalesOriginales;
                hoja.Cell(fila, 7).Value = item.QuintalesAjustados;
                hoja.Cell(fila, 8).Value = item.ReduccionQuintales;
                hoja.Cell(fila, 9).Value = item.PrecioPorQq;
                hoja.Cell(fila, 10).Value = item.QuintalesComprar;
                hoja.Cell(fila, 11).Value = item.SubtotalExacto;
                hoja.Cell(fila, 12).Value = item.CostoCompra;
                hoja.Range(fila, 3, fila, 8)
                    .Style.NumberFormat.Format = "0.0000";
                hoja.Cell(fila, 10).Style.NumberFormat.Format = "0";
                hoja.Range(fila, 9, fila, 9)
                    .Style.NumberFormat.Format = "C$ #,##0.00";
                hoja.Range(fila, 11, fila, 12)
                    .Style.NumberFormat.Format = "C$ #,##0.00";
                fila++;
            }

            FinalizarTabla(
                hoja,
                filaEncabezado,
                fila - 1,
                encabezados.Length,
                aplicarFiltro: false);
        }

        private static void AgregarResumenEconomico(
            IXLWorksheet hoja,
            AnalisisReporteResumenEconomico resumen,
            ref int fila)
        {
            AgregarTituloBloque(
                hoja,
                ref fila,
                "RESUMEN ECONÓMICO",
                12);

            int filaInicio = fila;
            AgregarDato(hoja, ref fila, "Costo comercial original", resumen.CostoComercialOriginal, moneda: true);
            AgregarDato(hoja, ref fila, "Costo fertilización mixta", resumen.CostoFertilizacionMixta, moneda: true);
            AgregarDato(hoja, ref fila, "Costo comercial ajustado", resumen.CostoComercialAjustado, moneda: true);
            AgregarDato(hoja, ref fila, "Costo total final", resumen.CostoTotalFinal, moneda: true);
            AgregarDato(
                hoja,
                ref fila,
                resumen.EsAhorro
                    ? "Ahorro frente al balance original"
                    : "Incremento frente al balance original",
                Math.Abs(resumen.DiferenciaEconomica),
                moneda: true);

            hoja.Range(filaInicio, 1, fila - 1, 2)
                .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            hoja.Range(filaInicio, 1, fila - 1, 2)
                .Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            hoja.Range(fila - 1, 1, fila - 1, 2)
                .Style.Fill.BackgroundColor = resumen.EsAhorro
                    ? XLColor.FromHtml(VerdeSuave)
                    : XLColor.FromHtml("#FDECEC");
        }

        private static void AgregarTituloBloque(
            IXLWorksheet hoja,
            ref int fila,
            string titulo,
            int ultimaColumna)
        {
            fila++;

            hoja.Cell(fila, 1).Value = titulo;
            IXLRange rango = hoja.Range(
                fila,
                1,
                fila,
                Math.Max(ultimaColumna, 2));

            rango.Merge();
            rango.Style.Fill.BackgroundColor = XLColor.FromHtml(VerdeSuave);
            rango.Style.Font.FontColor = XLColor.FromHtml(Verde);
            rango.Style.Font.Bold = true;
            rango.Style.Font.FontSize = 13;
            rango.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            rango.Style.Border.BottomBorderColor = XLColor.FromHtml(Verde);

            fila++;
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
            int ultimaColumna,
            bool aplicarFiltro = true)
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

                if (aplicarFiltro && ultimaFila > primeraFila)
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

        private static void ResaltarFormulaComercial(
            IXLWorksheet hoja,
            int fila,
            int ultimaColumna)
        {
            IXLRange rango = hoja.Range(fila, 1, fila, ultimaColumna);
            rango.Style.Fill.BackgroundColor =
                XLColor.FromHtml(AmarilloSuave);
            rango.Style.Font.Bold = true;
            rango.Style.Font.FontColor = XLColor.FromHtml(Cafe);
            rango.Style.Font.FontSize = 12;
            rango.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
            rango.Style.Alignment.WrapText = true;
            rango.Style.Border.OutsideBorder =
                XLBorderStyleValues.Medium;
            rango.Style.Border.OutsideBorderColor =
                XLColor.FromHtml(Cafe);
            hoja.Row(fila).Height = 28;
        }

        private static string InterpretarEnmienda(
            AnalisisReporteEnmienda enmienda)
        {
            if (enmienda.NecesidadEncaladoTonHa > 0)
            {
                return
                    $"El cálculo determinó una necesidad de " +
                    $"{enmienda.NecesidadEncaladoTonHa:N2} ton/ha de enmienda.";
            }

            if (enmienda.SaturacionActual >=
                enmienda.SaturacionDeseada)
            {
                return
                    $"El cálculo sí fue realizado. La saturación actual " +
                    $"({enmienda.SaturacionActual:N2}%) alcanza o supera la " +
                    $"deseada ({enmienda.SaturacionDeseada:N2}%); por eso la " +
                    "necesidad y la dosis resultan en cero.";
            }

            return
                "El cálculo fue realizado y no determinó una dosis positiva " +
                "con los parámetros configurados para la fuente seleccionada.";
        }

        private static void ConfigurarHoja(IXLWorksheet hoja)
        {
            int ultimaColumna = hoja.LastColumnUsed()?.ColumnNumber() ?? 1;

            hoja.ShowGridLines = false;
            hoja.PageSetup.PaperSize = XLPaperSize.LetterPaper;
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

        private static decimal ObtenerAporte(
            IReadOnlyDictionary<string, decimal> aportes,
            string simbolo)
        {
            foreach (KeyValuePair<string, decimal> aporte in aportes)
            {
                if (string.Equals(
                        aporte.Key.Trim(),
                        simbolo.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return aporte.Value;
                }
            }

            return 0;
        }

        private static int OrdenElemento(string simbolo)
        {
            return simbolo.Trim().ToUpperInvariant() switch
            {
                "N" => 1,
                "P" => 2,
                "K" => 3,
                "CA" => 4,
                "MG" => 5,
                "S" => 6,
                "FE" => 7,
                "MN" => 8,
                "ZN" => 9,
                "CU" => 10,
                "B" => 11,
                _ => 99
            };
        }
    }
}
