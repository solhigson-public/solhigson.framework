namespace Solhigson.Framework.Web;

public class PageMessage
{
    public string Message { get; set; }
    public PageMessageType Type { get; set; }

    public bool CloseOnClick { get; set; } = true;

    public bool EncodeHtml { get; set; } = true;


    public const string MessageKey = "::solhigson::pagemessage::";
    public const string SystemMessageKey = "::solhigson::pagemessage::systemmessage";
}

public enum PageMessageType
{
    SystemMessage = 1,
    Error = 2,
    Info = 3,
}