using System;

namespace FinanceApp.Models
{
    public class FinanceRecord
    {
        public string Type { get; set; }

        public string Category { get; set; }

        public double Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
