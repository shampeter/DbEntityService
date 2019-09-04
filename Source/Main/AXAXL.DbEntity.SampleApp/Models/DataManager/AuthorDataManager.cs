using System.Collections.Generic;
using System.Linq;
using AXAXL.DbEntity.SampleApp.Models.DTO;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Models.DataManager
{
    public class AuthorDataManager : IDataRepository<Author, AuthorDto>
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

        public Author Get(long id)
        {
			var author = _dbService
							.Query<Author>()
							.FirstOrDefault(a => a.Id == id);

            return author;
        }

        public AuthorDto GetDto(long id)
        {
			var author = _dbService.Query<Author>().FirstOrDefault(a => a.Id == id);
			return AuthorDtoMapper.MapToDto(author);
        }


        public void Add(Author entity)
        {
			entity.EntityStatus = EntityStatusEnum.New;
			_dbService.Persist().Submit(c => c.Save(entity)).Commit();
        }

        public void Update(Author entityToUpdate, Author entity)
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
        }

        public void Delete(Author entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
