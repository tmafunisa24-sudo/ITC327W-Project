// Summary: Handles dashboard requests and coordinates dashboard data for the user interface.
using Microsoft.AspNetCore.Mvc;

namespace ServiceHub_IT.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
