using System;

namespace D2DExpense.Models
{
    public class ReportItem
    {
        public DateTime? Date { get; set; }  // For daywise reports
        public string Month { get; set; }    // For monthwise reports
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
    }


}
