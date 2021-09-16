using System.Collections.Generic;

namespace Solhigson.Framework.Notification
{
    public class SmsParameters
    {
        public SmsParameters()
        {
            ToNumbers = new List<string>();
            PlaceHolderValues = new Dictionary<string, string>();
        }
        public string From { get; set; }
        public List<string> ToNumbers { get; set; }
        public string Text { get; set; }
        public string TemplateName { get; set; }
        public string ServiceName { get; set; }
        public Dictionary<string, string> PlaceHolderValues { get; set; }
    }
}