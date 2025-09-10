using ExpenseShareAPI.Data;
using ExpenseShareAPI.ExpenseShareModels;

using Microsoft.EntityFrameworkCore;

namespace ExpenseShareAPI.Services
{
    public interface IExpenseService
    {
        Task AddExpenseAsync(Expense expense);
     
        Task<(Dictionary<int, decimal> balances, List<SettlementDto> settlements)> CalculateBalancesAsync(int groupId);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly AppDbContext _context;

        public ExpenseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddExpenseAsync(Expense expense)
        {
            expense.CreatedAt = DateTime.UtcNow;
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
        }

        public async Task<(Dictionary<int, decimal> balances, List<SettlementDto> settlements)> CalculateBalancesAsync(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Expenses)
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                throw new Exception("Group not found");

            var memberIds = group.GroupMembers.Select(gm => gm.UserId).ToList();
            if (!memberIds.Any())
                throw new Exception("No members in this group");

            // 1️⃣ Calculate balances from expenses
            var totalExpenses = group.Expenses.Sum(e => e.Amount);
            var equalShare = Math.Round(totalExpenses / memberIds.Count, 2);

            var balances = memberIds.ToDictionary(id => id, _ => 0m);
            var paidAmounts = memberIds.ToDictionary(id => id, _ => 0m);

            foreach (var expense in group.Expenses)
            {
                if (paidAmounts.ContainsKey(expense.PaidById))
                    paidAmounts[expense.PaidById] += expense.Amount;
            }

            foreach (var memberId in memberIds)
            {
                balances[memberId] = Math.Round(paidAmounts[memberId] - equalShare, 2);
            }

            // 2️⃣ Adjust balances based on completed payments
            var payments = await _context.Payments
                .Where(p => p.GroupId == groupId && p.IsCompleted)
                .ToListAsync();

            foreach (var payment in payments)
            {
                if (balances.ContainsKey(payment.FromUserId))
                    balances[payment.FromUserId] += payment.Amount; // debtor paid

                if (balances.ContainsKey(payment.ToUserId))
                    balances[payment.ToUserId] -= payment.Amount; // creditor received
            }

            // 3️⃣ Recalculate settlements (who owes whom)
            var settlements = new List<SettlementDto>();
            var debtors = balances.Where(b => b.Value < 0).OrderBy(b => b.Value).ToList();
            var creditors = balances.Where(b => b.Value > 0).OrderByDescending(b => b.Value).ToList();

            foreach (var creditor in creditors)
            {
                var amountToReceive = creditor.Value;

                foreach (var debtor in debtors.ToList())
                {
                    if (amountToReceive <= 0) break;

                    var debt = Math.Abs(debtor.Value);
                    if (debt <= 0) continue;

                    var settlementAmount = Math.Min(amountToReceive, debt);

                    settlements.Add(new SettlementDto
                    {
                        FromUserId = debtor.Key,
                        ToUserId = creditor.Key,
                        Amount = Math.Round(settlementAmount, 2)
                    });

                    balances[debtor.Key] += settlementAmount;
                    balances[creditor.Key] -= settlementAmount;
                    amountToReceive -= settlementAmount;

                    if (Math.Abs(balances[debtor.Key]) < 0.01m)
                        debtors.Remove(debtor);
                }
            }

            return (balances, settlements);
        }
    }

    public class SettlementDto
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public decimal Amount { get; set; }
    }
}