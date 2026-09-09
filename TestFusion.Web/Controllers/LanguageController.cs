using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TestFusion.Web.Controllers
{
    public class LanguageController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(
            string culture,
            string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                culture = "nl-NL";
            }

            if (string.IsNullOrWhiteSpace(returnUrl) ||
                !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)
                ),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                }
            );

            return LocalRedirect(returnUrl);
        }
    }
}