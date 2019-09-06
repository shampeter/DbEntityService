using System;
using System.Collections.Generic;

namespace AXAXL.DbEntity.SampleApp.Models.Repository
{
    public interface IDataRepository<TEntity>
    {
        IEnumerable<TEntity> GetAll();
        TEntity Get(long id);
		TEntity Get(long id, RowVersion version);
		TEntity Add(TEntity entity);
        TEntity Update(TEntity existingEntityFromDb, TEntity entityReturnedFromClient);
        int Delete(TEntity entityToBeDeleted);
    }
}
