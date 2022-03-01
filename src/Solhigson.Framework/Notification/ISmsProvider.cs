namespace Solhigson.Framework.Notification;

public interface ISmsProvider
{
    public void SendSms(SmsParameters smsParameters);
}