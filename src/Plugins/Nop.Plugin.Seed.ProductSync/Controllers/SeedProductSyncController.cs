using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Seed.ProductSync.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin]
[Area(AreaNames.Admin)]
public class SeedProductSyncController : Controller
{
    // GET
    public IActionResult Configure()
    {
        return View("~/Plugins/Seed.ProductSync/Views/Configure.cshtml");
    }
}