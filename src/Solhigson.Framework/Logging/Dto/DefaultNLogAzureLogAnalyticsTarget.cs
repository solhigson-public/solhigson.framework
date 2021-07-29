namespace Solhigson.Framework.Logging.Dto
{
    public class DefaultNLogAzureLogAnalyticsTarget : DefaultNLogParameters
    {
        public string AzureAnalyticsWorkspaceId { get; set; }
        
        public string AzureAnalyticsSharedSecret { get; set; }
        
        public string AzureAnalyticsLogName { get; set; }
    }
}