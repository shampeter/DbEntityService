namespace AXAXL.DbEntity.SampleApp.Models.DTO
{
    public class AuthorDto
    {
        public AuthorDto()
        {
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public AuthorContactDto AuthorContact { get; set; }
    }
}
