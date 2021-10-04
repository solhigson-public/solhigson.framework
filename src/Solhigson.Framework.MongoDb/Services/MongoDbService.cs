using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog.Common;
using Solhigson.Framework.Data;
using Solhigson.Framework.Logging;
using Solhigson.Framework.MongoDb.Dto;

namespace Solhigson.Framework.MongoDb.Services
{
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
    }
    public class MongoDbService<T> where T : MongoDbDocumentBase
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDbService(string connectionString, string database, string collection)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(database);
            _collection = db.GetCollection<T>(collection);
        }
        
        public async Task<T> AddDocumentAsync(T document)
        {
            await _collection.InsertOneAsync(document);
            return document;
        }

        public async Task<T> FindAsync(string id) =>
            await _collection.Find(doc=>doc.Id == id).SingleOrDefaultAsync();
        
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter) =>
            await _collection.Find(filter).ToListAsync();

        public async Task<PagedList<T>> FindAsync(Expression<Func<T, bool>> filter, int pageNumber, int pageSize)
        {
            var count = await _collection.Find(filter).CountDocumentsAsync();
            var result = await _collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageNumber).ToListAsync();
            return PagedList.Create(result, count, pageNumber, pageSize);
        }

        public async Task UpdateAsync(T document) =>
            await _collection.ReplaceOneAsync(sub => sub.Id == document.Id, document);

        public async Task DeleteAsync(string id) =>
           await _collection.DeleteOneAsync(sub => sub.Id == id);
    }
}