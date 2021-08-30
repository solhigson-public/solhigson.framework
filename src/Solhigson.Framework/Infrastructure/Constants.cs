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

        public static class ServiceStatus
        {
            public const string Up = "Up";
            public const string Down = "Down";
        }
        
        public static class ClaimType
        {
            public const string RoleId = "Solhigson.RoleId";
        }
    }
}