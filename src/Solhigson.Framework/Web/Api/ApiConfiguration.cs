namespace Solhigson.Framework.Web.Api;

public record ApiConfiguration
{
    public bool LogOutBoundApiRequests { get; set; }
    public bool LogInBoundApiRequests { get; set; }

}