using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        // GET: Login page
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            // Simple authentication for prototype
            if (username == "lecturer" && password == "123")
            {
                HttpContext.Session.SetString("UserRole", "Lecturer");
                HttpContext.Session.SetString("UserName", "Demo Lecturer");
                TempData["SuccessMessage"] = "Welcome, Lecturer!";
                return RedirectToAction("Submit", "Claims");
            }
            else if (username == "coordinator" && password == "123")
            {
                HttpContext.Session.SetString("UserRole", "Coordinator");
                HttpContext.Session.SetString("UserName", "Demo Coordinator");
                TempData["SuccessMessage"] = "Welcome, Coordinator!";
                return RedirectToAction("Manage", "Claims");
            }
            else
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}