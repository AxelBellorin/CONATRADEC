using CONATRADEC.Models;
using System.Globalization;
using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Generador PDF sin dependencias externas.
    ///
    /// Produce un documento carta de texto, multipágina y compatible con los
    /// visores de Windows y Android. El contenido se obtiene del mismo
    /// GuardarTodoRequest almacenado localmente.
    /// </summary>
    public static class AnalisisPdfLocalService
    {
        private const int LineasPorPagina = 48;
        private const int MaximoCaracteres = 92;

        public static byte[] Generar(
            AnalisisReporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            List<string> lineas =
                ConstruirLineas(reporte);

            List<List<string>> paginas =
                Paginar(lineas);

            return ConstruirPdf(paginas);
        }

        private static List<string> ConstruirLineas(
            AnalisisReporte reporte)
        {
            var lineas = new List<string>
            {
                "CONATRADEC - REPORTE DE ANALISIS DE SUELO",
                "Generado localmente en el dispositivo",
                string.Empty,
                $"Identificador: {reporte.Identificador}",
                $"Fecha de laboratorio: {reporte.FechaAnalisis}",
                $"Laboratorio: {reporte.Laboratorio}",
                $"Cliente: {reporte.Cliente}",
                $"Terreno: {reporte.Terreno}",
                $"Cultivo: {reporte.TipoCultivo}",
                $"Tipo de analisis: {reporte.TipoAnalisis}",
                $"Produccion: {N(reporte.ProduccionQqOro)} qq oro",
                $"Tamano de finca: {N(reporte.TamanoFincaMz)} mz",
                $"pH: {N(reporte.Ph)}",
                $"Materia organica: {N(reporte.MateriaOrganica)} {reporte.UnidadMateriaOrganica}",
                $"Acidez total: {N(reporte.AcidezTotal)}",
                string.Empty,
                "VALORES DE LABORATORIO"
            };

            foreach (AnalisisReporteValorLaboratorio item
                     in reporte.ValoresLaboratorio)
            {
                lineas.Add(
                    $"- {item.Elemento}: {N(item.Cantidad)} {item.Unidad}");
            }

            lineas.Add(string.Empty);
            lineas.Add("REQUERIMIENTO ANUAL");

            foreach (AnalisisReporteRequerimiento item
                     in reporte.Requerimientos)
            {
                lineas.Add(
                    $"- {item.Elemento}: convertido {N(item.CantidadConvertidaLbMz)} lb/Mz; " +
                    $"requerimiento {N(item.RequerimientoLbMz)} {item.UnidadResultado}; " +
                    $"clasificacion {item.Clasificacion}");

                if (!string.IsNullOrWhiteSpace(item.Observacion))
                    lineas.Add($"  {item.Observacion}");
            }

            if (reporte.Balance != null)
            {
                AnalisisReporteBalance balance =
                    reporte.Balance;

                lineas.Add(string.Empty);
                lineas.Add("BALANCE DE FORMULA");
                lineas.Add(
                    $"Formula: {balance.NombreFormula}");
                lineas.Add(
                    $"Mezcla: {N(balance.TotalLibras)} lb / {N(balance.MezclaTotalQq)} qq");
                lineas.Add(
                    $"Dosis por planta: {N(balance.DosisPlantaAnualOz)} oz anual; " +
                    $"{N(balance.DosisPlantaPorAplicacionOz)} oz por aplicacion");
                lineas.Add(
                    $"Costo exacto de referencia: C$ {N(balance.PrecioExactoReferencia)}");
                lineas.Add(
                    $"Costo real de compra: C$ {N(balance.CostoRealCompra)}");
                lineas.Add(
                    $"Costo por aplicacion: C$ {N(balance.PrecioPorAplicacion)}");

                if (balance.FormulaComercial.Count > 0)
                {
                    lineas.Add(
                        "Formula comercial: " +
                        string.Join(
                            " - ",
                            balance.FormulaComercial
                                .Select(item =>
                                    $"{item.Key.ToUpperInvariant()} {N(item.Value)}")));
                }

                foreach (AnalisisReporteBalanceDetalle item
                         in balance.Detalles)
                {
                    lineas.Add(
                        $"- {item.Fuente} / {item.Elemento}: " +
                        $"{N(item.Libras)} lb, {N(item.QuintalesExactos)} qq exactos, " +
                        $"{N(item.QuintalesComprar)} qq a comprar, " +
                        $"C$ {N(item.CostoCompra)}");
                }
            }

            if (reporte.Enmienda != null)
            {
                AnalisisReporteEnmienda enmienda =
                    reporte.Enmienda;

                lineas.Add(string.Empty);
                lineas.Add("ENMIENDA CALCAREA");
                lineas.Add(
                    $"Fuente: {enmienda.Fuente}");
                lineas.Add(
                    $"CICE: {N(enmienda.Cice)}; saturacion actual: " +
                    $"{N(enmienda.SaturacionActual)}%; deseada: " +
                    $"{N(enmienda.SaturacionDeseada)}%; PRNT: {N(enmienda.Prnt)}%");
                lineas.Add(
                    $"Necesidad: {N(enmienda.NecesidadEncaladoTonHa)} ton/ha; " +
                    $"{N(enmienda.NecesidadEncaladoLbMz)} lb/Mz");
                lineas.Add(
                    $"Dosis: {N(enmienda.DosisPlantaAnualOz)} oz/planta anual; " +
                    $"{N(enmienda.DosisPlantaPorAplicacionOz)} oz/planta/aplicacion");
            }

            if (reporte.FertilizacionMixta != null)
            {
                AnalisisReporteFertilizacionMixta mixta =
                    reporte.FertilizacionMixta;

                lineas.Add(string.Empty);
                lineas.Add("FERTILIZACION MIXTA");
                lineas.Add(
                    $"Observacion: {mixta.Observacion}");

                foreach (AnalisisReporteMixtaFuente item
                         in mixta.Fuentes)
                {
                    lineas.Add(
                        $"- Fuente {item.Fuente}: {N(item.CantidadQq)} qq; " +
                        $"C$ {N(item.Costo)}");
                }

                foreach (AnalisisReporteMixtaDetalle item
                         in mixta.Detalles)
                {
                    lineas.Add(
                        $"- {item.Elemento}: requerido {N(item.RequerimientoOriginal)}, " +
                        $"aporte {N(item.AporteOrganico)}, deficit {N(item.Deficit)}, " +
                        $"sobrante {N(item.Sobrante)}");
                }

                if (mixta.ResumenEconomico != null)
                {
                    AnalisisReporteResumenEconomico resumen =
                        mixta.ResumenEconomico;

                    lineas.Add(
                        $"Costo total final: C$ {N(resumen.CostoTotalFinal)}; " +
                        $"diferencia: C$ {N(resumen.DiferenciaEconomica)}");
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    reporte.RecomendacionGeneral))
            {
                lineas.Add(string.Empty);
                lineas.Add("RECOMENDACION GENERAL");
                lineas.Add(reporte.RecomendacionGeneral);
            }

            if (reporte.Observaciones.Count > 0)
            {
                lineas.Add(string.Empty);
                lineas.Add("OBSERVACIONES");

                foreach (string observacion
                         in reporte.Observaciones)
                {
                    if (!string.IsNullOrWhiteSpace(observacion))
                        lineas.Add("- " + observacion.Trim());
                }
            }

            lineas.Add(string.Empty);
            lineas.Add(
                "Motor local versionado. El reporte conserva los precios y parametros usados en el calculo.");

            return lineas
                .SelectMany(Envolver)
                .ToList();
        }

        private static IEnumerable<string> Envolver(
            string? texto)
        {
            string value =
                LimpiarTexto(texto);

            if (value.Length <= MaximoCaracteres)
            {
                yield return value;
                yield break;
            }

            string pendiente = value;

            while (pendiente.Length >
                   MaximoCaracteres)
            {
                int corte =
                    pendiente.LastIndexOf(
                        ' ',
                        MaximoCaracteres);

                if (corte <= 0)
                    corte = MaximoCaracteres;

                yield return pendiente[..corte]
                    .TrimEnd();

                pendiente =
                    pendiente[corte..]
                        .TrimStart();
            }

            yield return pendiente;
        }

        private static List<List<string>> Paginar(
            List<string> lineas)
        {
            var paginas =
                new List<List<string>>();

            for (
                int index = 0;
                index < lineas.Count;
                index += LineasPorPagina)
            {
                paginas.Add(
                    lineas
                        .Skip(index)
                        .Take(LineasPorPagina)
                        .ToList());
            }

            if (paginas.Count == 0)
                paginas.Add(new List<string>());

            return paginas;
        }

        private static byte[] ConstruirPdf(
            List<List<string>> paginas)
        {
            int totalPaginas =
                paginas.Count;

            int catalogId = 1;
            int pagesId = 2;
            int fontId = 3;
            int firstPageId = 4;

            var objects =
                new Dictionary<int, byte[]>();

            var kids =
                new List<int>();

            for (
                int pageIndex = 0;
                pageIndex < totalPaginas;
                pageIndex++)
            {
                int pageId =
                    firstPageId +
                    pageIndex * 2;

                int contentId =
                    pageId + 1;

                kids.Add(pageId);

                string stream =
                    ConstruirStreamPagina(
                        paginas[pageIndex],
                        pageIndex + 1,
                        totalPaginas);

                byte[] streamBytes =
                    Encoding.Latin1.GetBytes(stream);

                objects[contentId] =
                    Encoding.Latin1.GetBytes(
                        $"<< /Length {streamBytes.Length} >>\nstream\n" +
                        stream +
                        "\nendstream");

                objects[pageId] =
                    Encoding.ASCII.GetBytes(
                        $"<< /Type /Page /Parent {pagesId} 0 R " +
                        "/MediaBox [0 0 612 792] " +
                        $"/Resources << /Font << /F1 {fontId} 0 R >> >> " +
                        $"/Contents {contentId} 0 R >>");
            }

            objects[catalogId] =
                Encoding.ASCII.GetBytes(
                    $"<< /Type /Catalog /Pages {pagesId} 0 R >>");

            objects[pagesId] =
                Encoding.ASCII.GetBytes(
                    $"<< /Type /Pages /Kids [" +
                    string.Join(
                        " ",
                        kids.Select(id =>
                            $"{id} 0 R")) +
                    $"] /Count {kids.Count} >>");

            objects[fontId] =
                Encoding.ASCII.GetBytes(
                    "<< /Type /Font /Subtype /Type1 " +
                    "/BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");

            int maxObject =
                objects.Keys.Max();

            using var output =
                new MemoryStream();

            WriteAscii(output, "%PDF-1.4\n");
            output.Write(
                new byte[]
                {
                    (byte)'%',
                    0xE2,
                    0xE3,
                    0xCF,
                    0xD3,
                    (byte)'\n'
                });

            var offsets =
                new long[maxObject + 1];

            for (
                int id = 1;
                id <= maxObject;
                id++)
            {
                offsets[id] =
                    output.Position;

                WriteAscii(
                    output,
                    $"{id} 0 obj\n");

                output.Write(objects[id]);

                WriteAscii(
                    output,
                    "\nendobj\n");
            }

            long xref =
                output.Position;

            WriteAscii(
                output,
                $"xref\n0 {maxObject + 1}\n");

            WriteAscii(
                output,
                "0000000000 65535 f \n");

            for (
                int id = 1;
                id <= maxObject;
                id++)
            {
                WriteAscii(
                    output,
                    $"{offsets[id]:0000000000} 00000 n \n");
            }

            WriteAscii(
                output,
                "trailer\n" +
                $"<< /Size {maxObject + 1} /Root {catalogId} 0 R >>\n" +
                "startxref\n" +
                $"{xref}\n" +
                "%%EOF");

            return output.ToArray();
        }

        private static string ConstruirStreamPagina(
            List<string> lineas,
            int pagina,
            int totalPaginas)
        {
            var builder =
                new StringBuilder();

            builder.AppendLine("BT");
            builder.AppendLine("/F1 10 Tf");
            builder.AppendLine("13 TL");
            builder.AppendLine("48 748 Td");

            foreach (string linea in lineas)
            {
                builder.Append('(');
                builder.Append(EscapePdf(linea));
                builder.AppendLine(") Tj");
                builder.AppendLine("T*");
            }

            builder.AppendLine("ET");
            builder.AppendLine("BT");
            builder.AppendLine("/F1 8 Tf");
            builder.AppendLine("48 28 Td");
            builder.Append('(');
            builder.Append(
                EscapePdf(
                    $"Pagina {pagina} de {totalPaginas}"));
            builder.AppendLine(") Tj");
            builder.AppendLine("ET");

            return builder.ToString()
                .Replace("\r\n", "\n");
        }

        private static string EscapePdf(
            string? value) =>
            LimpiarTexto(value)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

        private static string LimpiarTexto(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("–", "-")
                .Replace("—", "-")
                .Replace("•", "-")
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace("’", "'")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private static string N(
            decimal? value) =>
            value.HasValue
                ? value.Value.ToString(
                    "N2",
                    CultureInfo.InvariantCulture)
                : "N/D";

        private static void WriteAscii(
            Stream stream,
            string value)
        {
            byte[] bytes =
                Encoding.ASCII.GetBytes(value);

            stream.Write(bytes);
        }
    }
}
