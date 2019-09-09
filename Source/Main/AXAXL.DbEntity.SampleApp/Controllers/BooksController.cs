using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using AXAXL.DbEntity.SampleApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AXAXL.DbEntity.SampleApp.Controllers
{
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
		private const string C_GET_BOOK_BY_ID = @"GetBook";
		private readonly IDataRepository<Book> _bookRepository;
		private readonly IDataRepository<BookCategory> _categoryRepository;

        public BooksController(IDataRepository<Book> bookRepository, IDataRepository<BookCategory> categoryRepository)
        {
            _bookRepository = bookRepository;
			_categoryRepository = categoryRepository;
        }

		// GET: api/Books
		/// <summary>
		/// Get all books.  Note that Book in BookAuthors will be skipped.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IActionResult Get()
		{
			var books = _bookRepository.GetAll();
			return Ok(books);
		}

		// GET: api/Books/5
		/// <summary>
		/// Get book by Id.  Note that Book in BookAuthors will be skipped.
		/// </summary>
		/// <returns></returns>
		[HttpGet("{id}", Name = C_GET_BOOK_BY_ID)]
        public IActionResult Get(int id)
        {
            var book = _bookRepository.Get(id);
            if (book == null)
            {
                return NotFound("Book not found.");
            }

            return Ok(book);
        }

		// POST: api/Books
		/// <summary>
		/// Add a book.
		/// </summary>
		/// <param name="book">Book object from called.</param>
		/// <returns></returns>
		[HttpPost]
		public IActionResult Post([FromBody] Book book)
		{
			if (book is null)
			{
				return BadRequest("Book is null.");
			}

			if (!ModelState.IsValid)
			{
				return BadRequest();
			}

			var added = _bookRepository.Add(book);
			return CreatedAtRoute(C_GET_BOOK_BY_ID, new { Id = added.Id }, null);
		}

		// DELETE: api/ApiWithActions/5
		/// <summary>
		/// Delete a book by Id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="version"></param>
		/// <returns></returns>
		[HttpDelete("{id}")]
		public IActionResult Delete(int id, long version)
		{
			var book = _bookRepository.Get(id, (RowVersion)version);
			if (book == null)
			{
				return NotFound("The Publisher record couldn't be found.");
			}

			_bookRepository.Delete(book);
			return NoContent();
		}

		// GET: api/books/category
		/// <summary>
		/// Get all book categories.
		/// </summary>
		/// <returns></returns>
		[HttpGet("category")]
		public IActionResult GetCategory()
		{
			var categories = _categoryRepository.GetAll();
			return Ok(categories);
		}
	}
}