using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class CommonTestContext
	{
		// Don't know if this is the right way or not but just do it this way to keep things going.
		public static IDbService service = null;

		[AssemblyInitialize]
		public static void InitializeService(TestContext context)
		{
			var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
			
			var serviceProvider = new ServiceCollection()
				.AddLogging(
					c => c
						.AddConsole()
						.SetMinimumLevel(LogLevel.Debug)
				)
				.AddSqlDbEntityService(
					option => option
								.AddOrUpdateConnection("SQL_Connection", @"Server=(LocalDB)\MSSqlLocalDb; Database=DbEntityServiceUnitTestDb; Integrated Security=true")
								.SetAsDefaultConnection("SQL_Connection")
								.PrintNodeMapToFile(config.GetValue<string>(@"DbEntity:NodeMapExport"))
				)
				.AddSingleton<INextSequence, HelperMethods>()
				.BuildServiceProvider()
				;
			//var map = serviceProvider.GetService<INodeMap>();
			var assemblies = new[] { typeof(TCededContract).Assembly };

			CommonTestContext.service = serviceProvider
					.GetService<IDbService>()
					.Bootstrap(assemblies, new[] { "AXAXL.DbEntity.UnitTestLib" });
		}
	}
}
