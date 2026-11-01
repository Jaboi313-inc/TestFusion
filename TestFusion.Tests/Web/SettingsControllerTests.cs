using Microsoft.AspNetCore.Mvc;
using TestFusion.Core.Models.WebModels;
using TestFusion.Tests.Helpers;
using TestFusion.Web.Controllers;

namespace TestFusion.Tests.Web;

public class SettingsControllerTests
{
    [Fact]
    public async Task Index_ReturnsView()
    {
        var settingsService = new TestSettingsService();
        var controller = new SettingsController(settingsService);

        var result = await controller.Index(returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<SettingsViewModel>(view.Model);
    }
}