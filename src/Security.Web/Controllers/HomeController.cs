using Microsoft.AspNetCore.Mvc;

namespace Security.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
