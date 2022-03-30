using System.Collections.Generic;

namespace Solhigson.Framework.AzureCosmosDb.Dto;

public class CosmosDbResponse<T> where T : ICosmosDocumentBase
{
    public List<T> Items { get; set; }
    public double RequestCharge { get; set; }
        
    public string ContinuationToken { get; set; }
}