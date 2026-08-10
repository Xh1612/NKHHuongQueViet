// Services/EmailService.cs
using HuongQueViet.Models;
using System.Net;
using System.Net.Mail;

namespace HuongQueViet.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration config, ILogger<EmailService> logger) { _config = config; _logger = logger; }
        public async Task SendAsync(string toEmail, string subject, string body, string? fromOverride = null)
        {
            try
            {
                var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host not configured");
                var portValue = _config["Smtp:Port"] ?? "25";
                if (!int.TryParse(portValue, out var port)) port = 25;
                var user = _config["Smtp:User"];
                var pass = _config["Smtp:Password"];
                var from = fromOverride;
                // determine fallback/from behavior
                if (string.IsNullOrEmpty(from))
                {
                    // if configured to use user as from but no override provided, fall back to FromFallback or From
                    if (bool.TryParse(_config["Smtp:UseUserAsFrom"], out var useUserAsFrom) && useUserAsFrom)
                    {
                        from = _config["Smtp:FromFallback"] ?? _config["Smtp:From"] ?? "no-reply@local";
                    }
                    else
                    {
                        from = _config["Smtp:From"] ?? _config["Smtp:FromFallback"] ?? "no-reply@local";
                    }
                }
                var enableSsl = true;
                if (bool.TryParse(_config["Smtp:EnableSsl"], out var parsedSsl)) enableSsl = parsedSsl;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = enableSsl
                };

                var message = new MailMessage();
                message.From = new MailAddress(from);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} via {Host}:{Port}", toEmail, host, port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
                throw;
            }
        }
    }

    public class MockSmsService
    {
        private readonly ILogger<MockSmsService> _logger;
        public MockSmsService(ILogger<MockSmsService> logger) { _logger = logger; }
        public Task SendAsync(string phone, string message)
        {
            _logger.LogInformation("[MOCK SMS] Gửi tới {Phone}: {Message}", phone, message);
            return Task.CompletedTask;
        }
    }

    public interface INotificationService
    {
        Task NotifyOrderPlaced(Order order, string email, string phone, string? from = null);
        Task NotifyStatusChanged(Order order, string email, string phone, string? from = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly EmailService _email;
        private readonly MockSmsService _sms;
        public NotificationService(EmailService email, MockSmsService sms) { _email = email; _sms = sms; }

        public async Task NotifyOrderPlaced(Order order, string email, string phone, string? from = null)
        {
            await _email.SendAsync(email, $"Xác nhận đơn hàng #{order.Id}", $"Tổng tiền: {order.TotalAmount:N0} đ", from);
            await _sms.SendAsync(phone, $"Don hang #{order.Id} da duoc tiep nhan");
        }
        public async Task NotifyStatusChanged(Order order, string email, string phone, string? from = null)
        {
            await _email.SendAsync(email, $"Cập nhật đơn hàng #{order.Id}", $"Trạng thái: {order.Status}", from);
            await _sms.SendAsync(phone, $"Don hang #{order.Id}: {order.Status}");
        }
    }
}