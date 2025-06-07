using System.ComponentModel.DataAnnotations;

namespace D2DExpense.Models
{
    public class RegisterModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.(com|org|net|edu|gov|mil|int|info|biz|in)$",
           ErrorMessage = "Email must be a valid address.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%&*])[A-Za-z\d!@#$%&*]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, with at least 1 uppercase, 1 lowercase, 1 number, and 1 special character (!@#$%).")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        // New Fields
        [Required]

        [Range(1, double.MaxValue, ErrorMessage = "Monthly Income must be a positive number.")]
        public decimal MonthlyIncome { get; set; }

        [Required(ErrorMessage = "Age is required.")]
        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
        public int? Age { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile Number must be 10 digits.")]
        public string MobileNumber { get; set; }


        //public string ProfilePicture { get; set; }
    }
}
