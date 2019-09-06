using System;
using AXAXL.DbEntity.SampleApp.Models;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AXAXL.DbEntity.SampleApp.Controllers
{
    [Route("api/publishers")]
    [ApiController]
    public class PublishersController : ControllerBase
    {
        private readonly IDataRepository<Publisher> _dataRepository;

        public PublishersController(IDataRepository<Publisher> dataRepository)
        {
            _dataRepository = dataRepository;
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