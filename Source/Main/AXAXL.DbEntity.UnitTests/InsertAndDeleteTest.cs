using System;
using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using AXAXL.DbEntity.UnitTestLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class InsertAndDeleteTest
	{
		private static IDbService _dbService;

		[ClassInitialize()]
		public static void TestSetup(TestContext context)
		{
			_dbService = CommonTestContext.service;
			ClassInitializeHelper.TestDatabaseSetup();
		}
		[TestMethod]
		[Description("Happy path update test.")]
		public void InsertThenDeleteTest()
		{
			// Create new contract with layer and doc.
			var xlcompany = _dbService.Query<TCompany>().FirstOrDefault(c => c.CompanyPkey == 6);
			Assert.IsNotNull(xlcompany);
			var cedant = _dbService.Query<TCompany>().FirstOrDefault(c => c.CompanyPkey == 2);
			Assert.IsNotNull(cedant);
			var layerType = _dbService.Query<TLookups>().FirstOrDefault(c => c.Description == "Stop Loss");
			Assert.IsNotNull(layerType);

			var contract = new TCededContract
			{
				CedantCompany = cedant,
				XlCompany = xlcompany,
				CreationDate = DateTime.Now,
				UwYear = 2019,
				EntityStatus = EntityStatusEnum.New
			};

			var layer = new TCededContractLayer
			{
				Description = "New Layer created at " + DateTime.Now,
				AttachmentPoint = 1000000m,
				Limit = 3000000m,
				LayerType = layerType,
				EntityStatus = EntityStatusEnum.New
			};

			var doc = new TCededContractLayerDoc
			{
				Filename = "This is a test.txt",
				EntityStatus = EntityStatusEnum.New
			};

			layer.CededContractLayerDocs.Add(doc);
			contract.CededContractLayers.Add(layer);

			// Save to database and note the primary key for refresh.
			_dbService.Persist().Submit(c => c.Save(contract)).Commit();
			var pKey = contract.CededContractPkey;
			var contractNum = contract.CededContractNum;
			Console.WriteLine("Created contract with primary key {0} and contract number {1}", pKey, contractNum);
			
			// Refresh from DB to make sure that it's there.
			var contractAfterRefresh = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == pKey);
			Assert.AreEqual(pKey, contractAfterRefresh.CededContractPkey);
			Assert.AreEqual(contractNum, contractAfterRefresh.CededContractNum);
			Assert.AreEqual(1, contractAfterRefresh.CededContractLayers.Count);
			Assert.AreEqual(1000000m, contractAfterRefresh.CededContractLayers[0].AttachmentPoint);
			Assert.AreEqual(3000000m, contractAfterRefresh.CededContractLayers[0].Limit);
			Assert.AreEqual(1, contractAfterRefresh.CededContractLayers[0].CededContractLayerDocs.Count);
			Assert.AreEqual("This is a test.txt", contractAfterRefresh.CededContractLayers[0].CededContractLayerDocs[0].Filename);

			// Going to delete whole contract
			contractAfterRefresh.EntityStatus = EntityStatusEnum.Deleted;
			_dbService.Persist().Submit(c => c.Save(contractAfterRefresh)).Commit();

			// try to refresh from DB.  Should get null.
			var contractAfterDelete = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == pKey);
			Assert.IsNull(contractAfterDelete);
		}
	}
}
