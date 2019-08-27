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
						.SetMinimumLevel(LogLevel.Information)
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
								.Bootstrap(null, new[] { "AXAXL.DbEntity.UnitTestLib" });

			TCededContract contract;
			
			contract = GetAndPrintContract(service, 100);

			var stopLoss = service.Query<TLookups>().Where(l => l.Description == "Stop Loss").ToList().FirstOrDefault();
			var newLayer = new TCededContractLayer
			{
				Description = $"Layer at {DateTime.Now}",
				LayerType = stopLoss,
				AttachmentPoint = 1000000,
				Limit = 3000000,
				EntityStatus = EntityStatusEnum.New
			};

			contract.CededContractLayers.Add(newLayer);

			var rowInserted = service.Persist()
				.Submit(c => c.Save(contract))
				.Commit();
			Console.WriteLine("Saved {0}", rowInserted);
			Console.WriteLine(
				string.Join(
					Environment.NewLine,
					contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey,-10}, {l.Description,-50}, {l.LayerType.Description,-30}, {l.AttachmentPoint}/{l.Limit}, {l.ModifyDate}"))
				);

			contract = GetAndPrintContract(service, 100);

			var layerRefreshed = contract.CededContractLayers.Where(l => l.CededContractLayerPkey == newLayer.CededContractLayerPkey).FirstOrDefault();

			if (layerRefreshed == null)
			{
				Console.WriteLine("Layer {0} not found", newLayer.CededContractLayerPkey);
			}
			else
			{
				layerRefreshed.Limit += 1000000;
				layerRefreshed.EntityStatus = EntityStatusEnum.Updated;
				var rowUpdated = service.Persist()
					.Submit(c => c.Save(contract))
					.Commit();

				Console.WriteLine("Updated {0}", rowUpdated);
				Console.WriteLine(
					string.Join(
						Environment.NewLine,
						contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey,-10}, {l.Description,-50}, {l.LayerType.Description,-30}, {l.AttachmentPoint}/{l.Limit}, {l.ModifyDate}"))
					);
			}
			contract = GetAndPrintContract(service, 100);

			layerRefreshed = contract.CededContractLayers.Where(l => l.CededContractLayerPkey == newLayer.CededContractLayerPkey).FirstOrDefault();

			if (layerRefreshed == null)
			{
				Console.WriteLine("Layer {0} not found", newLayer.CededContractLayerPkey);
			}
			else
			{
				layerRefreshed.EntityStatus = EntityStatusEnum.Deleted;
				var rowUpdated = service.Persist()
					.Submit(c => c.Save(contract))
					.Commit();

				Console.WriteLine("Deleted {0}", rowUpdated);
				Console.WriteLine(
					string.Join(
						Environment.NewLine,
						contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey,-10}, {l.Description,-50}, {l.LayerType.Description,-30}, {l.AttachmentPoint}/{l.Limit}, {l.ModifyDate}"))
					);
			}

			contract = GetAndPrintContract(service, 100);

			/*
						var doc = service.Query<TCededContractLayerDoc>()
										.Where(c => c.DocGuid == 3)
										.ToArray()
										.FirstOrDefault();
						Console.WriteLine("T_Doc returned.  Owner Type {0} Owner Guid {1}", doc?.OwnerType, doc?.OwnerGuid);
			*/
		}

		private static TCededContract GetAndPrintContract(IDbService service, int contractNum)
		{
			var contracts = service.Query<TCededContract>()
				.Where(c => c.CededContractNum == contractNum)
				.ToArray();
			Console.WriteLine("Refreshed contract from database.  Found {0} contracts", contracts.Length);
			var contract = contracts.FirstOrDefault();
			Console.WriteLine(
				string.Join(
					Environment.NewLine,
					contract.CededContractLayers.Select(l => $"{l.CededContractLayerPkey,-10}, {l.Description,-50}, {l.LayerType.Description,-30}, {l.AttachmentPoint}/{l.Limit}, {l.ModifyDate}"))
				);
			return contract;
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
