using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Notification;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Services.Abstractions;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Utilities.Security;

namespace Solhigson.Framework.Services;

public class NotificationService(IServiceProvider serviceProvider) : INotificationService
{
    private readonly IMailProvider? _mailProvider = serviceProvider.GetService<IMailProvider>();
    private readonly ISmsProvider? _smsProvider = serviceProvider.GetService<ISmsProvider>();
    private readonly IRepositoryWrapper? _repositoryWrapper = serviceProvider.GetService<IRepositoryWrapper>();

    public void SendMail(EmailNotificationDetail emailNotificationDetail)
    {
        SendEmailInternalAsync(emailNotificationDetail).Wait();
    }

    public void SendMailAsync(EmailNotificationDetail emailNotificationDetail)
    {
        _ = SendEmailInternalAsync(emailNotificationDetail);
    }

    private async Task SendEmailInternalAsync(EmailNotificationDetail emailNotificationDetail)
    {
        try
        {
            if (_mailProvider == null)
            {
                this.LogWarning("No type of {mailProvider} has been registered, mail will not be sent", nameof(IMailProvider));
                return;
            }

            if (!emailNotificationDetail.HasAddresses())
            {
                this.LogWarning("No recipients, email will not be sent for template: {TemplateName}", emailNotificationDetail.TemplateName);
                return;
            }
            

            if (string.IsNullOrWhiteSpace(emailNotificationDetail.Body))
            {
                if (string.IsNullOrWhiteSpace(emailNotificationDetail.TemplateName))
                {
                    this.LogWarning("No email template specified.");
                    return;
                }
                if (_repositoryWrapper == null)
                {
                    this.LogWarning("Email will not be sent as SolhigsonAutofacModule was not initialized with a connection string and " +
                                  "email has a template specified");
                    return;
                }

                var template = await
                    _repositoryWrapper.NotificationTemplateRepository.GetByNameCachedAsync(emailNotificationDetail
                        .TemplateName);
                if (template is null)
                {
                    this.LogWarning("Notification Template: [{TemplateName}] not found. Email will not be sent", emailNotificationDetail.TemplateName);
                    return;
                }

                var contents = template.Template;
                var bodyTemplate = await _repositoryWrapper.NotificationTemplateRepository.GetByNameCachedAsync("EmailBody");
                if (bodyTemplate != null)
                {
                    contents = bodyTemplate.Template.Replace("[[body]]", contents);
                }

                emailNotificationDetail.Body = HelperFunctions.ReplacePlaceHolders(contents,
                    emailNotificationDetail.TemplatePlaceholders);
            }
            
            this.LogDebug("Validations passed - sending email");
            _mailProvider.SendMail(emailNotificationDetail);
        }
        catch (Exception e)
        {
            this.LogError(e);
        }
    }

    public void SendSms(SmsParameters parameters)
    {
        SendSmsInternalAsync(parameters).Wait();
    }

    public void SendSmsAsync(SmsParameters parameters)
    {
        _ = SendSmsInternalAsync(parameters);
    }

    private async Task SendSmsInternalAsync(SmsParameters parameters)
    {
        try
        {
            if (_smsProvider == null)
            {
                this.LogWarning($"No type of {nameof(ISmsProvider)} has been registered, SMS will not be sent");
                return;
            }
            if (string.IsNullOrWhiteSpace(parameters.From))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(parameters.Text) &&
                !string.IsNullOrWhiteSpace(parameters.TemplateName))
            {
                if (_repositoryWrapper == null)
                {
                    this.LogWarning("SMS will not be sent as SolhigsonAutofacModule was not initialized with a connection string and " +
                                  "template was specified");
                    return;
                }
                var template = await
                    _repositoryWrapper.NotificationTemplateRepository.GetByNameCachedAsync(parameters.TemplateName);
                if (template == null)
                {
                    return;
                }
                parameters.Text = HelperFunctions.ReplacePlaceHolders(template.Template,
                    parameters.PlaceHolderValues);
            }
            if (string.IsNullOrWhiteSpace(parameters.Text))
            {
                return;
            }

            if (!parameters.ToNumbers.Any())
            {
                return;
            }

            if (parameters.Text.Length > 160)
            {
                parameters.Text = parameters.Text[..156] + "...";
            }

            _smsProvider.SendSms(parameters);
        }
        catch (Exception e)
        {
            this.LogError(e);
        }
    }

}