using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
