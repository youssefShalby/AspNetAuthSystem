

namespace E_Commerce.BLL.Services;

public interface IEmailService
{
	CommonResponse SendEmail(string toEmail, string subject, string body);
	string ResetPasswordEmailBody(string url);
	string VerficationCodeEmailBody(string code);
	string GenerateOtpCode();
}
