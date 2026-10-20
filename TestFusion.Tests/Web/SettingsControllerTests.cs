using Microsoft.AspNetCore.Mvc;
using TestFusion.Web.Controllers;

namespace TestFusion.Tests.Web;

public class SettingsControllerTests
{
    [Fact]
    public void Index_ReturnsView()
    {
        var controller = new SettingsController();

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
