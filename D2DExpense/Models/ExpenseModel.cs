using System;

namespace D2DExpense.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; } // For Day-wise
        public string Month { get; set; } // For Month-wise
        public string Type { get; set; } // Expense or Investment
        public string Category { get; set; } // Expense/Investment category
        public decimal Amount { get; set; }
    }


}
