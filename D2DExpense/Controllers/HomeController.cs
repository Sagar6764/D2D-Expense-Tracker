using D2DExpense.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace D2DExpense.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        public IActionResult About()
        {
            return View();
        }

        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        private string connectionString => _configuration.GetConnectionString("DefaultConnection");

        // Index page: Show login modal if not logged in, else show dashboard
        public IActionResult Index()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");

            // Check if the user is logged in
            if (userId == null)
            {
                return View("Index"); // Show the public view with login options
            }
            else
            {
                return RedirectToAction("Dashboard"); // Redirect to dashboard if logged in
            }
        }

        // Dashboard page: Show financial summary with the graph if the user is logged in
        public IActionResult Dashboard()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Index"); // If not logged in, redirect to index page
            }

            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");
            decimal monthlyIncome = 0;
            decimal totalExpense = 0;
            decimal totalInvestment = 0;

            // Retrieve financial data from the database
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Fetch Monthly Income from Users table
                string incomeQuery = "SELECT MonthlyIncome FROM Users WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(incomeQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        monthlyIncome = Convert.ToDecimal(result);
                    }
                }

                // Fetch Expenses and Investments Data for the current month
                string currentMonth = DateTime.Now.ToString("yyyy-MM"); // Will be "2025-04"
                string expenseQuery = @"
    SELECT * FROM Expenses 
    WHERE Email = @Email AND (
        (Date IS NOT NULL AND FORMAT(Date, 'yyyy-MM') = @CurrentMonth)
        OR (Month = @CurrentMonth)
    )";
                using (SqlCommand cmd = new SqlCommand(expenseQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@CurrentMonth", currentMonth);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var amount = Convert.ToDecimal(reader["Amount"]);
                        var type = reader["Type"].ToString();

                        if (type == "Expense") totalExpense += amount;
                        else if (type == "Investment") totalInvestment += amount;
                    }
                }
            }

            // Calculate Savings
            decimal totalSavings = monthlyIncome - (totalExpense + totalInvestment);

            // Pass data to the view
            ViewBag.MonthlyIncome = monthlyIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.TotalInvestment = totalInvestment;
            ViewBag.TotalSavings = totalSavings;

            return View(); // Return the view to display the dashboard
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Error handling page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
