using System.Diagnostics;
using HealthCheckService.Models;
using Microsoft.AspNetCore.Mvc;

namespace HealthCheckService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HomeViewModel homeViewModel;
        public HomeController(HomeViewModel homeViewModel, ILogger<HomeController> logger)
        {
            this.homeViewModel = homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel));
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(homeViewModel);
        }

        public IActionResult Api()
        {
            return Redirect("/swagger");
        }

        public IActionResult Metrics([FromQuery] string key)
        {
            var endpoint = homeViewModel.HealthEndpoints.Endpoints.FirstOrDefault(x => x.Key == key);
            if (endpoint == null)
                return NotFound();
            return View(new MetricsViewModel(endpoint));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
