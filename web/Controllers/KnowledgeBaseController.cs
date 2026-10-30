// This controller manages the Knowledge Base page and displays its main Index view.
using Microsoft.AspNetCore.Mvc;

namespace ServiceHub_IT.Controllers;

public class KnowledgeBaseController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
