using System.Collections.Generic;

namespace Solhigson.Framework.AzureCosmosDb.Dto
{
    public class CosmosDbResponse<T> where T : CosmosDocumentBase
    {
        public List<T> Items { get; set; }
        public double RequestCharge { get; set; }
        
        public string ContinuationToken { get; set; }
    }}