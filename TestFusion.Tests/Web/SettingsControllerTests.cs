using Microsoft.AspNetCore.Mvc;
using TestFusion.Tests.Helpers;
using TestFusion.Web.Controllers;

namespace TestFusion.Tests.Web;

public class SettingsControllerTests
{
    [Fact]
    public void Index_ReturnsView()
    {
        var settingsService = new TestSettingsService();
        var controller = new SettingsController(settingsService);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
