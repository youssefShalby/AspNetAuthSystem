
namespace E_Commerce.BLL.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
	public EmailService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public CommonResponse SendEmail(string toEmail, string subject, string body)
	{
		string senderEmail = _configuration["Email:senderEmail"];
		string senderPassword = _configuration["Email:senderPasswordKey"];

		MailMessage message = new MailMessage();
		message.From = new MailAddress(senderEmail);
		message.To.Add(new MailAddress(toEmail));
		message.Subject = subject;
		message.Body = body;
		message.IsBodyHtml = true;

		SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
		smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
        smtpClient.EnableSsl = true;
		try
		{
			smtpClient.Send(message);
			return new CommonResponse("Message Set", true);
		}
		catch (Exception ex)
		{
			return new CommonResponse($"Message sent fail, ${ex.Message}", false);
		}
	}
	public string GenerateOtpCode()
	{
		return new Random().Next(1000, 9999).ToString();
	}

	public string VerficationCodeEmailBody(string code)
	{
		return @$"<!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Email Verification</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f2f2f2;
                            margin: 0;
                            padding: 0;
                        }}

                        .container {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #fff;
                            border-radius: 5px;
                            padding: 20px;
                            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                        }}

                        h1 {{
                            color: #333;
                        }}

                        b,p {{
                            color: #333;
                            margin-bottom: 20px;
                        }}

                        .verification-code {{
                            font-size: 24px;
                            font-weight: bold;
                            color: #007bff;
                            margin-bottom: 20px;
                        }}

                        .footer {{
                            margin-top: 20px;
                            text-align: center;
                            color: #777;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h1>Email Verification</h1>
                        <b>Thank you for signing up! To complete your registration, please use the following verification code:</b>
                        <p class=""verification-code"">{code}</p>
                        <b>If you did not request this code, please ignore this email.</b>
                        <div class=""footer"">
                            <p>This email was sent automatically. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>";
	}
	public string ResetPasswordEmailBody(string url)
	{
		return @$"<!DOCTYPE html>
                <html lang=""en"">
                <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Password Reset</title>
                <style>
                    /* Reset CSS */
                    body, html {{
                    margin: 0;
                    padding: 0;
                    font-family: Arial, sans-serif;
                    }}
                    /* Wrapper */
                    .wrapper {{
                    max-width: 600px;
                    margin: 0 auto;
                    padding: 20px;
                    }}
                    /* Header */
                    .header {{
                    text-align: center;
                    margin-bottom: 20px;
                    }}
                    /* Content */
                    .content {{
                    background-color: #f9f9f9;
                    padding: 20px;
                    border-radius: 5px;
                    }}
                    /* Button */
                    .button {{
                    display: inline-block;
                    background-color: #007bff;
                    color: #fff;
                    text-decoration: none;
                    padding: 10px 20px;
                    border-radius: 5px;
                    }}
                </style>
                </head>
                <body>
                <div class=""wrapper"">
                    <div class=""header"">
                    <h1>Password Reset</h1>
                    </div>
                    <div class=""content"">
                    <p>You have requested a password reset. Click the button below to reset your password:</p>
                    <p><a class=""button"" href='{url}'>Reset Password</a></p>
                    <p>If you didn't request a password reset, you can safely ignore this email.</p>
                    </div>
                </div>
                </body>
                </html>
";
	}
}
