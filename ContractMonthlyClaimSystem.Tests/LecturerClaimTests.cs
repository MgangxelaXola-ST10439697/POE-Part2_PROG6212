using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractMonthlyClaimSystem.Controllers;
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace ContractMonthlyClaimSystem.Tests
{
    /// <summary>
    /// Unit Tests for Independent Contractor (Lecturer) Functionality
    /// Tests focus on claim submission and calculation features
    /// </summary>
    public class LecturerClaimTests
    {
        #region Helper Methods

        /// <summary>
        /// Creates an in-memory database for testing
        /// </summary>
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Creates a mock web hosting environment for file operations
        /// </summary>
        private Mock<IWebHostEnvironment> GetMockEnvironment()
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());
            return mockEnv;
        }

        #endregion

        #region Test 1: Total Amount Calculation (REQUIRED)

        /// <summary>
        /// TEST 1: Verify that total amount is calculated correctly
        /// Formula: Total Amount = Hours Worked × Hourly Rate
        /// </summary>
        [Fact]
        public void CalculateTotalAmount_WithValidHoursAndRate_ReturnsCorrectTotal()
        {
            // Arrange - Set up test data
            var claim = new Claim
            {
                HoursWorked = 10,      // 10 hours
                HourlyRate = 150       // R150 per hour
            };

            // Act - Calculate total amount
            var totalAmount = claim.TotalAmount;

            // Assert - Verify result
            Assert.Equal(1500, totalAmount); // Expected: 10 × 150 = 1500
        }

        /// <summary>
        /// TEST 1b: Verify calculation with decimal hours (e.g., 7.5 hours)
        /// </summary>
        [Fact]
        public void CalculateTotalAmount_WithDecimalHours_ReturnsCorrectTotal()
        {
            // Arrange
            var claim = new Claim
            {
                HoursWorked = 7.5m,    // 7.5 hours
                HourlyRate = 200       // R200 per hour
            };

            // Act
            var totalAmount = claim.TotalAmount;

            // Assert
            Assert.Equal(1500, totalAmount); // Expected: 7.5 × 200 = 1500
        }

        #endregion

        #region Test 2: Lecturer Claim Submission

        /// <summary>
        /// TEST 2: Verify that lecturer can successfully submit a claim
        /// </summary>
        [Fact]
        public async Task SubmitClaim_WithValidData_SavesClaimToDatabase()
        {
            // Arrange - Set up controller and test data
            var context = GetInMemoryDbContext();
            var mockEnv = GetMockEnvironment();
            var controller = new ClaimsController(context, mockEnv.Object);

            var claim = new Claim
            {
                LecturerName = "John Doe",
                LecturerEmail = "john.doe@example.com",
                HoursWorked = 20,
                HourlyRate = 250,
                Notes = "Test monthly claim for October"
            };

            // Act - Submit the claim
            var result = await controller.Submit(claim, null);

            // Assert - Verify claim was saved
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClaimsController.Submit), redirectResult.ActionName);

            // Verify database has 1 claim
            var savedClaims = await context.Claims.CountAsync();
            Assert.Equal(1, savedClaims);

            // Verify claim details
            var savedClaim = await context.Claims.FirstAsync();
            Assert.Equal("John Doe", savedClaim.LecturerName);
            Assert.Equal(20, savedClaim.HoursWorked);
            Assert.Equal(250, savedClaim.HourlyRate);
            Assert.Equal(5000, savedClaim.TotalAmount); // 20 × 250 = 5000
        }

        #endregion

        #region Test 3: View Lecturer Claim History

        /// <summary>
        /// TEST 3: Verify that lecturer can view their claim history
        /// </summary>
        [Fact]
        public async Task ViewClaimHistory_WithExistingClaims_ReturnsAllClaims()
        {
            // Arrange - Set up controller and add test claims
            var context = GetInMemoryDbContext();
            var mockEnv = GetMockEnvironment();
            var controller = new ClaimsController(context, mockEnv.Object);

            // Add multiple claims to database
            context.Claims.AddRange(
                new Claim
                {
                    LecturerName = "John Doe",
                    LecturerEmail = "john@example.com",
                    HoursWorked = 10,
                    HourlyRate = 100,
                    Status = "Pending",
                    DateSubmitted = DateTime.Now.AddDays(-2)
                },
                new Claim
                {
                    LecturerName = "John Doe",
                    LecturerEmail = "john@example.com",
                    HoursWorked = 15,
                    HourlyRate = 150,
                    Status = "Approved",
                    DateSubmitted = DateTime.Now.AddDays(-1)
                },
                new Claim
                {
                    LecturerName = "John Doe",
                    LecturerEmail = "john@example.com",
                    HoursWorked = 8,
                    HourlyRate = 120,
                    Status = "Pending",
                    DateSubmitted = DateTime.Now
                }
            );
            await context.SaveChangesAsync();

            // Act - Retrieve claim history
            var result = await controller.History();

            // Assert - Verify all claims are returned
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Claim>>(viewResult.Model);

            Assert.Equal(3, model.Count()); // Should have 3 claims

            // Verify ViewBag statistics
            Assert.Equal(3, controller.ViewBag.TotalClaims);
            Assert.Equal(2, controller.ViewBag.PendingClaims);
            Assert.Equal(1, controller.ViewBag.ApprovedClaims);
        }

        #endregion

        #region Test 4: Track Claim Status (BONUS)

        /// <summary>
        /// TEST 4 (BONUS): Verify that lecturer can track their claim status
        /// </summary>
        [Fact]
        public async Task TrackClaimStatus_WithSubmittedClaim_ReturnsCorrectStatus()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEnv = GetMockEnvironment();
            var controller = new ClaimsController(context, mockEnv.Object);

            var claim = new Claim
            {
                LecturerName = "Jane Smith",
                LecturerEmail = "jane@example.com",
                HoursWorked = 12,
                HourlyRate = 180,
                Status = "Pending"
            };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Track();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Claim>>(viewResult.Model);

            var trackedClaim = model.First();
            Assert.Equal("Pending", trackedClaim.Status);
            Assert.Equal("Jane Smith", trackedClaim.LecturerName);
        }

        #endregion

        #region Test 5: Default Claim Status (BONUS)

        /// <summary>
        /// TEST 5 (BONUS): Verify that new claims have default "Pending" status
        /// </summary>
        [Fact]
        public void NewClaim_DefaultStatus_IsPending()
        {
            // Arrange & Act
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                LecturerEmail = "test@example.com",
                HoursWorked = 5,
                HourlyRate = 100
            };

            // Assert
            Assert.Equal("Pending", claim.Status);
        }

        #endregion

        #region Test 6: Date Submitted Auto-Set (BONUS)

        /// <summary>
        /// TEST 6 (BONUS): Verify that DateSubmitted is automatically set to current date/time
        /// </summary>
        [Fact]
        public void NewClaim_DateSubmitted_IsSetToCurrentDate()
        {
            // Arrange & Act
            var beforeCreation = DateTime.Now;
            var claim = new Claim
            {
                LecturerName = "Test Lecturer",
                LecturerEmail = "test@example.com",
                HoursWorked = 5,
                HourlyRate = 100
            };
            var afterCreation = DateTime.Now;

            // Assert - DateSubmitted should be between before and after
            Assert.True(claim.DateSubmitted >= beforeCreation);
            Assert.True(claim.DateSubmitted <= afterCreation);
        }

        #endregion

        #region Test 7: Claim Validation (BONUS)

        /// <summary>
        /// TEST 7 (BONUS): Verify that invalid claims are rejected
        /// </summary>
        [Fact]
        public async Task SubmitClaim_WithInvalidData_ReturnsViewWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEnv = GetMockEnvironment();
            var controller = new ClaimsController(context, mockEnv.Object);

            // Add model error to simulate validation failure
            controller.ModelState.AddModelError("LecturerName", "Lecturer name is required");

            var invalidClaim = new Claim(); // Empty claim

            // Act
            var result = await controller.Submit(invalidClaim, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(0, await context.Claims.CountAsync()); // No claim saved
        }

        #endregion
    }
}