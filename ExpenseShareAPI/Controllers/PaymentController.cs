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

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // POST: /api/Payment
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDTO dto)
        {
            try
            {
                var result = await _paymentService.CreatePaymentAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: /api/Payment/{id}/complete
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

        // GET: /api/Payment/group/{groupId}
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetPaymentsForGroup(int groupId, bool? isCompleted = null)
        {
            var payments = await _paymentService.GetPaymentsForGroupAsync(groupId, isCompleted);
            return Ok(payments);
        }
    }
}