using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using Solhigson.Framework.Data;
using Solhigson.Framework.MongoDb.Dto;

namespace Solhigson.Framework.MongoDb.Services
{
    public class MongoDbService<T> where T : MongoDbDocumentBase
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDbService(string connectionString, string database, string collection)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(database);
            _collection = db.GetCollection<T>(collection);
        }
        
        public async Task<T> AddDocument(T document)
        {
            await _collection.InsertOneAsync(document);
            return document;
        }

        public async Task<T> Find(string id) =>
            await _collection.Find(doc=>doc.Id == id).SingleOrDefaultAsync();
        
        public async Task<List<T>> Find(Expression<Func<T, bool>> filter) =>
            await _collection.Find(filter).ToListAsync();

        public async Task<PagedList<T>> Find(Expression<Func<T, bool>> filter, int pageNumber, int pageSize)
        {
            var count = await _collection.Find(filter).CountDocumentsAsync();
            var result = await _collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageNumber).ToListAsync();
            return new PagedList<T>(result, (int)count, pageNumber, pageSize);
        }

        public async Task Update(T document) =>
            await _collection.ReplaceOneAsync(sub => sub.Id == document.Id, document);

        public void Delete(string id) =>
            _collection.DeleteOne(sub => sub.Id == id);
    }
}