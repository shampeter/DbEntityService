namespace AXAXL.DbEntity.Interfaces
{
	public enum EntityStatusEnum
	{
		NoChange,
		New,
		Updated,
		Deleted
	}
	public interface ITrackable
	{
		EntityStatusEnum EntityStatus { get; set; }
	}
}