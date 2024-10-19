namespace Solhigson.Framework.AzureCosmosDb.Dto;

public class CosmosDbInitializationResult
{
    public bool LogContainerInitializationSuccess { get; set; }
    public bool AuditContainerInitializationSuccess { get; set; }
}