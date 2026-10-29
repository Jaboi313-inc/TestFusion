using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TestFusion.Core;
using TestFusion.Core.Enums;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Services.Services;

public static class PDFService
{
    private sealed class CustomerInfoGroup
    {
        public string Key { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public static byte[] Generate(
        GeneratedModel model,
        PdfLayoutModeEnum layoutMode,
        IStringLocalizer<SharedResource> localizer,
        ITimeZoneService timeZoneService)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var showDebugInformation = model.Settings.ShowDebugInformation;
        var groupTestsByBaseName = model.Settings.GroupTestsByBaseName;

        var matchCustomerName = model.Settings.MatchCustomerName;
        var matchCustomerPhone = model.Settings.MatchCustomerPhone;
        var matchCustomerMail = model.Settings.MatchCustomerMail;
        var matchCustomerNotes = model.Settings.MatchCustomerNotes;

        var maxInjectorsPerPage = model.Settings.PdfMaxInjectorsPerPage;

        var displayTests = model.AllTests
            .GroupBy(
                test => NormalizeTestName(test.TestName, groupTestsByBaseName),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var orderedInjectorIndexes = model.Injectors
            .Select((injector, index) => new { Injector = injector, Index = index })
            .GroupBy(item => CreateCustomerKey(
                item.Injector.Data.CustomerName,
                item.Injector.Data.CustomerPhone,
                item.Injector.Data.CustomerMail,
                item.Injector.Data.CustomerNotes,
                matchCustomerName,
                matchCustomerPhone,
                matchCustomerMail,
                matchCustomerNotes))
            .SelectMany(group => group.Select(item => item.Index))
            .ToList();

        var injectorGroups = orderedInjectorIndexes
            .Chunk(maxInjectorsPerPage)
            .Select(group => (IReadOnlyList<int>)group.ToList())
            .ToList();

        if (injectorGroups.Count == 0)
            injectorGroups.Add(Array.Empty<int>());

        return Document.Create(document =>
        {
            foreach (var injectorIndexes in injectorGroups)
            {
                if (layoutMode == PdfLayoutModeEnum.OneTestPerPage)
                {
                    foreach (var test in displayTests)
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
                                        injectorIndexes,
                                        localizer,
                                        timeZoneService,
                                        matchCustomerName,
                                        matchCustomerPhone,
                                        matchCustomerMail,
                                        matchCustomerNotes);

                                    column.Item()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                injectorIndexes,
                                                test,
                                                localizer,
                                                showDebugInformation,
                                                groupTestsByBaseName));
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span(localizer["Page"] + " ");
                                    text.CurrentPageNumber();
                                    text.Span(" / ");
                                    text.TotalPages();
                                });
                        });
                    }

                    continue;
                }

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
                                injectorIndexes,
                                localizer,
                                timeZoneService,
                                matchCustomerName,
                                matchCustomerPhone,
                                matchCustomerMail,
                                matchCustomerNotes);

                            foreach (var test in displayTests)
                            {
                                if (layoutMode == PdfLayoutModeEnum.KeepTestTogether)
                                {
                                    column.Item()
                                        .PreventPageBreak()
                                        .Element(container =>
                                            CreateTest(
                                                container,
                                                model,
                                                injectorIndexes,
                                                test,
                                                localizer,
                                                showDebugInformation,
                                                groupTestsByBaseName));

                                    continue;
                                }

                                column.Item()
                                    .Element(container =>
                                        CreateTest(
                                            container,
                                            model,
                                            injectorIndexes,
                                            test,
                                            localizer,
                                            showDebugInformation,
                                            groupTestsByBaseName));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span(localizer["Page"] + " ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            }
        }).GeneratePdf();
    }

    private static void CreateGeneralInfo(
        ColumnDescriptor column,
        GeneratedModel model,
        IReadOnlyList<int> injectorIndexes,
        IStringLocalizer<SharedResource> localizer,
        ITimeZoneService timeZoneService,
        bool matchCustomerName,
        bool matchCustomerPhone,
        bool matchCustomerMail,
        bool matchCustomerNotes)
    {
        if (injectorIndexes.Count == 0)
            return;

        var firstInjector = model.Injectors[injectorIndexes[0]];
        var customerGroups = CreateCustomerGroups(
            model,
            injectorIndexes,
            matchCustomerName,
            matchCustomerPhone,
            matchCustomerMail,
            matchCustomerNotes);

        column.Item()
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(130);

                    foreach (var _ in injectorIndexes)
                        columns.RelativeColumn();
                });

                // General injector information + grouped customer information
                table.Cell()
                    .Border(1)
                    .Padding(6)
                    .Column(info =>
                    {
                        info.Item().Text(localizer["Injector"] + ":").Bold();
                        info.Item().Text($"{localizer["InjectorPartNumber"]}: {firstInjector.Data.PartNumber}");
                        info.Item().Text($"{localizer["InjectorBrand"]}: {firstInjector.Data.PartBrand}");
                        info.Item().Text($"{localizer["InjectorType"]}: {firstInjector.Data.PartType}");
                    });

                foreach (var group in customerGroups)
                {
                    table.Cell()
                        .ColumnSpan((uint)group.Count)
                        .Border(1)
                        .Padding(6)
                        .Column(info =>
                        {
                            info.Item().Text(localizer["Customer"] + ":").Bold();
                            info.Item().Text($"{localizer["CustomerName"]}: {group.Name}");

                            if (!string.IsNullOrWhiteSpace(group.Phone))
                                info.Item().Text($"{localizer["CustomerPhoneNumber"]}: {group.Phone}");

                            if (!string.IsNullOrWhiteSpace(group.Mail))
                                info.Item().Text($"{localizer["CustomerMail"]}: {group.Mail}");

                            if (!string.IsNullOrWhiteSpace(group.Notes))
                                info.Item().Text($"{localizer["CustomerNote"]}: {RemoveHtml(group.Notes)}");
                        });
                }

                // Injector detail labels
                table.Cell()
                    .Border(1)
                    .Padding(6)
                    .Column(cell =>
                    {
                        cell.Item().MinHeight(12).Text(localizer["InjectorTimeOffTesting"] + ":");
                        cell.Item().MinHeight(12).Text(localizer["InjectorSerialNumber"] + ":");
                        cell.Item().MinHeight(12).Text(localizer["InjectorNote"] + ":");
                    });

                // Injector detail values
                foreach (var injectorIndex in injectorIndexes)
                {
                    var injector = model.Injectors[injectorIndex];

                    table.Cell()
                        .Border(1)
                        .Padding(6)
                        .Column(cell =>
                        {
                            cell.Item()
                                .MinHeight(12)
                                .Text(timeZoneService.FormatForUser(
                                    injector.Data.TimeOffTesting,
                                    "dd-MM-yyyy HH:mm"));

                            cell.Item()
                                .MinHeight(12)
                                .Text(string.IsNullOrWhiteSpace(injector.Data.InjectorSerialNumber)
                                    ? string.Empty
                                    : injector.Data.InjectorSerialNumber);

                            cell.Item()
                                .MinHeight(12)
                                .Text(string.IsNullOrWhiteSpace(injector.Data.TestNotes)
                                    ? string.Empty
                                    : RemoveHtml(injector.Data.TestNotes));
                        });
                }

                // Test / tank header row
                table.Cell()
                    .Border(1)
                    .Padding(6)
                    .MinHeight(24)
                    .AlignBottom()
                    .Text(localizer["TestTestNameAndTank"])
                    .Bold();

                foreach (var _ in injectorIndexes)
                {
                    table.Cell()
                        .Border(1)
                        .MinHeight(24)
                        .Text(string.Empty);
                }
            });
    }

    private static void CreateTest(
        IContainer container,
        GeneratedModel model,
        IReadOnlyList<int> injectorIndexes,
        TestFusion.Core.Models.TestResult.TestModel test,
        IStringLocalizer<SharedResource> localizer,
        bool showDebugInformation,
        bool groupTestsByBaseName)
    {
        if (injectorIndexes.Count == 0)
            return;

        container.Column(column =>
        {
            var normalizedTest = NormalizeTestName(test.TestName, groupTestsByBaseName);

            var injectorTests = injectorIndexes
                .Select(index => model.Injectors[index].NormalizedTests
                    .FirstOrDefault(t => NormalizeTestName(t.TestName, groupTestsByBaseName) == normalizedTest))
                .ToList();

            var hasAnyNonSkippedTest =
                injectorTests.Any(t => t != null && !t.IsSkipped);

            var tankSubData = injectorIndexes
                .Select(index => model.Injectors[index])
                .SelectMany(injector => injector.Data.Tests)
                .Where(t =>
                    NormalizeTestName(t.TestName, groupTestsByBaseName) == normalizedTest &&
                    t.TestStatus != 1)
                .SelectMany(t => t.SubTests ?? new())
                .GroupBy(t => t.TankName)
                .Select(group => group.First())
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

                            foreach (var _ in injectorIndexes)
                                columns.RelativeColumn();
                        });

                        table.Cell()
                            .Border(1)
                            .Padding(6)
                            .Text(localizer["TestResponseTime"] + ":");

                        for (var i = 0; i < injectorTests.Count; i++)
                        {
                            var injectorTest = injectorTests[i];

                            table.Cell()
                                .Border(1)
                                .Padding(6)
                                .AlignMiddle()
                                .Element(cell =>
                                {
                                    if (injectorTest == null || injectorTest.IsSkipped)
                                    {
                                        cell.Text("-");
                                        return;
                                    }

                                    cell.Text(
                                        $"{injectorTest.Response} " +
                                        $"{localizer["SecondIndicator"]}");
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

                            foreach (var _ in injectorIndexes)
                                columns.RelativeColumn();
                        });

                        table.Cell()
                            .Border(1)
                            .Padding(6)
                            .Text("");

                        foreach (var _ in injectorIndexes)
                        {
                            table.Cell()
                                .Border(1)
                                .Background(Colors.Grey.Lighten4)
                                .MinHeight(45)
                                .Padding(6)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text(localizer["TestSkipped"])
                                .Bold()
                                .FontSize(9);
                        }
                    });

                return;
            }

            foreach (var tank in tankSubData)
            {
                column.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(130);

                            foreach (var _ in injectorIndexes)
                                columns.RelativeColumn();
                        });

                        var average = (tank.Min + tank.Max) / 2;
                        var tolerance = average - tank.Min;

                        table.Cell()
                            .Border(1)
                            .Padding(5)
                            .Column(cell =>
                            {
                                cell.Item()
                                    .Text(tank.TankName)
                                    .Bold();

                                cell.Item()
                                    .Text($"{localizer["TestMin"]}: {tank.Min}");

                                cell.Item()
                                    .Text($"{localizer["TestMax"]}: {tank.Max}");

                                cell.Item()
                                    .Text($"{localizer["TestLimit"]}: {average} +/- {tolerance}");
                            });

                        foreach (var injectorIndex in injectorIndexes)
                        {
                            var injector = model.Injectors[injectorIndex];

                            var injectorTest = injector.NormalizedTests
                                .FirstOrDefault(t =>
                                    NormalizeTestName(t.TestName, groupTestsByBaseName) == normalizedTest);

                            var actualTest = injector.Data.Tests
                                .FirstOrDefault(t => t.TestId == test.TestId);

                            var actualSub = actualTest?.SubTests
                                .FirstOrDefault(s =>
                                    s.TankPosition == tank.TankPosition &&
                                    s.TankName == tank.TankName);

                            var sub = injector.Data.Tests
                                .Where(t =>
                                    NormalizeTestName(t.TestName, groupTestsByBaseName) == normalizedTest)
                                .SelectMany(t => t.SubTests ?? new())
                                .FirstOrDefault(s =>
                                    s.TankName == tank.TankName);

                            if (injectorTest == null ||
                                injectorTest.IsSkipped ||
                                sub == null)
                            {
                                table.Cell()
                                    .Border(1)
                                    .Background(Colors.Grey.Lighten4)
                                    .MinHeight(45)
                                    .Padding(6)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text(localizer["TestSkipped"])
                                    .Bold()
                                    .FontSize(9);

                                continue;
                            }

                            table.Cell()
                                .Border(1)
                                .Background(GetResultBackgroundColor(sub.ResultColor))
                                .DefaultTextStyle(style =>
                                    style.FontColor(GetResultTextColor(sub.ResultColor)))
                                .Padding(6)
                                .Element(cell =>
                                {
                                    if (sub.Results == null ||
                                        sub.Results.Count == 0)
                                    {
                                        cell.Text(localizer["TestNoResults"]);
                                        return;
                                    }

                                    var resultMin = sub.Results.Min();
                                    var resultAverage = sub.Results.Average();
                                    var resultMax = sub.Results.Max();

                                    cell.Column(result =>
                                    {
                                        if (showDebugInformation &&
                                            actualTest != null &&
                                            actualSub != null)
                                        {
                                            var debugAverage = (actualSub.Min + actualSub.Max) / 2;
                                            var debugTolerance = debugAverage - actualSub.Min;

                                            result.Item().Text("DEBUG").Bold();
                                            result.Item().Text($"TestId: {actualTest.TestId}");
                                            result.Item().Text($"TestName: {actualTest.TestName}");
                                            result.Item().Text($"TestType: {actualTest.TestType}");
                                            result.Item().Text($"Tank: {actualSub.TankName}");
                                            result.Item().Text($"Position: {actualSub.TankPosition}");
                                            result.Item().Text($"Min: {actualSub.Min}");
                                            result.Item().Text($"Max: {actualSub.Max}");
                                            result.Item().Text($"Limit: {debugAverage} +/- {debugTolerance}");
                                            result.Item().Text($"BlueMin: {actualSub.BlueMin}");
                                            result.Item().Text($"BlueMax: {actualSub.BlueMax}");
                                            result.Item().Text($"YellowMin: {actualSub.YellowMin}");
                                            result.Item().Text($"YellowMax: {actualSub.YellowMax}");
                                            result.Item().Text($"TolBlue: {actualSub.TolBlue}");
                                            result.Item().Text($"ProcentMin: {actualSub.ProcentMin}");
                                            result.Item().Text($"ProcentMax: {actualSub.ProcentMax}");
                                            result.Item().PaddingBottom(5).Text($"ProcentText: {actualSub.ProcentText}");
                                        }

                                        result.Item()
                                            .Text(text =>
                                            {
                                                text.Span(localizer["TestMin"] + ": ").Bold();
                                                text.Span($"{resultMin} {sub.ResultMin}");
                                            });

                                        result.Item()
                                            .Text(text =>
                                            {
                                                text.Span(localizer["TestAvg"] + ": ").Bold();
                                                text.Span($"{resultAverage:0.0} {sub.ResultAverage}");
                                            });

                                        result.Item()
                                            .Text(text =>
                                            {
                                                text.Span(localizer["TestMax"] + ": ").Bold();
                                                text.Span($"{resultMax} {sub.ResultMax}");
                                            });

                                        result.Item()
                                            .PaddingTop(5)
                                            .Text(text =>
                                            {
                                                text.Span(localizer["TestResults"] + ": ").Bold();
                                                text.Span(string.Join(" | ", sub.Results));
                                            });
                                    });
                                });
                        }
                    });
            }
        });
    }

    private static List<CustomerInfoGroup> CreateCustomerGroups(
        GeneratedModel model,
        IReadOnlyList<int> injectorIndexes,
        bool matchCustomerName,
        bool matchCustomerPhone,
        bool matchCustomerMail,
        bool matchCustomerNotes)
    {
        var groups = new List<CustomerInfoGroup>();

        foreach (var injectorIndex in injectorIndexes)
        {
            var data = model.Injectors[injectorIndex].Data;

            var key = CreateCustomerKey(
                data.CustomerName,
                data.CustomerPhone,
                data.CustomerMail,
                data.CustomerNotes,
                matchCustomerName,
                matchCustomerPhone,
                matchCustomerMail,
                matchCustomerNotes);

            if (groups.Count > 0 && groups[^1].Key == key)
            {
                groups[^1].Count++;
                continue;
            }

            groups.Add(new CustomerInfoGroup
            {
                Key = key,
                Count = 1,
                Name = data.CustomerName?.Trim() ?? string.Empty,
                Phone = data.CustomerPhone?.Trim() ?? string.Empty,
                Mail = data.CustomerMail?.Trim() ?? string.Empty,
                Notes = data.CustomerNotes?.Trim() ?? string.Empty
            });
        }

        return groups;
    }

    private static string CreateCustomerKey(
        string? name,
        string? phone,
        string? mail,
        string? notes,
        bool matchName,
        bool matchPhone,
        bool matchMail,
        bool matchNotes)
    {
        var values = new List<string>();

        if (matchName)
            values.Add("NAME:" + NormalizeCustomerValue(name));

        if (matchPhone)
            values.Add("PHONE:" + NormalizeCustomerValue(phone));

        if (matchMail)
            values.Add("MAIL:" + NormalizeCustomerValue(mail));

        if (matchNotes)
            values.Add("NOTES:" + NormalizeCustomerValue(notes));

        return string.Join("\u001F", values);
    }

    private static string NormalizeCustomerValue(string? value)
    {
        if (value == null)
            return "<NULL>";

        var withoutHtml = System.Text.RegularExpressions.Regex.Replace(value, "<.*?>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutHtml);

        var normalized = System.Text.RegularExpressions.Regex
            .Replace(decoded, @"\s+", " ")
            .Trim();

        if (normalized.Length == 0)
            return "<EMPTY>";

        return normalized.ToUpperInvariant();
    }

    private static string NormalizeTestName(string name, bool groupTestsByBaseName)
    {
        var normalizedName = name.Replace(" : SKIPPED", "").Trim();

        if (!groupTestsByBaseName)
            return normalizedName;

        var separatorIndex = normalizedName.IndexOf(':');

        return separatorIndex >= 0
            ? normalizedName[..separatorIndex].Trim()
            : normalizedName;
    }

    private static string DisplayValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string RemoveHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return System.Net.WebUtility.HtmlDecode(
            System.Text.RegularExpressions.Regex.Replace(value, "<.*?>", ""));
    }

    private static string GetResultBackgroundColor(int resultColor)
    {
        return resultColor switch
        {
            1 => "#FF331C",
            2 => "#FF331C",
            3 => "#FF331C",
            4 => "#FF331C",
            5 => "#0000FF",
            6 => "#00AB4F",
            8 => "#FF331C",
            _ => "#FFFFFF"
        };
    }

    private static string GetResultTextColor(int resultColor)
    {
        return resultColor switch
        {
            1 => "#FFFFFF",
            2 => "#FFFFFF",
            3 => "#FFFFFF",
            4 => "#FFFFFF",
            5 => "#FFFFFF",
            6 => "#FFFFFF",
            8 => "#FFFFFF",
            _ => "#000000"
        };
    }
}
