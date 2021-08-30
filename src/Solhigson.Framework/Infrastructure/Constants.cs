namespace Solhigson.Framework.Infrastructure
{
    public static class Constants
    {
        public static class ServiceType
        {
            public const string Internal = "Internal";
            public const string External = "External";
            public const string Self = "Self";
        }

        public static class Group
        {
            public const string ServiceStatus = "ServiceStatus";
            public const string AppLog = "AppLog";
        }

        public class ServiceStatus
        {
            public static string Up = "Up";
            public static string Down = "Down";
        }
        
        public static class ClaimType
        {
            public const string RoleIds = "Solhigson.RoleId";
        }
    }
}