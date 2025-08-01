namespace D2DExpense.Models
{
    public class SIPTransaction
    {
        public int TransactionId { get; set; } // 👈 ADD THIS
        public DateTime NAVDate { get; set; }
        public decimal NAV { get; set; }
        public decimal Units { get; set; }

        // Optional - computed property (if needed in future)
        public decimal CurrentValue => NAV * Units;
    }


    public class SIPViewModel
    {
        public int SIPId { get; set; }
        public string SIPName { get; set; }
        public decimal MonthlyAmount { get; set; }
        public DateTime StartDate { get; set; }
         public string SchemeCode { get; set; }

        public List<SIPTransaction> Transactions { get; set; } = new();
        public decimal InvestedAmount { get; set; } // Add this
        public decimal CurrentValue { get; set; }
    }
}
