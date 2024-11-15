using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog.Common;
using Solhigson.Framework.Data;
using Solhigson.Framework.Dto;
using Solhigson.Framework.MongoDb.Dto;

namespace Solhigson.Framework.MongoDb.Services;

internal static class MongoDbServiceFactory
{
    internal static MongoDbService<TK>? Create<TK>(string connectionString, string database)
        where TK : IMongoDbDocumentBase
    {
        try
        {
            return new MongoDbService<TK>(connectionString, database);
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Unable to intialize mongo db service");
        }

        return null;
    }
    
}


public class MongoDbService
{
    private readonly ConcurrentDictionary<string, object> _collections = new();

    private IMongoCollection<T>? GetCollection<T>() where T : IMongoDbDocumentBase
    {
        var type = typeof(T);
        var fullName = type.FullName;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }
        return _collections.GetOrAdd(fullName, (_) => _databaseClient.GetCollection<T>(GetCollectionName<T>())) as IMongoCollection<T>;
    }
    
    
    //public IMongoCollection<TBase> Collection { get; }
    private readonly IMongoDatabase _databaseClient;

    public MongoDbService(string connectionString, string database)
    {
        var client = new MongoClient(connectionString);
        _databaseClient = client.GetDatabase(database);
    }
    
    public MongoDbService(IMongoDatabase database)
    {
        _databaseClient = database;
    }

        
    public async Task<ResponseInfo<T>> AddDocumentAsync<T>(T document, CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var resp = new ResponseInfo<T>();
        var coll = GetCollection<T>();
        if (coll is null)
        {
            return resp.Fail();
        }
        await coll.InsertOneAsync(document, cancellationToken: cancellationToken);
        return resp.Success(document);
    }

    public async Task<T?> FindAsync<T>(string id, CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var coll = GetCollection<T>();
        return coll is null 
            ? default 
            : await coll.Find(doc => doc.Id == id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<T>> FindAsync<T>(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var coll = GetCollection<T>();
        if (coll is null)
        {
            return [];
        }
        return await coll.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<PagedList<T>> FindAsync<T>(Expression<Func<T, bool>> filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var coll = GetCollection<T>();
        if (coll is null)
        {
            return PagedList.Create<T>([], 0, 1, 1);
        }

        var count = await coll.Find(filter).CountDocumentsAsync(cancellationToken);
        var result = await coll.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken);
        return PagedList.Create(result, count, pageNumber, pageSize);
    }

    public async Task UpdateAsync<T>(T document, CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var coll = GetCollection<T>();
        if (coll is null)
        {
            return;
        }
        await coll.ReplaceOneAsync(sub => sub.Id == document.Id, document, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : IMongoDbDocumentBase
    {
        var coll = GetCollection<T>();
        if (coll is null)
        {
            return;
        }
        await coll.DeleteOneAsync(sub => sub.Id == id, cancellationToken: cancellationToken);
    }

    public static string GetCollectionName<T>() where T : IMongoDbDocumentBase
    {
        return typeof(T).Name;
    }
}

public class MongoDbService<T> where T : IMongoDbDocumentBase
{
    private readonly MongoDbService _mongoDbService;
    public string CollectionName { get; private set; }

    public MongoDbService(string connectionString, string database)
    {
        _mongoDbService = new MongoDbService(connectionString, database);
        CollectionName = MongoDbService.GetCollectionName<T>();
    }
        
    public MongoDbService(IMongoDatabase database)
    {
        _mongoDbService = new MongoDbService(database);
        CollectionName = MongoDbService.GetCollectionName<T>();
    }

    public async Task<ResponseInfo<T>> AddDocumentAsync(T document, CancellationToken cancellationToken = default)
    {
        return await _mongoDbService.AddDocumentAsync(document, cancellationToken);
    }

    public async Task<T?> FindAsync(string id, CancellationToken cancellationToken = default) => await _mongoDbService.FindAsync<T>(id, cancellationToken);

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) 
        => await _mongoDbService.FindAsync(filter, cancellationToken);
    
    public async Task<PagedList<T>> FindAsync(Expression<Func<T, bool>> filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _mongoDbService.FindAsync(filter, pageNumber, pageSize, cancellationToken);
    }

    public async Task UpdateAsync(T document, CancellationToken cancellationToken = default) => await _mongoDbService.UpdateAsync(document, cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) => await _mongoDbService.DeleteAsync<T>(id, cancellationToken);
}


// public class MongoDbService<T> where T : IMongoDbDocumentBase
// {
//     public IMongoCollection<T> Collection { get; }
//
//     public MongoDbService(string connectionString, string database, string collection)
//     {
//         var client = new MongoClient(connectionString);
//         var db = client.GetDatabase(database);
//         Collection = db.GetCollection<T>(collection);
//             
//     }
//         
//     public async Task<T> AddDocumentAsync(T document)
//     {
//         await Collection.InsertOneAsync(document);
//         return document;
//     }
//
//     public async Task<T> FindAsync(string id) =>
//         await Collection.Find(doc=>doc.Id == id).SingleOrDefaultAsync();
//         
//     public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter) =>
//         await Collection.Find(filter).ToListAsync();
//
//     public async Task<PagedList<T>> FindAsync(Expression<Func<T, bool>> filter, int pageNumber, int pageSize)
//     {
//         var count = await Collection.Find(filter).CountDocumentsAsync();
//         var result = await Collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync();
//         return PagedList.Create(result, count, pageNumber, pageSize);
//     }
//
//     public async Task UpdateAsync(T document) =>
//         await Collection.ReplaceOneAsync(sub => sub.Id == document.Id, document);
//
//     public async Task DeleteAsync(string id) =>
//         await Collection.DeleteOneAsync(sub => sub.Id == id);
// }

