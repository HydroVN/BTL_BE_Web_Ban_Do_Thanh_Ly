using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WebBH.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        // Tiêm IConfiguration vào để đọc appsettings.json
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Đọc dữ liệu từ mục EmailSettings trong appsettings.json
            var adminEmail = _configuration["EmailSettings:Email"];
            var appPassword = _configuration["EmailSettings:Password"];

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("RE:COLLECT Support", adminEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(adminEmail, appPassword);
                    await client.SendAsync(emailMessage);
                }
                catch (System.Exception ex)
                {
                    throw new System.Exception("Lỗi gửi email: " + ex.Message);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}