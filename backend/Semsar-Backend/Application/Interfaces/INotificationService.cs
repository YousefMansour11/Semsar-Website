using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string to, string subject, string body);
        string GenerateWhatsAppLink(string phone, string message);
    }
}