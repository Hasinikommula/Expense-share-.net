namespace ExpenseShareAPI.DTO_s
{
    public class CreatePaymentDTO
    {
        public decimal Amount { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public int GroupId { get; set; }
    }
}
