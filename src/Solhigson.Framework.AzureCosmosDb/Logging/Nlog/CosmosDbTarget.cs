using System;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NLog;
using NLog.Common;
using NLog.Targets;
using Solhigson.Framework.AzureCosmosDb.Dto;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.AzureCosmosDb.Logging.Nlog;

public class CosmosDbTarget<T> : TargetWithLayout where T : CosmosDocumentBase
{
    private CosmosDbService _service;
    private readonly TimeSpan _ttl;

    public CosmosDbTarget(Database database, string containerName, TimeSpan ttl)
    {
        _service = new CosmosDbService(database.Client, database.Id, containerName);
        _ttl = ttl;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        var log = Layout.Render(logEvent);
        if (SendToAzureCosmosDb(log))
        {
            return;
        }
        InternalLogger.Log(logEvent.Level, log);
    }


    private bool SendToAzureCosmosDb(string jsonString)
    {
        try
        {
            var document = JsonConvert.DeserializeObject<T>(jsonString);
            document.TimeToLive = (int)_ttl.TotalSeconds;
            document.Id = Guid.NewGuid().ToString();
            document.Timestamp = DateUtils.CurrentUnixTimestamp;
            AsyncTools.RunSync(() => _service.AddDocumentAsync(document));
            return true;
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Error while sending log messages to Azure Cosmos Db");
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _service = null;
        base.Dispose(disposing);
    }
}