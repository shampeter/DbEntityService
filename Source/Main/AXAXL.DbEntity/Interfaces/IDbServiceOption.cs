namespace AXAXL.DbEntity.Interfaces
{
	public interface IDbServiceOption
	{
		IDbServiceOption AddOrUpdateConnection(string connectionName, string connectionString);
		IDbServiceOption SetAsDefaultConnection(string connectionName);
		string GetDefaultConnectionString();
		string GetConnectionString(string connectionName);
	}
}
