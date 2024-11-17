using System;

namespace Solhigson.Framework.MongoDb.Dto;

public interface IMongoDbDocumentBase
{
    string? Id { get; set; }
    DateTime Ttl { get; set; }
}
