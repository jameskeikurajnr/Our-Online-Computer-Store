using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class SearchController : Controller
    {
        private readonly IAiSearchService _ai;
        private readonly IProductService _products;

        public SearchController(IAiSearchService ai, IProductService products)
        {
            _ai = ai;
            _products = products;
        }

        public async Task<IActionResult> Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index", "Shop");

            ViewBag.Query = q;
            ViewBag.AiInterpretation = await _ai.InterpretQueryAsync(q);

            var results = await _products.SearchAsync(q);
            return View(results);
        }
    }
}
