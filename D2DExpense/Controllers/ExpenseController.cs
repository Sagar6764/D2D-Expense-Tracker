using D2DExpense.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace D2DExpense.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public ExpenseController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        private string connectionString => _configuration.GetConnectionString("DefaultConnection");

        public IActionResult Index(int page = 1)
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");
            List<Expense> expenses = new List<Expense>();
            decimal totalExpense = 0;
            decimal totalInvestment = 0;
            decimal total = 0;
            int pageSize = 30;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Step 1: Get total expense & total investment (not paginated)
                string totalQuery = @"
            SELECT 
                SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END) AS TotalExpense,
                SUM(CASE WHEN Type = 'Investment' THEN Amount ELSE 0 END) AS TotalInvestment
            FROM Expenses
            WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(totalQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalExpense = reader["TotalExpense"] != DBNull.Value ? Convert.ToDecimal(reader["TotalExpense"]) : 0;
                            totalInvestment = reader["TotalInvestment"] != DBNull.Value ? Convert.ToDecimal(reader["TotalInvestment"]) : 0;
                        }
                    }
                }

                // Step 2: Get paginated list of expenses (sorted by Date descending)
                string expenseQuery = @"
            SELECT * FROM (
                SELECT ROW_NUMBER() OVER (ORDER BY 
                    CASE 
                        WHEN [Date] IS NOT NULL THEN [Date] 
                        ELSE '1900-01-01' 
                    END DESC) AS RowNum, * 
                FROM Expenses 
                WHERE Email = @Email
            ) AS RowConstrainedResult
            WHERE RowNum >= @StartRow AND RowNum <= @EndRow
            ORDER BY RowNum";

                using (SqlCommand cmd = new SqlCommand(expenseQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
                    cmd.Parameters.AddWithValue("@EndRow", page * pageSize);

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        expenses.Add(new Expense
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Email = reader["Email"].ToString(),
                            Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]) : DateTime.MinValue,
                            Month = reader["Month"] != DBNull.Value ? reader["Month"].ToString() : "",
                            Type = reader["Type"].ToString(),
                            Category = reader["Category"].ToString(),
                            Amount = Convert.ToDecimal(reader["Amount"])
                        });
                    }
                }
            }

            ViewBag.TotalExpense = totalExpense;
            ViewBag.TotalInvestment = totalInvestment;
            ViewBag.Total = totalExpense + totalInvestment;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = expenses.Count == pageSize;

            return View(expenses);
        }




        [HttpPost]
        public IActionResult AddExpense(Expense model)
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Expenses (Email, Date, Month, Type, Category, Amount) VALUES (@Email, @Date, @Month, @Type, @Category, @Amount)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                   if (!string.IsNullOrEmpty(model.Month))
{
    // Month-wise entry: clear date
    cmd.Parameters.AddWithValue("@Date", DBNull.Value);
    cmd.Parameters.AddWithValue("@Month", model.Month);
}
else
{
    // Day-wise entry: clear month
    cmd.Parameters.AddWithValue("@Date", model.Date != DateTime.MinValue ? model.Date : (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@Month", DBNull.Value);
}

                    cmd.Parameters.AddWithValue("@Type", model.Type);
                    cmd.Parameters.AddWithValue("@Category", model.Category);
                    cmd.Parameters.AddWithValue("@Amount", model.Amount);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditExpense(int id)
        {
            Expense expense = new Expense();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Expenses WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        expense.Id = Convert.ToInt32(reader["Id"]);
                        expense.Email = reader["Email"].ToString();
                        expense.Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]) : DateTime.MinValue;
                        expense.Month = reader["Month"] != DBNull.Value ? reader["Month"].ToString() : "";
                        expense.Type = reader["Type"].ToString();
                        expense.Category = reader["Category"].ToString();
                        expense.Amount = Convert.ToDecimal(reader["Amount"]);
                    }
                }
            }
            return View("Edit", expense);
        }

        [HttpPost]
        public IActionResult EditExpense(Expense model)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Expenses SET Date = @Date, Month = @Month, Type = @Type, Category = @Category, Amount = @Amount WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@Date", model.Date != DateTime.MinValue ? model.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Month", string.IsNullOrEmpty(model.Month) ? (object)DBNull.Value : model.Month);
                    cmd.Parameters.AddWithValue("@Type", model.Type);
                    cmd.Parameters.AddWithValue("@Category", model.Category);
                    cmd.Parameters.AddWithValue("@Amount", model.Amount);
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["SuccessMessage"] = "Expense updated successfully!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult DeleteExpense(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Expenses WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}
