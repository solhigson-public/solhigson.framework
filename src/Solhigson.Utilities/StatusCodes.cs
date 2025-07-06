namespace Solhigson.Utilities;

public static class StatusCode
{
    public const string ErrorFromServiceProvider = "11011";
    public const string NotSupportedByServiceProvider = "11012";
        
    public static string MessageIntegrityValidationFailed = "20050";

    public const string InsufficientFunds = "62002";
    public const string InvalidAccountNumber = "60001";
    public const string TransactionNotFound = "50001";
    public const string TransactionPending = "50002";
    public const string TransactionFailed = "50004";
    public const string ServiceNotFound = "70010";
    public const string ServiceDisabledViaConfiguration = "70011";
    public const string SystemUnderMaintenance = "70012";
    public const string Timeout = "_TIMEOUT";
    public const string Successful = "90000";
    public const string UnExpectedError = "10001";
    public const string InvalidRequest = "10002";
    public const string UnAuthorised = "10003";

    public static string EmailAlreadyExist = "10009";
}