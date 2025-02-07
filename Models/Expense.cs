using System;

namespace BudgetApp
{
    public class Expense
    {
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        // NEW: Allows marking an expense for deletion.
        public bool IsSelected { get; set; }
    }
}
