namespace ExpenseShareAPI.DTO_s
{
    public class ExpenseCreatingDTO
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int PaidById { get; set; }
        public int GroupId { get; set; }
    }
}
