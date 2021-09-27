namespace Solhigson.Framework.Logging.Nlog.Dto
{
    public class DefaultNLogAzureLogAnalyticsParameters : DefaultNLogParameters
    {
        public string AzureAnalyticsWorkspaceId { get; set; }
        
        public string AzureAnalyticsSharedSecret { get; set; }
        
        public string AzureAnalyticsLogName { get; set; }
    }
}