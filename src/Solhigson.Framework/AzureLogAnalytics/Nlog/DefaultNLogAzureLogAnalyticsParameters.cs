using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.AzureLogAnalytics.Nlog
{
    public class DefaultNLogAzureLogAnalyticsParameters : DefaultNLogParameters
    {
        public string AzureAnalyticsWorkspaceId { get; set; }
        
        public string AzureAnalyticsSharedSecret { get; set; }
        
        public string AzureAnalyticsLogName { get; set; }
    }
}