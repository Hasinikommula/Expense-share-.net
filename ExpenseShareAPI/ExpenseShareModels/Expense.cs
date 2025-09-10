using System.Text.RegularExpressions;

namespace ExpenseShareAPI.ExpenseShareModels
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int PaidById { get; set; }   // Foreign Key-> User who paid
        public int GroupId { get; set; }    // Foreign Key-> Group
        public DateTime CreatedAt { get; set; }

        public virtual User PaidBy { get; set; }
        public virtual Group Group { get; set; }
    }
}
