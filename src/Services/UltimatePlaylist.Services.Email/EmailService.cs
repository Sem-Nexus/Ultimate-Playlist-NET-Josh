#region Usings

using System.Net;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Exceptions;
using UltimatePlaylist.Services.Common.Interfaces;
using UltimatePlaylist.Services.Common.Models.Email.Jobs;

#endregion

namespace UltimatePlaylist.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig config;
        private readonly SendGridClient client;

        public EmailService(IOptions<EmailConfig> options)
        {
            config = options.Value;
            client = new SendGridClient(config.SendGridClientKey);
        }

        public async Task SendEmailAsync(EmailRequest email)
        {
            var sendgridMessage = MailHelper.CreateSingleTemplateEmailToMultipleRecipients(
                new EmailAddress(config.SenderEmail, config.SenderName),
                email.Recipients.Select(recipient => new EmailAddress(recipient.Email, recipient.Name)).ToList(),
                email.TemplateId,
                email.TemplateModel);

            var response = await client.SendEmailAsync(sendgridMessage);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                var body = await response.Body.ReadAsStringAsync();

                throw new BusinessException($"{ErrorType.ErrorSendingEmail} {body}");
            }
        }

        public async Task SendEmailWithExcelAttachment(string toEmail, string subject, string file)
        {

            DateTime today = DateTime.Now;           
            var message = new SendGridMessage
            {                
                Subject = subject,
                From = new EmailAddress(config.SenderEmail),
                HtmlContent = "<p><b>Winners List Date: " + today.ToString("MM-dd-yyyy") + "</b></p>"
            };

           //To do add array of emails from config file
            message.AddTo(new EmailAddress("marco@semnexus.com"));
            message.AddTo(new EmailAddress("adrian@semnexus.com"));
            message.AddTo(new EmailAddress("shevy@eliteshout.com"));
            message.AddTo(new EmailAddress("up@azlottery.gov"));
            message.AddTo(new EmailAddress("dandrego@azlottery.gov"));

            message.AddAttachment(today.ToString("MM-dd-yyyy") +".xlsx", file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var response = await client.SendEmailAsync(message);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                var body = await response.Body.ReadAsStringAsync();

                throw new BusinessException($"{ErrorType.ErrorSendingEmail} {body}");
            }

        }
    }
}
