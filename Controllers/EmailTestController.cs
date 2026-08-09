using HuongQueViet.Services;
using Microsoft.AspNetCore.Mvc;

namespace HuongQueViet.Controllers
{
    [Route("email")]
    public class EmailTestController : Controller
    {
        private readonly EmailService _email;
        private readonly ILogger<EmailTestController> _logger;
        public EmailTestController(EmailService email, ILogger<EmailTestController> logger)
        {
            _email = email;
            _logger = logger;
        }

        // GET /email/test?to=someone@example.com&subject=hi
        [HttpGet("test")]
        public async Task<IActionResult> Test(string? to, string? subject, string? body)
        {
            to ??= "test@example.com";
            subject ??= "Test email from HuongQueViet";
            body ??= $"Test email body at {DateTime.UtcNow:O}";

            try
            {
                await _email.SendAsync(to, subject, body);
                _logger.LogInformation("Test email sent to {To}", to);
                return Content($"Sent test email to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {To}", to);
                return StatusCode(500, "Failed to send email: " + ex.Message);
            }
        }
    }
}
