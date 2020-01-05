using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Benchmarks.Models;

using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace AXAXL.DbEntity.Benchmarks
{
	public class BenchmarkBase
	{
		protected string ConnecitonString { get; set; }
		protected IDbService DbService { get; set; }

		protected void Setup()
		{
			var config = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
								.AddEnvironmentVariables()
								.Build();
			this.ConnecitonString = config["ConnectionString:CLR"];

			var services = new ServiceCollection()
				.AddLogging(
					c => c
						.AddConsole()
						.SetMinimumLevel(LogLevel.Information)
				)
				.AddMemoryCache()
				;
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterModule(new AutofacModule(config));
			containerBuilder.Populate(services);

			var serviceProvider = new AutofacServiceProvider(containerBuilder.Build());

			#region Commented.  DI without Autofac
			/*
			 *			var serviceProvider = new ServiceCollection()
							.AddLogging(
								c => c
									.AddConsole()
									.SetMinimumLevel(LogLevel.Information)
							)
							.AddSqlDbEntityService(
								option => option
											.AddOrUpdateConnection("SQL_Connection", this.ConnecitonString)
											.SetAsDefaultConnection("SQL_Connection")
											.PrintNodeMapToFile(config.GetValue<string>(@"DbEntity:NodeMapExport"))
							)
							.BuildServiceProvider()
							;
			*/
			#endregion

			this.DbService = serviceProvider
					.GetService<IDbService>()
					.Bootstrap();
		}


	}
}
