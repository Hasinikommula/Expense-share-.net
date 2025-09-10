namespace ExpenseShareAPI.ExpenseShareModels
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }

    
        public int FromUserId { get; set; }

        
        public int ToUserId { get; set; }

        public int GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCompleted { get; set; }

        public virtual User FromUser { get; set; }
        public virtual User ToUser { get; set; }
        public virtual Group Group { get; set; }
    }
}
