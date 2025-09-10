using ExpenseShareAPI.ExpenseShareModels;
using ExpenseSharingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseShareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Login model)
        {
            var user = _authService.Authenticate(model.Email, model.Password);
            if (user == null)
                return BadRequest(new { message = "Email or password is incorrect" });

            var token = _authService.GenerateJwtToken(user);

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                Token = token
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Register model)
        {
            try
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    IsAdmin = model.IsAdmin
                };

                _authService.Register(user, model.Password);

                var token = _authService.GenerateJwtToken(user);

                return Ok(new
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
