using System;
using System.Text.RegularExpressions;
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

			var resultSet2 = _dbService
							.ExecuteCommand()
							.SetCommand("select * from t_ceded_contract order by ceded_contract_pkey")
							.Execute<TCededContract>(out output)
							.ToArray();
			Assert.AreEqual(2, resultSet2.Length);
			Assert.AreEqual(100, resultSet2[0].CededContractNum);
			Assert.AreEqual(101, resultSet2[1].CededContractNum);
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
			// Version column seems to hold different value in different machine even though the test database was imported int LocalDB on every run on a test class.  Thus
			// we need to get the version from the database of a record first before using it in query again to verify where clause compilation can handle RowVersion custom type.
			var primaryKey = 1;
			var contract = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == primaryKey);
			Assert.IsNotNull(contract);

			long versionInLong = contract.Version;
			var contractFromLongVersion = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == primaryKey && c.Version == versionInLong);
			Assert.IsNotNull(contractFromLongVersion);
			Assert.AreEqual(100, contractFromLongVersion.CededContractNum);

			ulong versionInULong = contract.Version;
			var contractFromULongVersion = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == primaryKey && c.Version == versionInULong);
			Assert.IsNotNull(contractFromULongVersion);
			Assert.AreEqual(100, contractFromULongVersion.CededContractNum);

			var nativeVersion = contract.Version;
			var contractFromNativeVersion = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == primaryKey && c.Version == nativeVersion);
			Assert.IsNotNull(contractFromNativeVersion);
			Assert.AreEqual(100, contractFromNativeVersion.CededContractNum);

			int versionInInt = (int)contract.Version;
			var contractFromIntVersion = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == primaryKey && c.Version == versionInInt);
			Assert.IsNotNull(contractFromIntVersion);
			Assert.AreEqual(100, contractFromIntVersion.CededContractNum);
		}

		[TestMethod]
		[Description("Raw Query involving RowVersion comparison")]
		public void RawQueryWithRowVersion()
		{
			// Version column seems to hold different value in different machine even though the test database was imported int LocalDB on every run on a test class.  Thus
			// we need to get the version from the database of a record first before using it in query again to verify where clause compilation can handle RowVersion custom type.
			int key = 1;
			var contract = _dbService.Query<TCededContract>().FirstOrDefault(c => c.CededContractPkey == key);
			var sql =
				@"select c.ceded_contract_pkey, c.ceded_contract_num " +
				@"from t_ceded_contract c " +
				@"where c.ceded_contract_pkey = @key and c.version = @ver";

			var version = contract.Version;
			var resultSet = this.ExecuteRawQuery(sql, ("key", key), ("ver", version));
			Assert.IsNotNull(resultSet);
			Assert.AreEqual(1, resultSet.Count());
			Assert.AreEqual(100, resultSet.First().ceded_contract_num);

			RowVersion? nullableVersion = contract.Version;
			var resultSetWithNullableVersion = this.ExecuteRawQuery(sql, ("key", key), ("ver", nullableVersion));
			Assert.IsNotNull(resultSetWithNullableVersion);
			Assert.AreEqual(1, resultSetWithNullableVersion.Count());
			Assert.AreEqual(100, resultSetWithNullableVersion.First().ceded_contract_num);
		}

		[TestMethod]
		[Description("Query with OrderBy and Max row return")]
		public void QueryWithOrderByAndMaxReturn()
		{
			var companyNamesInAscOrderAsInDb = new [] {
						@"AON Benefield",
						@"Guy Carpentar",
						@"State Farm",
						@"Travellers",
						@"XL Reinsurance America",
						@"XL Reinsurance Bermuda"
					};
			var companyInOrder = _dbService.Query<TCompany>().OrderBy(c => c.CompanyName).ToArray(200);
			Assert.AreEqual(6, companyInOrder.Length);
			for (int i = 0; i < companyNamesInAscOrderAsInDb.Length; i++)
			{
				Assert.AreEqual(companyNamesInAscOrderAsInDb[i], companyInOrder[i].CompanyName);
			}
			var companyUnordered = _dbService.Query<TCompany>().ToArray(200);
			Assert.AreEqual(6, companyUnordered.Length);
			var companyOrderedByLinq = companyUnordered.OrderBy(c => c.CompanyName).ToArray();
			for (int i = 0; i < companyOrderedByLinq.Length; i++)
			{
				Assert.AreEqual(companyOrderedByLinq[i].CompanyName, companyInOrder[i].CompanyName);
			}
		}

		[TestMethod]
		[Description("Query with LIKE operator")]
		public void QueryWithLIKESqlOp()
		{
			var searchingFor = @"LOSS";
			var sqlPattern = $"%{searchingFor}%";
			var regExPattern = $"^.*{searchingFor}.*$";
			var regex = new Regex(regExPattern, RegexOptions.IgnoreCase);

			var lookupWithDescLikeLoss = _dbService.Query<TLookups>().Where(l => l.Description.Like(sqlPattern)).ToArray();

			Console.WriteLine("Lookup description returned from pattern {0} are: {1}", sqlPattern, string.Join(", ", lookupWithDescLikeLoss.Select(l => l.Description)));

			Assert.AreEqual(2, lookupWithDescLikeLoss.Length);

			var allMatches = lookupWithDescLikeLoss.All(l => regex.IsMatch(l.Description));
			Assert.AreEqual(true, allMatches, $"Not all description returned matcth '{sqlPattern}'!");

			var allLookups = _dbService.Query<TLookups>().ToArray();
			var allLookupsFiltered = allLookups.Where(l => regex.IsMatch(l.Description)).ToArray();

			var comparer = new GenericEqualityComparer<TLookups>(
									(a, b) => a?.LookupsPkey == b?.LookupsPkey, 
									(t) => t.LookupsPkey.GetHashCode()
									);
			var emptySet = allLookupsFiltered.Except(lookupWithDescLikeLoss, comparer).ToArray();

			Assert.AreEqual(0, emptySet.Length);
		}

		[TestMethod]
		[Description("Query with IN operator")]
		public void QueryWithINSqlOp()
		{
			var companyComparer = new GenericEqualityComparer<TCompany>(
							(a, b) => a?.CompanyPkey == b?.CompanyPkey,
							(c) => c.CompanyPkey.GetHashCode()
						);
			var cedantAndBroker1 = _dbService.Query<TCompany>().Where(c => c.CompanyTypeFkey.In(new[] { 101, 102 })).ToArray();
			Assert.AreEqual(4, cedantAndBroker1.Length);

			Assert.IsTrue(cedantAndBroker1.All(c => new[] { 101, 102 }.Contains(c.CompanyTypeFkey)), $"Returned company are not all of type 101 or 102");

			var cedantAndBrokerTypes = new [] { 101, 102 };
			var cedantAndBroker2 = _dbService.Query<TCompany>().Where(c => c.CompanyTypeFkey.In(cedantAndBrokerTypes)).ToArray();
			Assert.AreEqual(4, cedantAndBroker2.Length);

			Assert.IsTrue(cedantAndBroker2.All(c => cedantAndBrokerTypes.Contains(c.CompanyTypeFkey)), $"Returned company are not all of type 101 or 102");

			var allCedantAndBrokers = _dbService.Query<TCompany>().ToArray();
			var cedantAndBrokersFilteredFromAll = allCedantAndBrokers
													.Where(c => cedantAndBrokerTypes.Contains(c.CompanyTypeFkey)).ToArray();
			Assert.AreEqual(
				0, 
				cedantAndBrokersFilteredFromAll.Except(
					cedantAndBroker1,
					companyComparer
					)
				.ToArray()
				.Length
				);
			Assert.IsTrue(
				allCedantAndBrokers
					.Except(cedantAndBroker2, companyComparer)
					.All(c => c.CompanyTypeFkey != 101 && c.CompanyTypeFkey != 102)
			);
		}

		[TestMethod]
		[Description("Filter by parent entity properties")]
		public void FilterByParentEntityProperties()
		{
			var resultSet1 = _dbService
							.Query<TCededContractLayer>()
							.Where(
								l => 
									l.AttachmentPoint > 0 && 
									(
										l.CededContract.CedantCompany.CompanyName == @"State Farm" ||
										l.CededContract.CedantCompany.CompanyName == @"Travellers"
									)
							)
							.ToArray()
							;
			Assert.IsTrue(
				resultSet1.All(l => 
					l.CededContract.CedantCompany.CompanyName == @"State Farm" || 
					l.CededContract.CedantCompany.CompanyName == @"Travellers")
				);
			Assert.AreEqual(5, resultSet1.Length, $"Actual number of rows returned = {resultSet1.Length}");

			var cedents = new[] { "State Farm", "Travellers" };
			var resultSet2 = _dbService
				.Query<TCededContractLayer>()
				.Where(
					l =>
						l.AttachmentPoint > 0 &&
						l.CededContract.CedantCompany.CompanyName.In(cedents)
				)
				.ToArray()
				;
			Assert.AreEqual(5, resultSet2.Length, $"Actual number of rows returned = {resultSet2.Length}");

			var stateFarm = @"%State%";
			var resultSet3 = _dbService
				.Query<TCededContractLayer>()
				.Where(
					l =>
						l.AttachmentPoint > 0 &&
						l.CededContract.CedantCompany.CompanyName.Like(stateFarm)
				)
				.ToArray()
				;
			Assert.AreEqual(3, resultSet3.Length, $"Actual number of rows returned = {resultSet3.Length}");
		}
		[TestMethod]
		[Description("Multiple Where() and And() Call test")]
		public void MultiWhereCall()
		{
			var resultSet1 = _dbService
							.Query<TCededContractLayer>()
							.Where(l => l.AttachmentPoint > 0)
							.And(l => l.CededContract.CedantCompany.CompanyName.Like("%State%"))
							.ToArray()
							;
			Assert.AreEqual(3, resultSet1.Length, $"Actual number of rows returned = {resultSet1.Length}");
		}
		[TestMethod]
		[Description("Query with Or() test")]
		public void MultiOrCall()
		{
			var resultSet1 = _dbService
							.Query<TCededContractLayer>()
							.Where(l =>	l.AttachmentPoint > 0)
							.Or(
								l => l.CededContract.CedantCompany.CompanyName == @"State Farm",
								l => l.CededContract.CedantCompany.CompanyName == @"Travellers"
							)
							.ToArray()
							;
			Assert.IsTrue(
				resultSet1.All(l =>
					l.CededContract.CedantCompany.CompanyName == @"State Farm" ||
					l.CededContract.CedantCompany.CompanyName == @"Travellers")
				);
			Assert.AreEqual(5, resultSet1.Length, $"Actual number of rows returned = {resultSet1.Length}");
		}
		[TestMethod]
		[Description("Query with child filtering")]
		public void QueryWithChildFiltering()
		{
			// Return all contracts but select only text file t_doc under contract and gif file t_doc uner layer, which is none.
			var query = _dbService
								.Query<TCededContract>()
								;
			query.Where<TCededContract, TCededContractDoc>(d => d.Filename.Like("%.txt"));
			query.Where<TCededContractLayer, TCededContractLayerDoc>(d => d.Filename.Like("%.gif"));

			var allContracts = query.ToArray();

			var contract1 = allContracts.Where(c => c.CededContractPkey == 1).FirstOrDefault();
			var contract2 = allContracts.Where(c => c.CededContractPkey == 2).FirstOrDefault();

			// Sanity check.  There should be 2 layers on contract 1 and 3 layers on contract 2.
			Assert.AreEqual(2, contract1.CededContractLayers.Count);
			Assert.AreEqual(3, contract2.CededContractLayers.Count);
			Assert.AreEqual(@"XL Reinsurance America", contract1.XlCompany.CompanyName);
			Assert.AreEqual(@"XL Reinsurance Bermuda", contract2.XlCompany.CompanyName);

			// There should be 1 text file t_doc under each contract
			Assert.AreEqual(1, contract1.CededContractDocs.Count);
			Assert.AreEqual(1, contract2.CededContractDocs.Count);
			Assert.IsTrue(contract1.CededContractDocs[0].Filename.EndsWith(".txt"));
			Assert.IsTrue(contract2.CededContractDocs[0].Filename.EndsWith(".txt"));

			// There should be no t_doc retrieved under layer because the condition returns none.
			Assert.IsTrue(contract1.CededContractLayers.Count(l => l.CededContractLayerDocs.Count > 0) == 0);
			Assert.IsTrue(contract2.CededContractLayers.Count(l => l.CededContractLayerDocs.Count > 0) == 0);

			// Now query that search for GIF file for t_doc under layer so.  There should be 1.
			allContracts = _dbService
							.Query<TCededContract>()
							.Where<TCededContract, TCededContractDoc>(d => d.Filename.Like("%.txt"))
							.Where<TCededContractLayer, TCededContractLayerDoc>(d => d.Filename.Like("%.txt"))
							.ToArray();

			contract1 = allContracts.Where(c => c.CededContractPkey == 1).FirstOrDefault();
			contract2 = allContracts.Where(c => c.CededContractPkey == 2).FirstOrDefault();

			// Sanity check.  There should be 2 layers on contract 1 and 3 layers on contract 2.
			Assert.AreEqual(2, contract1.CededContractLayers.Count);
			Assert.AreEqual(3, contract2.CededContractLayers.Count);
			Assert.AreEqual(@"XL Reinsurance America", contract1.XlCompany.CompanyName);
			Assert.AreEqual(@"XL Reinsurance Bermuda", contract2.XlCompany.CompanyName);

			// There should be 1 text file t_doc under each contract
			Assert.AreEqual(1, contract1.CededContractDocs.Count);
			Assert.AreEqual(1, contract2.CededContractDocs.Count);
			Assert.IsTrue(contract1.CededContractDocs[0].Filename.EndsWith(".txt"));
			Assert.IsTrue(contract2.CededContractDocs[0].Filename.EndsWith(".txt"));

			// Just want to see it with my own eyes.
			Console.WriteLine("Contract: {0} / Doc: {1} / Filename: {2}", contract1.CededContractPkey, contract1.CededContractDocs[0].DocGuid, contract1.CededContractDocs[0].Filename);
			Console.WriteLine("Contract: {0} / Doc: {1} / Filename: {2}", contract2.CededContractPkey, contract2.CededContractDocs[0].DocGuid, contract2.CededContractDocs[0].Filename);

			// There should be no t_doc retrieved under layer because the condition returns none.
			Assert.IsTrue(contract1.CededContractLayers.Count(l => l.CededContractLayerDocs.Count > 0) == 1);
			Assert.IsTrue(contract2.CededContractLayers.Count(l => l.CededContractLayerDocs.Count > 0) == 0);

			var contractLayer1 = contract1.CededContractLayers.FirstOrDefault(l => l.CededContractLayerPkey == 1);
			Assert.IsTrue(contractLayer1.CededContractLayerDocs.FirstOrDefault()?.Filename.EndsWith(".txt") ?? false);

			Console.WriteLine(
				"Contract: {0} / Layer: {1} / Doc: {2} / Filename: {3}", 
				contract1.CededContractPkey, 
				contractLayer1.CededContractLayerPkey, 
				contractLayer1.CededContractLayerDocs.FirstOrDefault().DocGuid,
				contractLayer1.CededContractLayerDocs.FirstOrDefault().Filename
				);
		}
		private IEnumerable<dynamic> ExecuteRawQuery(string query, params (string Name, object Value)[] parameters)
		{
			var inputParameters = parameters.Select(p => (p.Name, p.Value, ParameterDirection.Input)).ToArray();
			IDictionary<string, object> output;
			var resultSet = _dbService.ExecuteCommand()
							.SetCommand(query)
							.SetParameters(inputParameters)
							.Execute(out output);
			return resultSet;
		}
		/// <summary>
		/// Insight of this utility class was drawn from https://social.msdn.microsoft.com/Forums/en-US/25c182ee-e68c-48a2-af89-6a215d2de828/iequalitycomparer-to-lambdaanonymous?forum=csharplanguage
		/// </summary>
		/// <typeparam name="T">target type</typeparam>
		private class GenericEqualityComparer<T> : IEqualityComparer<T>
		{
			private Func<T, T, bool> equals;
			private Func<T, int> hash;
			public GenericEqualityComparer(Func<T, T, bool> equals, Func<T, int> hash)
			{
				this.equals = equals;
				this.hash = hash;
			}

			public bool Equals(T x, T y)
			{
				return this.equals.Invoke(x, y);
			}

			public int GetHashCode(T obj)
			{
				return this.hash.Invoke(obj);
			}
		}
	}
}
