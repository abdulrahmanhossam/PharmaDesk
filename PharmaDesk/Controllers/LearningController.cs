using Microsoft.AspNetCore.Mvc;

namespace PharmaDesk.Controllers
{
    public class LearningController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
