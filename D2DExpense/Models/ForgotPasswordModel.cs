
    using System.ComponentModel.DataAnnotations;

    namespace D2DExpense.Models
    {
        public class ForgotPasswordModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.(com|org|net|edu|gov|mil|int|info|biz|in)$",
                ErrorMessage = "Email must be a valid address.")]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%&*])[A-Za-z\d!@#$%&*]{8,}$",
                ErrorMessage = "Password must be at least 8 characters long, with at least 1 uppercase, 1 lowercase, 1 number, and 1 special character (!@#$%&*).")]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; }
        }
    }


