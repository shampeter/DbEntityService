using System;
using AXAXL.DbEntity.SampleApp.Models;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AXAXL.DbEntity.SampleApp.Controllers
{
    [Route("api/authors")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
		private const string C_GET_AUTHOR_BY_ID = @"GetAuthor";

        private readonly IDataRepository<Author> _dataRepository;

        public AuthorsController(IDataRepository<Author> dataRepository)
        {
            _dataRepository = dataRepository;
		}

		// GET: api/Authors
		/// <summary>
		/// Get all author records.  Note that the Author within AuthorContact and BookAuthors are skipped.
		/// </summary>
		/// <returns>List of <see cref="Author"/></returns>
		[HttpGet]
        public IActionResult Get()
        {
            var authors = _dataRepository.GetAll();
            return Ok(authors);
        }

		// GET: api/Authors/5
		/// <summary>
		/// Get author by id.  Note that the Author within AuthorContact and BookAuthors are skipped.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}", Name = C_GET_AUTHOR_BY_ID)]
        public IActionResult Get(int id)
        {
            var author = _dataRepository.Get(id);
            if (author == null)
            {
                return NotFound("Author not found.");
            }

            return Ok(author);
        }

        // POST: api/Authors
		/// <summary>
		/// Add a author.
		/// </summary>
		/// <param name="author"></param>
		/// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] Author author)
        {
            if (author is null)
            {
                return BadRequest("Author is null.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var added = _dataRepository.Add(author);
            return CreatedAtRoute(C_GET_AUTHOR_BY_ID, new { Id = added.Id }, null);
        }

        // PUT: api/Authors/5
		/// <summary>
		/// Update a update.
		/// </summary>
		/// <param name="id">Id of author being updated.</param>
		/// <param name="author">Entity returned by called with updated data.</param>
		/// <returns></returns>
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Author author)
        {
            if (author == null)
            {
                return BadRequest("Author is null.");
            }

            var authorToUpdate = _dataRepository.Get(id, author.Version);
            if (authorToUpdate == null)
            {
                return NotFound("The author record couldn't be found and has been updated by someone else.");
            }

			if (!ModelState.IsValid)
			{
				return BadRequest();
			}

			_dataRepository.Update(authorToUpdate, author);
            return NoContent();
        }


    }
}