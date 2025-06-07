using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using D2DExpense.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;


namespace D2DExpense.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string connectionString => _configuration.GetConnectionString("DefaultConnection");

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            ModelState.Remove("MonthlyIncome");
            ModelState.Remove("Age");
            ModelState.Remove("MobileNumber");
            ModelState.Remove("ProfilePicture");
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the email already exists
                    SqlCommand checkEmailCommand = new SqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@Email)", connection);
                    checkEmailCommand.Parameters.AddWithValue("@Email", model.Email);

                    int existingUserCount = (int)checkEmailCommand.ExecuteScalar();
                    if (existingUserCount > 0)
                    {
                        ModelState.AddModelError("Email", "This email is already registered.");
                        return View(model);
                    }

                    // Hash the password
                    string hashedPassword = HashPassword(model.Password);

                    // Insert new user
                    SqlCommand command = new SqlCommand(
                        "INSERT INTO Users (Name, Email, Password) VALUES (@Name, @Email, @Password); SELECT SCOPE_IDENTITY();", connection);
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Email", model.Email);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        int userId = Convert.ToInt32(result);
                        HttpContext.Session.SetInt32("UserID", userId);
                        HttpContext.Session.SetString("UserName", model.Name);

                        TempData["SuccessMessage"] = "Registration successful! Welcome.";
                        return RedirectToAction("Profile", "Account");  // Redirect to Profile page
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "An error occurred while registering.";
                    }
                }
            }
            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the email exists
                using (SqlCommand emailCheckCommand = new SqlCommand(
                    "SELECT Id, Name, Password FROM Users WHERE Email = @Email", connection))
                {
                    emailCheckCommand.Parameters.AddWithValue("@Email", model.Email);

                    using (SqlDataReader reader = emailCheckCommand.ExecuteReader())
                    {
                        if (!reader.Read())  // If no record found, email does not exist
                        {
                            ModelState.AddModelError("Email", "Email not found. Please register.");
                            return View(model);
                        }

                        int userId = reader.GetInt32(0);
                        string userName = reader.GetString(1);
                        string storedHashedPassword = reader.GetString(2);  // Retrieve stored hashed password

                        reader.Close(); // Close the reader before moving to the next step

                        // Hash the entered password and compare
                        string hashedInputPassword = HashPassword(model.Password);
                        if (!hashedInputPassword.Equals(storedHashedPassword))
                        {
                            ModelState.AddModelError("Password", "Incorrect password. Please try again.");
                            return View(model);
                        }

                        // Store user data in session upon successful login
                        HttpContext.Session.SetInt32("UserID", userId);
                        HttpContext.Session.SetString("UserName", userName);
                        HttpContext.Session.SetString("UserEmail", model.Email); // Store email in session

                        TempData["SuccessMessage"] = "Login successful!";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
        }

        // GET: Account/Profile
        public ActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            RegisterModel model = new RegisterModel();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT Name, Email, MonthlyIncome, Age, MobileNumber, ProfilePicture FROM Users WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Name = reader["Name"].ToString();
                            model.Email = reader["Email"].ToString();
                            model.MonthlyIncome = Convert.ToDecimal(reader["MonthlyIncome"]);
                            model.Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : (int?)null;
                            model.MobileNumber = reader["MobileNumber"]?.ToString();
                           
                        }
                    }
                }
            }

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(RegisterModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // Remove unnecessary validation
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(@"
                UPDATE Users 
                SET Name = @Name, 
                    MonthlyIncome = @MonthlyIncome, 
                    Age = @Age, 
                    MobileNumber = @MobileNumber
                WHERE Id = @Id", connection))
                        {
                            command.Parameters.AddWithValue("@Name", model.Name);
                            command.Parameters.AddWithValue("@MonthlyIncome", model.MonthlyIncome);
                            command.Parameters.AddWithValue("@Age", (object?)model.Age ?? DBNull.Value);
                            command.Parameters.AddWithValue("@MobileNumber", (object?)model.MobileNumber ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Id", userId);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                TempData["SuccessMessage"] = "Profile updated successfully!";
                                HttpContext.Session.SetString("UserName", model.Name);
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input. Errors: " + string.Join(", ", errors);
            }

            return RedirectToAction("Profile");
        }
        // ✅ 🔹 Google Login Redirect
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // ✅ 🔹 Google Login Response
        public async Task<IActionResult> GoogleResponse()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
                return RedirectToAction("Login");

            var claims = authenticateResult.Principal.Identities
                .FirstOrDefault()?.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                });

            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (email != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand checkUserCommand = new SqlCommand("SELECT Id, Name FROM Users WHERE Email = @Email", connection);
                    checkUserCommand.Parameters.AddWithValue("@Email", email);
                    SqlDataReader reader = checkUserCommand.ExecuteReader();

                    int userId;
                    if (reader.Read())
                    {
                        userId = reader.GetInt32(0);
                        name = reader.GetString(1);
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();
                        SqlCommand insertUserCommand = new SqlCommand(
                            "INSERT INTO Users (Name, Email, Password) VALUES (@Name, @Email, NULL); SELECT SCOPE_IDENTITY();", connection);
                        insertUserCommand.Parameters.AddWithValue("@Name", name);
                        insertUserCommand.Parameters.AddWithValue("@Email", email);

                        object result = insertUserCommand.ExecuteScalar();
                        userId = Convert.ToInt32(result);
                    }

                    // Store user session
                    HttpContext.Session.SetInt32("UserID", userId);
                    HttpContext.Session.SetString("UserName", name ?? "User");
                    HttpContext.Session.SetString("UserEmail", email);

                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Login");
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return View(model);
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if email exists
                using (SqlCommand checkCommand = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", model.Email);

                    int userExists = (int)checkCommand.ExecuteScalar();
                    if (userExists == 0)
                    {
                        TempData["ErrorMessage"] = "Email not found.";
                        return View(model);
                    }
                }

                // Update password
                string hashedPassword = HashPassword(model.NewPassword);

                using (SqlCommand updateCommand = new SqlCommand("UPDATE Users SET Password = @Password WHERE Email = @Email", connection))
                {
                    updateCommand.Parameters.AddWithValue("@Password", hashedPassword);
                    updateCommand.Parameters.AddWithValue("@Email", model.Email);

                    int rowsAffected = updateCommand.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Something went wrong. Please try again.";
                        return View(model);
                    }
                }
            }
        }


        // Logout
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Password Hashing Function
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
