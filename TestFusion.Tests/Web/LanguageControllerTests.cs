using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using TestFusion.Tests.Helpers;
using TestFusion.Web.Controllers;

namespace TestFusion.Tests.Web;

public class LanguageControllerTests
{
    [Fact]
    public void SetLanguage_EmptyCulture_UsesDutchAndRedirectsToLocalUrl()
    {
        var controller = new LanguageController();
        var httpContext = ControllerTestHelper.AttachHttpContext(controller);

        var result = controller.SetLanguage("", "/Settings");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Settings", redirect.Url);
        var cookie = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(CookieRequestCultureProvider.DefaultCookieName, cookie);
        Assert.Contains("nl-NL", cookie);
    }

    [Fact]
    public void SetLanguage_UnsafeReturnUrl_RedirectsToRoot()
    {
        var controller = new LanguageController();
        ControllerTestHelper.AttachHttpContext(controller);

        var result = controller.SetLanguage("en-US", "https://example.com/evil");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public void SetLanguage_ValidCulture_WritesCultureCookie()
    {
        var controller = new LanguageController();
        var httpContext = ControllerTestHelper.AttachHttpContext(controller);

        controller.SetLanguage("en-US", "/");

        var cookie = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("en-US", cookie);
        Assert.True(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }
}
