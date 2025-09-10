using ExpenseShareAPI.Data;
using ExpenseShareAPI.ExpenseShareModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ExpenseShareAPI.Services.ExpenseService;

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
            {
                throw new Exception($"Group with ID {groupId} not found.");
            }

           
            var groupMembers = group.GroupMembers ?? new List<GroupMember>();

          
            var validGroupMembers = groupMembers.Where(gm => gm.User != null).ToList();

            if (!validGroupMembers.Any())
            {
                return (new Dictionary<int, decimal>(), new List<SettlementDto>());
            }

            var memberIds = validGroupMembers.Select(gm => gm.UserId).ToList();
            var userMap = validGroupMembers.ToDictionary(gm => gm.UserId, gm => gm.User.Username);

            // Safely handle a potentially null Expenses collection
            var expenses = group.Expenses ?? new List<Expense>();
            var totalExpenses = expenses.Sum(e => e.Amount);
            var equalShare = memberIds.Count > 0 ? Math.Round(totalExpenses / memberIds.Count, 2) : 0m;

            var balances = memberIds.ToDictionary(id => id, _ => 0m);
            var paidAmounts = memberIds.ToDictionary(id => id, _ => 0m);

            foreach (var expense in expenses)
            {
                if (paidAmounts.ContainsKey(expense.PaidById))
                    paidAmounts[expense.PaidById] += expense.Amount;
            }

            foreach (var memberId in memberIds)
            {
                balances[memberId] = Math.Round(paidAmounts.GetValueOrDefault(memberId) - equalShare, 2);
            }

            var payments = await _context.Payments
                .Where(p => p.GroupId == groupId && p.IsCompleted)
                .ToListAsync();

            foreach (var payment in payments)
            {
                if (balances.ContainsKey(payment.FromUserId))
                    balances[payment.FromUserId] += payment.Amount;

                if (balances.ContainsKey(payment.ToUserId))
                    balances[payment.ToUserId] -= payment.Amount;
            }

            var settlements = new List<SettlementDto>();
            var debtors = balances.Where(b => b.Value < 0).ToDictionary(b => b.Key, b => b.Value);
            var creditors = balances.Where(b => b.Value > 0).ToDictionary(b => b.Key, b => b.Value);

            while (debtors.Any() && creditors.Any())
            {
                var creditorEntry = creditors.OrderByDescending(c => c.Value).First();
                var debtorEntry = debtors.OrderBy(d => d.Value).First();

                var amountToSettle = Math.Min(creditorEntry.Value, Math.Abs(debtorEntry.Value));

                if (amountToSettle == 0) break;

                settlements.Add(new SettlementDto
                {
                    FromUserId = debtorEntry.Key,
                    ToUserId = creditorEntry.Key,
                    FromUserName = userMap.GetValueOrDefault(debtorEntry.Key, "Unknown"),
                    ToUserName = userMap.GetValueOrDefault(creditorEntry.Key, "Unknown"),
                    Amount = Math.Round(amountToSettle, 2)
                });

                creditors[creditorEntry.Key] -= amountToSettle;
                debtors[debtorEntry.Key] += amountToSettle;

                if (Math.Abs(creditors[creditorEntry.Key]) < 0.01m)
                    creditors.Remove(creditorEntry.Key);

                if (Math.Abs(debtors[debtorEntry.Key]) < 0.01m)
                    debtors.Remove(debtorEntry.Key);
            }

            return (balances, settlements);
        }
       
        public class SettlementDto
        {
            public int FromUserId { get; set; }
            public string FromUserName { get; set; }
            public int ToUserId { get; set; }
            public string ToUserName { get; set; }
            public decimal Amount { get; set; }
        }
    }
}