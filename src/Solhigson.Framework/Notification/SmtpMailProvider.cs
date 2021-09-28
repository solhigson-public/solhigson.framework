using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Notification
{
    public class SmtpMailProvider : IMailProvider
    {
        private readonly SmtpConfiguration _smtpConfiguration;
        public SmtpMailProvider(SmtpConfiguration smtpConfiguration)
        {
            _smtpConfiguration = smtpConfiguration;
        }
        
        public void SendMail(EmailNotificationDetail emailNotificationDetail)
        {
            try
            {
                this.ELogDebug("Sending mail - default");
                var mail = new MailMessage();
                mail.Subject = emailNotificationDetail.Subject;
                mail.From = new MailAddress(emailNotificationDetail.FromAddress, emailNotificationDetail.FromDisplayAddress);
                var alternateView = AlternateView.CreateAlternateViewFromString(
                    emailNotificationDetail.Body,
                    null,
                    MediaTypeNames.Text.Html);

                foreach (var address in emailNotificationDetail.ToAddresses)
                {
                    mail.To.Add(address);
                }

                foreach (var address in emailNotificationDetail.CcAddresses)
                {
                    mail.CC.Add(address);
                }

                foreach (var address in emailNotificationDetail.BccAddresses)
                {
                    mail.Bcc.Add(address);
                }

                if (emailNotificationDetail.Attachments != null && emailNotificationDetail.Attachments.Any())
                {
                    foreach (var attachment in emailNotificationDetail.Attachments)
                    {
                        if (attachment.Data == null)
                        {
                            continue;
                        }
                        var memoryStream = new MemoryStream(attachment.Data);
                        mail.Attachments.Add(new Attachment(memoryStream, attachment.Name, attachment.ContentType));
                    }
                }

                mail.AlternateViews.Add(alternateView);
                mail.IsBodyHtml = true;

                SendMail(mail);
            }
            catch (Exception e)
            {
                this.ELogError(e);
            }
        }


        private void SendMail(MailMessage mail)
        {
            this.ELogDebug("Sending via email client...");
            try
            {
                var client = new SmtpClient
                {
                    Host = _smtpConfiguration.Server,
                    Port = _smtpConfiguration.Port,
                };

                if (!string.IsNullOrWhiteSpace(_smtpConfiguration.Username)
                    && !string.IsNullOrWhiteSpace(_smtpConfiguration.Password))
                {
                    client.Credentials = new NetworkCredential(_smtpConfiguration.Username,
                        _smtpConfiguration.Password);
                }

                client.EnableSsl = _smtpConfiguration.EnableSsl;
                client.Send(mail);
                this.ELogDebug("Mail has been sent.");
            }
            catch (Exception e)
            {
                this.ELogError(e);
            }
            finally
            {
                if (mail.Attachments.Any())
                {
                    foreach (var attachment in mail.Attachments)
                    {
                        attachment.ContentStream?.Dispose();
                    }
                }
            }
        }

    }
}