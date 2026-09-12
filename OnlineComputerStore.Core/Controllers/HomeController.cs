using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _products;
        private readonly IAiHomepageService _aiHomepage;
        private readonly IEmailService _email;

        public HomeController(IProductService products, IAiHomepageService aiHomepage, IEmailService email)
        {
            _products = products;
            _aiHomepage = aiHomepage;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            var all = await _products.GetAllAsync();
            var history = HttpContext.Session.GetString("BrowseHistory");
            ViewBag.HasBrowseHistory = !string.IsNullOrWhiteSpace(history);
            ViewBag.Persona = await _aiHomepage.GetPersonaAsync(string.IsNullOrWhiteSpace(history) ? "no browsing history yet" : history);
            ViewBag.Recommended = all;

            // Top 2 trending products. Shop shows the next 5 from this same
            // ranking (skipping these 2), so the two pages never show the same item.
            var trending = (await _products.GetTrendingAsync()).Take(2).ToList();
            return View(trending);
        }

        public IActionResult About() => View();

        [HttpGet]
        public IActionResult Contact() => View(new ContactViewModel());

        [HttpPost]
        public IActionResult ClearBrowseHistory()
        {
            HttpContext.Session.Remove("BrowseHistory");
            TempData["BrowseHistoryCleared"] = true;
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _email.SendContactMessageAsync(model.Name, model.Email, model.Subject, model.Message);

            if (success)
            {
                TempData["ContactSuccess"] = true;
            }
            else
            {
                TempData["ContactError"] = message;
            }

            return RedirectToAction("Contact");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}