using System;
using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.UnitTestLib.Models;
using AXAXL.DbEntity.UnitTestLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AXAXL.DbEntity.UnitTests
{
	[TestClass]
	public class UpdateTest
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
		public void SingleUpdateTest()
		{
			var contract = _dbService.Query<TCededContract>().Where(p => p.CededContractNum == 101).ToArray().FirstOrDefault();
			Assert.IsNotNull(contract);
			var layer = contract.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();
			Assert.IsNotNull(layer);

			var limitIncrement = 1000m;
			var timeBeforeUpdate = DateTime.Now;
			var limitBeforeUpdate = layer.Limit;
			var modifyDateBeforeUpdate = layer.ModifyDate;
			var versionBeforeUpdate = layer.Version;

			layer.Limit += limitIncrement;
			layer.EntityStatus = EntityStatusEnum.Updated;

			var rc = _dbService.Persist().Submit(c => c.Save(contract)).Commit();

			var layerAfterUpdate = contract.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();
			var modifyDateAfterUpdate = layerAfterUpdate.ModifyDate;
			var versionAfterUpdate = layerAfterUpdate.Version;

			// Confirm system or DB updated values were carried back into entity after update.
			Assert.IsTrue(modifyDateAfterUpdate >= timeBeforeUpdate, $"Modify date after update is {modifyDateAfterUpdate}, but timestamp taken before update is {timeBeforeUpdate}");
			Assert.AreNotEqual<string>(BitConverter.ToString(versionBeforeUpdate), BitConverter.ToString(versionAfterUpdate), $"Version after update is {BitConverter.ToString(versionAfterUpdate)}, and version before update is {BitConverter.ToString(versionBeforeUpdate)}");

			var contractAfterRefresh = _dbService.Query<TCededContract>().Where(p => p.CededContractNum == 101).ToArray().FirstOrDefault();
			Assert.IsNotNull(contractAfterRefresh);
			var layerAfterRefresh = contract.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();
			Assert.IsNotNull(layerAfterRefresh);

			var limitAfterRefresh = layerAfterRefresh.Limit;
			var modifyDateAfterRefresh = layerAfterRefresh.ModifyDate;
			var versionAfterRefresh = layerAfterRefresh.Version;

			Assert.AreEqual<decimal?>(layerAfterRefresh.Limit, limitBeforeUpdate + limitIncrement);
			Assert.AreEqual<DateTime>(modifyDateAfterUpdate, modifyDateAfterRefresh);
			Assert.AreEqual<string>(BitConverter.ToString(versionAfterUpdate), BitConverter.ToString(versionAfterRefresh));
		}

		[TestMethod]
		[ExpectedException(typeof(DbUpdateConcurrencyException))]
		public void ConcurrentUpdateErrorTest()
		{
			var contract1 = _dbService.Query<TCededContract>().Where(p => p.CededContractPkey == 1).ToArray().FirstOrDefault();
			var contract2 = _dbService.Query<TCededContract>().Where(p => p.CededContractPkey == 1).ToArray().FirstOrDefault();

			Assert.IsNotNull(contract1);
			Assert.IsNotNull(contract2);

			contract1.UwYear = 2019;
			contract1.EntityStatus = EntityStatusEnum.Updated;

			contract2.UwYear = 2020;
			contract2.EntityStatus = EntityStatusEnum.Updated;

			_dbService.Persist().Submit(c => c.Save(contract1)).Commit();
			contract1 = _dbService.Query<TCededContract>().Where(c => c.CededContractPkey == 1).ToArray().FirstOrDefault();
			Assert.AreEqual(2019, contract1.UwYear);

			// This is expected to fail with DbUpdateConcurrencyException.
			_dbService.Persist().Submit(c => c.Save(contract2)).Commit();
			contract2 = _dbService.Query<TCededContract>().Where(c => c.CededContractPkey == 1).ToArray().FirstOrDefault();
			Assert.AreEqual(2020, contract2.UwYear);
		}
	}
}
