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
        private readonly IDataRepository<Author> _dataRepository;

        public AuthorsController(IDataRepository<Author> dataRepository)
        {
            _dataRepository = dataRepository;
        }

        // GET: api/Authors
        [HttpGet]
        public IActionResult Get()
        {
            var authors = _dataRepository.GetAll();
            return Ok(authors);
        }

        // GET: api/Authors/5
        [HttpGet("{id}", Name = "GetAuthor")]
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

            _dataRepository.Add(author);
            return CreatedAtRoute("GetAuthor", new { Id = author.Id }, null);
        }

        // PUT: api/Authors/5
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
                return NotFound("The Employee record couldn't be found.");
            }

            _dataRepository.Update(authorToUpdate, author);
            return NoContent();
        }
    }
}