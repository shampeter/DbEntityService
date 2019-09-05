using System.Collections.Generic;

namespace AXAXL.DbEntity.SampleApp.Models.Repository
{
    public interface IDataRepository<TEntity>
    {
        IEnumerable<TEntity> GetAll();
        TEntity Get(long id, long version = -1);
		TEntity Add(TEntity entity);
        TEntity Update(TEntity entityToUpdate, TEntity entity);
        int Delete(long id);
    }
}
