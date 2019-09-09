using System;
using System.Collections.Generic;
using System.Linq;
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
					.Exclude<AuthorContact>(c => c.Author)
					.Exclude<BookAuthors>(ba => ba.Author)
					.ToList();
        }

		public Author Get(long id)
		{
			return _dbService
					.Query<Author>()
					.Exclude<AuthorContact>(c => c.Author)
					.Exclude<BookAuthors>(ba => ba.Author)
					.FirstOrDefault((a) => a.Id == id);
		}

		public Author Get(long id, RowVersion version)
        {
			return _dbService
					.Query<Author>()
					.FirstOrDefault((a) => a.Id == id && a.Version == version);
        }

        public Author Add(Author entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(c => c.Save(entity)).Commit();
			return entity;
        }

        public Author Update(Author existingEntityFromDb, Author entityReturnedFromClient)
        {
            existingEntityFromDb.Name = entityReturnedFromClient.Name;

            existingEntityFromDb.Contact.Address = entityReturnedFromClient.Contact?.Address;
            existingEntityFromDb.Contact.ContactNumber = entityReturnedFromClient.Contact?.ContactNumber;

			var deletedBooks = existingEntityFromDb.BookAuthors.Except(entityReturnedFromClient.BookAuthors, BookAuthors._equalityComparer).ToList();
            var addedBooks = entityReturnedFromClient.BookAuthors.Except(existingEntityFromDb.BookAuthors, BookAuthors._equalityComparer).ToList();

			foreach(var deleted in deletedBooks)
			{
				deleted.EntityStatus = EntityStatusEnum.Deleted;
			}
			foreach(var added in addedBooks)
			{
				existingEntityFromDb.BookAuthors.Add(added);
				added.EntityStatus = EntityStatusEnum.New;
			}
			_dbService.Persist().Submit(c => c.Save(existingEntityFromDb)).Commit();

			return existingEntityFromDb;
        }

		public int Delete(Author entityToDelete)
		{
			entityToDelete.EntityStatus = EntityStatusEnum.Deleted;
			return _dbService.Persist().Submit(c => c.Save(entityToDelete)).Commit();
		}
    }
}
