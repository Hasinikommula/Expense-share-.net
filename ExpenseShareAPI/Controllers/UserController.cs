using ExpenseShareAPI.Data;
using ExpenseShareAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ✅ Only logged-in users can access
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }




        [HttpGet()]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Where(u => !u.IsAdmin)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email
                })
                .ToListAsync();

            return Ok(users);
        }

        //Get user by Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.IsAdmin
            });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetUserDashboard()
        {
            // Get the ID of the currently logged-in user from the token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            // Finds all groups the user is a member of
            var userGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.GroupMembers) // To get member count
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Expenses) // To get total expenses
                .Select(gm => gm.Group)
                .ToListAsync();

            var dashboardGroups = new List<object>();

            foreach (var group in userGroups)
            {
                // Calculate total expenses for the group
                var totalExpenses = group.Expenses.Sum(e => e.Amount);

               
               
                var expenseService = HttpContext.RequestServices.GetRequiredService<IExpenseService>();
                var (balances, _) = await expenseService.CalculateBalancesAsync(group.Id);


                var userBalance = balances.FirstOrDefault(b => b.Key == userId).Value;


                dashboardGroups.Add(new
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    MemberCount = group.GroupMembers.Count,
                    TotalAmount = totalExpenses,
                    UserBalance = userBalance
                });
            }

            return Ok(dashboardGroups);
        }
        [HttpGet("{id}/groups")]
        public async Task<IActionResult> GetGroupsForUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.GroupMembers)
                    .ThenInclude(gm => gm.Group)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var groups = user.GroupMembers
                .Select(gm => new
                {
                    gm.Group.Id,
                    gm.Group.Name,
                    MemberCount = gm.Group.GroupMembers.Count
                })
                .ToList();

            return Ok(groups);
        }

    }
}
