using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using D2DExpense.Models;
using Microsoft.AspNetCore.Http;

namespace D2DExpense.Controllers
{
    public class ReportController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GenerateReport(string reportType, string selectedDate, string selectedMonth, string selectedYear,
                                            string categoryType, string category, string investmentCategory)
        {
            List<ReportItem> reportItems = new();
            decimal totalAmountSpent = 0;

            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");
            string query = "SELECT Date, Month, Category, Amount, Type FROM Expenses WHERE Email = @Email";

            if (!string.IsNullOrEmpty(reportType))
            {
                switch (reportType)
                {
                    case "Day":
                        if (!string.IsNullOrEmpty(selectedDate))
                            query += " AND CAST(Date AS DATE) = @SelectedDate";
                        break;
                    case "Month":
                        if (!string.IsNullOrEmpty(selectedMonth))
                            query += " AND (FORMAT(Date, 'yyyy-MM') = @SelectedMonth OR Month = @SelectedMonth)";
                        break;
                    case "Year":
                        if (!string.IsNullOrEmpty(selectedYear))
                            query += " AND (YEAR(Date) = @SelectedYear OR LEFT(Month, 4) = @SelectedYear)";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(categoryType))
            {
                query += " AND Type = @Type";

                if (categoryType == "Expense" && !string.IsNullOrEmpty(category))
                    query += " AND Category = @Category";
                else if (categoryType == "Investment" && !string.IsNullOrEmpty(investmentCategory))
                    query += " AND Category = @InvestmentCategory";
            }

            // 👇 Add this line to sort from current to past
            query += " ORDER BY Date DESC";


            using (SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    if (!string.IsNullOrEmpty(reportType))
                    {
                        if (reportType == "Day" && !string.IsNullOrEmpty(selectedDate))
                            cmd.Parameters.AddWithValue("@SelectedDate", DateTime.Parse(selectedDate));
                        if (reportType == "Month" && !string.IsNullOrEmpty(selectedMonth))
                            cmd.Parameters.AddWithValue("@SelectedMonth", selectedMonth);
                        if (reportType == "Year" && !string.IsNullOrEmpty(selectedYear))
                            cmd.Parameters.AddWithValue("@SelectedYear", selectedYear);
                    }

                    if (!string.IsNullOrEmpty(categoryType))
                    {
                        cmd.Parameters.AddWithValue("@Type", categoryType);
                        if (categoryType == "Expense" && !string.IsNullOrEmpty(category))
                            cmd.Parameters.AddWithValue("@Category", category);
                        else if (categoryType == "Investment" && !string.IsNullOrEmpty(investmentCategory))
                            cmd.Parameters.AddWithValue("@InvestmentCategory", investmentCategory);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportItems.Add(new ReportItem
                            {
                                Date = reader["Date"] as DateTime?,
                                Month = reader["Month"]?.ToString(),
                                Category = reader["Category"]?.ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                Type = reader["Type"]?.ToString()
                            });

                            totalAmountSpent += Convert.ToDecimal(reader["Amount"]);
                        }
                    }
                }
            }

            ViewData["ReportData"] = reportItems;
            ViewData["TotalAmountSpent"] = totalAmountSpent;
            return View("Index");
        }
    }
}
