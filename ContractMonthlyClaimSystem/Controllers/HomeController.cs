using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Contract Monthly Claim System";
            return View();
        }

        public IActionResult About()
        {
            ViewBag.Message = "This system allows lecturers to submit monthly claims and upload supporting documents.";
            return View();
        }
    }
}
