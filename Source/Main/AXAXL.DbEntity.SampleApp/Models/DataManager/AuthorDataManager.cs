using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AXAXL.DbEntity.SampleApp.Models.DTO;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models.DataManager
{
    public class AuthorDataManager : IDataRepository<Author>
    {
        readonly IDbService _dbService;

        public AuthorDataManager(IDbService dbService)
        {
            _dbService = dbService;
        }

        public IEnumerable<Author> GetAll()
        {
            return _dbService
					.Query<Author>()
					.ToList();
        }

        public Author Get(long id, long version = -1)
        {
			Expression<Func<Author, bool>> whereWithVersion = (a) => a.Id == id && a.Version == (Timestamp)version;
			Expression<Func<Author, bool>> whereWithNoVersion = (a) => a.Id == id;
			var where = version < 0 ? whereWithNoVersion : whereWithVersion;

			Author author = _dbService
							.Query<Author>()
							.FirstOrDefault(where);

            return author;
        }

        public Author Add(Author entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(c => c.Save(entity)).Commit();
			return entity;
        }

        public Author Update(Author entityToUpdate, Author entity)
        {
            entityToUpdate.Name = entity.Name;

            entityToUpdate.Contact.Address = entity.Contact?.Address;
            entityToUpdate.Contact.ContactNumber = entity.Contact?.ContactNumber;

            var deletedBooks = entityToUpdate.BookAuthors.Except(entity.BookAuthors).ToList();
            var addedBooks = entity.BookAuthors.Except(entityToUpdate.BookAuthors).ToList();

			foreach(var deleted in deletedBooks)
			{
				deleted.EntityStatus = EntityStatusEnum.Deleted;
			}
			foreach(var added in addedBooks)
			{
				entityToUpdate.BookAuthors.Add(added);
				added.EntityStatus = EntityStatusEnum.New;
			}
			_dbService.Persist().Submit(c => c.Save(entityToUpdate)).Commit();

			return entityToUpdate;
        }

        public int Delete(long id)
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
