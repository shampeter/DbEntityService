using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AXAXL.DbEntity.UnitTestLib.TestData;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;

namespace AXAXL.DbEntity.UnitTestLib.Utilities
{
	public class ClassInitializeHelper
	{
		public static IDbService TestClassSetup()
		{
			var nodeMapPrintFile = Path.ChangeExtension(Path.GetTempFileName(), "md");
			var report = TestDataPreparation.InstallUnitTestDbIntoSqlLocalDb();
			Console.WriteLine(report);
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
								.PrintNodeMapToFile(nodeMapPrintFile)
				)
				.BuildServiceProvider()
				;
			//var map = serviceProvider.GetService<INodeMap>();
			var assemblies = new[] { typeof(TCededContract).Assembly };
			var service = serviceProvider
					.GetService<IDbService>()
					.Bootstrap(assemblies, new[] { "AXAXL.DbEntity.UnitTestLib" });

			return service;
		}
	}
}
