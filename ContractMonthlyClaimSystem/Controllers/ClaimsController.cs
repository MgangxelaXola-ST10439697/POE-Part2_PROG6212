using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Submit claim form
        public IActionResult Submit()
        {
            return View();
        }

        // POST: Submit claim - SIMPLIFIED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim claim, IFormFile? upload)
        {
            Console.WriteLine("=== SUBMIT METHOD STARTED ===");
            Console.WriteLine($"LecturerName: {claim?.LecturerName}");
            Console.WriteLine($"LecturerEmail: {claim?.LecturerEmail}");
            Console.WriteLine($"HoursWorked: {claim?.HoursWorked}");
            Console.WriteLine($"HourlyRate: {claim?.HourlyRate}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // Log all validation errors
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== VALIDATION ERRORS ===");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"ERROR: {error.ErrorMessage}");
                    }
                }

                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View(claim);
            }

            try
            {
                // Create NEW claim object with ONLY required fields
                var newClaim = new Claim
                {
                    LecturerName = claim.LecturerName,
                    LecturerEmail = claim.LecturerEmail,
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = claim.HourlyRate,
                    Notes = string.IsNullOrWhiteSpace(claim.Notes) ? null : claim.Notes,
                    Status = "Pending",
                    DateSubmitted = DateTime.Now
                };

                Console.WriteLine("=== NEW CLAIM CREATED ===");
                Console.WriteLine($"New Claim - Name: {newClaim.LecturerName}");
                Console.WriteLine($"New Claim - Email: {newClaim.LecturerEmail}");

                // Handle file upload ONLY if provided
                if (upload != null && upload.Length > 0)
                {
                    Console.WriteLine($"=== FILE UPLOAD DETECTED: {upload.FileName} ===");

                    var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                    var fileExtension = Path.GetExtension(upload.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Only PDF, DOCX, and XLSX files are allowed.";
                        return View(claim);
                    }

                    if (upload.Length > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "File size cannot exceed 5MB.";
                        return View(claim);
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                        Console.WriteLine($"Created uploads folder: {uploadsFolder}");
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await upload.CopyToAsync(stream);
                    }

                    newClaim.FileName = upload.FileName;
                    newClaim.FilePath = "/uploads/" + uniqueFileName;

                    Console.WriteLine($"File saved: {filePath}");
                }
                else
                {
                    Console.WriteLine("=== NO FILE UPLOADED ===");
                }

                // Add to database
                Console.WriteLine("=== ATTEMPTING TO SAVE TO DATABASE ===");
                _context.Claims.Add(newClaim);

                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"=== SAVE RESULT: {saveResult} records affected ===");
                Console.WriteLine($"=== CLAIM SAVED WITH ID: {newClaim.Id} ===");

                TempData["SuccessMessage"] = $"✅ Claim submitted successfully! Claim ID: {newClaim.Id}, Amount: R{newClaim.TotalAmount:N2}";
                return RedirectToAction(nameof(Submit));
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== EXCEPTION OCCURRED ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error saving claim: {ex.Message}";
                return View(claim);
            }
        }

        // View claim history
        public async Task<IActionResult> History()
        {
            var claims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            Console.WriteLine($"Found {claims.Count} claims in database"); // Debug line

            ViewBag.TotalClaims = claims.Count;
            ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");
            ViewBag.ApprovedClaims = claims.Count(c => c.Status == "Approved");

            return View(claims);
        }

        // Coordinator view - manage claims
        public async Task<IActionResult> Manage()
        {
            // NEW: Check authorization
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Coordinator" && userRole != "Manager")
            {
                TempData["ErrorMessage"] = "Access denied. Only coordinators and managers can access this page.";
                return RedirectToAction("Index", "Home");
            }

            var claims = await _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            ViewBag.TotalClaims = claims.Count;
            ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");
            ViewBag.ApprovedClaims = claims.Count(c => c.Status == "Approved");
            ViewBag.RejectedClaims = claims.Count(c => c.Status == "Rejected");

            return View(claims);
        }

        // Track claim status
        public async Task<IActionResult> Track()
        {
            try
            {
                // Get all claims ordered by date
                var claims = await _context.Claims
                    .OrderByDescending(c => c.DateSubmitted)
                    .ToListAsync();

                // ADD: Statistics for the view
                ViewBag.TotalClaims = claims.Count;
                ViewBag.PendingClaims = claims.Count(c => c.Status == "Pending");
                ViewBag.ApprovedClaims = claims.Count(c => c.Status == "Approved");
                ViewBag.RejectedClaims = claims.Count(c => c.Status == "Rejected");

                //  ADD: Calculate total amounts by status
                ViewBag.TotalAmount = claims.Sum(c => c.TotalAmount);
                ViewBag.PendingAmount = claims.Where(c => c.Status == "Pending").Sum(c => c.TotalAmount);
                ViewBag.ApprovedAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount);

                Console.WriteLine($"Track: Retrieved {claims.Count} claims");

                return View(claims);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Track: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading claim tracking. Please try again.";
                return View(new List<Claim>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Approved";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Claim #{id} approved!";
            }
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Claim #{id} rejected!";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}