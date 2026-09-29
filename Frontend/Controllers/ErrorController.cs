using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using OnleyiciBakimSistemi.Models;

namespace OnleyiciBakimSistemi.Controllers;

// UseStatusCodePagesWithReExecute("/Error/{0}") bu controller'a düşer.
// Örn: olmayan bir sayfa -> 404 -> Index(404) -> Views/Error/NotFound.cshtml
//      sunucu hatası    -> 500 -> Index(500) -> Views/Error/Generic.cshtml
[Route("Error")]
public class ErrorController : Controller
{
    [Route("{code:int}")]
    public IActionResult Index(int code)
    {
        if (code is < 100 or > 599)
            code = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode ?? 500;
        Response.StatusCode = code;

        var vm = new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier,
            StatusCode = code,
        };

        if (code == 404)
        {
            return View("NotFound", vm);
        }

        return View("Generic", vm);
    }
}
