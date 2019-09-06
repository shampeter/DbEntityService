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
        private readonly IDataRepository<Book> _dataRepository;

        public BooksController(IDataRepository<Book> dataRepository)
        {
            _dataRepository = dataRepository;
        }

        // GET: api/Books/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var book = _dataRepository.Get(id);
            if (book == null)
            {
                return NotFound("Book not found.");
            }

            return Ok(book);
        }
    }
}