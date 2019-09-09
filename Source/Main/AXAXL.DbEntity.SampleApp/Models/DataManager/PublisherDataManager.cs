using System.Collections.Generic;
using System.Linq;
using System;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models.DataManager
{
    public class PublisherDataManager : IDataRepository<Publisher>
    {
        readonly IDbService _dbService;

        public PublisherDataManager(IDbService dbService)
        {
            _dbService = dbService;
        }

        public IEnumerable<Publisher> GetAll()
        {
			return _dbService
					.Query<Publisher>()
					.Exclude(p => p.Books)
					.ToArray();
        }

        public Publisher Get(long id)
        {
			return _dbService.Query<Publisher>().FirstOrDefault(p => p.Id == id);
        }
		public Publisher Get(long id, RowVersion version)
		{
			return _dbService.Query<Publisher>().FirstOrDefault(p => p.Id == id && p.Version == version);
		}
        public Publisher Add(Publisher entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(p => p.Save(entity)).Commit();
			return entity;
        }

        public Publisher Update(Publisher existingEntityFromDb, Publisher entityReturnedFromClient)
        {
			existingEntityFromDb.EntityStatus = EntityStatusEnum.Updated;
			existingEntityFromDb.Name = entityReturnedFromClient.Name;
			var deleted = existingEntityFromDb.Books.Except(entityReturnedFromClient.Books).ToArray();
			var added = entityReturnedFromClient.Books.Except(existingEntityFromDb.Books).ToArray();
			foreach(var each in deleted)
			{
				each.EntityStatus = EntityStatusEnum.Deleted;
			}
			foreach(var each in added)
			{
				each.EntityStatus = EntityStatusEnum.New;
			}

			_dbService.Persist().Submit(c => c.Save(existingEntityFromDb)).Commit();

			return existingEntityFromDb;
        }

        public int Delete(Publisher entity)
        {
			entity.EntityStatus = EntityStatusEnum.Deleted;
			return _dbService.Persist().Submit(c => c.Save(entity)).Commit();
        }
    }
}
