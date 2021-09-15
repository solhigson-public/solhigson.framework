﻿using System;
using System.Collections.Generic;
using System.Linq;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Notification
{
    public class EmailNotificationDetail
    {
        public EmailNotificationDetail()
        {
            
        }

        public void AddToAddress(string address)
        {
            AddAddress(address, ToAddresses);
        }

        public void AddCcAddress(string address)
        {
            AddAddress(address, CcAddresses);
        }

        public void AddBccAddress(string address)
        {
            AddAddress(address, BccAddresses);
        }

        private static void AddAddress(string address, List<string> addressList)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }
            addressList ??= new List<string>();
            var addresses = address.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var addr in addresses.Select(entry => entry.Trim()))
            {
                if (!addr.IsValidEmailAddress())
                {
                    continue;
                }
                if (!addressList.Contains(addr))
                {
                    addressList.Add(addr);
                }
            }
        }
        public string Subject { get; set; }

        public string TemplateName { get; set; }

        public string Body { get; set; }

        public IDictionary<string, string> TemplatePlaceholders { get; set; }

        public IList<AttachmentHelper> Attachments { get; set; }

        public object CustomData { get; set; }

        public string FromAddress { get; set; }

        public string FromDisplayAddress { get; set; }

        private List<string> _toAddresses;

        public List<string> ToAddresses
        {
            get { return _toAddresses ??= new List<string>(); }
            set => _toAddresses = value;
        }

        private List<string> _ccAddresses;
        public List<string> CcAddresses
        {
            get { return _ccAddresses ??= new List<string>(); }
            set => _ccAddresses = value;
        }

        private List<string> _bccAddresses;

        public List<string> BccAddresses
        {
            get { return _bccAddresses ??= new List<string>(); }
            set => _bccAddresses = value;
        }

        public string SendGridTemplateId { get; set; }

        public string ChainId { get; set; }

        internal string ToEmail { get; private set; }

        internal List<string> ValidatedEmailAddresses { get; set; } 

        public bool ValidateEmailAddresses { get; set; }
    }
}