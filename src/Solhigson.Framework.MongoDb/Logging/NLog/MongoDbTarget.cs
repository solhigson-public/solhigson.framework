using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using NLog;
using NLog.Common;
using NLog.Targets;
using Solhigson.Framework.MongoDb.Dto;
using Solhigson.Framework.MongoDb.Services;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.MongoDb.Logging.NLog;

public class MongoDbTarget<T> : TargetWithLayout where T : MongoDbDocumentBase
{
    private MongoDbService<T> _service;
    private readonly TimeSpan _expireAfter;

    public MongoDbTarget([NotNull] MongoDbService<T> service, TimeSpan expireAfter)
    {
        _service = service;
        _expireAfter = expireAfter;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        var log = Layout.Render(logEvent);
        if (SendToMongoDb(log))
        {
            return;
        }

        InternalLogger.Log(logEvent.Level, log);
    }


    private bool SendToMongoDb(string jsonString)
    {
        try
        {
            var document = JsonConvert.DeserializeObject<T>(jsonString);
            document.Id = Guid.NewGuid().ToString();
            document.Ttl = DateTime.UtcNow.Add(_expireAfter);
            AsyncTools.RunSync(() => _service.AddDocumentAsync(document));
            return true;
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Error while sending log messages to Mongo Db");
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _service = null;
        base.Dispose(disposing);
    }
}