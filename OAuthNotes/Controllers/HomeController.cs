using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OAuthNotes.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private static List<string> _notes;

        public HomeController()
        {
            if (_notes == null)
            {
                _notes = new List<string> {"", "", ""};
            }
        }

        public IActionResult Index()
        {

            return View(_notes);
        }

        [HttpPost]
        public IActionResult Add(string note)
        {
            _notes.Add(note);
            if (_notes.Count > 3)
            {
                _notes.RemoveAt(0);
            }
            return RedirectToAction("Index");
        }
    }
}
