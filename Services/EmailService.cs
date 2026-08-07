// Services/EmailService.cs
using HuongQueViet.Models;
using System.Net;
using System.Net.Mail;

namespace HuongQueViet.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) { _config = config; }
        public async Task SendAsync(string toEmail, string subject, string body)
        {
            using var client = new SmtpClient(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]!))
            {
                Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Password"]),
                EnableSsl = true
            };
            await client.SendMailAsync(new MailMessage(_config["Smtp:From"]!, toEmail, subject, body));
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
        Task NotifyOrderPlaced(Order order, string email, string phone);
        Task NotifyStatusChanged(Order order, string email, string phone);
    }

    public class NotificationService : INotificationService
    {
        private readonly EmailService _email;
        private readonly MockSmsService _sms;
        public NotificationService(EmailService email, MockSmsService sms) { _email = email; _sms = sms; }

        public async Task NotifyOrderPlaced(Order order, string email, string phone)
        {
            await _email.SendAsync(email, $"Xác nhận đơn hàng #{order.Id}", $"Tổng tiền: {order.TotalAmount:N0} đ");
            await _sms.SendAsync(phone, $"Don hang #{order.Id} da duoc tiep nhan");
        }
        public async Task NotifyStatusChanged(Order order, string email, string phone)
        {
            await _email.SendAsync(email, $"Cập nhật đơn hàng #{order.Id}", $"Trạng thái: {order.Status}");
            await _sms.SendAsync(phone, $"Don hang #{order.Id}: {order.Status}");
        }
    }
}