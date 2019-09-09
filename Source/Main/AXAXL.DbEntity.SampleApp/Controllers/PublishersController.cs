using System;
using AXAXL.DbEntity.SampleApp.Models;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using Microsoft.AspNetCore.Mvc;
using AXAXL.DbEntity.Interfaces;

namespace AXAXL.DbEntity.SampleApp.Controllers
{
    [Route("api/publishers")]
    [ApiController]
    public class PublishersController : ControllerBase
    {
		private const string C_GET_PUBLISHER_BY_ID = "GetPublisher";
        private readonly IDataRepository<Publisher> _dataRepository;

        public PublishersController(IDataRepository<Publisher> dataRepository)
        {
            _dataRepository = dataRepository;
        }

		// GET: api/publishers
		[HttpGet]
		public IActionResult Get()
		{
			var publishers = _dataRepository.GetAll();
			return Ok(publishers);
		}
		// Get: api/publishers/5
		[HttpGet("{id}", Name = C_GET_PUBLISHER_BY_ID)]
		public IActionResult Get(long id)
		{
			var publisher = _dataRepository.Get(id);
			if (publisher == null)
			{
				return NotFound("The Publisher record couldn't be found.");
			}
			return Ok(publisher);
		}
		// POST: api/publishers
		[HttpPost]
		public IActionResult Post([FromBody] Publisher publisher)
		{
			if (publisher is null)
			{
				return BadRequest("Publisher is null.");
			}

			if (!ModelState.IsValid)
			{
				return BadRequest();
			}
			var added = _dataRepository.Add(publisher);
			return CreatedAtRoute(new { Id = added.Id }, null);
		}       
		
		// DELETE: api/ApiWithActions/5
		[HttpDelete("{id}")]
        public IActionResult Delete(int id, long version)
        {
            var publisher = _dataRepository.Get(id, (RowVersion)version);
            if (publisher == null)
            {
                return NotFound("The Publisher record couldn't be found.");
            }

            _dataRepository.Delete(publisher);
            return NoContent();
        }
    }
}