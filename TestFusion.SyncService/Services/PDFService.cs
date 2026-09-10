using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TestFusion.Core;
using TestFusion.Core.Enums;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Web.Services;

public static class PDFService
{
    public static byte[] Generate(
        GeneratedModel model,
        PdfLayoutModeEnum layoutMode,
        IStringLocalizer<SharedResource> localizer)
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
                    .Text(localizer["InjectorComparison"])
                    .FontSize(18)
                    .Bold();

                page.Content()
                    .PaddingTop(10)
                    .Column(column =>
                    {
                        column.Spacing(8);

                        CreateGeneralInfo(
                            column,
                            model,
                            localizer);

                        for (int i = 0; i < model.AllTests.Count; i++)
                        {
                            var test = model.AllTests[i];

                            switch (layoutMode)
                            {
                                case PdfLayoutModeEnum.Compact:

                                    column.Item()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                test,
                                                localizer));

                                    break;


                                case PdfLayoutModeEnum.KeepTestTogether:

                                    column.Item()
                                        .PreventPageBreak()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                test,
                                                localizer));

                                    break;


                                case PdfLayoutModeEnum.OneTestPerPage:

                                    column.Item()
                                        .PageBreak();

                                    column.Item()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                test,
                                                localizer));

                                    break;


                                default:

                                    column.Item()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                test,
                                                localizer));

                                    break;
                            }
                        }
                    });


                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(
                            localizer["Page"] + " ");

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
        GeneratedModel model,
        IStringLocalizer<SharedResource> localizer)
    {
        var firstInjector =
            model.Injectors.FirstOrDefault();

        if (firstInjector == null)
            return;


        // General injector/customer information
        column.Item()
            .Border(1)
            .Padding(8)
            .Row(row =>
            {
                /*
                 * Injector
                 */
                row.RelativeItem()
                    .Column(info =>
                    {
                        info.Item()
                            .Text(
                                localizer["Injector"] + ":")
                            .Bold();


                        info.Item()
                            .Text(
                                $"{localizer["InjectorPartNumber"]}: " +
                                $"{firstInjector.Data.PartNumber}");


                        info.Item()
                            .Text(
                                $"{localizer["InjectorBrand"]}: " +
                                $"{firstInjector.Data.PartBrand}");


                        info.Item()
                            .Text(
                                $"{localizer["InjectorType"]}: " +
                                $"{firstInjector.Data.PartType}");
                    });


                /*
                 * Customer
                 */
                row.RelativeItem()
                    .Column(info =>
                    {
                        info.Item()
                            .Text(
                                localizer["Customer"] + ":")
                            .Bold();


                        info.Item()
                            .Text(
                                $"{localizer["CustomerName"]}: " +
                                $"{firstInjector.Data.CustomerName}");


                        if (!string.IsNullOrWhiteSpace(
                            firstInjector.Data.CustomerPhone))
                        {
                            info.Item()
                                .Text(
                                    $"{localizer["CustomerPhoneNumber"]}: " +
                                    $"{firstInjector.Data.CustomerPhone}");
                        }


                        if (!string.IsNullOrWhiteSpace(
                            firstInjector.Data.CustomerMail))
                        {
                            info.Item()
                                .Text(
                                    $"{localizer["CustomerMail"]}: " +
                                    $"{firstInjector.Data.CustomerMail}");
                        }


                        if (!string.IsNullOrWhiteSpace(
                            firstInjector.Data.CustomerNotes))
                        {
                            info.Item()
                                .Text(
                                    $"{localizer["CustomerNote"]}: " +
                                    $"{RemoveHtml(firstInjector.Data.CustomerNotes)}");
                        }
                    });
            });


        /*
         * Injector-specific information
         */
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


                // Left column
                table.Cell()
                    .Border(1)
                    .Padding(5)
                    .Column(cell =>
                    {
                        cell.Item()
                            .Text(
                                localizer["InjectorTimeOffTesting"] + ":");


                        cell.Item()
                            .PaddingTop(8)
                            .Text(
                                localizer["InjectorSerialNumber"] + ":");


                        cell.Item()
                            .PaddingTop(8)
                            .Text(
                                localizer["InjectorNote"] + ":");
                    });


                // Injector columns
                foreach (var injector in model.Injectors)
                {
                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .Column(cell =>
                        {
                            // Date/time
                            cell.Item()
                                .Text(
                                    injector.Data.TimeOffTesting
                                        .ToString("dd-MM-yyyy HH:mm"));


                            // Serial number
                            cell.Item()
                                .PaddingTop(8)
                                .Text(
                                    string.IsNullOrWhiteSpace(
                                        injector.Data.InjectorSerialNumber)
                                        ? "-"
                                        : injector.Data.InjectorSerialNumber);


                            // Notes
                            cell.Item()
                                .PaddingTop(8)
                                .Text(
                                    string.IsNullOrWhiteSpace(
                                        injector.Data.TestNotes)
                                        ? "-"
                                        : RemoveHtml(
                                            injector.Data.TestNotes));
                        });
                }
            });
    }



    private static void CreateTest(
        IContainer container,
        GeneratedModel model,
        TestFusion.Core.Models.TestResult.TestModel test,
        IStringLocalizer<SharedResource> localizer)
    {
        container.Column(column =>
        {
            string normalizedTest =
                NormalizeTestName(test.TestName);


            var injectorTests = model.Injectors
                .Select(injector =>
                    injector.NormalizedTests
                        .FirstOrDefault(t =>
                            NormalizeTestName(t.TestName)
                            == normalizedTest))
                .ToList();

            bool hasAnyNonSkippedTest =
                injectorTests.Any(t =>
                    t != null &&
                    !t.IsSkipped);

            var tankSubData = model.Injectors
                .SelectMany(injector =>
                    injector.Data.Tests)
                .Where(t =>
                    NormalizeTestName(t.TestName)
                    == normalizedTest &&
                    t.TestStatus != 1)
                .SelectMany(t =>
                    t.SubTests ?? new())
                .GroupBy(t =>
                    t.TankName)
                .Select(g =>
                    g.First())
                .ToList();



            /*
             * TEST HEADER
             */
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
                        .Text(
                            $"{localizer["TestType"]}: " +
                            $"{(string.IsNullOrWhiteSpace(test.TestType) ? "-" : test.TestType)}");
                });

            if (hasAnyNonSkippedTest)
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


                        table.Cell()
                            .Border(1)
                            .Padding(6)
                            .Text(
                                localizer["TestResponseTime"] + ":");


                        for (int i = 0;
                             i < injectorTests.Count;
                             i++)
                        {
                            var injectorTest =
                                injectorTests[i];


                            table.Cell()
                                .Border(1)
                                .Padding(6)
                                .AlignMiddle()
                                .Element(cell =>
                                {
                                    if (injectorTest == null ||
                                        injectorTest.IsSkipped)
                                    {
                                        cell.Text("-");
                                    }
                                    else
                                    {
                                        cell.Text(
                                            $"{injectorTest.Response} " +
                                            $"{localizer["SecondIndicator"]}");
                                    }
                                });
                        }
                    });
            }
            else
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


                        table.Cell()
                            .Border(1)
                            .Padding(12)
                            .Text("");


                        foreach (var injector in model.Injectors)
                        {
                            table.Cell()
                                .Border(1)
                                .Background(Colors.Grey.Lighten4)
                                .MinHeight(45)
                                .Padding(12)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text(
                                    localizer["TestSkipped"])
                                .Bold()
                                .FontSize(9);
                        }
                    });

                return;
            }



            /*
             * TANK / RESULT ROWS
             */
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


                        decimal average =
                            (tank.Min + tank.Max) / 2;

                        decimal tolerance =
                            average - tank.Min;



                        /*
                         * Tank information
                         */
                        table.Cell()
                            .Border(1)
                            .Padding(5)
                            .Column(cell =>
                            {
                                cell.Item()
                                    .Text(tank.TankName)
                                    .Bold();

                                cell.Item()
                                    .Text(
                                        $"{localizer["TestMin"]}: " +
                                        $"{tank.Min}");


                                cell.Item()
                                    .Text(
                                        $"{localizer["TestMax"]}: " +
                                        $"{tank.Max}");


                                cell.Item()
                                    .Text(
                                        $"{localizer["TestLimit"]}: " +
                                        $"{average} +/- {tolerance}");
                            });


                        /*
                         * Injector result cells
                         */
                        foreach (var injector in model.Injectors)
                        {
                            var injectorTest =
                                injector.NormalizedTests
                                    .FirstOrDefault(t =>
                                        NormalizeTestName(t.TestName)
                                        == normalizedTest);


                            var sub = injector.Data.Tests
                                .Where(t =>
                                    NormalizeTestName(t.TestName)
                                    == normalizedTest)
                                .SelectMany(t =>
                                    t.SubTests ?? new())
                                .FirstOrDefault(s =>
                                    s.TankName
                                    == tank.TankName);


                            /*
                             * SKIPPED
                             */
                            if (injectorTest == null ||
                                injectorTest.IsSkipped ||
                                sub == null)
                            {
                                table.Cell()
                                    .Border(1)
                                    .Background(
                                        Colors.Grey.Lighten4)
                                    .MinHeight(45)
                                    .Padding(12)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text(
                                        localizer["TestSkipped"])
                                    .Bold()
                                    .FontSize(9);

                                continue;
                            }



                            /*
                             * NORMAL RESULT CELL
                             */
                            table.Cell()
                                .Border(1)
                                .Background(
                                    GetResultBackgroundColor(
                                        sub.ResultColor))
                                .DefaultTextStyle(style =>
                                    style.FontColor(
                                        GetResultTextColor(
                                            sub.ResultColor)))
                                .Padding(6)
                                .Element(cell =>
                                {
                                    if (sub.Results == null ||
                                        sub.Results.Count == 0)
                                    {
                                        cell.Text(
                                            localizer["TestNoResults"]);

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
                                                text.Span(
                                                        localizer["TestMin"]
                                                        + ": ")
                                                    .Bold();

                                                text.Span(
                                                    $"{resultMin} " +
                                                    $"{sub.ResultMin}");
                                            });


                                        result.Item()
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        localizer["TestAvg"]
                                                        + ": ")
                                                    .Bold();

                                                text.Span(
                                                    $"{resultAverage:0.0} " +
                                                    $"{sub.ResultAverage}");
                                            });


                                        result.Item()
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        localizer["TestMax"]
                                                        + ": ")
                                                    .Bold();

                                                text.Span(
                                                    $"{resultMax} " +
                                                    $"{sub.ResultMax}");
                                            });


                                        result.Item()
                                            .PaddingTop(5)
                                            .Text(text =>
                                            {
                                                text.Span(
                                                        localizer["TestResults"]
                                                        + ": ")
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
        });
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