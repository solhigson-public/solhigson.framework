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

public class NotificationService : INotificationService
{
    private readonly IMailProvider _mailProvider;
    private readonly ISmsProvider _smsProvider;
    private readonly IRepositoryWrapper _repositoryWrapper;
    public NotificationService(IServiceProvider serviceProvider)// : base(repositoryWrapper)
    {
        _repositoryWrapper = serviceProvider.GetService<IRepositoryWrapper>();
        _mailProvider = serviceProvider.GetService<IMailProvider>();
        _smsProvider = serviceProvider.GetService<ISmsProvider>();
    }
        
    public void SendMail(EmailNotificationDetail emailNotificationDetail)
    {
        SendEmailInternal(emailNotificationDetail);
    }

    public void SendMailAsync(EmailNotificationDetail emailNotificationDetail)
    {
        Task.Factory.StartNew(() => SendEmailInternal(emailNotificationDetail));
    }

    private void SendEmailInternal(EmailNotificationDetail emailNotificationDetail)
    {
        try
        {
            if (_mailProvider == null)
            {
                this.ELogWarn($"No type of {nameof(IMailProvider)} has been registered, mail will not be sent");
                return;
            }

            if (!emailNotificationDetail.HasAddresses())
            {
                this.ELogWarn($"No recipients, email will not be sent for template: {emailNotificationDetail.TemplateName}");
                return;
            }
            

            if (string.IsNullOrWhiteSpace(emailNotificationDetail.Body))
            {
                if (string.IsNullOrWhiteSpace(emailNotificationDetail.TemplateName))
                {
                    this.ELogWarn("No email template specified.");
                    return;
                }
                if (_repositoryWrapper == null)
                {
                    this.ELogWarn("Email will not be sent as SolhigsonAutofacModule was not initialized with a connection string and " +
                                  "email has a template specified");
                    return;
                }

                var template =
                    _repositoryWrapper.NotificationTemplateRepository.GetByNameCached(emailNotificationDetail
                        .TemplateName);
                if (template is null)
                {
                    this.ELogWarn($"Notification Template: [{emailNotificationDetail.TemplateName}] not found. Email will not be sent");
                    return;
                }

                var contents = template.Template;
                var bodyTemplate = _repositoryWrapper.NotificationTemplateRepository.GetByNameCached("EmailBody");
                if (bodyTemplate != null)
                {
                    contents = bodyTemplate.Template.Replace("[[body]]", contents);
                }

                emailNotificationDetail.Body = HelperFunctions.ReplacePlaceHolders(contents,
                    emailNotificationDetail.TemplatePlaceholders);
            }
            
            this.ELogDebug("Validations passed - sending email");
            _mailProvider.SendMail(emailNotificationDetail);
        }
        catch (Exception e)
        {
            this.ELogError(e);
        }
    }

    public void SendSms(SmsParameters parameters)
    {
        SendSmsInternal(parameters);
    }

    public void SendSmsAsync(SmsParameters parameters)
    {
        Task.Factory.StartNew(() => SendSmsInternal(parameters));
    }

    private void SendSmsInternal(SmsParameters parameters)
    {
        try
        {
            if (_smsProvider == null)
            {
                this.ELogWarn($"No type of {nameof(ISmsProvider)} has been registered, SMS will not be sent");
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
                    this.ELogWarn("SMS will not be sent as SolhigsonAutofacModule was not initialized with a connection string and " +
                                  "template was specified");
                    return;
                }
                var template =
                    _repositoryWrapper.NotificationTemplateRepository.GetByNameCached(parameters.TemplateName);
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
            this.ELogError(e);
        }
    }

}