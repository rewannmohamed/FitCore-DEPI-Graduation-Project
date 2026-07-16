//using FitCore.DAL.Data.Contexts;
//using FitCore.DAL.Data.Models;
//using FitCore.DAL.Interfaces;
//using Microsoft.EntityFrameworkCore.Storage;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace FitCore.DAL.Repositories
//{
//    public class UnitOfWork(FitCoreDbContext _context) : IUnitOfWork
//    {
//        private ConcurrentDictionary<string, object> _repositories = new ConcurrentDictionary<string, object>();

//        public IGenericRepository<Entity> GetRepository<Entity>() where Entity : class
//        {

//            return (IGenericRepository<Entity>)
//                _repositories.GetOrAdd(typeof(Entity).Name,
//                new GenericRepository<Entity>(_context));
//        }

//        public async Task<int> SaveChangesAsync()
//        {
//            return await _context.SaveChangesAsync();
//        }

//        public async Task<IDbContextTransaction> BeginTransactionAsync()
//        {
//            return await _context.Database.BeginTransactionAsync();
//        }

//        public void Dispose()
//        {
//            _context.Dispose();
//        }

//    }
//}