namespace Solhigson.Framework.AzureCosmosDb.Dto;

public interface ICosmosDocumentBase
{
    string PartitionKey { get; }
    string Id { get; set; }
    double Timestamp { get; set; }
    int? TimeToLive { get; set; }
}