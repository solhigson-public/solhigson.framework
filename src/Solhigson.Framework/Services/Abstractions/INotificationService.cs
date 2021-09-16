using System.Threading.Tasks;
using Solhigson.Framework.Notification;

namespace Solhigson.Framework.Services.Abstractions
{
    public interface INotificationService : IServiceBase
    {
        public void SendSms(SmsParameters parameters);
        public void SendSmsAsync(SmsParameters parameters);
        public void SendMail(EmailNotificationDetail emailNotificationDetail);
        public void SendMailAsync(EmailNotificationDetail emailNotificationDetail);
    }
}