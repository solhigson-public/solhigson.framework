using System;
using System.Collections.Generic;
using System.Linq;
using Solhigson.Framework.Extensions;
using Solhigson.Utilities;
using Solhigson.Utilities.Extensions;

namespace Solhigson.Framework.Notification;

public class EmailNotificationDetail
{
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

    private static void AddAddress(string address, HashSet<string>? addressHashSet)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }
        addressHashSet ??= [];
        var addresses = address.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var addr in addresses.Select(entry => entry.Trim()))
        {
            if (!addr.IsValidEmailAddress())
            {
                continue;
            }

            addressHashSet.Add(addr);
        }
    }
    public string? Subject { get; set; }

    public string? TemplateName { get; set; }

    public string? Body { get; set; }

    public IDictionary<string, string?>? TemplatePlaceholders { get; set; }

    public IList<AttachmentHelper>? Attachments { get; set; }

    public string? FromAddress { get; set; }

    public string? FromDisplayAddress { get; set; }

    private HashSet<string>? _toAddresses;

    public HashSet<string> ToAddresses => _toAddresses ??= [];

    private HashSet<string>? _ccAddresses;
    public HashSet<string> CcAddresses => _ccAddresses ??= [];

    private HashSet<string>? _bccAddresses;
    public HashSet<string> BccAddresses => _bccAddresses ??= [];

    public bool HasAddresses()
    {
        return ToAddresses.Count != 0 || CcAddresses.Count != 0 || BccAddresses.Count != 0;
    }
}