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
		public static void TestDatabaseSetup()
		{
			//var nodeMapPrintFile = Path.ChangeExtension(Path.GetTempFileName(), "md");
			var report = TestDataPreparation.InstallUnitTestDbIntoSqlLocalDb();
			//Console.WriteLine(report);
			return;
		}
	}
}
