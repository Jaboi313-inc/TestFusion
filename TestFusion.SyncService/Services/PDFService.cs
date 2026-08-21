using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Web.Services;

public static class PDFService
{
    public static byte[] Generate(GeneratedModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);

                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header()
                    .Text("Vergelijking verstuivers")
                    .FontSize(18)
                    .Bold();

                page.Content()
                    .PaddingTop(10)
                    .Column(column =>
                    {
                        column.Spacing(8);

                        CreateGeneralInfo(column, model);

                        foreach (var test in model.AllTests)
                        {
                            CreateTest(column, model, test);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        })
        .GeneratePdf();
    }

    private static void CreateGeneralInfo(
        ColumnDescriptor column,
        GeneratedModel model)
    {
        var firstInjector = model.Injectors.FirstOrDefault();

        if (firstInjector == null)
            return;

        column.Item()
            .Border(1)
            .Padding(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Verstuiver:")
                            .Bold();

                        info.Item()
                            .Text($"Nummer: {firstInjector.Data.PartNumber}");

                        info.Item()
                            .Text($"Merk: {firstInjector.Data.PartBrand}");

                        info.Item()
                            .Text($"Type: {firstInjector.Data.PartType}");
                    });

                row.RelativeItem()
                    .Column(info =>
                    {
                        info.Item()
                            .Text("Klant:")
                            .Bold();

                        info.Item()
                            .Text($"Naam: {firstInjector.Data.CustomerName}");

                        if (!string.IsNullOrWhiteSpace(firstInjector.Data.CustomerPhone))
                        {
                            info.Item()
                                .Text($"Tel: {firstInjector.Data.CustomerPhone}");
                        }

                        if (!string.IsNullOrWhiteSpace(firstInjector.Data.CustomerMail))
                        {
                            info.Item()
                                .Text($"Mail: {firstInjector.Data.CustomerMail}");
                        }

                        if (!string.IsNullOrWhiteSpace(firstInjector.Data.CustomerNotes))
                        {
                            info.Item()
                                .Text($"Notitie: {firstInjector.Data.CustomerNotes}");
                        }
                    });
            });

        column.Item()
            .PaddingTop(5)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(130);

                    foreach (var injector in model.Injectors)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Cell()
                    .Border(1)
                    .Padding(5)
                    .Text("Datum en tijd van testen:")
                    .Bold();

                foreach (var injector in model.Injectors)
                {
                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .Column(cell =>
                        {
                            cell.Item()
                                .Text(
                                    injector.Data.TimeOffTesting
                                        .ToString("dd-MM-yyyy HH:mm")
                                );

                            if (!string.IsNullOrWhiteSpace(injector.Data.TestNotes))
                            {
                                cell.Item()
                                    .PaddingTop(4)
                                    .Text("Notitie:")
                                    .Bold();

                                cell.Item()
                                    .Text(RemoveHtml(injector.Data.TestNotes));
                            }
                        });
                }
            });
    }

    private static void CreateTest(
        ColumnDescriptor column,
        GeneratedModel model,
        TestFusion.Core.Models.TestResult.TestModel test)
    {
        string normalizedTest = NormalizeTestName(test.TestName);

        var tankSubData = model.Injectors
            .First()
            .Data.Tests
            .Where(t =>
                NormalizeTestName(t.TestName) == normalizedTest)
            .SelectMany(t => t.SubTests)
            .ToList();

        column.Item()
            .PaddingTop(5)
            .Border(1)
            .Background(Colors.Grey.Lighten3)
            .Padding(6)
            .Column(header =>
            {
                header.Item()
                    .Text(normalizedTest)
                    .Bold()
                    .FontSize(10);

                header.Item()
                    .Text($"Response time: {test.TestResponseTime} s");

                header.Item()
                    .Text($"Test type: {test.TestType}");
            });

        foreach (var tank in tankSubData)
        {
            column.Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);

                        foreach (var injector in model.Injectors)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    decimal average = (tank.Min + tank.Max) / 2;
                    decimal tolerance = average - tank.Min;

                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .Column(cell =>
                        {
                            cell.Item()
                                .Text(tank.TankName)
                                .Bold();

                            cell.Item()
                                .Text($"Min: {tank.Min}");

                            cell.Item()
                                .Text($"Max: {tank.Max}");

                            cell.Item()
                                .Text($"Limiet: {average} +/- {tolerance}");
                        });

                    foreach (var injector in model.Injectors)
                    {
                        var sub = injector.Data.Tests
                            .Where(t =>
                                NormalizeTestName(t.TestName) == normalizedTest)
                            .SelectMany(t => t.SubTests)
                            .FirstOrDefault(s =>
                                s.TankName == tank.TankName);

                        table.Cell()
                            .Border(1)
                            .Background(
                                sub == null
                                    ? Colors.Grey.Lighten4
                                    : GetResultBackgroundColor(sub.ResultColor)
                            )
                            .DefaultTextStyle(style =>
                                style.FontColor(
                                    sub == null
                                        ? "#000000"
                                        : GetResultTextColor(sub.ResultColor)
                                )
                            )
                            .Padding(6)
                            .Element(cell =>
                            {
                                if (sub == null)
                                {
                                    cell.Text("Skipped")
                                        .Bold();

                                    return;
                                }

                                if (sub.Results == null ||
                                    sub.Results.Count == 0)
                                {
                                    cell.Text("Geen resultaten");
                                    return;
                                }

                                decimal resultMin =
                                    sub.Results.Min();

                                decimal resultAverage =
                                    sub.Results.Average();

                                decimal resultMax =
                                    sub.Results.Max();

                                cell.Column(result =>
                                {
                                    result.Item()
                                        .Text(text =>
                                        {
                                            text.Span("Min: ")
                                                .Bold();

                                            text.Span(
                                                $"{resultMin} {sub.ResultMin}");
                                        });

                                    result.Item()
                                        .Text(text =>
                                        {
                                            text.Span("Avg: ")
                                                .Bold();

                                            text.Span(
                                                $"{resultAverage:0.0} {sub.ResultAverage}");
                                        });

                                    result.Item()
                                        .Text(text =>
                                        {
                                            text.Span("Max: ")
                                                .Bold();

                                            text.Span(
                                                $"{resultMax} {sub.ResultMax}");
                                        });

                                    result.Item()
                                        .PaddingTop(5)
                                        .Text(text =>
                                        {
                                            text.Span("Results: ")
                                                .Bold();

                                            text.Span(
                                                string.Join(
                                                    " | ",
                                                    sub.Results));
                                        });
                                });
                            });
                    }
                });
        }
    }

    private static string NormalizeTestName(string name)
    {
        return name
            .Replace(" : SKIPPED", "")
            .Trim();
    }

    private static string RemoveHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return System.Text.RegularExpressions.Regex.Replace(
            value,
            "<.*?>",
            ""
        );
    }

    private static string GetResultBackgroundColor(int resultColor)
    {
        return resultColor switch
        {
            1 => "#FF331C", // Red
            2 => "#FF331C", // Red
            3 => "#FF331C", // Red
            4 => "#FF331C", // Red
            5 => "#0000FF", // Blue
            6 => "#00AB4F", // Green
            8 => "#FF331C", // Red
            _ => "#FFFFFF"  // White
        };
    }

    private static string GetResultTextColor(int resultColor)
    {
        return resultColor switch
        {
            1 => "#FFFFFF", // White text on Red background
            2 => "#FFFFFF", // White text on Red background
            3 => "#FFFFFF", // White text on Red background
            4 => "#FFFFFF", // White text on Red background
            5 => "#FFFFFF", // White text on Blue background
            6 => "#FFFFFF", // White text on Green background
            8 => "#FFFFFF", // White text on Red background
            _ => "#000000"  // Black text on other backgrounds
        };
    }
}