using CONATRADEC.Models;
using System.Globalization;
using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Generador PDF profesional sin dependencias nativas ni llamadas a API.
    ///
    /// Replica la estructura visual del reporte online:
    /// - tamaño carta;
    /// - encabezado institucional verde;
    /// - secciones y tablas;
    /// - fórmula comercial destacada;
    /// - costos exactos y reales;
    /// - pie de página numerado.
    ///
    /// Utiliza fuentes PDF estándar para funcionar en Windows y Android.
    /// </summary>
    public static class AnalisisPdfLocalService
    {
        private const double PageWidth = 612;
        private const double PageHeight = 792;
        private const double Margin = 28;
        private const double ContentWidth = PageWidth - Margin * 2;

        private static readonly PdfColor Verde =
            PdfColor.FromHex("#3B655B");
        private static readonly PdfColor Cafe =
            PdfColor.FromHex("#9B552C");
        private static readonly PdfColor AmarilloSuave =
            PdfColor.FromHex("#FFF8D8");
        private static readonly PdfColor VerdeSuave =
            PdfColor.FromHex("#EEF5F2");
        private static readonly PdfColor GrisBorde =
            PdfColor.FromHex("#D1D5DB");
        private static readonly PdfColor GrisFondo =
            PdfColor.FromHex("#F9FAFB");
        private static readonly PdfColor GrisTexto =
            PdfColor.FromHex("#4B5563");
        private static readonly PdfColor Blanco =
            new(1, 1, 1);
        private static readonly PdfColor Negro =
            PdfColor.FromHex("#1F2937");

        private static readonly CultureInfo Cultura =
            CultureInfo.GetCultureInfo("es-NI");

        public static byte[] Generar(AnalisisReporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            var document = new PdfReportDocument();
            var writer = new ReportWriter(document, reporte);

            writer.Begin();
            ComponerDatosGenerales(writer, reporte);
            ComponerValoresLaboratorio(writer, reporte);
            ComponerRequerimiento(writer, reporte);

            if (reporte.Balance != null)
                ComponerBalance(writer, reporte.Balance);

            if (reporte.Enmienda != null)
                ComponerEnmienda(writer, reporte.Enmienda);

            if (reporte.FertilizacionMixta != null)
            {
                ComponerFertilizacionMixta(
                    writer,
                    reporte.FertilizacionMixta);
            }

            writer.Finish();
            return document.Build();
        }

        private static void ComponerDatosGenerales(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle("Datos generales");

            var rows = new List<(string, string, string, string)>
            {
                ("Cliente", ValorO(reporte.Cliente),
                 "Terreno", ValorO(reporte.Terreno)),
                ("Fecha del análisis", ValorO(reporte.FechaAnalisis),
                 "Laboratorio", ValorO(reporte.Laboratorio)),
                ("Cultivo", ValorO(reporte.TipoCultivo),
                 "Tipo de análisis", ValorO(reporte.TipoAnalisis)),
                ("Producción", $"{N(reporte.ProduccionQqOro)} qq oro",
                 "Tamaño", $"{N(reporte.TamanoFincaMz)} mz"),
                ("pH", N(reporte.Ph),
                 "Acidez total", N(reporte.AcidezTotal)),
                ("Materia orgánica",
                 N(reporte.MateriaOrganica,
                   reporte.UnidadMateriaOrganica),
                 "Responsable", ValorO(reporte.Responsable))
            };

            writer.KeyValueGrid(rows);
        }

        private static void ComponerValoresLaboratorio(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle(
                "Valores originales del laboratorio");

            var rows = reporte.ValoresLaboratorio
                .Select(item => new[]
                {
                    ValorO(item.Elemento),
                    N(item.Cantidad, 4),
                    ValorO(item.Unidad)
                })
                .ToList();

            writer.Table(
                new[] { "Elemento", "Cantidad", "Unidad" },
                new[] { 0.50, 0.25, 0.25 },
                rows,
                numericColumns: new HashSet<int> { 1 });
        }

        private static void ComponerRequerimiento(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle("Requerimiento anual");

            var rows = reporte.Requerimientos
                .Select(item => new[]
                {
                    ValorO(item.Elemento),
                    N(item.CantidadIngresada, 4),
                    item.RequerimientoLbMz.HasValue
                        ? $"{N(item.RequerimientoLbMz)} " +
                          ValorO(item.UnidadResultado, "lb/Mz")
                        : "-",
                    ValorO(item.Clasificacion, "-"),
                    ValorO(item.Observacion, "-")
                })
                .ToList();

            writer.Table(
                new[]
                {
                    "Elemento",
                    "Ingresado",
                    "Requerimiento",
                    "Clasificación",
                    "Observación"
                },
                new[] { 0.17, 0.13, 0.18, 0.17, 0.35 },
                rows,
                numericColumns: new HashSet<int> { 1 });

            if (!string.IsNullOrWhiteSpace(
                    reporte.RecomendacionGeneral))
            {
                writer.HighlightBox(
                    "Recomendación general",
                    reporte.RecomendacionGeneral,
                    VerdeSuave,
                    Verde);
            }

            if (reporte.Observaciones.Count > 0)
            {
                string observaciones = string.Join(
                    " · ",
                    reporte.Observaciones
                        .Where(item =>
                            !string.IsNullOrWhiteSpace(item)));

                if (!string.IsNullOrWhiteSpace(observaciones))
                {
                    writer.Paragraph(
                        "Observaciones: " + observaciones,
                        boldPrefixLength: 14);
                }
            }
        }

        private static void ComponerBalance(
            ReportWriter writer,
            AnalisisReporteBalance balance)
        {
            writer.SectionTitle("Balance de fórmula");

            var summary = new List<string>
            {
                ValorO(balance.NombreFormula, "Fórmula nutricional"),
                $"Mezcla exacta: {N(balance.MezclaTotalQq, 3)} qq   ·   " +
                $"Aplicaciones: {balance.TotalAplicaciones}   ·   " +
                $"Dosis/planta/aplicación: " +
                $"{N(balance.DosisPlantaPorAplicacionOz)} oz",
                $"Costo real de compra: C$ {N(balance.CostoRealCompra)}   ·   " +
                $"Precio exacto de referencia: C$ " +
                N(balance.PrecioExactoReferencia)
            };

            writer.SummaryBox(summary, GrisFondo, Verde);

            if (balance.FormulaComercial.Count > 0)
            {
                string formula = string.Join(
                    "  ·  ",
                    balance.FormulaComercial
                        .OrderBy(item => OrdenElemento(item.Key))
                        .Select(item =>
                            $"{item.Key.ToUpperInvariant()} " +
                            N(item.Value)));

                writer.HighlightBox(
                    "Fórmula comercial",
                    formula,
                    AmarilloSuave,
                    Cafe);
            }

            writer.Subtitle("Detalle de dosificación");

            var dosageRows = balance.Detalles
                .Select(item => new[]
                {
                    $"{ValorO(item.Fuente)} / {ValorO(item.Elemento)}",
                    N(item.RequerimientoLibras),
                    N(item.Libras),
                    N(item.LibrasPorAplicacion),
                    N(item.OnzasAnuales),
                    N(item.OnzasPorAplicacion)
                })
                .ToList();

            writer.Table(
                new[]
                {
                    "Fuente / elemento",
                    "Req. lb",
                    "Lb/año",
                    "Lb/aplic.",
                    "Oz/año",
                    "Oz/aplic."
                },
                new[] { 0.31, 0.13, 0.13, 0.14, 0.14, 0.15 },
                dosageRows,
                new HashSet<int> { 1, 2, 3, 4, 5 });

            writer.Subtitle("Detalle de compra");

            var purchaseRows = balance.Detalles
                .Select(item => new[]
                {
                    $"{ValorO(item.Fuente)} / {ValorO(item.Elemento)}",
                    N(item.QuintalesExactos, 3),
                    N(item.QuintalesComprar, 0),
                    $"C$ {N(item.PrecioPorQuintal)}",
                    $"C$ {N(item.SubtotalExacto)}",
                    $"C$ {N(item.CostoCompra)}"
                })
                .ToList();

            writer.Table(
                new[]
                {
                    "Fuente / elemento",
                    "QQ exactos",
                    "QQ compra",
                    "Precio/QQ",
                    "Subtotal exacto",
                    "Costo compra"
                },
                new[] { 0.28, 0.12, 0.12, 0.14, 0.17, 0.17 },
                purchaseRows,
                new HashSet<int> { 1, 2, 3, 4, 5 });

            List<string> symbols = balance.Detalles
                .SelectMany(item => item.Aportes.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(OrdenElemento)
                .ToList();

            if (symbols.Count > 0)
            {
                writer.Subtitle("Aportes por fuente");

                var headers = new List<string> { "Fuente" };
                headers.AddRange(symbols.Select(x => x.ToUpperInvariant()));

                var weights = new List<double> { 0.40 };
                double remaining = 0.60 / symbols.Count;
                weights.AddRange(symbols.Select(_ => remaining));

                var aporteRows = balance.Detalles
                    .Select(item =>
                    {
                        var row = new List<string>
                        {
                            ValorO(item.Fuente)
                        };

                        row.AddRange(symbols.Select(symbol =>
                            item.Aportes.TryGetValue(
                                symbol,
                                out decimal value)
                                    ? N(value)
                                    : "-"));

                        return row.ToArray();
                    })
                    .ToList();

                writer.Table(
                    headers.ToArray(),
                    weights.ToArray(),
                    aporteRows,
                    Enumerable.Range(1, symbols.Count)
                        .ToHashSet());
            }
        }

        private static void ComponerEnmienda(
            ReportWriter writer,
            AnalisisReporteEnmienda enmienda)
        {
            writer.SectionTitle("Enmienda calcárea");

            writer.SummaryBox(
                new[]
                {
                    $"Fuente: {ValorO(enmienda.Fuente)}",
                    $"CICE: {N(enmienda.Cice)}   ·   " +
                    $"Saturación actual: {N(enmienda.SaturacionActual)}%   ·   " +
                    $"Deseada: {N(enmienda.SaturacionDeseada)}%   ·   " +
                    $"PRNT: {N(enmienda.Prnt)}%",
                    $"Necesidad: {N(enmienda.NecesidadEncaladoTonHa)} ton/ha   ·   " +
                    $"{N(enmienda.NecesidadEncaladoLbMz)} lb/Mz",
                    $"Dosis: {N(enmienda.DosisPlantaAnualOz)} oz/planta/año   ·   " +
                    $"{N(enmienda.DosisPlantaPorAplicacionOz)} oz/planta/aplicación"
                },
                VerdeSuave,
                Verde);

            writer.Table(
                new[]
                {
                    "pH",
                    "Ca",
                    "Mg",
                    "K",
                    "Acidez",
                    "Kg/ha",
                    "Lb/ha"
                },
                new[] { 0.11, 0.11, 0.11, 0.11, 0.14, 0.21, 0.21 },
                new List<string[]>
                {
                    new[]
                    {
                        N(enmienda.Ph),
                        N(enmienda.Calcio),
                        N(enmienda.Magnesio),
                        N(enmienda.Potasio),
                        N(enmienda.AcidezTotal),
                        N(enmienda.NecesidadEncaladoKgHa),
                        N(enmienda.NecesidadEncaladoLbHa)
                    }
                },
                Enumerable.Range(0, 7).ToHashSet());
        }

        private static void ComponerFertilizacionMixta(
            ReportWriter writer,
            AnalisisReporteFertilizacionMixta mixta)
        {
            writer.SectionTitle("Fertilización mixta");

            writer.SummaryBox(
                new[]
                {
                    mixta.EsComplementoBalance
                        ? "Modalidad: complemento del balance comercial"
                        : "Modalidad: fertilización mixta independiente",
                    string.IsNullOrWhiteSpace(mixta.Observacion)
                        ? "Sin observación adicional."
                        : mixta.Observacion
                },
                GrisFondo,
                Verde);

            writer.Subtitle("Fuentes utilizadas");

            writer.Table(
                new[]
                {
                    "Fuente",
                    "Cantidad (qq)",
                    "Precio/QQ",
                    "Costo"
                },
                new[] { 0.46, 0.18, 0.18, 0.18 },
                mixta.Fuentes.Select(item => new[]
                {
                    ValorO(item.Fuente),
                    N(item.CantidadQq),
                    $"C$ {N(item.PrecioPorQq)}",
                    $"C$ {N(item.Costo)}"
                }).ToList(),
                new HashSet<int> { 1, 2, 3 });

            writer.Subtitle("Resultado por elemento");

            writer.Table(
                new[]
                {
                    "Elemento",
                    "Requerimiento",
                    "Aporte orgánico",
                    "Diferencia",
                    "Déficit",
                    "Sobrante"
                },
                new[] { 0.21, 0.17, 0.17, 0.15, 0.15, 0.15 },
                mixta.Detalles.Select(item => new[]
                {
                    ValorO(item.Elemento),
                    N(item.RequerimientoOriginal),
                    N(item.AporteOrganico),
                    N(item.Diferencia),
                    N(item.Deficit),
                    N(item.Sobrante)
                }).ToList(),
                new HashSet<int> { 1, 2, 3, 4, 5 });

            if (mixta.AportesPorFuente.Count > 0)
            {
                writer.Subtitle("Aportes por fuente");

                writer.Table(
                    new[]
                    {
                        "Fuente",
                        "Elemento",
                        "Cantidad qq",
                        "Aporte/qq",
                        "Aporte total"
                    },
                    new[] { 0.32, 0.18, 0.16, 0.17, 0.17 },
                    mixta.AportesPorFuente.Select(item => new[]
                    {
                        ValorO(item.Fuente),
                        ValorO(item.Elemento),
                        N(item.CantidadQq),
                        N(item.AportePorQq),
                        N(item.AporteTotal)
                    }).ToList(),
                    new HashSet<int> { 2, 3, 4 });
            }

            if (mixta.BalanceAjustado != null)
                ComponerBalanceAjustado(writer, mixta.BalanceAjustado);

            if (mixta.ResumenEconomico != null)
            {
                AnalisisReporteResumenEconomico resumen =
                    mixta.ResumenEconomico;

                writer.HighlightBox(
                    "Resumen económico",
                    $"Comercial original: C$ {N(resumen.CostoComercialOriginal)}   ·   " +
                    $"Mixta: C$ {N(resumen.CostoFertilizacionMixta)}   ·   " +
                    $"Comercial ajustado: C$ {N(resumen.CostoComercialAjustado)}\n" +
                    $"Costo total final: C$ {N(resumen.CostoTotalFinal)}   ·   " +
                    $"{(resumen.EsAhorro ? "Ahorro" : "Diferencia")}: " +
                    $"C$ {N(Math.Abs(resumen.DiferenciaEconomica))}",
                    AmarilloSuave,
                    Cafe);
            }
        }

        private static void ComponerBalanceAjustado(
            ReportWriter writer,
            AnalisisReporteBalanceAjustado balance)
        {
            writer.Subtitle("Balance comercial ajustado");

            if (balance.FormulaComercial.Count > 0)
            {
                writer.HighlightBox(
                    "Fórmula comercial ajustada",
                    string.Join(
                        "  ·  ",
                        balance.FormulaComercial
                            .OrderBy(item => OrdenElemento(item.Key))
                            .Select(item =>
                                $"{item.Key.ToUpperInvariant()} " +
                                N(item.Value))),
                    AmarilloSuave,
                    Cafe);
            }

            writer.Table(
                new[]
                {
                    "Fuente / elemento",
                    "Req. original",
                    "Aporte orgánico",
                    "Req. ajustado",
                    "QQ compra",
                    "Costo compra"
                },
                new[] { 0.29, 0.14, 0.15, 0.14, 0.13, 0.15 },
                balance.Detalles.Select(item => new[]
                {
                    $"{ValorO(item.Fuente)} / {ValorO(item.Elemento)}",
                    N(item.RequerimientoOriginalLb),
                    N(item.AporteOrganicoLb),
                    N(item.RequerimientoAjustadoLb),
                    N(item.QuintalesComprar, 0),
                    $"C$ {N(item.CostoCompra)}"
                }).ToList(),
                new HashSet<int> { 1, 2, 3, 4, 5 });
        }

        private static string N(
            decimal? value,
            int decimals = 2)
        {
            if (!value.HasValue)
                return "-";

            return value.Value.ToString(
                "N" + decimals,
                Cultura);
        }

        private static string N(
            decimal value,
            int decimals = 2) =>
            value.ToString("N" + decimals, Cultura);

        private static string N(
            decimal? value,
            string? unit)
        {
            string number = N(value);
            return number == "-" ||
                   string.IsNullOrWhiteSpace(unit)
                ? number
                : $"{number} {unit.Trim()}";
        }

        private static string ValorO(
            string? value,
            string fallback = "No disponible") =>
            string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();

        private static int OrdenElemento(string? value) =>
            (value ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "N" => 1,
                "P" => 2,
                "K" => 3,
                "CA" => 4,
                "MG" => 5,
                _ => 99
            };

        private sealed class ReportWriter
        {
            private readonly PdfReportDocument document;
            private readonly AnalisisReporte reporte;

            private PdfPage page = null!;
            private double y;

            public ReportWriter(
                PdfReportDocument document,
                AnalisisReporte reporte)
            {
                this.document = document;
                this.reporte = reporte;
            }

            public void Begin() => NewPage();

            public void Finish()
            {
            }

            public void SectionTitle(string title)
            {
                Ensure(34);
                y -= 7;
                page.FillRect(Margin, y - 23, ContentWidth, 23, Verde);
                page.Text(
                    title,
                    Margin + 9,
                    y - 16,
                    11,
                    bold: true,
                    Blanco);
                y -= 31;
            }

            public void Subtitle(string text)
            {
                Ensure(23);
                y -= 4;
                page.Text(
                    text,
                    Margin,
                    y - 11,
                    9.5,
                    bold: true,
                    GrisTexto);
                y -= 20;
            }

            public void KeyValueGrid(
                IEnumerable<(string Key1, string Value1,
                    string Key2, string Value2)> rows)
            {
                const double keyWidth = 78;
                double pairWidth = ContentWidth / 2;

                foreach (var row in rows)
                {
                    double valueWidth = pairWidth - keyWidth;
                    int lines = Math.Max(
                        Wrap(row.Value1, valueWidth - 10, 8.5).Count,
                        Wrap(row.Value2, valueWidth - 10, 8.5).Count);

                    double height = Math.Max(25, 11 + lines * 10);
                    Ensure(height);

                    DrawCell(
                        Margin,
                        y - height,
                        keyWidth,
                        height,
                        row.Key1,
                        bold: true,
                        GrisFondo,
                        GrisTexto,
                        8.2);

                    DrawCell(
                        Margin + keyWidth,
                        y - height,
                        valueWidth,
                        height,
                        row.Value1,
                        false,
                        Blanco,
                        Negro,
                        8.5);

                    DrawCell(
                        Margin + pairWidth,
                        y - height,
                        keyWidth,
                        height,
                        row.Key2,
                        true,
                        GrisFondo,
                        GrisTexto,
                        8.2);

                    DrawCell(
                        Margin + pairWidth + keyWidth,
                        y - height,
                        valueWidth,
                        height,
                        row.Value2,
                        false,
                        Blanco,
                        Negro,
                        8.5);

                    y -= height;
                }

                y -= 7;
            }

            public void Table(
                string[] headers,
                double[] weights,
                List<string[]> rows,
                HashSet<int>? numericColumns = null)
            {
                if (headers.Length == 0 ||
                    headers.Length != weights.Length)
                {
                    return;
                }

                numericColumns ??= new HashSet<int>();
                DrawTableHeader(headers, weights);

                int rowIndex = 0;

                foreach (string[] row in rows)
                {
                    double height = CalculateRowHeight(
                        row,
                        weights,
                        7.5);

                    if (y - height < FooterLimit)
                    {
                        NewPage();
                        DrawTableHeader(headers, weights);
                    }

                    PdfColor background = rowIndex % 2 == 0
                        ? Blanco
                        : GrisFondo;

                    double x = Margin;

                    for (int index = 0;
                         index < headers.Length;
                         index++)
                    {
                        double width =
                            ContentWidth * weights[index];

                        string value = index < row.Length
                            ? row[index]
                            : string.Empty;

                        DrawCell(
                            x,
                            y - height,
                            width,
                            height,
                            value,
                            false,
                            background,
                            Negro,
                            7.5,
                            numericColumns.Contains(index)
                                ? TextAlignment.Right
                                : TextAlignment.Left);

                        x += width;
                    }

                    y -= height;
                    rowIndex++;
                }

                y -= 8;
            }

            public void SummaryBox(
                IEnumerable<string> lines,
                PdfColor background,
                PdfColor accent)
            {
                List<string> wrapped = lines
                    .SelectMany(line =>
                        Wrap(line, ContentWidth - 24, 8.5))
                    .ToList();

                double height = 15 + wrapped.Count * 11;
                Ensure(height + 7);

                page.FillRect(
                    Margin,
                    y - height,
                    ContentWidth,
                    height,
                    background);

                page.StrokeRect(
                    Margin,
                    y - height,
                    ContentWidth,
                    height,
                    GrisBorde,
                    0.8);

                page.FillRect(
                    Margin,
                    y - height,
                    4,
                    height,
                    accent);

                double textY = y - 14;

                for (int i = 0; i < wrapped.Count; i++)
                {
                    page.Text(
                        wrapped[i],
                        Margin + 12,
                        textY,
                        i == 0 ? 9.5 : 8.5,
                        bold: i == 0,
                        i == 0 ? accent : Negro);
                    textY -= 11;
                }

                y -= height + 8;
            }

            public void HighlightBox(
                string title,
                string body,
                PdfColor background,
                PdfColor accent)
            {
                List<string> lines = Wrap(
                    body,
                    ContentWidth - 24,
                    8.5);

                double height = 29 + lines.Count * 11;
                Ensure(height + 7);

                page.FillRect(
                    Margin,
                    y - height,
                    ContentWidth,
                    height,
                    background);
                page.StrokeRect(
                    Margin,
                    y - height,
                    ContentWidth,
                    height,
                    accent,
                    0.8);

                page.Text(
                    title,
                    Margin + 10,
                    y - 16,
                    9.5,
                    true,
                    accent);

                double textY = y - 30;
                foreach (string line in lines)
                {
                    page.Text(
                        line,
                        Margin + 10,
                        textY,
                        8.5,
                        false,
                        Negro);
                    textY -= 11;
                }

                y -= height + 8;
            }

            public void Paragraph(
                string text,
                int boldPrefixLength = 0)
            {
                List<string> lines = Wrap(
                    text,
                    ContentWidth,
                    8.5);

                double height = lines.Count * 11 + 5;
                Ensure(height);

                double textY = y - 10;
                foreach (string line in lines)
                {
                    page.Text(
                        line,
                        Margin,
                        textY,
                        8.5,
                        bold: false,
                        GrisTexto);
                    textY -= 11;
                }

                y -= height;
            }

            private void NewPage()
            {
                page = document.AddPage(
                    reporte.Identificador);

                page.FillRect(
                    Margin,
                    PageHeight - Margin - 68,
                    ContentWidth,
                    68,
                    Verde);

                page.Text(
                    "CONATRACAFÉ SOIL",
                    Margin + 14,
                    PageHeight - Margin - 26,
                    17,
                    true,
                    Blanco);

                page.Text(
                    "Reporte integral de análisis de suelo",
                    Margin + 14,
                    PageHeight - Margin - 44,
                    9.5,
                    false,
                    Blanco);

                string identifier = ValorO(
                    reporte.Identificador,
                    "Análisis de suelo");

                page.TextRight(
                    identifier,
                    PageWidth - Margin - 14,
                    PageHeight - Margin - 25,
                    10.5,
                    true,
                    Blanco);

                page.TextRight(
                    $"Generado localmente: {DateTime.Now:dd/MM/yyyy HH:mm}",
                    PageWidth - Margin - 14,
                    PageHeight - Margin - 43,
                    7.5,
                    false,
                    Blanco);

                y = PageHeight - Margin - 80;
            }

            private void DrawTableHeader(
                string[] headers,
                double[] weights)
            {
                double height = CalculateRowHeight(
                    headers,
                    weights,
                    7.5,
                    minimum: 25);

                Ensure(height);

                double x = Margin;
                for (int i = 0; i < headers.Length; i++)
                {
                    double width = ContentWidth * weights[i];

                    DrawCell(
                        x,
                        y - height,
                        width,
                        height,
                        headers[i],
                        true,
                        Verde,
                        Blanco,
                        7.5,
                        TextAlignment.Center);

                    x += width;
                }

                y -= height;
            }

            private double CalculateRowHeight(
                string[] values,
                double[] weights,
                double fontSize,
                double minimum = 23)
            {
                int maxLines = 1;

                for (int i = 0;
                     i < Math.Min(values.Length, weights.Length);
                     i++)
                {
                    double width =
                        ContentWidth * weights[i] - 8;

                    maxLines = Math.Max(
                        maxLines,
                        Wrap(values[i], width, fontSize).Count);
                }

                return Math.Max(minimum, 10 + maxLines * 9.2);
            }

            private void DrawCell(
                double x,
                double bottom,
                double width,
                double height,
                string text,
                bool bold,
                PdfColor background,
                PdfColor foreground,
                double fontSize,
                TextAlignment alignment = TextAlignment.Left)
            {
                page.FillRect(x, bottom, width, height, background);
                page.StrokeRect(
                    x,
                    bottom,
                    width,
                    height,
                    GrisBorde,
                    0.45);

                List<string> lines = Wrap(
                    text,
                    width - 8,
                    fontSize);

                double lineHeight = fontSize + 2;
                double top = bottom + height - 7 - fontSize;

                for (int index = 0;
                     index < lines.Count;
                     index++)
                {
                    double textY = top - index * lineHeight;
                    string line = lines[index];

                    if (alignment == TextAlignment.Right)
                    {
                        page.TextRight(
                            line,
                            x + width - 4,
                            textY,
                            fontSize,
                            bold,
                            foreground);
                    }
                    else if (alignment == TextAlignment.Center)
                    {
                        page.TextCenter(
                            line,
                            x + width / 2,
                            textY,
                            fontSize,
                            bold,
                            foreground);
                    }
                    else
                    {
                        page.Text(
                            line,
                            x + 4,
                            textY,
                            fontSize,
                            bold,
                            foreground);
                    }
                }
            }

            private void Ensure(double requiredHeight)
            {
                if (y - requiredHeight < FooterLimit)
                    NewPage();
            }

            private static double FooterLimit => Margin + 28;
        }

        private sealed class PdfReportDocument
        {
            private readonly List<PdfPage> pages = new();

            public PdfPage AddPage(string identifier)
            {
                var page = new PdfPage();
                pages.Add(page);
                return page;
            }

            public byte[] Build()
            {
                int totalPages = pages.Count;

                for (int i = 0; i < totalPages; i++)
                {
                    PdfPage page = pages[i];
                    page.Line(
                        Margin,
                        Margin + 15,
                        PageWidth - Margin,
                        Margin + 15,
                        GrisBorde,
                        0.7);

                    page.TextCenter(
                        $"CONATRACAFÉ SOIL · Página {i + 1} de {totalPages}",
                        PageWidth / 2,
                        Margin + 4,
                        7.5,
                        false,
                        GrisTexto);
                }

                return PdfBuilder.Build(pages);
            }
        }

        private sealed class PdfPage
        {
            private readonly StringBuilder commands = new();

            public string Commands => commands.ToString();

            public void FillRect(
                double x,
                double y,
                double width,
                double height,
                PdfColor color)
            {
                commands.AppendLine(
                    $"q {color.FillCommand} " +
                    $"{F(x)} {F(y)} {F(width)} {F(height)} re f Q");
            }

            public void StrokeRect(
                double x,
                double y,
                double width,
                double height,
                PdfColor color,
                double lineWidth)
            {
                commands.AppendLine(
                    $"q {color.StrokeCommand} {F(lineWidth)} w " +
                    $"{F(x)} {F(y)} {F(width)} {F(height)} re S Q");
            }

            public void Line(
                double x1,
                double y1,
                double x2,
                double y2,
                PdfColor color,
                double lineWidth)
            {
                commands.AppendLine(
                    $"q {color.StrokeCommand} {F(lineWidth)} w " +
                    $"{F(x1)} {F(y1)} m {F(x2)} {F(y2)} l S Q");
            }

            public void Text(
                string text,
                double x,
                double y,
                double size,
                bool bold,
                PdfColor color)
            {
                commands.AppendLine(
                    $"BT /{(bold ? "F2" : "F1")} {F(size)} Tf " +
                    $"{color.FillCommand} {F(x)} {F(y)} Td " +
                    $"({Escape(text)}) Tj ET");
            }

            public void TextRight(
                string text,
                double right,
                double y,
                double size,
                bool bold,
                PdfColor color)
            {
                double width = EstimateWidth(text, size, bold);
                Text(text, right - width, y, size, bold, color);
            }

            public void TextCenter(
                string text,
                double center,
                double y,
                double size,
                bool bold,
                PdfColor color)
            {
                double width = EstimateWidth(text, size, bold);
                Text(text, center - width / 2, y, size, bold, color);
            }
        }

        private static class PdfBuilder
        {
            public static byte[] Build(List<PdfPage> pages)
            {
                const int catalogId = 1;
                const int pagesId = 2;
                const int regularFontId = 3;
                const int boldFontId = 4;
                const int firstPageId = 5;

                var objects = new Dictionary<int, byte[]>();
                var pageIds = new List<int>();

                for (int index = 0; index < pages.Count; index++)
                {
                    int pageId = firstPageId + index * 2;
                    int contentId = pageId + 1;
                    pageIds.Add(pageId);

                    byte[] stream = Encoding.Latin1.GetBytes(
                        pages[index].Commands);

                    objects[contentId] = Encoding.Latin1.GetBytes(
                        $"<< /Length {stream.Length} >>\nstream\n" +
                        pages[index].Commands +
                        "\nendstream");

                    objects[pageId] = Encoding.ASCII.GetBytes(
                        $"<< /Type /Page /Parent {pagesId} 0 R " +
                        $"/MediaBox [0 0 {F(PageWidth)} {F(PageHeight)}] " +
                        $"/Resources << /Font << /F1 {regularFontId} 0 R " +
                        $"/F2 {boldFontId} 0 R >> >> " +
                        $"/Contents {contentId} 0 R >>");
                }

                objects[catalogId] = Encoding.ASCII.GetBytes(
                    $"<< /Type /Catalog /Pages {pagesId} 0 R >>");

                objects[pagesId] = Encoding.ASCII.GetBytes(
                    $"<< /Type /Pages /Kids [" +
                    string.Join(
                        " ",
                        pageIds.Select(id => $"{id} 0 R")) +
                    $"] /Count {pageIds.Count} >>");

                objects[regularFontId] = Encoding.ASCII.GetBytes(
                    "<< /Type /Font /Subtype /Type1 " +
                    "/BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");

                objects[boldFontId] = Encoding.ASCII.GetBytes(
                    "<< /Type /Font /Subtype /Type1 " +
                    "/BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

                return Serialize(objects, catalogId);
            }

            private static byte[] Serialize(
                Dictionary<int, byte[]> objects,
                int catalogId)
            {
                int maxObject = objects.Keys.Max();
                using var output = new MemoryStream();

                WriteAscii(output, "%PDF-1.4\n");
                output.Write(new byte[]
                {
                    (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n'
                });

                var offsets = new long[maxObject + 1];

                for (int id = 1; id <= maxObject; id++)
                {
                    offsets[id] = output.Position;
                    WriteAscii(output, $"{id} 0 obj\n");
                    output.Write(objects[id]);
                    WriteAscii(output, "\nendobj\n");
                }

                long xref = output.Position;
                WriteAscii(output, $"xref\n0 {maxObject + 1}\n");
                WriteAscii(output, "0000000000 65535 f \n");

                for (int id = 1; id <= maxObject; id++)
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

            private static void WriteAscii(
                Stream stream,
                string value)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value);
                stream.Write(bytes);
            }
        }

        private readonly record struct PdfColor(
            double R,
            double G,
            double B)
        {
            public string FillCommand =>
                $"{F(R)} {F(G)} {F(B)} rg";

            public string StrokeCommand =>
                $"{F(R)} {F(G)} {F(B)} RG";

            public static PdfColor FromHex(string hex)
            {
                string value = hex.Trim().TrimStart('#');

                return new PdfColor(
                    Convert.ToInt32(value[..2], 16) / 255d,
                    Convert.ToInt32(value.Substring(2, 2), 16) / 255d,
                    Convert.ToInt32(value.Substring(4, 2), 16) / 255d);
            }
        }

        private enum TextAlignment
        {
            Left,
            Center,
            Right
        }

        private static List<string> Wrap(
            string? text,
            double width,
            double fontSize)
        {
            string value = Sanitize(text);
            if (string.IsNullOrEmpty(value))
                return new List<string> { string.Empty };

            var result = new List<string>();

            foreach (string paragraph in value.Split('\n'))
            {
                string[] words = paragraph.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 0)
                {
                    result.Add(string.Empty);
                    continue;
                }

                var line = new StringBuilder();

                foreach (string word in words)
                {
                    string candidate = line.Length == 0
                        ? word
                        : line + " " + word;

                    if (EstimateWidth(candidate, fontSize, false) <= width)
                    {
                        line.Clear();
                        line.Append(candidate);
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        result.Add(line.ToString());
                        line.Clear();
                    }

                    if (EstimateWidth(word, fontSize, false) <= width)
                    {
                        line.Append(word);
                        continue;
                    }

                    int maxChars = Math.Max(
                        1,
                        (int)Math.Floor(width / (fontSize * 0.53)));

                    for (int i = 0; i < word.Length; i += maxChars)
                    {
                        string part = word.Substring(
                            i,
                            Math.Min(maxChars, word.Length - i));

                        if (i + maxChars < word.Length)
                            result.Add(part);
                        else
                            line.Append(part);
                    }
                }

                if (line.Length > 0)
                    result.Add(line.ToString());
            }

            return result.Count == 0
                ? new List<string> { string.Empty }
                : result;
        }

        private static double EstimateWidth(
            string? text,
            double fontSize,
            bool bold)
        {
            string value = Sanitize(text);
            double factor = bold ? 0.56 : 0.52;
            return value.Length * fontSize * factor;
        }

        private static string Escape(string? text) =>
            Sanitize(text)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

        private static string Sanitize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var builder = new StringBuilder();

            foreach (char character in text
                         .Replace("–", "-")
                         .Replace("—", "-")
                         .Replace("•", "·")
                         .Replace("“", "\"")
                         .Replace("”", "\"")
                         .Replace("’", "'")
                         .Replace("\r", string.Empty))
            {
                builder.Append(character <= 255
                    ? character
                    : '?');
            }

            return builder.ToString().Trim();
        }

        private static string F(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
