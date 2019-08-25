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
			var scriptFilename = @"TestData/Seed/DbEntityServiceTestDbSeed.dacpac";
			Debug.Assert(string.IsNullOrEmpty(scriptFilename) == false);
			var connectionString = @"Server=(LocalDb)\MSSQLLocalDb; Integrated Security=true;";
			var package = DacPackage.Load(scriptFilename);
			var service = new DacServices(connectionString);
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
