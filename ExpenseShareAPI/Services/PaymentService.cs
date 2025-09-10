
using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.ExpenseShareModels;
using Microsoft.EntityFrameworkCore;
using System;
using static ExpenseShareAPI.Services.ExpenseService;

namespace ExpenseShareAPI.Services
{
    public interface IPaymentService
    {
        Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO dto);
        Task<(bool success, Dictionary<int, decimal> balances, List<SettlementDto> settlements)> CompletePaymentAsync(int paymentId);
        Task<List<PaymentDTO>> GetPaymentsForGroupAsync(int groupId, bool? isCompleted = null);
    }

    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentDTO> CreatePaymentAsync(CreatePaymentDTO dto)
        {
            if (!_context.Users.Any(u => u.Id == dto.FromUserId))
                throw new Exception("FromUser not found");

            if (!_context.Users.Any(u => u.Id == dto.ToUserId))
                throw new Exception("ToUser not found");

            if (!_context.Groups.Any(g => g.Id == dto.GroupId))
                throw new Exception("Group not found");

            var payment = new Payment
            {
                Amount = dto.Amount,
                FromUserId = dto.FromUserId,
                ToUserId = dto.ToUserId,
                GroupId = dto.GroupId,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return new PaymentDTO
            {
                Id = payment.Id,
                Amount = payment.Amount,
                FromUserId = payment.FromUserId,
                ToUserId = payment.ToUserId,
                GroupId = payment.GroupId,
                CreatedAt = payment.CreatedAt,
                IsCompleted = payment.IsCompleted
            };
        }

        public async Task<(bool success, Dictionary<int, decimal> balances, List<SettlementDto> settlements)> CompletePaymentAsync(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return (false, null, null);

           
            payment.IsCompleted = true;
            await _context.SaveChangesAsync();

           
            var expenseService = new ExpenseService(_context);
            var (balances, settlements) = await expenseService.CalculateBalancesAsync(payment.GroupId);

            return (true, balances, settlements);
        }

        public async Task<List<PaymentDTO>> GetPaymentsForGroupAsync(int groupId, bool? isCompleted = null)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Where(p => p.GroupId == groupId);

            if (isCompleted.HasValue)
                query = query.Where(p => p.IsCompleted == isCompleted.Value);

            return await query
                .Select(p => new PaymentDTO
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    FromUserId = p.FromUserId,
                    ToUserId = p.ToUserId,
                    GroupId = p.GroupId,
                    CreatedAt = p.CreatedAt,
                    IsCompleted = p.IsCompleted
                })
                .ToListAsync();
        }
    }
}