using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseShareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IExpenseService _expenseService;


        public PaymentController(IPaymentService paymentService,IExpenseService expenseService)
        {
            _paymentService = paymentService;
            _expenseService = expenseService;
        }

      
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDTO dto)
        {
            try
            {
                var paymentResult = await _paymentService.CreatePaymentAsync(dto);
                

              
                var (balances, settlements) = await _expenseService.CalculateBalancesAsync(dto.GroupId);

                return Ok(new
                {
                    message = "Payment recorded successfully",
                    balances,
                    settlements
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompletePayment(int id)
        {
            var result = await _paymentService.CompletePaymentAsync(id);
            if (!result.success) return NotFound();

            return Ok(new
            {
                message = "Payment marked completed",
                balances = result.balances,
                settlements = result.settlements
            });
        }

       
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetPaymentsForGroup(int groupId, bool? isCompleted = null)
        {
            var payments = await _paymentService.GetPaymentsForGroupAsync(groupId, isCompleted);
            return Ok(payments);
        }
    }
}