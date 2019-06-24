using Microsoft.AspNetCore.Mvc;

namespace OAuthNotes.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {

            return View();
        }
    }
}
