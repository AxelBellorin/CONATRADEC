using CONATRADEC.Models;
using System.Globalization;
using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Genera el PDF local sin llamadas al servidor ni dependencias nativas.
    ///
    /// La estructura replica el PDF online:
    /// - requerimiento anual de mayor a menor necesidad;
    /// - balance, compra y aportes;
    /// - enmienda calcárea con resultado final en lb/Mz;
    /// - fertilización mixta, balance ajustado y resumen económico.
    /// </summary>
    public static class AnalisisPdfLocalService
    {
        private const double PageWidth = 612;
        private const double PageHeight = 792;
        private const double Margin = 28;
        private const double ContentWidth =
            PageWidth - Margin * 2;

        private static readonly PdfColor Verde =
            PdfColor.FromHex("#3B655B");
        private static readonly PdfColor Cafe =
            PdfColor.FromHex("#9B552C");
        private static readonly PdfColor AmarilloSuave =
            PdfColor.FromHex("#FFF8D8");
        private static readonly PdfColor VerdeSuave =
            PdfColor.FromHex("#EEF5F2");
        private static readonly PdfColor RojoSuave =
            PdfColor.FromHex("#FDECEC");
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

        public static byte[] Generar(
            AnalisisReporte reporte)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            var document = new PdfReportDocument();
            var writer = new ReportWriter(
                document,
                reporte);

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

            return document.Build();
        }

        private static void ComponerDatosGenerales(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle("Datos generales");

            writer.KeyValueGrid(
                new List<
                    (string, string, string, string)>
                {
                    (
                        "Cliente",
                        ValorO(reporte.Cliente),
                        "Terreno",
                        ValorO(reporte.Terreno)
                    ),
                    (
                        "Fecha del análisis",
                        ValorO(reporte.FechaAnalisis),
                        "Laboratorio",
                        ValorO(reporte.Laboratorio)
                    ),
                    (
                        "Cultivo",
                        ValorO(reporte.TipoCultivo),
                        "Tipo de análisis",
                        ValorO(reporte.TipoAnalisis)
                    ),
                    (
                        "Producción",
                        $"{N(reporte.ProduccionQqOro)} qq oro",
                        "Tamaño",
                        $"{N(reporte.TamanoFincaMz)} mz"
                    ),
                    (
                        "pH",
                        N(reporte.Ph),
                        "Acidez total",
                        N(reporte.AcidezTotal)
                    ),
                    (
                        "Materia orgánica",
                        N(
                            reporte.MateriaOrganica,
                            reporte.UnidadMateriaOrganica),
                        "Responsable",
                        ValorO(reporte.Responsable)
                    )
                });
        }

        private static void ComponerValoresLaboratorio(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle(
                "Valores originales del laboratorio");

            writer.Table(
                new[]
                {
                    "Elemento",
                    "Cantidad",
                    "Unidad"
                },
                new[]
                {
                    0.50,
                    0.25,
                    0.25
                },
                reporte.ValoresLaboratorio
                    .Select(item => new[]
                    {
                        ValorO(item.Elemento),
                        N(item.Cantidad, 4),
                        ValorO(item.Unidad)
                    })
                    .ToList(),
                new HashSet<int> { 1 });
        }

        private static void ComponerRequerimiento(
            ReportWriter writer,
            AnalisisReporte reporte)
        {
            writer.SectionTitle("Requerimiento anual");

            List<string[]> rows =
                reporte.Requerimientos
                    .OrderByDescending(x =>
                        x.RequerimientoLbMz ?? 0)
                    .ThenBy(x => x.Elemento)
                    .Select(item => new[]
                    {
                        ValorO(item.Elemento),
                        N(item.CantidadIngresada, 4),
                        item.RequerimientoLbMz.HasValue
                            ? $"{N(item.RequerimientoLbMz)} lb/Mz"
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
                new[]
                {
                    0.17,
                    0.13,
                    0.18,
                    0.17,
                    0.35
                },
                rows,
                new HashSet<int> { 1 });

            if (!string.IsNullOrWhiteSpace(
                    reporte.RecomendacionGeneral))
            {
                writer.HighlightBox(
                    "Recomendación general",
                    reporte.RecomendacionGeneral,
                    VerdeSuave,
                    Verde);
            }

            string observaciones = string.Join(
                " · ",
                reporte.Observaciones
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                writer.Paragraph(
                    "Observaciones: " + observaciones);
            }
        }

        private static void ComponerBalance(
            ReportWriter writer,
            AnalisisReporteBalance balance)
        {
            writer.SectionTitle("Balance de fórmula");

            writer.SummaryBox(
                new[]
                {
                    ValorO(
                        balance.NombreFormula,
                        "Fórmula nutricional"),
                    $"Mezcla exacta: " +
                    $"{N(balance.MezclaTotalQq, 3)} qq  ·  " +
                    $"Aplicaciones: {balance.TotalAplicaciones}  ·  " +
                    $"Dosis/planta/aplicación: " +
                    $"{N(balance.DosisPlantaPorAplicacionOz)} oz",
                    $"Costo real de compra: " +
                    $"C$ {N(balance.CostoRealCompra)}  ·  " +
                    $"Precio exacto de referencia: " +
                    $"C$ {N(balance.PrecioExactoReferencia)}"
                },
                GrisFondo,
                Verde);

            if (balance.FormulaComercial.Count > 0)
            {
                writer.HighlightBox(
                    "Fórmula comercial",
                    string.Join(
                        "  ·  ",
                        balance.FormulaComercial
                            .OrderBy(x =>
                                OrdenElemento(x.Key))
                            .Select(x =>
                                $"{x.Key.ToUpperInvariant()} " +
                                N(x.Value))),
                    AmarilloSuave,
                    Cafe);
            }

            writer.Subtitle("Detalle de dosificación");

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
                new[]
                {
                    0.31,
                    0.13,
                    0.13,
                    0.14,
                    0.14,
                    0.15
                },
                balance.Detalles
                    .Select(item => new[]
                    {
                        $"{ValorO(item.Fuente)} / " +
                        ValorO(item.Elemento),
                        N(item.RequerimientoLibras),
                        N(item.Libras),
                        N(item.LibrasPorAplicacion),
                        N(item.OnzasAnuales),
                        N(item.OnzasPorAplicacion)
                    })
                    .ToList(),
                new HashSet<int>
                {
                    1, 2, 3, 4, 5
                });

            writer.Subtitle("Detalle de compra");

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
                new[]
                {
                    0.28,
                    0.12,
                    0.12,
                    0.14,
                    0.17,
                    0.17
                },
                balance.Detalles
                    .Select(item => new[]
                    {
                        $"{ValorO(item.Fuente)} / " +
                        ValorO(item.Elemento),
                        N(item.QuintalesExactos, 3),
                        N(item.QuintalesComprar, 0),
                        $"C$ {N(item.PrecioPorQuintal)}",
                        $"C$ {N(item.SubtotalExacto)}",
                        $"C$ {N(item.CostoCompra)}"
                    })
                    .ToList(),
                new HashSet<int>
                {
                    1, 2, 3, 4, 5
                });

            if (balance.Detalles.Any(x =>
                    x.Aportes.Count > 0))
            {
                writer.Subtitle(
                    "Aportes nutricionales por fuente");

                writer.Table(
                    new[]
                    {
                        "Fuente",
                        "Libras",
                        "QQ",
                        "Aportes"
                    },
                    new[]
                    {
                        0.28,
                        0.14,
                        0.14,
                        0.44
                    },
                    balance.Detalles
                        .Select(item => new[]
                        {
                            ValorO(item.Fuente),
                            N(item.Libras),
                            N(item.QuintalesExactos, 3),
                            TextoAportes(item.Aportes)
                        })
                        .ToList(),
                    new HashSet<int> { 1, 2 });
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
                    ValorO(
                        enmienda.Fuente,
                        "Fuente no especificada"),
                    $"{enmienda.TotalAplicaciones} aplicaciones  ·  " +
                    $"{enmienda.TotalPlantas:N0} plantas"
                },
                GrisFondo,
                Cafe);

            writer.KeyValueGrid(
                new List<
                    (string, string, string, string)>
                {
                    (
                        "pH",
                        N(enmienda.Ph),
                        "Acidez total",
                        N(enmienda.AcidezTotal)
                    ),
                    (
                        "Calcio",
                        N(enmienda.Calcio),
                        "Magnesio",
                        N(enmienda.Magnesio)
                    ),
                    (
                        "Potasio",
                        N(enmienda.Potasio),
                        "CICE",
                        N(enmienda.Cice)
                    ),
                    (
                        "Saturación actual",
                        $"{N(enmienda.SaturacionActual)}%",
                        "Saturación deseada",
                        $"{N(enmienda.SaturacionDeseada)}%"
                    ),
                    (
                        "PRNT",
                        $"{N(enmienda.Prnt)}%",
                        "Necesidad",
                        $"{N(enmienda.NecesidadEncaladoLbMz)} lb/Mz"
                    ),
                    (
                        "Dosis anual",
                        $"{N(enmienda.DosisPlantaAnualOz)} oz/planta",
                        "Por aplicación",
                        $"{N(enmienda.DosisPlantaPorAplicacionOz)} oz/planta"
                    ),
                    (
                        "Análisis",
                        ValorO(enmienda.NombreAnalisis),
                        "Unidad final",
                        "lb/Mz"
                    )
                });

            writer.HighlightBox(
                "Interpretación",
                InterpretarEnmienda(enmienda),
                VerdeSuave,
                Verde);
        }

        private static void ComponerFertilizacionMixta(
            ReportWriter writer,
            AnalisisReporteFertilizacionMixta mixta)
        {
            writer.SectionTitle("Fertilización mixta");

            if (!string.IsNullOrWhiteSpace(
                    mixta.Observacion))
            {
                writer.Paragraph(mixta.Observacion);
            }

            List<string[]> fuentes =
                mixta.Fuentes
                    .Select(item => new[]
                    {
                        ValorO(item.Fuente),
                        N(item.CantidadQq),
                        $"C$ {N(item.PrecioPorQq)}",
                        $"C$ {N(item.Costo)}"
                    })
                    .ToList();

            fuentes.Add(
                new[]
                {
                    "Total",
                    N(mixta.Fuentes.Sum(x => x.CantidadQq)),
                    string.Empty,
                    $"C$ {N(mixta.Fuentes.Sum(x => x.Costo))}"
                });

            writer.Table(
                new[]
                {
                    "Fuente utilizada",
                    "Cantidad (qq)",
                    "Precio/QQ",
                    "Costo"
                },
                new[]
                {
                    0.46,
                    0.18,
                    0.18,
                    0.18
                },
                fuentes,
                new HashSet<int> { 1, 2, 3 });

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
                new[]
                {
                    0.21,
                    0.17,
                    0.17,
                    0.15,
                    0.15,
                    0.15
                },
                mixta.Detalles
                    .Select(item => new[]
                    {
                        ValorO(item.Elemento),
                        N(item.RequerimientoOriginal),
                        N(item.AporteOrganico),
                        N(item.Diferencia),
                        N(item.Deficit),
                        N(item.Sobrante)
                    })
                    .ToList(),
                new HashSet<int>
                {
                    1, 2, 3, 4, 5
                });

            if (mixta.AportesPorFuente.Count > 0)
            {
                writer.Subtitle(
                    "Aportes de fertilización mixta por fuente");

                writer.Table(
                    new[]
                    {
                        "Fuente",
                        "Elemento",
                        "Cantidad (qq)",
                        "Aporte/QQ",
                        "Aporte total"
                    },
                    new[]
                    {
                        0.32,
                        0.18,
                        0.16,
                        0.17,
                        0.17
                    },
                    mixta.AportesPorFuente
                        .Select(item => new[]
                        {
                            ValorO(item.Fuente),
                            ValorO(item.Elemento),
                            N(item.CantidadQq),
                            N(item.AportePorQq),
                            N(item.AporteTotal)
                        })
                        .ToList(),
                    new HashSet<int> { 2, 3, 4 });
            }

            if (mixta.BalanceAjustado != null)
            {
                ComponerBalanceAjustado(
                    writer,
                    mixta.BalanceAjustado);
            }

            if (mixta.ResumenEconomico != null)
            {
                ComponerResumenEconomico(
                    writer,
                    mixta.ResumenEconomico);
            }
        }

        private static void ComponerBalanceAjustado(
            ReportWriter writer,
            AnalisisReporteBalanceAjustado balance)
        {
            writer.SectionTitle(
                "Balance comercial ajustado");

            writer.SummaryBox(
                new[]
                {
                    ValorO(
                        balance.NombreFormula,
                        "Balance ajustado"),
                    $"Mezcla exacta: " +
                    $"{N(balance.MezclaTotalQq, 3)} qq  ·  " +
                    $"Total: {N(balance.TotalLibras)} lb  ·  " +
                    $"Dosis/planta/aplicación: " +
                    $"{N(balance.DosisPlantaPorAplicacionOz)} oz",
                    $"Costo comercial ajustado: " +
                    $"C$ {N(balance.CostoRealCompra)}  ·  " +
                    $"Costo por aplicación: " +
                    $"C$ {N(balance.PrecioPorAplicacion)}"
                },
                GrisFondo,
                Verde);

            if (balance.FormulaComercial.Count > 0)
            {
                writer.HighlightBox(
                    "Fórmula comercial ajustada",
                    string.Join(
                        "  ·  ",
                        balance.FormulaComercial
                            .OrderBy(x =>
                                OrdenElemento(x.Key))
                            .Select(x =>
                                $"{x.Key.ToUpperInvariant()} " +
                                N(x.Value))),
                    AmarilloSuave,
                    Cafe);
            }

            writer.Subtitle("Ajuste de requerimientos");

            writer.Table(
                new[]
                {
                    "Fuente / elemento",
                    "Requerimiento original",
                    "Aporte orgánico",
                    "Requerimiento ajustado"
                },
                new[]
                {
                    0.34,
                    0.22,
                    0.21,
                    0.23
                },
                balance.Detalles
                    .Select(item => new[]
                    {
                        $"{ValorO(item.Fuente)} / " +
                        ValorO(item.Elemento),
                        N(item.RequerimientoOriginalLb),
                        N(item.AporteOrganicoLb),
                        N(item.RequerimientoAjustadoLb)
                    })
                    .ToList(),
                new HashSet<int> { 1, 2, 3 });

            writer.Subtitle("Compra comercial ajustada");

            writer.Table(
                new[]
                {
                    "Fuente",
                    "QQ original",
                    "QQ ajustado",
                    "Reducción",
                    "QQ compra",
                    "Precio/QQ",
                    "Costo compra"
                },
                new[]
                {
                    0.25,
                    0.12,
                    0.13,
                    0.12,
                    0.12,
                    0.13,
                    0.13
                },
                balance.Detalles
                    .Select(item => new[]
                    {
                        ValorO(item.Fuente),
                        N(item.QuintalesOriginales, 3),
                        N(item.QuintalesAjustados, 3),
                        N(item.ReduccionQuintales, 3),
                        N(item.QuintalesComprar, 0),
                        $"C$ {N(item.PrecioPorQq)}",
                        $"C$ {N(item.CostoCompra)}"
                    })
                    .ToList(),
                new HashSet<int>
                {
                    1, 2, 3, 4, 5, 6
                });
        }

        private static void ComponerResumenEconomico(
            ReportWriter writer,
            AnalisisReporteResumenEconomico resumen)
        {
            writer.SectionTitle("Resumen económico");

            writer.KeyValueGrid(
                new List<
                    (string, string, string, string)>
                {
                    (
                        "Costo comercial original",
                        $"C$ {N(resumen.CostoComercialOriginal)}",
                        "Costo fertilización mixta",
                        $"C$ {N(resumen.CostoFertilizacionMixta)}"
                    ),
                    (
                        "Costo comercial ajustado",
                        $"C$ {N(resumen.CostoComercialAjustado)}",
                        "Costo total final",
                        $"C$ {N(resumen.CostoTotalFinal)}"
                    ),
                    (
                        resumen.EsAhorro
                            ? "Ahorro"
                            : "Incremento",
                        $"C$ {N(Math.Abs(resumen.DiferenciaEconomica))}",
                        "Comparación",
                        resumen.EsAhorro
                            ? "Menor que el balance original"
                            : "Mayor que el balance original"
                    )
                },
                resumen.EsAhorro
                    ? VerdeSuave
                    : RojoSuave);
        }

        private static string InterpretarEnmienda(
            AnalisisReporteEnmienda enmienda)
        {
            if (enmienda.NecesidadEncaladoLbMz > 0)
            {
                return
                    $"El cálculo determinó una necesidad de " +
                    $"{N(enmienda.NecesidadEncaladoLbMz)} " +
                    "lb/Mz de enmienda.";
            }

            if (enmienda.SaturacionActual >=
                enmienda.SaturacionDeseada)
            {
                return
                    $"El cálculo sí fue realizado. La saturación actual " +
                    $"({N(enmienda.SaturacionActual)}%) alcanza o supera " +
                    $"la deseada ({N(enmienda.SaturacionDeseada)}%); " +
                    "por eso la necesidad y la dosis resultan en cero.";
            }

            return
                "El cálculo fue realizado y no determinó una dosis " +
                "positiva con los parámetros configurados para la " +
                "fuente seleccionada.";
        }

        private static string TextoAportes(
            IReadOnlyDictionary<string, decimal> aportes) =>
            aportes.Count == 0
                ? "-"
                : string.Join(
                    " · ",
                    aportes
                        .OrderBy(x =>
                            OrdenElemento(x.Key))
                        .Select(x =>
                            $"{x.Key}: {N(x.Value)}"));

        private static int OrdenElemento(
            string? valor) =>
            (valor ?? string.Empty)
                .Trim()
                .ToUpperInvariant() switch
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

        private static string N(
            decimal? valor,
            int decimales = 2)
        {
            if (!valor.HasValue)
                return "-";

            return valor.Value.ToString(
                "N" + decimales,
                Cultura);
        }

        private static string N(
            decimal valor,
            int decimales = 2) =>
            valor.ToString(
                "N" + decimales,
                Cultura);

        private static string N(
            decimal? valor,
            string? unidad)
        {
            string numero = N(valor);

            return numero == "-" ||
                   string.IsNullOrWhiteSpace(unidad)
                ? numero
                : $"{numero} {unidad.Trim()}";
        }

        private static string ValorO(
            string? valor,
            string alternativo = "No disponible") =>
            string.IsNullOrWhiteSpace(valor)
                ? alternativo
                : valor.Trim();

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

            public void SectionTitle(
                string titulo)
            {
                Ensure(34);
                y -= 7;

                page.FillRect(
                    Margin,
                    y - 23,
                    ContentWidth,
                    23,
                    Verde);

                page.Text(
                    titulo,
                    Margin + 9,
                    y - 16,
                    11,
                    true,
                    Blanco);

                y -= 31;
            }

            public void Subtitle(
                string texto)
            {
                Ensure(23);
                y -= 4;

                page.Text(
                    texto,
                    Margin,
                    y - 11,
                    9.5,
                    true,
                    GrisTexto);

                y -= 20;
            }

            public void KeyValueGrid(
                IEnumerable<(
                    string Key1,
                    string Value1,
                    string Key2,
                    string Value2)> rows,
                PdfColor? valueBackground = null)
            {
                const double keyWidth = 78;
                double pairWidth =
                    ContentWidth / 2;

                PdfColor fondoValor =
                    valueBackground ?? Blanco;

                foreach (var row in rows)
                {
                    double valueWidth =
                        pairWidth - keyWidth;

                    int lineas = Math.Max(
                        Wrap(
                            row.Value1,
                            valueWidth - 10,
                            8.5).Count,
                        Wrap(
                            row.Value2,
                            valueWidth - 10,
                            8.5).Count);

                    double alto = Math.Max(
                        25,
                        11 + lineas * 10);

                    Ensure(alto);

                    DrawCell(
                        Margin,
                        y - alto,
                        keyWidth,
                        alto,
                        row.Key1,
                        true,
                        GrisFondo,
                        GrisTexto,
                        8.2);

                    DrawCell(
                        Margin + keyWidth,
                        y - alto,
                        valueWidth,
                        alto,
                        row.Value1,
                        false,
                        fondoValor,
                        Negro,
                        8.5);

                    DrawCell(
                        Margin + pairWidth,
                        y - alto,
                        keyWidth,
                        alto,
                        row.Key2,
                        true,
                        GrisFondo,
                        GrisTexto,
                        8.2);

                    DrawCell(
                        Margin + pairWidth + keyWidth,
                        y - alto,
                        valueWidth,
                        alto,
                        row.Value2,
                        false,
                        fondoValor,
                        Negro,
                        8.5);

                    y -= alto;
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

                numericColumns ??=
                    new HashSet<int>();

                DrawTableHeader(
                    headers,
                    weights);

                int rowIndex = 0;

                foreach (string[] row in rows)
                {
                    double alto = CalculateRowHeight(
                        row,
                        weights,
                        7.5);

                    if (y - alto < FooterLimit)
                    {
                        NewPage();
                        DrawTableHeader(
                            headers,
                            weights);
                    }

                    PdfColor fondo = rowIndex % 2 == 0
                        ? Blanco
                        : GrisFondo;

                    double x = Margin;

                    for (int index = 0;
                         index < headers.Length;
                         index++)
                    {
                        double ancho =
                            ContentWidth * weights[index];

                        string valor = index < row.Length
                            ? row[index]
                            : string.Empty;

                        DrawCell(
                            x,
                            y - alto,
                            ancho,
                            alto,
                            valor,
                            false,
                            fondo,
                            Negro,
                            7.5,
                            numericColumns.Contains(index)
                                ? TextAlignment.Right
                                : TextAlignment.Left);

                        x += ancho;
                    }

                    y -= alto;
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
                        Wrap(
                            line,
                            ContentWidth - 24,
                            8.5))
                    .ToList();

                double alto =
                    15 + wrapped.Count * 11;

                Ensure(alto + 7);

                page.FillRect(
                    Margin,
                    y - alto,
                    ContentWidth,
                    alto,
                    background);

                page.StrokeRect(
                    Margin,
                    y - alto,
                    ContentWidth,
                    alto,
                    GrisBorde,
                    0.8);

                page.FillRect(
                    Margin,
                    y - alto,
                    4,
                    alto,
                    accent);

                double textY = y - 14;

                for (int i = 0;
                     i < wrapped.Count;
                     i++)
                {
                    page.Text(
                        wrapped[i],
                        Margin + 12,
                        textY,
                        i == 0 ? 9.5 : 8.5,
                        i == 0,
                        i == 0 ? accent : Negro);

                    textY -= 11;
                }

                y -= alto + 8;
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

                double alto =
                    29 + lines.Count * 11;

                Ensure(alto + 7);

                page.FillRect(
                    Margin,
                    y - alto,
                    ContentWidth,
                    alto,
                    background);

                page.StrokeRect(
                    Margin,
                    y - alto,
                    ContentWidth,
                    alto,
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

                y -= alto + 8;
            }

            public void Paragraph(
                string text)
            {
                List<string> lines = Wrap(
                    text,
                    ContentWidth,
                    8.5);

                double alto =
                    lines.Count * 11 + 5;

                Ensure(alto);

                double textY = y - 10;

                foreach (string line in lines)
                {
                    page.Text(
                        line,
                        Margin,
                        textY,
                        8.5,
                        false,
                        GrisTexto);

                    textY -= 11;
                }

                y -= alto;
            }

            private void NewPage()
            {
                page = document.AddPage();

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

                page.TextRight(
                    ValorO(
                        reporte.Identificador,
                        "Análisis de suelo"),
                    PageWidth - Margin - 14,
                    PageHeight - Margin - 25,
                    10.5,
                    true,
                    Blanco);

                page.TextRight(
                    $"Generado localmente: " +
                    $"{DateTime.Now:dd/MM/yyyy HH:mm}",
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
                double alto = CalculateRowHeight(
                    headers,
                    weights,
                    7.5,
                    25);

                Ensure(alto);

                double x = Margin;

                for (int i = 0;
                     i < headers.Length;
                     i++)
                {
                    double ancho =
                        ContentWidth * weights[i];

                    DrawCell(
                        x,
                        y - alto,
                        ancho,
                        alto,
                        headers[i],
                        true,
                        Verde,
                        Blanco,
                        7.5,
                        TextAlignment.Center);

                    x += ancho;
                }

                y -= alto;
            }

            private double CalculateRowHeight(
                string[] values,
                double[] weights,
                double fontSize,
                double minimum = 23)
            {
                int maxLines = 1;

                for (int i = 0;
                     i < Math.Min(
                         values.Length,
                         weights.Length);
                     i++)
                {
                    double width =
                        ContentWidth * weights[i] - 8;

                    maxLines = Math.Max(
                        maxLines,
                        Wrap(
                            values[i],
                            width,
                            fontSize).Count);
                }

                return Math.Max(
                    minimum,
                    10 + maxLines * 9.2);
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
                TextAlignment alignment =
                    TextAlignment.Left)
            {
                page.FillRect(
                    x,
                    bottom,
                    width,
                    height,
                    background);

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

                double lineHeight =
                    fontSize + 2;

                double top =
                    bottom + height - 7 - fontSize;

                for (int index = 0;
                     index < lines.Count;
                     index++)
                {
                    double textY =
                        top - index * lineHeight;

                    string line = lines[index];

                    switch (alignment)
                    {
                        case TextAlignment.Right:
                            page.TextRight(
                                line,
                                x + width - 4,
                                textY,
                                fontSize,
                                bold,
                                foreground);
                            break;

                        case TextAlignment.Center:
                            page.TextCenter(
                                line,
                                x + width / 2,
                                textY,
                                fontSize,
                                bold,
                                foreground);
                            break;

                        default:
                            page.Text(
                                line,
                                x + 4,
                                textY,
                                fontSize,
                                bold,
                                foreground);
                            break;
                    }
                }
            }

            private void Ensure(
                double requiredHeight)
            {
                if (y - requiredHeight < FooterLimit)
                    NewPage();
            }

            private static double FooterLimit =>
                Margin + 28;
        }

        private sealed class PdfReportDocument
        {
            private readonly List<PdfPage> pages =
                new();

            public PdfPage AddPage()
            {
                var page = new PdfPage();
                pages.Add(page);
                return page;
            }

            public byte[] Build()
            {
                int totalPages = pages.Count;

                for (int i = 0;
                     i < totalPages;
                     i++)
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
                        $"CONATRACAFÉ SOIL · Página " +
                        $"{i + 1} de {totalPages}",
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
            private readonly StringBuilder commands =
                new();

            public string Commands =>
                commands.ToString();

            public void FillRect(
                double x,
                double y,
                double width,
                double height,
                PdfColor color)
            {
                commands.AppendLine(
                    $"q {color.FillCommand} " +
                    $"{F(x)} {F(y)} {F(width)} {F(height)} " +
                    "re f Q");
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
                    $"q {color.StrokeCommand} " +
                    $"{F(lineWidth)} w " +
                    $"{F(x)} {F(y)} {F(width)} {F(height)} " +
                    "re S Q");
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
                    $"q {color.StrokeCommand} " +
                    $"{F(lineWidth)} w " +
                    $"{F(x1)} {F(y1)} m " +
                    $"{F(x2)} {F(y2)} l S Q");
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
                    $"BT /{(bold ? "F2" : "F1")} " +
                    $"{F(size)} Tf {color.FillCommand} " +
                    $"{F(x)} {F(y)} Td " +
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
                double width = EstimateWidth(
                    text,
                    size,
                    bold);

                Text(
                    text,
                    right - width,
                    y,
                    size,
                    bold,
                    color);
            }

            public void TextCenter(
                string text,
                double center,
                double y,
                double size,
                bool bold,
                PdfColor color)
            {
                double width = EstimateWidth(
                    text,
                    size,
                    bold);

                Text(
                    text,
                    center - width / 2,
                    y,
                    size,
                    bold,
                    color);
            }
        }

        private static class PdfBuilder
        {
            public static byte[] Build(
                List<PdfPage> pages)
            {
                const int catalogId = 1;
                const int pagesId = 2;
                const int regularFontId = 3;
                const int boldFontId = 4;
                const int firstPageId = 5;

                var objects =
                    new Dictionary<int, byte[]>();

                var pageIds =
                    new List<int>();

                for (int index = 0;
                     index < pages.Count;
                     index++)
                {
                    int pageId =
                        firstPageId + index * 2;

                    int contentId =
                        pageId + 1;

                    pageIds.Add(pageId);

                    byte[] stream =
                        Encoding.Latin1.GetBytes(
                            pages[index].Commands);

                    objects[contentId] =
                        Encoding.Latin1.GetBytes(
                            $"<< /Length {stream.Length} >>\n" +
                            "stream\n" +
                            pages[index].Commands +
                            "\nendstream");

                    objects[pageId] =
                        Encoding.ASCII.GetBytes(
                            $"<< /Type /Page /Parent " +
                            $"{pagesId} 0 R " +
                            $"/MediaBox [0 0 {F(PageWidth)} " +
                            $"{F(PageHeight)}] " +
                            $"/Resources << /Font << " +
                            $"/F1 {regularFontId} 0 R " +
                            $"/F2 {boldFontId} 0 R >> >> " +
                            $"/Contents {contentId} 0 R >>");
                }

                objects[catalogId] =
                    Encoding.ASCII.GetBytes(
                        $"<< /Type /Catalog /Pages " +
                        $"{pagesId} 0 R >>");

                objects[pagesId] =
                    Encoding.ASCII.GetBytes(
                        "<< /Type /Pages /Kids [" +
                        string.Join(
                            " ",
                            pageIds.Select(id =>
                                $"{id} 0 R")) +
                        $"] /Count {pageIds.Count} >>");

                objects[regularFontId] =
                    Encoding.ASCII.GetBytes(
                        "<< /Type /Font /Subtype /Type1 " +
                        "/BaseFont /Helvetica " +
                        "/Encoding /WinAnsiEncoding >>");

                objects[boldFontId] =
                    Encoding.ASCII.GetBytes(
                        "<< /Type /Font /Subtype /Type1 " +
                        "/BaseFont /Helvetica-Bold " +
                        "/Encoding /WinAnsiEncoding >>");

                return Serialize(
                    objects,
                    catalogId);
            }

            private static byte[] Serialize(
                Dictionary<int, byte[]> objects,
                int catalogId)
            {
                int maxObject = objects.Keys.Max();

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

                for (int id = 1;
                     id <= maxObject;
                     id++)
                {
                    offsets[id] = output.Position;

                    WriteAscii(
                        output,
                        $"{id} 0 obj\n");

                    output.Write(objects[id]);

                    WriteAscii(
                        output,
                        "\nendobj\n");
                }

                long xref = output.Position;

                WriteAscii(
                    output,
                    $"xref\n0 {maxObject + 1}\n");

                WriteAscii(
                    output,
                    "0000000000 65535 f \n");

                for (int id = 1;
                     id <= maxObject;
                     id++)
                {
                    WriteAscii(
                        output,
                        $"{offsets[id]:0000000000} " +
                        "00000 n \n");
                }

                WriteAscii(
                    output,
                    "trailer\n" +
                    $"<< /Size {maxObject + 1} " +
                    $"/Root {catalogId} 0 R >>\n" +
                    "startxref\n" +
                    $"{xref}\n" +
                    "%%EOF");

                return output.ToArray();
            }

            private static void WriteAscii(
                Stream stream,
                string value)
            {
                byte[] bytes =
                    Encoding.ASCII.GetBytes(value);

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

            public static PdfColor FromHex(
                string hex)
            {
                string value =
                    hex.Trim().TrimStart('#');

                return new PdfColor(
                    Convert.ToInt32(
                        value[..2],
                        16) / 255d,
                    Convert.ToInt32(
                        value.Substring(2, 2),
                        16) / 255d,
                    Convert.ToInt32(
                        value.Substring(4, 2),
                        16) / 255d);
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
            {
                return new List<string>
                {
                    string.Empty
                };
            }

            var result = new List<string>();

            foreach (string paragraph
                     in value.Split('\n'))
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
                    string candidate =
                        line.Length == 0
                            ? word
                            : line + " " + word;

                    if (EstimateWidth(
                            candidate,
                            fontSize,
                            false) <= width)
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

                    if (EstimateWidth(
                            word,
                            fontSize,
                            false) <= width)
                    {
                        line.Append(word);
                        continue;
                    }

                    int maxChars = Math.Max(
                        1,
                        (int)Math.Floor(
                            width /
                            (fontSize * 0.53)));

                    for (int i = 0;
                         i < word.Length;
                         i += maxChars)
                    {
                        string part = word.Substring(
                            i,
                            Math.Min(
                                maxChars,
                                word.Length - i));

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

            return value.Length *
                   fontSize *
                   factor;
        }

        private static string Escape(
            string? text) =>
            Sanitize(text)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

        private static string Sanitize(
            string? text)
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
                builder.Append(
                    character <= 255
                        ? character
                        : '?');
            }

            return builder.ToString().Trim();
        }

        private static string F(
            double value) =>
            value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
    }
}
