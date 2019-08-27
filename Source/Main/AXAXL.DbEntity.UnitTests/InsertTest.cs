using System;
using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using AXAXL.DbEntity.UnitTestLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class InsertTest
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
		[Description("Inserting a child to a parent")]
		public void ChildInsertionTest()
		{
			var contract = _dbService.Query<TCededContract>().Where(c => c.CededContractPkey == 2).ToArray().FirstOrDefault();
			Assert.IsNotNull(contract);
			var layerType = _dbService.Query<TLookups>().Where(l => l.Description == @"Stop Loss").ToArray().FirstOrDefault();
			Assert.IsNotNull(layerType);

			var layer = new TCededContractLayer
						{
							Description = "This is test layer 4",
							AttachmentPoint = 8000000,
							Limit = 10000000,
							LayerType = layerType,
							EntityStatus = EntityStatusEnum.New
						};
			layer.CededContractLayerDocs.Add(
						new TCededContractLayerDoc {
							Filename = @"layer_file_2.txt",
							EntityStatus = EntityStatusEnum.New
						});
			contract.CededContractLayers.Add(layer);

			var rowCount = _dbService.Persist().Submit(c => c.Save(contract)).Commit();
			Console.WriteLine("No. of changes = {0}", rowCount);
			Assert.AreEqual(2, rowCount);

			var today = DateTime.Today;
			var layerAfterInsert = contract.CededContractLayers.Where(l => l.Description == @"This is test layer 4").FirstOrDefault();

			Assert.IsNotNull(layerAfterInsert);

			Console.WriteLine("Primary key of layer inserted = {0}", layerAfterInsert.CededContractLayerPkey);

			Assert.AreNotEqual(default(int), layerAfterInsert.CededContractLayerPkey);
			Assert.AreEqual(today, layerAfterInsert.ModifyDate.Date);

			var docAfterInsert = layerAfterInsert?.CededContractLayerDocs.FirstOrDefault();

			Assert.IsNotNull(docAfterInsert);

			Console.WriteLine("Primary key of t_doc inserted = {0}", docAfterInsert.DocGuid);

			Assert.AreNotEqual(default(int), docAfterInsert.DocGuid);
			Assert.AreEqual(layerAfterInsert.CededContractLayerPkey, docAfterInsert.OwnerGuid);
			Assert.AreEqual(@"Layer", docAfterInsert.OwnerType);

			var contractAfterRefresh = _dbService.Query<TCededContract>().Where(c => c.CededContractPkey == 2).ToArray().FirstOrDefault();

			var layerAfterRefresh = contract.CededContractLayers.Where(l => l.Description == @"This is test layer 4").FirstOrDefault();
			Assert.AreEqual(layerAfterInsert, layerAfterRefresh);
		}
	}
}
