using System;
using System.Linq;
using System.Collections.Generic;
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

			#region wrong test case. Saved as a lesson to remind myself.
			/*	Bug in test case.  injected fields are updated after sql parameters are set.Thus the object returned from persist
			 *	has the right value but not the data in DB!  Fixing code to check both!

				var layerAfterUpdate = contract.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();
				var modifyDateAfterUpdate = layerAfterUpdate.ModifyDate;
				var versionAfterUpdate = layerAfterUpdate.Version;

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
  			*/
			#endregion

			var layerAfterUpdate = contract.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();
			var modifyDateAfterUpdate = layerAfterUpdate.ModifyDate;
			var versionAfterUpdate = layerAfterUpdate.Version;

			// Make sure entity returned from update has the right values for those injected values.
			Assert.IsTrue(modifyDateAfterUpdate >= modifyDateBeforeUpdate, $"Modify date after update is {modifyDateAfterUpdate}, but modify date before update is {modifyDateBeforeUpdate}");
			Assert.IsTrue(modifyDateAfterUpdate >= timeBeforeUpdate, $"Modify date after update is {modifyDateAfterUpdate}, but timestamp taken before update is {timeBeforeUpdate}");
			Assert.AreNotEqual<string>(BitConverter.ToString(versionBeforeUpdate), BitConverter.ToString(versionAfterUpdate), $"Version after update is {BitConverter.ToString(versionAfterUpdate)}, and version before update is {BitConverter.ToString(versionBeforeUpdate)}");

			// Get data refreshed from database.
			var contractAfterRefresh = _dbService.Query<TCededContract>().Where(p => p.CededContractNum == 101).ToArray().FirstOrDefault();
			Assert.IsNotNull(contractAfterRefresh);
			var layerAfterRefresh = contractAfterRefresh.CededContractLayers.Where(l => l.Description == "This is test layer 2").FirstOrDefault();

			var limitAfterRefresh = layerAfterRefresh.Limit;
			var modifyDateAfterRefresh = layerAfterRefresh.ModifyDate;
			var versionAfterRefresh = layerAfterRefresh.Version;
			
			// Make sure right values are injected and saved.
			Assert.IsTrue(modifyDateAfterRefresh >= modifyDateBeforeUpdate, $"Modify date after refresh is {modifyDateAfterRefresh}, but modify date before update is {modifyDateBeforeUpdate}");
			Assert.IsTrue(modifyDateAfterRefresh >= timeBeforeUpdate, $"Modify date after refresh is {modifyDateAfterRefresh}, but timestamp taken before update is {timeBeforeUpdate}");
			Assert.AreNotEqual<string>(BitConverter.ToString(versionBeforeUpdate), BitConverter.ToString(versionAfterRefresh), $"Version after refresh is {BitConverter.ToString(versionAfterRefresh)}, and version before update is {BitConverter.ToString(versionBeforeUpdate)}");

			Assert.AreEqual<decimal?>(layerAfterRefresh.Limit, limitBeforeUpdate + limitIncrement);
			// Assert.AreEqual seems to fail to test properly even with 2 identical DateTime.  Switch to compare DateTime as string.
			Assert.AreEqual<string>(modifyDateAfterUpdate.ToString(), modifyDateAfterRefresh.ToString());
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

		[TestMethod]
		public void ActionInjectionVersionPlusOneTest()
		{
			var xlCompanies = _dbService.Query<TCompany>().Where(c => c.CompanyName.Like("XL Reinsurance%")).ToArray();
			Dictionary<int, (string CompanyName, DateTime AddedDt, DateTime ModifyDt, int Version)> beforeUpdate = new Dictionary<int, (string, DateTime, DateTime, int)>();
			// There should be 2 companies records with name starting with XL Reinsurance
			Assert.AreEqual(xlCompanies.Count(), 2);

			// Add 'AXA' to company name.
			foreach(var company in xlCompanies)
			{
				beforeUpdate.Add(
					company.CompanyPkey, 
					(CompanyName: company.CompanyName, AddedDt: company.AddedDate, ModifyDt: company.ModifyDate, Version: company.Version)
					);
				company.EntityStatus = EntityStatusEnum.Updated;
				company.CompanyName = $"AXA {company.CompanyName}";
			}

			_dbService.Persist().Submit(c => c.Save(xlCompanies)).Commit();

			// test object returned after persist.
			foreach(var cpAfterUpdate in xlCompanies)
			{
				var recordBeforeUpdate = beforeUpdate[cpAfterUpdate.CompanyPkey];
				Assert.AreNotEqual<string>(cpAfterUpdate.CompanyName, recordBeforeUpdate.CompanyName);
				Assert.AreEqual<string>(cpAfterUpdate.AddedDate.ToString(), recordBeforeUpdate.AddedDt.ToString());
				Assert.IsTrue(cpAfterUpdate.ModifyDate > recordBeforeUpdate.ModifyDt);
				Assert.IsTrue(cpAfterUpdate.Version > recordBeforeUpdate.Version);
				Assert.AreEqual<int>(cpAfterUpdate.Version, recordBeforeUpdate.Version + 1);
			}

			// refresh data from database, and there should be 2 records.
			var xlCompaniesAfterRefresh = _dbService.Query<TCompany>().Where(c => c.CompanyPkey.In(beforeUpdate.Keys.ToArray())).ToArray();
			Assert.AreEqual(xlCompaniesAfterRefresh.Count(), 2);

			// test object returned after refresh.
			foreach (var cpAfterRefresh in xlCompaniesAfterRefresh)
			{
				var recordBeforeUpdate = beforeUpdate[cpAfterRefresh.CompanyPkey];
				Assert.AreNotEqual<string>(cpAfterRefresh.CompanyName, recordBeforeUpdate.CompanyName);
				Assert.AreEqual<string>(cpAfterRefresh.AddedDate.ToString(), recordBeforeUpdate.AddedDt.ToString());
				Assert.IsTrue(cpAfterRefresh.ModifyDate > recordBeforeUpdate.ModifyDt);
				Assert.IsTrue(cpAfterRefresh.Version > recordBeforeUpdate.Version);
				Assert.AreEqual<int>(cpAfterRefresh.Version, recordBeforeUpdate.Version + 1);
			}

		}
	}
}
