#region Usings

using System.Threading.Tasks;
using UltimatePlaylist.Services.Common.Models.Email.Jobs;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest email);

        Task SendEmailWithExcelAttachment(string toEmail, string subject, string file);
    }
}
