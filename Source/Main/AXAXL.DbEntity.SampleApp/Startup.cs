using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AXAXL.DbEntity.SampleApp.Models;
using AXAXL.DbEntity.SampleApp.Models.DataManager;
using AXAXL.DbEntity.SampleApp.Models.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.Types.NewtonsoftConverters;
using Autofac;
using AXAXL.DbEntity.SampleApp.Autofac;
using Newtonsoft.Json;

namespace AXAXL.DbEntity.SampleApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			//var filePath = Path.Combine(System.AppContext.BaseDirectory, "AXAXL.DbEntity.SampleApp.xml");
			services
				// If Autofac is not used, the following will install DbEntitySerivces into the IoC container.
				//.AddSqlDbEntityService(
				//	dbOption => dbOption
				//				.AddOrUpdateConnection("BookDb", Configuration["ConnectionString:BooksDB"])
				//				.SetAsDefaultConnection("BookDb")
				//				.PrintNodeMapToFile(Configuration.GetValue<string>(@"DbEntity:NodeMapExport"))
				//)
				.AddScoped<IDataRepository<Author>, AuthorDataManager>()
				.AddScoped<IDataRepository<Book>, BookDataManager>()
				.AddScoped<IDataRepository<Publisher>, PublisherDataManager>()
				.AddScoped<IDataRepository<BookCategory>, BookCategoryDataManager>()
				.AddSwaggerGen(
					c =>
					{
						c.SwaggerDoc("v1", new Info
						{
							Title = "Distributed Transactions Feasibility Study API",
							Version = "v1",
							Description = "Sample App using AXAXL.DbEntity library",
							TermsOfService = "None"
						});
						//c.IncludeXmlComments(filePath);
					}
				)
				;
			services
				.AddMvc()
				.AddJsonOptions(
					options => 
					{
						options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
						if (options.SerializerSettings.Converters != null)
						{
							options.SerializerSettings.Converters.Add(new RowVersionConverter());
						}
						else
						{
							options.SerializerSettings.Converters = new[] { new RowVersionConverter() };
						}
					}
				)
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				;
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app
					.UseDeveloperExceptionPage()
					.UseSwagger()
					.UseSwaggerUI(
						c =>
						{
							c.SwaggerEndpoint("/swagger/v1/swagger.json", "DbEntity Sample App API v1");
							//c.RoutePrefix = string.Empty;
						}
					)
					;
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseMvc();

			var dbService = app.ApplicationServices
				.GetService<IDbService>()
				.Bootstrap(
					//new[] { typeof(AXAXL.DbEntity.SampleApp.Models.Author).Assembly },
					//new[] { @"AXAXL.DbEntity.SampleApp.Models" }
				);
        }

		// Hide the following code if Autofac is not used.
		public void ConfigureContainer(ContainerBuilder builder)
		{
			builder.RegisterModule(new AutofacModule(this.Configuration));
		}
	}
}
