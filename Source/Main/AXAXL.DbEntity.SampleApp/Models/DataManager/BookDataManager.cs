using System.Collections.Generic;
using System.Linq;
using System;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models.DataManager
{
    public class BookDataManager : IDataRepository<Book>
    {
        readonly IDbService _dbService;

        public BookDataManager(IDbService dbService)
        {
            _dbService = dbService;
        }

        public IEnumerable<Book> GetAll()
        {
			return _dbService.Query<Book>().ToArray();
        }
        
        public Book Get(long id)
        {
			return _dbService.Query<Book>().FirstOrDefault(b => b.Id == id);
        }

		public Book Get(long id, RowVersion version)
		{
			return _dbService.Query<Book>().FirstOrDefault(b => b.Id == id && b.Version == version);
		}

        public Book Add(Book entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(c => c.Save(entity)).Commit();
			return entity;
        }

        public Book Update(Book existingEntityFromDb, Book entityReturnedFromClient)
        {
			existingEntityFromDb.EntityStatus = EntityStatusEnum.Updated;
			existingEntityFromDb.CategoryId = entityReturnedFromClient.CategoryId;
			existingEntityFromDb.PublisherId = entityReturnedFromClient.PublisherId;
			existingEntityFromDb.Title = entityReturnedFromClient.Title;

			_dbService.Persist().Submit(c => c.Save(existingEntityFromDb)).Commit();

			return existingEntityFromDb;
        }

		public int Delete(Book entityToDelete)
		{

			entityToDelete.EntityStatus = EntityStatusEnum.Deleted;
			return _dbService.Persist().Submit(c => c.Save(entityToDelete)).Commit();
		}
    }
}
