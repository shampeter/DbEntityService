using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AXAXL.DbEntity.UnitTestLib.TestData;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class UnitTest1
	{
		private IDbService dbService { get; set; }

		[TestInitialize()]
		public void TestSetup()
		{
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
								.AddOrUpdateConnection("SQL_Connection", @"Server=localhost,1433; Database=DbEntityServiceTestDb; User Id=DbEntityService; Password=Password1")
								.SetAsDefaultConnection("SQL_Connection")
				)
				.BuildServiceProvider()
				;
			//var map = serviceProvider.GetService<INodeMap>();
			this.dbService = serviceProvider
								.GetService<IDbService>()
								.Bootstrap();
		}
		[TestMethod]
		public void TestMethod1()
		{
			var contract = this.dbService.Query<TCededContract>()
								.Where(c => c.CededContractPkey == 1)
								.ToList()
								.FirstOrDefault();
			Assert.IsNotNull(contract);
			Assert.IsTrue(contract.CededContractNum == 100 && contract.UwYear == 2000);
			Assert.IsTrue(contract.XlCompany.CompanyName == @"XL Reinsurance America");
			Assert.IsTrue(contract.CedantCompany.CompanyName == @"Travellers");
			Assert.IsTrue(contract.CededContractDocs.Count == 1 && contract.CededContractDocs[0].Filename == @"contract_file_1.txt");
		}
	}
}
