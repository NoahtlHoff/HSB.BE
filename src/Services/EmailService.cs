using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;


namespace HSB.BE.Services
{
	public interface IEmailService
	{
		Task SendWelcomeEmailAsync(string toEmail, string Name);
	}
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<EmailService> _logger;
		public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task SendWelcomeEmailAsync(string toEmail, string userName)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress("Your App", _configuration["Email:User"]));
				message.To.Add(new MailboxAddress(userName, toEmail));
				message.Subject = "Welcome to Our App!";

				message.Body = new TextPart("html")
				{
					Text = $@"
                        <h1>Welcome {userName}!</h1>
                        <p>Thank you for registering with us.</p>
                        <p>We're excited to have you on board!</p>
                    "
				};

				using var client = new SmtpClient();
				await client.ConnectAsync(
					_configuration["Email:Host"],
					int.Parse(_configuration["Email:Port"]),
					SecureSocketOptions.StartTls
				);

				await client.AuthenticateAsync(
					_configuration["Email:User"],
					_configuration["Email:Password"]
				);

				await client.SendAsync(message);
				await client.DisconnectAsync(true);

				_logger.LogInformation($"Email sent successfully to {toEmail}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to send email to {toEmail}");
			}
		}
	}
}
