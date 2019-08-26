using System;
using System.Linq;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Services;
using AXAXL.DbEntity.MSSql;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.UnitTestLib.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Test.Sample
{
	class Program
    {
        static void Main(string[] args)
        {
			//BuildNodeTest();
			//SqlDriverTest();
			//QueryTest();
			QueryTest2();
		}
		static void QueryTest2()
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
				)
				.BuildServiceProvider()
				;
			//var map = serviceProvider.GetService<INodeMap>();
			var service = serviceProvider
								.GetService<IDbService>()
								.Bootstrap();

			var contracts = service.Query<TCededContract>()
				.Where(c => c.CededContractNum == 100)
				.ToArray();
			Console.WriteLine("Returned {0} contracts", contracts.Length);
			var contract = contracts.FirstOrDefault();
			Console.WriteLine(
				string.Join(
					Environment.NewLine,
					contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey}, {l.Description}, {l.LayerType.Description}"))
				);
			var stopLoss = service.Query<TLookups>().Where(l => l.Description == "Stop Loss").ToList().FirstOrDefault();
			contract.CededContractLayers.Add(
				new TCededContractLayer
				{
					Description = "Third Layer 2019-08-25",
					LayerType = stopLoss,
					AttachmentPoint = 1000000,
					Limit = 3000000,
					EntityStatus = EntityStatusEnum.New
				}
				);
			var rowCount = service.Persist()
				.Submit(c => c.Save(contract))
				.Commit();
			Console.WriteLine("Saved {0}", rowCount);
			Console.WriteLine(
				string.Join(
					Environment.NewLine,
					contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey}, {l.Description}, {l.LayerType.Description}"))
				);

			/*
						var doc = service.Query<TCededContractLayerDoc>()
										.Where(c => c.DocGuid == 3)
										.ToArray()
										.FirstOrDefault();
						Console.WriteLine("T_Doc returned.  Owner Type {0} Owner Guid {1}", doc?.OwnerType, doc?.OwnerGuid);
			*/
		}
		//static void BuildNodeTest()
		//{
		//	var map = new NodeMap();
		//	map.BuildNodes();
		//	Console.WriteLine(map.ToMarkDown());
		//}

		/*
		static void QueryTest()
		{
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
			var service = serviceProvider
								.GetService<IDbService>()
								.Bootstrap();
			var contracts = service.Query<TCededContract>()
				.Where(c => c.CededContractNum == 1000)
				.ToArray();
			Console.WriteLine("Returned {0} contracts", contracts.Length);
			var contract = contracts.FirstOrDefault();
			Console.WriteLine("{0}[{1}] {2}[{3}] {4}-{5}",
				contract.CedantCompany?.CompanyName,
				contract.CedantCompany?.CompanyTypeFkeyNavigation?.Description,
				contract.XlCompany?.CompanyName,
				contract.XlCompany?.CompanyTypeFkeyNavigation?.Description,
				contract.CededContractNum,
				contract.UwYear
				);
			foreach(var layer in contract.CededContractLayers)
			{
				Console.WriteLine("{0} {1} {2} {3}",
					layer.CededContract?.CededContractPkey,
					layer.Description,
					layer.LayerType?.Description,
					layer.Limit
					);
			}

		}
		static void SqlDriverTest()
		{
			var serviceProvider = new ServiceCollection()
				.AddLogging(
					c => c
						.AddConsole()
						.SetMinimumLevel(LogLevel.Debug)
				)
				.AddSingleton<IDatabaseDriver, MSSqlDriver>()
				.AddSingleton<INodeMap, NodeMap>()
				.AddSingleton<IMSSqlGenerator, MSSqlGenerator>()
				.BuildServiceProvider()
				;
			var map = serviceProvider.GetService<INodeMap>();

			map.BuildNodes();

			var driver = serviceProvider.GetService<IDatabaseDriver>();
			var connectionString = @"Server=localhost,1433; Database=DbEntityServiceTestDb; User Id=DbEntityService; Password=Password1";
			var lookups = driver.Select<TLookups>(connectionString, map.GetNode(typeof(TLookups)), (t) => t.LookupsPkey == 20 && t.LookupsGroupPkey == 1);
			foreach(var lookup in lookups)
			{
				Console.WriteLine("{0}, {1}, {2}, {3}", lookup.LookupsPkey, lookup.LookupsGroupPkey, lookup.Description, lookup.Version);
			}
		}
		*/
	}
	}
