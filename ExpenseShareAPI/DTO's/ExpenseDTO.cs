namespace ExpenseShareAPI.DTO_s
{
    public class ExpenseDTO
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaidById { get; set; }
        public int GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
