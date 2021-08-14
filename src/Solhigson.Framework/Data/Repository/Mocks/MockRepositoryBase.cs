using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data.Repository.Mocks
{
    public class MockRepositoryBase<T> : IRepositoryBase<T> where T : class, new()
    {
        protected List<T> Data { get; } = new List<T>();
        public T New()
        {
            return new T();
        }

        static readonly object SyncObj = new object();
        public IQueryable<T> GetAll()
        {
            return Data.AsQueryable();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return GetAll().Where(expression);
        }

        public bool ExistsWithCondition(Expression<Func<T, bool>> expression)
        {
            return GetAll().Any(expression);
        }

        public T Add(T entity)
        {
            lock (SyncObj)
            {
                var props = typeof(T).GetProperties()
                    .FirstOrDefault(t => t.HasAttribute<KeyAttribute>());
                if (props != null)
                {
                    var localDataCopy = GetAll().ToList();
                    var value = props.GetValue(entity);
                    if (localDataCopy.Select(existing => props.GetValue(existing)).Contains(value))
                    {
                        throw new Exception(
                            $"Entity {typeof(T).Name} already exists in mock data set with key of {value}");
                    }

                    if (props.PropertyType == typeof(Guid))
                    {
                        props.SetValue(entity, Guid.NewGuid());
                    }
                    else if (props.PropertyType.IsPrimitive)
                    {
                        var id = localDataCopy.Select(data => Convert.ToInt64(props.GetValue(data))).Prepend(0).Max();
                        id++;
                        props.SetValue(entity, Convert.ChangeType(id, props.PropertyType));
                    }
                }

                Data.Add(entity);
                return entity;
            }
        }

        public void AddRange(IEnumerable<T> entities)
        {
            Data.AddRange(entities);
        }

        public T Attach(T entity)
        {
            return entity;
        }

        public void AttachRange(IEnumerable<T> entities)
        {
            
        }

        public T Update(T entity)
        {
            lock (SyncObj)
            {
                var existing = GetExisting(entity);
                if (existing != null)
                {
                    Data.Remove(existing);
                }
                Data.Add(entity);
                return entity;
            } 
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public T Remove(T entity)
        {
            lock (SyncObj)
            {
                var existing = GetExisting(entity);
                if (existing != null)
                {
                    Data.Remove(existing);
                }

                return entity;
            } 
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }

        private T GetExisting(T entity)
        {
            var props = typeof(T).GetProperties()
                .FirstOrDefault(t => t.HasAttribute<KeyAttribute>());
            if (props == null)
            {
                return null;
            }

            var keyValue = props.GetValue(entity);
            return Data.FirstOrDefault(data => props.GetValue(data).Equals(keyValue));
        }
    }
}