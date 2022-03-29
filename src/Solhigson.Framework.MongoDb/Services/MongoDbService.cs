using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog.Common;
using Solhigson.Framework.Data;
using Solhigson.Framework.Logging;
using Solhigson.Framework.MongoDb.Dto;

namespace Solhigson.Framework.MongoDb.Services;

internal static class MongoDbServiceFactory
{
    internal static MongoDbService<TK> Create<TK>(string connectionString, string database, string collection)
        where TK : MongoDbDocumentBase
    {
        try
        {
            return new MongoDbService<TK>(connectionString, database, collection);
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Unable to intialize mongo db service");
        }

        return null;
    }
    
    internal static MongoDbDefaultService<TK> CreateDefault<TK>(string connectionString, string database, string collection)
    {
        try
        {
            return new MongoDbDefaultService<TK>(connectionString, database, collection);
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Unable to intialize mongo db service");
        }

        return null;
    }

}
public class MongoDbService<T> where T : MongoDbDocumentBase
{
    public IMongoCollection<T> Collection { get; }

    public MongoDbService(string connectionString, string database, string collection)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(database);
        Collection = db.GetCollection<T>(collection);
            
    }
        
    public async Task<T> AddDocumentAsync(T document)
    {
        await Collection.InsertOneAsync(document);
        return document;
    }

    public async Task<T> FindAsync(string id) =>
        await Collection.Find(doc=>doc.Id == id).SingleOrDefaultAsync();
        
    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter) =>
        await Collection.Find(filter).ToListAsync();

    public async Task<PagedList<T>> FindAsync(Expression<Func<T, bool>> filter, int pageNumber, int pageSize)
    {
        var count = await Collection.Find(filter).CountDocumentsAsync();
        var result = await Collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync();
        return PagedList.Create(result, count, pageNumber, pageSize);
    }

    public async Task UpdateAsync(T document) =>
        await Collection.ReplaceOneAsync(sub => sub.Id == document.Id, document);

    public async Task DeleteAsync(string id) =>
        await Collection.DeleteOneAsync(sub => sub.Id == id);
}

internal class MongoDbDefaultService<T>
{
    public IMongoCollection<T> Collection { get; }

    public MongoDbDefaultService(string connectionString, string database, string collection)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(database);
        Collection = db.GetCollection<T>(collection);
            
    }
        
    public async Task<T> AddDocumentAsync(T document)
    {
        await Collection.InsertOneAsync(document);
        return document;
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter) =>
        await Collection.Find(filter).ToListAsync();

    public async Task<PagedList<T>> FindAsync(Expression<Func<T, bool>> filter, int pageNumber, int pageSize)
    {
        var count = await Collection.Find(filter).CountDocumentsAsync();
        var result = await Collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync();
        return PagedList.Create(result, count, pageNumber, pageSize);
    }
}