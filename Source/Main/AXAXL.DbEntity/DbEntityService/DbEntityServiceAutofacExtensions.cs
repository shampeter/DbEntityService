using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using AXAXL.DbEntity.Interfaces;
using AXAXL.DbEntity.MSSql;
using AXAXL.DbEntity.Services;
using AXAXL.DbEntity.EntityGraph;
using AXAXL.DbEntity.MSSql.Autofac;

namespace Autofac
{
	public static class DbEntityServiceAutofacExtensions
	{
		public static ContainerBuilder AddSqlDbEntityService(this ContainerBuilder builder, Action<IDbServiceOption> config)
		{
			builder
				.RegisterType<MSSqlDriver>()
				.As<IDatabaseDriver>()
				.SingleInstance()
				;
			builder
				.RegisterType<NodeMap>()
				.As<INodeMap>()
				.SingleInstance()
				;
			builder
				.RegisterType<MSSqlGenerator>()
				.As<IMSSqlGenerator>()
				.SingleInstance()
				;
			builder
				.Register(context => {
					var option = new DbServiceOption();
					config?.Invoke(option);
					return option;
				})
				.As<IDbServiceOption>()
				.SingleInstance()
				;
			builder
				.RegisterType<DbService>()
				.As<IDbService>()
				.SingleInstance()
				;

			return builder;
		}

		public static ContainerBuilder AddSqlDbEntityServiceWithCacheForSqlGenerator(this ContainerBuilder builder, Action<IDbServiceOption> config)
		{
			builder
				.RegisterType<MSSqlDriver>()
				.As<IDatabaseDriver>()
				.SingleInstance()
				;
			builder
				.RegisterType<NodeMap>()
				.As<INodeMap>()
				.SingleInstance()
				;
			builder
				.RegisterType<MSSqlGenerator>()
				.As<IMSSqlGenerator>()
				.EnableInterfaceInterceptors(
					new ProxyGenerationOptions(
						new MSSqlGeneratorResponseCache.MethodSelectionHookForSQLGenCache()
				))
				.SingleInstance()
				;
			builder
				.Register(context => {
					var option = new DbServiceOption();
					config?.Invoke(option);
					return option;
				})
				.As<IDbServiceOption>()
				.SingleInstance()
				;
			builder
				.RegisterType<DbService>()
				.As<IDbService>()
				.SingleInstance()
				;
			builder
				.RegisterType<MSSqlGeneratorResponseCache>()
				.Named<IInterceptor>(MSSqlGeneratorResponseCache.C_MS_SQL_GENERATOR_CACHE_INTERCEPTOR_NAME)
				;

			return builder;
		}
	}
}
