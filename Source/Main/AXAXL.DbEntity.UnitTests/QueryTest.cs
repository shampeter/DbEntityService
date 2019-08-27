using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using AXAXL.DbEntity.UnitTestLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class QueryTest
	{
		private static IDbService _dbService;

		private TestContext _testContext;
		public TestContext TestContext
		{
			get => this._testContext;
			set => this._testContext = value;
		}

		[ClassInitialize()]
		public static void TestSetup(TestContext context)
		{
			_dbService = ClassInitializeHelper.TestClassSetup();
		}
		[TestMethod]
		[Description("Query from parent and test if all children are built")]
		public void TestBuildFromParentToChild()
		{
			var contract = _dbService.Query<TCededContract>()
								.Where(c => c.CededContractPkey == 1)
								.ToList()
								.FirstOrDefault();
			Assert.IsNotNull(contract);
			Assert.IsTrue(contract.CededContractNum == 100 && contract.UwYear == 2000);
			Assert.IsTrue(contract.XlCompany.CompanyName == @"XL Reinsurance America");
			Assert.IsTrue(contract.CedantCompany.CompanyName == @"Travellers");
			Assert.IsTrue(contract.CededContractDocs.Count == 1 && contract.CededContractDocs[0].Filename == @"contract_file_1.txt");
		}

		[TestMethod]
		[Description("Query from child and test if parent are built")]
		public void TestBuildFromChildToParent()
		{
			var doc = _dbService.Query<TCededContractLayerDoc>()
								.Where(c => c.DocGuid == 3)
								.ToList()
								.FirstOrDefault();
			Assert.IsNotNull(doc);
			Assert.IsTrue(doc.CededContractLayer.Description == @"First Test Layer" && doc.CededContractLayer.CededContractLayerPkey == 1);
			Assert.IsTrue(doc.CededContractLayer.LayerType.Description == @"Excess of Loss");
			Assert.IsTrue(doc.CededContractLayer.CededContract.CededContractPkey == 1 && doc.CededContractLayer.CededContract.CedantCompany.CompanyName == "Travellers");
		}
	}
}
