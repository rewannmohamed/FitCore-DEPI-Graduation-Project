//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using FitCore.DAL.Data.Contexts;
//using FitCore.DAL.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using System.Linq.Expressions;

//namespace FitCore.DAL.Repositories
//{
//    public class GenericRepository<T>(FitCoreDbContext _context) : IGenericRepository<T> where T : class
//    {
//        /// <summary>
//        /// For return id as enumerable data
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        public async Task<T?> GetByIdAsync(int id)
//        {
//            return await _context.Set<T>().FindAsync(id);
//        }

//        public async Task<IEnumerable<T>> GetAllAsync()
//        {
//            return await _context.Set<T>().ToListAsync();
//        }

//        public async Task AddAsync(T entity)
//        {
//            await _context.AddAsync(entity);
//        }
//        //public async Task AddRangeAsync(IEnumerable<T> entities)
//        //{
//        //    await _context.AddRangeAsync(entities);
//        //}

//        public void Update(T entity)
//        {
//            _context.Update(entity);
//        }

//        //public void UpdateRange(IEnumerable<T> entities)
//        //{
//        //    _context.UpdateRange(entities);
//        //}

//        public void Delete(T entity)
//        {
//            _context.Remove(entity);
//        }
//        //public void DeleteRange(IEnumerable<T> entities)
//        //{
//        //    _context.RemoveRange(entities);
//        //}


//        public IQueryable<T> GetAllAsIQueryable()
//        {
//            return _context.Set<T>();
//        }

//        public IQueryable<T> GetByIdAsIQueryable(int id)
//        {
//            //extract PK
//            var keyName = _context.Model.FindEntityType(typeof(T))!
//                                  .FindPrimaryKey()!
//                                  .Properties // if it is composite key
//                                  .Select(x => x.Name) //pk name as string
//                                  .Single();

//            return _context.Set<T>() //int for cast
//                           .Where(e => EF.Property<int>(e, keyName) == id);
//        }

//    }
//}