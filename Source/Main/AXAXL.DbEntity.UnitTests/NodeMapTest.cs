using System;
using System.IO;
using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using AXAXL.DbEntity.UnitTestLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class NodeMapTest
	{
		[TestMethod]
		public void NodeMapBuildTest()
		{
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
								.PrintNodeMapToFile(@"c:\temp\nodemap.md")
				)
				.BuildServiceProvider()
				;
			//var map = serviceProvider.GetService<INodeMap>();
			var assemblies = new[] { typeof(TCededContract).Assembly };
			var service = serviceProvider
					.GetService<IDbService>()
					.Bootstrap(assemblies, new[] { "AXAXL.DbEntity.UnitTestLib" });
		}
	}
}
