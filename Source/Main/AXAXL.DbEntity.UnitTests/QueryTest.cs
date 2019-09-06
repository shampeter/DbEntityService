using System.Linq;
using System.Data;
using System.Collections.Generic;
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
			_dbService = CommonTestContext.service;
			ClassInitializeHelper.TestDatabaseSetup();
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
			Assert.IsTrue(contract.CededContractDocs.Count == 1);
			Assert.AreEqual(@"contract_file_1.txt", contract.CededContractDocs[0].Filename);
			Assert.AreEqual(1, contract.CededContractDocs[0].OwnerGuid);
			Assert.AreEqual(@"Contract", contract.CededContractDocs[0].OwnerType);

			Assert.AreEqual(2, contract.CededContractLayers.Count, "Number of layers doesn't match!");
			var layer1 = contract.CededContractLayers[0];
			Assert.AreEqual(@"First Test Layer", layer1.Description);
			Assert.AreEqual(1000000.0000m, layer1.AttachmentPoint);
			Assert.AreEqual(2000000.0000m, layer1.Limit);
			Assert.AreEqual(@"Excess of Loss", layer1.LayerType?.Description);
			Assert.AreEqual(1, layer1.CededContractLayerDocs?.Count);
			Assert.AreEqual(1, layer1.CededContractLayerDocs[0]?.OwnerGuid);
			Assert.AreEqual(@"Layer", layer1.CededContractLayerDocs[0]?.OwnerType, "Owner type is wrong");

			var layer2 = contract.CededContractLayers[1];
			Assert.AreEqual(@"Second Test Layer", layer2.Description);
			Assert.AreEqual(3000000.0000m, layer2.AttachmentPoint);
			Assert.AreEqual(3000000.0000m, layer2.Limit);
			Assert.AreEqual(@"Stop Loss", layer2.LayerType?.Description);
			Assert.AreEqual(0, layer2.CededContractLayerDocs?.Count);
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

		[TestMethod]
		[Description("Query that return nothing.")]
		public void QueryReturnNothingTest()
		{
			var company = _dbService.Query<TCompany>().FirstOrDefault(c => c.CompanyPkey == 1000);
			Assert.IsNull(company, "Null should be returned where query failed to return anything.");
		}

		[TestMethod]
		[Description("Query with child exlusion test")]
		public void QueryWithExclusionTest()
		{
			var contract = _dbService
							.Query<TCededContract>()
							.Where(c => c.CededContractNum == 100)
							.Exclude<TCededContractLayer>(l => l.CededContractLayerDocs)
							.ToArray()
							.FirstOrDefault()
							;
			Assert.IsNotNull(contract);
			var docCount = contract.CededContractLayers.Sum(l => l.CededContractLayerDocs.Count);
			Assert.AreEqual(0, docCount);
		}

		[TestMethod]
		[Description("Raw Sql Command test with no parameters")]
		public void RawQueryTest()
		{
			IDictionary<string, object> output;
			var resultSet = _dbService
							.ExecuteCommand()
							.SetCommand("Select c.company_name, ct.description from t_company c inner join t_lookups ct on c.company_type_fkey = ct.lookups_pkey")
							.Execute(out output)
							.ToArray();
			Assert.IsNotNull(resultSet);
			Assert.AreEqual(6, resultSet.Length, $"Number of rows returned = {resultSet.Length} but expecting 6");
			var stateFarm = resultSet.Where(r => r.company_name == "State Farm").FirstOrDefault();
			Assert.IsNotNull(stateFarm);
			Assert.AreEqual("Cedant Company", stateFarm.description);
		}

		[TestMethod]
		[Description("Query with no where clause that return everything.")]
		public void ReturnAllContractTest()
		{
			var contracts = _dbService.Query<TCededContract>().ToList();

			Assert.AreEqual(2, contracts.Count(), $"There should be 2 contracts");
			Assert.AreEqual(5, contracts.SelectMany(p => p.CededContractLayers).Count(), "There should be totally 5 layers.");
			Assert.AreEqual(3, contracts.SelectMany(c => c.CededContractDocs).Count() + contracts.SelectMany(p => p.CededContractLayers).SelectMany(l => l.CededContractLayerDocs).Count());
		}

		[TestMethod]
		[Description("Query with max. number of row returned set")]
		public void MaxRowTest()
		{
			IList<TLookups> lookups = null;

			lookups = _dbService.Query<TLookups>().ToList(3);
			Assert.AreEqual(3, lookups.Count(), $"Should only return 3 lookups");

			lookups = _dbService.Query<TLookups>().ToArray(7);
			Assert.AreEqual(6, lookups.Count(), $"There should be totally 6 lookups");

			lookups = _dbService.Query<TLookups>().ToArray();
			Assert.AreEqual(6, lookups.Count(), $"There should be totally 6 lookups");

		}

		[TestMethod]
		[Description("Query involving RowVersion comparison")]
		public void QueryWithRowVersion()
		{
			long version = 2019;
			var contract = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == 1 && c.Version == version);
			Assert.IsNotNull(contract);
			Assert.AreEqual(100, contract.CededContractNum);
		}

		[TestMethod]
		[Description("Raw Query involving RowVersion comparison")]
		public void RawQueryWithRowVersion()
		{
			long version = 2019;
			int key = 1;
			var sql =
				@"select c.ceded_contract_pkey, c.ceded_contract_num " +
				@"from t_ceded_contract c " +
				@"where c.ceded_contract_pkey = @key and c.version = @ver";

			IDictionary<string, object> output;
			var resultSet = _dbService.ExecuteCommand()
					.SetCommand(sql)
					.SetParameters(
						("key", key, ParameterDirection.Input),
						("ver", version, ParameterDirection.Input)
					)
					.Execute(out output)
					;
			Assert.IsNotNull(resultSet);
			Assert.AreEqual(1, resultSet.Count());
			Assert.AreEqual(100, resultSet.First().ceded_contract_num);
		}
	}
}
