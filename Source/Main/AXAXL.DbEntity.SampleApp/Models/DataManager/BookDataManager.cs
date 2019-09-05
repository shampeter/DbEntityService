using System.Collections.Generic;
using System.Linq;
using AXAXL.DbEntity.SampleApp.Models.DTO;
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

        public Book Add(Book entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(c => c.Save(entity)).Commit();
			return entity;
        }

        public Book Update(Book entityToUpdate, Book entity)
        {
			entityToUpdate.EntityStatus = EntityStatusEnum.Updated;
			entityToUpdate.CategoryId = entity.CategoryId;
			entityToUpdate.PublisherId = entity.PublisherId;
			entityToUpdate.Title = entity.Title;
        }

        public int Delete(Book entity)
        {
			var returnCount = 0;
			var entityToDelete = _dbService.Query<Book>().FirstOrDefault(b => b.Id == id);
			if (entityToDelete != null)
			{
				entityToDelete.EntityStatus = EntityStatusEnum.Deleted;
				returnCount = _dbService.Persist().Submit(c => c.Save(entityToDelete)).Commit();
			}
			return returnCount;
		}
    }
}
