﻿using D2DExpense.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text.Json;

namespace D2DExpense.Controllers
{
    public class SIPController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly INAVService _navService;

        public SIPController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, INAVService navService)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _navService = navService;
        }

        private string GetConnectionString() => _configuration.GetConnectionString("DefaultConnection");

        private void EnsureTablesExist(SqlConnection conn)
        {
            string createSIPDetails = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SIPDetails' AND xtype='U')
                CREATE TABLE SIPDetails (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Email NVARCHAR(100) NOT NULL,
                    SIPName NVARCHAR(100) NOT NULL,
                    MonthlyAmount DECIMAL(18, 2) NOT NULL,
                    StartDate DATE NOT NULL,
                    SchemeCode NVARCHAR(10) NOT NULL
                )";

            string createSIPTransactions = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SIPTransactions' AND xtype='U')
                CREATE TABLE SIPTransactions (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    SIPId INT NOT NULL FOREIGN KEY REFERENCES SIPDetails(Id) ON DELETE CASCADE,
                    NAVDate DATE NOT NULL,
                    NAV DECIMAL(18, 4) NOT NULL,
                    Units DECIMAL(18, 4) NOT NULL
                )";

            new SqlCommand(createSIPDetails, conn).ExecuteNonQuery();
            new SqlCommand(createSIPTransactions, conn).ExecuteNonQuery();
        }

        public async Task<IActionResult> Index()
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");
            var model = new List<SIPViewModel>();
            decimal totalInvested = 0, totalCurrentValue = 0;

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                // Fetch SIPs
                string sipQuery = "SELECT Id, SIPName, MonthlyAmount, StartDate, SchemeCode FROM SIPDetails WHERE Email = @Email";
                SqlCommand sipCmd = new SqlCommand(sipQuery, conn);
                sipCmd.Parameters.AddWithValue("@Email", email);
                SqlDataReader sipReader = sipCmd.ExecuteReader();

                while (sipReader.Read())
                {
                    model.Add(new SIPViewModel
                    {
                        SIPId = Convert.ToInt32(sipReader["Id"]),
                        SIPName = sipReader["SIPName"].ToString(),
                        MonthlyAmount = Convert.ToDecimal(sipReader["MonthlyAmount"]),
                        StartDate = Convert.ToDateTime(sipReader["StartDate"]),
                        SchemeCode = sipReader["SchemeCode"].ToString()
                    });
                }
                sipReader.Close();

                foreach (var sip in model)
                {
                    string txQuery = "SELECT Id, NAVDate, NAV, Units FROM SIPTransactions WHERE SIPId = @Id ORDER BY NAVDate";
                    SqlCommand txCmd = new SqlCommand(txQuery, conn);
                    txCmd.Parameters.AddWithValue("@Id", sip.SIPId);
                    SqlDataReader txReader = txCmd.ExecuteReader();

                    while (txReader.Read())
                    {
                        var tx = new SIPTransaction
                        {
                            TransactionId = Convert.ToInt32(txReader["Id"]),
                            NAVDate = Convert.ToDateTime(txReader["NAVDate"]),
                            NAV = Convert.ToDecimal(txReader["NAV"]),
                            Units = Convert.ToDecimal(txReader["Units"])
                        };
                        
                        sip.Transactions.Add(tx);
                    }
                    txReader.Close();

                    decimal invested = sip.Transactions.Sum(t => t.Units * t.NAV);
                    decimal unitsHeld = sip.Transactions.Sum(t => t.Units);
                    decimal latestNav = 0;

                    if (!string.IsNullOrEmpty(sip.SchemeCode))
                    {
                        try
                        {
                            latestNav = await _navService.GetLatestNAVAsync(sip.SchemeCode);
                        }
                        catch
                        {
                            latestNav = 0;
                        }
                    }

                    decimal currentValue = unitsHeld * latestNav;

                    // Assign per-SIP values
                    sip.InvestedAmount = invested;
                    sip.CurrentValue = currentValue;

                    // For totals
                    totalInvested += invested;
                    totalCurrentValue += currentValue;

                }

            }

            ViewBag.TotalInvested = totalInvested;
            ViewBag.TotalCurrentValue = totalCurrentValue;

            return View(model);
        }

        [HttpPost]
        public IActionResult AddSIP(string sipName, decimal monthlyAmount, DateTime startDate, string schemeCode)
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand cmd = new SqlCommand("INSERT INTO SIPDetails (Email, SIPName, MonthlyAmount, StartDate, SchemeCode) VALUES (@Email, @Name, @Amt, @Start, @Code)", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Name", sipName);
                cmd.Parameters.AddWithValue("@Amt", monthlyAmount);
                cmd.Parameters.AddWithValue("@Start", startDate);
                cmd.Parameters.AddWithValue("@Code", schemeCode);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddSIPTransaction(int sipId, DateTime navDate, decimal nav)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand getAmountCmd = new SqlCommand("SELECT MonthlyAmount FROM SIPDetails WHERE Id = @Id", conn);
                getAmountCmd.Parameters.AddWithValue("@Id", sipId);
                decimal amount = Convert.ToDecimal(getAmountCmd.ExecuteScalar());

                decimal units = amount / nav;

                SqlCommand cmd = new SqlCommand("INSERT INTO SIPTransactions (SIPId, NAVDate, NAV, Units) VALUES (@SIPId, @Date, @NAV, @Units)", conn);
                cmd.Parameters.AddWithValue("@SIPId", sipId);
                cmd.Parameters.AddWithValue("@Date", navDate);
                cmd.Parameters.AddWithValue("@NAV", nav);
                cmd.Parameters.AddWithValue("@Units", units);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EditSIP(int sipId, string sipName, decimal monthlyAmount, DateTime startDate, string schemeCode)
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand cmd = new SqlCommand("UPDATE SIPDetails SET SIPName = @Name, MonthlyAmount = @Amt, StartDate = @Start, SchemeCode = @Code WHERE Id = @Id AND Email = @Email", conn);
                cmd.Parameters.AddWithValue("@Id", sipId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Name", sipName);
                cmd.Parameters.AddWithValue("@Amt", monthlyAmount);
                cmd.Parameters.AddWithValue("@Start", startDate);
                cmd.Parameters.AddWithValue("@Code", schemeCode);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EditSIPTransaction(int transactionId, int sipId, DateTime navDate, decimal nav)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand amtCmd = new SqlCommand("SELECT MonthlyAmount FROM SIPDetails WHERE Id = @SIPId", conn);
                amtCmd.Parameters.AddWithValue("@SIPId", sipId);
                decimal amount = Convert.ToDecimal(amtCmd.ExecuteScalar());

                decimal units = amount / nav;

                SqlCommand cmd = new SqlCommand(@"
                    UPDATE SIPTransactions 
                    SET NAVDate = @NAVDate, NAV = @NAV, Units = @Units 
                    WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", transactionId);
                cmd.Parameters.AddWithValue("@NAVDate", navDate);
                cmd.Parameters.AddWithValue("@NAV", nav);
                cmd.Parameters.AddWithValue("@Units", units);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteSIPTransaction(int transactionId)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand cmd = new SqlCommand("DELETE FROM SIPTransactions WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", transactionId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> GetSchemeCodeOnline(string sipName)
        {
            using (var client = new HttpClient())
            {
                string query = $"https://api.mfapi.in/mf/search?q={Uri.EscapeDataString(sipName)}";
                var response = await client.GetAsync(query);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    try
                    {
                        var results = JsonSerializer.Deserialize<List<MFSearchResult>>(json, options);
                        var firstMatch = results?.FirstOrDefault();

                        if (firstMatch != null)
                        {
                            return Json(new { schemeCode = firstMatch.SchemeCode.ToString() });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { schemeCode = "", error = "JSON parsing error", rawResponse = json });
                    }
                }
            }

            return Json(new { schemeCode = "" });
        }



        // DTO for deserialization
        private class MFSearchResult
        {
            public string SchemeName { get; set; }
            public int SchemeCode { get; set; } // changed from string to int
        }



        [HttpPost]
        public IActionResult DeleteSIP(int sipId)
        {
            string email = _httpContextAccessor.HttpContext.Session.GetString("UserEmail");

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                EnsureTablesExist(conn);

                SqlCommand deleteTxCmd = new SqlCommand("DELETE FROM SIPTransactions WHERE SIPId = @Id", conn);
                deleteTxCmd.Parameters.AddWithValue("@Id", sipId);
                deleteTxCmd.ExecuteNonQuery();

                SqlCommand deleteSIPCmd = new SqlCommand("DELETE FROM SIPDetails WHERE Id = @Id AND Email = @Email", conn);
                deleteSIPCmd.Parameters.AddWithValue("@Id", sipId);
                deleteSIPCmd.Parameters.AddWithValue("@Email", email);
                deleteSIPCmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
