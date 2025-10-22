using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Upload document form
        public IActionResult Upload()
        {
            // Get all claims for the dropdown
            var claims = _context.Claims.OrderByDescending(c => c.DateSubmitted).ToList();
            ViewBag.Claims = claims;
            return View();
        }

        // POST: Upload document for existing claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int claimId, IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return RedirectToAction(nameof(Upload));
                }

                var claim = await _context.Claims.FirstOrDefaultAsync(c => c.Id == claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found! Please check the Claim ID.";
                    return RedirectToAction(nameof(Upload));
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                var fileExtension = Path.GetExtension(upload.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Only PDF, DOCX, and XLSX files are allowed.";
                    return RedirectToAction(nameof(Upload));
                }

                // Validate file size (max 5MB)
                if (upload.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size cannot exceed 5MB.";
                    return RedirectToAction(nameof(Upload));
                }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(fileStream);
                }

                // Update the claim with the new document
                claim.FileName = upload.FileName;
                claim.FilePath = "/uploads/" + uniqueFileName;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Document uploaded successfully for Claim #{claimId}!";
                return RedirectToAction("History", "Claims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while uploading the file: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }
    }
}