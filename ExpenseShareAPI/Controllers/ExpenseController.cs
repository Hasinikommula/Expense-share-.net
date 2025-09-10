using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.ExpenseShareModels;
using ExpenseShareAPI.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseShareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly AppDbContext _context;

        public ExpenseController(IExpenseService expenseService, AppDbContext context)
        {
            _expenseService = expenseService;
            _context = context;
        }

        // ✅ Add Expense
        [HttpPost]
        public async Task<IActionResult> AddExpense([FromBody]  ExpenseCreatingDTO dto )
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.GroupMembers)
                    .FirstOrDefaultAsync(g => g.Id == dto.GroupId);

                if (group == null)
                    return BadRequest(new { message = "Group not found" });

                if (!group.GroupMembers.Any(gm => gm.UserId == dto.PaidById))
                    return BadRequest(new { message = "Payer must be a group member" });

                var expense = new Expense
                {
                    Description = dto.Description,
                    Amount = dto.Amount,
                    PaidById = dto.PaidById,
                    GroupId = dto.GroupId,
                    CreatedAt = DateTime.UtcNow
                };

                await _expenseService.AddExpenseAsync(expense);

                var expenseDto = new ExpenseDTO
                {
                    Id = expense.Id,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    PaidById = expense.PaidById,
                    GroupId = expense.GroupId,
                    CreatedAt = expense.CreatedAt
                };

                return Ok(new { message = "Expense added", expense = expenseDto });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Get Expenses
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetExpenses(int groupId)
        {
            var expenses = await _context.Expenses
                .Include(e => e.PaidBy)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();

            var expenseDtos = expenses.Select(e => new ExpenseDTO
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                PaidById = e.PaidById,
                GroupId = e.GroupId,
                CreatedAt = e.CreatedAt
            }).ToList();

            return Ok(expenseDtos);
        }

        // ✅ Get Balances
        [HttpGet("group/{groupId}/balances")]
        public async Task<IActionResult> GetBalances(int groupId)
        {
            try
            {
                var (balances, settlements) = await _expenseService.CalculateBalancesAsync(groupId);
                return Ok(new { balances, settlements });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
