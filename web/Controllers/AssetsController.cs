// Provides the public-facing asset index page.
using Microsoft.AspNetCore.Mvc;

namespace ServiceHub_IT.Controllers;

[Route("Assets")]
public class AssetsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
