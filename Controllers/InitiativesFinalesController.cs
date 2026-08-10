using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CI.Controllers
{
    [Authorize]
    public class InitiativesFinalesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}