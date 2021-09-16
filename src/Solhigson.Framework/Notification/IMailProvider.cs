namespace Solhigson.Framework.Notification
{
    public interface IMailProvider
    {
        public void SendMail(EmailNotificationDetail emailNotificationDetail);
    }
}