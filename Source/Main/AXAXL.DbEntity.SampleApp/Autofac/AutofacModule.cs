using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;

namespace AXAXL.DbEntity.SampleApp.Autofac
{
	public class AutofacModule : Module
	{
		private IConfiguration Configuration { get; }
		public AutofacModule(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}
		protected override void Load(ContainerBuilder builder)
		{
			// use this extension when debugging SQL generation which is not being cached.
			builder.AddSqlDbEntityService(
					dbOption => dbOption
								.AddOrUpdateConnection("BookDb", this.Configuration["ConnectionString:BooksDB"])
								.SetAsDefaultConnection("BookDb")
								.PrintNodeMapToFile(this.Configuration.GetValue<string>(@"DbEntity:NodeMapExport")));

			// use this extension for production and QA when optimized performance is needed.
			/*
			builder.AddSqlDbEntityServiceWithCacheForSqlGenerator(
					dbOption => dbOption
								.AddOrUpdateConnection("BookDb", this.Configuration["ConnectionString:BooksDB"])
								.SetAsDefaultConnection("BookDb")
								.PrintNodeMapToFile(this.Configuration.GetValue<string>(@"DbEntity:NodeMapExport")));
			*/
		}
	}
}
