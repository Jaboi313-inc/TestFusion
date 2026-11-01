namespace TestFusion.Core.Models;

public static class PageNavigationDefaults
{
    public static Dictionary<string, PageNavigationSettings> Create()
    {
        return new Dictionary<string, PageNavigationSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["Home/Index"] = new()
            {
                ShowBackToLeft = false,
                ShowBackToTop = true,
                ShowBackToStart = false
            },
            ["Analysis/Generate"] = new()
            {
                ShowBackToLeft = true,
                ShowBackToTop = true,
                ShowBackToStart = true
            },
            ["Debug/Index"] = new()
            {
                ShowBackToLeft = false,
                ShowBackToTop = true,
                ShowBackToStart = false
            },
            ["Settings/Index"] = new()
            {
                ShowBackToLeft = false,
                ShowBackToTop = true,
                ShowBackToStart = false
            }
        };
    }
}