using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Solhigson.Framework.AzureCosmosDb.Dto;

namespace Solhigson.Framework.AzureCosmosDb;

public class CosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(
        CosmosClient dbClient,
        string databaseName,
        string containerName)
    {
        _container = dbClient.GetContainer(databaseName, containerName);
    }

    public async Task AddItemAsync<T>(T item) where T : CosmosDocumentBase
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = Guid.NewGuid().ToString();
        }

        await _container.CreateItemAsync(item, new PartitionKey(item.PartitionKey));
    }

    public async Task AddDocumentAsync(object item)
    {
        await _container.CreateItemAsync(item);
    }


    public async Task DeleteItemAsync<T>(string id, string partitionKey) where T : CosmosDocumentBase
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
    }

    public async Task<T> GetItemAsync<T>(string id, string partitionKey) where T : CosmosDocumentBase
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<CosmosDbResponse<T>> GetItemsAsync<T>(string queryString,
        string continuationToken = null, int? maxItemCount = null) where T : CosmosDocumentBase
    {
        maxItemCount ??= 100;
            
        var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString),
            continuationToken, new QueryRequestOptions
            {
                MaxItemCount = maxItemCount,
            });
        var results = new CosmosDbResponse<T>
        {
            Items = new List<T>()
        };
            
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();

            results.Items.AddRange(response.ToList());
            results.RequestCharge += response.RequestCharge;
            results.ContinuationToken = response.ContinuationToken;
        }

        return results;
    }

    public async Task UpdateItemAsync<T>(T item) where T : CosmosDocumentBase
    {
        await _container.UpsertItemAsync(item, new PartitionKey(item.PartitionKey));
    }
}