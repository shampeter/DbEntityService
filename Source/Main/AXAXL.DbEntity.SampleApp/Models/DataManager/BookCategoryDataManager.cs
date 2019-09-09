using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models.DataManager
{
	public class BookCategoryDataManager : IDataRepository<BookCategory>
	{
		readonly IDbService _dbService;
		public BookCategoryDataManager(IDbService dbService)
		{
			_dbService = dbService;
		}

		public BookCategory Add(BookCategory entity)
		{
			entity.EntityStatus = EntityStatusEnum.New;
			this._dbService.Persist().Submit(c => c.Save(entity)).Commit();
			return entity;
		}

		public int Delete(BookCategory entityToBeDeleted)
		{
			entityToBeDeleted.EntityStatus = EntityStatusEnum.Deleted;
			return this._dbService.Persist().Submit(c => c.Save(entityToBeDeleted)).Commit();
		}

		public BookCategory Get(long id)
		{
			return this._dbService
						.Query<BookCategory>()
						.Exclude(b => b.Books)
						.FirstOrDefault(b => b.Id == id);
		}

		public BookCategory Get(long id, RowVersion version)
		{
			return this._dbService
						.Query<BookCategory>()
						.Exclude(b => b.Books)
						.FirstOrDefault(b => b.Id == id && b.Version == version);
		}

		public IEnumerable<BookCategory> GetAll()
		{
			return this._dbService
						.Query<BookCategory>()
						.Exclude(c => c.Books)
						.ToArray();
		}

		public BookCategory Update(BookCategory existingEntityFromDb, BookCategory entityReturnedFromClient)
		{
			existingEntityFromDb.Name = entityReturnedFromClient.Name;
			existingEntityFromDb.EntityStatus = EntityStatusEnum.Updated;
			this._dbService.Persist()
							.Submit(
								changeSet => 
									changeSet
									.Exclude<BookCategory>(c => c.Books)
									.Save(existingEntityFromDb)
							)
							.Commit();
			return existingEntityFromDb;
		}
	}
}
