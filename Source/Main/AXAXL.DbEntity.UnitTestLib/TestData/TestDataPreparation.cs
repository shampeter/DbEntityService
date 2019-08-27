using System;
using System.IO;
using Microsoft.SqlServer.Dac;
using System.Diagnostics;

namespace AXAXL.DbEntity.UnitTestLib.TestData
{
	public class TestDataPreparation
	{
		public static string InstallUnitTestDbIntoSqlLocalDb()
		{
			const string C_SEED_DACPAC_LOC = @"TestData/Seed/DbEntityServiceUnitTestDb.dacpac";
			const string C_MSSQLLOCALDB_CONNECTION_STRING = @"Server=(LocalDb)\MSSQLLocalDb; Integrated Security=true;";

			var package = DacPackage.Load(C_SEED_DACPAC_LOC);
			var service = new DacServices(C_MSSQLLOCALDB_CONNECTION_STRING);
			var option = new PublishOptions();
			option.GenerateDeploymentReport = true;
			option.GenerateDeploymentScript = false;
			option.DeployOptions = new DacDeployOptions();
			option.DeployOptions.CreateNewDatabase = true;
			//service.Deploy(package, "DbEntityServiceUnitTestDb", upgradeExisting: true, options: option);
			var result = service.Publish(package, "DbEntityServiceUnitTestDb", option);

			return result.DeploymentReport;
		}
	}
}
